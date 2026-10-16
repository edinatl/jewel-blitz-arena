using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class CosmeticsCatalogTests
{
    [Test]
    public void S5_stub_has_25_cosmetics_with_sockets_including_Diadem()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        ValidationResult result = ContentValidator.Validate(pack);
        Assert.That(result.IsOk, Is.True, string.Join("; ", result.Errors));

        ContentCatalog catalog = ContentCompiler.Compile(pack);
        int cosmetics = 0;
        foreach (ItemRecord item in catalog.Items)
        {
            if (item.Category != ItemCategory.Cosmetic)
            {
                continue;
            }

            cosmetics++;
            Assert.That(item.CosmeticSocket, Is.Not.EqualTo(CosmeticSocket.None), item.Name);
            Assert.That(item.CrateEligibility, Is.EqualTo(CrateEligibility.Never), item.Name);
            Assert.That(item.BuyPrice, Is.EqualTo(0), item.Name);
            Assert.That(
                item.ItemId,
                Is.InRange(SampleContentFactory.CosmeticItemIdMin, SampleContentFactory.CosmeticItemIdMax));
        }

        Assert.That(cosmetics, Is.EqualTo(SampleContentFactory.CosmeticCount));
        Assert.That(
            catalog.TryGetItem(SampleContentFactory.RegentsDiademItemId, out ItemRecord diadem),
            Is.True);
        Assert.That(diadem.Name, Is.EqualTo("Regent's Diadem"));
        Assert.That(diadem.CosmeticSocket, Is.EqualTo(CosmeticSocket.Head));
        Assert.That(diadem.Rarity, Is.EqualTo(7));
        Assert.That(diadem.SellPrice, Is.EqualTo(CitrineGiftsRuntime.DiademSellCarats));
        Assert.That(CitrineGiftsRuntime.DiademItemId, Is.EqualTo(SampleContentFactory.RegentsDiademItemId));
    }

    [Test]
    public void Blob_round_trip_preserves_cosmetic_socket()
    {
        ContentCatalog compiled = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        ContentCatalog loaded = ContentBlobReader.Read(ContentBlobWriter.Write(compiled));
        Assert.That(loaded.ContentHash, Is.EqualTo(compiled.ContentHash));
        Assert.That(loaded.TryGetItem(56, out ItemRecord cap), Is.True);
        Assert.That(cap.CosmeticSocket, Is.EqualTo(CosmeticSocket.Head));
        Assert.That(loaded.TryGetItem(76, out ItemRecord wings), Is.True);
        Assert.That(wings.CosmeticSocket, Is.EqualTo(CosmeticSocket.Back));
    }

    [Test]
    public void Validator_rejects_cosmetic_without_socket()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        AuthoredItem diadem = pack.Items.Find(i => i.ItemId == SampleContentFactory.RegentsDiademItemId)!;
        diadem.CosmeticSocket = string.Empty;
        ValidationResult result = ContentValidator.Validate(pack);
        Assert.That(result.IsOk, Is.False);
        Assert.That(result.Errors.Exists(e => e.Code == "cosmetic_socket"), Is.True);
    }
}
