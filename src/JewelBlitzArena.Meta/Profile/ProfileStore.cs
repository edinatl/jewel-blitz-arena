using System;
using System.IO;
using System.Text;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Atomic profile IO (temp + rename) with one rolling `.bak`. Binary, versioned payload.
    /// </summary>
    public sealed class ProfileStore
    {
        public const uint Magic = 0x4A424150; // 'JBAP'
        private readonly string _path;

        public ProfileStore(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path => _path;

        public string BackupPath => _path + ".bak";

        public PlayerProfile LoadOrCreate()
        {
            if (!File.Exists(_path))
            {
                return PlayerProfile.CreateNew();
            }

            byte[] bytes = File.ReadAllBytes(_path);
            PlayerProfile profile = ReadBytes(bytes);
            return ProfileMigrator.MigrateToCurrent(profile);
        }

        public void Save(PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            ProfileMigrator.MigrateToCurrent(profile);
            byte[] bytes = WriteBytes(profile);

            string? dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string temp = _path + ".tmp";
            File.WriteAllBytes(temp, bytes);

            if (File.Exists(_path))
            {
                File.Copy(_path, BackupPath, overwrite: true);
            }

            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }

        public static byte[] WriteBytes(PlayerProfile profile)
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(Magic);
                bw.Write(profile.SchemaVersion);
                WriteString(bw, profile.DisplayName);
                bw.Write(profile.Carats);
                bw.Write(profile.DistinctTournamentClears);
                bw.Write(profile.Flags);
                bw.Write(profile.UnlockedHouseRulesBits);
                WriteHouseRules(bw, profile.HouseRulesDefaults);
                // v2+: BestExpeditionDifficulty sits before Ledger so v1 blobs still parse.
                bw.Write(profile.BestExpeditionDifficulty);
                // v3+: Chits + FacetWheelWidthLevel.
                bw.Write(profile.Chits);
                bw.Write(profile.FacetWheelWidthLevel);
                // v4+: PlaytimeSeconds + CaratsSpentLifetime.
                bw.Write(profile.PlaytimeSeconds);
                bw.Write(profile.CaratsSpentLifetime);
                // v5+: ProfileSalt + TinkerCounter.
                bw.Write(profile.ProfileSalt);
                bw.Write(profile.TinkerCounter);

                int[] owned = profile.Ledger.ToArray();
                bw.Write(owned.Length);
                for (int i = 0; i < owned.Length; i++)
                {
                    bw.Write(owned[i]);
                }

                // v6+: Bench Notes after Ledger.
                if (profile.SchemaVersion >= ProfileSchema.Version6)
                {
                    WriteBenchNotes(bw, profile.BenchNotes);
                }

                // v7+: ShopStockSeed after Bench Notes.
                if (profile.SchemaVersion >= ProfileSchema.Version7)
                {
                    bw.Write(profile.ShopStockSeed);
                }
            }

            return ms.ToArray();
        }

        public static PlayerProfile ReadBytes(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

            uint magic = br.ReadUInt32();
            if (magic != Magic)
            {
                throw new InvalidDataException("Not a Jewel Blitz Arena profile (bad magic).");
            }

            var profile = new PlayerProfile
            {
                SchemaVersion = br.ReadInt32(),
                DisplayName = ReadString(br),
                Carats = br.ReadInt32(),
                DistinctTournamentClears = br.ReadInt32(),
                Flags = br.ReadInt32(),
                UnlockedHouseRulesBits = br.ReadInt32(),
                HouseRulesDefaults = ReadHouseRules(br),
            };

            if (profile.SchemaVersion >= ProfileSchema.Version2)
            {
                profile.BestExpeditionDifficulty = br.ReadInt32();
            }

            if (profile.SchemaVersion >= ProfileSchema.Version3)
            {
                profile.Chits = br.ReadInt32();
                profile.FacetWheelWidthLevel = br.ReadInt32();
            }

            if (profile.SchemaVersion >= ProfileSchema.Version4)
            {
                profile.PlaytimeSeconds = br.ReadInt32();
                profile.CaratsSpentLifetime = br.ReadInt32();
            }

            if (profile.SchemaVersion >= ProfileSchema.Version5)
            {
                profile.ProfileSalt = br.ReadUInt64();
                profile.TinkerCounter = br.ReadInt32();
            }

            int count = br.ReadInt32();
            if (count < 0 || count > 10_000)
            {
                throw new InvalidDataException("Corrupt Ledger count.");
            }

            var owned = new int[count];
            for (int i = 0; i < count; i++)
            {
                owned[i] = br.ReadInt32();
            }

            profile.Ledger.ReplaceAll(owned);

            if (profile.SchemaVersion >= ProfileSchema.Version6)
            {
                profile.BenchNotes.ReplaceAll(ReadBenchNotes(br));
            }

            if (profile.SchemaVersion >= ProfileSchema.Version7)
            {
                profile.ShopStockSeed = br.ReadUInt64();
            }

            return profile;
        }

        /// <summary>Build a synthetic v0 profile blob for migration tests.</summary>
        public static PlayerProfile CreateV0Fixture()
        {
            return new PlayerProfile
            {
                SchemaVersion = ProfileSchema.Version0,
                DisplayName = string.Empty,
                Carats = 0,
                DistinctTournamentClears = 0,
                Flags = 0,
                UnlockedHouseRulesBits = 0,
                HouseRulesDefaults = default,
            };
        }

        private static void WriteBenchNotes(BinaryWriter bw, BenchNotesLedger notes)
        {
            var list = notes.ToList();
            bw.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                BenchNotesLedger.Note n = list[i];
                bw.Write(n.InputA);
                bw.Write(n.InputB);
                bw.Write(n.OutcomesSeen);
                bw.Write(n.PoolRevealed);
            }
        }

        private static BenchNotesLedger.Note[] ReadBenchNotes(BinaryReader br)
        {
            int count = br.ReadInt32();
            if (count < 0 || count > 10_000)
            {
                throw new InvalidDataException("Corrupt Bench Notes count.");
            }

            var notes = new BenchNotesLedger.Note[count];
            for (int i = 0; i < count; i++)
            {
                int a = br.ReadInt32();
                int b = br.ReadInt32();
                byte outcomes = br.ReadByte();
                bool revealed = br.ReadBoolean();
                notes[i] = new BenchNotesLedger.Note(a, b, outcomes, revealed);
            }

            return notes;
        }

        private static void WriteHouseRules(BinaryWriter bw, HouseRulesSettings h)
        {
            bw.Write(h.PocketItems);
            bw.Write(h.JewelCap);
            bw.Write(h.LusterTimer);
            bw.Write(h.BlitzedDamageTakenPercent);
            bw.Write(h.BlitzedAttackPercent);
            bw.Write(h.RallySeconds);
            bw.Write(h.PhaseClockPercent);
        }

        private static HouseRulesSettings ReadHouseRules(BinaryReader br) => new HouseRulesSettings
        {
            PocketItems = br.ReadBoolean(),
            JewelCap = br.ReadInt32(),
            LusterTimer = br.ReadBoolean(),
            BlitzedDamageTakenPercent = br.ReadInt32(),
            BlitzedAttackPercent = br.ReadInt32(),
            RallySeconds = br.ReadInt32(),
            PhaseClockPercent = br.ReadInt32(),
        };

        private static void WriteString(BinaryWriter bw, string value)
        {
            string s = value ?? string.Empty;
            byte[] utf8 = Encoding.UTF8.GetBytes(s);
            bw.Write(utf8.Length);
            bw.Write(utf8);
        }

        private static string ReadString(BinaryReader br)
        {
            int len = br.ReadInt32();
            if (len < 0 || len > 1024)
            {
                throw new InvalidDataException("Corrupt profile string.");
            }

            byte[] utf8 = br.ReadBytes(len);
            return Encoding.UTF8.GetString(utf8);
        }
    }
}
