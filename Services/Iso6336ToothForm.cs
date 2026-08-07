namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Tooth form factor Y_F and stress correction factor Y_S according to
/// ISO 6336-3:2006, Method B (load applied at the outer point of single pair
/// tooth contact).
///
/// Equation numbers in the comments refer to ISO 6336-3:2006.
/// Only the equations are implemented here - no text from the standard is reproduced.
///
/// Scope of this implementation: EXTERNAL spur and helical gears.
/// Internal gears (Eq. 11, 16, 19) are not implemented; callers must not use it for them.
/// </summary>
public static class Iso6336ToothForm
{
    /// <summary>Result of a Method B tooth form evaluation.</summary>
    public class ToothFormResult
    {
        public double YF { get; set; }          // Form factor
        public double YS { get; set; }          // Stress correction factor
        public double sFn { get; set; }         // Tooth root normal chord (mm)
        public double hFe { get; set; }         // Bending moment arm (mm)
        public double rhoF { get; set; }        // Root fillet radius (mm)
        public double qs { get; set; }          // Notch parameter
        public double alphaFen { get; set; }    // Load application angle (rad)
        public double zn { get; set; }          // Virtual number of teeth
        public double theta { get; set; }       // Auxiliary angle (rad)
        public bool Converged { get; set; }     // Iteration of Eq. (14) converged
        public string? Warning { get; set; }    // Out-of-range notice, if any
    }

    /// <summary>
    /// Calculates Y_F and Y_S for one external gear of a mating pair.
    /// </summary>
    /// <param name="z">Number of teeth (external, positive)</param>
    /// <param name="mn">Normal module (mm)</param>
    /// <param name="alphaN">Normal pressure angle (degrees)</param>
    /// <param name="beta">Helix angle (degrees)</param>
    /// <param name="x">Profile shift coefficient</param>
    /// <param name="da">Tip diameter of this gear (mm)</param>
    /// <param name="d">Reference diameter of this gear (mm)</param>
    /// <param name="epsilonAlpha">Transverse contact ratio of the pair</param>
    /// <param name="hfP">Dedendum of the basic rack (mm), typically 1.25·mn</param>
    /// <param name="rhofP">Root fillet radius of the basic rack (mm), typically 0.38·mn</param>
    /// <param name="spr">Undercut amount pr - q (mm); 0 when gears are not undercut</param>
    public static ToothFormResult Calculate(
        double z, double mn, double alphaN, double beta, double x,
        double da, double d, double epsilonAlpha,
        double hfP, double rhofP, double spr = 0)
    {
        var r = new ToothFormResult();

        if (z <= 0 || mn <= 0 || d <= 0 || da <= d)
        {
            r.Warning = "Invalid geometry for tooth form calculation.";
            return r;
        }

        double alphaNRad = alphaN * Math.PI / 180.0;
        double betaRad = beta * Math.PI / 180.0;

        // === Virtual gear parameters (Eq. 20, 21, 23, 24, 26, 27) ===
        double betaB = Math.Asin(Math.Sin(betaRad) * Math.Cos(alphaNRad));            // Eq. (20)
        double cosBetaB = Math.Cos(betaB);
        double zn = z / (cosBetaB * cosBetaB * Math.Cos(betaRad));                    // Eq. (21)
        double epsilonAlphaN = epsilonAlpha / (cosBetaB * cosBetaB);                  // Eq. (23)
        double dn = mn * zn;                                                          // Eq. (24)
        double dbn = dn * Math.Cos(alphaNRad);                                        // Eq. (26)
        double dan = dn + da - d;                                                     // Eq. (27)

        r.zn = zn;

        // === Outer point of single pair tooth contact (Eq. 28) ===
        double tipTerm = (dan / 2.0) * (dan / 2.0) - (dbn / 2.0) * (dbn / 2.0);
        if (tipTerm <= 0)
        {
            r.Warning = "Tip diameter lies inside the base circle - cannot evaluate tooth form.";
            return r;
        }
        double inner = Math.Sqrt(tipTerm)
                     - (Math.PI * d * Math.Cos(betaRad) * Math.Cos(alphaNRad) / Math.Abs(z)) * (epsilonAlphaN - 1.0);
        double den = 2.0 * Math.Sqrt(inner * inner + (dbn / 2.0) * (dbn / 2.0));      // Eq. (28), z > 0

        // === Load application angle (Eq. 29, 30, 31) ===
        double cosAlphaEn = dbn / den;
        cosAlphaEn = Math.Max(-1.0, Math.Min(1.0, cosAlphaEn));
        double alphaEn = Math.Acos(cosAlphaEn);                                       // Eq. (29)

        double gammaE = (0.5 * Math.PI + 2.0 * Math.Tan(alphaNRad) * x) / zn
                      + Inv(alphaNRad) - Inv(alphaEn);                                // Eq. (30)
        double alphaFen = alphaEn - gammaE;                                           // Eq. (31)
        r.alphaFen = alphaFen;

        // === Auxiliary values (Eq. 10, 12, 13, 14) ===
        // Eq. (10)
        double E = (Math.PI / 4.0) * mn
                 - hfP * Math.Tan(alphaNRad)
                 + spr / Math.Cos(alphaNRad)
                 - (1.0 - Math.Sin(alphaNRad)) * rhofP / Math.Cos(alphaNRad);

        // rhofPv = rhofP for external gears (Eq. 11 applies to internal gears only)
        double rhofPv = rhofP;

        double G = rhofPv / mn - hfP / mn + x;                                        // Eq. (12)

        const double T = Math.PI / 3.0;                                               // external gears
        double H = (2.0 / zn) * (Math.PI / 2.0 - E / mn) - T;                         // Eq. (13)

        // Eq. (14) - transcendental, solved by fixed-point iteration.
        // Seed value per the standard: theta = pi/6 for external gears.
        double theta = Math.PI / 6.0;
        bool converged = false;
        for (int i = 0; i < 50; i++)
        {
            double next = (2.0 * G / zn) * Math.Tan(theta) - H;
            if (Math.Abs(next - theta) < 1e-10) { theta = next; converged = true; break; }
            theta = next;
            if (double.IsNaN(theta) || double.IsInfinity(theta)) break;
        }
        r.theta = theta;
        r.Converged = converged;

        if (double.IsNaN(theta) || double.IsInfinity(theta))
        {
            r.Warning = "Tooth form iteration (Eq. 14) did not converge.";
            return r;
        }

        double cosTheta = Math.Cos(theta);
        double bracket = G / cosTheta - rhofPv / mn;   // recurring group in Eq. (15), (18)

        // === Tooth root normal chord (Eq. 15, external gears) ===
        double sFnOverMn = zn * Math.Sin(Math.PI / 3.0 - theta) + Math.Sqrt(3.0) * bracket;
        double sFn = sFnOverMn * mn;

        // === Root fillet radius (Eq. 17) ===
        double denomRho = cosTheta * (zn * cosTheta * cosTheta - 2.0 * G);
        double rhoFOverMn = rhofPv / mn + (2.0 * G * G) / denomRho;
        double rhoF = rhoFOverMn * mn;

        // === Bending moment arm (Eq. 18, external gears) ===
        //
        // NOTE ON EQ. (18): in the BS ISO 6336-3:2006 PDF the two bracketed groups
        //   ... - zn cos(pi/3 - theta) ( G/cos(theta) - rho_fPv/mn ) ...
        // appear adjacent with no visible operator, which would read as a product.
        // That interpretation is rejected here for two independent reasons:
        //   1. It is dimensionally/geometrically impossible: for a standard 20-tooth
        //      spur gear it yields hFe ~ 25.9·mn, while the whole tooth is only
        //      ~2.25·mn high. The subtraction form yields hFe ~ 1.07·mn, which is
        //      consistent with the tooth geometry.
        //   2. The internal-gear counterpart, Eq. (19), shows the same position as
        //      "- sqrt(3) · ( ... )", i.e. the structure is
        //      "- zn cos(...) - [coefficient] · ( G/cos(theta) - rho_fPv/mn )".
        //      For external gears the coefficient is 1.
        // The subtraction form is therefore used; the missing glyph is treated as a
        // rendering defect of this PDF (its text layer is likewise corrupted).
        double hFeOverMn = 0.5 * (
              (Math.Cos(gammaE) - Math.Sin(gammaE) * Math.Tan(alphaFen)) * (den / mn)
            - zn * Math.Cos(Math.PI / 3.0 - theta)
            - bracket);
        double hFe = hFeOverMn * mn;

        r.sFn = sFn;
        r.hFe = hFe;
        r.rhoF = rhoF;

        if (sFn <= 0 || hFe <= 0 || rhoF <= 0)
        {
            r.Warning = "Tooth form produced non-physical dimensions - check geometry and basic rack data.";
            return r;
        }

        // === Form factor (Eq. 9) ===
        r.YF = (6.0 * hFeOverMn * Math.Cos(alphaFen))
             / (sFnOverMn * sFnOverMn * Math.Cos(alphaNRad));

        // === Stress correction factor (Eq. 36, 37, 38) ===
        double qs = sFn / (2.0 * rhoF);                                               // Eq. (38)
        double L = sFn / hFe;                                                         // Eq. (37)
        r.qs = qs;

        if (qs < 1.0 || qs >= 8.0)
        {
            r.Warning = $"Notch parameter qs = {qs:F2} is outside the validity range 1 <= qs < 8 of Eq. (36).";
        }

        double exponent = 1.0 / (1.21 + 2.3 / L);
        r.YS = (1.2 + 0.13 * L) * Math.Pow(qs, exponent);                             // Eq. (36)

        return r;
    }

    /// <summary>Involute function: inv(a) = tan(a) - a.</summary>
    private static double Inv(double angleRad) => Math.Tan(angleRad) - angleRad;
}
