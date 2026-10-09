using System;
using System.IO;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void Patch54fTests()
    {
        Run("54f class HP scales origin but allocated HP stays additive",()=>{
            var d=HeroAt(1);
            Check(d.originHealth==400,"initial test origin HP");
            d.addedHealth=30;
            Check(d.GetCalculatedHealth(AttackMode.Melee)==830,"melee = 2 * origin + 30");
            Check(d.GetCalculatedHealth(AttackMode.RangedPhysical)==270,"bow = 0.6 * origin + 30");
            Check(d.GetCalculatedHealth(AttackMode.RangedMagic)==430,"magic = origin + 30");
            d.originHealth=600;d.ApplyHeroBalance();
            Check(d.GetCalculatedHealth(AttackMode.Melee)==1230&&d.GetCalculatedHealth(AttackMode.RangedPhysical)==390,
                "future custom origin remains composable");
        });
        Run("54f melee growth rolls only at level-up and round-trips through save",()=>{
            var d=HeroAt(1);UnityEngine.Random.InitState(2510);d.AddExp(216);
            Check(d.currentLevel==3&&d.meleeGrowthThroughLevel==3,"two level gains");
            Check(d.meleeGrowthBonus>=100&&d.meleeGrowthBonus<=200,"50..100 per level");
            int bonus=d.meleeGrowthBonus;d.ApplyHeroBalance();Check(d.meleeGrowthBonus==bonus,"no rebalance reroll");
            var save=new GameObject().AddComponent<SaveManager>();
            string dir=Path.Combine(Path.GetTempPath(),"idle-f-"+Guid.NewGuid());Directory.CreateDirectory(dir);
            try {
                Set(save,"saveDirectory",dir);Check(save.SaveGame(d,SaveSlot.AutoSave),"save");
                var loaded=HeroAt(1);Check(save.LoadGame(loaded,SaveSlot.AutoSave),"load");
                loaded.ApplyHeroBalance();Check(loaded.meleeGrowthBonus==bonus&&loaded.meleeGrowthThroughLevel==3,
                    "per-level rolls persist");loaded.ApplyHeroBalance();Check(loaded.meleeGrowthBonus==bonus,"load idempotent");
            } finally {Directory.Delete(dir,true);}
        });
        Run("54f old saves migrate completed melee growth levels deterministically",()=>{
            var d=new EntityDataSO {currentLevel=20,baseHealth=704,balanceVersion=1};
            d.ApplyHeroBalance();
            Check(d.originHealth==400&&d.meleeGrowthBonus==75*19&&d.meleeGrowthThroughLevel==20,
                "compensate 19 old levels once without RNG");
            int bonus=d.meleeGrowthBonus;d.ApplyHeroBalance();Check(d.meleeGrowthBonus==bonus,"no repeat");
        });
        Run("54f class healing damage and cooldown match spec",()=>{
            Check(CombatBalance.MeleeKillHeal(1,3)==3&&CombatBalance.MeleeKillHeal(1,5)==5,"level1 heal range");
            Check(CombatBalance.MeleeKillHeal(21,3)==6&&CombatBalance.MeleeKillHeal(21,5)==10,"5% per level");
            Near(CombatBalance.HeroHealthMultiplier(AttackMode.Melee),2,"melee twice base");
            Near(CombatBalance.HeroHealthMultiplier(AttackMode.RangedPhysical),.6f,"bow 60% base");
            Near(CombatBalance.ModeMultiplier(AttackMode.RangedPhysical,.55f),.7f,"bow exactly 70% damage");
            Near(CombatBalance.AttackInterval(AttackMode.Melee,1)/CombatBalance.AttackInterval(AttackMode.RangedPhysical,1),
                1.5f,"bow 150 percent fire rate");
            Near(CombatBalance.AttackInterval(AttackMode.RangedMagic,1),1.4f,"magic hero baseline");
            Near(CombatBalance.AttackInterval(AttackMode.RangedMagic,1,false,true),2f,"monster cast speed unchanged");
        });
        Run("54f class switch preserves percentage rather than healing free HP",()=>{
            var f=new Field();f.combat.SetupHeroInfo(HeroAt(1));Set(f.combat,"currentHeroHP",400);
            f.hero.ChangeAttackMode(1);f.combat.OnHeroAttackModeChanged();
            Check(f.combat.MaxHeroHP==240&&f.combat.CurrentHeroHP==120,"half HP in bow");
            f.hero.ChangeAttackMode(2);f.combat.OnHeroAttackModeChanged();
            Check(f.combat.MaxHeroHP==400&&f.combat.CurrentHeroHP==200,"half HP in magic");
            f.hero.ChangeAttackMode(0);f.combat.OnHeroAttackModeChanged();
            Check(f.combat.MaxHeroHP==800&&f.combat.CurrentHeroHP==400,"half HP back in melee");
        });
        Run("54f magic charges only while an eligible target exists",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);
            CombatStep(f,2);Near(Get<float>(f.combat,"heroAttackTimer"),0,"no wave/target no charge");
            var m=f.Monster(-200);f.Battle(m);CombatStep(f,.7f);
            Near(Get<float>(f.combat,"heroAttackTimer"),.7f,"charging with target");
            f.space.SetPosition(m.Rect,new Vector2(-550,0));CombatStep(f,.1f);
            Near(Get<float>(f.combat,"heroAttackTimer"),0,"cancel charge when outside range");
            f.space.SetPosition(m.Rect,new Vector2(-200,0));CombatStep(f,1.4f);
            Check(f.combat.PendingProjectileCount==1,"new full windup launches");
            Near(f.combat.heroMagicImpactRadius,250f,"hero Ground-local AoE 250");
            Near(f.combat.magicImpactRadius,100f,"monster AoE kept");
        });
        Run("54f combat HUD is never above start/confirmation/game-over/details",()=>{
            Check(!UIManager.ShouldShowCombatHUD(false,true,false,false,false),"start screen");
            Check(!UIManager.ShouldShowCombatHUD(true,false,true,false,false),"game over");
            Check(!UIManager.ShouldShowCombatHUD(true,false,false,true,false),"confirmation");
            Check(!UIManager.ShouldShowCombatHUD(true,false,false,false,true),"details/settings");
            Check(UIManager.ShouldShowCombatHUD(true,false,false,false,false),"normal gameplay visible");
        });
    }
}
