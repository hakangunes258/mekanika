using System.Net.Http.Json;
using System.Text.Json;
using MechanicalCalculatorWeb.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Talks to Supabase GoTrue (auth) and PostgREST (the delete-account RPC) over plain
/// HttpClient — no Supabase SDK, to keep the WASM download small. Owns the persisted
/// <see cref="UserSession"/>; the AuthenticationStateProvider layers ClaimsPrincipal
/// on top of it.
/// </summary>
public class SupabaseAuthService
{
    private const string SessionStorageKey = "mekanika.session";

    private readonly HttpClient _http;
    private readonly SupabaseConfig _config;
    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _js;

    private UserSession? _session;

    public SupabaseAuthService(HttpClient http, SupabaseConfig config, NavigationManager navigation, IJSRuntime js)
    {
        _http = http;
        _config = config;
        _navigation = navigation;
        _js = js;
    }

    /// <summary>Raised whenever the session appears or disappears, so the UI can re-render.</summary>
    public event Action? AuthStateChanged;

    public UserSession? CurrentSession => _session;
    public bool IsSignedIn => _session != null && !_session.IsExpired;

    // ============ SESSION LOAD / PERSIST ============

    /// <summary>
    /// Loads the session from localStorage on startup, refreshing the token if it has
    /// expired. Safe to call repeatedly; only touches storage.
    /// </summary>
    public async Task<UserSession?> InitializeAsync()
    {
        var raw = await _js.InvokeAsync<string?>("mekanikaAuth.get", SessionStorageKey);
        if (string.IsNullOrEmpty(raw)) return null;

        try
        {
            _session = JsonSerializer.Deserialize<UserSession>(raw);
        }
        catch (JsonException)
        {
            await ClearSessionAsync();
            return null;
        }

        if (_session == null) return null;

        // A stored-but-expired token is common (tab closed for an hour); try to refresh
        // silently before giving up on the session.
        if (_session.IsExpired)
        {
            var refreshed = await RefreshAsync();
            if (!refreshed) return null;
        }

        return _session;
    }

    private async Task PersistSessionAsync(UserSession session)
    {
        _session = session;
        await _js.InvokeVoidAsync("mekanikaAuth.set", SessionStorageKey,
            JsonSerializer.Serialize(session));
        AuthStateChanged?.Invoke();
    }

    private async Task ClearSessionAsync()
    {
        _session = null;
        await _js.InvokeVoidAsync("mekanikaAuth.remove", SessionStorageKey);
        AuthStateChanged?.Invoke();
    }

    // ============ SIGN IN ============

    /// <summary>
    /// Redirects the browser into Google's consent flow. Google returns to
    /// {origin}/auth/callback, where <see cref="CompleteFromCallbackAsync"/> finishes.
    /// </summary>
    public void SignInWithGoogle()
    {
        var redirectTo = Uri.EscapeDataString($"{Origin()}/auth/callback");
        var url = $"{_config.AuthUrl}/authorize?provider=google&redirect_to={redirectTo}";
        _navigation.NavigateTo(url, forceLoad: true);
    }

    /// <summary>
    /// Sends a magic-link / OTP email. The link lands on {origin}/auth/callback with
    /// the session in the fragment. Returns (ok, error) so the page can show a message.
    /// </summary>
    public async Task<(bool ok, string? error)> SendMagicLinkAsync(string email)
    {
        // GoTrue takes the post-click destination as the `redirect_to` query param,
        // not a body field — putting it in the body would silently fall back to the
        // project's Site URL.
        var redirectTo = Uri.EscapeDataString($"{Origin()}/auth/callback");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.AuthUrl}/otp?redirect_to={redirectTo}");
        AddApiKey(request);
        request.Content = JsonContent.Create(new
        {
            email,
            create_user = true
        });

        try
        {
            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);

            var body = await response.Content.ReadAsStringAsync();
            return (false, ExtractError(body) ?? $"Request failed ({(int)response.StatusCode}).");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Consumes the tokens Supabase left in the URL fragment after a redirect, loads
    /// the user, and persists the session. Returns (ok, error).
    /// </summary>
    public async Task<(bool ok, string? error)> CompleteFromCallbackAsync()
    {
        var hash = await _js.InvokeAsync<Dictionary<string, string>>("mekanikaAuth.readHashParams");
        await _js.InvokeVoidAsync("mekanikaAuth.clearHash");

        if (hash.TryGetValue("error_description", out var errDesc))
            return (false, Uri.UnescapeDataString(errDesc));
        if (hash.TryGetValue("error", out var err))
            return (false, Uri.UnescapeDataString(err));

        if (!hash.TryGetValue("access_token", out var accessToken) ||
            !hash.TryGetValue("refresh_token", out var refreshToken))
            return (false, "No session was returned. The link may have expired.");

        long expiresAt = hash.TryGetValue("expires_at", out var eaRaw) && long.TryParse(eaRaw, out var ea)
            ? ea
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds()
              + (hash.TryGetValue("expires_in", out var eiRaw) && long.TryParse(eiRaw, out var ei) ? ei : 3600);

        var user = await FetchUserAsync(accessToken);
        if (user == null) return (false, "Could not read the user profile.");

        await PersistSessionAsync(new UserSession
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.Value.id,
            Email = user.Value.email
        });

        return (true, null);
    }

    // ============ REFRESH / SIGN OUT / DELETE ============

    /// <summary>Exchanges the refresh token for a new access token. False clears the session.</summary>
    public async Task<bool> RefreshAsync()
    {
        if (_session == null || string.IsNullOrEmpty(_session.RefreshToken)) return false;

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.AuthUrl}/token?grant_type=refresh_token");
        AddApiKey(request);
        request.Content = JsonContent.Create(new { refresh_token = _session.RefreshToken });

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await ClearSessionAsync();
                return false;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? "";
            var refreshToken = root.GetProperty("refresh_token").GetString() ?? "";
            long expiresAt = root.TryGetProperty("expires_at", out var ea)
                ? ea.GetInt64()
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                  + (root.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 3600);

            var email = _session.Email;
            var userId = _session.UserId;
            if (root.TryGetProperty("user", out var userEl))
            {
                email = userEl.GetProperty("email").GetString() ?? email;
                userId = userEl.GetProperty("id").GetString() ?? userId;
            }

            await PersistSessionAsync(new UserSession
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                UserId = userId,
                Email = email
            });
            return true;
        }
        catch
        {
            await ClearSessionAsync();
            return false;
        }
    }

    public async Task SignOutAsync()
    {
        if (_session != null && !string.IsNullOrEmpty(_session.AccessToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.AuthUrl}/logout");
            AddApiKey(request);
            request.Headers.Add("Authorization", $"Bearer {_session.AccessToken}");
            try { await _http.SendAsync(request); }
            catch { /* clear locally regardless of the server response */ }
        }

        await ClearSessionAsync();
    }

    /// <summary>
    /// Deletes the account and all its data via the delete_current_user() RPC. On
    /// success the session is cleared. Returns (ok, error).
    /// </summary>
    public async Task<(bool ok, string? error)> DeleteAccountAsync()
    {
        if (_session == null || _session.IsExpired) return (false, "You are not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.RestUrl}/rpc/delete_current_user");
        AddApiKey(request);
        request.Headers.Add("Authorization", $"Bearer {_session.AccessToken}");
        request.Content = JsonContent.Create(new { });

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (false, ExtractError(body) ?? $"Deletion failed ({(int)response.StatusCode}).");
            }

            await ClearSessionAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ============ HELPERS ============

    private async Task<(string id, string email)?> FetchUserAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.AuthUrl}/user");
        AddApiKey(request);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            return (
                root.GetProperty("id").GetString() ?? "",
                root.TryGetProperty("email", out var e) ? e.GetString() ?? "" : ""
            );
        }
        catch
        {
            return null;
        }
    }

    private void AddApiKey(HttpRequestMessage request)
        => request.Headers.Add("apikey", _config.PublishableKey);

    private string Origin()
    {
        // BaseUri always ends with '/'; the callback route must not double it.
        var baseUri = _navigation.BaseUri.TrimEnd('/');
        return baseUri;
    }

    private static string? ExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            foreach (var key in new[] { "error_description", "msg", "message", "error" })
            {
                if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
        }
        catch (JsonException) { /* not JSON — fall through */ }
        return null;
    }
}
