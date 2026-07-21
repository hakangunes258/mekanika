using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// ISO 286 limits and fits.
///
/// Covers nominal sizes from 1 mm up to 500 mm for:
///   - Standard tolerance grades IT5 - IT8 (ISO 286-1:2010, Table 1)
///   - Shaft fundamental deviations h, n, p, r, s, t, u (ISO 286-2:2010)
///   - Hole fundamental deviation H (ISO 286-2:2010)
///
/// All deviation values are in micrometers (um).
///
/// Sign convention (ISO 286-1):
///   Shafts: for h the fundamental deviation is the UPPER deviation (es = 0, ei = es - IT).
///           for n..u (interference letters) it is the LOWER deviation (ei, es = ei + IT).
///   Holes:  for H the fundamental deviation is the LOWER deviation (EI = 0, ES = EI + IT).
/// </summary>
public static class Iso286
{
    // ---------------------------------------------------------------------
    // Nominal size ranges
    // ---------------------------------------------------------------------

    // Upper bounds (inclusive) of the main size ranges used by the IT table.
    // ISO 286-1:2010 Table 1.
    private static readonly double[] ItRanges =
        { 3, 6, 10, 18, 30, 50, 80, 120, 180, 250, 315, 400, 500 };

    // Upper bounds (inclusive) of the intermediate size ranges used by the
    // fundamental deviation tables. ISO 286-2:2010.
    private static readonly double[] FdRanges =
        { 3, 6, 10, 14, 18, 24, 30, 40, 50, 65, 80, 100, 120, 140, 160, 180,
          200, 225, 250, 280, 315, 355, 400, 450, 500 };

    // ---------------------------------------------------------------------
    // Standard tolerance grades (IT), in um - ISO 286-1:2010 Table 1
    // Indexed against ItRanges.
    // ---------------------------------------------------------------------
    private static readonly Dictionary<int, int[]> ItValues = new()
    {
        //                <=3  6  10  18  30  50  80 120 180 250 315 400 500
        { 5, new[] {        4,  5,  6,  8,  9, 11, 13, 15, 18, 20, 23, 25, 27 } },
        { 6, new[] {        6,  8,  9, 11, 13, 16, 19, 22, 25, 29, 32, 36, 40 } },
        { 7, new[] {       10, 12, 15, 18, 21, 25, 30, 35, 40, 46, 52, 57, 63 } },
        { 8, new[] {       14, 18, 22, 27, 33, 39, 46, 54, 63, 72, 81, 89, 97 } }
    };

    // ---------------------------------------------------------------------
    // Shaft fundamental deviations ei, in um - ISO 286-2:2010
    // Indexed against FdRanges. int.MinValue marks "not defined by the standard".
    // ---------------------------------------------------------------------
    private const int NA = int.MinValue;

    private static readonly Dictionary<char, int[]> ShaftEi = new()
    {
        //          <=3   6   10  14  18  24  30  40  50  65  80 100 120 140 160 180 200 225 250 280 315 355 400 450 500
        { 'n', new[] { 4,  8, 10, 12, 12, 15, 15, 17, 17, 20, 20, 23, 23, 27, 27, 27, 31, 31, 31, 34, 34, 37, 37, 40, 40 } },
        { 'p', new[] { 6, 12, 15, 18, 18, 22, 22, 26, 26, 32, 32, 37, 37, 43, 43, 43, 50, 50, 50, 56, 56, 62, 62, 68, 68 } },
        { 'r', new[] {10, 15, 19, 23, 23, 28, 28, 34, 34, 41, 43, 51, 54, 63, 65, 68, 77, 80, 84, 94, 98,108,114,126,132 } },
        { 's', new[] {14, 19, 23, 28, 28, 35, 35, 43, 43, 53, 59, 71, 79, 92,100,108,122,130,140,158,170,190,208,232,252 } },
        { 't', new[] {NA, NA, NA, NA, NA, NA, 41, 48, 54, 66, 75, 91,104,122,134,146,166,180,196,218,240,268,294,330,360 } },
        { 'u', new[] {18, 23, 28, 33, 33, 41, 48, 60, 70, 87,102,124,144,170,190,210,236,258,284,315,350,390,435,490,540 } }
    };

    // ---------------------------------------------------------------------
    // Lookup helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Returns the index into a range array for the given diameter, or -1 if
    /// the diameter falls outside the covered span (1 mm .. 500 mm).
    /// Ranges are (previous, current] - i.e. the upper bound is inclusive.
    /// </summary>
    private static int RangeIndex(double[] ranges, double diameter)
    {
        if (diameter <= 0 || diameter > ranges[ranges.Length - 1]) return -1;

        for (int i = 0; i < ranges.Length; i++)
        {
            if (diameter <= ranges[i]) return i;
        }
        return -1;
    }

    /// <summary>
    /// Standard tolerance grade value IT in um, or 0 if not covered.
    /// </summary>
    public static int GetItValue(int grade, double diameter)
    {
        if (!ItValues.TryGetValue(grade, out var table)) return 0;

        int i = RangeIndex(ItRanges, diameter);
        return i < 0 ? 0 : table[i];
    }

    /// <summary>
    /// Shaft fundamental deviation ei in um for the interference letters n..u.
    /// Returns null when the letter/diameter combination is not defined.
    /// </summary>
    private static int? GetShaftEi(char letter, double diameter)
    {
        if (!ShaftEi.TryGetValue(letter, out var table)) return null;

        int i = RangeIndex(FdRanges, diameter);
        if (i < 0) return null;

        int ei = table[i];
        return ei == NA ? (int?)null : ei;
    }

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------

    /// <summary>
    /// Gets the upper and lower deviation for a tolerance code and nominal diameter.
    /// </summary>
    /// <param name="tolerance">The tolerance code, e.g. "H7", "p6".</param>
    /// <param name="diameter">The nominal diameter in mm.</param>
    /// <returns>
    /// (upper, lower) deviations in micrometers (um), or null when the
    /// combination is not covered by this implementation or by the standard.
    /// </returns>
    public static (double upper, double lower)? TryGetDeviations(string tolerance, double diameter)
    {
        if (string.IsNullOrWhiteSpace(tolerance) || tolerance.Length < 2) return null;

        char letter = tolerance[0];
        if (!int.TryParse(tolerance.Substring(1), out int grade)) return null;

        int it = GetItValue(grade, diameter);
        if (it == 0) return null;

        // ----- Shaft (lowercase letter) -----
        if (char.IsLower(letter))
        {
            // 'h' is the basic shaft: the fundamental deviation is the UPPER
            // deviation and equals zero, so the shaft lies entirely below nominal.
            if (letter == 'h') return (0.0, -(double)it);

            int? ei = GetShaftEi(letter, diameter);
            if (ei == null) return null;

            // For n..u the fundamental deviation is the LOWER deviation.
            return (ei.Value + (double)it, (double)ei.Value);
        }

        // ----- Hole (uppercase letter) -----
        if (letter == 'H')
        {
            // 'H' is the basic hole: EI = 0, ES = +IT.
            return ((double)it, 0.0);
        }

        // Any other hole letter is not implemented - report it rather than
        // silently returning a zero-deviation (which would look like H).
        return null;
    }

    /// <summary>
    /// True when the tolerance code and diameter are covered by this implementation.
    /// </summary>
    public static bool IsSupported(string tolerance, double diameter)
        => TryGetDeviations(tolerance, diameter) != null;

    /// <summary>
    /// Gets the upper and lower deviation, falling back to (0, 0) when the
    /// combination is not supported. Prefer <see cref="TryGetDeviations"/> so
    /// unsupported combinations can be reported to the user.
    /// </summary>
    /// <param name="tolerance">The tolerance code, e.g. "H7", "p6".</param>
    /// <param name="diameter">The nominal diameter in mm.</param>
    /// <returns>A tuple with (upper, lower) deviations in micrometers (um).</returns>
    public static (double upper, double lower) GetDeviations(string tolerance, double diameter)
        => TryGetDeviations(tolerance, diameter) ?? (0, 0);
}
