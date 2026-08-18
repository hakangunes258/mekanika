using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

public class InterferenceFitTests
{
    /// <summary>
    /// Solid steel shaft in a steel hub, d 50, D_a 100, a flat 50 um of diametral
    /// interference with no roughness, thermal or centrifugal terms in play.
    ///
    /// Classical Lame for this joint gives a compliance of
    ///   C = d (C1/E1 + C2/E2),  C1 = 1 - v = 0.7,
    ///   C2 = (1 + Q^2)/(1 - Q^2) + v = 1.9666.. at Q = d/D_a = 0.5
    ///   C = 50 (0.7 + 1.96667)/210000 = 6.349206e-4 mm/MPa
    ///   p = 0.050 / C = 78.75 MPa exactly.
    /// </summary>
    [Fact]
    public void ContactPressureMatchesLame()
    {
        var engine = SolidSteelJoint();
        engine.Calculate();

        Assert.Equal(50.0, engine.EffectiveMaxInterference, 6);
        Assert.Equal(78.75, engine.MaxContactPressure, 4);
    }

    /// <summary>
    /// Roughness loss is 0.8 (Rz_shaft + Rz_hub) per DIN 7190 and comes off the
    /// geometric interference before any pressure is worked out.
    /// </summary>
    [Fact]
    public void RoughnessComesOffTheInterference()
    {
        var engine = SolidSteelJoint();
        engine.ShaftRoughness = 6.3;
        engine.HubRoughness = 6.3;
        engine.Calculate();

        Assert.Equal(10.08, engine.RoughnessLoss, 6);
        Assert.Equal(39.92, engine.EffectiveMaxInterference, 6);
        Assert.Equal(39.92 / 50.0 * 78.75, engine.MaxContactPressure, 4);
    }

    /// <summary>
    /// A hub that never reaches the shaft is not a joint. The engine returns zero
    /// pressure rather than a compliance computed from a negative Q term - which
    /// would produce a large, plausible and entirely wrong number.
    /// </summary>
    [Theory]
    [InlineData(50.0, 50.0)]    // hub outer diameter equal to the joint
    [InlineData(50.0, 40.0)]    // hub outer diameter smaller than the joint
    public void ImpossibleGeometryGivesNoPressure(double d, double da)
    {
        var engine = SolidSteelJoint();
        engine.NominalDiameter = d;
        engine.HubOuterDiameter = da;
        engine.Calculate();

        Assert.Equal(0.0, engine.MaxContactPressure);
    }

    /// <summary>
    /// Centrifugal loss was once computed from diameters where the theory calls
    /// for radii, which overstated it by a factor of four. At 3000 rpm on this
    /// joint the loss is a few tenths of a MPa; a factor of four would be visible
    /// immediately. The guard here is the ratio between two speeds: the loss goes
    /// with omega^2, so doubling the speed must quadruple it.
    /// </summary>
    [Fact]
    public void CentrifugalLossScalesWithSpeedSquared()
    {
        var slow = SolidSteelJoint();
        slow.RotationalSpeed = 3000;
        slow.Calculate();

        var fast = SolidSteelJoint();
        fast.RotationalSpeed = 6000;
        fast.Calculate();

        Assert.True(slow.RotationalPressureLoss > 0, "a rotating joint must lose some interference");
        Assert.Equal(4.0, fast.RotationalPressureLoss / slow.RotationalPressureLoss, 6);
    }

    private static InterferenceFitEngine SolidSteelJoint() => new()
    {
        NominalDiameter = 50,
        HubOuterDiameter = 100,
        ShaftInnerDiameter = 0,
        FitLength = 50,
        ShaftUpperDeviation = 50,
        ShaftLowerDeviation = 50,
        HubUpperDeviation = 0,
        HubLowerDeviation = 0,
        ShaftRoughness = 0,
        HubRoughness = 0,
        AmbientTemperature = 20,
        ShaftServiceTemperature = 20,
        HubServiceTemperature = 20,
        RotationalSpeed = 0,
        AppliedTorque = 500,
        LoadFactor = 1.0,
        ShaftMaterial = TestMaterials.Steel(),
        HubMaterial = TestMaterials.Steel()
    };
}
