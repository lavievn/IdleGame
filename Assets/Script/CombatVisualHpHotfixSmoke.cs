#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TuTienCore;
using UnityEngine;
using static Development016Smoke;

internal static class CombatVisualHpHotfixSmoke
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static T Read<T>(object owner,string name) => (T)owner.GetType().GetField(name,Fields).GetValue(owner);
    static T Call<T>(object owner,string name,params object[] args) =>
        (T)owner.GetType().GetMethod(name,Fields).Invoke(owner,args);
    static void Set(object owner,string name,object value) => owner.GetType().GetField(name,Fields).SetValue(owner,value);
    static IList Sparks(BattleEffects fx) => Read<IList>(fx,"sparks");
    static IList Flashes(BattleEffects fx) => Read<IList>(fx,"bowFlashes");

    static System.Collections.Generic.List<CombatManager.ActiveMonsterInfo> QuietWave(GameManager game,
        CombatManager combat,EnvironmentManager space,HeroController hero,params Vector2[] positions)
    {
        var enemies=Wave(game,combat,space,hero,positions);
        foreach (var enemy in enemies)
            enemy.controller.attackMode=enemy.cycleMode=AttackMode.Melee;
        return enemies;
    }

    static Vector2 ScreenPoint(EnvironmentManager space,Vector2 battlePoint) =>
        RectTransformUtility.WorldToScreenPoint(null,space.BattleArea.TransformPoint(battlePoint));

    static void CheckOffset(EnvironmentManager space,Vector2 origin,Vector2 actual,Vector2 expected,string label)
    {
        var offset=ScreenPoint(space,actual)-ScreenPoint(space,origin);
        Check((offset-expected).sqrMagnitude<.01f,label + " projected delta=" + offset);
    }

    static Vector2 Body(CombatManager combat,RectTransform actor,EnvironmentManager space) =>
        Call<Vector2>(combat,"BodyPosition",actor,space);

    static void CheckHit(CombatManager combat,EnvironmentManager space,BattleEffects fx,RectTransform target,int count)
    {
        var sparks=Sparks(fx);
        Check(sparks.Count==count*9,"One Bow-style burst (nine sparks) per accepted hit, count="+sparks.Count);
        var first=Read<Vector2>(sparks[0],"position");
        CheckOffset(space,Body(combat,target,space),first,new Vector2(0,20),"Accepted impact offset");
        Check(!Read<bool>(sparks[0],"fire"),"Uses the common Bow burst");
    }

    static IEnumerator TestHealth(GameManager game,SaveManager saves,UIManager ui,CombatManager combat,
        HeroController hero,EnvironmentManager space,Action<string> pass)
    {
        var state=combat.CurrentHeroCombatState;
        Check(game.HeroData.originHealth==1000 && state.CurrentHP==1000 && state.MaxHP==1000,
            "New Hero starts with 1000 HP and MaxHP");
        Check(game.GrantWeaponSkillSamples(),"Owned profile receives samples");
        var samples=EquipmentFoundation.SkillSampleIds;
        state.CurrentHP=600;
        Equip(game,samples[0]);
        Equip(game,samples[1]);
        Check(state.CurrentHP==600 && state.MaxHP==1000,"Bow swap");
        Equip(game,samples[2]);
        Check(state.CurrentHP==600 && state.MaxHP==1000,"Staff swap");
        Check(game.UnequipGear(EquipmentSlot.Weapon) && state.CurrentHP==600 && state.MaxHP==1000,"Unarmed swap");
        Equip(game,samples[0]);
        Check(state.CurrentHP==600 && state.MaxHP==1000,"Sword swap");
        game.HeroData.AddExp(game.HeroData.expToNextLevel);
        combat.RefreshRegisteredHeroStats(hero);
        int max=1000+16+game.HeroData.meleeGrowthBonus;
        Check(game.HeroData.currentLevel==2 && state.MaxHP==max && max>1016 && state.CurrentHP==600,"Level HP pool");
        Equip(game,samples[1]);
        Check(state.MaxHP==max && state.CurrentHP==600,"Level HP applies to all families");
        Equip(game,samples[0]);
        var charm=EquipmentFoundation.CreateCommon("base:artifact",1,125);
        Check(EquipmentRuntime.AddInstance(game.HeroData,charm),"Owned HP charm");
        Check(EquipmentStatPreview.TryEquip(game.HeroData,charm.instanceId,EquipmentSlot.Artifact,
            hero.attackMode,out var preview) && preview.Find(CombatStatId.MaxHealth).preview==max+125,
            "HP preview agrees with runtime");
        Check(game.EquipGear(charm.instanceId,EquipmentSlot.Artifact) && state.MaxHP==max+125 &&
            state.CurrentHP==600,"HP charm increases only the cap");
        state.CurrentHP=max+50;
        Check(game.UnequipGear(EquipmentSlot.Artifact) && state.MaxHP==max && state.CurrentHP==max,"Unequip clamps HP");
        Check(game.EquipGear(charm.instanceId,EquipmentSlot.Artifact) && state.CurrentHP==max,"Re-equip cannot heal");
        state.CurrentHP=615;
        Check(game.TryManualSave(SaveSlot.ManualSave1) && game.LoadAndContinue(SaveSlot.ManualSave1),
            "Save/Load with HP modifier");
        Check(state.CurrentHP==615 && state.MaxHP==max+125,"Load retains absolute HP");
        yield return Ready(game,ui);
        var legacy=UnityEngine.Object.Instantiate(game.HeroData);
        legacy.originHealth=400;
        legacy.ApplyHeroBalance();
        int oldMax=400+16+legacy.meleeGrowthBonus+125;
        Check(legacy.GetCalculatedHealth(AttackMode.Melee)==oldMax &&
            legacy.GetCalculatedHealth(AttackMode.RangedMagic)==oldMax && legacy.combatSkills.health==615,
            "Legacy creation HP and stored current HP stay intact");
        legacy.combatSkills.health=oldMax+100;
        Check(saves.SaveGame(legacy,SaveSlot.ManualSave2),"Save owned legacy cap fixture");
        UnityEngine.Object.Destroy(legacy);
        var path=Path.Combine(saves.SaveDirectory,"ManualSave2.json");
        string stored=File.ReadAllText(path);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2) && state.CurrentHP==oldMax && state.MaxHP==oldMax &&
            game.FullEventHistory.Contains("HP trong bản lưu") && File.ReadAllText(path)==stored,
            "Over-cap legacy HP is explained and clamped in memory; the source file is unchanged");
        yield return Ready(game,ui);
        Check(game.LoadAndContinue(SaveSlot.ManualSave1),"Restore new Hero fixture");
        yield return Ready(game,ui);
        pass("New HP 1000, four families, level growth, real HP gear, clamp/re-equip, absolute Save/Load and explained legacy clamp.");
    }

    internal static IEnumerator Run(GameManager game,SaveManager saves,UIManager ui,CombatManager combat,
        HeroController hero,EnvironmentManager space,Func<int,int,IEnumerator> resize,Func<string,IEnumerator> capture,Action<string> pass)
    {
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        UnityEngine.Random.InitState(905);
        game.OnNewGameClicked();
        game.OnConfirmResetClicked();
        yield return Ready(game,ui);
        yield return TestHealth(game,saves,ui,combat,hero,space,pass);
        var state=combat.CurrentHeroCombatState;
        var progress=game.HeroData.combatSkills;
        var fx=Read<BattleEffects>(combat,"effects");
        Clear(game,combat);
        state.CurrentHP=500;
        ui.SetPaused(false);
        yield return new WaitForSeconds(.45f);
        ui.SetPaused(true);
        Check(state.CurrentHP>=504 && state.CurrentHP<=506,"Exploration naturally regenerates near 10 HP/s");
        int hp=state.CurrentHP;
        yield return new WaitForSecondsRealtime(.25f);
        Check(state.CurrentHP==hp,"Pause gives no HP");

        var enemies=QuietWave(game,combat,space,hero,new Vector2(-470,0));
        var before=space.Position(hero.heroRect);
        var separation=Mathf.Abs(space.Position(enemies[0].controller.Rect).x-before.x);
        long released=combat.HeroReleaseCount;
        ui.SetPaused(false);
        yield return new WaitForSeconds(.30f);
        ui.SetPaused(true);
        Check(state.CurrentHP>=hp+2 && combat.HeroReleaseCount==released &&
            Mathf.Abs(space.Position(enemies[0].controller.Rect).x-space.Position(hero.heroRect).x)<separation,
            "Moving toward an alerted enemy still heals");
        var info=QuietWave(game,combat,space,hero,new Vector2(-180,0))[0];
        info.controller.attackMode=info.cycleMode=AttackMode.RangedMagic;
        info.attackTimer=0f;
        state.AttackTimer=30f;
        hp=state.CurrentHP;
        ui.SetPaused(false);
        yield return Wait(()=>info.attackTimer>.20f,3,"Incoming Monster begins a real windup");
        ui.SetPaused(true);
        Check(state.CurrentHP==hp && combat.PendingProjectileCount==0,"Incoming windup blocks HP before damage");
        Check(state.EngagedOpponents.Count==1,"Only actual attackers join engagement");
        space.SetPosition(info.controller.Rect,space.Position(hero.heroRect)-new Vector2(1600,0));
        ui.SetPaused(false);
        yield return new WaitForSeconds(.14f);
        Check(state.CurrentHP==hp,"Short break in range cannot flicker regen");
        yield return Wait(()=>state.CurrentHP>hp,3,"Disengaging restores HP while a wave still exists");
        ui.SetPaused(true);
        pass("Exploration and approach heal; enemy windup stops healing before impact; grace and Pause work.");

        var basicSword=QuietWave(game,combat,space,hero,new Vector2(-42,0))[0].controller;
        long basicHits=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>basicHits,4,"Sword basic hits");
        ui.SetPaused(true);
        CheckHit(combat,space,fx,basicSword.Rect,1);

        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(1150,647)})
        {
            yield return resize(size.x,size.y);
            foreach(float direction in new[]{-1f,1f})
            {
                enemies=QuietWave(game,combat,space,hero,new Vector2(direction*42,0),
                    new Vector2(direction*62,35),new Vector2(direction*77,49),new Vector2(-direction*170,0));
                progress.Add(WeaponSkillRegistry.Thrust);
                progress.cooldowns.Clear();
                hp=state.CurrentHP;
                long hits=combat.AcceptedHeroHitCount;
                ui.SetPaused(false);
                yield return Wait(()=>combat.CastingWeaponSkill!=null,4,"Sword starts casting");
                yield return new WaitForSeconds(.18f);
                ui.SetPaused(true);
                var casts=Read<IDictionary>(fx,"casts");
                var cast=Read<Vector2>(casts[hero.heroRect],"center");
                CheckOffset(space,Body(combat,hero.heroRect,space),cast,new Vector2(-4,10),"Hero casting offset");
                Check(state.CurrentHP==hp && combat.AcceptedHeroHitCount==hits,"Hero cast blocks HP before damage");
                if(size.x==600 && direction<0) yield return capture("hotfix-casting");
                ui.SetPaused(false);
                yield return Wait(()=>combat.AcceptedHeroHitCount>hits,3,"Thrust damage");
                ui.SetPaused(true);
                var thrust=Read<IList>(fx,"thrusts")[0];
                var origin=Read<Vector2>(thrust,"origin");
                var skill=WeaponSkillRegistry.Find(WeaponSkillRegistry.Thrust);
                var first=enemies[0].controller.Rect;
                Check(Read<float>(thrust,"direction")==direction && Mathf.Abs(Read<float>(thrust,"width")-240f)<.001f &&
                    Mathf.Abs(Read<float>(thrust,"height")-84f)<.001f && origin.y >= Call<Vector2>(combat,"CastVisualAnchor",hero.heroRect,space).y &&
                    origin==Call<Vector2>(combat,"SkillTargetOrigin",skill,state,space) &&
                    combat.AcceptedHeroHitCount==hits+2 && enemies[2].currentHP==5000 && enemies[3].currentHP==5000 &&
                    casts.Count==0 && first!=null,
                    $"Thrust bounds: width={Read<float>(thrust,"width"):R} height={Read<float>(thrust,"height"):R} " +
                    $"dir={Read<float>(thrust,"direction")}/{direction} origin={origin}/{Call<Vector2>(combat,"SkillTargetOrigin",skill,state,space)} " +
                    $"castY={Call<Vector2>(combat,"CastVisualAnchor",hero.heroRect,space).y} " +
                    $"hits={combat.AcceptedHeroHitCount-hits} outside={enemies[2].currentHP}/{enemies[3].currentHP} casts={casts.Count}");
                CheckHit(combat,space,fx,first,2);
                if(size.x==600) yield return capture(direction<0?"hotfix-thrust-left":"hotfix-thrust-right");
            }
        }
        pass("Casting projects -4,+10 px at two resolutions; elevated thrust mirrors and hits each target once; impact is +20 px.");

        yield return resize(800,450);
        Equip(game,EquipmentFoundation.SkillSampleIds[1]);
        progress.Add(WeaponSkillRegistry.Volley);
        enemies=QuietWave(game,combat,space,hero,new Vector2(-120,0),new Vector2(-170,0));
        long volley=combat.VolleyProjectiles, accepted=combat.AcceptedHeroHitCount, skills=combat.WeaponSkillReleases;
        ui.SetPaused(false);
        yield return Wait(()=>combat.WeaponSkillReleases>skills,4,"Bow Volley releases");
        ui.SetPaused(true);
        Check(Flashes(fx).Count==1 && Read<float>(Flashes(fx)[0],"direction")==-1f &&
            combat.VolleyProjectiles==volley+1 && Sparks(fx).Count==0,"One forward Bow flash at release before any hit");
        yield return capture("hotfix-bow-release");
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>=accepted+3,5,"Three locked arrows land");
        ui.SetPaused(true);
        Check(combat.WeaponSkillReleases==skills+1 && combat.VolleyProjectiles==volley+3 &&
            combat.AcceptedHeroHitCount==accepted+3 && Flashes(fx).Count==0,"One flash, three projectiles and three hits");
        progress.queue.Clear();
        var basicBow=QuietWave(game,combat,space,hero,new Vector2(-120,0))[0].controller;
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>accepted+3,4,"Bow basic hits");
        ui.SetPaused(true);
        CheckHit(combat,space,fx,basicBow.Rect,1);
        var target=QuietWave(game,combat,space,hero,new Vector2(-220,0))[0].controller;
        ui.SetPaused(false);
        yield return Wait(()=>combat.PendingProjectileCount==1,3,"Bow basic projectile launches");
        state.AttackTimer=30;
        space.SetPosition(target.Rect,space.Position(hero.heroRect)-new Vector2(780,0));
        yield return Wait(()=>combat.PendingProjectileCount==0,4,"Bow basic misses old snapshot");
        ui.SetPaused(true);
        Check(Sparks(fx).Count==0 && combat.AcceptedHeroHitCount==accepted+4,"Miss has no hit VFX or damage");
        pass("Liên Hoàn Tiễn flashes only once and keeps three hits. Missed basic arrow produces no hit VFX.");

        Equip(game,EquipmentFoundation.SkillSampleIds[2]);
        enemies=QuietWave(game,combat,space,hero,new Vector2(-130,0),new Vector2(-190,0));
        accepted=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>accepted,4,"Staff basic single-target");
        ui.SetPaused(true);
        Check(combat.AcceptedHeroHitCount==accepted+1,"Staff basic accepted one hit");
        Check(Sparks(fx).Count==9,"Staff basic uses one common hit burst");
        progress.Add(WeaponSkillRegistry.Fireball);
        enemies=QuietWave(game,combat,space,hero,new Vector2(-110,0),new Vector2(-140,0),new Vector2(-510,0));
        accepted=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>accepted,6,"Staff AoE hits two");
        ui.SetPaused(true);
        Check(combat.AcceptedHeroHitCount==accepted+2 && Sparks(fx).Count==18 && enemies[2].currentHP==5000,
            "AoE has one common hit burst for each actual victim");

        progress.cooldowns.Clear();
        QuietWave(game,combat,space,hero,new Vector2(-170,0));
        accepted=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.PendingProjectileCount==1,5,"Fireball launches before weapon swap");
        ui.SetPaused(true);
        Equip(game,EquipmentFoundation.SkillSampleIds[0]);
        state.AttackTimer=60f;
        ui.SetPaused(false);
        yield return Wait(()=>combat.PendingProjectileCount==0 && combat.DetachedMagicVisualCount==0,5,"Cancelled fireball visual retires");
        ui.SetPaused(true);
        Check(Sparks(fx).Count==0 && combat.AcceptedHeroHitCount==accepted,"Cancelled Fireball leaves no hit burst");

        info=QuietWave(game,combat,space,hero,new Vector2(-35,0))[0];
        info.attackTimer=0;
        state.AttackTimer=60;
        hp=state.CurrentHP;
        ui.SetPaused(false);
        yield return Wait(()=>state.CurrentHP<hp,4,"Monster basic hits");
        ui.SetPaused(true);
        CheckHit(combat,space,fx,hero.heroRect,1);

        info=QuietWave(game,combat,space,hero,new Vector2(-170,0))[0];
        info.controller.attackMode=info.cycleMode=AttackMode.RangedMagic;
        Set(info.controller,"combatSkillIds",new[]{WeaponSkillRegistry.Fireball});
        Set(info.controller,"combatSkillMana",10f);
        info.attackTimer=0;
        state.AttackTimer=60f;
        ui.SetPaused(false);
        yield return Wait(()=>info.skillCast!=null,4,"Monster fixture casts");
        var monsterCast=Read<Vector2>(Read<IDictionary>(fx,"casts")[info.controller.Rect],"center");
        CheckOffset(space,Body(combat,info.controller.Rect,space),monsterCast,new Vector2(-4,10),"Monster casting offset");
        hp=state.CurrentHP;
        yield return Wait(()=>state.CurrentHP<hp,6,"Monster skill accepted");
        ui.SetPaused(true);
        CheckHit(combat,space,fx,hero.heroRect,1);
        Set(info.controller,"combatSkillIds",Array.Empty<string>());
        Set(info.controller,"combatSkillMana",0f);
        Check(game.UnequipGear(EquipmentSlot.Weapon),"Owned Unarmed");
        QuietWave(game,combat,space,hero,new Vector2(-35,0));
        accepted=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>accepted,4,"Unarmed hits");
        ui.SetPaused(true);
        Check(Sparks(fx).Count==9,"Unarmed uses shared hit effect");
        Check(combat.ApplyDamageToRegisteredHero(hero,1000000,"Owned Retry") && state.CurrentHP==0,"Defeated");
        game.OnRetryClicked();
        yield return Ready(game,ui);
        Check(state.CurrentHP==state.MaxHP && state.EngagedOpponents.Count==0 && state.HealthRegenRemainder==0f,
            "Retry rebinds HP and clears engagement");
        pass("All weapon basics, Staff AoE, Monster basic and skill share accepted hits; cancelled Fireball is silent; Retry resets HP and engagement.");

        if (!game.HeroData.equipmentItems.Exists(item=>item.templateId==EquipmentFoundation.SkillSampleIds[0]))
            Check(game.GrantWeaponSkillSamples(),"Grant owned Gate samples");
        if (WeaponBasicAttack.Equipped(game.HeroData)?.templateId!=EquipmentFoundation.SkillSampleIds[0])
            Equip(game,EquipmentFoundation.SkillSampleIds[0]);
        var candidate=UnityEngine.Object.Instantiate(game.HeroData);
        candidate.combatSkills.health=500;
        candidate.combatSkills.queue.Clear();
        candidate.combatSkills.cooldowns.Clear();
        candidate.combatSkills.Add(WeaponSkillRegistry.Thrust);
        candidate.combatStatModifiers.Add(new CombatStatModifier {sourceId="owned:hotfix:gate",stat=CombatStatId.Attack,flat=120});
        var profile=saves.CultivationProfile;
        using (var binding=new CultivationGameplayBinding(candidate,profile,HeavenlyGateV13Fixtures.Session(profile,180,7))) { }
        Check(saves.SaveGame(candidate,SaveSlot.ManualSave2),"Owned Gate fixture");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2),"Load Gate fixture");
        yield return Ready(game,ui);
        Check(game.RequestTribulation(),"Real Gate begins");
        yield return Wait(()=>combat.HeavenlyGateActive && !ui.IsPaused,15,"Gate runs");
        float deadline=Time.realtimeSinceStartup+45f;
        float observed=Time.time;
        hp=state.CurrentHP;
        while (game.HasTribulationLiability)
        {
            yield return null;
            Check(Time.realtimeSinceStartup<deadline,"Gate outcome");
            if (!combat.HeavenlyGateActive) continue;
            Check(state.CurrentHP<=hp,"No natural HP in Gate approach/combat");
            hp=state.CurrentHP;
        }
        Check(Time.time-observed>.5f && game.Cultivation.Current.Foundation.ActualRealmId=="realm:truc-co" &&
            game.Cultivation.Current.Reserve.Amount==7,"Gate completes with reserve and no in-trial healing");
        pass("Real Thiên Quan suppresses passive HP throughout and settles to Trúc Cơ with reserve.");
    }
}
#endif
