using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class MaterialsTincturesTests
{
    [Test]
    public void Catalog_has_one_material_and_tincture_per_canonical_tag()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        ValidationResult result = ContentValidator.Validate(pack);
        Assert.That(result.IsOk, Is.True, string.Join("; ", result.Errors));

        ContentCatalog catalog = ContentCompiler.Compile(pack);
        Assert.That(catalog.TryGetItem(SampleContentFactory.MaterialScrapItemId, out ItemRecord scrap), Is.True);
        Assert.That(scrap.Category, Is.EqualTo(ItemCategory.Material));
        Assert.That(scrap.Tags, Is.EqualTo(ItemTag.None));
        Assert.That(scrap.CrateEligibility, Is.EqualTo(CrateEligibility.Never));

        Assert.That(
            catalog.TryGetItem(SampleContentFactory.BlueprintScrapItemId, out ItemRecord bp),
            Is.True);
        Assert.That(bp.Category, Is.EqualTo(ItemCategory.Card));
        Assert.That(bp.Name, Does.Contain("Blueprint"));

        Assert.That(TinkerRuntime.ScrapItemId, Is.EqualTo(SampleContentFactory.MaterialScrapItemId));
    }

    [Test]
    public void Tinker_with_Blueprint_Scrap_allows_tier_up_to_7()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetItem(8, out ItemRecord cutlass), Is.True); // metal r3
        Assert.That(catalog.TryGetItem(5, out ItemRecord pry), Is.True); // metal r2
        ItemTag affinity = TinkerRuntime.AffinitySet(cutlass, pry, ItemTag.None, out bool shared);
        Assert.That(shared, Is.True);
        int without = TinkerRuntime.TargetTier(cutlass, pry, shared, blueprintScrap: false);
        int withBp = TinkerRuntime.TargetTier(cutlass, pry, shared, blueprintScrap: true);
        Assert.That(without, Is.LessThanOrEqualTo(TinkerRuntime.MaxTargetTierWithoutBlueprint));
        Assert.That(withBp, Is.GreaterThanOrEqualTo(without));
        Assert.That(withBp, Is.LessThanOrEqualTo(TinkerRuntime.MaxTargetTier));
        Assert.That(affinity & ItemTag.Metal, Is.EqualTo(ItemTag.Metal));
    }

    [Test]
    public void Tinker_failure_grants_catalog_Scrap()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        for (int counter = 0; counter < 200; counter++)
        {
            var profile = PlayerProfile.CreateNew();
            profile.ProfileSalt = 0x5343524150UL;
            profile.TinkerCounter = counter;
            TinkerAttemptResult r = TinkerRuntime.TryCraft(profile, catalog, 3, 5, 0);
            if (r.Outcome != TinkerOutcome.Failure)
            {
                continue;
            }

            Assert.That(profile.Ledger.Owns(SampleContentFactory.MaterialScrapItemId), Is.True);
            return;
        }

        Assert.Fail("expected a failure craft");
    }
}
