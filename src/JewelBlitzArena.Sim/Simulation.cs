using System;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Deterministic match simulation. Presentation reads snapshots and writes inputs;
    /// only this type mutates <see cref="MatchState"/>.
    /// Integrated S1 combat rules + S2 physics/jewels/items.
    /// </summary>
    public sealed class Simulation
    {
        public const int TickRateHz = 60;
        public const int DefaultLife = 1000;
        public const int DefaultLusterMax = MatchRules.LusterMax;

        private static readonly Fix DefaultSpeedWalk = Fix.FromInt(3);
        private static readonly Fix DefaultSpeedRun = Fix.FromInt(5);
        private static readonly Fix DefaultSpeedFull = Fix.FromInt(7);

        private MatchState _state;
        private ContentCatalog? _catalog;
        private StageGraph _stageGraph = StageGraph.Empty;
        private SimContactBuffer _contacts;
        private readonly KitRuntime[] _kits = new KitRuntime[PoolCaps.Actors];
        private readonly Fix[] _dtWalk = new Fix[PoolCaps.Actors];
        private readonly Fix[] _dtRun = new Fix[PoolCaps.Actors];
        private readonly Fix[] _dtFull = new Fix[PoolCaps.Actors];
        private readonly InputFrame[] _lastInputs = new InputFrame[PoolCaps.Actors];
        private readonly byte[] _cpuLevels = new byte[PoolCaps.Actors];

        public int Frame => _state.Frame;

        public int ActorCount => _state.ActorCount;

        public bool MatchOver => _state.MatchOver;

        public ContentCatalog? Catalog => _catalog;

        public StageGraph StageGraph => _stageGraph;

        public ref readonly MatchState State => ref _state;

        /// <summary>Events from the last <see cref="Step"/> (not hashed). S1 consumes via CombatSystem.</summary>
        public ref readonly SimContactBuffer Contacts => ref _contacts;

        public void Reset(in MatchConfig config) => Reset(config, catalog: null);

        public void Reset(in MatchConfig config, ContentCatalog? catalog)
        {
            _catalog = catalog;
            _contacts.Clear();
            _state = default;
            _state.Seed = config.Seed;
            _state.StageId = config.StageId;
            _state.ActorCount = config.ActorCount < 1
                ? 1
                : (config.ActorCount > PoolCaps.Actors ? PoolCaps.Actors : config.ActorCount);
            _state.Frame = 0;
            _state.HouseRulesBits = config.HouseRulesBits != 0
                ? config.HouseRulesBits
                : MatchRules.HouseLusterTimer;
            _state.NextPlacementFromBottom = _state.ActorCount;
            _state.MatchEndFrame = 0;

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                MatchStateAccess.SetPlacement(ref _state, i, 0);
            }

            _state.GameplayRng.Seed(config.Seed);
            for (int i = 0; i < PoolCaps.CpuBrains; i++)
            {
                MatchStateAccess.CpuRng(ref _state, i).Seed(
                    config.Seed ^ (0xC0FFEEUL + (ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
            }

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                _kits[i] = KitRuntime.FromDefaults(i);
                _dtWalk[i] = DefaultSpeedWalk / TickRateHz;
                _dtRun[i] = DefaultSpeedRun / TickRateHz;
                _dtFull[i] = DefaultSpeedFull / TickRateHz;
                _lastInputs[i] = InputFrame.Neutral;
                _cpuLevels[i] = (byte)config.GetCpuLevel(i);
            }

            unsafe
            {
                for (int i = 0; i < _state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                    actor = default;
                    actor.FighterId = config.FighterIds[i];
                    actor.HeldItemSlot = ActorSlot.NoHeldItem;
                    actor.HeldItemSlotLeft = ActorSlot.NoHeldItem;

                    KitRuntime kit;
                    if (catalog != null && catalog.TryGetFighter(actor.FighterId, out FighterKitRecord record))
                    {
                        kit = KitRuntime.FromRecord(record);
                    }
                    else
                    {
                        kit = KitRuntime.FromDefaults(actor.FighterId);
                    }

                    _kits[i] = kit;
                    _dtWalk[i] = new Fix(kit.WalkSpeedRaw) / TickRateHz;
                    _dtRun[i] = new Fix(kit.RunSpeedRaw) / TickRateHz;
                    _dtFull[i] = new Fix(kit.FullSpeedRaw) / TickRateHz;

                    int life = config.StartingLife[i];
                    if (life <= 0)
                    {
                        life = kit.MaxLife;
                    }

                    actor.Life = life > 0 ? life : DefaultLife;
                    actor.MaxLife = actor.Life;
                    actor.Luster = 0;
                    actor.IsAlive = true;
                    actor.Team = config.Teams[i];
                    actor.ReachRating = (byte)kit.ReachRating;
                    actor.KitBehavior = (byte)kit.Behavior;
                    actor.PosX = Fix.FromInt(i * 2 - 1);
                    actor.PosY = Fix.Zero;
                    actor.PosZ = Fix.Zero;
                    actor.Facing = i == 0 ? (byte)0 : (byte)1;
                }
            }

            ItemRuntime.InitializeMatch(ref _state);
            WorldPatchRuntime.InitializeMatch(ref _state);
            _stageGraph = StageRuntime.ResolveGraph(catalog, config.StageId);
            int jewelCap = config.JewelCap > 0
                ? config.JewelCap
                : (_stageGraph.JewelCapOverride > 0
                    ? _stageGraph.JewelCapOverride
                    : MatchRules.DefaultJewelCap(_state.ActorCount));
            JewelRuntime.InitializeMatch(ref _state, jewelCap);
            StageRuntime.InitializeMatch(
                ref _state,
                _stageGraph,
                config.PhaseClockPermille,
                ref _contacts);
            BreakableRuntime.InitializeMatch(ref _state, _stageGraph);
            BossPartRuntime.InitializeMatch(ref _state, _stageGraph);
            ProjectileRuntime.InitializeMatch(ref _state);
            GleamBudgetRuntime.InitializeMatch(ref _state, _stageGraph);
        }

        public void Step(InputFrame i0, InputFrame i1, InputFrame i2, InputFrame i3)
        {
            if (_state.MatchOver)
            {
                _state.Frame++;
                return;
            }

            _contacts.Clear();
            _lastInputs[0] = ResolveSlotInput(0, i0);
            _lastInputs[1] = ResolveSlotInput(1, i1);
            _lastInputs[2] = ResolveSlotInput(2, i2);
            _lastInputs[3] = ResolveSlotInput(3, i3);

            // S1 combat input (skips Attack when holding an item).
            CombatSystem.ApplyInput(ref _state, 0, _lastInputs[0], _kits[0], _dtWalk[0], _dtRun[0], _dtFull[0], _stageGraph);
            CombatSystem.ApplyInput(ref _state, 1, _lastInputs[1], _kits[1], _dtWalk[1], _dtRun[1], _dtFull[1], _stageGraph);
            CombatSystem.ApplyInput(ref _state, 2, _lastInputs[2], _kits[2], _dtWalk[2], _dtRun[2], _dtFull[2], _stageGraph);
            CombatSystem.ApplyInput(ref _state, 3, _lastInputs[3], _kits[3], _dtWalk[3], _dtRun[3], _dtFull[3], _stageGraph);

            // S2 item pickup/throw/catch.
            for (int a = 0; a < _state.ActorCount; a++)
            {
                ItemRuntime.ProcessActorButtons(ref _state, a, _lastInputs[a], ref _contacts);
            }

            // Harvester mount: Action near a broken leg climbs to the tail.
            for (int a = 0; a < _state.ActorCount; a++)
            {
                BossPartRuntime.ProcessMountInput(
                    ref _state, a, _lastInputs[a], ref _contacts);
            }

            IntegrateActorsHorizontal();
            PhysicsWorld.StepActorsJewelsItems(ref _state, ref _contacts);
            ItemRuntime.TickThrownHits(ref _state, ref _contacts);
            ProjectileRuntime.Tick(ref _state, ref _contacts);
            JewelRuntime.TickPickups(ref _state, ref _contacts);
            JewelRuntime.TickSpawns(ref _state);
            JewelSystem.TickScheduledRespawns(ref _state);

            // S1 consumes S2 contacts (Blitz, thrown-item damage), then ticks combat.
            CombatSystem.ConsumeContacts(ref _state, ref _contacts);
            CombatSystem.TickActors(ref _state, _kits);
            // Boss parts before crates so anatomy takes Attack priority on Harvester.
            BossPartRuntime.ResolveAttackHits(ref _state, _kits, ref _contacts);
            BreakableRuntime.ResolveAttackHits(
                ref _state, _kits, _stageGraph, _catalog, ref _contacts);
            BossPartRuntime.Tick(
                ref _state,
                _lastInputs[0],
                _lastInputs[1],
                _lastInputs[2],
                _lastInputs[3],
                ref _contacts);
            ProjectileRuntime.TickCuttersCadence(ref _state, _stageGraph);
            WorldPatchRuntime.Tick(ref _state);
            GleamRuntime.Tick(ref _state, ref _contacts);

            // S4 stage graph: timers, transitions, hazards, kinematics.
            StageRuntime.Tick(ref _state, _stageGraph, ref _contacts);

            _state.ActiveJewelCount = JewelRuntime.CountTowardCap(ref _state);
            _state.ActiveLooseItemCount = CrateSpawner.CountActiveItems(ref _state);
            _state.ActiveProjectileCount = ProjectileRuntime.CountActive(ref _state);

            _ = _state.GameplayRng.NextUInt64();
            _state.Frame++;
        }

        public void Step(ReadOnlySpan<InputFrame> inputs)
        {
            InputFrame i0 = inputs.Length > 0 ? inputs[0] : InputFrame.Neutral;
            InputFrame i1 = inputs.Length > 1 ? inputs[1] : InputFrame.Neutral;
            InputFrame i2 = inputs.Length > 2 ? inputs[2] : InputFrame.Neutral;
            InputFrame i3 = inputs.Length > 3 ? inputs[3] : InputFrame.Neutral;
            Step(i0, i1, i2, i3);
        }

        public void DebugGrantJewels(int actorIndex, int count)
        {
            if (actorIndex < 0 || actorIndex >= _state.ActorCount)
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, actorIndex);
            for (int n = 0; n < count; n++)
            {
                if (actor.IsBlitzed)
                {
                    break;
                }

                // Prefer real owned slots when the jewel cap allows; otherwise bump the
                // held count only (legacy Blitz-enter tests).
                if (!JewelRuntime.TryGiveOwnedJewel(ref _state, actorIndex))
                {
                    actor.JewelsHeld++;
                    actor.JewelsCollectedTotal++;
                }

                actor = ref MatchStateAccess.Actor(ref _state, actorIndex);
                if (actor.JewelsHeld >= MatchRules.BlitzJewelCount)
                {
                    JewelSystem.BeginBlitz(ref _state, actorIndex);
                    break;
                }
            }
        }

        public bool DebugDropOneJewel(int actorIndex) =>
            JewelRuntime.TryDropOneHeldJewel(ref _state, actorIndex);

        public int SpawnLooseItem(int itemId, Fix x, Fix y, Fix z) =>
            CrateSpawner.TrySpawnLooseItem(ref _state, itemId, new FixVec3(x, y, z));

        /// <summary>
        /// Spawn a field item and immediately arm its Ice/Smoke/Adhesive world patch
        /// (corpus / authored status-item scenarios).
        /// </summary>
        public int SpawnLooseItemWithWorldPatch(int itemId, Fix x, Fix y, Fix z)
        {
            int slot = SpawnLooseItem(itemId, x, y, z);
            if (slot >= 0)
            {
                WorldPatchRuntime.TrySpawnFromItem(ref _state, itemId, x, z);
            }

            return slot;
        }

        public int SpawnCrateItemAt(Fix x, Fix z)
        {
            int id = CrateSpawner.RollAlwaysEligible(ref _state, _catalog);
            return CrateSpawner.TrySpawnLooseItem(ref _state, id, new FixVec3(x, Fix.Zero, z));
        }

        public ulong Hash()
        {
            unsafe
            {
                fixed (MatchState* p = &_state)
                {
                    return XxHash64.Hash(p, MatchState.SizeOf);
                }
            }
        }

        public MatchState Snapshot() => _state;

        public void Restore(in MatchState snapshot) => _state = snapshot;

        /// <summary>
        /// Hand a fighter slot to a deterministic CPU brain (online disconnect / fill).
        /// Level defaults to <see cref="MatchConfig"/> per-slot CPU level from Reset.
        /// </summary>
        public void SetCpuControlled(int slot, bool controlled, int? level = null)
        {
            if (controlled)
            {
                int lvl = level ?? (slot >= 0 && slot < PoolCaps.Actors
                    ? _cpuLevels[slot]
                    : CpuBrainRuntime.DefaultLevel);
                if (slot >= 0 && slot < PoolCaps.Actors)
                {
                    _cpuLevels[slot] = (byte)CpuBrainRuntime.ClampLevel(lvl);
                }

                CpuBrainRuntime.Activate(ref _state, slot, lvl);
            }
            else
            {
                CpuBrainRuntime.Deactivate(ref _state, slot);
            }
        }

        public bool IsCpuControlled(int slot) => CpuBrainRuntime.IsActive(ref _state, slot);

        public int GetCpuLevel(int slot) =>
            slot >= 0 && slot < PoolCaps.Actors
                ? CpuBrainRuntime.ClampLevel(_cpuLevels[slot] == 0 ? CpuBrainRuntime.DefaultLevel : _cpuLevels[slot])
                : CpuBrainRuntime.DefaultLevel;

        public MatchResult BuildResult()
        {
            var result = new MatchResult
            {
                FramesPlayed = _state.Frame,
                ActorCount = _state.ActorCount,
                CaratsEarned = _state.CaratsCollected,
            };

            unsafe
            {
                for (int i = 0; i < PoolCaps.Actors; i++)
                {
                    result.Placements[i] = 0;
                    result.Kos[i] = 0;
                    result.JewelsCollected[i] = 0;
                }

                for (int i = 0; i < _state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                    result.Kos[i] = actor.KosDealt;
                    result.JewelsCollected[i] = actor.JewelsCollectedTotal;
                    result.Placements[i] = MatchStateAccess.GetPlacement(ref _state, i);
                }

                bool anyPlacement = false;
                for (int i = 0; i < _state.ActorCount; i++)
                {
                    if (result.Placements[i] != 0)
                    {
                        anyPlacement = true;
                        break;
                    }
                }

                if (!anyPlacement)
                {
                    for (int place = 1; place <= _state.ActorCount; place++)
                    {
                        int best = -1;
                        int bestLife = int.MinValue;
                        for (int i = 0; i < _state.ActorCount; i++)
                        {
                            if (result.Placements[i] != 0)
                            {
                                continue;
                            }

                            int life = MatchStateAccess.Actor(ref _state, i).Life;
                            if (life > bestLife)
                            {
                                bestLife = life;
                                best = i;
                            }
                        }

                        if (best >= 0)
                        {
                            result.Placements[best] = place;
                        }
                    }
                }
            }

            return result;
        }

        private InputFrame ResolveSlotInput(int slot, InputFrame provided)
        {
            if (slot < _state.ActorCount && CpuBrainRuntime.IsActive(ref _state, slot))
            {
                return CpuBrainRuntime.Think(ref _state, slot);
            }

            return provided;
        }

        private void IntegrateActorsHorizontal()
        {
            for (int i = 0; i < _state.ActorCount; i++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                if (!actor.IsAlive || actor.IsEliminated)
                {
                    continue;
                }

                actor.PosX += actor.VelX;
                actor.PosZ += actor.VelZ;
            }
        }
    }
}
