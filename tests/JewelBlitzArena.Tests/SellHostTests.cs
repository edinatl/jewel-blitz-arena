using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;

namespace JewelBlitzArena.Tests;

public sealed class SellHostTests
{
    [Test]
    public void OpenSell_binds_list_to_BuildSellList()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(1);
        profile.Ledger.Grant(SampleContentFactory.MaterialScrapItemId);
        profile.Ledger.Grant(LedgerInventory.CustomGreyboxTokenItemId); // not in catalog

        IReadOnlyList<ShopRuntime.SellEntry> viaRoom = menu.OpenSell(profile, catalog);
        IReadOnlyList<ShopRuntime.SellEntry> viaShop = ShopRuntime.BuildSellList(catalog, profile);
        Assert.That(profile.HasFlag(ProfileFlags.TouchedSellRoom), Is.True);
        Assert.That(viaRoom.Count, Is.EqualTo(viaShop.Count));
        Assert.That(viaRoom.Count, Is.EqualTo(2));
        Assert.That(viaRoom[0].ItemId, Is.EqualTo(1));
        Assert.That(viaRoom[1].ItemId, Is.EqualTo(SampleContentFactory.MaterialScrapItemId));

        int before = profile.Carats;
        Assert.That(menu.TrySell(profile, catalog, 1), Is.True);
        Assert.That(profile.Carats, Is.GreaterThan(before));
        Assert.That(profile.Ledger.Owns(1), Is.True, "ever-owned retained");
    }

    [Test]
    public void MetaGreyboxFlow_OpenSellList_matches_BuildSellList()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-sell-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Ledger.Grant(3);
            flow.Profile.Ledger.Grant(5);

            IReadOnlyList<ShopRuntime.SellEntry> host = flow.OpenSellList();
            IReadOnlyList<ShopRuntime.SellEntry> expected =
                ShopRuntime.BuildSellList(catalog, flow.Profile);
            Assert.That(host.Count, Is.EqualTo(expected.Count));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedSellRoom), Is.True);
            for (int i = 0; i < host.Count; i++)
            {
                Assert.That(host[i].ItemId, Is.EqualTo(expected[i].ItemId));
                Assert.That(host[i].SellPrice, Is.EqualTo(expected[i].SellPrice));
            }

            int caratsBefore = flow.Profile.Carats;
            Assert.That(flow.TrySellOwnedItem(host[0].ItemId), Is.True);
            Assert.That(flow.Profile.Carats, Is.EqualTo(caratsBefore + host[0].SellPrice));
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
