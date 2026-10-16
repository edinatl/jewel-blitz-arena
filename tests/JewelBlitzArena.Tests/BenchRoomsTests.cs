using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class BenchRoomsTests
{
    [Test]
    public void BuildRooms_lists_seven_rooms_and_Talk_badge_when_Citrine_present()
    {
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        IReadOnlyList<BenchRoomsRuntime.BenchRoomEntry> rooms =
            menu.BuildBenchRooms(profile, visitSeed: 1UL);
        Assert.That(rooms.Count, Is.EqualTo(7));
        Assert.That(rooms.Any(r => r.Room == BenchRoom.FittingRoom), Is.True);
        Assert.That(rooms.First(r => r.Room == BenchRoom.Talk).Badge, Is.Null);

        profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
        profile.PlaytimeSeconds = 60 * 60 * 20;
        profile.CaratsSpentLifetime = 200_000;

        string? badge = null;
        for (ulong seed = 1; seed < 50_000; seed++)
        {
            rooms = menu.BuildBenchRooms(profile, seed);
            badge = rooms.First(r => r.Room == BenchRoom.Talk).Badge;
            if (badge != null)
            {
                break;
            }
        }

        Assert.That(badge, Is.EqualTo("Citrine is at the bench"));
    }

    [Test]
    public void OpenTalk_grants_gift_when_Citrine_at_bench()
    {
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
        profile.PlaytimeSeconds = 60 * 60 * 20;
        profile.CaratsSpentLifetime = 200_000;

        int granted = 0;
        for (ulong seed = 1; seed < 50_000; seed++)
        {
            var p = PlayerProfile.CreateNew();
            p.SetFlag(ProfileFlags.UnlockedCaptainCitrine);
            p.PlaytimeSeconds = profile.PlaytimeSeconds;
            p.CaratsSpentLifetime = profile.CaratsSpentLifetime;
            granted = menu.OpenTalk(p, seed);
            if (granted != 0)
            {
                Assert.That(granted, Is.EqualTo(CitrineGiftsRuntime.SolitaireItemId));
                Assert.That(p.HasFlag(ProfileFlags.TouchedTalkRoom), Is.True);
                Assert.That(p.Ledger.Owns(CitrineGiftsRuntime.SolitaireItemId), Is.True);
                return;
            }
        }

        Assert.Fail("expected a Talk gift within seeds");
    }

    [Test]
    public void FittingRoom_equips_Diadem_when_owned()
    {
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        Assert.That(menu.TryEquipDiadem(profile), Is.False);
        Assert.That(profile.HasFlag(ProfileFlags.TouchedFittingRoom), Is.True);

        profile.Ledger.Grant(CitrineGiftsRuntime.DiademItemId);
        Assert.That(menu.TryEquipDiadem(profile), Is.True);
        Assert.That(BenchRoomsRuntime.IsDiademEquipped(profile), Is.True);
        Assert.That(BenchRoomsRuntime.TryUnequipDiadem(profile), Is.True);
        Assert.That(BenchRoomsRuntime.IsDiademEquipped(profile), Is.False);
    }

    [Test]
    public void OpenBuy_binds_stock_to_BuildStock()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        profile.ShopStockSeed = 0x42555953544F434BUL;
        profile.Carats = 10_000;

        IReadOnlyList<ShopRuntime.StockEntry> viaRoom = menu.OpenBuy(profile, catalog);
        IReadOnlyList<ShopRuntime.StockEntry> viaShop = ShopRuntime.BuildStock(catalog, profile);
        Assert.That(profile.HasFlag(ProfileFlags.TouchedBuyRoom), Is.True);
        Assert.That(viaRoom.Count, Is.EqualTo(viaShop.Count));
        Assert.That(viaRoom.Count, Is.EqualTo(
            ShopRuntime.AlwaysStockCount + ShopRuntime.RotatingStockCount));
        for (int i = 0; i < viaRoom.Count; i++)
        {
            Assert.That(viaRoom[i].ItemId, Is.EqualTo(viaShop[i].ItemId));
            Assert.That(viaRoom[i].BuyPrice, Is.EqualTo(viaShop[i].BuyPrice));
        }

        int first = viaRoom[0].ItemId;
        Assert.That(menu.TryBuy(profile, catalog, first), Is.True);
        Assert.That(profile.Ledger.Owns(first), Is.True);
    }
}
