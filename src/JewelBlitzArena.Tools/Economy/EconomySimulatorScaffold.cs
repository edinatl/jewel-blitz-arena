namespace JewelBlitzArena.Tools.Economy;

/// <summary>
/// Scaffold for the M2 economy simulator (Expedition carat curves, Tinker, Facet Wheel).
/// S8 only reserves the API surface; no balance fidelity yet.
/// </summary>
public sealed class EconomySimulatorScaffold
{
    public sealed class RunRequest
    {
        public int Difficulty { get; init; } = 1;
        public int StageId { get; init; }
        public int Iterations { get; init; } = 1;
        public ulong Seed { get; init; }
    }

    public sealed class RunReport
    {
        public int Iterations { get; init; }
        public int CaratsTotal { get; init; }
        public double CaratsPerMinute { get; init; }
        public string Status { get; init; } = "scaffold";
        public string Notes { get; init; } =
            "Economy simulator scaffold (S8). Full fidelity lands at M2 with S5 content + Expedition loop.";
    }

    public RunReport Run(RunRequest request)
    {
        if (request.Iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Iterations must be >= 1.");
        }

        // Deterministic placeholder so CI can call the tool without inventing economy rules.
        int carats = (int)((request.Seed ^ (ulong)request.Difficulty ^ (ulong)request.StageId) % 100UL)
            * request.Iterations;

        return new RunReport
        {
            Iterations = request.Iterations,
            CaratsTotal = carats,
            CaratsPerMinute = 0,
            Status = "scaffold",
        };
    }
}
