namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Transverse load factors K_Halpha and K_Falpha according to ISO 6336-1:2006,
/// Clause 8, Method B, together with the running-in allowance y_alpha of 8.3.5.
///
/// Equation numbers in the comments refer to ISO 6336-1:2006.
/// </summary>
public static class Iso6336TransverseFactor
{
    /// <summary>Material groups used by the running-in allowance equations (8.3.5.1).</summary>
    public enum RunInMaterialGroup
    {
        /// <summary>St, St(cast), V, V(cast), GGG(perl./bai.), GTS(perl.) - Eq. (75)</summary>
        SteelAndThroughHardened,
        /// <summary>GG and GGG(ferr.) - Eq. (76)</summary>
        GreyOrFerriticNodularIron,
        /// <summary>Eh, IF, NT(nitr.), NV(nitr.), NV(nitrocar.) - Eq. (77)</summary>
        SurfaceHardened
    }

    public class Result
    {
        public double KHalpha { get; set; } = 1.0;
        public double KFalpha { get; set; } = 1.0;
        public double yAlpha { get; set; }      // running-in allowance (um)
        public double qAlpha { get; set; }      // c_ga*(f_pb - y_a)/(F_tH/b)
        public double FtH { get; set; }         // determinant transverse tangential load (N)
        public bool KHalphaLimited { get; set; }
        public bool KFalphaLimited { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Running-in allowance y_alpha - ISO 6336-1 Eq. (75) to (77).
    ///
    /// NOTE ON EQ. (75): the printed fraction can be read as 160/(sigma_Hlim * f_pb).
    /// That reading is excluded by the standard's own stated limits: it gives an upper
    /// limit of 12 800/sigma_Hlim at f_pb = 80 um only if y_alpha = (160/sigma_Hlim)*f_pb
    /// (160*80 = 12 800), and likewise 6 400/sigma_Hlim at f_pb = 40 um. The
    /// multiplicative form is therefore used.
    /// </summary>
    /// <param name="group">Material group</param>
    /// <param name="fpb">Base pitch deviation (um)</param>
    /// <param name="sigmaHlim">Allowable stress number, contact (N/mm2)</param>
    /// <param name="v">Pitch line velocity (m/s)</param>
    public static double RunningInAllowance(RunInMaterialGroup group, double fpb,
                                            double sigmaHlim, double v)
    {
        if (fpb <= 0) return 0;

        switch (group)
        {
            case RunInMaterialGroup.SteelAndThroughHardened:
            {
                if (sigmaHlim <= 0) return 0;
                double y = (160.0 / sigmaHlim) * fpb;                 // Eq. (75)
                if (v > 10.0) y = Math.Min(y, 6400.0 / sigmaHlim);    // f_pb = 40 um cap
                else if (v > 5.0) y = Math.Min(y, 12800.0 / sigmaHlim); // f_pb = 80 um cap
                return y;
            }
            case RunInMaterialGroup.GreyOrFerriticNodularIron:
            {
                double y = 0.275 * fpb;                               // Eq. (76)
                if (v > 10.0) y = Math.Min(y, 11.0);
                else if (v > 5.0) y = Math.Min(y, 22.0);
                return y;
            }
            case RunInMaterialGroup.SurfaceHardened:
            default:
                return Math.Min(0.075 * fpb, 3.0);                    // Eq. (77), cap 3 um
        }
    }

    /// <summary>
    /// K_Halpha and K_Falpha per ISO 6336-1, 8.3 (Method B), with the limiting
    /// conditions of 8.3.3 and 8.3.4.
    /// </summary>
    /// <param name="Ft">Nominal tangential load (N)</param>
    /// <param name="b">Face width (mm)</param>
    /// <param name="KA">Application factor</param>
    /// <param name="KV">Dynamic factor</param>
    /// <param name="KHbeta">Face load factor (flank)</param>
    /// <param name="cGammaAlpha">Mesh stiffness (N/(mm*um))</param>
    /// <param name="fpb">Base pitch deviation, larger of pinion/wheel (um)</param>
    /// <param name="ffalpha">Profile form deviation (um) - used instead of f_pb when larger (footnote 12)</param>
    /// <param name="yAlpha">Running-in allowance (um)</param>
    /// <param name="epsilonAlpha">Transverse contact ratio</param>
    /// <param name="epsilonGamma">Total contact ratio</param>
    /// <param name="ZEpsilon">Contact ratio factor Z_eps (for the K_Halpha limit)</param>
    public static Result Calculate(double Ft, double b, double KA, double KV, double KHbeta,
                                   double cGammaAlpha, double fpb, double ffalpha, double yAlpha,
                                   double epsilonAlpha, double epsilonGamma, double ZEpsilon)
    {
        var r = new Result();

        if (Ft <= 0 || b <= 0 || epsilonAlpha <= 0 || epsilonGamma <= 0 || cGammaAlpha <= 0)
        {
            r.Note = "Invalid input for transverse load factors; K_Halpha = K_Falpha = 1 assumed.";
            return r;
        }

        // Footnote 12: if the profile form deviation exceeds the base pitch deviation,
        // the profile form deviation is used instead.
        double deviation = Math.Max(fpb, ffalpha);

        // Determinant transverse tangential load F_tH = Ft * KA * KV * KHbeta
        double FtH = Ft * KA * KV * KHbeta;
        r.FtH = FtH;
        r.yAlpha = yAlpha;

        double specific = FtH / b;                     // N/mm
        if (specific <= 0)
        {
            r.Note = "Determinant tangential load is zero.";
            return r;
        }

        double qAlpha = cGammaAlpha * (deviation - yAlpha) / specific;
        r.qAlpha = qAlpha;

        double kAlpha;
        if (epsilonGamma <= 2.0)
        {
            // Eq. (71)
            kAlpha = (epsilonGamma / 2.0) * (0.9 + 0.4 * qAlpha);
        }
        else
        {
            // Eq. (72). The square root covers only 2(eps_gamma - 1)/eps_gamma;
            // q_alpha multiplies it from outside.
            //
            // This had q_alpha inside the root. Two things show that is wrong. The
            // branches must meet: at eps_gamma = 2 Eq. (71) gives 0.9 + 0.4 q_alpha,
            // and so does this one with q_alpha outside, because the root is then
            // exactly 1 - while with q_alpha inside it jumps to 0.9 + 0.4 sqrt(q).
            // And a KISSsoft 2022 ISO 6336:2006 report on a 22/45, mn 3, beta 22.5
            // pair prints K_Halpha = 1.016 where the form below gives 1.017 fed the
            // report's own K_Hbeta; the rooted form gives 1.111.
            //
            // Direction matters: sqrt(q) > q below 1 and sqrt(q) < q above it, so
            // the old form was conservative for a heavily loaded pair and NOT
            // conservative for a lightly loaded or coarse one - which is exactly
            // where this factor carries weight.
            kAlpha = 0.9 + 0.4 * Math.Sqrt(2.0 * (epsilonGamma - 1.0) / epsilonGamma) * qAlpha;
        }

        double kH = kAlpha;
        double kF = kAlpha;

        // --- Limiting conditions for K_Halpha (8.3.3, Eq. 73) ---
        if (ZEpsilon > 0)
        {
            double limitH = epsilonGamma / (epsilonAlpha * ZEpsilon * ZEpsilon);
            if (kH > limitH) { kH = limitH; r.KHalphaLimited = true; }
        }
        if (kH < 1.0) { kH = 1.0; r.KHalphaLimited = true; }

        // --- Limiting conditions for K_Falpha (8.3.4, Eq. 74) ---
        double limitF = epsilonGamma / (0.25 * epsilonAlpha + 0.75);
        if (kF > limitF) { kF = limitF; r.KFalphaLimited = true; }
        if (kF < 1.0) { kF = 1.0; r.KFalphaLimited = true; }

        r.KHalpha = kH;
        r.KFalpha = kF;
        return r;
    }

    /// <summary>Maps an ISO 6336-5 material group to the running-in material group of 8.3.5.1.</summary>
    public static RunInMaterialGroup MapMaterialGroup(GearMaterialType t) => t switch
    {
        GearMaterialType.GreyCastIron => RunInMaterialGroup.GreyOrFerriticNodularIron,
        GearMaterialType.CaseHardened
            or GearMaterialType.FlameOrInductionHardened
            or GearMaterialType.NitridedNitridingSteel
            or GearMaterialType.NitridedThroughHardeningSteel
            or GearMaterialType.Nitrocarburized => RunInMaterialGroup.SurfaceHardened,
        _ => RunInMaterialGroup.SteelAndThroughHardened
    };
}
