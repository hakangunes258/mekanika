using System;
using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Interference fit calculation engine based on DIN 7190, with added effects
/// for temperature and rotational speed.
/// </summary>
public class InterferenceFitEngine
{
    // Input Parameters - Dimensions
    public double NominalDiameter { get; set; }      // mm
    public double FitLength { get; set; }            // mm
    public double ShaftInnerDiameter { get; set; }   // mm (for hollow shaft)
    public double HubOuterDiameter { get; set; }     // mm

    // Input Parameters - Tolerances
    public double ShaftUpperDeviation { get; set; }  // μm
    public double ShaftLowerDeviation { get; set; }  // μm
    public double HubUpperDeviation { get; set; }    // μm
    public double HubLowerDeviation { get; set; }    // μm

    // Input Parameters - Surface Roughness
    public double ShaftRoughness { get; set; }       // Rz in μm
    public double HubRoughness { get; set; }         // Rz in μm

    // Input Parameters - Operating Conditions
    public double AppliedTorque { get; set; }        // Nm
    public double AppliedAxialForce { get; set; }    // N
    public double RotationalSpeed { get; set; }      // rpm
    public double ShaftServiceTemperature { get; set; } // °C
    public double HubServiceTemperature { get; set; }   // °C
    public double AmbientTemperature { get; set; } = 20; // °C
    public double LoadFactor { get; set; } = 1.0;    // Application factor
    public double FrictionCoefficientTorque { get; set; } = 0.12;  // Friction in rotation axis
    public double FrictionCoefficientAssembly { get; set; } = 0.15; // Friction in assembly/axial axis
    
    // Materials
    public Material ShaftMaterial { get; set; } = new();
    public Material HubMaterial { get; set; } = new();

    // Calculated Values - Interference
    public double MaxInterference { get; set; }      // μm
    public double MinInterference { get; set; }      // μm
    public double RoughnessLoss { get; set; }        // μm
    public double ThermalInterferenceChange { get; set; } // μm
    public double EffectiveMaxInterference { get; set; } // μm
    public double EffectiveMinInterference { get; set; } // μm

    // Calculated Values - Pressure
    public double MaxContactPressure { get; set; }   // MPa
    public double MinContactPressure { get; set; }   // MPa
    public double RotationalPressureLoss { get; set; } // MPa

    // Calculated Values - Stress
    public double ShaftTangentialStress { get; set; }   // MPa
    public double ShaftRadialStress { get; set; }       // MPa
    public double ShaftVonMisesStress { get; set; }     // MPa
    public double HubTangentialStress { get; set; }     // MPa
    public double HubRadialStress { get; set; }         // MPa
    public double HubVonMisesStress { get; set; }       // MPa

    // Calculated Values - Capacity
    public double MaxTorqueCapacity { get; set; }       // Nm
    public double MinTorqueCapacity { get; set; }       // Nm
    public double MaxAxialForceCapacity { get; set; }   // N
    public double MinAxialForceCapacity { get; set; }   // N

    // Calculated Values - Assembly
    public double AssemblyForce { get; set; }           // N
    public double HeatingTemperature { get; set; }      // °C
    public double CoolingTemperature { get; set; }      // °C

    // Safety Factors
    public double SafetyFactorSliding { get; set; }      // Against sliding (torque)
    public double SafetyFactorAxial { get; set; }
    public double SafetyFactorShaftStress { get; set; }  // Shaft yield safety
    public double SafetyFactorHubStress { get; set; }    // Hub yield safety

    public void Calculate()
    {
        // The order of these calculations is important
        CalculateGeometricAndThermalInterference();
        CalculatePressureFromInterference();
        CalculateRotationalPressureLoss();
        CalculateStresses();
        CalculateCapacities();
        CalculateAssemblyParameters();
        CalculateSafetyFactors();
    }

    private void CalculateGeometricAndThermalInterference()
    {
        // 1. Geometric interference from tolerances
        MaxInterference = ShaftUpperDeviation - HubLowerDeviation;
        MinInterference = ShaftLowerDeviation - HubUpperDeviation;

        // 2. Surface roughness loss (DIN 7190)
        // Using Rz directly (peak-to-valley height)
        RoughnessLoss = 0.8 * (ShaftRoughness + HubRoughness);

        // 3. Thermal expansion/contraction effect on interference
        // Positive value means interference increases (e.g. shaft hotter than hub)
        double deltaTShaft = ShaftServiceTemperature - AmbientTemperature;
        double deltaTHub = HubServiceTemperature - AmbientTemperature;
        double thermalShaft = ShaftMaterial.ThermalExpansion * 1e-6 * deltaTShaft * NominalDiameter; // mm
        double thermalHub = HubMaterial.ThermalExpansion * 1e-6 * deltaTHub * NominalDiameter;     // mm
        ThermalInterferenceChange = (thermalShaft - thermalHub) * 1000; // μm

        // 4. Effective interference at operating temperature, after roughness loss
        EffectiveMaxInterference = MaxInterference - RoughnessLoss + ThermalInterferenceChange;
        EffectiveMinInterference = MinInterference - RoughnessLoss + ThermalInterferenceChange;

        if (EffectiveMinInterference < 0) EffectiveMinInterference = 0;
        if (EffectiveMaxInterference < EffectiveMinInterference) EffectiveMaxInterference = EffectiveMinInterference;
    }

    /// <summary>
    /// Joint compliance C = d · (C1/E1 + C2/E2) in mm/MPa (DIN 7190).
    /// Converts a diametral interference (mm) into interface pressure (MPa):
    /// p = δ / C. Returns 0 when the geometry is invalid.
    /// </summary>
    private double GetJointCompliance()
    {
        double d = NominalDiameter;
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        // Geometry must be physically meaningful: the hub has to be larger than
        // the joint diameter, and a hollow shaft's bore smaller than it.
        if (d <= 0 || da <= d || di < 0 || di >= d) return 0;

        // Diameter ratios
        double Qi = di > 0 ? di / d : 0;
        double Qa = d / da;

        // Material properties
        double E1 = ShaftMaterial.ElasticModulus * 1000; // GPa to MPa
        double E2 = HubMaterial.ElasticModulus * 1000;
        double v1 = ShaftMaterial.PoissonRatio;
        double v2 = HubMaterial.PoissonRatio;

        if (E1 <= 0 || E2 <= 0) return 0;

        // Lamé coefficients for pressure calculation
        double C1 = (1 + Qi * Qi) / (1 - Qi * Qi) - v1;
        double C2 = (1 + Qa * Qa) / (1 - Qa * Qa) + v2;

        return d * (C1 / E1 + C2 / E2);
    }

    private void CalculatePressureFromInterference()
    {
        double compliance = GetJointCompliance();

        if (compliance > 0)
        {
            MaxContactPressure = (EffectiveMaxInterference / 1000) / compliance;
            MinContactPressure = (EffectiveMinInterference / 1000) / compliance;
        }
        else
        {
            MaxContactPressure = 0;
            MinContactPressure = 0;
        }
    }

    /// <summary>
    /// Loss of interface pressure caused by centrifugal expansion.
    ///
    /// Both parts rotate together, so both expand outwards. The hub bore expands
    /// more than the shaft surface (the hub carries more mass outboard of the
    /// joint), and it is the DIFFERENCE of the two radial displacements that is
    /// lost from the interference — the shaft's own expansion partly compensates
    /// the loss rather than adding to it.
    ///
    /// Rotating-disc theory (plane stress), free-body displacement at the surface
    /// of interest, expressed with RADII:
    ///   Hub bore (inner radius rf, outer radius ra):
    ///     σθ(rf) = ρω²/8 · [ (3+ν)(rf² + 2ra²) − (1+3ν)rf² ]
    ///   Shaft surface (outer radius rf, bore radius ri):
    ///     σθ(rf) = ρω²/8 · [ (3+ν)(2ri² + rf²) − (1+3ν)rf² ]
    ///   u = r · σθ / E   (σr = 0 at both free surfaces)
    /// </summary>
    private void CalculateRotationalPressureLoss()
    {
        RotationalPressureLoss = 0;

        double compliance = GetJointCompliance();
        if (RotationalSpeed <= 0 || compliance <= 0) return;

        double omega = RotationalSpeed * 2 * Math.PI / 60; // rad/s

        // RADII in metres (the previous implementation used diameters here,
        // which overstated the loss by a factor of four).
        double rf = NominalDiameter / 2000.0;      // joint radius
        double ri = ShaftInnerDiameter / 2000.0;   // shaft bore radius (0 if solid)
        double ra = HubOuterDiameter / 2000.0;     // hub outer radius

        double rho_s = ShaftMaterial.Density;  // kg/m³
        double rho_h = HubMaterial.Density;    // kg/m³
        double v_s = ShaftMaterial.PoissonRatio;
        double v_h = HubMaterial.PoissonRatio;
        double E_s = ShaftMaterial.ElasticModulus * 1e9; // GPa to Pa
        double E_h = HubMaterial.ElasticModulus * 1e9;

        if (E_s <= 0 || E_h <= 0) return;

        // Hub: tangential stress at the bore, then radial displacement (m)
        double sigmaTheta_h = (rho_h * omega * omega / 8.0) *
                              ((3.0 + v_h) * (rf * rf + 2.0 * ra * ra) - (1.0 + 3.0 * v_h) * rf * rf);
        double u_hub = rf * sigmaTheta_h / E_h;

        // Shaft: tangential stress at the outer surface, then radial displacement (m).
        // For a solid shaft ri = 0, which reduces to σθ = ρω²rf²(1−ν)/4.
        double sigmaTheta_s = (rho_s * omega * omega / 8.0) *
                              ((3.0 + v_s) * (2.0 * ri * ri + rf * rf) - (1.0 + 3.0 * v_s) * rf * rf);
        double u_shaft = rf * sigmaTheta_s / E_s;

        // Diametral interference lost to rotation (mm)
        double interferenceLoss = 2.0 * (u_hub - u_shaft) * 1000.0;
        if (interferenceLoss < 0) interferenceLoss = 0; // shaft outgrows the hub: no loss

        // Convert to an equivalent pressure loss using the same joint compliance
        RotationalPressureLoss = interferenceLoss / compliance;

        // Subtract the loss from the calculated pressures
        MaxContactPressure -= RotationalPressureLoss;
        MinContactPressure -= RotationalPressureLoss;

        if (MinContactPressure < 0) MinContactPressure = 0;
        if (MaxContactPressure < MinContactPressure) MaxContactPressure = MinContactPressure;
    }

    private void CalculateStresses()
    {
        double d = NominalDiameter;
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        double Qi = di > 0 ? di / d : 0;
        double Qa = d / da;

        // Shaft stresses (inner member) at the fit surface
        ShaftRadialStress = -MaxContactPressure;
        if (di > 0) // Hollow
            ShaftTangentialStress = -MaxContactPressure * (1 + Qi * Qi) / (1 - Qi * Qi);
        else // Solid
            ShaftTangentialStress = -MaxContactPressure;
        
        // Hub stresses (outer member) at the fit surface
        HubRadialStress = -MaxContactPressure;
        HubTangentialStress = MaxContactPressure * (1 + Qa * Qa) / (1 - Qa * Qa);

        // Von Mises equivalent stress (plane stress assumption)
        ShaftVonMisesStress = Math.Sqrt(Math.Pow(ShaftTangentialStress, 2) - ShaftTangentialStress * ShaftRadialStress + Math.Pow(ShaftRadialStress, 2));
        HubVonMisesStress = Math.Sqrt(Math.Pow(HubTangentialStress, 2) - HubTangentialStress * HubRadialStress + Math.Pow(HubRadialStress, 2));
    }

    private void CalculateCapacities()
    {
        double d_m = NominalDiameter / 1000; // m
        double L_m = FitLength / 1000;       // m

        // Torque capacity: T = μ_t * p * π * d² * L / 2 (uses torque/rotation friction coefficient)
        MaxTorqueCapacity = FrictionCoefficientTorque * MaxContactPressure * 1e6 * Math.PI * d_m * d_m * L_m / 2;
        MinTorqueCapacity = FrictionCoefficientTorque * MinContactPressure * 1e6 * Math.PI * d_m * d_m * L_m / 2;

        // Axial force capacity: F_a = μ_a * p * π * d * L (uses assembly/axial friction coefficient)
        MaxAxialForceCapacity = FrictionCoefficientAssembly * MaxContactPressure * 1e6 * Math.PI * d_m * L_m;
        MinAxialForceCapacity = FrictionCoefficientAssembly * MinContactPressure * 1e6 * Math.PI * d_m * L_m;
    }

    private void CalculateAssemblyParameters()
    {
        double d_m = NominalDiameter / 1000; // m
        double L_m = FitLength / 1000;       // m

        double geomMaxInterference_um = ShaftUpperDeviation - HubLowerDeviation - RoughnessLoss;
        
        // Assembly force (press fit) - based on max geometric interference without temp/rotation
        // And using the dedicated assembly friction coefficient
        // Pressure for assembly is calculated from geometric interference
        double d = NominalDiameter;
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;
        double Qi = di > 0 ? di / d : 0;
        double Qa = d / da;
        double E1 = ShaftMaterial.ElasticModulus * 1000;
        double E2 = HubMaterial.ElasticModulus * 1000;
        double v1 = ShaftMaterial.PoissonRatio;
        double v2 = HubMaterial.PoissonRatio;
        double C1 = (1 + Qi * Qi) / (1 - Qi * Qi) - v1;
        double C2 = (1 + Qa * Qa) / (1 - Qa * Qa) + v2;
        double denominator = d * (C1 / E1 + C2 / E2);
        double assemblyPressure = (denominator > 0) ? (geomMaxInterference_um / 1000) / denominator : 0;
        
        AssemblyForce = FrictionCoefficientAssembly * assemblyPressure * 1e6 * Math.PI * d_m * L_m;

        // Shrink fit temperatures also use geometric interference
        double requiredClearance = (geomMaxInterference_um / 1000) + (0.0005 * NominalDiameter); // safety clearance in mm
        
        // Heating temperature for hub
        double alpha_h = HubMaterial.ThermalExpansion * 1e-6; // 1/K
        if (alpha_h > 0)
        {
            HeatingTemperature = AmbientTemperature + requiredClearance / (alpha_h * NominalDiameter);
        }

        // Cooling temperature for shaft
        double alpha_s = ShaftMaterial.ThermalExpansion * 1e-6; // 1/K
        if (alpha_s > 0)
        {
            CoolingTemperature = AmbientTemperature - requiredClearance / (alpha_s * NominalDiameter);
        }
    }

    private void CalculateSafetyFactors()
    {
        // Against sliding (torque) - includes load factor
        double effectiveTorque = AppliedTorque * LoadFactor;
        SafetyFactorSliding = effectiveTorque > 0 ? MinTorqueCapacity / effectiveTorque : 999;

        // Against axial load - includes load factor
        double effectiveAxialForce = AppliedAxialForce * LoadFactor;
        SafetyFactorAxial = effectiveAxialForce > 0 ? MinAxialForceCapacity / effectiveAxialForce : 999;

        // Against yield
        SafetyFactorShaftStress = ShaftVonMisesStress > 0 ? ShaftMaterial.YieldStrength / ShaftVonMisesStress : 999;
        SafetyFactorHubStress = HubVonMisesStress > 0 ? HubMaterial.YieldStrength / HubVonMisesStress : 999;
    }
}