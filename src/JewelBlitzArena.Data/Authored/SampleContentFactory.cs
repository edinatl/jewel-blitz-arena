using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    /// <summary>
    /// S5 stub pack: Rook kit, Citrine Bazaar three phases, 25 always-eligible starter items.
    /// </summary>
    public static class SampleContentFactory
    {
        public const int RookFighterId = 1;
        public const int CitrineBazaarStageId = 1;

        public static AuthoredContentPack CreateS5StubPack()
        {
            return new AuthoredContentPack
            {
                SchemaVersion = ContentSchema.Version,
                Fighters = new List<AuthoredFighter> { CreateRook() },
                Stages = new List<AuthoredStage> { CreateCitrineBazaar() },
                Items = CreateStarterSet(),
            };
        }

        public static AuthoredFighter CreateRook()
        {
            return new AuthoredFighter
            {
                FighterId = RookFighterId,
                Name = "Rook Citrine",
                SpeedRating = 3,
                LifeRating = 3,
                ReachRating = 3,
                AirRating = 3,
                PowerRating = 3,
                WalkSpeed = "3",
                RunSpeed = "5",
                FullSpeed = "7",
                Moves = new List<AuthoredMove>
                {
                    new AuthoredMove { Name = "Jab", Class = "Attack", Damage = 35, StartupFrames = 4, ActiveFrames = 2, RecoveryFrames = 8 },
                    new AuthoredMove { Name = "Strong", Class = "Strong", Damage = 90, StartupFrames = 10, ActiveFrames = 3, RecoveryFrames = 18 },
                    new AuthoredMove { Name = "Air Attack", Class = "AirAttack", Damage = 45, StartupFrames = 6, ActiveFrames = 3, RecoveryFrames = 12 },
                    new AuthoredMove { Name = "Throw", Class = "Throw", Damage = 100, StartupFrames = 5, ActiveFrames = 2, RecoveryFrames = 20 },
                    new AuthoredMove { Name = "Thermal Dive", Class = "Surge", Damage = 300, StartupFrames = 12, ActiveFrames = 6, RecoveryFrames = 24 },
                    new AuthoredMove { Name = "Windlass", Class = "Surge", Damage = 170, StartupFrames = 14, ActiveFrames = 8, RecoveryFrames = 28 },
                },
            };
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
                        // Gust pull-back during Breakaway enter (inherits start from phase A volume).
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
            // Exact 25 always-eligible items from items-and-economy.md §2.1
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

            // Firearm (2)
            Add("Pistol", "Firearm", 1, 25, 0, 100, 40);
            Add("Flare Pistol", "Firearm", 2, 30, 0, 180, 70);
            // Melee (6)
            Add("Iron Pipe", "Melee", 1, 30, 0, 80, 30);
            Add("Bat", "Melee", 1, 28, 0, 70, 25);
            Add("Pry Bar", "Melee", 2, 35, 0, 120, 45);
            Add("Mallet", "Melee", 2, 40, 0, 140, 50);
            Add("Paper Fan", "Melee", 1, 15, 0, 60, 20);
            Add("Cutlass", "Melee", 3, 50, 0, 220, 80);
            // Explosive (4)
            Add("Oil Bottle", "Explosive", 1, 35, 0, 90, 35);
            Add("Small Bomb", "Explosive", 2, 45, 0, 150, 55);
            Add("Medium Bomb", "Explosive", 3, 60, 0, 240, 90);
            Add("Barrel", "Explosive", 3, 70, 0, 260, 100);
            // Healing (5)
            Add("Apple", "Healing", 1, 0, 80, 40, 15);
            Add("Pear", "Healing", 1, 0, 90, 45, 18);
            Add("Skewer", "Healing", 2, 0, 120, 80, 30);
            Add("Shortcake", "Healing", 2, 0, 150, 100, 40);
            Add("Steak", "Healing", 3, 0, 200, 160, 60);
            // Status (4)
            Add("Adhesive Spray", "Status", 2, 0, 0, 110, 40);
            Add("Small Shield", "Status", 2, 0, 0, 130, 50);
            Add("Smoke Jar", "Status", 1, 0, 0, 70, 25);
            Add("Leg Irons", "Status", 3, 0, 0, 200, 75);
            // Mobility (2)
            Add("Rollerskates", "Mobility", 2, 0, 0, 150, 55);
            Add("Spring Boots", "Mobility", 3, 0, 0, 220, 80);
            // Summon (1)
            Add("Kitten", "Summon", 3, 20, 0, 300, 100);
            // Toy (1)
            Add("Trumpet", "Toy", 1, 10, 0, 50, 20);

            return items;
        }
    }
}
