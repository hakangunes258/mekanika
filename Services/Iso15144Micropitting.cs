using System;
using System.Collections.Generic;
using System.Linq;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Micropitting load capacity — ISO/TR 15144-1:2014, Method B.
///
/// Micropitting is a lubrication-regime failure, not a stress one: it happens where the oil
/// film is too thin next to the roughness of the flanks. So the criterion is a ratio of
/// lengths, not of stresses —
///
///     λ_GF,Y = h_Y / Ra                     Formula (2)
///     S_λ    = λ_GF,min / λ_GFP             Formula (1)
///
/// with h_Y the Dowson/Higginson film thickness, Formula (4). Method B evaluates it at the
/// seven points A, AB, B, C, D, DE, E on the path of contact and takes the minimum, because
/// micropitting occurs in the areas of negative specific sliding rather than at the peak load.
///
/// Scope of this implementation: EXTERNAL cylindrical gears, spur and helical, with
/// UNMODIFIED profiles (Clause 11.1). The profile-modified branches (11.2, 11.5, 11.7) and the
/// buttressing factor (11.3) are not implemented; a stated tip relief still reaches the bulk
/// temperature through X_Ca. Clause 8.2 also notes that ε_α &gt; 2 can only be done by Method A.
/// </summary>
public static class Iso15144Micropitting
{
    /// <summary>Base oil family for the pressure-viscosity coefficient, Formulae (9)-(11).</summary>
    public enum OilFamily { Mineral, PaoSynthetic, PagSynthetic }

    public class Input
    {
        public double alphaT { get; set; }
        public double alphaWt { get; set; }
        public double betaB { get; set; }
        public double a { get; set; }
        public double u { get; set; }
        public int z1 { get; set; }
        public int z2 { get; set; }
        public double dw1 { get; set; }
        public double dw2 { get; set; }
        public double da1 { get; set; }
        public double da2 { get; set; }
        public double db1 { get; set; }
        public double db2 { get; set; }
        public double b { get; set; }
        public double epsilonAlpha { get; set; }
        public double epsilonGamma { get; set; }

        public double Ft { get; set; }             // transverse tangential load at reference cylinder (N)
        public double n1 { get; set; }             // pinion speed (r/min)
        public double T1 { get; set; }             // pinion torque (Nm)
        public double KA { get; set; } = 1;
        public double KV { get; set; } = 1;
        public double KHalpha { get; set; } = 1;
        public double KHbeta { get; set; } = 1;
        public bool PinionDrives { get; set; } = true;

        public double E1 { get; set; } = 206000;
        public double E2 { get; set; } = 206000;
        public double nu1 { get; set; } = 0.3;
        public double nu2 { get; set; } = 0.3;
        /// <summary>Table 2, steel: density 7800 kg/m³, c 440 J/(kg·K), λ 45 W/(m·K).</summary>
        public double RhoM { get; set; } = 7800;
        public double CM { get; set; } = 440;
        public double LambdaM { get; set; } = 45;

        public double Ra1 { get; set; }
        public double Ra2 { get; set; }
        public int QualityGrade { get; set; } = 6;
        public double Ca1 { get; set; }
        public double Ca2 { get; set; }
        public double cPrime { get; set; } = 14;
        public double cGammaAlpha { get; set; } = 20;
        /// <summary>True when the gear set carries tip relief the maker considers adequate.</summary>
        public bool AdequateTipRelief { get; set; }

        public double OilTemperature { get; set; } = 70;
        public double Nu40 { get; set; } = 220;
        public double Nu100 { get; set; }
        public double Rho15 { get; set; }          // kg/m³, 0 = estimate from ν40
        public OilFamily Family { get; set; } = OilFamily.Mineral;
        public LubricantType Lubricant { get; set; } = LubricantType.Mineral;
        public LubricationMethod Method { get; set; } = LubricationMethod.Dip;

        /// <summary>FVA-FZG C-GF/8,3/90 micropitting failure load stage (SKS), Annex A.</summary>
        public double MicropittingLoadStage { get; set; } = 8;
        /// <summary>Permissible λ_GFP entered directly. 0 = read Figure A.1 from the load stage.</summary>
        public double LambdaGfpOverride { get; set; }
    }

    public record PointResult(string Name, double gY, double RhoN, double XY,
                              double Flash, double Contact, double Lambda);

    public class Result
    {
        public bool Valid { get; set; }

        public double MuM { get; set; }
        public double HV { get; set; }
        public double XCa { get; set; }
        public double XS { get; set; }
        public double XR { get; set; }
        public double KBgamma { get; set; }
        public double BulkTemperature { get; set; }
        public double Er { get; set; }
        public double GM { get; set; }
        public double Alpha38 { get; set; }
        public double AlphaThetaM { get; set; }
        public double EtaThetaM { get; set; }
        public double NuThetaM { get; set; }
        public double Ra { get; set; }

        public double LambdaMin { get; set; }
        public string LambdaMinAt { get; set; } = "";
        public double LambdaGfp { get; set; }
        public double SafetyFactor { get; set; }

        public List<PointResult> Points { get; } = new();
        public List<string> Notes { get; } = new();
    }

    /// <summary>Lubricant factor X_L, Table 3 — same values the scuffing parts use.</summary>
    public static double LubricantFactor(LubricantType t) => t switch
    {
        LubricantType.PolyglycolWaterSoluble => 0.6,
        LubricantType.PolyglycolNonWaterSoluble => 0.7,
        LubricantType.Polyalphaolefin => 0.8,
        LubricantType.PhosphateEster => 1.3,
        LubricantType.TractionFluid => 1.5,
        _ => 1.0
    };

    /// <summary>Helical load factor K_Bγ, Formulae (88)-(90). Same curve as ISO/TR 13989-2.</summary>
    public static double HelicalLoadFactor(double epsGamma) =>
        Iso13989IntegralTemperature.HelicalLoadFactor(epsGamma);

    /// <summary>
    /// Permissible specific film thickness λ_GFP from Annex A Figure A.1, for mineral oils
    /// tested to FVA-FZG C-GF/8,3/90.
    ///
    /// These points are DIGITISED FROM A FIGURE in an informative annex, not from a table, and
    /// the annex itself says it is "provided as reference only". They are the weakest numbers
    /// in this module, which is why the caller can enter λ_GFP directly and the result names
    /// where the value came from. The figure is drawn for Ra = 0,50 µm.
    /// </summary>
    public static double PermissibleLambdaFromFigureA1(double vg, double loadStage)
    {
        // curve endpoints at SKS 5 and SKS 10, read off Figure A.1
        (double vg, double at5, double at10)[] curves =
        {
            (32,  0.130, 0.055),
            (100, 0.250, 0.090),
            (220, 0.400, 0.145),
            (460, 0.570, 0.200)
        };

        double s = Math.Clamp(loadStage, 5, 10);

        // The curves fall roughly exponentially with the load stage; interpolate each
        // logarithmically in the stage, then interpolate logarithmically in viscosity.
        double Curve((double vg, double at5, double at10) c)
            => c.at5 * Math.Pow(c.at10 / c.at5, (s - 5) / 5.0);

        double lv = Math.Log(Math.Clamp(vg, 32, 460));
        for (int k = 0; k < curves.Length - 1; k++)
        {
            if (lv <= Math.Log(curves[k + 1].vg) || k == curves.Length - 2)
            {
                double l0 = Math.Log(curves[k].vg), l1 = Math.Log(curves[k + 1].vg);
                double f = l1 > l0 ? (lv - l0) / (l1 - l0) : 0;
                f = Math.Clamp(f, 0, 1);
                double a0 = Curve(curves[k]), a1 = Curve(curves[k + 1]);
                return a0 + (a1 - a0) * f;
            }
        }
        return Curve(curves[^1]);
    }

    /// <summary>Kinematic viscosity at a temperature, Formulae (17)-(19). Same Walther relation
    /// the scuffing module uses, restated here because this standard spells it out.</summary>
    public static double KinematicViscosityAt(double nu40, double nu100, double thetaC)
        => Iso13989FlashTemperature.ViscosityAt(nu40, nu100, thetaC);

    public static Result Calculate(Input i)
    {
        var r = new Result();

        if (i.a <= 0 || i.b <= 0 || i.Ft <= 0 || i.n1 <= 0 || i.u <= 0 ||
            i.da1 <= 0 || i.db1 <= 0 || i.da2 <= 0 || i.db2 <= 0 || i.dw1 <= 0)
        {
            r.Notes.Add("Micropitting was not evaluated: the geometry or the load is incomplete.");
            return r;
        }

        double at = i.alphaT * Math.PI / 180.0;
        double awt = i.alphaWt * Math.PI / 180.0;
        double bb = i.betaB * Math.PI / 180.0;

        // --- reduced modulus, Formula (6) ---
        r.Er = 2.0 / ((1 - i.nu1 * i.nu1) / i.E1 + (1 - i.nu2 * i.nu2) / i.E2);
        r.Ra = 0.5 * (i.Ra1 + i.Ra2);                                                 // (3)
        if (r.Ra <= 0) { r.Notes.Add("Micropitting was not evaluated: the flank roughness is zero."); return r; }

        // --- path of contact ---
        double gAlpha = 0.5 * (Math.Sqrt(Math.Max(0, i.da1 * i.da1 - i.db1 * i.db1))
                             + Math.Sqrt(Math.Max(0, i.da2 * i.da2 - i.db2 * i.db2)))
                      - i.a * Math.Sin(awt);
        double pet = Math.PI * i.db1 / i.z1;                                          // transverse base pitch
        if (gAlpha <= 0 || pet <= 0) { r.Notes.Add("Micropitting was not evaluated: the path of contact is degenerate."); return r; }

        double gC = i.db1 / 2.0 * Math.Tan(awt)
                  - Math.Sqrt(Math.Max(0, i.da1 * i.da1 / 4.0 - i.db1 * i.db1 / 4.0)) + gAlpha; // (37)

        var points = new (string Name, double g)[]
        {
            ("A",  0.0),                                                              // (34)
            ("AB", (gAlpha - pet) / 2.0),                                             // (35)
            ("B",  gAlpha - pet),                                                     // (36)
            ("C",  gC),                                                               // (37)
            ("D",  pet),                                                              // (38)
            ("DE", (gAlpha - pet) / 2.0 + pet),                                        // (39)
            ("E",  gAlpha)                                                            // (40)
        };

        // --- normal relative radius at the pitch point, for mu_m and X_R ---
        double rhoNC = NormalRelativeRadius(gC, gAlpha, i, bb);
        if (rhoNC <= 0) { r.Notes.Add("Micropitting was not evaluated: the curvature at the pitch point is degenerate."); return r; }

        // --- viscosity and density at the oil temperature, for mu_m ---
        double nu100 = i.Nu100 > 0 ? i.Nu100 : Iso13989FlashTemperature.TypicalNu100(i.Nu40);
        double rho15 = i.Rho15 > 0 ? i.Rho15 : 43.37 * Math.Log10(i.Nu40) + 805.5;    // (21)
        double nuOil = KinematicViscosityAt(i.Nu40, nu100, i.OilTemperature);
        double rhoOil = rho15 * (1 - 0.7 * ((i.OilTemperature + 273) - 289) / rho15); // (20)
        double etaOil = 1e-6 * nuOil * rhoOil;                                        // (16), N·s/m²

        if (etaOil <= 0) { r.Notes.Add("Micropitting was not evaluated: the oil viscosity is not usable."); return r; }

        // --- mean coefficient of friction, Formula (86) ---
        r.KBgamma = HelicalLoadFactor(i.epsilonGamma);
        r.XR = 2.2 * Math.Pow(r.Ra / rhoNC, 0.25);                                    // (87)
        double xL = LubricantFactor(i.Lubricant);

        double vSigmaC = SumTangentialVelocity(gC, gAlpha, i, awt);
        double fbt = i.Ft / Math.Cos(at);                                             // transverse load in the plane of action
        if (vSigmaC <= 0) { r.Notes.Add("Micropitting was not evaluated: the sum of tangential velocities is zero."); return r; }

        r.MuM = 0.045 * Math.Pow(i.KA * i.KV * i.KHalpha * i.KHbeta * fbt * r.KBgamma
                                 / (i.b * vSigmaC * rhoNC), 0.2)
              * Math.Pow(1e3 * etaOil, -0.05) * r.XR * xL;                            // (86)

        // --- load losses factor, Formulae (91)-(92) ---
        double eps1 = i.z1 / (2 * Math.PI) * (Math.Sqrt(Math.Max(0, Math.Pow(i.da1 / i.db1, 2) - 1)) - Math.Tan(awt));
        double eps2 = Math.Abs(i.z2) / (2 * Math.PI) * (Math.Sqrt(Math.Max(0, Math.Pow(i.da2 / i.db2, 2) - 1)) - Math.Tan(awt));
        double epsA = i.epsilonAlpha > 0 ? i.epsilonAlpha : eps1 + eps2;

        r.HV = epsA < 2
            ? (eps1 * eps1 + eps2 * eps2 + 1 - epsA) * (1.0 / i.z1 + 1.0 / Math.Abs(i.z2)) * Math.PI / Math.Cos(bb)
            : 0.5 * epsA * (1.0 / i.z1 + 1.0 / Math.Abs(i.z2)) * Math.PI / Math.Cos(bb);

        // --- tip relief factor, Method B, Formulae (100)-(101) ---
        r.XCa = TipReliefFactorMethodB(i, eps1, eps2);

        r.XS = i.Method switch
        {
            LubricationMethod.Spray => 1.2,
            LubricationMethod.Grease => 1.2,
            LubricationMethod.Submerged => 0.2,
            _ => 1.0
        };

        // --- bulk temperature, Formula (84) ---
        double power = 2 * Math.PI * i.n1 / 60.0 * i.T1 / 1000.0;                     // (85), kW
        r.BulkTemperature = i.OilTemperature
            + 7400 * Math.Pow(power * r.MuM * r.HV / (i.a * i.b), 0.72)
              * r.XS / (1.2 * r.XCa);                                                 // (84)

        // --- lubricant properties at the bulk temperature ---
        double eta38 = 1e-6 * KinematicViscosityAt(i.Nu40, nu100, 38)
                     * rho15 * (1 - 0.7 * ((38.0 + 273) - 289) / rho15);
        r.Alpha38 = i.Family switch
        {
            OilFamily.PaoSynthetic => 1.466e-8 * Math.Pow(eta38, 0.0507),             // (10)
            OilFamily.PagSynthetic => 1.392e-8 * Math.Pow(eta38, 0.1572),             // (11)
            _ => 2.657e-8 * Math.Pow(eta38, 0.1348)                                   // (9)
        };
        r.AlphaThetaM = r.Alpha38 * (1 + 516 * (1.0 / (r.BulkTemperature + 273) - 1.0 / 311)); // (8)

        r.NuThetaM = KinematicViscosityAt(i.Nu40, nu100, r.BulkTemperature);
        double rhoThetaM = rho15 * (1 - 0.7 * ((r.BulkTemperature + 273) - 289) / rho15);
        r.EtaThetaM = 1e-6 * r.NuThetaM * rhoThetaM;                                  // (16)

        r.GM = 1e6 * r.AlphaThetaM * r.Er;                                            // (5)

        double bM1 = Math.Sqrt(i.RhoM * i.CM * i.LambdaM);                            // (82)
        double bM2 = bM1;                                                             // (83), same material
        double zE = Math.Sqrt(r.Er / (2 * Math.PI));                                  // (26)

        // --- sweep the seven points ---
        foreach (var (name, g) in points)
        {
            double rhoN = NormalRelativeRadius(g, gAlpha, i, bb);
            if (rhoN <= 0) continue;

            double xY = LoadSharingFactor(g, gAlpha, pet, i);
            if (xY <= 0) continue;

            double vr1 = TangentialVelocity(g, gAlpha, i, awt, pinion: true);
            double vr2 = TangentialVelocity(g, gAlpha, i, awt, pinion: false);
            double vSum = vr1 + vr2;                                                  // (13)
            double vSlide = Math.Abs(vr1 - vr2);                                      // (81)

            double uY = r.EtaThetaM * vSum / (2000.0 * r.Er * rhoN);                  // (12)

            double pH = zE * Math.Sqrt(i.Ft * xY / (i.b * rhoN * Math.Cos(at) * Math.Cos(bb))); // (25)
            double pDyn = pH * Math.Sqrt(i.KA * i.KV * i.KHalpha * i.KHbeta);         // (24)
            double wY = 2 * Math.PI * pDyn * pDyn / (r.Er * r.Er);                    // (22)

            // flash temperature, Formula (80)
            double denom = bM1 * Math.Sqrt(vr1) + bM2 * Math.Sqrt(vr2);
            double flash = denom > 1e-12
                ? Math.Sqrt(Math.PI) / 2.0
                  * r.MuM * pDyn * 1e6 * vSlide / denom
                  * Math.Sqrt(8 * rhoN * pDyn / (1000.0 * r.Er))
                : 0;

            double contact = r.BulkTemperature + flash;                               // (79)

            // sliding parameter, Formula (27)
            double alphaB = r.Alpha38 * (1 + 516 * (1.0 / (contact + 273) - 1.0 / 311)); // (28)
            double nuB = KinematicViscosityAt(i.Nu40, nu100, contact);
            double rhoB = rho15 * (1 - 0.7 * ((contact + 273) - 289) / rho15);        // (33)
            double etaB = 1e-6 * nuB * rhoB;                                          // (29)
            double sGF = (r.AlphaThetaM * r.EtaThetaM) > 0
                ? alphaB * etaB / (r.AlphaThetaM * r.EtaThetaM) : 1;                  // (27)

            double h = 1600 * rhoN * Math.Pow(r.GM, 0.6) * Math.Pow(uY, 0.7)
                     * Math.Pow(wY, -0.13) * Math.Pow(Math.Max(sGF, 1e-12), 0.22);    // (4)

            double lambda = h / r.Ra;                                                 // (2)
            r.Points.Add(new PointResult(name, g, rhoN, xY, flash, contact, lambda));
        }

        if (r.Points.Count == 0) { r.Notes.Add("Micropitting was not evaluated: no usable contact point."); return r; }

        var worst = r.Points.OrderBy(p => p.Lambda).First();
        r.LambdaMin = worst.Lambda;
        r.LambdaMinAt = worst.Name;

        // --- permissible film thickness ---
        if (i.LambdaGfpOverride > 0)
        {
            r.LambdaGfp = i.LambdaGfpOverride;
            r.Notes.Add("The permissible specific film thickness was entered directly.");
        }
        else
        {
            r.LambdaGfp = PermissibleLambdaFromFigureA1(i.Nu40, i.MicropittingLoadStage);
            r.Notes.Add($"λ_GFP = {r.LambdaGfp:F3} read from Annex A Figure A.1 for ISO VG {i.Nu40:F0} "
                      + $"at micropitting load stage {i.MicropittingLoadStage:F0}. That annex is "
                      + "informative and the figure is drawn for Ra = 0,50 µm — enter λ_GFP directly "
                      + "if the oil has a test result of its own.");
        }

        r.SafetyFactor = r.LambdaGfp > 1e-9 ? r.LambdaMin / r.LambdaGfp : 0;

        if (epsA > 2)
            r.Notes.Add($"The transverse contact ratio is {epsA:F2}. Clause 8.2 states that gears with "
                      + "ε_α > 2 can only be calculated by Method A; Method B was used anyway and the "
                      + "result should be treated as indicative.");

        if (i.Method == LubricationMethod.Grease)
        {
            r.Notes.Add("The lubricant is a grease. ISO/TR 15144-1 is written for oils, and the "
                      + "micropitting load stage of a grease is rarely published - if it was not supplied "
                      + "by the manufacturer, the assumed stage behind lambda_GFP is a guess and this "
                      + "result is not a verdict on the design.");
        }

        r.Valid = true;
        return r;
    }

    /// <summary>Y-circle diameters and the normal relative radius, Formulae (41)-(45).</summary>
    private static double NormalRelativeRadius(double g, double gAlpha, Input i, double bb)
    {
        double t1 = Math.Sqrt(Math.Max(0, i.da1 * i.da1 / 4 - i.db1 * i.db1 / 4)) - gAlpha + g;
        double dY1 = 2 * Math.Sqrt(i.db1 * i.db1 / 4 + t1 * t1);                      // (41)
        double t2 = Math.Sqrt(Math.Max(0, i.da2 * i.da2 / 4 - i.db2 * i.db2 / 4)) - g;
        double dY2 = 2 * Math.Sqrt(i.db2 * i.db2 / 4 + t2 * t2);                      // (42)

        double rt1 = Math.Sqrt(Math.Max(0, dY1 * dY1 - i.db1 * i.db1)) / 2.0;         // (44)
        double rt2 = Math.Sqrt(Math.Max(0, dY2 * dY2 - i.db2 * i.db2)) / 2.0;
        double sum = rt1 + rt2;
        if (sum <= 1e-12) return 0;
        double rhoT = rt1 * rt2 / sum;                                                // (43)
        return rhoT / Math.Cos(bb);                                                   // (45)
    }

    /// <summary>Tangential velocity on one flank, Formulae (14)-(15).</summary>
    private static double TangentialVelocity(double g, double gAlpha, Input i, double awt, bool pinion)
    {
        double t1 = Math.Sqrt(Math.Max(0, i.da1 * i.da1 / 4 - i.db1 * i.db1 / 4)) - gAlpha + g;
        double dY1 = 2 * Math.Sqrt(i.db1 * i.db1 / 4 + t1 * t1);
        double t2 = Math.Sqrt(Math.Max(0, i.da2 * i.da2 / 4 - i.db2 * i.db2 / 4)) - g;
        double dY2 = 2 * Math.Sqrt(i.db2 * i.db2 / 4 + t2 * t2);

        if (pinion)
        {
            double denom = i.dw1 * i.dw1 - i.db1 * i.db1;
            if (denom <= 0) return 0;
            return 2 * Math.PI * i.n1 / 60.0 * i.dw1 / 2000.0 * Math.Sin(awt)
                 * Math.Sqrt(Math.Max(0, (dY1 * dY1 - i.db1 * i.db1) / denom));       // (14)
        }
        double denom2 = i.dw2 * i.dw2 - i.db2 * i.db2;
        if (denom2 <= 0) return 0;
        return 2 * Math.PI * i.n1 / (i.u * 60.0) * i.dw2 / 2000.0 * Math.Sin(awt)
             * Math.Sqrt(Math.Max(0, (dY2 * dY2 - i.db2 * i.db2) / denom2));          // (15)
    }

    private static double SumTangentialVelocity(double g, double gAlpha, Input i, double awt)
        => TangentialVelocity(g, gAlpha, i, awt, true) + TangentialVelocity(g, gAlpha, i, awt, false);

    /// <summary>
    /// Load sharing factor for unmodified profiles, Formulae (46)-(48). Q is 7 for grade 7 or
    /// finer, otherwise the grade itself.
    /// </summary>
    private static double LoadSharingFactor(double g, double gAlpha, double pet, Input i)
    {
        double gB = gAlpha - pet, gD = pet;
        double q = i.QualityGrade <= 7 ? 7.0 : i.QualityGrade;
        double baseValue = (q - 2.0) / 15.0;

        double x;
        if (g < gB && gB > 0) x = baseValue + (1.0 / 3.0) * (g / gB);                 // (46)
        else if (g <= gD) x = 1.0;                                                    // (47)
        else if (gAlpha > gD) x = baseValue + (1.0 / 3.0) * ((gAlpha - g) / (gAlpha - gD)); // (48)
        else x = 1.0;

        return Math.Clamp(x, 0, 1);
    }

    /// <summary>Tip relief factor, Method B, Formulae (100)-(101).</summary>
    private static double TipReliefFactorMethodB(Input i, double eps1, double eps2)
    {
        if (i.QualityGrade > 6 || !i.AdequateTipRelief) return 1.0;                    // (101)

        double epsMax = Math.Max(eps1, eps2);
        return 1 + 0.24 * epsMax + 0.71 * epsMax * epsMax;                            // (100)
    }
}
