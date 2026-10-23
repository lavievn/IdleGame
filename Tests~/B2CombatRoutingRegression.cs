using System;
using UnityEngine;
using TuTienCore;

// B2: test two real deployed controllers sharing one CombatManager.
// No second Hero is added to the production scene or saved in this phase.
partial class MotionRegression
{
    static HeroController B2Hero(Field f,float x,AttackMode mode)
    {
        var hero=new GameObject().AddComponent<HeroController>();
        hero.enableRangedLaneMotion=f.hero.enableRangedLaneMotion;
        hero.heroRect=(RectTransform)new GameObject(true).transform;
        hero.heroRect.parent=f.ground;
        Call(hero,"Awake");
        hero.attackMode=mode;
        hero.SpawnHero();
        hero.moveSpeed=0f;
        f.space.SetPosition(hero.heroRect,new Vector2(x,0f));
        return hero;
    }
    static EntityDataSO B2Data(string name,int health=10000)
    {
        return new EntityDataSO {
            entityName=name,baseHealth=health,baseDamage=10,
            expToNextLevel=1000000
        };
    }

    static void B2CombatRoutingTests()
    {
        Run("B2 registers separate controllers, HP, attack timers and data",()=>{
            var f=new Field();
            var second=B2Hero(f,-190,AttackMode.RangedPhysical);
            var data=B2Data("Second");
            var state=f.combat.RegisterAdditionalHero(second,data);
            Check(state!=null&&state!=f.combat.CurrentHeroCombatState,
                "additional Hero has unique combat state");
            Check(f.combat.StateFor(second)==state&&
                  f.combat.StateFor(f.hero)==f.combat.CurrentHeroCombatState,
                "lookup returns each controller's state");
            Check(f.combat.RegisteredHeroCount==2,"both participants registered");
            state.CurrentHP-=123;
            state.AttackTimer=2f;
            Check(f.combat.CurrentHeroHP==f.combat.MaxHeroHP,
                "companion injury does not reduce primary HP");
            Near(f.combat.CurrentHeroCombatState.AttackTimer,0,
                "companion timer does not advance primary timer");
            Check(state.Data==data,"companion uses its own data");
        });

        Run("B2 two Heroes can attack DIFFERENT nearby monsters",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-190f,AttackMode.RangedPhysical);
            var other=f.combat.RegisterAdditionalHero(second,B2Data("Archer"));
            var front=f.Monster(-20f,AttackMode.RangedMagic);
            var distant=f.Monster(-235f,AttackMode.RangedMagic);
            f.Battle(front,distant);
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(f.Enemies[0].currentHP<f.Enemies[0].maxHP,
                "primary melee hits close enemy");
            Check(f.combat.PendingProjectileCount==1,
                "secondary archer launches its own projectile");
            Check(other.AttackTimer>0f&&
                  f.combat.CurrentHeroCombatState.AttackTimer>0f,
                "both attack timers advance independently");
            Time.deltaTime=.2f;Call(f.combat,"Update");
            Check(f.Enemies[1].currentHP<f.Enemies[1].maxHP,
                "secondary physical projectile hits its selected distant enemy");
        });

        Run("B2 melee monster damages the ACTUAL closest Hero, not primary",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.Melee);
            var secondary=f.combat.RegisterAdditionalHero(second,B2Data("Front"));
            var monster=f.Monster(-230f,AttackMode.Melee);
            f.Battle(monster);
            monster.TickMovement(f.space,0f);
            Check(monster.CurrentTarget==second,"monster AI selected nearby teammate");
            int mainHP=f.combat.CurrentHeroHP,extraHP=secondary.CurrentHP;
            Time.deltaTime=.1f;Call(f.combat,"Update");
            Check(secondary.CurrentHP<extraHP,"teammate receives melee damage");
            Check(f.combat.CurrentHeroHP==mainHP,
                "primary takes no damage from teammate's attacker");
        });

        Run("B2 enemy ranged projectile NEVER switches target after firing",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.Melee);
            var secondary=f.combat.RegisterAdditionalHero(second,B2Data("Target"));
            var monster=f.Monster(-300f,AttackMode.RangedPhysical);
            f.Battle(monster);monster.TickMovement(f.space,0f);
            Check(monster.CurrentTarget==second,"ranged enemy locks companion");
            int primaryHP=f.combat.CurrentHeroHP,initial=secondary.CurrentHP;
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,"enemy projectile in flight");
            f.space.SetPosition(second.heroRect,new Vector2(-50f,0f));
            Time.deltaTime=.3f;Call(f.combat,"Update");
            Check(secondary.CurrentHP==initial,"projectile misses moved original target");
            Check(f.combat.CurrentHeroHP==primaryHP,
                "missed projectile is NOT retargeted to primary");
        });

        Run("B2 old projectile cannot hit a respawned companion life",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.Melee);
            var data=B2Data("Target");
            var state=f.combat.RegisterAdditionalHero(second,data);
            var monster=f.Monster(-300f,AttackMode.RangedPhysical);
            f.Battle(monster);monster.TickMovement(f.space,0f);
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,"old life has incoming shot");
            int previous=state.LifeVersion;
            f.combat.RegisterAdditionalHero(second,data);
            Check(state.LifeVersion==previous+1,"rebind increments life identity");
            Time.deltaTime=.3f;Call(f.combat,"Update");
            Check(state.CurrentHP==state.MaxHP,
                "projectile from previous life is discarded");
        });

        Run("B2 spawning the same Hero object again invalidates older shots",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.Melee);
            var state=f.combat.RegisterAdditionalHero(second,B2Data("Reborn"));
            var monster=f.Monster(-300f,AttackMode.RangedPhysical);
            f.Battle(monster);monster.TickMovement(f.space,0f);
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,"shot locked to original spawn");
            int before=second.SpawnVersion;
            second.SpawnHero();
            f.space.SetPosition(second.heroRect,new Vector2(-200f,0f));
            Check(second.SpawnVersion==before+1,"Hero spawn identity increments");
            Time.deltaTime=.3f;Call(f.combat,"Update");
            Check(state.CurrentHP==state.MaxHP,
                "reused Hero GameObject is not hit by old projectile");
        });

        Run("B2 pooled monster activation invalidates older physical shots",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.RangedPhysical);
            f.combat.RegisterAdditionalHero(second,B2Data("Archer"));
            var monster=f.Monster(-260f,AttackMode.RangedMagic);
            f.Battle(monster);
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,"physical shot is in flight");
            int before=monster.SpawnVersion;
            int hp=f.Enemies[0].currentHP;
            monster.MarkDead();
            Call(monster,"OnEnable");
            Check(monster.SpawnVersion==before+1,"pooled activation is a new life");
            Time.deltaTime=.3f;Call(f.combat,"Update");
            Check(f.Enemies[0].currentHP==hp,
                "shot at old monster lifetime cannot hit reactivated monster");
        });

        Run("B2 dead companion's pending shots are cancelled",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.RangedPhysical);
            var state=f.combat.RegisterAdditionalHero(second,B2Data("Archer"));
            var monster=f.Monster(-260f,AttackMode.RangedMagic);
            f.Battle(monster);
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,
                "companion projectile was launched");
            int hp=f.Enemies[0].currentHP;
            f.combat.ApplyDamageToRegisteredHero(second,1000000,"test");
            Check(state.CurrentHP==0&&second.IsDead,"shooter died");
            Time.deltaTime=.3f;Call(f.combat,"Update");
            Check(f.Enemies[0].currentHP==hp,
                "old projectile cannot deal damage from a dead shooter");
        });

        Run("B2 primary death does not end battle while teammate survives",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.Melee);
            var state=f.combat.RegisterAdditionalHero(second,B2Data("Survivor"));
            var monster=f.Monster(-220f,AttackMode.Melee);
            f.Battle(monster);
            Check(f.combat.ApplyDamageToRegisteredHero(f.hero,1000000,"test"),
                "primary can receive targeted damage");
            Check(f.hero.IsDead&&state.CanFight,
                "one death does not mark the surviving Hero dead");
            Check(f.Enemies.Count==1,
                "monsters not despawned when teammate remains alive");
            f.Step(.02f);
            Check(f.space.CurrentCameraFocusCount>=1,
                "camera still has surviving team and combat objects");
            Check(f.combat.ApplyDamageToRegisteredHero(second,1000000,"test"),
                "remaining companion can receive lethal damage");
            Check(second.IsDead&&f.Enemies.Count==0,
                "only FINAL Hero death clears the battle");
        });

        Run("B2 group focus smoothing resets on primary death and camera switches Hero",()=>{
            var f=new Field();f.space.useSoftZoneCamera=true;
            f.space.cameraUseCombatCenter=true;f.hero.moveSpeed=0f;
            var second=B2Hero(f,-190f,AttackMode.RangedPhysical);
            second.moveSpeed=0f;
            f.combat.RegisterAdditionalHero(second,B2Data("Camera survivor"));
            f.space.SetPosition(second.heroRect,new Vector2(-190f,65f));
            // B2Hero.SpawnHero follows the newly spawned companion. Track
            // primary again, as happens in a normal primary-led party.
            f.space.FollowHero(f.hero);
            f.Monster(-410f,AttackMode.RangedMagic);
            for(int i=0;i<12;i++)f.Step(.1f);
            var smoother=Get<CombatFocusSmoother>(f.space,"combatFocusSmoother");
            Check(Math.Abs(smoother.Offset)>20f,
                "group focus had nonzero offset before changing camera subject");
            Check(f.combat.ApplyDamageToRegisteredHero(f.hero,1000000,"test"),
                "the currently followed primary Hero can die");
            float survivorXBeforeCameraPan = f.space.Position(second.heroRect).x;
            f.Step(.1f);
            Check(f.hero.IsDead && second.IsDeployed,
                "companion remains alive and deployed");
            Check(f.space.CurrentCameraFocusCount==2,
                "new center includes survivor and monster, not dead Hero");
            Check(Math.Abs(smoother.Offset)<15f,
                "new camera subject resets the old focus offset before filtering");
            Near(f.space.CurrentCameraSmoothedFocusX,
                survivorXBeforeCameraPan+smoother.Offset,
                "new focus is computed relative to the new living Hero",.1f);
        });

        Run("B2 companion attack-mode change preserves HP ratio independently",()=>{
            var f=new Field();
            var second=B2Hero(f,-100f,AttackMode.Melee);
            var state=f.combat.RegisterAdditionalHero(second,B2Data("Dual",400));
            int max=state.MaxHP;
            state.CurrentHP=max/2;
            state.AttackTimer=2f;
            f.combat.CurrentHeroCombatState.AttackTimer=.4f;
            second.ChangeAttackMode(1);
            f.combat.OnRegisteredHeroModeChanged(second);
            Check(state.MaxHP==state.Data.GetCalculatedHealth(AttackMode.RangedPhysical),
                "companion HP adjusted to its own stance");
            Near(state.CurrentHP/(float)state.MaxHP,.5f,
                "companion health percentage preserved",.02f);
            Near(state.AttackTimer,0f,"companion windup reset");
            Near(f.combat.CurrentHeroCombatState.AttackTimer,.4f,
                "primary windup unchanged");
        });

        Run("B2 monster death credits EXP to actual Hero who fired the shot",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var secondary=B2Hero(f,-200f,AttackMode.RangedPhysical);
            var data=B2Data("Killer");
            f.combat.RegisterAdditionalHero(secondary,data);
            var monster=f.Monster(-240f,AttackMode.RangedMagic);
            f.Battle(monster);
            f.Enemies[0].currentHP=1;
            int primaryExp=f.combat.CurrentHeroCombatState.Data.currentExp;
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,
                "secondary launches lethal physical shot");
            Time.deltaTime=.25f;Call(f.combat,"Update");
            Check(f.Enemies.Count==0&&!monster.IsAlive,
                "secondary shot actually killed its target");
            Check(data.currentExp>0,"EXP credited to killer");
            Check(f.combat.CurrentHeroCombatState.Data.currentExp==primaryExp,
                "kill made by another Hero does not mutate primary XP");
        });

        Run("B2 escaped wave still recovers when ONLY teammate remains alive",()=>{
            var f=new Field();
            SaveManager save;string dir;
            var gm=WaveFixture(f,out save,out dir);
            try
            {
                var secondary=B2Hero(f,-200f,AttackMode.Melee);
                f.combat.RegisterAdditionalHero(secondary,B2Data("Survivor"));
                f.combat.ApplyDamageToRegisteredHero(f.hero,1000000,"test");
                Check(f.hero.IsDead&&f.combat.HasLivingHero,
                    "primary dead but wave must remain active");
                var escaped=f.Monster(700f,AttackMode.Melee);
                f.Battle(escaped);
                gm.activeMonsters.Add(escaped.gameObject);
                Call(gm,"LateUpdate");
                Check(Get<Coroutine>(gm,"nextWaveCoroutine")!=null,
                    "escaped-wave recovery still schedules replay");
                Check(f.hero.IsDead&&!f.hero.IsDeployed,
                    "replay must not resurrect dead primary");
                Check(secondary.IsDeployed,"survivor stays deployed");
            }
            finally { System.IO.Directory.Delete(dir,true); }
        });

        Run("B2 unregister prevents previously aimed shot hitting detached Hero",()=>{
            var f=new Field();f.hero.moveSpeed=0f;
            var second=B2Hero(f,-200f,AttackMode.Melee);
            var state=f.combat.RegisterAdditionalHero(second,B2Data("Temporary"));
            var monster=f.Monster(-300f,AttackMode.RangedPhysical);
            f.Battle(monster);monster.TickMovement(f.space,0f);
            Time.deltaTime=.01f;Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==1,"shot launched before unregister");
            Check(f.combat.UnregisterAdditionalHero(second),
                "companion unregister succeeded");
            int hp=state.CurrentHP;
            Time.deltaTime=.3f;Call(f.combat,"Update");
            Check(state.CurrentHP==hp,"detached Hero not damaged by old bullet");
            Check(f.combat.StateFor(second)==null&&f.combat.RegisteredHeroCount==1,
                "primary remains the only registered Hero");
        });
    }
}
