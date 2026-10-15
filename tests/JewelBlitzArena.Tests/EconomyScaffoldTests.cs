using JewelBlitzArena.Tools.Economy;

namespace JewelBlitzArena.Tests;

public sealed class EconomyScaffoldTests
{
    [Test]
    public void Scaffold_run_is_deterministic_for_same_request()
    {
        var sim = new EconomySimulatorScaffold();
        var request = new EconomySimulatorScaffold.RunRequest
        {
            Seed = 42,
            Difficulty = 3,
            StageId = 1,
            Iterations = 10,
        };

        var a = sim.Run(request);
        var b = sim.Run(request);
        Assert.That(a.CaratsTotal, Is.EqualTo(b.CaratsTotal));
        Assert.That(a.Status, Is.EqualTo("scaffold"));
    }
}
