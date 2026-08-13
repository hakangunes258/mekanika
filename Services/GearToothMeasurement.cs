namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Tooth thickness, inspection ("control") dimensions and backlash for external
/// cylindrical involute gears, per ISO 21771:2007 (geometry and tooth thickness) and
/// ISO/TR 10064-2 (backlash and tooth thickness allowances).
///
/// What this covers:
///   - tooth thickness at the reference, base and tip cylinders
///   - normal chordal tooth thickness and chordal height (gear tooth caliper)
///   - base tangent length / span measurement W_k over k teeth, with the number of
///     teeth to span and the resulting measuring circle
///   - dimension over balls or pins M_d, with a "best size" ball recommendation
///   - tooth thickness allowances and the normal / circumferential / radial backlash
///     that follows from them together with the centre distance deviation
///
/// Everything is derived from the involute geometry itself; the only tabulated data is
/// the list of preferred ball sizes. Sign convention throughout: tooth thickness
/// allowances A_sne (upper) and A_sni (lower) are NEGATIVE, since a tooth is thinned
/// from nominal to create backlash, and A_sni &lt;= A_sne.
///
/// SCOPE: external gears only. Internal gears reverse several signs and are not handled.
/// </summary>
public static class GearToothMeasurement
{
    // ===================== Involute helpers =====================

    /// <summary>Involute function inv(a) = tan(a) - a, angle in radians.</summary>
    public static double Inv(double angleRad) => Math.Tan(angleRad) - angleRad;

    /// <summary>
    /// Inverse involute: returns the angle (rad) whose involute is <paramref name="invValue"/>.
    /// Newton iteration on f(a) = tan a - a - inv, f'(a) = tan^2 a, seeded with the
    /// classical cube-root approximation a ~ (3*inv)^(1/3).
    /// </summary>
    public static double InvertInvolute(double invValue)
    {
        if (invValue <= 0) return 0;

        double a = Math.Cbrt(3.0 * invValue);
        if (a >= Math.PI / 2.0 - 1e-6) a = Math.PI / 2.0 - 1e-6;

        for (int i = 0; i < 60; i++)
        {
            double t = Math.Tan(a);
            double f = t - a - invValue;
            double df = t * t;
            if (df < 1e-12) break;

            double next = a - f / df;
            if (next <= 0) next = a / 2.0;
            if (next >= Math.PI / 2.0 - 1e-9) next = (a + Math.PI / 2.0) / 2.0;

            if (Math.Abs(next - a) < 1e-13) { a = next; break; }
            a = next;
        }
        return a;
    }

    // ===================== Inputs and results =====================

    /// <summary>Everything one gear of the pair contributes to a measurement calculation.</summary>
    public class GearInput
    {
        public int z { get; set; }
        public double mn { get; set; }            // normal module (mm)
        public double alphaN { get; set; } = 20;  // normal pressure angle (deg)
        public double beta { get; set; }          // helix angle (deg)
        public double x { get; set; }             // profile shift coefficient
        public double d { get; set; }             // reference diameter (mm)
        public double db { get; set; }            // base diameter (mm)
        public double da { get; set; }            // tip diameter (mm)
        public double df { get; set; }            // root diameter (mm)
        public double b { get; set; }             // face width (mm)

        /// <summary>Upper tooth thickness allowance A_sne (mm, negative = thinner than nominal).</summary>
        public double Asne { get; set; }
        /// <summary>Lower tooth thickness allowance A_sni (mm, negative, &lt;= A_sne).</summary>
        public double Asni { get; set; }

        /// <summary>Ball / pin diameter for the over-pins measurement (mm). 0 = use the best size.</summary>
        public double BallDiameter { get; set; }

        /// <summary>
        /// Number of teeth to span for W_k. 0 lets the calculator pick the value that puts the
        /// contact at mid-flank; a drawing that already states k must override it, because a
        /// span dimension only means anything against the k it was measured over.
        /// </summary>
        public int SpanTeeth { get; set; }
    }

    public class Result
    {
        // --- tooth thickness ---
        public double sn { get; set; }            // normal tooth thickness at reference (mm)
        public double st { get; set; }            // transverse tooth thickness at reference (mm)
        public double sbt { get; set; }           // transverse tooth thickness at base circle (mm)
        public double sat { get; set; }           // transverse tooth thickness at tip (mm)
        public double san { get; set; }           // normal tooth thickness at tip (mm)
        public double alphaAt { get; set; }       // transverse pressure angle at tip (deg)
        public double alphaT { get; set; }        // transverse pressure angle (deg)
        public double betaB { get; set; }         // base helix angle (deg)

        // --- chordal (gear tooth caliper) ---
        public double zn { get; set; }            // virtual number of teeth
        public double snChordal { get; set; }     // normal chordal tooth thickness (mm)
        public double haChordal { get; set; }     // chordal height above the tip (mm)

        // --- span measurement (base tangent length) ---
        public int k { get; set; }                // number of teeth spanned
        public int kPreferred { get; set; }       // what the calculator would choose on its own
        public double Wk { get; set; }            // nominal base tangent length (mm)
        public double WkUpper { get; set; }       // with A_sne (mm)
        public double WkLower { get; set; }       // with A_sni (mm)
        public double AWe { get; set; }           // upper allowance on W (mm)
        public double AWi { get; set; }           // lower allowance on W (mm)
        public double dMk { get; set; }           // measuring circle diameter of the span (mm)
        public double bMinSpan { get; set; }      // face width needed to fit the span (mm)
        public string? SpanWarning { get; set; }

        // --- measurement over balls / pins ---
        public double DM { get; set; }            // ball diameter used (mm)
        public double DMBest { get; set; }        // best-size ball diameter (mm)
        public double alphaM { get; set; }        // pressure angle at the ball centre circle (deg)
        public double dMBallCentre { get; set; }  // ball centre circle diameter (mm)
        public double Md { get; set; }            // nominal dimension over balls (mm)
        public double MdUpper { get; set; }       // with A_sne (mm)
        public double MdLower { get; set; }       // with A_sni (mm)
        public double dBallContact { get; set; }  // diameter at which the ball touches the flank (mm)
        public bool OddTeeth { get; set; }
        public string? BallWarning { get; set; }

        // --- allowances as supplied ---
        public double Asne { get; set; }
        public double Asni { get; set; }
        public double Tsn { get; set; }           // tooth thickness tolerance A_sne - A_sni (mm)

        public List<string> Notes { get; } = new();
    }

    /// <summary>Preferred ball / pin diameters (mm) - the sizes normally kept in a metrology set.</summary>
    public static readonly double[] PreferredBallSizes =
    {
        0.6, 0.8, 1.0, 1.25, 1.5, 1.75, 2.0, 2.25, 2.5, 2.75, 3.0, 3.5, 4.0, 4.5, 5.0,
        5.5, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 16.0, 18.0, 20.0,
        22.0, 25.0, 28.0, 30.0, 35.0, 40.0, 45.0, 50.0
    };

    // ===================== Main entry point =====================

    public static Result Calculate(GearInput g)
    {
        var r = new Result { Asne = g.Asne, Asni = g.Asni, Tsn = g.Asne - g.Asni };

        if (g.z <= 0 || g.mn <= 0 || g.d <= 0 || g.db <= 0 || g.da <= g.db)
        {
            r.Notes.Add("Invalid geometry - tooth thickness and control dimensions could not be evaluated.");
            return r;
        }

        double alphaNRad = g.alphaN * Math.PI / 180.0;
        double betaRad = g.beta * Math.PI / 180.0;

        // Transverse pressure angle and base helix angle, from the same relations the
        // rest of the engine uses, so the two can never drift apart.
        double alphaTRad = Math.Atan(Math.Tan(alphaNRad) / Math.Cos(betaRad));
        double betaBRad = Math.Asin(Math.Sin(betaRad) * Math.Cos(alphaNRad));
        r.alphaT = alphaTRad * 180.0 / Math.PI;
        r.betaB = betaBRad * 180.0 / Math.PI;

        // ---------- Tooth thickness (ISO 21771, 5.4) ----------
        // s_n = m_n (pi/2 + 2 x tan alpha_n)
        r.sn = g.mn * (Math.PI / 2.0 + 2.0 * g.x * Math.Tan(alphaNRad));
        r.st = r.sn / Math.Cos(betaRad);

        // Transverse tooth thickness at any diameter d_y:
        //   s_yt = d_y (s_t/d + inv alpha_t - inv alpha_yt)
        double invAlphaT = Inv(alphaTRad);
        r.sbt = g.db * (r.st / g.d + invAlphaT);          // at the base circle, inv = 0

        double cosAlphaAt = g.db / g.da;
        cosAlphaAt = Math.Max(-1.0, Math.Min(1.0, cosAlphaAt));
        double alphaAtRad = Math.Acos(cosAlphaAt);
        r.alphaAt = alphaAtRad * 180.0 / Math.PI;
        r.sat = g.da * (r.st / g.d + invAlphaT - Inv(alphaAtRad));

        // Normal tip thickness: the helix angle grows towards the tip, tan beta_a = tan beta * d_a/d
        double betaARad = Math.Atan(Math.Tan(betaRad) * g.da / g.d);
        r.san = r.sat * Math.Cos(betaARad);

        if (r.sat <= 0)
        {
            r.Notes.Add("The tooth is pointed - the tip diameter is above the point where the flanks meet. " +
                        "Reduce the profile shift or the tip diameter.");
        }
        else if (r.san < 0.2 * g.mn)
        {
            r.Notes.Add($"Normal tip tooth thickness s_an = {r.san:F3} mm is below the usual minimum of " +
                        $"0.2 m_n = {0.2 * g.mn:F3} mm. Hardened gears are normally kept above this to avoid " +
                        "a brittle tip.");
        }

        // ---------- Chordal tooth thickness (gear tooth caliper) ----------
        // Measured in the normal section, i.e. on the virtual spur gear.
        double cosBetaB = Math.Cos(betaBRad);
        r.zn = g.z / (cosBetaB * cosBetaB * Math.Cos(betaRad));
        double dn = g.mn * r.zn;
        if (dn > 0)
        {
            double psi = r.sn / dn;                       // half the tooth's angular width, rad
            r.snChordal = dn * Math.Sin(psi);
            double ha = (g.da - g.d) / 2.0;
            r.haChordal = ha + (dn / 2.0) * (1.0 - Math.Cos(psi));
        }

        // ---------- Span measurement W_k (ISO 21771, 5.5) ----------
        CalculateSpan(g, r, alphaNRad, betaRad, alphaTRad, betaBRad, invAlphaT);

        // ---------- Dimension over balls / pins (ISO 21771, 5.6) ----------
        CalculateOverBalls(g, r, alphaNRad, betaRad, alphaTRad, betaBRad, invAlphaT);

        return r;
    }

    // ===================== Span measurement =====================

    private static void CalculateSpan(GearInput g, Result r, double alphaNRad, double betaRad,
                                      double alphaTRad, double betaBRad, double invAlphaT)
    {
        // The base tangent length over k teeth:
        //   W_k = m_n cos(alpha_n) [ pi (k - 0.5) + z inv(alpha_t) ] + 2 x m_n sin(alpha_n)
        // It is linear in the tooth thickness with slope cos(alpha_n), which is why the
        // allowance on W is simply A_sn cos(alpha_n) (see below).
        //
        // Choosing k: the caliper must touch the flank part-way up, so the number of
        // teeth is picked to put the contact at the middle of the usable flank. Solving
        //   W_k = m_n cos(alpha_n) z tan(alpha_y)
        // for k gives
        //   k = 0.5 + [ z (tan(alpha_y) - inv(alpha_t)) - 2 x tan(alpha_n) ] / pi
        // For a standard 20 deg spur gear this reproduces the familiar values
        // (z = 20 -> k = 3, z = 40 -> k = 5).
        double dLower = Math.Max(g.db, g.df);
        double dTarget = (dLower + g.da) / 2.0;

        double tanAlphaY = Math.Sqrt(Math.Max(0, (dTarget / g.db) * (dTarget / g.db) - 1.0));
        double kExact = 0.5 + (g.z * (tanAlphaY - invAlphaT) - 2.0 * g.x * Math.Tan(alphaNRad)) / Math.PI;

        // A drawing may already state the number of teeth to span, and W_k only means anything
        // against the k it was measured over. An explicit value therefore wins; the derived one
        // is what the calculator would choose on its own.
        int k = g.SpanTeeth > 0
            ? g.SpanTeeth
            : (int)Math.Round(kExact, MidpointRounding.AwayFromZero);

        if (k < 1) k = 1;
        if (k > g.z - 1) k = Math.Max(1, g.z - 1);
        r.k = k;
        r.kPreferred = Math.Min(Math.Max((int)Math.Round(kExact, MidpointRounding.AwayFromZero), 1),
                                Math.Max(1, g.z - 1));

        if (g.SpanTeeth > 0 && k != g.SpanTeeth)
        {
            r.Notes.Add($"The requested span of {g.SpanTeeth} teeth is outside 1..{g.z - 1} for this "
                      + $"gear; {k} was used instead.");
        }

        double cosAlphaN = Math.Cos(alphaNRad);
        r.Wk = g.mn * cosAlphaN * (Math.PI * (k - 0.5) + g.z * invAlphaT)
             + 2.0 * g.x * g.mn * Math.Sin(alphaNRad);

        // A tooth thickness deviation A_sn changes W by exactly A_sn cos(alpha_n).
        r.AWe = g.Asne * cosAlphaN;
        r.AWi = g.Asni * cosAlphaN;
        r.WkUpper = r.Wk + r.AWe;
        r.WkLower = r.Wk + r.AWi;

        // Measuring circle: the caliper plane is tangent to the base cylinder, so the
        // contact sits at d = sqrt(d_b^2 + (W_k / cos beta_b)^2).
        double cosBetaB = Math.Cos(betaBRad);
        double wTransverse = cosBetaB > 0 ? r.Wk / cosBetaB : r.Wk;
        r.dMk = Math.Sqrt(g.db * g.db + wTransverse * wTransverse);

        // For a helical gear the span runs diagonally across the face, so the face width
        // has to be at least W_k sin(beta_b) plus room for the caliper jaws.
        r.bMinSpan = r.Wk * Math.Sin(Math.Abs(betaBRad));

        var problems = new List<string>();
        if (r.dMk > g.da - 0.2 * g.mn)
            problems.Add($"the contact would sit at d = {r.dMk:F3} mm, too close to the tip (d_a = {g.da:F3} mm)");
        if (r.dMk < dLower + 0.1 * g.mn)
            problems.Add($"the contact would sit at d = {r.dMk:F3} mm, below the usable flank (from d = {dLower:F3} mm)");
        if (g.b > 0 && r.bMinSpan > 0 && g.b < r.bMinSpan * 1.15)
            problems.Add($"the face width {g.b:F1} mm is too narrow for a span of {r.Wk:F3} mm at " +
                         $"beta_b = {r.betaB:F2} deg (needs about {r.bMinSpan * 1.15:F1} mm)");

        if (problems.Count > 0)
        {
            r.SpanWarning = "Span measurement is not usable on this gear: " + string.Join("; ", problems) +
                            ". Use the dimension over balls instead.";
        }
    }

    // ===================== Dimension over balls / pins =====================

    private static void CalculateOverBalls(GearInput g, Result r, double alphaNRad, double betaRad,
                                           double alphaTRad, double betaBRad, double invAlphaT)
    {
        double cosBetaB = Math.Cos(betaBRad);
        if (cosBetaB <= 0) { r.BallWarning = "Base helix angle out of range."; return; }

        // Half of the angular tooth space at the base cylinder:
        //   eta_b = pi/(2z) - 2 x tan(alpha_n)/z - inv(alpha_t)
        double etaB = Math.PI / (2.0 * g.z) - 2.0 * g.x * Math.Tan(alphaNRad) / g.z - invAlphaT;

        // Best-size ball: the one that touches the flank half way up the usable flank,
        // the same target the span measurement uses. From the ball equilibrium
        //   D_M / (d_b cos beta_b) = inv(alpha_c) + eta_b   with  tan(alpha_y) = alpha_c - eta_b
        //
        // Aiming at the REFERENCE cylinder instead (the textbook "best size" rule) is only
        // right for an unshifted gear: on a positively shifted one it puts the ball so deep
        // that it never reaches past the tip and the measurement cannot be taken at all.
        double dLowerBall = Math.Max(g.db, g.df);
        double dTargetBall = (dLowerBall + g.da) / 2.0;
        double tanAlphaYBest = Math.Sqrt(Math.Max(0, (dTargetBall / g.db) * (dTargetBall / g.db) - 1.0));
        double alphaCBest = etaB + tanAlphaYBest;
        r.DMBest = g.db * cosBetaB * (Inv(alphaCBest) + etaB);

        double DM = g.BallDiameter > 0 ? g.BallDiameter : NearestPreferredBall(r.DMBest);
        r.DM = DM;

        // Ball centre circle from the involute condition above.
        double invAlphaC = invAlphaT + DM / (g.db * cosBetaB) - Math.PI / (2.0 * g.z)
                         + 2.0 * g.x * Math.Tan(alphaNRad) / g.z;

        if (invAlphaC <= 0)
        {
            r.BallWarning = "The chosen ball is too small - it would sit below the base circle. " +
                            $"Use about {r.DMBest:F2} mm.";
            return;
        }

        double alphaCRad = InvertInvolute(invAlphaC);
        r.alphaM = alphaCRad * 180.0 / Math.PI;
        r.dMBallCentre = g.db / Math.Cos(alphaCRad);

        r.OddTeeth = g.z % 2 != 0;
        r.Md = r.OddTeeth
            ? r.dMBallCentre * Math.Cos(Math.PI / (2.0 * g.z)) + DM
            : r.dMBallCentre + DM;

        // Contact diameter: tan(alpha_y) = alpha_c - eta_b (exact for spur gears, and a
        // close approximation for helical gears measured with balls).
        double tanAlphaYM = alphaCRad - etaB;
        r.dBallContact = tanAlphaYM > 0
            ? g.db * Math.Sqrt(1.0 + tanAlphaYM * tanAlphaYM)
            : 0;

        // Allowances: rather than differentiating M_d, the whole calculation is repeated
        // with the thinned tooth. That is exact and needs no linearisation.
        r.MdUpper = MdForThicknessDeviation(g, DM, alphaNRad, alphaTRad, cosBetaB, invAlphaT, g.Asne);
        r.MdLower = MdForThicknessDeviation(g, DM, alphaNRad, alphaTRad, cosBetaB, invAlphaT, g.Asni);

        var problems = new List<string>();
        if (r.Md <= g.da)
            problems.Add($"the ball does not reach past the tip (M_d = {r.Md:F3} mm, d_a = {g.da:F3} mm)");
        if (r.dMBallCentre - DM <= g.df)
            problems.Add($"the ball bottoms out in the root (d_f = {g.df:F3} mm)");
        if (r.dBallContact > 0 && r.dBallContact > g.da)
            problems.Add($"the contact point falls above the tip diameter");
        if (r.dBallContact > 0 && r.dBallContact < g.db)
            problems.Add("the contact point falls below the base circle");

        if (problems.Count > 0)
        {
            r.BallWarning = "Measurement over balls is not usable with this ball size: " +
                            string.Join("; ", problems) + $". The best size for this gear is about {r.DMBest:F2} mm.";
        }

        if (r.OddTeeth && Math.Abs(g.beta) > 0.01)
        {
            r.Notes.Add("Odd tooth count with a helix angle: the dimension over balls uses the standard " +
                        "cos(pi/2z) correction, which is an approximation for helical gears. Prefer an even " +
                        "tooth count, or measure over one ball against the reference cylinder.");
        }
    }

    /// <summary>
    /// M_d recomputed with the tooth thickness changed by <paramref name="deltaSn"/> (mm,
    /// negative for a thinner tooth). Thinning the tooth widens the space, so the ball
    /// drops and M_d falls.
    /// </summary>
    private static double MdForThicknessDeviation(GearInput g, double DM, double alphaNRad,
                                                  double alphaTRad, double cosBetaB,
                                                  double invAlphaT, double deltaSn)
    {
        // s_t/d enters inv(alpha_c) directly; a normal thickness change deltaSn shifts it
        // by deltaSn / (d cos beta).
        double betaRad = g.beta * Math.PI / 180.0;
        double deltaTerm = deltaSn / (g.d * Math.Cos(betaRad));

        double invAlphaC = invAlphaT + DM / (g.db * cosBetaB) - Math.PI / (2.0 * g.z)
                         + 2.0 * g.x * Math.Tan(alphaNRad) / g.z + deltaTerm;

        if (invAlphaC <= 0) return 0;

        double alphaCRad = InvertInvolute(invAlphaC);
        double dCentre = g.db / Math.Cos(alphaCRad);

        return g.z % 2 != 0
            ? dCentre * Math.Cos(Math.PI / (2.0 * g.z)) + DM
            : dCentre + DM;
    }

    /// <summary>Closest preferred ball size to the calculated best size.</summary>
    public static double NearestPreferredBall(double best)
    {
        if (best <= 0) return 0;
        double nearest = PreferredBallSizes[0];
        double bestDiff = Math.Abs(nearest - best);
        foreach (double s in PreferredBallSizes)
        {
            double diff = Math.Abs(s - best);
            if (diff < bestDiff) { bestDiff = diff; nearest = s; }
        }
        return nearest;
    }

    // ===================== Backlash =====================

    public class BacklashResult
    {
        public double jbnMin { get; set; }     // normal backlash, minimum (mm)
        public double jbnMax { get; set; }     // normal backlash, maximum (mm)
        public double jwtMin { get; set; }     // circumferential backlash at the working pitch circle (mm)
        public double jwtMax { get; set; }
        public double jrMin { get; set; }      // radial backlash (mm)
        public double jrMax { get; set; }
        public double jbnRecommended { get; set; } // ISO/TR 10064-2 recommended minimum (mm)
        public double AaUpper { get; set; }    // centre distance deviation used (mm)
        public double AaLower { get; set; }
        public List<string> Notes { get; } = new();
    }

    /// <summary>
    /// Backlash of the pair from the two gears' tooth thickness allowances and the
    /// centre distance deviation.
    ///
    /// A normal tooth thickness deviation A_sn shortens the base tangent by A_sn cos(alpha_n),
    /// and the backlash measured normal to the flanks is the sum of both gears' contributions:
    ///
    ///     j_bn = -(A_sn1 + A_sn2) cos(alpha_n) + 2 A_a sin(alpha_wt) cos(beta_b)
    ///     j_wt = j_bn / (cos(alpha_wt) cos(beta_b))
    ///     j_r  = j_wt / (2 tan(alpha_wt))
    ///
    /// The minimum occurs with the thickest teeth (A_sne) and the smallest centre
    /// distance; the maximum with the thinnest teeth (A_sni) and the largest.
    /// </summary>
    /// <param name="Asne1">Upper tooth thickness allowance, gear 1 (mm, negative)</param>
    /// <param name="Asni1">Lower tooth thickness allowance, gear 1 (mm, negative)</param>
    /// <param name="Asne2">Upper tooth thickness allowance, gear 2 (mm, negative)</param>
    /// <param name="Asni2">Lower tooth thickness allowance, gear 2 (mm, negative)</param>
    /// <param name="AaUpper">Upper centre distance deviation (mm)</param>
    /// <param name="AaLower">Lower centre distance deviation (mm)</param>
    /// <param name="alphaN">Normal pressure angle (deg)</param>
    /// <param name="alphaWt">Working transverse pressure angle (deg)</param>
    /// <param name="betaB">Base helix angle (deg)</param>
    /// <param name="centreDistance">Centre distance a (mm), for the recommended minimum</param>
    /// <param name="mn">Normal module (mm), for the recommended minimum</param>
    public static BacklashResult CalculateBacklash(
        double Asne1, double Asni1, double Asne2, double Asni2,
        double AaUpper, double AaLower,
        double alphaN, double alphaWt, double betaB,
        double centreDistance, double mn)
    {
        var r = new BacklashResult { AaUpper = AaUpper, AaLower = AaLower };

        double cosAlphaN = Math.Cos(alphaN * Math.PI / 180.0);
        double alphaWtRad = alphaWt * Math.PI / 180.0;
        double cosBetaB = Math.Cos(betaB * Math.PI / 180.0);

        r.jbnMin = -(Asne1 + Asne2) * cosAlphaN + 2.0 * AaLower * Math.Sin(alphaWtRad) * cosBetaB;
        r.jbnMax = -(Asni1 + Asni2) * cosAlphaN + 2.0 * AaUpper * Math.Sin(alphaWtRad) * cosBetaB;

        double denom = Math.Cos(alphaWtRad) * cosBetaB;
        if (denom > 0)
        {
            r.jwtMin = r.jbnMin / denom;
            r.jwtMax = r.jbnMax / denom;
        }

        double tanAlphaWt = Math.Tan(alphaWtRad);
        if (tanAlphaWt > 0)
        {
            r.jrMin = r.jwtMin / (2.0 * tanAlphaWt);
            r.jrMax = r.jwtMax / (2.0 * tanAlphaWt);
        }

        // ISO/TR 10064-2 recommended minimum normal backlash for coarse-pitch gears
        // in an industrial gearbox with commercial manufacturing and steel housings.
        r.jbnRecommended = RecommendedMinimumBacklash(centreDistance, mn);

        if (r.jbnMin <= 0)
        {
            r.Notes.Add("Minimum backlash is zero or negative: with the thickest teeth and the smallest " +
                        "centre distance the pair can bind. Increase the tooth thinning (more negative A_sne) " +
                        "or widen the centre distance tolerance.");
        }
        else if (r.jbnMin < r.jbnRecommended)
        {
            r.Notes.Add($"Minimum backlash {r.jbnMin * 1000:F0} um is below the ISO/TR 10064-2 " +
                        $"recommendation of {r.jbnRecommended * 1000:F0} um for this centre distance and module. " +
                        "That is acceptable for a controlled application (low temperature rise, accurate housing), " +
                        "but it leaves little margin for thermal expansion and contamination.");
        }

        return r;
    }

    /// <summary>
    /// Recommended minimum normal backlash, ISO/TR 10064-2:
    ///     j_bn,min = (2/3) (0.06 + 0.0005 a + 0.03 m_n)     [mm]
    /// </summary>
    public static double RecommendedMinimumBacklash(double centreDistance, double mn)
        => (2.0 / 3.0) * (0.06 + 0.0005 * Math.Abs(centreDistance) + 0.03 * mn);

    /// <summary>
    /// Tooth thickness allowances that deliver a target minimum backlash, split evenly
    /// between the two gears:
    ///
    ///     A_sne = -( j_bn,min - 2 A_a,lower sin(alpha_wt) cos(beta_b) ) / (2 cos(alpha_n))
    ///
    /// The tolerance width T_sn has to come from somewhere; the caller supplies it (this
    /// module uses the ISO 1328-1 cumulative pitch tolerance F_p of the gear, a common
    /// practical choice, and says so in the results).
    /// </summary>
    public static (double Asne, double Asni) AllowancesForTargetBacklash(
        double targetJbn, double Tsn, double AaLower, double alphaN, double alphaWt, double betaB)
    {
        double cosAlphaN = Math.Cos(alphaN * Math.PI / 180.0);
        double cosBetaB = Math.Cos(betaB * Math.PI / 180.0);
        double sinAlphaWt = Math.Sin(alphaWt * Math.PI / 180.0);

        if (cosAlphaN <= 0) return (0, 0);

        double asne = -(targetJbn - 2.0 * AaLower * sinAlphaWt * cosBetaB) / (2.0 * cosAlphaN);
        if (asne > 0) asne = 0;    // never a thicker-than-nominal tooth

        return (asne, asne - Math.Abs(Tsn));
    }

    // ===================== Inverses: a measured quantity back to A_sn =====================
    //
    // Everything downstream of the tooth thickness allowance is derived from it, so the
    // engine only ever needs A_sne/A_sni. These four helpers let the *user* work in the
    // quantity they actually have on a drawing or a gauge, and convert.
    //
    // Note the asymmetry, because it decides what the UI has to ask for:
    //   - backlash is a property of the PAIR, so one number constrains the SUM of the two
    //     gears' allowances and a split rule is needed;
    //   - W_k and M_d are measured on ONE gear, so they invert per gear with no split.

    /// <summary>
    /// The sum A_sn1 + A_sn2 that produces a given normal backlash at a given centre
    /// distance deviation. Straight rearrangement of the j_bn relation in
    /// <see cref="CalculateBacklash"/> — keep the two in step.
    /// </summary>
    public static double AllowanceSumForNormalBacklash(
        double jbn, double Aa, double alphaN, double alphaWt, double betaB)
    {
        double cosAlphaN = Math.Cos(alphaN * Math.PI / 180.0);
        if (cosAlphaN <= 0) return 0;

        double cosBetaB = Math.Cos(betaB * Math.PI / 180.0);
        double sinAlphaWt = Math.Sin(alphaWt * Math.PI / 180.0);

        return (2.0 * Aa * sinAlphaWt * cosBetaB - jbn) / cosAlphaN;
    }

    /// <summary>Circumferential backlash at the working pitch circle → normal backlash.</summary>
    public static double NormalBacklashFromCircumferential(double jwt, double alphaWt, double betaB)
        => jwt * Math.Cos(alphaWt * Math.PI / 180.0) * Math.Cos(betaB * Math.PI / 180.0);

    /// <summary>
    /// Radial backlash → normal backlash. j_wt = 2 j_r tan(alpha_wt), and substituting into
    /// the line above leaves j_bn = 2 j_r sin(alpha_wt) cos(beta_b).
    /// </summary>
    public static double NormalBacklashFromRadial(double jr, double alphaWt, double betaB)
        => 2.0 * jr * Math.Sin(alphaWt * Math.PI / 180.0) * Math.Cos(betaB * Math.PI / 180.0);

    /// <summary>
    /// The allowance A_sn that puts the base tangent length at <paramref name="targetWk"/>.
    /// A normal thickness deviation shortens the span by A_sn cos(alpha_n), so this is exact
    /// and needs no iteration. Returns null when the nominal span cannot be evaluated.
    /// </summary>
    public static double? AllowanceForSpan(GearInput g, double targetWk)
    {
        if (targetWk <= 0) return null;

        var nominal = Calculate(Probe(g, 0, 0));
        if (nominal.Wk <= 0) return null;

        double cosAlphaN = Math.Cos(g.alphaN * Math.PI / 180.0);
        if (cosAlphaN <= 0) return null;

        return (targetWk - nominal.Wk) / cosAlphaN;
    }

    /// <summary>
    /// The allowance A_sn that puts the dimension over balls at <paramref name="targetMd"/>.
    ///
    /// Inverted with a secant iteration over the same <c>MdForThicknessDeviation</c> the
    /// results table uses, rather than through a sensitivity factor. M_d is very nearly
    /// linear in A_sn over any realistic allowance so this converges in two or three steps,
    /// and re-using the forward solve means the inverse cannot disagree with the number the
    /// user sees printed next to it. Returns null when it does not converge.
    /// </summary>
    public static double? AllowanceForBallDimension(GearInput g, double targetMd)
    {
        if (targetMd <= 0) return null;

        var probe = Probe(g, 0, 0);
        var nominal = Calculate(probe);
        if (nominal.DM <= 0 || nominal.Md <= 0) return null;

        double alphaNRad = g.alphaN * Math.PI / 180.0;
        double betaRad = g.beta * Math.PI / 180.0;
        double alphaTRad = Math.Atan(Math.Tan(alphaNRad) / Math.Cos(betaRad));
        double cosBetaB = Math.Cos(Math.Asin(Math.Sin(betaRad) * Math.Cos(alphaNRad)));
        double invAlphaT = Inv(alphaTRad);

        double Residual(double asn)
        {
            double md = MdForThicknessDeviation(probe, nominal.DM, alphaNRad, alphaTRad,
                                                cosBetaB, invAlphaT, asn);
            return md <= 0 ? double.NaN : md - targetMd;
        }

        double a0 = 0.0, a1 = -0.02 * g.mn;
        double f0 = Residual(a0), f1 = Residual(a1);

        for (int i = 0; i < 40; i++)
        {
            if (double.IsNaN(f0) || double.IsNaN(f1)) return null;
            if (Math.Abs(f1) < 1e-9) break;

            double slope = f1 - f0;
            if (Math.Abs(slope) < 1e-15) break;

            double a2 = a1 - f1 * (a1 - a0) / slope;
            a0 = a1; f0 = f1;
            a1 = a2; f1 = Residual(a1);
        }

        return !double.IsNaN(f1) && Math.Abs(f1) <= 1e-6 ? a1 : null;
    }

    /// <summary>A copy of the gear with different allowances; GearInput is a class, not a record.</summary>
    private static GearInput Probe(GearInput g, double asne, double asni) => new()
    {
        z = g.z, mn = g.mn, alphaN = g.alphaN, beta = g.beta, x = g.x,
        d = g.d, db = g.db, da = g.da, df = g.df, b = g.b,
        Asne = asne, Asni = asni, BallDiameter = g.BallDiameter,
        // Carry the span across, or inverting a W_k limit would solve against a different k
        // from the one the user measured over.
        SpanTeeth = g.SpanTeeth
    };
}
