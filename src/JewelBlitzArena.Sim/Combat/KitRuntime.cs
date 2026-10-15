using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Per-fighter move tables resolved from <see cref="ContentCatalog"/> at Reset.
    /// Not part of <see cref="MatchState"/> (content hash covers catalogs).
    /// </summary>
    public sealed class KitRuntime
    {
        public int FighterId;
        public int MaxLife = Simulation.DefaultLife;
        public int ReachRating = 3;
        public long WalkSpeedRaw = Fix.FromInt(3).Raw;
        public long RunSpeedRaw = Fix.FromInt(5).Raw;
        public long FullSpeedRaw = Fix.FromInt(7).Raw;
        public KitBehavior Behavior = KitBehavior.None;

        public MoveRecord? Attack;
        public MoveRecord? Strong;
        public MoveRecord? AirAttack;
        public MoveRecord? Throw;
        public MoveRecord? Flare;
        public MoveRecord? SurgeA;
        public MoveRecord? SurgeB;

        public static KitRuntime FromDefaults(int fighterId)
        {
            return new KitRuntime
            {
                FighterId = fighterId,
                Attack = StubMove("Jab", MoveClass.Attack, 35, 4, 2, 8),
                Strong = StubMove("Strong", MoveClass.Strong, 90, 10, 3, 18),
                Flare = StubMove("Flare", MoveClass.Flare, 40, 6, 3, 10),
                SurgeA = StubMove("SurgeA", MoveClass.Surge, 300, 12, 6, 24),
                SurgeB = StubMove("SurgeB", MoveClass.Surge, 170, 14, 8, 28),
                Throw = StubMove("Throw", MoveClass.Throw, 100, 4, 2, 20),
            };
        }

        public static KitRuntime FromRecord(FighterKitRecord record)
        {
            var kit = new KitRuntime
            {
                FighterId = record.FighterId,
                MaxLife = record.MaxLife > 0 ? record.MaxLife : Simulation.DefaultLife,
                ReachRating = record.ReachRating > 0 ? record.ReachRating : 3,
                WalkSpeedRaw = record.WalkSpeedRaw,
                RunSpeedRaw = record.RunSpeedRaw,
                FullSpeedRaw = record.FullSpeedRaw,
                Behavior = KitBehavior.None,
            };

            int surgeIndex = 0;
            foreach (MoveRecord move in record.Moves)
            {
                switch (move.Class)
                {
                    case MoveClass.Attack:
                        kit.Attack ??= move;
                        break;
                    case MoveClass.Strong:
                    case MoveClass.UtilityStrong:
                        kit.Strong ??= move;
                        break;
                    case MoveClass.AirAttack:
                        kit.AirAttack ??= move;
                        break;
                    case MoveClass.Throw:
                        kit.Throw ??= move;
                        break;
                    case MoveClass.Flare:
                        kit.Flare ??= move;
                        break;
                    case MoveClass.Surge:
                        if (surgeIndex == 0)
                        {
                            kit.SurgeA = move;
                        }
                        else if (surgeIndex == 1)
                        {
                            kit.SurgeB = move;
                        }

                        surgeIndex++;
                        break;
                }
            }

            // Attack-while-Blitzed becomes Flare; if no authored Flare, reuse Attack.
            kit.Flare ??= kit.Attack ?? StubMove("Flare", MoveClass.Flare, 40, 6, 3, 10);
            kit.Attack ??= StubMove("Jab", MoveClass.Attack, 35, 4, 2, 8);
            kit.Strong ??= StubMove("Strong", MoveClass.Strong, 90, 10, 3, 18);
            kit.SurgeA ??= StubMove("SurgeA", MoveClass.Surge, 300, 12, 6, 24);
            kit.SurgeB ??= StubMove("SurgeB", MoveClass.Surge, 170, 14, 8, 28);
            return kit;
        }

        private static MoveRecord StubMove(
            string name,
            MoveClass moveClass,
            int damage,
            int startup,
            int active,
            int recovery)
        {
            return new MoveRecord
            {
                Name = name,
                Class = moveClass,
                Damage = damage,
                StartupFrames = startup,
                ActiveFrames = active,
                RecoveryFrames = recovery,
            };
        }
    }
}
