using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TuTienCore
{
    public enum OriginStage { Gather, Return, SearchForest, SearchElite, Complete }
    public enum HerbalParticipation { Offered, Accepted, Declined }

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
        public HerbalParticipation herbalParticipation;
        public bool declinedHerbalOffer;
        public bool hideStoryPrompts;
        public List<OriginEncounterRecord> encounters;
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
        public const int SaveVersion = 2;

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
                version = SaveVersion,
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
            if (origin == null || (state.version != 1 && state.version != SaveVersion) || hero.race != RaceType.NhanToc ||
                (hero.gender != GenderType.Nam && hero.gender != GenderType.Nu) ||
                !IsActor(state.guardian, origin.GuardianId) || !IsActor(state.companion, origin.CompanionId) ||
                state.companion.gender != Opposite(hero.gender) || state.stage < OriginStage.Gather ||
                state.stage > OriginStage.Complete || state.herbWaves < 0 || state.herbWaves > OriginDefinition.Waves ||
                state.searchWaves < 0 || state.searchWaves > OriginDefinition.Waves)
                return false;
            if (state.version == SaveVersion &&
                (state.herbalParticipation < HerbalParticipation.Offered ||
                 state.herbalParticipation > HerbalParticipation.Declined ||
                 (state.herbalParticipation != HerbalParticipation.Accepted &&
                  (state.stage != OriginStage.Gather || state.herbWaves != 0)) ||
                 (state.herbalParticipation == HerbalParticipation.Declined && !state.declinedHerbalOffer) ||
                 !HanVuEncounters.IsValid(state))) return false;
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
                hero.originStory.herbalParticipation == HerbalParticipation.Accepted &&
                hero.originStory.stage == OriginStage.SearchElite &&
                hero.originStory.searchWaves == OriginDefinition.Waves - 1;
        }

        public static void UpgradeSave(EntityDataSO hero)
        {
            var state = hero.originStory;
            if (state == null || state.version != 1) return;
            state.herbalParticipation = state.herbWaves > 0 || state.stage != OriginStage.Gather
                ? HerbalParticipation.Accepted : HerbalParticipation.Offered;
            state.declinedHerbalOffer = false;
            state.hideStoryPrompts = false;
            state.encounters = null;
            state.version = SaveVersion;
            hero.isDirty = true;
        }

        public static bool CanOfferHerbs(EntityDataSO hero) => IsSafe(hero) &&
            hero.originStory.stage == OriginStage.Gather &&
            hero.originStory.herbalParticipation != HerbalParticipation.Accepted;

        public static bool ChooseHerbs(EntityDataSO hero, bool accept)
        {
            if (!CanOfferHerbs(hero)) return false;
            var state = hero.originStory;
            if (!accept && state.herbalParticipation == HerbalParticipation.Declined) return false;
            state.herbalParticipation = accept ? HerbalParticipation.Accepted : HerbalParticipation.Declined;
            if (!accept) state.declinedHerbalOffer = true;
            hero.isDirty = true;
            return true;
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
                    " lại ho. Lát rảnh, ta ghé hỏi xem người còn thuốc không.";
            }
            return null;
        }

        // Called only after CombatManager accepts the final kill of a wave.
        // Quest counts follow completed encounters independently of map wave numbers.
        internal static bool CreditWave(EntityDataSO hero, bool defeatedStoryElite, out string narration)
        {
            narration = null;
            var origin = Definition(hero);
            if (origin == null || hero.originStory.herbalParticipation != HerbalParticipation.Accepted) return false;
            var state = hero.originStory;
            if (hero.worldMapId == origin.Forest && state.stage == OriginStage.Gather)
            {
                state.herbWaves++;
                if (state.herbWaves == OriginDefinition.Waves)
                {
                    state.stage = OriginStage.Return;
                    narration = "Đã đủ lá bạc. Khi về làng, ta có thể sắc thuốc.";
                }
            }
            else if (hero.worldMapId == origin.EliteMap && state.stage == OriginStage.SearchElite &&
                defeatedStoryElite == IsEliteWave(hero))
            {
                state.searchWaves++;
                if (defeatedStoryElite)
                {
                    state.stage = OriginStage.Complete;
                    narration = "Nút dây này… giống nút " + state.companion.name + " vẫn buộc. Vì sao nó lại ở đây?";
                }
            }
            else return false;
            hero.isDirty = true;
            return true;
        }

        public static bool CanInteract(EntityDataSO hero)
        {
            var origin = Definition(hero);
            return origin != null && (IsSafe(hero) ||
                (hero.worldMapId == origin.Forest && hero.originStory.herbalParticipation == HerbalParticipation.Accepted &&
                 hero.originStory.stage == OriginStage.SearchForest));
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
            if (Definition(hero) == null || hero.originStory.hideStoryPrompts ||
                hero.originStory.herbalParticipation != HerbalParticipation.Accepted) return false;
            var origin = Definition(hero);
            switch (hero.originStory.stage)
            {
                case OriginStage.Gather: return hero.worldMapId != origin.Forest;
                case OriginStage.Return: return !IsSafe(hero);
                case OriginStage.SearchForest: return hero.worldMapId != origin.Forest;
                case OriginStage.SearchElite: return hero.worldMapId != origin.EliteMap;
                default: return false;
            }
        }

        public static string Hint(EntityDataSO hero)
        {
            var state = hero.originStory;
            if (state.hideStoryPrompts || state.herbalParticipation != HerbalParticipation.Accepted) return "";
            switch (state.stage)
            {
                case OriginStage.Gather: return hero.worldMapId == HanVu.Forest
                    ? "Lá bạc " + state.herbWaves + "/5. Xem chiến lợi phẩm trong Kho đồ."
                    : "Lá bạc mọc ở Cô Hồn Lâm. Có thể trở lại khi thuận đường.";
                case OriginStage.Return: return IsSafe(hero) ? "Đã đủ lá bạc để sắc thuốc."
                    : "Có thể trở về Hàn Vụ thôn để sắc thuốc.";
                case OriginStage.SearchForest: return hero.worldMapId == HanVu.Forest
                    ? "Có thể xem dấu vết ở ngã rẽ."
                    : "Cô Hồn Lâm là nơi " + state.companion.name + " được nhắc tới lần cuối.";
                case OriginStage.SearchElite: return hero.worldMapId == HanVu.EliteMap
                    ? "Đợt tìm kiếm " + state.searchWaves + "/5. Sói đầu đàn xuất hiện ở đợt cuối."
                    : "Mảnh giấy chỉ về Ma Tùng Sâm.";
                default: return "";
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
            if (state.declinedHerbalOffer)
                lines.Add("Lựa chọn · Đã từ chối lời nhờ tìm thuốc.");
            if (state.herbalParticipation == HerbalParticipation.Accepted)
                lines.Add("Lựa chọn · Đã nhận lời tìm lá bạc.");
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
            HanVuEncounters.AddJournal(state, lines);
            return lines;
        }
    }
}
