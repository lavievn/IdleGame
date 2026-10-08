using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static void CombatStep(Field f, float dt) { Time.deltaTime=dt; Call(f.combat,"Update"); }
    static List<BattleEffects.Bolt> Bolts(Field f) { return Get<List<BattleEffects.Bolt>>(Get<BattleEffects>(f.combat,"effects"),"bolts"); }
    static void NewFeatureTests()
    {
        Run("new default zone insets each edge by quarter of original width",()=>{
            var f=new Field(); f.space.speedZoneInset=new EnvironmentManager().speedZoneInset;
            Near(f.space.ZoneLeftX,-227.5f,"27.25 percent left");Near(f.space.ZoneRightX,197.5f,"69.75 percent right");
            f.space.scrollSpeed=150; f.hero.moveSpeed=300;
            for(int i=0;i<180;i++)f.Step(1f/60f);
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneLeftX,"new left anchor",0.02f);
            f.hero.moveSpeed=0;
            for(int i=0;i<200;i++)f.Step(1f/60f);
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"new right anchor",0.02f);
            Near(f.space.CurrentCameraSpeed,0,"stopped at new edge");
        });
        Run("running background tracks actual hero movement independently of camera",()=>{
            foreach(float speed in new[]{75f,150f,300f,600f}) {
                var f=new Field();f.hero.moveSpeed=speed;f.Step(0.1f);
                Near(f.space.CurrentBackgroundSpeed,speed,"background speed");
                Near(f.space.CurrentCameraSpeed,150,"base camera stays independent");
            }
        });
        Run("stationary combat background and hero share camera displacement",()=>{
            var f=new Field();f.space.speedZoneInset=.25f;f.hero.moveSpeed=300;
            for(int i=0;i<180;i++)f.Step(1f/60f);
            f.Monster(f.space.ZoneLeftX-20);
            for(int i=0;i<200;i++) {
                float before=f.space.Position(f.hero.heroRect).x;f.Step(1f/60f);
                Near(f.space.CurrentBackgroundSpeed,f.space.CurrentCameraSpeed,"camera and background match",0.05f);
                Near((f.space.Position(f.hero.heroRect).x-before)*60,f.space.CurrentBackgroundSpeed,"hero and ground match",0.05f);
            }
            Near(f.space.CurrentBackgroundSpeed,0,"background stops at right");
            f.hero.Die();f.Step();Near(f.space.CurrentBackgroundSpeed,0,"corpse edge remains still");
        });
        Run("wave delay inverse speed has hard quarter floor",()=>{
            Near(BattleMotion.WaveDelay(1.5f,150),1.5f,"baseline");
            Near(BattleMotion.WaveDelay(1.5f,300),.75f,"double speed");
            Near(BattleMotion.WaveDelay(1.5f,600),.375f,"quadruple speed");
            Near(BattleMotion.WaveDelay(1.5f,6000),.375f,"floor");
            Near(BattleMotion.WaveDelay(1.5f,75),3f,"slow speed");
            Check(float.IsPositiveInfinity(BattleMotion.WaveDelay(1.5f,0)),"zero pauses");
        });
        Run("pending wave reacts to speed changes and pauses at zero",()=>{
            var f=new Field();var gm=new GameObject().AddComponent<GameManager>();
            Set(gm,"heroController",f.hero);Set(gm,"hasDeployed",true);
            var e=(IEnumerator)typeof(GameManager).GetMethod("WaitAndCallNextWave",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(gm,null);
            Check(e.MoveNext(),"starts waiting");Time.deltaTime=.15f;
            for(int i=0;i<5;i++)Check(e.MoveNext(),"half at base speed");
            f.hero.moveSpeed=0;for(int i=0;i<100;i++)Check(e.MoveNext(),"zero speed freezes pending progress");
            f.hero.moveSpeed=300;
            Check(e.MoveNext(),"70 percent");Check(e.MoveNext(),"90 percent");Check(!e.MoveNext(),"finishes using faster rate");
        });
        Run("physical shot damages one target only at impact",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var a=f.Monster(-200);var b=f.Monster(-250);f.Battle(a,b);f.hero.TickMovement(f.space,0);
            CombatStep(f,1f);Check(f.combat.PendingProjectileCount==1,"single physical projectile");
            Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"launch no damage");
            CombatStep(f,.09f);var bolt=Bolts(f)[0];Near(bolt.position.y,24,"physical flies straight");
            Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"flight no damage");
            CombatStep(f,.1f);Check(f.Enemies[0].currentHP<f.Enemies[0].maxHP,"impact damage");
            Check(f.Enemies[1].currentHP==f.Enemies[1].maxHP,"other target untouched");
            int hp=f.Enemies[0].currentHP;CombatStep(f,.1f);Check(f.Enemies[0].currentHP==hp,"exactly once");
        });
        Run("magic fans curved fireballs to in-range targets only",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var a=f.Monster(-200);var b=f.Monster(-300);var c=f.Monster(-480);f.Battle(a,b,c);f.hero.TickMovement(f.space,0);
            CombatStep(f,2f);Check(f.combat.PendingProjectileCount==2,"two in range");
            CombatStep(f,.6f);foreach(var bolt in Bolts(f))Check(bolt.magic&&bolt.position.y>50,"curved above straight path");
            Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"wait for arrival");
            CombatStep(f,.61f);Check(f.Enemies[0].currentHP<f.Enemies[0].maxHP&&f.Enemies[1].currentHP<f.Enemies[1].maxHP,"both arrived");
            Check(f.Enemies[2].currentHP==f.Enemies[2].maxHP,"off range untouched");
        });
        Run("flight duration remains below attack interval even at extreme attack speed",()=>{
            foreach(float interval in new[]{2f,1f,.1f,.001f}) {
                float flight=BattleMotion.FlightDuration(1.2f,interval);
                Check(flight>0&&flight<interval,"bounded flight");
            }
        });
        Run("projectile and particles pan with camera rather than independent scenery",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);
            CombatStep(f,2);CombatStep(f,.3f);var bolt=Bolts(f)[0];float x=bolt.position.x;
            Time.deltaTime=.1f;Call(f.space,"LateUpdate");Near(bolt.position.x-x,15,"bolt follows camera");
            CombatStep(f,.3f);Near(bolt.position.x,-85,"start and target both pan before interpolation",.01f);
        });
        Run("cleared shots cannot damage pooled monster reused in new wave",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);CombatStep(f,1);
            f.combat.ForceClearAllMonsters();Check(f.combat.PendingProjectileCount==0,"clear cancels");
            m.gameObject.SetActive(true);f.Battle(m);f.hero.TickMovement(f.space,0);CombatStep(f,.2f);
            Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"new life unharmed");
        });
        Run("death or hidden target cancels shots without retargeting",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var a=f.Monster(-200);var b=f.Monster(-250);f.Battle(a,b);f.hero.TickMovement(f.space,0);CombatStep(f,1);
            a.MarkDead();CombatStep(f,.2f);Check(f.combat.PendingProjectileCount==0,"dead target canceled");
            Check(f.Enemies[1].currentHP==f.Enemies[1].maxHP,"no retarget damage");
            f.hero.HideHero();CombatStep(f,.2f);Check(f.combat.PendingProjectileCount==0,"hidden hero clean");
        });
        Run("monster ranged attack also waits for projectile arrival",()=>{
            var f=new Field();var m=f.Monster(-200,AttackMode.RangedPhysical);f.Battle(m);m.TickMovement(f.space,0);
            int hp=Get<int>(f.combat,"currentHeroHP");CombatStep(f,1);
            Check(f.combat.PendingProjectileCount==1,"monster launched");Check(Get<int>(f.combat,"currentHeroHP")==hp,"no early damage");
            CombatStep(f,.2f);Check(Get<int>(f.combat,"currentHeroHP")<hp,"monster impact");
        });
        Run("lethal projectile clears battle safely during projectile iteration",()=>{
            var f=new Field();var m=f.Monster(-200,AttackMode.RangedPhysical);f.Battle(m);m.TickMovement(f.space,0);Set(f.combat,"currentHeroHP",1);
            CombatStep(f,1);CombatStep(f,.2f);Check(f.hero.IsDead,"ranged lethal hit");Check(f.combat.PendingProjectileCount==0&&f.Enemies.Count==0,"cleared after callback");
        });
        Run("procedural UI mesh has valid triangles and expires particles",()=>{
            var f=new Field();var fx=BattleEffects.Create(f.ground);
            Check(!fx.raycastTarget,"does not intercept UI");
            fx.Burst(new Vector2(10,20),false,1);fx.Burst(new Vector2(50,20),true,1);
            var a=fx.AddBolt(new Vector2(100,40),false,2);var b=fx.AddBolt(new Vector2(200,70),true,5);
            var vh=new VertexHelper();var populate=typeof(BattleEffects).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance);
            populate.Invoke(fx,new object[]{vh});Check(vh.currentVertCount>100,"generated geometry");
            foreach(int i in vh.triangles)Check(i>=0&&i<vh.currentVertCount,"valid index");
            foreach(var v in vh.vertices)Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y),"finite vertices");
            fx.RemoveBolt(a);fx.RemoveBolt(b);fx.Advance(1);populate.Invoke(fx,new object[]{vh});Check(vh.currentVertCount==0,"no leaked particles");
        });
    }
}
