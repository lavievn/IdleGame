using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static Field LootField(bool enabled,out GameManager gm)
    {
        var f=new Field();UIManager.Instance=null;
        gm=new GameObject().AddComponent<GameManager>();
        var primary=HeroAt(20);primary.expToNextLevel=1000000;
        EquipmentInventory.InitializeNewRun(primary);EquipmentRuntime.InitializeNewRun(primary);
        Set(gm,"runtimeHeroData",primary);Set(gm,"controlledLootTest",enabled);Set(gm,"hasDeployed",true);
        Set(gm,"heroController",f.hero);Set(gm,"combatManager",f.combat);Set(gm,"environmentManager",f.space);
        Set(f.combat,"gameManager",gm);f.combat.SetupHeroInfo(primary);
        // Retain legacy/B2 AoE coverage; primary Staff is single-target in weapon tests.
        f.hero.BindBasicAttackData(primary,true);return f;
    }
    static void LootBattle(Field f,GameManager gm,params MonsterController[] monsters)
    {
        f.Battle(monsters);gm.activeMonsters.Clear();foreach(var m in monsters)gm.activeMonsters.Add(m.gameObject);
    }
    static void Lethal(Field f,CombatManager.ActiveMonsterInfo info,HeroCombatState attacker=null)
    {
        typeof(CombatManager).GetMethod("DealDamageFromHero",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(f.combat,new object[]{info,int.MaxValue,attacker??f.combat.CurrentHeroCombatState,null});
    }
    static void DeathCallback(Field f,CombatManager.ActiveMonsterInfo info)
    {
        typeof(CombatManager).GetMethod("HandleMonsterDeathBy",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(f.combat,new object[]{info,f.combat.CurrentHeroCombatState});
    }
    static CombatLootRewards.Outcome CompleteLoot(CombatLootRewards service,CombatLootRewards.KillKey key,EntityDataSO h,bool on=true)
    {string id,error;return service.Complete(key,h,on,out id,out error);}
    static EntityDataSO LootData() {var h=HeroAt(1);EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);return h;}
    static void CombatLootTests()
    {
        Run("loot T01/T08/T16 primary kill and final wave grant exact factory gear before wave completion",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-20);LootBattle(f,gm,m);var info=f.Enemies[0];
            gm.HeroData.isDirty=false;int before=gm.HeroData.GetCalculatedDamage(f.hero.attackMode);
            Lethal(f,info);var items=gm.HeroData.equipmentItems;
            Check(items.Count==1 && items[0].templateId==EquipmentFoundation.DemoSword && items[0].level==1 &&
                items[0].rarity==EquipmentRarity.Common && items[0].refineLevel==0 && !items[0].isEquipped &&
                items[0].extraStats.Count==0 && items[0].mainStats[0].flat==10 && !string.IsNullOrEmpty(items[0].instanceId),"exact Common demo sword");
            Check(gm.HeroData.isDirty && gm.HeroData.GetCalculatedDamage(f.hero.attackMode)==before,"receive does not equip/recalculate");
            Check(gm.IsWaitingForExploration && gm.activeMonsters.Count==0,"last reward survives wave completion");
        });
        Run("loot T01 B2 killer owns EXP while primary (even fallen) owns loot",()=>{
            GameManager gm;var f=LootField(true,out gm);var companion=B2Hero(f,-80,AttackMode.RangedPhysical);
            var data=B2Data("loot ally");EquipmentRuntime.InitializeNewRun(data);
            var state=f.combat.RegisterAdditionalHero(companion,data);var m=f.Monster(-100);LootBattle(f,gm,m);
            int exp=gm.HeroData.currentExp;f.hero.Die();Lethal(f,f.Enemies[0],state);
            Check(gm.HeroData.equipmentItems.Count==1 && data.equipmentItems.Count==0,"primary receives item, not B2");
            Check(data.currentExp>0 && gm.HeroData.currentExp==exp,"EXP remains attacker-owned");
        });
        Run("loot T02/T03 duplicate and fabricated non-lethal death cannot repeat EXP or wave",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-20);LootBattle(f,gm,m);var info=f.Enemies[0];
            typeof(CombatManager).GetMethod("HandleMonsterDeath",BindingFlags.NonPublic|BindingFlags.Instance)
                .Invoke(f.combat,new object[]{info});
            DeathCallback(f,info);Check(gm.HeroData.equipmentItems.Count==0 && gm.HeroData.currentExp==0,"positive HP callback rejected");
            info.currentHP=0;DeathCallback(f,info);Check(gm.HeroData.equipmentItems.Count==0,"zero HP without accepted damage rejected");
            info.currentHP=1;Lethal(f,info);int exp=gm.HeroData.currentExp,wave=gm.HeroData.completedWavesInMap;
            DeathCallback(f,info);Lethal(f,info);gm.OnMonsterDied(info.go);
            Check(gm.HeroData.equipmentItems.Count==1 && gm.HeroData.currentExp==exp && gm.HeroData.completedWavesInMap==wave,"at-most-once all results");
        });
        Run("loot T04/T08/T09 pool reuse and repeated map farming permit new life only",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-20);LootBattle(f,gm,m);var old=f.Enemies[0];Lethal(f,old);
            m.gameObject.SetActive(true);Call(m,"OnEnable");LootBattle(f,gm,m);var fresh=f.Enemies[0];
            DeathCallback(f,old);Lethal(f,old);Check(gm.HeroData.equipmentItems.Count==1,"old pool lifetime denied");
            Lethal(f,fresh);Check(gm.HeroData.equipmentItems.Count==2 && gm.HeroData.equipmentItems[0].instanceId!=gm.HeroData.equipmentItems[1].instanceId,"new life unique gear");
        });
        Run("loot T05/T07 stale projectile, cleanup and escaped-wave cancellation give no loot",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-200);LootBattle(f,gm,m);EmitHeroMagic(f);
            var info=f.Enemies[0];var shot=FirstShot(f);float dt=Get<float>(shot,"duration");
            f.combat.ClearWaveKeepingMagicVisuals();DeathCallback(f,info);CombatStep(f,dt+1);
            Check(gm.HeroData.equipmentItems.Count==0,"cancel/tail cannot reward");
            var other=f.Monster(-20);var escaping=f.Monster(-200);LootBattle(f,gm,other,escaping);
            var next=f.Enemies[1];Lethal(f,f.Enemies[0]);Check(gm.HeroData.equipmentItems.Count==1,"accepted kill before escape is rewarded");
            Call(gm,"RecoverEscapedWave");DeathCallback(f,next);Check(gm.HeroData.equipmentItems.Count==1,"escape adds no loot and preserves prior accepted reward");
        });
        Run("loot T06 legitimate Magic AoE awards each target once",()=>{
            GameManager gm;var f=LootField(true,out gm);var a=f.Monster(-200);var b=f.Monster(-210);LootBattle(f,gm,a,b);
            foreach(var info in f.Enemies)info.currentHP=1;
            EmitHeroMagic(f);float duration=Get<float>(FirstShot(f),"duration");CombatStep(f,duration+.1f);
            Check(gm.HeroData.equipmentItems.Count==2 && f.Enemies.Count==0,"one gear per accepted AoE victim");
        });
        Run("loot T10/T11 in-memory snapshots restore exact IDs, older rollback, new epoch and new kills",()=>{
            GameManager gm;var f=LootField(true,out gm);string old=JsonUtility.ToJson(gm.HeroData,true);
            var m=f.Monster(-20);LootBattle(f,gm,m);var info=f.Enemies[0];Lethal(f,info);
            string snapshot=JsonUtility.ToJson(gm.HeroData,true);string id=gm.HeroData.equipmentItems[0].instanceId;
            var restored=LootData();JsonUtility.FromJsonOverwrite(snapshot,restored);EquipmentInventory.Normalize(restored);EquipmentRuntime.Normalize(restored);
            Check(restored.equipmentItems.Count==1 && restored.equipmentItems[0].instanceId==id,"in-memory saved exact item");
            Set(gm,"runtimeHeroData",restored);f.combat.SetupHeroInfo(restored);DeathCallback(f,info);
            Check(restored.equipmentItems.Count==1,"restore did not replay reward");
            JsonUtility.FromJsonOverwrite(old,restored);Check(restored.equipmentItems.Count==0,"older snapshot rolls back loot");
            var next=f.Monster(-20);LootBattle(f,gm,next);Lethal(f,f.Enemies[0]);Check(restored.equipmentItems.Count==1,"new lifetime can receive normally");
        });
        Run("loot T12/T15 rejected future snapshot leaves live context, NoDrop/duplicate are pure",()=>{
            var h=LootData();var service=new CombatLootRewards();service.BeginEncounter(h);var key=service.Register(1,1);
            var future=LootData();future.equipmentVersion=999;string reason;
            Check(!EquipmentFoundation.TryValidateSaveVersions(future,out reason) && service.IsCurrent(key,h),"reject candidate without publishing or invalidating live epoch");
            h.isDirty=false;Check(service.TryClaim(key,h),"claim Off kill");
            Check(CompleteLoot(service,key,h,false)==CombatLootRewards.Outcome.NoDrop && !h.isDirty,"Off NoDrop does not dirty");
            Check(!service.TryClaim(key,h) && CompleteLoot(service,key,h)==CombatLootRewards.Outcome.Rejected && !h.isDirty,"duplicate cannot turn Off outcome into Awarded");
        });
        Run("loot T13 gear pagination is not capacity and full stacks are irrelevant to gear-only A",()=>{
            var h=LootData();EquipmentInventory.Add(h,EquipmentCatalog.HealthCharm,9998);
            var service=new CombatLootRewards();service.BeginEncounter(h);
            for(int i=1;i<=25;i++) {var k=service.Register(i,1);Check(service.TryClaim(k,h),"new kill");Check(CompleteLoot(service,k,h)==CombatLootRewards.Outcome.Awarded,"gear added beyond UI page");}
            Check(h.equipmentItems.Count==25 && EquipmentInventory.Count(h,EquipmentCatalog.HealthCharm)==9999,"no stack mutation/overflow/drop");
        });
        Run("loot T14 insertion failure quarantines without reroll, invalid item/schema also fail",()=>{
            foreach(int fault in new[]{0,1,2,3}) {
                var h=LootData();var item=EquipmentFoundation.CreateDemoSword();EquipmentRuntime.AddInstance(h,item);int calls=0;
                var service=new CombatLootRewards(()=>{calls++;if(fault==0)return item;var x=EquipmentFoundation.CreateDemoSword();x.level=0;return x;});
                if(fault==2)h.equipmentItems=null;if(fault==3)h.equipmentVersion=999;
                service.BeginEncounter(h);var key=service.Register(1,1);service.TryClaim(key,h);h.isDirty=false;
                string id,error;
                Check(service.Complete(key,h,true,out id,out error)==CombatLootRewards.Outcome.Failed &&
                    service.Faulted && !h.isDirty && error.Contains("FAILED") && error.Contains("life=1"),"clear terminal diagnostic failure");
                int after=calls;Check(CompleteLoot(service,key,h)==CombatLootRewards.Outcome.Rejected,"no duplicate retry");
                service.BeginEncounter(h);key=service.Register(2,1);service.TryClaim(key,h);CompleteLoot(service,key,h);
                Check(calls==after,"fault persists across encounters, no new ID");
            }
        });
        Run("loot T02 resolving receipt rejects re-entry before factory insertion",()=>{
            var h=LootData();CombatLootRewards service=null;CombatLootRewards.KillKey key=default(CombatLootRewards.KillKey);int calls=0;
            service=new CombatLootRewards(()=>{calls++;Check(!service.TryClaim(key,h),"re-entry claim denied");Check(CompleteLoot(service,key,h)==CombatLootRewards.Outcome.Rejected,"re-entry complete denied");return EquipmentFoundation.CreateDemoSword();});
            service.BeginEncounter(h);key=service.Register(1,1);service.TryClaim(key,h);
            Check(CompleteLoot(service,key,h)==CombatLootRewards.Outcome.Awarded && calls==1 && h.equipmentItems.Count==1,"one factory/insertion");
        });
        Run("loot T05/T09 map, snapshot reference, disable and cancel invalidate context with bounded receipts",()=>{
            var h=LootData();var service=new CombatLootRewards();service.BeginEncounter(h);var key=service.Register(1,2);
            Check(!service.IsCurrent(new CombatLootRewards.KillKey(key.RunEpoch,key.Encounter,1,3),h),"spawn mismatch");
            h.mapNumber++;Check(!service.TryClaim(key,h),"map context mismatch");h.mapNumber--;
            Check(!service.TryClaim(key,LootData()),"different primary reference");service.Invalidate();Check(service.ReceiptCount==0 && !service.TryClaim(key,h),"cancel bounded state");
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-20);LootBattle(f,gm,m);var victim=f.Enemies[0];Call(f.combat,"OnDisable");Lethal(f,victim);
            Check(gm.HeroData.equipmentItems.Count==0 && gm.HeroData.currentExp==0,"disabled context not rewardable");
        });
        Run("loot T15 early save refusal preserves dirty and cannot re-award",()=>{
            var h=LootData();var service=new CombatLootRewards();service.BeginEncounter(h);
            var key=service.Register(1,1);service.TryClaim(key,h);CompleteLoot(service,key,h);
            h.equipmentVersion=999;var saves=new GameObject().AddComponent<SaveManager>();
            // No Awake and no directory configured. Version refusal occurs before ALL I/O.
            Check(!saves.SaveGame(h,SaveSlot.AutoSave) && h.isDirty,"failed save keeps dirty/item");
            Check(CompleteLoot(service,key,h)==CombatLootRewards.Outcome.Rejected && h.equipmentItems.Count==1,"save retry is not reward retry");
        });
        Run("loot enable resumes validated living combat with a fresh token, old projectile still rejected",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-200);LootBattle(f,gm,m);EmitHeroMagic(f);
            var shot=FirstShot(f);var victim=f.Enemies[0];var old=victim.lootKey;
            Call(f.combat,"OnDisable");Call(f.combat,"OnEnable");
            Check(victim.lootKey.Encounter!=old.Encounter,"new explicit validated encounter");
            Get<System.Collections.IList>(f.combat,"projectiles").Add(shot);CombatStep(f,20);
            Check(gm.HeroData.equipmentItems.Count==0,"old projectile cannot use rebound life");
            Lethal(f,victim);Check(gm.HeroData.equipmentItems.Count==1,"new attacks can finish resumed combat");
        });
        Run("loot owner disable cancels reward context even if CombatManager stays enabled",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-200);LootBattle(f,gm,m);
            var victim=f.Enemies[0];var old=victim.lootKey;
            gm.enabled=false;Call(gm,"OnDisable");Lethal(f,victim);
            Check(gm.HeroData.equipmentItems.Count==0 && gm.HeroData.currentExp==0,"disabled owner is not rewardable");
            EmitHeroMagic(f);float duration=Get<float>(FirstShot(f),"duration");
            gm.enabled=true;Call(gm,"OnEnable");Check(victim.lootKey.Encounter!=old.Encounter,"owner re-enable revalidates new context");
            typeof(CombatManager).GetMethod("TickProjectiles",BindingFlags.NonPublic|BindingFlags.Instance)
                .Invoke(f.combat,new object[]{f.space,duration+1f});
            Check(victim.currentHP==victim.maxHP && gm.HeroData.equipmentItems.Count==0,"volley emitted while owner disabled cannot enter resumed context");
            Lethal(f,victim);Check(gm.HeroData.equipmentItems.Count==1,"living encounter resumes correctly");
        });
        Run("loot T19 kill to Inventory comparison to real equip changes combat stats only on equip",()=>{
            GameManager gm;var f=LootField(true,out gm);var m=f.Monster(-20);LootBattle(f,gm,m);
            int attack=gm.HeroData.GetCalculatedDamage(AttackMode.Melee);Lethal(f,f.Enemies[0]);
            var item=gm.HeroData.equipmentItems[0];Check(gm.HeroData.GetCalculatedDamage(AttackMode.Melee)==attack,"loot alone has no bonus");
            var ui=new GameObject().AddComponent<UIManager>();
            var panel=InventoryPanelUI.Install(ui,(RectTransform)new GameObject(true).transform);panel.Open();
            gm.HeroData.isDirty=false;int hp=f.combat.CurrentHeroCombatState.CurrentHP;
            SelectComparison(panel,"gear:"+item.instanceId);var prediction=Compare(gm.HeroData,"gear:"+item.instanceId);
            Check(CompareText(panel).Contains("Preview:") && !gm.HeroData.isDirty && f.combat.CurrentHeroCombatState.CurrentHP==hp,"inventory preview remains pure");
            Call(panel,"EquipSelected");Near(gm.HeroData.GetCalculatedDamage(AttackMode.Melee),prediction.Find(CombatStatId.Attack).preview,"real equip matches predicted ATK");
            Check(gm.HeroData.GetCalculatedDamage(AttackMode.Melee)==attack+10 && gm.HeroData.equipmentItems.Count==1,"one sword equipped, no duplicate");
        });
        Run("loot T16/T18 enabled differs only in gear, not combat RNG, EXP, damage, spawn or healing",()=>{
            int[] baseline=null;
            foreach(bool enabled in new[]{false,true}) {
                UnityEngine.Random.InitState(3942);GameManager gm;var f=LootField(enabled,out gm);
                f.combat.CurrentHeroCombatState.CurrentHP=30;var m=f.Monster(-20);LootBattle(f,gm,m);var info=f.Enemies[0];
                int max=info.maxHP,level=info.data.currentLevel;Lethal(f,info);
                var values=new[]{gm.HeroData.currentExp,f.combat.CurrentHeroCombatState.CurrentHP,max,level,UnityEngine.Random.Range(0,100000)};
                if(!enabled)baseline=values;else for(int i=0;i<values.Length;i++)Check(values[i]==baseline[i],"unchanged combat output "+i);
                Check(gm.HeroData.equipmentItems.Count==(enabled?1:0),"flag controls only loot");
            }
        });
    }
}
