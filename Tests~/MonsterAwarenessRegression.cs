using System;
using UnityEngine;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static void Look(MonsterController monster,bool right,float interval=5f)
    {
        typeof(MonsterController).GetMethod("SetFacing",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(monster,new object[]{right});
        Set(monster,"nextLook",interval);
    }
    static MonsterController IdleMonster(Field f,float x,AttackMode mode=AttackMode.Melee,float y=0f)
    {return f.Monster(x,mode,y,false);}
    static void MonsterAwarenessTests()
    {
        foreach(var mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic})
        Run("unaware "+mode+" stands on Ground, cannot attack or select a combat target",()=>{
            var f=new Field();var m=IdleMonster(f,-20,mode);f.Battle(m);
            float hp=f.combat.CurrentHeroHP;
            m.TickMovement(f.space,.2f,-31f);
            Near(f.space.Position(m.Rect).x,-51,"only Ground-camera compensation");
            Check(m.CurrentTarget==null && !m.CanAttack(f.hero,f.space),"no hidden ranged/melee attack while unaware");
            CombatStep(f,.2f);
            Near(f.combat.CurrentHeroHP,hp,"unaware monster did not damage Hero");
        });
        foreach(bool right in new[]{false,true})
        Run("sight uses facing half-plane, 500-unit Ground distance and living Heroes / "+right,()=>{
            var f=new Field();var m=IdleMonster(f,0);f.Battle(m);Look(m,right);
            float sign=right?1:-1;
            f.space.SetPosition(f.hero.heroRect,new Vector2(-sign*100,0));
            f.combat.TickMonsterAwareness(f.space,0f);Check(!m.HasDetectedHero,"behind cannot be seen");
            f.space.SetPosition(f.hero.heroRect,new Vector2(sign*501,0));
            f.combat.TickMonsterAwareness(f.space,0f);Check(!m.HasDetectedHero,"outside 500");
            f.space.SetPosition(f.hero.heroRect,new Vector2(sign*500,90));
            f.combat.TickMonsterAwareness(f.space,0f);Check(!m.HasDetectedHero,"distance includes Ground Y");
            f.space.SetPosition(f.hero.heroRect,new Vector2(sign*500,0));
            f.combat.TickMonsterAwareness(f.space,0f);
            Check(m.Awareness==MonsterAwareness.Suspicious,"inclusive 500 boundary");
        });
        Run("idle facing flips only after 1.5 to 5 seconds and never consumes combat RNG",()=>{
            var f=new Field();var m=IdleMonster(f,0);f.Battle(m);f.hero.HideHero();
            UnityEngine.Random.InitState(199);float expected=UnityEngine.Random.Range(0f,1f);UnityEngine.Random.InitState(199);
            for(int i=0;i<64;i++)
            {
                float delay=Get<float>(m,"nextLook");
                // Previous tick crossed its deadline by 1ms; include that
                // already elapsed fraction when checking the sampled interval.
                Check(delay+(i==0?0f:.001f)>=1.5f-.0001f && delay<=5f,"look interval bounds");
                bool facing=m.FacingRight;
                f.combat.TickMonsterAwareness(f.space,delay-.001f);Check(m.FacingRight==facing,"no early flip");
                f.combat.TickMonsterAwareness(f.space,.002f);Check(m.FacingRight!=facing,"flip at the random deadline");
            }
            Near(UnityEngine.Random.Range(0f,1f),expected,"cosmetic timer RNG is separate");
        });
        Run("one visual detection alerts the entire encounter with synchronized yellow ? then red !",()=>{
            var f=new Field();var a=IdleMonster(f,0);var b=IdleMonster(f,-900);var c=IdleMonster(f,1200);
            f.Battle(a,b,c);Look(a,true);Look(b,false);Look(c,true);
            f.space.SetPosition(f.hero.heroRect,new Vector2(300,0));
            f.combat.TickMonsterAwareness(f.space,.1f);
            foreach(var m in new[]{a,b,c})
            {
                Check(m.Awareness==MonsterAwareness.Suspicious,"all members see ? together regardless of range/facing");
                var label=Get<TextMeshProUGUI>(m,"awarenessLabel");Check(label.text=="?" && label.color.g>.5f,"yellow ?");
            }
            f.combat.TickMonsterAwareness(f.space,.49f);
            foreach(var m in new[]{a,b,c})Check(!m.IsAggressive,"no early attack");
            f.combat.TickMonsterAwareness(f.space,.01f);
            foreach(var m in new[]{a,b,c})Check(m.IsAggressive && Get<TextMeshProUGUI>(m,"awarenessLabel").text=="!","all engage at .5s");
            a.TickMovement(f.space,.1f,0f);Check(f.space.Position(a.Rect).x>0,"engaged monster approaches Hero");
        });
        Run("pause freezes facing and question delay, resume continues remaining time",()=>{
            var f=new Field();var m=IdleMonster(f,0);f.Battle(m);Look(m,true,1.5f);
            f.space.SetPosition(f.hero.heroRect,new Vector2(800,0));
            Time.timeScale=0;
            try{f.combat.TickMonsterAwareness(f.space,10f);Near(Get<float>(m,"nextLook"),1.5f,"paused facing timer");}
            finally{Time.timeScale=1;}
            f.space.SetPosition(f.hero.heroRect,new Vector2(100,0));f.combat.TickMonsterAwareness(f.space,0f);
            f.combat.TickMonsterAwareness(f.space,.2f);Time.timeScale=0;
            try{f.combat.TickMonsterAwareness(f.space,10f);Check(!m.IsAggressive,"paused ?");}
            finally{Time.timeScale=1;}
            f.combat.TickMonsterAwareness(f.space,.29f);Check(!m.IsAggressive,"remaining delay preserved");
            f.combat.TickMonsterAwareness(f.space,.01f);Check(m.IsAggressive,"resume finishes ?");
        });
        Run("surprise doubles only first accepted damage and the entire group counters immediately",()=>{
            var f=new Field();var a=IdleMonster(f,0);var b=IdleMonster(f,-900);f.Battle(a,b);
            f.Enemies[0].currentHP=1000;
            typeof(CombatManager).GetMethod("DealDamageToMonster",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(f.combat,new object[]{f.Enemies[0],17,null});
            Check(f.Enemies[0].currentHP==966 && a.IsAggressive && b.IsAggressive,"34 damage and immediate group counter");
            Check(Get<TextMeshProUGUI>(b,"awarenessLabel").text=="!","no question delay after attack");
            typeof(CombatManager).GetMethod("DealDamageToMonster",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(f.combat,new object[]{f.Enemies[0],17,null});
            Check(f.Enemies[0].currentHP==949,"second strike is normal");
        });
        Run("question already detected Hero so hit is normal but cancels remaining delay for all",()=>{
            var f=new Field();var a=IdleMonster(f,0);var b=IdleMonster(f,-900);f.Battle(a,b);Look(a,true);
            f.space.SetPosition(f.hero.heroRect,new Vector2(100,0));f.combat.TickMonsterAwareness(f.space,0f);
            f.Enemies[0].currentHP=1000;
            typeof(CombatManager).GetMethod("DealDamageToMonster",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(f.combat,new object[]{f.Enemies[0],17,null});
            Check(f.Enemies[0].currentHP==983 && a.IsAggressive && b.IsAggressive,"normal damage, immediate engagement");
        });
        Run("pooled life resets awareness and stale victim cannot double damage or wake new encounter",()=>{
            var f=new Field();var old=IdleMonster(f,0);f.Battle(old);var stale=f.Enemies[0];
            old.ReceiveAlert(f.hero,true);Call(old,"OnDisable");Call(old,"OnEnable");
            Check(!old.HasDetectedHero && Get<TextMeshProUGUI>(old,"awarenessLabel").gameObject.activeSelf==false,"pool resets marker/state");
            var fresh=IdleMonster(f,-300);f.Battle(fresh);
            typeof(CombatManager).GetMethod("DealDamageToMonster",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(f.combat,new object[]{stale,10,null});
            Check(!fresh.HasDetectedHero,"old victim cannot alert new wave");
        });
        Run("lethal surprise still alerts surviving members with saturated damage and unchanged one-time loot",()=>{
            GameManager gm;var f=LootField(true,out gm);var a=IdleMonster(f,0);var b=IdleMonster(f,-900);
            LootBattle(f,gm,a,b);var victim=f.Enemies[0];
            Lethal(f,victim);Check(!a.IsAlive && b.IsAggressive,"lethal hit wakes survivors");
            DeathCallback(f,victim);Lethal(f,victim);
            Check(gm.HeroData.equipmentItems.Count==1,"exactly one item despite duplicate lethal callback");
        });
        Run("missed physical projectile cannot reveal monsters or consume surprise",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Bow));
            var m=IdleMonster(f,-200);LootBattle(f,gm,m);var victim=f.Enemies[0];victim.currentHP=10000;
            var shot=FireWeapon(f);Check(!m.HasDetectedHero,"launch is not a hit");
            f.space.SetPosition(m.Rect,new Vector2(700,0));f.combat.CurrentHeroCombatState.AttackTimer=100;
            CombatStep(f,Get<float>(shot,"duration")+.01f);
            Check(victim.currentHP==10000 && !m.HasDetectedHero,"miss did not damage or alert");
        });
        Run("projectile surprise eligibility is decided on impact rather than on launch",()=>{
            GameManager gm;var f=WeaponField(out gm);WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Bow));
            var m=IdleMonster(f,-200);LootBattle(f,gm,m);var victim=f.Enemies[0];victim.currentHP=10000;
            var shot=FireWeapon(f);var trace=Get<DamageTrace>(shot,"trace");
            int expected=CombatDamage.ResolveBasicSnapshot(trace.elementDamage,trace.basicBoost,victim.data,AttackMode.RangedPhysical,f.combat.resistanceSettings);
            m.ReceiveAlert(f.hero,false);f.combat.CurrentHeroCombatState.AttackTimer=100;
            CombatStep(f,Get<float>(shot,"duration")+.01f);
            Check(victim.currentHP==10000-expected && m.IsAggressive,"detected during flight means normal damage at impact");
        });
    }
}
