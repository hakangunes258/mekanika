namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Moment of Inertia calculation engine for various cross-sections
/// Supports 8 fundamental cross-section shapes for structural analysis
/// All shapes are centered at origin (0,0)
/// </summary>
public class MomentOfInertiaEngine
{
    // ============ INPUT PARAMETERS ============

    public ShapeType SelectedShape { get; set; } = ShapeType.Rectangle;

    // Rectangle / Hollow Rectangle
    public double Width { get; set; }              // b (mm)
    public double Height { get; set; }             // h (mm)
    public double InnerWidth { get; set; }         // b_i (mm) - for hollow rectangle
    public double InnerHeight { get; set; }        // h_i (mm) - for hollow rectangle

    // Circle / Hollow Circle
    public double Diameter { get; set; }           // d (mm)
    public double InnerDiameter { get; set; }      // d_i (mm) - for hollow circle

    // I-Beam (H-Beam)
    public double FlangeWidth { get; set; }        // b_f (mm)
    public double TotalHeight { get; set; }        // h (mm)
    public double FlangeThickness { get; set; }    // t_f (mm)
    public double WebThickness { get; set; }       // t_w (mm)

    // C-Channel
    public double ChannelWidth { get; set; }       // b (mm)
    public double ChannelHeight { get; set; }      // h (mm)
    public double ChannelFlangeThickness { get; set; }  // t_f (mm)
    public double ChannelWebThickness { get; set; }     // t_w (mm)

    // T-Section
    public double TFlangeWidth { get; set; }       // b_f (mm)
    public double THeight { get; set; }            // h (mm)
    public double TFlangeThickness { get; set; }   // t_f (mm)
    public double TStemThickness { get; set; }     // t_w (mm)

    // L-Section (Angle)
    public double LegWidth { get; set; }           // b (mm)
    public double LegHeight { get; set; }          // h (mm)
    public double LegThickness { get; set; }       // t (mm)

    // ============ CALCULATED VALUES ============

    // Area
    public double Area { get; set; }               // mm²

    // Moment of Inertia
    public double Ix { get; set; }                 // mm⁴ (about X-axis)
    public double Iy { get; set; }                 // mm⁴ (about Y-axis)
    public double Ixy { get; set; }                // mm⁴ (product of inertia)
    public double Ip { get; set; }                 // mm⁴ (Polar moment of inertia)

    // Principal axes
    public double IuPrincipal { get; set; }        // mm⁴ (major principal, max)
    public double IvPrincipal { get; set; }        // mm⁴ (minor principal, min - governs bending)
    public double PrincipalAngle { get; set; }     // degrees, from X to the major principal axis
    public bool AxesArePrincipal { get; set; }     // true when Ixy = 0 (X/Y are principal)

    // Section Modulus
    public double Wx { get; set; }                 // mm³ (about X-axis, governing fibre)
    public double Wy { get; set; }                 // mm³ (about Y-axis, governing fibre)

    // Per-fibre section moduli. On an asymmetric section these differ by a
    // factor of two or more, and only the smaller is safe for a symmetric
    // allowable stress.
    public double WxTop { get; set; }              // mm³
    public double WxBottom { get; set; }           // mm³
    public double WyLeft { get; set; }             // mm³
    public double WyRight { get; set; }            // mm³
    public bool SectionIsAsymmetricAboutX { get; set; }
    public bool SectionIsAsymmetricAboutY { get; set; }

    // Radius of Gyration
    public double rx { get; set; }                 // mm (about X-axis)
    public double ry { get; set; }                 // mm (about Y-axis)
    /// <summary>Radius of gyration about the MAJOR principal axis (mm).</summary>
    public double ru { get; set; }
    /// <summary>
    /// Radius of gyration about the MINOR principal axis (mm). This is the one a
    /// column buckles about — not min(rx, ry), which is only the same thing when
    /// X and Y are themselves principal axes.
    /// </summary>
    public double rv { get; set; }

    /// <summary>
    /// St Venant torsion constant (mm^4). NOT the same as Ip except for a solid
    /// or hollow circle. Zero when no expression applies to the shape.
    /// </summary>
    public double It { get; set; }
    /// <summary>How It was obtained, for the results to state.</summary>
    public string TorsionConstantBasis { get; set; } = "";

    // Centroid distances (from origin - always 0 for symmetric sections)
    public double CentroidY { get; set; }          // mm (Y distance from origin to centroid)
    public double CentroidX { get; set; }          // mm (X distance from origin to centroid)

    // For section modulus calculation (distances from centroid to extreme fibers)
    public double DistanceToTopFiber { get; set; }    // mm
    public double DistanceToBottomFiber { get; set; } // mm
    public double DistanceToLeftFiber { get; set; }   // mm
    public double DistanceToRightFiber { get; set; }  // mm

    // Material properties
    public double Density { get; set; } = 7850;       // kg/m³ (default: steel)
    public double Length { get; set; } = 1000;        // mm (default: 1 meter)

    // Weight calculation
    public double MassPerMeter { get; set; }          // kg/m
    public double TotalMass { get; set; }             // kg

    // ============ MAIN CALCULATION METHOD ============

    public void Calculate()
    {
        // Every shape except the L-section is symmetric about at least one
        // centroidal axis, so its product of inertia is zero. The L-section
        // overwrites this in CalculateLSection.
        Ixy = 0;

        switch (SelectedShape)
        {
            case ShapeType.Rectangle:
                CalculateRectangle();
                break;
            case ShapeType.HollowRectangle:
                CalculateHollowRectangle();
                break;
            case ShapeType.Circle:
                CalculateCircle();
                break;
            case ShapeType.HollowCircle:
                CalculateHollowCircle();
                break;
            case ShapeType.IBeam:
                CalculateIBeam();
                break;
            case ShapeType.CChannel:
                CalculateCChannel();
                break;
            case ShapeType.TSection:
                CalculateTSection();
                break;
            case ShapeType.LSection:
                CalculateLSection();
                break;
        }

        // Principal axes (for symmetric sections these coincide with X/Y)
        CalculatePrincipalAxes();

        // Calculate polar moment of inertia
        Ip = Ix + Iy;

        CalculateSectionModuli();

        // Calculate radius of gyration
        if (Area > 0)
        {
            rx = Math.Sqrt(Ix / Area);
            ry = Math.Sqrt(Iy / Area);

            // About the PRINCIPAL axes. r_v is the one that governs buckling, and
            // on an angle it is nowhere near min(rx, ry): an L 100x100x12 has
            // rx = ry = 30,51 mm and r_v = 19,57 mm. Taking the smaller of rx and
            // ry there understates the slenderness by a factor of 1,56 and
            // overstates the Euler load by 2,4.
            ru = Math.Sqrt(IuPrincipal / Area);
            rv = Math.Sqrt(IvPrincipal / Area);
        }

        CalculateTorsionConstant();

        // Calculate mass
        CalculateMass();
    }

    private void CalculateMass()
    {
        MassPerMeter = Area * Density / 1_000_000.0;
        TotalMass = MassPerMeter * (Length / 1000.0);
    }

    // ============ CALCULATION METHODS FOR EACH SHAPE ============
    // All shapes are calculated with centroid at origin (0,0)

    private void CalculateRectangle()
    {
        // Rectangle centered at origin
        Area = Width * Height;
        Ix = (Width * Math.Pow(Height, 3)) / 12.0;
        Iy = (Height * Math.Pow(Width, 3)) / 12.0;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        // Distances to extreme fibers (from centroid)
        DistanceToTopFiber = Height / 2.0;
        DistanceToBottomFiber = Height / 2.0;
        DistanceToLeftFiber = Width / 2.0;
        DistanceToRightFiber = Width / 2.0;

        Wx = Ix / DistanceToTopFiber;
        Wy = Iy / DistanceToLeftFiber;
    }

    private void CalculateHollowRectangle()
    {
        // Hollow rectangle centered at origin
        Area = (Width * Height) - (InnerWidth * InnerHeight);
        Ix = ((Width * Math.Pow(Height, 3)) - (InnerWidth * Math.Pow(InnerHeight, 3))) / 12.0;
        Iy = ((Height * Math.Pow(Width, 3)) - (InnerHeight * Math.Pow(InnerWidth, 3))) / 12.0;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        DistanceToTopFiber = Height / 2.0;
        DistanceToBottomFiber = Height / 2.0;
        DistanceToLeftFiber = Width / 2.0;
        DistanceToRightFiber = Width / 2.0;

        Wx = Ix / DistanceToTopFiber;
        Wy = Iy / DistanceToLeftFiber;
    }

    private void CalculateCircle()
    {
        // Circle centered at origin
        double r = Diameter / 2.0;
        Area = Math.PI * Math.Pow(r, 2);
        Ix = (Math.PI * Math.Pow(Diameter, 4)) / 64.0;
        Iy = Ix;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        DistanceToTopFiber = r;
        DistanceToBottomFiber = r;
        DistanceToLeftFiber = r;
        DistanceToRightFiber = r;

        Wx = (Math.PI * Math.Pow(Diameter, 3)) / 32.0;
        Wy = Wx;
    }

    private void CalculateHollowCircle()
    {
        // Hollow circle (tube) centered at origin
        double r_o = Diameter / 2.0;
        double r_i = InnerDiameter / 2.0;

        Area = Math.PI * (Math.Pow(r_o, 2) - Math.Pow(r_i, 2));
        Ix = (Math.PI / 64.0) * (Math.Pow(Diameter, 4) - Math.Pow(InnerDiameter, 4));
        Iy = Ix;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        DistanceToTopFiber = r_o;
        DistanceToBottomFiber = r_o;
        DistanceToLeftFiber = r_o;
        DistanceToRightFiber = r_o;

        Wx = Ix / DistanceToTopFiber;
        Wy = Ix / DistanceToLeftFiber;
    }

    private void CalculateIBeam()
    {
        // I-Beam centered at origin
        double h = TotalHeight;
        double b = FlangeWidth;
        double tf = FlangeThickness;
        double tw = WebThickness;
        double hw = h - 2 * tf; // Web height

        Area = 2 * (b * tf) + (hw * tw);

        // I-beam is symmetric - Ix about centroidal axis
        double flangeIx = 2 * ((b * Math.Pow(tf, 3)) / 12.0 + (b * tf) * Math.Pow((h - tf) / 2.0, 2));
        double webIx = (tw * Math.Pow(hw, 3)) / 12.0;
        Ix = flangeIx + webIx;

        // Iy about centroidal axis
        Iy = 2 * ((tf * Math.Pow(b, 3)) / 12.0) + ((hw * Math.Pow(tw, 3)) / 12.0);

        // Centroid at origin (symmetric section)
        CentroidY = 0;
        CentroidX = 0;

        DistanceToTopFiber = h / 2.0;
        DistanceToBottomFiber = h / 2.0;
        DistanceToLeftFiber = b / 2.0;
        DistanceToRightFiber = b / 2.0;

        Wx = Ix / DistanceToTopFiber;
        Wy = Iy / DistanceToLeftFiber;
    }

    private void CalculateCChannel()
    {
        // C-Channel - asymmetric about Y axis
        double h = ChannelHeight;
        double b = ChannelWidth;
        double tf = ChannelFlangeThickness;
        double tw = ChannelWebThickness;
        double hw = h - 2 * tf;

        // Areas of components
        double Af = b * tf;  // Flange area
        double Aw = hw * tw; // Web area
        Area = 2 * Af + Aw;

        // Calculate centroid X position (from web back)
        // Then shift to make centroid at origin
        double xFlange = b / 2.0;
        double xWeb = tw / 2.0;
        double centroidXFromBack = (2 * Af * xFlange + Aw * xWeb) / Area;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        // Ix about centroidal X-axis (symmetric about X)
        double flangeIx = 2 * ((b * Math.Pow(tf, 3)) / 12.0 + Af * Math.Pow((h - tf) / 2.0, 2));
        double webIx = (tw * Math.Pow(hw, 3)) / 12.0;
        Ix = flangeIx + webIx;

        // Iy about centroidal Y-axis (using parallel axis theorem)
        double flange1Iy = (tf * Math.Pow(b, 3)) / 12.0 + Af * Math.Pow(xFlange - centroidXFromBack, 2);
        double flange2Iy = flange1Iy;
        double webIy = (hw * Math.Pow(tw, 3)) / 12.0 + Aw * Math.Pow(xWeb - centroidXFromBack, 2);
        Iy = flange1Iy + flange2Iy + webIy;

        // Distances to extreme fibers
        DistanceToTopFiber = h / 2.0;
        DistanceToBottomFiber = h / 2.0;
        DistanceToLeftFiber = centroidXFromBack;
        DistanceToRightFiber = b - centroidXFromBack;

        Wx = Ix / DistanceToTopFiber;
        Wy = Iy / Math.Max(DistanceToLeftFiber, DistanceToRightFiber);
    }

    private void CalculateTSection()
    {
        // T-Section - asymmetric about X axis
        double bf = TFlangeWidth;
        double h = THeight;
        double tf = TFlangeThickness;
        double tw = TStemThickness;
        double hs = h - tf; // Stem height

        double Af = bf * tf;  // Flange area
        double As = hs * tw;  // Stem area
        Area = Af + As;

        // Calculate centroid Y position (from bottom)
        // Flange at top, stem below
        double yFlange = h - tf / 2.0;
        double yStem = hs / 2.0;
        double centroidYFromBottom = (Af * yFlange + As * yStem) / Area;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        // Distances to extreme fibers
        DistanceToTopFiber = h - centroidYFromBottom;
        DistanceToBottomFiber = centroidYFromBottom;
        DistanceToLeftFiber = bf / 2.0;
        DistanceToRightFiber = bf / 2.0;

        // Ix about centroidal axis using parallel axis theorem
        double flangeIx = (bf * Math.Pow(tf, 3)) / 12.0 + Af * Math.Pow(yFlange - centroidYFromBottom, 2);
        double stemIx = (tw * Math.Pow(hs, 3)) / 12.0 + As * Math.Pow(yStem - centroidYFromBottom, 2);
        Ix = flangeIx + stemIx;

        // Iy about centroidal axis (symmetric about Y)
        double flangeIy = (tf * Math.Pow(bf, 3)) / 12.0;
        double stemIy = (hs * Math.Pow(tw, 3)) / 12.0;
        Iy = flangeIy + stemIy;

        Wx = Ix / Math.Max(DistanceToTopFiber, DistanceToBottomFiber);
        Wy = Iy / DistanceToLeftFiber;
    }

    private void CalculateLSection()
    {
        // L-Section (Angle) - asymmetric about both axes
        double b = LegWidth;      // Horizontal leg
        double h = LegHeight;     // Vertical leg
        double t = LegThickness;

        // Horizontal leg (at bottom) and vertical leg (at left)
        double A1 = b * t;           // Horizontal leg area
        double A2 = (h - t) * t;     // Vertical leg area (excluding corner)
        Area = A1 + A2;

        // Calculate centroid position (from corner)
        double x1 = b / 2.0;         // Horizontal leg centroid X
        double y1 = t / 2.0;         // Horizontal leg centroid Y
        double x2 = t / 2.0;         // Vertical leg centroid X
        double y2 = t + (h - t) / 2.0; // Vertical leg centroid Y

        double centroidXFromCorner = (A1 * x1 + A2 * x2) / Area;
        double centroidYFromCorner = (A1 * y1 + A2 * y2) / Area;

        // Centroid at origin
        CentroidY = 0;
        CentroidX = 0;

        // Distances to extreme fibers (from centroid)
        DistanceToTopFiber = h - centroidYFromCorner;
        DistanceToBottomFiber = centroidYFromCorner;
        DistanceToLeftFiber = centroidXFromCorner;
        DistanceToRightFiber = b - centroidXFromCorner;

        // Ix about centroidal axis
        double Ix1 = (b * Math.Pow(t, 3)) / 12.0 + A1 * Math.Pow(y1 - centroidYFromCorner, 2);
        double Ix2 = (t * Math.Pow(h - t, 3)) / 12.0 + A2 * Math.Pow(y2 - centroidYFromCorner, 2);
        Ix = Ix1 + Ix2;

        // Iy about centroidal axis
        double Iy1 = (t * Math.Pow(b, 3)) / 12.0 + A1 * Math.Pow(x1 - centroidXFromCorner, 2);
        double Iy2 = ((h - t) * Math.Pow(t, 3)) / 12.0 + A2 * Math.Pow(x2 - centroidXFromCorner, 2);
        Iy = Iy1 + Iy2;

        // Product of inertia Ixy about the centroidal axes.
        // Each rectangle's own Ixy is zero about its own centroid (both are
        // symmetric about their own axes), so only the transfer terms remain:
        //   Ixy = Σ Ai · dxi · dyi
        double Ixy1 = A1 * (x1 - centroidXFromCorner) * (y1 - centroidYFromCorner);
        double Ixy2 = A2 * (x2 - centroidXFromCorner) * (y2 - centroidYFromCorner);
        Ixy = Ixy1 + Ixy2;

        Wx = Ix / Math.Max(DistanceToTopFiber, DistanceToBottomFiber);
        Wy = Iy / Math.Max(DistanceToLeftFiber, DistanceToRightFiber);
    }

    /// <summary>
    /// Section modulus at EACH extreme fibre, plus the governing (smallest) value
    /// in each direction.
    ///
    /// On an asymmetric section the two fibres are far apart: a T 120x150 has
    /// W_x = 169 435 mm3 at its flange face and 68 149 mm3 at the stem tip. The
    /// smaller one governs a symmetric allowable stress and is what Wx reports,
    /// but a reader checking a compression-only or tension-only limit needs both,
    /// and seeing only one invites the assumption that the section is symmetric.
    ///
    /// Computing them here rather than in each shape also removed a slip in the
    /// hollow circle, which divided Ix by the left-fibre distance to get Wy.
    /// </summary>
    private void CalculateSectionModuli()
    {
        WxTop = DistanceToTopFiber > 0 ? Ix / DistanceToTopFiber : 0;
        WxBottom = DistanceToBottomFiber > 0 ? Ix / DistanceToBottomFiber : 0;
        WyLeft = DistanceToLeftFiber > 0 ? Iy / DistanceToLeftFiber : 0;
        WyRight = DistanceToRightFiber > 0 ? Iy / DistanceToRightFiber : 0;

        double MinPositive(double a, double b)
        {
            if (a > 0 && b > 0) return Math.Min(a, b);
            return a > 0 ? a : b;
        }

        double wx = MinPositive(WxTop, WxBottom);
        double wy = MinPositive(WyLeft, WyRight);
        if (wx > 0) Wx = wx;
        if (wy > 0) Wy = wy;

        SectionIsAsymmetricAboutX = WxTop > 0 && WxBottom > 0
            && Math.Abs(WxTop - WxBottom) / Math.Max(WxTop, WxBottom) > 1e-6;
        SectionIsAsymmetricAboutY = WyLeft > 0 && WyRight > 0
            && Math.Abs(WyLeft - WyRight) / Math.Max(WyLeft, WyRight) > 1e-6;
    }

    /// <summary>
    /// St Venant torsion constant.
    ///
    /// I_p = I_x + I_y is the POLAR second moment of area and equals the torsion
    /// constant only for a circular section. For every other shape it is far too
    /// large — an I-beam or a channel twists on the thin-walled open-section
    /// value, which can be two orders of magnitude smaller — so presenting I_p
    /// next to a torsion result invites a dangerous substitution.
    ///
    ///   circle / tube:  I_t = I_p
    ///   open section:   I_t = sum(b_i * t_i^3) / 3   (thin-walled, no fillets)
    ///   closed box:     I_t = 4 A_m^2 / integral(ds/t)   (Bredt)
    ///   solid rectangle: Roark's beta * b * h^3, beta from the aspect ratio
    /// </summary>
    private void CalculateTorsionConstant()
    {
        It = 0;
        TorsionConstantBasis = "";

        switch (SelectedShape)
        {
            case ShapeType.Circle:
            case ShapeType.HollowCircle:
                It = Ip;
                TorsionConstantBasis = "circular section: I_t = I_p exactly";
                break;

            case ShapeType.Rectangle:
            {
                // Solid rectangle, St Venant. The series solution
                //   I_t = a b^3 [1/3 - 0.21 (b/a)(1 - b^4/(12 a^4))]
                // with a the long side and b the short one; accurate to well
                // under 1 % for every aspect ratio.
                double a = Math.Max(Width, Height);
                double b = Math.Min(Width, Height);
                if (a > 0 && b > 0)
                {
                    double ratio = b / a;
                    It = a * b * b * b * (1.0 / 3.0 - 0.21 * ratio * (1.0 - Math.Pow(ratio, 4) / 12.0));
                    TorsionConstantBasis = "solid rectangle, St Venant series";
                }
                break;
            }

            case ShapeType.HollowRectangle:
            {
                // Bredt, closed thin-walled box. Wall thicknesses from the
                // difference of the outer and inner dimensions.
                double tv = (Width - InnerWidth) / 2.0;    // vertical walls
                double th = (Height - InnerHeight) / 2.0;  // horizontal walls
                if (tv > 0 && th > 0)
                {
                    double bm = Width - tv;    // mid-line width
                    double hm = Height - th;   // mid-line height
                    double am = bm * hm;
                    double perimeterOverT = 2.0 * bm / th + 2.0 * hm / tv;
                    if (am > 0 && perimeterOverT > 0)
                    {
                        It = 4.0 * am * am / perimeterOverT;
                        TorsionConstantBasis = "closed box, Bredt thin-walled";
                    }
                }
                break;
            }

            case ShapeType.IBeam:
                It = (2.0 * FlangeWidth * Math.Pow(FlangeThickness, 3)
                      + (TotalHeight - 2.0 * FlangeThickness) * Math.Pow(WebThickness, 3)) / 3.0;
                TorsionConstantBasis = "open section, sum(b t^3)/3, no fillets";
                break;

            case ShapeType.CChannel:
                It = (2.0 * ChannelWidth * Math.Pow(ChannelFlangeThickness, 3)
                      + (ChannelHeight - 2.0 * ChannelFlangeThickness) * Math.Pow(ChannelWebThickness, 3)) / 3.0;
                TorsionConstantBasis = "open section, sum(b t^3)/3, no fillets";
                break;

            case ShapeType.TSection:
                It = (TFlangeWidth * Math.Pow(TFlangeThickness, 3)
                      + (THeight - TFlangeThickness) * Math.Pow(TStemThickness, 3)) / 3.0;
                TorsionConstantBasis = "open section, sum(b t^3)/3, no fillets";
                break;

            case ShapeType.LSection:
                It = ((LegWidth - LegThickness) * Math.Pow(LegThickness, 3)
                      + LegHeight * Math.Pow(LegThickness, 3)) / 3.0;
                TorsionConstantBasis = "open section, sum(b t^3)/3, no fillets";
                break;
        }
    }

    /// <summary>
    /// Principal second moments of area Iu (max) and Iv (min) and the angle to
    /// the principal axes, from Ix, Iy and Ixy.
    ///
    ///   Iu,Iv = (Ix+Iy)/2 ± sqrt( ((Ix-Iy)/2)² + Ixy² )
    ///   tan(2α) = -2·Ixy / (Ix - Iy)
    ///
    /// For a section with Ixy != 0 (such as an unequal or equal-leg angle) the
    /// X/Y axes are NOT principal axes: bending about X alone still produces
    /// deflection in Y. Using Ix/Iy directly for bending design is unsafe for
    /// such sections - Iv (the smaller principal value) governs.
    /// </summary>
    private void CalculatePrincipalAxes()
    {
        double avg = (Ix + Iy) / 2.0;
        double diff = (Ix - Iy) / 2.0;
        double radius = Math.Sqrt(diff * diff + Ixy * Ixy);

        IuPrincipal = avg + radius;
        IvPrincipal = avg - radius;
        if (IvPrincipal < 0) IvPrincipal = 0; // guard against round-off

        // Angle from the X axis to the major principal axis, in degrees
        PrincipalAngle = 0.5 * Math.Atan2(-2.0 * Ixy, Ix - Iy) * 180.0 / Math.PI;

        // The X/Y axes are principal only when the product of inertia vanishes.
        // Compare against the section's own scale rather than against zero.
        double scale = Math.Max(Math.Abs(Ix), Math.Abs(Iy));
        AxesArePrincipal = scale <= 0 || Math.Abs(Ixy) / scale < 1e-9;
    }

    // ============ VALIDATION METHODS ============

    /// <summary>
    /// Every condition the selected shape has to satisfy, each with the message
    /// that names it. Empty means the geometry is usable.
    ///
    /// This is ONE list rather than a bool and a separate message because the two
    /// had drifted: the bool rejected a web thicker than the flange and a missing
    /// dimension, and neither had any text, so Calculate refused in silence and
    /// the button looked dead. A condition without a message cannot exist here.
    /// </summary>
    public List<string> Validate()
    {
        var messages = new List<string>();

        void Positive(double value, string name)
        {
            if (value <= 0) messages.Add($"{name} must be greater than 0.");
        }

        switch (SelectedShape)
        {
            case ShapeType.Rectangle:
                Positive(Width, "Width (b)");
                Positive(Height, "Height (h)");
                break;

            case ShapeType.HollowRectangle:
                Positive(Width, "Outer width (b)");
                Positive(Height, "Outer height (h)");
                Positive(InnerWidth, "Inner width (bi)");
                Positive(InnerHeight, "Inner height (hi)");
                if (Width > 0 && InnerWidth >= Width)
                    messages.Add($"Inner width (bi) must be smaller than the outer width ({Width:0.##} mm), or there is no wall.");
                if (Height > 0 && InnerHeight >= Height)
                    messages.Add($"Inner height (hi) must be smaller than the outer height ({Height:0.##} mm), or there is no wall.");
                break;

            case ShapeType.Circle:
                Positive(Diameter, "Diameter (d)");
                break;

            case ShapeType.HollowCircle:
                Positive(Diameter, "Outer diameter (d)");
                Positive(InnerDiameter, "Inner diameter (di)");
                if (Diameter > 0 && InnerDiameter >= Diameter)
                    messages.Add($"Inner diameter (di) must be smaller than the outer diameter ({Diameter:0.##} mm), or there is no wall.");
                break;

            case ShapeType.IBeam:
                Positive(FlangeWidth, "Flange width (bf)");
                Positive(TotalHeight, "Total height (h)");
                Positive(FlangeThickness, "Flange thickness (tf)");
                Positive(WebThickness, "Web thickness (tw)");
                if (TotalHeight > 0 && 2 * FlangeThickness >= TotalHeight)
                    messages.Add($"The two flanges (2·tf = {2 * FlangeThickness:0.##} mm) must be thinner than the total height ({TotalHeight:0.##} mm), or there is no web left.");
                if (FlangeWidth > 0 && WebThickness > FlangeWidth)
                    messages.Add($"Web thickness (tw) cannot exceed the flange width ({FlangeWidth:0.##} mm) — the web would stand outside the flange.");
                break;

            case ShapeType.CChannel:
                Positive(ChannelWidth, "Channel width (b)");
                Positive(ChannelHeight, "Channel height (h)");
                Positive(ChannelFlangeThickness, "Flange thickness (tf)");
                Positive(ChannelWebThickness, "Web thickness (tw)");
                if (ChannelHeight > 0 && 2 * ChannelFlangeThickness >= ChannelHeight)
                    messages.Add($"The two flanges (2·tf = {2 * ChannelFlangeThickness:0.##} mm) must be thinner than the channel height ({ChannelHeight:0.##} mm), or there is no web left.");
                if (ChannelWidth > 0 && ChannelWebThickness >= ChannelWidth)
                    messages.Add($"Web thickness (tw) must be smaller than the channel width ({ChannelWidth:0.##} mm).");
                break;

            case ShapeType.TSection:
                Positive(TFlangeWidth, "Flange width (bf)");
                Positive(THeight, "Total height (h)");
                Positive(TFlangeThickness, "Flange thickness (tf)");
                Positive(TStemThickness, "Stem thickness (tw)");
                if (THeight > 0 && TFlangeThickness >= THeight)
                    messages.Add($"Flange thickness (tf) must be less than the total height ({THeight:0.##} mm), or there is no stem left.");
                if (TFlangeWidth > 0 && TStemThickness > TFlangeWidth)
                    messages.Add($"Stem thickness (tw) cannot exceed the flange width ({TFlangeWidth:0.##} mm).");
                break;

            case ShapeType.LSection:
                Positive(LegWidth, "Leg width (b)");
                Positive(LegHeight, "Leg height (h)");
                Positive(LegThickness, "Leg thickness (t)");
                if (LegWidth > 0 && LegThickness >= LegWidth)
                    messages.Add($"Leg thickness (t) must be less than the leg width ({LegWidth:0.##} mm).");
                if (LegHeight > 0 && LegThickness >= LegHeight)
                    messages.Add($"Leg thickness (t) must be less than the leg height ({LegHeight:0.##} mm).");
                break;

            default:
                messages.Add("Select a cross-section shape.");
                break;
        }

        if (Density <= 0) messages.Add("Density must be greater than 0.");
        if (Length <= 0) messages.Add("Length must be greater than 0.");

        return messages;
    }

    public bool ValidateInputs() => Validate().Count == 0;
}

// ============ SHAPE TYPE ENUM ============

public enum ShapeType
{
    Rectangle,
    HollowRectangle,
    Circle,
    HollowCircle,
    IBeam,
    CChannel,
    TSection,
    LSection
}
