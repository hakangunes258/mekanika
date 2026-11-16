namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Disc spring (Tellerfeder) calculation engine.
///
/// Method: DIN 2092 (calculation), with the dimension series, groups and the
/// static stress practice limit of DIN 2093. The underlying theory is
/// Almen-Laszlo: the cross-section is treated as rigid and rotating about a
/// neutral point, so the tangential stress at a material point varies as -z/r.
/// That single statement explains the whole shape of the equations below:
///
///   - the part of the stress that flips sign between the two faces is the same at
///     both radii apart from the 1/r, which gives the exact identity
///     sigma_III - sigma_IV = (sigma_II - sigma_I)/delta. In the normal working
///     range of a DIN 2093 spring that puts the two upper-face points (I, IV) in
///     compression and the two lower-face points (II, III) in tension - though at a
///     large shape number and small deflection the common term can carry all four
///     compressive, so the identity is the invariant and the signs are not;
///   - the magnitude falls as 1/r, which is where the 1/delta on the outer-edge
///     points comes from.
///
/// Both of those are asserted in Tests/DiscSpringTests.cs rather than left as
/// prose, because they are the cheapest check that the four stress formulas have
/// not been transcribed wrongly.
///
/// Notation, all in the DIN 2092 symbols:
///   De, Di   outer / inner diameter, mm
///   t        material thickness, mm
///   t'       reduced thickness of a spring WITH contact surfaces, mm
///   l0       free overall height, mm
///   h0       free cone height = l0 - t' , mm (= the deflection to flat)
///   s        deflection of a SINGLE disc, mm
///   delta    De/Di
/// </summary>
public class DiscSpringEngine
{
    // ============ INPUT PARAMETERS ============

    // Geometry
    public double OuterDiameter { get; set; }          // De (mm)
    public double InnerDiameter { get; set; }          // Di (mm)
    public double Thickness { get; set; }              // t  (mm)
    public double FreeHeight { get; set; }             // l0 (mm) - overall free height

    /// <summary>
    /// True for a spring with contact surfaces and a reduced thickness, i.e. the
    /// DIN 2093 Group 3 construction. When false t' = t and K4 = 1, which is what
    /// makes the plain spring a special case of the same equations rather than a
    /// second code path.
    /// </summary>
    public bool HasContactSurfaces { get; set; }

    /// <summary>
    /// t' (mm), the reduced thickness. Only read when <see cref="HasContactSurfaces"/>
    /// is set. 0 means "use the usual 0,94 t", which is the ratio the Group 3 sizes
    /// of DIN 2093 are built around.
    /// </summary>
    public double ReducedThickness { get; set; }

    // Material
    public double ElasticModulus { get; set; } = 206000;   // E (MPa)
    public double PoissonRatio { get; set; } = 0.30;       // nu (-)
    public double Density { get; set; } = 7.85e-3;         // g/mm^3
    public string MaterialGrade { get; set; } = "51CrV4";

    /// <summary>
    /// Permissible value for the CALCULATED stress at point I at the flat position,
    /// MPa. 2600 is the DIN 2093 practice figure for spring steel; it is far above
    /// the yield strength on purpose - sigma_I is a theoretical edge stress that a
    /// real disc relieves by local yielding (setting) on its first few strokes.
    /// Editable, because it is a material property and the module takes other
    /// materials.
    /// </summary>
    public double PermissibleStaticStress { get; set; } = 2600;

    /// <summary>
    /// sigma_O,zul (MPa) - the permissible UPPER stress for a fatigue application,
    /// read off the DIN 2093 fatigue diagram for the spring's group at the actual
    /// lower stress. 0 means "not supplied", and the fatigue rows then say so
    /// instead of inventing a limit. The diagrams themselves are not digitised -
    /// see the note on this in CLAUDE.md.
    /// </summary>
    public double PermissibleUpperStress { get; set; }

    // Loading
    public DiscSpringLoadInput LoadInput { get; set; } = DiscSpringLoadInput.Deflection;

    /// <summary>s1 (mm) - deflection of a single disc at the lower working position.</summary>
    public double Deflection1 { get; set; }
    /// <summary>s2 (mm) - deflection of a single disc at the upper working position.</summary>
    public double Deflection2 { get; set; }
    /// <summary>F1 (N) - force on the STACK at the lower position, when working from force.</summary>
    public double Force1Input { get; set; }
    /// <summary>F2 (N) - force on the STACK at the upper position, when working from force.</summary>
    public double Force2Input { get; set; }

    // Stack
    /// <summary>n - discs nested in parallel in one set. Multiplies force.</summary>
    public int ParallelCount { get; set; } = 1;
    /// <summary>i - sets stacked in series in the column. Multiplies deflection.</summary>
    public int SeriesCount { get; set; } = 1;

    // ============ CALCULATED - GEOMETRY ============

    public double DiameterRatio { get; set; }        // delta = De/Di
    public double CalcThickness { get; set; }        // t' used in the equations (= t when plain)
    public double ThicknessRatio { get; set; }       // t'/t
    public double ConeHeight { get; set; }           // h0 = l0 - t'
    public double FlatDeflection { get; set; }       // sc = h0
    public double H0OverT { get; set; }              // h0/t' - the shape number
    public double DeOverT { get; set; }              // De/t
    public double L0OverT { get; set; }              // l0/t
    public double RadialWidth { get; set; }          // (De - Di)/2
    public double ConeAngle { get; set; }            // degrees, free state
    public double Mass { get; set; }                 // g, one disc

    public double K1 { get; set; }
    public double K2 { get; set; }
    public double K3 { get; set; }
    public double K4 { get; set; } = 1.0;
    /// <summary>True when K4 had to be forced to 1 because the t'/t, l0/t pair is out of range.</summary>
    public bool K4FellBack { get; set; }

    /// <summary>DIN 2093 thickness group: 1 (t &lt;= 1,25), 2 (&lt;= 6), 3 (&lt;= 14), 0 outside.</summary>
    public int ThicknessGroup { get; set; }
    /// <summary>Which DIN 2093 series the shape number h0/t resembles: "A", "B", "C" or "" .</summary>
    public string SeriesMatch { get; set; } = "";

    // ============ CALCULATED - CHARACTERISTIC ============

    public DiscSpringPoint Position1 { get; set; } = new();
    public DiscSpringPoint Position2 { get; set; } = new();
    /// <summary>The DIN 2092 recommended travel limit, s = 0,75 h0.</summary>
    public DiscSpringPoint RecommendedLimit { get; set; } = new();
    /// <summary>Flat, s = h0. F here is Fc.</summary>
    public DiscSpringPoint Flat { get; set; } = new();
    /// <summary>s/h0 = 0,25 / 0,50 / 0,75 / 1,00, for the characteristic table.</summary>
    public List<DiscSpringPoint> Curve { get; set; } = new();

    public double WorkingStroke { get; set; }        // s2 - s1, single disc (mm)
    public double WorkingForce { get; set; }         // F2 - F1, stack (N)
    public double SecantRate { get; set; }           // (F2-F1)/(s2-s1), stack (N/mm)

    /// <summary>
    /// The characteristic turns over (the tangent rate reaches zero before flat)
    /// when K4.h0/t' &gt; sqrt(2). Exact, not a rule of thumb: the tangent rate is
    /// proportional to K4^2(a^2 - 3ax + 1.5x^2) + 1 with a = h0/t', x = s/t', whose
    /// minimum over [0, a] sits at x = a and equals 1 - 0.5 K4^2 a^2.
    /// </summary>
    public bool CharacteristicTurnsOver { get; set; }
    public double TurnOverShapeLimit { get; set; }   // the sqrt(2)/K4 that h0/t' is compared against

    // ============ CALCULATED - STACK ============

    public double StackFreeLength { get; set; }      // L0 (mm)
    public double StackSolidLength { get; set; }     // i.n.t (mm)
    public double StackMaxDeflection { get; set; }   // i.h0 (mm)
    public double StackMass { get; set; }            // g
    public double StackRate { get; set; }            // N/mm at position 2 (tangent)
    public int DiscCount { get; set; }               // n.i

    // ============ CALCULATED - STRENGTH ============

    /// <summary>|sigma_I| at the flat position - the figure DIN 2093's limit is stated at.</summary>
    public double StaticStressAtFlat { get; set; }
    /// <summary>Permissible / |sigma_I| at FLAT. Answers "may this disc ever be fully compressed".</summary>
    public double SafetyFactorStatic { get; set; }

    /// <summary>|sigma_I| at the upper working position s2.</summary>
    public double StaticStressAtPosition2 { get; set; }
    /// <summary>
    /// Permissible / |sigma_I| at s2 - the static check that governs the design AS
    /// SPECIFIED, and the one to read first.
    ///
    /// Both exist because reporting only the flat-position factor marks a perfectly
    /// usable design as a failure. DIN 2093 states its 2600 MPa figure at s = h0, so
    /// that is where it has to be evaluated - but a series A disc is already past it
    /// at flat (2680 MPa on the module's own worked example) while sitting at 2086 MPa
    /// at the 0,75 h0 travel limit. One number cannot say both things, and a red FAIL
    /// on a design that never approaches flat is the interface bug this repo's
    /// November 2026 review kept finding.
    /// </summary>
    public double SafetyFactorAtPosition2 { get; set; }
    /// <summary>True when s2 reaches the flat position, so the flat-position check is the live one.</summary>
    public bool WorksToFlat { get; set; }

    /// <summary>"II" or "III" - the tensile point with the larger stress at position 2.</summary>
    public string GoverningFatiguePoint { get; set; } = "";
    public double UpperStress { get; set; }          // sigma_o, at s2 (MPa)
    public double LowerStress { get; set; }          // sigma_u, at s1 (MPa)
    public double StressRange { get; set; }          // sigma_h = sigma_o - sigma_u (MPa)
    public bool FatigueLimitSupplied { get; set; }
    public double SafetyFactorFatigue { get; set; }

    // ============ REFERENCE DATA ============

    /// <summary>
    /// E, nu, density and a representative tensile strength per material. DIN 2093
    /// itself is written for 51CrV4 / C60S; the others are the materials DIN 2092
    /// names for disc springs outside that scope. The numbers are typical values
    /// for the grade and every one of them is editable on the page - a disc spring
    /// result is far more sensitive to E and nu than to the grade name.
    /// </summary>
    public static readonly Dictionary<string, DiscSpringMaterial> Materials = new()
    {
        ["51CrV4"] = new("51CrV4 / C60S - DIN 2093 spring steel", 206000, 0.30, 7.85e-3, 1500),
        ["1.4568"] = new("X7CrNiAl17-7 (1.4568) - stainless", 195000, 0.33, 7.80e-3, 1250),
        ["1.4310"] = new("X10CrNi18-8 (1.4310) - stainless", 185000, 0.33, 7.90e-3, 1300),
        ["CuSn8"] = new("CuSn8 - phosphor bronze", 115000, 0.35, 8.80e-3, 700),
        ["CuBe2"] = new("CuBe2 - beryllium copper", 120000, 0.35, 8.30e-3, 1200),
        ["Inconel"] = new("Inconel X-750 - high temperature", 214000, 0.29, 8.25e-3, 1250),
    };

    /// <summary>
    /// The three DIN 2093 series, by the two ratios that define them. Used only to
    /// tell the user which series their free-entered spring resembles - the size
    /// table itself is deliberately NOT embedded here (see CLAUDE.md).
    /// </summary>
    public static readonly (string Series, double H0OverT, double DeOverT, string Note)[] SeriesGuide =
    {
        ("A", 0.40, 18, "stiff, short travel - the usual choice for high load"),
        ("B", 0.75, 28, "medium"),
        ("C", 1.30, 40, "soft, long travel - close to the turn-over limit"),
    };

    // ============ SHARED WITH THE PAGE ============

    /// <summary>
    /// t' - the thickness the equations use. Public and static because the page has
    /// to validate h0 = l0 - t' &gt; 0 BEFORE Calculate runs, and a second copy of
    /// this rule in the page is exactly the drift this repo keeps being bitten by.
    /// </summary>
    public static double EffectiveThickness(double thickness, bool hasContactSurfaces, double reducedThickness)
    {
        if (!hasContactSurfaces) return thickness;
        double t = reducedThickness > 0 ? reducedThickness : 0.94 * thickness;
        return t > thickness ? thickness : t;
    }

    // ============ MAIN CALCULATION ============

    public void Calculate()
    {
        CalculateGeometry();
        CalculateFactors();
        ResolvePositions();
        CalculatePoints();
        CalculateStack();
        CalculateStrength();
    }

    // ============ STEPS ============

    private void CalculateGeometry()
    {
        DiameterRatio = SafeDivide(OuterDiameter, InnerDiameter);
        RadialWidth = (OuterDiameter - InnerDiameter) / 2.0;

        // t' = t for a spring without contact surfaces, which is what collapses K4
        // to 1 and every formula below to its plain form.
        CalcThickness = EffectiveThickness(Thickness, HasContactSurfaces, ReducedThickness);
        ThicknessRatio = SafeDivide(CalcThickness, Thickness);

        // h0 is measured with the thickness the equations use, so a Group 3 spring
        // of the same free height has a slightly LARGER cone height than its
        // nominal thickness would suggest.
        ConeHeight = FreeHeight - CalcThickness;
        FlatDeflection = ConeHeight;

        H0OverT = SafeDivide(ConeHeight, CalcThickness);
        DeOverT = SafeDivide(OuterDiameter, Thickness);
        L0OverT = SafeDivide(FreeHeight, Thickness);

        ConeAngle = RadialWidth > 0
            ? Math.Atan2(ConeHeight, RadialWidth) * 180.0 / Math.PI
            : 0;

        double area = Math.PI / 4.0 * (OuterDiameter * OuterDiameter - InnerDiameter * InnerDiameter);
        Mass = area * Thickness * Density;

        ThicknessGroup = Thickness <= 0 ? 0
            : Thickness <= 1.25 ? 1
            : Thickness <= 6.0 ? 2
            : Thickness <= 14.0 ? 3
            : 0;

        SeriesMatch = "";
        if (H0OverT > 0)
        {
            var best = SeriesGuide.OrderBy(g => Math.Abs(g.H0OverT - H0OverT)).First();
            // Only claim a resemblance when it is one. A spring halfway between two
            // series is not series A because A happens to be marginally nearer.
            // 0,12 and not 0,20: the A-B gap is 0,35, so a 0,20 window overlaps the
            // midpoint and NOTHING can ever come back unmatched - the guard would be
            // decoration. A test pins that an in-between shape is reported as such.
            if (Math.Abs(best.H0OverT - H0OverT) <= 0.12) SeriesMatch = best.Series;
        }
    }

    private void CalculateFactors()
    {
        double d = DiameterRatio;
        if (d <= 1.0)
        {
            K1 = K2 = K3 = 0;
            K4 = 1;
            return;
        }

        double lnd = Math.Log(d);

        // DIN 2092. The 1/pi on K1 and the 6/pi, 3/pi on K2, K3 are part of the
        // factors as the standard prints them; dropping them scales K1 by pi and
        // still produces a plausible-looking force, which is why the test file
        // pins all three against their closed forms at delta = 2.
        K1 = (1.0 / Math.PI) * Math.Pow((d - 1.0) / d, 2) / ((d + 1.0) / (d - 1.0) - 2.0 / lnd);
        K2 = (6.0 / Math.PI) * ((d - 1.0) / lnd - 1.0) / lnd;
        K3 = (3.0 / Math.PI) * (d - 1.0) / lnd;

        CalculateK4();

        TurnOverShapeLimit = K4 > 0 ? Math.Sqrt(2.0) / K4 : Math.Sqrt(2.0);
        CharacteristicTurnsOver = H0OverT > TurnOverShapeLimit;
    }

    /// <summary>
    /// K4, the correction for a spring with contact surfaces and reduced thickness
    /// (DIN 2092). K4 = 1 for every spring without them.
    ///
    /// The anchor that makes this trustworthy without the standard's own worked
    /// example: substituting t' = t collapses C1 to 1/u and C2 to 1 + 1/u with
    /// u = 5/32 (l0/t - 1)^2, and then
    ///     K4^2 = -1/(2u) + sqrt(1/(4u)^2 ... ) = -1/(2u) + (1/(2u) + 1) = 1
    /// exactly, for ANY l0/t. A transcription error in either C1 or C2 breaks that
    /// identity, so Tests/DiscSpringTests.cs checks it across a sweep of l0/t.
    /// </summary>
    private void CalculateK4()
    {
        K4 = 1.0;
        K4FellBack = false;

        if (!HasContactSurfaces || ThicknessRatio >= 1.0 || ThicknessRatio <= 0) return;

        double tr = ThicknessRatio;
        double lt = L0OverT;

        double a = 0.25 * lt - tr + 0.75;
        double b = 0.625 * lt - tr + 0.375;
        if (a <= 0 || b <= 0)
        {
            K4FellBack = true;
            return;
        }

        double c1 = tr * tr / (a * b);
        double c2 = c1 / (tr * tr * tr) * (5.0 / 32.0 * Math.Pow(lt - 1.0, 2) + 1.0);

        double inner = c1 * c1 / 4.0 + c2;
        if (inner < 0)
        {
            K4FellBack = true;
            return;
        }

        double k4sq = -c1 / 2.0 + Math.Sqrt(inner);
        if (k4sq <= 0)
        {
            K4FellBack = true;
            return;
        }

        K4 = Math.Sqrt(k4sq);
    }

    /// <summary>
    /// Fills s1 and s2 from whichever pair the user supplied. Working from force
    /// inverts the force equation numerically.
    /// </summary>
    private void ResolvePositions()
    {
        if (LoadInput != DiscSpringLoadInput.Force) return;

        // The stack carries n discs in parallel, so one disc sees F/n.
        double n = Math.Max(1, ParallelCount);
        Deflection1 = DeflectionForForce(Force1Input / n);
        Deflection2 = DeflectionForForce(Force2Input / n);
    }

    private void CalculatePoints()
    {
        Position1 = PointAt(Deflection1);
        Position2 = PointAt(Deflection2);
        RecommendedLimit = PointAt(0.75 * ConeHeight);
        Flat = PointAt(ConeHeight);

        Curve.Clear();
        foreach (var r in new[] { 0.25, 0.50, 0.75, 1.00 })
            Curve.Add(PointAt(r * ConeHeight));

        WorkingStroke = Deflection2 - Deflection1;
        WorkingForce = Position2.StackForce - Position1.StackForce;
        SecantRate = SafeDivide(WorkingForce, WorkingStroke * Math.Max(1, SeriesCount));
    }

    private void CalculateStack()
    {
        int n = Math.Max(1, ParallelCount);
        int i = Math.Max(1, SeriesCount);

        DiscCount = n * i;
        // Each extra disc nested in parallel adds its MATERIAL thickness t to the
        // column, not the reduced thickness - t' describes the ground contact flat,
        // not how thick the disc is.
        StackFreeLength = i * (FreeHeight + (n - 1) * Thickness);
        StackSolidLength = DiscCount * Thickness;
        StackMaxDeflection = i * ConeHeight;
        StackMass = DiscCount * Mass;
        StackRate = Position2.Rate * n / i;
    }

    private void CalculateStrength()
    {
        StaticStressAtFlat = Math.Abs(Flat.StressI);
        SafetyFactorStatic = SafeDivide(PermissibleStaticStress, StaticStressAtFlat);

        StaticStressAtPosition2 = Math.Abs(Position2.StressI);
        SafetyFactorAtPosition2 = SafeDivide(PermissibleStaticStress, StaticStressAtPosition2);

        WorksToFlat = ConeHeight > 0 && Deflection2 >= ConeHeight - ConeHeight * 1e-9;

        // DIN 2093 assesses fatigue on sigma_II or sigma_III, whichever is the
        // larger. Which one that is depends on the shape number, so it is decided
        // at the UPPER position and then the lower stress is read at the SAME
        // point - a range taken between two different points is not a range.
        if (Position2.StressII >= Position2.StressIII)
        {
            GoverningFatiguePoint = "II";
            UpperStress = Position2.StressII;
            LowerStress = Position1.StressII;
        }
        else
        {
            GoverningFatiguePoint = "III";
            UpperStress = Position2.StressIII;
            LowerStress = Position1.StressIII;
        }

        StressRange = UpperStress - LowerStress;

        FatigueLimitSupplied = PermissibleUpperStress > 0;
        SafetyFactorFatigue = FatigueLimitSupplied
            ? SafeDivide(PermissibleUpperStress, UpperStress)
            : 0;
    }

    // ============ THE EQUATIONS ============

    /// <summary>
    /// Force on ONE disc at deflection s, DIN 2092:
    ///
    ///   F = 4E/(1-nu^2) . t'^4/(K1 De^2) . K4^2 . s/t'
    ///       . [ K4^2 (h0/t' - s/t')(h0/t' - s/(2t')) + 1 ]
    ///
    /// With K4 = 1 and t' = t this is the plain form for a spring without contact
    /// surfaces, which is why there is one method and not two.
    /// </summary>
    public double ForceAt(double s)
    {
        if (CalcThickness <= 0 || K1 <= 0 || OuterDiameter <= 0) return 0;

        double x = s / CalcThickness;
        double a = H0OverT;
        double pre = 4.0 * ElasticModulus / (1.0 - PoissonRatio * PoissonRatio)
                     * Math.Pow(CalcThickness, 4) / (K1 * OuterDiameter * OuterDiameter)
                     * K4 * K4;

        return pre * x * (K4 * K4 * (a - x) * (a - x / 2.0) + 1.0);
    }

    /// <summary>
    /// Tangent rate dF/ds of ONE disc at deflection s - the derivative of
    /// <see cref="ForceAt"/>, not a secant through two points. A disc spring's
    /// characteristic is a cubic, so the two differ, and which one is wanted
    /// depends on the question: the tangent for stability and resonance, the
    /// secant for "what does this stroke cost me in force".
    /// </summary>
    public double RateAt(double s)
    {
        if (CalcThickness <= 0 || K1 <= 0 || OuterDiameter <= 0) return 0;

        double x = s / CalcThickness;
        double a = H0OverT;
        double pre = 4.0 * ElasticModulus / (1.0 - PoissonRatio * PoissonRatio)
                     * Math.Pow(CalcThickness, 3) / (K1 * OuterDiameter * OuterDiameter)
                     * K4 * K4;

        return pre * (K4 * K4 * (a * a - 3.0 * a * x + 1.5 * x * x) + 1.0);
    }

    /// <summary>
    /// Energy stored in ONE disc at deflection s. The exact integral of
    /// <see cref="ForceAt"/>, which collapses to a square:
    ///   W = 2E/(1-nu^2) . t'^5/(K1 De^2) . K4^2 . (s/t')^2 . [K4^2 (h0/t' - s/2t')^2 + 1]
    /// </summary>
    public double EnergyAt(double s)
    {
        if (CalcThickness <= 0 || K1 <= 0 || OuterDiameter <= 0) return 0;

        double x = s / CalcThickness;
        double a = H0OverT;
        double pre = 2.0 * ElasticModulus / (1.0 - PoissonRatio * PoissonRatio)
                     * Math.Pow(CalcThickness, 5) / (K1 * OuterDiameter * OuterDiameter)
                     * K4 * K4;

        return pre * x * x * (K4 * K4 * Math.Pow(a - x / 2.0, 2) + 1.0);
    }

    /// <summary>
    /// The four DIN 2092 corner stresses at deflection s, MPa. Points I and IV sit
    /// on the upper face (compressive), II and III on the lower face (tensile);
    /// I and II are at the inner edge, III and IV at the outer edge, which is where
    /// the 1/delta comes from.
    /// </summary>
    public (double I, double II, double III, double IV) StressesAt(double s)
    {
        if (CalcThickness <= 0 || K1 <= 0 || OuterDiameter <= 0 || DiameterRatio <= 1)
            return (0, 0, 0, 0);

        // The common factor: 4E/(1-nu^2) . t'^2/(K1 De^2) . s/t'  =  4E t' s / ((1-nu^2) K1 De^2)
        double c = 4.0 * ElasticModulus / (1.0 - PoissonRatio * PoissonRatio)
                   * CalcThickness * s / (K1 * OuterDiameter * OuterDiameter);

        double x = H0OverT - s / (2.0 * CalcThickness);   // h0/t' - s/(2t')
        double inner = K4 * K2 * x;
        double outer = K4 * (K2 - 2.0 * K3) * x;
        double invDelta = 1.0 / DiameterRatio;

        double sI = -c * K4 * (inner + K3);
        double sII = -c * K4 * (inner - K3);
        double sIII = c * K4 * invDelta * (outer + K3);
        double sIV = c * K4 * invDelta * (outer - K3);

        return (sI, sII, sIII, sIV);
    }

    private DiscSpringPoint PointAt(double s)
    {
        var (sI, sII, sIII, sIV) = StressesAt(s);
        int n = Math.Max(1, ParallelCount);
        int i = Math.Max(1, SeriesCount);
        double f = ForceAt(s);

        return new DiscSpringPoint
        {
            Deflection = s,
            DeflectionRatio = SafeDivide(s, ConeHeight),
            Force = f,
            Rate = RateAt(s),
            Energy = EnergyAt(s),
            StressI = sI,
            StressII = sII,
            StressIII = sIII,
            StressIV = sIV,
            StackForce = f * n,
            StackDeflection = s * i,
            StackLength = StackFreeLengthFor(s),
        };
    }

    private double StackFreeLengthFor(double s)
    {
        int n = Math.Max(1, ParallelCount);
        int i = Math.Max(1, SeriesCount);
        return i * (FreeHeight - s + (n - 1) * Thickness);
    }

    /// <summary>
    /// Inverts <see cref="ForceAt"/> on [0, h0] by bisection. Bisection rather than
    /// Newton because the characteristic can turn over: where it does there are two
    /// deflections for one force, and the scan below returns the FIRST (smallest)
    /// root, which is the branch a stack actually sits on. The page says so when
    /// <see cref="CharacteristicTurnsOver"/> is set rather than quietly picking one.
    /// </summary>
    public double DeflectionForForce(double force)
    {
        if (force <= 0 || ConeHeight <= 0) return 0;
        if (force >= ForceAt(ConeHeight) && !CharacteristicTurnsOver) return ConeHeight;

        const int steps = 400;
        double prevS = 0, prevF = 0;
        for (int k = 1; k <= steps; k++)
        {
            double s = ConeHeight * k / steps;
            double f = ForceAt(s);
            if ((prevF - force) * (f - force) <= 0 && f != prevF)
            {
                // Bisect inside the bracket.
                double lo = prevS, hi = s;
                for (int j = 0; j < 60; j++)
                {
                    double mid = 0.5 * (lo + hi);
                    if ((ForceAt(lo) - force) * (ForceAt(mid) - force) <= 0) hi = mid; else lo = mid;
                }
                return 0.5 * (lo + hi);
            }
            prevS = s; prevF = f;
        }
        return ConeHeight;
    }

    // ============ VALIDATION ============

    /// <summary>
    /// Design warnings, shown above the results. These are not reasons to refuse a
    /// calculation - the page's own validation does that - but things a disc spring
    /// design goes wrong on.
    /// </summary>
    public List<string> Validate()
    {
        var w = new List<string>();

        if (DiameterRatio < 1.5 || DiameterRatio > 3.5)
            w.Add($"Diameter ratio delta = {DiameterRatio:F2} is outside the usual 1,5-3,5. " +
                  "DIN 2093 sizes sit near 2,0-2,3; the Almen-Laszlo theory loses accuracy as delta approaches 1.");

        // The comparisons carry a relative tolerance because the two interesting
        // deflections are the limits themselves. s₂ typed as 0.675 against a
        // computed 0.75 x 0.9 differ in the last bit of a double, which fired the
        // warning AT the recommended limit - and printed "0.68 mm is above 0.68 mm".
        double tol = Math.Max(ConeHeight, 1.0) * 1e-9;

        if (Deflection2 > ConeHeight + tol)
            w.Add($"Deflection s₂ = {Deflection2:F3} mm exceeds the flat position h₀ = {ConeHeight:F3} mm. " +
                  "Past flat the disc bears on its edges and none of these equations apply.");
        else if (Deflection2 > 0.75 * ConeHeight + tol)
            w.Add($"Deflection s₂ = {Deflection2:F3} mm is above the 0.75 h₀ = {0.75 * ConeHeight:F3} mm " +
                  "that DIN 2092 recommends as the travel limit. The characteristic and the stresses are both " +
                  "least reliable in the last quarter of the travel.");

        if (CharacteristicTurnsOver)
            w.Add($"Shape number h₀/t' = {H0OverT:F2} is above {TurnOverShapeLimit:F2}, so the rate reaches zero " +
                  "before the flat position: the force falls again over the last part of the travel and the spring " +
                  "can snap through. This is a stability property of the shape, not of the load.");

        if (ThicknessGroup == 0 && Thickness > 0)
            w.Add($"Thickness t = {Thickness:F2} mm is outside the DIN 2093 range (up to 14 mm), so the group, the " +
                  "contact-surface construction and the 2600 MPa static figure are all extrapolations.");

        if (ThicknessGroup == 3 && !HasContactSurfaces)
            w.Add("Group 3 thickness (above 6 mm) without contact surfaces. DIN 2093 gives this group reduced-thickness " +
                  "contact surfaces; without them the load is carried on a sharp edge and K4 = 1 overstates the force.");

        // Two different statements, and conflating them marks a usable design as a
        // failure. The first is about the design; the second is about the disc.
        if (SafetyFactorAtPosition2 > 0 && SafetyFactorAtPosition2 < 1.0)
            w.Add($"Calculated stress at point I at the working position is {StaticStressAtPosition2:F0} MPa " +
                  $"against a permissible {PermissibleStaticStress:F0} MPa. This design is overstressed as specified.");
        else if (SafetyFactorStatic > 0 && SafetyFactorStatic < 1.0 && !WorksToFlat)
            w.Add($"At the working position the disc is within the permissible stress, but at the FLAT position " +
                  $"point I would reach {StaticStressAtFlat:F0} MPa against a permissible " +
                  $"{PermissibleStaticStress:F0} MPa. Nothing must be able to compress it that far - fit a stop. " +
                  "This is normal for a series A shape and is most of the reason DIN 2092 limits the travel.");

        if (Deflection1 > 0 && Deflection1 < 0.15 * ConeHeight - tol)
            w.Add($"Lower position s₁ = {Deflection1:F3} mm is under 0.15 h₀. A fatigue-loaded stack is usually " +
                  "preloaded past that point, both to keep the discs seated and to lift the whole cycle away from zero stress.");

        if (ParallelCount > 4)
            w.Add($"{ParallelCount} discs in parallel. Friction between nested discs is not modelled here, and it grows " +
                  "with every interface - above about 3 or 4 the hysteresis is no longer a small correction.");

        return w;
    }

    private static double SafeDivide(double a, double b) => b != 0 ? a / b : 0;
}

/// <summary>Whether the working positions are given as deflections or as stack forces.</summary>
public enum DiscSpringLoadInput
{
    Deflection,
    Force
}

/// <summary>One point on the characteristic: a deflection and everything that follows from it.</summary>
public class DiscSpringPoint
{
    public double Deflection { get; set; }       // s, one disc (mm)
    public double DeflectionRatio { get; set; }  // s/h0
    public double Force { get; set; }            // F, one disc (N)
    public double Rate { get; set; }             // dF/ds, one disc (N/mm)
    public double Energy { get; set; }           // W, one disc (Nmm)

    public double StressI { get; set; }          // MPa, upper face at Di  (compressive)
    public double StressII { get; set; }         // MPa, lower face at Di  (tensile)
    public double StressIII { get; set; }        // MPa, lower face at De  (tensile)
    public double StressIV { get; set; }         // MPa, upper face at De  (compressive)

    public double StackForce { get; set; }       // N
    public double StackDeflection { get; set; }  // mm
    public double StackLength { get; set; }      // mm, loaded length of the column
}

/// <summary>A disc spring material: the two elastic constants the equations need, plus density.</summary>
public record DiscSpringMaterial(string Name, double E, double Nu, double Density, double TensileStrength);
