using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;

namespace JewelBlitzArena.Tests;

public sealed class BenchRoomAsciiTests
{
    [Test]
    public void RenderBuy_lists_BuildStock_names_and_prices()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        profile.ShopStockSeed = 0x415343494942UL;
        profile.Carats = 1234;
        IReadOnlyList<ShopRuntime.StockEntry> stock = ShopRuntime.BuildStock(catalog, profile);

        string ascii = BenchRoomAscii.RenderBuy(stock, catalog, profile);
        Assert.That(ascii, Does.Contain("=== Buy ==="));
        Assert.That(ascii, Does.Contain("Carats: 1234"));
        Assert.That(ascii, Does.Contain("NEW"));
        Assert.That(catalog.TryGetItem(stock[0].ItemId, out ItemRecord first), Is.True);
        Assert.That(ascii, Does.Contain(first.Name));
        Assert.That(ascii, Does.Contain($"{stock[0].BuyPrice}c"));
    }

    [Test]
    public void RenderFacetWheel_shows_all_band_weights()
    {
        var profile = PlayerProfile.CreateNew();
        profile.Chits = 9;
        profile.FacetWheelWidthLevel = 2;
        string ascii = BenchRoomAscii.RenderFacetWheel(profile);
        Assert.That(ascii, Does.Contain("=== Facet Wheel ==="));
        Assert.That(ascii, Does.Contain("width=2/5"));
        Assert.That(ascii, Does.Contain("Tier 1-2"));
        Assert.That(ascii, Does.Contain("Cosmetic"));
        Assert.That(ascii, Does.Contain("Nothing"));
        Assert.That(ascii, Does.Contain("‰"));
    }

    [Test]
    public void MetaGreyboxFlow_RenderBuyRoom_uses_host_stock_bind()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-bench-ascii-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.ShopStockSeed = 0x52454E444552UL;
            flow.Profile.Carats = 2_000;

            string buy = flow.RenderBuyRoom();
            Assert.That(buy, Does.Contain("=== Buy ==="));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedBuyRoom), Is.True);

            string wheel = flow.RenderFacetWheelRoom();
            Assert.That(wheel, Does.Contain("=== Facet Wheel ==="));

            string rooms = flow.RenderBenchRooms();
            Assert.That(rooms, Does.Contain("Fitting Room"));
            Assert.That(rooms, Does.Contain("Facet Wheel"));
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
