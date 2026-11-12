using System;
using System.Linq;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the Nov 2026 review of the section properties module.
/// </summary>
public class MomentOfInertiaTests
{
    private static MomentOfInertiaEngine Angle() => new()
    {
        SelectedShape = ShapeType.LSection,
        LegWidth = 100,
        LegHeight = 100,
        LegThickness = 12
    };

    private static MomentOfInertiaEngine IBeam() => new()
    {
        SelectedShape = ShapeType.IBeam,
        FlangeWidth = 100,
        TotalHeight = 200,
        FlangeThickness = 15,
        WebThickness = 10
    };

    /// <summary>
    /// An equal-leg angle has rx = ry, and neither is the radius a column
    /// buckles about: its weak axis runs at 45 degrees through the heel. The
    /// page's own text told the reader to use the smaller of rx and ry, which
    /// here is 30,51 mm against the correct 19,57 - a slenderness 1,56 times too
    /// low and an Euler load 2,4 times too high.
    /// </summary>
    [Fact]
    public void AnAngleBucklesAboutItsPrincipalAxisNotAboutXOrY()
    {
        var engine = Angle();
        engine.Calculate();

        Assert.False(engine.AxesArePrincipal);
        Assert.Equal(engine.rx, engine.ry, 9);              // equal legs
        Assert.Equal(45.0, Math.Abs(engine.PrincipalAngle), 6);

        Assert.Equal(30.51, engine.rx, 2);
        Assert.Equal(19.57, engine.rv, 2);
        Assert.Equal(38.45, engine.ru, 2);

        Assert.True(engine.rv < Math.Min(engine.rx, engine.ry),
            "the minor principal radius must be the smallest of all");

        // Definitions, independently: r = sqrt(I/A) on the principal values.
        Assert.Equal(Math.Sqrt(engine.IvPrincipal / engine.Area), engine.rv, 9);
        Assert.Equal(Math.Sqrt(engine.IuPrincipal / engine.Area), engine.ru, 9);
    }

    /// <summary>
    /// The principal moments come out of Mohr's circle on Ix, Iy and Ixy, and
    /// their sum is invariant - which is the cheapest check on the whole set.
    /// </summary>
    [Fact]
    public void PrincipalMomentsSumToTheSameInvariant()
    {
        var engine = Angle();
        engine.Calculate();

        Assert.Equal(engine.Ix + engine.Iy, engine.IuPrincipal + engine.IvPrincipal, 6);
        Assert.Equal(3.336e6, engine.IuPrincipal, 3.0e3);
        Assert.Equal(0.864e6, engine.IvPrincipal, 1.0e3);
        Assert.True(engine.Ixy < 0, "an angle in this orientation has a negative product of inertia");
    }

    /// <summary>
    /// A symmetric section's X and Y axes ARE principal, so the smaller of rx and
    /// ry is the right value there and no principal rows are needed.
    /// </summary>
    [Fact]
    public void ASymmetricSectionNeedsNoPrincipalCorrection()
    {
        var engine = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.Rectangle,
            Width = 100,
            Height = 150
        };
        engine.Calculate();

        Assert.True(engine.AxesArePrincipal);
        Assert.Equal(0.0, engine.Ixy, 9);
        Assert.Equal(Math.Min(engine.rx, engine.ry), engine.rv, 6);
        Assert.Equal(28.87, engine.ry, 2);
    }

    /// <summary>
    /// I_p is the POLAR second moment of area and is the torsion constant only
    /// for a circle or a tube. Presenting it beside a torsion result invited a
    /// substitution that is two orders of magnitude wrong on an open section.
    /// </summary>
    [Fact]
    public void PolarMomentIsTheTorsionConstantOnlyForARoundSection()
    {
        var circle = new MomentOfInertiaEngine { SelectedShape = ShapeType.Circle, Diameter = 100 };
        circle.Calculate();
        Assert.Equal(circle.Ip, circle.It, 6);

        var tube = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.HollowCircle, Diameter = 100, InnerDiameter = 80
        };
        tube.Calculate();
        Assert.Equal(tube.Ip, tube.It, 6);

        var beam = IBeam();
        beam.Calculate();
        // sum(b t^3)/3 over two flanges and the web
        double expected = (2 * 100 * Math.Pow(15, 3) + (200 - 30) * Math.Pow(10, 3)) / 3.0;
        Assert.Equal(expected, beam.It, 6);
        Assert.True(beam.Ip / beam.It > 100, "the gap on an open section is two orders of magnitude");
    }

    /// <summary>
    /// A closed box twists on Bredt, not on the open-section sum - the difference
    /// is what makes a box section the right choice for torsion in the first
    /// place, so a box must come out far stiffer than an equivalent open shape.
    /// </summary>
    [Fact]
    public void AClosedBoxTwistsOnBredt()
    {
        var box = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.HollowRectangle,
            Width = 100, Height = 150, InnerWidth = 80, InnerHeight = 130
        };
        box.Calculate();

        // t = 10 both ways; mid-line 90 x 140.
        double am = 90.0 * 140.0;
        double perimeterOverT = 2 * 90.0 / 10.0 + 2 * 140.0 / 10.0;
        Assert.Equal(4 * am * am / perimeterOverT, box.It, 6);

        // An open channel of comparable size is far softer in torsion.
        var channel = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.CChannel,
            ChannelWidth = 100, ChannelHeight = 150,
            ChannelFlangeThickness = 10, ChannelWebThickness = 10
        };
        channel.Calculate();
        Assert.True(box.It > 20 * channel.It);
    }

    /// <summary>
    /// A solid square's torsion constant is about 0,1406 b^4 (St Venant), not
    /// I_p = b^4/6 = 0,1667 b^4. The series form has to reproduce that.
    /// </summary>
    [Fact]
    public void ASolidSquareMatchesTheStVenantConstant()
    {
        var square = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.Rectangle, Width = 100, Height = 100
        };
        square.Calculate();

        Assert.Equal(0.1406 * Math.Pow(100, 4), square.It, 1.0e5);
        Assert.True(square.It < square.Ip);
    }

    /// <summary>
    /// On an asymmetric section the two extreme fibres have different moduli, and
    /// the reported one is the smaller. A T 120x150 is the example: 169 435 mm3
    /// at the flange face against 68 149 at the stem tip.
    /// </summary>
    [Fact]
    public void AnAsymmetricSectionReportsBothFibresAndTheSmallerGoverns()
    {
        var tee = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.TSection,
            TFlangeWidth = 120, THeight = 150,
            TFlangeThickness = 15, TStemThickness = 12
        };
        tee.Calculate();

        Assert.True(tee.SectionIsAsymmetricAboutX);
        Assert.False(tee.SectionIsAsymmetricAboutY);

        Assert.Equal(Math.Min(tee.WxTop, tee.WxBottom), tee.Wx, 6);
        Assert.Equal(68149, Math.Min(tee.WxTop, tee.WxBottom), 0);
        Assert.Equal(169435, Math.Max(tee.WxTop, tee.WxBottom), 0);

        // Definitions, independently.
        Assert.Equal(tee.Ix / tee.DistanceToTopFiber, tee.WxTop, 6);
        Assert.Equal(tee.Ix / tee.DistanceToBottomFiber, tee.WxBottom, 6);
    }

    /// <summary>
    /// A tube's Wy comes from Iy and the left/right fibre, not from Ix. The two
    /// are equal on a circle, which is why computing Wy from Ix went unnoticed -
    /// the per-fibre pass removed the slip rather than relying on that.
    /// </summary>
    [Fact]
    public void ATubesSectionModulusComesFromItsOwnAxis()
    {
        var tube = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.HollowCircle, Diameter = 100, InnerDiameter = 80
        };
        tube.Calculate();

        Assert.Equal(tube.Iy / tube.DistanceToLeftFiber, tube.WyLeft, 6);
        Assert.Equal(tube.Wx, tube.Wy, 6);
        Assert.Equal(57962, tube.Wx, 0);
    }

    /// <summary>
    /// Every condition that stops a calculation must come with the message that
    /// names it. The bool and the message had drifted: a web thicker than its
    /// flange was rejected with no text at all, so the button looked dead.
    /// </summary>
    [Theory]
    [InlineData(ShapeType.IBeam)]
    [InlineData(ShapeType.CChannel)]
    [InlineData(ShapeType.TSection)]
    [InlineData(ShapeType.LSection)]
    [InlineData(ShapeType.Rectangle)]
    [InlineData(ShapeType.HollowRectangle)]
    [InlineData(ShapeType.Circle)]
    [InlineData(ShapeType.HollowCircle)]
    public void AnEmptyShapeIsRefusedWithAReason(ShapeType shape)
    {
        var engine = new MomentOfInertiaEngine { SelectedShape = shape };
        var messages = engine.Validate();

        Assert.False(engine.ValidateInputs());
        Assert.NotEmpty(messages);
        Assert.All(messages, m => Assert.False(string.IsNullOrWhiteSpace(m)));
    }

    /// <summary>A web wider than its flange is the case that had no message.</summary>
    [Fact]
    public void AWebWiderThanItsFlangeIsNamed()
    {
        var engine = IBeam();
        engine.WebThickness = 200;

        Assert.False(engine.ValidateInputs());
        Assert.Contains("flange width", string.Join(" ", engine.Validate()));
    }

    /// <summary>A usable section produces no messages at all.</summary>
    [Fact]
    public void AValidSectionHasNothingToReport()
    {
        Assert.Empty(IBeam().Validate());
        Assert.Empty(Angle().Validate());
    }
}
