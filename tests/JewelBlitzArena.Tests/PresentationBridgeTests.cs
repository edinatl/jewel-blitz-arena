using JewelBlitzArena.Data;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class PresentationBridgeTests
{
    private const ulong Seed = 0x53335F484F5354UL; // S3_HOST

    [Test]
    public void InputQuantizer_neutral_deadzone()
    {
        InputFrame frame = InputQuantizer.FromAxes(0.1f, 0.1f);
        Assert.That(frame.Magnitude, Is.EqualTo(0));
        Assert.That(frame.Direction, Is.EqualTo(0));
        Assert.That(frame.Bits & 0xFF, Is.EqualTo(0));
    }

    [Test]
    public void InputQuantizer_full_east_is_direction_0()
    {
        InputFrame frame = InputQuantizer.FromAxes(1f, 0f);
        Assert.That(frame.Magnitude, Is.EqualTo(3));
        Assert.That(frame.Direction, Is.EqualTo(0));
    }

    [Test]
    public void InputQuantizer_full_north_is_direction_8()
    {
        InputFrame frame = InputQuantizer.FromAxes(0f, 1f);
        Assert.That(frame.Magnitude, Is.EqualTo(3));
        Assert.That(frame.Direction, Is.EqualTo(8));
    }

    [Test]
    public void InputQuantizer_magnitude_bands()
    {
        Assert.That(InputQuantizer.QuantizeMagnitude(0.24f), Is.EqualTo(0));
        Assert.That(InputQuantizer.QuantizeMagnitude(0.30f), Is.EqualTo(1));
        Assert.That(InputQuantizer.QuantizeMagnitude(0.60f), Is.EqualTo(2));
        Assert.That(InputQuantizer.QuantizeMagnitude(0.90f), Is.EqualTo(3));
    }

    [Test]
    public void InputQuantizer_buttons_pack_into_bits()
    {
        InputFrame frame = InputQuantizer.FromAxes(0f, 0f, jump: true, attack: true, action: true);
        Assert.That(frame.Jump, Is.True);
        Assert.That(frame.Attack, Is.True);
        Assert.That(frame.Action, Is.True);
    }

    [Test]
    public void FixDisplay_one_is_1()
    {
        Assert.That(FixDisplay.ToFloat(Fix.One), Is.EqualTo(1f).Within(1e-6f));
        Assert.That(FixDisplay.ToFloat(Fix.FromInt(3)), Is.EqualTo(3f).Within(1e-6f));
    }

    [Test]
    public void MatchSession_scripted_walk_restart_matches_hash()
    {
        var session = new MatchSession();
        session.ResetDefault(Seed, actorCount: 2);

        const int frames = 120;
        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = InputQuantizer.FromCardinal(1, 0);
            InputFrame p1 = InputQuantizer.FromCardinal(-1, 0);
            session.Step(p0, p1);
        }

        ulong hash = session.Hash();
        Assert.That(hash, Is.Not.EqualTo(0UL));

        var again = new MatchSession();
        again.ResetDefault(Seed, actorCount: 2);
        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = InputQuantizer.FromCardinal(1, 0);
            InputFrame p1 = InputQuantizer.FromCardinal(-1, 0);
            again.Step(p0, p1);
        }

        Assert.That(again.Hash(), Is.EqualTo(hash));

        // Spawn is P0 at x=-1, P1 at x=+1; +X / −X walk moves both toward center.
        float x0 = FixDisplay.ToFloat(session.Current.Actor0.PosX);
        float x1 = FixDisplay.ToFloat(session.Current.Actor1.PosX);
        Assert.That(x0, Is.GreaterThan(-1f));
        Assert.That(x1, Is.LessThan(1f));
        Assert.That(x0, Is.Not.EqualTo(x1));
    }

    [Test]
    public void GreyboxBoard_renders_actors()
    {
        var session = new MatchSession();
        session.ResetDefault(Seed, actorCount: 2);
        for (int f = 0; f < 30; f++)
        {
            session.Step(InputQuantizer.FromCardinal(1, 0), InputQuantizer.FromCardinal(-1, 0));
        }

        MatchState state = session.Current;
        string board = GreyboxBoard.Render(ref state);
        Assert.That(board, Does.Contain("A"));
        Assert.That(board, Does.Contain("B"));
        Assert.That(board, Does.Contain("frame="));
        Assert.That(board, Does.Contain("patches=0"));
    }

    [Test]
    public void MatchWorldPatchHud_collects_and_formats_active_patches()
    {
        var state = new MatchState { ActorCount = 2 };
        WorldPatchRuntime.InitializeMatch(ref state);
        Assert.That(MatchWorldPatchHud.FormatSummary(ref state), Is.EqualTo(string.Empty));

        WorldPatchRuntime.Spawn(
            ref state,
            WorldPatchRuntime.KindIce,
            Fix.FromInt(1),
            Fix.Zero,
            MatchRules.IceJarFreezeRange,
            MatchRules.IcePatchLifetimeFrames);
        WorldPatchRuntime.Spawn(
            ref state,
            WorldPatchRuntime.KindSmoke,
            Fix.FromInt(2),
            Fix.Zero,
            MatchRules.SmokeJarBlindRange,
            MatchRules.SmokePatchLifetimeFrames);
        WorldPatchRuntime.Spawn(
            ref state,
            WorldPatchRuntime.KindSlow,
            Fix.FromInt(-1),
            Fix.Zero,
            MatchRules.AdhesiveSlowRange,
            MatchRules.AdhesivePatchLifetimeFrames);

        var buf = new WorldPatchHudEntry[MatchWorldPatchHud.MaxEntries];
        int n = MatchWorldPatchHud.Collect(ref state, buf);
        Assert.That(n, Is.EqualTo(3));
        Assert.That(buf[0].KindTag, Is.EqualTo("ICE"));
        Assert.That(buf[0].BoardGlyph, Is.EqualTo('~'));
        Assert.That(buf[1].KindTag, Is.EqualTo("SMK"));
        Assert.That(buf[1].BoardGlyph, Is.EqualTo('#'));
        Assert.That(buf[2].KindTag, Is.EqualTo("SLW"));
        Assert.That(buf[2].BoardGlyph, Is.EqualTo('='));
        Assert.That(buf[0].RemainingFrames, Is.EqualTo(MatchRules.IcePatchLifetimeFrames));

        string summary = MatchWorldPatchHud.FormatSummary(ref state);
        Assert.That(summary, Does.StartWith("patches=3"));
        Assert.That(summary, Does.Contain("ICE@"));
        Assert.That(summary, Does.Contain("SMK@"));
        Assert.That(summary, Does.Contain("SLW@"));

        string board = GreyboxBoard.Render(ref state);
        Assert.That(board, Does.Contain("patches=3"));
        Assert.That(board, Does.Contain("hazards=0"));
        Assert.That(board, Does.Contain("~"));
        Assert.That(board, Does.Contain("#"));
        Assert.That(board, Does.Contain("="));
    }

    [Test]
    public void MatchStageHazardHud_reports_citrine_breakaway_hazards()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(0x48415A485544UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        MatchState bazaar = sim.Snapshot();
        Assert.That(MatchStageHazardHud.FormatPhaseSummary(ref bazaar), Does.Contain("phase=0"));
        Assert.That(MatchStageHazardHud.FormatPhaseSummary(ref bazaar), Does.Contain("hazards=1"));
        Assert.That(MatchStageHazardHud.FormatHazardSummary(ref bazaar), Does.Contain("HZD1@"));

        // Through Bazaar duration into Breakaway transition — hazards off while TRANS.
        for (int f = 0; f < 25 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState transition = sim.Snapshot();
        string phaseLine = MatchStageHazardHud.FormatPhaseSummary(ref transition);
        Assert.That(phaseLine, Does.Contain("phase=1"));
        Assert.That(phaseLine, Does.Contain("TRANS"));
        Assert.That(phaseLine, Does.Contain("HZD_OFF"));

        for (int f = 0; f < 2 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState breakaway = sim.Snapshot();
        var hazBuf = new StageHazardHudEntry[MatchStageHazardHud.MaxHazards];
        int hn = MatchStageHazardHud.CollectHazards(ref breakaway, hazBuf);
        Assert.That(hn, Is.EqualTo(1));
        Assert.That(hazBuf[0].HazardId, Is.EqualTo(2));
        Assert.That(hazBuf[0].Damage, Is.EqualTo(80));

        var kinBuf = new StageKinematicHudEntry[MatchStageHazardHud.MaxKinematics];
        int kn = MatchStageHazardHud.CollectKinematics(ref breakaway, kinBuf);
        Assert.That(kn, Is.EqualTo(1));
        Assert.That(kinBuf[0].TrackId, Is.EqualTo(1));

        string summary = MatchStageHazardHud.FormatHazardSummary(ref breakaway);
        Assert.That(summary, Does.StartWith("hazards=1"));
        Assert.That(summary, Does.Contain("HZD2"));

        string board = GreyboxBoard.Render(ref breakaway);
        Assert.That(board, Does.Contain("hazards=1"));
        Assert.That(board, Does.Contain("kin=1"));
        // Hazard glyph may be overwritten by actors at the same tile; header counts are authoritative.
        Assert.That(board, Does.Contain("H").Or.Contain("!"));
    }
}
