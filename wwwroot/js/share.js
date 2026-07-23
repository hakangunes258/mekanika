// Shareable calculation links.
//
// Paired with Services/CalculationShareService.cs. Kept deliberately small: the
// payload is built and parsed in C#, this file only touches the two browser APIs
// Blazor cannot reach cleanly on its own.

window.mekanikaShare = {
    // Returns true only on a confirmed copy. The caller shows the link for manual
    // copying when this is false, so a silent failure must never report success.
    copy: async function (text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch (e) {
            // Permission denied, or the document was not focused. Fall through.
        }

        // Fallback for non-secure contexts (e.g. the local dev server on http).
        try {
            const area = document.createElement('textarea');
            area.value = text;
            area.setAttribute('readonly', '');
            area.style.position = 'fixed';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            const ok = document.execCommand('copy');
            document.body.removeChild(area);
            return ok;
        } catch (e) {
            return false;
        }
    },

    // Selects the link text so Ctrl+C works when the clipboard API is blocked.
    selectInput: function (el) {
        if (el) {
            el.focus();
            el.select();
        }
    },

    // Strips #s=... after a restore, without pushing a history entry.
    clearFragment: function () {
        if (window.location.hash) {
            history.replaceState(null, '', window.location.pathname + window.location.search);
        }
    }
};
