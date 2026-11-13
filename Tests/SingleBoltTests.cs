using System;
using System.Collections.Generic;
using System.Linq;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the Nov 2026 review of the single bolt module. The default joint
/// is the page's own worked example: M12 class 8.8 through-bolt with nut and two
/// washers, 2 x 15 mm S235JR, D_A 36, every friction 0,12, torque wrench, SV1,
/// F_A swinging 2 kN to 10 kN, 5 kN of residual clamp force required.
/// </summary>
public class SingleBoltTests
{
    private static SingleBoltInput Default() => new()
    {
        BoltSize = "M12",
        StrengthClass = "8.8",
        BoltStandard = "ISO4014",
        BoltLength = 65,
        JointType = JointType.ThroughHole,
        BoltingType = BoltingType.SV1_ThroughBolt_LoadAtHead,
        TighteningMethod = "Torque wrench",
        SurfaceCondition = "Custom",
        FrictionThread = 0.12,
        FrictionHead = 0.12,
        InterfaceFriction = 0.12,
        NumberOfInterfaces = 1,
        UseWasherUnderHead = true,
        UseWasherUnderNut = true,
        AxialForceMin = 2000,
        AxialForceMax = 10000,
        RequiredResidualClampForce = 5000,
        AutoCalculateLoadIntroductionFactor = true,
        ClampedParts = new List<ClampedPart>
        {
            // The page writes the assembly outside diameter onto the FIRST part
            // only, so the fixture does the same - otherwise the compliance comes
            // out slightly different from what the page shows.
            new() { Thickness = 15, MaterialName = "S235JR", ElasticModulus = 210000, HoleDiameter = 13.5, OuterDiameter = 36, SurfaceFinish = "Rolled" },
            new() { Thickness = 15, MaterialName = "S235JR", ElasticModulus = 210000, HoleDiameter = 13.5, SurfaceFinish = "Rolled" }
        }
    };

    private static SingleBoltResult Run(SingleBoltInput input)
        => new BoltCalculationEngine().Calculate(input);

    /// <summary>
    /// The worked example, so a change to any of the chain below is visible.
    /// Every figure here was read off the running page.
    /// </summary>
    [Fact]
    public void TheWorkedExampleReproduces()
    {
        var r = Run(Default());

        Assert.Equal(84.3, r.As, 1);
        // The page reads 0,1254 for this joint. The fixture lands 0,9 % away
        // because some of the page's own defaults (washer lookup, surface
        // condition) are not replicated here, so the tolerance is deliberate -
        // what this pins is the chain, not the last digit.
        Assert.Equal(0.1254, r.PhiN, 0.002);
        Assert.Equal(33.5, r.FZ_total_um, 0.1);
        Assert.Equal(8478, r.FZ, 40.0);
        Assert.Equal(1254, r.FSA, 15.0);
        Assert.Equal(22222, r.FM_min, 60.0);
        Assert.Equal(35556, r.FM_max, 100.0);
        Assert.Equal(37770, r.F09_max, 20.0);
    }

    /// <summary>
    /// The transverse force reached NOTHING. LoadCase defaulted to AxialOnly and
    /// no control on the page ever changed it, so the page zeroed F_Q and M_B on
    /// every run: entering 4000 N left every result byte-identical.
    ///
    /// VDI 2230 R1: F_KQ,req = F_Q/(q_F mu_T) = 4000/(1 x 0,12) = 33 333 N.
    /// </summary>
    [Fact]
    public void TransverseForceSetsTheRequiredClampForce()
    {
        var input = Default();
        input.ShearForce = 4000;
        input.LoadCase = LoadCaseType.AxialAndTransverse;
        var r = Run(input);

        Assert.Equal(4000.0 / (1 * 0.12), r.FKR_slip, 0);
        Assert.Equal(r.FKR_slip, r.FKR_min, 0);

        // And it must change the answer: this M12 cannot hold that joint.
        var baseline = Run(Default());
        Assert.True(r.FM_min > 2 * baseline.FM_min);
        Assert.True(r.SF_clamp < 1.0, "the bolt cannot deliver 33 kN of clamp force");
        Assert.NotEmpty(r.Errors);
    }

    /// <summary>
    /// A working moment at the interface tries to lift one edge. The clamp force
    /// has to keep the whole face in compression:
    ///   F_K,req = M_B . A/W, with A and W of the annular interface.
    /// The engine never read the field at all.
    /// </summary>
    [Fact]
    public void BendingMomentSetsAClampForceRequirement()
    {
        var input = Default();
        input.BendingMoment = 200_000;        // 200 Nm in Nmm
        input.LoadCase = LoadCaseType.AxialAndTransverse;
        var r = Run(input);

        double DA = 36, dh = 13.5;
        double area = Math.PI * (DA * DA - dh * dh) / 4.0;
        double W = Math.PI * (Math.Pow(DA, 4) - Math.Pow(dh, 4)) / (32.0 * DA);
        Assert.Equal(200_000 * area / W, r.FKR_moment, 0);

        Assert.True(r.FKR_moment > 0);
        Assert.Equal(Math.Max(5000, r.FKR_moment), r.FKR_min, 0);

        // Zero moment must leave it alone.
        Assert.Equal(0.0, Run(Default()).FKR_moment);
    }

    /// <summary>
    /// SF_clamp used to be FK_min/FKR_min, which is 1,00 BY CONSTRUCTION because
    /// FM_min is sized as FKR_min + FZ + FPA - so every user who entered a
    /// required clamp force saw a red FAIL at exactly 1,00. It is now the clamp
    /// force the bolt can really deliver against what the joint needs, and it
    /// moves.
    /// </summary>
    [Fact]
    public void ClampMarginIsNotOneByConstruction()
    {
        var r = Run(Default());

        Assert.NotEqual(1.0, r.SF_clamp, 2);
        Assert.Equal(r.FKR_available / r.FKR_min, r.SF_clamp, 9);

        // It is F_M,zul/alpha_A less the losses.
        Assert.Equal(r.F09_max / r.AlphaA - r.FZ - r.FPA, r.FKR_available, 6);

        // A lighter requirement must give a larger margin.
        var easier = Default();
        easier.RequiredResidualClampForce = 1000;
        Assert.True(Run(easier).SF_clamp > r.SF_clamp);
    }

    /// <summary>
    /// A joint that comes apart has failed whether or not a clamp force was
    /// requested. The check was guarded on FKR_min > 0, so a joint the page
    /// itself showed with a negative working clamp force reported "n/a, no clamp
    /// force required".
    /// </summary>
    [Fact]
    public void AJointThatOpensIsReportedEvenWithNoClampRequirement()
    {
        var input = Default();
        input.RequiredResidualClampForce = 0;
        input.AxialForceMax = 60000;          // far past this bolt
        input.AxialForceMin = 0;
        var r = Run(input);

        Assert.True(r.FKR_available <= 0, "this load opens the joint");
        Assert.Contains("opens", string.Join(" ", r.Errors));
        Assert.Equal(0.0, r.SF_clamp);
    }

    /// <summary>
    /// The required bolt length was computed, displayed, and never compared with
    /// the bolt selected. A 45 mm bolt sat beside "Required 50 mm" in silence.
    /// </summary>
    [Fact]
    public void ABoltTooShortIsRefused()
    {
        var shortBolt = Default();
        shortBolt.BoltLength = 45;
        var r = Run(shortBolt);

        Assert.False(r.BoltLengthSufficient);
        Assert.Contains("needs", string.Join(" ", r.Errors));

        Assert.True(Run(Default()).BoltLengthSufficient);
    }

    /// <summary>
    /// With a washer fitted the head bears on the WASHER and the washer bears on
    /// the part over its own, much larger, outside diameter. Judging the part by
    /// the head-on-washer pressure was the wrong pressure against the wrong
    /// limit: 419,6 MPa against S235's 490 for a part really at about 111.
    /// </summary>
    [Fact]
    public void SurfacePressureIsJudgedOnWhatThePartBears()
    {
        var r = Run(Default());

        Assert.True(r.WasherCarriesTheHeadPressure);
        Assert.Equal(r.PressureWasherHead, r.PressureOnPartHead, 6);
        Assert.True(r.PressureOnPartHead < r.PressureHead / 2,
            "the washer spreads the load over a far larger area");
        Assert.Equal(r.PressureAllowable / Math.Max(r.PressureOnPartHead, r.PressureOnPartNut),
                     r.SF_pressure, 6);

        // Without washers the head bears directly and the two coincide.
        var bare = Default();
        bare.UseWasherUnderHead = false;
        bare.UseWasherUnderNut = false;
        var b = Run(bare);
        Assert.False(b.WasherCarriesTheHeadPressure);
        Assert.Equal(b.PressureHead, b.PressureOnPartHead, 6);
        Assert.True(b.SF_pressure < r.SF_pressure);
    }

    /// <summary>
    /// Yield and angle controlled tightening take the bolt TO its yield point by
    /// design, so VDI 2230 puts alpha_A at 1 for them. They were listed at 1,06
    /// and 1,20, which produced a comfortable-looking "yield safety factor" of
    /// 1,78 for a method whose whole principle is reaching yield.
    /// </summary>
    [Theory]
    [InlineData("Yield controlled")]
    [InlineData("Angle controlled")]
    public void YieldControlledMethodsAreFlaggedAndHaveNoScatterMargin(string method)
    {
        var tightening = BoltService.TighteningMethods.Single(t => t.Method == method);

        Assert.True(tightening.IsYieldControlled);
        Assert.Equal(1.0, tightening.AlphaA_typical, 9);
    }

    /// <summary>
    /// Torque-controlled methods are not flagged, and an impact wrench is the
    /// worst of them - it was listed at 2,0 where the table runs to about 2,5-4.
    /// </summary>
    [Fact]
    public void TorqueMethodsKeepTheirScatter()
    {
        var wrench = BoltService.TighteningMethods.Single(t => t.Method == "Torque wrench");
        Assert.False(wrench.IsYieldControlled);
        Assert.Equal(1.6, wrench.AlphaA_typical, 9);

        var impact = BoltService.TighteningMethods.Single(t => t.Method == "Impact wrench");
        Assert.False(impact.IsYieldControlled);
        Assert.True(impact.AlphaA_typical >= 2.5, "an unadjusted impact wrench is worse than 2,0");

        // The scatter must rise monotonically from the measured methods to the
        // crudest one, or the table has been mis-entered.
        var ultrasonic = BoltService.TighteningMethods.Single(t => t.Method == "Ultrasonic");
        var hydraulic = BoltService.TighteningMethods.Single(t => t.Method == "Hydraulic tensioner");
        Assert.True(hydraulic.AlphaA_typical < wrench.AlphaA_typical);
        Assert.True(ultrasonic.AlphaA_typical < wrench.AlphaA_typical);
        Assert.True(ultrasonic.AlphaA_typical > 1.0, "a measurement is not perfect");
    }

    /// <summary>
    /// A tapped hole is the one configuration where the THREAD can strip rather
    /// than the bolt stretch, and nothing checked it. The minimum engagement
    /// depends on the tapped material: a class 8.8 bolt into aluminium needs
    /// roughly twice what it needs into steel.
    /// </summary>
    [Fact]
    public void TappedHoleThreadEngagementIsChecked()
    {
        var steel = Default();
        steel.JointType = JointType.TappedHole;
        steel.BoltingType = BoltingType.SV4_TappedHole_LoadAtHead;
        steel.BoltLength = 45;
        var rs = Run(steel);

        Assert.Equal(0.8 * rs.d, rs.MinThreadEngagement, 6);
        Assert.Equal("steel", rs.ThreadEngagementMaterial);

        var alu = Default();
        alu.JointType = JointType.TappedHole;
        alu.BoltingType = BoltingType.SV4_TappedHole_LoadAtHead;
        alu.BoltLength = 45;
        alu.ClampedParts[1].MaterialName = "AlMgSi1 (6082)";
        var ra = Run(alu);

        Assert.Equal("aluminium", ra.ThreadEngagementMaterial);
        Assert.Equal(2.0 * ra.d, ra.MinThreadEngagement, 6);
        Assert.True(ra.MinThreadEngagement > rs.MinThreadEngagement);

        // A through-bolt has no tapped thread to strip.
        Assert.Equal(0.0, Run(Default()).MinThreadEngagement);
    }
}
