namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Helical extension spring calculation engine based on EN 13906-2
/// </summary>
public class ExtensionSpringEngine
{
    // Input Parameters - Geometry
    public double WireDiameter { get; set; }          // d (mm)
    public double MeanCoilDiameter { get; set; }      // Dm (mm)
    public double BodyLength { get; set; }            // LK (mm) - coil body length
    public double FreeLength { get; set; }            // L0 (mm) - total free length including hooks (calculated)
    public double ActiveCoils { get; set; }           // n (number)
    public string HookType { get; set; } = "German";  // German, English, Raised, Side, DoubleLoop, Threaded
    
    // Hook-specific parameters
    public double RaiseAmount { get; set; }           // For Raised Hook - raise height (mm)
    public double ThreadLength { get; set; }          // For Threaded Insert - thread length (mm)

    // Input Parameters - Material
    public double ShearModulus { get; set; } = 81500; // G (MPa)
    public double ElasticModulus { get; set; } = 206000; // E (MPa)
    public double TensileStrength { get; set; } = 1700; // Rm (MPa)
    public double AllowableShearStress { get; set; }  // τzul (MPa)
    public string MaterialGrade { get; set; } = "SH";

    // Input Parameters - Loading (deflection-based)
    public double InitialTension { get; set; }        // F0 - initial tension (N)
    public double Deflection1Input { get; set; }      // s1 - deflection at position 1 (mm)
    public double Deflection2Input { get; set; }      // s2 - deflection at position 2 (mm)

    // Calculated Values - Geometry
    public double SpringIndex { get; set; }           // w = Dm/d
    public double OuterDiameter { get; set; }         // De (mm)
    public double InnerDiameter { get; set; }         // Di (mm)
    public double CoilPitch { get; set; }             // p (mm) - typically equals d for extension springs
    public double WireLength { get; set; }            // Ld (mm)
    public double SpringMass { get; set; }            // m (g)
    public double HookOpeningInner { get; set; }      // LH inner dimension (mm)
    public double HookLength1 { get; set; }           // First hook length (mm)
    public double HookLength2 { get; set; }           // Second hook length (mm)

    // Calculated Values - Mechanical
    public double SpringRate { get; set; }            // R (N/mm)
    public double SpringRateCalculated { get; set; }  // R from formula
    public double MaxDeflection { get; set; }         // smax (mm)
    public double MaxForce { get; set; }              // Fn (N), the governing limit
    /// <summary>Force the BODY alone allows (N).</summary>
    public double MaxForceBody { get; set; }
    /// <summary>Force the HOOK allows (N). Infinite for a threaded insert.</summary>
    public double MaxForceHook { get; set; }
    /// <summary>"body" or "hook", whichever set the maximum force.</summary>
    public string MaxForceLimitedBy { get; set; } = "";
    public double StrokeLength { get; set; }          // sh = s2 - s1 (mm)
    public double Deflection1 { get; set; }           // s1 at position 1 (mm)
    public double Deflection2 { get; set; }           // s2 at position 2 (mm)
    public double Force1 { get; set; }                // F1 at s1 (N)
    public double Force2 { get; set; }                // F2 at s2 (N)

    // Calculated Values - Lengths
    public double Length1 { get; set; }               // L1 = L0 + s1 (mm)
    public double Length2 { get; set; }               // L2 = L0 + s2 (mm)
    public double MaxLength { get; set; }             // Lmax = L0 + smax (mm)

    // Calculated Values - Initial Tension
    public double InitialTensionMin { get; set; }     // F0 min (N)
    public double InitialTensionMax { get; set; }     // F0 max (N)
    public double InitialStress { get; set; }         // τ0 (MPa)

    // Calculated Values - Body Stress (uncorrected - without Wahl factor for display)
    public double WahlFactor { get; set; }            // k (stress correction factor)
    public double ShearStress0 { get; set; }          // τ0 at F0 (MPa) - uncorrected
    public double ShearStress1 { get; set; }          // τ1 at F1 (MPa) - uncorrected
    public double ShearStress2 { get; set; }          // τ2 at F2 (MPa) - uncorrected
    public double ShearStressMax { get; set; }        // τn at Fn (MPa) - uncorrected
    public double ShearStress1Corrected { get; set; } // τk1 at F1 (MPa) - with Wahl factor
    public double ShearStress2Corrected { get; set; } // τk2 at F2 (MPa) - with Wahl factor for safety calc
    public double StressRange { get; set; }           // τkh = τ2 - τ1 (MPa)

    // Calculated Values - Hook Stress
    public double HookBendingStress { get; set; }     // σB (MPa)
    public double HookShearStress { get; set; }       // τH (MPa)
    public double HookStressFactor { get; set; }      // Hook stress concentration
    public double HookRadius { get; set; }            // R1 - hook bend radius (mm)
    /// <summary>Bending moment arm at the body-to-hook section (mm): the loop radius, not R1.</summary>
    public double HookBendingMomentArm { get; set; }
    /// <summary>K1, bending concentration from the loop radius. Never below 1.</summary>
    public double HookBendingFactor { get; set; }
    /// <summary>K2, torsional concentration from the bend radius. Never below 1.</summary>
    public double HookShearFactor { get; set; }

    // Calculated Values - Safety (based on Position 2 or Max if no position defined)
    public double SafetyFactorBody { get; set; }      // νs body
    public double SafetyFactorHook { get; set; }      // νs hook - the lower of the two below
    public double SafetyFactorHookBending { get; set; } // σB against 0.70·Rm
    public double SafetyFactorHookShear { get; set; }   // τH against τzul
    public string HookCriticalMode { get; set; } = ""; // "Bending" or "Shear" - which governs the hook
    public string CriticalLocation { get; set; } = "Body"; // "Body" or "Hook"
    public double CriticalSafetyFactor { get; set; } // The lower of body/hook

    // Calculated Values - Fatigue Analysis
    public double FatigueMeanStress { get; set; }
    public double FatigueStressAmplitude { get; set; }
    public double FatigueStressRatio { get; set; }
    public double FatigueEnduranceLimit { get; set; }
    public double FatigueSafetyFactor { get; set; }
    public double FatigueEstimatedCycles { get; set; }
    /// <summary>
    /// Fatigue safety at the HOOK, which is where extension springs break. The
    /// check used to cover the body only, while the page's own text said so.
    /// </summary>
    public double FatigueHookSafetyFactor { get; set; }
    /// <summary>The lower of the body and hook fatigue safeties.</summary>
    public double FatigueGoverningSafetyFactor { get; set; }
    /// <summary>"body" or "hook", whichever produced the governing safety.</summary>
    public string FatigueGoverningSite { get; set; } = "";

    // Calculated Values - Natural Frequency
    public double NaturalFrequency { get; set; }      // fe (Hz)

    // Hook type dimensions and properties
    public static readonly Dictionary<string, HookTypeInfo> HookTypes = new()
    {
        { "German", new HookTypeInfo(0.5, 0.95, 1.0, "German Hook (Full Loop)", "Standard, most common, good general use") },
        { "English", new HookTypeInfo(0.4, 0.85, 0.9, "English Hook (Machine Loop)", "Machine formed, slightly lower stress") },
        { "Raised", new HookTypeInfo(0.6, 1.1, 0.75, "Raised Hook (Elevated)", "Lowest stress concentration, requires raise amount") },
        { "Side", new HookTypeInfo(0.5, 0.7, 1.0, "Side Hook (90° offset)", "Compact, hooks at 90° to each other") },
        { "DoubleLoop", new HookTypeInfo(0.5, 1.0, 0.85, "Double Loop", "Two full loops, high strength connection") },
        { "Threaded", new HookTypeInfo(0.0, 0.0, 0.0, "Threaded Insert", "No hook stress, highest strength, requires thread length") }
    };

    public class HookTypeInfo
    {
        public double R1Factor { get; }
        public double HookLengthFactor { get; }
        /// <summary>
        /// How much of the full loop radius D/2 the bending moment arm actually
        /// is. This used to be a reduction applied to the stress CONCENTRATION,
        /// which let the factor fall below 1 - a bend that relieved stress. The
        /// geometry is where a raised hook earns its lower stress: it brings the
        /// load line closer to the body axis.
        /// </summary>
        public double MomentArmFactor { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public HookTypeInfo(double r1, double hookLen, double momentArm, string name, string desc)
        {
            R1Factor = r1;
            HookLengthFactor = hookLen;
            MomentArmFactor = momentArm;
            DisplayName = name;
            Description = desc;
        }
    }

    // Material properties
    public static readonly Dictionary<string, (double G, double E, double Rm, string Name)> Materials = new()
    {
        { "SH", (81500, 206000, 1700, "EN 10270-1 SH - Patented Spring Steel") },
        { "SL", (81500, 206000, 1400, "EN 10270-1 SL - Patented Spring Steel") },
        { "SM", (81500, 206000, 1550, "EN 10270-1 SM - Patented Spring Steel") },
        { "DH", (81500, 206000, 1900, "EN 10270-1 DH - Patented Spring Steel") },
        { "VDC", (79500, 206000, 1800, "EN 10270-2 VDC - Oil Hardened Steel") },
        { "VDSiCr", (79500, 206000, 1900, "EN 10270-2 VDSiCr - Oil Hardened SiCr") },
        { "1.4310", (73000, 185000, 1600, "EN 10270-3 1.4310 - Stainless Steel") },
        { "1.4568", (75000, 190000, 1900, "EN 10270-3 1.4568 - Stainless Steel") },
        { "CuSn8", (42000, 118000, 700, "EN 12166 CuSn8 - Phosphor Bronze") },
        { "CuBe2", (47000, 131000, 1200, "EN 12166 CuBe2 - Beryllium Copper") },
        { "Inconel", (77000, 214000, 1250, "Inconel X-750 - High Temperature") }
    };

    // Standard wire diameters (mm) - EN 10270-1
    public static readonly double[] StandardWireDiameters = 
    { 
        0.20, 0.22, 0.25, 0.28, 0.30, 0.32, 0.35, 0.40, 0.45, 0.50,
        0.55, 0.60, 0.65, 0.70, 0.80, 0.90, 1.00, 1.10, 1.20, 1.40,
        1.60, 1.80, 2.00, 2.20, 2.50, 2.80, 3.00, 3.20, 3.50, 4.00,
        4.50, 5.00, 5.50, 6.00, 6.50, 7.00, 8.00, 9.00, 10.00, 11.00,
        12.00, 13.00, 14.00, 15.00, 16.00
    };

    public void Calculate()
    {
        CalculateGeometry();
        CalculateInitialTension();
        CalculateSpringRate();
        CalculateWorkingConditions();
        CalculateBodyStresses();
        CalculateHookStresses();
        // After the hook stresses, because the maximum force is whichever of the
        // body and the hook reaches its limit first.
        ApplyHookForceLimit();
        CalculateSafety();
        CalculateNaturalFrequency();
        
        // Perform fatigue analysis only if there is a stress cycle
        if (Deflection1Input > 0 && Deflection2Input > 0 && Deflection1Input != Deflection2Input)
        {
            CalculateFatigue();
        }
    }

    private void CalculateFatigue()
    {
        // On the CORRECTED body stress. EN 13906-2 applies the stress correction
        // factor k to a dynamically loaded spring; running fatigue on the
        // uncorrected stress took the safety factor from 2,98 to 3,53 on the
        // default spring - 18 % optimistic, and inconsistent with the static
        // check beside it, which did correct.
        FatigueMeanStress = (ShearStress1Corrected + ShearStress2Corrected) / 2;
        FatigueStressAmplitude = Math.Abs(ShearStress2Corrected - ShearStress1Corrected) / 2;

        if (ShearStress2Corrected > 0)
        {
            FatigueStressRatio = ShearStress1Corrected / ShearStress2Corrected;
        }

        // Endurance limit estimation
        FatigueEnduranceLimit = 0.30 * TensileStrength;

        // Ultimate shear strength estimation
        double ultimateShear = 0.65 * TensileStrength;

        if (FatigueStressAmplitude > 0)
        {
            // Goodman relation for fatigue safety factor
            double goodmanValue = (FatigueStressAmplitude / FatigueEnduranceLimit) + (FatigueMeanStress / ultimateShear);
            FatigueSafetyFactor = 1.0 / goodmanValue;
        }
        else
        {
            FatigueSafetyFactor = 999;
        }

        // ---- The hook, which is where extension springs actually break ----
        //
        // The body was the only thing checked, while the page's own text says the
        // hook is the failure site. The hook stresses scale with the force, so
        // the two working positions give the amplitude and mean directly.
        FatigueHookSafetyFactor = 999;
        if (Force2 > 0 && HookType != "Threaded")
        {
            double hookAtF2 = Math.Max(HookBendingStress, HookShearStress);
            if (hookAtF2 > 0 && Force2 > Force1)
            {
                double hookAtF1 = hookAtF2 * Force1 / Force2;
                double hookMean = (hookAtF1 + hookAtF2) / 2;
                double hookAmplitude = (hookAtF2 - hookAtF1) / 2;

                // Bending governs at the hook, so it is judged against a bending
                // endurance limit rather than a shear one.
                double hookEndurance = 0.40 * TensileStrength;
                double hookUltimate = TensileStrength;

                if (hookAmplitude > 0)
                {
                    double g = hookAmplitude / hookEndurance + hookMean / hookUltimate;
                    FatigueHookSafetyFactor = g > 0 ? 1.0 / g : 999;
                }
            }
        }

        FatigueGoverningSafetyFactor = Math.Min(FatigueSafetyFactor, FatigueHookSafetyFactor);
        FatigueGoverningSite = FatigueHookSafetyFactor < FatigueSafetyFactor ? "hook" : "body";
        
        // Estimate cycles to failure (simplified S-N curve)
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

    private void CalculateGeometry()
    {
        // Spring index
        SpringIndex = MeanCoilDiameter / WireDiameter;

        // Diameters
        OuterDiameter = MeanCoilDiameter + WireDiameter;
        InnerDiameter = MeanCoilDiameter - WireDiameter;

        // Extension springs are typically wound with coils touching
        CoilPitch = WireDiameter;

        // Body length (coils touching)
        BodyLength = (ActiveCoils + 1) * WireDiameter;

        // Hook opening (inner dimension)
        HookOpeningInner = MeanCoilDiameter - WireDiameter;

        // Calculate hook lengths based on hook type
        CalculateHookLengths();

        // Free length = Body length + Hook lengths
        FreeLength = BodyLength + HookLength1 + HookLength2;

        // Wire length (body + hooks)
        double bodyWireLength = Math.PI * MeanCoilDiameter * (ActiveCoils + 1);
        double hookWireLength = CalculateHookWireLength();
        WireLength = bodyWireLength + hookWireLength;

        // Spring mass (steel density ~7850 kg/m³)
        double wireVolume = Math.PI * Math.Pow(WireDiameter / 2, 2) * WireLength;
        SpringMass = wireVolume * 7.85e-6 * 1000; // grams
    }

    private void CalculateHookLengths()
    {
        var hookInfo = HookTypes.GetValueOrDefault(HookType, HookTypes["German"]);
        
        if (HookType == "Threaded")
        {
            HookLength1 = ThreadLength > 0 ? ThreadLength : InnerDiameter * 0.8;
            HookLength2 = HookLength1;
        }
        else if (HookType == "Raised")
        {
            double baseHookLength = InnerDiameter * hookInfo.HookLengthFactor;
            double raise = RaiseAmount > 0 ? RaiseAmount : InnerDiameter * 0.3;
            HookLength1 = baseHookLength + raise;
            HookLength2 = HookLength1;
        }
        else
        {
            HookLength1 = InnerDiameter * hookInfo.HookLengthFactor;
            HookLength2 = HookLength1;
        }
    }

    private double CalculateHookWireLength()
    {
        if (HookType == "Threaded")
        {
            return 0;
        }
        
        var hookInfo = HookTypes.GetValueOrDefault(HookType, HookTypes["German"]);
        double hookArcLength = Math.PI * InnerDiameter * hookInfo.R1Factor;
        double straightLength = HookLength1 - (InnerDiameter * hookInfo.R1Factor);
        
        return 2 * (hookArcLength + Math.Max(0, straightLength));
    }

    private void CalculateInitialTension()
    {
        // Initial tension range per EN 13906-2: τ0 = 0.05 to 0.20 × Rm
        double tau0Min = 0.05 * TensileStrength;
        double tau0Max = 0.20 * TensileStrength;
        
        // Adjust based on spring index
        if (SpringIndex > 8)
        {
            tau0Max *= 0.8;
        }
        else if (SpringIndex < 5)
        {
            tau0Min *= 1.2;
        }

        // Convert stress to force: F0 = τ0 × π × d³ / (8 × Dm)
        double forceConstant = (Math.PI * Math.Pow(WireDiameter, 3)) / (8 * MeanCoilDiameter);
        InitialTensionMin = tau0Min * forceConstant;
        InitialTensionMax = tau0Max * forceConstant;

        // Use provided initial tension or calculate default
        if (InitialTension <= 0)
        {
            InitialTension = (InitialTensionMin + InitialTensionMax) / 2;
        }

        // Calculate actual initial stress
        InitialStress = InitialTension / forceConstant;
    }

    private void CalculateSpringRate()
    {
        // Spring rate: R = (G × d⁴) / (8 × Dm³ × n)
        SpringRateCalculated = (ShearModulus * Math.Pow(WireDiameter, 4)) / 
                               (8 * Math.Pow(MeanCoilDiameter, 3) * ActiveCoils);

        SpringRate = SpringRateCalculated;

        // Wahl correction factor (for safety calculations only)
        double c = SpringIndex;
        WahlFactor = (4 * c - 1) / (4 * c - 4) + 0.615 / c;

        // Allowable shear stress (0.45 × Rm for static)
        if (AllowableShearStress <= 0)
        {
            AllowableShearStress = 0.45 * TensileStrength;
        }
        
        // Maximum force and deflection (theoretical limits)
        // τmax = (8 × F × Dm) / (π × d³) → F = τmax × π × d³ / (8 × Dm)
        double stressConstant = (8 * MeanCoilDiameter) / (Math.PI * Math.Pow(WireDiameter, 3));
        MaxForceBody = AllowableShearStress / stressConstant;
        MaxForce = MaxForceBody;
        MaxForceLimitedBy = "body";
        MaxDeflection = (MaxForce - InitialTension) / SpringRate;
        MaxLength = FreeLength + MaxDeflection;
    }

    /// <summary>
    /// The maximum force was taken from the BODY alone, and on this spring the
    /// hook reaches its limit first: at the 150,2 N the body allows, the hook
    /// shear stress is 974 MPa against an allowable 765, a safety of 0,79. The
    /// stated maximum extension was therefore not a safe one.
    ///
    /// Both hook stresses are linear in the force, so each limit is found by
    /// scaling from the design force the hook stresses were computed at. Runs
    /// after CalculateHookStresses, which needs MaxForce to exist first.
    /// </summary>
    private void ApplyHookForceLimit()
    {
        MaxForceHook = double.PositiveInfinity;
        if (HookType == "Threaded") return;

        double designForce = (Deflection2 > 0 && Force2 > InitialTension) ? Force2 : MaxForceBody;
        if (designForce <= 0) return;

        double allowableBending = 0.70 * TensileStrength;

        if (HookBendingStress > 0)
            MaxForceHook = Math.Min(MaxForceHook, designForce * allowableBending / HookBendingStress);
        if (HookShearStress > 0)
            MaxForceHook = Math.Min(MaxForceHook, designForce * AllowableShearStress / HookShearStress);

        if (MaxForceHook < MaxForce)
        {
            MaxForce = MaxForceHook;
            MaxForceLimitedBy = "hook";
            MaxDeflection = SpringRate > 0 ? (MaxForce - InitialTension) / SpringRate : 0;
            if (MaxDeflection < 0) MaxDeflection = 0;
            MaxLength = FreeLength + MaxDeflection;
        }
    }

    private void CalculateWorkingConditions()
    {
        Deflection1 = Deflection1Input;
        Deflection2 = Deflection2Input;

        if (Deflection1 > 0)
        {
            Force1 = InitialTension + SpringRate * Deflection1;
            Length1 = FreeLength + Deflection1;
        }
        else
        {
            Force1 = InitialTension;
            Length1 = FreeLength;
        }

        if (Deflection2 > 0)
        {
            Force2 = InitialTension + SpringRate * Deflection2;
            Length2 = FreeLength + Deflection2;
        }
        else
        {
            Force2 = Force1;
            Length2 = Length1;
        }

        StrokeLength = Math.Abs(Deflection2 - Deflection1);
    }

    private void CalculateBodyStresses()
    {
        // Shear stress formula (UNCORRECTED - without Wahl factor):
        // τ = (8 × F × Dm) / (π × d³)
        double stressConstant = (8 * MeanCoilDiameter) / (Math.PI * Math.Pow(WireDiameter, 3));

        // Stress at initial tension
        ShearStress0 = stressConstant * InitialTension;

        // Stress at Position 1 (uncorrected)
        if (Deflection1 > 0)
        {
            ShearStress1 = stressConstant * Force1;
        }
        else
        {
            ShearStress1 = stressConstant * InitialTension;
        }

        // Stress at Position 2 (uncorrected)
        if (Deflection2 > 0)
        {
            ShearStress2 = stressConstant * Force2;
        }
        else
        {
            ShearStress2 = ShearStress1;
        }

        // Corrected stresses (with the Wahl factor), used by the safety and the
        // fatigue checks. Position 1 was never corrected, so the fatigue check
        // ran on a mixed pair.
        ShearStress1Corrected = WahlFactor * ShearStress1;
        ShearStress2Corrected = WahlFactor * ShearStress2;

        // Maximum stress (uncorrected)
        ShearStressMax = stressConstant * MaxForce;

        // Stress range (for fatigue)
        StressRange = Math.Abs(ShearStress2 - ShearStress1);
    }

    private void CalculateHookStresses()
    {
        var hookInfo = HookTypes.GetValueOrDefault(HookType, HookTypes["German"]);
        
        if (HookType == "Threaded")
        {
            HookBendingStress = 0;
            HookShearStress = 0;
            HookStressFactor = 0;
            HookRadius = 0;
            return;
        }

        // Hook bend radius, at the transition from the body into the hook.
        HookRadius = hookInfo.R1Factor * InnerDiameter / 2;
        if (HookRadius < WireDiameter)
        {
            HookRadius = WireDiameter;
        }

        // ---- Two separate stress concentration factors ----
        //
        // These were one factor, computed from the BEND radius and applied to
        // both the bending and the shear stress, with a per-hook-type reduction
        // multiplier on top. Three things were wrong with that:
        //
        //  * The bending moment arm is the radius of the hook LOOP, not the bend
        //    radius. The load comes in through the middle of the loop, so the
        //    section where the hook meets the body carries M = F·D/2. Using R1
        //    (3,5 mm against 8 mm on the default spring) halved the bending
        //    stress: 298 MPa reported against 575 actual, a hook bending safety
        //    of 3,99 where the real figure is 2,07.
        //  * It also made the model run backwards. A LARGER bend radius came out
        //    as more stress, where in reality a generous bend relieves the stress
        //    concentration.
        //  * The per-type reduction multiplier could take the factor BELOW 1 - a
        //    raised hook gave 0,91 - so the hook came out less stressed than the
        //    plain body, which no bend can do. A bend always concentrates.
        //
        // Bending, at the body-to-hook section, on the loop radius:
        //   K1 = (4 C1² − C1 − 1) / (4 C1 (C1 − 1)),   C1 = D/d
        // Torsion, at the bend itself, on the bend radius:
        //   K2 = (4 C2 − 1) / (4 C2 − 4),              C2 = 2 R1/d
        double c1 = WireDiameter > 0 ? MeanCoilDiameter / WireDiameter : 0;
        HookBendingFactor = c1 > 1.1
            ? (4 * c1 * c1 - c1 - 1) / (4 * c1 * (c1 - 1))
            : 1.5;

        double c2 = WireDiameter > 0 ? 2 * HookRadius / WireDiameter : 0;
        HookShearFactor = c2 > 1.1
            ? (4 * c2 - 1) / (4 * c2 - 4)
            : 1.5;

        // A bend cannot relieve stress, so neither factor may drop below 1.
        // The hook TYPE changes the geometry - a raised hook shortens the bending
        // moment arm, which the arm itself has to express - not the concentration.
        if (HookBendingFactor < 1.0) HookBendingFactor = 1.0;
        if (HookShearFactor < 1.0) HookShearFactor = 1.0;

        // Reported as the governing one of the two, for the single-value row.
        HookStressFactor = Math.Max(HookBendingFactor, HookShearFactor);

        // Design force: use Position 2 force if defined, otherwise use max force
        double designForce = (Deflection2 > 0 && Force2 > InitialTension) ? Force2 : MaxForce;

        // Hook bending stress, moment arm D/2 reduced by the hook type: a raised
        // hook brings the load line closer to the body axis, which is the real
        // mechanism behind its lower stress.
        HookBendingMomentArm = MeanCoilDiameter / 2.0 * hookInfo.MomentArmFactor;
        double bendingMoment = (32 * designForce * HookBendingMomentArm) / (Math.PI * Math.Pow(WireDiameter, 3));
        double directStress = (4 * designForce) / (Math.PI * Math.Pow(WireDiameter, 2));
        HookBendingStress = HookBendingFactor * bendingMoment + directStress;

        // Hook shear stress
        HookShearStress = HookShearFactor * (8 * designForce * MeanCoilDiameter) /
                          (Math.PI * Math.Pow(WireDiameter, 3));
    }

    private void CalculateSafety()
    {
        // Body safety factor based on Position 2 (or max if no position defined)
        // Using corrected stress (with Wahl factor) for safety calculation
        double bodyStressForSafety;
        
        if (Deflection2 > 0)
        {
            // Use Position 2 corrected stress
            bodyStressForSafety = ShearStress2Corrected;
        }
        else
        {
            // No position defined, use max stress with Wahl correction
            bodyStressForSafety = WahlFactor * ShearStressMax;
        }

        if (bodyStressForSafety > 0)
        {
            SafetyFactorBody = AllowableShearStress / bodyStressForSafety;
        }
        else
        {
            SafetyFactorBody = 999;
        }

        // Hook safety factor.
        //
        // EN 13906-2 checks the hook in TWO places: bending at the transition from
        // the body into the hook, and torsion/shear in the hook bend itself. The
        // shear stress was being computed and displayed but compared against
        // nothing, so a hook that was critical in shear reported the (higher)
        // bending safety factor and the summary named the wrong critical location -
        // optimistic, on the unsafe side.
        if (HookType == "Threaded")
        {
            // A threaded insert has no hook bend to check.
            SafetyFactorHookBending = 999;
            SafetyFactorHookShear = 999;
            SafetyFactorHook = 999;
            HookCriticalMode = "";
        }
        else
        {
            double allowableBendingStress = 0.70 * TensileStrength;

            SafetyFactorHookBending = HookBendingStress > 0
                ? allowableBendingStress / HookBendingStress
                : 999;

            SafetyFactorHookShear = HookShearStress > 0
                ? AllowableShearStress / HookShearStress
                : 999;

            SafetyFactorHook = Math.Min(SafetyFactorHookBending, SafetyFactorHookShear);
            HookCriticalMode = SafetyFactorHookShear < SafetyFactorHookBending ? "Shear" : "Bending";
        }

        // Determine critical location and safety factor
        if (SafetyFactorHook < SafetyFactorBody && SafetyFactorHook < 999)
        {
            CriticalLocation = "Hook";
            CriticalSafetyFactor = SafetyFactorHook;
        }
        else
        {
            CriticalLocation = "Body";
            CriticalSafetyFactor = SafetyFactorBody;
        }
    }

    private void CalculateNaturalFrequency()
    {
        double rho = 7850; // kg/m³
        NaturalFrequency = (WireDiameter / 1000) / 
                          (Math.PI * ActiveCoils * Math.Pow(MeanCoilDiameter / 1000, 2)) * 
                          Math.Sqrt(ShearModulus * 1e6 / (8 * rho));
    }

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (WireDiameter <= 0)
            errors.Add("Wire diameter must be positive");
        
        if (MeanCoilDiameter <= 0)
            errors.Add("Mean coil diameter must be positive");
        
        if (SpringIndex < 3 || SpringIndex > 20)
            errors.Add($"Spring index {SpringIndex:F1} is outside recommended range (3-20)");
        
        if (ActiveCoils < 3)
            errors.Add("Minimum 3 active coils recommended for extension springs");
        
        if (InitialTension < InitialTensionMin * 0.5)
            errors.Add($"Initial tension {InitialTension:F1}N is very low (min recommended: {InitialTensionMin:F1}N)");
        
        if (InitialTension > InitialTensionMax * 1.5)
            errors.Add($"Initial tension {InitialTension:F1}N is very high (max recommended: {InitialTensionMax:F1}N)");

        if (Deflection2 > MaxDeflection)
            errors.Add($"Working deflection {Deflection2:F1}mm exceeds maximum {MaxDeflection:F1}mm");

        if (CriticalSafetyFactor < 1.0)
            errors.Add($"Safety factor {CriticalSafetyFactor:F2} is below 1.0 - UNSAFE DESIGN");

        if (SafetyFactorHook < 1.2 && HookType != "Threaded")
            errors.Add($"Hook safety factor {SafetyFactorHook:F2} is low - consider Raised Hook or Threaded Insert");

        return errors;
    }

    public static double CalculateActiveCoils(double wireDia, double meanDia, double rate, double G)
    {
        return (G * Math.Pow(wireDia, 4)) / (8 * Math.Pow(meanDia, 3) * rate);
    }

    public static double CalculateInitialTensionFromStress(double wireDia, double meanDia, double tau0)
    {
        return tau0 * (Math.PI * Math.Pow(wireDia, 3)) / (8 * meanDia);
    }
}
