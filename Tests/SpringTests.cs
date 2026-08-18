using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

public class SpringTests
{
    /// <summary>
    /// EN 13906-1 rate, R = G d^4 / (8 Dm^3 n). For d 4, Dm 32, n 8, G 81500:
    ///   81500 x 256 / (8 x 32768 x 8) = 20864000 / 2097152 = 9.9487304 N/mm
    /// </summary>
    [Fact]
    public void RateFollowsTheClosedForm()
    {
        var spring = Spring4x32();
        spring.Calculate();

        Assert.Equal(20864000.0 / 2097152.0, spring.SpringRateCalculated, 9);
        Assert.Equal(spring.SpringRateCalculated, spring.SpringRate, 9);
    }

    /// <summary>
    /// Wahl at spring index w = 8: (4w-1)/(4w-4) + 0.615/w = 31/28 + 0.076875.
    /// </summary>
    [Fact]
    public void WahlFactorMatchesTheStandardForm()
    {
        var spring = Spring4x32();
        spring.Calculate();

        Assert.Equal(8.0, spring.SpringIndex, 9);
        Assert.Equal(31.0 / 28.0 + 0.615 / 8.0, spring.WahlFactor, 9);
    }

    /// <summary>
    /// Two test points override the closed form: a spring measured at 20 N / 90 mm
    /// and 60 N / 70 mm has a rate of 40 N over 20 mm whatever its geometry says.
    /// </summary>
    [Fact]
    public void MeasuredPointsWinOverTheClosedForm()
    {
        var spring = Spring4x32();
        spring.Force1 = 20; spring.Length1 = 90;
        spring.Force2 = 60; spring.Length2 = 70;
        spring.Calculate();

        Assert.Equal(2.0, spring.SpringRate, 9);
        Assert.NotEqual(spring.SpringRateCalculated, spring.SpringRate);
    }

    private static SpringEngine Spring4x32() => new()
    {
        WireDiameter = 4,
        MeanCoilDiameter = 32,
        ActiveCoils = 8,
        TotalCoils = 10,
        FreeLength = 100,
        ShearModulus = 81500,
        TensileStrength = 1700
    };
}
