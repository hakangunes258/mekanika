namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Internal dynamic factor K_V according to ISO 6336-1:2006, Clause 6, Method B,
/// together with the tooth stiffness parameters of Clause 9.
///
/// Equation numbers in the comments refer to ISO 6336-1:2006. Only the equations and
/// tabulated coefficients are implemented; no text of the standard is reproduced.
///
/// Scope: external spur and helical gear pairs (single mesh). Planetary/idler
/// arrangements (6.4.7) and internal gears are not covered here.
/// </summary>
public static class Iso6336DynamicFactor
{
    /// <summary>Operating range of the gear pair relative to the main resonance.</summary>
    public enum SpeedRange { Subcritical, MainResonance, Intermediate, Supercritical }

    public class Input
    {
        // Geometry
        public int z1 { get; set; }
        public int z2 { get; set; }
        public double mn { get; set; }          // mm
        public double b { get; set; }           // face width, mm
        public double beta { get; set; }        // helix angle, deg
        public double alphaN { get; set; } = 20;// normal pressure angle, deg
        public double x1 { get; set; }
        public double x2 { get; set; }
        public double d1 { get; set; }          // reference diameter, mm
        public double d2 { get; set; }
        public double da1 { get; set; }         // tip diameter, mm
        public double da2 { get; set; }
        public double df1 { get; set; }         // root diameter, mm
        public double df2 { get; set; }
        public double di1 { get; set; }         // bore diameter, mm (0 = solid)
        public double di2 { get; set; }
        public double db1 { get; set; }         // base diameter, mm
        public double epsilonAlpha { get; set; }
        public double epsilonGamma { get; set; }

        // Basic rack (for CB) - ISO 53 profile A: hfP = 1.25 mn, alphaPn = 20 deg
        public double hfP { get; set; }         // mm
        public double alphaPn { get; set; } = 20; // deg

        // Loading
        public double Ft { get; set; }          // nominal tangential force at reference cylinder, N
        public double KA { get; set; } = 1.0;
        public double n1 { get; set; }          // pinion speed, min^-1

        // Material
        public double rho1 { get; set; } = 7.85e-6; // kg/mm3
        public double rho2 { get; set; } = 7.85e-6;
        public double E1 { get; set; } = 206000;    // MPa
        public double E2 { get; set; } = 206000;

        // Gear blank: solid disc by default -> CR = 1,0  (Eq. 84)
        public bool SolidDiscGears { get; set; } = true;
        public double bs { get; set; }          // web thickness, mm (Eq. 85)
        public double sR { get; set; }          // rim thickness, mm (Eq. 85)

        // --- Deviations (micrometres) ---
        // f_pb  : base pitch deviation, f_falpha : profile form deviation.
        // ISO 6336-1 takes these from the WHEEL (the larger values) - see 6.4.3.
        // They originate from ISO 1328-1; supply them explicitly.
        public double fpb { get; set; }
        public double ffalpha { get; set; }
        // Running-in allowances (Eq. 18, 19). Defaulting to 0 is CONSERVATIVE:
        // it keeps the effective deviations at their full value and raises K_V.
        public double yp { get; set; }
        public double yf { get; set; }
        // Design tip relief Ca (micrometres). When the gear has no specified profile
        // modification, Cay from Table 4 is substituted - set UseCayForCa = true.
        public double Ca { get; set; }
        public bool UseCayForCa { get; set; } = true;
        public double sigmaHlim1 { get; set; }  // MPa, for Cay
        public double sigmaHlim2 { get; set; }
        /// <summary>Per footnote 4 of 6.4.3: for accuracy grades 6 to 12, Bk = 1,0.</summary>
        public bool ForceBkUnity { get; set; }
    }

    public class Result
    {
        public double KV { get; set; }
        public SpeedRange Range { get; set; }
        public double N { get; set; }           // resonance ratio
        public double NS { get; set; }          // lower limit of main resonance range
        public double nE1 { get; set; }         // resonance running speed, min^-1
        public double cPrime { get; set; }      // single tooth stiffness, N/(mm*um)
        public double cGammaAlpha { get; set; } // mesh stiffness, N/(mm*um)
        public double cGammaBeta { get; set; }  // N/(mm*um)
        public double mred { get; set; }        // reduced mass, kg/mm
        public double Bp { get; set; }
        public double Bf { get; set; }
        public double Bk { get; set; }
        public double K { get; set; }
        public double Cay { get; set; }
        public double SpecificLoad { get; set; } // Ft*KA/b, N/mm
        public bool Valid { get; set; }
        public List<string> Warnings { get; } = new();
    }

    // Table 9 - coefficients for Equation (82)
    private const double C1 = 0.047_23, C2 = 0.155_51, C3 = 0.257_91,
                         C4 = -0.006_35, C5 = -0.116_54, C6 = -0.001_93,
                         C7 = -0.241_88, C8 = 0.005_29, C9 = 0.001_82;

    public static Result Calculate(Input i)
    {
        var r = new Result();

        if (i.z1 <= 0 || i.z2 <= 0 || i.mn <= 0 || i.b <= 0 || i.Ft <= 0 || i.n1 <= 0 || i.db1 <= 0)
        {
            r.Warnings.Add("Invalid input for dynamic factor calculation.");
            r.KV = 1.0;
            return r;
        }

        double betaRad = i.beta * Math.PI / 180.0;
        double alphaNRad = i.alphaN * Math.PI / 180.0;

        // Specific loading Ft*KA/b (N/mm) - governs several branches
        double w = i.Ft * i.KA / i.b;
        r.SpecificLoad = w;

        // === Virtual tooth numbers (for Eq. 82) ===
        double betaB = Math.Asin(Math.Sin(betaRad) * Math.Cos(alphaNRad));
        double cosBetaB = Math.Cos(betaB);
        double zn1 = i.z1 / (cosBetaB * cosBetaB * Math.Cos(betaRad));
        double zn2 = i.z2 / (cosBetaB * cosBetaB * Math.Cos(betaRad));

        // === Single tooth stiffness c' (Clause 9.3.1) ===
        // Eq. (82): minimum flexibility of a pair of teeth
        double qPrime = C1
                      + C2 / zn1 + C3 / zn2
                      + C4 * i.x1 + C5 * i.x1 / zn1
                      + C6 * i.x2 + C7 * i.x2 / zn2
                      + C8 * i.x1 * i.x1 + C9 * i.x2 * i.x2;

        if (qPrime <= 0)
        {
            r.Warnings.Add("Tooth flexibility q' (Eq. 82) is non-positive - geometry outside the validity range.");
            r.KV = 1.0;
            return r;
        }

        double cth = 1.0 / qPrime;                                   // Eq. (81)

        // Validity note of Eq. (81)/(82): x1 >= x2 and -0,5 <= x1+x2 <= 2,0
        double sumX = i.x1 + i.x2;
        if (sumX < -0.5 || sumX > 2.0)
            r.Warnings.Add($"Sum of profile shifts {sumX:F2} is outside the validity range (-0.5 to 2.0) of ISO 6336-1 Eq. (82).");

        const double CM = 0.8;                                       // Eq. (83)

        // Gear blank factor CR - Eq. (84) solid disc, Eq. (85) webbed
        double CR = 1.0;
        if (!i.SolidDiscGears && i.b > 0 && i.mn > 0 && i.sR > 0 && i.bs > 0)
        {
            double bsOverB = Math.Max(0.2, Math.Min(1.2, i.bs / i.b));        // boundary conditions
            double sROverMn = Math.Max(1.0, i.sR / i.mn);
            CR = 1.0 + Math.Log(bsOverB) / (5.0 * Math.Exp(sROverMn / 5.0));  // Eq. (85)
        }

        // Basic rack factor CB - Eq. (86)
        // The reference basic rack of Eq. (86) has h_fP = 1,2 m_n, not 1,25. With 1,25 in the
        // bracket the factor collapsed to exactly 1,000 for the ISO 53 profile A rack that
        // nearly every gear here uses - i.e. the factor did nothing for the common case. The
        // correct value there is 0,975, which is what commercial software reports for the same
        // rack. c' and c_gamma were ~2,5 % high as a result, carrying into K_V, K_Hbeta, K_Halpha.
        double hfPOverMn = i.hfP > 0 ? i.hfP / i.mn : 1.25;
        double CB = (1.0 + 0.5 * (1.2 - hfPOverMn)) * (1.0 - 0.02 * (20.0 - i.alphaPn));

        double cPrime = cth * CM * CR * CB * Math.Cos(betaRad);      // Eq. (80)

        // Low specific load correction - Eq. (90)
        if (w < 100.0)
            cPrime = cth * CM * CB * CR * Math.Cos(betaRad) * Math.Pow(w / 100.0, 0.25);

        // Material combination other than steel/steel - Eq. (88), (89)
        const double ESt = 206000.0;
        if (Math.Abs(i.E1 - ESt) > 1 || Math.Abs(i.E2 - ESt) > 1)
        {
            double E = 2.0 * i.E1 * i.E2 / (i.E1 + i.E2);            // Eq. (89)
            cPrime *= E / ESt;                                       // Eq. (88)
        }

        r.cPrime = cPrime;

        // === Mesh stiffness (Clause 9.3.2) ===
        double cGammaAlpha = cPrime * (0.75 * i.epsilonAlpha + 0.25); // Eq. (91)
        r.cGammaAlpha = cGammaAlpha;
        r.cGammaBeta = 0.85 * cGammaAlpha;                            // Eq. (92)

        if (i.epsilonAlpha < 1.2)
            r.Warnings.Add("Transverse contact ratio < 1,2: c_gamma_alpha from Eq. (91) can be up to 10 % high.");

        // === Reduced mass (6.4.8, Eq. 30-32) ===
        double dm1 = (i.da1 + i.df1) / 2.0;                           // Eq. (31)
        double dm2 = (i.da2 + i.df2) / 2.0;
        // Eq. (32) as printed reads q1 = di1/dm1 ; q2 = di1/dm2. Using di2 for the
        // wheel is the physically consistent reading (each gear's own bore); for the
        // usual solid gears (di = 0) both readings coincide.
        double q1 = dm1 > 0 ? i.di1 / dm1 : 0;
        double q2 = dm2 > 0 ? i.di2 / dm2 : 0;
        double u = (double)i.z2 / i.z1;

        double term1 = (1.0 - Math.Pow(q1, 4)) * i.rho1;
        double term2 = (1.0 - Math.Pow(q2, 4)) * i.rho2 * u * u;
        if (term1 <= 0 || term2 <= 0)
        {
            r.Warnings.Add("Reduced mass could not be evaluated (check bore diameters).");
            r.KV = 1.0;
            return r;
        }

        double mred = (Math.PI / 8.0) * Math.Pow(dm1 / i.db1, 2)
                    * (dm1 * dm1) / (1.0 / term1 + 1.0 / term2);      // Eq. (30)
        r.mred = mred;

        // === Resonance speed and ratio (6.4.2) ===
        double nE1 = (30000.0 / (Math.PI * i.z1)) * Math.Sqrt(cGammaAlpha / mred); // Eq. (6)
        r.nE1 = nE1;
        double N = i.n1 / nE1;                                        // Eq. (9)
        r.N = N;

        // Lower limit of the main resonance range - Eq. (11), (12)
        double NS = w < 100.0 ? 0.5 + 0.35 * Math.Sqrt(w / 100.0) : 0.85;
        r.NS = NS;

        // === Cay (Table 4) and effective deviations ===
        double CayOf(double sigmaHlim) => (1.0 / 18.0) * Math.Pow(sigmaHlim / 97.0 - 18.45, 2) + 1.5;
        double Cay = 0;
        if (i.sigmaHlim1 > 0 && i.sigmaHlim2 > 0)
            Cay = 0.5 * (CayOf(i.sigmaHlim1) + CayOf(i.sigmaHlim2)); // NOTE under Table 4
        else if (i.sigmaHlim1 > 0)
            Cay = CayOf(i.sigmaHlim1);
        r.Cay = Cay;

        double fpbEff = Math.Max(0, i.fpb - i.yp);                    // Eq. (18)
        double ffaEff = Math.Max(0, i.ffalpha - i.yf);                // Eq. (19)

        // Ca: design tip relief; Cay substituted when no profile modification specified
        double Ca = i.UseCayForCa ? Cay : i.Ca;

        // === B factors (Eq. 15-17) ===
        r.Bp = cPrime * fpbEff / w;                                   // Eq. (15)
        r.Bf = cPrime * ffaEff / w;                                   // Eq. (16)
        r.Bk = i.ForceBkUnity ? 1.0 : Math.Abs(1.0 - cPrime * Ca / w);// Eq. (17) + footnote 4

        // === Cv factors (Table 4) ===
        double eg = i.epsilonGamma;
        double Cv1 = 0.32;
        double Cv2 = eg <= 2.0 ? 0.34 : 0.57 / (eg - 0.3);
        double Cv3 = eg <= 2.0 ? 0.23 : 0.096 / (eg - 1.56);
        double Cv4 = eg <= 2.0 ? 0.90 : (0.57 - 0.05 * eg) / (eg - 1.44);
        double Cv5 = 0.47;
        double Cv6 = eg <= 2.0 ? 0.47 : 0.12 / (eg - 1.74);
        double Cv7 = eg <= 1.5 ? 0.75
                   : eg <= 2.5 ? 0.125 * Math.Sin(Math.PI * (eg - 2.0)) + 0.875
                   : 1.0;

        // === K_V by speed range ===
        double KvSub()   { double K = Cv1 * r.Bp + Cv2 * r.Bf + Cv3 * r.Bk; r.K = K; return N * K + 1.0; }   // Eq. (13),(14)
        double KvMain()  => Cv1 * r.Bp + Cv2 * r.Bf + Cv4 * r.Bk + 1.0;                                      // Eq. (20)
        double KvSuper() => Cv5 * r.Bp + Cv6 * r.Bf + Cv7;                                                   // Eq. (21)

        if (N <= NS)
        {
            r.Range = SpeedRange.Subcritical;
            r.KV = KvSub();
        }
        else if (N <= 1.15)
        {
            r.Range = SpeedRange.MainResonance;
            r.KV = KvMain();
            r.Warnings.Add("Operating in the main resonance range - this should be avoided; ISO 6336-1 recommends refined analysis by Method A (the real K_V can deviate by up to 40 %).");
        }
        else if (N < 1.5)
        {
            r.Range = SpeedRange.Intermediate;
            // Eq. (22): linear interpolation between K_V at N = 1,15 and N = 1,5
            double kv115 = KvMain();
            double kv15 = KvSuper();
            r.KV = kv15 + (kv115 - kv15) / 0.35 * (1.5 - N);
            r.Warnings.Add("Operating in the intermediate range - ISO 6336-1 recommends refined analysis by Method A.");
        }
        else
        {
            r.Range = SpeedRange.Supercritical;
            r.KV = KvSuper();
        }

        if (r.KV < 1.0) r.KV = 1.0;
        r.Valid = true;
        return r;
    }
}
