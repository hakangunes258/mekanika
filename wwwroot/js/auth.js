// Auth storage + OAuth-callback fragment parsing.
//
// Paired with Services/SupabaseAuthService.cs. Kept small: token exchange and the
// session model live in C#; this only touches the two browser things Blazor cannot
// do cleanly — localStorage and reading/clearing the URL fragment that Supabase
// returns tokens in.

window.mekanikaAuth = {
    get: function (key) {
        try { return localStorage.getItem(key); } catch (e) { return null; }
    },

    set: function (key, value) {
        try { localStorage.setItem(key, value); } catch (e) { /* private mode / quota */ }
    },

    remove: function (key) {
        try { localStorage.removeItem(key); } catch (e) { /* ignore */ }
    },

    // After a Google / magic-link redirect, Supabase returns the session in the URL
    // fragment: #access_token=...&refresh_token=...&expires_in=...&token_type=bearer
    // (or #error=...&error_description=...). Returns it as a flat object, or {} if
    // there is no fragment.
    readHashParams: function () {
        const hash = window.location.hash;
        if (!hash || hash.length < 2) return {};

        const params = new URLSearchParams(hash.substring(1));
        const result = {};
        for (const [k, v] of params.entries()) result[k] = v;
        return result;
    },

    // Drops the fragment after we have consumed the tokens, so they do not linger in
    // the address bar or a reload. No history entry added.
    clearHash: function () {
        if (window.location.hash) {
            history.replaceState(null, '', window.location.pathname + window.location.search);
        }
    }
};
