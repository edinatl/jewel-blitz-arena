using System;
using System.IO;
using System.Text;

namespace JewelBlitzArena.Data
{
    public static class ContentBlobReader
    {
        public static ContentCatalog Read(byte[] blob)
        {
            using var ms = new MemoryStream(blob, writable: false);
            using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

            byte[] magic = br.ReadBytes(4);
            if (magic.Length != 4
                || magic[0] != ContentSchema.Magic[0]
                || magic[1] != ContentSchema.Magic[1]
                || magic[2] != ContentSchema.Magic[2]
                || magic[3] != ContentSchema.Magic[3])
            {
                throw new InvalidDataException("Not a Jewel Blitz Arena content blob (bad magic).");
            }

            uint version = br.ReadUInt32();
            if (version != ContentSchema.Version)
            {
                throw new InvalidDataException($"Unsupported content schema version {version}.");
            }

            ulong embeddedHash = br.ReadUInt64();
            int payloadLength = br.ReadInt32();
            if (payloadLength < 0 || payloadLength > blob.Length)
            {
                throw new InvalidDataException("Corrupt content blob length.");
            }

            byte[] payload = br.ReadBytes(payloadLength);
            if (payload.Length != payloadLength)
            {
                throw new InvalidDataException("Truncated content blob payload.");
            }

            ulong actual = ContentXxHash64.Hash(payload);
            if (actual != embeddedHash)
            {
                throw new InvalidDataException(
                    $"Content hash mismatch (embedded 0x{embeddedHash:X16}, actual 0x{actual:X16}).");
            }

            using var pms = new MemoryStream(payload, writable: false);
            using var pbr = new BinaryReader(pms, Encoding.UTF8, leaveOpen: true);

            int fighterCount = pbr.ReadInt32();
            var fighters = new FighterKitRecord[fighterCount];
            for (int i = 0; i < fighterCount; i++)
            {
                fighters[i] = ReadFighter(pbr);
            }

            int stageCount = pbr.ReadInt32();
            var stages = new StageRecord[stageCount];
            for (int i = 0; i < stageCount; i++)
            {
                stages[i] = ReadStage(pbr);
            }

            int itemCount = pbr.ReadInt32();
            var items = new ItemRecord[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                items[i] = ReadItem(pbr);
            }

            return new ContentCatalog(fighters, stages, items, embeddedHash);
        }

        private static FighterKitRecord ReadFighter(BinaryReader br)
        {
            var f = new FighterKitRecord
            {
                FighterId = br.ReadInt32(),
                Name = ReadString(br),
                SpeedRating = br.ReadInt32(),
                LifeRating = br.ReadInt32(),
                MaxLife = br.ReadInt32(),
                ReachRating = br.ReadInt32(),
                AirRating = br.ReadInt32(),
                PowerRating = br.ReadInt32(),
                WalkSpeedRaw = br.ReadInt64(),
                RunSpeedRaw = br.ReadInt64(),
                FullSpeedRaw = br.ReadInt64(),
                KitBehavior = br.ReadByte(),
            };

            int moveCount = br.ReadInt32();
            f.Moves = new MoveRecord[moveCount];
            for (int i = 0; i < moveCount; i++)
            {
                f.Moves[i] = new MoveRecord
                {
                    Name = ReadString(br),
                    Class = (MoveClass)br.ReadByte(),
                    Damage = br.ReadInt32(),
                    StartupFrames = br.ReadInt32(),
                    ActiveFrames = br.ReadInt32(),
                    RecoveryFrames = br.ReadInt32(),
                    UtilityExempt = br.ReadBoolean(),
                    Effect = (MoveEffect)br.ReadByte(),
                };
            }

            return f;
        }

        private static StageRecord ReadStage(BinaryReader br)
        {
            var s = new StageRecord
            {
                StageId = br.ReadInt32(),
                Name = ReadString(br),
                JewelCrateChancePercent = br.ReadInt32(),
                FallIsKo = br.ReadBoolean(),
                JewelCapOverride = br.ReadInt32(),
            };

            int phaseCount = br.ReadInt32();
            s.Phases = new StagePhaseRecord[phaseCount];
            for (int i = 0; i < phaseCount; i++)
            {
                var phase = new StagePhaseRecord
                {
                    Name = ReadString(br),
                    DurationFrames = br.ReadInt32(),
                    GeometrySetId = br.ReadInt32(),
                    BlitzSuppressed = br.ReadBoolean(),
                    TransitionFrames = br.ReadInt32(),
                    CarryItems = (CarryItemsMode)br.ReadByte(),
                    CarryJewels = (CarryJewelsMode)br.ReadByte(),
                    CarryJewelsDropPermille = br.ReadInt32(),
                    CameraCenterXRaw = br.ReadInt64(),
                    CameraCenterYRaw = br.ReadInt64(),
                    CameraCenterZRaw = br.ReadInt64(),
                    CameraRadiusRaw = br.ReadInt64(),
                };

                phase.HasScriptedCamera = br.ReadBoolean();
                if (phase.HasScriptedCamera)
                {
                    phase.ScriptedCameraMask = br.ReadByte();
                    phase.ScriptedStartCenterXRaw = br.ReadInt64();
                    phase.ScriptedStartCenterYRaw = br.ReadInt64();
                    phase.ScriptedStartCenterZRaw = br.ReadInt64();
                    phase.ScriptedStartRadiusRaw = br.ReadInt64();
                    phase.ScriptedEndCenterXRaw = br.ReadInt64();
                    phase.ScriptedEndCenterYRaw = br.ReadInt64();
                    phase.ScriptedEndCenterZRaw = br.ReadInt64();
                    phase.ScriptedEndRadiusRaw = br.ReadInt64();
                }

                int hazardCount = br.ReadInt32();
                phase.Hazards = new StageHazardRecord[hazardCount];
                for (int h = 0; h < hazardCount; h++)
                {
                    phase.Hazards[h] = new StageHazardRecord
                    {
                        HazardId = br.ReadInt32(),
                        Name = ReadString(br),
                        Trigger = (HazardTriggerKind)br.ReadByte(),
                        PeriodFrames = br.ReadInt32(),
                        TelegraphFrames = br.ReadInt32(),
                        Damage = br.ReadInt32(),
                        CenterXRaw = br.ReadInt64(),
                        CenterYRaw = br.ReadInt64(),
                        CenterZRaw = br.ReadInt64(),
                        RadiusRaw = br.ReadInt64(),
                    };
                }

                int kinCount = br.ReadInt32();
                phase.Kinematics = new StageKinematicRecord[kinCount];
                for (int k = 0; k < kinCount; k++)
                {
                    phase.Kinematics[k] = new StageKinematicRecord
                    {
                        TrackId = br.ReadInt32(),
                        Name = ReadString(br),
                        PeriodFrames = br.ReadInt32(),
                        AmplitudeRaw = br.ReadInt64(),
                        OriginXRaw = br.ReadInt64(),
                        OriginYRaw = br.ReadInt64(),
                        OriginZRaw = br.ReadInt64(),
                    };
                }

                int crateCount = br.ReadInt32();
                phase.CratePoints = new StageCratePointRecord[crateCount];
                for (int c = 0; c < crateCount; c++)
                {
                    phase.CratePoints[c] = new StageCratePointRecord
                    {
                        XRaw = br.ReadInt64(),
                        ZRaw = br.ReadInt64(),
                    };
                }

                int waterCount = br.ReadInt32();
                phase.WaterVolumes = new StageWaterVolumeRecord[waterCount];
                for (int w = 0; w < waterCount; w++)
                {
                    phase.WaterVolumes[w] = new StageWaterVolumeRecord
                    {
                        CenterXRaw = br.ReadInt64(),
                        CenterZRaw = br.ReadInt64(),
                        RadiusRaw = br.ReadInt64(),
                    };
                }

                s.Phases[i] = phase;
            }

            return s;
        }

        private static ItemRecord ReadItem(BinaryReader br)
        {
            return new ItemRecord
            {
                ItemId = br.ReadInt32(),
                Name = ReadString(br),
                Category = (ItemCategory)br.ReadByte(),
                Rarity = br.ReadInt32(),
                CrateEligibility = (CrateEligibility)br.ReadByte(),
                Pocketable = br.ReadBoolean(),
                BuyPrice = br.ReadInt32(),
                SellPrice = br.ReadInt32(),
                DamagePerUse = br.ReadInt32(),
                HealPerUse = br.ReadInt32(),
            };
        }

        private static string ReadString(BinaryReader br)
        {
            int length = br.ReadInt32();
            if (length < 0 || length > 1024)
            {
                throw new InvalidDataException("Corrupt string length in content blob.");
            }

            byte[] bytes = br.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
