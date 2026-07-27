using System.Net.Http.Json;
using System.Text.Json;
using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// The signed-in user's own additions to the built-in reference libraries, over
/// Supabase PostgREST. Every request carries the user's access token; Row Level
/// Security scopes all reads and writes to that user, so there is no user filter in
/// the queries here — the database enforces it.
///
/// <see cref="RefreshAsync"/> pushes the loaded items into the static providers
/// (today <see cref="MaterialService"/>), so every calculator sees the merged list
/// through the accessor it already uses. It runs once at startup — before the first
/// render — and again whenever the auth state changes, which keeps every consumer
/// synchronous. Signed out, it clears the custom items rather than leaving another
/// user's entries in the dropdowns.
/// </summary>
public class CustomLibraryService
{
    /// <summary>
    /// Pinned to BOTH ends of the `data` round-trip, because the two defaults do not
    /// agree: JsonContent.Create writes with JsonSerializerDefaults.Web (camelCase),
    /// while JsonElement.Deserialize&lt;T&gt;() with no options matches property names
    /// case-SENSITIVELY against PascalCase. Nothing errors — every property just
    /// silently reads back as its default. That shipped once: standards came back
    /// blank and every number came back 0. Do not let the two ends drift again.
    /// </summary>
    private static readonly JsonSerializerOptions LibraryJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SupabaseConfig _config;
    private readonly SupabaseAuthService _auth;

    public CustomLibraryService(HttpClient http, SupabaseConfig config, SupabaseAuthService auth)
    {
        _http = http;
        _config = config;
        _auth = auth;
    }

    /// <summary>Raised after the custom items change, so an open page can re-render.</summary>
    public event Action? Changed;

    /// <summary>
    /// Reloads every library from the server and republishes it. Safe to call at any
    /// time; signed out it just clears. Failures leave the libraries empty rather
    /// than stale — a calculator showing a material the user no longer owns would be
    /// worse than one showing only the built-ins.
    /// </summary>
    public async Task RefreshAsync()
    {
        var items = await ListAsync(LibraryItem.KindMaterial);
        MaterialService.SetCustomMaterials(items.Select(ToMaterial).Where(m => m != null)!);
        Changed?.Invoke();
    }

    // ============ MATERIALS ============

    /// <summary>
    /// Creates or updates one custom material and republishes the merged library.
    /// A non-null <see cref="Material.CustomId"/> means update. Returns (ok, error).
    /// </summary>
    public async Task<(bool ok, string? error)> SaveMaterialAsync(Material material)
    {
        var name = material.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return (false, "Give the material a name.");

        if (MaterialService.IsBuiltInName(name))
            return (false, $"“{name}” is already a built-in material. Choose another name.");

        material.Name = name;
        material.Standard = material.Standard.Trim();

        var result = material.CustomId == null
            ? await InsertAsync(LibraryItem.KindMaterial, name, material)
            : await UpdateAsync(material.CustomId, name, material);

        if (result.ok) await RefreshAsync();
        return result;
    }

    /// <summary>Deletes one custom material and republishes the merged library.</summary>
    public async Task<(bool ok, string? error)> DeleteMaterialAsync(string id)
    {
        var result = await DeleteAsync(id);
        if (result.ok) await RefreshAsync();
        return result;
    }

    private static Material? ToMaterial(LibraryItem item)
    {
        try
        {
            var material = item.Data.Deserialize<Material>(LibraryJson);
            if (material == null) return null;

            // The `name` column is the authority — it is the one the unique index and
            // every stored reference (share links, saved calculations) agree on.
            material.Name = item.Name;
            material.CustomId = item.Id;
            return material;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ============ GENERIC CRUD (one table, kind-tagged) ============

    private async Task<List<LibraryItem>> ListAsync(string kind)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token == null) return new();

        var url = $"{_config.RestUrl}/library_items" +
                  "?select=id,kind,name,data,created_at,updated_at" +
                  $"&kind=eq.{Uri.EscapeDataString(kind)}" +
                  "&order=name.asc";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuth(request, token);

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            return await response.Content.ReadFromJsonAsync<List<LibraryItem>>(LibraryJson) ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// user_id is sent explicitly to satisfy the RLS insert check (auth.uid() =
    /// user_id); the check is what makes it un-spoofable.
    /// </summary>
    private async Task<(bool ok, string? error)> InsertAsync(string kind, string name, object data)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        var userId = _auth.CurrentSession?.UserId;
        if (token == null || string.IsNullOrEmpty(userId))
            return (false, "You are not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.RestUrl}/library_items");
        AddAuth(request, token);
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = JsonContent.Create(new
        {
            user_id = userId,
            kind,
            name,
            data
        }, options: LibraryJson);

        return await SendAsync(request, "Could not save");
    }

    private async Task<(bool ok, string? error)> UpdateAsync(string id, string name, object data)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token == null) return (false, "You are not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"{_config.RestUrl}/library_items?id=eq.{Uri.EscapeDataString(id)}");
        AddAuth(request, token);
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = JsonContent.Create(new { name, data }, options: LibraryJson);

        return await SendAsync(request, "Could not save");
    }

    private async Task<(bool ok, string? error)> DeleteAsync(string id)
    {
        var token = await _auth.GetValidAccessTokenAsync();
        if (token == null) return (false, "You are not signed in.");

        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_config.RestUrl}/library_items?id=eq.{Uri.EscapeDataString(id)}");
        AddAuth(request, token);

        return await SendAsync(request, "Could not delete");
    }

    private async Task<(bool ok, string? error)> SendAsync(HttpRequestMessage request, string failureVerb)
    {
        try
        {
            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode) return (true, null);

            var body = await response.Content.ReadAsStringAsync();

            // 23505 = unique violation, i.e. the user already has an item by this name.
            if (body.Contains("23505", StringComparison.Ordinal))
                return (false, "You already have an entry with this name.");

            // A rejected token surfaces as PostgREST's own JWT complaint, which means
            // nothing to the user. Say what they can actually do about it.
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                    or System.Net.HttpStatusCode.Forbidden)
                return (false, "Your session has expired. Sign in again and retry.");

            return (false, ExtractError(body) ?? $"{failureVerb} ({(int)response.StatusCode}).");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
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
