namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Ownership boundary between S1 (actor/match rules) and S2 (physics/jewels/items).
    /// Keep shared surface minimal so PRs can merge independently.
    /// </summary>
    /// <remarks>
    /// <para><b>S2 owns:</b> fixed-point body integration, ground/bounds, jewel spawn/pickup/
    /// circulation, loose-item pickup/throw/catch windows, crate spawn API, contact
    /// enumeration for the last step.</para>
    /// <para><b>S1 owns:</b> damage from contacts, knockdown → jewel drop policy, Blitz enter
    /// on third jewel, Life/Luster rules, attack hitboxes.</para>
    /// <para><b>Shared hooks S1 may call:</b>
    /// <see cref="JewelRuntime.TryDropOneHeldJewel"/>,
    /// <see cref="JewelRuntime.TryDropAllHeldJewels"/>,
    /// <see cref="Simulation.Contacts"/> (read-only after <c>Step</c>).
    /// </para>
    /// <para><b>Shared hooks S2 emits (no damage applied):</b>
    /// <see cref="SimEventKind.ActorJewelPickup"/>,
    /// <see cref="SimEventKind.JewelReadyToBlitz"/> (held count reached 3 — S1 applies Blitz),
    /// <see cref="SimEventKind.ThrownItemHitActor"/> (S1 decides damage),
    /// <see cref="SimEventKind.ActorActorOverlap"/> (S1 decides throws/grabs later).
    /// </para>
    /// </remarks>
    public static class S1S2Boundary
    {
        public const string SliceOwnerPhysics = "S2";
        public const string SliceOwnerCombat = "S1";
    }

    public enum SimEventKind : byte
    {
        None = 0,
        ActorJewelPickup = 1,
        JewelReadyToBlitz = 2,
        ActorItemPickup = 3,
        ItemThrown = 4,
        ThrownItemHitActor = 5,
        ItemCaught = 6,
        ActorActorOverlap = 7,
        PhaseChanged = 8,
        PhaseTransitionStarted = 9,
        HazardHitActor = 10,
        /// <summary>Loose item left flight and settled on the field. SlotA = item index.</summary>
        ItemLanded = 11,
        /// <summary>Stage crate / breakable opened. SlotA = crate index.</summary>
        CrateBroken = 12,
        /// <summary>Expedition gleam collected. SlotA = actor, SlotB = gleam slot.</summary>
        GleamPickedUp = 13,
        /// <summary>Boss part took damage. SlotA = part index, SlotB = attacker.</summary>
        BossPartHit = 14,
        /// <summary>Boss part Life reached 0. SlotA = part index, SlotB = kind.</summary>
        BossPartBroken = 15,
        /// <summary>Fighter mounted Harvester tail. SlotA = actor.</summary>
        BossMounted = 16,
        /// <summary>Harvester Buck shake. SlotA = actor thrown (or 255), SlotB = hanging (1) / thrown (0).</summary>
        BossBuck = 17,
    }

    /// <summary>Ephemeral per-step event (not part of <see cref="MatchState"/> hash).</summary>
    public struct SimContactEvent
    {
        public SimEventKind Kind;
        public byte SlotA;
        public byte SlotB;
        public ushort Frame;
    }

    /// <summary>Fixed-cap event buffer for one step. Not snapshotted.</summary>
    public unsafe struct SimContactBuffer
    {
        public const int Capacity = 64;
        public fixed byte Kinds[Capacity];
        public fixed byte SlotAs[Capacity];
        public fixed byte SlotBs[Capacity];
        public int Count;

        public void Clear() => Count = 0;

        public void Add(SimEventKind kind, byte a, byte b)
        {
            if (Count >= Capacity)
            {
                return;
            }

            int i = Count++;
            Kinds[i] = (byte)kind;
            SlotAs[i] = a;
            SlotBs[i] = b;
        }

        public SimContactEvent Get(int index)
        {
            return new SimContactEvent
            {
                Kind = (SimEventKind)Kinds[index],
                SlotA = SlotAs[index],
                SlotB = SlotBs[index],
            };
        }
    }
}
