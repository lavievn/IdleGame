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
        // Optional second layer: neither branch blocks the original reveal.
        public const string RefugeeDiary = "tanglinh.refugee_diary";
        public const string LingmaiScar = "tanglinh.lingmai_scar";
        public const string Aftermath = "tanglinh.aftermath";
        private static readonly string[] physical = { Rumor, Inscription, Jade, Seal };

        // Follow-up discoveries use a compact rule graph. RequireAll and
        // RequireAny are evaluated on saved clue IDs; the location and wave
        // threshold must ALSO be met. No online AI or runtime story rerolls.
        private sealed class FollowUp
        {
            public readonly string Id, Notice;
            public readonly string[] RequireAll, RequireAny;
            public readonly int MinWave;
            public FollowUp(string id,int minWave,string[] all,string[] any,string notice)
            {
                Id=id; MinWave=minWave; RequireAll=all; RequireAny=any; Notice=notice;
            }
        }
        private static readonly FollowUp[] followUps =
        {
            new FollowUp(RefugeeDiary,2,new[]{Inscription},new[]{Jade,Seal},
                "[Di thư] Nhật ký của một hậu duệ kể về dòng người chạy nạn. Họ căm ghét Tạ Vô Trần nhưng cũng nghi ngờ lời kết tội của tông môn."),
            new FollowUp(LingmaiScar,3,new[]{Jade,Seal},null,
                "[Linh mạch] Phát hiện vết nứt cổ: dấu khai thác trận pháp xuất hiện TRƯỚC khi Tạ Vô Trần cắt mạch.")
        };

        private static bool Eligible(EntityDataSO hero,FollowUp rule)
        {
            foreach (string id in rule.RequireAll)
                if (!Has(hero,id)) return false;
            if (rule.RequireAny == null || rule.RequireAny.Length == 0) return true;
            foreach (string id in rule.RequireAny)
                if (Has(hero,id)) return true;
            return false;
        }

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
                    id != Seal && id != Revelation &&
                    id != RefugeeDiary && id != LingmaiScar && id != Aftermath) continue;
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

        // The 3 original witnesses stay near each birth region (<= level 13).
        // Optional second-layer sites use a slightly deeper map band when
        // possible, without affecting its combat level or travel conditions.
        // Each region provides a possible source; one discovered copy suffices.
        public static string Site(EntityDataSO hero, string clueId, string continent)
        {
            bool deeper = clueId == RefugeeDiary || clueId == LingmaiScar;
            if (hero == null || hero.storySeed == 0 ||
                (clueId != Inscription && clueId != Jade && clueId != Seal &&
                 !deeper) || string.IsNullOrEmpty(continent)) return "";
            var candidates = new List<WorldMapNode>();
            foreach (WorldMapNode n in WorldMapCatalog.All)
                if (n.Continent == continent && !n.IsBoss &&
                    (deeper ? (n.SuggestedLevel >= 14 && n.SuggestedLevel <= 31)
                            : n.SuggestedLevel <= 13))
                    candidates.Add(n);
            // Never strand a story if a provisional graph changes its bands.
            if (candidates.Count == 0)
                foreach (WorldMapNode n in WorldMapCatalog.All)
                    if (n.Continent == continent && !n.IsBoss &&
                        n.SuggestedLevel <= (deeper ? 31 : 13))
                        candidates.Add(n);
            if (candidates.Count == 0)
                foreach (WorldMapNode n in WorldMapCatalog.All)
                    if (n.Continent == continent && !n.IsBoss) candidates.Add(n);
            if (candidates.Count == 0) return "";
            candidates.Sort((a,b) => string.CompareOrdinal(a.Id,b.Id));
            uint hash = StableHash(hero.storySeed.ToString() + "|" +
                continent + (deeper ? "|aftermath" : ""));
            int start = (int)(hash % (uint)candidates.Count);
            int offset = clueId == Inscription || clueId == RefugeeDiary ? 0 :
                clueId == Jade || clueId == LingmaiScar ? 1 : 2;
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

            // Discover extra consequences independently of the MAIN ending:
            // all/any conditions and a second location-based interaction.
            foreach (FollowUp followUp in followUps)
            {
                if (!Has(hero,followUp.Id) && Eligible(hero,followUp) &&
                    hero.worldMapWaveProgress >= followUp.MinWave &&
                    node.Id == Site(hero,followUp.Id,node.Continent) &&
                    Mark(hero,followUp.Id))
                    notices.Add(followUp.Notice);
            }
            if (Has(hero,Revelation) && Has(hero,RefugeeDiary) &&
                Has(hero,LingmaiScar) && Mark(hero,Aftermath))
                notices.Add("[Dư âm] Di thư và vết nứt xác nhận bi kịch còn để lại hậu quả. Sự thật đã rõ, nhưng những người sống sót không vì thế mà hết oán hận.");
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
            if (hero.storyFoundIds.Count == 0)
                return "DỊ VĂN\n\nChưa phát hiện dấu vết nào.\nTiếp tục chiến đấu ở bất kỳ map nào để nghe những lời đồn đầu tiên.";
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
            if (Has(hero,Inscription) && Has(hero,Jade))
                sb.AppendLine("\nSUY LUẬN: Bia cáo tội và ngọc giản mô tả cùng một hành động nhưng trái ngược về động cơ.");
            if (Has(hero,Jade) && Has(hero,Seal))
                sb.AppendLine("SUY LUẬN: Trận pháp đã bị can thiệp trước khi linh mạch bị cắt.");
            if (Has(hero,Inscription) && Has(hero,Seal) && !Has(hero,Jade))
                sb.AppendLine("SUY LUẬN: Bia đá có thể đã bỏ qua một phần nguyên nhân tai họa.");
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
            if (Has(hero,RefugeeDiary) || Has(hero,LingmaiScar) ||
                Has(hero,Inscription) || Has(hero,Seal))
            {
                sb.AppendLine("\nHỆ QUẢ CÒN SÓT LẠI");
                if (Has(hero,RefugeeDiary))
                    sb.AppendLine("• Nhật ký nạn dân: Người sống sót không biết toàn bộ sự thật và vẫn mang mối hận.");
                if (Has(hero,LingmaiScar))
                    sb.AppendLine("• Vết nứt linh mạch: Có chứng cứ vật chất về việc khai thác phong ấn trước thảm họa.");
                if (Has(hero,Aftermath))
                    sb.AppendLine("• Dư âm: Đã ghép được cả lời kể của hậu duệ lẫn vết tích linh mạch.");
                else
                {
                    WorldMapNode current;
                    if (WorldMapCatalog.TryGet(hero.worldMapId,out current))
                    {
                        foreach (FollowUp followUp in followUps)
                            if (!Has(hero,followUp.Id) && Eligible(hero,followUp))
                                sb.AppendLine("• " +
                                    (followUp.Id == RefugeeDiary ? "Nhật ký nạn dân: " : "Vết nứt linh mạch: ") +
                                    Name(Site(hero,followUp.Id,current.Continent)) +
                                    " (ít nhất " + followUp.MinWave + " wave).");
                    }
                    if (!Has(hero,RefugeeDiary) && !Has(hero,Inscription))
                        sb.AppendLine("• Có những dòng ghi chép chỉ có thể lần theo sau khi tìm thấy bia cáo tội.");
                    if (!Has(hero,LingmaiScar) && (!Has(hero,Jade) || !Has(hero,Seal)))
                        sb.AppendLine("• Cần đối chiếu ngọc giản với trận văn để lần theo dấu tích khai thác.");
                }
            }
            sb.AppendLine("\nTự khám phá qua các wave; không tự chuyển map hay ngắt quá trình idle.");
            return sb.ToString();
        }
    }
}
