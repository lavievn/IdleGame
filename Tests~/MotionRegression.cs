using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
using TuTienCore;

partial class MotionRegression
{
    static int tests;
    static void Check(bool condition,string message) { if (!condition) throw new Exception(message); }
    static void Near(float a,float b,string message,float tolerance=0.002f) { Check(Math.Abs(a-b)<=tolerance,message+": "+a+" != "+b); }
    static void Run(string name,Action test) { test(); tests++; Console.WriteLine("PASS "+name); }
    static void Call(object value,string method) { value.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,null); }
    static void Set(object value,string field,object data) { value.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(value,data); }
    static T Get<T>(object value,string field) { return (T)value.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value); }

    class Field
    {
        public EnvironmentManager space;
        public HeroController hero;
        public RectTransform ground,grass;
        public MonsterSpawner spawner;
        public CombatManager combat;
        public Field(float scale=1f)
        {
            if (EnvironmentManager.Instance != null) Call(EnvironmentManager.Instance,"OnDestroy");
            HeroController.ActiveHeroes.Clear();MonsterController.ActiveMonsters.Clear();UnityEngine.Object.objects.Clear();
            ground=(RectTransform)new GameObject(true).transform;ground.sizeDelta=new Vector2(1000,100);ground.localScale=new Vector3(scale,scale,1);
            grass=(RectTransform)new GameObject(true).transform;grass.parent=ground;grass.anchoredPosition=new Vector2(600,0);grass.sizeDelta=new Vector2(200,100);
            space=new GameObject().AddComponent<EnvironmentManager>();space.speedZoneInset=0;Set(space,"battleArea",ground);Set(space,"groundDetails",new[]{grass});Call(space,"Awake");
            hero=new GameObject().AddComponent<HeroController>();hero.heroRect=(RectTransform)new GameObject(true).transform;hero.heroRect.parent=ground;hero.heroRect.anchorMin=hero.heroRect.anchorMax=new Vector2(1,0);hero.heroRect.pivot=new Vector2(1,0);Call(hero,"Awake");hero.SpawnHero();
            spawner=new GameObject().AddComponent<MonsterSpawner>();Set(spawner,"monsterPool",new ObjectPool<GameObject>(()=>new GameObject(),m=>{},m=>m.SetActive(false),m=>{},5,20));
            combat=new GameObject().AddComponent<CombatManager>();Call(combat,"Start");combat.SetupHeroInfo(new EntityDataSO { baseHealth=10000,baseDamage=10,expToNextLevel=1000000 });
        }
        public MonsterController Monster(float x,AttackMode mode=AttackMode.Melee,float y=0)
        {
            var g=new GameObject(true);g.transform.parent=ground;var m=g.AddComponent<MonsterController>();Call(m,"Awake");Call(m,"OnEnable");m.attackMode=mode;space.SetPosition(m.Rect,new Vector2(x,y));return m;
        }
        public void Step(float dt=0.1f,bool damage=false)
        {
            Time.deltaTime=dt;Time.time+=dt;Call(space,"Update");if(damage)Call(combat,"Update");Call(space,"LateUpdate");
        }
        public void Battle(params MonsterController[] monsters)
        {
            var list=new List<GameObject>();foreach(var m in monsters) list.Add(m.gameObject);
            combat.StartBattle(list,new EntityDataSO {baseHealth=10000,baseDamage=10});
        }
        public List<CombatManager.ActiveMonsterInfo> Enemies { get { return Get<List<CombatManager.ActiveMonsterInfo>>(combat,"activeMonsters"); } }
    }

    static void Main()
    {
        Run("clamp approach in both directions, including long frame",()=>{
            Near(BattleMotion.Approach(0,100,30,1000,1),70,"right");Near(BattleMotion.Approach(0,-100,30,1000,1),-70,"left");Near(BattleMotion.Approach(0,10,30,1000,1),0,"already in range");
        });
        Run("equal melee range at 0.2/0.5/1/2 Canvas scale",()=>{
            foreach(float scale in new[]{0.2f,0.5f,1f,2f}) { var f=new Field(scale);var m=f.Monster(-35);Check(f.hero.CanAttack(m,f.space),"hero scale "+scale);Check(m.CanAttack(f.hero,f.space),"monster scale "+scale);f.space.SetPosition(m.Rect,new Vector2(-36,0));Check(!f.hero.CanAttack(m,f.space)&&!m.CanAttack(f.hero,f.space),"outside range"); }
        });
        Run("camera pans all actors and ground equally",()=>{
            foreach(float scale in new[]{0.2f,1f,2f}) { var f=new Field(scale);var m=f.Monster(-300);f.hero.TickMovement(f.space,0);m.TickMovement(f.space,0);float h=f.space.Position(f.hero.heroRect).x,b=f.space.Position(m.Rect).x,g=f.space.Position(f.grass).x;Time.deltaTime=0.1f;Call(f.space,"LateUpdate");float pan=f.space.Position(f.hero.heroRect).x-h;Check(pan>0,"base camera pan continues");Near(f.space.Position(m.Rect).x-b,pan,"monster pan");Near(f.space.Position(f.grass).x-g,pan,"ground pan"); }
        });
        Run("offscreen monster advances right independently of world pan",()=>{
            var f=new Field();var m=f.Monster(-550);f.Step();Near(f.space.Position(m.Rect).x+550,f.space.Position(f.grass).x-100+15,"forward movement plus camera");Check(m.CurrentTarget==f.hero,"scans hero at spawn");
        });
        Run("ranged hero stops while melee monster keeps approaching",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var m=f.Monster(-300);f.hero.TickMovement(f.space,0.1f);float before=f.space.Position(f.hero.heroRect).x;f.hero.TickMovement(f.space,0.1f);Near(f.space.Position(f.hero.heroRect).x,before,"hero stays");m.TickMovement(f.space,0.1f);Near(f.space.Position(m.Rect).x,-285,"monster independent speed");
        });
        Run("another attacker cannot halve approaching monster speed",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var close=f.Monster(-20,AttackMode.RangedMagic);var far=f.Monster(-300);close.TickMovement(f.space,0.1f);far.TickMovement(f.space,0.1f);Check(close.currentState==MonsterState.Attacking,"attacker ready");Near(f.space.Position(far.Rect).x,-285,"far speed stays 150");
        });
        Run("melee hero approaches a stationary ranged monster",()=>{
            var f=new Field();var m=f.Monster(-200,AttackMode.RangedMagic);f.hero.TickMovement(f.space,0.1f);m.TickMovement(f.space,0.1f);Near(f.space.Position(f.hero.heroRect).x,-15,"hero advances");Near(f.space.Position(m.Rect).x,-200,"ranged monster stays");
        });
        Run("nearest target rescanned while approaching and retained during attack",()=>{
            var f=new Field();var a=f.Monster(-100);var b=f.Monster(-101);f.hero.TickMovement(f.space,0);f.space.SetPosition(b.Rect,new Vector2(-90,0));f.hero.TickMovement(f.space,0);Check(f.hero.CurrentTarget==b,"closer target selected while approaching");f.space.SetPosition(b.Rect,new Vector2(-30,0));f.hero.TickMovement(f.space,0);f.space.SetPosition(a.Rect,new Vector2(-20,0));f.hero.TickMovement(f.space,0);Check(f.hero.CurrentTarget==b,"in-range target retained during strike");b.MarkDead();f.hero.TickMovement(f.space,0);Check(f.hero.CurrentTarget==a,"reacquires on death");
        });
        Run("hidden hero freezes exploration; respawn recovers",()=>{
            var f=new Field();var m=f.Monster(-550);f.hero.HideHero();float x=f.space.Position(m.Rect).x;f.Step();Near(f.space.Position(m.Rect).x,x,"no hidden camera");Check(HeroController.ActiveHeroes.Count==0,"unregistered");f.hero.SpawnHero();Check(f.hero.IsDeployed,"respawn");Near(f.space.Position(f.hero.heroRect).x,f.space.HomeX,"spawn centre");
        });
        Run("hero death stops AI and retry clears dead state",()=>{
            var f=new Field();f.hero.Die();f.Step();Check(f.hero.IsDead&&!f.hero.IsDeployed,"dead stopped");f.hero.SpawnHero();Check(f.hero.IsDeployed&&!f.hero.IsDead,"retry alive");
        });
        Run("no crossing at low frame rate",()=>{
            var f=new Field();var m=f.Monster(-100);f.hero.TickMovement(f.space,2);m.TickMovement(f.space,2);Near(f.space.Position(f.hero.heroRect).x-f.space.Position(m.Rect).x,35,"stop at range");
        });
        Run("zone camera holds speed across 30/60/144 FPS",()=>{
            foreach(int fps in new[]{30,60,144}) {
                var f=new Field(); f.space.scrollSpeed=1000; f.hero.moveSpeed=1000;
                for(int i=0;i<fps*2;i++) f.Step(1f/fps);
                Near(f.space.Position(f.hero.heroRect).x,0,"equal speeds stay centred",0.02f);
                Near(f.space.CurrentCameraSpeed,1000,"base speed",0.02f);
            }
        });
        Run("wrapping waits for full decoration to exit and preserves overshoot",()=>{
            var f=new Field();f.space.SetPosition(f.grass,new Vector2(610,0));f.space.PanEnvironment(5);Near(f.space.Position(f.grass).x,615,"still partly visible within padding");f.space.PanEnvironment(10);Near(f.space.Position(f.grass).x,-615,"wrapped with overshoot");
        });
        Run("AoE cannot hit off-range monsters",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var near=f.Monster(-200);var far=f.Monster(-480);f.Battle(near,far);f.hero.TickMovement(f.space,0);Time.deltaTime=2;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"no damage before arrival");Time.deltaTime=1.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP<f.Enemies[0].maxHP,"near hit");Check(f.Enemies[1].currentHP==f.Enemies[1].maxHP,"far untouched");
        });
        Run("leaving range cancels windup before damage",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=1.9f;Call(f.combat,"Update");f.space.SetPosition(m.Rect,new Vector2(-480,0));Time.deltaTime=0.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"no ghost hit");Near(Get<float>(f.combat,"heroAttackTimer"),0,"windup reset");
        });
        Run("changing attack mode restarts windup",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-20);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=1.9f;Call(f.combat,"Update");f.hero.ChangeAttackMode(0);Time.deltaTime=0.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"no inherited magic timer");
        });
        Run("clear battle cancels timers and pooled respawn resets state",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=1.9f;Call(f.combat,"Update");f.combat.ForceClearAllMonsters();Near(Get<float>(f.combat,"heroAttackTimer"),0,"cleared timer");Check(MonsterController.ActiveMonsters.Count==0,"pool removed");m.gameObject.SetActive(true);Check(m.currentState==MonsterState.PassiveScroll&&m.CurrentTarget==null,"pool reset");f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=0.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"fresh windup");
        });
        Run("melee aligns Y, ranged stays in own lane",()=>{
            var f=new Field();var m=f.Monster(-35,AttackMode.Melee,80);f.hero.TickMovement(f.space,0.1f);Near(f.space.Position(f.hero.heroRect).y,15,"melee lane");Check(!f.hero.CanAttack(m,f.space),"not aligned yet");f.hero.ChangeAttackMode(1);f.hero.TickMovement(f.space,0.1f);Near(f.space.Position(f.hero.heroRect).y,15,"ranged keeps lane");
        });
        Run("pending wave is unique and cancelled on manual load",()=>{
            var f=new Field();var gm=new GameObject().AddComponent<GameManager>();
            Set(gm,"heroController",f.hero);Set(gm,"combatManager",f.combat);Set(gm,"saveManager",new GameObject().AddComponent<SaveManager>());
            Set(gm,"runtimeHeroData",new EntityDataSO());Set(gm,"hasDeployed",true);
            var save=Get<SaveManager>(gm,"saveManager");var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"idle-regression-"+Guid.NewGuid());System.IO.Directory.CreateDirectory(dir);Set(save,"saveDirectory",dir);save.SaveGame(new EntityDataSO(),SaveSlot.AutoSave);
            gm.preGameUI=new GameObject();gm.infoText=new GameObject().AddComponent<TMPro.TextMeshProUGUI>();
            var dead=new GameObject();gm.activeMonsters.Add(dead);gm.OnMonsterDied(dead);
            var pending=Get<Coroutine>(gm,"nextWaveCoroutine");Check(pending!=null,"scheduled once");
            gm.OnMonsterDied(dead);Check(Get<Coroutine>(gm,"nextWaveCoroutine")==pending,"duplicate death ignored");
            gm.ForceManualLoad(0);Check(pending.stopped,"pending wave stopped");Check(Get<Coroutine>(gm,"nextWaveCoroutine")==null,"handle cleared");Check(!f.hero.IsDeployed,"loaded hero hidden");
        });
        Run("lethal monster hit stops hero and clears battle",()=>{
            var f=new Field();var m=f.Monster(-20);f.Battle(m);m.TickMovement(f.space,0);Set(f.combat,"currentHeroHP",1);
            Time.deltaTime=2;Call(f.combat,"Update");Check(f.hero.IsDead,"Die called");Check(!f.hero.IsDeployed,"AI disabled");Check(f.Enemies.Count==0,"wave cleared");
        });
        Run("aspect resize recalculates home and range without Start cache",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);float before=f.space.AttackRange(AttackMode.RangedPhysical,true);
            f.ground.sizeDelta=new Vector2(1600,100);Near(f.space.AttackRange(AttackMode.RangedPhysical,true),before*1.6f,"live width");
            f.space.explorationHeroX=0.6f;Near(f.space.HomeX,160,"live home");
        });
        Run("equal speed preserves off-centre position after combat",()=>{
            var f=new Field(); f.space.scrollSpeed=1000; f.hero.moveSpeed=1000;
            f.space.SetPosition(f.hero.heroRect,new Vector2(250,0));
            for(int i=0;i<60;i++) f.Step(1f/60f);
            Near(f.space.Position(f.hero.heroRect).x,250,"no centre attraction",0.02f);
        });
        Run("fast hero drifts left then pushes camera to its own speed",()=>{
            foreach(int fps in new[]{30,60,144}) {
                var f=new Field(); f.space.scrollSpeed=1000; f.hero.moveSpeed=1500;
                for(int i=0;i<fps*2;i++) {
                    f.Step(1f/fps);
                    float x=f.space.Position(f.hero.heroRect).x;
                    Check(x>=f.space.ZoneLeftX-0.02f&&x<=f.space.ZoneRightX+0.02f,"hero inside zone");
                }
                Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneLeftX,"left anchor",0.02f);
                Near(f.space.CurrentCameraSpeed,1500,"camera catches fast hero",0.1f);
                Near(f.hero.moveSpeed,1500,"hero speed untouched");
            }
        });
        Run("slow hero drifts right then camera slows to hero speed",()=>{
            foreach(int fps in new[]{30,60,144}) {
                var f=new Field(); f.space.scrollSpeed=1000; f.hero.moveSpeed=500;
                for(int i=0;i<fps*2;i++) f.Step(1f/fps);
                Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"right anchor",0.02f);
                Near(f.space.CurrentCameraSpeed,500,"camera follows slow hero",0.1f);
            }
        });
        Run("standing combat hero drifts with world then pins right edge",()=>{
            var f=new Field(); f.space.scrollSpeed=1000; f.hero.ChangeAttackMode(1);
            var m=f.Monster(-200,AttackMode.RangedMagic); float gap=200;
            for(int i=0;i<120;i++) {
                f.Step(1f/60f);
                Near(f.space.Position(f.hero.heroRect).x-f.space.Position(m.Rect).x,gap,"camera preserves attack gap",0.03f);
            }
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"combat right anchor",0.02f);
            Near(f.space.CurrentCameraSpeed,0,"camera stopped",0.02f);
            Check(f.hero.CurrentState==HeroState.Combat,"still fighting");
        });
        Run("dead body stays camera subject until right edge then stops",()=>{
            var f=new Field(); f.space.scrollSpeed=1000; f.hero.Die();
            f.Step(0.1f); Near(f.space.Position(f.hero.heroRect).x,100,"corpse pans with world");
            Check(HeroController.ActiveHeroes.Count==0,"corpse not a combat actor");
            for(int i=0;i<120;i++) f.Step(1f/60f);
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"corpse remains in view",0.02f);
            Near(f.space.CurrentCameraSpeed,0,"stops at corpse",0.02f);
            f.hero.HideHero(); f.Step(); Check(!f.space.IsScrolling,"hide removes camera subject");
            f.hero.SpawnHero(); f.hero.moveSpeed=1000; f.Step();
            Near(f.space.Position(f.hero.heroRect).x,f.space.HomeX,"retry starts centred",0.02f);
            Near(f.space.CurrentCameraSpeed,1000,"retry resumes camera",0.02f);
        });
        Run("speed changes while anchored do not leave zone",()=>{
            var f=new Field(); f.space.scrollSpeed=1000; f.hero.moveSpeed=1500;
            for(int i=0;i<120;i++)f.Step(1f/60f);
            Near(f.space.CurrentCameraSpeed,1500,"left edge fast",0.1f);
            f.hero.moveSpeed=500; f.Step(0.1f);
            Check(f.space.Position(f.hero.heroRect).x>f.space.ZoneLeftX,"drifts right inside zone");
            Near(f.space.CurrentCameraSpeed,1000,"returns to base inside zone",0.1f);
            for(int i=0;i<150;i++)f.Step(1f/60f);
            Near(f.space.CurrentCameraSpeed,500,"right edge slow",0.1f);
            f.hero.moveSpeed=0; f.Step(); Near(f.space.CurrentCameraSpeed,0,"standing edge",0.02f);
            f.hero.moveSpeed=1000; f.Step();
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"equal keeps right anchor",0.02f);
            Near(f.space.CurrentCameraSpeed,1000,"moving resumes",0.1f);
        });
        Run("fast left anchor releases to base speed during combat then stops at right",()=>{
            foreach(float fps in new[]{30f,60f,144f}) {
                var f=new Field(); f.space.scrollSpeed=1000; f.hero.moveSpeed=1500;
                for(int i=0;i<(int)(fps*2);i++) f.Step(1f/fps);
                Near(f.space.CurrentCameraSpeed,1500,"fast left anchor",0.1f);
                var m=f.Monster(f.space.ZoneLeftX-20);
                float previous=f.space.Position(f.hero.heroRect).x;
                for(int i=0;i<(int)(fps*2);i++) {
                    f.Step(1f/fps);
                    float x=f.space.Position(f.hero.heroRect).x;
                    float expected=Math.Min(1000,(f.space.ZoneRightX-previous)*fps);
                    Near(f.space.CurrentCameraSpeed,expected,"base speed until boundary frame",0.15f);
                    Near(x-f.space.Position(m.Rect).x,20,"standing combat pair drifts together",0.05f);
                    Check(f.hero.CurrentState==HeroState.Combat,"hero stopped to fight");
                    previous=x;
                }
                Near(previous,f.space.ZoneRightX,"right anchor",0.03f);
                Near(f.space.CurrentCameraSpeed,0,"stationary right anchor",0.03f);
                Near(f.hero.moveSpeed,1500,"configured hero speed unchanged");
            }
        });
        Run("camera is independent of monster count and attack mode",()=>{
            var f=new Field(); Time.deltaTime=0.1f; Call(f.space,"LateUpdate");float a=f.space.CurrentCameraSpeed;
            f.Monster(-100);f.Monster(300,AttackMode.RangedMagic);f.hero.ChangeAttackMode(2);
            Call(f.space,"LateUpdate");Near(f.space.CurrentCameraSpeed,a,"no enemy-based framing");
        });
        Run("zone camera does not overshoot with a long frame",()=>{
            var f=new Field(); f.space.scrollSpeed=1000;f.hero.moveSpeed=2000;f.Step(2f);
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneLeftX,"long frame left");
            f.hero.moveSpeed=0;f.Step(2f);
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"long frame right");
            f.Step(2f);Near(f.space.CurrentCameraSpeed,0,"anchored stop");
        });
        Run("zone constraints remain correct across Canvas scales",()=>{
            foreach(float scale in new[]{0.2f,0.5f,1f,2f}) {
                var f=new Field(scale);f.space.scrollSpeed=1000;f.hero.moveSpeed=500;
                for(int i=0;i<120;i++) f.Step(1f/60f);
                Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"scale "+scale,0.03f);
                Near(f.space.CurrentCameraSpeed,500,"speed at scale "+scale,0.1f);
            }
        });
        Run("resizing and reversed zone configuration keep hero in view",()=>{
            var f=new Field(); f.hero.moveSpeed=0;f.space.SetPosition(f.hero.heroRect,new Vector2(450,0));
            f.ground.sizeDelta=new Vector2(500,100);f.space.speedZoneLeft=0.91f;f.space.speedZoneRight=0.06f;f.Step();
            Near(f.space.Position(f.hero.heroRect).x,f.space.ZoneRightX,"resize correction",0.02f);
            Check(f.space.ZoneLeftX<f.space.ZoneRightX,"reversed edges sorted");
        });
        Run("paused time causes no camera drift",()=>{
            var f=new Field();f.hero.moveSpeed=0;float x=f.space.Position(f.hero.heroRect).x;f.Step(0);
            Near(f.space.Position(f.hero.heroRect).x,x,"pause position");Near(f.space.CurrentCameraSpeed,0,"pause speed");
        });
        NewFeatureTests();
        DesktopMenuTests();
        IdentityMapTests();
        ElementWorldTests();
        Patch54aTests();
        GroundRelativeMotionTests();
        TerrainTests();
        MenuDifficultyTests();
        BalanceTests();
        BalanceSimulations();
        Console.WriteLine("PASS: "+tests+" regression scenarios (Unity test doubles; not Play Mode).");
    }
}
