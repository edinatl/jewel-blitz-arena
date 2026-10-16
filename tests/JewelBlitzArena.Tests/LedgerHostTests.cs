using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;

namespace JewelBlitzArena.Tests;

public sealed class LedgerHostTests
{
    [Test]
    public void OpenLedger_lists_owned_catalog_items_and_pockets()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(1);
        profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
        profile.Ledger.Grant(LedgerInventory.CustomGreyboxTokenItemId);

        IReadOnlyList<LedgerRuntime.LedgerEntry> entries = menu.OpenLedger(profile, catalog);
        Assert.That(profile.HasFlag(ProfileFlags.TouchedLedgerRoom), Is.True);
        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries.Any(e => e.ItemId == SampleContentFactory.SolitaireItemId && e.Pocketable), Is.True);

        Assert.That(menu.TryPocket(profile, catalog, SampleContentFactory.SolitaireItemId), Is.True);
        Assert.That(profile.Pockets.Contains(SampleContentFactory.SolitaireItemId), Is.True);
        Assert.That(menu.TryPocket(profile, catalog, 1), Is.True); // pistol pocketable
        entries = menu.OpenLedger(profile, catalog);
        Assert.That(entries.Count(e => e.IsPocketed), Is.EqualTo(2));
        Assert.That(menu.TryUnpocket(profile, SampleContentFactory.SolitaireItemId), Is.True);
        Assert.That(profile.Pockets.Contains(SampleContentFactory.SolitaireItemId), Is.False);
    }

    [Test]
    public void Pocket_rejects_non_pocketable_and_caps_at_five()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(SampleContentFactory.MaterialScrapItemId);
        Assert.That(
            LedgerRuntime.TryPocket(profile, catalog, SampleContentFactory.MaterialScrapItemId),
            Is.False);

        // Pocket five pocketable starters.
        for (int id = 1; id <= 5; id++)
        {
            profile.Ledger.Grant(id);
            Assert.That(LedgerRuntime.TryPocket(profile, catalog, id), Is.True, $"id {id}");
        }

        profile.Ledger.Grant(6);
        Assert.That(LedgerRuntime.TryPocket(profile, catalog, 6), Is.False);
        Assert.That(profile.Pockets.Count, Is.EqualTo(PocketLoadout.MaxSlots));
    }

    [Test]
    public void Profile_v9_round_trips_pockets()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-led-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var store = new ProfileStore(path);
            PlayerProfile profile = PlayerProfile.CreateNew("Ed");
            profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
            profile.Pockets.TryAdd(SampleContentFactory.SolitaireItemId);
            store.Save(profile);

            PlayerProfile loaded = store.LoadOrCreate();
            Assert.That(loaded.SchemaVersion, Is.EqualTo(ProfileSchema.Version9));
            Assert.That(loaded.Pockets.Contains(SampleContentFactory.SolitaireItemId), Is.True);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void MetaGreyboxFlow_OpenLedger_and_Render()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-ledger-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
            Assert.That(flow.TryPocketItem(SampleContentFactory.SolitaireItemId), Is.True);

            IReadOnlyList<LedgerRuntime.LedgerEntry> entries = flow.OpenLedger();
            Assert.That(entries.Any(e => e.IsPocketed), Is.True);
            string ascii = flow.RenderLedgerRoom();
            Assert.That(ascii, Does.Contain("=== Ledger ==="));
            Assert.That(ascii, Does.Contain("Solitaire"));
            Assert.That(ascii, Does.Contain(" P "));
        }
        finally
        {
            string? dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
