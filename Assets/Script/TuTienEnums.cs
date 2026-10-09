using System.Collections.Generic;
using UnityEngine;

namespace TuTienCore
{
    public enum RaceType { NhanToc, YeuThu, MaToc, LinhThe, ConLai }
    public enum ElementType { Kim, Moc, Thuy, Hoa, Tho, Doc, Bang, Vo }
    public enum TerrainType { DongBang, CaoNguyen, DamLay, RungRam, SaMac, DoiNui, HoNuoc, Bien }
    public enum RegionTheme { SonLam, BinhNguyen, UTrach, HoangMac, HaiVuc, CaoSon }
    public enum MonsterClass { Thu, BoSat, Chim, ThuySinh, ChanKhop, LuongCu, Long, Nhan }
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
        public static RaceType GenerateMonsterRace(TerrainType terrain)
            => terrain == TerrainType.Bien ? (RaceType)Random.Range(1, 5) : GenerateRandomRace();
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
            if (count == 1 && race != RaceType.ConLai) available.Add(ElementType.Vo);
            var roots = new List<ElementType>();
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, available.Count);
                roots.Add(available[index]); available.RemoveAt(index);
            }
            return roots;
        }
        // Only the special Vô inheritance case is settled; ordinary breeding is not implemented.
        public static bool TryResolveVoidInheritance(List<ElementType> parentA, List<ElementType> parentB, out List<ElementType> child)
        {
            bool a = parentA != null && parentA.Count == 1 && parentA[0] == ElementType.Vo;
            bool b = parentB != null && parentB.Count == 1 && parentB[0] == ElementType.Vo;
            child = null;
            if (!a && !b) return false;
            child = a && b ? new List<ElementType> { ElementType.Vo } : new List<ElementType>(a ? parentB ?? new List<ElementType>() : parentA ?? new List<ElementType>());
            child.RemoveAll(root => root == ElementType.Vo && !(a && b));
            return true;
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
            => GetElementalMultiplier(att, 3, def, 3);
        public static float GetElementalMultiplier(ElementType att, int attTier, ElementType def, int defTier)
        {
            int gap = Mathf.Clamp(attTier, 1, 5) - Mathf.Clamp(defTier, 1, 5);
            if (Counters(att, def))
                return 1f + .30f * (gap >= 0 ? 1f + .10f * gap : Mathf.Max(.25f, 1f + .25f * gap));
            if (Counters(def, att)) return 1f - .30f * (1f - .10f * gap);
            return 1f; // Vô, legacy special roots, same root and unrelated pairs.
        }
        public static float GetElementalMultiplier(List<ElementType> attackers, List<ElementType> defenders)
        {
            if (attackers == null || defenders == null || attackers.Count == 0 || defenders.Count == 0) return 1f;
            float sum = 0;
            foreach (var att in attackers) foreach (var def in defenders) sum += GetElementalMultiplier(att, def);
            return sum / (attackers.Count * defenders.Count);
        }
        public static void GenerateRootProfile(EntityDataSO data)
        {
            data.spiritRoots = GenerateRandomRoots(data.race);
            data.rootTiers = new List<int>(); data.rootWeights = new List<float>();
            foreach (var root in data.spiritRoots) data.rootTiers.Add(Random.Range(1, 6));
            int count = data.spiritRoots.Count;
            if (count == 2)
            {
                int gap = data.rootTiers[0] - data.rootTiers[1];
                float first = .5f + Mathf.Clamp(gap, -3, 3) * .1f;
                data.rootWeights.Add(first); data.rootWeights.Add(1f - first);
            }
            else for (int i = 0; i < count; i++) data.rootWeights.Add(1f / count);
            data.hybridSecondaryRace = (RaceType)Random.Range(0, 4);
            data.NormalizeRoots();
        }
        public static float GetElementalMultiplier(EntityDataSO attacker, EntityDataSO defender)
        {
            attacker.NormalizeRoots(); defender.NormalizeRoots();
            if (attacker.spiritRoots.Count == 0 || defender.spiritRoots.Count == 0) return 1f;
            float sum = 0f;
            for (int a = 0; a < attacker.spiritRoots.Count; a++)
                for (int d = 0; d < defender.spiritRoots.Count; d++)
                    sum += attacker.rootWeights[a] * defender.rootWeights[d] * GetElementalMultiplier(
                        attacker.spiritRoots[a], attacker.rootTiers[a], defender.spiritRoots[d], defender.rootTiers[d]);
            return sum;
        }
        public static DamageTrace EvaluateDamage(EntityDataSO attacker, EntityDataSO defender, AttackMode mode, float roll, float physicalFactor = .8f)
        {
            attacker.NormalizeRoots();
            int attack = Mathf.Max(1, attacker.GetCalculatedDamage());
            roll = Mathf.Clamp(roll, .85f, 1f);
            var snapshot = new DamageTrace { attacker = IdentityDisplay.Describe(attacker),
                attackRaw = attacker.baseDamage, attackAdded = attacker.addedDamage, attackBase = attack,
                modeMultiplier = CombatBalance.ModeMultiplier(mode, physicalFactor), roll = roll,
                rolledDamage = CombatBalance.Damage(attack, mode, roll, physicalFactor),
                sourceRoots = new List<ElementType>(attacker.spiritRoots), sourceTiers = new List<int>(attacker.rootTiers),
                sourceWeights = new List<float>(attacker.rootWeights) };
            return EvaluateSnapshot(snapshot, defender);
        }
        public static DamageTrace EvaluateSnapshot(DamageTrace snapshot, EntityDataSO defender)
        {
            defender.NormalizeRoots();
            var parts = new List<string>();float element = 0f;
            for (int a = 0; a < snapshot.sourceRoots.Count; a++) for (int d = 0; d < defender.spiritRoots.Count; d++) {
                ElementType root = snapshot.sourceRoots[a];int tier = snapshot.sourceTiers[a];
                float pair = GetElementalMultiplier(root, tier, defender.spiritRoots[d], defender.rootTiers[d]);
                element += snapshot.sourceWeights[a] * defender.rootWeights[d] * pair;
                int gap = tier - defender.rootTiers[d];
                string rule = Counters(root, defender.spiritRoots[d])
                    ? (gap >= 0 ? "+30% × (1 + 10% × " + gap + ")" : "+30% × max(25%, 1 + 25% × (" + gap + "))")
                    : Counters(defender.spiritRoots[d], root) ? "−30% × (1 − 10% × (" + gap + "))" : "trung tính";
                parts.Add(IdentityDisplay.Element(root) + " " + tier + " → " +
                    IdentityDisplay.Element(defender.spiritRoots[d]) + " " + defender.rootTiers[d] + ": " +
                    snapshot.sourceWeights[a].ToString("P3") + " × " + defender.rootWeights[d].ToString("P3") +
                    " × " + pair.ToString("0.###") + " (" + ((pair - 1f) * 100f).ToString("+0.##;-0.##;0") + "%; " + rule + ")");
            }
            if (parts.Count == 0) element = 1f;
            return new DamageTrace { attacker = snapshot.attacker, defender = IdentityDisplay.Describe(defender),
                pairs = parts.Count == 0 ? "Không có cặp linh căn: ×1" : string.Join("\n", parts.ToArray()),
                attackRaw = snapshot.attackRaw, attackAdded = snapshot.attackAdded, attackBase = snapshot.attackBase,
                modeMultiplier = snapshot.modeMultiplier, roll = snapshot.roll, rolledDamage = snapshot.rolledDamage,
                elementMultiplier = element, elementDamage = Mathf.Max(1, Mathf.RoundToInt(snapshot.rolledDamage * element)),
                sourceRoots = snapshot.sourceRoots, sourceTiers = snapshot.sourceTiers, sourceWeights = snapshot.sourceWeights };
        }
        public static int Damage(EntityDataSO attacker, EntityDataSO defender, AttackMode mode, float roll, int mapMultiplier = 1)
        {
            return Mathf.Max(1, Mathf.RoundToInt(EvaluateDamage(attacker, defender, mode, roll).elementDamage * mapMultiplier));
        }
    }

    public sealed class DamageTrace
    {
        public string attacker, defender, pairs;
        public List<ElementType> sourceRoots;
        public List<int> sourceTiers;
        public List<float> sourceWeights;
        public int attackRaw, attackAdded, attackBase, rolledDamage, elementDamage;
        public float modeMultiplier, roll, elementMultiplier;
        private static string F(float value) => value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        public string Describe(float mapScale, int hardScale, int finalDamage)
        {
            return attacker + " → " + defender + "\n" +
                "ATK cơ bản: " + attackRaw + "; cộng điểm: " + attackAdded + "; tổng: " + attackBase + "; kiểu đánh ×" + F(modeMultiplier) + "; dao động ×" + F(roll) + "\n" +
                "Sát thương nền = làm tròn(" + attackBase + " × " + F(modeMultiplier) + " × " + F(roll) + ") = " + rolledDamage + "\n" +
                pairs + "\nTổng hệ số linh căn = " + F(elementMultiplier) +
                "; sau linh căn = làm tròn(" + rolledDamage + " × " + F(elementMultiplier) + ") = " + elementDamage + "\n" +
                "Kết quả = tối thiểu 1, làm tròn(" + elementDamage + " × map " + F(mapScale) + " × khó " + hardScale + ") = " + finalDamage;
        }
    }

    public static class WorldNames
    {
        private static readonly string[][] MapNames = {
            new[] { "Thanh Phong Nguyên", "Bạch Lộ Bình Nguyên", "Vân Khê Đồng", "Thiên Lý Nguyên", "Kim Tuệ Nguyên" },
            new[] { "Thiên Phong Cao Nguyên", "Vân Đỉnh Nguyên", "Hàn Sương Cao Nguyên", "Bạch Vân Đài", "Cửu Tiêu Nguyên" },
            new[] { "U Minh Trạch", "Hắc Thủy Trạch", "Độc Vụ Đầm", "Thanh Liên Trạch", "Vụ Ẩn Trạch" },
            new[] { "Vạn Mộc Lâm", "Thanh U Lâm", "Cổ Thụ Lâm", "Bích Ảnh Lâm", "Vân Ẩn Lâm" },
            new[] { "Xích Sa Mạc", "Hoàng Sa Hải", "Lưu Sa Mạc", "Viêm Dương Mạc", "Tịch Dương Sa Hải" },
            new[] { "Thanh Vân Sơn", "Hắc Nham Lĩnh", "Bạch Thạch Khâu", "Liên Vân Lĩnh", "Cửu Phong Sơn" },
            new[] { "Bích Thủy Hồ", "Minh Nguyệt Hồ", "Thanh Liên Hồ", "Vân Kính Hồ", "Hàn Ngọc Hồ" },
            new[] { "Thương Hải", "Bích Hải", "Vân Hải Vực", "Huyền Hải", "Thiên Lam Hải" }
        };
        public static readonly string[] AnimalNames = {
            "Trư", "Hổ", "Lang", "Hồ", "Thố", "Lộc", "Ngưu", "Mã", "Dương", "Hầu",
            "Viên", "Hùng", "Miêu", "Khuyển", "Thử", "Tượng", "Tê", "Báo", "Sư", "Ly",
            "Xà", "Mãng", "Quy", "Ngạc", "Thiềm", "Oa", "Ưng", "Điêu", "Hạc", "Ô",
            "Yến", "Tước", "Nhạn", "Áp", "Kê", "Khổng Tước", "Hải Âu", "Bức", "Lý", "Ngư",
            "Kình", "Sa", "Chương", "Giải", "Hà", "Chu", "Hạt", "Ngô Công", "Phong", "Nghĩ", "Long"
        };
        private static readonly TerrainType[][] RegionTerrains = {
            new[] { TerrainType.RungRam, TerrainType.DoiNui, TerrainType.HoNuoc },
            new[] { TerrainType.DongBang, TerrainType.CaoNguyen, TerrainType.HoNuoc },
            new[] { TerrainType.DamLay, TerrainType.RungRam, TerrainType.HoNuoc },
            new[] { TerrainType.SaMac, TerrainType.CaoNguyen, TerrainType.DoiNui },
            new[] { TerrainType.Bien },
            new[] { TerrainType.DoiNui, TerrainType.CaoNguyen, TerrainType.RungRam }
        };
        public static string Terrain(TerrainType terrain)
            => new[] { "Đồng bằng", "Cao nguyên", "Đầm lầy", "Rừng rậm", "Sa mạc", "Đồi núi", "Hồ nước", "Biển" }[(int)terrain];
        public static string Region(RegionTheme theme)
            => new[] { "Sơn Lâm", "Bình Nguyên", "U Trạch", "Hoang Mạc", "Hải Vực", "Cao Sơn" }[(int)theme];
        public static bool AllowsTerrain(RegionTheme theme, TerrainType terrain)
            => System.Array.IndexOf(RegionTerrains[(int)theme], terrain) >= 0;
        public static void EnsureRegion(EntityDataSO data, bool preserveTerrain = false)
        {
            int index = (System.Math.Max(1, data.mapNumber) - 1) / 5;
            if (data.regionIndex == index && System.Enum.IsDefined(typeof(RegionTheme), data.regionTheme)) return;
            var choices = new List<RegionTheme>();
            for (int i = 0; i < RegionTerrains.Length; i++)
                if (!preserveTerrain || AllowsTerrain((RegionTheme)i, data.mapTerrain)) choices.Add((RegionTheme)i);
            data.regionTheme = choices[Random.Range(0, choices.Count)];
            data.regionIndex = index; data.isDirty = true;
        }
        public static void AssignMap(EntityDataSO data)
        {
            EnsureRegion(data);
            string previous = data.mapName;
            var terrains = RegionTerrains[(int)data.regionTheme];
            do {
                data.mapTerrain = terrains[Random.Range(0, terrains.Length)];
                var names = MapNames[(int)data.mapTerrain];
                data.mapName = names[Random.Range(0, names.Length)];
            } while (data.mapName == previous);
            data.isDirty = true;
        }
        private static readonly string[][] HabitatAnimals = {
            new[] { "Trư", "Lang", "Hồ", "Thố", "Lộc", "Ngưu", "Mã", "Dương", "Khuyển", "Thử", "Xà", "Ưng", "Tước", "Kê", "Phong", "Nghĩ" },
            new[] { "Lang", "Hồ", "Ngưu", "Mã", "Dương", "Thố", "Hùng", "Báo", "Ưng", "Điêu", "Hạc", "Xà", "Hạt" },
            new[] { "Xà", "Mãng", "Quy", "Ngạc", "Thiềm", "Oa", "Chu", "Ngô Công", "Lý", "Ngư", "Hà", "Giải", "Hạc", "Áp", "Phong" },
            new[] { "Xà", "Mãng", "Lang", "Hổ", "Hùng", "Lộc", "Hồ", "Hầu", "Viên", "Trư", "Tượng", "Tê", "Báo", "Ly", "Thố", "Ưng", "Điêu", "Ô", "Yến", "Tước", "Khổng Tước", "Bức", "Chu", "Ngô Công", "Phong", "Nghĩ" },
            new[] { "Xà", "Mãng", "Chu", "Hạt", "Thử", "Ưng", "Điêu", "Ngô Công", "Nghĩ" },
            new[] { "Dương", "Hùng", "Lang", "Hồ", "Báo", "Thố", "Lộc", "Xà", "Mãng", "Ưng", "Điêu", "Bức", "Hạt", "Ngô Công" },
            new[] { "Lý", "Ngư", "Quy", "Ngạc", "Hà", "Giải", "Thiềm", "Oa", "Hạc", "Áp", "Nhạn", "Long" },
            new[] { "Long", "Ngư", "Kình", "Sa", "Chương", "Giải", "Hà", "Hải Âu", "Nhạn", "Ưng" }
        };
        public static bool AnimalAllowed(TerrainType terrain, string animal)
            => System.Array.IndexOf(HabitatAnimals[(int)terrain], animal) >= 0 || terrain == TerrainType.SaMac && (animal == "Hổ" || animal == "Báo");
        public static string RandomAnimal(TerrainType terrain)
        {
            // Combined desert tiger/leopard rarity is 1%; no aquatic animal is eligible.
            if (terrain == TerrainType.SaMac && Random.Range(0, 100) == 0) return Random.Range(0, 2) == 0 ? "Hổ" : "Báo";
            var choices = HabitatAnimals[(int)terrain]; return choices[Random.Range(0, choices.Length)];
        }
        public static MonsterClass ClassifyAnimal(string animal)
        {
            if (animal == "Long") return MonsterClass.Long;
            if (System.Array.IndexOf(new[] { "Lý", "Ngư", "Kình", "Sa", "Chương", "Giải", "Hà" }, animal) >= 0) return MonsterClass.ThuySinh;
            if (System.Array.IndexOf(new[] { "Xà", "Mãng", "Quy", "Ngạc" }, animal) >= 0) return MonsterClass.BoSat;
            if (System.Array.IndexOf(new[] { "Thiềm", "Oa" }, animal) >= 0) return MonsterClass.LuongCu;
            if (System.Array.IndexOf(new[] { "Chu", "Hạt", "Ngô Công", "Phong", "Nghĩ" }, animal) >= 0) return MonsterClass.ChanKhop;
            if (System.Array.IndexOf(new[] { "Ưng", "Điêu", "Hạc", "Ô", "Yến", "Tước", "Nhạn", "Áp", "Kê", "Khổng Tước", "Hải Âu" }, animal) >= 0) return MonsterClass.Chim;
            return MonsterClass.Thu;
        }
        public static float MonsterDamageScale(int mapVisit)
            => Mathf.Min(1f, .5f + .1f * (System.Math.Max(1, mapVisit) / 20));
        public static string MonsterName(EntityDataSO data, string animal)
        {
            if (data.race == RaceType.NhanToc) return data.entityName;
            data.NormalizeRoots();
            int dominant = 0;
            for (int i = 1; i < data.spiritRoots.Count; i++)
                if (data.rootWeights[i] > data.rootWeights[dominant] ||
                    data.rootWeights[i] == data.rootWeights[dominant] && data.rootTiers[i] > data.rootTiers[dominant]) dominant = i;
            string prefix = data.spiritRoots.Count == 0 ? "Vô" : IdentityDisplay.Element(data.spiritRoots[dominant]);
            string marker = "";
            if (data.race == RaceType.ConLai)
                switch (data.hybridSecondaryRace) {
                    case RaceType.LinhThe: marker = " Linh"; break;
                    case RaceType.MaToc: marker = " Ma"; break;
                    case RaceType.NhanToc: marker = " Nhân"; break;
                    default: marker = " Thú"; break;
                }
            return prefix + marker + " " + animal;
        }
        public static string RandomMonsterName(EntityDataSO data) => RandomMonsterName(data, TerrainType.DongBang);
        public static string RandomMonsterName(EntityDataSO data, TerrainType terrain)
        {
            if (data.race == RaceType.NhanToc) { data.monsterAnimal = ""; data.monsterClass = MonsterClass.Nhan; return data.entityName; }
            data.monsterAnimal = RandomAnimal(terrain); data.monsterClass = ClassifyAnimal(data.monsterAnimal);
            return MonsterName(data, data.monsterAnimal);
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
                case ElementType.Vo:return "Vô"; case ElementType.Doc:return "Độc"; case ElementType.Bang:return "Băng"; default:return "Không rõ"; }
        }
        public static string Describe(EntityDataSO data)
        {
            var roots = new List<string>();
            data.NormalizeRoots();
            for (int i = 0; i < data.spiritRoots.Count; i++)
                roots.Add(Element(data.spiritRoots[i]) + " " + Tier(data.rootTiers[i]) +
                    (data.spiritRoots.Count > 1 ? " " + Mathf.RoundToInt(data.rootWeights[i] * 100f) + "%" : ""));
            return data.entityName + " · " + Race(data.race) + " · " + (roots.Count > 0 ? string.Join("/", roots.ToArray()) : "Không hệ");
        }
        public static string Tier(int tier)
        {
            return new[] { "Nhất trọng", "Nhị trọng", "Tam trọng", "Tứ trọng", "Ngũ trọng" }[Mathf.Clamp(tier, 1, 5) - 1];
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