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

    // Calculated Values - Stress, at the governing point of the governing state
    public double ShaftTangentialStress { get; set; }   // MPa
    public double ShaftRadialStress { get; set; }       // MPa
    public double ShaftVonMisesStress { get; set; }     // MPa
    public double HubTangentialStress { get; set; }     // MPa
    public double HubRadialStress { get; set; }         // MPa
    public double HubVonMisesStress { get; set; }       // MPa

    /// <summary>Where the shaft's governing stress sits — "bore" or "fit surface".</summary>
    public string ShaftStressPoint { get; private set; } = "";
    /// <summary>Where the hub's governing stress sits — "bore" or "outer surface".</summary>
    public string HubStressPoint { get; private set; } = "";
    /// <summary>Which operating state produced the shaft's governing stress.</summary>
    public string ShaftGoverningState { get; private set; } = "";
    /// <summary>Which operating state produced the hub's governing stress.</summary>
    public string HubGoverningState { get; private set; } = "";

    /// <summary>
    /// The operating states the yield check is run for. Rotation and a hotter hub
    /// both RELIEVE the interference, so the state shown on the capacity rows is
    /// not the one that loads the parts hardest.
    /// </summary>
    public List<StressCase> StressCases { get; } = new();

    // Calculated Values - Capacity
    public double MaxTorqueCapacity { get; set; }       // Nm
    public double MinTorqueCapacity { get; set; }       // Nm
    public double MaxAxialForceCapacity { get; set; }   // N
    public double MinAxialForceCapacity { get; set; }   // N

    // Calculated Values - Combined loading (DIN 7190: one friction capacity, shared)
    public double TangentialForce { get; set; }         // N, from the applied torque
    public double ResultantForce { get; set; }          // N
    public double MinResultantCapacity { get; set; }    // N

    /// <summary>
    /// The friction coefficient the combined check runs on: the lower of the two
    /// entered, since the resultant slides in whichever direction is weaker.
    /// </summary>
    public double FrictionCoefficientCombined =>
        Math.Min(FrictionCoefficientTorque, FrictionCoefficientAssembly);

    // Calculated Values - Assembly
    public double AssemblyForce { get; set; }           // N
    public double HeatingTemperature { get; set; }      // °C
    public double CoolingTemperature { get; set; }      // °C
    /// <summary>Diametral clearance assumed for assembly, on top of the interference (mm).</summary>
    public double AssemblyClearance { get; set; }       // mm

    // Safety Factors
    public double SafetyFactorSliding { get; set; }      // Against sliding (torque alone)
    public double SafetyFactorAxial { get; set; }        // Against sliding (axial alone)
    public double SafetyFactorCombined { get; set; }     // Against sliding (resultant)
    public double SafetyFactorShaftStress { get; set; }  // Shaft yield safety
    public double SafetyFactorHubStress { get; set; }    // Hub yield safety

    // Why the interference was lost, when it was
    /// <summary>True when the tolerance pair can produce clearance at its loose limit.</summary>
    public bool IsTransitionFit { get; private set; }
    /// <summary>True when the minimum effective interference reached zero.</summary>
    public bool MinInterferenceLost { get; private set; }
    /// <summary>
    /// Plain-language reasons the grip was lost, in the order they take effect.
    /// Empty when the joint keeps interference at every limit.
    /// </summary>
    public List<string> InterferenceLossReasons { get; } = new();

    public void Calculate()
    {
        StressCases.Clear();
        InterferenceLossReasons.Clear();

        // The order of these calculations is important
        CalculateGeometricAndThermalInterference();
        CalculatePressureFromInterference();
        CalculateRotationalPressureLoss();
        CalculateStresses();
        CalculateCapacities();
        CalculateAssemblyParameters();
        CalculateSafetyFactors();
        DiagnoseInterferenceLoss();
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

    /// <summary>Interface pressure (MPa) for a diametral interference in microns.</summary>
    private double PressureFor(double interferenceMicrons)
    {
        double compliance = GetJointCompliance();
        if (compliance <= 0 || interferenceMicrons <= 0) return 0;
        return (interferenceMicrons / 1000.0) / compliance;
    }

    // Pressure at the tight limit in each state, kept for the yield check.
    private double _pressureAmbient;    // standstill, ambient, no thermal change
    private double _pressureOperating;  // standstill, at service temperature
    private double _omega;              // rad/s, 0 when not rotating

    private void CalculatePressureFromInterference()
    {
        // Tight limit at ambient: tolerances less roughness, no thermal change.
        _pressureAmbient = PressureFor(MaxInterference - RoughnessLoss);
        _pressureOperating = PressureFor(EffectiveMaxInterference);

        MaxContactPressure = _pressureOperating;
        MinContactPressure = PressureFor(EffectiveMinInterference);
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
        _omega = 0;

        double compliance = GetJointCompliance();
        if (RotationalSpeed <= 0 || compliance <= 0) return;

        double omega = RotationalSpeed * 2 * Math.PI / 60; // rad/s
        _omega = omega;

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

    /// <summary>Von Mises equivalent stress for a plane stress state (MPa).</summary>
    private static double VonMises(double sigmaT, double sigmaR)
        => Math.Sqrt(sigmaT * sigmaT - sigmaT * sigmaR + sigmaR * sigmaR);

    /// <summary>
    /// Tangential stress (MPa) at the BORE of a rotating annulus, plane stress.
    /// Radii in metres, density in kg/m3. Pass ri = 0 for a solid body, which
    /// then returns the stress at its centre.
    /// </summary>
    private static double SpinStressAtBore(double rho, double omega, double ri, double ro, double nu)
        => rho * omega * omega / 8.0
           * ((3.0 + nu) * (ri * ri + 2.0 * ro * ro) - (1.0 + 3.0 * nu) * ri * ri) / 1e6;

    /// <summary>
    /// Tangential stress (MPa) at the OUTER SURFACE of a rotating annulus,
    /// plane stress. Radii in metres, density in kg/m3.
    /// </summary>
    private static double SpinStressAtRim(double rho, double omega, double ri, double ro, double nu)
        => rho * omega * omega / 8.0
           * ((3.0 + nu) * (2.0 * ri * ri + ro * ro) - (1.0 + 3.0 * nu) * ro * ro) / 1e6;

    /// <summary>
    /// Stresses in both members for ONE operating state.
    ///
    /// Every candidate point is evaluated and the largest returned, because the
    /// peak stress is not always at the fit surface: in a HOLLOW SHAFT it sits at
    /// the bore, where the radial stress has relaxed to zero and the tangential
    /// stress has risen to -2p/(1-Qi^2). For Qi = 0,8 that is 174 MPa against the
    /// 130 MPa at the fit surface, so reading the fit surface alone understates
    /// the stress by a quarter. DIN 7190 states the bore value for this reason.
    /// </summary>
    private StressCase StressesFor(string state, double pressure, double omega)
    {
        double d = NominalDiameter;
        double di = ShaftInnerDiameter;
        double da = HubOuterDiameter;

        double Qi = di > 0 && di < d ? di / d : 0;
        double Qa = da > d ? d / da : 0;

        double rf = d / 2000.0;    // joint radius, m
        double ri = di / 2000.0;   // shaft bore radius, m
        double ra = da / 2000.0;   // hub outer radius, m

        // Centrifugal tangential stress: it ADDS to the shrink-fit stress even
        // though rotation relieves the pressure.
        double spinShaftRim = omega > 0
            ? SpinStressAtRim(ShaftMaterial.Density, omega, ri, rf, ShaftMaterial.PoissonRatio) : 0;
        double spinShaftBore = omega > 0
            ? SpinStressAtBore(ShaftMaterial.Density, omega, ri, rf, ShaftMaterial.PoissonRatio) : 0;
        double spinHubBore = omega > 0
            ? SpinStressAtBore(HubMaterial.Density, omega, rf, ra, HubMaterial.PoissonRatio) : 0;
        double spinHubRim = omega > 0
            ? SpinStressAtRim(HubMaterial.Density, omega, rf, ra, HubMaterial.PoissonRatio) : 0;

        // ---- Shaft (inner member, external pressure p) ----
        double shaftSurfaceT = (Qi > 0 ? -pressure * (1 + Qi * Qi) / (1 - Qi * Qi) : -pressure) + spinShaftRim;
        double shaftSurfaceR = -pressure;
        double shaftSurfaceV = VonMises(shaftSurfaceT, shaftSurfaceR);

        double shaftT = shaftSurfaceT, shaftR = shaftSurfaceR, shaftV = shaftSurfaceV;
        string shaftPoint = "fit surface";

        if (Qi > 0)
        {
            // Bore: radial stress is zero on a free surface.
            double boreT = -2.0 * pressure / (1 - Qi * Qi) + spinShaftBore;
            double boreV = VonMises(boreT, 0);
            if (boreV > shaftV)
            {
                shaftT = boreT; shaftR = 0; shaftV = boreV; shaftPoint = "bore";
            }
        }

        // ---- Hub (outer member, internal pressure p) ----
        double hubBoreT = (Qa > 0 ? pressure * (1 + Qa * Qa) / (1 - Qa * Qa) : 0) + spinHubBore;
        double hubBoreR = -pressure;
        double hubBoreV = VonMises(hubBoreT, hubBoreR);

        double hubT = hubBoreT, hubR = hubBoreR, hubV = hubBoreV;
        string hubPoint = "bore";

        double hubRimT = (Qa > 0 ? 2.0 * pressure * Qa * Qa / (1 - Qa * Qa) : 0) + spinHubRim;
        double hubRimV = VonMises(hubRimT, 0);
        if (hubRimV > hubV)
        {
            hubT = hubRimT; hubR = 0; hubV = hubRimV; hubPoint = "outer surface";
        }

        return new StressCase
        {
            State = state,
            Pressure = pressure,
            ShaftTangential = shaftT,
            ShaftRadial = shaftR,
            ShaftVonMises = shaftV,
            ShaftPoint = shaftPoint,
            HubTangential = hubT,
            HubRadial = hubR,
            HubVonMises = hubV,
            HubPoint = hubPoint
        };
    }

    /// <summary>
    /// Yield is checked against the WORST of the operating states, not against
    /// the one the capacity rows are quoted at.
    ///
    /// Rotation and a hotter hub both relieve the interference, so a joint that
    /// looks comfortable at speed or at temperature can be at its tightest the
    /// moment it stops or cools — which is every shutdown, and the state it is
    /// in straight after assembly.
    /// </summary>
    private void CalculateStresses()
    {
        StressCases.Add(StressesFor("Standstill, ambient temperature", _pressureAmbient, 0));

        bool thermal = Math.Abs(ThermalInterferenceChange) > 1e-9;
        if (thermal)
            StressCases.Add(StressesFor("Standstill, service temperature", _pressureOperating, 0));

        if (_omega > 0)
            StressCases.Add(StressesFor("Rotating, service temperature", MaxContactPressure, _omega));

        var shaftWorst = StressCases[0];
        var hubWorst = StressCases[0];
        foreach (var c in StressCases)
        {
            if (c.ShaftVonMises > shaftWorst.ShaftVonMises) shaftWorst = c;
            if (c.HubVonMises > hubWorst.HubVonMises) hubWorst = c;
        }

        ShaftTangentialStress = shaftWorst.ShaftTangential;
        ShaftRadialStress = shaftWorst.ShaftRadial;
        ShaftVonMisesStress = shaftWorst.ShaftVonMises;
        ShaftStressPoint = shaftWorst.ShaftPoint;
        ShaftGoverningState = shaftWorst.State;

        HubTangentialStress = hubWorst.HubTangential;
        HubRadialStress = hubWorst.HubRadial;
        HubVonMisesStress = hubWorst.HubVonMises;
        HubStressPoint = hubWorst.HubPoint;
        HubGoverningState = hubWorst.State;
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

        // Combined loading. Torque and axial force are carried by ONE friction
        // interface, so the circumferential and axial components share a single
        // capacity (DIN 7190). Checking them separately passes joints that slip:
        // 10 kN axial alongside 250 Nm on Ø50 gives two safeties of 1,28 and 1,59
        // and a resultant safety of 0,90.
        TangentialForce = NominalDiameter > 0
            ? 2000.0 * AppliedTorque * LoadFactor / NominalDiameter : 0;
        double axial = AppliedAxialForce * LoadFactor;
        ResultantForce = Math.Sqrt(TangentialForce * TangentialForce + axial * axial);
        MinResultantCapacity = FrictionCoefficientCombined * MinContactPressure * 1e6 * Math.PI * d_m * L_m;
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

        // Shrink fit temperatures also use geometric interference.
        // The assembly clearance is the gap left for the parts to go together
        // once expanded. It is shown in the results because the heating
        // temperature follows directly from it and nothing else on the page
        // reveals which value was assumed.
        AssemblyClearance = 0.0005 * NominalDiameter;                          // mm
        double requiredClearance = (geomMaxInterference_um / 1000) + AssemblyClearance; // mm

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

        // Against sliding under the resultant of both
        SafetyFactorCombined = ResultantForce > 0 ? MinResultantCapacity / ResultantForce : 999;

        // Against yield
        SafetyFactorShaftStress = ShaftVonMisesStress > 0 ? ShaftMaterial.YieldStrength / ShaftVonMisesStress : 999;
        SafetyFactorHubStress = HubVonMisesStress > 0 ? HubMaterial.YieldStrength / HubVonMisesStress : 999;
    }

    /// <summary>
    /// Names what ate the interference, when there is none left.
    ///
    /// Without this the page reports a safety factor of 0,00 and nothing else:
    /// a transition fit, a joint rough enough to lose its grip and an aluminium
    /// hub at 100 °C all look identical, and the user cannot tell which input to
    /// change.
    /// </summary>
    private void DiagnoseInterferenceLoss()
    {
        IsTransitionFit = MinInterference < 0;
        MinInterferenceLost = MinContactPressure <= 0;

        if (IsTransitionFit)
            InterferenceLossReasons.Add(
                $"The tolerance pair is a transition fit: at its loose limit the shaft is " +
                $"{Math.Abs(MinInterference):0.#} µm smaller than the bore, so there is clearance " +
                "rather than interference. Choose a tighter shaft tolerance.");

        double afterRoughness = MinInterference - RoughnessLoss;
        if (!IsTransitionFit && afterRoughness <= 0)
            InterferenceLossReasons.Add(
                $"Surface roughness takes {RoughnessLoss:0.#} µm off the diametral interference, " +
                $"more than the {MinInterference:0.#} µm the tolerances provide. " +
                "A finer finish on either part recovers it.");

        if (afterRoughness > 0 && EffectiveMinInterference <= 0)
            InterferenceLossReasons.Add(
                $"Thermal expansion removes a further {-ThermalInterferenceChange:0.#} µm at the " +
                "service temperatures entered, leaving no interference. The joint is loose while hot.");

        if (EffectiveMinInterference > 0 && MinContactPressure <= 0 && RotationalPressureLoss > 0)
            InterferenceLossReasons.Add(
                $"Centrifugal expansion at {RotationalSpeed:0} rpm costs {RotationalPressureLoss:0.##} MPa, " +
                "which is the whole of the contact pressure at the loose limit.");
    }
}

/// <summary>
/// One operating state of the joint, with the stresses it produces in each
/// member and the point on each member they were read at.
/// </summary>
public sealed class StressCase
{
    public string State { get; init; } = "";
    public double Pressure { get; init; }          // MPa

    public double ShaftTangential { get; init; }   // MPa
    public double ShaftRadial { get; init; }       // MPa
    public double ShaftVonMises { get; init; }     // MPa
    public string ShaftPoint { get; init; } = "";

    public double HubTangential { get; init; }     // MPa
    public double HubRadial { get; init; }         // MPa
    public double HubVonMises { get; init; }       // MPa
    public string HubPoint { get; init; } = "";
}