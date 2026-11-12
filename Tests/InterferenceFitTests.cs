using System;
using System.Linq;
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

    /// <summary>
    /// A HOLLOW shaft carries its largest stress at the BORE, not at the fit
    /// surface. Under external pressure p on a shaft of bore ratio Qi the two
    /// candidate points are
    ///   fit surface: sigma_r = -p,  sigma_t = -p (1+Qi^2)/(1-Qi^2)
    ///   bore:        sigma_r = 0,   sigma_t = -2p/(1-Qi^2)
    /// and the bore always wins, by 1/sqrt(1 - Qi^2 + Qi^4) (34 % at Qi = 0,8).
    /// Reporting the fit surface alone understated a Qi = 0,8 sleeve by a quarter.
    /// </summary>
    [Fact]
    public void HollowShaftPeakStressIsAtTheBore()
    {
        var engine = SolidSteelJoint();
        engine.ShaftInnerDiameter = 40;   // Qi = 0,8
        engine.Calculate();

        double p = engine.MaxContactPressure;
        double qi = 0.8;

        Assert.Equal("bore", engine.ShaftStressPoint);
        Assert.Equal(0.0, engine.ShaftRadialStress, 9);
        Assert.Equal(-2.0 * p / (1 - qi * qi), engine.ShaftTangentialStress, 6);
        Assert.Equal(2.0 * p / (1 - qi * qi), engine.ShaftVonMisesStress, 6);

        // The fit surface value, which is what used to be reported.
        double surfaceT = -p * (1 + qi * qi) / (1 - qi * qi);
        double surfaceV = Math.Sqrt(surfaceT * surfaceT - surfaceT * -p + p * p);
        Assert.True(engine.ShaftVonMisesStress > surfaceV,
            "the bore must govern over the fit surface");
    }

    /// <summary>A solid shaft has no bore, so the fit surface is the only point.</summary>
    [Fact]
    public void SolidShaftIsEvaluatedAtTheFitSurface()
    {
        var engine = SolidSteelJoint();
        engine.Calculate();

        Assert.Equal("fit surface", engine.ShaftStressPoint);
        Assert.Equal(-engine.MaxContactPressure, engine.ShaftRadialStress, 9);
        Assert.Equal(-engine.MaxContactPressure, engine.ShaftTangentialStress, 9);
    }

    /// <summary>
    /// Rotation pulls two ways: it RELIEVES the interface pressure while ADDING a
    /// centrifugal hoop stress at the hub bore. Which way the net goes depends on
    /// the geometry, so neither state can be assumed to govern and both have to be
    /// evaluated. The engine reports the worse of them.
    ///
    /// Standstill is the state the parts are in after assembly and at every
    /// shutdown, so it is never the one to skip; it carries the full pressure.
    /// </summary>
    [Fact]
    public void BothStandstillAndRotatingStatesAreChecked()
    {
        var engine = SolidSteelJoint();
        engine.RotationalSpeed = 10000;
        engine.Calculate();

        Assert.True(engine.RotationalPressureLoss > 0);

        var standstill = engine.StressCases.Single(c => c.State == "Standstill, ambient temperature");
        var rotating = engine.StressCases.Single(c => c.State.StartsWith("Rotating"));

        // Standstill carries the full pressure; rotation has given some away.
        Assert.True(standstill.Pressure > rotating.Pressure);

        // The reported value is the worse of the two, whichever that is.
        Assert.Equal(Math.Max(standstill.HubVonMises, rotating.HubVonMises), engine.HubVonMisesStress, 9);
        Assert.Equal(Math.Max(standstill.ShaftVonMises, rotating.ShaftVonMises), engine.ShaftVonMisesStress, 9);

        // On this geometry the centrifugal term outweighs the pressure it costs,
        // so the rotating state is the harder one - which is only visible because
        // the hoop stress is added rather than ignored.
        Assert.True(rotating.HubVonMises > standstill.HubVonMises * 1.0,
            "the centrifugal hoop stress must reach the hub bore");
        Assert.Equal("Rotating, service temperature", engine.HubGoverningState);
    }

    /// <summary>
    /// Pressure alone cannot explain the rotating hub's stress: the centrifugal
    /// hoop stress is on top of it. Without it a spinning hub reads SAFER than a
    /// stationary one on every geometry, which is the error this guards.
    /// </summary>
    [Fact]
    public void CentrifugalHoopStressIsAddedNotJustSubtracted()
    {
        var engine = SolidSteelJoint();
        engine.RotationalSpeed = 10000;
        engine.Calculate();

        var rotating = engine.StressCases.Single(c => c.State.StartsWith("Rotating"));

        // What the same pressure would give with no rotation at all.
        var still = SolidSteelJoint();
        still.ShaftUpperDeviation = 50 * rotating.Pressure / 78.75;
        still.ShaftLowerDeviation = still.ShaftUpperDeviation;
        still.Calculate();

        Assert.Equal(rotating.Pressure, still.MaxContactPressure, 6);
        Assert.True(rotating.HubVonMises > still.HubVonMisesStress,
            "at the same pressure, a rotating hub must be stressed more");
    }

    /// <summary>
    /// A hub that runs hotter than the shaft loses interference, so the joint is
    /// at its tightest cold. A safety factor quoted only at temperature can be an
    /// order of magnitude optimistic: an aluminium hub at 100 C drops the pressure
    /// from 28 MPa to under 2.
    /// </summary>
    [Fact]
    public void YieldIsCheckedColdWhenTheHubRunsHot()
    {
        var engine = SolidSteelJoint();
        engine.HubMaterial = TestMaterials.Steel();
        engine.HubMaterial.ThermalExpansion = 23.0;   // aluminium
        engine.HubServiceTemperature = 100;
        engine.ShaftServiceTemperature = 100;
        engine.Calculate();

        Assert.True(engine.ThermalInterferenceChange < 0, "a hotter alloy hub grows away from the shaft");
        Assert.Equal("Standstill, ambient temperature", engine.HubGoverningState);

        var hot = engine.StressCases.Single(c => c.State == "Standstill, service temperature");
        Assert.True(engine.HubVonMisesStress > 5 * hot.HubVonMises,
            "the cold state is far harder than the hot one here");
    }

    /// <summary>
    /// Torque and axial force share one friction interface, so the resultant
    /// safety can never exceed either separate one, and is strictly lower when
    /// both loads act. Checking them separately is what let a joint at 0,90 show
    /// 1,28 and 1,59 and read as acceptable.
    /// </summary>
    [Fact]
    public void CombinedSafetyIsLowerThanEitherAlone()
    {
        var engine = SolidSteelJoint();
        engine.AppliedTorque = 250;
        engine.AppliedAxialForce = 10000;
        engine.Calculate();

        Assert.True(engine.SafetyFactorCombined < engine.SafetyFactorSliding);
        Assert.True(engine.SafetyFactorCombined < engine.SafetyFactorAxial);

        // F_t = 2000 T / d, and the resultant is the hypotenuse.
        double ft = 2000.0 * 250 / 50.0;
        Assert.Equal(ft, engine.TangentialForce, 6);
        Assert.Equal(Math.Sqrt(ft * ft + 10000.0 * 10000.0), engine.ResultantForce, 6);
    }

    /// <summary>Axial force alone must not change the resultant check's meaning.</summary>
    [Fact]
    public void CombinedSafetyMatchesTheSingleLoadWhenOnlyOneActs()
    {
        var engine = SolidSteelJoint();
        engine.AppliedTorque = 500;
        engine.AppliedAxialForce = 0;
        engine.Calculate();

        // Same friction coefficient, so the two must agree exactly.
        Assert.Equal(engine.FrictionCoefficientTorque, engine.FrictionCoefficientCombined, 9);
        Assert.Equal(engine.SafetyFactorSliding, engine.SafetyFactorCombined, 6);
    }

    /// <summary>
    /// When the grip is gone the page has to say which input ate it. A transition
    /// fit, a rough joint and a hot aluminium hub all produce a safety factor of
    /// zero and are indistinguishable without this.
    /// </summary>
    [Fact]
    public void LostInterferenceNamesItsCause()
    {
        var transition = SolidSteelJoint();
        transition.ShaftLowerDeviation = -8;      // loose limit gives clearance
        transition.Calculate();
        Assert.True(transition.IsTransitionFit);
        Assert.Contains("transition fit", string.Join(" ", transition.InterferenceLossReasons));

        var rough = SolidSteelJoint();
        rough.ShaftLowerDeviation = 18;
        rough.ShaftRoughness = 30;
        rough.HubRoughness = 30;                  // 48 um of loss against 18 um
        rough.Calculate();
        Assert.Contains("roughness", string.Join(" ", rough.InterferenceLossReasons));
    }

    /// <summary>
    /// The engine instance is reused across "Back to Input", so a second run must
    /// not inherit the first one's state. Lists in particular have to be cleared.
    /// </summary>
    [Fact]
    public void ASecondRunDoesNotInheritTheFirst()
    {
        var engine = SolidSteelJoint();
        engine.ShaftLowerDeviation = -8;
        engine.Calculate();
        Assert.NotEmpty(engine.InterferenceLossReasons);
        int cases = engine.StressCases.Count;

        engine.ShaftLowerDeviation = 50;
        engine.Calculate();
        Assert.Empty(engine.InterferenceLossReasons);
        Assert.Equal(cases, engine.StressCases.Count);
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
