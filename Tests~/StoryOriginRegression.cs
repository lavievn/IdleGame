using System.Collections.Generic;
using System.Linq;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static void StoryOriginTests()
    {
        Run("Hàn Vụ identity reuses name pools and preserves Human root diversity", () =>
        {
            var genders = new HashSet<GenderType>();
            var guardianGenders = new HashSet<GenderType>();
            var roots = new HashSet<int>();
            var hero = ScriptableObject.CreateInstance<EntityDataSO>();
            for (int i = 0; i < 500; i++)
            {
                StoryOrigin.InitializeNewHero(hero);
                var state = hero.originStory;
                Check(StoryOrigin.IsValid(hero) && hero.race == RaceType.NhanToc &&
                    state.guardian.race == RaceType.NhanToc && state.companion.race == RaceType.NhanToc &&
                    state.companion.gender == StoryOrigin.Opposite(hero.gender), "Human cast, opposite companion");
                foreach (string name in new[] { hero.entityName, state.guardian.name, state.companion.name })
                    Check(name.Split(' ').Length >= 2 && name.Split(' ').Length <= 3, "Generated 2–3 word name");
                genders.Add(hero.gender);
                guardianGenders.Add(state.guardian.gender);
                roots.Add(hero.spiritRoots.Count);
            }
            Check(genders.Count == 2 && guardianGenders.Count == 2 && roots.SetEquals(new[] { 1, 2, 3, 4 }),
                "Both genders and all Human root counts appear");
        });

        Run("Hàn Vụ separates optional progress, world travel and encounter decisions", () =>
        {
            var hero = ScriptableObject.CreateInstance<EntityDataSO>();
            StoryOrigin.InitializeNewHero(hero);
            var origin = StoryOrigin.HanVu;
            hero.worldMapId = origin.Village;
            Check(StoryOrigin.IsSafe(hero) && StoryOrigin.Arrive(hero) != null &&
                StoryOrigin.Arrive(hero) == null && !StoryOrigin.HintMap(hero), "Quiet intro without taking a quest");
            var merchant = HanVuEncounters.Available(hero);
            Check(merchant != null && hero.originStory.encounters == null, "Unseen offer creates no record");
            Check(HanVuEncounters.Observe(hero, merchant.Id) && !HanVuEncounters.Observe(hero, merchant.Id) &&
                !HanVuEncounters.Choose(hero, merchant.Id, EncounterChoice.Pending), "Opening records only observation");
            Check(HanVuEncounters.Choose(hero, merchant.Id, EncounterChoice.Declined) &&
                !HanVuEncounters.Choose(hero, merchant.Id, EncounterChoice.Participated) &&
                HanVuEncounters.Available(hero) == null, "Refusal is final for this encounter");
            StoryOrigin.Interact(hero);
            Check(StoryOrigin.ChooseHerbs(hero, false), "Explicit quest decline");
            hero.worldMapId = origin.Forest;
            Check(!StoryOrigin.CreditWave(hero, false, out _) && StoryOrigin.ItemCount(hero, origin.HerbId) == 0,
                "Declined quest receives no herbs from farm");
            hero.worldMapId = origin.Village;
            Check(StoryOrigin.ChooseHerbs(hero, true) && !StoryOrigin.ChooseHerbs(hero, true), "Later opt-in");
            hero.worldMapId = origin.Forest;
            for (int wave = 1; wave <= 5; wave++)
            {
                Check(StoryOrigin.CreditWave(hero, false, out _) && StoryOrigin.ItemCount(hero, origin.HerbId) == wave,
                    "Accepted wave contributes exactly one herb");
            }
            Check(!StoryOrigin.CreditWave(hero, false, out _) && hero.originStory.stage == OriginStage.Return,
                "Herbs capped at five");
            hero.worldMapId = origin.Village;
            StoryOrigin.Interact(hero);
            StoryOrigin.Interact(hero);
            Check(StoryOrigin.ItemCount(hero, origin.HerbId) == 0, "Herbs consumed once");
            hero.worldMapId = origin.Forest;
            StoryOrigin.Interact(hero);
            Check(StoryOrigin.Interact(hero) == null && StoryOrigin.ItemCount(hero, origin.TrailId) == 1,
                "Trail once");
            hero.worldMapId = origin.EliteMap;
            Check(!StoryOrigin.CreditWave(hero, true, out _), "Premature elite rejected");
            for (int wave = 1; wave <= 4; wave++) StoryOrigin.CreditWave(hero, false, out _);
            Check(StoryOrigin.IsEliteWave(hero) && !StoryOrigin.CreditWave(hero, false, out _),
                "Only the genuine elite can finish the quest");
            Check(StoryOrigin.CreditWave(hero, true, out _) && !StoryOrigin.CreditWave(hero, true, out _) &&
                StoryOrigin.ItemCount(hero, origin.ClueId) == 1 && StoryOrigin.IsValid(hero), "One clue");
            Check(StoryOrigin.Journal(hero).Any(line => line.StartsWith("Suy đoán")), "Inference separately recorded");
            hero.originStory = null;
            Check(StoryOrigin.IsValid(hero) && !StoryOrigin.IsWolfMap(hero), "Origin-less hero");
        });

        Run("Hàn Vụ dialogue uses all saved gender combinations without changing identity", () =>
        {
            foreach (var gender in new[] { GenderType.Nam, GenderType.Nu })
            foreach (var guardianGender in new[] { GenderType.Nam, GenderType.Nu })
            {
                var hero = ScriptableObject.CreateInstance<EntityDataSO>();
                StoryOrigin.InitializeNewHero(hero);
                hero.gender = gender;
                hero.entityName = "Tên nhân vật đã lưu";
                var state = hero.originStory;
                state.guardian.gender = guardianGender;
                state.guardian.name = "Tên người nuôi đã lưu";
                state.companion.gender = StoryOrigin.Opposite(gender);
                state.companion.name = "Tên bạn đã lưu";
                string title = guardianGender == GenderType.Nam ? "Ông " : "Bà ";
                var opening = HanVuNarrative.Lines(hero, HanVuConversation.Opening);
                Check(opening.Length == 6 && opening[0].Speaker == title + state.guardian.name &&
                    opening[1].Speaker == hero.entityName && opening[4].Text.Contains(state.companion.name),
                    "Opening addresses the persisted cast");
                var woodcutter = HanVuNarrative.Lines(hero, HanVuConversation.Woodcutter);
                Check(woodcutter[0].Speaker == hero.entityName && woodcutter[0].Text.Contains(state.companion.name) &&
                    woodcutter[1].Text.Contains(gender == GenderType.Nam ? "Cô ấy" : "Cậu ấy"),
                    "Question precedes gendered testimony");
                Check(!state.heardOpeningTrip && !state.heardWoodcutter &&
                    state.herbalParticipation == HerbalParticipation.Offered && StoryOrigin.IsValid(hero),
                    "Building dialogue neither records testimony nor accepts the quest");
            }
        });

        Run("Hàn Vụ woodcutter records only the displayed answer, once and at the forest", () =>
        {
            var hero = ScriptableObject.CreateInstance<EntityDataSO>();
            StoryOrigin.InitializeNewHero(hero);
            hero.worldMapId = StoryOrigin.HanVu.Forest;
            hero.isDirty = false;
            Check(!HanVuNarrative.TalkToWoodcutter(hero, 0) && !hero.isDirty &&
                !StoryOrigin.Journal(hero).Any(x => x.Contains("Người đốn củi")),
                "Closing at the question invents no testimony");
            Check(HanVuNarrative.TalkToWoodcutter(hero, 1) &&
                !HanVuNarrative.TalkToWoodcutter(hero, 1) &&
                StoryOrigin.Journal(hero).Count(x => x.StartsWith("Lời kể · Người đốn củi")) == 1,
                "Answer creates one replay-safe journal fact");
            hero.worldMapId = StoryOrigin.HanVu.Village;
            Check(!HanVuNarrative.TalkToWoodcutter(hero, 1) && !HanVuNarrative.ObserveKnot(hero) &&
                !HanVuNarrative.ObserveNote(hero), "Forest facts cannot be credited elsewhere");
            Check(hero.originStory.herbalParticipation == HerbalParticipation.Offered &&
                hero.originStory.herbWaves == 0, "Optional testimony does not start the quest");
        });

        Run("Hàn Vụ ignored encounters remain pending across travel and do not imply refusal", () =>
        {
            var hero = ScriptableObject.CreateInstance<EntityDataSO>();
            StoryOrigin.InitializeNewHero(hero);
            WorldMapCatalog.StartNew(hero, StoryOrigin.HanVu.Village);
            var merchant = HanVuEncounters.Available(hero);
            Check(HanVuEncounters.Observe(hero, merchant.Id), "Open the encounter");
            Check(!HanVuEncounters.Choose(hero, merchant.Id, EncounterChoice.Pending) &&
                hero.originStory.encounters[0].choice == EncounterChoice.Pending &&
                !StoryOrigin.Journal(hero).Any(x => x.StartsWith("Lựa chọn")), "Close is neutral");
            Check(WorldMapCatalog.TryTravel(hero, StoryOrigin.HanVu.Forest), "Leave before accepting herbs");
            for (int i = 0; i < 5; i++)
            {
                hero.CompleteWave();
                Check(!StoryOrigin.CreditWave(hero, false, out _), "Farm is not quest participation");
            }
            Check(WorldMapCatalog.TryTravel(hero, StoryOrigin.HanVu.Village) &&
                HanVuEncounters.Available(hero).Id == merchant.Id &&
                hero.originStory.encounters[0].choice == EncounterChoice.Pending, "Pending offer survives return");
            Check(HanVuEncounters.Choose(hero, merchant.Id, EncounterChoice.Participated) &&
                !HanVuEncounters.Choose(hero, merchant.Id, EncounterChoice.Participated) &&
                hero.originStory.herbWaves == 0, "One explicit result with no quest credit");
        });

        Run("Hàn Vụ clues, quest counts and hidden prompts are independent", () =>
        {
            var hero = ScriptableObject.CreateInstance<EntityDataSO>();
            StoryOrigin.InitializeNewHero(hero);
            hero.worldMapId = StoryOrigin.HanVu.Village;
            Check(StoryOrigin.ChooseHerbs(hero, true), "Opt in");
            hero.originStory.hideStoryPrompts = true;
            hero.worldMapId = "TV-24";
            Check(!StoryOrigin.CreditWave(hero, false, out _) && !HanVuNarrative.ObserveNote(hero),
                "Unrelated waves and maps create no story credit");
            hero.worldMapId = StoryOrigin.HanVu.Forest;
            Check(HanVuNarrative.ObserveKnot(hero) && !HanVuNarrative.ObserveKnot(hero) &&
                HanVuNarrative.ObserveNote(hero) && !HanVuNarrative.ObserveNote(hero), "Observations are idempotent");
            Check(StoryOrigin.ItemCount(hero, StoryOrigin.HanVu.TrailId) == 0 &&
                hero.originStory.stage == OriginStage.Gather && !hero.originStory.heardWoodcutter,
                "Seeing a clue does not start the search or fabricate testimony");
            for (int i = 0; i < 5; i++) Check(StoryOrigin.CreditWave(hero, false, out _), "Hidden quest still progresses");
            Check(StoryOrigin.Hint(hero) == "" && !StoryOrigin.HintMap(hero) &&
                hero.originStory.stage == OriginStage.Return && StoryOrigin.IsValid(hero), "Hints are optional presentation");
        });

        Run("Hàn Vụ v1 and v2 migration preserves cast and decisions and is idempotent", () =>
        {
            foreach (int version in new[] { 1, 2 })
            foreach (int herbs in new[] { 0, 2, 5 })
            {
                var hero = ScriptableObject.CreateInstance<EntityDataSO>();
                StoryOrigin.InitializeNewHero(hero);
                var state = hero.originStory;
                state.version = version;
                state.herbWaves = herbs;
                state.stage = herbs == 5 ? OriginStage.Return : OriginStage.Gather;
                state.herbalParticipation = herbs > 0 ? HerbalParticipation.Accepted : HerbalParticipation.Offered;
                hero.worldMapId = StoryOrigin.HanVu.Forest;
                var guardian = state.guardian;
                var companion = state.companion;
                if (version == 2)
                {
                    state.hideStoryPrompts = true;
                    state.encounters = new List<OriginEncounterRecord>
                    {
                        new OriginEncounterRecord { id = "hanvu.merchant_rumor", choice = EncounterChoice.Declined }
                    };
                }
                Check(StoryOrigin.IsValid(hero), "Legacy state valid before migration");
                StoryOrigin.UpgradeSave(hero);
                Check(state.version == StoryOrigin.SaveVersion && StoryOrigin.IsValid(hero) &&
                    state.guardian == guardian && state.companion == companion && state.herbWaves == herbs &&
                    hero.worldMapId == StoryOrigin.HanVu.Forest && !state.heardOpeningTrip && !state.heardWoodcutter,
                    "Upgrade retains identities, map, counts and does not invent testimony");
                Check(version != 2 || (state.hideStoryPrompts && state.encounters[0].choice == EncounterChoice.Declined),
                    "V2 choices and prompt preference survive");
                state.heardWoodcutter = true;
                hero.isDirty = false;
                StoryOrigin.UpgradeSave(hero);
                Check(state.heardWoodcutter && !hero.isDirty, "Repeated upgrade preserves newly observed facts");
            }
        });
    }
}
