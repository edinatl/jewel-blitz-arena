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
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedTillySetter), Is.False);
        Assert.That(
            profile.UnlockedHouseRulesBits & HouseRulesBits.PhaseClock,
            Is.Not.EqualTo(0));

        UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedSoundTest), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedTillySetter), Is.True);

        while (profile.DistinctTournamentClears < 12)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        }

        Assert.That(profile.HasFlag(ProfileFlags.UnlockedTerracesExtra), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedBossRushPreset), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedBehindCounter), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedRemixOption), Is.True);
        Assert.That(UnlockLadderRuntime.ExtraArenaSlotCount(profile), Is.EqualTo(2));
    }

    [Test]
    public void Synthetic_profile_walks_full_ladder_to_14_with_extra_arenas()
    {
        var profile = PlayerProfile.CreateNew();
        profile.SetFlag(ProfileFlags.OwnedFullCast);

        // Expected flip at each clear count (modes.md §8).
        int?[] expectedNewFlag =
        {
            null, // 0 — unused
            null, // 1 House Rule only
            null,
            null,
            null,
            null,
            null,
            ProfileFlags.UnlockedCaptainCitrine, // 7 (+ Phase Clock)
            ProfileFlags.UnlockedTillySetter, // 8 (+ Sound Test)
            ProfileFlags.UnlockedTerracesExtra, // 9 Extra slot 1
            ProfileFlags.UnlockedBossRushPreset, // 10
            ProfileFlags.UnlockedBehindCounter, // 11 Extra slot 2
            ProfileFlags.UnlockedRemixOption, // 12
            ProfileFlags.UnlockedSpirewalk, // 13 Extra slot 3
            ProfileFlags.UnlockedCascade, // 14 Extra slot 4
        };

        for (int clear = 1; clear <= UnlockLadderRuntime.MaxLadderClears; clear++)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
            Assert.That(profile.DistinctTournamentClears, Is.EqualTo(clear));

            int? flag = expectedNewFlag[clear];
            if (flag != null)
            {
                Assert.That(
                    profile.HasFlag(flag.Value),
                    Is.True,
                    $"clear {clear} should unlock flag {flag.Value}");
            }

            int slots = UnlockLadderRuntime.ExtraArenaSlotCount(profile);
            int expectSlots = clear < 9 ? 0
                : clear < 11 ? 1
                : clear < 13 ? 2
                : clear < 14 ? 3
                : 4;
            Assert.That(slots, Is.EqualTo(expectSlots), $"clear {clear} Extra Arena slots");
        }

        Assert.That(profile.UnlockedHouseRulesBits, Is.EqualTo(
            HouseRulesBits.PocketItems
            | HouseRulesBits.JewelCapOverride
            | HouseRulesBits.LusterTimer
            | HouseRulesBits.BlitzedDamageTaken
            | HouseRulesBits.BlitzedAttack
            | HouseRulesBits.RallySeconds
            | HouseRulesBits.PhaseClock));
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedCascade), Is.True);
        Assert.That(UnlockLadderRuntime.ExtraArenaSlotCount(profile), Is.EqualTo(4));
    }

    [Test]
    public void Cascade_and_slot_4_require_full_cast()
    {
        var profile = PlayerProfile.CreateNew();
        // No OwnedFullCast.
        for (int i = 0; i < 14; i++)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        }

        Assert.That(profile.DistinctTournamentClears, Is.EqualTo(14));
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedSpirewalk), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedCascade), Is.False);
        Assert.That(UnlockLadderRuntime.ExtraArenaSlotCount(profile), Is.EqualTo(3));
        Assert.That(
            UnlockLadderRuntime.IsCascadeUnlocked(14, ownedFullCast: false),
            Is.False);
        Assert.That(
            UnlockLadderRuntime.IsCascadeUnlocked(14, ownedFullCast: true),
            Is.True);

        profile.SetFlag(ProfileFlags.OwnedFullCast);
        UnlockLadderRuntime.SyncUnlocks(profile);
        Assert.That(profile.HasFlag(ProfileFlags.UnlockedCascade), Is.True);
        Assert.That(UnlockLadderRuntime.ExtraArenaSlotCount(profile), Is.EqualTo(4));
    }

    [Test]
    public void Synthetic_harness_asserts_every_modes_md_row()
    {
        // Drive a fresh profile through each condition and assert the unlock flips
        // (modes.md §8 automated-test requirement).
        for (int clears = 0; clears <= UnlockLadderRuntime.MaxLadderClears; clears++)
        {
            var profile = PlayerProfile.CreateNew();
            profile.SetFlag(ProfileFlags.OwnedFullCast);
            profile.DistinctTournamentClears = clears;
            UnlockLadderRuntime.SyncUnlocks(profile);

            Assert.That(
                UnlockLadderRuntime.IsCaptainCitrineUnlocked(clears),
                Is.EqualTo(profile.HasFlag(ProfileFlags.UnlockedCaptainCitrine)));
            Assert.That(
                UnlockLadderRuntime.IsTillySetterUnlocked(clears),
                Is.EqualTo(profile.HasFlag(ProfileFlags.UnlockedTillySetter)));
            Assert.That(
                UnlockLadderRuntime.IsBehindCounterUnlocked(clears),
                Is.EqualTo(profile.HasFlag(ProfileFlags.UnlockedBehindCounter)));
            Assert.That(
                UnlockLadderRuntime.IsSpirewalkUnlocked(clears),
                Is.EqualTo(profile.HasFlag(ProfileFlags.UnlockedSpirewalk)));
            Assert.That(
                UnlockLadderRuntime.IsCascadeUnlocked(clears, true),
                Is.EqualTo(profile.HasFlag(ProfileFlags.UnlockedCascade)));
        }
    }
}
