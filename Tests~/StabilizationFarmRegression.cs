using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
partial class MotionRegression
{
    static void StabilizationLifecycleTests()
    {
        Run("v03 Pause retains primary pending attack interval and Resume releases once",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Bow));
            var m=f.Monster(-200);LootBattle(f,gm,m);Tough(f);f.hero.TickMovement(f.space,0);
            var state=f.combat.CurrentHeroCombatState;state.AttackTimer=.4f;state.WindupMode=f.hero.attackMode;
            CombatStep(f,.1f);Near(state.AttackTimer,.3f,"pending interval");Check(f.combat.PendingProjectileCount==0,"not released yet");
            Time.timeScale=0;CombatStep(f,2);Near(state.AttackTimer,.3f,"paused interval unchanged");Check(f.combat.PendingProjectileCount==0,"pause does not fire");
            Time.timeScale=1;CombatStep(f,.31f);Check(f.combat.PendingProjectileCount==1,"resume one release");
        });
        foreach(bool monsterFirst in new[]{false,true})
        Run("v03 same-frame death ordering monsterFirst="+monsterFirst,()=>{
            GameManager gm;var f=WeaponField(out gm);var m=f.Monster(-20);LootBattle(f,gm,m);var victim=f.Enemies[0];
            if(monsterFirst)Lethal(f,victim);
            f.combat.ApplyDamageToRegisteredHero(f.hero,int.MaxValue,"[owned ordering fixture]");
            if(!monsterFirst)Lethal(f,victim);
            DeathCallback(f,victim);Lethal(f,victim);
            Check(!f.combat.HasLivingHero && gm.HeroData.equipmentItems.Count==(monsterFirst?1:0),"only accepted pre-owner-death kill may grant once");
            Check(f.combat.PendingProjectileCount==0,"owner defeat clears outgoing shots");
        });
    }
    static void StabilizationCameraSoakTests()
    {
        Run("v03 100 repeated move stop reversal cycles and ten hidden-entry resets",()=>{
            var c=new MultiZoneCamera();float x=576,maxError=0,dt=1f/60;
            for(int cycle=0;cycle<100;cycle++) {
                if(cycle%10==0){c.Reset();x=576;Near(c.Velocity,0,"hidden-entry velocity reset");}
                int direction=cycle%2==0?-1:1;float target=direction<0?.65f:.35f;
                for(int frame=0;frame<720;frame++) {x+=direction*450*dt;float pan=FramedPan(c,x,dt,target,-direction*450);Check(Mathf.Abs(pan)<=48.001f,"soak bounded pan");x+=pan;}
                Near((x+960)/1920,target,"soak moving frame",.015f);
                for(int frame=0;frame<360;frame++)x+=FramedPan(c,x,dt,target,0);
                float error=Mathf.Abs((x+960)/1920-target);maxError=Mathf.Max(maxError,error);Near((x+960)/1920,target,"soak stop6",.002f);
            }
            Console.WriteLine("CAMERA SOAK cycles=100 frames=108000 resets=10 maxStopError="+maxError);
        });
    }
    static string FarmGear(EntityDataSO h)
    {var rows=new List<string>();foreach(var g in h.equipmentItems)rows.Add(JsonUtility.ToJson(g,true));rows.Sort(StringComparer.Ordinal);return string.Join("\n",rows);}
    static void StabilizationFarmTests()
    {
        Run("v03 bounded 1000 pooled deaths rewards forge and owned save checkpoints",()=>{
            CombatBalance.ResetDevProfiles();GameManager gm;var f=WeaponField(out gm);var h=gm.HeroData;
            Set(gm,"controlledLootTest",false);var cfg=MainGameGearConfig();Set(gm,"productionLoot",cfg);
            var mr=new System.Random(303);var er=new System.Random(304);
            var service=Get<CombatLootRewards>(f.combat,"lootRewards");
            // Inject only RNG sources, keeping authored 0.60 / 0.08 and quantities.
            typeof(CombatLootRewards).GetField("materialRoll",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(service,new Func<double>(mr.NextDouble));
            typeof(CombatLootRewards).GetField("equipmentRoll",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(service,new Func<double>(er.NextDouble));
            var pooled=f.Monster(-20);int forged=0,checkpoints=0;float exp=h.currentExp;
            string dir=Path.Combine(Path.GetTempPath(),"idle-v03-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            var owner=new GameObject();owner.SetActive(false);var saves=owner.AddComponent<SaveManager>();saves.ConfigureSaveDirectoryBeforeAwake(dir);
            try {
                for(int i=1;i<=1000;i++) {
                    if(i>1){pooled.gameObject.SetActive(true);Call(pooled,"OnEnable");f.space.SetPosition(pooled.Rect,new Vector2(-20,0));}
                    LootBattle(f,gm,pooled);var victim=f.Enemies[0];Lethal(f,victim);
                    string gear=FarmGear(h);int stock=EquipmentInventory.Count(h,EquipmentRefinement.MaterialId);float xp=h.currentExp;
                    DeathCallback(f,victim);Lethal(f,victim);
                    Check(FarmGear(h)==gear && EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==stock && h.currentExp==xp,"duplicate callback mutation at "+i);
                    Check(service.ReceiptCount<=1,"receipts must not accumulate across encounters");
                    var ids=new HashSet<string>();foreach(var g in h.equipmentItems)Check(ids.Add(g.instanceId),"unique gear instance");
                    if(i%50==0 && h.equipmentItems.Count>0 && stock>=11) {
                        var item=h.equipmentItems[h.equipmentItems.Count-1];if(item.isEquipped)Check(gm.UnequipGear(EquipmentSlot.Weapon),"unequip before repeating equip");Check(gm.EquipGear(item.instanceId,EquipmentSlot.Weapon),"equip awarded gear at "+i);
                        string error;Check(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.NormalUp,out error),error);
                        Check(gm.ApplyForgeTest(item.instanceId,EquipmentRefinement.Operation.SpecialUp,out error),error);
                        Check(EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)==stock-11,"real reward Forge budget");forged++;
                        int power=WeaponBasicAttack.DamageSource(h);EquipmentRuntime.RebuildModifiers(h);EquipmentRuntime.RebuildModifiers(h);
                        Check(WeaponBasicAttack.DamageSource(h)==power,"rebuild must not multiply modifiers");
                    }
                    if(i%200==0) {
                        EquipmentRuntime.Normalize(h);gear=FarmGear(h);int damage=WeaponBasicAttack.DamageSource(h),hp=h.GetCalculatedHealth(f.hero.attackMode);
                        Check(saves.SaveGame(h,SaveSlot.ManualSave1),"owned farm Save");var loaded=new EntityDataSO();
                        Check(saves.LoadGame(loaded,SaveSlot.ManualSave1),"owned farm Load");
                        Check(FarmGear(loaded)==gear && EquipmentInventory.Count(loaded,EquipmentRefinement.MaterialId)==EquipmentInventory.Count(h,EquipmentRefinement.MaterialId),"gear/material/history roundtrip");
                        Check(WeaponBasicAttack.DamageSource(loaded)==damage && loaded.GetCalculatedHealth(f.hero.attackMode)==hp && loaded.statPoints==h.statPoints && loaded.addedHealth==h.addedHealth && loaded.addedDamage==h.addedDamage,"derived and allocated stats roundtrip");checkpoints++;
                    }
                }
                Check(h.currentExp>exp && h.equipmentItems.Count>0 && forged>0 && checkpoints==5,"meaningful bounded workload");
                Console.WriteLine("FARM kills=1000 encounters=1000 pooledActors=1 gear="+h.equipmentItems.Count+" material="+EquipmentInventory.Count(h,EquipmentRefinement.MaterialId)+" forgePairs="+forged+" saveLoad="+checkpoints+" receipts="+service.ReceiptCount+" exp="+h.currentExp);
            } finally {foreach(string file in Directory.GetFiles(dir))File.Delete(file);Directory.Delete(dir);}
        });
    }
}
