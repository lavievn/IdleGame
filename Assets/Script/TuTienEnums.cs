using System.Collections.Generic;
using UnityEngine;

namespace TuTienCore
{
    public enum RaceType { NhanToc, YeuThu, MaToc, LinhThe, ConLai }
    public enum ElementType { Kim, Moc, Thuy, Hoa, Tho, Doc, Bang }
    public enum WeaponType { Kiem, Dao, Quyen }
    public enum GenderType { Nam, Nu }
    public enum AttackMode { Melee, RangedPhysical, RangedMagic } // Đã chuyển vào đây

    public static class NameDatabase
    {
        private static readonly string[] MaleNames = { "Hàn Lập", "Vương Lâm", "Tiêu Viêm", "Thạch Hạo", "Đường Tam", "Diệp Phàm", "Mạnh Hạo", "Tần Vũ", "Lâm Lôi", "Kỷ Ninh" };
        private static readonly string[] FemaleNames = { "Bích Dao", "Lục Tuyết Kỳ", "Ngoan Nhân", "Mỹ Đỗ Toa", "Huân Nhi", "Cửu U", "Tử Nguyệt", "Cơ Tử Nguyệt", "Nhan Như Ngọc", "An Diệu Y" };
        public static string GetRandomName(GenderType gender)
        {
            var sources = new List<string>(gender == GenderType.Nam ? MaleNames : FemaleNames);
            int count = Random.Range(1, 4);
            var words = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, sources.Count);
                string[] parts = sources[index].Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                words.Add(parts[Random.Range(0, System.Math.Min(3, parts.Length))]);
                sources.RemoveAt(index); // At most one syllable from each source name.
            }
            return string.Join(" ", words.ToArray());
        }
    }

    public static class RankDatabase
    {
        private static readonly string[] RankNames = { "Luyện Khí", "Trúc Cơ", "Kết Đan", "Nguyên Anh", "Hóa Thần", "Luyện Hư", "Hợp Thể", "Đại Thừa", "Độ Kiếp", "Chân Tiên" };
        public static string GetRankName(int level)
        {
            int rankIndex = (level - 1) / 10;
            return rankIndex < RankNames.Length ? RankNames[rankIndex] : "Tiên Đế";
        }
    }

    public static class SynergyMath
    {
        public static RaceType GenerateRandomRace() => (RaceType)Random.Range(0, 5);
        public static int RootCount(int roll, RaceType race)
        {
            if (race == RaceType.ConLai) return 2;
            return roll < 60 ? 1 : roll < 85 ? 2 : roll < 95 ? 3 : 4;
        }
        public static List<ElementType> GenerateRandomRoots() => GenerateRandomRoots(RaceType.NhanToc);
        public static List<ElementType> GenerateRandomRoots(RaceType race)
        {
            int count = RootCount(Random.Range(0, 100), race);
            var available = new List<ElementType> { ElementType.Kim, ElementType.Moc, ElementType.Hoa, ElementType.Thuy, ElementType.Tho };
            var roots = new List<ElementType>();
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, available.Count);
                roots.Add(available[index]); available.RemoveAt(index);
            }
            return roots;
        }
        public static float GetLevelAdvantageMultiplier(int attLvl, int defLvl) => attLvl <= defLvl ? 1.0f : 1.0f + ((attLvl - defLvl) * 0.05f);
        public static float GetWeaponLevelPenalty(int heroLvl, int weaponLvl) => heroLvl >= weaponLvl ? 1.0f : Mathf.Max(0.2f, 1.0f - ((weaponLvl - heroLvl) * 0.1f));
        private static bool Counters(ElementType att, ElementType def)
        {
            return att == ElementType.Kim && def == ElementType.Moc ||
                att == ElementType.Moc && def == ElementType.Tho ||
                att == ElementType.Tho && def == ElementType.Thuy ||
                att == ElementType.Thuy && def == ElementType.Hoa ||
                att == ElementType.Hoa && def == ElementType.Kim;
        }
        public static float GetElementalMultiplier(ElementType att, ElementType def)
            => Counters(att, def) ? 1.25f : Counters(def, att) ? .8f : 1f;
        public static float GetElementalMultiplier(List<ElementType> attackers, List<ElementType> defenders)
        {
            if (attackers == null || defenders == null || attackers.Count == 0 || defenders.Count == 0) return 1f;
            float sum = 0;
            foreach (var att in attackers) foreach (var def in defenders) sum += GetElementalMultiplier(att, def);
            return sum / (attackers.Count * defenders.Count);
        }
        public static int Damage(EntityDataSO attacker, EntityDataSO defender, AttackMode mode, float roll, int mapMultiplier = 1)
        {
            float element = GetElementalMultiplier(attacker.spiritRoots, defender.spiritRoots);
            // Race-specific bonuses have not been specified; all races use x1.
            return Mathf.Max(1, Mathf.RoundToInt(CombatBalance.Damage(attacker.GetCalculatedDamage(), mode, roll) * element * mapMultiplier));
        }
    }
    public static class IdentityDisplay
    {
        public static string Race(RaceType race)
        {
            switch (race) { case RaceType.YeuThu:return "Yêu thú"; case RaceType.MaToc:return "Ma tộc";
                case RaceType.LinhThe:return "Linh thể"; case RaceType.ConLai:return "Con lai"; default:return "Nhân tộc"; }
        }
        public static string Element(ElementType root)
        {
            switch (root) { case ElementType.Kim:return "Kim"; case ElementType.Moc:return "Mộc";
                case ElementType.Hoa:return "Hỏa"; case ElementType.Thuy:return "Thủy"; case ElementType.Tho:return "Thổ";
                case ElementType.Doc:return "Độc"; case ElementType.Bang:return "Băng"; default:return "Không rõ"; }
        }
        public static string Describe(EntityDataSO data)
        {
            var roots = new List<string>();
            if (data.spiritRoots != null) foreach (var root in data.spiritRoots) roots.Add(Element(root));
            return data.entityName + " · " + Race(data.race) + " · " + (roots.Count > 0 ? string.Join("/", roots.ToArray()) : "Không hệ");
        }
        public static Color ElementColor(ElementType root)
        {
            switch (root) { case ElementType.Kim:return new Color(1f,.85f,.05f); case ElementType.Moc:return new Color(.1f,.8f,.2f);
                case ElementType.Hoa:return new Color(1f,.1f,.1f); case ElementType.Thuy:return new Color(.1f,.4f,1f);
                case ElementType.Tho:return new Color(.55f,.3f,.1f); default:return new Color(1,1,1); }
        }
        public static Color Tint(Color original, List<ElementType> roots)
        {
            if (roots == null || roots.Count == 0) return original;
            float r=0,g=0,b=0;
            foreach (var root in roots) { Color c=ElementColor(root); r+=c.r;g+=c.g;b+=c.b; }
            return new Color((original.r+r/roots.Count)*.5f,(original.g+g/roots.Count)*.5f,
                (original.b+b/roots.Count)*.5f,original.a);
        }
    }

}