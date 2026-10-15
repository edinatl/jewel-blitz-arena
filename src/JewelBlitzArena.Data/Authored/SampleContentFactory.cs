using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    /// <summary>
    /// S5 stub pack: full cast kits (cast.md §6 + kit behaviors), Citrine Bazaar phases,
    /// 25 always-eligible starter items.
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
                Stages = new List<AuthoredStage> { CreateCitrineBazaar() },
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
                Move("Smoke Vault", "Surge", 0, 12, 4, 24, effect: "Utility"),
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
                Move("Toolbox Mine Field", "Surge", 70, 12, 8, 20),
                Move("Spare Magnet", "Surge", 0, 10, 6, 18, effect: "Magnet"),
                Move("Spare Flame", "Surge", 200, 12, 8, 24),
                Move("Spare Chaff", "Surge", 40, 8, 4, 16),
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

        public static List<AuthoredItem> CreateStarterSet()
        {
            var items = new List<AuthoredItem>();
            int id = 1;

            void Add(string name, string category, int rarity, int damage, int heal, int buy, int sell)
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
                });
            }

            Add("Pistol", "Firearm", 1, 25, 0, 100, 40);
            Add("Flare Pistol", "Firearm", 2, 30, 0, 180, 70);
            Add("Iron Pipe", "Melee", 1, 30, 0, 80, 30);
            Add("Bat", "Melee", 1, 28, 0, 70, 25);
            Add("Pry Bar", "Melee", 2, 35, 0, 120, 45);
            Add("Mallet", "Melee", 2, 40, 0, 140, 50);
            Add("Paper Fan", "Melee", 1, 15, 0, 60, 20);
            Add("Cutlass", "Melee", 3, 50, 0, 220, 80);
            Add("Oil Bottle", "Explosive", 1, 35, 0, 90, 35);
            Add("Small Bomb", "Explosive", 2, 45, 0, 150, 55);
            Add("Medium Bomb", "Explosive", 3, 60, 0, 240, 90);
            Add("Barrel", "Explosive", 3, 70, 0, 260, 100);
            Add("Apple", "Healing", 1, 0, 80, 40, 15);
            Add("Pear", "Healing", 1, 0, 90, 45, 18);
            Add("Skewer", "Healing", 2, 0, 120, 80, 30);
            Add("Shortcake", "Healing", 2, 0, 150, 100, 40);
            Add("Steak", "Healing", 3, 0, 200, 160, 60);
            Add("Adhesive Spray", "Status", 2, 0, 0, 110, 40);
            Add("Small Shield", "Status", 2, 0, 0, 130, 50);
            Add("Smoke Jar", "Status", 1, 0, 0, 70, 25);
            Add("Leg Irons", "Status", 3, 0, 0, 200, 75);
            Add("Rollerskates", "Mobility", 2, 0, 0, 150, 55);
            Add("Spring Boots", "Mobility", 3, 0, 0, 220, 80);
            Add("Kitten", "Summon", 3, 20, 0, 300, 100);
            Add("Trumpet", "Toy", 1, 10, 0, 50, 20);

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
