using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class ShopRuntimeTests
{
    private static ContentCatalog Catalog() =>
        ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());

    [Test]
    public void BuildStock_has_always_plus_rotating_and_marks_new()
    {
        ContentCatalog catalog = Catalog();
        var profile = PlayerProfile.CreateNew();
        IReadOnlyList<ShopRuntime.StockEntry> stock =
            ShopRuntime.BuildStock(catalog, profile, seed: 0x53544F434BUL);

        Assert.That(stock.Count, Is.GreaterThan(0));
        Assert.That(stock.Count, Is.LessThanOrEqualTo(
            ShopRuntime.AlwaysStockCount + ShopRuntime.RotatingStockCount));
        Assert.That(stock[0].IsNewToProfile, Is.True);

        // Deterministic for same seed.
        IReadOnlyList<ShopRuntime.StockEntry> again =
            ShopRuntime.BuildStock(catalog, profile, seed: 0x53544F434BUL);
        Assert.That(again.Count, Is.EqualTo(stock.Count));
        for (int i = 0; i < stock.Count; i++)
        {
            Assert.That(again[i].ItemId, Is.EqualTo(stock[i].ItemId));
        }
    }

    [Test]
    public void TryBuy_spends_carats_grants_item_and_tracks_lifetime_spend()
    {
        ContentCatalog catalog = Catalog();
        Assert.That(catalog.TryGetItem(1, out ItemRecord pistol), Is.True);
        var profile = PlayerProfile.CreateNew();
        profile.Carats = pistol.BuyPrice + 50;

        Assert.That(ShopRuntime.TryBuy(profile, catalog, pistol.ItemId), Is.True);
        Assert.That(profile.Carats, Is.EqualTo(50));
        Assert.That(profile.CaratsSpentLifetime, Is.EqualTo(pistol.BuyPrice));
        Assert.That(profile.Ledger.Owns(pistol.ItemId), Is.True);

        Assert.That(ShopRuntime.TryBuy(profile, catalog, pistol.ItemId), Is.False,
            "not enough carats for a second buy");
    }

    [Test]
    public void TrySell_pays_sell_price_keeps_ever_owned()
    {
        ContentCatalog catalog = Catalog();
        Assert.That(catalog.TryGetItem(1, out ItemRecord pistol), Is.True);
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(pistol.ItemId);
        profile.Carats = 0;

        Assert.That(ShopRuntime.TrySell(profile, catalog, pistol.ItemId), Is.True);
        Assert.That(profile.Carats, Is.EqualTo(pistol.SellPrice));
        Assert.That(profile.Ledger.Owns(pistol.ItemId), Is.True, "ever-owned retained");
    }

    [Test]
    public void TryBuy_rejects_unsellable_and_broke()
    {
        ContentCatalog catalog = Catalog();
        var profile = PlayerProfile.CreateNew();
        profile.Carats = 0;
        Assert.That(ShopRuntime.TryBuy(profile, catalog, 1), Is.False);

        // Stage weapons are never sold.
        Assert.That(
            ShopRuntime.TryBuy(profile, catalog, SampleContentFactory.StageMortarItemId),
            Is.False);
    }

    [Test]
    public void FacetWheel_widen_counts_toward_CaratsSpentLifetime()
    {
        var profile = PlayerProfile.CreateNew();
        profile.Carats = 2000;
        Assert.That(FacetWheelRuntime.TryWiden(profile), Is.True);
        Assert.That(profile.CaratsSpentLifetime, Is.EqualTo(2000));
    }
}
