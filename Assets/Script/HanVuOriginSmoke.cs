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

    private static WorldMapPanelUI SelectPolygon(UIManager ui, string id)
    {
        var map = (WorldMapPanelUI)typeof(UIManager).GetField("worldMapUI", Members).GetValue(ui);
        var art = (RectTransform)typeof(WorldMapPanelUI).GetField("artRect", Members).GetValue(map);
        var polygon = WorldMapPolygons.Find(id);
        Check(map.IsOpen && polygon != null, "World Map opens with the authored polygon");
        var position = polygon.vertices.Aggregate(Vector2.zero, (sum, point) => sum + point) /
            polygon.vertices.Count;
        var local = new Vector2(art.rect.xMin + art.rect.width * position.x,
            art.rect.yMin + art.rect.height * position.y);
        var screen = RectTransformUtility.WorldToScreenPoint(null, art.TransformPoint(local));
        Check(map.HandleMapClick(screen), "Map accepts the polygon hit");
        return map;
    }

    private static IEnumerator SelectForest(GameManager game, UIManager ui)
    {
        Click(ui, "WorldMapButton");
        yield return null;
        SelectPolygon(ui, Origin.Forest);
        Check(game.HeroData.worldMapId == Origin.Village, "Selection leaves the player in the village");
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
            Check(enemy.storyElite == elite, "Authoritative elite marker");
            if (StoryOrigin.IsWolfMap(game.HeroData))
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

    private const string TravelerMap = "TV-24";
    private const string MerchantId = "hanvu.merchant_rumor";
    private const string BasketId = "hanvu.herb_basket";
    private const string TravelerId = "hanvu.wounded_traveler";

    private static OriginEncounterRecord Record(GameManager game, string id) =>
        game.HeroData.originStory.encounters?.Find(entry => entry.id == id);

    private static string Json(Dictionary<string, string> fields) =>
        "{" + string.Join(",", fields.Select(pair => "\"" + pair.Key + "\":" + pair.Value)) + "}";

    private static void WriteOldSave(GameManager game, SaveManager saves, bool priorOrigin, int herbs)
    {
        var clone = UnityEngine.Object.Instantiate(game.HeroData);
        if (priorOrigin)
        {
            clone.originStory.version = 1;
            clone.originStory.stage = OriginStage.Gather;
            clone.originStory.herbWaves = herbs;
            clone.originStory.searchWaves = 0;
        }
        else
        {
            clone.originStory = null;
            clone.entityName = "Tên Đã Lưu";
            clone.race = RaceType.YeuThu;
        }
        Check(CultivationSaveJson.TryObject(JsonUtility.ToJson(clone), out var fields, out var error), error);
        UnityEngine.Object.Destroy(clone);
        if (priorOrigin)
        {
            Check(CultivationSaveJson.TryObject(fields["originStory"], out var story, out error), error);
            foreach (string key in new[] { "herbalParticipation", "declinedHerbalOffer", "hideStoryPrompts", "encounters" })
                story.Remove(key);
            fields["originStory"] = Json(story);
        }
        else fields.Remove("originStory");
        saves.SaveGame(SaveSlot.ManualSave2.ToString(), Json(fields));
        Check(game.LoadAndContinue(SaveSlot.ManualSave2), "Load earlier snapshot");
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
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game, ui);
        var data = game.HeroData;
        string identity = Identity(data);
        Check(StoryOrigin.IsValid(data) && StoryOrigin.IsSafe(data) && data.originStory.introduced &&
            data.originStory.herbalParticipation == HerbalParticipation.Offered &&
            !data.originStory.heardCompanion && !StoryOrigin.HintMap(data), "Fresh optional origin");
        Call(game, "CallNextWave");
        Check(Enemies(combat).Count == 0 && !game.IsWaitingForExploration &&
            !WorldMapCatalog.IsCleared(data, Origin.Village), "Village stays safe");
        Check(WorldMapCatalog.TravelBlockReason(data, TravelerMap) == null &&
            WorldMapCatalog.TravelBlockReason(data, Origin.EliteMap) != null, "Adjacent departures and actual adjacency");
        Check(Record(game, MerchantId) == null && !StoryOrigin.Journal(data).Any(x => x.Contains("bán hàng")),
            "Unseen encounter absent from journal");
        yield return capture("optional-han-vu-village");
        yield return SelectForest(game, ui);
        Click(ui, "WorldMapButton");
        yield return null;
        var map = SelectPolygon(ui, Origin.Village);
        var summary = (TMPro.TextMeshProUGUI)typeof(WorldMapPanelUI).GetField("summary", Members).GetValue(map);
        var travel = (RectTransform)typeof(WorldMapPanelUI).GetField("travel", Members).GetValue(map);
        Check(summary.text.Contains("5 wave") && !travel.gameObject.activeSelf,
            "Ordinary map's clear gate stays visible and blocks the return route");
        map.Close();
        yield return null;
        var activeTime = game.Cultivation.ActiveSeconds;
        for (int i = 0; i < 5; i++) yield return ClearWave(game, ui, combat, hero, space, false);
        data = game.HeroData;
        Check(data.originStory.herbalParticipation == HerbalParticipation.Offered &&
            data.originStory.herbWaves == 0 && WorldMapCatalog.IsCleared(data, Origin.Forest) &&
            game.Cultivation.ActiveSeconds > activeTime, "Unaccepted story leaves farm, travel unlock and cultivation active");
        yield return Restore(game, ui, identity);
        pass("Safe start and UI travel without quest, standard return gate, five farm waves, cultivation and Save/Load.");

        ui.SetPaused(false);
        Click(ui, "HanVuEncounter");
        yield return null;
        Check(Time.timeScale > 0f && Record(game, BasketId)?.choice == EncounterChoice.Pending,
            "Opening encounter observes it while combat runs");
        yield return capture("optional-han-vu-encounter");
        Click(ui, "HanVuAccept");
        Check(Time.timeScale > 0f && Record(game, BasketId).choice == EncounterChoice.Participated &&
            !game.TryChooseOriginEncounter(BasketId, EncounterChoice.Participated), "Participate once");
        ui.SetPaused(true);
        yield return Go(game, ui, TravelerMap);
        Click(ui, "HanVuEncounter");
        yield return null;
        Click(ui, "HanVuDecline");
        Check(Record(game, TravelerId)?.choice == EncounterChoice.Declined &&
            HanVuEncounters.Available(game.HeroData) == null, "Refusal persists with no reopening");
        for (int i = 0; i < 5; i++) yield return ClearWave(game, ui, combat, hero, space, false);
        yield return Go(game, ui, Origin.Village);
        Click(ui, "HanVuEncounter");
        yield return null;
        Click(ui, "HanVuDismiss");
        var journal = StoryOrigin.Journal(game.HeroData);
        Check(Record(game, MerchantId)?.choice == EncounterChoice.Pending &&
            journal.Any(x => x.StartsWith("Ghi nhận") && x.Contains("bán hàng")) &&
            !journal.Any(x => x.Contains("Xin phép đi") || x.Contains("sương trong rừng")) &&
            game.HeroData.originStory.herbWaves == 0, "Closing records the observation only");
        yield return Restore(game, ui, identity);
        Check(Record(game, MerchantId).choice == EncounterChoice.Pending &&
            Record(game, TravelerId).choice == EncounterChoice.Declined &&
            Record(game, BasketId).choice == EncounterChoice.Participated, "All three dispositions round trip");
        Check(game.OpenOriginEncounter()?.Id == MerchantId, "Pending encounter remains reopenable");
        pass("Three optional encounters exercise participation, refusal and no response. Outcomes and discovery persist.");

        Click(ui, "HanVuTalk");
        yield return null;
        Click(ui, "HanVuDismiss");
        Check(game.HeroData.originStory.herbalParticipation == HerbalParticipation.Offered, "Closing quest offer is neutral");
        yield return null;
        Click(ui, "HanVuTalk");
        yield return null;
        Click(ui, "HanVuDecline");
        Check(game.HeroData.originStory.herbalParticipation == HerbalParticipation.Declined &&
            WorldMapCatalog.TravelBlockReason(game.HeroData, Origin.Forest) == null, "Quest refusal has no travel penalty");
        yield return Restore(game, ui, identity);
        Click(ui, "HanVuTalk");
        yield return null;
        Click(ui, "HanVuAccept");
        Check(game.HeroData.originStory.herbalParticipation == HerbalParticipation.Accepted &&
            game.HeroData.originStory.herbWaves == 0, "Explicit acceptance grants no retroactive herbs");
        yield return null;
        Click(ui, "HanVuJournal");
        yield return null;
        Click(ui, "HanVuPrompts");
        Click(ui, "HanVuDismiss");
        yield return Go(game, ui, Origin.Forest);
        for (int i = 0; i < 2; i++) yield return ClearWave(game, ui, combat, hero, space, false);
        yield return Go(game, ui, TravelerMap);
        yield return ClearWave(game, ui, combat, hero, space, false);
        yield return Go(game, ui, Origin.Forest);
        Check(game.HeroData.worldMapLoopWave == 1 && game.HeroData.originStory.herbWaves == 2,
            "Travel leaves quest count independent of world wave index");
        for (int i = 0; i < 2; i++) yield return ClearWave(game, ui, combat, hero, space, false);
        Call(game, "CallNextWave");
        yield return Restore(game, ui, identity);
        Check(game.HeroData.worldMapLoopWave == 3 && game.HeroData.originStory.hideStoryPrompts &&
            StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 4, "Reload before the fifth qualifying completion");
        Call(combat, "DealDamageToHero", int.MaxValue / 16);
        Check(!game.IsGameplayHUDVisible && game.HeroData.worldMapLoopWave == 1, "Defeat uses regular world reset");
        game.OnRetryClicked();
        yield return Ready(game, ui);
        Check(Identity(game.HeroData) == identity && StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 4,
            "Retry preserves identity and herbs");
        yield return ClearWave(game, ui, combat, hero, space, false);
        Check(game.HeroData.originStory.stage == OriginStage.Return && !StoryOrigin.HintMap(game.HeroData) &&
            StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 5, "Five exact herbs with prompts disabled");
        yield return Restore(game, ui, identity);
        pass("Herbal decline and later acceptance, five qualifying waves only, unrelated waves, travel, Retry and hidden prompts.");

        yield return Go(game, ui, Origin.Village);
        Click(ui, "HanVuTalk");
        Check(game.HeroData.originStory.stage == OriginStage.SearchForest &&
            StoryOrigin.ItemCount(game.HeroData, Origin.HerbId) == 0, "Medicine consumed once");
        yield return Go(game, ui, Origin.Forest);
        ui.SetPaused(false);
        Click(ui, "HanVuTalk");
        Check(Time.timeScale > 0f && game.HeroData.originStory.stage == OriginStage.SearchElite &&
            StoryOrigin.ItemCount(game.HeroData, Origin.TrailId) == 1 && Enemies(combat).Count > 0,
            "Trail interaction works during combat");
        ui.SetPaused(true);
        yield return Go(game, ui, Origin.EliteMap);
        for (int i = 0; i < 4; i++) yield return ClearWave(game, ui, combat, hero, space, false);
        Check(!StoryOrigin.CreditWave(game.HeroData, false, out _) &&
            StoryOrigin.ItemCount(game.HeroData, Origin.ClueId) == 0, "Ordinary wolf cannot grant elite clue");
        yield return Restore(game, ui, identity);
        yield return ClearWave(game, ui, combat, hero, space, true);
        data = game.HeroData;
        journal = StoryOrigin.Journal(data);
        Check(data.originStory.stage == OriginStage.Complete && StoryOrigin.ItemCount(data, Origin.ClueId) == 1 &&
            journal.Count(x => x.StartsWith("Vật chứng · Dây vải")) == 1 && journal.Any(x => x.StartsWith("Suy đoán")),
            "Exactly one authentic clue and separate inference");
        yield return Restore(game, ui, identity);
        yield return Go(game, ui, Origin.Forest);
        yield return Go(game, ui, Origin.EliteMap);
        Check(StoryOrigin.ItemCount(game.HeroData, Origin.ClueId) == 1 && !StoryOrigin.IsEliteWave(game.HeroData),
            "No elite clue replay on return");
        pass("Real accepted story Elite death awards one clue. Ordinary kills, duplicate callbacks and revisits cannot replay it.");

        WriteOldSave(game, saves, true, 2);
        yield return Ready(game, ui);
        Check(Identity(game.HeroData) == identity && game.HeroData.originStory.version == StoryOrigin.SaveVersion &&
            game.HeroData.originStory.herbalParticipation == HerbalParticipation.Accepted &&
            game.HeroData.originStory.herbWaves == 2 && game.HeroData.worldMapId == Origin.EliteMap,
            "Version 1 progressed snapshot migrates without changing names, location or count");
        WriteOldSave(game, saves, true, 0);
        yield return Ready(game, ui);
        Check(Identity(game.HeroData) == identity &&
            game.HeroData.originStory.herbalParticipation == HerbalParticipation.Offered,
            "Version 1 untouched quest becomes an offer");
        WriteOldSave(game, saves, false, 0);
        yield return Ready(game, ui);
        Check(game.HeroData.entityName == "Tên Đã Lưu" && game.HeroData.race == RaceType.YeuThu &&
            StoryOrigin.Definition(game.HeroData) == null && game.HeroData.worldMapId == Origin.EliteMap,
            "Origin-less snapshot keeps old identity and map");
        pass("Version 1 Hàn Vụ migration preserves prior progress and identity. Origin-less snapshots remain compatible.");
    }
}
#endif
