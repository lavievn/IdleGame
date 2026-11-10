using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TuTienCore
{
    public enum OriginStage { Gather, Return, SearchForest, SearchElite, Complete }

    [Serializable]
    public sealed class OriginActor
    {
        public string id;
        public string name;
        public GenderType gender;
        public RaceType race;

        public static OriginActor Create(string id, GenderType gender) => new OriginActor
        {
            id = id, gender = gender, race = RaceType.NhanToc, name = NameDatabase.GetRandomName(gender)
        };
    }

    [Serializable]
    public sealed class OriginStoryState
    {
        public int version;
        public string originId;
        public OriginActor guardian;
        public OriginActor companion;
        public bool introduced;
        public bool heardCompanion;
        public OriginStage stage;
        public int herbWaves;
        public int searchWaves;
    }

    public sealed class OriginDefinition
    {
        public readonly string Id, QuestId, Village, VillageName, Forest, EliteMap;
        public readonly string GuardianId, CompanionId, HerbId, TrailId, ClueId;
        public const int Waves = 5;

        public OriginDefinition(string id, string questId, string village, string villageName,
            string forest, string eliteMap)
        {
            Id = id;
            QuestId = questId;
            Village = village;
            VillageName = villageName;
            Forest = forest;
            EliteMap = eliteMap;
            GuardianId = id + ".guardian";
            CompanionId = id + ".companion";
            HerbId = id + ".herb";
            TrailId = id + ".trail";
            ClueId = id + ".elite_clue";
        }
    }

    public static class StoryOrigin
    {
        public static readonly OriginDefinition HanVu = new OriginDefinition(
            "hanvu", "hanvu.find_medicine", "TV-20", "Hàn Vụ thôn", "TV-16", "TV-11");
        public const string QuestName = "Tìm thuốc cứu người";

        public static OriginDefinition Definition(EntityDataSO hero) =>
            hero?.originStory?.originId == HanVu.Id ? HanVu : null;

        public static GenderType Opposite(GenderType gender) =>
            gender == GenderType.Nam ? GenderType.Nu : GenderType.Nam;

        public static void InitializeNewHero(EntityDataSO hero)
        {
            hero.gender = Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu;
            hero.entityName = NameDatabase.GetRandomName(hero.gender);
            hero.race = RaceType.NhanToc;
            SynergyMath.GenerateRootProfile(hero);
            hero.originStory = new OriginStoryState
            {
                version = 1,
                originId = HanVu.Id,
                guardian = OriginActor.Create(HanVu.GuardianId, Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu),
                companion = OriginActor.Create(HanVu.CompanionId, Opposite(hero.gender))
            };
        }

        public static bool IsValid(EntityDataSO hero)
        {
            var state = hero.originStory;
            if (state == null) return true;
            if (string.IsNullOrEmpty(state.originId)) return state.version == 0;
            var origin = Definition(hero);
            if (origin == null || state.version != 1 || hero.race != RaceType.NhanToc ||
                (hero.gender != GenderType.Nam && hero.gender != GenderType.Nu) ||
                !IsActor(state.guardian, origin.GuardianId) || !IsActor(state.companion, origin.CompanionId) ||
                state.companion.gender != Opposite(hero.gender) || state.stage < OriginStage.Gather ||
                state.stage > OriginStage.Complete || state.herbWaves < 0 || state.herbWaves > OriginDefinition.Waves ||
                state.searchWaves < 0 || state.searchWaves > OriginDefinition.Waves)
                return false;
            return (state.herbWaves == OriginDefinition.Waves) == (state.stage >= OriginStage.Return) &&
                (state.searchWaves == OriginDefinition.Waves) == (state.stage == OriginStage.Complete) &&
                (state.stage >= OriginStage.SearchElite || state.searchWaves == 0);
        }

        private static bool IsActor(OriginActor actor, string id) =>
            actor != null && actor.id == id && !string.IsNullOrWhiteSpace(actor.name) &&
            actor.race == RaceType.NhanToc && (actor.gender == GenderType.Nam || actor.gender == GenderType.Nu);

        public static string MapName(EntityDataSO hero, WorldMapNode node)
        {
            var origin = Definition(hero);
            return origin != null && origin.Village == node.Id ? origin.VillageName : node.Name;
        }

        public static bool IsSafe(EntityDataSO hero)
        {
            var origin = Definition(hero);
            return origin != null && hero.worldMapId == origin.Village;
        }

        public static bool IsWolfMap(EntityDataSO hero)
        {
            var origin = Definition(hero);
            return origin != null && (hero.worldMapId == origin.Forest || hero.worldMapId == origin.EliteMap);
        }

        public static bool IsEliteWave(EntityDataSO hero)
        {
            var origin = Definition(hero);
            return origin != null && hero.worldMapId == origin.EliteMap &&
                hero.originStory.stage == OriginStage.SearchElite &&
                hero.worldMapLoopWave == OriginDefinition.Waves;
        }

        // Null falls through to the normal map-clear rule. Allowed is an
        // explicit exception for the peaceful village and known return paths.
        public static bool TravelOverride(EntityDataSO hero, string target, out string reason)
        {
            reason = null;
            var origin = Definition(hero);
            if (origin == null) return false;
            var stage = hero.originStory.stage;
            if (IsSafe(hero) && target == origin.Forest) return true;
            if (hero.worldMapId == origin.Forest && target == origin.Village) return true;
            if (hero.worldMapId == origin.EliteMap && target == origin.Forest) return true;
            if (stage == OriginStage.Complete) return false;
            if (hero.worldMapId == origin.Forest && target == origin.EliteMap && stage == OriginStage.SearchElite)
                return true;
            reason = "Hãy theo chỉ dẫn trong Nhật ký Hàn Vụ trước khi đi xa hơn.";
            return true;
        }

        // Origin chapters checkpoint completed waves. Leaving or dying during
        // an unfinished wave replays that wave, with no duplicate story grants.
        public static void RestoreOpeningWave(EntityDataSO hero)
        {
            var origin = Definition(hero);
            if (origin == null) return;
            var state = hero.originStory;
            int waves;
            if (hero.worldMapId == origin.Forest && state.stage == OriginStage.Gather)
                waves = state.herbWaves;
            else if (hero.worldMapId == origin.EliteMap && state.stage == OriginStage.SearchElite)
                waves = state.searchWaves;
            else return;
            hero.worldMapWaveProgress = waves;
            hero.worldMapLoopWave = waves + 1;
            hero.completedWavesInMap = waves;
        }

        private static string GuardianTitle(OriginStoryState state) =>
            state.guardian.gender == GenderType.Nam ? "lão ông" : "lão bà";

        public static string Arrive(EntityDataSO hero)
        {
            var origin = Definition(hero);
            if (origin == null) return null;
            var state = hero.originStory;
            if (!state.introduced && IsSafe(hero))
            {
                state.introduced = true;
                hero.isDirty = true;
                return GuardianTitle(state) + " " + state.guardian.name +
                    " mấy hôm nay ho nặng quá… Ta phải vào rừng tìm ít thảo dược.";
            }
            return null;
        }

        public static string AfterWave(EntityDataSO hero, int wave)
        {
            var origin = Definition(hero);
            if (origin == null) return null;
            var state = hero.originStory;
            if (hero.worldMapId == origin.Forest && state.stage == OriginStage.Gather && wave == state.herbWaves + 1)
            {
                state.herbWaves = wave;
                hero.isDirty = true;
                if (wave < OriginDefinition.Waves) return null;
                state.stage = OriginStage.Return;
                return "Đã đủ thảo dược. Ta phải về Hàn Vụ thôn sắc thuốc.";
            }
            if (hero.worldMapId == origin.EliteMap && state.stage == OriginStage.SearchElite && wave == state.searchWaves + 1)
            {
                state.searchWaves = wave;
                hero.isDirty = true;
                if (wave < OriginDefinition.Waves) return null;
                state.stage = OriginStage.Complete;
                return "Nút dây này… giống nút " + state.companion.name + " vẫn buộc. Vì sao nó lại ở đây?";
            }
            return null;
        }

        public static bool CanInteract(EntityDataSO hero)
        {
            var origin = Definition(hero);
            return origin != null && (IsSafe(hero) ||
                (hero.worldMapId == origin.Forest && hero.originStory.stage == OriginStage.SearchForest));
        }

        public static string Interact(EntityDataSO hero)
        {
            if (!CanInteract(hero)) return null;
            var state = hero.originStory;
            if (!IsSafe(hero))
            {
                state.stage = OriginStage.SearchElite;
                hero.isDirty = true;
                return "Có một mảnh giấy ghim ở ngã rẽ. Dòng còn đọc được chỉ về Ma Tùng Sâm.";
            }
            string guardian = state.guardian.name;
            string elder = state.guardian.gender == GenderType.Nam ? "ông" : "bà";
            string companion = state.companion.name;
            if (state.stage == OriginStage.Return)
            {
                state.stage = OriginStage.SearchForest;
                hero.isDirty = true;
                return "Sắc thuốc xong, " + guardian + " dịu cơn ho. “" + companion +
                    " đã vào Cô Hồn Lâm tìm vị thuốc khác. Cháu xem ở ngã rẽ giúp " + elder + ".”";
            }
            if (state.stage == OriginStage.Gather)
            {
                if (!state.heardCompanion) { state.heardCompanion = true; hero.isDirty = true; }
                return companion + ": “Hai đứa mình lớn lên hái củi ở đây. Cậu tìm thuốc cho " + elder +
                    " nhé, dọc bìa Cô Hồn Lâm có cây lá bạc.”";
            }
            return guardian + ": “" + elder + " đã đỡ ho. " + companion + " vẫn chưa về.”";
        }

        public static string InteractionLabel(EntityDataSO hero) => IsSafe(hero)
            ? (hero.originStory.stage == OriginStage.Return ? "Sắc thuốc" : "Trò chuyện")
            : "Xem dấu vết";

        public static bool HintMap(EntityDataSO hero)
        {
            if (Definition(hero) == null) return false;
            switch (hero.originStory.stage)
            {
                case OriginStage.Gather: return IsSafe(hero);
                case OriginStage.Return: return !IsSafe(hero);
                case OriginStage.SearchForest: return IsSafe(hero);
                case OriginStage.SearchElite: return hero.worldMapId != HanVu.EliteMap;
                default: return false;
            }
        }

        public static string Hint(EntityDataSO hero)
        {
            var state = hero.originStory;
            switch (state.stage)
            {
                case OriginStage.Gather: return IsSafe(hero)
                    ? "Bấm tên map để mở Bản đồ, chọn Cô Hồn Lâm rồi bấm Di chuyển."
                    : "Hạ 5 đợt sói, thảo dược " + state.herbWaves + "/5. Xem chiến lợi phẩm thường trong Kho đồ.";
                case OriginStage.Return: return IsSafe(hero) ? "Chọn Sắc thuốc để giúp người nuôi dưỡng."
                    : "Bấm tên map để mở Bản đồ và tự về Hàn Vụ thôn.";
                case OriginStage.SearchForest: return IsSafe(hero) ? "Trở lại Cô Hồn Lâm tìm dấu của " + state.companion.name + "."
                    : "Chọn Xem dấu vết. Nhật ký lưu lại manh mối.";
                case OriginStage.SearchElite: return hero.worldMapId == HanVu.EliteMap
                    ? "Đi hết 5 đợt ở Ma Tùng Sâm. Sói đầu đàn xuất hiện ở đợt cuối."
                    : "Mở Bản đồ và tự đi đến Ma Tùng Sâm qua Cô Hồn Lâm.";
                default: return "Mở Nhật ký Hàn Vụ để xem vật chứng và những điều còn bỏ ngỏ.";
            }
        }

        public static int ItemCount(EntityDataSO hero, string itemId)
        {
            var origin = Definition(hero);
            if (origin == null) return 0;
            var state = hero.originStory;
            if (itemId == origin.HerbId) return state.stage <= OriginStage.Return ? state.herbWaves : 0;
            if (itemId == origin.TrailId) return state.stage >= OriginStage.SearchElite ? 1 : 0;
            if (itemId == origin.ClueId) return state.stage == OriginStage.Complete ? 1 : 0;
            return 0;
        }

        // Rebuild readable entries from persisted facts, so repeated clicks
        // never append duplicate journal records or multiply quest items.
        public static IReadOnlyList<string> Journal(EntityDataSO hero)
        {
            var state = hero.originStory;
            var lines = new List<string> { QuestName };
            if (state.introduced)
                lines.Add("Ghi nhận · Ta lớn lên ở Hàn Vụ, được " + state.guardian.name +
                    " nuôi dưỡng. " + state.companion.name + " là bạn thuở nhỏ. Gần đây người ho nặng.");
            if (state.heardCompanion)
                lines.Add("Lời kể · " + state.companion.name + " chỉ nơi tìm cây lá bạc ở bìa Cô Hồn Lâm.");
            if (state.herbWaves > 0)
                lines.Add(state.stage <= OriginStage.Return
                    ? "Vật phẩm · Thảo dược lá bạc ×" + ItemCount(hero, HanVu.HerbId) + ". Dùng sắc thuốc."
                    : "Vật phẩm · Đã dùng 5 thảo dược lá bạc để sắc thuốc.");
            if (state.stage >= OriginStage.SearchForest)
            {
                lines.Add("Ghi nhận · Người nuôi dưỡng tạm dịu cơn ho sau khi uống thuốc.");
                lines.Add("Lời kể · Theo " + state.guardian.name + ", " + state.companion.name +
                    " đã vào rừng tìm một vị thuốc khác.");
            }
            if (ItemCount(hero, HanVu.TrailId) == 1)
                lines.Add("Vật chứng · Mảnh giấy ở ngã rẽ Cô Hồn Lâm còn dòng chữ “Ma Tùng Sâm”.");
            if (ItemCount(hero, HanVu.ClueId) == 1)
            {
                lines.Add("Vật chứng · Dây vải sờn, buộc nút đôi. Tìm thấy gần sói đầu đàn ở Ma Tùng Sâm.");
                lines.Add("Suy đoán · Nút dây giống cách " + state.companion.name +
                    " thường buộc. Ai đã để nó lại, và người ấy đang ở đâu?");
            }
            return lines;
        }
    }
}
