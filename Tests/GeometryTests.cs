using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

public class GeometryTests
{
    /// <summary>A 1:10 taper has a half angle of atan(1/20) = 2.8624 degrees.</summary>
    [Fact]
    public void TaperHalfAngleMatchesTheRatio()
    {
        var engine = new TaperFitEngine
        {
            TaperRatio = 10,
            MeanDiameter = 50,
            FitLength = 60,
            HubOuterDiameter = 100,
            ShaftMaterial = TestMaterials.Steel(),
            HubMaterial = TestMaterials.Steel()
        };
        engine.Calculate();

        Assert.Equal(System.Math.Atan(0.05) * 180.0 / System.Math.PI, engine.HalfAngle, 9);
        Assert.Equal(2.8624052, engine.HalfAngle, 6);
    }

    /// <summary>Rectangle 40 wide x 80 tall: I_x = bh^3/12, I_y = hb^3/12.</summary>
    [Fact]
    public void RectangleSecondMomentsAreTextbook()
    {
        var engine = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.Rectangle,
            Width = 40,
            Height = 80
        };
        engine.Calculate();

        Assert.Equal(3200.0, engine.Area, 6);
        Assert.Equal(40.0 * 80 * 80 * 80 / 12.0, engine.Ix, 6);
        Assert.Equal(80.0 * 40 * 40 * 40 / 12.0, engine.Iy, 6);
    }

    /// <summary>Circle of 60 mm: I = pi d^4 / 64 = 636172.512 mm^4.</summary>
    [Fact]
    public void CircleSecondMomentIsTextbook()
    {
        var engine = new MomentOfInertiaEngine
        {
            SelectedShape = ShapeType.Circle,
            Diameter = 60
        };
        engine.Calculate();

        Assert.Equal(System.Math.PI * System.Math.Pow(60, 4) / 64.0, engine.Ix, 6);
        Assert.Equal(engine.Ix, engine.Iy, 6);
    }
}
