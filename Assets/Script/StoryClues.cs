using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TuTienCore
{
    // Story prototype 0.0.54: a FINISHED historical incident, discovered in
    // arbitrary order from short idle logs. No quests, combat overrides or
    // changes to the world-map travel rules.
    public static class StoryClues
    {
        public const int Version = 1;
        public const string Rumor = "tanglinh.rumor";
        public const string Inscription = "tanglinh.inscription";
        public const string Jade = "tanglinh.jade";
        public const string Seal = "tanglinh.seal";
        public const string Revelation = "tanglinh.revelation";
        private static readonly string[] physical = { Rumor, Inscription, Jade, Seal };

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in value ?? "") { h ^= c; h *= 16777619u; }
                return h;
            }
        }

        public static void InitializeNew(EntityDataSO hero)
        {
            if (hero == null) return;
            // New Game rolls once; never reroll on reload, map transfer or retry.
            hero.storyVersion = Version;
            hero.storySeed = UnityEngine.Random.Range(1, int.MaxValue);
            hero.storyFoundIds = new List<string>();
            hero.isDirty = true;
        }

        public static void Normalize(EntityDataSO hero)
        {
            if (hero == null) return;
            if (!WorldMapCatalog.Enabled(hero))
            {
                // Old sequential savegames retain their original progression.
                return;
            }
            if (hero.storyVersion < 1 || hero.storySeed == 0)
            {
                // Backward-compatible initialization for pre-story WORLD saves.
                // The seed is stable for a given historical save, and gets
                // persisted on its next autosave/manual save.
                uint hash = StableHash((hero.entityName ?? "") + "|" +
                    (hero.worldMapId ?? "") + "|tanglinh.v1");
                hero.storySeed = (int)(hash % (int.MaxValue - 1u)) + 1;
                hero.storyVersion = Version;
                hero.storyFoundIds = new List<string>();
                hero.isDirty = true;
            }
            if (hero.storyFoundIds == null)
            {
                hero.storyFoundIds = new List<string>();
                hero.isDirty = true;
            }
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var clean = new List<string>();
            foreach (string id in hero.storyFoundIds)
            {
                if (id != Rumor && id != Inscription && id != Jade &&
                    id != Seal && id != Revelation) continue;
                if (unique.Add(id)) clean.Add(id);
            }
            if (clean.Count != hero.storyFoundIds.Count)
            {
                hero.storyFoundIds = clean;
                hero.isDirty = true;
            }
        }

        public static bool Has(EntityDataSO hero, string id)
            => hero != null && hero.storyFoundIds != null &&
               hero.storyFoundIds.Contains(id);

        private static bool Mark(EntityDataSO hero, string id)
        {
            if (Has(hero,id)) return false;
            hero.storyFoundIds.Add(id);
            hero.isDirty = true;
            return true;
        }

        // Every large region receives one attainable copy of each discovery
        // SITE (the discovered FACT is unique per character). Sites are chosen
        // among low-level non-Boss maps; 3 physical clues never occupy the
        // same map within a region when there are at least 3 candidates.
        public static string Site(EntityDataSO hero, string clueId, string continent)
        {
            if (hero == null || hero.storySeed == 0 ||
                (clueId != Inscription && clueId != Jade && clueId != Seal) ||
                string.IsNullOrEmpty(continent)) return "";
            var candidates = new List<WorldMapNode>();
            foreach (WorldMapNode n in WorldMapCatalog.All)
                if (n.Continent == continent && !n.IsBoss && n.SuggestedLevel <= 13)
                    candidates.Add(n);
            if (candidates.Count == 0)
                foreach (WorldMapNode n in WorldMapCatalog.All)
                    if (n.Continent == continent && !n.IsBoss) candidates.Add(n);
            if (candidates.Count == 0) return "";
            candidates.Sort((a,b) => string.CompareOrdinal(a.Id,b.Id));
            uint h = StableHash(hero.storySeed.ToString() + "|" + continent);
            int start = (int)(h % (uint)candidates.Count);
            int offset = clueId == Inscription ? 0 : clueId == Jade ? 1 : 2;
            return candidates[(start + offset) % candidates.Count].Id;
        }

        public static List<string> OnCompletedWave(EntityDataSO hero)
        {
            var notices = new List<string>();
            if (hero == null || !WorldMapCatalog.Enabled(hero)) return notices;
            Normalize(hero);
            WorldMapNode node;
            if (!WorldMapCatalog.TryGet(hero.worldMapId,out node)) return notices;

            // No probability gate on core clues: visiting the right site and
            // clearing the required waves is sufficient, even when AFK.
            if (hero.worldMapWaveProgress >= 1 && Mark(hero,Rumor))
                notices.Add("[Dị văn] Một lời đồn cũ nhắc đến Đêm Táng Linh Mạch và tội nhân Tạ Vô Trần.");
            if (hero.worldMapWaveProgress >= 2 &&
                node.Id == Site(hero,Inscription,node.Continent) && Mark(hero,Inscription))
                notices.Add("[Di tích] Bia cáo tội ghi Tạ Vô Trần đã phá ba nhánh linh mạch. Tại sao?");
            if (hero.worldMapWaveProgress >= 3 &&
                node.Id == Site(hero,Jade,node.Continent) && Mark(hero,Jade))
                notices.Add("[Di vật] Nhặt được ngọc giản vỡ: 'Nếu không cắt mạch, tai kiếp sẽ lan khắp thiên hạ.'");
            if (hero.worldMapWaveProgress >= 4 &&
                node.Id == Site(hero,Seal,node.Continent) && Mark(hero,Seal))
                notices.Add("[Dấu vết] Trận văn còn sót lại cho thấy một phong ấn bị khai thác trước thảm họa.");

            // Deductions can be made in ANY acquisition order; no hidden
            // linear quest chain and no forced map travel or boss kill.
            if (Has(hero,Inscription) && Has(hero,Jade) && Has(hero,Seal) &&
                Mark(hero,Revelation))
                notices.Add("[Chân tướng] Thiên Huyền Tông đã làm nứt phong ấn; Tạ Vô Trần hy sinh một thành để cứu các vùng còn lại. Ông không hoàn toàn vô tội.");
            return notices;
        }

        private static string Name(string id)
        {
            WorldMapNode n;
            return WorldMapCatalog.TryGet(id,out n) ? n.Name + " (" + id + ")" : id;
        }

        public static string Journal(EntityDataSO hero)
        {
            if (hero == null || !WorldMapCatalog.Enabled(hero))
                return "Nhật ký Dị Văn chỉ khả dụng trong chế độ Bản đồ Thế giới.";
            Normalize(hero);
            var sb = new StringBuilder();
            sb.AppendLine("ĐÊM TÁNG LINH MẠCH");
            int found = 0;
            foreach (string id in physical) if (Has(hero,id)) found++;
            sb.AppendLine("Dấu vết: " + found + "/4");
            sb.AppendLine();
            if (Has(hero,Rumor))
                sb.AppendLine("• Lời đồn: Tạ Vô Trần bị kết tội phá linh mạch hơn ba trăm năm trước.");
            else
                sb.AppendLine("• Chưa nghe thấy dị văn nào. Tiếp tục chiến đấu tại map hiện tại.");
            if (Has(hero,Inscription))
                sb.AppendLine("• Bia đá: Ông đã phá ba nhánh linh mạch; động cơ vẫn còn là bí ẩn.");
            if (Has(hero,Jade))
                sb.AppendLine("• Ngọc giản: Việc cắt mạch có thể đã ngăn một tai kiếp lớn hơn.");
            if (Has(hero,Seal))
                sb.AppendLine("• Trận văn: Có kẻ đã khai thác phong ấn trước đêm tai họa.");
            if (Has(hero,Revelation))
                sb.AppendLine("\nCHÂN TƯỚNG: Tông môn gây ra tai kiếp, Tạ Vô Trần ngăn nó bằng một hy sinh tàn khốc. Lịch sử chính thống chỉ kể một nửa sự thật.");
            if (Has(hero,Rumor) && !Has(hero,Revelation))
            {
                WorldMapNode current;
                if (WorldMapCatalog.TryGet(hero.worldMapId,out current))
                {
                    sb.AppendLine("\nGỢI Ý TRUY TÌM · " + current.Continent);
                    if (!Has(hero,Inscription))
                        sb.AppendLine("• Bia đá: " + Name(Site(hero,Inscription,current.Continent)) + " (ít nhất 2 wave).");
                    if (!Has(hero,Jade))
                        sb.AppendLine("• Ngọc giản: " + Name(Site(hero,Jade,current.Continent)) + " (ít nhất 3 wave).");
                    if (!Has(hero,Seal))
                        sb.AppendLine("• Trận văn: " + Name(Site(hero,Seal,current.Continent)) + " (ít nhất 4 wave).");
                }
            }
            sb.AppendLine("\nTự khám phá qua các wave; không tự chuyển map hay ngắt quá trình idle.");
            return sb.ToString();
        }
    }
}
