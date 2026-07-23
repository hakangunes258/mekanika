using System.Text.Json.Serialization;

namespace MechanicalCalculatorWeb.Models;

/// <summary>
/// A row of the Supabase `calculations` table: one saved calculation belonging to
/// the signed-in user. Property names map to the snake_case columns.
///
/// Only inputs are stored — the engine re-runs when the calculation is reopened, so
/// a saved calculation always reflects the current engine.
/// </summary>
public class SavedCalculation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("module_key")]
    public string ModuleKey { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("inputs")]
    public Dictionary<string, string> Inputs { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
