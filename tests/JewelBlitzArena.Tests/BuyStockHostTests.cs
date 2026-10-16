using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;

namespace JewelBlitzArena.Tests;

public sealed class BuyStockHostTests
{
    [Test]
    public void MetaGreyboxFlow_OpenBuyStock_matches_BuildStock()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-buy-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.ShopStockSeed = 0x484F5354425559UL;
            flow.Profile.Carats = 5_000;

            IReadOnlyList<ShopRuntime.StockEntry> host = flow.OpenBuyStock();
            IReadOnlyList<ShopRuntime.StockEntry> expected =
                ShopRuntime.BuildStock(catalog, flow.Profile);

            Assert.That(host.Count, Is.EqualTo(expected.Count));
            Assert.That(host.Count, Is.GreaterThan(0));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedBuyRoom), Is.True);
            for (int i = 0; i < host.Count; i++)
            {
                Assert.That(host[i].ItemId, Is.EqualTo(expected[i].ItemId));
            }

            int itemId = host[0].ItemId;
            Assert.That(flow.TryBuyStockItem(itemId), Is.True);
            Assert.That(flow.Profile.Ledger.Owns(itemId), Is.True);
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

    [Test]
    public void MetaGreyboxFlow_OpenBuyStock_empty_without_catalog()
    {
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-buy-nocat-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.BootProfile();
            Assert.That(flow.OpenBuyStock(), Is.Empty);
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
