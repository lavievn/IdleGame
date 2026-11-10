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

        Run("Hàn Vụ events are ordered, scoped to the route and award each item once", () =>
        {
            var hero = ScriptableObject.CreateInstance<EntityDataSO>();
            StoryOrigin.InitializeNewHero(hero);
            var origin = StoryOrigin.HanVu;
            hero.worldMapId = origin.Village;
            Check(StoryOrigin.IsSafe(hero) && StoryOrigin.Arrive(hero) != null &&
                StoryOrigin.Arrive(hero) == null, "Intro once");
            Check(StoryOrigin.TravelOverride(hero, origin.Forest, out var reason) && reason == null,
                "Peaceful village can reach forest");
            StoryOrigin.Interact(hero);
            StoryOrigin.Interact(hero);
            Check(StoryOrigin.Journal(hero).Count == 3, "Talk fact appears once");
            hero.worldMapId = origin.Forest;
            Check(StoryOrigin.IsWolfMap(hero), "Forest wolf profile");
            for (int wave = 1; wave <= 5; wave++)
            {
                StoryOrigin.AfterWave(hero, wave);
                StoryOrigin.AfterWave(hero, wave);
                Check(StoryOrigin.ItemCount(hero, origin.HerbId) == wave, "One herb per wave ordinal");
            }
            Check(hero.originStory.stage == OriginStage.Return && StoryOrigin.HintMap(hero), "Prompt manual return");
            Check(StoryOrigin.TravelOverride(hero, origin.EliteMap, out reason) && reason != null,
                "Elite blocked before trail");
            hero.worldMapId = origin.Village;
            StoryOrigin.Interact(hero);
            StoryOrigin.Interact(hero);
            Check(StoryOrigin.ItemCount(hero, origin.HerbId) == 0, "Herbs consumed once");
            hero.worldMapId = origin.Forest;
            StoryOrigin.Interact(hero);
            Check(StoryOrigin.Interact(hero) == null && StoryOrigin.ItemCount(hero, origin.TrailId) == 1,
                "Trail once");
            hero.worldMapId = origin.EliteMap;
            Check(StoryOrigin.AfterWave(hero, 5) == null, "Out-of-order boss does not grant a clue");
            for (int wave = 1; wave <= 4; wave++) StoryOrigin.AfterWave(hero, wave);
            StoryOrigin.RestoreOpeningWave(hero);
            Check(hero.worldMapLoopWave == 5 && StoryOrigin.IsEliteWave(hero), "Resume at actual elite wave");
            StoryOrigin.AfterWave(hero, 5);
            Check(StoryOrigin.AfterWave(hero, 5) == null && !StoryOrigin.IsEliteWave(hero) &&
                StoryOrigin.ItemCount(hero, origin.ClueId) == 1 && StoryOrigin.IsValid(hero),
                "Completion is exactly once");
            Check(StoryOrigin.Journal(hero).Any(line => line.StartsWith("Suy đoán")), "Inference separately recorded");
            hero.originStory = null;
            Check(StoryOrigin.IsValid(hero) && !StoryOrigin.IsWolfMap(hero) &&
                !StoryOrigin.TravelOverride(hero, origin.Forest, out reason), "Legacy campaign keeps normal map rules");
        });
    }
}
