namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Expedition gleam pickups (items-and-economy.md §7). Shares the GleamPickups pool with
    /// world patches via TypeId ranges: patches 1–3, gleams ≥ <see cref="KindCopper"/>.
    /// Full budget split (KO 50% / knockdown 30% / containers 20%) lands with Expedition mode.
    /// </summary>
    public static class GleamRuntime
    {
        public const int KindCopper = 10;
        public const int KindSilver = 11;
        public const int KindGold = 12;
        public const byte FlagActive = 1 << 0;

        public static readonly Fix PickupRadius = Fix.One;

        public static bool IsGleamKind(int typeId) =>
            typeId == KindCopper || typeId == KindSilver || typeId == KindGold;

        public static int CaratsForKind(int typeId) =>
            typeId switch
            {
                KindCopper => MatchRules.GleamCopperCarats,
                KindSilver => MatchRules.GleamSilverCarats,
                KindGold => MatchRules.GleamGoldCarats,
                _ => 0,
            };

        public static int TrySpawnCopper(ref MatchState state, Fix x, Fix z) =>
            TrySpawn(ref state, KindCopper, x, z);

        public static int TrySpawn(ref MatchState state, int kind, Fix x, Fix z)
        {
            if (!IsGleamKind(kind))
            {
                return -1;
            }

            int slot = FindFreeSlot(ref state);
            if (slot < 0)
            {
                return -1;
            }

            var gleam = new GenericPoolSlot
            {
                PosX = x,
                PosY = Fix.Zero,
                PosZ = z,
                TypeId = kind,
                Param0 = MatchRules.GleamDespawnFrames,
                Flags = FlagActive,
                Reserved1 = (ushort)CaratsForKind(kind),
            };
            MatchStateAccess.SetWorldPatch(ref state, slot, gleam);
            return slot;
        }

        /// <summary>Despawn timer + living-actor pickup (carats stashed in Reserved1 until Meta).</summary>
        public static void Tick(ref MatchState state, ref SimContactBuffer events)
        {
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                GenericPoolSlot gleam = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((gleam.Flags & FlagActive) == 0 || !IsGleamKind(gleam.TypeId))
                {
                    continue;
                }

                gleam.Param0--;
                if (gleam.Param0 <= 0)
                {
                    MatchStateAccess.SetWorldPatch(ref state, i, default);
                    continue;
                }

                int picker = FindPicker(ref state, gleam.PosX, gleam.PosZ);
                if (picker >= 0)
                {
                    int carats = gleam.Reserved1;
                    if (carats <= 0)
                    {
                        carats = CaratsForKind(gleam.TypeId);
                    }

                    state.CaratsCollected += carats;
                    events.Add(SimEventKind.GleamPickedUp, (byte)picker, (byte)i);
                    MatchStateAccess.SetWorldPatch(ref state, i, default);
                    continue;
                }

                MatchStateAccess.SetWorldPatch(ref state, i, gleam);
            }
        }

        public static int CountActive(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                GenericPoolSlot g = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((g.Flags & FlagActive) != 0 && IsGleamKind(g.TypeId))
                {
                    n++;
                }
            }

            return n;
        }

        private static int FindPicker(ref MatchState state, Fix x, Fix z)
        {
            // Expedition: only slot 0 (player) may collect. Tournament path unused without flag.
            bool expedition = (state.HouseRulesBits & MatchRules.ExpeditionGleams) != 0;
            int start = expedition ? 0 : 0;
            int end = expedition ? 1 : state.ActorCount;
            for (int a = start; a < end && a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated)
                {
                    continue;
                }

                if (PhysicsWorld.OverlapsXZ(actor.PosX, actor.PosZ, x, z, PickupRadius))
                {
                    return a;
                }
            }

            return -1;
        }

        private static int FindFreeSlot(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                if ((MatchStateAccess.GetWorldPatch(ref state, i).Flags & FlagActive) == 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
