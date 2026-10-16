using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class BenchNotesTests
{
    [Test]
    public void Craft_records_pair_outcomes_order_independent()
    {
        var profile = PlayerProfile.CreateNew();
        profile.ProfileSalt = 0x4E4F54455331UL;
        TinkerAttemptResult r = TinkerRuntime.TryCraft(profile, 3, 5, 0);
        Assert.That(profile.BenchNotes.Count, Is.EqualTo(1));
        Assert.That(profile.BenchNotes.HasSeen(5, 3, r.Outcome), Is.True);
        Assert.That(profile.BenchNotes.HasSeen(3, 5, r.Outcome), Is.True);
    }

    [Test]
    public void Logbook_reveals_pool_and_is_consumed()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        int logbook = BenchNotesRuntime.LogbookItemIdMin;
        profile.Ledger.Grant(logbook);

        Assert.That(BenchNotesRuntime.TryGiveLogbook(profile, logbook, 3, 5), Is.True);
        Assert.That(profile.Ledger.Owns(logbook), Is.False);
        Assert.That(profile.BenchNotes.IsPoolRevealed(3, 5), Is.True);
        Assert.That(BenchNotesRuntime.TryGiveLogbook(profile, logbook, 3, 5), Is.False);

        Assert.That(catalog.TryGetItem(3, out ItemRecord a), Is.True);
        Assert.That(catalog.TryGetItem(5, out ItemRecord b), Is.True);
        ItemTag affinity = TinkerRuntime.AffinitySet(a, b, ItemTag.None, out bool shared);
        int tier = TinkerRuntime.TargetTier(a, b, shared, blueprintScrap: false);
        Assert.That(affinity, Is.Not.EqualTo(ItemTag.None));
        int[] pool = BenchNotesRuntime.RevealedPoolItemIds(profile, catalog, 3, 5, tier);
        Assert.That(pool.Length, Is.GreaterThan(0));
    }

    [Test]
    public void Profile_v6_round_trips_bench_notes()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-notes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var store = new ProfileStore(path);
            PlayerProfile profile = PlayerProfile.CreateNew("Ed");
            profile.BenchNotes.RecordAttempt(1, 2, TinkerOutcome.Success);
            profile.BenchNotes.RevealPool(1, 2);
            store.Save(profile);

            PlayerProfile loaded = store.LoadOrCreate();
            Assert.That(loaded.SchemaVersion, Is.EqualTo(ProfileSchema.Version6));
            Assert.That(loaded.BenchNotes.HasSeen(1, 2, TinkerOutcome.Success), Is.True);
            Assert.That(loaded.BenchNotes.IsPoolRevealed(2, 1), Is.True);
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
