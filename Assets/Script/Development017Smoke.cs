#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using TuTienCore;
using UnityEngine;
using static Development016Smoke;

internal static class Development017Smoke
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Read<T>(object o,string field)=>(T)o.GetType().GetField(field,Fields).GetValue(o);
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Fields).SetValue(o,value);
    static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Fields).Invoke(o,args);
    static bool Near(float a,float b)=>Mathf.Abs(a-b)<.01f;
    static int Derived(EntityDataSO data)=>data.combatStatModifiers.Count(m=>m.sourceId!=null &&
        m.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,StringComparison.Ordinal));

    static IEnumerator OpenInventory(UIManager ui)
    {
        Click(ui,"InventoryButton");
        yield return new WaitForSecondsRealtime(.45f);
        Check(Read<InventoryPanelUI>(ui,"inventoryUI").IsOpen,"Open real Inventory");
    }

    static void Select(InventoryPanelUI ui,string id)
    {
        Set(ui,"selectedKey","gear:"+id);
        ui.Refresh();
    }

    static IEnumerator CloseInventory(UIManager ui)
    {
        Click(ui,"InventoryClose");
        yield return new WaitForSecondsRealtime(.45f);
    }

    static IEnumerator EquipFixtures(GameManager game,UIManager ui,CombatManager combat,
        HeroController hero,Action<string> pass)
    {
        var data=game.HeroData;
        var actor=combat.CurrentHeroCombatState;
        Check(actor.MaxHP==1000 && Near(actor.MaxMana,100),"Fresh caps");
        Check(game.GrantWeaponSkillSamples(),"Three DEV skill weapon instances");
        Check(!game.GrantWeaponSkillSamples(),"Weapon grants idempotent");
        for(int i=0;i<3;i++)
        {
            string template=EquipmentFoundation.SkillSampleIds[i];
            var item=data.equipmentItems.Single(x=>x.templateId==template);
            Equip(game,template);
            var skill=WeaponSkillRegistry.All[i];
            Check(item.rarity==EquipmentRarity.Hoang && item.mainStats.Count==1 && item.extraStats.Count==0 &&
                item.grantedSkillId==skill.Id && skill.IsAvailable(data,item) &&
                !data.combatSkills.queue.Contains(skill.Id) && data.combatSkills.learned.Count==0 &&
                actor.MaxHP==1000,"One item grant, no automatic queue or MaxHP from family");
        }
        actor.CurrentHP=616;
        data.combatSkills.mana=69;
        yield return OpenInventory(ui);
        var inventory=Read<InventoryPanelUI>(ui,"inventoryUI");
        Click(ui,"InventoryGrantCharms");
        Check(!game.GrantInventorySamples(),"DEV defensive and mana grants idempotent");
        var armor=data.equipmentItems.Single(x=>x.templateId==EquipmentFoundation.DefenseSample);
        var mana=data.equipmentItems.Single(x=>x.templateId==EquipmentFoundation.ManaSample);
        var damage=new[]{new CombatDamagePart {school=DamageSchool.Physical,fixedDamage=100f}};
        int incoming=CombatDamage.Resolve(null,data,AttackMode.Melee,damage).totalDamage;
        Select(inventory,armor.instanceId);
        Click(ui,"InventoryEquip");
        Check(CombatStats.Get(data,CombatStatId.Armor)==12 &&
            CombatDamage.Resolve(null,data,AttackMode.Melee,damage).totalDamage<incoming,"Chest mitigates real physical resolver");
        Select(inventory,mana.instanceId);
        Click(ui,"InventoryDetailCompare");
        var pages=Read<List<string>>(inventory,"detailPages");
        Check(pages.Any(page=>page.Contains("Mana tối đa") && page.Contains("125") && page.Contains("+25")),
            "Inventory renders Mana baseline and signed delta");
        Check(EquipmentStatPreview.TryEquip(data,mana.instanceId,EquipmentSlot.Artifact,hero.attackMode,out var preview) &&
            Near(preview.Find(CombatStatId.MaxMana).current,100) && Near(preview.Find(CombatStatId.MaxMana).preview,125) &&
            Near(preview.Find(CombatStatId.ManaRegeneration).current,2) && Near(preview.Find(CombatStatId.HealthRegeneration).current,10),
            "Preview uses canonical resource baselines");
        Click(ui,"InventoryEquip");
        Check(Near(actor.MaxMana,125) && Near(actor.CurrentMana,69) && actor.CurrentHP==616 && actor.MaxHP==1000,
            "Equip raises cap without healing or filling");
        actor.Data.combatSkills.mana=119;
        Click(ui,"InventoryUnequip");
        Check(Near(actor.MaxMana,100) && Near(actor.CurrentMana,100),"Unequip clamps only excess");
        Click(ui,"InventoryEquip");
        Check(Near(actor.MaxMana,125) && Near(actor.CurrentMana,100) &&
            !EquipmentRuntime.RebuildModifiers(data) && Derived(data)==3,"Repeated rebuild is idempotent");
        yield return CloseInventory(ui);
        pass("Hoàng grants exact IDs without learning/queuing, family swaps preserve HP, Armor mitigates and Mana caps/preview agree.");
    }

    internal static IEnumerator Run(GameManager game,SaveManager saves,UIManager ui,CombatManager combat,
        HeroController hero,EnvironmentManager space,Func<int,int,IEnumerator> resize,
        Func<string,IEnumerator> capture,Action<string> pass)
    {
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        UnityEngine.Random.InitState(17017);
        game.OnNewGameClicked();game.OnConfirmResetClicked();
        yield return Ready(game,ui);
        if (StoryOrigin.IsSafe(game.HeroData))
        {
            Check(game.TryTravelToWorldMap(StoryOrigin.HanVu.Forest),"Travel from Hàn Vụ to the loot map");
            yield return Ready(game,ui);
        }
        var data=game.HeroData;
        var actor=combat.CurrentHeroCombatState;
        var cfg=game.ProductionLoot;
        Check(cfg.equipment.enabled && cfg.equipment.chance==.08 && cfg.material.chance==.60 &&
            cfg.equipment.templates.Count==2 && !game.ControlledLootTestEnabled && data.equipmentItems.Count==0,
            "Authored production drop config");
        cfg.equipment.chance=1;cfg.material.chance=1; // Owned runtime only.
        var enemy=Wave(game,combat,space,hero,new Vector2(-30,0))[0];
        enemy.controller.attackMode=enemy.cycleMode=AttackMode.Melee;
        enemy.currentHP=1;
        ui.SetPaused(false);
        yield return Wait(()=>data.equipmentItems.Count==1,8,"Kill and auto-collect production gear");
        ui.SetPaused(true);
        cfg.equipment.chance=.08;cfg.material.chance=.60;
        var loot=data.equipmentItems[0];
        string lootId=loot.instanceId,lootTemplate=loot.templateId;
        Check(loot.rarity==EquipmentRarity.Common && loot.mainStats.Single().flat==4 &&
            loot.extraStats.Count==0 && EquipmentFoundation.GrantedSkillId(loot)==null &&
            !loot.isEquipped && (lootTemplate==EquipmentFoundation.DemoSword || lootTemplate==EquipmentFoundation.DemoBow),
            "Exactly one real Common loot item auto-collected");
        Call(combat,"HandleMonsterDeath",enemy);
        Check(data.equipmentItems.Count==1 && data.equipmentItems[0].instanceId==lootId,"Duplicate death cannot duplicate reward");
        yield return resize(600,338);
        yield return OpenInventory(ui);
        var inventory=Read<InventoryPanelUI>(ui,"inventoryUI");
        Click(ui,"BagCell0");
        var pages=Read<List<string>>(inventory,"detailPages");
        Check(pages.Any(p=>p.Contains("Physical Attack") && p.Contains("+4")),"Loot comparison shows actual +4");
        for(int i=0;i<pages.Count-1 && !Read<TextMeshProUGUI>(inventory,"detail").text.Contains("+4");i++)
            Click(ui,"InventoryDetailNext");
        var details=Read<TextMeshProUGUI>(inventory,"detail");
        details.ForceMeshUpdate();
        Check(details.text.Contains("+4") && !details.isTextOverflowing,"Compact comparison text fits");
        yield return capture("017-compare-compact");
        Click(ui,"InventoryEquip");
        Check(loot.isEquipped && WeaponBasicAttack.Physical(data)==14,"Equip through normal Inventory handler");
        Click(ui,"InventoryGrantMaterials");
        Click(ui,"InventoryForge");
        yield return null;
        int materials=EquipmentInventory.Count(data,EquipmentRefinement.MaterialId);
        Click(ui,"Forge_Up1");Click(ui,"Forge_Up2");
        Check(ReferenceEquals(loot,EquipmentRuntime.Find(data,lootId)) && loot.isEquipped &&
            loot.refineLevel==1 && loot.specialForgeCount==1 && loot.forgeHistory.Count==1 &&
            EquipmentInventory.Count(data,EquipmentRefinement.MaterialId)==materials-11 &&
            WeaponBasicAttack.Physical(data)==15 && Derived(data)==1 && EquipmentFoundation.GrantedSkillId(loot)==null,
            "Existing Forge mutates the worn instance once, no new skill from rarity alone");
        yield return capture("017-forge");
        Click(ui,"Forge_Close");
        yield return CloseInventory(ui);
        yield return resize(800,450);
        pass("Real Monster kill auto-collects an authored Common weapon, comparison +4, UI equip and existing Forge use same ID.");

        yield return EquipFixtures(game,ui,combat,hero,pass);
        var sword=data.equipmentItems.Single(x=>x.templateId==EquipmentFoundation.SkillSampleIds[0]);
        var legacy=data.equipmentItems.Single(x=>x.templateId==EquipmentFoundation.SkillSampleIds[1]);
        var legacyCommon=EquipmentFoundation.CreateDemoSword();
        legacyCommon.extraStats.Add(new ItemStatLine {stat=CombatStatId.MaxHealth,flat=500});
        Check(EquipmentRuntime.AddInstance(data,legacyCommon) && game.EquipGear(legacyCommon.instanceId,EquipmentSlot.Weapon) &&
            actor.MaxHP==1000,"Legacy Common bonus stays inert");
        legacy.grantedSkillId="";
        Equip(game,legacy.templateId);
        Check(WeaponSkillRegistry.Find(WeaponSkillRegistry.Volley).IsAvailable(data,legacy),"Legacy template grant resolves");
        legacy.grantedSkillId=WeaponSkillRegistry.Fireball;
        Check(EquipmentFoundation.GrantedSkillId(legacy)==null,"Mismatched saved grant does not grant anything");
        legacy.grantedSkillId="";
        Equip(game,EquipmentFoundation.SkillSampleIds[2]);
        yield return OpenInventory(ui);
        Select(inventory,sword.instanceId);
        Click(ui,"InventoryDetailCompare");
        pages=Read<List<string>>(inventory,"detailPages");
        Check(pages.Any(p=>p.Contains("Hỏa Cầu Thuật") && p.Contains("Đâm kiếm")) &&
            pages.Any(p=>p.Contains("Magical Attack") && p.Contains("-10")),"Skill swap and negative stat appear");
        for(int i=0;i<pages.Count-1 && !details.text.Contains("Kỹ năng từ trang bị");i++) Click(ui,"InventoryDetailNext");
        details.ForceMeshUpdate();
        Check(details.text.Contains("Kỹ năng từ trang bị") && !details.isTextOverflowing,"Skill comparison fits normal view");
        yield return capture("017-skill-compare");
        yield return CloseInventory(ui);

        data.combatSkills.Add(WeaponSkillRegistry.Thrust);
        long released=combat.WeaponSkillReleases, hits=combat.AcceptedHeroHitCount;
        enemy=Wave(game,combat,space,hero,new Vector2(-45,0))[0];
        enemy.controller.attackMode=enemy.cycleMode=AttackMode.Melee;
        ui.SetPaused(false);
        yield return Wait(()=>combat.AcceptedHeroHitCount>hits,6,"Staff basic while Thrust unavailable");
        ui.SetPaused(true);
        Check(combat.WeaponSkillReleases==released,"Incompatible queued skill did not fire");
        Equip(game,sword.templateId);
        enemy=Wave(game,combat,space,hero,new Vector2(-42,0))[0];
        enemy.controller.attackMode=enemy.cycleMode=AttackMode.Melee;
        ui.SetPaused(false);
        yield return Wait(()=>combat.WeaponSkillReleases==released+1,6,"Thrust re-enabled by equipment");
        ui.SetPaused(true);
        float cd=data.combatSkills.Remaining(WeaponSkillRegistry.Thrust);
        Check(cd>0 && game.UnequipGear(EquipmentSlot.Weapon),"Remove live skill source");
        Equip(game,sword.templateId);
        Check(Near(cd,data.combatSkills.Remaining(WeaponSkillRegistry.Thrust)) && data.combatSkills.queue.Count==1,
            "Cooldown and queue survive removing/re-equipping weapon");
        pass("Inventory shows skill gain/loss and negative delta; real Combat re-enables only queued and supported skill.");

        EquipmentRuntime.Normalize(data);
        actor.CurrentHP=617;data.combatSkills.mana=67;
        Check(game.TryManualSave(SaveSlot.ManualSave1),"Save canonical gear/resources");
        var path=Path.Combine(saves.SaveDirectory,"ManualSave1.json");
        string saved=File.ReadAllText(path);
        string core=JsonUtility.ToJson(loot);
        int level=data.currentLevel;
        Check(game.LoadAndContinue(SaveSlot.ManualSave1),"Load saved gear");
        data=game.HeroData;
        Check(File.ReadAllText(path)==saved && JsonUtility.ToJson(EquipmentRuntime.Find(data,lootId))==core &&
            EquipmentRuntime.Find(data,sword.instanceId).grantedSkillId==WeaponSkillRegistry.Thrust &&
            EquipmentRuntime.Find(data,legacy.instanceId).grantedSkillId=="" &&
            EquipmentRuntime.Find(data,legacyCommon.instanceId).extraStats.Single().flat==500 &&
            data.equipmentItems.Select(i=>i.instanceId).Distinct().Count()==data.equipmentItems.Count &&
            actor.CurrentHP==617 && actor.MaxHP==1000 && Near(actor.CurrentMana,67) && Near(actor.MaxMana,125) &&
            Near(data.combatSkills.Remaining(WeaponSkillRegistry.Thrust),cd) && data.currentLevel==level &&
            data.combatSkills.queue.SequenceEqual(new[]{WeaponSkillRegistry.Thrust}) &&
            !EquipmentRuntime.RebuildModifiers(data),
            $"Exact Load: HP {actor.CurrentHP}/{actor.MaxHP}, Mana {actor.CurrentMana}/{actor.MaxMana}, " +
            $"cooldown {data.combatSkills.Remaining(WeaponSkillRegistry.Thrust)}/{cd}, " +
            $"loot snapshot equal: {JsonUtility.ToJson(EquipmentRuntime.Find(data,lootId))==core}");
        yield return Ready(game,ui);
        Check(combat.ApplyDamageToRegisteredHero(hero,1000000,"Owned Retry"),"Defeat for Retry");
        game.OnRetryClicked();yield return Ready(game,ui);
        data=game.HeroData;
        Check(data.equipmentItems.Count(i=>i.instanceId==lootId)==1 && !EquipmentRuntime.RebuildModifiers(data),
            "Retry does not re-award or apply gear twice");
        pass("Save/Load keeps IDs, slot, rolls, Forge and resources; legacy fields preserved; Retry cannot duplicate gear.");

        var candidate=UnityEngine.Object.Instantiate(data);
        candidate.combatSkills.health=-1;
        candidate.combatSkills.cooldowns.Clear();
        candidate.combatStatModifiers.Add(new CombatStatModifier {sourceId="owned:017:gate",stat=CombatStatId.Attack,flat=120});
        var profile=saves.CultivationProfile;
        using(var binding=new CultivationGameplayBinding(candidate,profile,HeavenlyGateV13Fixtures.Session(profile,180,7))){}
        Check(saves.SaveGame(candidate,SaveSlot.ManualSave2),"Save Gate fixture");
        UnityEngine.Object.Destroy(candidate);
        Check(game.LoadAndContinue(SaveSlot.ManualSave2),"Load Gate fixture");yield return Ready(game,ui);
        Check(game.RequestTribulation(),"Actual Gate starts");
        yield return Wait(()=>!game.HasTribulationLiability,50,"Gate settles");
        Check(game.Cultivation.Current.Foundation.ActualRealmId=="realm:truc-co" &&
            game.Cultivation.Current.Reserve.Amount==7 && game.HeroData.equipmentItems.Count(i=>i.instanceId==lootId)==1,
            "Gate preserves gear and reserve");
        pass("Real Thiên Quan transitions to Trúc Cơ and keeps forged loot and reserve.");
    }
}
#endif
