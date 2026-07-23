using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Turns a <see cref="CalculationState"/> into a shareable link and reads one back.
///
/// The payload rides in the URL *fragment* (#s=...), not the query string, for
/// three reasons: fragments are never sent to the server, so a shared engineering
/// calculation does not leak into server logs or Google Analytics; they do not
/// interfere with the 404.html SPA rewrite GitHub Pages relies on; and they leave
/// the canonical URL intact for SEO.
/// </summary>
public class CalculationShareService
{
    private const string FragmentKey = "s=";

    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _js;

    public CalculationShareService(NavigationManager navigation, IJSRuntime js)
    {
        _navigation = navigation;
        _js = js;
    }

    /// <summary>Absolute link that restores <paramref name="state"/> when opened.</summary>
    public string BuildLink(CalculationState state)
    {
        var current = new Uri(_navigation.Uri);
        var baseUrl = current.GetLeftPart(UriPartial.Path);
        return $"{baseUrl}#{FragmentKey}{ToBase64Url(state.Serialize())}";
    }

    /// <summary>
    /// Reads shared state out of the current URL, or null if there is none.
    ///
    /// <paramref name="expectedModule"/> guards against a link for one calculator
    /// being opened on another — which would otherwise restore a plausible-looking
    /// but meaningless set of inputs.
    /// </summary>
    public CalculationState? TryReadFromUrl(string expectedModule)
    {
        var fragment = new Uri(_navigation.Uri).Fragment;
        if (fragment.Length <= 1) return null;

        fragment = fragment.TrimStart('#');
        if (!fragment.StartsWith(FragmentKey, StringComparison.Ordinal)) return null;

        var payload = FromBase64Url(fragment[FragmentKey.Length..]);
        if (payload == null) return null;

        var state = CalculationState.Deserialize(payload);
        if (state == null || !string.Equals(state.Module, expectedModule, StringComparison.Ordinal))
            return null;

        return state;
    }

    /// <summary>
    /// Drops the payload from the address bar after a successful restore, without
    /// adding a history entry. Otherwise the stale fragment would reappear if the
    /// user edited the inputs and then reloaded the page.
    /// </summary>
    public async Task ClearUrlFragmentAsync()
    {
        try { await _js.InvokeVoidAsync("mekanikaShare.clearFragment"); }
        catch { /* cosmetic only — never break a restore over this */ }
    }

    /// <summary>
    /// Copies to the clipboard. Returns false when the browser refuses (no
    /// permission, or a non-secure context), so the caller can show the link for
    /// manual copying instead.
    /// </summary>
    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try { return await _js.InvokeAsync<bool>("mekanikaShare.copy", text); }
        catch { return false; }
    }

    // ============ BASE64URL ============
    // Plain base64 is not URL-safe: '+' and '/' get mangled by chat clients and
    // mail rewriters, and '=' padding trips some link parsers.

    private static string ToBase64Url(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                  .TrimEnd('=')
                  .Replace('+', '-')
                  .Replace('/', '_');

    private static string? FromBase64Url(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => null!   // length % 4 == 1 is never valid base64
        };

        if (padded == null) return null;

        try { return Encoding.UTF8.GetString(Convert.FromBase64String(padded)); }
        catch (FormatException) { return null; }
        catch (DecoderFallbackException) { return null; }
    }
}
