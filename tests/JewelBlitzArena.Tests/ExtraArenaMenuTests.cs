using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ExtraArenaMenuTests
{
    [Test]
    public void Box_entries_lock_until_ladder_slots_unlock()
    {
        var profile = PlayerProfile.CreateNew();
        IReadOnlyList<ExtraArenaRuntime.ExtraArenaEntry> locked =
            ExtraArenaRuntime.BoxEntries(profile);
        Assert.That(locked.Count, Is.EqualTo(4));
        Assert.That(locked.All(e => !e.Unlocked), Is.True);

        profile.SetFlag(ProfileFlags.OwnedFullCast);
        for (int i = 0; i < 14; i++)
        {
            UnlockLadderRuntime.ApplyDistinctTournamentClear(profile);
        }

        IReadOnlyList<ExtraArenaRuntime.ExtraArenaEntry> open =
            ExtraArenaRuntime.BoxEntries(profile);
        Assert.That(open.All(e => e.Unlocked), Is.True);
        Assert.That(open[0].StageId, Is.EqualTo(ExtraArenaRuntime.TerracesNoBossStageId));
        Assert.That(open[3].Name, Is.EqualTo("The Cascade"));
    }

    [Test]
    public void Custom_stage_cursor_appends_extra_box_after_catalog()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        var menu = new MetaMenuShell();
        IReadOnlyList<ExtraArenaRuntime.StageCursorEntry> cursor =
            menu.BuildCustomStageCursor(profile, catalog);

        Assert.That(cursor.Count, Is.EqualTo(catalog.Stages.Length + 4));
        Assert.That(cursor.Count(e => e.IsExtraArenaBox), Is.EqualTo(4));
        Assert.That(cursor.Count(e => e.IsExtraArenaBox && e.Unlocked), Is.EqualTo(0));
        Assert.That(menu.ExtraArenaSlotsUnlocked(profile), Is.EqualTo(0));
    }

    [Test]
    public void Custom_BuildConfig_rejects_locked_extra_arena()
    {
        var profile = PlayerProfile.CreateNew();
        var orch = new CustomModeOrchestrator();
        var request = new ModeRequest
        {
            Mode = ModeId.Custom,
            Seed = 0x4558545241UL,
            ActorCount = 2,
            StageId = ExtraArenaRuntime.CascadeStageId,
        };

        MatchConfig config = orch.BuildConfig(profile, request);
        Assert.That(config.StageId, Is.EqualTo(SampleContentFactory.CitrineBazaarStageId));

        profile.SetFlag(ProfileFlags.OwnedFullCast);
        profile.DistinctTournamentClears = 14;
        UnlockLadderRuntime.SyncUnlocks(profile);
        config = orch.BuildConfig(profile, request);
        Assert.That(config.StageId, Is.EqualTo(ExtraArenaRuntime.CascadeStageId));
    }
}
