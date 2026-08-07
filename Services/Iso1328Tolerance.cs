namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Flank tolerances for cylindrical involute gears according to
/// ISO 1328-1:2013 (as adopted in ANSI/AGMA ISO 1328-1-B14), Clause 5.
///
/// Formula numbers in the comments refer to that standard. Only the formulae and
/// the numeric limits are implemented; no text of the standard is reproduced.
///
/// IMPORTANT - EDITION NOTE
/// This is the 2013 edition, which defines ELEVEN flank tolerance classes numbered
/// 1 to 11 in order of increasing tolerance. The earlier ISO 1328-1:1995 edition used
/// accuracy grades 0 to 12 and different tolerance formulae. ISO 6336-1:2006 normatively
/// references the 1995 edition, so when these tolerances are fed into a ISO 6336 dynamic
/// factor calculation the class/grade numbering is NOT interchangeable - see
/// <see cref="EditionMismatchNote"/>.
/// </summary>
public static class Iso1328Tolerance
{
    public const string EditionMismatchNote =
        "Tolerances are calculated per ISO 1328-1:2013 (classes 1-11). ISO 6336-1:2006 references " +
        "ISO 1328-1:1995 (grades 0-12); the numbering and the formulae differ between editions, so " +
        "values used in a ISO 6336 dynamic factor calculation should be confirmed against the edition " +
        "specified for the gear.";

    /// <summary>Full set of flank tolerances for one gear, in micrometres.</summary>
    public class Tolerances
    {
        public double SinglePitch { get; set; }        // f_pT   (5)
        public double CumulativePitch { get; set; }    // F_pT   (6)
        public double ProfileSlope { get; set; }       // f_HalphaT (7)
        public double ProfileForm { get; set; }        // f_falphaT (8)
        public double ProfileTotal { get; set; }       // F_alphaT  (9)
        public double HelixSlope { get; set; }         // f_HbetaT  (10)
        public double HelixForm { get; set; }          // f_fbetaT  (11)
        public double HelixTotal { get; set; }         // F_betaT   (12)

        public int ToleranceClass { get; set; }
        public bool InScope { get; set; }
        public string? Warning { get; set; }
    }

    /// <summary>
    /// Calculates the flank tolerances of one gear.
    /// </summary>
    /// <param name="toleranceClass">Flank tolerance class A (1 = tightest ... 11 = loosest)</param>
    /// <param name="d">Reference diameter (mm)</param>
    /// <param name="mn">Normal module (mm)</param>
    /// <param name="b">Face width, axial (mm)</param>
    /// <param name="z">Number of teeth</param>
    /// <param name="beta">Helix angle (degrees)</param>
    public static Tolerances Calculate(int toleranceClass, double d, double mn, double b,
                                       double z = 0, double beta = 0)
    {
        var t = new Tolerances { ToleranceClass = toleranceClass };

        // Scope of ISO 1328-1:2013, Clause 1. The standard states that the formulae
        // shall NOT be extrapolated beyond these limits.
        var outOfScope = new List<string>();
        if (z > 0 && (z < 5 || z > 1000)) outOfScope.Add($"z = {z:F0} (5-1000)");
        if (d < 5 || d > 15000) outOfScope.Add($"d = {d:F1} mm (5-15000 mm)");
        if (mn < 0.5 || mn > 70) outOfScope.Add($"mn = {mn:F2} mm (0.5-70 mm)");
        if (b < 4 || b > 1200) outOfScope.Add($"b = {b:F1} mm (4-1200 mm)");
        if (Math.Abs(beta) > 45) outOfScope.Add($"beta = {beta:F1} deg (<= 45 deg)");
        if (toleranceClass < 1 || toleranceClass > 11) outOfScope.Add($"class {toleranceClass} (1-11)");

        t.InScope = outOfScope.Count == 0;
        if (!t.InScope)
        {
            t.Warning = "Outside the ISO 1328-1 range of application: " + string.Join(", ", outOfScope)
                      + ". The standard requires such tolerances to be agreed between manufacturer and purchaser.";
        }

        // Step factor: values for class A are the class-5 value multiplied by sqrt(2)^(A-5)  (5.2.2)
        double step = Math.Pow(Math.Sqrt(2.0), toleranceClass - 5);

        // Unrounded class-5 base values
        double fpT   = (0.001 * d + 0.4 * mn + 5) * step;                              // (5)
        double FpT   = (0.002 * d + 0.55 * Math.Sqrt(d) + 0.7 * mn + 12) * step;       // (6)
        double fHaT  = (0.4 * mn + 0.001 * d + 4) * step;                              // (7)
        double ffaT  = (0.55 * mn + 5) * step;                                         // (8)
        double FaT   = Math.Sqrt(fHaT * fHaT + ffaT * ffaT);                           // (9) uses UNROUNDED values
        double fHbT  = (0.05 * Math.Sqrt(d) + 0.35 * Math.Sqrt(b) + 4) * step;         // (10)
        double ffbT  = (0.07 * Math.Sqrt(d) + 0.45 * Math.Sqrt(b) + 4) * step;         // (11)
        double FbT   = Math.Sqrt(fHbT * fHbT + ffbT * ffbT);                           // (12) uses UNROUNDED values

        t.SinglePitch     = Round(fpT);
        t.CumulativePitch = Round(FpT);
        t.ProfileSlope    = Round(fHaT);
        t.ProfileForm     = Round(ffaT);
        t.ProfileTotal    = Round(FaT);
        t.HelixSlope      = Round(fHbT);
        t.HelixForm       = Round(ffbT);
        t.HelixTotal      = Round(FbT);

        return t;
    }

    /// <summary>
    /// Rounding rules of ISO 1328-1:2013, 5.2.3 (values in micrometres):
    ///   &gt; 10    -> nearest integer
    ///   5 to 10  -> nearest 0,5
    ///   &lt; 5    -> nearest 0,1
    /// </summary>
    public static double Round(double valueMicrometres)
    {
        double v = valueMicrometres;
        if (v > 10.0) return Math.Round(v, MidpointRounding.AwayFromZero);
        if (v >= 5.0) return Math.Round(v * 2.0, MidpointRounding.AwayFromZero) / 2.0;
        return Math.Round(v, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>Unrounded single pitch tolerance - Formula (5) without the rounding rules.</summary>
    public static double SinglePitchUnrounded(int toleranceClass, double d, double mn)
        => (0.001 * d + 0.4 * mn + 5) * Math.Pow(Math.Sqrt(2.0), toleranceClass - 5);

    /// <summary>Unrounded profile form tolerance - Formula (8) without the rounding rules.</summary>
    public static double ProfileFormUnrounded(int toleranceClass, double mn)
        => (0.55 * mn + 5) * Math.Pow(Math.Sqrt(2.0), toleranceClass - 5);

    /// <summary>Unrounded helix slope tolerance - Formula (10) without the rounding rules.</summary>
    public static double HelixSlopeUnrounded(int toleranceClass, double d, double b)
        => (0.05 * Math.Sqrt(d) + 0.35 * Math.Sqrt(b) + 4) * Math.Pow(Math.Sqrt(2.0), toleranceClass - 5);
}
