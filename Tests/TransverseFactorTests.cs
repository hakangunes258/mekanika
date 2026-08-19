using MechanicalCalculatorWeb.Services;
using Xunit;

namespace Mekanika.Tests;

/// <summary>
/// ISO 6336-1 Clause 8.3. K_Halpha has two branches, one either side of
/// eps_gamma = 2, and the shape of the second is easy to get wrong.
/// </summary>
public class TransverseFactorTests
{
    /// <summary>
    /// The branches must meet. Eq. (71) gives 0.9 + 0.4 q_alpha at eps_gamma = 2;
    /// Eq. (72) has sqrt(2(eps_gamma - 1)/eps_gamma), which is exactly 1 there, so
    /// it must give the same thing.
    ///
    /// This is the check that catches q_alpha being written inside the square root
    /// rather than outside it. With it inside, the second branch reads
    /// 0.9 + 0.4 sqrt(q) and the two disagree at the seam for every q but 1 - here
    /// 1.094 against 0.994. It shipped that way, and the error runs the wrong way
    /// for a lightly loaded or coarse pair: sqrt(q) is below q once q exceeds 1, so
    /// K_Halpha came out too small exactly where it matters most.
    /// </summary>
    [Theory]
    [InlineData(400.0)]
    [InlineData(700.0)]
    [InlineData(1200.0)]
    public void TheTwoBranchesMeetAtTotalContactRatioTwo(double loadPerMm)
    {
        // Straddle the seam as closely as floating point allows, so what is being
        // measured is the join between the two expressions and not the 1e-4 of
        // eps_gamma between two sample points.
        var below = Evaluate(2.0 - 1e-9, loadPerMm);
        var above = Evaluate(2.0 + 1e-9, loadPerMm);

        Assert.Equal(below.KHalpha, above.KHalpha, 8);
    }

    /// <summary>
    /// A KISSsoft 2022 SP3 report to ISO 6336:2006 for a 22/45, mn 3, beta 22.5
    /// pair at 50 kW prints K_Halpha = 1.016. Fed that report's own K_Hbeta of
    /// 1.052 - ours is higher, for reasons recorded in CLAUDE.md - the engine
    /// lands on 1.017.
    /// </summary>
    [Fact]
    public void MatchesTheKisssoftReportWhenGivenItsOwnFaceLoadFactor()
    {
        var r = Iso6336TransverseFactor.Calculate(
            Ft: 11139.4, b: 27.0, KA: 1.25, KV: 1.019, KHbeta: 1.052,
            cGammaAlpha: 16.815, fpb: 9.6, ffalpha: 0.0, yAlpha: 0.70,
            epsilonAlpha: 1.349, epsilonGamma: 2.445, ZEpsilon: 0.861);

        Assert.InRange(r.KHalpha, 1.005, 1.030);
    }

    /// <summary>Never below 1: a transverse load factor cannot relieve a mesh.</summary>
    [Theory]
    [InlineData(1.2)]
    [InlineData(1.8)]
    [InlineData(2.4)]
    [InlineData(3.1)]
    public void NeverFallsBelowUnity(double epsilonGamma)
    {
        Assert.True(Evaluate(epsilonGamma, 2000.0).KHalpha >= 1.0);
    }

    private static Iso6336TransverseFactor.Result Evaluate(double epsilonGamma, double loadPerMm)
    {
        const double b = 27.0;
        return Iso6336TransverseFactor.Calculate(
            Ft: loadPerMm * b, b: b, KA: 1.0, KV: 1.0, KHbeta: 1.0,
            cGammaAlpha: 16.815, fpb: 9.6, ffalpha: 0.0, yAlpha: 0.70,
            epsilonAlpha: 1.349, epsilonGamma: epsilonGamma, ZEpsilon: 0.861);
    }
}
