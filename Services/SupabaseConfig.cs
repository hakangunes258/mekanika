namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Supabase connection settings, bound from wwwroot/appsettings.json.
///
/// Both values are public by design: the publishable key ships in the WASM bundle
/// and is safe to expose — Row Level Security, not key secrecy, is what keeps data
/// private. The secret / service_role key must NEVER appear here or anywhere on the
/// client.
/// </summary>
public class SupabaseConfig
{
    public string Url { get; set; } = "";
    public string PublishableKey { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(PublishableKey);

    // Convenience endpoint builders so callers never hand-concatenate paths.
    public string AuthUrl => $"{Url.TrimEnd('/')}/auth/v1";
    public string RestUrl => $"{Url.TrimEnd('/')}/rest/v1";
}
