namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Face load factors K_Hbeta and K_Fbeta according to ISO 6336-1:2006,
/// Clause 7.5 (Method C) and Clause 7.6.
///
/// Equation numbers in the comments refer to ISO 6336-1:2006.
///
/// SCOPE NOTE - shaft deflection f_sh
/// Method C models the pinion/pinion-shaft elastic deflection through f_sh, which
/// requires the shaft diameter, bearing span and pinion offset (7.5.2.4, Figure 13).
/// This calculator deliberately does not model shafts, so f_sh is an OPTIONAL input:
///   - f_sh = 0 means shaft deflection is neglected (rigid shaft assumption). This is
///     NOT conservative: a real shaft deflects and raises K_Hbeta.
///   - Users who have f_sh from a shaft analysis can enter it directly.
/// The manufacturing component f_ma is taken from the ISO 1328-1 helix slope tolerance,
/// which ISO 6336-1, 7.4.4.2 explicitly permits.
/// </summary>
public static class Iso6336FaceLoadFactor
{
    /// <summary>Helix modification types of ISO 6336-1 Table 8.</summary>
    public enum HelixModification
    {
        /// <summary>No modification - B1 = 1, B2 = 1 (Table 8, row 1)</summary>
        None,
        /// <summary>Central crowning, C_beta = 0,5 f_ma - B1 = 1, B2 = 0,5 (row 2)</summary>
        CentralCrowningFma,
        /// <summary>Central crowning, C_beta = 0,5 (f_ma + f_sh) - B1 = 0,5, B2 = 0,5 (row 3)</summary>
        CentralCrowningFmaFsh,
        /// <summary>Helix correction only - B1 = 0,1, B2 = 1,0 (row 4)</summary>
        HelixCorrectionOnly,
        /// <summary>Helix correction plus central crowning - B1 = 0,1, B2 = 0,5 (row 5)</summary>
        HelixCorrectionPlusCrowning,
        /// <summary>End relief - B1 = 0,7, B2 = 0,7 (row 6)</summary>
        EndRelief
    }

    /// <summary>Material groups for the running-in allowance of 7.5.2.1.</summary>
    public enum RunInMaterialGroup
    {
        /// <summary>St, St(cast), V, V(cast), GGG(perl./bai.), GTS(perl.) - Eq. (44), (45)</summary>
        SteelAndThroughHardened,
        /// <summary>GG, GGG(ferr.) - Eq. (46), (47)</summary>
        GreyOrFerriticNodularIron,
        /// <summary>Eh, IF, NT(nitr.), NV(nitr.), NV(nitrocar.) - Eq. (48)</summary>
        SurfaceHardened
    }

    public class Result
    {
        public double KHbeta { get; set; } = 1.0;
        public double KFbeta { get; set; } = 1.0;
        public double Fbx { get; set; }        // initial equivalent misalignment (um), after the Eq. (56) floor
        public double FbxCalculated { get; set; } // before the floor (um)
        public double FbxMin { get; set; }     // Eq. (56) floor (um)
        public bool FbxFloored { get; set; }   // true when the floor was the governing value
        public double Fby { get; set; }        // effective equivalent misalignment (um)
        public double yBeta { get; set; }      // running-in allowance (um)
        public double B1 { get; set; }
        public double B2 { get; set; }
        public double FmOverB { get; set; }    // mean load intensity (N/mm)
        public double BcalOverB { get; set; }  // contact width ratio
        public double NF { get; set; }         // exponent of Eq. (70)
        public double BOverH { get; set; }     // face width to tooth depth ratio
        public bool FullFaceContact { get; set; }
        public List<string> Notes { get; } = new();
    }

    /// <summary>Table 8 constants B1 and B2.</summary>
    public static (double B1, double B2) TableEight(HelixModification m) => m switch
    {
        HelixModification.None                        => (1.0, 1.0),
        HelixModification.CentralCrowningFma          => (1.0, 0.5),
        HelixModification.CentralCrowningFmaFsh       => (0.5, 0.5),
        HelixModification.HelixCorrectionOnly         => (0.1, 1.0),
        HelixModification.HelixCorrectionPlusCrowning => (0.1, 0.5),
        HelixModification.EndRelief                   => (0.7, 0.7),
        _                                             => (1.0, 1.0)
    };

    /// <summary>
    /// Running-in allowance y_beta - ISO 6336-1 Eq. (44) to (48).
    ///
    /// As with y_alpha (8.3.5.1), the printed fraction in Eq. (44) is confirmed to be
    /// multiplicative by the standard's own limits: 320 * 80 um = 25 600/sigma_Hlim and
    /// 320 * 40 um = 12 800/sigma_Hlim.
    /// </summary>
    public static double RunningInAllowanceBeta(RunInMaterialGroup group, double Fbx,
                                                double sigmaHlim, double v)
    {
        if (Fbx <= 0) return 0;

        double y;
        switch (group)
        {
            case RunInMaterialGroup.SteelAndThroughHardened:
                if (sigmaHlim <= 0) return 0;
                y = (320.0 / sigmaHlim) * Fbx;                       // Eq. (44)
                if (v > 10.0) y = Math.Min(y, 12800.0 / sigmaHlim);
                else if (v > 5.0) y = Math.Min(y, 25600.0 / sigmaHlim);
                break;

            case RunInMaterialGroup.GreyOrFerriticNodularIron:
                y = 0.55 * Fbx;                                      // Eq. (46)
                if (v > 10.0) y = Math.Min(y, 22.0);
                else if (v > 5.0) y = Math.Min(y, 45.0);
                break;

            case RunInMaterialGroup.SurfaceHardened:
            default:
                y = 0.15 * Fbx;                                      // Eq. (48)
                break;
        }

        // y_beta <= F_bx (7.5.2.1)
        return Math.Min(y, Fbx);
    }

    /// <summary>
    /// K_Hbeta and K_Fbeta per ISO 6336-1, 7.5 (Method C) and 7.6.
    /// </summary>
    /// <param name="Ft">Nominal tangential load (N)</param>
    /// <param name="b">Face width (mm)</param>
    /// <param name="KA">Application factor</param>
    /// <param name="KV">Dynamic factor</param>
    /// <param name="cGammaBeta">Mesh stiffness c_gamma_beta (N/(mm*um))</param>
    /// <param name="fsh">Pinion/shaft deflection component (um); 0 = neglected</param>
    /// <param name="fma">Mesh misalignment from manufacturing (um)</param>
    /// <param name="modification">Helix modification per Table 8</param>
    /// <param name="group">Material group for the running-in allowance</param>
    /// <param name="sigmaHlim">Allowable stress number, contact, of the softer material (N/mm2)</param>
    /// <param name="v">Pitch line velocity (m/s)</param>
    /// <param name="bOverH1">Face width to tooth depth ratio of gear 1</param>
    /// <param name="bOverH2">Face width to tooth depth ratio of gear 2</param>
    /// <param name="fHbeta">
    /// Helix slope deviation of the determinant gear (um), for the Equation (56) floor on
    /// F_bx. Pass 0 to skip the floor (which understates K_Hbeta - see below).
    /// </param>
    public static Result Calculate(double Ft, double b, double KA, double KV,
                                   double cGammaBeta, double fsh, double fma,
                                   HelixModification modification, RunInMaterialGroup group,
                                   double sigmaHlim, double v,
                                   double bOverH1, double bOverH2,
                                   double fHbeta = 0)
    {
        var r = new Result();

        if (Ft <= 0 || b <= 0 || cGammaBeta <= 0)
        {
            r.Notes.Add("Invalid input for face load factor; K_Hbeta = K_Fbeta = 1 assumed.");
            return r;
        }

        var (B1, B2) = TableEight(modification);
        r.B1 = B1;
        r.B2 = B2;

        // Mean load intensity F_m/b, with F_m = Ft * KA * KV
        double FmOverB = Ft * KA * KV / b;
        r.FmOverB = FmOverB;

        // Eq. (52): initial equivalent misalignment, for a contact pattern whose size and
        // suitability are not proven. The alternative Eq. (53) is the compensatory form,
        // which is only permitted once a favourable contact pattern has been verified;
        // this calculator cannot verify that, so it always takes the additive branch.
        double Fbx = 1.33 * B1 * fsh + B2 * fma;
        r.FbxCalculated = Fbx;

        // Eq. (55), (56): both Eq. (52) and Eq. (53) carry "F_bx >= F_bx,min". Omitting
        // that floor lets a nominally perfect gear report an unrealistically low K_Hbeta.
        r.FbxMin = Iso6336ShaftDeflection.MinimumMisalignment(FmOverB, fHbeta);
        if (fHbeta > 0 && r.FbxMin > Fbx)
        {
            Fbx = r.FbxMin;
            r.FbxFloored = true;
            r.Notes.Add($"Initial equivalent misalignment was raised to the ISO 6336-1 Eq. (56) minimum of " +
                        $"{r.FbxMin:F2} um (the calculated value was {r.FbxCalculated:F2} um).");
        }

        r.Fbx = Fbx;

        if (fsh <= 0)
        {
            r.Notes.Add("Shaft deflection f_sh was taken as 0 (rigid shaft). A real pinion shaft deflects and increases K_Hbeta - calculate it from the shaft dimensions, or enter a value from a shaft analysis.");
        }

        if (Fbx <= 0)
        {
            r.Notes.Add("Initial equivalent misalignment is zero; K_Hbeta = 1 assumed.");
            r.KHbeta = 1.0;
            r.KFbeta = 1.0;
            return r;
        }

        // Eq. (44)-(48): running-in allowance, then Eq. (43)
        double yBeta = RunningInAllowanceBeta(group, Fbx, sigmaHlim, v);
        r.yBeta = yBeta;
        double Fby = Math.Max(0, Fbx - yBeta);                       // Eq. (43)
        r.Fby = Fby;

        // Eq. (39) / (41): the branch depends on whether contact spans the full face
        double ratio = Fby * cGammaBeta / (2.0 * FmOverB);
        if (ratio >= 1.0)
        {
            // b_cal/b <= 1: contact does not extend across the full face width
            r.KHbeta = Math.Sqrt(2.0 * Fby * cGammaBeta / FmOverB);  // Eq. (39)
            r.BcalOverB = Math.Sqrt(2.0 * FmOverB / (Fby * cGammaBeta)); // Eq. (40)
            r.FullFaceContact = false;
            if (r.KHbeta < 2.0) r.KHbeta = 2.0;                      // Eq. (39) states >= 2
        }
        else
        {
            // b_cal/b > 1: contact across the full face width
            r.KHbeta = 1.0 + ratio;                                  // Eq. (41)
            r.BcalOverB = 0.5 + FmOverB / (Fby * cGammaBeta);        // Eq. (42)
            r.FullFaceContact = true;
        }

        if (r.KHbeta < 1.0) r.KHbeta = 1.0;

        // === K_Fbeta - Clause 7.6, Eq. (69), (70) ===
        // The smaller of b1/h1 and b2/h2 is used; when b/h < 3, substitute 3.
        double bOverH = Math.Min(bOverH1, bOverH2);
        if (bOverH < 3.0) bOverH = 3.0;
        r.BOverH = bOverH;

        double hOverB = 1.0 / bOverH;
        double NF = 1.0 / (1.0 + hOverB + hOverB * hOverB);          // Eq. (70)
        r.NF = NF;
        r.KFbeta = Math.Pow(r.KHbeta, NF);                           // Eq. (69)

        return r;
    }

    /// <summary>Maps an ISO 6336-5 material group to the running-in group of 7.5.2.1.</summary>
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
