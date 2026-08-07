namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Material quality grade per ISO 6336-5.
/// ML = modest demands, MQ = normal demands achievable by experienced manufacturers,
/// ME = high demands requiring a high level of process control.
/// </summary>
public enum GearQualityGrade { ML, MQ, ME }

/// <summary>Stress type for the Table 1 lookup.</summary>
public enum GearStressType { Contact, Bending }

/// <summary>Material / heat treatment groups of ISO 6336-5:2016 Table 1.</summary>
public enum GearMaterialType
{
    NormalizedLowCarbonSteel,       // St
    NormalizedCastSteel,            // St (cast)
    BlackMalleableCastIron,         // GTS (perl.)
    NodularCastIron,                // GGG
    GreyCastIron,                   // GG
    ThroughHardenedCarbonSteel,     // V
    ThroughHardenedAlloySteel,      // V
    ThroughHardenedCastCarbonSteel, // V (cast)
    ThroughHardenedCastAlloySteel,  // V (cast)
    CaseHardened,                   // Eh
    FlameOrInductionHardened,       // IF
    NitridedNitridingSteel,         // NT (nitr.)
    NitridedThroughHardeningSteel,  // NV (nitr.)
    Nitrocarburized                 // NV (nitrocar.)
}

/// <summary>
/// Allowable stress numbers sigma_Hlim (contact) and sigma_Flim (bending) according to
/// ISO 6336-5:2016, Clause 5.5 / Table 1:
///
///     sigma_lim = A * x + B      with x = surface hardness (HBW or HV per Table 1)
///
/// Only the tabulated constants are reproduced here (numeric data), not the text of
/// the standard. Row numbers in the comments refer to ISO 6336-5:2016 Table 1.
/// </summary>
public static class Iso6336Material
{
    /// <summary>One Table 1 row: sigma_lim = A*x + B, valid for hardness in [HMin, HMax].</summary>
    public record Entry(double A, double B, string HardnessScale, double HMin, double HMax, int Row);

    public class AllowableStress
    {
        public double SigmaLim { get; set; }     // MPa
        public double Hardness { get; set; }     // as supplied
        public string HardnessScale { get; set; } = "";
        public double HMin { get; set; }
        public double HMax { get; set; }
        public int TableRow { get; set; }
        public bool InRange { get; set; }
        public string? Warning { get; set; }
    }

    // ISO 6336-5:2016 Table 1 - constants A and B, hardness scale and validity range.
    // Key: (material type, stress type, quality grade)
    private static readonly Dictionary<(GearMaterialType, GearStressType, GearQualityGrade), Entry> Table1 = new()
    {
        // --- Normalized low carbon steels / cast steels (rows 1-8) ---
        [(GearMaterialType.NormalizedLowCarbonSteel, GearStressType.Contact, GearQualityGrade.ML)] = new(1.000, 190, "HBW", 110, 210, 1),
        [(GearMaterialType.NormalizedLowCarbonSteel, GearStressType.Contact, GearQualityGrade.MQ)] = new(1.000, 190, "HBW", 110, 210, 1),
        [(GearMaterialType.NormalizedLowCarbonSteel, GearStressType.Contact, GearQualityGrade.ME)] = new(1.520, 250, "HBW", 110, 210, 2),
        [(GearMaterialType.NormalizedCastSteel,      GearStressType.Contact, GearQualityGrade.ML)] = new(0.986, 131, "HBW", 140, 210, 3),
        [(GearMaterialType.NormalizedCastSteel,      GearStressType.Contact, GearQualityGrade.MQ)] = new(0.986, 131, "HBW", 140, 210, 3),
        [(GearMaterialType.NormalizedCastSteel,      GearStressType.Contact, GearQualityGrade.ME)] = new(1.143, 237, "HBW", 140, 210, 4),
        [(GearMaterialType.NormalizedLowCarbonSteel, GearStressType.Bending, GearQualityGrade.ML)] = new(0.455,  69, "HBW", 110, 210, 5),
        [(GearMaterialType.NormalizedLowCarbonSteel, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.455,  69, "HBW", 110, 210, 5),
        [(GearMaterialType.NormalizedLowCarbonSteel, GearStressType.Bending, GearQualityGrade.ME)] = new(0.386, 147, "HBW", 110, 210, 6),
        [(GearMaterialType.NormalizedCastSteel,      GearStressType.Bending, GearQualityGrade.ML)] = new(0.313,  62, "HBW", 140, 210, 7),
        [(GearMaterialType.NormalizedCastSteel,      GearStressType.Bending, GearQualityGrade.MQ)] = new(0.313,  62, "HBW", 140, 210, 7),
        [(GearMaterialType.NormalizedCastSteel,      GearStressType.Bending, GearQualityGrade.ME)] = new(0.254, 137, "HBW", 140, 210, 8),

        // --- Cast iron materials (rows 9-20) ---
        [(GearMaterialType.BlackMalleableCastIron, GearStressType.Contact, GearQualityGrade.ML)] = new(1.371, 143, "HBW", 135, 250, 9),
        [(GearMaterialType.BlackMalleableCastIron, GearStressType.Contact, GearQualityGrade.MQ)] = new(1.371, 143, "HBW", 135, 250, 9),
        [(GearMaterialType.BlackMalleableCastIron, GearStressType.Contact, GearQualityGrade.ME)] = new(1.333, 267, "HBW", 175, 250, 10),
        [(GearMaterialType.NodularCastIron,        GearStressType.Contact, GearQualityGrade.ML)] = new(1.434, 211, "HBW", 175, 300, 11),
        [(GearMaterialType.NodularCastIron,        GearStressType.Contact, GearQualityGrade.MQ)] = new(1.434, 211, "HBW", 175, 300, 11),
        [(GearMaterialType.NodularCastIron,        GearStressType.Contact, GearQualityGrade.ME)] = new(1.500, 250, "HBW", 200, 300, 12),
        [(GearMaterialType.GreyCastIron,           GearStressType.Contact, GearQualityGrade.ML)] = new(1.033, 132, "HBW", 150, 240, 13),
        [(GearMaterialType.GreyCastIron,           GearStressType.Contact, GearQualityGrade.MQ)] = new(1.033, 132, "HBW", 150, 240, 13),
        [(GearMaterialType.GreyCastIron,           GearStressType.Contact, GearQualityGrade.ME)] = new(1.465, 122, "HBW", 175, 275, 14),
        [(GearMaterialType.BlackMalleableCastIron, GearStressType.Bending, GearQualityGrade.ML)] = new(0.345,  77, "HBW", 135, 250, 15),
        [(GearMaterialType.BlackMalleableCastIron, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.345,  77, "HBW", 135, 250, 15),
        [(GearMaterialType.BlackMalleableCastIron, GearStressType.Bending, GearQualityGrade.ME)] = new(0.403, 128, "HBW", 175, 250, 16),
        [(GearMaterialType.NodularCastIron,        GearStressType.Bending, GearQualityGrade.ML)] = new(0.350, 119, "HBW", 175, 300, 17),
        [(GearMaterialType.NodularCastIron,        GearStressType.Bending, GearQualityGrade.MQ)] = new(0.350, 119, "HBW", 175, 300, 17),
        [(GearMaterialType.NodularCastIron,        GearStressType.Bending, GearQualityGrade.ME)] = new(0.380, 134, "HBW", 200, 300, 18),
        [(GearMaterialType.GreyCastIron,           GearStressType.Bending, GearQualityGrade.ML)] = new(0.256,   8, "HBW", 150, 240, 19),
        [(GearMaterialType.GreyCastIron,           GearStressType.Bending, GearQualityGrade.MQ)] = new(0.256,   8, "HBW", 150, 240, 19),
        [(GearMaterialType.GreyCastIron,           GearStressType.Bending, GearQualityGrade.ME)] = new(0.200,  53, "HBW", 175, 275, 20),

        // --- Through hardened wrought steels (rows 21-32) ---
        [(GearMaterialType.ThroughHardenedCarbonSteel, GearStressType.Contact, GearQualityGrade.ML)] = new(0.963, 283, "HV", 135, 210, 21),
        [(GearMaterialType.ThroughHardenedCarbonSteel, GearStressType.Contact, GearQualityGrade.MQ)] = new(0.925, 360, "HV", 135, 210, 22),
        [(GearMaterialType.ThroughHardenedCarbonSteel, GearStressType.Contact, GearQualityGrade.ME)] = new(0.838, 432, "HV", 135, 210, 23),
        [(GearMaterialType.ThroughHardenedAlloySteel,  GearStressType.Contact, GearQualityGrade.ML)] = new(1.313, 188, "HV", 200, 360, 24),
        [(GearMaterialType.ThroughHardenedAlloySteel,  GearStressType.Contact, GearQualityGrade.MQ)] = new(1.313, 373, "HV", 200, 360, 25),
        [(GearMaterialType.ThroughHardenedAlloySteel,  GearStressType.Contact, GearQualityGrade.ME)] = new(2.213, 260, "HV", 200, 390, 26),
        [(GearMaterialType.ThroughHardenedCarbonSteel, GearStressType.Bending, GearQualityGrade.ML)] = new(0.250, 108, "HV", 115, 215, 27),
        [(GearMaterialType.ThroughHardenedCarbonSteel, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.240, 163, "HV", 115, 215, 28),
        [(GearMaterialType.ThroughHardenedCarbonSteel, GearStressType.Bending, GearQualityGrade.ME)] = new(0.283, 202, "HV", 115, 215, 29),
        [(GearMaterialType.ThroughHardenedAlloySteel,  GearStressType.Bending, GearQualityGrade.ML)] = new(0.423, 104, "HV", 200, 360, 30),
        [(GearMaterialType.ThroughHardenedAlloySteel,  GearStressType.Bending, GearQualityGrade.MQ)] = new(0.425, 187, "HV", 200, 360, 31),
        [(GearMaterialType.ThroughHardenedAlloySteel,  GearStressType.Bending, GearQualityGrade.ME)] = new(0.358, 231, "HV", 200, 390, 32),

        // --- Through hardened cast steels (rows 33-40) ---
        [(GearMaterialType.ThroughHardenedCastCarbonSteel, GearStressType.Contact, GearQualityGrade.ML)] = new(0.831, 300, "HV", 130, 215, 33),
        [(GearMaterialType.ThroughHardenedCastCarbonSteel, GearStressType.Contact, GearQualityGrade.MQ)] = new(0.831, 300, "HV", 130, 215, 33),
        [(GearMaterialType.ThroughHardenedCastCarbonSteel, GearStressType.Contact, GearQualityGrade.ME)] = new(0.951, 345, "HV", 130, 215, 34),
        [(GearMaterialType.ThroughHardenedCastAlloySteel,  GearStressType.Contact, GearQualityGrade.ML)] = new(1.276, 298, "HV", 200, 360, 35),
        [(GearMaterialType.ThroughHardenedCastAlloySteel,  GearStressType.Contact, GearQualityGrade.MQ)] = new(1.276, 298, "HV", 200, 360, 35),
        [(GearMaterialType.ThroughHardenedCastAlloySteel,  GearStressType.Contact, GearQualityGrade.ME)] = new(1.350, 356, "HV", 200, 360, 36),
        [(GearMaterialType.ThroughHardenedCastCarbonSteel, GearStressType.Bending, GearQualityGrade.ML)] = new(0.224, 117, "HV", 130, 215, 37),
        [(GearMaterialType.ThroughHardenedCastCarbonSteel, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.224, 117, "HV", 130, 215, 37),
        [(GearMaterialType.ThroughHardenedCastCarbonSteel, GearStressType.Bending, GearQualityGrade.ME)] = new(0.286, 167, "HV", 130, 215, 38),
        [(GearMaterialType.ThroughHardenedCastAlloySteel,  GearStressType.Bending, GearQualityGrade.ML)] = new(0.364, 161, "HV", 200, 360, 39),
        [(GearMaterialType.ThroughHardenedCastAlloySteel,  GearStressType.Bending, GearQualityGrade.MQ)] = new(0.364, 161, "HV", 200, 360, 39),
        [(GearMaterialType.ThroughHardenedCastAlloySteel,  GearStressType.Bending, GearQualityGrade.ME)] = new(0.356, 186, "HV", 200, 360, 40),

        // --- Case hardened wrought steels (rows 41-48) ---
        // A = 0: sigma_lim is independent of surface hardness within the valid range.
        // Bending MQ has additional core-hardness variants in Table 1 (rows 46: 461 for
        // "W 25 HRC upper", row 47: 500 for "W 30 HRC"); the conservative lower value
        // of row 45 is used here.
        [(GearMaterialType.CaseHardened, GearStressType.Contact, GearQualityGrade.ML)] = new(0.000, 1300, "HV", 600, 800, 41),
        [(GearMaterialType.CaseHardened, GearStressType.Contact, GearQualityGrade.MQ)] = new(0.000, 1500, "HV", 660, 800, 42),
        [(GearMaterialType.CaseHardened, GearStressType.Contact, GearQualityGrade.ME)] = new(0.000, 1650, "HV", 660, 800, 43),
        [(GearMaterialType.CaseHardened, GearStressType.Bending, GearQualityGrade.ML)] = new(0.000,  312, "HV", 600, 800, 44),
        [(GearMaterialType.CaseHardened, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.000,  425, "HV", 660, 800, 45),
        [(GearMaterialType.CaseHardened, GearStressType.Bending, GearQualityGrade.ME)] = new(0.000,  525, "HV", 660, 800, 48),

        // --- Flame or induction hardened wrought and cast steels (rows 49-55) ---
        [(GearMaterialType.FlameOrInductionHardened, GearStressType.Contact, GearQualityGrade.ML)] = new(0.740,  602, "HV", 485, 615, 49),
        [(GearMaterialType.FlameOrInductionHardened, GearStressType.Contact, GearQualityGrade.MQ)] = new(0.541,  882, "HV", 500, 615, 50),
        [(GearMaterialType.FlameOrInductionHardened, GearStressType.Contact, GearQualityGrade.ME)] = new(0.505, 1013, "HV", 500, 615, 51),
        [(GearMaterialType.FlameOrInductionHardened, GearStressType.Bending, GearQualityGrade.ML)] = new(0.305,   76, "HV", 485, 615, 52),
        [(GearMaterialType.FlameOrInductionHardened, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.138,  290, "HV", 500, 570, 53),
        [(GearMaterialType.FlameOrInductionHardened, GearStressType.Bending, GearQualityGrade.ME)] = new(0.271,  237, "HV", 500, 615, 55),

        // --- Nitrided steels (rows 56-67) ---
        [(GearMaterialType.NitridedNitridingSteel,       GearStressType.Contact, GearQualityGrade.ML)] = new(0.000, 1125, "HV", 650, 900, 56),
        [(GearMaterialType.NitridedNitridingSteel,       GearStressType.Contact, GearQualityGrade.MQ)] = new(0.000, 1250, "HV", 650, 900, 57),
        [(GearMaterialType.NitridedNitridingSteel,       GearStressType.Contact, GearQualityGrade.ME)] = new(0.000, 1450, "HV", 650, 900, 58),
        [(GearMaterialType.NitridedThroughHardeningSteel, GearStressType.Contact, GearQualityGrade.ML)] = new(0.000,  788, "HV", 450, 650, 59),
        [(GearMaterialType.NitridedThroughHardeningSteel, GearStressType.Contact, GearQualityGrade.MQ)] = new(0.000,  998, "HV", 450, 650, 60),
        [(GearMaterialType.NitridedThroughHardeningSteel, GearStressType.Contact, GearQualityGrade.ME)] = new(0.000, 1217, "HV", 450, 650, 61),
        [(GearMaterialType.NitridedNitridingSteel,       GearStressType.Bending, GearQualityGrade.ML)] = new(0.000,  270, "HV", 650, 900, 62),
        [(GearMaterialType.NitridedNitridingSteel,       GearStressType.Bending, GearQualityGrade.MQ)] = new(0.000,  420, "HV", 650, 900, 63),
        [(GearMaterialType.NitridedNitridingSteel,       GearStressType.Bending, GearQualityGrade.ME)] = new(0.000,  468, "HV", 650, 900, 64),
        [(GearMaterialType.NitridedThroughHardeningSteel, GearStressType.Bending, GearQualityGrade.ML)] = new(0.000,  258, "HV", 450, 650, 65),
        [(GearMaterialType.NitridedThroughHardeningSteel, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.000,  363, "HV", 450, 650, 66),
        [(GearMaterialType.NitridedThroughHardeningSteel, GearStressType.Bending, GearQualityGrade.ME)] = new(0.000,  432, "HV", 450, 650, 67),

        // --- Wrought steels, nitrocarburized (rows 68-73) ---
        [(GearMaterialType.Nitrocarburized, GearStressType.Contact, GearQualityGrade.ML)] = new(0.000, 650, "HV", 300, 650, 68),
        [(GearMaterialType.Nitrocarburized, GearStressType.Contact, GearQualityGrade.MQ)] = new(1.167, 425, "HV", 300, 450, 69),
        [(GearMaterialType.Nitrocarburized, GearStressType.Contact, GearQualityGrade.ME)] = new(1.167, 425, "HV", 300, 450, 69),
        [(GearMaterialType.Nitrocarburized, GearStressType.Bending, GearQualityGrade.ML)] = new(0.000, 224, "HV", 300, 650, 71),
        [(GearMaterialType.Nitrocarburized, GearStressType.Bending, GearQualityGrade.MQ)] = new(0.653,  94, "HV", 300, 450, 72),
        [(GearMaterialType.Nitrocarburized, GearStressType.Bending, GearQualityGrade.ME)] = new(0.653,  94, "HV", 300, 450, 72),
    };

    /// <summary>
    /// Allowable stress number from ISO 6336-5 Table 1: sigma_lim = A*x + B.
    /// </summary>
    /// <param name="type">Material / heat treatment group</param>
    /// <param name="stress">Contact (sigma_Hlim) or Bending (sigma_Flim)</param>
    /// <param name="quality">Material quality grade ML / MQ / ME</param>
    /// <param name="surfaceHardness">Surface hardness on the finished functional surface (HBW or HV per Table 1)</param>
    public static AllowableStress Get(GearMaterialType type, GearStressType stress,
                                      GearQualityGrade quality, double surfaceHardness)
    {
        var result = new AllowableStress { Hardness = surfaceHardness };

        if (!Table1.TryGetValue((type, stress, quality), out var e))
        {
            result.Warning = "No ISO 6336-5 Table 1 entry for this material / quality combination.";
            return result;
        }

        result.HardnessScale = e.HardnessScale;
        result.HMin = e.HMin;
        result.HMax = e.HMax;
        result.TableRow = e.Row;
        result.InRange = surfaceHardness >= e.HMin && surfaceHardness <= e.HMax;

        // ISO 6336-5 Clause 5.5, Eq. (2)
        result.SigmaLim = e.A * surfaceHardness + e.B;

        if (!result.InRange)
        {
            // Outside the tabulated range the standard requires agreement between
            // manufacturer and purchaser; clamp so the extrapolation is not silently used.
            double clamped = Math.Max(e.HMin, Math.Min(e.HMax, surfaceHardness));
            result.SigmaLim = e.A * clamped + e.B;
            result.Warning = $"Surface hardness {surfaceHardness:F0} {e.HardnessScale} is outside the ISO 6336-5 Table 1 range " +
                             $"({e.HMin:F0}-{e.HMax:F0} {e.HardnessScale}) for this material; the value was evaluated at the range limit.";
        }

        return result;
    }

    /// <summary>Human-readable name of a material group.</summary>
    public static string DisplayName(GearMaterialType t) => t switch
    {
        GearMaterialType.NormalizedLowCarbonSteel => "Normalized low carbon steel (St)",
        GearMaterialType.NormalizedCastSteel => "Normalized cast steel (St cast)",
        GearMaterialType.BlackMalleableCastIron => "Black malleable cast iron (GTS)",
        GearMaterialType.NodularCastIron => "Nodular cast iron (GGG)",
        GearMaterialType.GreyCastIron => "Grey cast iron (GG)",
        GearMaterialType.ThroughHardenedCarbonSteel => "Through hardened carbon steel (V)",
        GearMaterialType.ThroughHardenedAlloySteel => "Through hardened alloy steel (V)",
        GearMaterialType.ThroughHardenedCastCarbonSteel => "Through hardened cast carbon steel (V cast)",
        GearMaterialType.ThroughHardenedCastAlloySteel => "Through hardened cast alloy steel (V cast)",
        GearMaterialType.CaseHardened => "Case hardened wrought steel (Eh)",
        GearMaterialType.FlameOrInductionHardened => "Flame or induction hardened (IF)",
        GearMaterialType.NitridedNitridingSteel => "Nitrided nitriding steel (NT)",
        GearMaterialType.NitridedThroughHardeningSteel => "Nitrided through hardening steel (NV)",
        GearMaterialType.Nitrocarburized => "Nitrocarburized through hardening steel (NV)",
        _ => t.ToString()
    };
}
