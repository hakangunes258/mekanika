using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the internal gear pair (external pinion in an annulus).
///
/// No reference report for an internal pair has been run yet, so these do not compare against
/// commercial software. They pin down what can be checked without one: closed-form geometry
/// worked by hand, the ISO 6336-3 internal tooth form against an independent vector
/// derivation, the tip interference condition against a brute-force mesh simulation, the
/// ball measurement against the involute it is supposed to touch, and the directions every
/// internal-pair result must move in relative to the equivalent external pair.
/// </summary>
public class InternalGearTests
{
    /// <summary>
    /// z 20/60, m_n = 2, ISO 53 profile A (ρ*_fP = 0.38): at a = 40.8 against a reference 40 and
    /// x1 = +0.3 (x2 ≈ −0.728) the pair is clear of every interference and its annulus root stays
    /// inside the q_s &lt; 8 range of ISO 6336-3 Eq. (36). The external pair is the same teeth at
    /// its own reference centre distance.
    /// </summary>
    private static GearPairEngine Pair(bool internalGear, double x1 = 0.3, double? a = null)
    {
        var e = new GearPairEngine
        {
            InternalGear = internalGear,
            Power = 10,
            Speed1 = 1000,
            RequiredServiceLife = 20000,
            NormalModule = 2,
            HelixAngle = 0,
            NumberOfTeeth1 = 20,
            NumberOfTeeth2 = 60,
            CenterDistance = a ?? (internalGear ? 40.8 : 80),
            ProfileShiftCoeff1 = x1,
            FaceWidth1 = 30,
            FaceWidth2 = 30,
            RootRadiusCoeff = 0.38,
            RingOuterDiameter = internalGear ? 150 : 0
        };
        e.Material1 = GearMaterial.ByLabel("18CrNiMo7-6 - Case-hardened")!;
        e.Material2 = GearMaterial.ByLabel("18CrNiMo7-6 - Case-hardened")!;
        e.Calculate();
        return e;
    }

    // ------------------------------------------------------------------ geometry

    [Fact]
    public void Geometry_matches_hand_calculation()
    {
        var e = Pair(true, x1: 0.3, a: 40);

        // a = a_d, so Σx = 0 and x2 = −x1.
        Assert.Equal(0.0, e.SumProfileShift, 9);
        Assert.Equal(-0.3, e.ProfileShiftCoeff2, 9);
        Assert.Equal(20.0, e.WorkingPressureAngle, 9);

        // Annulus: d_a2 = d2 − 2m(1 + x2), d_f2 = d2 + 2m(1,25 − x2).
        Assert.Equal(120.0, e.ReferenceDiameter2, 9);
        Assert.Equal(120.0 - 4.0 * 0.7, e.TipDiameter2, 9);
        Assert.Equal(120.0 + 4.0 * 1.55, e.RootDiameter2, 9);

        // d_w1 = 2a/(u − 1) = 40, the reference diameter when a = a_d.
        Assert.Equal(40.0, e.WorkingPitchDiameter1, 9);
        Assert.Equal(120.0, e.WorkingPitchDiameter2, 9);

        // Path of contact g_α = √(ra1² − rb1²) − √(ra2² − rb2²) + a sin α_wt.
        double rb1 = 20 * Math.Cos(Math.PI / 9), rb2 = 60 * Math.Cos(Math.PI / 9);
        double ra1 = 22.6, ra2 = 58.6;
        double g = Math.Sqrt(ra1 * ra1 - rb1 * rb1) - Math.Sqrt(ra2 * ra2 - rb2 * rb2) + 40 * Math.Sin(Math.PI / 9);
        Assert.Equal(g, e.PathOfContact, 9);
        Assert.Equal(g / (Math.PI * 2 * Math.Cos(Math.PI / 9)), e.TransverseContactRatio, 9);

        // Clearance pinion tip → annulus root: d_f2/2 − a − d_a1/2 = 63,1 − 40 − 22,6.
        Assert.Equal(0.5, e.TipClearance, 9);

        // The annulus turns the same way as the pinion, at n1/u.
        Assert.Equal(1000.0 / 3.0, e.Speed2, 9);
    }

    [Fact]
    public void Larger_centre_distance_gives_negative_profile_shift_sum()
    {
        // On an internal pair the pinion moves TOWARDS the annulus teeth as a grows, which the
        // annulus has to make room for by shifting its profile outwards (x2 < 0).
        var e = new GearPairEngine
        {
            InternalGear = true, NormalModule = 2, NumberOfTeeth1 = 20, NumberOfTeeth2 = 60,
            CenterDistance = 41
        };
        e.CalculateProfileShift2();
        Assert.True(e.SumProfileShift < 0);
        Assert.Equal(40.0, e.ReferenceCentreDistance(), 9);
    }

    [Fact]
    public void Calculated_tip_alteration_restores_the_reference_clearance()
    {
        // The k = y − Σx identity holds in the signed convention too: c = m(h_fP − h_aP).
        var e = new GearPairEngine
        {
            InternalGear = true, Power = 10, Speed1 = 1000, RequiredServiceLife = 1000,
            NormalModule = 2, NumberOfTeeth1 = 20, NumberOfTeeth2 = 60, CenterDistance = 40.6,
            ProfileShiftCoeff1 = 0.3, FaceWidth1 = 30, FaceWidth2 = 30,
            TipAlterationMode = TipAlterationSource.Calculated
        };
        e.Calculate();
        Assert.Equal(0.5, e.TipClearance, 9);
    }

    // ------------------------------------------------------------------ tooth form

    [Fact]
    public void Internal_tooth_form_matches_independent_vector_derivation()
    {
        // Spur annulus z = 60, m_n = 2, x = 0, ISO 53-like rack h_fP = 2,5, ρ_fP = 0,76, ε_α = 1,75.
        // Reference values from an independent derivation: trochoid of the rack's tip-fillet
        // centre, offset by ρ_fP, tangent at 60° to the tooth axis, load on the involute normal
        // at the outer point of single pair contact (scratch prototype, agrees to 1e-12).
        var r = Iso6336ToothForm.Calculate(
            z: -60, mn: 2, alphaN: 20, beta: 0, x: 0,
            da: -116, d: -120, epsilonAlpha: 1.75, hfP: 2.5, rhofP: 0.76);

        Assert.Null(r.Warning);
        Assert.Equal(5.507293841678, r.sFn, 9);
        Assert.Equal(2.709300367657, r.hFe, 9);
        Assert.Equal(0.667549058356, r.rhoF, 9);
        Assert.Equal(1.078742437021, r.YF, 9);
    }

    [Fact]
    public void Internal_tooth_form_rejects_inconsistent_signs()
    {
        // An internal gear has its tip inside the reference circle: |d_a| < |d|.
        var r = Iso6336ToothForm.Calculate(
            z: -60, mn: 2, alphaN: 20, beta: 0, x: 0,
            da: -124, d: -120, epsilonAlpha: 1.75, hfP: 2.5, rhofP: 0.76);
        Assert.Equal(0, r.YF);
        Assert.NotNull(r.Warning);
    }

    [Fact]
    public void Annulus_tooth_has_lower_form_factor_than_external_wheel()
    {
        // The annulus tooth widens towards its root, so it bends less than the convex tooth of
        // an external wheel of the same tooth count.
        var ext = Pair(false, x1: 0);
        var inn = Pair(true, x1: 0.3);
        Assert.True(inn.ToothFormFactor2 < ext.ToothFormFactor2);
        Assert.True(inn.ToothFormValid2);
    }

    // ------------------------------------------------------------------ interference

    [Theory]
    // z1 = 40, m = 1, x = 0, full depth: a brute-force mesh simulation (pinion tooth outline
    // swept through the annulus frame, penetration of the annulus tooth measured) finds
    // interference up to z2 = 48 and none from 49 on.
    [InlineData(47, true)]
    [InlineData(48, true)]
    [InlineData(49, false)]
    [InlineData(52, false)]
    public void Tip_interference_matches_brute_force_threshold(int z2, bool expectInterference)
    {
        var e = new GearPairEngine
        {
            InternalGear = true, Power = 1, Speed1 = 1000, RequiredServiceLife = 1000,
            NormalModule = 1, NumberOfTeeth1 = 40, NumberOfTeeth2 = z2,
            CenterDistance = (z2 - 40) / 2.0, FaceWidth1 = 10, FaceWidth2 = 10
        };
        e.Calculate();
        Assert.Equal(expectInterference, e.TipInterferenceMargin < 0);
        Assert.Equal(expectInterference, e.InternalGearWarnings.Any(w => w.StartsWith("Tip interference")));
    }

    [Fact]
    public void Standard_teeth_on_a_small_pinion_show_involute_interference()
    {
        // z1 = 20 in a z2 = 60 annulus with unshifted full-depth teeth: the annulus tip reaches
        // past the pinion's base circle, the textbook reason annulus tips are enlarged.
        var plain = Pair(true, x1: 0, a: 40);
        Assert.True(plain.PinionRootContactRadius <= 0);
        Assert.Contains(plain.InternalGearWarnings, w => w.StartsWith("Involute interference"));
    }

    [Fact]
    public void Annulus_tip_below_the_pinion_form_circle_is_root_interference()
    {
        // x1 = +0.3 / x2 = −0.3 at the reference centre distance: the annulus tip stays outside
        // the pinion's base circle (ρ_1A > 0) but engages it BELOW its root form circle, i.e. on
        // the fillet. The first version of the checks compared with the base circle only, passed
        // this pair and shipped it as the page default.
        var fillet = Pair(true, x1: 0.3, a: 40);
        Assert.True(fillet.PinionRootContactRadius > 0);
        Assert.True(fillet.PinionActiveRootDiameter < fillet.PinionFormDiameter);
        Assert.Contains(fillet.InternalGearWarnings, w => w.StartsWith("Root interference on the pinion"));

        var clear = Pair(true);
        Assert.True(clear.PinionActiveRootDiameter > clear.PinionFormDiameter);
        Assert.True(clear.RingActiveRootDiameter < clear.RingFormDiameter);
        Assert.Empty(clear.InternalGearWarnings);
        Assert.Null(clear.ToothFormWarning2);   // q_s inside Eq. (36)
    }

    [Fact]
    public void Negative_annulus_shift_shrinks_the_rack_model_fillet()
    {
        // The tension any internal design here runs into: the negative x2 that keeps the annulus
        // tip off the pinion fillet also tightens the annulus root fillet in the rack-generated
        // model of ISO 6336-3 - with profile B (ρ*_fP = 0.30) far enough to leave Eq. (36).
        // Pinned so that a change to the internal tooth form, or to Eq. (11) once it is
        // implemented, shows up here rather than silently.
        var profileA = Pair(true);
        var profileB = Pair(true);
        profileB.RootRadiusCoeff = 0.30;
        profileB.Calculate();

        Assert.True(profileB.RootFilletRadius2 < profileA.RootFilletRadius2);
        Assert.True(profileA.NotchParameter2 < 8);
        Assert.True(profileB.NotchParameter2 >= 8);
        Assert.NotNull(profileB.ToothFormWarning2);
    }

    [Fact]
    public void Pinion_form_circle_matches_a_simulated_rack_generation()
    {
        // ISO 21771 d_Ff against a brute-force generation (rack with the ρ_fP tip fillet rolled
        // over the pinion, generated flank compared with the involute): z = 20, m_n = 1.5,
        // x = 0, h*_fP = 1.25, ρ*_fP = 0.3 → the involute starts at d ≈ 28.211 mm.
        var e = new GearPairEngine
        {
            InternalGear = true, Power = 1, Speed1 = 1000, RequiredServiceLife = 1000,
            NormalModule = 1.5, NumberOfTeeth1 = 20, NumberOfTeeth2 = 60, CenterDistance = 30.6,
            ProfileShiftCoeff1 = 0, FaceWidth1 = 15, FaceWidth2 = 15, RootRadiusCoeff = 0.3
        };
        e.Calculate();
        Assert.Equal(28.211, e.PinionFormDiameter, 2);
    }

    // ------------------------------------------------------------------ strength

    [Fact]
    public void Internal_pair_has_lower_nominal_contact_stress()
    {
        var ext = Pair(false, x1: 0);
        var inn = Pair(true, x1: 0, a: 40);   // same α_wt (20°) as the external pair

        // σ_H0 ∝ Z_ε √((u ± 1)/u): the (u − 1) of the conforming contact, times the Z_ε of the
        // longer path of contact. Everything else is identical for the two.
        double expected = ext.NominalContactStress
                        * Math.Sqrt(2.0 / 4.0)
                        * inn.ContactRatioFactorFlank / ext.ContactRatioFactorFlank;
        Assert.Equal(expected, inn.NominalContactStress, 6);
    }

    [Fact]
    public void Internal_rim_factor_branches_meet_at_three_and_a_half_modules()
    {
        // ISO 6336-3 Clause 8, internal gears: 1,15 ln(8,324 m_n / s_R), unity from s_R = 3,5 m_n.
        double YbAt(double outside)
        {
            var e = Pair(true);
            e.RingOuterDiameter = outside;
            e.Calculate();
            return e.RimThicknessFactor2;
        }

        double df2 = Pair(true).RootDiameter2;
        Assert.Equal(1.0, YbAt(df2 + 2 * 3.5 * 2 + 0.01), 9);
        Assert.Equal(1.15 * Math.Log(8.324 / 3.5), YbAt(df2 + 2 * 3.5 * 2 - 1e-9), 6);
        Assert.Equal(1.15 * Math.Log(8.324 / 2.5), YbAt(df2 + 2 * 2.5 * 2), 9);
        Assert.Equal(1.0, YbAt(0), 9);   // no rim diameter: not checked
    }

    [Fact]
    public void Dynamic_factor_runs_for_an_internal_pair()
    {
        var e = Pair(true);
        Assert.NotNull(e.DynamicResult);
        Assert.True(e.DynamicResult!.Valid);
        // The annulus counts as infinitely heavy, so the reduced mass is the pinion's own,
        // which is larger than any reduced mass of the pinion with a finite partner.
        Assert.True(e.DynamicResult.mred > Pair(false).DynamicResult!.mred);
    }

    // ------------------------------------------------------------------ measurement

    [Fact]
    public void Ball_touches_the_annulus_flank_at_its_own_radius()
    {
        var e = Pair(true);
        var m = e.Measurement2!;
        Assert.True(m.Md > 0);
        Assert.Null(m.BallWarning);

        // Brute force: sample the annulus tooth flank and find its distance from the ball
        // centre, which sits on the space centreline (half a pitch from the tooth axis).
        double alpha = Math.PI / 9, z = 60, x = e.ProfileShiftCoeff2, rb = e.BaseDiameter2 / 2;
        double inv(double a) => Math.Tan(a) - a;
        double halfAngle(double r) => (Math.PI / 2 + 2 * x * Math.Tan(alpha)) / z - inv(alpha) + inv(Math.Acos(rb / r));

        double rc = m.dMBallCentre / 2, cx = rc * Math.Sin(Math.PI / z), cy = rc * Math.Cos(Math.PI / z);
        double best = double.MaxValue;
        for (int k = 0; k <= 200000; k++)
        {
            double r = e.TipDiameter2 / 2 + (e.RootDiameter2 / 2 - e.TipDiameter2 / 2) * k / 200000.0;
            double t = halfAngle(r);
            best = Math.Min(best, Math.Sqrt(Math.Pow(r * Math.Sin(t) - cx, 2) + Math.Pow(r * Math.Cos(t) - cy, 2)));
        }
        Assert.Equal(m.DM / 2, best, 5);

        // Even tooth count: M_i = d_M − D_M.
        Assert.Equal(m.dMBallCentre - m.DM, m.Md, 9);
    }

    [Fact]
    public void Thinner_annulus_tooth_increases_dimension_between_balls()
    {
        var m = Pair(true).Measurement2!;
        Assert.True(m.Asne < 0 && m.Asni < m.Asne);
        Assert.True(m.MdUpper > m.Md);
        Assert.True(m.MdLower > m.MdUpper);
        Assert.Equal(0, m.Wk);   // no span measurement on an annulus
    }

    [Fact]
    public void Ball_limit_inversion_round_trips_on_an_annulus()
    {
        var e = Pair(true);
        var m = e.Measurement2!;
        var g = new GearToothMeasurement.GearInput
        {
            z = 60, mn = 2, alphaN = 20, beta = 0, x = e.ProfileShiftCoeff2,
            d = e.ReferenceDiameter2, db = e.BaseDiameter2, da = e.TipDiameter2, df = e.RootDiameter2,
            b = 30, Internal = true, BallDiameter = m.DM
        };
        double? asn = GearToothMeasurement.AllowanceForBallDimension(g, m.MdLower);
        Assert.NotNull(asn);
        Assert.Equal(m.Asni, asn!.Value, 6);
    }

    [Fact]
    public void Ball_limits_on_an_annulus_map_largest_to_most_thinning()
    {
        // The drawing states largest / smallest permitted M_i. On an annulus the largest is the
        // thinnest tooth (A_sni) - the reverse of an external gear - so the inversion has to swap.
        var reference = Pair(true);
        var m2 = reference.Measurement2!;

        var e = Pair(true);
        e.AllowanceMode = ToothThicknessAllowanceMode.BallLimits;
        e.BallDiameter2 = m2.DM;
        e.BallDiameter1 = reference.Measurement1!.DM;
        e.BallLimitUpper1 = reference.Measurement1.MdUpper;
        e.BallLimitLower1 = reference.Measurement1.MdLower;
        e.BallLimitUpper2 = Math.Max(m2.MdUpper, m2.MdLower);   // largest permitted M_i
        e.BallLimitLower2 = Math.Min(m2.MdUpper, m2.MdLower);
        e.Calculate();

        Assert.Equal(m2.Asne, e.Asne2, 6);
        Assert.Equal(m2.Asni, e.Asni2, 6);
        Assert.DoesNotContain("Could not be resolved", e.AllowanceNote);
    }

    [Fact]
    public void Larger_centre_distance_reduces_internal_backlash()
    {
        var e = Pair(true);
        e.AllowanceMode = ToothThicknessAllowanceMode.Manual;
        e.Asne1 = e.Asne2 = -0.05; e.Asni1 = e.Asni2 = -0.08;
        e.CentreDistanceUpperDev = 0.02; e.CentreDistanceLowerDev = 0.0001;
        e.Calculate();
        double jMinWide = e.Backlash!.jbnMin;

        e.CentreDistanceUpperDev = 0.0002; e.CentreDistanceLowerDev = 0.0001;
        e.Calculate();
        Assert.True(jMinWide < e.Backlash!.jbnMin);
    }

    // ------------------------------------------------------------------ scuffing, micropitting

    [Fact]
    public void Flash_temperature_internal_geometry_factor_agrees_with_general_equation()
    {
        // X_G for an internal pair was derived (A.6 in magnitudes); the general Eq. (5), which
        // works in ρ_1 and ρ_2 with ρ_2 negative, must give the same flash temperature to the
        // same few percent it does for an external pair.
        var inn = Pair(true).Scuffing!;
        var ext = Pair(false, 0).Scuffing!;
        Assert.True(inn.Valid);
        double relInt = Math.Abs(inn.FlashMax - inn.FlashMaxAlternate) / inn.FlashMax;
        double relExt = Math.Abs(ext.FlashMax - ext.FlashMaxAlternate) / ext.FlashMax;
        Assert.Equal(relExt, relInt, 2);
    }

    [Fact]
    public void Internal_pair_runs_cooler_than_external()
    {
        // Less sliding on the conforming internal mesh: lower flash temperature and lower
        // loss factor than the external pair of the same teeth.
        var inn = Pair(true);
        var ext = Pair(false, 0);
        Assert.True(inn.Scuffing!.FlashMax < ext.Scuffing!.FlashMax);
        Assert.True(inn.ToothLossFactor < ext.ToothLossFactor);
        Assert.True(inn.ScuffingIntegral!.Valid);
        Assert.True(inn.ScuffingIntegral.IntegralTemperature < ext.ScuffingIntegral!.IntegralTemperature);
    }

    [Fact]
    public void Micropitting_flash_is_zero_at_the_pitch_point_of_an_internal_pair()
    {
        var mp = Pair(true).Micropitting!;
        Assert.True(mp.Valid);
        var c = mp.Points.Single(p => p.Name == "C");
        Assert.Equal(0.0, c.Flash, 6);

        // ρ grows monotonically from A to E on both flanks of an internal pair, so the
        // relative radius does too.
        var ordered = mp.Points.OrderBy(p => p.gY).Select(p => p.RhoN).ToList();
        for (int k = 1; k < ordered.Count; k++) Assert.True(ordered[k] > ordered[k - 1]);
    }

    [Fact]
    public void External_pair_is_unaffected_by_the_internal_branches()
    {
        // Same numbers as before the internal branches existed (SpurPair in GearPairEngineTests
        // already pins the external chain); here only the new outputs are checked to stay idle.
        var e = Pair(false, 0);
        Assert.Empty(e.InternalGearWarnings);
        Assert.Equal(0, e.TipInterferenceMargin);
        Assert.False(double.IsNaN(e.MinProfileShift2));
    }
}
