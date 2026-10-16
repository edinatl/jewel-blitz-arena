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

    [Test]
    public void RenderTalk_shows_Citrine_status_and_gift_line()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
        profile.PlaytimeSeconds = 7200;
        profile.CaratsSpentLifetime = 20_000;

        string away = BenchRoomAscii.RenderTalk(profile, visitSeed: 1UL, catalog: catalog);
        Assert.That(away, Does.Contain("=== Talk ==="));
        Assert.That(away, Does.Contain("Appear chance:"));
        Assert.That(away, Does.Contain("Solitaire=pending"));

        bool granted = false;
        for (ulong seed = 1; seed < 4000; seed++)
        {
            if (!CitrineGiftsRuntime.IsAtBench(profile, seed))
            {
                continue;
            }

            int gift = CitrineGiftsRuntime.TryGrantGift(profile, seed);
            string ascii = BenchRoomAscii.RenderTalk(profile, seed, gift, catalog);
            Assert.That(ascii, Does.Contain("Citrine is at the bench."));
            if (gift != 0)
            {
                Assert.That(ascii, Does.Contain("Last gift:"));
                Assert.That(ascii, Does.Contain("Solitaire"));
                granted = true;
                break;
            }
        }

        Assert.That(granted, Is.True, "expected a Talk gift within seeds");
    }

    [Test]
    public void RenderTinker_shows_last_craft_outcome()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        TinkerRuntime.EnsureSalt(profile);
        profile.Ledger.Grant(3);
        profile.Ledger.Grant(5);
        TinkerAttemptResult craft = TinkerRuntime.TryCraft(profile, catalog, 3, 5);
        string ascii = BenchRoomAscii.RenderTinker(profile, catalog, craft, 3, 5);
        Assert.That(ascii, Does.Contain("=== Tinker ==="));
        Assert.That(ascii, Does.Contain("Chits:"));
        Assert.That(ascii, Does.Contain("Salt: set"));
        Assert.That(ascii, Does.Contain("Inputs:"));
        Assert.That(ascii, Does.Contain("Last craft:"));
        Assert.That(ascii, Does.Contain(craft.Outcome.ToString()));
    }

    [Test]
    public void MetaGreyboxFlow_RenderTalk_and_Tinker()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-talk-tinker-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
            flow.Profile.PlaytimeSeconds = 3600;
            flow.Profile.CaratsSpentLifetime = 50_000;

            string talk = flow.RenderTalkRoom(visitSeed: 0x54414C4BUL);
            Assert.That(talk, Does.Contain("=== Talk ==="));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedTalkRoom), Is.True);

            string tinker = flow.RenderTinkerRoom(craft: true);
            Assert.That(tinker, Does.Contain("=== Tinker ==="));
            Assert.That(tinker, Does.Contain("Last craft:"));
            Assert.That(flow.Profile.HasFlag(ProfileFlags.TouchedTinkerStub), Is.True);
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
