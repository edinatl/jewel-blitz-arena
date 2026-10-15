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
    }
}
