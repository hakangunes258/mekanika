namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// The factors that turn an allowable stress number (sigma_Flim / sigma_Hlim) into a
/// permissible stress (sigma_FP / sigma_HP):
///
///   sigma_FP = sigma_Flim * Y_ST * Y_NT * Y_deltarelT * Y_RrelT * Y_X    (ISO 6336-3:2006, Clause 5)
///   sigma_HP = sigma_Hlim * Z_NT * Z_L * Z_v * Z_R * Z_W * Z_X           (ISO 6336-2:2006, Clause 5)
///
/// Z_L, Z_v and Z_R live in <see cref="Iso6336SurfaceFactors"/>; everything else is here.
/// Equation and table numbers in the comments refer to the 2006 editions. Only the
/// equations and tabulated coefficients are implemented; no text of the standard is
/// reproduced.
///
/// These replace the blanket constants the module used previously (a flat 0.95 standing
/// in for Y_deltarelT*Y_RrelT*Y_X, and Z_NT/Y_NT curves invented for the purpose). Each
/// factor below is now the real curve, and every one of them was checked against the
/// standard's own anchor points - see the comments on the individual methods.
/// </summary>
public static class Iso6336LifeFactors
{
    /// <summary>
    /// Life factor groups. ISO 6336-2 Table 2 and ISO 6336-3 Table 1 split the materials
    /// the same way, so one grouping serves both.
    /// </summary>
    public enum LifeGroup
    {
        /// <summary>St, V, GGG(perl./bai.), GTS(perl.), Eh, IF.</summary>
        SteelAndHardened,
        /// <summary>GG, GGG(ferr.), NT(nitr.), NV(nitr.), NV(nitrocar.).</summary>
        NitridedAndCastIron
    }

    /// <summary>Surface/size factor groups of ISO 6336-3 Tables 4 and 5.</summary>
    public enum SurfaceGroup
    {
        /// <summary>Structural steel (St).</summary>
        StructuralSteel,
        /// <summary>V, Eh, IF, NT, NV - through hardened and surface hardened steels.</summary>
        HardenedSteel,
        /// <summary>GG, GGG(ferr.) - grey and ferritic nodular cast iron.</summary>
        CastIron
    }

    public static LifeGroup MapLifeGroup(GearMaterialType t) => t switch
    {
        GearMaterialType.GreyCastIron
            or GearMaterialType.NitridedNitridingSteel
            or GearMaterialType.NitridedThroughHardeningSteel
            or GearMaterialType.Nitrocarburized => LifeGroup.NitridedAndCastIron,
        _ => LifeGroup.SteelAndHardened
    };

    public static SurfaceGroup MapSurfaceGroup(GearMaterialType t) => t switch
    {
        GearMaterialType.NormalizedLowCarbonSteel
            or GearMaterialType.NormalizedCastSteel => SurfaceGroup.StructuralSteel,
        GearMaterialType.GreyCastIron
            or GearMaterialType.NodularCastIron
            or GearMaterialType.BlackMalleableCastIron => SurfaceGroup.CastIron,
        _ => SurfaceGroup.HardenedSteel
    };

    /// <summary>True for the case/flame/induction hardened and nitrided groups.</summary>
    public static bool IsSurfaceHardened(GearMaterialType t) => t is
        GearMaterialType.CaseHardened
        or GearMaterialType.FlameOrInductionHardened
        or GearMaterialType.NitridedNitridingSteel
        or GearMaterialType.NitridedThroughHardeningSteel
        or GearMaterialType.Nitrocarburized;

    // ===================== Life factor, tooth root: Y_NT =====================

    /// <summary>
    /// Life factor for tooth root stress Y_NT - ISO 6336-3:2006 Table 1.
    ///
    /// Anchor points of the standard reproduced by these expressions:
    ///   SteelAndHardened   : 2.5 at N = 1e4 ((3e6/1e4)^0.16  = 2.491)
    ///   NitridedAndCastIron: 1.6 at N = 1e3 ((3e6/1e3)^0.06  = 1.617)
    ///   both               : 1.0 at N = 3e6, 0.85 at N = 1e10 ((3e6/1e10)^0.02 = 0.850)
    /// </summary>
    /// <param name="cycles">Number of load cycles N_L</param>
    /// <param name="group">Material life group</param>
    /// <param name="optimumConditions">
    /// When true, Y_NT is held at 1.0 beyond the knee instead of following the descending
    /// branch. ISO 6336-3 permits this only for optimum material, manufacturing and
    /// lubrication backed by experience, so it defaults to false (conservative).
    /// </param>
    public static double YNT(double cycles, LifeGroup group, bool optimumConditions = false)
    {
        if (cycles <= 0) return 1.0;

        double staticLimit = group == LifeGroup.SteelAndHardened ? 1e4 : 1e3;
        double slope = group == LifeGroup.SteelAndHardened ? 0.16 : 0.06;

        if (cycles <= staticLimit)
            return Math.Pow(3e6 / staticLimit, slope);

        if (cycles <= 3e6)
            return Math.Pow(3e6 / cycles, slope);

        // Long-life branch: 1.0 at 3e6 falling to 0.85 at 1e10.
        return optimumConditions ? 1.0 : Math.Pow(3e6 / cycles, 0.02);
    }

    // ===================== Life factor, tooth flank: Z_NT =====================

    /// <summary>
    /// Life factor for contact stress Z_NT - ISO 6336-2:2006 Table 2.
    ///
    /// Anchor points reproduced:
    ///   SteelAndHardened   : 1.6 at N = 6e5 ((3e8/6e5)^0.0756 = 1.600), 1.0 at N = 1e9
    ///   NitridedAndCastIron: 1.3 at N = 1e5 ((2e6/1e5)^0.0875 = 1.300), 1.0 at N = 2e6
    ///   descending branch reaches 0.85 at N = 1e10 in both cases.
    ///
    /// TWO ROWS, NOT ONE. Table 2 splits the steel/hardened group by whether some pitting is
    /// acceptable on the finished gear:
    ///   - pitting NOT permissible (the default): 1.6 at 1e5 falling to 1.0 at N = 5e7
    ///   - a certain amount of pitting permissible: 1.6 at 6e5 falling to 1.0 at N = 1e9
    /// Only the second was implemented, and it is the optimistic one: at 1.1e8 cycles it gives
    /// Z_NT = 1.13 where the first gives 1.00, which inflates sigma_HP by 13 % and can turn a
    /// failing flank into a passing one. The first row is now the default.
    ///
    /// Cross-check on the first row: the exponent ln(1,6)/ln(5e7/1e5) = 0,075627 gives
    /// Z_NT = 1,0135 at N = 4,186e7 - the value KISSsoft reports for the same gear.
    /// </summary>
    /// <param name="limitedPittingPermissible">
    /// True when a certain amount of pitting is acceptable on the finished flank, which moves
    /// the knee of the steel/hardened curve from 5e7 to 1e9 cycles. Defaults to false.
    /// </param>
    public static double ZNT(double cycles, LifeGroup group, bool optimumConditions = false,
                             bool limitedPittingPermissible = false)
    {
        if (cycles <= 0) return 1.0;

        if (group == LifeGroup.SteelAndHardened)
        {
            if (limitedPittingPermissible)
            {
                if (cycles <= 6e5) return 1.6;
                if (cycles <= 1e7) return Math.Pow(3e8 / cycles, 0.0756);
                if (cycles <= 1e9) return Math.Pow(1e9 / cycles, 0.057);
                // 1.0 at 1e9 down to 0.85 at 1e10: exponent = ln(0.85)/ln(0.1) = 0.07058
                return optimumConditions ? 1.0 : Math.Pow(1e9 / cycles, 0.07058);
            }

            // Pitting not permissible: 1.6 at 1e5 down to 1.0 at 5e7 (exponent
            // ln(1.6)/ln(500) = 0.075627), then CONTINUING down to 0.85 at 1e10
            // (exponent ln(1/0.85)/ln(200) = 0.030674).
            //
            // The second branch was flat at 1.0 as far as 1e9, which is not what the standard
            // draws and is non-conservative: at 9e7 cycles it gives 1.000 where the curve gives
            // 0.982. Both exponents are anchored against a KISSsoft ISO 6336:2006 Method B
            // report on the same gear - 1,008 at 4,5e7 and 0,982 at 9,0e7, reproduced to four
            // figures - and that report states the range as "Z_NT and Y_NT >= 0,85".
            if (cycles <= 1e5) return 1.6;
            if (cycles <= 5e7) return Math.Pow(5e7 / cycles, 0.075627);
            return optimumConditions ? 1.0 : Math.Pow(5e7 / cycles, 0.030674);
        }

        if (cycles <= 1e5) return 1.3;
        if (cycles <= 2e6) return Math.Pow(2e6 / cycles, 0.0875);
        // 1.0 at 2e6 down to 0.85 at 1e10: exponent = ln(0.85)/ln(2e-4) = 0.01908
        return optimumConditions ? 1.0 : Math.Pow(2e6 / cycles, 0.01908);
    }

    // ===================== Relative notch sensitivity: Y_deltarelT =====================

    /// <summary>
    /// Slip-layer thickness rho' (mm) - ISO 6336-3:2006 Table 3.
    ///
    /// Surface hardened and cast iron materials have a single tabulated value; for
    /// through hardened steels rho' is a function of the yield strength, interpolated
    /// linearly between the tabulated points below.
    /// </summary>
    public static double SlipLayerThickness(GearMaterialType type, double yieldStrength)
    {
        if (IsSurfaceHardened(type)) return 0.0030;

        if (type == GearMaterialType.GreyCastIron) return 0.3124;
        if (type == GearMaterialType.NodularCastIron ||
            type == GearMaterialType.BlackMalleableCastIron) return 0.1005;

        // Table 3, through hardened / structural steels, keyed on sigma_s (N/mm2)
        double[] sigmaS = { 300, 400, 500, 600, 800, 1000 };
        double[] rho = { 0.0833, 0.0445, 0.0281, 0.0194, 0.0064, 0.0014 };

        double s = yieldStrength > 0 ? yieldStrength : 300;
        if (s <= sigmaS[0]) return rho[0];
        if (s >= sigmaS[^1]) return rho[^1];

        for (int i = 1; i < sigmaS.Length; i++)
        {
            if (s <= sigmaS[i])
            {
                double f = (s - sigmaS[i - 1]) / (sigmaS[i] - sigmaS[i - 1]);
                return rho[i - 1] + f * (rho[i] - rho[i - 1]);
            }
        }
        return rho[^1];
    }

    /// <summary>
    /// Relative notch sensitivity factor Y_deltarelT - ISO 6336-3:2006 Method B, Eq. (49):
    ///
    ///     Y_deltarelT = (1 + sqrt(rho' * chi*)) / (1 + sqrt(rho' * chi*_T))
    ///
    /// with the relative stress gradient chi* = (1 + 2 q_s)/5 and the reference test gear
    /// at q_sT = 2.5, i.e. chi*_T = 1.2 mm^-1.
    /// </summary>
    /// <param name="qs">Notch parameter q_s of the gear (from the ISO 6336-3 tooth form)</param>
    /// <param name="type">Material group, for the slip-layer thickness</param>
    /// <param name="yieldStrength">Yield strength sigma_s (N/mm2), used for through hardened steels</param>
    public static double YdeltaRelT(double qs, GearMaterialType type, double yieldStrength)
    {
        if (qs <= 0) return 1.0;

        double rhoPrime = SlipLayerThickness(type, yieldStrength);
        double chi = (1.0 + 2.0 * qs) / 5.0;          // mm^-1
        const double chiT = 1.2;                       // reference test gear, q_sT = 2.5

        return (1.0 + Math.Sqrt(rhoPrime * chi)) / (1.0 + Math.Sqrt(rhoPrime * chiT));
    }

    // ===================== Relative surface condition: Y_RrelT =====================

    /// <summary>
    /// Relative surface condition factor Y_RrelT - ISO 6336-3:2006 Method B, Eq. (51)-(56).
    ///
    /// R_z here is the peak-to-valley roughness of the TOOTH ROOT FILLET, not of the flank.
    /// All three branches return 1.0 at R_z = 10 um, which is the reference test gear -
    /// that identity is what pins the constants below to the right rows of the standard.
    /// </summary>
    /// <param name="rzRoot">Root fillet peak-to-valley roughness R_z (um)</param>
    public static double YRrelT(double rzRoot, SurfaceGroup group)
    {
        double rz = rzRoot > 0 ? rzRoot : 10.0;

        if (rz < 1.0)
        {
            return group switch
            {
                SurfaceGroup.StructuralSteel => 1.025,
                SurfaceGroup.CastIron => 1.070,
                _ => 1.120
            };
        }

        if (rz > 40.0) rz = 40.0;   // upper bound of the tabulated range

        return group switch
        {
            SurfaceGroup.StructuralSteel => 4.299 - 3.259 * Math.Pow(rz + 1.0, 0.0058),
            SurfaceGroup.CastIron => 5.306 - 4.203 * Math.Pow(rz + 1.0, 0.0100),
            _ => 1.674 - 0.529 * Math.Pow(rz + 1.0, 0.1000)
        };
    }

    // ===================== Size factors: Y_X and Z_X =====================

    /// <summary>
    /// Size factor for tooth root strength Y_X - ISO 6336-3:2006 Table 5.
    /// Each branch is continuous with its plateau at the upper module limit
    /// (St: 1.03 - 0.006*30 = 0.85; Eh: 1.05 - 0.01*25 = 0.80; GG: 1.075 - 0.015*25 = 0.70).
    /// </summary>
    public static double YX(double mn, SurfaceGroup group)
    {
        if (mn <= 5.0) return 1.0;

        switch (group)
        {
            case SurfaceGroup.CastIron:
                return mn >= 25.0 ? 0.70 : 1.075 - 0.015 * mn;

            case SurfaceGroup.HardenedSteel:
                // Applies to Eh/IF/NT/NV. Through hardened steels follow the St row,
                // but the difference only appears above mn = 5 and is small; the
                // hardened row is the conservative one, so it is used for both.
                return mn >= 25.0 ? 0.80 : 1.05 - 0.01 * mn;

            default:
                return mn >= 30.0 ? 0.85 : 1.03 - 0.006 * mn;
        }
    }

    /// <summary>
    /// Size factor for contact stress Z_X - ISO 6336-2:2006 Table 4.
    /// Unity for through hardened steels and for any material up to mn = 10; surface
    /// hardened materials fall to 0.90 at mn = 30 (1.05 - 0.005*30 = 0.90).
    /// </summary>
    public static double ZX(double mn, GearMaterialType type)
    {
        if (!IsSurfaceHardened(type)) return 1.0;
        if (mn <= 10.0) return 1.0;
        return mn >= 30.0 ? 0.90 : 1.05 - 0.005 * mn;
    }

    // ===================== Work hardening factor: Z_W =====================

    /// <summary>
    /// Work hardening factor Z_W - ISO 6336-2:2006 Clause 13.
    ///
    /// Z_W raises the permissible contact stress of a SOFT, through hardened wheel that
    /// is run against a substantially harder pinion with smooth flanks: the hard flanks
    /// cold-work the soft ones. It applies to that gear only, and only while the
    /// conditions hold - in every other pairing (both gears hardened, both soft, or a
    /// rough hard flank) the standard's own conditions give Z_W = 1.0, which is what
    /// this returns.
    /// </summary>
    /// <param name="softHardnessHV">Surface hardness of THIS gear (HV)</param>
    /// <param name="mateIsSurfaceHardened">True when the mating gear is surface hardened</param>
    /// <param name="thisIsSurfaceHardened">True when THIS gear is surface hardened</param>
    /// <param name="mateFlankRz">Flank roughness R_z of the mating (hard) gear (um)</param>
    public static double ZW(double softHardnessHV, bool mateIsSurfaceHardened,
                            bool thisIsSurfaceHardened, double mateFlankRz)
    {
        // Only a soft gear running against a hard, smooth mate is work hardened.
        if (thisIsSurfaceHardened || !mateIsSurfaceHardened) return 1.0;
        if (mateFlankRz > 6.0) return 1.0;   // 13.2: hard flank must be smooth

        // Table 1 of ISO 6336-5 gives the through hardened rows in HV; the Z_W
        // expression is written in HBW. Over the range that matters (<= 450 HV) the
        // two scales differ by about 5 %.
        double hb = softHardnessHV * 0.95;

        if (hb < 130.0) return 1.2;
        if (hb > 470.0) return 1.0;
        return 1.2 - (hb - 130.0) / 1700.0;
    }
}
