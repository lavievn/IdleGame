using System;
using UnityEngine;
using TuTienCore;

// Pure-runtime and integration regressions for the first B2 skill slice.
// No book UI, animations, permanent save or default skills are added.
partial class MotionRegression
{
    static SkillTierDefinition SkillTestTier(int tier,int bolts=1,
        bool protectedUltimate=false,float cast=1f,float release=1f,
        float cost1=40f,float cost9=10f,float cooldown1=5f,float cooldown9=1f,
        SkillTargetRule target=SkillTargetRule.LowestHealth,
        SkillHitRule hit=SkillHitRule.RetargetOnDeath,
        HitControlEffect control=default(HitControlEffect))
    {
        return new SkillTierDefinition("ThienKiem",tier,9,bolts,120f+10f*(tier-1),
            cost1,cost9,cooldown1,cooldown9,cast,release,750f,.1f,
            target,hit,protectedUltimate,0f,control);
    }
    static HeroSkillRuntime ReadySkill(SkillTierDefinition tier,float mana=200f,
        float regen=0f)
    {
        var runtime=new HeroSkillRuntime();
        runtime.RegisterTier(tier);
        Check(runtime.LearnSingleTier(tier.ArtId,tier.Tier),"skill book learned its exact tier");
        Check(runtime.Equip(tier.ArtId,tier.Tier),"learned tier can be equipped");
        runtime.ConfigureMana(mana,regen,true);
        return runtime;
    }
    static void SkillFoundationTests()
    {
        Run("Skill: single-tier book does not unlock earlier tiers",()=>{
            var runtime=new HeroSkillRuntime();
            runtime.RegisterTier(SkillTestTier(1));
            runtime.RegisterTier(SkillTestTier(3));
            runtime.RegisterTier(SkillTestTier(9,bolts:2,protectedUltimate:true));
            Check(runtime.LearnSingleTier("ThienKiem",3),"can learn standalone tier 3");
            var k=runtime.KnowledgeOf("ThienKiem");
            Check(k.IsUnlocked(3)&&!k.IsUnlocked(1)&&!k.IsUnlocked(9),
                "book tier 3 does not grant lower or higher tiers");
            Check(!runtime.Equip("ThienKiem",1),"cannot equip tier 1 from tier 3 book");
            Check(runtime.Equip("ThienKiem",3),"can equip tier 3");
            Check(!runtime.LearnCompleteManual("ThienKiem"),
                "incomplete prototype definitions cannot fake a full book");
            for(int i=2;i<=8;i++)if(i!=3)runtime.RegisterTier(SkillTestTier(i));
            Check(runtime.LearnCompleteManual("ThienKiem"),"complete book grants all nine tiers");
            for(int i=1;i<=9;i++)Check(k.IsUnlocked(i),"full book unlock "+i);
        });

        Run("Skill: every tier retains independent proficiency and experience",()=>{
            var runtime=new HeroSkillRuntime();
            var t1=SkillTestTier(1);
            var t2=SkillTestTier(2,cost1:60f,cost9:5f,cooldown1:6f,cooldown9:1f);
            runtime.RegisterTier(t1);runtime.RegisterTier(t2);
            runtime.LearnSingleTier("ThienKiem",1);
            runtime.LearnSingleTier("ThienKiem",2);
            var a=runtime.KnowledgeOf("ThienKiem").At(1);
            var b=runtime.KnowledgeOf("ThienKiem").At(2);
            a.GainExperience(80,level=>10);
            Check(a.Level==9&&b.Level==1,"tier 1 training does not affect tier 2");
            Near(t1.ManaCost(a.Level),10f,"tier1 lvl9 mana");
            Near(t1.Cooldown(a.Level),1f,"tier1 lvl9 cooldown");
            Near(t2.ManaCost(b.Level),60f,"tier2 lvl1 mana");
            Near(t2.Cooldown(b.Level),6f,"tier2 lvl1 cooldown");
            Check(runtime.Equip("ThienKiem",2),"switch to tier2");
            Check(runtime.KnowledgeOf("ThienKiem").At(1).Level==9,
                "switching tiers preserves earlier training");
            b.GainExperience(80,level=>10);
            Near(t2.ManaCost(b.Level),5f,"tier2 lvl9 mana");
            Near(t2.Cooldown(b.Level),1f,"tier2 lvl9 cooldown");
            Near(t2.CastSeconds,1f,"fixed cast never shortens with practice");
        });

        Run("Skill: chosen tier stays fixed when mana is insufficient",()=>{
            var runtime=new HeroSkillRuntime();
            var low=SkillTestTier(3,cost1:120f,cost9:5f);
            var high=SkillTestTier(9,cost1:140f,cost9:100f,protectedUltimate:true);
            runtime.RegisterTier(low);runtime.RegisterTier(high);
            runtime.LearnSingleTier("ThienKiem",3);runtime.LearnSingleTier("ThienKiem",9);
            runtime.ConfigureMana(200f,15f,true);
            Check(runtime.Equip("ThienKiem",3),"equip low tier first");
            Check(runtime.TryBegin(1,1),"spend 120 mana in a first cast");
            Near(runtime.CurrentMana,80f,"low tier cast leaves only 80 mana");
            runtime.Advance(1f);
            runtime.Advance(1f);
            Check(runtime.Equip("ThienKiem",9),"player manually chooses tier 9");
            Check(!runtime.CanBegin&&runtime.Equipped.Tier==9,
                "insufficient mana never selects learned tier 3 automatically");
            runtime.Advance(4f);
            Check(runtime.CanBegin,"mana and cooldown recover over time");
            runtime.Advance(100f);
            Near(runtime.CurrentMana,200f,"regeneration never exceeds maximum");
        });

        Run("Skill: mana remains private to each Hero state",()=>{
            var a=new HeroCombatState();var b=new HeroCombatState();
            a.BeginLife(new EntityDataSO(),1000);
            b.BeginLife(new EntityDataSO(),1000);
            a.Skills.ConfigureMana(100f,5f,true);
            b.Skills.ConfigureMana(200f,20f,true);
            var tier=SkillTestTier(3,cost1:50f);
            a.Skills.RegisterTier(tier);
            a.Skills.LearnSingleTier("ThienKiem",3);
            a.Skills.Equip("ThienKiem",3);
            Check(a.Skills.TryBegin(a.LifeVersion,1),"first actor starts casting");
            Near(a.Skills.CurrentMana,50f,"first actor pays mana");
            Near(b.Skills.CurrentMana,200f,"second actor mana unchanged");
        });

        Run("Skill: protected ultimate is valid only at the highest tier",()=>{
            bool rejected=false;
            try { SkillTestTier(5,protectedUltimate:true); }
            catch(ArgumentException) { rejected=true; }
            Check(rejected,"tier 5 cannot grant ultimate-only protection");
            var final=ReadySkill(SkillTestTier(9,protectedUltimate:true));
            Check(final.TryBegin(1,1)&&final.IsDamageImmune&&final.IsControlImmune,
                "ultimate protection applies as soon as casting starts");
            var ordinary=ReadySkill(SkillTestTier(3));
            ordinary.TryBegin(1,1);
            Check(!ordinary.IsDamageImmune&&!ordinary.IsControlImmune,
                "regular skills never inherit the ultimate's protection");
        });

        Run("Skill: immunity covers fixed windup PLUS full repeated release",()=>{
            var runtime=ReadySkill(SkillTestTier(9,bolts:5,protectedUltimate:true,
                cast:3f,release:4f));
            Check(runtime.TryBegin(17,33),"ultimate begins");
            Check(runtime.ActiveCast.LifeVersion==17 &&
                runtime.ActiveCast.SpawnVersion==33,"cast records its caster life");
            var step=runtime.Advance(2.99f);
            Check(step.Emissions==0&&runtime.IsDamageImmune,
                "still protected without emitting during 3-second cast");
            step=runtime.Advance(.01f);
            Check(step.BeganRelease&&step.Emissions==1&&runtime.IsDamageImmune,
                "first sword emits after full 3 seconds");
            step=runtime.Advance(2f);
            Check(step.Emissions==2&&runtime.IsDamageImmune,
                "two further swords during the protected release");
            step=runtime.Advance(1.99f);
            Check(step.Emissions==1&&runtime.IsDamageImmune,
                "protection remains during final part of barrage");
            step=runtime.Advance(.01f);
            Check(step.Emissions==1&&step.Completed && !runtime.IsDamageImmune &&
                !runtime.IsControlImmune && !runtime.IsExecuting,
                "final sword ends protection precisely at release end");
        });

        Run("Skill: cancel and new life cannot leak the invulnerable state",()=>{
            var runtime=ReadySkill(SkillTestTier(9,protectedUltimate:true,cast:3f));
            runtime.TryBegin(1,1);
            float spent=runtime.CurrentMana;
            runtime.Cancel();
            Check(!runtime.IsDamageImmune&&!runtime.IsControlImmune&&!runtime.IsExecuting,
                "cancel clears protection instantly");
            Near(runtime.CurrentMana,spent,"cancel does not silently refund paid mana");
            var state=new HeroCombatState();
            state.BeginLife(new EntityDataSO(),100);
            var skill=state.Skills;
            skill.RegisterTier(SkillTestTier(9,protectedUltimate:true));
            skill.LearnSingleTier("ThienKiem",9);
            skill.Equip("ThienKiem",9);
            skill.ConfigureMana(200f,0f,true);
            skill.TryBegin(state.LifeVersion,1);
            Check(state.IsDamageImmune,"the old life was protected");
            state.BeginLife(new EntityDataSO(),100);
            Check(!state.IsDamageImmune && !state.IsControlImmune &&
                !skill.IsExecuting,"new life resets old immunity");
            Check(skill.KnowledgeOf("ThienKiem").IsUnlocked(9),
                "new life does not erase learned martial art in runtime");
        });

        Run("Skill: switching tiers cannot bypass shared martial-art cooldown",()=>{
            var rt=ReadySkill(SkillTestTier(9,bolts:2,protectedUltimate:true,
                cast:1f,release:1f,cooldown1:6f,cooldown9:3f));
            rt.RegisterTier(SkillTestTier(3,cast:1f,release:1f));
            rt.LearnSingleTier("ThienKiem",3);
            rt.TryBegin(1,1);
            Check(!rt.Equip("ThienKiem",3),"cannot change tier during cast");
            rt.Advance(1f); // cooldown begins when the first sword is released
            rt.Advance(1f); // release complete, cooldown has 5 seconds left
            Check(rt.Equip("ThienKiem",3),"may manually switch tiers after release");
            Check(!rt.CanBegin&&rt.CooldownRemaining("ThienKiem")>0f,
                "tier3 shares the martial art's remaining cooldown");
            rt.Advance(5f);
            Check(rt.CanBegin,"tier3 can cast only when shared cooldown is over");
        });

        Run("Skill B2: damage API rejects attacks during protected cast/release",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var monster=f.Monster(-180f,AttackMode.RangedMagic);
            f.Battle(monster);
            var state=f.combat.CurrentHeroCombatState;
            state.Skills.RegisterTier(SkillTestTier(9,bolts:3,
                protectedUltimate:true,cast:3f,release:2f));
            state.Skills.LearnSingleTier("ThienKiem",9);
            state.Skills.Equip("ThienKiem",9);
            state.Skills.ConfigureMana(200f,0f,true);
            Check(f.combat.TryCastEquippedSkill(f.hero),"B2 manager starts selected skill");
            int hp=state.CurrentHP;
            Check(!f.combat.ApplyDamageToRegisteredHero(f.hero,80,"test"),
                "incoming damage rejected during casting");
            Check(state.CurrentHP==hp,"no HP loss during casting");
            state.Skills.Advance(3.5f);
            Check(state.IsDamageImmune,"barrage remains protected");
            Check(!f.combat.ApplyDamageToRegisteredHero(f.hero,80,"test"),
                "incoming damage rejected during release");
            state.Skills.Advance(1.5f);
            Check(!state.IsDamageImmune,"end of release removes immunity");
            Check(f.combat.ApplyDamageToRegisteredHero(f.hero,80,"test"),
                "damage again works after release");
            Check(state.CurrentHP<hp,"HP decreases after ultimate ends");
        });

        Run("Skill B2: sword barrage targets weakest and redirects after death",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var weak=f.Monster(-170f,AttackMode.RangedMagic);
            var strong=f.Monster(-240f,AttackMode.RangedMagic);
            f.Battle(weak,strong);
            var state=f.combat.CurrentHeroCombatState;
            var tier=SkillTestTier(9,bolts:3,protectedUltimate:true,
                cast:.5f,release:1f,cost1:30f);
            state.Skills.RegisterTier(tier);
            state.Skills.LearnSingleTier("ThienKiem",9);
            state.Skills.Equip("ThienKiem",9);
            state.Skills.ConfigureMana(200f,0f,true);
            f.Enemies[0].currentHP=1; // Weakest-health priority independent of distance.
            var healthy=f.Enemies[1];
            int healthyHP=healthy.currentHP;
            Check(f.combat.TryCastEquippedSkill(f.hero),"begin fixed-tier ultimate");
            Check(state.IsDamageImmune,"ultimate enters protected casting");
            Time.deltaTime=.5f;Call(f.combat,"Update");
            Check(!weak.IsAlive&&f.Enemies.Count==1,
                "first guided sword eliminates lowest-HP monster");
            Time.deltaTime=.5f;Call(f.combat,"Update");
            Check(healthy.currentHP<healthyHP,
                "remaining swords seek another living enemy");
            Time.deltaTime=.5f;Call(f.combat,"Update");
            Check(!state.IsDamageImmune,"protection ends after final sword");
            Check(state.Skills.CooldownRemaining("ThienKiem")>0f,
                "martial-art cooldown remains after barrage");
        });
        Run("Hit control: ordinary hit has cosmetic-only reaction, never stun",()=>{
            var target=new HitControlState();
            Check(!target.TryApply(default(HitControlEffect),0f),
                "normal zero-effect hit never causes real interruption");
            Check(!target.IsStaggered&&target.GraceRemaining==0f,
                "no hidden stun or grace timer from cosmetic damage");
        });

        Run("Hit control: real stagger is capped and cannot be chained indefinitely",()=>{
            var target=new HitControlState();
            var fromWeapon=new HitControlEffect(1f,.8f,0f);
            Check(target.TryApply(fromWeapon,0f),"weapon proc applies");
            Near(target.StaggerRemaining,.25f,"maximum brief stagger cap");
            Check(!target.TryApply(fromWeapon,0f),"second hit cannot extend active stagger");
            target.Advance(.26f);
            Check(!target.IsStaggered,"stagger ends naturally");
            Check(!target.TryApply(fromWeapon,0f),"recovery immunity blocks immediate re-stun");
            target.Advance(.61f);
            Check(target.TryApply(fromWeapon,0f),"new proc only after grace expires");
        });

        Run("Hit control: probability, resistance and boss immunity are independent",()=>{
            var target=new HitControlState();
            var passive=new HitControlEffect(.3f,.16f,.6f);
            Check(!target.TryApply(passive,.5f),"missed proc does nothing");
            Check(target.TryApply(passive,.1f),"successful passive proc interrupts");
            target.Reset();
            target.StaggerResistance=1f;
            Check(!target.TryApply(passive,0f),"immune boss ignores miniature stagger");
            target.StaggerResistance=.5f;
            Check(!target.TryApply(passive,.2f),"half resistance halves proc chance");
            Check(target.TryApply(passive,.1f),"weaker resistance admits proc");
        });

        Run("Hit control: ultimate protection blocks stun for the entire release",()=>{
            var runtime=ReadySkill(SkillTestTier(9,bolts:3,
                protectedUltimate:true,cast:2f,release:3f));
            var state=new HitControlState();
            var onHit=new HitControlEffect(1f,.2f,0f);
            runtime.TryBegin(1,1);
            Check(!state.TryApply(onHit,0f,runtime.IsControlImmune),
                "casting protection blocks incoming CC");
            runtime.Advance(3f);
            Check(!state.TryApply(onHit,0f,runtime.IsControlImmune),
                "active sword volley is still immune");
            runtime.Advance(2f);
            Check(state.TryApply(onHit,0f,runtime.IsControlImmune),
                "normal CC returns exactly when release finishes");
        });

        Run("Hit control B2: weapon/passive can stun on a BASIC hit only",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var monster=f.Monster(-20f,AttackMode.RangedMagic);
            f.Battle(monster);
            var attacker=f.combat.CurrentHeroCombatState;
            attacker.BasicAttackControl=new HitControlEffect(1f,.15f,.6f);
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(monster.HitControl.IsStaggered,
                "passive on the normal melee attack can cause a real hit-stun");
            Check(!attacker.HitControl.IsStaggered,
                "the attacker does not accidentally stagger itself");
        });

        Run("Hit control B2: skill hit uses its own effect, not weapon basic proc",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var monster=f.Monster(-170f,AttackMode.RangedMagic);
            f.Battle(monster);
            var actor=f.combat.CurrentHeroCombatState;
            actor.BasicAttackControl=new HitControlEffect(1f,.2f,.6f);
            var skill=SkillTestTier(3,cast:.1f,release:.1f);
            actor.Skills.RegisterTier(skill);
            actor.Skills.LearnSingleTier("ThienKiem",3);
            actor.Skills.Equip("ThienKiem",3);
            actor.Skills.ConfigureMana(200f,0f,true);
            Check(f.combat.TryCastEquippedSkill(f.hero),"skill cast starts");
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(!monster.HitControl.IsStaggered,
                "skill without a control effect never borrows the weapon's basic proc");
            Time.deltaTime=.1f;Call(f.combat,"Update"); // complete release
            f.space.SetPosition(f.hero.heroRect,new Vector2(-150f,0f));
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(monster.HitControl.IsStaggered,
                "normal attack separately uses the passive weapon proc");
        });

        Run("Hit control B2: real enemy hit-stun stops Hero motion and resets on new life",()=>{
            var f=new Field();f.hero.moveSpeed=120f;
            var monster=f.Monster(-20f,AttackMode.Melee);
            f.Battle(monster);
            var attacker=f.Enemies[0];
            attacker.BasicAttackControl=new HitControlEffect(1f,.25f,.6f);
            monster.TickMovement(f.space,0f);
            Time.deltaTime=.1f;Call(f.combat,"Update");
            var heroState=f.combat.CurrentHeroCombatState;
            Check(heroState.HitControl.IsStaggered,"enemy can explicitly apply a real stun");
            float before=f.space.Position(f.hero.heroRect).x;
            f.hero.TickMovement(f.space,.1f);
            Near(f.space.Position(f.hero.heroRect).x,before,
                "Hero movement is suspended during actual stun");
            f.combat.SetupHeroInfo(new EntityDataSO {baseHealth=400});
            Check(!heroState.HitControl.IsStaggered &&
                heroState.HitControl.GraceRemaining==0f,
                "retry/new life clears previous stagger and resistance window");
        });

        Run("Hit control B2: reused pooled monster cannot inherit old stun",()=>{
            var f=new Field();var m=f.Monster(-200f);
            Check(m.HitControl.TryApply(new HitControlEffect(1f,.2f,.6f),0f),
                "monster suffers a skill stagger");
            Call(m,"OnEnable");
            Check(!m.HitControl.IsStaggered && m.HitControl.GraceRemaining==0f,
                "pooled monster activates without old lifetime's CC state");
        });

        Run("Hit reaction B2: ordinary damage emits visual cue without stagger",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            int reactions=0;
            f.hero.HitReactionRequested+=()=>reactions++;
            var m=f.Monster(-20f,AttackMode.Melee);
            f.Battle(m);m.TickMovement(f.space,0f);
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(reactions==1,"default monster basic hit requests one cosmetic cue");
            Check(!f.combat.CurrentHeroCombatState.HitControl.IsStaggered,
                "cosmetic hit does not stop the Hero's actions");
        });

        Run("Hit control B2: explicit skill effect can stagger while normal skills do not",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var monster=f.Monster(-170f,AttackMode.RangedMagic);
            f.Battle(monster);
            var state=f.combat.CurrentHeroCombatState;
            var skill=SkillTestTier(3,cast:.1f,release:.1f,
                control:new HitControlEffect(1f,.2f,.6f));
            state.Skills.RegisterTier(skill);
            state.Skills.LearnSingleTier("ThienKiem",3);
            state.Skills.Equip("ThienKiem",3);
            state.Skills.ConfigureMana(200f,0f,true);
            Check(f.combat.TryCastEquippedSkill(f.hero),"manual skill begins");
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(monster.HitControl.IsStaggered,
                "only a skill with an explicit effect applies crowd control");
            Check(f.Enemies[0].attackTimer==0f,
                "skill-induced stun interrupts the victim's attack cycle");
        });

    }
}
