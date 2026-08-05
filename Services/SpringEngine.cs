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
    // page's <select> binds, so they must match CalculateBuckling's switch exactly.
    // "BothFlat"        = both ends seated on flat surfaces  → ν = 1.0 (most common)
    // "OneFixedOneFlat" = one end fixed, one end flat        → ν = 0.7
    // "BothFixed"       = both ends in guide sleeves         → ν = 0.5
    // "OneFixedOneFree" = one end clamped, other free        → ν = 2.0
    public string EndCondition { get; set; } = "BothFlat";

    // Input Parameters - Material
    public double ShearModulus { get; set; } = 81500; // G (MPa) - default for spring steel
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
    public double WahlFactor { get; set; }            // k (stress correction)
    public double ShearStress1 { get; set; }          // τ1 at F1 (MPa)
    public double ShearStress2 { get; set; }          // τ2 at F2 (MPa)
    public double ShearStressMax { get; set; }        // τc at Fc (MPa)
    public double StressRange { get; set; }           // τkh = τ2 - τ1 (MPa)

    // Calculated Values - Safety & Buckling
    public double SafetyFactorStatic { get; set; }    // νs at the operating point F2
    public double SafetyFactorSolid { get; set; }     // νs if compressed to solid length
    public double SlendernessRatio { get; set; }      // λ = L0/Dm
    public double EndConditionFactor { get; set; }    // ν (seating / buckling length factor)
    public double EffectiveSlenderness { get; set; }  // λ/ν (effective slenderness for buckling check)
    public double BucklingLimit { get; set; }         // Critical deflection ratio
    public bool BucklingRisk { get; set; }

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

        // Solid length
        SolidLength = EndType switch
        {
            "Open" => (TotalCoils + 1) * WireDiameter,
            "Closed" => (TotalCoils + 1) * WireDiameter,
            "ClosedGround" => TotalCoils * WireDiameter,
            _ => TotalCoils * WireDiameter
        };

        // Coil pitch (free state)
        CoilPitch = (FreeLength - 2 * WireDiameter) / ActiveCoils;

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

        // Spring rate from test points (if both provided)
        if (Length1 > 0 && Length2 > 0 && Length1 != Length2)
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
    }

    private void CalculateStresses()
    {
        // Wahl correction factor (accounts for curvature and direct shear)
        double c = SpringIndex;
        WahlFactor = (4 * c - 1) / (4 * c - 4) + 0.615 / c;

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

    private void CalculateBuckling()
    {
        // Slenderness ratio: λ = L0 / Dm
        SlendernessRatio = FreeLength / MeanCoilDiameter;

        // End condition factor v (EN 13906-1 Table 1)
        //   BothFlat        : both ends seated on flat surfaces (most common)  → v = 1.0
        //   OneFixedOneFlat : one end fixed in pocket, other on flat surface   → v = 0.7
        //   BothFixed       : both ends in guide pockets / guided rod          → v = 0.5
        //   OneFixedOneFree : one end clamped, other completely free (cantilever) → v = 2.0
        EndConditionFactor = EndCondition switch
        {
            "OneFixedOneFlat" => 0.7,
            "BothFixed"       => 0.5,
            "OneFixedOneFree" => 2.0,
            _                 => 1.0   // BothFlat (default)
        };

        // Effective slenderness ratio: λeff = λ / v
        EffectiveSlenderness = SlendernessRatio / EndConditionFactor;

        // EN 13906-1 buckling criterion:
        // A spring buckles when the relative deflection (s/L0) exceeds the critical value:
        //   (s/L0)_crit = 1 - sqrt(1 - (D_crit / λeff)²)
        // where D_crit ≈ 2.62 for steel springs (Euler–Haringx model)
        // Practical rule: buckling risk if λeff > 2.62 AND actual deflection > critical value

        const double D_crit = 2.62;

        if (EffectiveSlenderness > D_crit)
        {
            // Critical relative deflection at which buckling occurs
            double term = D_crit / EffectiveSlenderness;
            double criticalDeflectionRatio = 1.0 - Math.Sqrt(1.0 - term * term);
            BucklingLimit = criticalDeflectionRatio;

            // Current maximum relative deflection
            double actualDeflectionRatio = MaxDeflection / FreeLength;
            BucklingRisk = actualDeflectionRatio >= criticalDeflectionRatio;
        }
        else
        {
            // λeff ≤ 2.62 → no buckling possible for any deflection
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
