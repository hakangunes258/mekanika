using MechanicalCalculatorWeb.Models;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Clamp (Clamping) connection calculation engine for shaft-hub connections.
///
/// No single standard covers this: the bolt preload follows VDI 2230, the hub
/// stresses are Lamé thick-cylinder, and the friction/torque capacity is general
/// machine design (Roloff/Matek). It previously cited DIN 703, which is the
/// standard for SHAFT COLLARS (Stellringe) and unrelated to a clamped hub.
/// </summary>
public class ClampConnectionEngine
{
    // ============ INPUT PARAMETERS ============

    // Input Parameters - Shaft
    public double ShaftDiameter { get; set; }         // d (mm)

    // Input Parameters - Clamp Dimensions
    public double ClampOuterDiameter { get; set; }    // Da (mm)
    public double ClampLength { get; set; }           // L (mm)
    public double SlitWidth { get; set; }             // b (mm) - slit/gap width
    public int NumberOfBolts { get; set; } = 1;       // Number of clamping bolts

    // Input Parameters - Bolt
    public double BoltDiameter { get; set; }          // M (mm) - nominal bolt diameter
    public double BoltTighteningTorque { get; set; }  // MA (Nm)
    public double BoltYieldStrength { get; set; }     // Rp0.2 (MPa) - bolt yield strength
    public double ThreadPitch { get; set; }           // P (mm)

    // Input Parameters - Loading
    public double AppliedTorque { get; set; }         // T (Nm)
    public double AppliedAxialForce { get; set; }     // Fa (N)
    public double LoadFactor { get; set; } = 1.0;     // Application factor

    // Materials
    public Material ShaftMaterial { get; set; } = new();
    public Material HubMaterial { get; set; } = new();

    // Friction Coefficients
    public double FrictionCoefficientInterface { get; set; } = 0.15;  // μ shaft-hub interface
    public double FrictionCoefficientThread { get; set; } = 0.12;     // μ thread friction
    public double FrictionCoefficientHead { get; set; } = 0.12;       // μ bolt head friction

    // ============ CALCULATED VALUES ============

    // Calculated Values - Geometry
    public double DiameterRatio { get; set; }         // Qa = d/Da
    public double ContactArea { get; set; }           // A (mm²)
    public double ContactAngle { get; set; }          // β (degrees) - effective contact angle
    public double EffectiveContactLength { get; set; } // Leff (mm)

    // Calculated Values - Bolt
    public double BoltPreloadForce { get; set; }      // Fv (N) - preload force from tightening
    public double BoltStressArea { get; set; }        // As (mm²)
    public double BoltStress { get; set; }            // σb (MPa)
    public double BoltUtilization { get; set; }       // % of yield

    // Calculated Values - Contact Pressure
    public double NormalForce { get; set; }           // Fn (N) - normal force on interface
    public double ContactPressure { get; set; }       // p (MPa)
    public double MaxContactPressure { get; set; }    // pmax (MPa)
    public double AllowableContactPressure { get; set; } // pzul (MPa)

    // Calculated Values - Capacities
    public double TorqueCapacity { get; set; }        // Tmax (Nm)
    public double AxialForceCapacity { get; set; }    // Famax (N)

    // Calculated Values - Forces
    public double RequiredNormalForce { get; set; }   // Fn,req (N)
    public double TangentialForce { get; set; }       // Ft (N)

    // Calculated Values - Stresses
    public double HubTangentialStress { get; set; }   // σt,hub (MPa)
    public double HubRadialStress { get; set; }       // σr,hub (MPa)
    public double HubVonMisesStress { get; set; }     // σv,hub (MPa)

    // Safety Factors
    public double SafetyFactorSliding { get; set; }   // Against torque/axial sliding
    public double SafetyFactorHubStress { get; set; } // Hub material yield
    public double SafetyFactorBolt { get; set; }      // Bolt yield
    public double SafetyFactorPressure { get; set; }  // Contact pressure
    public double SafetyFactorMin { get; set; }       // Minimum overall

    // Standard Clamp Sizes based on shaft diameter
    public static readonly List<ClampDimension> StandardClamps = new()
    {
        new ClampDimension { ShaftDiaMin = 6, ShaftDiaMax = 8, OuterDia = 16, Length = 12, SlitWidth = 3, BoltSize = 4 },
        new ClampDimension { ShaftDiaMin = 8, ShaftDiaMax = 10, OuterDia = 20, Length = 14, SlitWidth = 3, BoltSize = 4 },
        new ClampDimension { ShaftDiaMin = 10, ShaftDiaMax = 12, OuterDia = 24, Length = 16, SlitWidth = 4, BoltSize = 5 },
        new ClampDimension { ShaftDiaMin = 12, ShaftDiaMax = 16, OuterDia = 28, Length = 18, SlitWidth = 4, BoltSize = 5 },
        new ClampDimension { ShaftDiaMin = 16, ShaftDiaMax = 20, OuterDia = 35, Length = 22, SlitWidth = 5, BoltSize = 6 },
        new ClampDimension { ShaftDiaMin = 20, ShaftDiaMax = 25, OuterDia = 42, Length = 26, SlitWidth = 5, BoltSize = 6 },
        new ClampDimension { ShaftDiaMin = 25, ShaftDiaMax = 30, OuterDia = 50, Length = 30, SlitWidth = 6, BoltSize = 8 },
        new ClampDimension { ShaftDiaMin = 30, ShaftDiaMax = 35, OuterDia = 57, Length = 35, SlitWidth = 6, BoltSize = 8 },
        new ClampDimension { ShaftDiaMin = 35, ShaftDiaMax = 40, OuterDia = 65, Length = 40, SlitWidth = 7, BoltSize = 10 },
        new ClampDimension { ShaftDiaMin = 40, ShaftDiaMax = 45, OuterDia = 72, Length = 45, SlitWidth = 7, BoltSize = 10 },
        new ClampDimension { ShaftDiaMin = 45, ShaftDiaMax = 50, OuterDia = 80, Length = 50, SlitWidth = 8, BoltSize = 10 },
        new ClampDimension { ShaftDiaMin = 50, ShaftDiaMax = 60, OuterDia = 90, Length = 55, SlitWidth = 8, BoltSize = 12 },
        new ClampDimension { ShaftDiaMin = 60, ShaftDiaMax = 70, OuterDia = 105, Length = 65, SlitWidth = 10, BoltSize = 12 },
        new ClampDimension { ShaftDiaMin = 70, ShaftDiaMax = 80, OuterDia = 120, Length = 75, SlitWidth = 10, BoltSize = 14 },
        new ClampDimension { ShaftDiaMin = 80, ShaftDiaMax = 90, OuterDia = 135, Length = 85, SlitWidth = 12, BoltSize = 16 },
        new ClampDimension { ShaftDiaMin = 90, ShaftDiaMax = 100, OuterDia = 150, Length = 95, SlitWidth = 12, BoltSize = 16 }
    };

    // Standard bolt properties (metric, class 8.8 default)
    public static readonly Dictionary<double, (double StressArea, double Pitch)> BoltData = new()
    {
        { 4, (8.78, 0.7) },
        { 5, (14.2, 0.8) },
        { 6, (20.1, 1.0) },
        { 8, (36.6, 1.25) },
        { 10, (58.0, 1.5) },
        { 12, (84.3, 1.75) },
        { 14, (115, 2.0) },
        { 16, (157, 2.0) },
        { 18, (192, 2.5) },
        { 20, (245, 2.5) },
        { 22, (303, 2.5) },
        { 24, (353, 3.0) }
    };

    public static ClampDimension? GetStandardClamp(double shaftDiameter)
    {
        return StandardClamps.FirstOrDefault(c =>
            shaftDiameter > c.ShaftDiaMin && shaftDiameter <= c.ShaftDiaMax);
    }

    // ============ MAIN CALCULATION METHOD ============

    public void Calculate()
    {
        CalculateGeometry();
        CalculateBoltPreload();
        CalculateContactPressure();
        CalculateCapacities();
        CalculateStresses();
        CalculateSafetyFactors();
    }

    // ============ CALCULATION STEPS ============

    private void CalculateGeometry()
    {
        // Diameter ratio
        DiameterRatio = ClampOuterDiameter > 0 ? ShaftDiameter / ClampOuterDiameter : 0;

        // Effective contact angle (accounting for slit)
        // The slit reduces the effective wrap angle.
        // Clamp the Asin argument to [-1, 1]: a slit wider than the shaft is not
        // physically meaningful, but it must not produce NaN and poison every
        // downstream result.
        double slitRatio = ShaftDiameter > 0 ? SlitWidth / ShaftDiameter : 1.0;
        slitRatio = Math.Clamp(slitRatio, 0.0, 1.0);
        double slitAngle = 2 * Math.Asin(slitRatio) * 180 / Math.PI;
        ContactAngle = 360 - slitAngle;
        if (ContactAngle < 180) ContactAngle = 180; // Minimum realistic contact

        // Effective contact length (accounting for edge effects)
        EffectiveContactLength = ClampLength * 0.85; // 85% efficiency factor

        // Contact area = π × d × Leff × (contact angle / 360)
        ContactArea = Math.PI * ShaftDiameter * EffectiveContactLength * (ContactAngle / 360);
    }

    private void CalculateBoltPreload()
    {
        // Get bolt stress area and pitch
        if (BoltData.TryGetValue(BoltDiameter, out var boltInfo))
        {
            BoltStressArea = boltInfo.StressArea;
            if (ThreadPitch <= 0) ThreadPitch = boltInfo.Pitch;
        }
        else
        {
            // Approximate stress area for non-standard bolts
            double d2Approx = BoltDiameter - 0.6495 * ThreadPitch; // Pitch diameter
            double d3 = BoltDiameter - 1.2268 * ThreadPitch; // Minor diameter
            BoltStressArea = Math.PI / 4 * Math.Pow((d2Approx + d3) / 2, 2);
        }

        // Calculate preload force from tightening torque
        // MA = Fv × (0.16 × P + 0.58 × d2 × μG + dm/2 × μK)
        // Simplified: MA ≈ Fv × d × (0.16 + 0.58 × μG + 0.5 × μK) for M×1 thread
        double d2 = BoltDiameter - 0.6495 * ThreadPitch; // Pitch diameter
        double dm = BoltDiameter * 1.4; // Approximate bearing diameter under head

        double threadTerm = ThreadPitch / (2 * Math.PI) + FrictionCoefficientThread * d2 / (2 * Math.Cos(Math.PI / 6));
        double headTerm = FrictionCoefficientHead * dm / 2;
        double totalFactor = threadTerm + headTerm;

        if (totalFactor > 0)
        {
            BoltPreloadForce = (BoltTighteningTorque * 1000) / totalFactor; // Convert Nm to Nmm
        }

        // Bolt stress
        BoltStress = BoltPreloadForce / BoltStressArea;
        BoltUtilization = (BoltStress / BoltYieldStrength) * 100;
    }

    private void CalculateContactPressure()
    {
        // === CONTACT PRESSURE FROM FORCE EQUILIBRIUM (Roloff/Matek) ===
        //
        // Cut the clamp along the diametral joint plane. All n bolts cross that
        // plane, so the total bolt preload n·Fv must balance the resultant of the
        // interface pressure over one half-shell:
        //
        //   n·Fv = ∫₀^π p · sinθ · (d/2) · lF dθ = p · d · lF      (uniform p)
        //
        // Hence the mean pressure follows from the PROJECTED area d × lF:
        //
        //   p = n·Fv / (d · lF)
        //
        // and the integrated normal force over the whole wrapped surface is
        //
        //   Fn = p · π · d · lF = π · n · Fv
        //
        // so the transmissible torque is T = μ·Fn·d/2 = (π/2)·μ·n·Fv·d.
        //
        // NOTE: this is the SPLIT-hub (geteilte Nabe) model, where both joints
        // carry bolts. A single-slit hub additionally acts as a lever hinged at
        // the slot root, which Roloff/Matek treats with the lever arms L1 (slot
        // root to seat centre) and L2 (slot root to bolt axis). That geometry is
        // not an input to this module, so the slit is only used to reduce the
        // wrapped contact angle. Results are therefore on the optimistic side for
        // single-slit collars - see the module verification status.

        double projectedArea = ShaftDiameter * EffectiveContactLength; // mm²
        double totalPreload = BoltPreloadForce * NumberOfBolts;        // N

        // Total normal force integrated over the contact surface
        NormalForce = Math.PI * totalPreload * (ContactAngle / 360.0);

        // Mean contact pressure from equilibrium (projected area)
        ContactPressure = projectedArea > 0 ? totalPreload / projectedArea : 0;

        // Peak contact pressure. Kp is the pressure-distribution factor
        // (Roloff/Matek): 1.0 uniform, 1.233 (π²/8) cosine, 1.571 (π/2) linear.
        // A slotted clamp with clearance approaches the linear distribution.
        const double Kp = Math.PI / 2.0;
        MaxContactPressure = ContactPressure * Kp;

        // Allowable contact pressure (based on weaker material)
        double hubYield = HubMaterial.YieldStrength;
        double shaftYield = ShaftMaterial.YieldStrength;
        double weakerYield = Math.Min(hubYield, shaftYield);
        AllowableContactPressure = weakerYield * 0.8; // 80% of yield for bearing
    }

    private void CalculateCapacities()
    {
        // Applied forces with load factor
        double effectiveTorque = AppliedTorque * LoadFactor;
        double effectiveAxialForce = AppliedAxialForce * LoadFactor;

        // Tangential force from torque
        double radius = ShaftDiameter / 2000.0; // m
        TangentialForce = effectiveTorque > 0 ? effectiveTorque / radius : 0;

        // Torque capacity: T = μ × Fn × r
        TorqueCapacity = FrictionCoefficientInterface * NormalForce * (ShaftDiameter / 2000.0);

        // Axial force capacity: Fa = μ × Fn
        AxialForceCapacity = FrictionCoefficientInterface * NormalForce;

        // Combined loading - required normal force
        // Fn,req = √((Ft/μ)² + (Fa/μ)²) for combined loading
        double ftOverMu = TangentialForce / FrictionCoefficientInterface;
        double faOverMu = effectiveAxialForce / FrictionCoefficientInterface;
        RequiredNormalForce = Math.Sqrt(ftOverMu * ftOverMu + faOverMu * faOverMu);
    }

    private void CalculateStresses()
    {
        double d = ShaftDiameter;
        double Da = ClampOuterDiameter;
        double Qa = d / Da;

        // Hub stresses (similar to thick-walled cylinder under external pressure)
        // The clamp experiences internal pressure from the bolt clamping

        // Tangential stress at inner surface (max tensile at slit region)
        HubTangentialStress = ContactPressure * (1 + Qa * Qa) / (1 - Qa * Qa);

        // Radial stress at inner surface
        HubRadialStress = -ContactPressure;

        // Von Mises equivalent stress
        HubVonMisesStress = Math.Sqrt(
            Math.Pow(HubTangentialStress, 2) -
            HubTangentialStress * HubRadialStress +
            Math.Pow(HubRadialStress, 2)
        );
    }

    private void CalculateSafetyFactors()
    {
        // Safety against sliding (torque + axial combined)
        if (RequiredNormalForce > 0)
        {
            SafetyFactorSliding = NormalForce / RequiredNormalForce;
        }
        else
        {
            SafetyFactorSliding = 999;
        }

        // Safety factor for hub stress
        SafetyFactorHubStress = HubVonMisesStress > 0
            ? HubMaterial.YieldStrength / HubVonMisesStress
            : 999;

        // Safety factor for bolt
        SafetyFactorBolt = BoltStress > 0
            ? BoltYieldStrength / BoltStress
            : 999;

        // Safety factor for contact pressure
        SafetyFactorPressure = MaxContactPressure > 0
            ? AllowableContactPressure / MaxContactPressure
            : 999;

        // Minimum overall safety factor
        SafetyFactorMin = Math.Min(
            Math.Min(SafetyFactorSliding, SafetyFactorHubStress),
            Math.Min(SafetyFactorBolt, SafetyFactorPressure)
        );
    }
}

// ============ SUPPORTING CLASSES ============

public class ClampDimension
{
    public double ShaftDiaMin { get; set; }
    public double ShaftDiaMax { get; set; }
    public double OuterDia { get; set; }      // Da
    public double Length { get; set; }         // L
    public double SlitWidth { get; set; }      // b
    public double BoltSize { get; set; }       // M

    public override string ToString() => $"Da={OuterDia}mm, L={Length}mm, M{BoltSize}";
}
