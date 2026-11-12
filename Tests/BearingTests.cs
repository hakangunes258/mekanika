using System;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the Nov 2026 review of the two bearing modules. The life
/// arithmetic itself lives in the Razor pages, so what can be anchored here is
/// BearingService: the ISO 281 X/Y selection and the ISO 76 static factors.
/// </summary>
public class BearingTests
{
    /// <summary>
    /// Pure thrust is a real load case and used to be refused outright. With
    /// Fr = 0 the ratio Fa/Fr is unbounded, which is above e for any bearing, so
    /// the axial branch applies: X = 0,56 and Y from the table.
    /// </summary>
    [Fact]
    public void PureThrustSelectsTheAxialBranch()
    {
        // 6205: C0 = 7,9 kN, f0 = 13,9. Fa = 1 kN, Fr = 0.
        var (X, Y, e, rel) = BearingService.CalculateXYFactors(1.0, 0.0, 7.9, 13.9);

        Assert.Equal(0.56, X, 9);
        Assert.True(Y > 0, "the axial factor must be in play");
        Assert.Equal(13.9 * 1.0 / 7.9, rel, 9);
        Assert.True(e > 0);
    }

    /// <summary>With no load at all there is nothing to select, and no NaN.</summary>
    [Fact]
    public void NoLoadAtAllIsNotAnAxialCase()
    {
        var (X, Y, _, _) = BearingService.CalculateXYFactors(0.0, 0.0, 7.9, 13.9);
        Assert.Equal(1.0, X, 9);
        Assert.Equal(0.0, Y, 9);
    }

    /// <summary>
    /// The 6205 worked example, independently: f0*Fa/C0 = 13,9*0,5/7,9 = 0,880
    /// interpolates to e = 0,271, and Fa/Fr = 0,2 is below it, so X = 1 and
    /// Y = 0 - the axial load does not enter P at all.
    /// </summary>
    [Fact]
    public void ALightThrustDoesNotEnterTheEquivalentLoad()
    {
        var (X, Y, e, rel) = BearingService.CalculateXYFactors(0.5, 2.5, 7.9, 13.9);

        Assert.Equal(0.880, rel, 3);
        Assert.Equal(0.271, e, 3);
        Assert.Equal(1.0, X, 9);
        Assert.Equal(0.0, Y, 9);
    }

    /// <summary>
    /// ISO 76 static axial factor for a SINGLE ROW angular contact ball bearing.
    /// The calculator used to take Y0 from the catalogue column, which holds the
    /// double-row values - exactly twice these.
    /// </summary>
    [Theory]
    [InlineData(15, 0.46)]
    [InlineData(20, 0.42)]
    [InlineData(25, 0.38)]
    [InlineData(30, 0.33)]
    [InlineData(40, 0.26)]
    public void SingleRowStaticAxialFactorFollowsIso76(double angle, double expected)
    {
        Assert.Equal(expected, BearingService.StaticAxialFactorY0(angle, false), 6);
        Assert.Equal(2.0 * expected, BearingService.StaticAxialFactorY0(angle, true), 6);
    }

    /// <summary>X0 is 0,5 for a single row and 1,0 for a pair.</summary>
    [Fact]
    public void StaticRadialFactorFollowsTheArrangement()
    {
        Assert.Equal(0.5, BearingService.StaticRadialFactorX0(false), 9);
        Assert.Equal(1.0, BearingService.StaticRadialFactorX0(true), 9);
    }

    /// <summary>
    /// The factor falls monotonically with the contact angle - a steeper bearing
    /// takes thrust better - and the table is interpolated between its rows
    /// rather than stepping.
    /// </summary>
    [Fact]
    public void StaticAxialFactorFallsWithTheContactAngle()
    {
        double previous = double.MaxValue;
        for (double angle = 15; angle <= 45; angle += 2.5)
        {
            double y0 = BearingService.StaticAxialFactorY0(angle, false);
            Assert.True(y0 < previous, $"Y0 must fall as the angle rises (at {angle} deg)");
            previous = y0;
        }

        // Between two rows, the interpolated value sits between them.
        double at35 = BearingService.StaticAxialFactorY0(35, false);
        double at375 = BearingService.StaticAxialFactorY0(37.5, false);
        double at40 = BearingService.StaticAxialFactorY0(40, false);
        Assert.True(at375 < at35 && at375 > at40);
        Assert.Equal((at35 + at40) / 2, at375, 9);
    }

    /// <summary>Outside the table the end values are held, not extrapolated to nonsense.</summary>
    [Fact]
    public void StaticAxialFactorIsClampedOutsideTheTable()
    {
        Assert.Equal(0.46, BearingService.StaticAxialFactorY0(5, false), 6);
        Assert.Equal(0.22, BearingService.StaticAxialFactorY0(60, false), 6);
        Assert.True(BearingService.StaticAxialFactorY0(60, false) > 0);
    }

    /// <summary>
    /// The end of the ISO 281 table has to be reachable, so the page can say when
    /// it is extrapolating rather than holding the last row in silence.
    /// </summary>
    [Fact]
    public void TheTableEndIsExposed()
    {
        double max = BearingService.MaxTabulatedRelativeAxialLoad;
        Assert.True(max > 1.0, "the table must extend past the first rows");

        var atEnd = BearingService.GetEandY_ISO281(max);
        var pastEnd = BearingService.GetEandY_ISO281(max * 3);
        Assert.Equal(atEnd.e, pastEnd.e, 9);
        Assert.Equal(atEnd.Y, pastEnd.Y, 9);
    }

    /// <summary>
    /// Every tapered roller bearing in the catalogue satisfies e*Y near 0,6,
    /// because both factors come from the same contact angle. That is what makes
    /// the plausibility check on a hand-entered pair meaningful.
    /// </summary>
    [Fact]
    public void EveryCatalogueTaperedBearingHasEYNearSixTenths()
    {
        foreach (var bearing in BearingService.TaperedRollerBearings)
        {
            double eY = bearing.e * bearing.Y;
            Assert.InRange(eY, 0.52, 0.66);

            // And Y0 is about 0,55 Y, per ISO 76.
            Assert.InRange(bearing.Y0 / bearing.Y, 0.52, 0.58);
        }
    }
}
