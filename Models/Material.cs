using System.Text.Json.Serialization;

namespace MechanicalCalculatorWeb.Models;

public class Material
{
    public string Name { get; set; } = "";
    public string Standard { get; set; } = "";
    public double YieldStrength { get; set; }      // MPa
    public double TensileStrength { get; set; }    // MPa
    public double ElasticModulus { get; set; }     // GPa
    public double PoissonRatio { get; set; }
    public double ThermalExpansion { get; set; }   // 10^-6/K
    public double Density { get; set; }            // kg/m³
    public double PermissibleSurfacePressure { get; set; } // MPa

    /// <summary>
    /// Supabase `library_items.id` for a material the signed-in user added; null for
    /// the built-in ones. Not part of the stored payload — it is the row's identity,
    /// not one of its properties.
    /// </summary>
    [JsonIgnore]
    public string? CustomId { get; set; }

    [JsonIgnore]
    public bool IsCustom => CustomId != null;

    /// <summary>
    /// The label every module's material dropdown shows. The standard is dropped when
    /// absent rather than rendered as an empty "()" — user-added grades often have no
    /// standard to quote.
    /// </summary>
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Standard) ? Name : $"{Name} ({Standard})";
}
