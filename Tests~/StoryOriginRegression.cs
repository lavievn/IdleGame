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
    }
}
