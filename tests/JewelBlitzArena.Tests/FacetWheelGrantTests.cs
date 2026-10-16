using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;

namespace JewelBlitzArena.Tests;

public sealed class FacetWheelGrantTests
{
    [Test]
    public void TrySpin_with_catalog_grants_material_or_item_deterministically()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        FacetWheelRuntime.SpinGrant? seen = null;
        for (ulong seed = 1; seed < 50_000; seed++)
        {
            var profile = PlayerProfile.CreateNew();
            profile.Chits = FacetWheelRuntime.SpinCostChits;
            Assert.That(
                FacetWheelRuntime.TrySpin(profile, catalog, seed, out FacetWheelRuntime.SpinGrant grant),
                Is.True);
            if (grant.Band == FacetWheelBand.Materials && grant.GrantedItemId != 0)
            {
                seen = grant;
                Assert.That(catalog.TryGetItem(grant.GrantedItemId, out ItemRecord mat), Is.True);
                Assert.That(mat.Category, Is.EqualTo(ItemCategory.Material));
                Assert.That(profile.Ledger.Owns(grant.GrantedItemId), Is.True);

                var again = PlayerProfile.CreateNew();
                again.Chits = FacetWheelRuntime.SpinCostChits;
                Assert.That(FacetWheelRuntime.TrySpin(again, catalog, seed, out FacetWheelRuntime.SpinGrant g2), Is.True);
                Assert.That(g2.Band, Is.EqualTo(grant.Band));
                Assert.That(g2.GrantedItemId, Is.EqualTo(grant.GrantedItemId));
                break;
            }
        }

        Assert.That(seen.HasValue, Is.True, "expected a Materials grant within seeds");
    }

    [Test]
    public void TrySpin_cosmetic_duplicate_pays_sell_price()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        for (ulong seed = 1; seed < 80_000; seed++)
        {
            var probe = PlayerProfile.CreateNew();
            probe.Chits = FacetWheelRuntime.SpinCostChits;
            if (!FacetWheelRuntime.TrySpin(probe, catalog, seed, out FacetWheelRuntime.SpinGrant first))
            {
                continue;
            }

            if (first.Band != FacetWheelBand.Cosmetic || first.GrantedItemId == 0)
            {
                continue;
            }

            var owned = PlayerProfile.CreateNew();
            owned.Chits = FacetWheelRuntime.SpinCostChits;
            owned.Ledger.Grant(first.GrantedItemId);
            int caratsBefore = owned.Carats;
            Assert.That(FacetWheelRuntime.TrySpin(owned, catalog, seed, out FacetWheelRuntime.SpinGrant dup), Is.True);
            Assert.That(dup.Band, Is.EqualTo(FacetWheelBand.Cosmetic));
            Assert.That(dup.GrantedItemId, Is.EqualTo(0));
            Assert.That(catalog.TryGetItem(first.GrantedItemId, out ItemRecord cos), Is.True);
            Assert.That(dup.CaratsReward, Is.EqualTo(cos.SellPrice));
            Assert.That(owned.Carats, Is.EqualTo(caratsBefore + cos.SellPrice));
            return;
        }

        Assert.Fail("expected a Cosmetic grant seed");
    }

    [Test]
    public void MetaGreyboxFlow_spin_grants_via_catalog()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-wheel-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Chits = FacetWheelRuntime.SpinCostChits * 200;

            bool granted = false;
            for (ulong seed = 1; seed < 20_000 && flow.Profile.Chits >= FacetWheelRuntime.SpinCostChits; seed++)
            {
                Assert.That(flow.TrySpinFacetWheel(seed, out FacetWheelRuntime.SpinGrant grant), Is.True);
                if (grant.GrantedItemId != 0)
                {
                    Assert.That(flow.Profile.Ledger.Owns(grant.GrantedItemId), Is.True);
                    granted = true;
                    break;
                }
            }

            Assert.That(granted, Is.True);
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
