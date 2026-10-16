using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class UnlockLadderTests
{
    [Test]
    public void House_rules_unlock_in_modes_md_order()
    {
        Assert.That(UnlockLadderRuntime.HouseRulesBitsForClears(0), Is.EqualTo(0));
        Assert.That(
            UnlockLadderRuntime.HouseRulesBitsForClears(1),
            Is.EqualTo(HouseRulesBits.PocketItems));
        Assert.That(
            UnlockLadderRuntime.IsHouseRuleUnlocked(3, HouseRulesBits.LusterTimer),
            Is.True);
        Assert.That(
            UnlockLadderRuntime.IsHouseRuleUnlocked(2, HouseRulesBits.LusterTimer),
            Is.False);
        Assert.That(
            UnlockLadderRuntime.HouseRulesBitsForClears(7) & HouseRulesBits.PhaseClock,
            Is.Not.EqualTo(0));
        Assert.That(
            UnlockLadderRuntime.HouseRulesBitsForClears(14),
            Is.EqualTo(UnlockLadderRuntime.HouseRulesBitsForClears(7)));
    }

    [Test]
    public void ApplyDistinctClear_flips_ladder_flags_at_thresholds()
    {
        var profile = PlayerProfile.CreateNew();
        Assert.That(profile.UnlockedHouseRulesBits, Is.EqualTo(0));

        for (int i = 0; i < 7; i++)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        }

        Assert.That(profile.DistinctTournamentClears, Is.EqualTo(7));
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedCaptainCitrine), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedSoundTest), Is.False);
        Assert.That(
            profile.UnlockedHouseRulesBits & HouseRulesBits.PhaseClock,
            Is.Not.EqualTo(0));

        UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedSoundTest), Is.True);

        while (profile.DistinctTournamentClears < 12)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        }

        Assert.That(profile.HasFlag(ProfileFlags.UnlockedTerracesExtra), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedBossRushPreset), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedRemixOption), Is.True);
    }

    [Test]
    public void Synthetic_profile_walks_full_ladder_to_14()
    {
        var profile = PlayerProfile.CreateNew();
        for (int i = 0; i < UnlockLadderRuntime.MaxLadderClears; i++)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        }

        Assert.That(profile.DistinctTournamentClears, Is.EqualTo(14));
        Assert.That(profile.UnlockedHouseRulesBits, Is.EqualTo(
            HouseRulesBits.PocketItems
            | HouseRulesBits.JewelCapOverride
            | HouseRulesBits.LusterTimer
            | HouseRulesBits.BlitzedDamageTaken
            | HouseRulesBits.BlitzedAttack
            | HouseRulesBits.RallySeconds
            | HouseRulesBits.PhaseClock));
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedRemixOption), Is.True);
    }
}
