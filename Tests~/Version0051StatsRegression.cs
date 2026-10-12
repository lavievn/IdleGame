using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    private static CombatStatModifier S(CombatStatId stat,float flat,string source="test",
        ElementType element=ElementType.Vo)
    {
        return new CombatStatModifier {stat=stat,flat=flat,sourceId=source,element=element};
    }

    static void Version0051StatsTests()
    {
        Run("0.0.51 empty modifiers preserve legacy attack/HP",()=>{
            var hero=new EntityDataSO {baseHealth=100,baseDamage=20,addedHealth=10,addedDamage=5};
            Check(hero.GetCalculatedHealth(AttackMode.Melee)==210,"old melee HP");
            Check(hero.GetCalculatedHealth(AttackMode.RangedMagic)==110,"old magic HP");
            Check(hero.GetCalculatedDamage()==25,"old attack");
            Check(CombatDamage.ResolveLegacy(100,hero,new EntityDataSO(),AttackMode.Melee,
                new CombatResistanceSettings())==100,"old unarmored damage");
        });

        Run("0.0.51 tagged bonuses honor conditions and source removal",()=>{
            var hero=new EntityDataSO {baseDamage=100};
            var bow=S(CombatStatId.Attack,20,"hunter");
            bow.requiredTags.Add("stance:bow");
            bow.blockedTags.Add("terrain:desert");
            hero.AddCombatModifier(bow);
            Check(hero.GetCalculatedDamage(AttackMode.Melee)==100,"sword stance no bow bonus");
            Check(hero.GetCalculatedDamage(AttackMode.RangedPhysical)==120,"bow stance bonus");
            hero.characterTags.Add("terrain:desert");
            Check(hero.GetCalculatedDamage(AttackMode.RangedPhysical)==100,"condition disabled");
            hero.characterTags.Clear();
            Check(hero.RemoveModifiersBySource("hunter")==1,"remove one source");
            Check(hero.GetCalculatedDamage(AttackMode.RangedPhysical)==100,"no permanent stacking");
        });

        Run("0.0.51 mixed physical and fire magic calculate independently",()=>{
            var atk=new EntityDataSO {baseDamage=100};
            var target=new EntityDataSO();
            target.AddCombatModifier(S(CombatStatId.Armor,42.857143f));
            target.AddCombatModifier(S(CombatStatId.MagicResistance,25f));
            target.AddCombatModifier(S(CombatStatId.ElementResistance,.25f,"fire",ElementType.Hoa));
            var parts=new [] {
                new CombatDamagePart { school=DamageSchool.Physical,element=ElementType.Vo,
                    fixedDamage=200f },
                new CombatDamagePart { school=DamageSchool.Magical,element=ElementType.Hoa,
                    fixedDamage=150f }
            };
            var result=CombatDamage.Resolve(atk,target,AttackMode.Melee,parts);
            Near(result.afterDefense[0],140f,"armor on physical",.01f);
            Near(result.afterDefense[1],90f,"magic and fire resistance",.01f);
            Check(result.totalDamage==230,"sum AFTER separate resistance");
        });

        Run("0.0.51 conditional fire damage only boosts fire components",()=>{
            var atk=new EntityDataSO {baseDamage=100};
            atk.AddCombatModifier(S(CombatStatId.ElementDamagePercent,.2f,"flame",ElementType.Hoa));
            var parts=new [] {
                new CombatDamagePart { school=DamageSchool.Physical,element=ElementType.Vo,attackMultiplier=1f },
                new CombatDamagePart { school=DamageSchool.Magical,element=ElementType.Hoa,attackMultiplier=1f }
            };
            var output=CombatDamage.Resolve(atk,new EntityDataSO(),AttackMode.Melee,parts);
            Check(output.totalDamage==220,"100 neutral plus 120 fire");
        });

        Run("0.0.51 capped armor never makes an actor invulnerable",()=>{
            var d=new EntityDataSO();
            d.AddCombatModifier(S(CombatStatId.Armor,1000000f));
            var parts=new [] {new CombatDamagePart {school=DamageSchool.Physical,fixedDamage=1000f}};
            var result=CombatDamage.Resolve(null,d,AttackMode.Melee,parts);
            Check(result.totalDamage>=150,"school damage reduction capped at 85%");
        });

        Run("0.0.51 HP bonuses respect stance and preserve base data",()=>{
            var hero=new EntityDataSO {baseHealth=100,addedHealth=10};
            var m=S(CombatStatId.MaxHealth,50f,"test-item");
            hero.AddCombatModifier(m);
            Check(hero.GetCalculatedHealth(AttackMode.Melee)==260,"stance plus equipment");
            Check(hero.GetCalculatedHealth(AttackMode.RangedMagic)==160,"magic stance plus equipment");
            Check(hero.baseHealth==100&&hero.addedHealth==10,"base not modified");
            hero.RemoveModifiersBySource("test-item");
            Check(hero.GetCalculatedHealth(AttackMode.Melee)==210,"equipment removed");
        });

        Run("0.0.51 passive HP recovery keeps fractional remainder",()=>{
            var f=new Field();var state=f.combat.CurrentHeroCombatState;
            state.Data.AddCombatModifier(S(CombatStatId.HealthRegeneration,3f));
            state.CurrentHP-=10;int original=state.CurrentHP;
            Time.deltaTime=.5f;Call(f.combat,"Update");
            Check(state.CurrentHP==original+1,"first fractional tick");
            Time.deltaTime=.5f;Call(f.combat,"Update");
            Check(state.CurrentHP==original+3,"fractional remainder carried");
        });

        Run("0.0.51 move speed uses same runtime tags",()=>{
            var f=new Field();
            var data=f.combat.CurrentHeroCombatState.Data;
            var m=S(CombatStatId.MovementSpeed,40f);
            m.requiredTags.Add("terrain:forest");
            data.AddCombatModifier(m);
            Near(f.hero.EffectiveMoveSpeed(),f.hero.moveSpeed,"outside forest");
            data.characterTags.Add("terrain:forest");
            Near(f.hero.EffectiveMoveSpeed(),f.hero.moveSpeed+40f,"forest bonus");
        });
        Run("0.0.51 runtime max HP refresh preserves fraction",()=>{
            var f=new Field();var state=f.combat.CurrentHeroCombatState;
            state.CurrentHP=state.MaxHP/2;
            int originalMax=state.MaxHP;
            state.Data.AddCombatModifier(S(CombatStatId.MaxHealth,100f,"gear"));
            f.combat.RefreshRegisteredHeroStats(f.hero);
            Check(state.MaxHP==originalMax+100,"HP reflects new modifier");
            Near((float)state.CurrentHP/state.MaxHP,.5f,"ratio preserved",.001f);
            state.Data.RemoveModifiersBySource("gear");
            f.combat.RefreshRegisteredHeroStats(f.hero);
            Check(state.MaxHP==originalMax,"unequip returns previous maximum");
            Near((float)state.CurrentHP/state.MaxHP,.5f,"unequip cannot heal for free",.001f);
        });

        Run("0.0.51 incoming resistance checks incoming type not attacker stance",()=>{
            var defender=new EntityDataSO();
            var armored=S(CombatStatId.Armor,100f);
            armored.requiredTags.Add("incoming:physical");
            defender.AddCombatModifier(armored);
            var enemy=new EntityDataSO {baseDamage=100};
            var physical=new [] {new CombatDamagePart {fixedDamage=100,
                school=DamageSchool.Physical}};
            var magical=new [] {new CombatDamagePart {fixedDamage=100,
                school=DamageSchool.Magical}};
            Check(CombatDamage.Resolve(enemy,defender,AttackMode.RangedPhysical,physical)
                .totalDamage==50,"physical armor blocks half");
            Check(CombatDamage.Resolve(enemy,defender,AttackMode.RangedPhysical,magical)
                .totalDamage==100,"same bow mode magic portion bypasses armor");
        });

    }
}
