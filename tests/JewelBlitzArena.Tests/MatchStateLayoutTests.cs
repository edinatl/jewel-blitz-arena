using System.Runtime.InteropServices;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class MatchStateLayoutTests
{
    [Test]
    public void Slot_strides_match_sizeof()
    {
        unsafe
        {
            Assert.That(sizeof(JewelSlot), Is.EqualTo(MatchState.JewelStride));
            Assert.That(sizeof(LooseItemSlot), Is.EqualTo(MatchState.LooseItemStride));
            Assert.That(sizeof(ProjectileSlot), Is.EqualTo(MatchState.ProjectileStride));
            Assert.That(sizeof(GenericPoolSlot), Is.EqualTo(MatchState.GenericStride));
            Assert.That(sizeof(CpuBrainSlot), Is.EqualTo(MatchState.CpuBrainStride));
            Assert.That(sizeof(ActorSlot), Is.EqualTo(ActorSlot.SizeOf));
            Assert.That(sizeof(Xoshiro256StarStar), Is.EqualTo(Xoshiro256StarStar.SizeOf));
            Assert.That(sizeof(InputFrame), Is.EqualTo(InputFrame.SizeOf));
        }
    }

    [Test]
    public void MatchState_sizeof_equals_sum_of_field_sizes()
    {
        unsafe
        {
            int expected =
                Xoshiro256StarStar.SizeOf * 5
                + ActorSlot.SizeOf * PoolCaps.Actors
                + MatchState.JewelStride * PoolCaps.Jewels
                + MatchState.LooseItemStride * PoolCaps.LooseItems
                + MatchState.ProjectileStride * PoolCaps.Projectiles
                + MatchState.GenericStride * PoolCaps.BossSubActors
                + MatchState.GenericStride * PoolCaps.Summons
                + MatchState.GenericStride * PoolCaps.KinematicBodies
                + MatchState.GenericStride * PoolCaps.HazardVolumes
                + MatchState.GenericStride * PoolCaps.Crates
                + MatchState.GenericStride * PoolCaps.GleamPickups
                + MatchState.GenericStride * PoolCaps.StageCreatures
                + MatchState.CpuBrainStride * PoolCaps.CpuBrains
                + sizeof(ulong) // Seed
                + sizeof(int) * 8; // Frame, ActorCount, StageId, Active*×3, JewelCap, NextJewelSpawnFrame

            Assert.That(sizeof(MatchState), Is.EqualTo(expected),
                "Padding detected — Pack=1 layout drifted. Reorder or pad fields explicitly.");
            Assert.That(MatchState.SizeOf, Is.EqualTo(sizeof(MatchState)));
            Assert.That(Marshal.SizeOf<MatchState>(), Is.EqualTo(sizeof(MatchState)));
        }
    }

    [Test]
    public void MatchState_is_under_32kib_snapshot_budget()
    {
        Assert.That(MatchState.SizeOf, Is.LessThanOrEqualTo(32 * 1024));
    }
}
