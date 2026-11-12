using MechanicalCalculatorWeb.Models;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

public class KeyConnectionTests
{
    /// <summary>
    /// DIN 6885 key 12x8 on a 40 mm shaft, 50 mm long, carrying 500 Nm.
    ///   F_t   = 2T/d          = 2 x 500000 Nmm / 40 mm      = 25000 N
    ///   l_eff = l - b         = 50 - 12                     = 38 mm
    ///   A_shaft = l_eff x t1  = 38 x 5                      = 190 mm2
    ///   tau   = F/(l_eff x b) = 25000 / 456                 = 54.8246 MPa
    /// </summary>
    [Fact]
    public void ForcesAndAreasFollowFromTheGeometry()
    {
        var engine = Key12x8OnShaft40();
        engine.Calculate();

        Assert.Equal(25000.0, engine.TangentialForce, 6);
        Assert.Equal(25000.0, engine.ForcePerKey, 6);
        Assert.Equal(38.0, engine.EffectiveLength, 6);
        Assert.Equal(190.0, engine.ContactAreaShaft, 6);
        Assert.Equal(25000.0 / 456.0, engine.ShearStress, 6);
    }

    /// <summary>
    /// The hub bears on whatever the key actually stands proud by, so the height
    /// is min(h - t1, t2) - here min(8 - 5, 3.3) = 3.0, not the nominal t2 of 3.3.
    /// Using t2 would give 199.4 MPa and overstate the joint by 10%. This is a
    /// deliberate conservatism and the required-length calculation shares it.
    /// </summary>
    [Fact]
    public void HubBearingHeightIsTheSmallerOfProtrusionAndKeywayDepth()
    {
        var engine = Key12x8OnShaft40();
        engine.Calculate();

        Assert.Equal(3.0, engine.BearingHeightHub, 6);
        Assert.Equal(114.0, engine.ContactAreaHub, 6);
        Assert.Equal(25000.0 / 114.0, engine.SurfacePressureHub, 6);
    }

    /// <summary>
    /// A second key is not worth a second key: DIN 6885 credits 0.75 per key, so
    /// two carry 1.5x the load of one, not 2x.
    /// </summary>
    [Fact]
    public void ExtraKeysAreCreditedAtSeventyFivePercent()
    {
        var one = Key12x8OnShaft40();
        one.Calculate();

        var two = Key12x8OnShaft40();
        two.NumberOfKeys = 2;
        two.Calculate();

        Assert.Equal(one.ForcePerKey / 1.5, two.ForcePerKey, 6);
    }

    /// <summary>
    /// Both flanks of a contact pair carry the same pressure, so the weaker of the
    /// two materials sets the allowable. The key material used to reach the shear
    /// check only: a soft key in hard parts reported the same pressure safety
    /// factors as an all-hard joint, i.e. wrong in the unsafe direction in exactly
    /// the common case.
    /// </summary>
    [Fact]
    public void TheSofterOfKeyAndMatingPartSetsTheAllowablePressure()
    {
        var engine = Key12x8OnShaft40();
        engine.KeyMaterial = Soft();
        engine.Calculate();

        // 0.9 x 235, not 0.9 x 430.
        Assert.Equal(211.5, engine.AllowablePressureHub, 6);
        Assert.Equal(211.5, engine.AllowablePressureShaft, 6);
        Assert.Contains("key", engine.GoverningPartHub);

        // The hub flank is at 219.3 MPa, so a soft key fails a joint the hard key passes.
        Assert.True(engine.SafetyFactorHub < 1.0);
        Assert.True(Key12x8OnShaft40Calculated().SafetyFactorHub > 1.5);
    }

    /// <summary>
    /// A stronger key than the parts must not raise the allowable - the shaft and
    /// hub keyways are still what crush.
    /// </summary>
    [Fact]
    public void AStrongerKeyDoesNotRaiseTheAllowablePressure()
    {
        var engine = Key12x8OnShaft40();
        engine.ShaftMaterial = Soft();
        engine.HubMaterial = Soft();
        engine.KeyMaterial = Hard();
        engine.Calculate();

        Assert.Equal(0.9 * Soft().YieldStrength, engine.AllowablePressureHub, 6);
        Assert.Contains("hub", engine.GoverningPartHub);
    }

    /// <summary>
    /// The stresses used to be written only inside `if (area > 0)`, so a run that
    /// came out degenerate left the PREVIOUS run's values on screen - and the page
    /// reuses one engine instance across "Back to Input", so that was the normal
    /// path. Geometry read l_eff = 0.0 next to a full set of safety factors.
    /// </summary>
    [Fact]
    public void ADegenerateRunDoesNotKeepThePreviousRunsStresses()
    {
        var engine = Key12x8OnShaft40();
        engine.KeyLength = 20;
        engine.Calculate();
        Assert.True(engine.SurfacePressureHub > 0);

        // Same instance, now a key as long as it is wide: no bearing length.
        engine.KeyLength = engine.KeyWidth;
        engine.Calculate();

        Assert.Equal(0.0, engine.EffectiveLength, 6);
        Assert.Equal(0.0, engine.SurfacePressureShaft, 6);
        Assert.Equal(0.0, engine.SurfacePressureHub, 6);
        Assert.Equal(0.0, engine.ShearStress, 6);
    }

    /// <summary>
    /// Zero contact area is a key that carries nothing, not an unloaded joint - the
    /// torque is validated positive before Calculate runs. It used to return the
    /// 999 sentinel, which painted all three rows "OK".
    /// </summary>
    [Fact]
    public void ZeroBearingAreaIsNotASafeJoint()
    {
        var engine = Key12x8OnShaft40();
        engine.KeyLength = engine.KeyWidth;
        engine.Calculate();

        Assert.Equal(0.0, engine.SafetyFactorShaft, 6);
        Assert.Equal(0.0, engine.SafetyFactorHub, 6);
        Assert.Equal(0.0, engine.SafetyFactorShear, 6);
        Assert.Equal(0.0, engine.SafetyFactorMin, 6);
    }

    /// <summary>
    /// l &lt; b was silently replaced by 0.9 l, which produced a bearing length for
    /// a key that has none. On a 12 mm wide key, a 10 mm key reported l_eff = 9.0.
    /// </summary>
    [Fact]
    public void AKeyShorterThanItIsWideHasNoBearingLength()
    {
        var engine = Key12x8OnShaft40();
        engine.KeyLength = 10;
        engine.Calculate();

        Assert.Equal(-2.0, engine.EffectiveLength, 6);
        Assert.Equal(0.0, engine.ContactAreaShaft, 6);
        Assert.Equal(0.0, engine.ContactAreaHub, 6);
        Assert.Equal(0.0, engine.ShearArea, 6);
    }

    /// <summary>
    /// A keyway cut deeper than the key is tall cannot give more bearing height
    /// than the key has. t1 = 9 on an 8 mm key used to be taken at face value and
    /// *improved* the shaft safety factor from 2.94 to 5.29.
    /// </summary>
    [Fact]
    public void AKeywayDeeperThanTheKeyAddsNoBearingHeight()
    {
        var engine = Key12x8OnShaft40();
        engine.KeywayDepthShaft = 9;
        engine.Calculate();

        // Capped at h = 8, not the 9 that was entered.
        Assert.Equal(8.0, engine.BearingHeightShaft, 6);

        // And with t1 past h there is nothing left to bear on the hub.
        Assert.True(engine.BearingHeightHub < 0);
        Assert.Equal(0.0, engine.ContactAreaHub, 6);
        Assert.Equal(0.0, engine.SafetyFactorHub, 6);
    }

    /// <summary>
    /// With t1 = h the key sits flush in the shaft and nothing stands proud to bear
    /// on the hub. That used to fall back to bearing on t2 and report SF_hub 1.94.
    /// </summary>
    [Fact]
    public void AKeyFlushInTheShaftCannotBearOnTheHub()
    {
        var engine = Key12x8OnShaft40();
        engine.KeywayDepthShaft = engine.KeyHeight;
        engine.Calculate();

        Assert.Equal(0.0, engine.BearingHeightHub, 6);
        Assert.Equal(0.0, engine.ContactAreaHub, 6);
        Assert.Equal(0.0, engine.SafetyFactorHub, 6);
    }

    /// <summary>
    /// DIN 6892 stops crediting key length at 1.3 d. Nothing enforced it, so a
    /// 100 mm key on a 40 mm shaft (2.5 d) was rated on all 88 mm of bearing
    /// length and came out SF_hub 4.09.
    /// </summary>
    [Fact]
    public void LoadCarryingLengthStopsAtOnePointThreeDiameters()
    {
        var engine = Key12x8OnShaft40();
        engine.KeyLength = 100;
        engine.Calculate();

        Assert.True(engine.LoadCarryingLengthLimited);
        Assert.Equal(52.0, engine.LoadCarryingLengthLimit, 6);
        Assert.Equal(52.0, engine.LoadCarryingLength, 6);

        // A longer key buys nothing past the limit.
        var longer = Key12x8OnShaft40();
        longer.KeyLength = 250;
        longer.Calculate();
        Assert.Equal(engine.SafetyFactorHub, longer.SafetyFactorHub, 6);
    }

    /// <summary>A key inside the limit is unaffected by it.</summary>
    [Fact]
    public void AKeyInsideTheLimitUsesItsWholeEffectiveLength()
    {
        var engine = Key12x8OnShaft40Calculated();

        Assert.False(engine.LoadCarryingLengthLimited);
        Assert.Equal(engine.EffectiveLength, engine.LoadCarryingLength, 6);
    }

    /// <summary>
    /// The required length is sized for the safety factor the page tells the reader
    /// to aim for. It used to be sized for 1.0, so fitting the recommended key
    /// scored "Marginal" in the table directly above the recommendation.
    /// </summary>
    [Fact]
    public void FittingTheRequiredLengthReachesTheTargetSafetyFactor()
    {
        var engine = Key12x8OnShaft40();
        engine.AppliedTorque = 600;
        engine.Calculate();

        var fitted = Key12x8OnShaft40();
        fitted.AppliedTorque = 600;
        fitted.KeyLength = engine.RequiredKeyLength;
        fitted.Calculate();

        Assert.True(fitted.SafetyFactorMin >= engine.TargetSafetyFactor,
            $"SF_min {fitted.SafetyFactorMin:F3} at the recommended {engine.RequiredKeyLength:F0} mm");
    }

    /// <summary>
    /// When the checks need more bearing length than 1.3 d allows, no key length
    /// works and the page has to say so rather than recommend an impossible key.
    /// </summary>
    [Fact]
    public void RequiredLengthIsFlaggedWhenNoKeyLengthCanReachTheTarget()
    {
        var engine = Key12x8OnShaft40();
        engine.AppliedTorque = 3000;
        engine.Calculate();

        Assert.True(engine.RequiredBearingLength > engine.LoadCarryingLengthLimit);
        Assert.True(engine.RequiredLengthExceedsLimit);
    }

    /// <summary>Doubling the torque must never improve a safety factor.</summary>
    [Fact]
    public void MoreTorqueNeverImprovesASafetyFactor()
    {
        var baseline = Key12x8OnShaft40Calculated();

        var doubled = Key12x8OnShaft40();
        doubled.AppliedTorque *= 2;
        doubled.Calculate();

        Assert.True(doubled.SafetyFactorShaft < baseline.SafetyFactorShaft);
        Assert.True(doubled.SafetyFactorHub < baseline.SafetyFactorHub);
        Assert.True(doubled.SafetyFactorShear < baseline.SafetyFactorShear);
        Assert.True(doubled.RequiredKeyLength > baseline.RequiredKeyLength);
    }

    private static Material Hard() => new()
    {
        Name = "Hard test steel",
        YieldStrength = 430,
        TensileStrength = 650,
        ElasticModulus = 206,
        PoissonRatio = 0.3
    };

    private static Material Soft() => new()
    {
        Name = "Soft test steel",
        YieldStrength = 235,
        TensileStrength = 360,
        ElasticModulus = 206,
        PoissonRatio = 0.3
    };

    private static KeyConnectionEngine Key12x8OnShaft40Calculated()
    {
        var engine = Key12x8OnShaft40();
        engine.Calculate();
        return engine;
    }

    private static KeyConnectionEngine Key12x8OnShaft40() => new()
    {
        ShaftDiameter = 40,
        KeyWidth = 12,
        KeyHeight = 8,
        KeyLength = 50,
        KeywayDepthShaft = 5.0,
        KeywayDepthHub = 3.3,
        NumberOfKeys = 1,
        AppliedTorque = 500,
        LoadFactor = 1.0,
        ShaftMaterial = Hard(),
        HubMaterial = Hard(),
        KeyMaterial = Hard()
    };
}
