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
    public double Wx { get; set; }                 // mm³ (about X-axis)
    public double Wy { get; set; }                 // mm³ (about Y-axis)

    // Radius of Gyration
    public double rx { get; set; }                 // mm (about X-axis)
    public double ry { get; set; }                 // mm (about Y-axis)

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

        // Calculate radius of gyration
        if (Area > 0)
        {
            rx = Math.Sqrt(Ix / Area);
            ry = Math.Sqrt(Iy / Area);
        }

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

    public bool ValidateInputs()
    {
        return SelectedShape switch
        {
            ShapeType.Rectangle => Width > 0 && Height > 0,
            ShapeType.HollowRectangle => Width > 0 && Height > 0 && InnerWidth > 0 && InnerHeight > 0
                                         && InnerWidth < Width && InnerHeight < Height,
            ShapeType.Circle => Diameter > 0,
            ShapeType.HollowCircle => Diameter > 0 && InnerDiameter > 0 && InnerDiameter < Diameter,
            ShapeType.IBeam => FlangeWidth > 0 && TotalHeight > 0 && FlangeThickness > 0 && WebThickness > 0
                              && 2 * FlangeThickness < TotalHeight && WebThickness <= FlangeWidth,
            ShapeType.CChannel => ChannelWidth > 0 && ChannelHeight > 0 && ChannelFlangeThickness > 0 && ChannelWebThickness > 0
                                  && 2 * ChannelFlangeThickness < ChannelHeight,
            ShapeType.TSection => TFlangeWidth > 0 && THeight > 0 && TFlangeThickness > 0 && TStemThickness > 0
                                  && TFlangeThickness < THeight && TStemThickness <= TFlangeWidth,
            ShapeType.LSection => LegWidth > 0 && LegHeight > 0 && LegThickness > 0
                                  && LegThickness < LegWidth && LegThickness < LegHeight,
            _ => false
        };
    }

    public string GetValidationError()
    {
        return SelectedShape switch
        {
            ShapeType.Rectangle when Width <= 0 || Height <= 0 => "Width and Height must be positive.",
            ShapeType.HollowRectangle when InnerWidth >= Width || InnerHeight >= Height =>
                "Inner dimensions must be smaller than outer dimensions.",
            ShapeType.Circle when Diameter <= 0 => "Diameter must be positive.",
            ShapeType.HollowCircle when InnerDiameter >= Diameter =>
                "Inner diameter must be smaller than outer diameter.",
            ShapeType.IBeam when 2 * FlangeThickness >= TotalHeight =>
                "Total flange thickness (2×tf) must be less than total height.",
            ShapeType.CChannel when 2 * ChannelFlangeThickness >= ChannelHeight =>
                "Total flange thickness (2×tf) must be less than channel height.",
            ShapeType.TSection when TFlangeThickness >= THeight =>
                "Flange thickness must be less than total height.",
            ShapeType.LSection when LegThickness >= LegWidth || LegThickness >= LegHeight =>
                "Leg thickness must be less than leg dimensions.",
            _ => string.Empty
        };
    }
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
