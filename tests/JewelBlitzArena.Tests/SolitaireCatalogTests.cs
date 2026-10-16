using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class SolitaireCatalogTests
{
    [Test]
    public void S5_stub_has_Solitaire_Gift_pocketable_never()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(
            catalog.TryGetItem(SampleContentFactory.SolitaireItemId, out ItemRecord solitaire),
            Is.True);
        Assert.That(solitaire.Name, Is.EqualTo("Solitaire"));
        Assert.That(solitaire.Category, Is.EqualTo(ItemCategory.Gift));
        Assert.That(solitaire.CrateEligibility, Is.EqualTo(CrateEligibility.Never));
        Assert.That(solitaire.Pocketable, Is.True);
        Assert.That(solitaire.Rarity, Is.EqualTo(7));
        Assert.That(solitaire.BuyPrice, Is.EqualTo(0));
        Assert.That(CitrineGiftsRuntime.SolitaireItemId, Is.EqualTo(SampleContentFactory.SolitaireItemId));
        Assert.That(ShopRuntime.IsSoldByTilly(solitaire), Is.False);
    }
}
