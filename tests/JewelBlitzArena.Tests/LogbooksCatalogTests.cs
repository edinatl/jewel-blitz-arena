using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class LogbooksCatalogTests
{
    [Test]
    public void S5_stub_has_20_Logbook_cards_in_catalog()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(BenchNotesRuntime.LogbookItemIdMin, Is.EqualTo(SampleContentFactory.LogbookItemIdMin));
        Assert.That(BenchNotesRuntime.LogbookCount, Is.EqualTo(20));

        int count = 0;
        for (int id = SampleContentFactory.LogbookItemIdMin; id <= SampleContentFactory.LogbookItemIdMax; id++)
        {
            Assert.That(catalog.TryGetItem(id, out ItemRecord item), Is.True, $"missing Logbook id {id}");
            Assert.That(item.Category, Is.EqualTo(ItemCategory.Card));
            Assert.That(item.CrateEligibility, Is.EqualTo(CrateEligibility.Never));
            Assert.That(item.Name, Does.StartWith("Logbook"));
            Assert.That(BenchNotesRuntime.IsLogbookItemId(id), Is.True);
            count++;
        }

        Assert.That(count, Is.EqualTo(SampleContentFactory.LogbookCount));
        Assert.That(BenchNotesRuntime.IsLogbookItemId(940001), Is.False);
    }

    [Test]
    public void Give_catalog_Logbook_still_reveals_Tinker_pool()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        int logbook = SampleContentFactory.LogbookItemIdMin;
        Assert.That(catalog.TryGetItem(logbook, out _), Is.True);
        profile.Ledger.Grant(logbook);

        Assert.That(BenchNotesRuntime.TryGiveLogbook(profile, logbook, 3, 5), Is.True);
        Assert.That(profile.Ledger.Owns(logbook), Is.False);
        Assert.That(profile.BenchNotes.IsPoolRevealed(3, 5), Is.True);
    }
}
