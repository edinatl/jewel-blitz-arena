using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ClearBonusGiftsTests
{
    [Test]
    public void Clear_bonus_rarity6_chance_only_on_diff8_and_rises_under_par()
    {
        int stage = SampleContentFactory.CitrineBazaarStageId;
        int par = ExpeditionChestRuntime.ParFrames(stage);
        Assert.That(
            ExpeditionClearBonusRuntime.Rarity6ChancePercent(4, par / 2, par),
            Is.EqualTo(0));
        Assert.That(
            ExpeditionClearBonusRuntime.Rarity6ChancePercent(8, par, par),
            Is.EqualTo(70));
        Assert.That(
            ExpeditionClearBonusRuntime.Rarity6ChancePercent(8, (par * 3) / 4, par),
            Is.EqualTo(85));
    }

    [Test]
    public void Clear_bonus_prefers_unowned_and_grants_to_ledger()
    {
        var profile = PlayerProfile.CreateNew();
        int stage = SampleContentFactory.CitrineBazaarStageId;
        int underPar = ExpeditionChestRuntime.ParFrames(stage) / 2;

        int granted = ExpeditionClearBonusRuntime.TryGrant(
            profile, 0x434C424F4E5553UL, stage, difficulty: 4, framesPlayed: underPar);
        Assert.That(granted, Is.Not.EqualTo(0));
        Assert.That(ExpeditionClearBonusRuntime.Rarity5Pool, Does.Contain(granted));
        Assert.That(profile.Ledger.Owns(granted), Is.True);

        // Exhaust r5 pool — still grants (owned fallback).
        foreach (int id in ExpeditionClearBonusRuntime.Rarity5Pool)
        {
            profile.Ledger.Grant(id);
        }

        int again = ExpeditionClearBonusRuntime.TryGrant(
            profile, 0x434C424F4E5553UL, stage, 4, underPar);
        Assert.That(again, Is.Not.EqualTo(0));
        Assert.That(profile.Ledger.Owns(again), Is.True);
    }

    [Test]
    public void Expedition_ApplyResult_grants_clear_bonus_on_clear_only()
    {
        var orch = new ExpeditionModeOrchestrator();
        var request = new ModeRequest
        {
            Mode = ModeId.Expedition,
            Seed = 0x434C424F4E5553UL,
            StageId = SampleContentFactory.CitrineBazaarStageId,
            Difficulty = 5,
        };

        var cleared = new MatchResult
        {
            CaratsEarned = 0,
            ActorCount = 2,
            FramesPlayed = ExpeditionChestRuntime.ParFrames(request.StageId),
        };
        unsafe
        {
            cleared.Placements[0] = 1;
            cleared.Placements[1] = 2;
        }

        var profile = PlayerProfile.CreateNew();
        orch.ApplyResult(profile, cleared, request);
        bool anyBonus = false;
        foreach (int id in ExpeditionClearBonusRuntime.Rarity5Pool)
        {
            if (profile.Ledger.Owns(id))
            {
                anyBonus = true;
                break;
            }
        }

        foreach (int id in ExpeditionClearBonusRuntime.Rarity6Pool)
        {
            if (profile.Ledger.Owns(id))
            {
                anyBonus = true;
                break;
            }
        }

        Assert.That(anyBonus, Is.True);

        var failed = cleared;
        unsafe
        {
            failed.Placements[0] = 2;
            failed.Placements[1] = 1;
        }

        profile = PlayerProfile.CreateNew();
        orch.ApplyResult(profile, failed, request);
        Assert.That(profile.Ledger.Count, Is.EqualTo(0));
    }

    [Test]
    public void Citrine_gifts_require_unlock_and_rise_with_playtime_spend()
    {
        var locked = PlayerProfile.CreateNew();
        Assert.That(CitrineGiftsRuntime.AppearanceChancePermille(locked), Is.EqualTo(0));

        var profile = PlayerProfile.CreateNew();
        profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
        int baseChance = CitrineGiftsRuntime.AppearanceChancePermille(profile);
        Assert.That(baseChance, Is.EqualTo(CitrineGiftsRuntime.BaseAppearPermille));

        profile.PlaytimeSeconds = 60 * 100; // 100 minutes → +100‰
        profile.CaratsSpentLifetime = 500 * 50; // +50‰
        int boosted = CitrineGiftsRuntime.AppearanceChancePermille(profile);
        Assert.That(boosted, Is.GreaterThan(baseChance));
        Assert.That(boosted, Is.EqualTo(baseChance + 100 + 50));
    }

    [Test]
    public void Citrine_grants_Solitaire_then_Diadem_once_each()
    {
        var profile = PlayerProfile.CreateNew();
        profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
        profile.PlaytimeSeconds = 60 * 60 * 10;
        profile.CaratsSpentLifetime = 100_000;

        // Brute a visit seed that places her at the bench.
        int first = 0;
        ulong seed = 0;
        for (ulong s = 1; s < 20_000; s++)
        {
            if (!CitrineGiftsRuntime.IsAtBench(profile, s))
            {
                continue;
            }

            first = CitrineGiftsRuntime.TryGrantGift(profile, s);
            seed = s;
            break;
        }

        Assert.That(first, Is.EqualTo(CitrineGiftsRuntime.SolitaireItemId));
        Assert.That(profile.HasFlag(ProfileFlags.ReceivedSolitaire), Is.True);
        Assert.That(profile.Ledger.Owns(CitrineGiftsRuntime.SolitaireItemId), Is.True);

        int second = 0;
        for (ulong s = seed + 1; s < seed + 50_000; s++)
        {
            if (!CitrineGiftsRuntime.IsAtBench(profile, s))
            {
                continue;
            }

            second = CitrineGiftsRuntime.TryGrantGift(profile, s);
            if (second != 0)
            {
                break;
            }
        }

        Assert.That(second, Is.EqualTo(CitrineGiftsRuntime.DiademItemId));
        Assert.That(profile.HasFlag(ProfileFlags.ReceivedDiadem), Is.True);
        Assert.That(CitrineGiftsRuntime.HasPendingGifts(profile), Is.False);
        Assert.That(CitrineGiftsRuntime.AppearanceChancePermille(profile), Is.EqualTo(0));
    }

    [Test]
    public void Profile_v4_round_trips_playtime_and_spend()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-gifts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var store = new ProfileStore(path);
            PlayerProfile profile = PlayerProfile.CreateNew("Ed");
            profile.PlaytimeSeconds = 3600;
            profile.CaratsSpentLifetime = 12_500;
            store.Save(profile);

            PlayerProfile loaded = store.LoadOrCreate();
            Assert.That(loaded.SchemaVersion, Is.EqualTo(ProfileSchema.Version4));
            Assert.That(loaded.PlaytimeSeconds, Is.EqualTo(3600));
            Assert.That(loaded.CaratsSpentLifetime, Is.EqualTo(12_500));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
