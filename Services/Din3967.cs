using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// DIN 3967 (August 1978) — tooth thickness allowances and tooth thickness tolerances.
///
/// Two tables, both keyed on the reference diameter d:
///   Table 1  upper allowance A_sne, by allowance series a … h (all negative, h = 0)
///   Table 2  tooth thickness tolerance T_sn, by tolerance series 21 … 30
///
/// The lower allowance is not tabulated; Clause 3.2 defines it as
///     A_sni = A_sne − T_sn
/// (both allowances are always negative, so the tolerance is deducted from the upper one).
///
/// A gear's tolerance zone is written as the tolerance series number followed by the
/// allowance series letter — Clause 3.4's example is "27cd", which at d = 100 mm gives
/// A_sne = −70 µm and A_sni = −170 µm. That designation is the anchor for this file: if a
/// value here is ever changed, check it still reproduces.
///
/// Clause 3.3: the preferred tolerance series are 24 to 27. It also warns that T_sn must be
/// at least twice the permissible tooth thickness fluctuation R_s of DIN 3962 Part 1 — that
/// check needs data this module does not hold, so it is surfaced as guidance, not enforced.
///
/// Note the scope limit that matters in practice: backlash cannot be obtained by simply
/// adding allowances. Clause A.1.2 is explicit that backlash-modifying effects (temperature,
/// housing tolerance, bore non-parallelism, tooth deviations, elasticity) sit between the
/// two, and Appendix A is a whole calculation for it. This class only supplies the
/// allowances; the resulting theoretical backlash is what the engine reports.
/// </summary>
public static class Din3967
{
    // Upper bounds (inclusive) of the reference diameter ranges. Both tables share them.
    // The first row of the standard is "– up to 10", so d must be greater than 0.
    private static readonly double[] DiameterRanges =
        { 10, 50, 125, 280, 560, 1000, 1600, 2500, 4000, 6300, 10000 };

    /// <summary>Allowance series of Table 1, coarsest (a) to zero (h).</summary>
    public static readonly string[] AllowanceSeries =
        { "a", "ab", "b", "bc", "c", "cd", "d", "e", "f", "g", "h" };

    /// <summary>Tolerance series of Table 2. 24 to 27 are the preferred ones (Clause 3.3).</summary>
    public static readonly int[] ToleranceSeries = { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };

    // Table 1. Upper tooth thickness allowances A_sne in µm.
    // Rows follow DiameterRanges, columns follow AllowanceSeries.
    private static readonly int[,] UpperAllowances =
    {
        //   a     ab     b     bc     c     cd     d     e     f     g    h
        {  -100,  -85,  -70,  -58,  -48,  -40,  -33,  -22,  -10,   -5,   0 },  // …10
        {  -135, -110,  -95,  -75,  -65,  -54,  -44,  -30,  -14,   -7,   0 },  // 10…50
        {  -180, -150, -125, -105,  -85,  -70,  -60,  -40,  -19,   -9,   0 },  // 50…125
        {  -250, -200, -170, -140, -115,  -95,  -80,  -56,  -26,  -12,   0 },  // 125…280
        {  -330, -280, -230, -190, -155, -130, -110,  -75,  -35,  -17,   0 },  // 280…560
        {  -450, -370, -310, -260, -210, -175, -145, -100,  -48,  -22,   0 },  // 560…1000
        {  -600, -500, -420, -340, -290, -240, -200, -135,  -64,  -30,   0 },  // 1000…1600
        {  -820, -680, -560, -460, -390, -320, -270, -180,  -85,  -41,   0 },  // 1600…2500
        { -1100, -920, -760, -620, -520, -430, -360, -250, -115,  -56,   0 },  // 2500…4000
        { -1500,-1250,-1020, -840, -700, -580, -480, -330, -155,  -75,   0 },  // 4000…6300
        { -2000,-1650,-1350,-1150, -940, -780, -640, -450, -210, -100,   0 }   // 6300…10000
    };

    // Table 2. Tooth thickness tolerances T_sn in µm.
    // Rows follow DiameterRanges, columns follow ToleranceSeries.
    private static readonly int[,] Tolerances =
    {
        //  21   22   23   24   25   26   27    28    29    30
        {    3,   5,   8,  12,  20,  30,  50,   80,  130,  200 },  // …10
        {    5,   8,  12,  20,  30,  50,  80,  130,  200,  300 },  // 10…50
        {    6,  10,  16,  25,  40,  60, 100,  160,  250,  400 },  // 50…125
        {    8,  12,  20,  30,  50,  80, 130,  200,  300,  500 },  // 125…280
        {   10,  16,  25,  40,  60, 100, 160,  250,  400,  600 },  // 280…560
        {   12,  20,  30,  50,  80, 130, 200,  300,  500,  800 },  // 560…1000
        {   16,  25,  40,  60, 100, 160, 250,  400,  600, 1000 },  // 1000…1600
        {   20,  30,  50,  80, 130, 200, 300,  500,  800, 1300 },  // 1600…2500
        {   25,  40,  60, 100, 160, 250, 400,  600, 1000, 1600 },  // 2500…4000
        {   30,  50,  80, 130, 200, 300, 500,  800, 1300, 2000 },  // 4000…6300
        {   40,  60, 100, 160, 250, 400, 600, 1000, 1600, 2400 }   // 6300…10000
    };

    /// <summary>The resolved allowances of one gear, in micrometres.</summary>
    public readonly record struct Result(double AsneMicron, double TsnMicron, string Designation)
    {
        /// <summary>A_sni = A_sne − T_sn (Clause 3.2). Both are negative.</summary>
        public double AsniMicron => AsneMicron - TsnMicron;

        public double AsneMm => AsneMicron / 1000.0;
        public double AsniMm => AsniMicron / 1000.0;
    }

    /// <summary>
    /// Row index for a reference diameter, or -1 when it falls outside 0 &lt; d ≤ 10000 mm.
    /// Ranges are (previous, current] — the upper bound is inclusive, so d = 50 is still in
    /// the "over 10 up to 50" row.
    /// </summary>
    private static int RangeIndex(double referenceDiameter)
    {
        if (referenceDiameter <= 0 || referenceDiameter > DiameterRanges[^1]) return -1;

        for (int i = 0; i < DiameterRanges.Length; i++)
        {
            if (referenceDiameter <= DiameterRanges[i]) return i;
        }
        return -1;
    }

    /// <summary>Upper allowance A_sne in µm, or null when the series or diameter is out of scope.</summary>
    public static double? UpperAllowanceMicron(string allowanceSeries, double referenceDiameter)
    {
        int col = Array.IndexOf(AllowanceSeries, allowanceSeries);
        int row = RangeIndex(referenceDiameter);
        if (col < 0 || row < 0) return null;
        return UpperAllowances[row, col];
    }

    /// <summary>Tooth thickness tolerance T_sn in µm, or null when out of scope.</summary>
    public static double? ToleranceMicron(int toleranceSeries, double referenceDiameter)
    {
        int col = Array.IndexOf(ToleranceSeries, toleranceSeries);
        int row = RangeIndex(referenceDiameter);
        if (col < 0 || row < 0) return null;
        return Tolerances[row, col];
    }

    /// <summary>
    /// Both allowances for a tolerance zone, e.g. series 27 with letter "cd" at d = 100 mm
    /// gives −70 / −170 µm.
    /// </summary>
    public static Result? Allowances(int toleranceSeries, string allowanceSeries, double referenceDiameter)
    {
        double? asne = UpperAllowanceMicron(allowanceSeries, referenceDiameter);
        double? tsn = ToleranceMicron(toleranceSeries, referenceDiameter);
        if (asne is null || tsn is null) return null;

        return new Result(asne.Value, tsn.Value, $"{toleranceSeries}{allowanceSeries}");
    }

    /// <summary>
    /// Parses a drawing designation such as "27cd" — the tolerance series number followed by
    /// the allowance series letter (Clause 3.4).
    /// </summary>
    public static Result? Allowances(string designation, double referenceDiameter)
    {
        if (string.IsNullOrWhiteSpace(designation)) return null;

        string text = designation.Trim();
        int split = 0;
        while (split < text.Length && char.IsDigit(text[split])) split++;

        if (split == 0 || split == text.Length) return null;
        if (!int.TryParse(text[..split], out int series)) return null;

        return Allowances(series, text[split..], referenceDiameter);
    }

    /// <summary>Clause 3.3 — the preferred tolerance series.</summary>
    public static bool IsPreferredToleranceSeries(int series) => series is >= 24 and <= 27;

    /// <summary>True when the reference diameter is inside the tables' range.</summary>
    public static bool CoversDiameter(double referenceDiameter) => RangeIndex(referenceDiameter) >= 0;

    /// <summary>
    /// Every tolerance zone for one reference diameter, for a picker. Ordered coarsest
    /// allowance first, matching the order of the columns in the standard's Table 1.
    /// </summary>
    public static IEnumerable<Result> AllZones(int toleranceSeries, double referenceDiameter)
        => AllowanceSeries
            .Select(letter => Allowances(toleranceSeries, letter, referenceDiameter))
            .Where(r => r is not null)
            .Select(r => r!.Value);
}
