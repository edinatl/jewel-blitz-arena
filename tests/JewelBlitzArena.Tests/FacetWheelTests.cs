using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class FacetWheelTests
{
    [Test]
    public void Band_weights_sum_to_1000_at_each_width_level()
    {
        var weights = new int[FacetWheelRuntime.BandCount];
        for (int level = 0; level <= FacetWheelRuntime.MaxWidthLevel; level++)
        {
            FacetWheelRuntime.BandWeightsPermille(level, weights);
            int sum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                Assert.That(weights[i], Is.GreaterThanOrEqualTo(0));
                sum += weights[i];
            }

            Assert.That(sum, Is.EqualTo(1000), $"width level {level}");
        }
    }

    [Test]
    public void Widen_shifts_weight_from_Nothing_to_high_tiers()
    {
        var baseW = new int[FacetWheelRuntime.BandCount];
        var wideW = new int[FacetWheelRuntime.BandCount];
        FacetWheelRuntime.BandWeightsPermille(0, baseW);
        FacetWheelRuntime.BandWeightsPermille(FacetWheelRuntime.MaxWidthLevel, wideW);
        Assert.That(
            wideW[(int)FacetWheelBand.Nothing],
            Is.LessThan(baseW[(int)FacetWheelBand.Nothing]));
        Assert.That(
            wideW[(int)FacetWheelBand.Tier67],
            Is.GreaterThan(baseW[(int)FacetWheelBand.Tier67]));
    }

    [Test]
    public void TrySpin_spends_chits_and_is_deterministic()
    {
        var profile = PlayerProfile.CreateNew();
        profile.Chits = 9;
        const ulong seed = 0x464143455457484CUL; // FACEWHL

        Assert.That(
            FacetWheelRuntime.TrySpin(profile, seed, out FacetWheelBand a, out int caratsA),
            Is.True);
        Assert.That(profile.Chits, Is.EqualTo(6));
        Assert.That(profile.HasFlag(ProfileFlags.TouchedFacetWheel), Is.True);

        var again = PlayerProfile.CreateNew();
        again.Chits = 9;
        Assert.That(FacetWheelRuntime.TrySpin(again, seed, out FacetWheelBand b, out int caratsB), Is.True);
        Assert.That(b, Is.EqualTo(a));
        Assert.That(caratsB, Is.EqualTo(caratsA));
    }

    [Test]
    public void TrySpin_fails_without_chits()
    {
        var profile = PlayerProfile.CreateNew();
        profile.Chits = 2;
        Assert.That(
            FacetWheelRuntime.TrySpin(profile, 1UL, out _, out _),
            Is.False);
        Assert.That(profile.Chits, Is.EqualTo(2));
    }

    [Test]
    public void TryWiden_spends_cumulative_carats()
    {
        var profile = PlayerProfile.CreateNew();
        profile.Carats = 2000;
        Assert.That(FacetWheelRuntime.TryWiden(profile), Is.True);
        Assert.That(profile.FacetWheelWidthLevel, Is.EqualTo(1));
        Assert.That(profile.Carats, Is.EqualTo(0));
        Assert.That(FacetWheelRuntime.TryWiden(profile), Is.False, "need 4000 more to reach level 2");
    }
}
