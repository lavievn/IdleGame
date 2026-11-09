#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Reflection;
using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;
using static Development016Smoke;

internal static class CombatUiRefinementSmoke
{
    const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static T Read<T>(object o, string field) => (T)o.GetType().GetField(field, Fields).GetValue(o);
    static void Write(object o, string field, object value) => o.GetType().GetField(field, Fields).SetValue(o,value);
    static void Invoke(object o, string method, params object[] args) => o.GetType().GetMethod(method,Fields).Invoke(o,args);

    static IEnumerator Music(int expected, Action<string> pass, string label)
    {
        var music = MapBgmController.Instance;
        yield return Wait(() =>
        {
            var sources = music.GetComponents<AudioSource>();
            Check(sources.Length == 2, "Two reusable BGM channels");
            return Read<int>(music,"currentGroup")==expected && !Read<bool>(music,"fading");
        }, 5, label);
        var channels = music.GetComponents<AudioSource>();
        int active = Array.FindAll(channels, x => x.isPlaying).Length;
        Check(active == (expected == 0 ? 0 : 1), "BGM settles to one channel: " + label);
        pass(label + " settles with at most two channels, one steady track.");
    }

    static Vector2 PolygonPoint(RectTransform art, string id)
    {
        var shape = WorldMapPolygons.Find(id);
        var center = Vector2.zero;
        foreach (var vertex in shape.vertices) center += vertex;
        center /= shape.vertices.Count;
        return RectTransformUtility.WorldToScreenPoint(null, art.TransformPoint(new Vector3(
            art.rect.xMin+center.x*art.rect.width,art.rect.yMin+center.y*art.rect.height)));
    }

    static IEnumerator Map(GameManager game, UIManager ui, Func<string,IEnumerator> capture, Action<string> pass)
    {
        Click(ui,"WorldMapButton");
        var map = Read<WorldMapPanelUI>(ui,"worldMapUI");
        var art = Read<RectTransform>(map,"artRect");
        var view = Read<RectTransform>(map,"viewport");
        var travel = Read<RectTransform>(map,"travel");
        Check(map.IsOpen && !travel.gameObject.activeSelf, "Current-map HUD opens map; Travel hidden");
        string origin=game.HeroData.worldMapId;
        Check(WorldMapCatalog.TryGet(origin,out var node),"World node available");
        string next=node.Links.Find(id => WorldMapCatalog.TryGet(id,out var target) && !target.IsBoss &&
            WorldMapPolygons.Find(id)!=null && target.Terrain!=node.Terrain);
        if (next == null) next=node.Links.Find(id => WorldMapCatalog.TryGet(id,out var target) &&
            !target.IsBoss && WorldMapPolygons.Find(id)!=null);
        Check(next!=null,"Fixture has an authored neighboring polygon");
        game.HeroData.worldMapCleared.Add(origin); // Unlock only in the owned snapshot.
        map.Refresh();
        yield return null;
        var point=RectTransformUtility.WorldToScreenPoint(null, view.position);
        float before=art.localScale.x;
        map.HandleScroll(point,2f);
        Check(art.localScale.x>before+.5f && art.localScale.x<=2.5f,"Wheel zoom sensitivity and max");
        var pan=art.anchoredPosition;
        string selection=Read<string>(map,"selectedMapId");
        Check(ui.TryBeginWorldMapPointer(point),"Map captures pointer away from controls");
        ui.DragWorldMapPointer(point+new Vector2(95f,30f));
        ui.EndWorldMapPointer(point+new Vector2(95f,30f));
        Check(art.anchoredPosition!=pan && Read<string>(map,"selectedMapId")==selection &&
            game.HeroData.worldMapId==origin,"Drag pans without selection or travel");
        map.HandleScroll(point,-30);
        Check(art.localScale.x==1f,"Zoom minimum");
        Canvas.ForceUpdateCanvases();
        var polygon=PolygonPoint(art,next);
        ui.HandlePointerClick(polygon,polygon);
        Check(Read<string>(map,"selectedMapId")==next && travel.gameObject.activeInHierarchy &&
            game.HeroData.worldMapId==origin,"Short click selects eligible polygon without travelling");
        var outline=Read<WorldMapOutlineGraphic>(map,"outlines");
        using (var mesh=new VertexHelper())
        {
            outline.GetType().GetMethod("OnPopulateMesh",Fields,null,new[]{typeof(VertexHelper)},null).Invoke(outline,new object[]{mesh});
            int selected=mesh.currentVertCount;
            outline.Refresh(game.HeroData,"");
            outline.GetType().GetMethod("OnPopulateMesh",Fields,null,new[]{typeof(VertexHelper)},null).Invoke(outline,new object[]{mesh});
            Check(mesh.currentVertCount<selected,"Only selected polygon adds glow strokes");
            map.Refresh();
        }
        yield return capture("refinement-map");
        Click(ui,"WorldMapTravelSelected");
        yield return Ready(game,ui);
        Check(game.HeroData.worldMapId==next && !map.IsOpen,"Explicit Travel follows existing world progression");
        pass("World map HUD entry, polygon glow, conditional Travel, wheel zoom, captured left drag, and real Travel.");
    }

    static System.Collections.Generic.List<CombatManager.ActiveMonsterInfo> QuietWave(GameManager game,
        CombatManager combat, EnvironmentManager space, HeroController hero, params Vector2[] positions)
    {
        var wave=Wave(game,combat,space,hero,positions);
        foreach(var enemy in wave) enemy.controller.attackMode=enemy.cycleMode=AttackMode.Melee;
        return wave;
    }

    static IEnumerator Gate(GameManager game, SaveManager saves, UIManager ui, CombatManager combat, Action<string> pass)
    {
        if (!game.HeroData.equipmentItems.Exists(item => item.templateId == EquipmentFoundation.SkillSampleIds[0]))
            Check(game.GrantWeaponSkillSamples(),"Retry fixture receives sample equipment");
        if (WeaponBasicAttack.Equipped(game.HeroData)?.templateId != EquipmentFoundation.SkillSampleIds[0])
            Equip(game,EquipmentFoundation.SkillSampleIds[0]);
        var candidate=UnityEngine.Object.Instantiate(game.HeroData);
        candidate.combatSkills.queue.Clear();
        candidate.combatSkills.nextSkillId="";
        candidate.combatSkills.Add(WeaponSkillRegistry.Thrust);
        candidate.combatSkills.cooldowns.Clear();
        candidate.combatSkills.health=-1;
        candidate.combatStatModifiers.Add(new CombatStatModifier {sourceId="owned:refinement:gate",stat=CombatStatId.Attack,flat=100});
        var profile=saves.CultivationProfile;
        using (var binding=new CultivationGameplayBinding(candidate,profile,HeavenlyGateV13Fixtures.Session(profile,180,7))) { }
        Check(saves.SaveGame(candidate,SaveSlot.ManualSave2),"Owned gate fixture Save");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2),"Load gate candidate");
        yield return Ready(game,ui);
        Check(game.RequestTribulation(),"Real Heavenly Gate enters");
        yield return Music(-3,pass,"Map → Heavenly Gate");
        var trial=Read<AudioSource>(MapBgmController.Instance,"trialSource");
        Check(trial!=null && trial.loop && trial.clip!=null,"Looping trial theme");
        var clip=trial.clip;
        ui.SetPaused(true);
        yield return new WaitForSecondsRealtime(.2f);
        Check(trial.clip==clip,"Pause keeps the chosen trial theme");
        ui.SetPaused(false);
        yield return Wait(()=>!game.HasTribulationLiability,45,"Gate settles");
        Check(game.Cultivation.Current.Foundation.ActualRealmId=="realm:truc-co" &&
            game.Cultivation.Current.Reserve.Amount==7,"Gate settles to Trúc Cơ with reserve");
        yield return Music(MapBgmController.GroupForRegion(game.HeroData.regionTheme),pass,"Heavenly Gate → map");
    }

    internal static IEnumerator Run(GameManager game, SaveManager saves, UIManager ui, CombatManager combat,
        HeroController hero, EnvironmentManager space, Func<int,int,IEnumerator> resize, Func<string,IEnumerator> capture, Action<string> pass)
    {
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        UnityEngine.Random.InitState(15015);
        yield return Music(-1,pass,"Title");
        game.OnNewGameClicked(); game.OnConfirmResetClicked();
        yield return Ready(game,ui);
        yield return Music(MapBgmController.GroupForRegion(game.HeroData.regionTheme),pass,"Title → New Game");
        Check(game.ReturnToMainMenu(),"Return to Title");
        yield return Music(-1,pass,"Game → Title");
        game.OnContinueClicked(); game.OnConfirmResetClicked();
        yield return Ready(game,ui);
        yield return Music(MapBgmController.GroupForRegion(game.HeroData.regionTheme),pass,"Title → Continue");
        yield return Map(game,ui,capture,pass);
        yield return Music(MapBgmController.GroupForRegion(game.HeroData.regionTheme),pass,"Map → destination");

        Check(game.GrantWeaponSkillSamples(),"Fixture receives real sample equipment");
        Equip(game,EquipmentFoundation.SkillSampleIds[2]);
        var state=combat.CurrentHeroCombatState;
        var progress=game.HeroData.combatSkills;
        progress.Add(WeaponSkillRegistry.Fireball);
        progress.mana=100f;
        Clear(game,combat);
        long releases=combat.WeaponSkillReleases;
        ui.SetPaused(false);
        yield return new WaitForSeconds(1.05f);
        Check(combat.CastingWeaponSkill==null && combat.WeaponSkillReleases==releases,"Exploration cannot start a cast");
        ui.SetPaused(true);
        var enemies=QuietWave(game,combat,space,hero,new Vector2(-150f,0f));
        var effects=Read<BattleEffects>(combat,"effects");
        var sfx=combat.GetComponent<HeroCombatSfx>();
        int heroSounds=sfx.StartedCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.CastingWeaponSkill!=null,4,"Hero starts real windup");
        yield return new WaitForSeconds(.4f);
        Check(combat.WeaponSkillReleases==releases && progress.mana==100f && effects.ActiveCasts==1 &&
            progress.Remaining(WeaponSkillRegistry.Fireball)==0f && combat.PendingProjectileCount==0,
            $"Casting only: releases={combat.WeaponSkillReleases-releases} mana={progress.mana} casts={effects.ActiveCasts} cooldown={progress.Remaining(WeaponSkillRegistry.Fireball)} projectiles={combat.PendingProjectileCount}");
        yield return capture("refinement-casting");
        space.SetPosition(enemies[0].controller.Rect,space.Position(hero.heroRect)-new Vector2(1100f,0f));
        yield return Wait(()=>combat.CastingWeaponSkill==null,2,"Lost target cancels cast");
        Check(combat.WeaponSkillReleases==releases && progress.mana==100f && effects.ActiveCasts==0 &&
            progress.Remaining(WeaponSkillRegistry.Fireball)==0f,"Cancellation clears VFX without charging");
        ui.SetPaused(true);
        QuietWave(game,combat,space,hero,new Vector2(-42f,0f));
        ui.SetPaused(false);
        yield return Wait(()=>combat.CastingWeaponSkill!=null,4,"Next opponent starts fresh windup");
        float started=Time.time;
        yield return Wait(()=>combat.WeaponSkillReleases>releases,4,"Fireball release");
        ui.SetPaused(true);
        Check(Time.time-started>=.99f && combat.WeaponSkillReleases==releases+1 &&
            Mathf.Abs(progress.mana-95f)<.03f && progress.Remaining(WeaponSkillRegistry.Fireball)>4.99f &&
            combat.PendingProjectileCount==1 && effects.ActiveCasts==0 && sfx.StartedCount>heroSounds,
            "Full cast before one release, with Mana, cooldown and SFX before impact");
        pass("Hero Fireball exploration, cancel, restart windup, one release, immediate cooldown, and actual Mana/SFX/VFX.");

        var timer=progress.cooldowns.Find(t=>t.id==WeaponSkillRegistry.Fireball);
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)})
        {
            yield return resize(size.x,size.y);
            Check(Screen.width==size.x && Screen.height==size.y,"Actual requested HUD resolution");
            var root=Read<RectTransform>(ui,"hudCanvas");
            var icon=Array.Find(root.GetComponentsInChildren<RectTransform>(true),x=>x.name=="WeaponSkillCooldown");
            Check(icon!=null && icon.gameObject.activeInHierarchy && Mathf.Abs(icon.rect.width-icon.rect.height)<.01f,
                "Visible square under Mana");
            var label=icon.Find("WeaponSkillLabel").GetComponent<TextMeshProUGUI>();
            label.ForceMeshUpdate();
            Check(label.text=="Trượng" && !label.isTextTruncated,"Full Staff label fits the square");
            var fill=(RectTransform)icon.Find("CooldownFill");
            for(int i=0;i<=4;i++)
            {
                timer.seconds=5f*(1f-i/4f);
                Invoke(ui,"TickHudW3");
                Check(Mathf.Abs(fill.anchorMax.y-i/4f)<.001f && fill.anchorMin.y==0f,
                    "Cooldown fill goes up from bottom");
            }
            timer.seconds=2.5f;
            Invoke(ui,"TickHudW3");
            yield return capture("refinement-cooldown-"+size.x);
        }
        Check(game.TryManualSave(SaveSlot.ManualSave1) && game.LoadAndContinue(SaveSlot.ManualSave1) &&
            game.HeroData.combatSkills.Remaining(WeaponSkillRegistry.Fireball)==2.5f,
            "Save/Load keeps cooldown precisely");
        yield return Ready(game,ui);
        yield return resize(800,450);
        progress=game.HeroData.combatSkills;
        Check(progress.Remove(WeaponSkillRegistry.Fireball),"Fixture removes Staff skill");
        enemies=QuietWave(game,combat,space,hero,new Vector2(-42f,0f),new Vector2(-60f,0f));
        long hits=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.PendingProjectileCount==1,4,"Staff basic ember");
        var shot=Read<IList>(combat,"projectiles")[0];
        var bolt=Read<BattleEffects.Bolt>(shot,"visual");
        Check(bolt.staffEmber && bolt.trailLength>0f && Read<float>(shot,"duration")>.3f &&
            Mathf.Abs(Read<float>(shot,"arc"))>0f && CombatManager.StaffEmberProgress(.5f,2f)==.25f,
            "Staff ember visual, curved ease-in and slower flight");
        yield return Wait(()=>combat.AcceptedHeroHitCount>hits,5,"Staff basic impact");
        ui.SetPaused(true);
        Check(combat.AcceptedHeroHitCount==hits+1,"Staff ember is single target");
        Equip(game,EquipmentFoundation.SkillSampleIds[0]);
        progress.Add(WeaponSkillRegistry.Thrust);
        foreach(float direction in new[]{-1f,1f})
        {
            progress.cooldowns.Clear();
            QuietWave(game,combat,space,hero,new Vector2(direction*32f,0f));
            releases=combat.WeaponSkillReleases;
            hits=combat.AcceptedHeroHitCount;
            ui.SetPaused(false);
            yield return Wait(()=>combat.WeaponSkillReleases>releases,4,"Thrust release");
            ui.SetPaused(true);
            var thrusts=Read<IList>(effects,"thrusts");
            Check(thrusts.Count==1 && Read<float>(thrusts[0],"direction")==direction &&
                Read<float>(thrusts[0],"width")==WeaponSkillRegistry.Find(WeaponSkillRegistry.Thrust).Width &&
                Read<float>(thrusts[0],"height")==WeaponSkillRegistry.Find(WeaponSkillRegistry.Thrust).Height &&
                combat.AcceptedHeroHitCount==hits+1,"Thrust visual and damage share both directions and dimensions");
            yield return capture(direction<0?"refinement-thrust-left":"refinement-thrust-right");
        }
        pass("Cooldown fill 0–100% at four sizes, Load preserves 2.5s, Staff ember is single-target, thrust mirrors with the same bounds.");

        yield return Monsters(game,ui,combat,hero,space,sfx,effects,pass);
        Click(ui,"InventoryButton");
        var inventory=Read<InventoryPanelUI>(ui,"inventoryUI");
        Check(inventory.IsOpen,"Inventory stays reachable");
        yield return new WaitForSecondsRealtime(.4f);
        Invoke(inventory,"SelectEquipped",EquipmentSlot.Weapon);
        Click(ui,"InventoryForge");
        Check(inventory.IsForgeOpen,"Forge still opens for equipped item");
        Click(ui,"Forge_Close");
        Click(ui,"InventoryClose");
        yield return new WaitForSecondsRealtime(.4f);
        Click(ui,"StatsButton");
        Check(Read<GameObject>(ui,"damagePanel").activeSelf,"Character panel still opens");
        ui.CloseDamageInfo();
        Invoke(combat,"ApplyIncomingDamage",1000000,"Owned Retry",null,null);
        Check(combat.CurrentHeroHP==0,"Defeated Hero");
        game.OnRetryClicked();
        yield return Ready(game,ui);
        Check(combat.CurrentHeroHP==combat.MaxHeroHP && effects.ActiveCasts==0,"Retry rebinds and clears old cast");
        pass("Inventory, Forge, Character and Retry smoke.");
        yield return Gate(game,saves,ui,combat,pass);
    }

    static IEnumerator Monsters(GameManager game, UIManager ui, CombatManager combat, HeroController hero,
        EnvironmentManager space, HeroCombatSfx sfx, BattleEffects effects, Action<string> pass)
    {
        var info=QuietWave(game,combat,space,hero,new Vector2(-40f,0f))[0];
        var monster=info.controller;
        monster.GetType().GetProperty("Awareness",Fields).SetValue(monster,MonsterAwareness.Unaware);
        space.SetPosition(monster.Rect,Vector2.zero);
        monster.TickMovement(space,.2f);
        Check(Mathf.Abs(space.Position(monster.Rect).magnitude-10f)<.03f,"Wander uses 50 units/sec");
        space.SetPosition(monster.Rect,new Vector2(space.BattleArea.rect.xMax,0f));
        monster.TickMovement(space,.2f);
        Check(space.Position(monster.Rect).x<space.BattleArea.rect.xMax-.1f,"Wander turns inward at the edge");
        monster.attackMode=AttackMode.Melee;
        monster.moveSpeed=100f;
        var p=space.Position(hero.heroRect)-new Vector2(260f,0f);
        space.SetPosition(monster.Rect,p);
        monster.ReceiveAlert(hero,true);
        monster.TickMovement(space,.2f);
        Check(Mathf.Abs(space.Position(monster.Rect).x-p.x-20f)<.04f,"Combat pursuit uses 100 units/sec");
        space.SetPosition(monster.Rect,space.Position(hero.heroRect)-new Vector2(35f,0f));
        info.attackTimer=0f;
        var actor=combat.CurrentHeroCombatState;
        actor.AttackTimer=60f;
        int hp=actor.CurrentHP, sounds=sfx.MonsterStartedCount;
        ui.SetPaused(false);
        yield return Wait(()=>actor.CurrentHP<hp,4,"Monster Basic");
        ui.SetPaused(true);
        Check(sfx.MonsterStartedCount>sounds && sfx.ActiveVoiceCount<=7,"Monster Basic uses capped combat SFX");
        for(int i=0;i<2;i++)
        {
            info=QuietWave(game,combat,space,hero,new Vector2(-70f,0f))[0];
            monster=info.controller;
            monster.attackMode=info.cycleMode=AttackMode.RangedMagic;
            info.attackTimer=0f;
            Write(monster,"combatSkillIds",new[]{WeaponSkillRegistry.Fireball});
            Write(monster,"combatSkillMana",10f);
            actor.AttackTimer=60f;
            long released=combat.MonsterSkillReleases;
            ui.SetPaused(false);
            yield return Wait(()=>info.skillCast!=null,5,"Monster active cast");
            float began=Time.time;
            yield return new WaitForSeconds(.42f);
            Check(combat.MonsterSkillReleases==released && effects.ActiveCasts==1 &&
                info.skillProgress.mana==10f && info.skillProgress.cooldowns.Count==0,
                "Monster cast shares timing, cost and VFX");
            if(i==0)
            {
                monster.MarkDead();
                yield return null;
                Check(combat.MonsterSkillReleases==released && effects.ActiveCasts==0,"Monster death cancels windup");
            }
            else
            {
                yield return Wait(()=>combat.MonsterSkillReleases>released,5,"Monster Fireball release");
                Check(Time.time-began>=.99f && combat.MonsterSkillReleases==released+1 &&
                    effects.ActiveCasts==0 && info.skillProgress.mana==5f &&
                    info.skillProgress.Remaining(WeaponSkillRegistry.Fireball)>4.99f,"Monster releases once after 1s");
                hp=actor.CurrentHP;
                yield return Wait(()=>actor.CurrentHP<hp,5,"Monster Fireball accepted impact");
            }
            ui.SetPaused(true);
            Write(monster,"combatSkillIds",Array.Empty<string>());
            Write(monster,"combatSkillMana",0f);
        }
        pass("Monster wandering and pursuit, Basic and capped SFX, active Fireball, death cancel and accepted damage.");
    }
}
#endif
