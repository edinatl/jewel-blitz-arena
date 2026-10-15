using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class XoshiroTests
{
    [Test]
    public void Same_seed_produces_same_sequence()
    {
        var a = new Xoshiro256StarStar();
        var b = new Xoshiro256StarStar();
        a.Seed(12345UL);
        b.Seed(12345UL);

        for (int i = 0; i < 64; i++)
        {
            Assert.That(a.NextUInt64(), Is.EqualTo(b.NextUInt64()));
        }
    }

    [Test]
    public void NextInt_stays_in_range()
    {
        var rng = new Xoshiro256StarStar();
        rng.Seed(99UL);
        for (int i = 0; i < 1000; i++)
        {
            int v = rng.NextInt(7);
            Assert.That(v, Is.InRange(0, 6));
        }
    }
}
