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
        ShaftMaterial = TestMaterials.Steel(),
        HubMaterial = TestMaterials.Steel(),
        KeyMaterial = TestMaterials.Steel()
    };
}
