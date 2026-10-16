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

    [Test]
    public void RenderSell_and_Fitting_and_Wheel_grant_line()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        profile.Carats = 10;
        profile.Ledger.Grant(1);
        profile.Ledger.Grant(SampleContentFactory.RegentsDiademItemId);
        profile.Fitting.Set(CosmeticSocket.Head, SampleContentFactory.RegentsDiademItemId);

        IReadOnlyList<ShopRuntime.SellEntry> sell = ShopRuntime.BuildSellList(catalog, profile);
        string sellAscii = BenchRoomAscii.RenderSell(sell, catalog, profile);
        Assert.That(sellAscii, Does.Contain("=== Sell ==="));
        Assert.That(sellAscii, Does.Contain("Pistol"));

        string fit = BenchRoomAscii.RenderFitting(profile, catalog);
        Assert.That(fit, Does.Contain("=== Fitting Room ==="));
        Assert.That(fit, Does.Contain("Head"));
        Assert.That(fit, Does.Contain("Regent's Diadem"));
        Assert.That(fit, Does.Contain("*"));

        var grant = new FacetWheelRuntime.SpinGrant(FacetWheelBand.Materials, 0, SampleContentFactory.MaterialScrapItemId);
        string wheel = BenchRoomAscii.RenderFacetWheel(profile, grant);
        Assert.That(wheel, Does.Contain("Last spin:"));
        Assert.That(wheel, Does.Contain($"#{SampleContentFactory.MaterialScrapItemId}"));
    }

    [Test]
    public void MetaGreyboxFlow_RenderSell_Fitting_Wheel_spin()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-bench-polish-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Ledger.Grant(3);
            flow.Profile.Ledger.Grant(76);
            flow.Profile.Chits = FacetWheelRuntime.SpinCostChits;

            string sell = flow.RenderSellRoom();
            Assert.That(sell, Does.Contain("=== Sell ==="));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedSellRoom), Is.True);

            flow.TryEquipCosmetic(76);
            string fit = flow.RenderFittingRoom();
            Assert.That(fit, Does.Contain("Small Wings"));
            Assert.That(fit, Does.Contain("Back"));

            string wheel = flow.RenderFacetWheelRoom(spinSeed: 0x504F4C495348UL);
            Assert.That(wheel, Does.Contain("Last spin:"));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedFacetWheel), Is.True);
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
