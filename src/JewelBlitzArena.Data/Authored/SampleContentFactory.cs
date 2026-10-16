using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    /// <summary>
    /// S5 stub pack: full cast kits (cast.md §6 + kit behaviors), five story-arena
    /// phase stubs (Citrine → Orrery), two boss-arena stubs, four Extra Arena stubs,
    /// 25 always-eligible starters plus 5 Never-eligible Vein Guard stage weapons.
    /// </summary>
    public static class SampleContentFactory
    {
        public const int RookFighterId = 1;
        public const int SableFighterId = 2;
        public const int BulwarkFighterId = 3;
        public const int LuneFighterId = 4;
        public const int TeoFighterId = 5;
        public const int WrenFighterId = 6;
        public const int DelphineFighterId = 7;
        public const int ClankFighterId = 8;
        public const int FerroFighterId = 9;
        public const int PipPepperFighterId = 10;
        public const int HobFighterId = 11;
        public const int LaCumbreFighterId = 12;
        public const int CaptainCitrineFighterId = 13;
        public const int TillyFighterId = 14;
        public const int CitrineBazaarStageId = 1;
        public const int FulgurCloisterStageId = 2;
        public const int SunkenStrongroomStageId = 3;
        public const int RimeCutworksStageId = 4;
        public const int OrreryStageId = 5;
        public const int HarvesterTerracesStageId = 6;
        public const int PressworksStageId = 7;

        /// <summary>Extra Arena stubs (stages.md §4) — Custom unlock ladder slots 1–4.</summary>
        public const int TerracesNoBossStageId = 101;
        public const int BehindCounterStageId = 102;
        public const int SpirewalkStageId = 103;
        public const int CascadeStageId = 104;

        /// <summary>S5 stub Adhesive Spray — thrown hit applies Slow (matches <c>MatchRules.AdhesiveSprayItemId</c>).</summary>
        public const int AdhesiveSprayItemId = 18;

        /// <summary>S5 stub Petrify Gas — thrown hit applies Freeze (matches <c>MatchRules.PetrifyGasItemId</c>).</summary>
        public const int PetrifyGasItemId = 19;

        /// <summary>S5 stub Smoke Jar — lasting Blind floor patch (matches <c>MatchRules.SmokeJarItemId</c>).</summary>
        public const int SmokeJarItemId = 20;

        /// <summary>S5 stub Ice Jar — lasting Freeze floor patch (matches <c>MatchRules.IceJarItemId</c>).</summary>
        public const int IceJarItemId = 21;

        /// <summary>Vein Guard stage weapons (never crate-eligible; MatchRules mirrors).</summary>
        public const int StageMortarItemId = 26;
        public const int StageFrostStaffItemId = 27;
        public const int StageMalletItemId = 28;
        public const int StageNetItemId = 29;
        public const int StageFlameItemId = 30;

        public static AuthoredContentPack CreateS5StubPack()
        {
            return new AuthoredContentPack
            {
                SchemaVersion = ContentSchema.Version,
                Fighters = new List<AuthoredFighter>
                {
                    CreateRook(),
                    CreateSable(),
                    CreateBulwark(),
                    CreateLune(),
                    CreateTeo(),
                    CreateWren(),
                    CreateDelphine(),
                    CreateClank(),
                    CreateFerro(),
                    CreatePipPepper(),
                    CreateHob(),
                    CreateLaCumbre(),
                    CreateCaptainCitrine(),
                    CreateTilly(),
                },
                Stages = new List<AuthoredStage>
                {
                    CreateCitrineBazaar(),
                    CreateFulgurCloister(),
                    CreateSunkenStrongroom(),
                    CreateRimeCutworks(),
                    CreateOrrery(),
                    CreateHarvesterTerraces(),
                    CreatePressworks(),
                    CreateTerracesNoBoss(),
                    CreateBehindTheCounter(),
                    CreateSpirewalk(),
                    CreateCascade(),
                },
                Items = CreateStarterSet(),
            };
        }

        /// <summary>Walk/run/full from Speed rating (Rook 3 → 3/5/7).</summary>
        public static void ApplySpeedFromRating(AuthoredFighter f)
        {
            int s = f.SpeedRating;
            if (s < 1)
            {
                s = 1;
            }

            if (s > 5)
            {
                s = 5;
            }

            f.WalkSpeed = s.ToString();
            f.RunSpeed = (s + 2).ToString();
            f.FullSpeed = (s + 4).ToString();
        }

        public static AuthoredFighter CreateRook()
        {
            var f = BaseFighter(
                RookFighterId,
                "Rook Citrine",
                speed: 3,
                life: 3,
                reach: 3,
                air: 3,
                power: 3,
                kitBehavior: "None");
            f.Moves = StandardMoves(
                "Jab", 35,
                "Strong", 90,
                "Air Attack", 45,
                surgeA: ("Thermal Dive", 300),
                surgeB: ("Windlass", 170),
                flare: ("Boost Slash", 45));
            return f;
        }

        public static AuthoredFighter CreateSable()
        {
            var f = BaseFighter(
                SableFighterId,
                "Sable",
                speed: 5,
                life: 1,
                reach: 2,
                air: 3,
                power: 2,
                kitBehavior: "VanishStrong");
            // Vanish Strong: short-range reappear; long recovery stands in for 30 f cooldown.
            f.Moves = new List<AuthoredMove>
            {
                Move("Flick", "Attack", 32, 3, 2, 6),
                Move("Vanish", "Strong", 85, 6, 2, 30),
                Move("Air Flick", "AirAttack", 42, 4, 2, 10),
                Move("Throw", "Throw", 100, 5, 2, 20),
                Move("Blink Strike", "Flare", 40, 4, 2, 8),
                Move("Curtain Call", "Surge", 320, 10, 8, 22),
                Move("Smoke Vault", "Surge", 0, 12, 4, 24, effect: "Dizzy"),
            };
            return f;
        }

        public static AuthoredFighter CreateBulwark()
        {
            var f = BaseFighter(
                BulwarkFighterId,
                "Bulwark Oyo",
                speed: 1,
                life: 5,
                reach: 2,
                air: 1,
                power: 5,
                kitBehavior: "CommandGrabCarry");
            // Command grab: 12 f startup (cast.md §2); carry throw separate.
            f.Moves = new List<AuthoredMove>
            {
                Move("Slam String", "Attack", 38, 5, 3, 10),
                Move("Command Grab", "Strong", 100, 12, 2, 22),
                Move("Air Slam", "AirAttack", 48, 8, 3, 14),
                Move("Carry Throw", "Throw", 100, 5, 2, 20),
                Move("Ground Pound", "Flare", 50, 8, 4, 14),
                Move("Beacon", "Surge", 200, 14, 8, 26),
                Move("Groundswell", "Surge", 160, 16, 6, 28),
            };
            return f;
        }

        public static AuthoredFighter CreateLune()
        {
            var f = BaseFighter(
                LuneFighterId,
                "Lune Varga",
                speed: 3,
                life: 2,
                reach: 5,
                air: 2,
                power: 2,
                kitBehavior: "RiposteStrong");
            f.Moves = StandardMoves(
                "Lunge", 34,
                "Riposte", 90,
                "Air Lunge", 44,
                surgeA: ("Measure", 200),
                surgeB: ("Coronet", 320),
                flare: ("Through-Lunge", 48));
            return f;
        }

        public static AuthoredFighter CreateTeo()
        {
            var f = BaseFighter(
                TeoFighterId,
                "Teo Cruz",
                speed: 4,
                life: 2,
                reach: 3,
                air: 3,
                power: 2,
                kitBehavior: "ExtendedDodge");
            f.Moves = StandardMoves(
                "Ginga", 33,
                "Cartwheel", 88,
                "Air Kick", 43,
                surgeA: ("Roda", 180),
                surgeB: ("Bateria", 60),
                flare: ("Somersault", 46));
            // Surge B is last in StandardMoves — Stun 60 f area (cast.md §5).
            f.Moves[f.Moves.Count - 1].Effect = "Stun";
            return f;
        }

        public static AuthoredFighter CreateWren()
        {
            var f = BaseFighter(
                WrenFighterId,
                "Wren Ashfall",
                speed: 3,
                life: 1,
                reach: 2,
                air: 5,
                power: 2,
                kitBehavior: "TripleJumpGlide");
            f.Moves = StandardMoves(
                "Wing Clip", 31,
                "Dive Kick", 92,
                "Air Clip", 41,
                surgeA: ("Downdraft", 150),
                surgeB: ("Updraft", 180),
                flare: ("Static Dash", 42));
            return f;
        }

        public static AuthoredFighter CreateDelphine()
        {
            var f = BaseFighter(
                DelphineFighterId,
                "Delphine",
                speed: 2,
                life: 2,
                reach: 4,
                air: 4,
                power: 2,
                kitBehavior: "None");
            f.Moves = new List<AuthoredMove>
            {
                Move("Motif", "Attack", 32, 5, 2, 9),
                Move("Sonic Cone", "UtilityStrong", 0, 8, 4, 16),
                Move("Air Motif", "AirAttack", 42, 6, 3, 12),
                Move("Throw", "Throw", 100, 5, 2, 20),
                Move("Note", "Flare", 40, 6, 3, 10),
                Move("Aria", "Surge", 200, 10, 120, 20),
                Move("Crescendo", "Surge", 160, 14, 8, 28),
            };
            return f;
        }

        public static AuthoredFighter CreateClank()
        {
            var f = BaseFighter(
                ClankFighterId,
                "Clank",
                speed: 2,
                life: 4,
                reach: 4,
                air: 1,
                power: 3,
                kitBehavior: "DoubleHold");
            // Surge order: Toolbox N/F/U/D then Spare Parts N/F/U/D (cast.md §2 / §5).
            f.Moves = new List<AuthoredMove>
            {
                Move("Clank Hit", "Attack", 36, 6, 3, 12),
                Move("Spring Grab", "Strong", 95, 10, 3, 18),
                Move("Air Clank", "AirAttack", 46, 7, 3, 14),
                Move("Throw", "Throw", 100, 5, 2, 20),
                Move("Shoulder Battery", "Flare", 45, 6, 3, 10),
                Move("Toolbox Mortar", "Surge", 280, 14, 4, 24),
                Move("Toolbox Ram", "Surge", 280, 10, 6, 22),
                Move("Toolbox Drop Weight", "Surge", 320, 18, 4, 28),
                Move("Toolbox Mine Field", "Surge", 70, 12, 8, 20, effect: "Slow"),
                Move("Spare Magnet", "Surge", 0, 10, 6, 18, effect: "Magnet"),
                Move("Spare Flame", "Surge", 200, 12, 8, 24),
                Move("Spare Chaff", "Surge", 40, 8, 4, 16, effect: "Blind"),
                Move("Spare Repair", "Surge", 280, 14, 2, 22, effect: "Heal"),
            };
            return f;
        }

        public static AuthoredFighter CreateFerro()
        {
            var f = BaseFighter(
                FerroFighterId,
                "Ferro Dusk",
                speed: 3,
                life: 3,
                reach: 4,
                air: 2,
                power: 2,
                kitBehavior: "None");
            f.Moves = new List<AuthoredMove>
            {
                Move("Stock", "Attack", 34, 5, 2, 9),
                Move("Lever Shot", "Strong", 60, 12, 2, 20, utilityExempt: true),
                Move("Air Stock", "AirAttack", 44, 6, 3, 12),
                Move("Throw", "Throw", 100, 5, 2, 20),
                Move("Piston Punch", "Flare", 48, 7, 3, 12),
                Move("Full Steam", "Surge", 280, 12, 6, 24),
                Move("Signal Fire", "Surge", 150, 14, 8, 28),
            };
            return f;
        }

        public static AuthoredFighter CreatePipPepper()
        {
            // Pip-leaning ratings for the shared slot (cast.md §6).
            var f = BaseFighter(
                PipPepperFighterId,
                "Pip & Pepper",
                speed: 4,
                life: 2,
                reach: 2,
                air: 3,
                power: 2,
                kitBehavior: "SwapPartners");
            f.Moves = StandardMoves(
                "Twin Jab", 33,
                "Swap In", 86,
                "Air Twin", 43,
                surgeA: ("Undertow", 140),
                surgeB: ("Breaker", 200),
                flare: ("Wave Dash", 44));
            return f;
        }

        public static AuthoredFighter CreateHob()
        {
            var f = BaseFighter(
                HobFighterId,
                "Hob",
                speed: 3,
                life: 3,
                reach: 3,
                air: 2,
                power: 3,
                kitBehavior: "None");
            f.Moves = StandardMoves(
                "Cleaver", 35,
                "Pan Swing", 80,
                "Air Cleaver", 45,
                surgeA: ("Boil Over", 200),
                surgeB: ("Pincer", 300),
                flare: ("Steam Vent", 46));
            return f;
        }

        public static AuthoredFighter CreateLaCumbre()
        {
            var f = BaseFighter(
                LaCumbreFighterId,
                "La Cumbre",
                speed: 1,
                life: 4,
                reach: 2,
                air: 2,
                power: 5,
                kitBehavior: "None");
            f.Moves = StandardMoves(
                "Lariat", 38,
                "Body Splash", 100,
                "Air Splash", 50,
                surgeA: ("Eruption", 200),
                surgeB: ("Caldera", 220),
                flare: ("Lava Fist", 50));
            return f;
        }

        public static AuthoredFighter CreateCaptainCitrine()
        {
            var f = BaseFighter(
                CaptainCitrineFighterId,
                "Captain Citrine",
                speed: 2,
                life: 4,
                reach: 4,
                air: 1,
                power: 3,
                kitBehavior: "None");
            f.Moves = StandardMoves(
                "Saber", 36,
                "Hook Pull", 92,
                "Air Saber", 46,
                surgeA: ("Grapple Line", 310),
                surgeB: ("Holdfast", 60),
                flare: ("Prism Volley", 44));
            // Surge B Holdfast — Pin 75 f in 3 m (cast.md §5).
            f.Moves[f.Moves.Count - 1].Effect = "Pin";
            return f;
        }

        public static AuthoredFighter CreateTilly()
        {
            var f = BaseFighter(
                TillyFighterId,
                "Tilly Setter",
                speed: 2,
                life: 1,
                reach: 5,
                air: 4,
                power: 1,
                kitBehavior: "InvoiceRepeatable");
            // Invoice: 3 × 45 = 135, 180 Luster (combat charges via KitBehavior).
            f.Moves = new List<AuthoredMove>
            {
                Move("Setter Pick", "Attack", 30, 5, 2, 9),
                Move("Item Hook", "Strong", 82, 10, 3, 18),
                Move("Air Pick", "AirAttack", 40, 6, 3, 12),
                Move("Throw", "Throw", 100, 5, 2, 20),
                Move("Cast and Reel", "Flare", 38, 6, 3, 10),
                Move("Invoice", "Surge", 135, 8, 6, 14),
                Move("Closing Time", "Surge", 150, 16, 6, 28),
            };
            return f;
        }

        public static AuthoredStage CreateCitrineBazaar()
        {
            return new AuthoredStage
            {
                StageId = CitrineBazaarStageId,
                Name = "Citrine Bazaar",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Bazaar",
                        DurationSeconds = "25",
                        GeometrySetId = 0,
                        CameraRadius = "12",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 1,
                                Name = "CandiedDropZone",
                                Trigger = "timer",
                                PeriodSeconds = "10",
                                TelegraphFrames = 30,
                                Damage = 0,
                                CenterX = "0",
                                CenterZ = "2",
                                Radius = "1.5",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Breakaway",
                        DurationSeconds = "25",
                        GeometrySetId = 1,
                        TransitionSeconds = "2",
                        CarryJewels = "drop_chance",
                        CarryJewelsDropChance = "0.5",
                        CarryItems = "all",
                        CameraRadius = "14",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            EndCenterY = "10",
                            EndRadius = "16",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 2,
                                Name = "StallCollision",
                                Trigger = "timer",
                                PeriodSeconds = "8",
                                TelegraphFrames = 40,
                                Damage = 80,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "2",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 1,
                                Name = "StallDrift",
                                PeriodSeconds = "8",
                                Amplitude = "2",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Geode shelf",
                        DurationSeconds = "0",
                        GeometrySetId = 2,
                        TransitionSeconds = "1",
                        CameraRadius = "16",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            StartRadius = "14",
                            EndRadius = "18",
                            EndCenterY = "9",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 3,
                                Name = "SpikeTurretZone",
                                Trigger = "phase_enter",
                                PeriodSeconds = "8",
                                TelegraphFrames = 40,
                                Damage = 90,
                                CenterX = "3",
                                CenterZ = "-2",
                                Radius = "1.2",
                            },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Fulgur Cloister greybox stub (stages.md §2.2): Courtyard → Landslide → Library.
        /// Gong / lantern spirits / bridge collapse deferred; crush track + gutter hazard land now.
        /// </summary>
        public static AuthoredStage CreateFulgurCloister()
        {
            return new AuthoredStage
            {
                StageId = FulgurCloisterStageId,
                Name = "Fulgur Cloister",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Courtyard",
                        DurationSeconds = "40",
                        GeometrySetId = 0,
                        CameraRadius = "14",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 10,
                                Name = "StormGutterFlush",
                                Trigger = "timer",
                                PeriodSeconds = "15",
                                TelegraphFrames = 30,
                                Damage = 0,
                                CenterX = "0",
                                CenterZ = "-3",
                                Radius = "2",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Library",
                        DurationSeconds = "0",
                        GeometrySetId = 1,
                        TransitionSeconds = "4",
                        CarryItems = "held_only",
                        CarryJewels = "all",
                        CameraRadius = "16",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            EndCenterY = "9",
                            EndRadius = "18",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 11,
                                Name = "BookcaseCrush",
                                Trigger = "timer",
                                PeriodSeconds = "10",
                                TelegraphFrames = 45,
                                Damage = 120,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "2.5",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 10,
                                Name = "SlidingBookcase",
                                PeriodSeconds = "10",
                                Amplitude = "3",
                                OriginX = "-1",
                                OriginZ = "0",
                            },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Sunken Strongroom greybox stub (stages.md §2.3): Vault floor → Vault door → Hull breach.
        /// Tidewight / drawers / Teller's Reel deferred; water volumes authored on phases B/C;
        /// rolling door + breach pulse land now.
        /// </summary>
        public static AuthoredStage CreateSunkenStrongroom()
        {
            return new AuthoredStage
            {
                StageId = SunkenStrongroomStageId,
                Name = "Sunken Strongroom",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Vault floor",
                        DurationSeconds = "25",
                        GeometrySetId = 0,
                        CameraRadius = "12",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 20,
                                Name = "TidewightZone",
                                Trigger = "timer",
                                PeriodSeconds = "12",
                                TelegraphFrames = 30,
                                Damage = 0,
                                CenterX = "2",
                                CenterZ = "0",
                                Radius = "1.5",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Vault door",
                        DurationSeconds = "25",
                        GeometrySetId = 1,
                        TransitionSeconds = "2",
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "14",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            EndCenterY = "9",
                            EndRadius = "16",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 21,
                                Name = "RollingDoorKnock",
                                Trigger = "timer",
                                PeriodSeconds = "8",
                                TelegraphFrames = 40,
                                Damage = 120,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "2.5",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 20,
                                Name = "RollingVaultDoor",
                                PeriodSeconds = "8",
                                Amplitude = "3",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                        WaterVolumes = new List<AuthoredWaterVolume>
                        {
                            new AuthoredWaterVolume
                            {
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "8",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Hull breach",
                        DurationSeconds = "0",
                        GeometrySetId = 2,
                        TransitionSeconds = "2",
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "16",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            StartRadius = "14",
                            EndCenterY = "8",
                            EndRadius = "18",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 22,
                                Name = "BreachPull",
                                Trigger = "phase_enter",
                                PeriodSeconds = "10",
                                TelegraphFrames = 40,
                                Damage = 150,
                                CenterX = "4",
                                CenterZ = "-2",
                                Radius = "2",
                            },
                        },
                        WaterVolumes = new List<AuthoredWaterVolume>
                        {
                            new AuthoredWaterVolume
                            {
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "8",
                            },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Rime Cutworks greybox stub (stages.md §2.4): Dry yard → Flooding → Crane yard.
        /// Boardable sleds / ice blocks / raft tip deferred; flooded-floor water authored on phase B;
        /// sled rail + crane sweep land now.
        /// </summary>
        public static AuthoredStage CreateRimeCutworks()
        {
            return new AuthoredStage
            {
                StageId = RimeCutworksStageId,
                Name = "Rime Cutworks",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Dry yard",
                        DurationSeconds = "25",
                        GeometrySetId = 0,
                        CameraRadius = "13",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 30,
                                Name = "CutterCannonZone",
                                Trigger = "timer",
                                PeriodSeconds = "10",
                                TelegraphFrames = 30,
                                Damage = 120,
                                CenterX = "-2",
                                CenterZ = "1",
                                Radius = "1.8",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 30,
                                Name = "CutterSledRail",
                                PeriodSeconds = "12",
                                Amplitude = "2.5",
                                OriginX = "0",
                                OriginZ = "2",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Flooding",
                        DurationSeconds = "25",
                        GeometrySetId = 1,
                        TransitionSeconds = "2",
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "14",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            EndCenterY = "9",
                            EndRadius = "16",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 31,
                                Name = "DriftSledBump",
                                Trigger = "timer",
                                PeriodSeconds = "8",
                                TelegraphFrames = 35,
                                Damage = 70,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "2",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 31,
                                Name = "FloatingSledDrift",
                                PeriodSeconds = "8",
                                Amplitude = "3",
                                OriginX = "1",
                                OriginZ = "0",
                            },
                        },
                        WaterVolumes = new List<AuthoredWaterVolume>
                        {
                            new AuthoredWaterVolume
                            {
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "8",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Crane yard",
                        DurationSeconds = "0",
                        GeometrySetId = 2,
                        TransitionSeconds = "2",
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "16",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            StartRadius = "14",
                            EndCenterY = "10",
                            EndRadius = "18",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 32,
                                Name = "MagnetCraneSweep",
                                Trigger = "phase_enter",
                                PeriodSeconds = "12",
                                TelegraphFrames = 45,
                                Damage = 100,
                                CenterX = "0",
                                CenterZ = "-1",
                                Radius = "2.2",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 32,
                                Name = "OverheadMagnetCrane",
                                PeriodSeconds = "12",
                                Amplitude = "4",
                                OriginX = "0",
                                OriginZ = "-1",
                            },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// The Orrery greybox stub (stages.md §2.5): Gear floor → Infestation → Planet disc.
        /// Void beetle AI / egg sacs / gondolas / updraft ride deferred; conveyor + spit/swipe + arm gap land now.
        /// </summary>
        public static AuthoredStage CreateOrrery()
        {
            return new AuthoredStage
            {
                StageId = OrreryStageId,
                Name = "The Orrery",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Gear floor",
                        DurationSeconds = "25",
                        GeometrySetId = 0,
                        CameraRadius = "13",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 40,
                                Name = "UpdraftRing",
                                Trigger = "timer",
                                PeriodSeconds = "10",
                                TelegraphFrames = 30,
                                Damage = 0,
                                CenterX = "0",
                                CenterZ = "2",
                                Radius = "1.5",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 40,
                                Name = "FloorConveyor",
                                PeriodSeconds = "10",
                                Amplitude = "2",
                                OriginX = "-2",
                                OriginZ = "0",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Infestation",
                        DurationSeconds = "30",
                        GeometrySetId = 1,
                        TransitionSeconds = "0",
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "14",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 41,
                                Name = "BeetleSpitPuddle",
                                Trigger = "timer",
                                PeriodSeconds = "8",
                                TelegraphFrames = 30,
                                Damage = 0,
                                CenterX = "1",
                                CenterZ = "1",
                                Radius = "1.8",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 42,
                                Name = "BeetleTailSwipe",
                                Trigger = "timer",
                                PeriodSeconds = "10",
                                TelegraphFrames = 40,
                                Damage = 100,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "2.5",
                            },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "Planet disc",
                        DurationSeconds = "0",
                        GeometrySetId = 2,
                        TransitionSeconds = "2",
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "16",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            StartRadius = "14",
                            EndCenterY = "11",
                            EndRadius = "18",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 43,
                                Name = "ArmGapFallout",
                                Trigger = "phase_enter",
                                PeriodSeconds = "12",
                                TelegraphFrames = 60,
                                Damage = 150,
                                CenterX = "4",
                                CenterZ = "0",
                                Radius = "1.5",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 44,
                                Name = "GustTowardGap",
                                Trigger = "timer",
                                PeriodSeconds = "12",
                                TelegraphFrames = 60,
                                Damage = 0,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "3",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 41,
                                Name = "RotatingArm",
                                PeriodSeconds = "12",
                                Amplitude = "3",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Terraces — The Harvester (stages.md §3.1). Jewel cap 6.
        /// Boss AI v1: footfall follows HarvesterPatrol; intake tracks nearest fighter + throw;
        /// StompShock applies Dizzy (damage 0). Terrace crates via authored crate_points;
        /// legs/head/tail/mount/Buck via BossPartRuntime; Cutters gravel via ProjectileRuntime.
        /// </summary>
        public static AuthoredStage CreateHarvesterTerraces()
        {
            return new AuthoredStage
            {
                StageId = HarvesterTerracesStageId,
                Name = "Terraces — The Harvester",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 6,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Terrace hunt",
                        DurationSeconds = "0",
                        GeometrySetId = 0,
                        CameraRadius = "16",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 50,
                                Name = "FootfallShadow",
                                Trigger = "timer",
                                PeriodSeconds = "8",
                                TelegraphFrames = 30,
                                Damage = 400,
                                CenterX = "0",
                                CenterZ = "2",
                                Radius = "2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 51,
                                Name = "IntakeCone",
                                Trigger = "timer",
                                PeriodSeconds = "12",
                                TelegraphFrames = 40,
                                Damage = 300,
                                CenterX = "3",
                                CenterZ = "0",
                                Radius = "2.5",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 52,
                                Name = "CuttingBeamLine",
                                Trigger = "timer",
                                PeriodSeconds = "20",
                                TelegraphFrames = 60,
                                Damage = 500,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "1.2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 53,
                                Name = "StompShock",
                                Trigger = "timer",
                                PeriodSeconds = "15",
                                TelegraphFrames = 40,
                                Damage = 0,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "4",
                            },
                            // Abrasive Spray (stages.md §3.1): 50/s while inside; side nozzles.
                            // Period 1 s + pulse overlap ≈ continuous 50/s; stun skipped in Sim.
                            new AuthoredHazard
                            {
                                HazardId = 54,
                                Name = "AbrasiveSprayL",
                                Trigger = "timer",
                                PeriodSeconds = "1",
                                TelegraphFrames = 0,
                                Damage = 50,
                                CenterX = "-6",
                                CenterZ = "0",
                                Radius = "2.5",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 55,
                                Name = "AbrasiveSprayR",
                                Trigger = "timer",
                                PeriodSeconds = "1",
                                TelegraphFrames = 0,
                                Damage = 50,
                                CenterX = "6",
                                CenterZ = "0",
                                Radius = "2.5",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 50,
                                Name = "HarvesterPatrol",
                                PeriodSeconds = "16",
                                Amplitude = "4",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                        CratePoints = new List<AuthoredCratePoint>
                        {
                            new AuthoredCratePoint { X = "6", Z = "5" },
                            new AuthoredCratePoint { X = "-6", Z = "5" },
                            new AuthoredCratePoint { X = "6", Z = "-5" },
                            new AuthoredCratePoint { X = "-6", Z = "-5" },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// The Pressworks — Chairman Vitre (stages.md §3.2). Jewel cap 6.
        /// Boss AI v1: LiveHandsChase relocates toward nearest fighter; Flood timeout landed.
        /// Vein Guards via authored creatures[] + StageCreatureRuntime (gate at 4 / 90 s);
        /// Vitre Life + Overpress via VitreBossRuntime; Flood timeout landed.
        /// </summary>
        /// <summary>Extra Arena: Harvester geometry without the boss (stages.md §4.1).</summary>
        public static AuthoredStage CreateTerracesNoBoss()
        {
            return new AuthoredStage
            {
                StageId = TerracesNoBossStageId,
                Name = "Terraces (no boss)",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Open terrace",
                        DurationSeconds = "0",
                        GeometrySetId = 0,
                        CameraRadius = "16",
                        Hazards = new List<AuthoredHazard>
                        {
                            // Spine crystals: hit → spine volley stub (damage on contact).
                            new AuthoredHazard
                            {
                                HazardId = 101,
                                Name = "SpineCrystalN",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 25,
                                CenterX = "0",
                                CenterZ = "6",
                                Radius = "1.2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 102,
                                Name = "SpineCrystalS",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 25,
                                CenterX = "0",
                                CenterZ = "-6",
                                Radius = "1.2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 103,
                                Name = "SpineCrystalE",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 25,
                                CenterX = "6",
                                CenterZ = "0",
                                Radius = "1.2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 104,
                                Name = "SpineCrystalW",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 25,
                                CenterX = "-6",
                                CenterZ = "0",
                                Radius = "1.2",
                            },
                        },
                        CratePoints = new List<AuthoredCratePoint>
                        {
                            new AuthoredCratePoint { X = "5", Z = "5" },
                            new AuthoredCratePoint { X = "-5", Z = "5" },
                            new AuthoredCratePoint { X = "5", Z = "-5" },
                            new AuthoredCratePoint { X = "-5", Z = "-5" },
                            new AuthoredCratePoint { X = "0", Z = "0" },
                            new AuthoredCratePoint { X = "8", Z = "0" },
                            new AuthoredCratePoint { X = "-8", Z = "0" },
                        },
                    },
                },
            };
        }

        /// <summary>Extra Arena: tiny scrap stage (stages.md §4.2).</summary>
        public static AuthoredStage CreateBehindTheCounter()
        {
            return new AuthoredStage
            {
                StageId = BehindCounterStageId,
                Name = "Behind the Counter",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Shop floor",
                        DurationSeconds = "0",
                        GeometrySetId = 0,
                        CameraRadius = "8",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 110,
                                Name = "ShelfDropN",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 10,
                                Damage = 40,
                                CenterX = "0",
                                CenterZ = "3",
                                Radius = "1",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 111,
                                Name = "ShelfDropE",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 10,
                                Damage = 40,
                                CenterX = "3",
                                CenterZ = "0",
                                Radius = "1",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 112,
                                Name = "ShelfDropW",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 10,
                                Damage = 40,
                                CenterX = "-3",
                                CenterZ = "0",
                                Radius = "1",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 110,
                                Name = "LampStandard",
                                PeriodSeconds = "0",
                                Amplitude = "0",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                        CratePoints = new List<AuthoredCratePoint>
                        {
                            new AuthoredCratePoint { X = "2", Z = "2" },
                            new AuthoredCratePoint { X = "-2", Z = "2" },
                            new AuthoredCratePoint { X = "2", Z = "-2" },
                            new AuthoredCratePoint { X = "-2", Z = "-2" },
                        },
                    },
                },
            };
        }

        /// <summary>Extra Arena: spars over star-well, fall_is_ko (stages.md §4.3).</summary>
        public static AuthoredStage CreateSpirewalk()
        {
            return new AuthoredStage
            {
                StageId = SpirewalkStageId,
                Name = "Spirewalk",
                JewelCrateChancePercent = 8,
                FallIsKo = true,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Spars",
                        DurationSeconds = "0",
                        GeometrySetId = 0,
                        CameraRadius = "14",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 120,
                                Name = "StarWell",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 0,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "3",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 120,
                                Name = "BannerSpringA",
                                PeriodSeconds = "0",
                                Amplitude = "0",
                                OriginX = "4",
                                OriginZ = "2",
                            },
                            new AuthoredKinematic
                            {
                                TrackId = 121,
                                Name = "BannerSpringB",
                                PeriodSeconds = "0",
                                Amplitude = "0",
                                OriginX = "-4",
                                OriginZ = "-2",
                            },
                        },
                        CratePoints = new List<AuthoredCratePoint>
                        {
                            new AuthoredCratePoint { X = "0", Z = "5" },
                        },
                    },
                },
            };
        }

        /// <summary>Extra Arena: cascade crates + water gaps, fall_is_ko (stages.md §4.4).</summary>
        public static AuthoredStage CreateCascade()
        {
            return new AuthoredStage
            {
                StageId = CascadeStageId,
                Name = "The Cascade",
                JewelCrateChancePercent = 8,
                FallIsKo = true,
                JewelCapOverride = 0,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Three tiers",
                        DurationSeconds = "0",
                        GeometrySetId = 0,
                        CameraRadius = "15",
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 130,
                                Name = "WallSpringL",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 0,
                                CenterX = "-5",
                                CenterZ = "0",
                                Radius = "1",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 131,
                                Name = "WallSpringR",
                                Trigger = "proximity",
                                PeriodSeconds = "0",
                                TelegraphFrames = 0,
                                Damage = 0,
                                CenterX = "5",
                                CenterZ = "0",
                                Radius = "1",
                            },
                        },
                        WaterVolumes = new List<AuthoredWaterVolume>
                        {
                            new AuthoredWaterVolume
                            {
                                CenterX = "0",
                                CenterZ = "-4",
                                Radius = "4",
                            },
                            new AuthoredWaterVolume
                            {
                                CenterX = "0",
                                CenterZ = "4",
                                Radius = "3",
                            },
                        },
                        CratePoints = new List<AuthoredCratePoint>
                        {
                            new AuthoredCratePoint { X = "-3", Z = "2" },
                            new AuthoredCratePoint { X = "0", Z = "2" },
                            new AuthoredCratePoint { X = "3", Z = "2" },
                            new AuthoredCratePoint { X = "-2", Z = "0" },
                            new AuthoredCratePoint { X = "2", Z = "0" },
                            new AuthoredCratePoint { X = "0", Z = "-2" },
                        },
                    },
                },
            };
        }

        public static AuthoredStage CreatePressworks()
        {
            return new AuthoredStage
            {
                StageId = PressworksStageId,
                Name = "The Pressworks",
                JewelCrateChancePercent = 8,
                FallIsKo = false,
                JewelCapOverride = 6,
                Phases = new List<AuthoredPhase>
                {
                    new AuthoredPhase
                    {
                        Name = "Sorting line",
                        DurationSeconds = "90",
                        GeometrySetId = 0,
                        BlitzSuppressed = true,
                        CameraRadius = "14",
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 60,
                                Name = "SortingConveyor",
                                PeriodSeconds = "10",
                                Amplitude = "2",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                        // Vein Guards (stages.md §3.2) — kinds 1–5; drop via Kind→stage weapon.
                        Creatures = new List<AuthoredCreature>
                        {
                            new AuthoredCreature { KindId = 1, X = "-6", Z = "3", MaxLife = 300 },
                            new AuthoredCreature { KindId = 2, X = "-3", Z = "-3", MaxLife = 300 },
                            new AuthoredCreature { KindId = 3, X = "0", Z = "4", MaxLife = 300 },
                            new AuthoredCreature { KindId = 4, X = "3", Z = "-2", MaxLife = 300 },
                            new AuthoredCreature { KindId = 5, X = "6", Z = "2", MaxLife = 300 },
                        },
                    },
                    new AuthoredPhase
                    {
                        Name = "The Press",
                        DurationSeconds = "0",
                        GeometrySetId = 1,
                        TransitionSeconds = "2",
                        BlitzSuppressed = false,
                        CarryItems = "all",
                        CarryJewels = "all",
                        CameraRadius = "16",
                        ScriptedCamera = new AuthoredScriptedCamera
                        {
                            Enabled = true,
                            EndCenterY = "10",
                            EndRadius = "18",
                        },
                        Hazards = new List<AuthoredHazard>
                        {
                            new AuthoredHazard
                            {
                                HazardId = 61,
                                Name = "PistonStabs",
                                Trigger = "timer",
                                PeriodSeconds = "8",
                                TelegraphFrames = 45,
                                Damage = 150,
                                CenterX = "0",
                                CenterZ = "0",
                                Radius = "2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 62,
                                Name = "PoisonSpewPool",
                                Trigger = "timer",
                                PeriodSeconds = "12",
                                TelegraphFrames = 40,
                                Damage = 40,
                                CenterX = "2",
                                CenterZ = "-1",
                                Radius = "2",
                            },
                            new AuthoredHazard
                            {
                                HazardId = 63,
                                Name = "LiveHandsChase",
                                Trigger = "timer",
                                PeriodSeconds = "6",
                                TelegraphFrames = 30,
                                Damage = 120,
                                CenterX = "-1",
                                CenterZ = "2",
                                Radius = "1.5",
                            },
                        },
                        Kinematics = new List<AuthoredKinematic>
                        {
                            new AuthoredKinematic
                            {
                                TrackId = 61,
                                Name = "PressHeadRing",
                                PeriodSeconds = "12",
                                Amplitude = "2.5",
                                OriginX = "0",
                                OriginZ = "0",
                            },
                        },
                    },
                },
            };
        }

        public static List<AuthoredItem> CreateStarterSet()
        {
            var items = new List<AuthoredItem>();
            int id = 1;

            void Add(
                string name,
                string category,
                int rarity,
                int damage,
                int heal,
                int buy,
                int sell,
                params string[] tags)
            {
                items.Add(new AuthoredItem
                {
                    ItemId = id++,
                    Name = name,
                    Category = category,
                    Rarity = rarity,
                    CrateEligibility = "Always",
                    Pocketable = category != "Summon",
                    DamagePerUse = damage,
                    HealPerUse = heal,
                    BuyPrice = buy,
                    SellPrice = sell,
                    Tags = new List<string>(tags ?? Array.Empty<string>()),
                });
            }

            Add("Pistol", "Firearm", 1, 25, 0, 100, 40, "powder", "metal");
            Add("Flare Pistol", "Firearm", 2, 30, 0, 180, 70, "flame", "powder");
            Add("Iron Pipe", "Melee", 1, 30, 0, 80, 30, "metal");
            Add("Bat", "Melee", 1, 28, 0, 70, 25, "paper");
            Add("Pry Bar", "Melee", 2, 35, 0, 120, 45, "metal");
            Add("Mallet", "Melee", 2, 40, 0, 140, 50, "stone", "metal");
            Add("Paper Fan", "Melee", 1, 15, 0, 60, 20, "paper");
            Add("Cutlass", "Melee", 3, 50, 0, 220, 80, "metal");
            Add("Oil Bottle", "Explosive", 1, 35, 0, 90, 35, "flame");
            Add("Small Bomb", "Explosive", 2, 45, 0, 150, 55, "powder");
            Add("Medium Bomb", "Explosive", 3, 60, 0, 240, 90, "powder");
            Add("Barrel", "Explosive", 3, 70, 0, 260, 100, "powder", "stone");
            Add("Apple", "Healing", 1, 0, 80, 40, 15, "food");
            Add("Pear", "Healing", 1, 0, 90, 45, 18, "food");
            Add("Skewer", "Healing", 2, 0, 120, 80, 30, "food", "metal");
            Add("Shortcake", "Healing", 2, 0, 150, 100, 40, "food");
            Add("Steak", "Healing", 3, 0, 200, 160, 60, "food");
            // ItemId 18 — Adhesive Spray applies Slow on thrown hit (items-and-economy.md).
            Add("Adhesive Spray", "Status", 2, 0, 0, 110, 40, "glass");
            // ItemId 19 — Petrify Gas applies Freeze 90 f on thrown hit (balance.md §4).
            Add("Petrify Gas", "Status", 2, 0, 0, 130, 50, "stone");
            // ItemId 20 — Smoke Jar lasting Blind floor patch.
            Add("Smoke Jar", "Status", 1, 0, 0, 70, 25, "powder");
            // ItemId 21 — Ice Jar lasting Freeze floor patch.
            Add("Ice Jar", "Status", 3, 0, 0, 200, 75, "ice");
            Add("Rollerskates", "Mobility", 2, 0, 0, 150, 55, "metal");
            Add("Spring Boots", "Mobility", 3, 0, 0, 220, 80, "metal", "star");
            Add("Kitten", "Summon", 3, 20, 0, 300, 100, "star");
            Add("Trumpet", "Toy", 1, 10, 0, 50, 20, "metal");

            // Vein Guard stage weapons (stages.md §3.2) — Never crate-eligible; Sim FlagStageOnly.
            void AddStageWeapon(int itemId, string name, int damage)
            {
                items.Add(new AuthoredItem
                {
                    ItemId = itemId,
                    Name = name,
                    Category = "StageWeapon",
                    Rarity = 1,
                    CrateEligibility = "Never",
                    Pocketable = false,
                    DamagePerUse = damage,
                    HealPerUse = 0,
                    BuyPrice = 0,
                    SellPrice = 0,
                });
            }

            AddStageWeapon(StageMortarItemId, "Stage Mortar", 40);
            AddStageWeapon(StageFrostStaffItemId, "Stage Frost Staff", 35);
            AddStageWeapon(StageMalletItemId, "Stage Mallet", 40);
            AddStageWeapon(StageNetItemId, "Stage Net", 20);
            AddStageWeapon(StageFlameItemId, "Stage Flame", 40);

            return items;
        }

        private static AuthoredFighter BaseFighter(
            int id,
            string name,
            int speed,
            int life,
            int reach,
            int air,
            int power,
            string kitBehavior)
        {
            var f = new AuthoredFighter
            {
                FighterId = id,
                Name = name,
                SpeedRating = speed,
                LifeRating = life,
                ReachRating = reach,
                AirRating = air,
                PowerRating = power,
                KitBehavior = kitBehavior,
            };
            ApplySpeedFromRating(f);
            return f;
        }

        private static List<AuthoredMove> StandardMoves(
            string attackName,
            int attackDamage,
            string strongName,
            int strongDamage,
            string airName,
            int airDamage,
            (string Name, int Damage) surgeA,
            (string Name, int Damage) surgeB,
            (string Name, int Damage) flare,
            string throwName = "Throw",
            int throwDamage = 100)
        {
            return new List<AuthoredMove>
            {
                Move(attackName, "Attack", attackDamage, 4, 2, 8),
                Move(strongName, "Strong", strongDamage, 10, 3, 18),
                Move(airName, "AirAttack", airDamage, 6, 3, 12),
                Move(throwName, "Throw", throwDamage, 5, 2, 20),
                Move(flare.Name, "Flare", flare.Damage, 6, 3, 10),
                Move(surgeA.Name, "Surge", surgeA.Damage, 12, 6, 24),
                Move(surgeB.Name, "Surge", surgeB.Damage, 14, 8, 28),
            };
        }

        private static AuthoredMove Move(
            string name,
            string moveClass,
            int damage,
            int startup,
            int active,
            int recovery,
            bool utilityExempt = false,
            string effect = "None")
        {
            return new AuthoredMove
            {
                Name = name,
                Class = moveClass,
                Damage = damage,
                StartupFrames = startup,
                ActiveFrames = active,
                RecoveryFrames = recovery,
                UtilityExempt = utilityExempt,
                Effect = effect,
            };
        }
    }
}
