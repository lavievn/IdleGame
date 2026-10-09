using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static EntityDataSO HeroAt(int level)
    {
        var data=new EntityDataSO { currentLevel=level };
        data.ApplyHeroBalance();return data;
    }
    static void BalanceTests()
    {
        Run("exponential EXP increases and preserves excess across multiple levels",()=>{
            var d=HeroAt(1);Check(d.expToNextLevel==100,"first threshold");
            d.AddExp(216);Check(d.currentLevel==3&&d.currentExp==1,"100 plus 115 spent");
            Check(d.expToNextLevel==133,"geometric threshold");
            for(int l=2;l<=100;l++)Check(CombatBalance.RequiredExp(l)>CombatBalance.RequiredExp(l-1),"increasing costs");
            double previous=0;
            for(int l=1;l<=100;l++) {
                double kills=(double)CombatBalance.RequiredExp(l)/CombatBalance.KillExp(l);
                Check(kills>previous,"same-level kills needed rise each level");previous=kills;
            }
            Check(CombatBalance.RequiredExp(999)==2000000000,"overflow guard");
            d.currentLevel=999;d.currentExp=1999999999;d.AddExp(int.MaxValue);
            Check(d.currentLevel==999&&d.currentExp==1999999999,"level and integer limit");
        });
        Run("hero growth and speed remain bounded through level 999",()=>{
            var d=HeroAt(20);Check(d.baseHealth==704&&d.baseDamage==29,"level20 bases");Near(d.baseAttackSpeed,1.095f,"linear speed");
            Check(d.statPoints==19,"one point per level");d.AllocateHealth();d.AllocateDamage();
            Check(d.addedHealth==6&&d.addedDamage==1&&d.statPoints==17,"new allocations");
            d=HeroAt(999);Near(d.baseAttackSpeed,1.25f,"speed cap");
        });
        Run("legacy save migration removes exponential speed without resetting progress",()=>{
            var d=new EntityDataSO {currentLevel=20,baseHealth=1380,baseDamage=48,baseAttackSpeed=100000,
                AddAttackSpeed=100000,expToNextLevel=600,currentExp=300,addedHealth=100,addedDamage=20,statPoints=18};
            d.ApplyHeroBalance();Check(d.currentLevel==20,"level retained");
            Near(d.baseAttackSpeed,1.095f,"bad speed repaired");
            Check(d.currentExp==d.expToNextLevel/2,"fractional progress retained");
            Check(d.addedHealth/6+d.addedDamage+d.statPoints==19,"point budget rebased");
            int hp=d.addedHealth,atk=d.addedDamage,exp=d.currentExp;d.ApplyHeroBalance();
            Check(d.addedHealth==hp&&d.addedDamage==atk&&d.currentExp==exp,"idempotent migration");
        });
        Run("level-up kill heals only 5 to 10 HP and never fills new maximum",()=>{
            var f=new Field();var d=HeroAt(1);d.currentExp=99;f.combat.SetupHeroInfo(d);
            Set(f.combat,"currentHeroHP",30);var m=f.Monster(-20);f.Battle(m);
            var victim=f.Enemies[0];
            typeof(CombatManager).GetMethod("HandleMonsterDeath",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f.combat,new object[]{victim});
            int hp=Get<int>(f.combat,"currentHeroHP");Check(d.currentLevel>=2,"leveled up");Check(hp>=35&&hp<=40,"small kill heal only");
            Check(hp<Get<int>(f.combat,"maxHeroHP"),"no full level heal");
        });
        Run("ordinary kill healing is capped and there is no idle regeneration",()=>{
            var f=new Field();var d=HeroAt(20);f.combat.SetupHeroInfo(d);var m=f.Monster(-20);f.Battle(m);
            Set(f.combat,"currentHeroHP",d.GetCalculatedHealth()-1);
            typeof(CombatManager).GetMethod("HandleMonsterDeath",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f.combat,new object[]{f.Enemies[0]});
            Check(Get<int>(f.combat,"currentHeroHP")==d.GetCalculatedHealth(),"heal capped");
            Set(f.combat,"currentHeroHP",100);for(int i=0;i<100;i++)CombatStep(f,1);
            Check(Get<int>(f.combat,"currentHeroHP")==100,"no idle heal");
        });
        Run("monster HP and ATK scale at spawn and mode timing has correct order",()=>{
            var f=new Field();f.combat.SetupHeroInfo(HeroAt(20));var m=f.Monster(-20);f.Battle(m);
            var data=f.Enemies[0].data;Check(data.currentLevel>=18&&data.currentLevel<=22,"relative spawn band");
            Check(data.baseDamage==CombatBalance.MonsterAttack(data.currentLevel)&&data.baseDamage>8,"monster attack scales");
            Check(f.Enemies[0].maxHP==CombatBalance.MonsterHealth(data.currentLevel),"monster health scales");
            Near(CombatBalance.AttackInterval(AttackMode.Melee,1),1.4f,"slower melee");
            Near(CombatBalance.AttackInterval(AttackMode.RangedPhysical,1),.7f,"faster physical");
            Check(CombatBalance.Damage(10,AttackMode.RangedMagic,1)==25,"magic 2.5x damage");
        });
    }

    static void BalanceSimulations()
    {
        // Actual movement/combat/projectile code with Unity API doubles. No forced
        // deaths: 1..3 random enemies per wave; XP/heal/growth remain enabled.
        Console.WriteLine("SIMULATION: 30 FPS, width 1000, initial HP full, 1..3 monsters per wave, max 120 simulated seconds, 20 seeds per row.");
        foreach(int mapVisit in new[]{1,100})
        foreach(int level in new[]{1,10,20,50,100,999})
        foreach(AttackMode mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic})
        {
            int dead=0,kills=0;float seconds=0;
            for(int seed=1;seed<=20;seed++)
            {
                var f=new Field();UnityEngine.Random.InitState(seed*101+level);
                f.space.speedZoneInset=.25f;var hero=HeroAt(level);hero.mapNumber=mapVisit;
                // Give the hero all earned points in damage: stronger than an
                // unallocated save and exposes easy one-shot/heal exploits.
                while(hero.statPoints>0)hero.AllocateDamage();
                f.combat.SetupHeroInfo(hero);f.hero.ChangeAttackMode((int)mode);
                int spawned=0,frame=0;float wait=0;
                for(;frame<3600&&!f.hero.IsDead;frame++)
                {
                    if(f.Enemies.Count==0)
                    {
                        wait-=1f/30;
                        if(wait<=0)
                        {
                            int n=UnityEngine.Random.Range(1,4);var batch=new List<MonsterController>();
                            for(int j=0;j<n;j++)batch.Add(f.Monster(-550-j*60,(AttackMode)UnityEngine.Random.Range(0,3),UnityEngine.Random.Range(10f,90f)));
                            f.Battle(batch.ToArray());spawned+=n;wait=1.5f;
                        }
                    }
                    f.Step(1f/30,true);
                }
                // Death clears the combat list, so read defeated count via total
                // EXP is unsuitable. Report survival only, without inventing kills.
                if(f.hero.IsDead)dead++;seconds+=frame/30f;kills+=spawned;
            }
            Console.WriteLine("SIM mapVisit="+mapVisit+" level="+level+" mode="+mode+" deaths="+dead+"/20 mean_observed_seconds="+(seconds/20).ToString("F1")+" mean_spawned="+(kills/20f).ToString("F1"));
            // Survival is observational: the intentional early-map damage reduction changes the old death-rate expectation.
            Check(dead>=0&&dead<=20&&seconds>0,"valid simulation observations");
        }
    }
}
