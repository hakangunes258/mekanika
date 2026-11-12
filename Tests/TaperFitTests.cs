using System;
using System.Linq;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the Nov 2026 review of the taper fit module. The default joint is
/// the one the page's own worked example uses: Ø50, L 60, 1:10, Δs 0,30 mm,
/// D_a 90, Rz 6,3/6,3, C45/C45, µ_s 0,15 / µ_d 0,12.
/// </summary>
public class TaperFitTests
{
    /// <summary>
    /// Geometry and interference, independently: tan φ = 1/(2X), the end
    /// diameters are d_m ∓ L/(2X), and the diametral interference is Δs/X at
    /// every section along the cone.
    /// </summary>
    [Fact]
    public void GeometryAndInterferenceFollowTheTaperRatio()
    {
        var engine = Default();
        engine.Calculate();

        Assert.Equal(Math.Atan(1.0 / 20.0) * 180 / Math.PI, engine.HalfAngle, 9);
        Assert.Equal(47.0, engine.SmallDiameter, 9);
        Assert.Equal(53.0, engine.LargeDiameter, 9);
        Assert.Equal(300.0 / 10.0, engine.DiametralInterference, 6);   // Δs/X in µm
        Assert.Equal(30.0 - 0.8 * 12.6, engine.EffectiveInterference, 6);
    }

    /// <summary>
    /// With a constant hub outside diameter the local pressure RISES towards the
    /// small end, because the local diameter is least there — so the small end,
    /// not the large one, carries the highest hub stress. The page's own text used
    /// to say the opposite ("at the large end ... where the wall is thinnest").
    /// </summary>
    [Fact]
    public void PressureRisesTowardsTheSmallEnd()
    {
        var engine = Default();
        engine.Calculate();

        var small = engine.Sections.Single(s => s.Name == "Small end");
        var mean = engine.Sections.Single(s => s.Name == "Mean");
        var large = engine.Sections.Single(s => s.Name == "Large end");

        Assert.True(small.Pressure > mean.Pressure);
        Assert.True(mean.Pressure > large.Pressure);
        Assert.True(small.HubVonMises > large.HubVonMises);

        // The quoted contact pressure is the mean-diameter value.
        Assert.Equal(mean.Pressure, engine.ContactPressure, 9);

        // The stress rows report the governing section, not the mean one.
        Assert.Equal(small.HubVonMises, engine.HubVonMisesStress, 9);
        Assert.Equal("Small end", engine.GoverningSection);
    }

    /// <summary>
    /// Same bug as the cylindrical fit: a HOLLOW shaft's peak stress sits at the
    /// bore, with σ_r = 0 and σ_t = −2p/(1−Qi²), not at the fit surface.
    /// </summary>
    [Fact]
    public void HollowShaftPeakStressIsAtTheBore()
    {
        var engine = Default();
        engine.ShaftInnerDiameter = 40;
        engine.Calculate();

        Assert.Equal("bore", engine.ShaftStressPoint);
        Assert.Equal(0.0, engine.ShaftRadialStress, 9);

        var governing = engine.Sections.Single(s => s.Name == engine.GoverningSection);
        double qi = 40.0 / governing.Diameter;
        Assert.Equal(-2.0 * governing.Pressure / (1 - qi * qi), engine.ShaftTangentialStress, 6);
    }

    /// <summary>
    /// Self-locking is decided by µ_s against the angle, not by a fixed 7,5°
    /// threshold. At µ_s = 0,10 a 1:5 taper (φ = 5,71°, tan φ = 0,100) is right at
    /// its limit and drew no warning under the old rule.
    /// </summary>
    [Theory]
    [InlineData(10.0, 0.15, true)]    // 1:10, φ 2,86° — comfortably self-locking
    [InlineData(5.0, 0.15, true)]     // 1:5, φ 5,71°
    [InlineData(5.0, 0.08, false)]    // same cone, slippery: no longer self-locking
    [InlineData(2.0, 0.15, false)]    // 1:2, φ 14,04° — ejects the hub
    public void SelfLockingDependsOnFrictionNotOnAFixedAngle(double ratio, double muStatic, bool locking)
    {
        var engine = Default();
        engine.TaperRatio = ratio;
        engine.FrictionCoefficientStatic = muStatic;
        engine.Calculate();

        Assert.Equal(locking, engine.IsSelfLocking);

        double phi = engine.HalfAngle * Math.PI / 180.0;
        Assert.Equal(muStatic / Math.Cos(phi) > Math.Tan(phi), engine.IsSelfLocking);
    }

    /// <summary>
    /// A taper that is not self-locking has NO axial holding capacity. The old
    /// code took the magnitude of a negative factor, so a 1:2 cone that pushes its
    /// own hub off reported +232 kN of axial capacity next to the warning saying
    /// self-locking might not occur.
    /// </summary>
    [Fact]
    public void ANonSelfLockingTaperHasNoAxialCapacity()
    {
        var engine = Default();
        engine.TaperRatio = 2;          // φ = 14,04°, tan φ = 0,25 > µ_s = 0,15
        engine.Calculate();

        Assert.False(engine.IsSelfLocking);
        Assert.Equal(0.0, engine.MaxAxialForceCapacity);
        Assert.True(engine.ExtractionForce < 0, "the cone ejects the hub");
        Assert.True(engine.RequiredNutForce > 0, "a nut has to hold it");

        // The retaining force is the wedge component less what friction holds.
        double phi = engine.HalfAngle * Math.PI / 180.0;
        double area = Math.PI * (engine.MeanDiameter / 1000.0) * (engine.FitLength / 1000.0);
        double p = engine.ContactPressure * 1e6;
        Assert.Equal(p * area * (Math.Tan(phi) - 0.15 / Math.Cos(phi)), engine.RequiredNutForce, 3);
    }

    /// <summary>A self-holding taper needs a pull to come apart, and no nut.</summary>
    [Fact]
    public void ASelfLockingTaperNeedsAPullToRelease()
    {
        var engine = Default();
        engine.Calculate();

        Assert.True(engine.IsSelfLocking);
        Assert.True(engine.ExtractionForce > 0);
        Assert.Equal(0.0, engine.RequiredNutForce);
        Assert.True(engine.MaxAxialForceCapacity > 0);

        // Pushing on always costs more than pulling off: the wedge changes sign.
        Assert.True(engine.RequiredAssemblyForce > engine.ExtractionForce);
    }

    /// <summary>
    /// Assembly, extraction and self-locking are break-loose conditions and use
    /// the STATIC coefficient; sliding in service uses the DYNAMIC one. The page
    /// text claimed the static coefficient resists the wedge while the capacity
    /// was computed with the dynamic one.
    /// </summary>
    [Fact]
    public void StaticAndDynamicCoefficientsReachTheRightQuantities()
    {
        var engine = Default();
        engine.Calculate();
        double extraction = engine.ExtractionForce;
        double assembly = engine.RequiredAssemblyForce;
        double torque = engine.MaxTorqueCapacity;

        var stickier = Default();
        stickier.FrictionCoefficientStatic = 0.25;   // static only
        stickier.Calculate();
        Assert.True(stickier.ExtractionForce > extraction);
        Assert.True(stickier.RequiredAssemblyForce > assembly);
        Assert.Equal(torque, stickier.MaxTorqueCapacity, 6);

        var grippier = Default();
        grippier.FrictionCoefficientDynamic = 0.20;  // dynamic only
        grippier.Calculate();
        Assert.True(grippier.MaxTorqueCapacity > torque);
        Assert.Equal(extraction, grippier.ExtractionForce, 6);
    }

    /// <summary>
    /// Torque and axial force draw on one friction interface, so the combined
    /// safety is strictly below either separate figure when both loads act. The
    /// two capacities are not equal here — the wedge opposes an axial load and
    /// not a torque — so they combine through an interaction ellipse.
    /// </summary>
    [Fact]
    public void CombinedSafetyIsLowerThanEitherAlone()
    {
        var engine = Default();
        engine.AppliedTorque = 250;
        engine.AppliedAxialForce = 10000;
        engine.Calculate();

        Assert.True(engine.SafetyFactorCombined < engine.SafetyFactorSliding);
        Assert.True(engine.SafetyFactorCombined < engine.SafetyFactorAxial);

        double ft = 2000.0 * 250 / 50.0;
        Assert.Equal(ft, engine.TangentialForce, 6);
        Assert.Equal(Math.Sqrt(ft * ft + 1e8), engine.ResultantForce, 6);

        double expected = 1.0 / Math.Sqrt(
            Math.Pow(ft / engine.TangentialForceCapacity, 2) +
            Math.Pow(10000.0 / engine.MaxAxialForceCapacity, 2));
        Assert.Equal(expected, engine.SafetyFactorCombined, 9);
    }

    /// <summary>
    /// With only one load acting, the interaction check must return exactly that
    /// load's own safety factor — otherwise it would quietly change every result
    /// that has no axial force.
    /// </summary>
    [Fact]
    public void CombinedSafetyMatchesTheSingleLoadWhenOnlyOneActs()
    {
        var torqueOnly = Default();
        torqueOnly.AppliedTorque = 250;
        torqueOnly.AppliedAxialForce = 0;
        torqueOnly.Calculate();
        Assert.Equal(torqueOnly.SafetyFactorSliding, torqueOnly.SafetyFactorCombined, 6);

        var axialOnly = Default();
        axialOnly.AppliedTorque = 0;
        axialOnly.AppliedAxialForce = 10000;
        axialOnly.Calculate();
        Assert.Equal(axialOnly.SafetyFactorAxial, axialOnly.SafetyFactorCombined, 6);
    }

    /// <summary>
    /// A taper that ejects its own hub cannot carry an axial load at all, so the
    /// combined safety has to be zero rather than a number borrowed from the
    /// torque capacity.
    /// </summary>
    [Fact]
    public void NoAxialCapacityUnderLoadGivesAZeroCombinedSafety()
    {
        var engine = Default();
        engine.TaperRatio = 2;
        engine.AppliedTorque = 250;
        engine.AppliedAxialForce = 1000;
        engine.Calculate();

        Assert.False(engine.IsSelfLocking);
        Assert.Equal(0.0, engine.SafetyFactorCombined);
    }

    /// <summary>
    /// The engine is reused across "Back to Input", so the section list must not
    /// grow with every run.
    /// </summary>
    [Fact]
    public void ASecondRunDoesNotAccumulateSections()
    {
        var engine = Default();
        engine.Calculate();
        Assert.Equal(3, engine.Sections.Count);

        engine.Calculate();
        Assert.Equal(3, engine.Sections.Count);
    }

    private static TaperFitEngine Default() => new()
    {
        MeanDiameter = 50,
        FitLength = 60,
        ShaftInnerDiameter = 0,
        HubOuterDiameter = 90,
        TaperRatio = 10,
        AxialDisplacement = 0.30,
        ShaftRoughnessRz = 6.3,
        HubRoughnessRz = 6.3,
        FrictionCoefficientStatic = 0.15,
        FrictionCoefficientDynamic = 0.12,
        AppliedTorque = 250,
        AppliedAxialForce = 0,
        LoadFactor = 1.0,
        ShaftMaterial = TestMaterials.Steel(),
        HubMaterial = TestMaterials.Steel()
    };
}
