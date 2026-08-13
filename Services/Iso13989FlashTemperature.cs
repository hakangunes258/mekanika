using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanicalCalculatorWeb.Services;

/// <summary>How the oil reaches the mesh. ISO/TR 13989-1 Eq. (22), factor X_S.</summary>
public enum LubricationMethod
{
    /// <summary>Injected spray. Heat transfer into the oil is impeded, so the bulk runs hotter.</summary>
    Spray,
    /// <summary>Dip (splash) lubrication, and meshes with an additional cooling spray.</summary>
    Dip,
    /// <summary>Gears submerged in oil, provided the cooling is sufficient.</summary>
    Submerged
}

/// <summary>
/// Base oil type. Sets the lubricant factor X_L of ISO/TR 13989-1 Eq. (27), which enters
/// both the coefficient of friction and the scuffing temperature.
/// </summary>
public enum LubricantType
{
    Mineral,
    PolyglycolWaterSoluble,
    PolyglycolNonWaterSoluble,
    Polyalphaolefin,
    PhosphateEster,
    TractionFluid
}

/// <summary>
/// Scuffing load capacity by the flash temperature method — ISO/TR 13989-1:2000.
///
/// Scope of this implementation: EXTERNAL cylindrical gears, spur and helical, with
/// unmodified profiles or a stated tip relief. Bevel and hypoid gears (Clauses 9.8, 9.9 and
/// Annex A.4) are not implemented; neither is the internal-pair geometry factor Eq. (A.7).
///
/// Blok's concept: the contact temperature is the bulk temperature plus a flash temperature
/// that varies along the path of contact, and scuffing is a temperature limit rather than a
/// fatigue process — one overload can do it.
///
///     Θ_B(Γ) = Θ_Mi + Θ_fl(Γ)                        Eq. (1)
///     Θ_fl   = μ_m X_M X_J X_G (X_Γ w_Bt)^0.75 √v_t / a^0.25    Eq. (A.5)
///     S_B    = (Θ_S − Θ_oil) / (Θ_Bmax − Θ_oil)      Eq. (100)
///
/// Eq. (A.5) is the form adapted for cylindrical gears; the general Eq. (5) is implemented
/// alongside it as <see cref="Result.FlashMaxAlternate"/> purely as a cross-check, because
/// the two are algebraically the same expression written in different variables and must
/// agree to within the rounding of their published constants.
///
/// NOTE ON THE SAFETY FACTOR. Clause 10.5 warns that a safety expressed as a quotient of
/// temperatures "may cause confusion", and advises also stating the margin Θ_S − Θ_Bmax
/// directly, for instance ≥ 50 K. Both are reported.
/// </summary>
public static class Iso13989FlashTemperature
{
    public class Input
    {
        // --- geometry ---
        public double mn { get; set; }              // normal module (mm)
        public double alphaN { get; set; } = 20;    // normal pressure angle (deg)
        public double beta { get; set; }            // helix angle (deg)
        public double alphaT { get; set; }          // transverse pressure angle (deg)
        public double alphaWt { get; set; }         // working transverse pressure angle (deg)
        public double betaB { get; set; }           // base helix angle (deg)
        public double a { get; set; }               // centre distance (mm)
        public double u { get; set; }               // gear ratio z2/z1
        public int z1 { get; set; }
        public int z2 { get; set; }
        public double d1 { get; set; }              // reference diameters (mm)
        public double d2 { get; set; }
        public double da1 { get; set; }             // tip diameters (mm)
        public double da2 { get; set; }
        public double b { get; set; }               // face width, smaller of the two (mm)
        public double epsilonAlpha { get; set; }    // transverse contact ratio
        public double epsilonBeta { get; set; }     // overlap ratio
        public double epsilonGamma { get; set; }    // total contact ratio

        // --- load ---
        public double Ft { get; set; }              // nominal tangential force (N)
        public double vt { get; set; }              // pitch line velocity (m/s)
        public double KA { get; set; } = 1.0;
        public double KV { get; set; } = 1.0;
        public double KHbeta { get; set; } = 1.0;   // = K_Bbeta, Eq. (14)
        public double KHalpha { get; set; } = 1.0;  // = K_Balpha, Eq. (15)
        public double Kmp { get; set; } = 1.0;      // multiple path factor, 1 for a simple pair

        /// <summary>True when the pinion drives (speed reducing). Decides which end of the
        /// path of contact the approach factor applies to, Eq. (45)-(48).</summary>
        public bool PinionDrives { get; set; } = true;

        // --- material ---
        public double E1 { get; set; } = 206000;    // N/mm²
        public double E2 { get; set; } = 206000;
        public double nu1 { get; set; } = 0.3;
        public double nu2 { get; set; } = 0.3;
        /// <summary>Thermal contact coefficient B_M, N/(mm^½·m^½·s^½·K). 435 is the standard's
        /// average for martensitic steels, Annex A.3.</summary>
        public double BM { get; set; } = 435;
        /// <summary>Structural factor X_W, Table 2.</summary>
        public double XW { get; set; } = 1.0;

        // --- surface ---
        public double Ra1 { get; set; }             // flank roughness Ra (µm)
        public double Ra2 { get; set; }
        /// <summary>Flank tolerance class, for the load sharing factor Q of Eq. (60).</summary>
        public int QualityGrade { get; set; } = 6;

        // --- profile modification ---
        public double Ca1 { get; set; }             // tip relief of pinion (µm), 0 = unmodified
        public double Ca2 { get; set; }
        /// <summary>Mesh stiffness c_gamma, N/(mm·µm), for the optimal tip relief of Eq. (B.1).</summary>
        public double cGamma { get; set; } = 20;

        // --- lubricant ---
        public double OilTemperature { get; set; } = 70;   // Θ_oil (°C)
        public double EtaOil { get; set; }                 // dynamic viscosity at Θ_oil (mPa·s)
        public LubricantType Lubricant { get; set; } = LubricantType.Mineral;
        public LubricationMethod Method { get; set; } = LubricationMethod.Dip;
        /// <summary>FZG A/8,3/90 load stage at which scuffing occurs, Eq. (99).</summary>
        public double FzgLoadStage { get; set; } = 12;
        /// <summary>True for oils with anti-scuff additives: Clause 10.3, X_Θ = 18 K/µs, t_c = 18 µs.</summary>
        public bool AntiScuffAdditives { get; set; }
    }

    public class Result
    {
        public bool Valid { get; set; }

        public double MuM { get; set; }             // mean coefficient of friction
        public double XL { get; set; }              // lubricant factor
        public double XR { get; set; }              // roughness factor
        public double XM { get; set; }              // thermo-elastic factor
        public double XS { get; set; }              // lubrication method factor
        public double XalphaBeta { get; set; }      // angle factor
        public double wBt { get; set; }             // transverse unit load (N/mm)
        public double Ceff { get; set; }            // optimal tip relief (µm)

        public double GammaA { get; set; }
        public double GammaE { get; set; }

        public double FlashMax { get; set; }        // Θ_flmax (K)
        public double FlashMean { get; set; }       // Θ_flm (K)
        public double GammaAtMax { get; set; }      // where the maximum sits on the path
        public double FlashMaxAlternate { get; set; } // Θ_flmax via Eq. (5), cross-check only

        public double BulkTemperature { get; set; }    // Θ_M (°C)
        public double ContactMax { get; set; }         // Θ_Bmax (°C)
        public double ScuffingTemperature { get; set; } // Θ_S (°C)
        public double SafetyFactor { get; set; }        // S_B
        public double MarginKelvin { get; set; }        // Θ_S − Θ_Bmax (K)

        public double ContactExposureTime { get; set; } // t_max (µs)

        /// <summary>Θ_fl sampled along the path, for a chart or a table.</summary>
        public List<(double Gamma, double Flash, double Contact)> Profile { get; } = new();

        public List<string> Notes { get; } = new();
    }

    /// <summary>
    /// Structural factor X_W, Table 2, from the ISO 6336-5 material classification.
    ///
    /// Table 2 is written in terms of surface treatment rather than the ISO 6336-5 groups, so
    /// this is a mapping, not a lookup. Case-hardened steel is taken at the "average austenite
    /// content (10 % to 20 %)" row, X_W = 1,00, because the austenite content is not something
    /// this module asks for; a gear known to sit outside that band needs the value overridden.
    /// Phosphated and copper-plated steels (1,25 and 1,50) are surface treatments the material
    /// list does not distinguish, so they are only reachable by overriding.
    /// </summary>
    public static double StructuralFactor(GearMaterialType type) => type switch
    {
        GearMaterialType.NitridedNitridingSteel => 1.50,
        GearMaterialType.NitridedThroughHardeningSteel => 1.50,
        GearMaterialType.Nitrocarburized => 1.50,
        _ => 1.00
    };

    /// <summary>Lubricant factor X_L, Eq. (27). eta in mPa·s at the oil temperature.</summary>
    public static double LubricantFactor(LubricantType type, double etaOil)
    {
        if (etaOil <= 0) return 0;

        double coefficient = type switch
        {
            LubricantType.PolyglycolWaterSoluble => 0.6,
            LubricantType.PolyglycolNonWaterSoluble => 0.7,
            LubricantType.Polyalphaolefin => 0.8,
            LubricantType.PhosphateEster => 1.3,
            LubricantType.TractionFluid => 1.5,
            _ => 1.0
        };
        return coefficient * Math.Pow(etaOil, -0.05);
    }

    /// <summary>
    /// Kinematic viscosity at an arbitrary temperature from two datasheet points, by the
    /// Walther / ASTM D341 relation
    ///     log10(log10(nu + 0.7)) = A − B log10(T)      T in kelvin, nu in mm²/s
    /// which is the standard way to get the viscosity at the oil temperature from the two
    /// numbers every oil datasheet carries.
    /// </summary>
    public static double ViscosityAt(double nu40, double nu100, double temperatureC)
    {
        if (nu40 <= 0 || nu100 <= 0) return 0;

        double W(double nu) => Math.Log10(Math.Log10(nu + 0.7));
        double t40 = Math.Log10(40 + 273.15);
        double t100 = Math.Log10(100 + 273.15);

        double slope = (W(nu40) - W(nu100)) / (t100 - t40);      // = B
        double aConst = W(nu40) + slope * t40;                   // = A

        double w = aConst - slope * Math.Log10(temperatureC + 273.15);
        double inner = Math.Pow(10, Math.Pow(10, w)) - 0.7;
        return inner > 0 ? inner : 0;
    }

    /// <summary>
    /// Typical ν100 for a mineral gear oil of the given ISO VG grade, used only as the
    /// starting value of an input the user can override. Derived from a viscosity index of
    /// about 95, which is representative of a plain mineral gear oil; a synthetic of the same
    /// VG grade has a markedly higher ν100 and should be entered from its datasheet.
    /// </summary>
    public static double TypicalNu100(double nu40)
    {
        if (nu40 <= 0) return 0;
        // Fitted to the VI ~ 95 mineral series: VG 32 -> 5.4, 46 -> 6.8, 68 -> 8.7,
        // 100 -> 11.4, 150 -> 15, 220 -> 19, 320 -> 24, 460 -> 31.
        return 0.264 * Math.Pow(nu40, 0.7644);
    }

    public static Result Calculate(Input i)
    {
        var r = new Result();

        if (i.mn <= 0 || i.a <= 0 || i.b <= 0 || i.z1 <= 0 || i.z2 <= 0 ||
            i.da1 <= 0 || i.da2 <= 0 || i.vt <= 0 || i.Ft <= 0 || i.u <= 0)
        {
            r.Notes.Add("Scuffing was not evaluated: the geometry or the load is incomplete.");
            return r;
        }

        double alphaTr = i.alphaT * Math.PI / 180.0;
        double alphaWtr = i.alphaWt * Math.PI / 180.0;
        double betaBr = i.betaB * Math.PI / 180.0;

        // --- Transverse unit load, Eq. (11) ---
        r.wBt = i.KA * i.KV * i.KHbeta * i.KHalpha * i.Kmp * i.Ft / i.b;

        // --- Parameters on the line of action, Eq. (30)-(35) ---
        double tanAlphaA1 = Math.Sqrt(Math.Max(0, Math.Pow(i.da1 / (i.d1 * Math.Cos(alphaTr)), 2) - 1));
        double tanAlphaA2 = Math.Sqrt(Math.Max(0, Math.Pow(i.da2 / (i.d2 * Math.Cos(alphaTr)), 2) - 1));
        double tanAlphaWt = Math.Tan(alphaWtr);

        if (tanAlphaWt <= 0)
        {
            r.Notes.Add("Scuffing was not evaluated: the working pressure angle is degenerate.");
            return r;
        }

        double gammaA = -(double)i.z2 / i.z1 * (tanAlphaA2 / tanAlphaWt - 1.0);                    // (30)
        double gammaB = tanAlphaA1 / tanAlphaWt - 1.0 - 2.0 * Math.PI / (i.z1 * tanAlphaWt);       // (31)
        double gammaD = -(double)i.z2 / i.z1 * (tanAlphaA2 / tanAlphaWt - 1.0) + 2.0 * Math.PI / (i.z1 * tanAlphaWt); // (32)
        double gammaE = tanAlphaA1 / tanAlphaWt - 1.0;                                             // (33)

        r.GammaA = gammaA;
        r.GammaE = gammaE;

        // --- Thermo-elastic factor, Eq. (A.9)-(A.14) ---
        // With equal thermal contact coefficients this reduces to Er^0.25 / B_M.
        //
        // The factor 1000 is the standard's mixed unit system, not a fudge: Clause 3.2 states
        // that "the units for B_M, c_gamma, X_M are adapted to the mixed application of metre
        // and millimetre". E_r is in N/mm² while B_M is in N/(mm^½·m^½·s^½·K), and the metre
        // against the millimetre is where the 1000 comes from. The anchor is Eq. (A.14):
        // E = 206000 N/mm², nu = 0,3 and B_M = 435 must give X_M = 50,0. Without the 1000 it
        // comes out at 0,05 and the flash temperature lands three orders of magnitude low —
        // which is exactly how this was caught.
        double Er = 2.0 / ((1 - i.nu1 * i.nu1) / i.E1 + (1 - i.nu2 * i.nu2) / i.E2);               // (A.10)
        r.XM = 1000.0 * Math.Pow(Er, 0.25) / i.BM;                                                 // (A.13)

        // --- Angle factor, Eq. (A.8) ---
        //
        // Reproduces Table A.1 to within 1,6 %: the table appears to have been computed with
        // 1,24 where Eq. (A.8) prints 1,22. The split does not matter on its own — footnote 5
        // says the constant was introduced "with no other purpose than to simplify (A.8)",
        // as 0,51 = 0,62/1,22 — so only the product 0,51 · X_alpha_beta in Eq. (A.6) is real,
        // and that is checked against the independent Eq. (5) route instead.
        r.XalphaBeta = 1.22 * Math.Pow(Math.Sin(alphaWtr), 0.25)
                     / Math.Sqrt(Math.Cos(alphaWtr)) * Math.Sqrt(Math.Cos(betaBr));

        // --- Coefficient of friction, method C, Eq. (25)-(28) ---
        double vSigmaC = 2.0 * Math.Min(i.vt, 50.0) * Math.Sin(alphaWtr);                          // (26)
        double rhoRelC = RelativeRadius(0, i.a, i.u, alphaWtr);                                    // (6) at Gamma = 0

        r.XL = LubricantFactor(i.Lubricant, i.EtaOil);
        r.XR = Math.Pow((i.Ra1 + i.Ra2) / 2.0, 0.25);                                              // (28)

        if (vSigmaC <= 0 || rhoRelC <= 0 || r.XL <= 0)
        {
            r.Notes.Add("Scuffing was not evaluated: the sliding velocity, the curvature or the "
                      + "oil viscosity is missing.");
            return r;
        }

        r.MuM = 0.060 * Math.Pow(r.wBt / (vSigmaC * rhoRelC), 0.2) * r.XL * r.XR;                  // (25)

        // --- Optimal tip relief, Eq. (B.1) ---
        double cGamma = i.cGamma > 0 ? i.cGamma : 20.0;
        r.Ceff = i.KA * i.Kmp * i.Ft / (i.b * Math.Cos(alphaTr) * cGamma);

        // --- Sweep the path of contact ---
        const int steps = 200;
        double flashSum = 0;
        double loadFactorAtMax = 0;

        for (int k = 0; k <= steps; k++)
        {
            double gamma = gammaA + (gammaE - gammaA) * k / steps;

            double xGamma = LoadSharingFactor(gamma, gammaA, gammaB, gammaD, gammaE, i, r.Ceff);
            double xJ = ApproachFactor(gamma, gammaA, gammaE, i, r.Ceff);
            double xG = GeometryFactor(gamma, i.u, r.XalphaBeta);

            double flash = xGamma <= 0 ? 0
                : r.MuM * r.XM * xJ * xG
                  * Math.Pow(xGamma * r.wBt, 0.75) * Math.Sqrt(i.vt) / Math.Pow(i.a, 0.25);        // (A.5)

            flashSum += flash;
            if (flash > r.FlashMax)
            {
                r.FlashMax = flash;
                r.GammaAtMax = gamma;
                loadFactorAtMax = xGamma;
            }

            if (k % 5 == 0) r.Profile.Add((gamma, flash, 0));
        }

        r.FlashMean = flashSum / (steps + 1);                                                      // (24)

        // --- Bulk temperature, Eq. (22)-(23) ---
        r.XS = i.Method switch
        {
            LubricationMethod.Spray => 1.2,
            LubricationMethod.Submerged => 0.2,
            _ => 1.0
        };
        double xMp = 1.0;                                                                          // (23), one mating gear
        r.BulkTemperature = i.OilTemperature + 0.47 * r.XS * xMp * r.FlashMean;                    // (22)

        r.ContactMax = r.BulkTemperature + r.FlashMax;                                             // (2)
        for (int p = 0; p < r.Profile.Count; p++)
        {
            var e = r.Profile[p];
            r.Profile[p] = (e.Gamma, e.Flash, r.BulkTemperature + e.Flash);
        }

        // --- Cross-check: the same flash temperature through the general Eq. (5) ---
        r.FlashMaxAlternate = FlashViaEq5(r.GammaAtMax, loadFactorAtMax, i, r);

        // --- Contact exposure time, Eq. (95)-(96) ---
        r.ContactExposureTime = ContactExposureTime(r.GammaAtMax, i, r.wBt, Er);

        // --- Scuffing temperature, Eq. (99), and safety, Eq. (100) ---
        r.ScuffingTemperature = 80.0 + (0.85 + 1.4 * i.XW) * r.XL * i.FzgLoadStage * i.FzgLoadStage;

        if (i.AntiScuffAdditives && r.ContactExposureTime < 18.0)
        {
            // Clause 10.3: with anti-scuff additives a short exposure raises the scuffing
            // temperature, X_Theta = 18 K/us up to t_c = 18 us.
            double lift = 18.0 * i.XW * (18.0 - r.ContactExposureTime);
            r.ScuffingTemperature += lift;
            r.Notes.Add($"Anti-scuff additives with a short contact exposure time "
                      + $"({r.ContactExposureTime:F1} µs < 18 µs) raise the scuffing temperature by "
                      + $"{lift:F0} K, Eq. (97). Clause 10.5 warns that this margin is only valid "
                      + "while the exposure time does not increase.");
        }

        double denominator = r.ContactMax - i.OilTemperature;
        r.SafetyFactor = denominator > 1e-9
            ? (r.ScuffingTemperature - i.OilTemperature) / denominator
            : 999;
        r.MarginKelvin = r.ScuffingTemperature - r.ContactMax;

        // --- Scope and validity notes ---
        if (i.epsilonBeta > 0 && i.epsilonGamma > 2)
        {
            r.Notes.Add("Wide helical gear (ε_γ > 2): the load sharing factor is the mean load "
                      + "1/ε_α with the buttressing factor, Clause 9.6.");
        }

        if (Math.Abs(r.FlashMax - r.FlashMaxAlternate) > 0.05 * Math.Max(r.FlashMax, 1))
        {
            r.Notes.Add($"Internal check: the adapted Eq. (A.5) and the general Eq. (5) differ by "
                      + $"more than 5 % ({r.FlashMax:F1} K against {r.FlashMaxAlternate:F1} K). "
                      + "Treat the result with caution.");
        }

        if (i.FzgLoadStage <= 0)
        {
            r.Notes.Add("No FZG load stage was given, so the scuffing temperature has no basis. "
                      + "Enter the stage from the oil's datasheet.");
        }

        r.Valid = true;
        return r;
    }

    /// <summary>Relative radius of curvature at a point on the path, Eq. (6)-(8).</summary>
    private static double RelativeRadius(double gamma, double a, double u, double alphaWtr)
    {
        double rho1 = (1 + gamma) / (1 + u) * a * Math.Sin(alphaWtr);                               // (7)
        double rho2 = (u - gamma) / (1 + u) * a * Math.Sin(alphaWtr);                               // (8)
        double sum = rho1 + rho2;
        return sum > 1e-12 ? rho1 * rho2 / sum : 0;                                                 // (6)
    }

    /// <summary>
    /// Geometry factor for an external pair, Eq. (A.6). The numerator is the difference of the
    /// square roots that Eq. (5) writes as abs(√ρ_y1 − √(ρ_y2/u)); the two are the same
    /// expression, since (u − Γ)/u = 1 − Γ/u.
    /// </summary>
    private static double GeometryFactor(double gamma, double u, double xAlphaBeta)
    {
        double t1 = 1 + gamma;
        double t2 = 1 - gamma / u;
        if (t1 <= 0 || t2 <= 0) return 0;

        double numerator = Math.Abs(Math.Sqrt(t1) - Math.Sqrt(t2));
        double denominator = Math.Pow(t1, 0.25) * Math.Pow(u - gamma, 0.25);
        if (denominator <= 1e-12) return 0;

        return 0.51 * xAlphaBeta * Math.Sqrt(u + 1) * numerator / denominator;
    }

    /// <summary>Approach factor, Eq. (45)-(48).</summary>
    private static double ApproachFactor(double gamma, double gammaA, double gammaE, Input i, double ceff)
    {
        double span = gammaE - gammaA;
        if (span <= 1e-12) return 1.0;

        if (i.PinionDrives)
        {
            if (gamma >= 0) return 1.0;                                                             // (45)
            double x = 1 + (ceff - i.Ca2) / 50.0 * Math.Pow(-gamma / span, 3);                      // (46)
            return Math.Max(x, 1.0);
        }

        if (gamma <= 0) return 1.0;                                                                 // (47)
        double xj = 1 + (ceff - i.Ca1) / 50.0 * Math.Pow(gamma / span, 3);                          // (48)
        return Math.Max(xj, 1.0);
    }

    /// <summary>
    /// Load sharing factor, Clause 9.
    ///
    /// Implemented branches: 9.2 spur with unmodified profiles, 9.4 narrow helical (the same
    /// shape multiplied by the buttressing factor) and 9.6 wide helical (the mean load 1/ε_α
    /// with buttressing). Profile-modified branches 9.3, 9.5 and 9.7 are not implemented; a
    /// stated tip relief still reaches the approach factor, which is where it matters most.
    /// </summary>
    private static double LoadSharingFactor(double gamma, double gammaA, double gammaB,
                                            double gammaD, double gammaE, Input i, double ceff)
    {
        double xBut = ButtressingFactor(gamma, gammaA, gammaE, i);

        // Wide helical: the load is shared over several teeth, so the mean is used.
        if (i.epsilonGamma > 2 && Math.Abs(i.beta) > 0.01)
        {
            return i.epsilonAlpha > 1e-9 ? xBut / i.epsilonAlpha : 0;                               // (76)
        }

        // Q is 7 for grade 7 or finer, otherwise the grade itself, Eq. (60).
        double q = i.QualityGrade <= 7 ? 7.0 : i.QualityGrade;
        double baseValue = (q - 2.0) / 15.0;

        double x;
        if (gamma < gammaB && gammaB > gammaA)
        {
            x = baseValue + (1.0 - baseValue) * (gamma - gammaA) / (gammaB - gammaA);               // (57)
        }
        else if (gamma <= gammaD)
        {
            x = 1.0;                                                                                // (58)
        }
        else if (gammaE > gammaD)
        {
            x = baseValue + (1.0 - baseValue) * (gammaE - gamma) / (gammaE - gammaD);               // (59)
        }
        else
        {
            x = 1.0;
        }

        return Math.Max(0, Math.Min(1.0, x)) * xBut;
    }

    /// <summary>Buttressing factor for helical teeth, Eq. (49)-(56). Unity for spur gears.</summary>
    private static double ButtressingFactor(double gamma, double gammaA, double gammaE, Input i)
    {
        if (Math.Abs(i.beta) < 0.01) return 1.0;

        double width = 0.2 * Math.Sin(i.betaB * Math.PI / 180.0);                                   // (49)
        if (width <= 1e-9) return 1.0;

        double peak = i.epsilonBeta >= 1 ? 1.3 : 1.0 + 0.3 * i.epsilonBeta;                         // (51)/(52)

        double gammaAU = gammaA + width;
        double gammaEU = gammaE - width;

        if (gamma < gammaAU) return peak - (gamma - gammaA) / width * (peak - 1.0);                 // (54)
        if (gamma <= gammaEU) return 1.0;                                                           // (55)
        return peak - (gammaE - gamma) / width * (peak - 1.0);                                      // (56)
    }

    /// <summary>
    /// The same flash temperature through the general Eq. (5), used only to check Eq. (A.5).
    /// Written in ρ_y1, ρ_y2 and n1 rather than in the geometry factor.
    /// </summary>
    private static double FlashViaEq5(double gamma, double xGamma, Input i, Result r)
    {
        if (xGamma <= 0) return 0;

        double alphaWtr = i.alphaWt * Math.PI / 180.0;
        double rho1 = (1 + gamma) / (1 + i.u) * i.a * Math.Sin(alphaWtr);
        double rho2 = (i.u - gamma) / (1 + i.u) * i.a * Math.Sin(alphaWtr);
        if (rho1 <= 0 || rho2 <= 0) return 0;

        double rhoRel = rho1 * rho2 / (rho1 + rho2);
        double n1 = i.vt * 60000.0 / (Math.PI * i.d1);                                              // r/min

        double xJ = ApproachFactor(gamma, r.GammaA, r.GammaE, i, r.Ceff);

        return 2.52 * r.MuM * (r.XM / 50.0) * xJ
             * Math.Pow(xGamma * r.wBt, 0.75)
             * Math.Sqrt(n1 / 60.0)
             * Math.Abs(Math.Sqrt(rho1) - Math.Sqrt(rho2 / i.u))
             / Math.Pow(rhoRel, 0.25);                                                              // (5)
    }

    /// <summary>
    /// Contact exposure time, Eq. (95)-(96): the longer of the two times a point on a flank
    /// spends inside the Hertzian band, in microseconds.
    /// </summary>
    private static double ContactExposureTime(double gamma, Input i, double wBt, double Er)
    {
        double alphaWtr = i.alphaWt * Math.PI / 180.0;
        double rhoRel = RelativeRadius(gamma, i.a, i.u, alphaWtr);
        if (rhoRel <= 0 || Er <= 0) return 0;

        // Semi-width of the Hertzian band, from the line-contact solution.
        double bH = Math.Sqrt(8.0 * wBt * rhoRel / (Math.PI * Er));                                 // mm

        // Tangential (rolling) velocities of the two flanks at this point, m/s.
        double vt = i.vt;
        double vg1 = vt * (1 + gamma) / 1.0;
        double vg2 = vt * (i.u - gamma) / i.u;
        double slowest = Math.Min(Math.Abs(vg1), Math.Abs(vg2));
        if (slowest <= 1e-9) return 0;

        // 2 bH [mm] / v [m/s] = 2 bH / v milliseconds -> x1000 for microseconds.
        return 2.0 * bH / slowest * 1000.0;
    }
}
