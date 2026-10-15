using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class FixTests
{
    [Test]
    public void FromInt_and_add_are_exact()
    {
        Fix a = Fix.FromInt(3);
        Fix b = Fix.FromInt(4);
        Assert.That((a + b).ToIntFloor(), Is.EqualTo(7));
        Assert.That((a * b).ToIntFloor(), Is.EqualTo(12));
    }

    [Test]
    public void MulWide_matches_fast_path_for_small_values()
    {
        Fix a = Fix.FromInt(12);
        Fix b = new Fix(Fix.OneRaw / 2); // 0.5
        Assert.That(Fix.MulWide(a, b), Is.EqualTo(a * b));
        Assert.That((a * b).ToIntFloor(), Is.EqualTo(6));
    }

    [Test]
    public void Direction_lut_has_32_unit_entries()
    {
        FixVec2 right = DirectionLut.GetUnit(0);
        Assert.That(right.X, Is.EqualTo(Fix.One));
        Assert.That(right.Y, Is.EqualTo(Fix.Zero));

        FixVec2 up = DirectionLut.GetUnit(8);
        Assert.That(up.X, Is.EqualTo(Fix.Zero));
        Assert.That(up.Y, Is.EqualTo(Fix.One));
    }
}
