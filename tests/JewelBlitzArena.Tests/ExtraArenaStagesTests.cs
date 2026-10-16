using JewelBlitzArena.Data;

namespace JewelBlitzArena.Tests;

public sealed class ExtraArenaStagesTests
{
    [Test]
    public void S5_stub_includes_four_extra_arena_graphs()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetStage(SampleContentFactory.TerracesNoBossStageId, out StageRecord terraces), Is.True);
        Assert.That(terraces.Name, Does.Contain("Terraces"));
        Assert.That(terraces.FallIsKo, Is.False);
        Assert.That(terraces.Phases.Length, Is.EqualTo(1));
        Assert.That(terraces.Phases[0].Hazards.Length, Is.GreaterThanOrEqualTo(4), "spine crystals");

        Assert.That(catalog.TryGetStage(SampleContentFactory.BehindCounterStageId, out StageRecord counter), Is.True);
        Assert.That(counter.Name, Does.Contain("Counter"));

        Assert.That(catalog.TryGetStage(SampleContentFactory.SpirewalkStageId, out StageRecord spire), Is.True);
        Assert.That(spire.FallIsKo, Is.True);

        Assert.That(catalog.TryGetStage(SampleContentFactory.CascadeStageId, out StageRecord cascade), Is.True);
        Assert.That(cascade.FallIsKo, Is.True);
        Assert.That(cascade.Phases[0].WaterVolumes.Length, Is.GreaterThanOrEqualTo(1));
    }
}
