using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;

namespace JewelBlitzArena.Tests;

public sealed class FittingLoadoutTests
{
    [Test]
    public void TryEquipCosmetic_fills_socket_and_replaces()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();

        Assert.That(menu.TryEquipCosmetic(profile, catalog, 56), Is.False, "not owned");
        profile.Ledger.Grant(56); // Felt Cap (Head)
        profile.Ledger.Grant(63); // Gauntlets (Arm)
        profile.Ledger.Grant(SampleContentFactory.RegentsDiademItemId);

        Assert.That(menu.TryEquipCosmetic(profile, catalog, 56), Is.True);
        Assert.That(BenchRoomsRuntime.GetEquipped(profile, CosmeticSocket.Head), Is.EqualTo(56));
        Assert.That(menu.TryEquipCosmetic(profile, catalog, 63), Is.True);
        Assert.That(BenchRoomsRuntime.GetEquipped(profile, CosmeticSocket.Arm), Is.EqualTo(63));

        Assert.That(menu.TryEquipCosmetic(profile, catalog, SampleContentFactory.RegentsDiademItemId), Is.True);
        Assert.That(BenchRoomsRuntime.GetEquipped(profile, CosmeticSocket.Head), Is.EqualTo(SampleContentFactory.RegentsDiademItemId));
        Assert.That(BenchRoomsRuntime.IsDiademEquipped(profile), Is.True);
        Assert.That(profile.HasFlag(ProfileFlags.EquippedDiadem), Is.True);

        Assert.That(menu.TryUnequipSocket(profile, CosmeticSocket.Head), Is.True);
        Assert.That(BenchRoomsRuntime.IsDiademEquipped(profile), Is.False);
        Assert.That(BenchRoomsRuntime.GetEquipped(profile, CosmeticSocket.Arm), Is.EqualTo(63));
    }

    [Test]
    public void Diadem_compat_wrappers_use_Head_socket()
    {
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(CitrineGiftsRuntime.DiademItemId);
        Assert.That(menu.TryEquipDiadem(profile), Is.True);
        Assert.That(BenchRoomsRuntime.GetEquipped(profile, CosmeticSocket.Head), Is.EqualTo(CitrineGiftsRuntime.DiademItemId));
        Assert.That(BenchRoomsRuntime.TryUnequipDiadem(profile), Is.True);
        Assert.That(BenchRoomsRuntime.GetEquipped(profile, CosmeticSocket.Head), Is.EqualTo(0));
    }

    [Test]
    public void Profile_v8_round_trips_Fitting_slots()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-fit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var store = new ProfileStore(path);
            PlayerProfile profile = PlayerProfile.CreateNew("Ed");
            profile.Fitting.Set(CosmeticSocket.Head, 55);
            profile.Fitting.Set(CosmeticSocket.Back, 76);
            store.Save(profile);

            PlayerProfile loaded = store.LoadOrCreate();
            Assert.That(loaded.SchemaVersion, Is.EqualTo(ProfileSchema.Current));
            Assert.That(loaded.Fitting.Get(CosmeticSocket.Head), Is.EqualTo(55));
            Assert.That(loaded.Fitting.Get(CosmeticSocket.Back), Is.EqualTo(76));
            Assert.That(loaded.Fitting.Get(CosmeticSocket.Arm), Is.EqualTo(0));
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
    public void MetaGreyboxFlow_equips_cosmetic_via_host()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-fit-host-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Ledger.Grant(76);
            Assert.That(flow.TryEquipCosmetic(76), Is.True);
            Assert.That(flow.Profile.Fitting.Get(CosmeticSocket.Back), Is.EqualTo(76));
            Assert.That(flow.TryUnequipSocket(CosmeticSocket.Back), Is.True);
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
