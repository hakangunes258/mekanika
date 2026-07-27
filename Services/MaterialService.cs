using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// The material library: the built-in grades below, plus any the signed-in user has
/// added (loaded by <see cref="CustomLibraryService"/>).
///
/// Custom materials are always appended AFTER the built-ins, so the position of a
/// built-in never moves — pages bind a dropdown by index, and a shifting index would
/// silently repoint an in-progress calculation at a different material. Lookups by
/// name hit the built-ins first for the same reason; a custom material may not take a
/// built-in's name (enforced in <see cref="CustomLibraryService.SaveMaterialAsync"/>).
/// </summary>
public class MaterialService
{
    private static readonly List<Material> _builtIn = new List<Material>
    {
        new Material
        {
            Name = "S235JR",
            Standard = "EN 10025",
            YieldStrength = 235,
            TensileStrength = 360,
            ElasticModulus = 206,
            PoissonRatio = 0.3,
            ThermalExpansion = 12,
            Density = 7850,
            PermissibleSurfacePressure = 490
        },
        new Material
        {
            Name = "S355JR",
            Standard = "EN 10025",
            YieldStrength = 355,
            TensileStrength = 470,
            ElasticModulus = 206,
            PoissonRatio = 0.3,
            ThermalExpansion = 12,
            Density = 7850,
            PermissibleSurfacePressure = 760
        },
        new Material
        {
            Name = "C45",
            Standard = "EN 10083",
            YieldStrength = 430,
            TensileStrength = 650,
            ElasticModulus = 206,
            PoissonRatio = 0.3,
            ThermalExpansion = 11.5,
            Density = 7850,
            PermissibleSurfacePressure = 1250
        },
        new Material
        {
            Name = "42CrMo4",
            Standard = "EN 10083",
            YieldStrength = 750,
            TensileStrength = 1000,
            ElasticModulus = 206,
            PoissonRatio = 0.3,
            ThermalExpansion = 11,
            Density = 7850,
            PermissibleSurfacePressure = 1300
        },
        new Material
        {
            Name = "16MnCr5",
            Standard = "EN 10084",
            YieldStrength = 590,
            TensileStrength = 780,
            ElasticModulus = 206,
            PoissonRatio = 0.3,
            ThermalExpansion = 11.5,
            Density = 7850,
            PermissibleSurfacePressure = 1300
        },
        new Material
        {
            Name = "GG25 (EN-GJL-250)",
            Standard = "EN 1561",
            YieldStrength = 165,
            TensileStrength = 250,
            ElasticModulus = 110,
            PoissonRatio = 0.26,
            ThermalExpansion = 10.5,
            Density = 7200,
            PermissibleSurfacePressure = 330
        },
        new Material
        {
            Name = "GGG40 (EN-GJS-400)",
            Standard = "EN 1563",
            YieldStrength = 250,
            TensileStrength = 400,
            ElasticModulus = 169,
            PoissonRatio = 0.28,
            ThermalExpansion = 11,
            Density = 7100,
            PermissibleSurfacePressure = 450
        },
        new Material
        {
            Name = "AlMgSi1 (6082)",
            Standard = "EN 573",
            YieldStrength = 250,
            TensileStrength = 290,
            ElasticModulus = 70,
            PoissonRatio = 0.33,
            ThermalExpansion = 23,
            Density = 2700,
            PermissibleSurfacePressure = 240
        },
        new Material
        {
            Name = "AlZnMgCu1.5 (7075)",
            Standard = "EN 573",
            YieldStrength = 503,
            TensileStrength = 572,
            ElasticModulus = 71.7,
            PoissonRatio = 0.33,
            ThermalExpansion = 23.4,
            Density = 2810,
            PermissibleSurfacePressure = 480
        },
        new Material
        {
            Name = "CuZn37 (Brass)",
            Standard = "EN 12167",
            YieldStrength = 200,
            TensileStrength = 360,
            ElasticModulus = 100,
            PoissonRatio = 0.35,
            ThermalExpansion = 21,
            Density = 8400,
            PermissibleSurfacePressure = 180
        },
        new Material
        {
            Name = "CuSn8 (Bronze)",
            Standard = "EN 1982",
            YieldStrength = 180,
            TensileStrength = 300,
            ElasticModulus = 110,
            PoissonRatio = 0.34,
            ThermalExpansion = 18,
            Density = 8800,
            PermissibleSurfacePressure = 200
        },
        new Material
        {
            Name = "X5CrNi18-10 (304)",
            Standard = "EN 10088",
            YieldStrength = 230,
            TensileStrength = 540,
            ElasticModulus = 200,
            PoissonRatio = 0.3,
            ThermalExpansion = 16,
            Density = 7900,
            PermissibleSurfacePressure = 230
        }
    };

    // ============ USER-ADDED MATERIALS ============

    private static List<Material> _custom = new();
    private static List<Material>? _all;

    /// <summary>The grades that ship with the app. Never empty, never user-editable.</summary>
    public static IReadOnlyList<Material> BuiltInMaterials => _builtIn;

    /// <summary>The signed-in user's own materials. Empty when signed out.</summary>
    public static IReadOnlyList<Material> CustomMaterials => _custom;

    /// <summary>
    /// Replaces the user's materials with the ones just loaded from the account.
    /// Called by <see cref="CustomLibraryService"/> at startup and on every auth
    /// change — an empty list on sign-out is the point, not a failure case.
    /// </summary>
    public static void SetCustomMaterials(IEnumerable<Material> materials)
    {
        _custom = materials.ToList();
        _all = null;
    }

    /// <summary>True if a built-in grade already uses this name (case-insensitive).</summary>
    public static bool IsBuiltInName(string name)
        => _builtIn.Any(m => string.Equals(m.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static List<Material> All => _all ??= _builtIn.Concat(_custom).ToList();

    // ============ ACCESSORS ============

    public List<Material> GetMaterials()
    {
        return All;
    }

    /// <summary>
    /// Returns all materials (static version for easy access)
    /// </summary>
    public static List<Material> GetAllMaterials()
    {
        return All;
    }

    /// <summary>
    /// Get material by name
    /// </summary>
    public static Material? GetMaterial(string name)
    {
        return All.FirstOrDefault(m => m.Name == name);
    }

    /// <summary>
    /// Get material by partial name match
    /// </summary>
    public static Material? GetMaterialByPartialName(string partialName)
    {
        return All.FirstOrDefault(m => m.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }
}
