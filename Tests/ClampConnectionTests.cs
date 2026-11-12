using System;
using MechanicalCalculatorWeb.Models;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the Nov 2026 review of the clamp connection module. The default
/// joint is the page's own worked example: Ø40 shaft, Da 70, L 50, b 4,
/// two M8 class 8.8 bolts at 25 Nm, µ 0,15, 200 Nm.
/// </summary>
public class ClampConnectionTests
{
    private static Material C45() => new()
    {
        Name = "C45",
        Standard = "EN 10083",
        YieldStrength = 430,
        TensileStrength = 650,
        ElasticModulus = 206,
        PoissonRatio = 0.3,
        Density = 7850
    };

    private static Material GreyIron() => new()
    {
        Name = "GG25 (EN-GJL-250)",
        Standard = "EN 1561",
        YieldStrength = 165,
        TensileStrength = 250,
        ElasticModulus = 105,
        PoissonRatio = 0.26,
        Density = 7200
    };

    private static ClampConnectionEngine Default() => new()
    {
        ShaftDiameter = 40,
        ClampOuterDiameter = 70,
        ClampLength = 50,
        SlitWidth = 4,
        NumberOfBolts = 2,
        BoltDiameter = 8,
        BoltTighteningTorque = 25,
        BoltYieldStrength = 640,          // class 8.8
        ThreadPitch = 1.25,
        AppliedTorque = 200,
        AppliedAxialForce = 0,
        LoadFactor = 1.0,
        FrictionCoefficientInterface = 0.15,
        FrictionCoefficientThread = 0.12,
        FrictionCoefficientHead = 0.12,
        ShaftMaterial = C45(),
        HubMaterial = C45()
    };

    /// <summary>
    /// A bolt carries TORSION as well as tension while it is tightened, and the
    /// torsion is still there once the wrench comes off. Checking the tensile
    /// stress alone overstated the reserve: 78 % of yield against the true 88 %,
    /// so a user who thought 22 % was left had 12 %.
    ///
    /// Independent anchor, VDI 2230 for an M8 at µ_G = 0,12:
    ///   d2 = 7,1881  d3 = 6,4665  dS = 6,8273
    ///   k = 1,5 (d2/dS)(P/(pi d2) + 1,155 µ_G) = 0,30632
    ///   sqrt(1 + 3 k^2) = 1,1320
    /// </summary>
    [Fact]
    public void BoltUtilizationIncludesTheTighteningTorsion()
    {
        var engine = Default();
        engine.Calculate();

        Assert.Equal(1.1320, engine.BoltTorsionFactor, 4);
        Assert.Equal(499.0, engine.BoltStress, 0);
        Assert.Equal(565.0, engine.BoltEquivalentStress, 0);
        Assert.Equal(88.3, engine.BoltUtilization, 1);
        Assert.Equal(640.0 / engine.BoltEquivalentStress, engine.SafetyFactorBolt, 9);

        // Tension alone would have read 78 %.
        Assert.Equal(78.0, engine.BoltStress / 640.0 * 100, 0);
    }

    /// <summary>
    /// F_M,zul is 90 % of yield on the EQUIVALENT stress. For an M8 class 8.8 at
    /// µ_G = 0,12 that is 18,6 kN, which is the figure VDI 2230's own tables
    /// carry - an independent check on the whole torsion factor.
    /// </summary>
    [Fact]
    public void AllowablePreloadMatchesTheVdiTable()
    {
        var engine = Default();
        engine.Calculate();

        Assert.Equal(18623, engine.AllowablePreloadForce, 0);
        Assert.Equal(0.9 * 640 * engine.BoltStressArea / engine.BoltTorsionFactor,
                     engine.AllowablePreloadForce, 6);

        // The example's 25 Nm sits just inside it.
        Assert.False(engine.PreloadExceedsAllowable);
        Assert.True(engine.AllowableTighteningTorque > 25.0);
        Assert.True(engine.AllowableTighteningTorque < 26.0);
    }

    /// <summary>
    /// A preload that would yield the bolt cannot be used for the capacities. At
    /// 60 Nm the page reported 43,8 kN of preload, 187 % of yield, and then a
    /// sliding safety of 4,00 marked OK on the strength of it.
    /// </summary>
    [Fact]
    public void AnImpossiblePreloadIsCappedNotBelieved()
    {
        var engine = Default();
        engine.BoltTighteningTorque = 60;
        engine.Calculate();

        Assert.True(engine.PreloadExceedsAllowable);
        Assert.True(engine.BoltUtilization > 180);
        Assert.Equal(engine.AllowablePreloadForce, engine.EffectivePreloadForce, 6);

        // Capacity must match the capped preload, not the one the torque implies.
        var capped = Default();
        capped.BoltTighteningTorque = engine.AllowableTighteningTorque;
        capped.Calculate();
        Assert.Equal(capped.TorqueCapacity, engine.TorqueCapacity, 3);

        // And it must be the bolt that governs, not sliding.
        Assert.Equal("bolt", engine.GoverningCheck);
    }

    /// <summary>
    /// The hub stress check runs on the PEAK pressure. The module's own Kp factor
    /// says the pressure is not uniform and the results text tells the reader to
    /// compare the maximum, so feeding the mean into the stress check contradicted
    /// both: 56,2 MPa instead of 88,3, and a safety factor of 7,65 instead of 4,87.
    /// </summary>
    [Fact]
    public void HubStressUsesThePeakPressureNotTheMean()
    {
        var engine = Default();
        engine.Calculate();

        Assert.Equal(-engine.MaxContactPressure, engine.HubRadialStress, 9);

        double qa = 40.0 / 70.0;
        Assert.Equal(engine.MaxContactPressure * (1 + qa * qa) / (1 - qa * qa),
                     engine.HubTangentialStress, 6);

        Assert.Equal(88.3, engine.HubVonMisesStress, 1);
        Assert.Equal(4.87, engine.SafetyFactorHubStress, 2);

        // Kp = pi/2 on a slotted clamp.
        Assert.Equal(Math.PI / 2.0, engine.MaxContactPressure / engine.ContactPressure, 9);
    }

    /// <summary>
    /// The design advice has to follow the governing check. On the default joint
    /// the bolt governs, and the page used to tell the user to tighten harder -
    /// which makes the only marginal check worse.
    /// </summary>
    [Fact]
    public void TheGoverningCheckIsIdentified()
    {
        var engine = Default();
        engine.Calculate();
        Assert.Equal("bolt", engine.GoverningCheck);
        Assert.Equal(engine.SafetyFactorBolt, engine.SafetyFactorMin, 9);

        // Load it far harder and sliding takes over.
        var overloaded = Default();
        overloaded.AppliedTorque = 2000;
        overloaded.Calculate();
        Assert.Equal("sliding", overloaded.GoverningCheck);
        Assert.Equal(overloaded.SafetyFactorSliding, overloaded.SafetyFactorMin, 9);
    }

    /// <summary>
    /// Grey cast iron (EN-GJL / GG) has no yield point. Nodular iron does, and
    /// must not be flagged. The match is case-insensitive via ToLowerInvariant,
    /// because under a Turkish culture "GJL" lowercases its I to a dotless i.
    /// </summary>
    [Fact]
    public void GreyCastIronIsFlaggedAndNodularIronIsNot()
    {
        var grey = Default();
        grey.HubMaterial = GreyIron();
        grey.Calculate();
        Assert.True(grey.BrittleHubMaterial);
        Assert.False(grey.BrittleShaftMaterial);

        var nodular = Default();
        nodular.HubMaterial = new Material
        {
            Name = "GGG40 (EN-GJS-400)", Standard = "EN 1563",
            YieldStrength = 250, TensileStrength = 400,
            ElasticModulus = 169, PoissonRatio = 0.275, Density = 7100
        };
        nodular.Calculate();
        Assert.False(nodular.BrittleHubMaterial);

        var steel = Default();
        steel.Calculate();
        Assert.False(steel.BrittleHubMaterial);
        Assert.False(steel.BrittleShaftMaterial);
    }

    /// <summary>
    /// The allowable bearing pressure comes from the WEAKER of the two parts and
    /// names it, so a soft shaft against a steel hub cannot change the answer
    /// invisibly.
    /// </summary>
    [Fact]
    public void TheWeakerPartSetsTheAllowableBearingPressure()
    {
        var engine = Default();
        engine.ShaftMaterial = GreyIron();      // Re 165 against the hub's 430
        engine.Calculate();

        Assert.Equal(0.8 * 165, engine.AllowableContactPressure, 6);
        Assert.Contains("shaft", engine.BearingMaterialLabel);
        Assert.Contains("GG25", engine.BearingMaterialLabel);
    }

    /// <summary>
    /// Doubling the number of bolts doubles the preload crossing the joint plane
    /// and so the capacity, while the stress in each bolt is unchanged. That
    /// direction is what makes "use more bolts" the right advice for a
    /// bolt-limited joint.
    /// </summary>
    [Fact]
    public void MoreBoltsRaiseCapacityWithoutLoadingEachBoltHarder()
    {
        var two = Default();
        two.Calculate();

        var four = Default();
        four.NumberOfBolts = 4;
        four.Calculate();

        Assert.Equal(2.0 * two.TorqueCapacity, four.TorqueCapacity, 6);
        Assert.Equal(two.BoltEquivalentStress, four.BoltEquivalentStress, 9);
        Assert.Equal(two.SafetyFactorBolt, four.SafetyFactorBolt, 9);
    }
}
