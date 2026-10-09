using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static IList ShotList(Field f) { return (IList)typeof(CombatManager).GetField("projectiles",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(f.combat); }
    static T ShotField<T>(object shot,string name) { return (T)shot.GetType().GetField(name).GetValue(shot); }
    static void SetShot(object shot,string name,object value) { shot.GetType().GetField(name).SetValue(shot,value); }
    static void Tough(Field f)
    {
        foreach(var e in f.Enemies) {e.currentHP=e.maxHP=10000;e.data.race=RaceType.YeuThu;e.data.spiritRoots=new List<ElementType>{ElementType.Vo};e.data.rootTiers=new List<int>{3};e.data.rootWeights=new List<float>{1};}
    }
    static void Patch54bTests()
    {
        Run("both sides strike immediately on horizontal range entry despite different Y",()=>{
            var f=new Field();var m=f.Monster(-30,AttackMode.Melee,90);f.Battle(m);Tough(f);
            int heroHP=f.combat.CurrentHeroHP;CombatStep(f,.01f);
            Check(f.Enemies[0].currentHP<10000&&f.combat.CurrentHeroHP<heroHP,"both strike without waiting for movement or lane alignment");
            int hp=f.Enemies[0].currentHP;CombatStep(f,.1f);Check(f.Enemies[0].currentHP==hp,"melee cooldown enforced");
        });
        Run("bow per-shot factor stays in 55 to 80 percent and appears in damage trace",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var m=f.Monster(-200);f.Battle(m);Tough(f);
            var h=Get<EntityDataSO>(f.combat,"runtimeHeroData");h.baseDamage=100;h.spiritRoots=new List<ElementType>{ElementType.Vo};
            float lo=1,hi=0;
            for(int i=0;i<200;i++){CombatStep(f,.7f);var shot=ShotList(f)[0];var tr=ShotField<DamageTrace>(shot,"trace");
                Check(tr.modeMultiplier>=.55f&&tr.modeMultiplier<=.8f,"bow reduction bounds");
                Check(tr.rolledDamage>=47&&tr.rolledDamage<=80,"separate old 85..100 percent variation");
                Check(tr.Describe(1,1,tr.elementDamage).Contains("kiểu đánh ×"),"Info contains factor");lo=Math.Min(lo,tr.modeMultiplier);hi=Math.Max(hi,tr.modeMultiplier);
                Set(f.combat,"heroAttackTimer",0f);typeof(CombatManager).GetMethod("ClearProjectiles",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(f.combat,new object[]{true});}
            Check(lo<.57f&&hi>.78f,"random factor varies across intended range");
        });
        Run("moving physical target is missed at fixed launch point, X and Y",()=>{
            foreach(bool vertical in new[]{false,true}){
                var f=new Field();f.hero.ChangeAttackMode(1);var m=f.Monster(-200);f.Battle(m);Tough(f);CombatStep(f,.01f);
                var end=ShotField<Vector2>(ShotList(f)[0],"end");f.space.SetPosition(m.Rect,new Vector2(vertical?-200:-160,vertical?30:0));
                CombatStep(f,.2f);Check(f.Enemies[0].currentHP==10000,"moving target escaped hit tolerance");Check(f.combat.PendingProjectileCount==0,"shot resolves once");
                Near(end.x,-200,"launch endpoint captured");
            }
        });
        Run("physical stationary target hit after camera pan across Canvas scales",()=>{
            foreach(float scale in new[]{.25f,1f,2f}){
                var f=new Field(scale);f.hero.ChangeAttackMode(1);var m=f.Monster(-200);f.Battle(m);Tough(f);CombatStep(f,.01f);
                var shot=ShotList(f)[0];f.combat.PanEffects(37);f.space.SetPosition(m.Rect,new Vector2(-163,0));f.space.SetPosition(f.hero.heroRect,new Vector2(37,0));
                Near(ShotField<Vector2>(shot,"end").x,-163,"endpoint shares camera pan");CombatStep(f,.2f);Check(f.Enemies[0].currentHP<10000,"pan does not create a miss");
            }
        });
        Run("enemy bow can miss a moving hero and uses same damage reduction",()=>{
            var f=new Field();var m=f.Monster(-200,AttackMode.RangedPhysical);f.Battle(m);CombatStep(f,.01f);
            var tr=ShotField<DamageTrace>(ShotList(f)[0],"trace");Check(tr.modeMultiplier>=.55f&&tr.modeMultiplier<=.8f,"monster bow nerf");
            int hp=f.combat.CurrentHeroHP;f.space.SetPosition(f.hero.heroRect,new Vector2(-30,0));CombatStep(f,.2f);Check(f.combat.CurrentHeroHP==hp,"enemy shot misses");
        });
        Run("magic charges before first cast, launches only one enlarged fireball",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);f.Battle(f.Monster(-200),f.Monster(-250),f.Monster(-300));Tough(f);
            CombatStep(f,1);Check(f.combat.PendingProjectileCount==0,"half charge no cast");
            var bar=Get<MagicChargeBar>(f.hero,"chargeBar");Check(bar.Root.gameObject.activeSelf,"magic bar visible");Near(bar.Fill.fillAmount,.5f,"half charge");
            CombatStep(f,1);Check(f.combat.PendingProjectileCount==1,"one projectile for three enemies");Near(Bolts(f)[0].size,20,"old radius5 increased300 percent");
            var tr=ShotField<DamageTrace>(ShotList(f)[0],"trace");Near(tr.modeMultiplier,2.5f,"magic damage multiplier");Near(bar.Fill.fillAmount,0,"reset at launch");
            CombatStep(f,1.2f);Check(f.combat.PendingProjectileCount==0,"no next cast before full charge");Near(bar.Fill.fillAmount,.6f,"next charge overlaps flight");
        });
        Run("magic charges on approach and holds full until an enemy enters range",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-480);f.Battle(m);Tough(f);
            CombatStep(f,2);Check(f.combat.PendingProjectileCount==0,"out of range does not fire");var bar=Get<MagicChargeBar>(f.hero,"chargeBar");Near(bar.Fill.fillAmount,1,"ready held");
            f.space.SetPosition(m.Rect,new Vector2(-400,0));CombatStep(f,.01f);Check(f.combat.PendingProjectileCount==1,"cast on entry");Near(bar.Fill.fillAmount,0,"cast resets bar");
        });
        Run("magic explosion uses radius100 at impact, including enemies outside shooter range",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var a=f.Monster(-400);var b=f.Monster(-490);var c=f.Monster(-501);var d=f.Monster(-400,AttackMode.Melee,101);f.Battle(a,b,c,d);Tough(f);
            CombatStep(f,2);Check(f.combat.PendingProjectileCount==1,"one target selected");CombatStep(f,1.2f);
            Check(f.Enemies[0].currentHP<10000&&f.Enemies[1].currentHP<10000,"impact radius hits second victim outside casting range");
            Check(f.Enemies[2].currentHP==10000&&f.Enemies[3].currentHP==10000,"outside circle untouched on X and Y");
        });
        Run("magic is fixed aim, moving primary can escape but a new arrival can be hit",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var a=f.Monster(-200);var b=f.Monster(-480);f.Battle(a,b);Tough(f);CombatStep(f,2);
            f.space.SetPosition(a.Rect,new Vector2(-50,0));f.space.SetPosition(b.Rect,new Vector2(-220,0));CombatStep(f,1.2f);
            Check(f.Enemies[0].currentHP==10000&&f.Enemies[1].currentHP<10000,"impact uses current victims around old endpoint");
        });
        Run("magic retains launch power and roots even if hero stats change in flight",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var a=f.Monster(-200);f.Battle(a);Tough(f);CombatStep(f,2);
            var tr=ShotField<DamageTrace>(ShotList(f)[0],"trace");int expected=SynergyMath.EvaluateSnapshot(tr,f.Enemies[0].data).elementDamage;
            var h=Get<EntityDataSO>(f.combat,"runtimeHeroData");h.baseDamage=10000;h.spiritRoots.Clear();h.spiritRoots.Add(ElementType.Hoa);CombatStep(f,1.2f);
            Check(f.Enemies[0].currentHP==10000-expected,"launch snapshot survives mutation");
        });
        Run("hero magic persists after primary dies while physical never retargets",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var a=f.Monster(-200);var b=f.Monster(-250);f.Battle(a,b);Tough(f);CombatStep(f,2);
            var victim=f.Enemies[0];typeof(CombatManager).GetMethod("HandleMonsterDeath",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(f.combat,new object[]{victim});
            CombatStep(f,1.2f);Check(f.Enemies.Count==1&&f.Enemies[0].currentHP<10000,"remaining enemy receives explosion");
        });
        Run("charge UI is blue, nonblocking, reused and hidden for other attacks and death",()=>{
            var f=new Field();var hpGO=new GameObject("HP",typeof(RectTransform),typeof(Image));hpGO.transform.SetParent(f.hero.heroRect,false);var hp=(RectTransform)hpGO.transform;
            hp.sizeDelta=new Vector2(50,10);hp.anchoredPosition=new Vector2(0,80);f.hero.hpFillImage=hpGO.GetComponent<Image>();
            f.hero.ChangeAttackMode(2);f.hero.UpdateChargeBar(true,.5f);var bar=Get<MagicChargeBar>(f.hero,"chargeBar");
            Check(bar.Fill.color.b>bar.Fill.color.r&&!bar.Fill.raycastTarget&&!bar.Root.GetComponent<Image>().raycastTarget,"blue UI does not intercept mouse");
            Near(bar.Root.anchoredPosition.y,92,"bar above HP");Near(((RectTransform)bar.Fill.transform).anchorMax.x,.5f,"visible half width");
            f.hero.ChangeAttackMode(1);Check(!bar.Root.gameObject.activeSelf&&bar.Fill.fillAmount==0,"hidden bow");f.hero.ChangeAttackMode(2);f.hero.UpdateChargeBar(true,.75f);Check(Get<MagicChargeBar>(f.hero,"chargeBar")==bar,"reuses single UI");f.hero.Die();Check(!bar.Root.gameObject.activeSelf,"hidden on death");
        });
        Run("monster magic charge matches its interval, pause freezes both bars and bolts",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-200,AttackMode.RangedMagic);f.Battle(m);CombatStep(f,1);
            var mb=Get<MagicChargeBar>(m,"chargeBar");Near(mb.Fill.fillAmount,1/CombatBalance.AttackInterval(m.attackMode,f.Enemies[0].data.baseAttackSpeed),"monster own charge speed");
            var hb=Get<MagicChargeBar>(f.hero,"chargeBar");float h=hb.Fill.fillAmount,v=mb.Fill.fillAmount;Time.timeScale=0;CombatStep(f,5);Near(hb.Fill.fillAmount,h,"hero pause");Near(mb.Fill.fillAmount,v,"monster pause");Check(f.combat.PendingProjectileCount==0,"no cast while paused");Time.timeScale=1;
            CombatStep(f,1);Check(f.combat.PendingProjectileCount==2,"each magic caster fires once");f.combat.ForceClearAllMonsters();Check(f.combat.PendingProjectileCount==0&&!hb.Root.gameObject.activeSelf&&!mb.Root.gameObject.activeSelf,"clear cancels UI and projectiles");
        });
        Run("enemy magic fixed endpoint can hit a nearby moved hero but miss beyond radius",()=>{
            foreach(float displacement in new[]{100f,101f}) {
                var f=new Field();var m=f.Monster(-200,AttackMode.RangedMagic);f.Battle(m);CombatStep(f,2);
                Check(f.combat.PendingProjectileCount==1,"enemy single magic cast");int hp=f.combat.CurrentHeroHP;
                f.space.SetPosition(f.hero.heroRect,new Vector2(displacement,0));CombatStep(f,1.2f);
                Check(displacement==100f?f.combat.CurrentHeroHP<hp:f.combat.CurrentHeroHP==hp,"radius boundary inclusive");
            }
        });
        Run("enemy source death cancels its projectile and stale pooled life cannot deal damage",()=>{
            var f=new Field();var m=f.Monster(-200,AttackMode.RangedMagic);f.Battle(m);CombatStep(f,2);int hp=f.combat.CurrentHeroHP;
            var victim=f.Enemies[0];typeof(CombatManager).GetMethod("HandleMonsterDeath",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(f.combat,new object[]{victim});
            int healed=f.combat.CurrentHeroHP;m.gameObject.SetActive(true);CombatStep(f,1.2f);
            Check(f.combat.PendingProjectileCount==0&&f.combat.CurrentHeroHP==healed,"dead shooter cannot hit after pooled activation");
        });
        Run("physical misses are recorded as zero damage without false hit formula",()=>{
            var f=new Field();var gm=f.combat.gameObject.AddComponent<GameManager>();Set(f.combat,"gameManager",gm);
            f.hero.ChangeAttackMode(1);var m=f.Monster(-200);f.Battle(m);CombatStep(f,.01f);Check(gm.DamageHistory.Length==0,"launch not logged as hit");
            f.space.SetPosition(m.Rect,new Vector2(-150,0));CombatStep(f,.2f);
            Check(gm.DamageHistory.Length==1&&gm.DamageHistory[0].Contains("sát thương 0")&&!gm.DamageHistory[0].Contains("làm tròn"),"miss has explicit zero record");
        });
        Run("next wave preserves hero charge but explicit reset clears it",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);CombatStep(f,1.5f);Near(Get<float>(f.combat,"heroAttackTimer"),1.5f,"charges between waves");
            f.Battle(f.Monster(-200));CombatStep(f,.5f);Check(f.combat.PendingProjectileCount==1,"normal wave does not restart charge");f.combat.ForceClearAllMonsters();Near(Get<float>(f.combat,"heroAttackTimer"),0,"explicit reset clears");
        });
    }
}
