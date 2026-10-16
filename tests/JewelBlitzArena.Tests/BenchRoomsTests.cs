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
}
