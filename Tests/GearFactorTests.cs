using MechanicalCalculatorWeb.Services;
using Xunit;
using static MechanicalCalculatorWeb.Services.Iso6336LifeFactors;

namespace Mekanika.Tests;

/// <summary>
/// The gear module carries roughly a dozen curves lifted out of ISO 6336. Each
/// one reproduces a point its own standard prints, and CLAUDE.md records those
/// points next to the code. They were comments; here they run.
///
/// Note what an anchor can and cannot tell you. It confirms the curve is
/// evaluated correctly - it says nothing about whether it is the right curve.
/// Z_NT shipped for a while on the optimistic of the two rows in ISO 6336-2
/// Table 2 and passed its own anchor the whole time.
/// </summary>
public class GearFactorTests
{
    /// <summary>Involute function and its inverse must round-trip.</summary>
    [Theory]
    [InlineData(14.0)]
    [InlineData(20.0)]
    [InlineData(25.0)]
    [InlineData(31.5)]
    public void InverseInvoluteRoundTrips(double degrees)
    {
        double rad = degrees * System.Math.PI / 180.0;
        double recovered = GearToothMeasurement.InvertInvolute(GearToothMeasurement.Inv(rad));

        Assert.Equal(rad, recovered, 10);
    }

    /// <summary>
    /// ISO 6336-2 Clause 6: Z_E for a steel pair, E 206000 MPa and nu 0.3, is the
    /// textbook 189.8 sqrt(N/mm2).
    /// </summary>
    [Fact]
    public void ElasticityFactorForSteelOnSteel()
    {
        Assert.Equal(189.8, Iso6336SurfaceFactors.CalculateZE(206000, 0.3, 206000, 0.3), 1);
    }

    /// <summary>
    /// ISO 6336-3 Table 4. Rz = 10 um is the reference roughness the factor is
    /// built around, so every material row passes close to 1 there.
    ///
    /// "Close to", not "equal to": the table's three expressions land on 1.00100,
    /// 1.00164 and 0.99436. 1.000 is the rounded value the standard names, and
    /// CLAUDE.md recorded it as if it were exact - which it is not, by up to 0.6%.
    /// Asserting equality to three decimals fails against a correct
    /// implementation, so the anchor is the 1% band the standard actually means.
    /// </summary>
    [Theory]
    [InlineData(SurfaceGroup.StructuralSteel)]
    [InlineData(SurfaceGroup.HardenedSteel)]
    [InlineData(SurfaceGroup.CastIron)]
    public void RelativeSurfaceFactorIsNearUnityAtTheReferenceRoughness(SurfaceGroup group)
    {
        double y = YRrelT(10.0, group);
        Assert.InRange(y, 0.99, 1.01);
    }

    /// <summary>
    /// A rougher root is a weaker root: the factor must fall as Rz rises, and sit
    /// above 1 for a finish better than the reference. Direction is the part an
    /// anchor at a single point cannot check.
    /// </summary>
    [Theory]
    [InlineData(SurfaceGroup.StructuralSteel)]
    [InlineData(SurfaceGroup.HardenedSteel)]
    [InlineData(SurfaceGroup.CastIron)]
    public void RelativeSurfaceFactorFallsAsTheRootGetsRougher(SurfaceGroup group)
    {
        Assert.True(YRrelT(1.0, group) > YRrelT(10.0, group), "a smoother root must not be penalised");
        Assert.True(YRrelT(10.0, group) > YRrelT(40.0, group), "a rougher root must not be rewarded");
    }

    /// <summary>
    /// ISO 6336-2 Table 2, steel and surface-hardened row. The knee sits at 5e7
    /// cycles unless pitting is acceptable on the finished flank, and the curve
    /// keeps descending past it to 0.85 at 1e10.
    ///
    /// The conservative row is the default on purpose: the optimistic one shipped
    /// once and inflated sigma_HP by 13% at 1.1e8 cycles.
    /// </summary>
    [Fact]
    public void FlankLifeFactorUsesTheConservativeRowByDefault()
    {
        Assert.True(ZNT(1e8, LifeGroup.SteelAndHardened) < ZNT(1e8, LifeGroup.SteelAndHardened, optimumConditions: true),
            "the default row must not be the one that permits pitting");

        Assert.Equal(1.0, ZNT(5e7, LifeGroup.SteelAndHardened), 2);
        Assert.Equal(0.85, ZNT(1e10, LifeGroup.SteelAndHardened), 2);
    }

    /// <summary>Both life factors fall as cycles rise, everywhere on the curve.</summary>
    [Fact]
    public void LifeFactorsAreMonotonic()
    {
        double[] cycles = { 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10 };
        for (int i = 1; i < cycles.Length; i++)
        {
            Assert.True(ZNT(cycles[i], LifeGroup.SteelAndHardened) <= ZNT(cycles[i - 1], LifeGroup.SteelAndHardened),
                $"Z_NT rose between {cycles[i - 1]:e0} and {cycles[i]:e0} cycles");
            Assert.True(YNT(cycles[i], LifeGroup.SteelAndHardened) <= YNT(cycles[i - 1], LifeGroup.SteelAndHardened),
                $"Y_NT rose between {cycles[i - 1]:e0} and {cycles[i]:e0} cycles");
        }
    }

    /// <summary>
    /// Base tangent length over 3 teeth of an unshifted spur gear, m 1, z 20,
    /// alpha 20 deg: W_k = m cos(alpha) [(k - 0.5) pi + z inv(alpha)] = 7.6604 mm.
    /// </summary>
    [Fact]
    public void SpanMeasurementMatchesTheClosedForm()
    {
        var result = GearToothMeasurement.Calculate(new GearToothMeasurement.GearInput
        {
            z = 20, mn = 1, alphaN = 20, beta = 0, x = 0,
            d = 20, db = 20 * System.Math.Cos(20 * System.Math.PI / 180.0),
            da = 22, df = 17.5, b = 20,
            SpanTeeth = 3
        });

        double expected = 1.0 * System.Math.Cos(20 * System.Math.PI / 180.0)
                          * (2.5 * System.Math.PI + 20 * GearToothMeasurement.Inv(20 * System.Math.PI / 180.0));

        Assert.Equal(7.6604, expected, 4);
        Assert.Equal(expected, result.Wk, 4);
    }
}
