using System;
using System.Reflection;
using UnityEngine;
using TuTienCore;

// B1 regression suite: verify two independent state objects BEFORE allowing
// CombatManager to control multiple live Heroes in B2.
partial class MotionRegression
{
    static void HeroCombatStateTests()
    {
        Run("B1 two HeroCombatState instances do not share HP or attack timers",()=>{
            var dataA=new EntityDataSO { entityName="A",baseHealth=400 };
            var dataB=new EntityDataSO { entityName="B",baseHealth=900 };
            var a=new HeroCombatState();
            var b=new HeroCombatState();
            a.BeginLife(dataA,400);
            b.BeginLife(dataB,900);
            a.CurrentHP=120;
            a.AttackTimer=1.7f;
            a.WindupMode=AttackMode.RangedMagic;
            Check(b.CurrentHP==900&&b.MaxHP==900,
                "taking damage on first actor cannot change second actor HP");
            Near(b.AttackTimer,0f,"first actor charging cannot charge second actor");
            Check(b.WindupMode==AttackMode.Melee,
                "first actor mode does not overwrite second actor mode");
            Check(a.Data==dataA&&b.Data==dataB&&a.Data!=b.Data,
                "combat states retain separate runtime data identities");
            b.CurrentHP=450;
            b.AttackTimer=.9f;
            b.WindupMode=AttackMode.RangedPhysical;
            Check(a.CurrentHP==120&&a.MaxHP==400,
                "modifying second actor preserves first actor health");
            Near(a.AttackTimer,1.7f,"independent cooldown after second actor changes");
            Check(a.WindupMode==AttackMode.RangedMagic,
                "independent attack mode after second actor changes");
        });

        Run("B1 BeginLife resets only bound state and leaves other Hero intact",()=>{
            var a=new HeroCombatState();
            var b=new HeroCombatState();
            a.BeginLife(new EntityDataSO {entityName="first"},300);
            b.BeginLife(new EntityDataSO {entityName="second"},650);
            a.CurrentHP=0;a.AttackTimer=5f;
            b.CurrentHP=321;b.AttackTimer=2.5f;
            var saveData=new EntityDataSO {entityName="first reloaded"};
            a.BeginLife(saveData,700);
            Check(a.Data==saveData&&a.CurrentHP==700&&a.MaxHP==700,
                "new deployment rebinds runtime data and refills only its state");
            Near(a.AttackTimer,0,"new deployment clears previous windup");
            Check(b.Data.entityName=="second"&&b.CurrentHP==321&&b.MaxHP==650,
                "unrelated Hero does not respawn or rebind");
            Near(b.AttackTimer,2.5f,"unrelated Hero windup not reset");
        });

        Run("B1 state reads each owner's active attack stance independently",()=>{
            var heroA=new GameObject().AddComponent<HeroController>();
            var heroB=new GameObject().AddComponent<HeroController>();
            var a=new HeroCombatState {Controller=heroA};
            var b=new HeroCombatState {Controller=heroB};
            heroA.attackMode=AttackMode.RangedPhysical;
            heroB.attackMode=AttackMode.RangedMagic;
            Check(a.SelectedMode==AttackMode.RangedPhysical,
                "state references own HeroController stance");
            Check(b.SelectedMode==AttackMode.RangedMagic,
                "second state has another attack mode");
            heroA.attackMode=AttackMode.Melee;
            Check(a.SelectedMode==AttackMode.Melee &&
                b.SelectedMode==AttackMode.RangedMagic,
                "changing one stance does not modify another");
        });

        Run("B1 existing CombatManager uses HeroCombatState as SINGLE HP/timer owner",()=>{
            var f=new Field();
            var state=f.combat.CurrentHeroCombatState;
            Check(state!=null&&state.Controller==f.hero,
                "Battle manager bound deployed main Hero controller");
            Check(state.CurrentHP==f.combat.CurrentHeroHP &&
                state.MaxHP==f.combat.MaxHeroHP,
                "public health API is backed by HeroCombatState");
            Check(state.Data!=null,"runtime Hero data remains bound");
            Check(typeof(CombatManager).GetField("currentHeroHP",
                BindingFlags.NonPublic|BindingFlags.Instance)==null,
                "old duplicate scalar HP field must not exist");
            Check(typeof(CombatManager).GetField("heroAttackTimer",
                BindingFlags.NonPublic|BindingFlags.Instance)==null,
                "old duplicate scalar cooldown field must not exist");
            int before=state.CurrentHP;
            Set(f.combat,"currentHeroHP",before-19);
            Check(state.CurrentHP==before-19&&f.combat.CurrentHeroHP==before-19,
                "historic HP regression helper reads/writes the new owner");
            Set(f.combat,"heroAttackTimer",1.25f);
            Near(state.AttackTimer,1.25f,"historic timer helper routes to state");
            f.combat.SetupHeroInfo(new EntityDataSO {baseHealth=900});
            Check(state.CurrentHP==f.combat.MaxHeroHP,
                "re-deploy refills state and existing public HUD values");
            Near(state.AttackTimer,0f,"setup clears timer on current owner");
        });

        Run("B1 mode changes preserve current HP ratio and reset only own windup",()=>{
            var f=new Field();
            var state=f.combat.CurrentHeroCombatState;
            f.combat.SetupHeroInfo(new EntityDataSO {
                baseHealth=400,baseDamage=10,expToNextLevel=1000000
            });
            f.hero.attackMode=AttackMode.Melee;
            f.combat.OnHeroAttackModeChanged();
            int maxBefore=state.MaxHP;
            state.CurrentHP=maxBefore/2;
            state.AttackTimer=1.5f;
            f.hero.attackMode=AttackMode.RangedPhysical;
            f.combat.OnHeroAttackModeChanged();
            int expectedMax=state.Data.GetCalculatedHealth(AttackMode.RangedPhysical);
            Check(state.MaxHP==expectedMax&&state.CurrentHP>0,
                "mode changes still recalculate per-stance max HP");
            Near(state.CurrentHP/(float)state.MaxHP,
                (maxBefore/2)/(float)maxBefore,
                "stance swap approximately preserves HP percentage",.02f);
            Near(state.AttackTimer,0f,
                "old windup cancelled when switching from melee to ranged");
            Check(f.combat.CurrentHeroHP==state.CurrentHP,
                "public legacy health property remains in sync");
        });

        Run("B1 current Hero death and respawn use the same state object",()=>{
            var f=new Field();
            var state=f.combat.CurrentHeroCombatState;
            var oldData=state.Data;
            Set(f.combat,"currentHeroHP",0);
            f.hero.Die();
            Check(state.CurrentHP==0&&f.hero.IsDead,"death retains zero HP");
            var fresh=new EntityDataSO {baseHealth=600,baseDamage=14};
            f.combat.SetupHeroInfo(fresh);
            f.hero.SpawnHero();
            Check(object.ReferenceEquals(state,f.combat.CurrentHeroCombatState),
                "retry reuses combat state instance");
            Check(state.Data==fresh&&state.Data!=oldData,
                "retry replaces only the previous runtime data reference");
            Check(state.CurrentHP==state.MaxHP&&state.MaxHP>0,
                "retry refills max HP without saving HP in EntityDataSO");
            Near(state.AttackTimer,0,"retry does not inherit attack timer");
        });
    }
}
