using MechanicalCalculatorWeb.Models;

namespace Mekanika.Tests;

/// <summary>
/// Materials defined here rather than taken from MaterialService, so that a
/// golden value depends only on the engine under test. Editing the material
/// database must not be able to move an expected result.
/// </summary>
internal static class TestMaterials
{
    public static Material Steel() => new()
    {
        Name = "Test steel",
        YieldStrength = 355,
        TensileStrength = 510,
        ElasticModulus = 210,      // GPa
        PoissonRatio = 0.3,
        ThermalExpansion = 11.5,   // 1e-6/K
        Density = 7850,            // kg/m3
        PermissibleSurfacePressure = 300
    };
}
