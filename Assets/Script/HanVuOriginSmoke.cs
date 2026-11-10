#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TuTienCore;
using UnityEngine;
using static Development016Smoke;

internal static class HanVuOriginSmoke
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly OriginDefinition Origin = StoryOrigin.HanVu;
    private static void Call(object owner, string method, params object[] args) =>
        owner.GetType().GetMethod(method, Members).Invoke(owner, args);
    private static List<CombatManager.ActiveMonsterInfo> Enemies(CombatManager combat) =>
        (List<CombatManager.ActiveMonsterInfo>)typeof(CombatManager).GetField("activeMonsters", Members).GetValue(combat);
    private static string Identity(EntityDataSO data) => string.Join("|", data.entityName, data.gender.ToString(),
        data.race.ToString(), data.originStory.guardian.name, data.originStory.guardian.gender.ToString(),
        data.originStory.companion.name, data.originStory.companion.gender.ToString(),
        string.Join(",", data.spiritRoots));

    private static IEnumerator Restore(GameManager game, UIManager ui, string identity)
    {
        var data = game.HeroData;
        var old = data.originStory;
        string map = data.worldMapId;
        string story = JsonUtility.ToJson(old);
        int wave = data.worldMapLoopWave;
        Check(game.TryManualSave(SaveSlot.ManualSave1) && game.LoadAndContinue(SaveSlot.ManualSave1), "Save and resume");
        yield return Ready(game, ui);
        var loaded = game.HeroData;
        Check(loaded != data && loaded.originStory != old && Identity(loaded) == identity &&
            JsonUtility.ToJson(loaded.originStory) == story && loaded.worldMapId == map &&
            loaded.worldMapLoopWave == wave, "Candidate swaps without reroll or replayed story grant");
    }

    private static IEnumerator Go(GameManager game, UIManager ui, string destination)
    {
        Check(game.TryTravelToWorldMap(destination), "Explicit travel to " + destination);
        yield return Ready(game, ui);
        Check(game.HeroData.worldMapId == destination, "Correct manual destination");
    }

    private static IEnumerator SelectForest(GameManager game, UIManager ui)
    {
        Click(ui, "WorldMapButton");
        yield return null;
        var map = (WorldMapPanelUI)typeof(UIManager).GetField("worldMapUI", Members).GetValue(ui);
        var art = (RectTransform)typeof(WorldMapPanelUI).GetField("artRect", Members).GetValue(map);
        var polygon = WorldMapPolygons.Find(Origin.Forest);
        Check(map.IsOpen && polygon != null, "World Map opens with the authored forest polygon");
        var position = polygon.vertices.Aggregate(Vector2.zero, (sum, point) => sum + point) /
            polygon.vertices.Count;
        var local = new Vector2(art.rect.xMin + art.rect.width * position.x,
            art.rect.yMin + art.rect.height * position.y);
        var screen = RectTransformUtility.WorldToScreenPoint(null, art.TransformPoint(local));
        Check(map.HandleMapClick(screen) && game.HeroData.worldMapId == Origin.Village,
            "Selecting a visible forest polygon leaves the player in the village");
        Click(ui, "WorldMapTravelSelected");
        yield return Ready(game, ui);
        Check(game.HeroData.worldMapId == Origin.Forest, "Travel button explicitly enters the forest");
    }

    private static void VerifyGeneratedCast()
    {
        var cast = ScriptableObject.CreateInstance<EntityDataSO>();
        var names = typeof(NameDatabase);
        const BindingFlags pools = BindingFlags.NonPublic | BindingFlags.Static;
        var surnames = (string[])names.GetField("Surnames", pools).GetValue(null);
        var middles = (string[])names.GetField("MiddleNames", pools).GetValue(null);
        var male = (string[])names.GetField("MaleGivenNames", pools).GetValue(null);
        var female = (string[])names.GetField("FemaleGivenNames", pools).GetValue(null);
        var guardianGenders = new HashSet<GenderType>();
        var rootCounts = new HashSet<int>();
        var nameLengths = new HashSet<int>();
        void CheckName(string name, GenderType gender)
        {
            var parts = name.Split(' ');
            Check(parts.Length >= 2 && parts.Length <= 3 && surnames.Contains(parts[0]) &&
                (gender == GenderType.Nam ? male : female).Contains(parts[parts.Length - 1]) &&
                (parts.Length == 2 || middles.Contains(parts[1])),
                "Structured name matches surname and gendered pool");
            nameLengths.Add(parts.Length);
        }
        for (int i = 0; i < 256; i++)
        {
            StoryOrigin.InitializeNewHero(cast);
            Check(StoryOrigin.IsValid(cast), "Human hero, Human guardian and opposite-gender companion");
            CheckName(cast.entityName, cast.gender);
            CheckName(cast.originStory.guardian.name, cast.originStory.guardian.gender);
            CheckName(cast.originStory.companion.name, cast.originStory.companion.gender);
            guardianGenders.Add(cast.originStory.guardian.gender);
            rootCounts.Add(cast.spiritRoots.Count);
        }
        Check(guardianGenders.Count == 2 && rootCounts.SetEquals(new[] { 1, 2, 3, 4 }),
            "Both guardian genders and all Human root counts appear");
        var exactOdds = new int[4];
        for (int roll = 0; roll < 100; roll++) exactOdds[SynergyMath.RootCount(roll, RaceType.NhanToc) - 1]++;
        Check(nameLengths.SetEquals(new[] { 2, 3 }) && exactOdds.SequenceEqual(new[] { 60, 25, 10, 5 }),
            "Both name lengths and unchanged root distribution");
        UnityEngine.Object.Destroy(cast);
    }

    private static IEnumerator ClearWave(GameManager game, UIManager ui, CombatManager combat,
        HeroController hero, EnvironmentManager space, bool elite)
    {
        if (Enemies(combat).Count == 0) Call(game, "CallNextWave");
        var actors = Enemies(combat).ToArray();
        Check(actors.Length > 0 && (!elite || actors.Length == 1), "Real spawned wave");
        var position = space.Position(hero.heroRect);
        foreach (var enemy in actors)
        {
            Check(enemy.data.race == RaceType.YeuThu && enemy.data.monsterAnimal == "Lang" &&
                enemy.data.entityName == (elite ? "Sói đầu đàn" : "Sói rừng") &&
                enemy.controller.SpriteIdentityKey == "Wolf" && enemy.controller.attackMode == AttackMode.Melee &&
                enemy.controller.SkillIds.Length == 0, "Beast, Wolf sprite key, melee-only, no active skills");
            enemy.controller.moveSpeed = 0;
            enemy.attackTimer = 1000f;
            enemy.currentHP = 1; // Owned fixture; damage, death and reward still use the real attack.
            enemy.controller.ReceiveAlert(hero, true);
            space.SetPosition(enemy.controller.Rect, position + new Vector2(-28f, 0f));
        }
        combat.CurrentHeroCombatState.AttackTimer = 0f;
        string originalMap = game.HeroData.worldMapId;
        int wave = game.HeroData.worldMapLoopWave;
        ui.SetPaused(false);
        yield return Wait(() => game.IsWaitingForExploration, 12, "Accepted melee kills");
        ui.SetPaused(true);
        Check(game.HeroData.worldMapId == originalMap && game.HeroData.worldMapLoopWave == wave % 5 + 1,
            "Exactly one wave credit; no auto-travel");
        string state = JsonUtility.ToJson(game.HeroData.originStory);
        int experience = game.HeroData.currentExp;
        foreach (var enemy in actors)
        {
            Call(combat, "HandleMonsterDeath", enemy);
            game.OnMonsterDied(enemy.go);
        }
        Check(JsonUtility.ToJson(game.HeroData.originStory) == state && game.HeroData.currentExp == experience,
            "Duplicate kill and wave callbacks are inert");
    }

    internal static IEnumerator Run(GameManager game, SaveManager saves, UIManager ui,
        CombatManager combat, HeroController hero, EnvironmentManager space,
        Func<string,IEnumerator> capture, Action<string> pass)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        UnityEngine.Random.InitState(17312);
        VerifyGeneratedCast();
        UnityEngine.Random.InitState(17312);
        game.OnNewGameClicked(); game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        var data = game.HeroData;
        string identity = Identity(data);
        Check(StoryOrigin.IsValid(data) && StoryOrigin.IsSafe(data) && data.mapName == Origin.VillageName &&
            data.originStory.introduced && data.race == RaceType.NhanToc &&
            data.originStory.guardian.race == RaceType.NhanToc &&
            data.originStory.companion.gender == StoryOrigin.Opposite(data.gender), "Hàn Vụ New Game identity");
        Call(game, "CallNextWave");
        Check(Enemies(combat).Count == 0 && !game.IsWaitingForExploration &&
            !WorldMapCatalog.IsCleared(data, Origin.Village), "Village cannot spawn or clear a wave");
        Click(ui, "HanVuTalk");
        Click(ui, "HanVuTalk");
        Check(data.originStory.heardCompanion && StoryOrigin.Journal(data).Count == 3, "One journal entry for repeated NPC talk");
        yield return capture("han-vu-village");
        yield return Restore(game, ui, identity);
        Check(!game.TryTravelToWorldMap(Origin.EliteMap), "No story bypass from village");
        pass("New Game, Human identities, safe village, intro and replayable NPC dialogue survive Save/Load.");

        yield return SelectForest(game, ui);
        for (int wave = 1; wave <= 4; wave++)
        {
            yield return ClearWave(game, ui, combat, hero, space, false);
            Check(StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == wave, "One herb per completed wave");
        }
        Call(game, "CallNextWave");
        Check(Enemies(combat).Count > 0 && game.HeroData.worldMapLoopWave == 5, "Wave five is active");
        yield return Restore(game, ui, identity);
        Check(game.HeroData.worldMapLoopWave == 5 && StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 4,
            "Reload mid-wave five preserves completed waves only");
        Call(combat, "DealDamageToHero", int.MaxValue / 16);
        Check(!game.IsGameplayHUDVisible && game.HeroData.worldMapLoopWave == 5, "Defeat stays on wave five");
        game.OnRetryClicked();
        yield return Ready(game, ui);
        Check(Identity(game.HeroData) == identity && StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 4,
            "Retry preserves identity and quest items");
        yield return ClearWave(game, ui, combat, hero, space, false);
        Check(game.HeroData.originStory.stage == OriginStage.Return && StoryOrigin.HintMap(game.HeroData) &&
            StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 5 &&
            WorldMapCatalog.IsCleared(game.HeroData, Origin.Forest), "Deterministic herb supply and return hint");
        yield return Restore(game, ui, identity);
        pass("Five approved wolf waves, deterministic herbs, mid-wave Save/Load and actual combat defeat/retry; duplicate callbacks inert.");

        yield return Go(game, ui, Origin.Village);
        Check(Enemies(combat).Count == 0, "Returning to village stays peaceful");
        Click(ui, "HanVuTalk");
        Check(game.HeroData.originStory.stage == OriginStage.SearchForest &&
            StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 0, "Medicine consumed once");
        Click(ui, "HanVuTalk");
        Check(game.HeroData.originStory.stage == OriginStage.SearchForest, "Repeated medicine interaction is inert");
        yield return Restore(game, ui, identity);
        yield return Go(game, ui, Origin.Forest);
        ui.SetPaused(false);
        Click(ui, "HanVuTalk");
        Check(Time.timeScale > 0f && game.HeroData.originStory.stage == OriginStage.SearchElite &&
            StoryOrigin.ItemCount(game.HeroData, Origin.TrailId) == 1 && Enemies(combat).Count > 0,
            "Trail interaction advances once without freezing combat");
        yield return null;
        Click(ui, "HanVuJournal");
        Check(Time.timeScale > 0f, "Journal leaves gameplay ticking");
        Click(ui, "HanVuDismiss");
        Check(Time.timeScale > 0f, "Dismissing hints never pauses");
        ui.SetPaused(true);
        yield return Restore(game, ui, identity);
        pass("Medicine, trail clue and journal are idempotent and non-blocking.");

        yield return Go(game, ui, Origin.EliteMap);
        for (int wave = 1; wave <= 5; wave++)
            yield return ClearWave(game, ui, combat, hero, space, wave == 5);
        data = game.HeroData;
        var journal = StoryOrigin.Journal(data);
        Check(data.originStory.stage == OriginStage.Complete && StoryOrigin.ItemCount(data, Origin.ClueId) == 1 &&
            journal.Any(x => x.StartsWith("Vật chứng · Dây vải")) && journal.Any(x => x.StartsWith("Suy đoán")),
            "One elite clue; journal separates observations from conjecture");
        yield return Restore(game, ui, identity);
        Click(ui, "HanVuJournal");
        yield return capture("han-vu-journal");
        Call(game, "CallNextWave");
        Check(!StoryOrigin.IsEliteWave(game.HeroData) && Enemies(combat).All(e => e.data.entityName == "Sói rừng"),
            "Elite does not respawn as a story reward");
        yield return Go(game, ui, Origin.Forest);
        yield return Go(game, ui, Origin.EliteMap);
        Check(StoryOrigin.ItemCount(game.HeroData, Origin.ClueId) == 1, "No clue replay on return");
        pass("Final-wave elite granted one ambiguous clue; saves and revisits retain it without replay.");

        var legacy = UnityEngine.Object.Instantiate(game.HeroData);
        legacy.originStory = null;
        legacy.entityName = "Tên Đã Lưu";
        legacy.race = RaceType.YeuThu;
        Check(CultivationSaveJson.TryObject(JsonUtility.ToJson(legacy), out var fields, out var error), error);
        UnityEngine.Object.Destroy(legacy);
        fields.Remove("originStory");
        var json = "{" + string.Join(",", fields.Select(pair => "\"" + pair.Key + "\":" + pair.Value)) + "}";
        saves.SaveGame(SaveSlot.ManualSave2.ToString(), json);
        Check(saves.HasSave(SaveSlot.ManualSave2), "Write a real origin-less fixture");
        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Load legacy fixture");
        yield return Ready(game, ui);
        Check(game.HeroData.entityName == "Tên Đã Lưu" && game.HeroData.race == RaceType.YeuThu &&
            StoryOrigin.Definition(game.HeroData) == null && game.HeroData.worldMapId == Origin.EliteMap,
            "Legacy identity, race and map remain intact");
        pass("Origin-less Save loads through the real candidate path without replay or replacement of old identity.");
    }
}
#endif
