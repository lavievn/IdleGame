using System;
using System.Collections;
using System.IO;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static object FirstShot(Field f) {return Get<IList>(f.combat,"projectiles")[0];}
    static void EmitHeroMagic(Field f)
    {
        f.hero.ChangeAttackMode(2);ReadySoundAttack(f,f.hero);CombatStep(f,.01f);
        Check(f.combat.PendingProjectileCount>0,"valid magic launch");
    }
    static int SparkCount(Field f) {return Get<IList>(Get<BattleEffects>(f.combat,"effects"),"sparks").Count;}
    static void KillInfo(Field f,CombatManager.ActiveMonsterInfo info)
    {
        Lethal(f,info);
    }
    static void MagicPersistenceTests()
    {
        Run("Magic retains launch endpoint after target death/null, same-wave AoE remains impact-only",()=>{
            var f=new Field();var target=f.Monster(-200);var near=f.Monster(-250);var far=f.Monster(-700);f.Battle(target,near,far);
            var nearby=f.Enemies[1];var outside=f.Enemies[2];int hp=nearby.currentHP;
            EmitHeroMagic(f);var shot=FirstShot(f);var end=Get<Vector2>(shot,"end");float duration=Get<float>(shot,"duration");
            Check(nearby.currentHP==hp,"no launch damage");KillInfo(f,f.Enemies[0]);
            Set(Get<object>(shot,"monster"),"controller",null); // Native destroyed-object semantics still need Unity.
            f.hero.ChangeAttackMode(0);CombatStep(f,.2f);Check(Bolts(f).Count==1,"target no longer required for graphic");
            Near(Get<Vector2>(shot,"end").x,end.x,"no retarget");Check(nearby.currentHP==hp,"no early AoE");
            CombatStep(f,duration-.2f+.001f);Check(nearby.currentHP<hp,"existing same-wave endpoint AoE preserved");
            Check(outside.currentHP==outside.maxHP,"not retargeted to distant enemy");Check(Bolts(f).Count==0 && SparkCount(f)>0,"terminal explosion then cleanup");
        });
        Run("Magic owner death continues as actor-free visual; pause/pan and terminal burst do not deal damage",()=>{
            var f=new Field();var target=f.Monster(-200);f.Battle(target);EmitHeroMagic(f);
            var bolt=Bolts(f)[0];float duration=Get<float>(FirstShot(f),"duration");
            f.combat.ApplyDamageToRegisteredHero(f.hero,1000000,"test");
            Check(f.combat.PendingProjectileCount==0 && f.combat.DetachedMagicVisualCount==1 && Bolts(f).Count==1,"combat gone, Magic still visible");
            int exp=f.combat.CurrentHeroCombatState.Data.currentExp;
            CombatStep(f,.2f);var p=bolt.position;Time.timeScale=0;CombatStep(f,5);Near(bolt.position.x,p.x,"pause X");Near(bolt.position.y,p.y,"pause Y");Time.timeScale=1;
            f.combat.PanEffects(15);Near(bolt.position.x,p.x+15,"pan applies once");CombatStep(f,.1f);
            Check(Bolts(f).Count==1,"continues without any living Hero");CombatStep(f,duration);
            Check(f.combat.DetachedMagicVisualCount==0 && Bolts(f).Count==0 && SparkCount(f)>0,"finite detached flight explodes");
            Check(f.combat.CurrentHeroCombatState.Data.currentExp==exp,"no detached EXP");
        });
        Run("enemy Magic source dies: graphic completes but old source cannot damage Hero",()=>{
            var f=new Field();var enemy=f.Monster(-200,AttackMode.RangedMagic);f.Battle(enemy);CombatStep(f,2);
            Check(f.combat.PendingProjectileCount==1,"enemy release");KillInfo(f,f.Enemies[0]);int hp=f.combat.CurrentHeroHP;
            CombatStep(f,.2f);Check(f.combat.PendingProjectileCount==0 && f.combat.DetachedMagicVisualCount==1 && Bolts(f).Count==1,"enemy source invalidates damage only");
            CombatStep(f,2);Check(f.combat.CurrentHeroHP==hp && Bolts(f).Count==0 && SparkCount(f)>0,"visual burst without stale damage");
        });
        Run("new wave/pool re-use retires old Magic without damaging new target or duplicating rewards",()=>{
            var f=new Field();var enemy=f.Monster(-200);f.Battle(enemy);EmitHeroMagic(f);var bolt=Bolts(f)[0];
            KillInfo(f,f.Enemies[0]); // Remove old life before the pool reuses it.
            Call(enemy,"OnDisable");enemy.gameObject.SetActive(true);Call(enemy,"OnEnable");enemy.attackMode=AttackMode.Melee;
            f.Battle(enemy);f.hero.ChangeAttackMode(0);int hp=f.Enemies[0].currentHP,exp=f.combat.CurrentHeroCombatState.Data.currentExp;
            Check(enemy.IsAlive && hp>0,"new pooled victim is a live eligible combatant");
            Check(f.combat.PendingProjectileCount==0 && f.combat.DetachedMagicVisualCount==1 && Bolts(f)[0]==bolt,"same graphic survives StartBattle");
            CombatStep(f,.2f);Check(Bolts(f).Count==1,"still in flight");CombatStep(f,2);
            Check(f.Enemies[0].currentHP==hp && f.combat.CurrentHeroCombatState.Data.currentExp==exp,"new wave immune to retired shot");
            Check(Bolts(f).Count==0 && f.combat.DetachedMagicVisualCount==0,"no leftover bolt");
        });
        Run("AttackMode change keeps released Magic and its original impact timing",()=>{
            var f=new Field();var enemy=f.Monster(-200);f.Battle(enemy);EmitHeroMagic(f);
            int hp=f.Enemies[0].currentHP;float duration=Get<float>(FirstShot(f),"duration");f.hero.ChangeAttackMode(0);
            CombatStep(f,duration-.01f);Check(Bolts(f).Count==1 && f.Enemies[0].currentHP==hp,"still awaiting original arrival");
            CombatStep(f,.011f);Check(Bolts(f).Count==0 && f.Enemies[0].currentHP<hp,"exact original impact window");
        });
        foreach(int killerIndex in new[]{0,2})
        Run("simultaneous Magic retires after final kill without double stepping or duplicate EXP / "+killerIndex,()=>{
            var f=new Field();var enemy=f.Monster(-200);f.Battle(enemy);
            EmitHeroMagic(f);EmitHeroMagic(f);EmitHeroMagic(f);
            var list=Get<IList>(f.combat,"projectiles");
            // Cover remaining shots that have already stepped this tick AND
            // those not stepped yet when the lethal callback retires them.
            for(int i=0;i<3;i++)Set(list[i],"duration",i==killerIndex?.1f:1f);
            int survivor=killerIndex==0?1:0;
            f.Enemies[0].currentHP=1;var survivorBolt=Bolts(f)[survivor];
            float expectedElapsed=Get<float>(list[survivor],"elapsed")+.1f;
            var path=Get<Vector2>(list[survivor],"end");var start=Get<Vector2>(list[survivor],"start");
            f.hero.ChangeAttackMode(0);CombatStep(f,.1f);
            Check(f.Enemies.Count==0 && f.combat.PendingProjectileCount==0 && f.combat.DetachedMagicVisualCount==2,"remaining volley retired, not deleted");
            Near(survivorBolt.position.x,start.x+(path.x-start.x)*expectedElapsed,"one step in callback frame");
            int exp=f.combat.CurrentHeroCombatState.Data.currentExp;CombatStep(f,1);
            Check(f.combat.CurrentHeroCombatState.Data.currentExp==exp && Bolts(f).Count==0,"no reward repeated by remaining visuals");
        });
        Run("detached Magic has bounded lifetime even with invalid/infinite configured duration",()=>{
            var f=new Field();var enemy=f.Monster(-200);f.Battle(enemy);EmitHeroMagic(f);
            Set(FirstShot(f),"duration",float.PositiveInfinity);f.hero.HideHero();CombatStep(f,.1f);
            Check(f.combat.DetachedMagicVisualCount==1,"orphan retained");CombatStep(f,9.8f);Check(Bolts(f).Count==1,"not removed too early");
            CombatStep(f,.11f);Check(Bolts(f).Count==0 && f.combat.DetachedMagicVisualCount==0,"10 gameplay-second orphan cap");
        });
        Run("old battle epoch cannot keep damage eligibility",()=>{
            var f=new Field();var enemy=f.Monster(-200);f.Battle(enemy);EmitHeroMagic(f);
            Set(FirstShot(f),"battleVersion",-1);int hp=f.Enemies[0].currentHP;f.hero.ChangeAttackMode(0);
            CombatStep(f,.2f);Check(f.combat.DetachedMagicVisualCount==1,"epoch mismatch retires");CombatStep(f,2);
            Check(f.Enemies[0].currentHP==hp && Bolts(f).Count==0,"old epoch is visual only");
        });
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        using(var sprites=new SpriteResources())
        {
            foreach(string route in new[]{"Load","Travel","Retry","New","Continue","Menu","Reset","Escape"})
            Run("Magic hard map entry or soft wave recovery boundary / "+route,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);
                    // This spawn fixture starts monsters offscreen, before
                    // they approach. Put one existing actor in valid cast range.
                    var target=f.field.Enemies[0].controller;
                    f.field.space.SetPosition(target.Rect,f.field.space.Position(f.field.hero.heroRect)+new Vector2(-200,0));
                    target.attackMode=AttackMode.Melee;EmitHeroMagic(f.field);
                    string one=File.ReadAllText(f.Slot("ManualSave1")),two=File.ReadAllText(f.Slot("ManualSave2"));
                    if(route=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"Load");
                    else if(route=="Travel")Check(f.manager.TryTravelToWorldMap(EntryNeighbor(f.manager)),"Travel");
                    else if(route=="Retry") {f.field.hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked();}
                    else if(route=="New" || route=="Continue")EntryTitle(f,route=="New");
                    else if(route=="Menu")Check(f.manager.ReturnToMainMenu(),"Menu");
                    else if(route=="Reset") {f.manager.OnResetClicked();f.manager.OnConfirmResetClicked();}
                    else Call(f.manager,"RecoverEscapedWave");
                    Check(f.field.combat.PendingProjectileCount==0,"old damage always canceled");
                    if(route=="Escape")Check(f.field.combat.DetachedMagicVisualCount==1 && Bolts(f.field).Count==1,"same-map recovery preserves visual");
                    else Check(f.field.combat.DetachedMagicVisualCount==0 && Bolts(f.field).Count==0,"hard map/life transition clears visuals");
                    Check(File.ReadAllText(f.Slot("ManualSave1"))==one && File.ReadAllText(f.Slot("ManualSave2"))==two,"manual slots byte-identical");
                }
            });
        }
        Resources.Unregister("Spawn/Appear");
    }
}
