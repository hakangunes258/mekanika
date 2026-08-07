namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Equivalent misalignment from pinion and pinion-shaft deflection, f_sh, according to
/// ISO 6336-1:2006, 7.5.2.4.1 (the approximate calculation, Equations 57 and 58) with the
/// pinion offset constant K' of Figure 13.
///
/// This is the same route a standalone gear calculation in commercial software takes: it
/// is NOT a shaft analysis. It reduces the shaft to four numbers — bearing span, pinion
/// offset from mid-span, shaft diameter and the mounting arrangement — and returns the
/// bending-plus-torsion component of mesh misalignment that those imply.
///
/// Only the equations and the tabulated constants are implemented; no text of the standard
/// is reproduced.
///
/// The standard's own assumptions behind 7.5.2.4.1, which the caller inherits:
///   - deflections of the wheel, wheel shaft, gear case and bearings are NOT included
///   - bearing clearances and bearing moment restraint are NOT included
///   - the pinion shaft has constant diameter, solid or with a bore under half the outside
///   - the shaft is steel
///   - external loads on the shaft (couplings, pulleys, chain wheels) are negligible
/// Anything outside that has to be assessed separately and folded into f_ma.
/// </summary>
public static class Iso6336ShaftDeflection
{
    /// <summary>
    /// Mounting arrangements of ISO 6336-1 Figure 13. The letters are the standard's own;
    /// match the physical layout against that figure, which shows where the torqued end
    /// sits relative to the pinion. <see cref="Description"/> gives the K' values so a
    /// case can be recognised without the figure to hand.
    /// </summary>
    public enum Arrangement { A, B, C, D, E }

    /// <summary>
    /// Figure 13: the constant K' for each arrangement, with and without stiffening of the
    /// span by the pinion body. Case C is unaffected by stiffening.
    /// </summary>
    public static (double withStiffening, double withoutStiffening) FigureThirteen(Arrangement a) => a switch
    {
        Arrangement.A => (0.48, 0.80),
        Arrangement.B => (-0.48, -0.80),
        Arrangement.C => (1.33, 1.33),
        Arrangement.D => (-0.36, -0.60),
        Arrangement.E => (-0.60, -1.00),
        _ => (0.48, 0.80)
    };

    public static string Description(Arrangement a)
    {
        var (with, without) = FigureThirteen(a);
        return $"Figure 13 {a.ToString().ToLowerInvariant()}) — K' = {with:0.##} stiffened / {without:0.##} unstiffened";
    }

    public class Result
    {
        /// <summary>Equivalent misalignment from shaft and pinion deflection (µm).</summary>
        public double fsh { get; set; }
        /// <summary>K' taken from Figure 13 for the chosen arrangement and stiffening.</summary>
        public double KPrime { get; set; }
        /// <summary>True when the pinion body was taken to stiffen the shaft span.</summary>
        public bool Stiffened { get; set; }
        /// <summary>Mean load intensity F_m/b (N/mm) the result was evaluated at.</summary>
        public double FmOverB { get; set; }
        /// <summary>The offset ratio s/l; the equations are bounded to 0 … 0,3.</summary>
        public double OffsetRatio { get; set; }
        /// <summary>The bracket ( |B* + K'·(l·s/d1²)·(d1/d_sh)⁴ − 0,3| + 0,3 ).</summary>
        public double Bracket { get; set; }
        public bool Valid { get; set; }
        public List<string> Notes { get; } = new();
    }

    /// <summary>
    /// f_sh per ISO 6336-1 Equation (57) for spur and single helical gears:
    ///
    ///   f_sh = (F_m/b) · 0,023 · ( |B* + K'·(l·s/d1²)·(d1/d_sh)⁴ − 0,3| + 0,3 ) · (b/d1)²
    ///
    /// and Equation (58) for double helical gears, which replaces 0,023 with 0,046 and the
    /// face width in the last term with the width of one helix.
    ///
    /// The 0,023 constant carries the units, so F_m/b in N/mm gives f_sh directly in µm.
    /// </summary>
    /// <param name="FmOverB">Mean load intensity F_m/b = F_t·K_A·K_V/b (N/mm)</param>
    /// <param name="b">Face width (mm); for double helical, the full width</param>
    /// <param name="bB">Width of one helix of a double helical gear (mm); ignored otherwise</param>
    /// <param name="d1">Pinion reference diameter (mm)</param>
    /// <param name="l">Bearing span (mm)</param>
    /// <param name="s">Distance from the pinion mid-plane to the middle of the bearing span (mm)</param>
    /// <param name="dsh">Outside diameter of the pinion shaft, for bending (mm)</param>
    /// <param name="boreDiameter">Bore of a hollow shaft (mm); 0 for solid</param>
    /// <param name="arrangement">Mounting arrangement per Figure 13</param>
    /// <param name="pinionCanStiffen">
    /// False when the pinion cannot stiffen the span whatever the diameter ratio — the
    /// standard names a pinion sliding on a shaft with a feather key, or a normal shrink
    /// fit, as cases where scarcely any stiffening is to be expected.
    /// </param>
    /// <param name="doubleHelical">Selects Equation (58) instead of (57)</param>
    /// <param name="powerPathPercent">
    /// Percentage of the input power carried by this one mesh. 100 (the default) means a
    /// single engagement, giving B* = 1 for spur/single helical and 1,5 for double helical.
    /// </param>
    public static Result Calculate(
        double FmOverB, double b, double bB, double d1, double l, double s, double dsh,
        double boreDiameter, Arrangement arrangement, bool pinionCanStiffen = true,
        bool doubleHelical = false, double powerPathPercent = 100.0)
    {
        var r = new Result { FmOverB = FmOverB };

        if (FmOverB <= 0 || b <= 0 || d1 <= 0 || l <= 0 || dsh <= 0)
        {
            r.Notes.Add("Bearing span, shaft diameter, face width and load are all needed to calculate f_sh; it was taken as 0.");
            return r;
        }

        if (s < 0)
        {
            r.Notes.Add("The pinion offset s is a distance and cannot be negative; its magnitude was used. " +
                        "Which side of mid-span the pinion sits on is expressed by the Figure 13 arrangement, not by the sign.");
            s = Math.Abs(s);
        }

        // Stiffening: Figure 13 footnote — the pinion body stiffens the span when
        // d1/d_sh >= 1,15, and not below that.
        bool stiffened = pinionCanStiffen && d1 / dsh >= 1.15;
        r.Stiffened = stiffened;

        var (kWith, kWithout) = FigureThirteen(arrangement);
        double kPrime = stiffened ? kWith : kWithout;
        r.KPrime = kPrime;

        // B* — 1 for a single engagement, 1,5 for double helical through a single mesh;
        // reduced when the power splits across several meshes.
        double bStar;
        if (powerPathPercent >= 99.999)
        {
            bStar = doubleHelical ? 1.5 : 1.0;
        }
        else
        {
            double k = Math.Max(1.0, powerPathPercent);
            bStar = doubleHelical ? 0.5 + (200.0 - k) / k : 1.0 + 2.0 * (100.0 - k) / k;
        }

        double offsetRatio = s / l;
        r.OffsetRatio = offsetRatio;

        double inner = bStar + kPrime * (l * s / (d1 * d1)) * Math.Pow(d1 / dsh, 4.0) - 0.3;
        double bracket = Math.Abs(inner) + 0.3;
        r.Bracket = bracket;

        double widthTerm = doubleHelical && bB > 0 ? bB : b;
        double coefficient = doubleHelical ? 0.046 : 0.023;

        r.fsh = FmOverB * coefficient * bracket * Math.Pow(widthTerm / d1, 2.0);
        r.Valid = true;

        // --- Range of application ---
        if (offsetRatio > 0.3)
        {
            r.Notes.Add($"The pinion offset ratio s/l = {offsetRatio:F3} is above the 0,3 that Figure 13 is " +
                        "drawn for. ISO 6336-1 asks for a comprehensive analysis beyond that, unless suitable " +
                        "helix correction is applied. Treat this f_sh as indicative only.");
        }

        if (boreDiameter > 0 && boreDiameter / dsh >= 0.5)
        {
            r.Notes.Add($"The shaft bore is {boreDiameter / dsh:F2}× the outside diameter. Equation (57) is " +
                        "only stated to be accurate for a bore under half the outside diameter, so the real " +
                        "deflection will be larger than this.");
        }

        if (d1 / dsh < 1.15 && pinionCanStiffen)
        {
            r.Notes.Add($"d1/d_sh = {d1 / dsh:F2} is below 1,15, so the pinion body was taken NOT to stiffen the " +
                        "shaft span and the larger K' of Figure 13 was used.");
        }

        return r;
    }

    /// <summary>
    /// Minimum equivalent misalignment F_βx,min — ISO 6336-1 Equations (55) and (56):
    /// the greater of 0,005 mm·µm/N × F_m/b and 0,5 f_Hβ.
    ///
    /// Both Equation (52) and Equation (53) carry "F_βx &gt;= F_βx,min", so this is a floor
    /// on the misalignment however small the calculated deflection and manufacturing
    /// components come out. Leaving it off understates K_Hβ.
    /// </summary>
    /// <param name="FmOverB">Mean load intensity F_m/b (N/mm)</param>
    /// <param name="fHbeta">Helix slope deviation of the determinant gear (µm)</param>
    public static double MinimumMisalignment(double FmOverB, double fHbeta)
        => Math.Max(0.005 * Math.Max(0, FmOverB), 0.5 * Math.Max(0, fHbeta));
}
