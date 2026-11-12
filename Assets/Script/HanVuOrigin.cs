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
        public bool heardCompanion; // Testimony already recorded by older saves.
        public bool heardOpeningTrip;
        public bool heardWoodcutter;
        public bool sawTrailKnot;
        public bool sawTrailNote;
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
        public const int SaveVersion = 3;

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

        // Detached story snapshots only; no normalization, migration or reroll.
        // Publishing into the existing root preserves N3 dialogue-context identity.
        internal static OriginStoryState CopyState(OriginStoryState source, OriginStoryState target = null)
        {
            if (source == null || ReferenceEquals(source, target)) return source;
            target = target ?? new OriginStoryState();
            target.version = source.version;
            target.originId = source.originId;
            target.guardian = CopyActor(source.guardian);
            target.companion = CopyActor(source.companion);
            target.introduced = source.introduced;
            target.heardCompanion = source.heardCompanion;
            target.heardOpeningTrip = source.heardOpeningTrip;
            target.heardWoodcutter = source.heardWoodcutter;
            target.sawTrailKnot = source.sawTrailKnot;
            target.sawTrailNote = source.sawTrailNote;
            target.stage = source.stage;
            target.herbWaves = source.herbWaves;
            target.searchWaves = source.searchWaves;
            target.herbalParticipation = source.herbalParticipation;
            target.declinedHerbalOffer = source.declinedHerbalOffer;
            target.hideStoryPrompts = source.hideStoryPrompts;
            target.encounters = source.encounters == null ? null : source.encounters.ConvertAll(record =>
                record == null ? null : new OriginEncounterRecord { id = record.id, choice = record.choice });
            return target;
        }

        private static OriginActor CopyActor(OriginActor source) => source == null ? null : new OriginActor
        { id = source.id, name = source.name, gender = source.gender, race = source.race };

        public static bool IsValid(EntityDataSO hero)
        {
            return IsValidState(hero.originStory, hero.race, hero.gender);
        }

        private static bool IsValidState(OriginStoryState state, RaceType race, GenderType gender)
        {
            if (state == null) return true;
            if (string.IsNullOrEmpty(state.originId)) return state.version == 0;
            var origin = state.originId == HanVu.Id ? HanVu : null;
            if (origin == null || (state.version < 1 || state.version > SaveVersion) || race != RaceType.NhanToc ||
                (gender != GenderType.Nam && gender != GenderType.Nu) ||
                !IsActor(state.guardian, origin.GuardianId) || !IsActor(state.companion, origin.CompanionId) ||
                state.companion.gender != Opposite(gender) || state.stage < OriginStage.Gather ||
                state.stage > OriginStage.Complete || state.herbWaves < 0 || state.herbWaves > OriginDefinition.Waves ||
                state.searchWaves < 0 || state.searchWaves > OriginDefinition.Waves)
                return false;
            if (state.version >= 2 &&
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

        // Presence and scalar types must be checked before JsonUtility supplies
        // default values. Required fields follow the existing v1/v2/v3 contract.
        internal static bool IsValidSnapshotJson(string json)
        {
            if (!CultivationSaveJson.TryObject(json, out var root, out _)) return false;
            if (!root.TryGetValue("originStory", out var token) || token == "null") return true;
            if (!CultivationSaveJson.TryObject(token, out var fields, out _) ||
                !ReadInteger(fields, "version", out int version)) return false;
            foreach (var name in fields.Keys)
                if (typeof(OriginStoryState).GetField(name) == null) return false;
            if (version == 0)
                return !fields.TryGetValue("originId", out var empty) || empty == "null" ||
                    CultivationSaveJson.TryString(empty, out var id) && string.IsNullOrEmpty(id);
            if (version < 1 || version > SaveVersion || !ReadText(fields, "originId") ||
                !ReadActor(fields, "guardian") || !ReadActor(fields, "companion")) return false;
            foreach (var name in new[] { "stage", "herbWaves", "searchWaves" })
                if (!ReadInteger(fields, name, out _)) return false;
            foreach (var name in new[] { "introduced", "heardCompanion" })
                if (!ReadBoolean(fields, name)) return false;
            if (version >= 2)
            {
                if (!ReadInteger(fields, "herbalParticipation", out _) ||
                    !ReadBoolean(fields, "declinedHerbalOffer") || !ReadBoolean(fields, "hideStoryPrompts") ||
                    !fields.TryGetValue("encounters", out var records) || !ReadEncounters(records)) return false;
            }
            if (version >= 3)
                foreach (var name in new[] { "heardOpeningTrip", "heardWoodcutter", "sawTrailKnot", "sawTrailNote" })
                    if (!ReadBoolean(fields, name)) return false;
            if (!ReadInteger(root, "race", out int race) || !ReadInteger(root, "gender", out int gender)) return false;
            try
            {
                return IsValidState(JsonUtility.FromJson<OriginStoryState>(token), (RaceType)race, (GenderType)gender);
            }
            catch (ArgumentException) { return false; }
        }

        private static bool ReadInteger(Dictionary<string, string> fields, string name, out int value)
        {
            value = 0;
            return fields.TryGetValue(name, out var token) && int.TryParse(token,
                System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out value);
        }
        private static bool ReadText(Dictionary<string, string> fields, string name) =>
            fields.TryGetValue(name, out var token) && CultivationSaveJson.TryString(token, out _);
        private static bool ReadBoolean(Dictionary<string, string> fields, string name) =>
            fields.TryGetValue(name, out var token) && (token == "true" || token == "false");
        private static bool ReadActor(Dictionary<string, string> fields, string name) =>
            fields.TryGetValue(name, out var token) && CultivationSaveJson.TryObject(token, out var actor, out _) &&
            actor.Count == 4 && ReadText(actor, "id") && ReadText(actor, "name") && ReadInteger(actor, "gender", out _) && ReadInteger(actor, "race", out _);

        private static bool ReadEncounters(string token)
        {
            if (token == "null") return true;
            if (token.Length < 2 || token[0] != '[' || token[token.Length - 1] != ']') return false;
            // The structural reader has already validated syntax/duplicates at
            // every depth. Split only top-level array elements, respecting strings.
            int start = 1, depth = 0;
            bool quoted = false, escaped = false;
            for (int i = 1; i < token.Length; i++)
            {
                char c = token[i];
                if (quoted)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') quoted = false;
                    continue;
                }
                if (c == '"') { quoted = true; continue; }
                if (depth == 0 && (c == ',' || i == token.Length - 1))
                {
                    string item = token.Substring(start, i - start).Trim();
                    if (item.Length == 0) return start == 1 && c == ']';
                    if (!CultivationSaveJson.TryObject(item, out var record, out _) ||
                        record.Count != 2 || !ReadText(record, "id") || !ReadInteger(record, "choice", out _)) return false;
                    start = i + 1;
                }
                else if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']') depth--;
            }
            return true;
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
            if (state == null || state.version < 1 || state.version >= SaveVersion) return;
            if (state.version == 1)
            {
                state.herbalParticipation = state.herbWaves > 0 || state.stage != OriginStage.Gather
                    ? HerbalParticipation.Accepted : HerbalParticipation.Offered;
                state.declinedHerbalOffer = false;
                state.hideStoryPrompts = false;
                state.encounters = null;
            }
            state.heardOpeningTrip = false;
            state.heardWoodcutter = false;
            state.sawTrailKnot = false;
            state.sawTrailNote = false;
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
                    " ho khẽ bên bếp.";
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
                    narration = "Cạnh gốc thông có dải vải thắt nút đôi. Có lẽ ai đó đánh dấu lối về.";
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
                HanVuNarrative.ObserveNote(hero);
                state.stage = OriginStage.SearchElite;
                hero.isDirty = true;
                return HanVuNarrative.TrailNote;
            }
            string guardian = state.guardian.name;
            string elder = state.guardian.gender == GenderType.Nam ? "ông" : "bà";
            string companion = state.companion.name;
            if (state.stage == OriginStage.Return)
            {
                state.stage = OriginStage.SearchForest;
                hero.isDirty = true;
                return "Sắc thuốc xong, " + guardian + " dịu cơn ho.";
            }
            if (state.stage == OriginStage.Gather)
            {
                return guardian + ": “Con khoác thêm áo đi. Sáng nay sương dày lắm.”";
            }
            return guardian + ": “" + elder + " đã đỡ ho. " + companion + " vẫn chưa về.”";
        }

        public static string InteractionLabel(EntityDataSO hero) => IsSafe(hero)
            ? (hero.originStory.stage == OriginStage.Return ? "Mang thuốc về" : "Trò chuyện")
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

            }
            if (ItemCount(hero, HanVu.TrailId) == 1)
                lines.Add("Vật chứng · " + HanVuNarrative.TrailNote);
            if (ItemCount(hero, HanVu.ClueId) == 1)
            {
                lines.Add("Vật chứng · Dải vải thắt nút đôi buộc ở gốc thông, gần chỗ sói đầu đàn tại Ma Tùng Sâm.");
                lines.Add("Suy đoán · Có thể " + state.companion.name +
                    " từng qua đây. Cũng có thể đây là dấu của người đi rừng.");
            }
            HanVuNarrative.AddJournal(hero, lines);
            HanVuEncounters.AddJournal(state, lines);
            return lines;
        }
    }
}
