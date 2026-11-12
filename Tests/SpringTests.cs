using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

public class SpringTests
{
    /// <summary>
    /// EN 13906-1 rate, R = G d^4 / (8 Dm^3 n). For d 4, Dm 32, n 8, G 81500:
    ///   81500 x 256 / (8 x 32768 x 8) = 20864000 / 2097152 = 9.9487304 N/mm
    /// </summary>
    [Fact]
    public void RateFollowsTheClosedForm()
    {
        var spring = Spring4x32();
        spring.Calculate();

        Assert.Equal(20864000.0 / 2097152.0, spring.SpringRateCalculated, 9);
        Assert.Equal(spring.SpringRateCalculated, spring.SpringRate, 9);
    }

    /// <summary>
    /// Wahl at spring index w = 8: (4w-1)/(4w-4) + 0.615/w = 31/28 + 0.076875.
    /// </summary>
    [Fact]
    public void WahlFactorMatchesTheStandardForm()
    {
        var spring = Spring4x32();
        spring.Calculate();

        Assert.Equal(8.0, spring.SpringIndex, 9);
        Assert.Equal(31.0 / 28.0 + 0.615 / 8.0, spring.WahlFactor, 9);
    }

    /// <summary>
    /// Two test points override the closed form: a spring measured at 20 N / 90 mm
    /// and 60 N / 70 mm has a rate of 40 N over 20 mm whatever its geometry says.
    /// </summary>
    [Fact]
    public void MeasuredPointsWinOverTheClosedForm()
    {
        var spring = Spring4x32();
        spring.Force1 = 20; spring.Length1 = 90;
        spring.Force2 = 60; spring.Length2 = 70;
        spring.Calculate();

        Assert.Equal(2.0, spring.SpringRate, 9);
        Assert.NotEqual(spring.SpringRateCalculated, spring.SpringRate);
    }

    /// <summary>
    /// Bergstraesser, (w + 0,5)/(w - 0,75), which is the form EN 13906-1 uses.
    /// At w = 8 it is 8,5/7,25 = 1,1724 against Wahl's 1,1839.
    /// </summary>
    [Fact]
    public void BergstrasserSitsJustBelowWahl()
    {
        var spring = Spring4x32();
        spring.Calculate();

        Assert.Equal(8.5 / 7.25, spring.BergstrasserFactor, 9);
        Assert.True(spring.BergstrasserFactor < spring.WahlFactor);
        Assert.True(spring.WahlFactor / spring.BergstrasserFactor < 1.02, "the two differ by about 1 %");
    }

    // ---------------- Buckling, EN 13906-1 ----------------

    /// <summary>
    /// The seating factor is a BUCKLING LENGTH factor: the buckling length is
    /// ν·L0, so a larger ν is a LESS stable seating. The effective slenderness
    /// was computed as λ/ν, which inverted the whole ranking - the most
    /// dangerous seating (one end free, ν = 2) came out the least slender and
    /// was reported "No buckling - OK", while the most stable one was flagged.
    /// </summary>
    [Fact]
    public void EffectiveSlendernessMultipliesBySeatingFactor()
    {
        var spring = Default80();
        spring.Calculate();

        Assert.Equal(80.0 / 24.0, spring.SlendernessRatio, 9);
        Assert.Equal(0.5, spring.EndConditionFactor, 9);                       // flat parallel ends
        Assert.Equal(0.5 * 80.0 / 24.0, spring.EffectiveSlenderness, 9);
    }

    /// <summary>
    /// Ends held parallel against flat plates cannot rotate, so that is the
    /// fixed-fixed case, ν = 0,5. ν = 1 is both ends hinged. The dropdown had
    /// flat plates at 1,0 and called 0,5 a "guided rod", which is not a seating
    /// case at all.
    /// </summary>
    [Theory]
    [InlineData("BothFlat", 0.5)]
    [InlineData("BothFixed", 0.5)]            // legacy key, same physical case
    [InlineData("OneFixedOneFlat", 0.7)]
    [InlineData("BothHinged", 1.0)]
    [InlineData("OneFixedOneSliding", 1.0)]
    [InlineData("OneFixedOneFree", 2.0)]
    [InlineData("something-a-stale-link-carried", 0.5)]
    public void SeatingFactorsFollowEn13906(string key, double expected)
    {
        Assert.Equal(expected, SpringEngine.SeatingFactor(key), 9);
    }

    /// <summary>
    /// The EN 13906-1 buckling deflection,
    ///   s_K = L0 (0,5/(1-G/E)) [1 - sqrt(1 - ((1-G/E)/(0,5+G/E)) (pi D/(nu L0))^2)]
    /// independently for the default spring (d 3, Dm 24, L0 80, G 81 500,
    /// E 206 000). G/E = 0,39563:
    ///   nu = 1   -> 24,3 mm
    ///   nu = 2   ->  5,2 mm
    ///   nu = 0,7 -> the root argument is negative: no buckling at any deflection
    /// The simplified constant-2,62 form this replaced gave 30,6 mm at nu = 1,
    /// which is 26 % on the unsafe side even with the inversion corrected.
    /// </summary>
    [Theory]
    [InlineData("BothHinged", 24.3)]
    [InlineData("OneFixedOneFree", 5.2)]
    public void BucklingDeflectionMatchesEn13906(string seating, double expected)
    {
        var spring = Default80();
        spring.EndCondition = seating;
        spring.Calculate();

        Assert.True(spring.BucklingPossible);
        Assert.Equal(expected, spring.BucklingDeflection, 0.1);
    }

    [Theory]
    [InlineData("OneFixedOneFlat")]   // nu 0,7
    [InlineData("BothFlat")]          // nu 0,5
    public void AShortFatSpringCannotBuckleAtAll(string seating)
    {
        var spring = Default80();
        spring.EndCondition = seating;
        spring.Calculate();

        Assert.False(spring.BucklingPossible);
        Assert.False(spring.BucklingRisk);
    }

    /// <summary>
    /// The ranking is the point: a looser seating must always buckle sooner.
    /// This is what a single-point anchor cannot see, and what was inverted.
    /// </summary>
    [Fact]
    public void ALooserSeatingAlwaysBucklesSooner()
    {
        double previous = double.MaxValue;
        foreach (var seating in new[] { "BothFlat", "OneFixedOneFlat", "BothHinged", "OneFixedOneFree" })
        {
            var spring = Default80();
            spring.FreeLength = 200;          // slender enough that all four can buckle
            spring.EndCondition = seating;
            spring.Calculate();

            Assert.True(spring.BucklingPossible, $"{seating} should be able to buckle at L0 = 200");
            Assert.True(spring.BucklingDeflection < previous,
                $"{seating} must buckle sooner than the stiffer seating before it");
            previous = spring.BucklingDeflection;
        }
    }

    /// <summary>
    /// The risk is judged at the deflection the user asked for, not at the solid
    /// length the spring never reaches. The default spring at L1 = 55 (s = 25 mm)
    /// buckles with one end free and does not with both ends on flat plates -
    /// the page reported the opposite of both.
    /// </summary>
    [Fact]
    public void RiskIsJudgedAtTheWorkingDeflection()
    {
        var free = Default80();
        free.EndCondition = "OneFixedOneFree";
        free.Length1 = 55;
        free.Force1 = 186.5;
        free.Length2 = 55;
        free.Force2 = 186.5;
        free.Calculate();
        Assert.Equal(25.0, free.MaxWorkingDeflection, 6);
        Assert.True(free.BucklingRisk, "25 mm is well past s_K of about 5 mm");

        var guided = Default80();
        guided.EndCondition = "BothFlat";
        guided.Length1 = 55;
        guided.Calculate();
        Assert.False(guided.BucklingRisk);
    }

    // ---------------- Coil clearance and solid length ----------------

    /// <summary>
    /// S_a = (0,0015 Dm^2/d + 0,1 d) n, EN 13906-1: the clearance that has to
    /// remain between the coils at the largest working force. The page checked
    /// only "clearance to solid > 0", so 1 mm counted as adequate where the
    /// standard asks for 4,7 mm on this spring.
    /// </summary>
    [Fact]
    public void MinimumCoilClearanceFollowsEn13906()
    {
        var spring = Default80();
        spring.Calculate();

        double expected = (0.0015 * 24 * 24 / 3 + 0.1 * 3) * 8;
        Assert.Equal(expected, spring.MinimumCoilClearance, 9);
        Assert.Equal(4.7, spring.MinimumCoilClearance, 0.05);
    }

    /// <summary>
    /// An UNGROUND end cannot lie flat, so EN 13906-1 allows (nt + 1,5) d for
    /// the solid length where a ground end gets nt d. The engine used
    /// (nt + 1) d for an unground end, half a wire diameter optimistic on the
    /// available stroke.
    /// </summary>
    [Fact]
    public void UngroundEndsGetTheExtraHalfDiameter()
    {
        var ground = Default80();
        ground.EndType = "ClosedGround";
        ground.Calculate();
        Assert.Equal(10 * 3.0, ground.SolidLength, 9);        // nt = n + 2 = 10

        var unground = Default80();
        unground.EndType = "Closed";
        unground.Calculate();
        Assert.Equal(11.5 * 3.0, unground.SolidLength, 9);

        // Half a wire diameter more than the (nt + 1) d the engine used before.
        Assert.Equal(0.5 * 3.0, unground.SolidLength - 11.0 * 3.0, 9);

        // And that much less stroke available.
        Assert.Equal(ground.MaxDeflection - 4.5, unground.MaxDeflection, 9);
    }

    /// <summary>
    /// An open-coiled spring has no dead coils, so the pitch is (L0 - d)/n and
    /// not (L0 - 2d)/n. Every end type used to get the same pitch.
    /// </summary>
    [Fact]
    public void AnOpenEndHasNoDeadCoils()
    {
        var open = Default80();
        open.EndType = "Open";
        open.Calculate();
        Assert.Equal(80.0 / 8.0, open.CoilPitch, 9);

        var closed = Default80();
        closed.EndType = "ClosedGround";
        closed.Calculate();
        Assert.Equal((80.0 - 6.0) / 8.0, closed.CoilPitch, 9);
        Assert.True(open.CoilPitch > closed.CoilPitch);
    }

    private static SpringEngine Spring4x32() => new()
    {
        WireDiameter = 4,
        MeanCoilDiameter = 32,
        ActiveCoils = 8,
        TotalCoils = 10,
        FreeLength = 100,
        ShearModulus = 81500,
        TensileStrength = 1700
    };

    /// <summary>
    /// The page's own worked example: d 3, Dm 24, L0 80, n 8, SH wire,
    /// closed and ground ends.
    /// </summary>
    private static SpringEngine Default80() => new()
    {
        WireDiameter = 3,
        MeanCoilDiameter = 24,
        ActiveCoils = 8,
        FreeLength = 80,
        EndType = "ClosedGround",
        EndCondition = "BothFlat",
        ShearModulus = 81500,
        ElasticModulus = 206000,
        TensileStrength = 1700
    };
}
