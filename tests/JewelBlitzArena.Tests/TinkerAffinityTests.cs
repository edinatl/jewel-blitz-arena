using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class TinkerAffinityTests
{
    private static ContentCatalog Catalog() =>
        ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());

    [Test]
    public void Starter_items_have_canonical_tags()
    {
        ContentCatalog catalog = Catalog();
        Assert.That(catalog.SchemaVersion, Is.EqualTo(11u));
        Assert.That(catalog.TryGetItem(1, out ItemRecord pistol), Is.True);
        Assert.That(pistol.Tags & ItemTag.Powder, Is.EqualTo(ItemTag.Powder));
        Assert.That(pistol.Tags & ItemTag.Metal, Is.EqualTo(ItemTag.Metal));
        Assert.That(catalog.TryGetItem(13, out ItemRecord apple), Is.True);
        Assert.That(apple.Tags, Is.EqualTo(ItemTag.Food));
    }

    [Test]
    public void Affinity_pool_intersects_shared_tags_at_tier()
    {
        ContentCatalog catalog = Catalog();
        Assert.That(catalog.TryGetItem(3, out ItemRecord ironPipe), Is.True); // metal
        Assert.That(catalog.TryGetItem(5, out ItemRecord pryBar), Is.True); // metal
        ItemTag affinity = TinkerRuntime.AffinitySet(ironPipe, pryBar, ItemTag.None, out bool shared);
        Assert.That(shared, Is.True);
        Assert.That(affinity & ItemTag.Metal, Is.EqualTo(ItemTag.Metal));

        var ids = new List<int>();
        var weights = new List<int>();
        TinkerRuntime.BuildPool(catalog, affinity, rarity: 1, ids, weights);
        Assert.That(ids.Count, Is.GreaterThan(0));
        Assert.That(ids, Does.Contain(ironPipe.ItemId));
    }

    [Test]
    public void Catalog_craft_success_grants_pool_item_not_stub_id()
    {
        ContentCatalog catalog = Catalog();
        // Hunt a success craft with metal×metal inputs.
        for (int counter = 0; counter < 400; counter++)
        {
            var profile = PlayerProfile.CreateNew();
            profile.ProfileSalt = 0x414646504F4F4CUL;
            profile.TinkerCounter = counter;
            TinkerAttemptResult r = TinkerRuntime.TryCraft(profile, catalog, 3, 5, 0);
            if (r.Outcome == TinkerOutcome.Failure)
            {
                continue;
            }

            Assert.That(r.ResultItemId, Is.GreaterThan(0));
            Assert.That(r.ResultItemId, Is.LessThan(TinkerRuntime.ResultItemBaseId),
                "catalog path must not use stub 930xxx ids");
            Assert.That(catalog.TryGetItem(r.ResultItemId, out ItemRecord granted), Is.True);
            Assert.That((granted.Tags & ItemTag.Metal), Is.Not.EqualTo(ItemTag.None));
            Assert.That(profile.Ledger.Owns(r.ResultItemId), Is.True);
            return;
        }

        Assert.Fail("expected a success craft within 400 counters");
    }

    [Test]
    public void Empty_affinity_forces_failure_even_on_success_roll()
    {
        // Stage weapons have no tags and are pool-ineligible — craft should fail.
        ContentCatalog catalog = Catalog();
        var profile = PlayerProfile.CreateNew();
        profile.ProfileSalt = 1UL;
        // Force many attempts; all should fail (no affinity).
        for (int i = 0; i < 20; i++)
        {
            TinkerAttemptResult r = TinkerRuntime.TryCraft(
                profile,
                catalog,
                SampleContentFactory.StageMortarItemId,
                SampleContentFactory.StageNetItemId,
                0);
            Assert.That(r.Outcome, Is.EqualTo(TinkerOutcome.Failure));
            Assert.That(r.ScrapGranted, Is.True);
        }
    }

    [Test]
    public void Content_blob_round_trips_tags()
    {
        ContentCatalog compiled = Catalog();
        byte[] blob = ContentBlobWriter.Write(compiled);
        ContentCatalog loaded = ContentBlobReader.Read(blob);
        Assert.That(loaded.SchemaVersion, Is.EqualTo(ContentSchema.Version));
        Assert.That(loaded.TryGetItem(21, out ItemRecord iceJar), Is.True);
        Assert.That(iceJar.Tags, Is.EqualTo(ItemTag.Ice));
        Assert.That(loaded.ContentHash, Is.EqualTo(compiled.ContentHash));
    }
}
