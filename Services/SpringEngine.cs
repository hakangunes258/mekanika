namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Helical compression spring calculation engine based on EN 13906-1
/// </summary>
public class SpringEngine
{
    // Input Parameters - Geometry
    public double WireDiameter { get; set; }          // d (mm)
    public double MeanCoilDiameter { get; set; }      // Dm (mm)
    public double FreeLength { get; set; }            // L0 (mm)
    public double ActiveCoils { get; set; }           // n (number)
    public double TotalCoils { get; set; }            // nt (number)
    public string EndType { get; set; } = "ClosedGround"; // Open, Closed, ClosedGround

    // Input Parameters - End Conditions (for buckling). These strings are what the
    // page's <select> binds and what share links carry, so SeatingFactor must
    // keep resolving every key it has ever accepted.
    public string EndCondition { get; set; } = "BothFlat";

    // Input Parameters - Material
    public double ShearModulus { get; set; } = 81500; // G (MPa) - default for spring steel
    /// <summary>
    /// E (MPa). Needed by the EN 13906-1 buckling expression, which depends on
    /// G/E and not on G alone. 206 000 is the value the standard uses for
    /// patented drawn and oil-hardened spring steel.
    /// </summary>
    public double ElasticModulus { get; set; } = 206000;
    public double TensileStrength { get; set; } = 1700; // Rm (MPa)
    public double AllowableShearStress { get; set; }  // τzul (MPa)
    public string MaterialGrade { get; set; } = "SH"; // Material grade

    // Input Parameters - Shot Peening
    public bool ShotPeened { get; set; } = false;

    // Input Parameters - Loading
    public double Force1 { get; set; }                // F1 at L1 (N)
    public double Length1 { get; set; }               // L1 (mm)
    public double Force2 { get; set; }                // F2 at L2 (N)
    public double Length2 { get; set; }               // L2 (mm)

    // Calculated Values - Geometry
    public double SpringIndex { get; set; }           // w = Dm/d
    public double OuterDiameter { get; set; }         // De (mm)
    public double InnerDiameter { get; set; }         // Di (mm)
    public double CoilPitch { get; set; }             // p (mm)
    public double SolidLength { get; set; }           // Lc (mm)
    public double WireLength { get; set; }            // Ld (mm)
    public double SpringMass { get; set; }            // m (g)

    // Calculated Values - Mechanical
    public double SpringRate { get; set; }            // R (N/mm)
    public double SpringRateCalculated { get; set; }  // R from formula
    // EN 13906-1 reserves the "n" subscript for the smallest permissible working
    // position (Ln), which this engine does not model. The values below are at the
    // solid length, so they carry the "c" subscript in the UI.
    public double MaxDeflection { get; set; }         // sc = L0 - Lc (mm)
    public double MaxForce { get; set; }              // Fc (N)
    public double StrokeLength { get; set; }          // sh = s2 - s1 (mm)

    // Calculated Values - Stress
    public double WahlFactor { get; set; }            // k (stress correction, Wahl)
    /// <summary>
    /// k by Bergstraesser, (w + 0,5)/(w - 0,75), which is the form EN 13906-1
    /// uses. About 1 % below Wahl; reported so a reader comparing against the
    /// standard can tell which factor a figure came from.
    /// </summary>
    public double BergstrasserFactor { get; set; }
    public double ShearStress1 { get; set; }          // τ1 at F1 (MPa)
    public double ShearStress2 { get; set; }          // τ2 at F2 (MPa)
    public double ShearStressMax { get; set; }        // τc at Fc (MPa)
    public double StressRange { get; set; }           // τkh = τ2 - τ1 (MPa)

    // Calculated Values - Safety & Buckling
    public double SafetyFactorStatic { get; set; }    // νs at the operating point F2
    public double SafetyFactorSolid { get; set; }     // νs if compressed to solid length
    public double SlendernessRatio { get; set; }      // λ = L0/Dm
    public double EndConditionFactor { get; set; }    // ν (seating / buckling length factor)
    public double EffectiveSlenderness { get; set; }  // ν·λ (effective slenderness for buckling)
    public double BucklingLimit { get; set; }         // s_K / L0, the critical deflection ratio
    /// <summary>s_K (mm): the deflection at which the spring buckles, EN 13906-1.</summary>
    public double BucklingDeflection { get; set; }
    /// <summary>False when the coil is too short and fat for buckling to exist at any deflection.</summary>
    public bool BucklingPossible { get; set; }
    public bool BucklingRisk { get; set; }

    /// <summary>
    /// Largest deflection the user actually asked for (from L1/L2). The buckling
    /// check is judged against this rather than against the solid-length
    /// deflection, which the spring never reaches in service.
    /// </summary>
    public double MaxWorkingDeflection { get; set; }

    /// <summary>
    /// S_a: the minimum sum of coil clearances EN 13906-1 requires at the
    /// largest working force, (0,0015·Dm²/d + 0,1·d)·n for a cold-coiled spring.
    /// </summary>
    public double MinimumCoilClearance { get; set; }
    /// <summary>Clearance actually left at the largest working deflection (mm).</summary>
    public double ProvidedCoilClearance { get; set; }

    // Calculated Values - Fatigue
    public double FatigueEnduranceFactor { get; set; } // 0.30 unpeened / 0.38 peened

    // Calculated Values - Natural Frequency
    public double NaturalFrequency { get; set; }      // fe (Hz)

    // Standard wire diameters (mm) - EN 10270-1
    public static readonly double[] StandardWireDiameters = 
    { 
        0.20, 0.22, 0.25, 0.28, 0.30, 0.32, 0.35, 0.40, 0.45, 0.50,
        0.55, 0.60, 0.65, 0.70, 0.80, 0.90, 1.00, 1.10, 1.20, 1.40,
        1.60, 1.80, 2.00, 2.20, 2.50, 2.80, 3.00, 3.20, 3.50, 4.00,
        4.50, 5.00, 5.50, 6.00, 6.50, 7.00, 8.00, 9.00, 10.00, 11.00,
        12.00, 13.00, 14.00, 15.00, 16.00, 17.00, 18.00, 19.00, 20.00
    };

    // Material properties
    public static readonly Dictionary<string, (double G, double Rm, string Name)> Materials = new()
    {
        // EN 10270-1 Patented spring steel wire
        { "SH", (81500, 1700, "EN 10270-1 SH - Patented Spring Steel") },
        { "SL", (81500, 1400, "EN 10270-1 SL - Patented Spring Steel") },
        { "SM", (81500, 1550, "EN 10270-1 SM - Patented Spring Steel") },
        { "DH", (81500, 1900, "EN 10270-1 DH - Patented Spring Steel") },
        
        // EN 10270-2 Oil hardened and tempered spring steel wire
        { "VDC", (79500, 1800, "EN 10270-2 VDC - Oil Hardened Steel") },
        { "VDSiCr", (79500, 1900, "EN 10270-2 VDSiCr - Oil Hardened SiCr") },
        
        // EN 10270-3 Stainless spring steel wire
        { "1.4310", (73000, 1600, "EN 10270-3 1.4310 - Stainless Steel") },
        { "1.4568", (75000, 1900, "EN 10270-3 1.4568 - Stainless Steel") },
        
        // Non-ferrous alloys
        { "CuSn8", (42000, 700, "EN 12166 CuSn8 - Phosphor Bronze") },
        { "CuBe2", (47000, 1200, "EN 12166 CuBe2 - Beryllium Copper") },
        
        // High temperature alloys
        { "Inconel", (77000, 1250, "Inconel X-750 - High Temperature") },
        { "Monel", (66000, 1050, "Monel K-500 - Corrosion Resistant") },
    };

    public void Calculate()
    {
        CalculateGeometry();
        CalculateSpringRate();
        CalculateStresses();
        CalculateSafety();
        CalculateBuckling();
        CalculateNaturalFrequency();
        CalculateFatigueEnduranceFactor();
    }

    private void CalculateGeometry()
    {
        // Spring index
        SpringIndex = MeanCoilDiameter / WireDiameter;

        // Diameters
        OuterDiameter = MeanCoilDiameter + WireDiameter;
        InnerDiameter = MeanCoilDiameter - WireDiameter;

        // Total coils, always re-derived from the CURRENT active coils and end type.
        //
        // This used to derive only when TotalCoils was still 0, which made it sticky:
        // the page reuses one engine instance, so after "Back to Input" a changed n or
        // end type kept the first calculation's nt - and with it a stale solid length,
        // max deflection, max force, wire length and mass, which then went into the
        // results and the PDF. There is no manual nt input, so nothing is overwritten
        // by deriving it unconditionally.
        TotalCoils = EndType switch
        {
            "Open" => ActiveCoils,
            "Closed" => ActiveCoils + 2,
            "ClosedGround" => ActiveCoils + 2,
            _ => ActiveCoils + 2
        };

        // Solid length. EN 13906-1 allows an extra half wire diameter for ends
        // that are NOT ground, because the end coil cannot lie perfectly flat:
        //   ground    L_c <= nt * d
        //   unground  L_c <= (nt + 1,5) * d
        // An unground closed end was being given (nt + 1) d, which is 0,5 d
        // optimistic on the available stroke.
        SolidLength = EndType switch
        {
            "Open" => (TotalCoils + 1.5) * WireDiameter,
            "Closed" => (TotalCoils + 1.5) * WireDiameter,
            "ClosedGround" => TotalCoils * WireDiameter,
            _ => TotalCoils * WireDiameter
        };

        // Coil pitch in the free state. Only a CLOSED end has two dead coils
        // taking up a wire diameter each; an open-coiled spring has none, so
        // subtracting 2d there understated the pitch.
        double deadLength = EndType == "Open" ? 0 : 2 * WireDiameter;
        CoilPitch = ActiveCoils > 0 ? (FreeLength - deadLength) / ActiveCoils : 0;

        // Wire length
        WireLength = Math.PI * MeanCoilDiameter * TotalCoils;

        // Spring mass (steel density ~7850 kg/m³)
        double wireVolume = Math.PI * Math.Pow(WireDiameter / 2, 2) * WireLength; // mm³
        SpringMass = wireVolume * 7.85e-6 * 1000; // grams
    }

    private void CalculateSpringRate()
    {
        // Spring rate from formula: R = (G * d^4) / (8 * Dm³ * n)
        SpringRateCalculated = (ShearModulus * Math.Pow(WireDiameter, 4)) / 
                               (8 * Math.Pow(MeanCoilDiameter, 3) * ActiveCoils);

        // Spring rate from two measured points, when they were actually measured.
        // The FORCES are what makes a pair of lengths a test point: the page
        // supplies Length1/Length2 so the buckling and clearance checks know the
        // real working stroke, and computes its own forces from the rate - with
        // Force1 = Force2 = 0 the subtraction below is 0 - 0 and the rate would
        // silently collapse to zero.
        if (Length1 > 0 && Length2 > 0 && Length1 != Length2 && (Force1 > 0 || Force2 > 0))
        {
            double s1 = FreeLength - Length1;
            double s2 = FreeLength - Length2;
            SpringRate = (Force2 - Force1) / (s2 - s1);
        }
        else
        {
            SpringRate = SpringRateCalculated;
        }

        // Maximum deflection and force
        MaxDeflection = FreeLength - SolidLength;
        MaxForce = SpringRate * MaxDeflection;

        // Stroke calculation
        if (Length1 > 0 && Length2 > 0)
        {
            double s1 = FreeLength - Length1;
            double s2 = FreeLength - Length2;
            StrokeLength = Math.Abs(s2 - s1);
        }

        // The largest deflection the user actually asked for. The buckling and
        // clearance checks belong at this point, not at the solid length - a
        // spring in service never reaches the solid length.
        MaxWorkingDeflection = 0;
        if (Length1 > 0) MaxWorkingDeflection = Math.Max(MaxWorkingDeflection, FreeLength - Length1);
        if (Length2 > 0) MaxWorkingDeflection = Math.Max(MaxWorkingDeflection, FreeLength - Length2);

        // S_a, EN 13906-1: the sum of the coil clearances that must remain at
        // the largest working force, so the spring never runs solid under load.
        // Checking only "clearance to solid > 0" accepted 1 mm as adequate where
        // the standard asks for 4,7 mm on the default spring.
        MinimumCoilClearance = WireDiameter > 0
            ? (0.0015 * MeanCoilDiameter * MeanCoilDiameter / WireDiameter + 0.1 * WireDiameter) * ActiveCoils
            : 0;
        ProvidedCoilClearance = MaxWorkingDeflection > 0
            ? MaxDeflection - MaxWorkingDeflection
            : MaxDeflection;
    }

    private void CalculateStresses()
    {
        // Stress correction factor, for curvature and direct shear.
        //
        // Two forms are in use and they differ by about 1 %:
        //   Wahl          k = (4w-1)/(4w-4) + 0,615/w        (1,184 at w = 8)
        //   Bergstraesser k = (w + 0,5)/(w - 0,75)           (1,172 at w = 8)
        // EN 13906-1 uses Bergstraesser. Both are reported so a reader comparing
        // against the standard can see which one a number came from; the stresses
        // below stay on Wahl, which is the more conservative of the two.
        double c = SpringIndex;
        WahlFactor = (4 * c - 1) / (4 * c - 4) + 0.615 / c;
        BergstrasserFactor = c > 0.75 ? (c + 0.5) / (c - 0.75) : WahlFactor;

        // Shear stress formula: τ = k * (8 * F * Dm) / (π * d³)
        double stressConstant = (8 * MeanCoilDiameter) / (Math.PI * Math.Pow(WireDiameter, 3));

        // Stress at F1
        ShearStress1 = WahlFactor * stressConstant * Force1;

        // Stress at F2
        ShearStress2 = WahlFactor * stressConstant * Force2;

        // Stress at max force
        ShearStressMax = WahlFactor * stressConstant * MaxForce;

        // Stress range (for fatigue)
        StressRange = Math.Abs(ShearStress2 - ShearStress1);
    }

    private void CalculateSafety()
    {
        // Allowable shear stress (typically 0.45-0.50 * Rm for static)
        if (AllowableShearStress <= 0)
        {
            AllowableShearStress = 0.45 * TensileStrength;
        }

        // Static safety factor at the OPERATING point (F2).
        // This is what a user reads as "is my spring safe in service".
        // It previously used ShearStressMax - the stress if the spring were
        // compressed all the way to solid length - which is a different (and much
        // more pessimistic) question and was easily mistaken for operating safety.
        SafetyFactorStatic = ShearStress2 > 0
            ? AllowableShearStress / ShearStress2
            : 999;

        // Separate check: safety if the spring is accidentally compressed solid.
        SafetyFactorSolid = ShearStressMax > 0
            ? AllowableShearStress / ShearStressMax
            : 999;
    }

    /// <summary>
    /// Seating factor ν for an end condition key. Legacy keys are kept
    /// resolvable because share links carry the string.
    ///
    /// ν is a BUCKLING LENGTH factor, exactly as in Euler's column: the
    /// buckling length is ν·L0, so a larger ν means a spring that buckles more
    /// easily. The value for a given seating follows the same analogy as a
    /// column, which is why ends held parallel against flat plates - no rotation
    /// at either end - give ν = 0,5, not 1,0. ν = 1 is the both-ends-hinged
    /// case.
    /// </summary>
    public static double SeatingFactor(string endCondition) => endCondition switch
    {
        // Both ends ground and resting on flat surfaces that stay parallel:
        // neither end can rotate, so this is the fixed-fixed case.
        "BothFlat" => 0.5,
        "BothFixed" => 0.5,            // legacy key, same physical case
        "OneFixedOneFlat" => 0.7,      // one end built in, the other hinged
        "BothHinged" => 1.0,           // ball-seated at both ends
        "OneFixedOneSliding" => 1.0,   // built in at one end, free to translate at the other
        "OneFixedOneFree" => 2.0,      // one end built in, the other entirely free
        _ => 0.5
    };

    private void CalculateBuckling()
    {
        // Slenderness ratio: λ = L0 / Dm
        SlendernessRatio = MeanCoilDiameter > 0 ? FreeLength / MeanCoilDiameter : 0;

        EndConditionFactor = SeatingFactor(EndCondition);

        // Effective slenderness is ν·λ, NOT λ/ν.
        //
        // This was inverted, and the consequence was as bad as it gets: the most
        // dangerous seating (one end free, ν = 2) came out as the LEAST slender
        // and was reported "No buckling - OK", while the most stable one
        // (ν = 0,5) was flagged as a risk. The default spring at s1 = 25 mm
        // buckles with one end free and does not with both ends guided; the page
        // said the opposite of both.
        EffectiveSlenderness = EndConditionFactor * SlendernessRatio;

        // EN 13906-1 buckling deflection:
        //
        //   s_K = L0 · (0,5/(1 − G/E)) · [ 1 − sqrt( 1 − ((1 − G/E)/(0,5 + G/E)) · (π·D/(ν·L0))² ) ]
        //
        // The spring cannot buckle at any deflection when the term under the
        // root is negative - the coil is too short and fat for the elastic
        // instability to exist.
        //
        // Anchors (default spring d 3, D 24, L0 80, n 8, G 81 500, E 206 000):
        //   ν = 1   → s_K = 24,3 mm
        //   ν = 2   → s_K =  5,2 mm
        //   ν = 0,7 → no buckling possible
        // The simplified constant-2,62 form this replaced gave 30,6 mm at ν = 1,
        // i.e. 26 % on the unsafe side even once the inversion was corrected.
        BucklingDeflection = 0;
        BucklingPossible = false;

        double gOverE = ElasticModulus > 0 ? ShearModulus / ElasticModulus : 0;
        double a = 1.0 - gOverE;
        double b = 0.5 + gOverE;

        if (a > 0 && b > 0 && FreeLength > 0 && MeanCoilDiameter > 0 && EndConditionFactor > 0)
        {
            double ratio = Math.PI * MeanCoilDiameter / (EndConditionFactor * FreeLength);
            double term = (a / b) * ratio * ratio;

            if (term < 1.0)
            {
                BucklingPossible = true;
                BucklingDeflection = FreeLength * (0.5 / a) * (1.0 - Math.Sqrt(1.0 - term));
                BucklingLimit = FreeLength > 0 ? BucklingDeflection / FreeLength : 1.0;

                // The spring is at risk once the working deflection reaches s_K.
                // Judged at the largest deflection actually asked for, falling
                // back to the solid length when no working position is entered.
                double workingDeflection = MaxWorkingDeflection > 0 ? MaxWorkingDeflection : MaxDeflection;
                BucklingRisk = workingDeflection >= BucklingDeflection;
            }
            else
            {
                // No buckling at any deflection.
                BucklingLimit = 1.0;
                BucklingRisk = false;
            }
        }
        else
        {
            BucklingLimit = 1.0;
            BucklingRisk = false;
        }
    }

    private void CalculateNaturalFrequency()
    {
        // Natural frequency (Hz)
        // fe = (d / (π * n * Dm²)) * √(G * 1000 / (8 * ρ))
        // Simplified: fe ≈ 3.56e5 * d / (n * Dm²) for steel springs

        double rho = 7850; // kg/m³
        NaturalFrequency = (WireDiameter / 1000) /
                          (Math.PI * ActiveCoils * Math.Pow(MeanCoilDiameter / 1000, 2)) *
                          Math.Sqrt(ShearModulus * 1e6 / (8 * rho));
    }

    private void CalculateFatigueEnduranceFactor()
    {
        // Fatigue endurance limit factor (relative to Rm) per EN 13906-1 / Bergsträsser
        // Unpeened spring steel:  τe ≈ 0.30 × Rm
        // Shot-peened spring steel: τe ≈ 0.38 × Rm  (+25-27% improvement)
        // References:
        //   - EN 13906-1:2013, Section 7.2 (Table 3 - τkh allowable values)
        //   - Wahl "Mechanical Springs", 2nd ed., Table 11-1
        FatigueEnduranceFactor = ShotPeened ? 0.38 : 0.30;
    }

    // Design helper: calculate required active coils for target spring rate
    public static double CalculateActiveCoils(double wireDia, double meanDia, double rate, double G)
    {
        return (G * Math.Pow(wireDia, 4)) / (8 * Math.Pow(meanDia, 3) * rate);
    }

    // Design helper: calculate wire diameter for target spring rate
    public static double CalculateWireDiameter(double meanDia, double rate, double n, double G)
    {
        return Math.Pow((8 * Math.Pow(meanDia, 3) * n * rate) / G, 0.25);
    }
}
