using System.Net.Http.Json;
using System.Text.Json;
using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Cloud storage for a user's saved calculations, over Supabase PostgREST. Every
/// request carries the user's access token; Row Level Security scopes all reads and
/// writes to that user, so there is no user filter in the queries here — the
/// database enforces it.
///
/// Persists the same <see cref="CalculationState"/> the share links use: inputs only,
/// as a jsonb object. Reopening re-runs the engine.
/// </summary>
public class CalculationStorageService
{
    private readonly HttpClient _http;
    private readonly SupabaseConfig _config;
    private readonly SupabaseAuthService _auth;

    public CalculationStorageService(HttpClient http, SupabaseConfig config, SupabaseAuthService auth)
    {
        _http = http;
        _config = config;
        _auth = auth;
    }

    /// <summary>
    /// The current user's saved calculations, newest first. Empty list if signed out
    /// or on error — the caller shows an empty state either way.
    /// </summary>
    public async Task<List<SavedCalculation>> ListAsync(string? moduleKey = null)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token == null) return new();

        var url = $"{_config.RestUrl}/calculations" +
                  "?select=id,module_key,title,inputs,created_at,updated_at" +
                  "&order=updated_at.desc";
        if (!string.IsNullOrEmpty(moduleKey))
            url += $"&module_key=eq.{Uri.EscapeDataString(moduleKey)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuth(request, token);

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var list = await response.Content.ReadFromJsonAsync<List<SavedCalculation>>();
            return list ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Inserts a new saved calculation. user_id is sent explicitly to satisfy the
    /// RLS insert check (auth.uid() = user_id); the check makes it un-spoofable.
    /// Returns (ok, error).
    /// </summary>
    public async Task<(bool ok, string? error)> SaveAsync(string moduleKey, string title, CalculationState state)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        var userId = _auth.CurrentSession?.UserId;
        if (token == null || string.IsNullOrEmpty(userId))
            return (false, "You are not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.RestUrl}/calculations");
        AddAuth(request, token);
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = JsonContent.Create(new
        {
            user_id = userId,
            module_key = moduleKey,
            title = string.IsNullOrWhiteSpace(title) ? moduleKey : title.Trim(),
            inputs = state.Values
        });

        try
        {
            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);

            var body = await response.Content.ReadAsStringAsync();
            return (false, ExtractError(body) ?? $"Save failed ({(int)response.StatusCode}).");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>Deletes one saved calculation. RLS ensures a user can only delete their own.</summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token == null) return false;

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_config.RestUrl}/calculations?id=eq.{Uri.EscapeDataString(id)}");
        AddAuth(request, token);

        try
        {
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void AddAuth(HttpRequestMessage request, string token)
    {
        request.Headers.Add("apikey", _config.PublishableKey);
        request.Headers.Add("Authorization", $"Bearer {token}");
    }

    private static string? ExtractError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            foreach (var key in new[] { "message", "error_description", "error", "hint" })
            {
                if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
        }
        catch (JsonException) { }
        return null;
    }
}
