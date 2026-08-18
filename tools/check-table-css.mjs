#!/usr/bin/env node
/*
    The eleven single-shape calculators each declare the same .results-table
    column rules in their own <style> block. They are duplicates, and they had
    quietly drifted: eight used 40/15/25/20, three used 45/15/25/15, and one had
    a different monospace fallback. Nothing surfaces that - the pages look fine
    on their own and only disagree when you put two of them side by side, which
    is how it was eventually found.

    gear-pair is deliberately excluded. It is the only page with more than one
    table shape, so its rules are column-count-aware (`:nth-child(n):nth-last-child(m)`)
    and cannot match this set; they are checked by rendering, not by text.

    Why the duplicates still exist: moving them into modern-icons.css would need
    scoping, because MyCalculations and Account use .results-table for a saved-list
    and a key/value table and would be restyled by it. Scoping with the obvious
    `#results-content` prefix would then out-specify gear-pair's rules - an id beats
    any number of pseudo-classes - and silently re-lay-out its 5-column tables.
    Until that is untangled, one canonical copy checked by this script.
*/

import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

const PAGES = 'Pages';
const EXCLUDE = new Set(['GearPair.razor']);
const RULE = /\.results-table (?:th|td):nth-child\(\d\)[^}]*\}/gs;

/*
    Razor parses the body of a <style> block as markup and knows nothing about CSS
    comment syntax, so a tag name written in angle brackets inside a CSS comment is
    read as a nested open tag: the next real closing tag matches it, the outer block
    is left unclosed, and the build dies with RZ9980 pointing at the opening line
    rather than at the comment. dotnet does catch it - half a minute later, and
    without saying why. This says why.
*/
for (const file of readdirSync(PAGES).filter(f => f.endsWith('.razor'))) {
    const src = readFileSync(join(PAGES, file), 'utf8');
    for (const block of src.matchAll(/<style>(.*?)<\/style>/gs)) {
        const stray = block[1].match(/<\/?[a-zA-Z][a-zA-Z0-9]*\s*\/?>/g);
        if (stray) {
            console.error(`::error file=${PAGES}/${file}::markup tag ${stray[0]} inside a <style> block. Razor reads it as a real tag and the block will not close (RZ9980). Write the tag name without angle brackets.`);
            process.exit(1);
        }
    }
}

const found = new Map();

for (const file of readdirSync(PAGES).filter(f => f.endsWith('.razor'))) {
    if (EXCLUDE.has(file)) continue;
    const rules = readFileSync(join(PAGES, file), 'utf8').match(RULE);
    if (!rules || rules.length < 4) continue;          /* not a standard results page */
    const key = rules.join(' ').replace(/\s+/g, ' ').trim();
    if (!found.has(key)) found.set(key, []);
    found.get(key).push(file);
}

if (found.size === 0) {
    console.error('ERROR: no page declares .results-table column rules - has the markup changed?');
    process.exit(1);
}

if (found.size === 1) {
    const [[, files]] = [...found];
    console.log(`.results-table column rules identical across ${files.length} pages.`);
    process.exit(0);
}

console.error(`::error::.results-table column rules have drifted into ${found.size} variants. Every single-shape calculator must declare the same block; pick one and apply it to all.`);
let n = 0;
for (const [rules, files] of found) {
    const widths = [...rules.matchAll(/th:nth-child\((\d)\) \{ width: (\d+)%/g)].map(m => m[2]).join('/');
    console.error(`  variant ${++n} (widths ${widths || 'n/a'}): ${files.join(', ')}`);
}
process.exit(1);
