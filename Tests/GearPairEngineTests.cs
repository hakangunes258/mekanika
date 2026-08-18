using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// End-to-end runs of the whole ISO 6336 chain. The factor tests next door check
/// individual curves; these check that the sequence in Calculate() holds together
/// and that its headline numbers stay put.
/// </summary>
public class GearPairEngineTests
{
    /// <summary>
    /// Transverse contact ratio of an unshifted spur pair, m 2, z 20/40, alpha 20:
    ///   eps_a = [sqrt(ra1^2 - rb1^2) + sqrt(ra2^2 - rb2^2) - a sin(alpha)] / (pi m cos alpha)
    /// which is 1.6352 by hand. Geometry is upstream of every strength factor, so
    /// this failing means nothing downstream can be trusted.
    /// </summary>
    [Fact]
    public void TransverseContactRatioMatchesHandCalculation()
    {
        var engine = SpurPair();
        engine.Calculate();

        const double alpha = 20.0 * System.Math.PI / 180.0;
        double d1 = 40, d2 = 80, a = 60;
        double ra1 = d1 / 2 + 2, ra2 = d2 / 2 + 2;
        double rb1 = d1 / 2 * System.Math.Cos(alpha), rb2 = d2 / 2 * System.Math.Cos(alpha);
        double expected = (System.Math.Sqrt(ra1 * ra1 - rb1 * rb1)
                           + System.Math.Sqrt(ra2 * ra2 - rb2 * rb2)
                           - a * System.Math.Sin(alpha))
                          / (System.Math.PI * 2 * System.Math.Cos(alpha));

        Assert.Equal(1.6352, expected, 4);
        Assert.Equal(expected, engine.TransverseContactRatio, 4);
    }

    /// <summary>
    /// ISO 6336-2 Clause 6: the zone factor of an unshifted spur pair at 20 deg is
    /// 2.4945, one of the few numbers in this standard everyone knows by sight.
    /// </summary>
    [Fact]
    public void ZoneFactorForAnUnshiftedSpurPair()
    {
        var engine = SpurPair();
        engine.Calculate();

        // Z_H = sqrt(2 cos(beta_b) cos(alpha_wt) / (cos^2(alpha_t) sin(alpha_wt))),
        // which for a spur pair with no shift collapses to sqrt(2 / (cos a sin a)).
        const double alpha = 20.0 * System.Math.PI / 180.0;
        double expected = System.Math.Sqrt(2.0 / (System.Math.Cos(alpha) * System.Math.Sin(alpha)));

        Assert.Equal(2.494573, expected, 6);
        Assert.Equal(expected, engine.ZoneFactorH, 5);
    }

    /// <summary>
    /// K_V and K_Hbeta both default to calculated. They used to default to a
    /// hard-coded 1.10 that reached every result on the page unless the user
    /// opened a dialog, and in the resonance range that stand-in is an order of
    /// magnitude low. A value of exactly 1.10 here would mean the default flipped
    /// back.
    /// </summary>
    [Fact]
    public void DynamicAndFaceLoadFactorsAreCalculatedNotStandIns()
    {
        var engine = SpurPair();
        engine.Calculate();

        Assert.True(engine.DynamicFactor > 1.0, "K_V must exceed 1 for a loaded, rotating pair");
        Assert.NotEqual(1.10, engine.DynamicFactor, 6);
        Assert.True(engine.FaceLoadFactorFlank > 1.0, "K_Hbeta must exceed 1 once shaft deflection is included");
    }

    /// <summary>
    /// Every headline result must be a real, positive number. A NaN here reaches
    /// the page as a blank cell and an infinity as a safety factor of "inf" -
    /// both of which have shipped from division by an uninitialised strength.
    /// </summary>
    [Fact]
    public void HeadlineResultsAreFiniteAndPositive()
    {
        var engine = SpurPair();
        engine.Calculate();

        foreach (var (name, value) in new (string, double)[]
        {
            (nameof(engine.TransverseContactRatio), engine.TransverseContactRatio),
            (nameof(engine.ZoneFactorH), engine.ZoneFactorH),
            (nameof(engine.ElasticityFactor), engine.ElasticityFactor),
            (nameof(engine.DynamicFactor), engine.DynamicFactor),
            (nameof(engine.FaceLoadFactorFlank), engine.FaceLoadFactorFlank),
        })
        {
            Assert.False(double.IsNaN(value), $"{name} came out NaN");
            Assert.False(double.IsInfinity(value), $"{name} came out infinite");
            Assert.True(value > 0, $"{name} came out {value}");
        }
    }

    /// <summary>A pair asked to carry more torque cannot come out safer.</summary>
    [Fact]
    public void MorePowerCannotRaiseTheSafetyFactors()
    {
        var light = SpurPair();
        light.Calculate();

        var heavy = SpurPair();
        heavy.Power = light.Power * 2;
        heavy.Calculate();

        Assert.True(heavy.FlankSafetyFactor1 < light.FlankSafetyFactor1,
            "doubling the power left the flank safety factor unchanged or higher");
        Assert.True(heavy.RootSafetyFactor1 < light.RootSafetyFactor1,
            "doubling the power left the root safety factor unchanged or higher");
    }

    /// <summary>m 2, z 20/40, unshifted spur pair on a 60 mm centre distance.</summary>
    private static GearPairEngine SpurPair()
    {
        var engine = new GearPairEngine
        {
            Power = 10,
            Speed1 = 1000,
            RequiredServiceLife = 20000,
            NormalModule = 2,
            HelixAngle = 0,
            CenterDistance = 60,
            NumberOfTeeth1 = 20,
            FaceWidth1 = 30,
            ProfileShiftCoeff1 = 0,
            NumberOfTeeth2 = 40,
            FaceWidth2 = 30,
            ProfileShiftCoeff2 = 0
        };
        return engine;
    }
}
