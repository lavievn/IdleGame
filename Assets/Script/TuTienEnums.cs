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
        public static string GetRandomName(GenderType gender) => gender == GenderType.Nam ? MaleNames[Random.Range(0, MaleNames.Length)] : FemaleNames[Random.Range(0, FemaleNames.Length)];
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
        public static List<ElementType> GenerateRandomRoots()
        {
            int count = Random.Range(1, 3);
            List<ElementType> roots = new List<ElementType>();
            for(int i=0; i<count; i++) roots.Add((ElementType)Random.Range(0, 7));
            return roots;
        }
        public static float GetLevelAdvantageMultiplier(int attLvl, int defLvl) => attLvl <= defLvl ? 1.0f : 1.0f + ((attLvl - defLvl) * 0.05f);
        public static float GetWeaponLevelPenalty(int heroLvl, int weaponLvl) => heroLvl >= weaponLvl ? 1.0f : Mathf.Max(0.2f, 1.0f - ((weaponLvl - heroLvl) * 0.1f));
        public static float GetElementalMultiplier(ElementType att, ElementType def) => 1.0f; 
    }
}