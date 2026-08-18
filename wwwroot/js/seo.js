/*
    Per-page SEO metadata, driven from Services/SeoService.cs.

    THIS FILE MUST KEEP ITS LINE BREAKS. It shipped as a single minified line
    that still contained `//` comments, so the first one commented out the rest
    of the file: the last object literal never closed, the whole file was a
    SyntaxError, and none of these functions were ever defined. SeoService wraps
    every call in try/catch and "fails silently", so nothing surfaced - every
    calculator page simply kept index.html's generic meta tags for months.
    Never minify this file, and never use `//` in anything that might be joined
    onto one line. Block comments only.
*/

window.setSeoMetaTags = function (metadata) {
    if (!metadata) return;

    if (metadata.title) {
        document.title = metadata.title;
    }

    setMetaTag('description', metadata.description);
    setMetaTag('keywords', metadata.keywords);
    setLinkTag('canonical', metadata.canonicalUrl);

    setMetaTag('og:type', metadata.ogType || 'website', 'property');
    setMetaTag('og:title', metadata.ogTitle, 'property');
    setMetaTag('og:description', metadata.ogDescription, 'property');
    setMetaTag('og:image', metadata.ogImage, 'property');
    setMetaTag('og:url', metadata.ogUrl, 'property');

    setMetaTag('twitter:card', metadata.twitterCard);
    setMetaTag('twitter:title', metadata.twitterTitle);
    setMetaTag('twitter:description', metadata.twitterDescription);
    setMetaTag('twitter:image', metadata.twitterImage);
};

function setMetaTag(name, content, attributeName = 'name') {
    if (!content) return;

    let element = document.querySelector(`meta[${attributeName}="${name}"]`);
    if (!element) {
        element = document.createElement('meta');
        element.setAttribute(attributeName, name);
        document.head.appendChild(element);
    }
    element.setAttribute('content', content);
}

function setLinkTag(rel, href) {
    if (!href) return;

    let element = document.querySelector(`link[rel="${rel}"]`);
    if (!element) {
        element = document.createElement('link');
        element.setAttribute('rel', rel);
        document.head.appendChild(element);
    }
    element.setAttribute('href', href);
}

/*
    JSON-LD needs the reserved keys "@context" and "@type". The pages build their
    structured data as C# anonymous objects, where `@` is the verbatim-identifier
    prefix - `@context = "..."` declares a property called plain `context`, so the
    keys can only ever arrive here unprefixed. Renaming them on the way out fixes
    all fifteen pages at once; writing it in C# instead would need a hand-built
    dictionary per page. Keys that already carry `@` are left alone, which is what
    lets setStructuredDataWithRating hand us a literal "@type" below.
*/
function toJsonLdKeys(value) {
    if (Array.isArray(value)) {
        return value.map(toJsonLdKeys);
    }

    if (value === null || typeof value !== 'object') {
        return value;
    }

    const out = {};
    for (const key of Object.keys(value)) {
        const mapped = (key === 'context' || key === 'type') ? '@' + key : key;
        out[mapped] = toJsonLdKeys(value[key]);
    }
    return out;
}

window.setStructuredData = function (data) {
    if (!data) return;

    const existingScript = document.querySelector('script[type="application/ld+json"][data-mekanika="true"]');
    if (existingScript) {
        existingScript.remove();
    }

    const script = document.createElement('script');
    script.type = 'application/ld+json';
    script.setAttribute('data-mekanika', 'true');
    script.textContent = JSON.stringify(toJsonLdKeys(data));
    document.head.appendChild(script);
};

window.setStructuredDataWithRating = function (data, averageRating, ratingCount) {
    if (!data || !averageRating || !ratingCount || ratingCount === 0) {
        window.setStructuredData(data);
        return;
    }

    const dataWithRating = JSON.parse(JSON.stringify(data));
    dataWithRating.aggregateRating = {
        "@type": "AggregateRating",
        "ratingValue": averageRating.toFixed(1),
        "ratingCount": ratingCount,
        "bestRating": "5",
        "worstRating": "1"
    };

    window.setStructuredData(dataWithRating);
    console.log(`SEO: Added aggregate rating - ${averageRating.toFixed(1)} (${ratingCount} ratings)`);
};

window.createIntersectionObserver = function (dotnetHelper, element) {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                dotnetHelper.invokeMethodAsync('OnIntersection');
                observer.disconnect();
            }
        });
    }, {
        rootMargin: '100px',
        threshold: 0.01
    });

    observer.observe(element);
    return { dispose: () => observer.disconnect() };
};

console.log('Mekanika SEO helpers loaded successfully');
