using System.Text.Json.Serialization;

namespace MechanicalCalculatorWeb.Models;

/// <summary>
/// A signed-in user's session, persisted to localStorage between visits. Holds only
/// what the client needs; the access token is a short-lived JWT, the refresh token
/// mints a new one when it expires.
/// </summary>
public class UserSession
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    /// <summary>Unix seconds at which the access token expires.</summary>
    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonIgnore]
    public bool IsExpired =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= ExpiresAt;

    /// <summary>True within two minutes of expiry, so we can refresh pre-emptively.</summary>
    [JsonIgnore]
    public bool IsNearExpiry =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= ExpiresAt - 120;
}
