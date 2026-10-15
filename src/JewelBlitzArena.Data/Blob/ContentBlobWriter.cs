using System;
using System.IO;
using System.Text;

namespace JewelBlitzArena.Data
{
    public static class ContentBlobWriter
    {
        public static byte[] Write(ContentCatalog catalog)
        {
            byte[] payload = WritePayload(catalog.Fighters, catalog.Stages, catalog.Items);
            ulong hash = ContentXxHash64.Hash(payload);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
            bw.Write(ContentSchema.Magic);
            bw.Write(ContentSchema.Version);
            bw.Write(hash);
            bw.Write(payload.Length);
            bw.Write(payload);
            bw.Flush();
            return ms.ToArray();
        }

        public static byte[] WritePayload(
            FighterKitRecord[] fighters,
            StageRecord[] stages,
            ItemRecord[] items)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            bw.Write(fighters.Length);
            foreach (FighterKitRecord f in fighters)
            {
                WriteFighter(bw, f);
            }

            bw.Write(stages.Length);
            foreach (StageRecord s in stages)
            {
                WriteStage(bw, s);
            }

            bw.Write(items.Length);
            foreach (ItemRecord i in items)
            {
                WriteItem(bw, i);
            }

            bw.Flush();
            return ms.ToArray();
        }

        private static void WriteFighter(BinaryWriter bw, FighterKitRecord f)
        {
            bw.Write(f.FighterId);
            WriteString(bw, f.Name);
            bw.Write(f.SpeedRating);
            bw.Write(f.LifeRating);
            bw.Write(f.MaxLife);
            bw.Write(f.ReachRating);
            bw.Write(f.AirRating);
            bw.Write(f.PowerRating);
            bw.Write(f.WalkSpeedRaw);
            bw.Write(f.RunSpeedRaw);
            bw.Write(f.FullSpeedRaw);
            bw.Write(f.Moves.Length);
            foreach (MoveRecord m in f.Moves)
            {
                WriteString(bw, m.Name);
                bw.Write((byte)m.Class);
                bw.Write(m.Damage);
                bw.Write(m.StartupFrames);
                bw.Write(m.ActiveFrames);
                bw.Write(m.RecoveryFrames);
                bw.Write(m.UtilityExempt);
            }
        }

        private static void WriteStage(BinaryWriter bw, StageRecord s)
        {
            bw.Write(s.StageId);
            WriteString(bw, s.Name);
            bw.Write(s.JewelCrateChancePercent);
            bw.Write(s.Phases.Length);
            foreach (StagePhaseRecord p in s.Phases)
            {
                WriteString(bw, p.Name);
                bw.Write(p.DurationFrames);
            }
        }

        private static void WriteItem(BinaryWriter bw, ItemRecord i)
        {
            bw.Write(i.ItemId);
            WriteString(bw, i.Name);
            bw.Write((byte)i.Category);
            bw.Write(i.Rarity);
            bw.Write((byte)i.CrateEligibility);
            bw.Write(i.Pocketable);
            bw.Write(i.BuyPrice);
            bw.Write(i.SellPrice);
            bw.Write(i.DamagePerUse);
            bw.Write(i.HealPerUse);
        }

        private static void WriteString(BinaryWriter bw, string value)
        {
            string s = value ?? string.Empty;
            if (s.Length > ContentSchema.MaxNameLength)
            {
                s = s.Substring(0, ContentSchema.MaxNameLength);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }
    }
}
