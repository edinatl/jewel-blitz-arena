using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class CofferRuntimeTests
{
    [Test]
    public void Coffer_requires_diff8_and_under_par()
    {
        int stage = SampleContentFactory.CitrineBazaarStageId;
        int par = ExpeditionChestRuntime.ParFrames(stage);
        Assert.That(
            ExpeditionChestRuntime.RollCoffer(0xC0FFEEUL, stage, par, difficulty: 8),
            Is.False,
            "at-par clear must not roll coffer");
        Assert.That(
            ExpeditionChestRuntime.RollCoffer(0xC0FFEEUL, stage, par / 2, difficulty: 7),
            Is.False,
            "diff < 8 must not roll coffer");
    }

    [Test]
    public void ResolveChest_can_pay_flat_coffer_value()
    {
        int stage = SampleContentFactory.CitrineBazaarStageId;
        int underPar = ExpeditionChestRuntime.ParFrames(stage) / 2;
        // Brute a seed that hits the 10% coffer band.
        bool found = false;
        for (ulong seed = 1; seed < 5000; seed++)
        {
            if (!ExpeditionChestRuntime.RollCoffer(seed, stage, underPar, 8))
            {
                continue;
            }

            int paid = ExpeditionChestRuntime.ResolveChestCarats(stage, 8, underPar, seed, true);
            Assert.That(paid, Is.EqualTo(ExpeditionChestRuntime.CofferValue));
            found = true;
            break;
        }

        Assert.That(found, Is.True, "expected at least one coffer seed in 1..4999");
    }
}
