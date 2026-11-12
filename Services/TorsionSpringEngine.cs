namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Helical torsion spring calculation engine based on EN 13906-3
/// </summary>
public class TorsionSpringEngine
{
    // ============ INPUT PARAMETERS ============

    // Input Parameters - Geometry
    public double WireDiameter { get; set; }          // d (mm)
    public double MeanCoilDiameter { get; set; }      // Dm (mm)
    public double ActiveCoils { get; set; }           // n (number)
    public double Leg1Length { get; set; }            // L1 (mm) - first leg length
    public double Leg2Length { get; set; }            // L2 (mm) - second leg length
    public string LegType { get; set; } = "Tangent";  // Tangent, Radial, Axial
    /// <summary>
    /// Winding hand. Drawing and manufacturing information only: it does NOT by
    /// itself decide whether the spring closes or opens under load. The page used
    /// to claim "left-hand = load opens spring", which is wrong - either hand can
    /// be loaded either way - and the results were identical for both anyway.
    /// </summary>
    public string WindingDirection { get; set; } = "Right"; // Right or Left

    /// <summary>
    /// Which way the load turns the spring relative to its winding: "Closing"
    /// tightens the coil onto the mandrel, "Opening" unwinds it. This is the
    /// input that actually matters, and it did not exist.
    /// </summary>
    public string LoadDirection { get; set; } = "Closing";

    /// <summary>Mandrel (arbor) diameter, mm. 0 means no mandrel check is possible.</summary>
    public double MandrelDiameter { get; set; }

    // Input Parameters - Material
    public double ElasticModulus { get; set; } = 206000;  // E (MPa)
    public double TensileStrength { get; set; } = 1700;   // Rm (MPa)
    public double AllowableBendingStress { get; set; }    // σzul (MPa)
    public string MaterialGrade { get; set; } = "SH";

    // Input Parameters - Operating Conditions
    public double Angle1Input { get; set; }           // α1 - deflection at position 1 (degrees)
    public double Angle2Input { get; set; }           // α2 - deflection at position 2 (degrees)
    public double Torque1Input { get; set; }          // M1 - moment at position 1 (Nmm) - alternative input
    public double Torque2Input { get; set; }          // M2 - moment at position 2 (Nmm) - alternative input

    // ============ CALCULATED VALUES - GEOMETRY ============

    public double SpringIndex { get; set; }           // w = Dm/d
    public double OuterDiameter { get; set; }         // De (mm)
    public double InnerDiameter { get; set; }         // Di (mm)
    public double BodyLength { get; set; }            // Lk (mm) - coil body length
    public double TotalCoils { get; set; }            // nt (total coils including inactive)
    public double EquivalentCoils { get; set; }       // ne = n + (L1+L2)/(3·pi·Dm), EN 13906-3
    public double CoilPitch { get; set; }             // p (mm)
    public double WireLength { get; set; }            // Ld (mm)
    public double SpringMass { get; set; }            // m (g)
    public double MomentArm { get; set; }             // R (mm) - effective moment arm

    // For torsion springs, the inner diameter changes with deflection
    public double InnerDiameterAtAngle1 { get; set; } // Di at α1
    public double InnerDiameterAtAngle2 { get; set; } // Di at α2
    public double InnerDiameterAtMax { get; set; }    // Di at max deflection

    // ============ CALCULATED VALUES - MECHANICAL ============

    public double SpringRate { get; set; }            // R (Nmm/degree)
    public double SpringRatePerRadian { get; set; }   // R (Nmm/rad)
    public double StressCorrectionFactor { get; set; } // q - EN 13906-3 stress correction factor
    /// <summary>
    /// The Wahl bending form of q, for comparison. About 1 % below the
    /// EN 13906-3 value, which is why it was on the unsafe side.
    /// </summary>
    public double WahlBendingFactor { get; set; }
    /// <summary>Smallest bore over every position asked for (mm).</summary>
    public double SmallestInnerDiameter { get; set; }
    /// <summary>Clearance between the smallest bore and the mandrel (mm). 0 when unchecked.</summary>
    public double MandrelClearance { get; set; }
    /// <summary>False when no mandrel diameter was entered, so there is nothing to check.</summary>
    public bool MandrelChecked { get; set; }
    /// <summary>0,85 of the smallest bore, the usual manufacturing recommendation (mm).</summary>
    public double RecommendedMandrelDiameter { get; set; }
    /// <summary>True when the permissible stress was lowered for an opening load.</summary>
    public bool AllowableStressReducedForOpening { get; set; }

    // Position 1 values
    public double Angle1 { get; set; }                // α1 (degrees)
    public double Torque1 { get; set; }               // M1 (Nmm)
    public double BendingStress1 { get; set; }        // σ1 (MPa)

    // Position 2 values
    public double Angle2 { get; set; }                // α2 (degrees)
    public double Torque2 { get; set; }               // M2 (Nmm)
    public double BendingStress2 { get; set; }        // σ2 (MPa)

    // Maximum values
    public double MaxDeflection { get; set; }         // αmax (degrees)
    public double MaxTorque { get; set; }             // Mmax (Nmm)
    public double MaxBendingStress { get; set; }      // σmax (MPa)

    // Working stroke
    public double WorkingAngle { get; set; }          // Δα = α2 - α1 (degrees)
    public double WorkingTorque { get; set; }         // ΔM = M2 - M1 (Nmm)

    // ============ CALCULATED VALUES - SAFETY ============

    public double SafetyFactorStatic { get; set; }    // Based on position 2 or max
    public double SafetyFactorPosition1 { get; set; }
    public double SafetyFactorPosition2 { get; set; }

    // ============ CALCULATED VALUES - FATIGUE ============

    public double FatigueMeanStress { get; set; }
    public double FatigueStressAmplitude { get; set; }
    public double FatigueStressRatio { get; set; }
    public double FatigueEnduranceLimit { get; set; }
    public double FatigueSafetyFactor { get; set; }
    public double FatigueEstimatedCycles { get; set; }

    // ============ STANDARD DATA ============

    // Standard wire diameters (mm) - EN 10270-1
    public static readonly double[] StandardWireDiameters =
    {
        0.20, 0.22, 0.25, 0.28, 0.30, 0.32, 0.35, 0.40, 0.45, 0.50,
        0.55, 0.60, 0.65, 0.70, 0.80, 0.90, 1.00, 1.10, 1.20, 1.40,
        1.60, 1.80, 2.00, 2.20, 2.50, 2.80, 3.00, 3.20, 3.50, 4.00,
        4.50, 5.00, 5.50, 6.00, 6.50, 7.00, 8.00, 9.00, 10.00, 11.00,
        12.00, 13.00, 14.00, 15.00, 16.00
    };

    // Material properties
    public static readonly Dictionary<string, (double E, double Rm, string Name)> Materials = new()
    {
        { "SH", (206000, 1700, "EN 10270-1 SH - Patented Spring Steel") },
        { "SL", (206000, 1400, "EN 10270-1 SL - Patented Spring Steel") },
        { "SM", (206000, 1550, "EN 10270-1 SM - Patented Spring Steel") },
        { "DH", (206000, 1900, "EN 10270-1 DH - Patented Spring Steel") },
        { "VDC", (206000, 1800, "EN 10270-2 VDC - Oil Hardened Steel") },
        { "VDSiCr", (206000, 1900, "EN 10270-2 VDSiCr - Oil Hardened SiCr") },
        { "1.4310", (185000, 1600, "EN 10270-3 1.4310 - Stainless Steel") },
        { "1.4568", (190000, 1900, "EN 10270-3 1.4568 - Stainless Steel") },
        { "CuSn8", (118000, 700, "EN 12166 CuSn8 - Phosphor Bronze") },
        { "CuBe2", (131000, 1200, "EN 12166 CuBe2 - Beryllium Copper") },
        { "Inconel", (214000, 1250, "Inconel X-750 - High Temperature") }
    };

    // Leg type information
    public static readonly Dictionary<string, (double ArmFactor, string Description)> LegTypes = new()
    {
        { "Tangent", (1.0, "Tangent legs - standard, perpendicular to coil axis") },
        { "Radial", (0.9, "Radial legs - pointing toward coil center") },
        { "Axial", (0.85, "Axial legs - parallel to coil axis") }
    };

    // ============ MAIN CALCULATION METHOD ============

    public void Calculate()
    {
        CalculateGeometry();
        CalculateSpringRate();
        CalculateWorkingConditions();
        CalculateStresses();
        CalculateSafety();

        // Perform fatigue analysis if there is a stress cycle
        if (Angle1Input > 0 && Angle2Input > 0 && Math.Abs(Angle1Input - Angle2Input) > 0.1)
        {
            CalculateFatigue();
        }
    }

    // ============ CALCULATION STEPS ============

    private void CalculateGeometry()
    {
        // Spring index
        SpringIndex = MeanCoilDiameter / WireDiameter;

        // Diameters
        OuterDiameter = MeanCoilDiameter + WireDiameter;
        InnerDiameter = MeanCoilDiameter - WireDiameter;

        // Total coils (torsion springs typically have inactive partial coils at ends)
        TotalCoils = ActiveCoils + 0.5; // Approximate

        // Coil pitch - torsion springs are usually wound with coils touching
        // or with small gap for friction-free rotation
        CoilPitch = WireDiameter * 1.1; // Small gap

        // Body length
        BodyLength = TotalCoils * CoilPitch;

        // Moment arm based on leg type
        var legInfo = LegTypes.GetValueOrDefault(LegType, LegTypes["Tangent"]);
        MomentArm = (MeanCoilDiameter / 2) * legInfo.ArmFactor + (Leg1Length + Leg2Length) / 4;

        // Wire length
        double coilWireLength = Math.PI * MeanCoilDiameter * TotalCoils;
        double legWireLength = Leg1Length + Leg2Length;
        WireLength = coilWireLength + legWireLength;

        // Spring mass (steel density ~7850 kg/m³)
        double wireVolume = Math.PI * Math.Pow(WireDiameter / 2, 2) * WireLength;
        SpringMass = wireVolume * 7.85e-6 * 1000; // grams

        // Stress correction factor for curved beam bending, EN 13906-3:
        //   q = (w + 0,07) / (w - 0,75)
        // The Wahl bending form (4w^2 - w - 1)/(4w(w - 1)) was used instead. It
        // is about 1 % LOWER - 1,103 against 1,113 at w = 8 - so it was on the
        // unsafe side, and the page is titled "according to EN 13906-3".
        double w = SpringIndex;
        if (w > 1.0)
        {
            StressCorrectionFactor = (w + 0.07) / (w - 0.75);
            WahlBendingFactor = w > 1.1 ? (4 * w * w - w - 1) / (4 * w * (w - 1)) : 1.5;
        }
        else
        {
            StressCorrectionFactor = 1.5; // Minimum value for very tight coils
            WahlBendingFactor = 1.5;
        }
    }

    private void CalculateSpringRate()
    {
        // Torsion spring rate formula (EN 13906-3):
        // R = (E × d⁴) / (3667 × Dm × ne) [Nmm/degree]
        // or R = (E × d⁴) / (64 × Dm × ne) [Nmm/rad]
        //
        // ne, not n: the legs bend too, so EN 13906-3 adds their contribution as an
        // EQUIVALENT number of coils,
        //     ne = n + (L1 + L2) / (3 · pi · Dm)
        // The engine already used the legs for the moment arm and the wire length but
        // not here, which made the spring read ~6% stiffer than it is. That is
        // conservative for stress at a given angle, but UNsafe for the reverse
        // question a designer usually asks - "what angle do I need for this torque" -
        // because it under-predicts the angle.
        EquivalentCoils = ActiveCoils + (Leg1Length + Leg2Length) / (3.0 * Math.PI * MeanCoilDiameter);

        SpringRate = (ElasticModulus * Math.Pow(WireDiameter, 4)) /
                     (3667 * MeanCoilDiameter * EquivalentCoils);

        SpringRatePerRadian = (ElasticModulus * Math.Pow(WireDiameter, 4)) /
                              (64 * MeanCoilDiameter * EquivalentCoils);

        // Allowable bending stress, 0,70 Rm for a statically loaded spring
        // closing onto its coil.
        //
        // Coiling leaves residual stresses that OPPOSE a closing load, which is
        // why a torsion spring is normally used that way and why the permissible
        // stress is as high as it is. Load it in the opening direction and those
        // residuals add to the applied stress instead, so EN 13906-3 does not
        // allow the same figure. 0,55 Rm is a conservative practice value for
        // that case - the standard's own reduction needs its text, so it is
        // stated on the page as our own assumption rather than as the standard's.
        if (AllowableBendingStress <= 0)
        {
            AllowableBendingStress = (LoadDirection == "Opening" ? 0.55 : 0.70) * TensileStrength;
        }
        AllowableStressReducedForOpening = LoadDirection == "Opening";

        // Maximum values based on allowable stress
        // σ = (32 × M × q) / (π × d³)
        // M = σ × π × d³ / (32 × q)
        double sectionModulus = Math.PI * Math.Pow(WireDiameter, 3) / 32;
        MaxTorque = AllowableBendingStress * sectionModulus / StressCorrectionFactor;
        MaxDeflection = MaxTorque / SpringRate;
        MaxBendingStress = AllowableBendingStress;
    }

    private void CalculateWorkingConditions()
    {
        // Calculate from angle input (primary method)
        if (Angle1Input > 0 || Angle2Input > 0)
        {
            Angle1 = Angle1Input;
            Angle2 = Angle2Input;
            Torque1 = SpringRate * Angle1;
            Torque2 = SpringRate * Angle2;
        }
        // Or calculate from torque input (alternative method)
        else if (Torque1Input > 0 || Torque2Input > 0)
        {
            Torque1 = Torque1Input;
            Torque2 = Torque2Input;
            Angle1 = SpringRate > 0 ? Torque1 / SpringRate : 0;
            Angle2 = SpringRate > 0 ? Torque2 / SpringRate : 0;
        }

        // Working stroke
        WorkingAngle = Math.Abs(Angle2 - Angle1);
        WorkingTorque = Math.Abs(Torque2 - Torque1);

        // Calculate inner diameter change with deflection
        // For closing spring (wound in direction of load):
        // Di(α) = Di × n / (n + α/360)
        // For opening spring (wound against direction of load):
        // Di(α) = Di × n / (n - α/360)

        // EN 13906-3 scales the MEAN diameter and then subtracts the wire:
        //   Di(alpha) = Dm . n/(n +/- alpha/360) - d
        // The engine scaled the INNER diameter instead, which leaves the wire
        // thickness being scaled along with the coil. That reads high - 0,08 mm
        // on the default spring, 0,50 mm at n = 2 - and the page tells the reader
        // to size the mandrel against the smallest diameter, so it was optimistic
        // in exactly the place it matters.
        //
        // The sign follows the LOAD direction, not the winding hand: a spring
        // loaded in the closing direction tightens onto the mandrel, one loaded
        // in the opening direction grows away from it.
        int sign = LoadDirection == "Opening" ? -1 : +1;

        double DiAt(double angle)
        {
            if (ActiveCoils <= 0) return InnerDiameter;
            double coils = ActiveCoils + sign * angle / 360.0;
            if (coils <= 0) return 0;
            return MeanCoilDiameter * ActiveCoils / coils - WireDiameter;
        }

        InnerDiameterAtAngle1 = Angle1 > 0 ? DiAt(Angle1) : InnerDiameter;
        InnerDiameterAtAngle2 = Angle2 > 0 ? DiAt(Angle2) : InnerDiameter;
        InnerDiameterAtMax = DiAt(MaxDeflection);

        // The smallest bore the mandrel has to fit through, over every position
        // asked for. At an angle past the permissible one the "at max" row is no
        // longer the smallest, which made that row misleading on its own.
        SmallestInnerDiameter = Math.Min(
            Math.Min(InnerDiameterAtAngle1, InnerDiameterAtAngle2),
            InnerDiameterAtMax);
        if (LoadDirection == "Opening") SmallestInnerDiameter = InnerDiameter;

        // Real mandrel clearance, when a mandrel diameter has been entered.
        // The row used to read "adequate clearance" with no mandrel diameter to
        // compare against - it was showing the bore and calling it a check.
        MandrelClearance = MandrelDiameter > 0 ? SmallestInnerDiameter - MandrelDiameter : 0;
        MandrelChecked = MandrelDiameter > 0;
        // Manufacturers size the mandrel at 0,8-0,9 of the smallest bore.
        RecommendedMandrelDiameter = 0.85 * SmallestInnerDiameter;
    }

    private void CalculateStresses()
    {
        // Bending stress formula:
        // σ = (32 × M × q) / (π × d³)
        double stressConstant = (32 * StressCorrectionFactor) / (Math.PI * Math.Pow(WireDiameter, 3));

        if (Torque1 > 0)
        {
            BendingStress1 = stressConstant * Torque1;
        }

        if (Torque2 > 0)
        {
            BendingStress2 = stressConstant * Torque2;
        }
    }

    private void CalculateSafety()
    {
        // Safety factor at position 1
        if (BendingStress1 > 0)
        {
            SafetyFactorPosition1 = AllowableBendingStress / BendingStress1;
        }
        else
        {
            SafetyFactorPosition1 = 999;
        }

        // Safety factor at position 2
        if (BendingStress2 > 0)
        {
            SafetyFactorPosition2 = AllowableBendingStress / BendingStress2;
        }
        else
        {
            SafetyFactorPosition2 = 999;
        }

        // Static safety factor (based on higher stress position)
        double maxOperatingStress = Math.Max(BendingStress1, BendingStress2);
        if (maxOperatingStress > 0)
        {
            SafetyFactorStatic = AllowableBendingStress / maxOperatingStress;
        }
        else
        {
            SafetyFactorStatic = 999;
        }
    }

    private void CalculateFatigue()
    {
        // Mean stress and amplitude
        FatigueMeanStress = (BendingStress1 + BendingStress2) / 2;
        FatigueStressAmplitude = Math.Abs(BendingStress2 - BendingStress1) / 2;

        if (BendingStress2 > 0)
        {
            FatigueStressRatio = BendingStress1 / BendingStress2;
        }

        // Endurance limit estimation for bending (approximately 0.35 × Rm)
        FatigueEnduranceLimit = 0.35 * TensileStrength;

        // Ultimate bending strength estimation
        double ultimateBending = 0.85 * TensileStrength;

        if (FatigueStressAmplitude > 0)
        {
            // Goodman relation for fatigue safety factor
            double goodmanValue = (FatigueStressAmplitude / FatigueEnduranceLimit) +
                                  (FatigueMeanStress / ultimateBending);
            FatigueSafetyFactor = 1.0 / goodmanValue;
        }
        else
        {
            FatigueSafetyFactor = 999;
        }

        // Estimate cycles to failure
        if (FatigueStressAmplitude < FatigueEnduranceLimit * 0.3)
        {
            FatigueEstimatedCycles = double.PositiveInfinity;
        }
        else if (FatigueStressAmplitude < FatigueEnduranceLimit)
        {
            double ratio = FatigueStressAmplitude / FatigueEnduranceLimit;
            FatigueEstimatedCycles = Math.Pow(10, 7 - 3 * (ratio - 0.3) / 0.7);
        }
        else
        {
            double ratio = FatigueStressAmplitude / FatigueEnduranceLimit;
            FatigueEstimatedCycles = Math.Pow(10, 6 - 4 * (ratio - 1));
            if (FatigueEstimatedCycles < 1000) FatigueEstimatedCycles = 1000;
        }
    }

    // Natural frequency intentionally NOT calculated.
    //
    // EN 13906-3 standardises no surge frequency for torsion springs (unlike -1 for
    // compression springs, where fe is defined). What used to be here was the
    // compression-spring formula with E substituted for G and a stray g term, which
    // returned ~4.5 kHz for an ordinary spring - a figure that could be neither
    // verified against the standard nor sanity-checked by the user. It was removed
    // rather than left sitting among values that are all traceable to EN 13906-3.
    //
    // If a surge check is ever needed here, take it from a cited source and label it
    // as outside the standard.

    // ============ VALIDATION ============

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (WireDiameter <= 0)
            errors.Add("Wire diameter must be positive");

        if (MeanCoilDiameter <= 0)
            errors.Add("Mean coil diameter must be positive");

        if (SpringIndex < 3 || SpringIndex > 20)
            errors.Add($"Spring index {SpringIndex:F1} is outside recommended range (3-20)");

        if (ActiveCoils < 2)
            errors.Add("Minimum 2 active coils recommended for torsion springs");

        if (Leg1Length <= 0 || Leg2Length <= 0)
            errors.Add("Leg lengths must be positive");

        if (Angle2 > MaxDeflection)
            errors.Add($"Working angle {Angle2:F1}° exceeds maximum {MaxDeflection:F1}°");

        if (InnerDiameterAtAngle2 < WireDiameter * 2)
            errors.Add($"Inner diameter at position 2 ({InnerDiameterAtAngle2:F1}mm) too small - may bind on mandrel");

        if (SafetyFactorStatic < 1.0)
            errors.Add($"Safety factor {SafetyFactorStatic:F2} is below 1.0 - UNSAFE DESIGN");

        return errors;
    }

    // ============ UTILITY METHODS ============

    public static double CalculateActiveCoils(double wireDia, double meanDia, double rate, double E)
    {
        // n = (E × d⁴) / (3667 × Dm × R)
        return (E * Math.Pow(wireDia, 4)) / (3667 * meanDia * rate);
    }

    public static double CalculateTorqueFromAngle(double angle, double rate)
    {
        return angle * rate;
    }

    public static double CalculateAngleFromTorque(double torque, double rate)
    {
        return rate > 0 ? torque / rate : 0;
    }
}
