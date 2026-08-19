#!/usr/bin/env node
/*
    Emits one real HTML file per route after `dotnet publish`, so GitHub Pages can
    answer a calculator URL with 200 and actual content instead of 404.html.

    Why this exists
    ---------------
    GitHub Pages has no SPA rewrite. It serves 404.html - with an HTTP 404 - for
    /interference-fit, so the routes in sitemap.xml were not indexable and every
    link preview (WhatsApp, LinkedIn, X, Slack, Bing) fell back to the site-level
    tags, with a canonical pointing at the home page. Writing <route>/index.html
    makes Pages serve the route directly: 200, its own title/description/canonical,
    its own OG tags and its own JSON-LD, none of which need JavaScript to be seen.

    Design decisions worth keeping
    ------------------------------
    * The page shell is the PUBLISHED index.html, transformed - not a template kept
      alongside it. A second copy of that <head> would drift the first time a script
      or a ?v= changes, and the drift would be invisible.
    * Module metadata comes from Services/ModuleMetadataService.cs, the same source
      the running app uses. A hand-maintained JSON copy with a "keep this in sync"
      note is exactly the drift this repo has been bitten by before.
    * The route list comes from sitemap.xml, so "the pages we generate" and "the
      pages we ask Google to crawl" cannot disagree. A sitemap URL with no metadata
      is a hard error; metadata with no sitemap entry is reported, because that is
      the /bolt orphan-page decision surfacing, not something to paper over.
    * Every replacement below asserts it matched exactly once. If index.html is
      restructured this fails loudly at build time rather than quietly emitting
      pages that have lost their canonical.

    Usage: node tools/generate-static-pages.mjs [--publish publish/wwwroot] [--repo .]
*/

import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';

const args = process.argv.slice(2);
const argOf = (name, dflt) => {
    const i = args.indexOf(name);
    return i >= 0 && args[i + 1] ? args[i + 1] : dflt;
};

const PUBLISH = argOf('--publish', 'publish/wwwroot');
const REPO = argOf('--repo', '.');
const ORIGIN = 'https://mekanika.org';

const fail = (msg) => { console.error(`ERROR: ${msg}`); process.exit(1); };

/* ---------------------------------------------------------------- metadata */

/*
    Pulls the ["key"] = new ModuleInfo { ... } entries out of the C# source. The
    parser is deliberately strict: a shape it does not recognise is an error, so a
    refactor of ModuleMetadataService breaks the build instead of silently emitting
    pages with empty descriptions.
*/
function parseModules(csharpPath) {
    const src = readFileSync(csharpPath, 'utf8');
    const header = /\n\s*\["([a-z0-9-]+)"\]\s*=\s*new ModuleInfo\s*\{/g;

    const starts = [];
    for (let m; (m = header.exec(src)) !== null;) {
        starts.push({ key: m[1], from: m.index, bodyFrom: header.lastIndex });
    }
    if (starts.length < 12) {
        fail(`only ${starts.length} module entries found in ${csharpPath} - the file's shape has changed`);
    }

    const str = (block, field) => {
        const m = new RegExp(`\\b${field}\\s*=\\s*"((?:[^"\\\\]|\\\\.)*)"`).exec(block);
        return m ? m[1].replace(/\\"/g, '"').replace(/\\\\/g, '\\') : null;
    };
    const strArray = (block, field) => {
        const m = new RegExp(`\\b${field}\\s*=\\s*new\\[\\]\\s*\\{([^}]*)\\}`).exec(block);
        if (!m) return [];
        return [...m[1].matchAll(/"((?:[^"\\]|\\.)*)"/g)].map(x => x[1]);
    };

    const modules = new Map();
    for (let i = 0; i < starts.length; i++) {
        const end = i + 1 < starts.length ? starts[i + 1].from : src.length;
        const block = src.slice(starts[i].bodyFrom, end);

        const name = str(block, 'Name');
        const route = str(block, 'Route');
        const description = str(block, 'Description');
        if (!name || !route || !description) {
            fail(`module "${starts[i].key}" is missing Name, Route or Description`);
        }

        modules.set(route, {
            key: starts[i].key,
            name,
            route,
            description,
            keywords: str(block, 'Keywords') || '',
            category: str(block, 'Category') || '',
            standards: strArray(block, 'VerificationStandards'),
            related: strArray(block, 'RelatedModules')
        });
    }
    return modules;
}

/* ----------------------------------------------------------------- routes */

/*
    Routes come from the sitemap. "/" is skipped - the real index.html already
    serves it, and rewriting it would strip the home page's own metadata.
*/
function parseRoutes(sitemapPath) {
    const xml = readFileSync(sitemapPath, 'utf8');
    const locs = [...xml.matchAll(/<loc>\s*([^<\s]+)\s*<\/loc>/g)].map(m => m[1]);
    if (!locs.length) fail(`no <loc> entries in ${sitemapPath}`);

    const routes = [];
    for (const loc of locs) {
        if (!loc.startsWith(ORIGIN)) fail(`sitemap URL outside ${ORIGIN}: ${loc}`);
        let path = loc.slice(ORIGIN.length).replace(/\/+$/, '');   /* tolerate either slash form */
        if (path === '') continue;                                  /* the home page */
        if (!path.startsWith('/')) path = '/' + path;
        routes.push(path);
    }
    return routes;
}

/* ------------------------------------------------------------ html helpers */

const esc = (s) => String(s)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');

/* Replace exactly one occurrence, or fail. Silent no-ops are the failure mode
   this whole script is meant to avoid. */
function replaceOnce(html, pattern, replacement, what, route) {
    /* Count with a global clone: html.match() on a non-global regex returns the
       capture groups, not the occurrences, which made a two-group pattern look
       like three matches. Patterns here stay group-free anyway. */
    const counter = new RegExp(pattern.source, pattern.flags.includes('g') ? pattern.flags : pattern.flags + 'g');
    const hits = html.match(counter);
    if (!hits || hits.length !== 1) {
        fail(`${route}: expected exactly one ${what} in index.html, found ${hits ? hits.length : 0}`);
    }
    /* Function form, so $& and $1 inside module descriptions stay literal. */
    return html.replace(pattern, () => replacement);
}

/*
    Blazor's router matches "@page /interference-fit" and does NOT match the
    trailing-slash form. GitHub Pages 301s /interference-fit to /interference-fit/
    to serve this file, so without this the app boots to a blank page.

    location.search and location.hash must both survive: a shared calculation is
    /key-connection#s=<payload>, and the fragment IS the payload. It rides through
    the 301 (browsers carry the fragment when the target has none of its own) and
    has to ride through this rewrite too.
*/
const SLASH_NORMALISER = `    <script>
        (function () {
            var p = location.pathname;
            if (p.length > 1 && p.charAt(p.length - 1) === '/') {
                history.replaceState(null, '', p.slice(0, -1) + location.search + location.hash);
            }
        })();
    </script>

`;

/*
    The reference section under a calculator, if that module has one. It is a
    plain HTML fragment in wwwroot/content/<key>.html, inlined here so a crawler
    that does not run JavaScript reads the whole thing - collapsed <details> and
    all, since collapsed is indexed and fetched-on-click is not.

    Shared/ModuleContent.razor fetches the SAME file at runtime. One copy, two
    readers; the alternative is two copies that drift.
*/
function moduleContent(publishDir, key) {
    const file = join(publishDir, 'content', `${key}.html`);
    if (!existsSync(file)) return null;
    return readFileSync(file, 'utf8').trim();
}

function staticBody(mod, content) {
    const standards = mod.standards.length
        /* Escape each name, then join - running the separator through esc() too
           printed a literal "&middot;" on the page. */
        ? `<p style="opacity:.75;font-size:13px;margin-top:10px;">${mod.standards.map(esc).join(' &middot; ')}</p>`
        : '';
    /*
        Sits inside the existing loading screen, so it is what a visitor sees for
        the second before Blazor takes over #app - and it is the whole of what a
        crawler that does not run JavaScript sees. It says what the page is; it
        does not claim anything the calculator does not do.
    */
    return `        <div class="loading-screen">
            <div class="loading-content">
                <img src="logo.png" alt="Mekanika" style="width: 280px; height: auto; margin-bottom: 16px; filter: brightness(0) invert(1);" />
                <h1 style="font-size:22px;font-weight:600;margin:0 0 8px;">${esc(mod.name)}</h1>
                <p style="max-width:620px;margin:0 auto;">${esc(mod.description)}</p>
                ${standards}
                <div class="spinner" style="margin-top: 20px;"></div>
                <noscript>
                    <p style="margin-top:16px;">This calculator runs in your browser and needs JavaScript enabled.</p>
                </noscript>
            </div>
        </div>${content ? `\n\n${content}` : ''}`;
}

function pageJsonLd(mod, url) {
    const data = {
        '@context': 'https://schema.org',
        '@type': 'SoftwareApplication',
        name: mod.name,
        url,
        description: mod.description,
        applicationCategory: 'EngineeringApplication',
        operatingSystem: 'Web Browser',
        browserRequirements: 'Requires JavaScript, WebAssembly support',
        offers: { '@type': 'Offer', price: '0', priceCurrency: 'USD' },
        isPartOf: { '@type': 'WebApplication', name: 'Mekanika', url: ORIGIN + '/' }
    };
    if (mod.standards.length) data.featureList = mod.standards.map(s => `Calculations to ${s}`);
    return `    <script type="application/ld+json">\n${JSON.stringify(data, null, 4).replace(/^/gm, '    ')}\n    </script>\n`;
}

/* ------------------------------------------------------------------- build */

const shellPath = join(PUBLISH, 'index.html');
if (!existsSync(shellPath)) fail(`${shellPath} not found - run dotnet publish first`);

const shell = readFileSync(shellPath, 'utf8');
const modules = parseModules(join(REPO, 'Services', 'ModuleMetadataService.cs'));
const routes = parseRoutes(join(PUBLISH, 'sitemap.xml'));

let written = 0;
let withContent = 0;
for (const route of routes) {
    const mod = modules.get(route);
    if (!mod) fail(`sitemap lists ${route} but ModuleMetadataService has no entry for it`);

    /*
        Canonical, og:url and the sitemap all use the trailing-slash form, because
        that is the URL GitHub Pages answers with 200. Pointing the canonical at
        the slash-less form would have the crawler follow a 301 and then be told
        the page it came from is the canonical one - a mixed signal for no gain.
    */
    const url = `${ORIGIN}${route}/`;
    const title = `${mod.name} - Free Online Tool | Mekanika`;
    const image = `${ORIGIN}/logo.png`;

    let html = shell;
    html = replaceOnce(html, /<title>[^<]*<\/title>/, `<title>${esc(title)}</title>`, '<title>', route);
    html = replaceOnce(html, /<meta name="title" content="[^"]*" \/>/, `<meta name="title" content="${esc(title)}" />`, 'meta name=title', route);
    html = replaceOnce(html, /<meta name="description" content="[^"]*" \/>/, `<meta name="description" content="${esc(mod.description)}" />`, 'meta description', route);
    html = replaceOnce(html, /<meta name="keywords" content="[^"]*" \/>/, `<meta name="keywords" content="${esc(mod.keywords)}" />`, 'meta keywords', route);
    html = replaceOnce(html, /<link rel="canonical" href="[^"]*" \/>/, `<link rel="canonical" href="${url}" />`, 'canonical', route);

    html = replaceOnce(html, /<meta property="og:url" content="[^"]*" \/>/, `<meta property="og:url" content="${url}" />`, 'og:url', route);
    html = replaceOnce(html, /<meta property="og:title" content="[^"]*" \/>/, `<meta property="og:title" content="${esc(mod.name)}" />`, 'og:title', route);
    html = replaceOnce(html, /<meta property="og:description" content="[^"]*" \/>/, `<meta property="og:description" content="${esc(mod.description)}" />`, 'og:description', route);
    html = replaceOnce(html, /<meta property="og:image" content="[^"]*" \/>/, `<meta property="og:image" content="${image}" />`, 'og:image', route);

    html = replaceOnce(html, /<meta property="twitter:url" content="[^"]*" \/>/, `<meta property="twitter:url" content="${url}" />`, 'twitter:url', route);
    html = replaceOnce(html, /<meta property="twitter:title" content="[^"]*" \/>/, `<meta property="twitter:title" content="${esc(mod.name)}" />`, 'twitter:title', route);
    html = replaceOnce(html, /<meta property="twitter:description" content="[^"]*" \/>/, `<meta property="twitter:description" content="${esc(mod.description)}" />`, 'twitter:description', route);
    html = replaceOnce(html, /<meta property="twitter:image" content="[^"]*" \/>/, `<meta property="twitter:image" content="${image}" />`, 'twitter:image', route);

    /* Page-level JSON-LD, added just before <base>, after the site-level block. */
    html = replaceOnce(html, /    <base href="\/" \/>/,
        `${pageJsonLd(mod, url)}\n    <base href="/" />`, '<base> tag', route);

    /* Crawlable content in place of the bare loading screen. */
    const content = moduleContent(PUBLISH, mod.key);
    if (content) withContent++;
    html = replaceOnce(html, /        <div class="loading-screen">[\s\S]*?\n        <\/div>/,
        staticBody(mod, content), 'loading screen block', route);

    /* Must run before the router does. */
    html = replaceOnce(html, /    <script src="_framework\/blazor\.webassembly\.js"><\/script>/,
        `${SLASH_NORMALISER}    <script src="_framework/blazor.webassembly.js"></script>`, 'blazor script tag', route);

    const out = join(PUBLISH, route.replace(/^\//, ''), 'index.html');
    mkdirSync(dirname(out), { recursive: true });
    writeFileSync(out, html, 'utf8');
    written++;
    console.log(`  ${route}/  ->  ${mod.name}`);
}

/* Metadata that no sitemap entry covers. Not fatal - /bolt is a known orphan and
   the decision on it belongs to a human - but it must not pass unmentioned. */
const orphans = [...modules.keys()].filter(r => !routes.includes(r));
if (orphans.length) {
    console.log(`\nNot generated (no sitemap entry): ${orphans.join(', ')}`);
}

console.log(`\n${written} static pages written to ${PUBLISH} (${withContent} with a reference section)`);
