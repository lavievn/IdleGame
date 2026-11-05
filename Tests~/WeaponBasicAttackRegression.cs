using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static Field WeaponField(out GameManager gm)
    {
        var f=LootField(true,out gm);
        var h=HeroAt(1);h.expToNextLevel=1000000;
        EquipmentInventory.InitializeNewRun(h);EquipmentRuntime.InitializeNewRun(h);
        h.spiritRoots=new List<ElementType>{ElementType.Vo};h.NormalizeRoots();
        Set(gm,"runtimeHeroData",h);f.combat.SetupHeroInfo(h); // real primary path, no B2 fallback
        return f;
    }
    static EquipmentInstance WeaponInBag(GameManager gm,WeaponFamily family)
    {
        var item=family==WeaponFamily.Sword?EquipmentFoundation.CreateDemoSword():
            family==WeaponFamily.Bow?EquipmentFoundation.CreateDemoBow():EquipmentFoundation.CreateDemoStaff();
        Check(EquipmentRuntime.AddInstance(gm.HeroData,item),"add dev sample");
        return EquipmentRuntime.Find(gm.HeroData,item.instanceId);
    }
    static void WearWeapon(GameManager gm,EquipmentInstance item)
    {Check(gm.EquipGear(item.instanceId,EquipmentSlot.Weapon),"real weapon handler");}
    static object FireWeapon(Field f)
    {
        f.combat.CurrentHeroCombatState.AttackTimer=0;CombatStep(f,.001f);
        Check(f.combat.PendingProjectileCount==1,"one released projectile");return FirstShot(f);
    }
    static void WeaponBasicAttackTests()
    {
        Run("v04 damage diagnostics identify physical and magical source across snapshot",()=>{
            GameManager gm;WeaponField(out gm);var h=gm.HeroData;var d=HeroAt(1);
            var sword=WeaponInBag(gm,WeaponFamily.Sword);WearWeapon(gm,sword);
            var physical=SynergyMath.EvaluateBasicDamage(h,d,AttackMode.Melee,1f);
            Check(physical.attackBase==WeaponBasicAttack.Physical(h),"effective physical stat");
            Check(physical.Describe(1,1,physical.elementDamage).Contains("Physical Attack"),"physical source label");
            var staff=WeaponInBag(gm,WeaponFamily.Staff);WearWeapon(gm,staff);
            int magicBefore=WeaponBasicAttack.Magical(h);
            h.combatStatModifiers.Add(new CombatStatModifier {stat=CombatStatId.MagicalAttack,flat=17});
            var magic=SynergyMath.EvaluateBasicDamage(h,d,AttackMode.RangedMagic,1f);
            var impact=SynergyMath.EvaluateSnapshot(magic,d);
            string text=impact.Describe(1,1,impact.elementDamage);
            Check(impact.attackBase==WeaponBasicAttack.Magical(h) && impact.attackBase==magicBefore+17,"magical source excludes Physical");
            Check(text.Contains("MagicalAttack") && text.Contains("Magical") && !text.Contains("cộng điểm:"),"impact retains source without false physical breakdown");
            Check(impact.elementDamage==magic.elementDamage && impact.roll==magic.roll,"diagnostic preserves damage and release snapshot");
        });
        Run("weapon 01 unarmed resolves Melee and baseline Physical10 Magical0",()=>{
            GameManager gm;var f=WeaponField(out gm);
            Check(f.hero.UsesWeaponBasicAttack && f.hero.attackMode==AttackMode.Melee,"primary weapon authority");
            Check(WeaponBasicAttack.Physical(gm.HeroData)==10 && WeaponBasicAttack.Magical(gm.HeroData)==0,"separate baselines");
        });
        foreach(var family in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff})
        Run("weapon 02..04 equip resolves structured family "+family,()=>{
            GameManager gm;var f=WeaponField(out gm);var item=WeaponInBag(gm,family);WearWeapon(gm,item);
            Check(WeaponBasicAttack.Family(item)==family && f.hero.attackMode==WeaponBasicAttack.Mode(family),"family and internal mode");
            Near(f.hero.attackRange,f.space.AttackRange(f.hero.attackMode,true),"range refreshed");
            Check(WeaponBasicAttack.DamageSource(gm.HeroData)==(family==WeaponFamily.Staff?10:20),"expected sample source");
        });
        Run("weapon 05 Sword Staff Bow Unarmed refreshes without healing teleport or free strike",()=>{
            GameManager gm;var f=WeaponField(out gm);var state=f.combat.CurrentHeroCombatState;
            state.CurrentHP=37;var point=f.space.Position(f.hero.heroRect);int life=state.LifeVersion;
            foreach(var family in new[]{WeaponFamily.Sword,WeaponFamily.Staff,WeaponFamily.Bow}) {
                WearWeapon(gm,WeaponInBag(gm,family));
                Check(state.CurrentHP==37 && state.LifeVersion==life,"no heal or life reset");
                Check(f.combat.PendingProjectileCount==0 && state.AttackTimer>0,"full new interval, no free release");
                Near(f.space.Position(f.hero.heroRect).x,point.x,"no X teleport");
                Near(f.space.Position(f.hero.heroRect).y,point.y,"no Y teleport");
            }
            Check(gm.UnequipGear(EquipmentSlot.Weapon) && f.hero.attackMode==AttackMode.Melee,"unequip unarmed");
            Check(state.CurrentHP==37 && WeaponBasicAttack.Magical(gm.HeroData)==0,"no heal or residual magical bonus");
        });
        Run("weapon 06 receive and non-Weapon equip cannot change attack profile",()=>{
            GameManager gm;var f=WeaponField(out gm);var staff=WeaponInBag(gm,WeaponFamily.Staff);WearWeapon(gm,staff);
            WeaponInBag(gm,WeaponFamily.Bow);var ring=EquipmentFoundation.CreateCommon("base:ring",1,4);
            EquipmentRuntime.AddInstance(gm.HeroData,ring);Check(gm.EquipGear(ring.instanceId,EquipmentSlot.RingLeft),"wear ring");
            Check(f.hero.attackMode==AttackMode.RangedMagic && WeaponBasicAttack.Magical(gm.HeroData)==10,"ring Attack stays physical");
        });
        Run("weapon 07 failed or repeated equip is combat-state pure",()=>{
            GameManager gm;var f=WeaponField(out gm);var bow=WeaponInBag(gm,WeaponFamily.Bow);WearWeapon(gm,bow);
            var s=f.combat.CurrentHeroCombatState;s.CurrentHP=31;s.AttackTimer=.63f;gm.HeroData.isDirty=false;
            Check(!gm.EquipGear(bow.instanceId,EquipmentSlot.Head) && !gm.EquipGear("missing",EquipmentSlot.Weapon) &&
                !gm.EquipGear(bow.instanceId,EquipmentSlot.Weapon),"invalid/missing/already worn reject");
            Check(!gm.HeroData.isDirty && s.CurrentHP==31 && s.AttackTimer==.63f &&
                f.hero.attackMode==AttackMode.RangedPhysical,"no failed mutation");
        });
        Run("weapon 08 legacy Attack bonus remains physical and never scales Staff twice",()=>{
            GameManager gm;WeaponField(out gm);gm.HeroData.addedDamage=71;
            Check(gm.EquipInventoryItem(EquipmentCatalog.AttackCharm),"legacy charm");
            WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            Check(WeaponBasicAttack.Physical(gm.HeroData)==101 && WeaponBasicAttack.Magical(gm.HeroData)==10,"physical legacy retained separately");
            var target=HeroAt(1);var trace=SynergyMath.EvaluateBasicDamage(gm.HeroData,target,AttackMode.RangedMagic,1);
            Check(trace.attackBase==10 && trace.rolledDamage==10 && trace.modeMultiplier==1,"no physical input or x2.5");
        });
        Run("weapon 09 Staff interval uses basic speed clock without charge or frame spam",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            f.Battle(f.Monster(-200));Tough(f);var s=f.combat.CurrentHeroCombatState;s.AttackTimer=1.4f;
            CombatStep(f,.7f);Check(f.combat.PendingProjectileCount==0,"interval half");
            Check(Get<MagicChargeBar>(f.hero,"chargeBar")==null,"no basic charge UI");
            CombatStep(f,.7f);Check(f.combat.PendingProjectileCount==1,"release at basic interval");
            CombatStep(f,.001f);Check(f.combat.PendingProjectileCount==1,"not per frame");
            Near(WeaponBasicAttack.Interval(AttackMode.RangedMagic,1.25f),1.12f,"existing attack speed scaling");
            gm.HeroData.baseAttackSpeed=1.25f;f.combat.RefreshWeaponAttack(f.hero);
            Near(s.AttackTimer,1.12f,"actual actor interval uses speed");
        });
        Run("weapon 10 Staff projectile single-target one hit no AoE explosion or legacy tail",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            f.Battle(f.Monster(-200),f.Monster(-210));Tough(f);var first=f.Enemies[0];var second=f.Enemies[1];
            var shot=FireWeapon(f);var trace=Get<DamageTrace>(shot,"trace");
            Check(Get<bool>(shot,"staffBasic") && !Get<bool>(shot,"magic") && Get<float>(shot,"arc")==0,"light straight bolt");
            Check(trace.weaponBasic && trace.attackBase==10 && trace.modeMultiplier==1,"ordinary magical snapshot");
            float flight=Get<float>(shot,"duration");CombatStep(f,flight+.001f);
            int expected=CombatDamage.ResolveBasicSnapshot(trace.elementDamage,trace.basicBoost,first.data,AttackMode.RangedMagic,null);
            Check(first.currentHP==10000-expected && second.currentHP==10000,"only selected target hit once");
            int hp=first.currentHP;CombatStep(f,.01f);
            Check(first.currentHP==hp && SparkCount(f)==0 && f.combat.DetachedMagicVisualCount==0,"no repeat explosion/tail");
        });
        Run("weapon 11 Staff damage uses magical resistance rather than armor",()=>{
            GameManager gm;WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            var h=gm.HeroData;var defender=HeroAt(1);
            defender.combatStatModifiers.Add(new CombatStatModifier {stat=CombatStatId.Armor,flat=1000});
            Check(CombatDamage.ResolveBasicSnapshot(10,0,defender,AttackMode.RangedMagic,null)==10,"armor irrelevant to Staff");
            defender.combatStatModifiers.Add(new CombatStatModifier {stat=CombatStatId.MagicResistance,flat=100});
            Check(CombatDamage.ResolveBasicSnapshot(10,0,defender,AttackMode.RangedMagic,null)==5,"existing magic defense kernel");
            h.combatStatModifiers.Add(new CombatStatModifier {stat=CombatStatId.MagicalDamagePercent,flat=.5f});
            Near(SynergyMath.EvaluateBasicDamage(h,defender,AttackMode.RangedMagic,1).basicBoost,.5f,"existing magical modifier");
        });
        Run("weapon 12 switching during pending interval cancels preparation without duplicate release",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            f.Battle(f.Monster(-200));Tough(f);f.combat.CurrentHeroCombatState.AttackTimer=.01f;
            WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Bow));CombatStep(f,.01f);
            Check(f.combat.PendingProjectileCount==0 && f.Enemies[0].currentHP==10000,"old ready cycle cannot release");
            CombatStep(f,.93f);Check(f.combat.PendingProjectileCount==1,"exactly one new Bow release");
            Check(Get<AttackMode>(FirstShot(f),"attackMode")==AttackMode.RangedPhysical,"new mode only");
        });
        Run("weapon 13 released Staff snapshot survives weapon and modifier replacement",()=>{
            GameManager gm;var f=WeaponField(out gm);var staff=WeaponInBag(gm,WeaponFamily.Staff);
            staff.extraStats.Add(new ItemStatLine {stat=CombatStatId.MagicalDamagePercent,flat=.5f});
            staff.rarity=EquipmentRarity.Thien;WearWeapon(gm,staff);f.Battle(f.Monster(-200));Tough(f);
            var shot=FireWeapon(f);var tr=Get<DamageTrace>(shot,"trace");var target=f.Enemies[0];
            int expected=CombatDamage.ResolveBasicSnapshot(tr.elementDamage,tr.basicBoost,target.data,AttackMode.RangedMagic,null);
            WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Sword));gm.HeroData.baseDamage=999;
            CombatStep(f,Get<float>(shot,"duration")+.001f);
            Check(target.currentHP==10000-expected && Get<AttackMode>(shot,"attackMode")==AttackMode.RangedMagic,"frozen offense and damage school");
        });
        Run("weapon 14 old Bow stays physical after switching to Staff",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Bow));
            f.Battle(f.Monster(-200));Tough(f);var target=f.Enemies[0];
            target.data.combatStatModifiers.Add(new CombatStatModifier {stat=CombatStatId.MagicResistance,flat=1000});
            var shot=FireWeapon(f);var tr=Get<DamageTrace>(shot,"trace");WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            CombatStep(f,Get<float>(shot,"duration")+.001f);
            Check(target.currentHP==10000-tr.elementDamage && tr.attackBase==20 && tr.modeMultiplier==.7f,"Bow damage frozen with old factor/school");
        });
        foreach(string invalidation in new[]{"target death","pool reuse","cancel","new map","owner death"})
        Run("weapon 15 Staff lifecycle rejects "+invalidation,()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            var m=f.Monster(-200);LootBattle(f,gm,m,f.Monster(-210));Tough(f);var target=f.Enemies[0];var other=f.Enemies[1];
            var shot=FireWeapon(f);
            if(invalidation=="target death")Lethal(f,target);
            if(invalidation=="pool reuse")Call(m,"OnEnable");
            if(invalidation=="cancel")f.combat.ClearWaveKeepingMagicVisuals();
            if(invalidation=="new map")gm.HeroData.worldMapId="invalidated-map";
            if(invalidation=="owner death")f.combat.ApplyDamageToRegisteredHero(f.hero,1000000,"test");
            int hp=target.currentHP,items=gm.HeroData.equipmentItems.Count;
            CombatStep(f,Get<float>(shot,"duration")+.001f);
            Check(target.currentHP==hp && other.currentHP==10000 && gm.HeroData.equipmentItems.Count==items,"no stale hit/AoE/reward");
            Check(f.combat.DetachedMagicVisualCount==0,"Staff never creates legacy explosion tail");
        });
        Run("weapon 16 in-memory save restores equipped IDs and ignores conflicting cached mode",()=>{
            GameManager gm;var f=WeaponField(out gm);var staff=WeaponInBag(gm,WeaponFamily.Staff);WearWeapon(gm,staff);
            gm.HeroData.savedAttackMode=AttackMode.Melee;string json=JsonUtility.ToJson(gm.HeroData,true);
            var restored=HeroAt(1);JsonUtility.FromJsonOverwrite(json,restored);
            EquipmentInventory.Normalize(restored);EquipmentRuntime.Normalize(restored);
            Set(gm,"runtimeHeroData",restored);f.combat.SetupHeroInfo(restored);
            Check(f.hero.attackMode==AttackMode.RangedMagic && WeaponBasicAttack.Identity(restored)==staff.instanceId &&
                WeaponBasicAttack.Magical(restored)==10,"weapon assignment authoritative after memory load");
            Check(restored.savedAttackMode==AttackMode.Melee,"load does not require rewriting cached enum");
        });
        Run("weapon 17 legacy Sword data has template family without item mutation; no weapon fallback Melee",()=>{
            GameManager gm;var f=WeaponField(out gm);var sword=WeaponInBag(gm,WeaponFamily.Sword);WearWeapon(gm,sword);
            gm.HeroData.savedAttackMode=AttackMode.RangedMagic;string before=JsonUtility.ToJson(sword,true);gm.HeroData.isDirty=false;
            f.combat.SetupHeroInfo(gm.HeroData);
            Check(f.hero.attackMode==AttackMode.Melee && WeaponBasicAttack.Physical(gm.HeroData)==20 &&
                JsonUtility.ToJson(sword,true)==before,"old Sword retained exactly, family resolved by ID");
            Check(gm.UnequipGear(EquipmentSlot.Weapon),"remove");f.combat.SetupHeroInfo(gm.HeroData);
            Check(f.hero.attackMode==AttackMode.Melee,"saved magic ignored when unarmed");
        });
        Run("weapon 18 explicit B2 fallback keeps independent magic and cannot alter primary",()=>{
            GameManager gm;var f=WeaponField(out gm);var b=B2Hero(f,-80,AttackMode.RangedMagic);
            var state=f.combat.RegisterAdditionalHero(b,B2Data("legacy mage"));state.AttackTimer=.5f;
            WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Bow));
            Check(!b.UsesWeaponBasicAttack && b.attackMode==AttackMode.RangedMagic && state.AttackTimer==.5f,"B2 unchanged");
            Near(SynergyMath.EvaluateDamage(state.Data,gm.HeroData,AttackMode.RangedMagic,1).modeMultiplier,2.5f,"legacy B2 path retained");
            Check(f.hero.attackMode==AttackMode.RangedPhysical,"primary independently bound");
        });
        Run("weapon 19 Profile A retains exactly Common Sword and cannot auto equip",()=>{
            GameManager gm;var f=WeaponField(out gm);var staff=WeaponInBag(gm,WeaponFamily.Staff);WearWeapon(gm,staff);
            var m=f.Monster(-200);LootBattle(f,gm,m);Lethal(f,f.Enemies[0]);DeathCallback(f,new CombatManager.ActiveMonsterInfo());
            var loot=gm.HeroData.equipmentItems.Find(x=>x.instanceId!=staff.instanceId);
            Check(gm.HeroData.equipmentItems.Count==2 && loot.templateId==EquipmentFoundation.DemoSword &&
                loot.rarity==EquipmentRarity.Common && loot.mainStats[0].flat==10 && !loot.isEquipped,"unchanged A factory");
            Check(f.hero.attackMode==AttackMode.RangedMagic,"grant cannot override Staff");
        });
        Run("weapon Staff lethal impact flows through EXP and single Profile A claim",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            LootBattle(f,gm,f.Monster(-200),f.Monster(-210));Tough(f);
            var victim=f.Enemies[0];var neighbor=f.Enemies[1];victim.currentHP=1;
            var shot=FireWeapon(f);CombatStep(f,Get<float>(shot,"duration")+.001f);
            Check(f.Enemies.Count==1 && neighbor.currentHP==10000 && gm.HeroData.currentExp>0,"one magical lethal victim and EXP");
            Check(gm.HeroData.equipmentItems.Count==2 && gm.HeroData.equipmentItems.FindAll(x=>
                x.templateId==EquipmentFoundation.DemoSword && !x.isEquipped).Count==1,"exactly one loot Sword beside worn Staff");
            int exp=gm.HeroData.currentExp;DeathCallback(f,victim);CombatStep(f,.001f);
            Check(gm.HeroData.currentExp==exp && gm.HeroData.equipmentItems.Count==2,"no duplicate from impact or callback");
        });
        Run("weapon 20 Compare predicts both attack sources and mode without combat or save mutation",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Sword));
            var staff=WeaponInBag(gm,WeaponFamily.Staff);var s=f.combat.CurrentHeroCombatState;s.CurrentHP=37;s.AttackTimer=.6f;
            gm.HeroData.isDirty=false;string before=JsonUtility.ToJson(gm.HeroData,true);var mods=gm.HeroData.combatStatModifiers;
            var c=Compare(gm.HeroData,"gear:"+staff.instanceId);
            Check(c.currentMode==AttackMode.Melee && c.mode==AttackMode.RangedMagic,"current and future profile");
            Near(c.Find(CombatStatId.Attack).current,20,"old physical");Near(c.Find(CombatStatId.Attack).preview,10,"future physical");
            Near(c.Find(CombatStatId.MagicalAttack).current,0,"old magical");Near(c.Find(CombatStatId.MagicalAttack).preview,10,"future magical");
            Check(before==JsonUtility.ToJson(gm.HeroData,true) && mods==gm.HeroData.combatStatModifiers && !gm.HeroData.isDirty &&
                s.CurrentHP==37 && s.AttackTimer==.6f && f.hero.attackMode==AttackMode.Melee,"pure preview");
            WearWeapon(gm,staff);Near(WeaponBasicAttack.Physical(gm.HeroData),c.Find(CombatStatId.Attack).preview,"physical matches equip");
            Near(WeaponBasicAttack.Magical(gm.HeroData),c.Find(CombatStatId.MagicalAttack).preview,"magical matches equip");
            Near(s.MaxHP,c.Find(CombatStatId.MaxHealth).preview,"mode-dependent old HP formula matches preview");
            c=Compare(gm.HeroData,"gear:"+staff.instanceId);Check(c.isUnequip && c.mode==AttackMode.Melee,"remove preview unarmed");
        });
        Run("weapon 21 serialized enums retain every old ID and append MagicalAttack",()=>{
            string[] old={"MaxHealth","Attack","Armor","MagicResistance","ElementResistance","PhysicalDamagePercent",
                "MagicalDamagePercent","ElementDamagePercent","AttackSpeed","MovementSpeed","CastSpeed",
                "CooldownReductionPercent","HealthRegeneration","ManaRegeneration","KillHealBonus"};
            for(int i=0;i<old.Length;i++)Check((int)Enum.Parse(typeof(CombatStatId),old[i])==i,"stable stat "+old[i]);
            Check((int)CombatStatId.MagicalAttack==15 && (int)AttackMode.Melee==0 &&
                (int)AttackMode.RangedPhysical==1 && (int)AttackMode.RangedMagic==2,"append only");
            Check(EquipmentFoundation.Version==3 && CombatStats.SchemaVersion==1,"no schema bump");
        });
        Run("weapon 22 manual UI callbacks and debug field cannot override equipped weapon",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));gm.HeroData.isDirty=false;
            gm.SetHeroAttackMode(0);f.hero.ChangeAttackMode(1);Check(f.hero.attackMode==AttackMode.RangedMagic && !gm.HeroData.isDirty,"callbacks ignored");
            f.hero.attackMode=AttackMode.Melee;f.hero.TickMovement(f.space,0);
            Check(f.hero.attackMode==AttackMode.RangedMagic,"manual inspector value re-resolves");
            foreach(string name in new[]{"Kiếm","Cung","Phép"})new GameObject(name,typeof(RectTransform));
            var ui=new GameObject().AddComponent<UIManager>();Call(ui,"HideLegacyAttackModeButtons");
            foreach(var rect in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(rect.gameObject.name=="Kiếm" || rect.gameObject.name=="Cung" || rect.gameObject.name=="Phép")
                    Check(!rect.gameObject.activeSelf,"removed manual HUD control");
        });
        Run("weapon 23 replacement preserves camera wave entry VFX and other actor identity",()=>{
            GameManager gm;var f=WeaponField(out gm);var bow=WeaponInBag(gm,WeaponFamily.Bow);
            var b=B2Hero(f,-80,AttackMode.Melee);var bs=f.combat.RegisterAdditionalHero(b,B2Data("ally"));
            var point=f.space.Position(b.heroRect);int life=bs.LifeVersion,wave=gm.HeroData.completedWavesInMap;
            float width=f.ground.rect.width;var fx=HeroSpawnVfx.Prepare(f.hero);fx.sheetOverride=new Texture2D(4000,2500,TextureFormat.RGBA32,false);
            Check(fx.Begin(),"valid fixture VFX sheet");
            WearWeapon(gm,bow);
            Check(gm.HeroData.completedWavesInMap==wave && !gm.IsEnteringGameplay && bs.LifeVersion==life,"wave/entry/life unchanged");
            Near(f.ground.rect.width,width,"projection unchanged");Near(f.space.Position(b.heroRect).x,point.x,"other body unchanged");
            Check(fx.IsPlaying,"weapon resolution cannot cancel active entry VFX");HeroSpawnVfx.Cancel(f.hero.heroRect);
        });
        Run("weapon same-family replacement resets targeting and cannot bypass attack interval",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            f.Battle(f.Monster(-200));Tough(f);f.hero.TickMovement(f.space,0);
            Check(f.hero.CurrentTarget!=null,"target acquired before swap");
            WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Staff));
            Check(f.hero.CurrentTarget==null && f.combat.CurrentHeroCombatState.AttackTimer>1f,"reset even when mode is unchanged");
            CombatStep(f,.001f);Check(f.combat.PendingProjectileCount==0,"no free same-family shot");
        });
        Run("weapon hidden magical affixes remain inactive and zero MagicalAttack cannot fabricate damage",()=>{
            GameManager gm;var f=WeaponField(out gm);var staff=WeaponInBag(gm,WeaponFamily.Staff);
            staff.mainStats[0].flat=0;staff.affixSchemaVersion=EquipmentFoundation.AffixSchemaVersion;
            staff.affixSlotCount=2;staff.extraStats.Add(new ItemStatLine {stat=CombatStatId.MagicalAttack,flat=9876,revealed=false});
            staff.extraStats.Add(new ItemStatLine {stat=CombatStatId.MagicalAttack,flat=8765,revealed=true,requiredRefineLevel=20});
            var preview=Compare(gm.HeroData,"gear:"+staff.instanceId);
            Near(preview.Find(CombatStatId.MagicalAttack).preview,0,"hidden and locked excluded");
            WearWeapon(gm,staff);f.Battle(f.Monster(-200));Tough(f);var shot=FireWeapon(f);
            Check(Get<DamageTrace>(shot,"trace").elementDamage==0,"zero source stays zero after roots");
            CombatStep(f,Get<float>(shot,"duration")+.001f);Check(f.Enemies[0].currentHP==10000,"no minimum-one phantom magic");
        });
        Run("weapon 24 explicit dev grant is separate from loot and never equips",()=>{
            GameManager gm;var f=WeaponField(out gm);Check(gm.GrantDevWeaponSamples(),"Editor dev grant");
            Check(gm.HeroData.equipmentItems.Count==2 && gm.HeroData.equipmentItems.Exists(x=>x.templateId==EquipmentFoundation.DemoBow) &&
                gm.HeroData.equipmentItems.Exists(x=>x.templateId==EquipmentFoundation.DemoStaff) &&
                WeaponBasicAttack.Equipped(gm.HeroData)==null && f.hero.attackMode==AttackMode.Melee,"two separate bag samples");
        });
    }
}
