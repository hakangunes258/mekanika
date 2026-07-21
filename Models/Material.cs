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

    public override string ToString() => $"{Name} ({Standard})";
}
