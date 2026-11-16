// shell.js - the two things the app shell asks of the page itself.
//
// Both are here rather than in share.js / analytics.js because neither belongs to a
// feature: they are page chrome. This file is NOT deferred in index.html, because
// setBuildStamp has to exist by the time Blazor's startup code runs.

// ---- build stamp -----------------------------------------------------------
// The stamp is a compiled constant inside the WASM assembly, so the only way the
// JavaScript side can learn it is to be told. Program.cs calls this during host
// startup, before the first render; pdfGenerator.js (deferred, so later) reads the
// value when it stamps a report footer.
window.mekanikaBuildStamp = '';

window.mekanikaSetBuildStamp = function (stamp) {
    window.mekanikaBuildStamp = stamp || '';
};

// ---- scroll to the results --------------------------------------------------
// Every calculator is one page with two views: the input form, then the results in
// its place. Pressing Calculate at the bottom of a long form swapped the view and
// left the scroll position where it was, so on gear-pair or torsion-spring the
// reader stayed looking at the middle of a results table with no idea the top had
// changed. Nothing in the app called scrollTo before this.
//
// The jump is INSTANT, not smooth, and that is deliberate on two counts. A smooth
// scroll is an animation that competes with the layout: the results are far taller
// than the form, and Chrome's scroll anchoring moved the position back down mid-flight
// - measured, the page ended up at y = 3966 having started at 1533. It is also simply
// not honoured everywhere; in the embedded browser the request was dropped and the page
// never moved at all, while an instant scroll landed every time. And what is happening
// here is a view being replaced wholesale, which reads as a page change rather than as
// travel across one page.
//
// Called twice: once now, once after two frames, so that a layout which settles after
// the first call cannot leave the reader half way down.
window.mekanikaScrollToTop = function () {
    var go = function () {
        try {
            window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
        } catch (e) {
            window.scrollTo(0, 0);
        }
    };

    go();

    if (window.requestAnimationFrame) {
        requestAnimationFrame(function () { requestAnimationFrame(go); });
    }
};
