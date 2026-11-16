using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// Anchors for the disc spring engine (DIN 2092 / DIN 2093).
///
/// No reference report for a disc spring has been run, so these are deliberately
/// not "golden values somebody once read off the page". Each one is either a closed
/// form written out independently, or an identity the equations must satisfy for
/// ANY input - which is the only kind of check that can catch a transcription error
/// without the standard to hand:
///
///   * K4 = 1 exactly when t' = t, for every l0/t. Breaks if either C1 or C2 is
///     mistyped.
///   * the tangent rate is the derivative of the force, and the energy its integral.
///   * the face-antisymmetric part of the stress is 1/delta smaller at the outer
///     edge than at the inner edge, which is the whole content of the Almen-Laszlo
///     "stress varies as -z/r" statement.
///   * the rate at flat is exactly zero when K4.h0/t' = sqrt(2).
/// </summary>
public class DiscSpringTests
{
    // ---------------- the shape factors ----------------

    /// <summary>
    /// DIN 2092 K1, K2, K3 at delta = 2, written out independently of the engine.
    /// The 1/pi, 6/pi and 3/pi are the part most easily dropped, and dropping one
    /// still produces a plausible-looking force - K1 off by pi scales every force
    /// by the same factor, which no eyeball catches.
    /// </summary>
    [Fact]
    public void ShapeFactorsFollowTheirClosedForms()
    {
        var s = SeriesA40();
        s.InnerDiameter = 20.0;   // delta = exactly 2
        s.OuterDiameter = 40.0;
        s.Calculate();

        double ln2 = Math.Log(2.0);
        double k1 = 1.0 / Math.PI * 0.25 / (3.0 - 2.0 / ln2);
        double k2 = 6.0 / Math.PI * (1.0 / ln2 - 1.0) / ln2;
        double k3 = 3.0 / Math.PI * 1.0 / ln2;

        Assert.Equal(2.0, s.DiameterRatio, 12);
        Assert.Equal(k1, s.K1, 12);
        Assert.Equal(k2, s.K2, 12);
        Assert.Equal(k3, s.K3, 12);

        // Magnitudes, so a sign or a reciprocal slip is caught too.
        Assert.InRange(s.K1, 0.68, 0.70);
        Assert.InRange(s.K2, 1.21, 1.23);
        Assert.InRange(s.K3, 1.37, 1.39);
    }

    /// <summary>
    /// A spring without contact surfaces has K4 = 1 and uses t' = t. That is what
    /// makes the plain spring a special case of the contact-surface equations rather
    /// than a second code path.
    /// </summary>
    [Fact]
    public void PlainSpringHasK4OfOne()
    {
        var s = SeriesA40();
        s.Calculate();

        Assert.False(s.HasContactSurfaces);
        Assert.Equal(1.0, s.K4, 12);
        Assert.Equal(s.Thickness, s.CalcThickness, 12);
        Assert.Equal(1.0, s.ThicknessRatio, 12);
        Assert.False(s.K4FellBack);
    }

    /// <summary>
    /// THE anchor for K4. Substituting t' = t into DIN 2092's C1 and C2 collapses
    /// them to 1/u and 1 + 1/u with u = 5/32 (l0/t - 1)^2, and then
    ///     K4^2 = -1/(2u) + sqrt(1/(2u) + 1)^2 = 1
    /// identically - for any l0/t. So driving t'/t towards 1 must drive K4 towards
    /// 1 at every shape, and a mistyped coefficient in either C breaks it.
    ///
    /// t'/t = 1 - 1e-6 rather than exactly 1, because at exactly 1 the engine short
    /// circuits and the formula under test never runs.
    /// </summary>
    [Theory]
    [InlineData(1.15)]
    [InlineData(1.30)]
    [InlineData(1.60)]
    [InlineData(2.20)]
    [InlineData(3.00)]
    public void K4CollapsesToOneAsTheThicknessReductionVanishes(double l0OverT)
    {
        var s = SeriesA40();
        s.Thickness = 5.0;
        s.FreeHeight = l0OverT * 5.0;
        s.HasContactSurfaces = true;
        s.ReducedThickness = 5.0 * (1.0 - 1e-6);
        s.Calculate();

        Assert.False(s.K4FellBack);
        Assert.Equal(1.0, s.K4, 4);
    }

    /// <summary>
    /// Direction: grinding contact surfaces shortens the lever arm, so the disc gets
    /// STIFFER and K4 rises above 1. DIN 2093 then takes thickness back out (t' &lt; t)
    /// so the catalogue force stays comparable - the two effects pull opposite ways,
    /// which is exactly why neither may be left out.
    /// </summary>
    [Fact]
    public void ReducedThicknessRaisesK4AboveOne()
    {
        var s = Group3_140();
        s.Calculate();

        Assert.True(s.K4 > 1.0, $"K4 = {s.K4:F4}; contact surfaces stiffen the disc");
        Assert.InRange(s.K4, 1.0, 1.3);
        Assert.Equal(0.94 * s.Thickness, s.CalcThickness, 12);
        Assert.Equal(3, s.ThicknessGroup);

        // h0 is measured against t', so it is LARGER than l0 - t would give.
        Assert.Equal(s.FreeHeight - s.CalcThickness, s.ConeHeight, 12);
        Assert.True(s.ConeHeight > s.FreeHeight - s.Thickness);
    }

    /// <summary>
    /// The direction that matters: calculating a Group 3 disc as if it had no contact
    /// surfaces UNDERSTATES its force, because the K4 and h0 gains outweigh the t'^4
    /// loss. The content file quotes both figures.
    /// </summary>
    [Fact]
    public void IgnoringContactSurfacesUnderstatesTheForce()
    {
        var withSurfaces = Group3_140();
        withSurfaces.Calculate();

        var asPlain = Group3_140();
        asPlain.HasContactSurfaces = false;
        asPlain.Calculate();

        Assert.Equal(100178, withSurfaces.Flat.Force, 0);
        Assert.Equal(86637, asPlain.Flat.Force, 0);
        Assert.True(asPlain.Flat.Force < withSurfaces.Flat.Force);
        Assert.InRange(asPlain.Flat.Force / withSurfaces.Flat.Force, 0.85, 0.87);
    }

    // ---------------- the characteristic ----------------

    /// <summary>
    /// The reported rate is dF/ds, not a secant. Checked against a central
    /// difference on the force at three places along the travel.
    /// </summary>
    [Theory]
    [InlineData(0.25)]
    [InlineData(0.50)]
    [InlineData(0.75)]
    public void RateIsTheDerivativeOfTheForce(double ratio)
    {
        var s = SeriesA40();
        s.Calculate();

        double at = ratio * s.ConeHeight;
        double h = s.ConeHeight * 1e-5;
        double numeric = (s.ForceAt(at + h) - s.ForceAt(at - h)) / (2 * h);

        Assert.Equal(numeric, s.RateAt(at), 4);
    }

    /// <summary>
    /// The energy expression is the exact integral of the force. Simpson over the
    /// travel to 0,75 h0 agrees to the last digit a cubic allows, because Simpson
    /// integrates a cubic exactly.
    /// </summary>
    [Fact]
    public void EnergyIsTheIntegralOfTheForce()
    {
        var s = SeriesA40();
        s.Calculate();

        double b = 0.75 * s.ConeHeight;
        double simpson = b / 6.0 * (s.ForceAt(0) + 4 * s.ForceAt(b / 2) + s.ForceAt(b));

        Assert.Equal(simpson, s.EnergyAt(b), 6);
    }

    /// <summary>
    /// At the flat position s = h0 the (h0/t - s/t) term is zero, so the bracket is
    /// exactly 1 and Fc reduces to the prefactor times the shape number. Written
    /// out here without the bracket at all.
    /// </summary>
    [Fact]
    public void ForceAtFlatDropsTheBracket()
    {
        var s = SeriesA40();
        s.Calculate();

        double pre = 4.0 * s.ElasticModulus / (1.0 - s.PoissonRatio * s.PoissonRatio)
                     * Math.Pow(s.CalcThickness, 4) / (s.K1 * s.OuterDiameter * s.OuterDiameter);

        Assert.Equal(pre * s.H0OverT, s.ForceAt(s.ConeHeight), 6);
        Assert.Equal(s.ForceAt(s.ConeHeight), s.Flat.Force, 9);
    }

    /// <summary>
    /// Zero deflection is zero force, zero rate contribution from the cubic term and
    /// zero stress - not a leftover from the previous position and not a sentinel.
    /// </summary>
    [Fact]
    public void ZeroDeflectionIsZeroForceAndZeroStress()
    {
        var s = SeriesA40();
        s.Calculate();

        Assert.Equal(0.0, s.ForceAt(0), 12);
        Assert.Equal(0.0, s.EnergyAt(0), 12);

        var (i, ii, iii, iv) = s.StressesAt(0);
        Assert.Equal(0.0, i, 12);
        Assert.Equal(0.0, ii, 12);
        Assert.Equal(0.0, iii, 12);
        Assert.Equal(0.0, iv, 12);
    }

    /// <summary>
    /// Exact, and the one stability statement this module makes: the tangent rate is
    /// proportional to K4^2(a^2 - 3ax + 1.5x^2) + 1 with a = h0/t', x = s/t'. Its
    /// minimum over the travel sits at x = a and equals 1 - 0.5 K4^2 a^2, so the rate
    /// at flat is exactly zero when K4.a = sqrt(2) - and the spring snaps through
    /// above it. No rule of thumb involved.
    /// </summary>
    [Fact]
    public void RateAtFlatIsZeroAtTheRootTwoShapeNumber()
    {
        var s = SeriesA40();
        s.Thickness = 2.0;
        s.FreeHeight = 2.0 + Math.Sqrt(2.0) * 2.0;   // h0/t = sqrt(2) exactly
        s.Calculate();

        Assert.Equal(Math.Sqrt(2.0), s.H0OverT, 12);
        Assert.Equal(0.0, s.RateAt(s.ConeHeight), 6);
        Assert.False(s.CharacteristicTurnsOver, "sqrt(2) is the boundary, not past it");

        s.FreeHeight += 0.01;
        s.Calculate();
        Assert.True(s.CharacteristicTurnsOver);
        Assert.True(s.RateAt(s.ConeHeight) < 0);
    }

    /// <summary>
    /// A series A spring rises monotonically the whole way to flat, because its shape
    /// number is far below sqrt(2).
    /// </summary>
    [Fact]
    public void SeriesAForceRisesAllTheWayToFlat()
    {
        var s = SeriesA40();
        s.Calculate();

        Assert.False(s.CharacteristicTurnsOver);

        double previous = 0;
        for (int k = 1; k <= 50; k++)
        {
            double f = s.ForceAt(s.ConeHeight * k / 50.0);
            Assert.True(f > previous, $"force fell back at s/h0 = {k / 50.0:F2}");
            previous = f;
        }
    }

    /// <summary>
    /// Working from a force inverts the characteristic: asking for the deflection at
    /// a force taken off the curve returns the deflection it came from.
    /// </summary>
    [Fact]
    public void ForceInversionRoundTrips()
    {
        var s = SeriesA40();
        s.Calculate();

        foreach (var ratio in new[] { 0.2, 0.5, 0.75, 0.95 })
        {
            double target = ratio * s.ConeHeight;
            double force = s.ForceAt(target);
            Assert.Equal(target, s.DeflectionForForce(force), 5);
        }
    }

    // ---------------- the four corner stresses ----------------

    /// <summary>
    /// The Almen-Laszlo statement in one assertion. The tangential stress at a
    /// material point varies as -z/r, so the part of it that flips sign between the
    /// two faces must be 1/delta smaller at the outer edge than at the inner edge:
    ///
    ///     sigma_III - sigma_IV = (sigma_II - sigma_I) / delta
    ///
    /// identically, at any deflection. This is the cheapest check that all four
    /// formulas, the 1/delta and the two K3 signs have been transcribed correctly -
    /// a sign slip on any one of them breaks it.
    /// </summary>
    [Theory]
    [InlineData(0.25)]
    [InlineData(0.75)]
    [InlineData(1.00)]
    public void OuterEdgeBendingIsTheInnerEdgeDividedByDelta(double ratio)
    {
        var s = SeriesA40();
        s.Calculate();

        var (i, ii, iii, iv) = s.StressesAt(ratio * s.ConeHeight);

        Assert.Equal((ii - i) / s.DiameterRatio, iii - iv, 6);
        Assert.True(ii - i > 0, "the lower face is in tension relative to the upper face");
    }

    /// <summary>
    /// In the normal working range of a DIN 2093 spring the two upper-face points are
    /// compressive and the two lower-face points tensile, with point I the largest in
    /// magnitude - which is why point I is the one the static limit applies to and
    /// points II and III the ones fatigue is assessed on.
    /// </summary>
    [Fact]
    public void UpperFaceIsCompressiveAndPointIIsTheLargest()
    {
        var s = SeriesA40();
        s.Calculate();

        var p = s.RecommendedLimit;

        Assert.True(p.StressI < 0, "point I, upper inner edge");
        Assert.True(p.StressIV < 0, "point IV, upper outer edge");
        Assert.True(p.StressII > 0, "point II, lower inner edge");
        Assert.True(p.StressIII > 0, "point III, lower outer edge");

        Assert.True(Math.Abs(p.StressI) > Math.Abs(p.StressII));
        Assert.True(Math.Abs(p.StressI) > Math.Abs(p.StressIII));
        Assert.True(Math.Abs(p.StressI) > Math.Abs(p.StressIV));
    }

    /// <summary>
    /// The stress carries a factor s, but the bracket it multiplies contains
    /// (h0/t' - s/2t'), which GROWS as the deflection falls. So the curve is concave:
    /// at half the travel the stress is more than half - 1441 against 2681 MPa on
    /// this spring, i.e. 54 %, not 50 %.
    ///
    /// The direction is what matters. "Back off a bit and the stress comes down
    /// proportionally" is the intuition a linear spring teaches, and it is wrong
    /// here in the unsafe direction: the first part of the travel costs more stress
    /// per millimetre than the last.
    /// </summary>
    [Fact]
    public void StressIsConcaveInDeflectionNotProportional()
    {
        var s = SeriesA40();
        s.Calculate();

        double full = Math.Abs(s.StressesAt(s.ConeHeight).I);
        double half = Math.Abs(s.StressesAt(0.5 * s.ConeHeight).I);

        Assert.True(half > 0.5 * full, $"half travel gives {half / full:P1} of the flat stress");
        Assert.True(half < full);
        Assert.InRange(half / full, 0.50, 0.60);
    }

    /// <summary>
    /// The fatigue point is chosen at the UPPER position and the lower stress is then
    /// read at the SAME point. A range taken between two different points is not a
    /// range, and on a spring where the governing point changes over the travel that
    /// is an easy mistake to make.
    /// </summary>
    [Fact]
    public void FatigueRangeIsReadAtOnePoint()
    {
        var s = SeriesA40();
        s.Deflection1 = 0.25 * 0.90;
        s.Deflection2 = 0.75 * 0.90;
        s.Calculate();

        Assert.Equal("II", s.GoverningFatiguePoint);
        Assert.Equal(s.Position2.StressII, s.UpperStress, 9);
        Assert.Equal(s.Position1.StressII, s.LowerStress, 9);
        Assert.Equal(s.UpperStress - s.LowerStress, s.StressRange, 9);
        Assert.True(s.StressRange > 0);
    }

    /// <summary>
    /// No fatigue limit entered means no fatigue safety factor - the page says the
    /// DIN 2093 diagram has to be read, rather than inventing a permissible stress.
    /// </summary>
    [Fact]
    public void FatigueSafetyNeedsASuppliedLimit()
    {
        var s = SeriesA40();
        s.Deflection1 = 0.2;
        s.Deflection2 = 0.6;
        s.Calculate();

        Assert.False(s.FatigueLimitSupplied);
        Assert.Equal(0.0, s.SafetyFactorFatigue);

        s.PermissibleUpperStress = 1200;
        s.Calculate();

        Assert.True(s.FatigueLimitSupplied);
        Assert.Equal(1200.0 / s.UpperStress, s.SafetyFactorFatigue, 9);
    }

    // ---------------- stacks ----------------

    /// <summary>
    /// n in parallel multiplies force, i in series multiplies deflection, and the
    /// column rate goes as n/i. The free length gains the material thickness t for
    /// every extra disc nested in parallel - t, not t': the reduced thickness
    /// describes the ground contact flat, not how thick the disc is.
    /// </summary>
    [Fact]
    public void StackMultipliesForceAndDeflection()
    {
        var one = SeriesA40();
        one.Deflection2 = 0.675;
        one.Calculate();

        var stack = SeriesA40();
        stack.Deflection2 = 0.675;
        stack.ParallelCount = 3;
        stack.SeriesCount = 4;
        stack.Calculate();

        Assert.Equal(12, stack.DiscCount);
        Assert.Equal(3 * one.Position2.Force, stack.Position2.StackForce, 9);
        Assert.Equal(4 * 0.675, stack.Position2.StackDeflection, 9);
        Assert.Equal(one.Position2.Rate * 3.0 / 4.0, stack.StackRate, 9);

        Assert.Equal(4 * (one.FreeHeight + 2 * one.Thickness), stack.StackFreeLength, 9);
        Assert.Equal(12 * one.Thickness, stack.StackSolidLength, 9);
        Assert.Equal(4 * one.ConeHeight, stack.StackMaxDeflection, 9);

        // One disc's stress does not know about the stack.
        Assert.Equal(one.Position2.StressI, stack.Position2.StressI, 9);
    }

    // ---------------- degenerate input ----------------

    /// <summary>
    /// The bug class this repo keeps finding: a page reuses one engine instance across
    /// Back to Input, so a second run on degenerate geometry must not leave the first
    /// run's numbers on screen. Every field is assigned on every run.
    /// </summary>
    [Fact]
    public void DegenerateGeometryDoesNotKeepThePreviousRun()
    {
        var s = SeriesA40();
        s.Deflection2 = 0.675;
        s.Calculate();
        Assert.True(s.Position2.Force > 0);

        s.OuterDiameter = 0;
        s.Calculate();

        Assert.Equal(0.0, s.Position2.Force);
        Assert.Equal(0.0, s.Position2.StressI);
        Assert.Equal(0.0, s.Flat.Force);
        Assert.Equal(0.0, s.StaticStressAtFlat);
    }

    /// <summary>
    /// A spring that carries nothing has a safety factor of 0, not 999. A sentinel
    /// here paints an impossible design green, which is how three other modules in
    /// this repo shipped a FAIL as "OK".
    /// </summary>
    [Fact]
    public void NoStressMeansZeroSafetyNotASentinel()
    {
        var s = SeriesA40();
        s.OuterDiameter = 0;
        s.Calculate();

        Assert.Equal(0.0, s.SafetyFactorStatic);
        Assert.Equal(0.0, s.SafetyFactorFatigue);
    }

    /// <summary>
    /// The series match is a resemblance test on the shape number, and it refuses to
    /// claim one when the spring sits between two series. Series A is h0/t ~ 0,40,
    /// B ~ 0,75, C ~ 1,30.
    /// </summary>
    [Theory]
    [InlineData(0.40, "A")]
    [InlineData(0.75, "B")]
    [InlineData(1.30, "C")]
    [InlineData(0.57, "")]
    public void SeriesIsMatchedOnTheShapeNumber(double h0OverT, string expected)
    {
        var s = SeriesA40();
        s.Thickness = 2.0;
        s.FreeHeight = 2.0 + h0OverT * 2.0;
        s.Calculate();

        Assert.Equal(h0OverT, s.H0OverT, 9);
        Assert.Equal(expected, s.SeriesMatch);
    }

    /// <summary>DIN 2093 groups split on thickness at 1,25 and 6 mm, and stop at 14.</summary>
    [Theory]
    [InlineData(0.80, 1)]
    [InlineData(1.25, 1)]
    [InlineData(2.25, 2)]
    [InlineData(6.00, 2)]
    [InlineData(8.00, 3)]
    [InlineData(14.00, 3)]
    [InlineData(20.00, 0)]
    public void ThicknessGroupFollowsDin2093(double t, int group)
    {
        var s = SeriesA40();
        s.Thickness = t;
        s.FreeHeight = t * 1.4;
        s.Calculate();

        Assert.Equal(group, s.ThicknessGroup);
    }

    /// <summary>
    /// Travel past flat is warned about rather than silently computed as if the disc
    /// were still a cone, and so is the 0,75 h0 recommendation.
    /// </summary>
    [Fact]
    public void TravelWarningsFireAtTheRightPlaces()
    {
        var s = SeriesA40();
        s.Deflection2 = 0.60;            // 0,67 h0
        s.Calculate();
        Assert.DoesNotContain(s.Validate(), m => m.Contains("0.75 h"));

        // AT the recommended limit, and this is a regression: s₂ typed as 0.675 and
        // a computed 0.75 x 0.9 differ in the last bit of a double, so the warning
        // fired at exactly the limit and read "0.68 mm is above 0.68 mm".
        s.Deflection2 = 0.675;
        s.Calculate();
        Assert.DoesNotContain(s.Validate(), m => m.Contains("0.75 h"));

        s.Deflection2 = 0.80;            // 0,89 h0
        s.Calculate();
        Assert.Contains(s.Validate(), m => m.Contains("0.75 h"));

        // AT flat - the flat position is reachable, so it is not "past flat".
        s.Deflection2 = 0.90;
        s.Calculate();
        Assert.DoesNotContain(s.Validate(), m => m.Contains("exceeds the flat position"));

        s.Deflection2 = 1.20;            // past flat
        s.Calculate();
        Assert.Contains(s.Validate(), m => m.Contains("exceeds the flat position"));
    }

    /// <summary>
    /// The static check is reported twice, at the working position and at flat, and
    /// they are different questions. A series A disc is past the 2600 MPa figure at
    /// flat and comfortably inside it at the 0,75 h0 travel limit, so reporting only
    /// the flat-position factor marks a perfectly usable design as failed - which is
    /// how it first shipped, and is the interface bug class this repo keeps finding.
    /// </summary>
    [Fact]
    public void StaticCheckIsSplitBetweenTheWorkingPositionAndFlat()
    {
        var s = SeriesA40();
        s.Deflection1 = 0.225;
        s.Deflection2 = 0.675;          // the recommended travel limit
        s.Calculate();

        Assert.False(s.WorksToFlat);
        Assert.True(s.SafetyFactorStatic < 1.0, "a series A disc is past the limit at flat");
        Assert.True(s.SafetyFactorAtPosition2 > 1.2, "but well inside it at 0,75 h0");

        // The warning names a stop, not an overstressed design.
        Assert.Contains(s.Validate(), m => m.Contains("fit a stop"));
        Assert.DoesNotContain(s.Validate(), m => m.Contains("overstressed as specified"));

        // Taken all the way to flat, the flat-position check becomes the live one and
        // the two factors coincide.
        s.Deflection2 = 0.900;
        s.Calculate();

        Assert.True(s.WorksToFlat);
        Assert.Equal(s.SafetyFactorStatic, s.SafetyFactorAtPosition2, 9);
        Assert.Contains(s.Validate(), m => m.Contains("overstressed as specified"));
    }

    // ---------------- the worked example ----------------

    /// <summary>
    /// The spring the reference section under the calculator is written around: a
    /// series A shape at 40 mm, worked between 0,25 h0 and 0,75 h0. These values are
    /// what the content file prints, so they are pinned here - if an engine change
    /// moves them, the worked example has to be re-run.
    ///
    /// Each is reconstructed from the closed form in the assertion rather than copied
    /// from a run, so the test is not merely "the engine still agrees with itself".
    /// </summary>
    [Fact]
    public void WorkedExampleReproduces()
    {
        var s = SeriesA40();
        s.Deflection1 = 0.225;           // 0,25 h0
        s.Deflection2 = 0.675;           // 0,75 h0
        s.Calculate();

        Assert.Equal(0.90, s.ConeHeight, 9);
        Assert.Equal(0.40, s.H0OverT, 9);
        Assert.Equal("A", s.SeriesMatch);
        Assert.Equal(2, s.ThicknessGroup);

        double delta = 40.0 / 20.4;
        Assert.Equal(delta, s.DiameterRatio, 9);

        // F at 0,75 h0, from the prefactor and the bracket written out by hand.
        double pre = 4.0 * 206000 / (1 - 0.09) * Math.Pow(2.25, 4) / (s.K1 * 1600);
        double x = 0.675 / 2.25;
        double expected = pre * x * ((0.40 - x) * (0.40 - x / 2) + 1.0);
        Assert.Equal(expected, s.Position2.Force, 6);

        // Round figures the content file quotes.
        Assert.Equal(6500, s.Position2.Force, 0);
        Assert.Equal(2086, Math.Abs(s.Position2.StressI), 0);
        Assert.Equal(1328, s.Position2.StressII, 0);
        Assert.Equal(2680, s.StaticStressAtFlat, 0);

        // The point of the example: a well-formed series A shape is already past the
        // 2600 MPa practice limit at the flat position, which is most of the reason
        // 0,75 h0 is the recommended travel limit rather than advice - while being
        // comfortably inside it at that limit.
        Assert.True(s.SafetyFactorStatic < 1.0);
        Assert.Equal(1.25, s.SafetyFactorAtPosition2, 2);
        Assert.True(Math.Abs(s.RecommendedLimit.StressI) < s.PermissibleStaticStress);

        // Figures the content file quotes in its prose.
        Assert.Equal(1955, s.Flat.Force - s.Position2.Force, 0);
        Assert.Equal(594, s.StaticStressAtFlat - s.StaticStressAtPosition2, 0);
    }

    // ---------------- fixtures ----------------

    /// <summary>
    /// A series A shape at De = 40: h0/t = 0,40 and De/t = 17,8, which are the two
    /// ratios that define the series. Free dimensions, not a catalogue row - the
    /// DIN 2093 size table is deliberately not embedded in this repo.
    /// </summary>
    private static DiscSpringEngine SeriesA40() => new()
    {
        OuterDiameter = 40.0,
        InnerDiameter = 20.4,
        Thickness = 2.25,
        FreeHeight = 3.15,
        ElasticModulus = 206000,
        PoissonRatio = 0.30,
        Density = 7.85e-3,
    };

    /// <summary>
    /// A Group 3 shape: thick enough that DIN 2093 gives it contact surfaces, and a
    /// series A proportion to go with it (De/t 17.5, h0/t' 0.40). The first version
    /// of this fixture was 80 x 40.5 x 8, which has De/t = 10 and runs to 7786 MPa at
    /// flat - a disc that cannot exist. It was caught by reading the live page, not by
    /// a test, because every assertion about K4 passed on it regardless.
    /// </summary>
    private static DiscSpringEngine Group3_140() => new()
    {
        OuterDiameter = 140.0,
        InnerDiameter = 72.0,
        Thickness = 8.0,
        FreeHeight = 10.5,
        HasContactSurfaces = true,
        ElasticModulus = 206000,
        PoissonRatio = 0.30,
        Density = 7.85e-3,
    };
}
