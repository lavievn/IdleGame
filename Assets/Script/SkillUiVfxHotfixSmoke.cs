#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using TuTienCore;
using UnityEngine;
using static Development016Smoke;

internal static class SkillUiVfxHotfixSmoke
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Read<T>(object owner,string field)=>(T)owner.GetType().GetField(field,Fields).GetValue(owner);
    static Vector2 BaseCast(CombatManager combat,HeroController hero,EnvironmentManager space) =>
        (Vector2)typeof(CombatManager).GetMethod("CastVisualAnchor",Fields).Invoke(combat,new object[]{hero.heroRect,space});
    static RectTransform Panel(UIManager ui)=>Read<RectTransform>(ui,"skillPanel");
    static TextMeshProUGUI Text(UIManager ui,string name)=>Read<TextMeshProUGUI>(ui,name);
    static readonly string[] Ordered={WeaponSkillRegistry.Fireball,WeaponSkillRegistry.Thrust,WeaponSkillRegistry.Volley};

    static IEnumerator Open(UIManager ui)
    {
        yield return Wait(()=>Read<RectTransform>(ui,"martialButton")?.gameObject.activeInHierarchy==true,4,"Visible Võ học button");
        Click(ui,"WeaponSkillsButton");
        yield return null;
        Check(Panel(ui).gameObject.activeSelf,"Võ học opens from HUD");
    }

    static void AssertVisibleText(UIManager ui)
    {
        Canvas.ForceUpdateCanvases();
        foreach (string name in new[]{"skillDetailTitle","skillDescription","skillDamage","skillStats","skillRequirements"})
        {
            var text=Text(ui,name);
            text.ForceMeshUpdate();
            Check(!text.isTextOverflowing,"Skill detail stays visible: "+name+" at "+Screen.width);
        }
    }

    static IEnumerator PanelChecks(GameManager game,UIManager ui,Func<int,int,IEnumerator> resize,
        Func<string,IEnumerator> capture,Action<string> pass)
    {
        var progress=game.HeroData.combatSkills;
        yield return Open(ui);
        var icons=Panel(ui).Find("SkillLibrary").GetComponentsInChildren<SkillSlotIcon>();
        Check(icons.Length==3 && icons.All(i=>i.color.r<.7f),"All three library icons visible and gray without gear");
        var queue=Panel(ui).Find("SkillQueue");
        Check(queue.GetComponentsInChildren<SkillSlotIcon>().Length==5 &&
            Mathf.Abs(((RectTransform)queue.Find("SkillQueueSlot0")).rect.width-
                ((RectTransform)queue.Find("SkillQueueSlot0")).rect.height)<.01f,"Five square queue slots");
        Click(ui,"SkillQueueSlot4");
        Check(Text(ui,"skillDetailTitle").text=="Ô thi triển trống","Empty slot can be selected");
        for(int i=0;i<3;i++)
        {
            Click(ui,"SkillLibrarySlot"+i);
            Check(Text(ui,"skillDetailTitle").text==WeaponSkillRegistry.All[i].Name &&
                Text(ui,"skillDescription").color==Color.white &&
                Text(ui,"skillRequirements").text.Contains("<color=#FF8888>Yêu cầu: Trang bị") &&
                !Read<RectTransform>(ui,"skillAdd").gameObject.activeSelf,
                "Gray skill opens white details and red condition without granting ownership");
        }
        Check(progress.learned.Count==0 && progress.queue.Count==0,"Browsing cannot learn or queue");
        Click(ui,"SkillGrantDev");
        Click(ui,"SkillClose");
        for(int i=0;i<3;i++)
        {
            Equip(game,EquipmentFoundation.SkillSampleIds[i]);
            yield return Open(ui);
            Check(icons.Count(icon=>icon.color.r>.7f)==1,"Only equipped grant is colored");
            Click(ui,"SkillLibrarySlot"+i);
            Click(ui,"SkillAdd");
            Click(ui,"SkillClose");
        }

        progress.mana=0;
        yield return Open(ui);
        Click(ui,"SkillLibrarySlot2");
        Check(Text(ui,"skillRequirements").text.Contains("Trang bị cấp · Dùng được") &&
            Text(ui,"skillRequirements").text.Contains("<color=#FF8888>Khi thi triển: Có 5 Mana</color>"),
            "Mana warning is cast-only; Staff source stays usable");
        Click(ui,"SkillQueueSlot2");
        Click(ui,"SkillUp");
        Click(ui,"SkillUp");
        Click(ui,"SkillDown");
        Click(ui,"SkillUp");
        Check(progress.queue.SequenceEqual(Ordered),"Move controls change only queue order");
        Click(ui,"SkillQueueSlot2");
        Click(ui,"SkillRemove");
        Check(progress.queue.Count==2 && !progress.queue.Contains(WeaponSkillRegistry.Volley),"Remove selected queued item");
        Click(ui,"SkillClose");
        Equip(game,EquipmentFoundation.SkillSampleIds[1]);
        yield return Open(ui);
        Click(ui,"SkillLibrarySlot1");
        Click(ui,"SkillAdd");
        Check(progress.queue.SequenceEqual(Ordered) && progress.learned.Count==0,"Re-add without learning");

        // Owned transient fixture only: unknown IDs remain inspectable/removable
        // and a second page exercises UI capacity independently of the registry.
        progress.queue.Add("combat:fixture-unknown-a");
        progress.queue.Add("combat:fixture-unknown-b");
        Click(ui,"SkillQueueSlot4");
        Check(Text(ui,"skillDetailTitle").text=="Kỹ năng không rõ","Unknown skill ID safe");
        Click(ui,"SkillQueueNext");
        Check(Text(ui,"skillDetailTitle").text=="Ô thi triển trống","Extra page contains an empty slot");
        Click(ui,"SkillQueuePrev");
        Click(ui,"SkillQueueSlot4"); Click(ui,"SkillRemove");
        Click(ui,"SkillQueueSlot3"); Click(ui,"SkillRemove");
        Check(progress.queue.SequenceEqual(Ordered),"Unknown queued items can be removed");

        Click(ui,"SkillLibrarySlot2");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)})
        {
            yield return resize(size.x,size.y);
            AssertVisibleText(ui);
            var corners=new Vector3[4];
            Panel(ui).GetWorldCorners(corners);
            var min=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            var max=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            Check(min.x>=0 && min.y>=0 && max.x<=Screen.width && max.y<=Screen.height,"Panel fits "+size);
            yield return capture("vo-hoc-"+size.x);
        }
        Click(ui,"SkillClose");
        progress.mana=100;
        pass("Võ học: five square slots, three browseable skills, queue add/remove/reorder and pages, gray/unowned, cast-only Mana, four sizes.");
    }

    static Vector2 ScreenPoint(EnvironmentManager space,Vector2 local) =>
        RectTransformUtility.WorldToScreenPoint(null,space.BattleArea.TransformPoint(local));

    static IEnumerator Shot(GameManager game,UIManager ui,CombatManager combat,HeroController hero,
        EnvironmentManager space,int family,bool skill,float facing,bool captureShot,Func<string,IEnumerator> capture)
    {
        var data=game.HeroData;
        var progress=data.combatSkills;
        var id=WeaponSkillRegistry.All[family].Id;
        if (skill) progress.Add(id); else progress.Remove(id);
        progress.cooldowns.Clear();
        progress.mana=100f;
        var target=Wave(game,combat,space,hero,new Vector2(facing*(family==0?35f:130f),0))[0];
        target.controller.attackMode=target.cycleMode=AttackMode.Melee;
        var fx=Read<BattleEffects>(combat,"effects");
        long arrows=combat.VolleyProjectiles, hits=combat.AcceptedHeroHitCount;
        ui.SetPaused(false);
        if (skill)
        {
            yield return Wait(()=>combat.CastingWeaponSkill==id,4,"Skill casting "+id);
            var cast=Read<IDictionary>(fx,"casts")[hero.heroRect];
            var shift=ScreenPoint(space,Read<Vector2>(cast,"center"))-ScreenPoint(space,BaseCast(combat,hero,space));
            Check(Mathf.Abs(shift.x)<.12f && Mathf.Abs(shift.y-(family==0?0:20))<.12f,
                "Projected casting lift "+id+" "+Screen.width+": "+shift);
            if(family==0)
            {
                yield return Wait(()=>combat.AcceptedHeroHitCount>hits,4,"Sword still hits");
                ui.SetPaused(true);
                yield break;
            }
        }
        yield return Wait(()=>combat.PendingProjectileCount==1,4,"Ranged release");
        ui.SetPaused(true);
        var projectile=Read<IList>(combat,"projectiles")[0];
        var start=Read<Vector2>(projectile,"start");
        var bolt=Read<BattleEffects.Bolt>(projectile,"visual");
        Check((bolt.position-start).sqrMagnitude<.01f && Read<float>(projectile,"elapsed")==0f,
            "Visual captured in actual creation frame");
        var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();
        Vector2 body=hero.heroRect.InverseTransformPoint(bridge.BodyGraphic.rectTransform.position);
        Vector2 muzzle=hero.heroRect.InverseTransformPoint(space.BattleArea.TransformPoint(start));
        var delta=muzzle-body;
        float y=delta.y/bridge.VisibleBodySize.y;
        float x=delta.x/bridge.VisibleBodySize.x;
        Check(Mathf.Abs(x-facing*(family==1?.24f:.18f))<.01f &&
            Mathf.Abs(y-(family==1?.48f:.51f))<.01f,"Mirrored chest/shoulder muzzle: "+delta);
        var flashes=Read<IList>(fx,"bowFlashes");
        if(family==1 && skill)
            Check(flashes.Count==1 && (Read<Vector2>(flashes[0],"origin")-start).sqrMagnitude<.01f,
                "Bow flash is centered at the first arrow origin");
        if(captureShot)
            yield return capture("projectile-"+(family==1?"cung":"truong")+(skill?"-skill":"-basic")+(facing<0?"-trai":"-phai"));
        var heroPoint=space.Position(hero.heroRect);
        space.SetPosition(hero.heroRect,heroPoint+new Vector2(12f,16f));
        Check(Read<Vector2>(projectile,"start")==start,"Live projectile does not follow the moving Hero");
        space.SetPosition(hero.heroRect,heroPoint);
        if(family==1 && skill && captureShot)
        {
            ui.SetPaused(false);
            yield return Wait(()=>combat.AcceptedHeroHitCount==hits+3,5,"Volley still deals three locked hits");
            ui.SetPaused(true);
            Check(combat.VolleyProjectiles==arrows+3,"Three arrows are still emitted");
        }
    }

    internal static IEnumerator Run(GameManager game,SaveManager saves,UIManager ui,CombatManager combat,
        HeroController hero,EnvironmentManager space,Func<int,int,IEnumerator> resize,Func<string,IEnumerator> capture,
        Action<string> pass,bool expectWindowsQuit)
    {
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        UnityEngine.Random.InitState(1706);
        game.OnNewGameClicked(); game.OnConfirmResetClicked();
        yield return Ready(game,ui);
        yield return PanelChecks(game,ui,resize,capture,pass);
        Check(game.TryManualSave(SaveSlot.ManualSave1) && game.LoadAndContinue(SaveSlot.ManualSave1),"Owned UI save/load");
        yield return Ready(game,ui);
        Check(game.HeroData.combatSkills.queue.SequenceEqual(Ordered),"Queue order survives load");
        yield return Open(ui);
        Click(ui,"SkillQueueSlot0");
        Check(Text(ui,"skillRequirements").text.Contains("Không khả dụng"),"Unequipped queued Staff remains gray after load");
        Click(ui,"SkillClose");
        pass("Queue and unavailable slot survive Save/Load; UI grants no learned skill.");

        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647)})
        {
            yield return resize(size.x,size.y);
            for(int family=0;family<3;family++)
            {
                var sample=EquipmentFoundation.SkillSampleIds[family];
                if(WeaponBasicAttack.Equipped(game.HeroData)?.templateId!=sample) Equip(game,sample);
                yield return Shot(game,ui,combat,hero,space,family,true,-1,size.x==800,capture);
            }
        }
        yield return resize(800,450);
        Equip(game,EquipmentFoundation.SkillSampleIds[1]);
        yield return Shot(game,ui,combat,hero,space,1,false,-1,true,capture);
        yield return Shot(game,ui,combat,hero,space,1,false,1,true,capture);
        Equip(game,EquipmentFoundation.SkillSampleIds[2]);
        yield return Shot(game,ui,combat,hero,space,2,false,-1,true,capture);
        yield return Shot(game,ui,combat,hero,space,2,false,1,true,capture);
        yield return Shot(game,ui,combat,hero,space,2,true,1,true,capture);
        pass("Casting offset at 600/800/1150; creation-frame screenshots, chest-height mirrored projectiles, one Bow flash and three arrows.");

        Equip(game,EquipmentFoundation.SkillSampleIds[0]);
        var candidate=UnityEngine.Object.Instantiate(game.HeroData);
        candidate.combatSkills.queue.Clear();
        foreach(string skill in Ordered) candidate.combatSkills.Add(skill);
        candidate.combatSkills.cooldowns.Clear();
        candidate.combatSkills.health=-1;
        candidate.combatStatModifiers.Add(new CombatStatModifier {sourceId="owned:skill-vfx:gate",stat=CombatStatId.Attack,flat=120});
        var profile=saves.CultivationProfile;
        using (var binding=new CultivationGameplayBinding(candidate,profile,HeavenlyGateV13Fixtures.NearBottleneck(profile))) { }
        Check(saves.SaveGame(candidate,SaveSlot.ManualSave2),"Owned Gate fixture");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2),"Load Gate candidate");
        yield return Ready(game,ui);
        Check(game.RequestTribulation(),"Actual Gate starts");
        yield return Wait(()=>!game.HasTribulationLiability,50,"Gate outcome");
        Check(game.Cultivation.Current.Foundation.ActualRealmId=="realm:truc-co" &&
            game.Cultivation.Current.Reserve.Amount==7 && game.HeroData.combatSkills.queue.SequenceEqual(Ordered),
            "Real Gate victory preserves Qi and queue");
        if(expectWindowsQuit) Check(game.IsEnteringGameplay,"Normal Quit exercised during entry handoff");
        pass("Thiên Quan victory, reserve and queue intact. Windows quit is requested during the entry handoff.");
    }

    internal static IEnumerator Reopen(GameManager game,UIManager ui,Action<string> pass)
    {
        Check(game.LoadAndContinue(SaveSlot.AutoSave),"Open AutoSave in a separate Windows process");
        yield return Ready(game,ui);
        Check(game.Cultivation.Current.Foundation.ActualRealmId=="realm:truc-co" &&
            game.Cultivation.Current.Reserve.Amount==7 && game.HeroData.combatSkills.queue.SequenceEqual(Ordered),
            "Reopened AutoSave preserves realm, Qi and ordered skills");
        pass("Restart after ordinary quit loads the owned AutoSave with realm, reserve and ordered skills.");
    }
}
#endif
