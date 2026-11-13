using System;
using System.Linq;
using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the Nov 2026 review of the two gear modules. The default pair is
/// the external page's own: m_n 1,5, z 20/40, a 45, x 0/0, b 15, ISO 53 profile B
/// (rho* 0,30), 18CrNiMo7-6, 3,5 kW at 2500 rpm.
/// </summary>
public class GearReviewTests
{
    private static GearPairEngine Default() => new()
    {
        Power = 3.5,
        Speed1 = 2500,
        RequiredServiceLife = 20000,
        NormalModule = 1.5,
        HelixAngle = 0,
        CenterDistance = 45,
        NumberOfTeeth1 = 20,
        FaceWidth1 = 15,
        ProfileShiftCoeff1 = 0,
        NumberOfTeeth2 = 40,
        FaceWidth2 = 15,
        ProfileShiftCoeff2 = 0
    };

    // ---------------- Y_B: the rim thickness factor ----------------

    /// <summary>
    /// ISO 6336-3 Clause 8 grades an EXTERNAL gear's rim on s_R/h, the rim
    /// thickness over the TOOTH HEIGHT. It was implemented as s_R/m_n, which is
    /// the INTERNAL gear's criterion and carries its own thresholds of 1,75 and
    /// 3,5 - the two are not interchangeable.
    ///
    /// On the default pinion with a 22 mm bore: d_f1 = 26,25, so s_R = 2,125 and
    /// h = (33,0 - 26,25)/2 = 3,375.
    ///   s_R/h   = 0,630 -> Y_B = 1,6 ln(2,242/0,630) = 2,031
    ///   s_R/m_n = 1,417 -> Y_B = 1,000      (what was reported)
    /// Halving the root stress on a thin-rimmed pinion is exactly the case this
    /// factor exists for.
    /// </summary>
    [Fact]
    public void ExternalRimFactorUsesToothHeightNotModule()
    {
        var engine = Default();
        engine.BoreDiameter1 = 22;
        engine.Calculate();

        double sR = (engine.RootDiameter1 - 22) / 2.0;
        double h = (engine.TipDiameter1 - engine.RootDiameter1) / 2.0;

        Assert.Equal(2.125, sR, 3);
        Assert.Equal(3.375, h, 3);
        Assert.Equal(1.6 * Math.Log(2.242 / (sR / h)), engine.RimThicknessFactor1, 6);
        Assert.Equal(2.031, engine.RimThicknessFactor1, 0.01);

        // The s_R/m_n criterion would have returned 1,000 here.
        Assert.True(sR / engine.NormalModule >= 1.2);
        Assert.True(engine.RimThicknessFactor1 > 1.9,
            "a rim this thin must amplify the root stress, not be ignored");
    }

    /// <summary>A solid blank, and a thick rim, both leave Y_B at 1.</summary>
    [Fact]
    public void AThickRimOrSolidBlankLeavesTheFactorAtOne()
    {
        var solid = Default();
        solid.Calculate();
        Assert.Equal(1.0, solid.RimThicknessFactor1, 9);

        var thick = Default();
        thick.BoreDiameter1 = 12;          // s_R/h = 3,1
        thick.Calculate();
        Assert.Equal(1.0, thick.RimThicknessFactor1, 9);
    }

    /// <summary>A thinner rim must always amplify more. Direction, not a point.</summary>
    [Fact]
    public void AThinnerRimAlwaysAmplifiesMore()
    {
        double previous = 0;
        foreach (double bore in new[] { 12.0, 18.0, 20.0, 22.0, 24.0 })
        {
            var engine = Default();
            engine.BoreDiameter1 = bore;
            engine.Calculate();
            Assert.True(engine.RimThicknessFactor1 >= previous,
                $"Y_B must not fall as the bore grows (at d_i = {bore})");
            previous = engine.RimThicknessFactor1;
        }
        Assert.True(previous > 1.0);
    }

    // ---------------- Interference on an EXTERNAL pair ----------------

    /// <summary>
    /// The external module had no interference checks at all - they grew on the
    /// internal page during its own review and were never brought across. A pair
    /// whose mesh begins below the pinion's base circle reported a contact ratio
    /// of 1,567 "OK", specific sliding clamped to 0,000 "OK", and a final verdict
    /// of "Tooth root and flank meet ISO 6336".
    ///
    /// z1 = 12 unshifted against z2 = 40 at a = 39: rho_A1 comes out negative.
    /// </summary>
    [Fact]
    public void InvoluteInterferenceOnAnExternalPairIsReported()
    {
        var engine = Default();
        engine.NumberOfTeeth1 = 12;
        engine.CenterDistance = 39;
        engine.Power = 0.3;
        engine.Calculate();

        Assert.True(engine.PinionRootContactRadius < 0,
            "contact begins inside the pinion base circle");
        Assert.NotEmpty(engine.InternalGearWarnings);
        Assert.Contains("Involute interference", string.Join(" ", engine.InternalGearWarnings));
    }

    /// <summary>A sound pair reports nothing, so the check is not just always on.</summary>
    [Fact]
    public void ASoundExternalPairHasNoInterferenceWarnings()
    {
        var engine = Default();
        engine.Calculate();

        Assert.True(engine.PinionRootContactRadius > 0);
        Assert.True(engine.PinionActiveRootDiameter >= engine.PinionFormDiameter - 1e-6);
        Assert.Empty(engine.InternalGearWarnings);
    }

    /// <summary>
    /// A centre distance far above the reference drives x2 up until the teeth
    /// collide. c = -1,2 mm was accepted with the contact ratio and the flank
    /// safety both marked OK.
    /// </summary>
    [Fact]
    public void NegativeTipClearanceIsReported()
    {
        var engine = Default();
        engine.CenterDistance = 50;
        engine.Calculate();

        Assert.True(engine.TipClearance < 0);
        Assert.Contains("tip clearance", string.Join(" ", engine.InternalGearWarnings).ToLowerInvariant());
    }

    // ---------------- Undercut limit ----------------

    /// <summary>
    /// x_min uses the depth of the tool's STRAIGHT flank, h_FfP0 = h_fP -
    /// rho_fP(1 - sin alpha_n), not its whole addendum: the tip rounding
    /// generates a trochoid rather than cutting the involute.
    ///
    /// For ISO 53 profile B (h*_fP = 1,25, rho* = 0,30) at alpha_n = 20 and
    /// z = 20: h_FfP0/m = 1,25 - 0,30(1 - 0,342) = 1,0526, so
    ///   x_min = 1,0526 - 20 sin^2(20)/2 = -0,1172
    /// against the -0,1698 that h*_aP = 1 gave. A gear at x = -0,15 is undercut
    /// and drew no warning.
    ///
    /// For profile A (rho* = 0,38) the two happen to coincide, which is why this
    /// was invisible on the internal module.
    /// </summary>
    [Fact]
    public void UndercutLimitAccountsForTheToolTipRadius()
    {
        var engine = Default();          // profile B: h*fP 1,25, rho* 0,30
        engine.Calculate();

        double hFfP0 = engine.DedendumCoeff
                     - engine.RootRadiusCoeff * (1 - Math.Sin(20 * Math.PI / 180));
        double expected = hFfP0 - 20 * Math.Pow(Math.Sin(20 * Math.PI / 180), 2) / 2.0;

        Assert.Equal(expected, engine.MinProfileShift1, 6);
        Assert.Equal(-0.1172, engine.MinProfileShift1, 0.0005);

        // The old form, h*_aP = 1, was more permissive - the unsafe direction.
        double old = 1.0 - 20 * Math.Pow(Math.Sin(20 * Math.PI / 180), 2) / 2.0;
        Assert.True(engine.MinProfileShift1 > old,
            "the real limit is less permissive than the one that ignored the tool tip radius");

        // x = -0,15 now falls below the limit, as it should.
        Assert.True(-0.15 < engine.MinProfileShift1);
    }

    /// <summary>
    /// Profile A is the case where the two forms coincide, so the fix must not
    /// move it.
    /// </summary>
    [Fact]
    public void ProfileAIsUnchangedByTheUndercutFix()
    {
        var engine = Default();
        engine.DedendumCoeff = 1.25;
        engine.RootRadiusCoeff = 0.38;
        engine.Calculate();

        double old = 1.0 - 20 * Math.Pow(Math.Sin(20 * Math.PI / 180), 2) / 2.0;
        Assert.Equal(old, engine.MinProfileShift1, 0.002);
    }

    // ---------------- Scuffing temperature ----------------

    /// <summary>
    /// X_L is two different quantities with one symbol. The FRICTION equation
    /// Eq. (27) wants the base oil coefficient times eta^-0,05; the SCUFFING
    /// TEMPERATURE Eq. (99) wants the base oil factor alone. Feeding the
    /// friction value into Eq. (99) put theta_S 14 % low - 348 C instead of
    /// 404 C for a mineral oil at FZG stage 12 - and disagreed with the integral
    /// method next door, which was already using 1,0.
    ///
    ///   theta_S = 80 + (0,85 + 1,4 X_W) X_L S_FZG^2
    ///           = 80 + (0,85 + 1,4) x 1,0 x 144 = 404
    /// </summary>
    [Fact]
    public void ScuffingTemperatureUsesTheBaseOilFactor()
    {
        Assert.Equal(1.0, Iso13989FlashTemperature.ScuffingTemperatureLubricantFactor(
            LubricantType.Mineral), 9);

        // The friction factor still carries the viscosity term and is below 1
        // at any realistic viscosity.
        double friction = Iso13989FlashTemperature.LubricantFactor(LubricantType.Mineral, 46);
        Assert.Equal(Math.Pow(46, -0.05), friction, 9);
        Assert.True(friction < 1.0);

        var engine = Default();
        engine.Calculate();
        Assert.Equal(404, engine.Scuffing!.ScuffingTemperature, 1.0);
    }

    /// <summary>
    /// A synthetic base oil keeps its type coefficient in Eq. (99); only the
    /// viscosity term is gone.
    /// </summary>
    [Fact]
    public void TheBaseOilTypeStillReachesTheScuffingTemperature()
    {
        Assert.Equal(0.8, Iso13989FlashTemperature.ScuffingTemperatureLubricantFactor(
            LubricantType.Polyalphaolefin), 9);
        Assert.Equal(1.5, Iso13989FlashTemperature.ScuffingTemperatureLubricantFactor(
            LubricantType.TractionFluid), 9);
    }

    // ---------------- Forces at the working circle ----------------

    /// <summary>
    /// ISO 6336 uses the reference-circle tangential force, and every load factor
    /// above depends on it - correctly. But the bearings see the mesh at the
    /// WORKING pitch circle and the working pressure angle, and on a shifted pair
    /// the radial force there is about 10 % higher. Only the reference set was
    /// shown, which is the unsafe direction for a shaft calculation.
    /// </summary>
    [Fact]
    public void BothForceSetsAreReportedAndTheyDifferOnAShiftedPair()
    {
        var concentric = Default();
        concentric.Calculate();
        // Unshifted at the reference centre distance the two coincide.
        Assert.Equal(concentric.RadialForce, concentric.WorkingRadialForce, 1e-6);

        var shifted = Default();
        shifted.ProfileShiftCoeff1 = 0.3;
        shifted.CenterDistance = 45.75;
        shifted.Calculate();

        Assert.True(shifted.WorkingRadialForce > shifted.RadialForce * 1.05,
            "the working radial force must be the larger on a positively shifted pair");
        Assert.Equal(shifted.WorkingTangentialForce * Math.Tan(shifted.WorkingPressureAngle * Math.PI / 180),
                     shifted.WorkingRadialForce, 6);
    }

    // ---------------- Running-in allowance consistency ----------------

    /// <summary>
    /// Footnote 12 makes the governing deviation max(f_pb, f_fa), and the
    /// running-in allowance has to come from the same one. y_alpha was computed
    /// from f_pb alone while q_alpha used the maximum, so the two could not be
    /// reconciled by hand.
    /// </summary>
    [Fact]
    public void TheRunningInAllowanceUsesTheGoverningDeviation()
    {
        var engine = Default();
        engine.Calculate();

        double governing = Math.Max(engine.BasePitchDeviation, engine.ProfileFormDeviation);
        var group = Iso6336TransverseFactor.MapMaterialGroup(engine.Material1.Iso6336Type);
        double expected = Iso6336TransverseFactor.RunningInAllowance(
            group, governing,
            Math.Min(engine.Material1.ContactFatigueLimit, engine.Material2.ContactFatigueLimit),
            engine.PitchLineVelocity);

        Assert.Equal(expected, engine.TransverseResult!.yAlpha, 6);
    }
}
