namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Surface durability influence factors according to ISO 6336-2:2006:
///   - single pair tooth contact factors Z_B and Z_D (Clause 6)
///   - elasticity factor Z_E (Clause 7)
///   - lubricant film factors Z_L, Z_v, Z_R (Clause 12, Method B)
///
/// Equation numbers in the comments refer to ISO 6336-2:2006. Only equations and
/// tabulated coefficients are implemented; no text of the standard is reproduced.
/// </summary>
public static class Iso6336SurfaceFactors
{
    // ================= Single pair tooth contact factors Z_B / Z_D =================

    public class ContactFactors
    {
        public double ZB { get; set; } = 1.0;
        public double ZD { get; set; } = 1.0;
        public double M1 { get; set; }
        public double M2 { get; set; }
        public string? Warning { get; set; }
    }

    /// <summary>
    /// Z_B and Z_D per ISO 6336-2 Clause 6.2 (for eps_alpha &lt;= 2).
    /// </summary>
    /// <param name="z1">Pinion teeth</param>
    /// <param name="z2">Wheel teeth</param>
    /// <param name="da1">Pinion tip diameter (mm)</param>
    /// <param name="db1">Pinion base diameter (mm)</param>
    /// <param name="da2">Wheel tip diameter (mm)</param>
    /// <param name="db2">Wheel base diameter (mm)</param>
    /// <param name="alphaWt">Working transverse pressure angle (degrees)</param>
    /// <param name="epsilonAlpha">Transverse contact ratio</param>
    /// <param name="epsilonBeta">Overlap ratio</param>
    public static ContactFactors CalculateZBZD(double z1, double z2,
        double da1, double db1, double da2, double db2,
        double alphaWt, double epsilonAlpha, double epsilonBeta)
    {
        var r = new ContactFactors();

        if (z1 <= 0 || z2 <= 0 || db1 <= 0 || db2 <= 0 || da1 <= db1 || da2 <= db2 || epsilonAlpha <= 0)
        {
            r.Warning = "Invalid geometry for Z_B / Z_D.";
            return r;
        }

        double tanAlphaWt = Math.Tan(alphaWt * Math.PI / 180.0);

        // Auxiliary terms of Eq. (17) and (18)
        double t1 = Math.Sqrt(da1 * da1 / (db1 * db1) - 1.0);
        double t2 = Math.Sqrt(da2 * da2 / (db2 * db2) - 1.0);
        double twoPiZ1 = 2.0 * Math.PI / z1;
        double twoPiZ2 = 2.0 * Math.PI / z2;

        // Eq. (17): M1
        double m1Den = (t1 - twoPiZ1) * (t2 - (epsilonAlpha - 1.0) * twoPiZ2);
        // Eq. (18): M2
        double m2Den = (t2 - twoPiZ2) * (t1 - (epsilonAlpha - 1.0) * twoPiZ1);

        r.M1 = m1Den > 0 ? tanAlphaWt / Math.Sqrt(m1Den) : 0;
        r.M2 = m2Den > 0 ? tanAlphaWt / Math.Sqrt(m2Den) : 0;

        if (epsilonAlpha > 2.0)
        {
            // 6.3: high precision gears with 2 < eps_alpha <= 2,5 - contact stress is
            // based on the inner point of two pair tooth contact; not covered here.
            r.ZB = 1.0;
            r.ZD = 1.0;
            r.Warning = "Transverse contact ratio > 2: ISO 6336-2, 6.3 applies (inner point of two pair tooth contact); Z_B = Z_D = 1 is used here.";
            return r;
        }

        if (epsilonAlpha <= 1.0)
        {
            r.Warning = "Transverse contact ratio <= 1 is not covered by ISO 6336-2, 6.2.";
            return r;
        }

        bool isSpur = Math.Abs(epsilonBeta) < 1e-9;

        if (isSpur)
        {
            // a) Spur gears with eps_alpha > 1
            r.ZB = r.M1 > 1.0 ? r.M1 : 1.0;
            r.ZD = r.M2 > 1.0 ? r.M2 : 1.0;
        }
        else if (epsilonBeta >= 1.0)
        {
            // b) Helical gears with eps_beta >= 1
            r.ZB = 1.0;
            r.ZD = 1.0;
        }
        else
        {
            // c) Helical gears with eps_beta < 1 - linear interpolation
            r.ZB = Math.Max(1.0, r.M1 - epsilonBeta * (r.M1 - 1.0));
            r.ZD = Math.Max(1.0, r.M2 - epsilonBeta * (r.M2 - 1.0));
        }

        // 6.2 note: Z_D should only be determined when u < 1,5; above that M2 is
        // usually < 1 and Z_D is set to 1,0.
        double u = z2 / z1;
        if (u > 1.5 && r.ZD > 1.0) r.ZD = 1.0;

        return r;
    }

    // ================= Elasticity factor Z_E =================

    /// <summary>Elasticity factor Z_E - ISO 6336-2 Eq. (19). E in MPa, result in sqrt(N/mm^2).</summary>
    public static double CalculateZE(double E1, double nu1, double E2, double nu2)
    {
        double term = (1.0 - nu1 * nu1) / E1 + (1.0 - nu2 * nu2) / E2;
        return term > 0 ? Math.Sqrt(1.0 / (Math.PI * term)) : 0;
    }

    // ================= Lubricant film factors Z_L, Z_v, Z_R =================

    public class LubricantFilmFactors
    {
        public double ZL { get; set; } = 1.0;
        public double Zv { get; set; } = 1.0;
        public double ZR { get; set; } = 1.0;
        public double CZL { get; set; }
        public double CZv { get; set; }
        public double CZR { get; set; }
        public double Rz { get; set; }       // mean peak-to-valley roughness, um
        public double Rz10 { get; set; }     // mean relative roughness, um
        public double RhoRed { get; set; }   // radius of relative curvature, mm
        public string? Warning { get; set; }
    }

    /// <summary>
    /// Z_L, Z_v and Z_R per ISO 6336-2 Clause 12.3 (Method B).
    ///
    /// All three are evaluated with the allowable stress number of the SOFTER of the
    /// two materials, as required by 12.3.
    /// </summary>
    /// <param name="sigmaHlimSofter">sigma_Hlim of the softer material (N/mm2)</param>
    /// <param name="nu40">Nominal lubricant viscosity at 40 C (mm2/s)</param>
    /// <param name="v">Pitch line velocity (m/s)</param>
    /// <param name="Rz1">Pinion flank peak-to-valley roughness after running-in (um)</param>
    /// <param name="Rz2">Wheel flank peak-to-valley roughness after running-in (um)</param>
    /// <param name="db1">Pinion base diameter (mm)</param>
    /// <param name="db2">Wheel base diameter (mm)</param>
    /// <param name="alphaWt">Working transverse pressure angle (degrees)</param>
    public static LubricantFilmFactors CalculateLubricantFilm(
        double sigmaHlimSofter, double nu40, double v,
        double Rz1, double Rz2, double db1, double db2, double alphaWt)
    {
        var r = new LubricantFilmFactors();

        if (sigmaHlimSofter <= 0 || nu40 <= 0 || v <= 0 || db1 <= 0 || db2 <= 0)
        {
            r.Warning = "Invalid input for lubricant film factors; Z_L = Z_v = Z_R = 1 assumed.";
            return r;
        }

        // --- C_ZL, Eq. (38), (39), (40) ---
        double CZL = sigmaHlimSofter < 850 ? 0.83
                   : sigmaHlimSofter > 1200 ? 0.91
                   : sigmaHlimSofter / 4375.0 + 0.6357;
        r.CZL = CZL;

        // --- Z_L, Eq. (37) using the nu40 form ---
        double den = 1.2 + 134.0 / nu40;
        r.ZL = CZL + 4.0 * (1.0 - CZL) / (den * den);

        // --- Z_v, Eq. (42), (43) ---
        double CZv = CZL + 0.02;
        r.CZv = CZv;
        r.Zv = CZv + 2.0 * (1.0 - CZv) / Math.Sqrt(0.8 + 32.0 / v);

        // --- Radius of relative curvature, Eq. (46), (47) ---
        double tanAlphaWt = Math.Tan(alphaWt * Math.PI / 180.0);
        double rho1 = 0.5 * db1 * tanAlphaWt;
        double rho2 = 0.5 * db2 * tanAlphaWt;
        double rhoRed = (rho1 + rho2) != 0 ? rho1 * rho2 / (rho1 + rho2) : 0;
        r.RhoRed = rhoRed;

        // --- Roughness, Eq. (44), (45) ---
        double Rz = (Rz1 + Rz2) / 2.0;
        r.Rz = Rz;

        if (rhoRed <= 0 || Rz <= 0)
        {
            r.ZR = 1.0;
            r.Warning = "Roughness factor Z_R could not be evaluated; Z_R = 1 assumed.";
            return r;
        }

        double Rz10 = Rz * Math.Cbrt(10.0 / rhoRed);
        r.Rz10 = Rz10;

        // --- C_ZR, Eq. (49), (50), (51) ---
        double CZR = sigmaHlimSofter < 850 ? 0.15
                   : sigmaHlimSofter > 1200 ? 0.08
                   : 0.32 - 0.0002 * sigmaHlimSofter;
        r.CZR = CZR;

        // --- Z_R, Eq. (48) ---
        r.ZR = Math.Pow(3.0 / Rz10, CZR);

        return r;
    }

    /// <summary>
    /// Nominal viscosity at 40 C for the ISO viscosity grades of ISO 6336-2 Table 3.
    /// </summary>
    public static readonly Dictionary<string, double> ViscosityGrades = new()
    {
        ["VG 32"] = 32,
        ["VG 46"] = 46,
        ["VG 68"] = 68,
        ["VG 100"] = 100,
        ["VG 150"] = 150,
        ["VG 220"] = 220,
        ["VG 320"] = 320
    };

    /// <summary>Ra to Rz conversion of ISO 6336-2, 12.3.1.3.1 footnote 3: Ra = Rz/6.</summary>
    public static double RaToRz(double ra) => ra * 6.0;
}
