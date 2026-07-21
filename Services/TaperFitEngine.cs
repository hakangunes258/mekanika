using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Taper fit calculation engine based on DIN 7190
/// </summary>
public class TaperFitEngine
{
    // Input Parameters - Dimensions
    public double MeanDiameter { get; set; }          // mm
    public double FitLength { get; set; }             // mm
    public double ShaftInnerDiameter { get; set; }    // mm (hollow shaft)
    public double HubOuterDiameter { get; set; }      // mm
    public double TaperRatio { get; set; }            // 1:X (e.g. 10 for 1:10)

    // Input Parameters - Assembly
    public double AxialDisplacement { get; set; }     // mm
    
    // Input Parameters - Surface
    public double ShaftRoughnessRz { get; set; }        // Rz (μm)
    public double HubRoughnessRz { get; set; }          // Rz (μm)
    public double FrictionCoefficientStatic { get; set; } = 0.15;
    public double FrictionCoefficientDynamic { get; set; } = 0.12;

    // Input Parameters - Operating
    public double AppliedTorque { get; set; }         // Nm
    public double AppliedAxialForce { get; set; }     // N
    public double LoadFactor { get; set; } = 1.0;     // Application factor (shock/duty)

    // Materials
    public Material ShaftMaterial { get; set; } = new();
    public Material HubMaterial { get; set; } = new();

    // Calculated Values - Geometry
    public double TaperAngle { get; set; }            // degrees (full angle)
    public double HalfAngle { get; set; }             // degrees
    public double SmallDiameter { get; set; }         // mm
    public double LargeDiameter { get; set; }         // mm
    public double TaperSlopePercent { get; set; }     // %

    // Calculated Values - Interference
    public double RadialInterference { get; set; }    // mm
    public double DiametralInterference { get; set; } // μm
    public double RoughnessLoss { get; set; }         // μm
    public double EffectiveInterference { get; set; } // μm

    // Calculated Values - Pressure & Stress
    public double ContactPressure { get; set; }       // MPa
    public double ShaftTangentialStress { get; set; } // MPa
    public double ShaftRadialStress { get; set; }     // MPa
    public double ShaftVonMisesStress { get; set; }   // MPa
    public double HubTangentialStress { get; set; }   // MPa
    public double HubRadialStress { get; set; }       // MPa
    public double HubVonMisesStress { get; set; }     // MPa

    // Calculated Values - Capacity
    public double MaxTorqueCapacity { get; set; }     // Nm
    public double MaxAxialForceCapacity { get; set; } // N
    public double RequiredAssemblyForce { get; set; } // N
    public double RequiredDisplacement { get; set; }  // mm

    // Safety Factors
    public double SafetyFactorSliding { get; set; }      // Against sliding (torque)
    public double SafetyFactorAxial { get; set; }
    public double SafetyFactorShaftStress { get; set; }  // Shaft yield safety
    public double SafetyFactorHubStress { get; set; }    // Hub yield safety

    // Standard taper ratios
    // Morse tapers are all close to 1:20 (nominally 1:19.002 to 1:20.047
    // depending on the number) - there is no 1:50 Morse taper. 1:50 is a
    // self-holding pin taper, and 1:30 is a non-self-holding tool taper.
    public static readonly Dictionary<string, double> StandardTapers = new()
    {
        { "1:20 (Morse 0-6, approx.)", 20 },
        { "1:16 (Metric, DIN 254)", 16 },
        { "1:10", 10 },
        { "1:5", 5 },
        { "1:30 (Tool taper)", 30 },
        { "1:50 (Taper pin, DIN 1B)", 50 }
    };

    public void Calculate()
    {
        CalculateGeometry();
        CalculateInterference();
        CalculateContactPressure();
        CalculateStresses();
        CalculateCapacities();
        CalculateSafetyFactors();
    }

    private void CalculateGeometry()
    {
        // Taper angle: tan(α/2) = 1 / (2*C) where C = taper ratio
        HalfAngle = Math.Atan(1.0 / (2.0 * TaperRatio)) * 180.0 / Math.PI;
        TaperAngle = 2.0 * HalfAngle;
        TaperSlopePercent = 100.0 / TaperRatio;

        // Calculate small and large diameters from mean diameter
        double deltaD = FitLength / TaperRatio;
        SmallDiameter = MeanDiameter - deltaD / 2.0;
        LargeDiameter = MeanDiameter + deltaD / 2.0;
    }

    private void CalculateInterference()
    {
        // Radial interference from axial displacement
        // δr = Δs * tan(α/2)
        double halfAngleRad = HalfAngle * Math.PI / 180.0;
        RadialInterference = AxialDisplacement * Math.Tan(halfAngleRad);
        DiametralInterference = 2.0 * RadialInterference * 1000.0; // to μm

        // Surface roughness loss (DIN 7190)
        RoughnessLoss = 0.8 * (ShaftRoughnessRz + HubRoughnessRz);

        // Effective interference
        EffectiveInterference = Math.Max(0, DiametralInterference - RoughnessLoss);

        // Required displacement for this interference
        if (DiametralInterference > 0)
        {
            RequiredDisplacement = (DiametralInterference / 1000.0) / (2.0 * Math.Tan(halfAngleRad));
        }
    }

    private void CalculateContactPressure()
    {
        // Lamé formula (same as cylindrical fit)
        double d = MeanDiameter;
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        double Qi = di > 0 ? di / d : 0;
        double Qa = d / da;

        double E1 = ShaftMaterial.ElasticModulus * 1000; // GPa to MPa
        double E2 = HubMaterial.ElasticModulus * 1000;
        double v1 = ShaftMaterial.PoissonRatio;
        double v2 = HubMaterial.PoissonRatio;

        double C1 = (1 + Qi * Qi) / (1 - Qi * Qi) - v1;
        double C2 = (1 + Qa * Qa) / (1 - Qa * Qa) + v2;

        double denominator = d * (C1 / E1 + C2 / E2);

        if (denominator > 0)
        {
            ContactPressure = (EffectiveInterference / 1000.0) / denominator;
        }
    }

    private void CalculateStresses()
    {
        double d = MeanDiameter;
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        double Qi = di > 0 ? di / d : 0;
        double Qa = d / da;

        // Shaft stresses
        if (di > 0)
        {
            ShaftTangentialStress = -ContactPressure * (1 + Qi * Qi) / (1 - Qi * Qi);
            ShaftRadialStress = -ContactPressure;
        }
        else
        {
            ShaftTangentialStress = -ContactPressure;
            ShaftRadialStress = -ContactPressure;
        }

        // Hub stresses (at inner surface)
        HubTangentialStress = ContactPressure * (1 + Qa * Qa) / (1 - Qa * Qa);
        HubRadialStress = -ContactPressure;

        // Von Mises equivalent stress
        ShaftVonMisesStress = Math.Sqrt(
            ShaftTangentialStress * ShaftTangentialStress +
            ShaftRadialStress * ShaftRadialStress -
            ShaftTangentialStress * ShaftRadialStress);

        HubVonMisesStress = Math.Sqrt(
            HubTangentialStress * HubTangentialStress +
            HubRadialStress * HubRadialStress -
            HubTangentialStress * HubRadialStress);
    }

    private void CalculateCapacities()
    {
        double dm = MeanDiameter / 1000.0; // m
        double L = FitLength / 1000.0;      // m
        double halfAngleRad = HalfAngle * Math.PI / 180.0;
        double p = ContactPressure * 1e6;   // Pa

        double muStatic = FrictionCoefficientStatic;
        double muDynamic = FrictionCoefficientDynamic;

        // Torque capacity: T = μ * p * π * dm² * L / 2
        MaxTorqueCapacity = muDynamic * p * Math.PI * dm * dm * L / 2.0;

        // Axial force capacity (taper surface)
        // Fa = p * π * dm * L * (μ / cos(α/2) - tan(α/2))
        double axialFactor = muDynamic / Math.Cos(halfAngleRad) - Math.Tan(halfAngleRad);
        if (axialFactor > 0)
        {
            MaxAxialForceCapacity = p * Math.PI * dm * L * axialFactor;
        }
        else
        {
            MaxAxialForceCapacity = muDynamic * p * Math.PI * dm * L / Math.Cos(halfAngleRad);
        }

        // Required assembly force
        RequiredAssemblyForce = p * Math.PI * dm * L * (muStatic / Math.Cos(halfAngleRad) + Math.Tan(halfAngleRad));
    }

    private void CalculateSafetyFactors()
    {
        // Design loads include the application factor, consistent with the other modules
        double designTorque = AppliedTorque * LoadFactor;
        double designAxialForce = AppliedAxialForce * LoadFactor;

        // Sliding safety factor (against torque)
        SafetyFactorSliding = designTorque > 0 ? MaxTorqueCapacity / designTorque : 999;

        // Axial force safety factor
        SafetyFactorAxial = designAxialForce > 0 ? MaxAxialForceCapacity / designAxialForce : 999;

        // Stress safety factors (against yield) - Re / σ_vonMises
        SafetyFactorShaftStress = ShaftVonMisesStress > 0 ? ShaftMaterial.YieldStrength / ShaftVonMisesStress : 999;
        SafetyFactorHubStress = HubVonMisesStress > 0 ? HubMaterial.YieldStrength / HubVonMisesStress : 999;
    }
}
