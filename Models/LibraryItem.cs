using System.Text.Json;
using System.Text.Json.Serialization;

namespace MechanicalCalculatorWeb.Models;

/// <summary>
/// A row of the Supabase `library_items` table: one entry the user added to a
/// built-in reference library. Property names map to the snake_case columns.
///
/// <see cref="Data"/> is the item itself, in the shape of whichever model the
/// <see cref="Kind"/> names (today only <see cref="Material"/>). It is kept as a
/// raw <see cref="JsonElement"/> here so one row type serves every library.
/// </summary>
public class LibraryItem
{
    public const string KindMaterial = "material";
    public const string KindBearing = "bearing";

    /// <summary>
    /// Gear grades are their own kind, not a flavour of <see cref="KindMaterial"/>: a gear
    /// material carries an ISO 6336-5 classification (material group, ML/MQ/ME quality
    /// grade, surface hardness) that <see cref="Material"/> has no fields for, and its
    /// allowable stress numbers are derived from those rather than entered.
    /// </summary>
    public const string KindGearMaterial = "gear-material";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    /// <summary>
    /// Also present inside <see cref="Data"/>; duplicated into its own column so the
    /// database can enforce "one name per user per library".
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
