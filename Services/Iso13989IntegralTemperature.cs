using System;
using System.Collections.Generic;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Scuffing load capacity by the integral temperature method — ISO/TR 13989-2:2000.
///
/// Scope: EXTERNAL cylindrical gears, spur and helical (Clause 6.1). Bevel (6.2) and hypoid
/// (6.3) are not implemented. The equations are stated for transverse contact ratios up to
/// about 2,5.
///
/// Where Part 1 sweeps the whole path of contact and takes the peak, this method takes the
/// flash temperature at the pinion tooth tip and converts it to a weighted mean over the path
/// through the contact ratio factor X_ε:
///
///     θ_flaint = θ_flaE · X_ε                          Eq. (18)
///     θ_int    = θ_M + C_2 · θ_flaint,  C_2 = 1,5      Eq. (17)
///     S_intS   = θ_intS / θ_int                        Eq. (14)
///
/// The standard's own introduction says the two methods give about the same assessment of
/// scuffing risk, and that the integral method is the less sensitive of the two where there
/// are local temperature peaks — low contact ratio, contact near the base circle. Running both
/// and showing them together is therefore a real cross-check, not duplication.
///
/// The safety factor here is a ratio of ABSOLUTE temperatures, unlike Part 1's ratio of
/// temperatures above the oil, so the two numbers are not comparable directly. Clause 6.1.1
/// gives the bands: S_intS &lt; 1 high risk, 1 to 2 critical, &gt; 2 low risk.
/// </summary>
public static class Iso13989IntegralTemperature
{
    public class Input
    {
        // geometry
        public double alphaN { get; set; } = 20;   // normal pressure angle (deg)
        public double alphaT { get; set; }         // transverse pressure angle (deg)
        public double alphaWt { get; set; }        // working transverse pressure angle (deg)
        public double beta { get; set; }           // helix angle (deg)
        public double betaB { get; set; }          // base helix angle (deg)
        public double a { get; set; }              // centre distance (mm)
        public double u { get; set; }              // gear ratio
        public int z1 { get; set; }
        public int z2 { get; set; }
        public double da1 { get; set; }
        public double da2 { get; set; }
        public double db1 { get; set; }
        public double db2 { get; set; }
        public double b { get; set; }              // face width, smaller (mm)
        public double epsilonGamma { get; set; }   // total contact ratio

        // load
        public double Ft { get; set; }             // nominal tangential force (N)
        public double v { get; set; }              // pitch line velocity (m/s)
        public double KA { get; set; } = 1;
        public double KV { get; set; } = 1;
        public double KBbeta { get; set; } = 1;    // = K_Hbeta
        public double KBalpha { get; set; } = 1;   // = K_Halpha
        public bool PinionDrives { get; set; } = true;

        // material
        public double E1 { get; set; } = 206000;
        public double E2 { get; set; } = 206000;
        public double nu1 { get; set; } = 0.3;
        public double nu2 { get; set; } = 0.3;
        /// <summary>Heat conductivity λ_M, N/(s·K). 50 for case-hardened steel, Clause 5.3.</summary>
        public double LambdaM { get; set; } = 50;
        /// <summary>Specific heat per unit volume c_v, N/(mm²·K). 3,8 for case-hardened steel.</summary>
        public double Cv { get; set; } = 3.8;
        /// <summary>Welding factor X_W, Table 3.</summary>
        public double XW { get; set; } = 1.0;

        // surface and profile
        public double Ra1 { get; set; }            // flank Ra of new flanks (µm)
        public double Ra2 { get; set; }
        /// <summary>Run-in state Φ_E: 1 fully run in, 0 newly manufactured. Eq. (8).</summary>
        public double PhiE { get; set; } = 1.0;
        public double Ca1 { get; set; }            // tip relief (µm)
        public double Ca2 { get; set; }
        /// <summary>Mesh stiffness c_gamma (helical) or single tooth c' (spur), N/(mm·µm).</summary>
        public double cGamma { get; set; } = 20;
        public double cPrime { get; set; } = 14;
        /// <summary>ISO 1328-1 class; tip relief is only credited at grade 6 or better.</summary>
        public int QualityGrade { get; set; } = 6;

        // lubricant
        public double OilTemperature { get; set; } = 70;
        public double EtaOil { get; set; }         // dynamic viscosity at oil temperature (mPa·s)
        public double Nu40 { get; set; } = 220;    // kinematic viscosity at 40 °C (mm²/s)
        public LubricantType Lubricant { get; set; } = LubricantType.Mineral;
        public LubricationMethod Method { get; set; } = LubricationMethod.Dip;
        public double FzgLoadStage { get; set; } = 12;
    }

    public class Result
    {
        public bool Valid { get; set; }

        public double MuMc { get; set; }
        public double XL { get; set; }
        public double XR { get; set; }
        public double XM { get; set; }
        public double XalphaBeta { get; set; }
        public double XBE { get; set; }
        public double XQ { get; set; }
        public double XCa { get; set; }
        public double XEps { get; set; }
        public double XE { get; set; }
        public double XS { get; set; }
        public double KBgamma { get; set; }
        public double wBt { get; set; }
        public double RhoRedC { get; set; }
        public double Eps1 { get; set; }
        public double Eps2 { get; set; }

        public double FlashAtTip { get; set; }        // θ_flaE (K)
        public double FlashIntegral { get; set; }     // θ_flaint (K)
        public double BulkTemperature { get; set; }   // θ_M (°C)
        public double IntegralTemperature { get; set; } // θ_int (°C)
        public double ScuffingIntegralTemperature { get; set; } // θ_intS (°C)
        public double SafetyFactor { get; set; }      // S_intS
        public double LoadSafetyFactor { get; set; }  // S_Sl, Eq. (15)

        public string RiskBand { get; set; } = "";
        public List<string> Notes { get; } = new();
    }

    /// <summary>Lubricant factor X_L, Clause 5.1.</summary>
    public static double LubricantFactor(LubricantType type) => type switch
    {
        LubricantType.PolyglycolWaterSoluble => 0.6,
        LubricantType.PolyglycolNonWaterSoluble => 0.7,
        LubricantType.Polyalphaolefin => 0.8,
        LubricantType.PhosphateEster => 1.3,
        LubricantType.TractionFluid => 1.5,
        _ => 1.0
    };

    /// <summary>Welding factor X_W, Table 3 — the same classification Part 1 calls X_W.</summary>
    public static double WeldingFactor(GearMaterialType type) => type switch
    {
        GearMaterialType.NitridedNitridingSteel => 1.50,
        GearMaterialType.NitridedThroughHardeningSteel => 1.50,
        GearMaterialType.Nitrocarburized => 1.50,
        _ => 1.00
    };

    /// <summary>
    /// Pressure angle factor X_αβ, Method A, Eq. (13).
    ///
    /// This is the FULL expression, and it reproduces Table 2 exactly — 0,978 at
    /// α'_t = 20°, β = 0 and 0,966 at β = 20°. ISO/TR 13989-1 Eq. (A.8) prints an
    /// abbreviated form that drops cos^0,25(α_n) and cos^0,5(α_t) and lands 1,6 % to 3,1 %
    /// low. Both parts describe the same physical factor, so this one is used for both.
    /// </summary>
    public static double PressureAngleFactor(double alphaWtDeg, double alphaNDeg,
                                             double alphaTDeg, double betaDeg)
    {
        double awt = alphaWtDeg * Math.PI / 180.0;
        double an = alphaNDeg * Math.PI / 180.0;
        double at = alphaTDeg * Math.PI / 180.0;
        double bt = betaDeg * Math.PI / 180.0;

        double cosAwt = Math.Cos(awt), cosAt = Math.Cos(at);
        if (cosAwt <= 0 || cosAt <= 0) return 1.0;

        return 1.22 * (Math.Pow(Math.Sin(awt), 0.25) * Math.Pow(Math.Cos(an), 0.25)
                                                     * Math.Pow(Math.Cos(bt), 0.25))
                    / (Math.Sqrt(cosAwt) * Math.Sqrt(cosAt));
    }

    /// <summary>Helical load factor K_Bγ, Eq. (5).</summary>
    public static double HelicalLoadFactor(double epsilonGamma)
    {
        if (epsilonGamma <= 2) return 1.0;
        if (epsilonGamma >= 3.5) return 1.3;
        return 1.0 + 0.2 * Math.Sqrt((epsilonGamma - 2) * (5 - epsilonGamma));
    }

    /// <summary>
    /// Contact ratio factor X_ε, Eq. (39)-(44). Converts the tip flash temperature into the
    /// weighted mean over the path. The branches are on ε_α and on whether each addendum
    /// contact ratio reaches 1.
    /// </summary>
    public static double ContactRatioFactor(double eps1, double eps2)
    {
        double epsA = eps1 + eps2;                                                    // (45)
        if (eps1 <= 1e-9 || epsA <= 1e-9) return 0;

        double k = 1.0 / (2.0 * epsA * eps1);

        if (epsA < 1 && eps1 < 1 && eps2 < 1)
            return k * (eps1 * eps1 + eps2 * eps2);                                   // (39)

        if (epsA >= 1 && epsA < 2 && eps1 < 1 && eps2 < 1)
            return k * (0.70 * (eps1 * eps1 + eps2 * eps2) - 0.22 * epsA
                        + 0.52 - 0.60 * eps1 * eps2);                                 // (40)

        if (epsA >= 1 && epsA < 2 && eps1 >= 1 && eps2 < 1)
            return k * (0.18 * eps1 * eps1 + 0.70 * eps2 * eps2 + 0.82 * eps1
                        - 0.52 * eps2 - 0.30 * eps1 * eps2);                          // (41)

        if (epsA >= 1 && epsA < 2 && eps1 < 1 && eps2 >= 1)
            return k * (0.70 * eps1 * eps1 + 0.18 * eps2 * eps2 - 0.52 * eps1
                        + 0.82 * eps2 - 0.30 * eps1 * eps2);                          // (42)

        if (epsA >= 2 && epsA < 3 && eps1 >= eps2)
            return k * (0.44 * eps1 * eps1 + 0.59 * eps2 * eps2 + 0.30 * eps1
                        - 0.30 * eps2 - 0.15 * eps1 * eps2);                          // (43)

        if (epsA >= 2 && epsA < 3)
            return k * (0.59 * eps1 * eps1 + 0.44 * eps2 * eps2 - 0.30 * eps1
                        + 0.30 * eps2 - 0.15 * eps1 * eps2);                          // (44)

        // Beyond eps_alpha = 3 the standard gives no branch; hold the last one and say so.
        return k * (0.44 * eps1 * eps1 + 0.59 * eps2 * eps2 + 0.30 * eps1
                    - 0.30 * eps2 - 0.15 * eps1 * eps2);
    }

    public static Result Calculate(Input i)
    {
        var r = new Result();

        if (i.a <= 0 || i.b <= 0 || i.Ft <= 0 || i.v <= 0 || i.u <= 0 ||
            i.da1 <= 0 || i.da2 <= 0 || i.db1 <= 0 || i.db2 <= 0)
        {
            r.Notes.Add("The integral temperature was not evaluated: the geometry or the load is incomplete.");
            return r;
        }

        double awt = i.alphaWt * Math.PI / 180.0;
        double at = i.alphaT * Math.PI / 180.0;
        double bb = i.betaB * Math.PI / 180.0;

        // --- transverse unit load, Eq. (4) ---
        r.wBt = i.KA * i.KV * i.KBbeta * i.KBalpha * i.Ft / i.b;

        // --- Clause 5.1 range limits, stated rather than silently applied ---
        double wForFriction = r.wBt;
        if (wForFriction < 150)
        {
            wForFriction = 150;
            r.Notes.Add($"The specific tooth load is {r.wBt:F0} N/mm; Clause 5.1 requires the "
                      + "limiting value of 150 N/mm to be used in the friction equation below that.");
        }
        double vForFriction = i.v;
        if (vForFriction > 50) { vForFriction = 50; r.Notes.Add("Pitch line velocity above 50 m/s: the friction equation is evaluated at 50 m/s."); }
        else if (vForFriction < 1) r.Notes.Add($"Pitch line velocity is {i.v:F2} m/s. Below 1 m/s the standard expects higher coefficients of friction than Eq. (1) gives.");

        // --- Eq. (2), (3) ---
        double vSigmaC = 2.0 * vForFriction * Math.Tan(awt) * Math.Cos(at);
        r.RhoRedC = i.u / ((1 + i.u) * (1 + i.u)) * i.a * Math.Sin(awt) / Math.Cos(bb);

        if (vSigmaC <= 0 || r.RhoRedC <= 0 || i.EtaOil <= 0)
        {
            r.Notes.Add("The integral temperature was not evaluated: sliding velocity, curvature or oil viscosity is missing.");
            return r;
        }

        r.KBgamma = HelicalLoadFactor(i.epsilonGamma);                                // (5)
        double ra = 0.5 * (i.Ra1 + i.Ra2);                                            // (6)
        r.XR = 2.2 * Math.Pow(ra / r.RhoRedC, 0.25);                                  // (7)
        r.XL = LubricantFactor(i.Lubricant);

        r.MuMc = 0.045 * Math.Pow(wForFriction * r.KBgamma / (vSigmaC * r.RhoRedC), 0.2)
               * Math.Pow(i.EtaOil, -0.05) * r.XR * r.XL;                             // (1)

        r.XE = 1.0 + (1.0 - i.PhiE) * 30.0 * ra / r.RhoRedC;                          // (8)

        // --- thermal flash factor, Eq. (11)-(12) ---
        // The sqrt(1000) is the same mixed metre/millimetre unit adaptation that Part 1
        // carries: case-hardened steel with lambda = 50, c_v = 3,8, E = 206000, nu = 0,3
        // must give X_M = 50,0.
        double eMean = 0.5 * (i.E1 + i.E2);
        double nuMean = 0.5 * (i.nu1 + i.nu2);
        double bM = Math.Sqrt(i.LambdaM * i.Cv);                                      // (12)
        r.XM = Math.Sqrt(1000.0) * Math.Pow(eMean, 0.25)
             / (Math.Pow(1 - nuMean * nuMean, 0.25) * bM);                            // (11)

        r.XalphaBeta = PressureAngleFactor(i.alphaWt, i.alphaN, i.alphaT, i.beta);    // (13)

        // --- geometry factor at the pinion tip, Eq. (22)-(24) ---
        double rhoE1 = 0.5 * Math.Sqrt(Math.Max(0, i.da1 * i.da1 - i.db1 * i.db1));   // (23)
        double rhoE2 = i.a * Math.Sin(awt) - rhoE1;                                   // (24)
        if (rhoE1 <= 0 || Math.Abs(rhoE2) < 1e-9)
        {
            r.Notes.Add("The integral temperature was not evaluated: the radius of curvature at the pinion tip is degenerate.");
            return r;
        }
        r.XBE = 0.51 * Math.Sqrt(i.u + 1)
              * (Math.Sqrt(rhoE1) - Math.Sqrt(Math.Abs(rhoE2) / i.u))
              / Math.Pow(rhoE1 * Math.Abs(rhoE2), 0.25);                              // (22)

        // --- addendum contact ratios, Eq. (30)-(31) ---
        r.Eps1 = i.z1 / (2 * Math.PI) * (Math.Sqrt(Math.Max(0, Math.Pow(i.da1 / i.db1, 2) - 1)) - Math.Tan(awt));
        r.Eps2 = Math.Abs(i.z2) / (2 * Math.PI) * (Math.Sqrt(Math.Max(0, Math.Pow(i.da2 / i.db2, 2) - 1)) - Math.Tan(awt));

        // --- approach factor, Eq. (25)-(29) ---
        double epsF = i.PinionDrives ? r.Eps2 : r.Eps1;
        double epsA2 = i.PinionDrives ? r.Eps1 : r.Eps2;
        double ratio = epsA2 > 1e-9 ? epsF / epsA2 : 0;
        r.XQ = ratio <= 1.5 ? 1.00 : ratio >= 3 ? 0.60 : 1.40 - 4.0 / 15.0 * ratio;

        // --- tip relief factor, Eq. (32)-(38) ---
        r.XCa = TipReliefFactor(i, r);

        // --- contact ratio factor, Eq. (39)-(45) ---
        r.XEps = ContactRatioFactor(r.Eps1, r.Eps2);
        if (r.Eps1 + r.Eps2 >= 3)
            r.Notes.Add($"The transverse contact ratio is {r.Eps1 + r.Eps2:F2}. Clause 6.1 states the "
                      + "equations for gears with ε_α up to about 2,5; the last X_ε branch was held.");

        // --- flash temperature at the pinion tooth tip, Eq. (19) ---
        r.FlashAtTip = r.MuMc * r.XM * r.XBE * r.XalphaBeta
                     * Math.Pow(r.KBgamma * r.wBt, 0.75) * Math.Sqrt(i.v)
                     / Math.Pow(Math.Abs(i.a), 0.25)
                     * r.XE / (r.XQ * r.XCa);                                         // (19)

        r.FlashIntegral = r.FlashAtTip * r.XEps;                                      // (18)

        // --- bulk temperature, method C, Eq. (20)-(21) ---
        r.XS = i.Method switch
        {
            LubricationMethod.Spray => 1.2,
            LubricationMethod.Submerged => 0.2,
            _ => 1.0
        };
        const double C1 = 0.7, C2 = 1.5;
        double xMp = 1.0;                                                             // (21), one mating gear
        r.BulkTemperature = i.OilTemperature + C1 * xMp * r.FlashIntegral * r.XS;     // (20)

        r.IntegralTemperature = r.BulkTemperature + C2 * r.FlashIntegral;             // (17)

        // --- scuffing integral temperature from the FZG A/8,3/90 test, Eq. (94)-(97) ---
        double t1t = 3.726 * i.FzgLoadStage * i.FzgLoadStage;                         // (97)
        double thetaMT = 80 + 0.23 * t1t * r.XL;                                      // (95)
        double thetaFlaintT = 0.2 * t1t * Math.Pow(100.0 / Math.Max(i.Nu40, 1e-9), 0.02) * r.XL; // (96)
        double xWrelT = i.XW / 1.0;                                                   // (102), X_WT = 1
        r.ScuffingIntegralTemperature = thetaMT + xWrelT * C2 * thetaFlaintT;         // (94)

        // --- safety, Eq. (14)-(15) ---
        r.SafetyFactor = r.IntegralTemperature > 1e-9
            ? r.ScuffingIntegralTemperature / r.IntegralTemperature : 999;

        double denom = r.IntegralTemperature - i.OilTemperature;
        r.LoadSafetyFactor = denom > 1e-9
            ? (r.ScuffingIntegralTemperature - i.OilTemperature) / denom : 999;       // (15)

        r.RiskBand = r.SafetyFactor < 1 ? "High scuffing risk"
                   : r.SafetyFactor <= 2 ? "Critical range — moderate scuffing risk"
                   : "Low scuffing risk";

        r.Valid = true;
        return r;
    }

    /// <summary>
    /// Tip relief factor X_Ca, Eq. (32)-(38). Which gear's tip relief counts depends on the
    /// ratio of the addendum contact ratios and on the direction of power flow.
    /// </summary>
    private static double TipReliefFactor(Input i, Result r)
    {
        // Clause 6.1.12: tip relief is only credited for ISO 1328-1 grade 6 or better.
        if (i.QualityGrade > 6) return 1.0;

        bool helical = Math.Abs(i.beta) > 0.01;
        double stiffness = helical ? i.cGamma : i.cPrime;
        if (stiffness <= 0) return 1.0;

        double cEff = i.KA * i.Ft / (i.b * stiffness);                                // (37)/(38)
        if (cEff <= 0) return 1.0;

        // (33)-(36): the driving flank's relief is the one that matters.
        bool useCa1 = i.PinionDrives ? r.Eps1 > 1.5 * r.Eps2 : r.Eps1 > (2.0 / 3.0) * r.Eps2;
        double caNominal = useCa1 ? i.Ca1 : i.Ca2;
        double ca = Math.Min(caNominal, cEff);

        double epsMax = Math.Max(r.Eps1, r.Eps2);
        double q = ca / cEff;

        return 1.0 + (0.06 + 0.18 * q) * epsMax + (0.02 + 0.69 * q) * epsMax * epsMax; // (32)
    }
}
