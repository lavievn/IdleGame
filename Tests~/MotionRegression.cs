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
    static void FinishShortMapEntry(GameManager manager,EnvironmentManager space)
    {
        Check(manager.IsEnteringGameplay && Get<bool>(manager,"openingRevealed"),"short map entry is pending");
        Check(Time.timeScale==0f && manager.activeMonsters.Count==0,"entry is paused before first wave");
        float old=Time.unscaledDeltaTime;Time.unscaledDeltaTime=HeroSpawnVfx.Duration;Time.frameCount++;
        try { Call(manager,"Update");Call(space,"LateUpdate");Call(manager,"LateUpdate"); }
        finally { Time.unscaledDeltaTime=old; }
        Check(!manager.IsEnteringGameplay && Time.timeScale==1f,"short entry completes before gameplay assertions");
    }
    // Legacy regression cases refer to the old CombatManager scalar field
    // names. Resolve them to HeroCombatState instead of keeping duplicate
    // mutable HP/timer data just to satisfy reflection from older tests.
    static object ResolveTestMember(object value,ref string name)
    {
        var combat=value as CombatManager;
        if (combat == null) return value;
        switch (name)
        {
            case "heroController": name="Controller"; break;
            case "runtimeHeroData": name="Data"; break;
            case "currentHeroHP": name="CurrentHP"; break;
            case "maxHeroHP": name="MaxHP"; break;
            case "heroAttackTimer": name="AttackTimer"; break;
            case "heroWindupMode": name="WindupMode"; break;
            default: return value;
        }
        return combat.CurrentHeroCombatState;
    }
    static void Set(object value,string member,object data)
    {
        value=ResolveTestMember(value,ref member);
        var flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        var field=value.GetType().GetField(member,flags);
        if (field != null) { field.SetValue(value,data); return; }
        var property=value.GetType().GetProperty(member,flags);
        if (property != null) { property.SetValue(value,data,null); return; }
        throw new MissingMemberException(value.GetType().Name,member);
    }
    static T Get<T>(object value,string member)
    {
        value=ResolveTestMember(value,ref member);
        var flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        var field=value.GetType().GetField(member,flags);
        if (field != null) return (T)field.GetValue(value);
        var property=value.GetType().GetProperty(member,flags);
        if (property != null) return (T)property.GetValue(value,null);
        throw new MissingMemberException(value.GetType().Name,member);
    }

    class Field
    {
        public EnvironmentManager space;
        public HeroController hero;
        public RectTransform ground,grass;
        public MonsterSpawner spawner;
        public CombatManager combat;
        public Field(float scale=1f,bool limitRenderMovement=false)
        {
            // A fresh simulated scene cannot inherit the previous scene's
            // asynchronous map-entry pause. Individual pause cases set it later.
            Time.timeScale=1f;
            if (EnvironmentManager.Instance != null) Call(EnvironmentManager.Instance,"OnDestroy");
            CombatBalance.ResetDevProfiles();
            HeroController.ActiveHeroes.Clear();HeroController.VisibleHeroes.Clear();MonsterController.ActiveMonsters.Clear();UnityEngine.Object.objects.Clear();
            ground=(RectTransform)new GameObject(true).transform;ground.sizeDelta=new Vector2(1000,100);ground.localScale=new Vector3(scale,scale,1);
            grass=(RectTransform)new GameObject(true).transform;grass.parent=ground;grass.anchoredPosition=new Vector2(600,0);grass.sizeDelta=new Vector2(200,100);
            space=new GameObject().AddComponent<EnvironmentManager>();
            // Historical math/AI tests advance seconds in a single call; those
            // are elapsed-time slices, not render frames. Keep that reference
            // explicit without changing their assertions. Hitch tests opt into
            // the production 1/30 budget; native fixtures keep runtime defaults.
            if(!limitRenderMovement)space.maxHeroMovementDeltaTime=float.MaxValue;
            space.useMultiZoneCamera=false; // Explicit legacy camera reference; W2.2h has its own default-on cases.
            // Existing camera tests describe the original hard-zone behavior.
            // Keep that reference available; new tests exercise soft chasing.
            space.useSoftZoneCamera=false;
            space.speedZoneInset=0;Set(space,"battleArea",ground);Set(space,"groundDetails",new[]{grass});Call(space,"Awake");
            hero=new GameObject().AddComponent<HeroController>();
            // Historical motion references keep their constant-Y contract.
            // Dedicated lane cases enable the production setting explicitly.
            hero.enableRangedLaneMotion=false;
            hero.heroRect=(RectTransform)new GameObject(true).transform;hero.heroRect.parent=ground;hero.heroRect.anchorMin=hero.heroRect.anchorMax=new Vector2(1,0);hero.heroRect.pivot=new Vector2(1,0);Call(hero,"Awake");hero.SpawnHero();
            spawner=new GameObject().AddComponent<MonsterSpawner>();Set(spawner,"monsterPool",new ObjectPool<GameObject>(()=>new GameObject(),m=>{},m=>m.SetActive(false),m=>{},5,20));
            combat=new GameObject().AddComponent<CombatManager>();Call(combat,"Start");combat.SetupHeroInfo(new EntityDataSO { baseHealth=10000,baseDamage=10,expToNextLevel=1000000 });
            // Historical attack-mode cases exercise the explicit temporary-B2 fallback.
            // Weapon regressions rebind through SetupHeroInfo to exercise the primary path.
            hero.BindBasicAttackData(combat.CurrentHeroCombatState.Data,true);
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

    static void Main(string[] args)
    {
        if(args.Length==1 && args[0]=="--loot-visuals") {LootDropPresentationTests();CombatLootTests();return;}
        if(args.Length==1 && args[0]=="--weapon-basic") {WeaponBasicAttackTests();return;}
        if(args.Length==1 && args[0]=="--combat-loot") {
            CombatLootTests();BalanceTests();Patch54bTests();return;
        }
        if(args.Length==1 && args[0]=="--inventory-compare") {
            EquipmentStatPreviewRegressionTests();InventoryComparisonTests();
            ManagementWindowRegressionTests();InventoryArtThemeRegressionTests();return;
        }
        if(args.Length==1 && args[0]=="--plains") {PlainsPresentationTests();return;}
        if(args.Length==1 && args[0]=="--terrain-art") {MultiTerrainTests();return;}
        if(args.Length==1 && args[0]=="--terrain-seams") {TerrainSeamTests();return;}
        if(args.Length==1 && args[0]=="--magic-persistence") {MagicPersistenceTests();return;}
        if(args.Length==1 && args[0]=="--audio-volumes") {DualVolumeTests();MusicVolumeRegressionTests();OpeningSettingsRegressionTests();return;}
        if(args.Length==1 && args[0]=="--enemy-sprites") { EnemySpriteTests();return; }
        // Run entry/B2 contracts with sequence resources present, instead of
        // the historical one-frame SpriteResources fallback configuration.
        if(args.Length==1 && args[0]=="--archer-entry") {
            using(var frames=new ArcherFrames()) {RangedLaneTests();B2SpawnPositionTests();}
            return;
        }
        if(args.Length==1 && args[0]=="--hero-sfx") { HeroSfxTests();return; }
        if(args.Length==1 && args[0]=="--archer-animation") { ArcherAnimationTests();return; }
        if(args.Length==1 && args[0]=="--ranged-lanes") { RangedLaneTests();return; }
        if(args.Length==1 && args[0]=="--opening-font") { OpeningFontTests();return; }
        if(args.Length==1 && args[0]=="--movement-spike") { HeroMovementSpikeTests();return; }
        if(args.Length==1 && args[0]=="--render-trace") { HeroRenderTraceTests();return; }
        if(args.Length==1 && args[0]=="--position-hotfix") { B2SpawnPositionTests();return; }
        if(args.Length==1 && args[0]=="--multi-zone") { MultiZoneCameraTests();return; }
        if(args.Length==1 && args[0]=="--map-entry") { UnifiedMapEntryTests();return; }
        if(args.Length==1 && args[0]=="--distance-waves") { DistanceWaveTests();return; }
        if(args.Length==1 && args[0]=="--soft-formation") { SoftFormationTests();return; }
        if(args.Length==1 && args[0]=="--camera-entry") { CameraEntryHandoffTests();return; }
        if(args.Length==1 && args[0]=="--world-trace") { WorldEntryTraceTests();return; }
        if(args.Length==1 && args[0]=="--wave-entry") { WaveEntryTests();return; }
        if(args.Length==1 && args[0]=="--formation") { FormationTests();B2CombatRoutingTests();B2TestPartyTests();return; }
        if(args.Length==1 && args[0]=="--spawn") { SpawnEntryTests();return; }
        if (args.Length == 1 && args[0] == "--desktop-ui") {
            DesktopMenuHotfixTests();DesktopMenuTests();ManualSaveMenuTests();
            MenuDifficultyTests();ManualLoadUiRegressionTests();
            // The legacy opening group scans UnityStub.Object.objects globally;
            // dispose prior simulated scenes before checking its fresh curtain.
            new Field();OpeningCinematicRegressionTests();Patch54cTests();
            return;
        }
        SpritePresentationTests();
        if (args.Length == 1 && args[0] == "--sprites") return;
        ArcherAnimationTests();
        EnemySpriteTests();
        DualVolumeTests();
        MagicPersistenceTests();
        HeroSfxTests();
        OpeningFontTests();
        WorldMapRegressionTests();
        MultiZoneCameraTests();
        UnifiedMapEntryTests();
        CameraEntryHandoffTests();
        WorldEntryTraceTests();
        HeroRenderTraceTests();
        HeroMovementSpikeTests();
        RangedLaneTests();
        WaveEntryTests();
        DistanceWaveTests();
        FormationTests();
        B2SpawnPositionTests();
        SoftFormationTests();
        SpawnEntryTests();
        DesktopMenuHotfixTests();
        ManualLoadUiRegressionTests();
        Run("six-vertex polygon uses true shape, not rectangular hitbox", () => {
            var hex = new List<Vector2> {
                new Vector2(.20f,.25f), new Vector2(.55f,.20f),
                new Vector2(.80f,.40f), new Vector2(.77f,.68f),
                new Vector2(.45f,.80f), new Vector2(.18f,.60f)
            };
            Check(WorldMapPolygons.ValidSixVertices(hex), "valid skewed hex");
            Check(WorldMapPolygons.Contains(hex,new Vector2(.50f,.50f)), "center inside");
            Check(!WorldMapPolygons.Contains(hex,new Vector2(.12f,.42f)),
                "point inside bounding rectangle but outside polygon rejected");
            Check(WorldMapPolygons.Contains(hex,new Vector2(.375f,.225f)),
                "border points selectable");
            var reversed = new List<Vector2>(hex);
            reversed.Reverse();
            Check(WorldMapPolygons.ValidSixVertices(reversed), "reversed winding accepted");
            Check(WorldMapPolygons.Contains(reversed,new Vector2(.50f,.50f)),
                "winding direction does not affect hit");
            var invalid = new List<Vector2>(hex);
            invalid[1] = hex[4];
            Check(!WorldMapPolygons.ValidSixVertices(invalid),
                "self-crossing or degenerate polygon rejected");
            var concave = new List<Vector2> {
                new Vector2(.2f,.2f),new Vector2(.8f,.2f),new Vector2(.8f,.45f),
                new Vector2(.5f,.4f),new Vector2(.8f,.8f),new Vector2(.2f,.8f)
            };
            Check(WorldMapPolygons.ValidSixVertices(concave), "concave hex accepted");
            Check(WorldMapPolygons.Triangulate(concave).Count == 12,
                "six-sided concave shape triangulates into four triangles");
        });
        Run("irregular world cells accept 3–24 vertices and reject crossed outlines", () => {
            var tri = new List<Vector2> {
                new Vector2(.1f,.1f),new Vector2(.9f,.1f),new Vector2(.5f,.9f)
            };
            Check(WorldMapPolygons.ValidPolygon(tri), "triangle outline is valid");
            Check(WorldMapPolygons.Triangulate(tri).Count == 3,
                "triangle renders as one face");
            var seven = new List<Vector2> {
                new Vector2(.2f,.15f),new Vector2(.5f,.12f),
                new Vector2(.83f,.26f),new Vector2(.88f,.53f),
                new Vector2(.69f,.83f),new Vector2(.37f,.87f),
                new Vector2(.14f,.56f)
            };
            Check(WorldMapPolygons.ValidPolygon(seven), "seven-sided cell is valid");
            Check(!WorldMapPolygons.ValidSixVertices(seven),
                "legacy six-corner validator stays strict");
            Check(WorldMapPolygons.Triangulate(seven).Count == 15,
                "seven corners triangulate into five faces");
            Check(WorldMapPolygons.Contains(seven,new Vector2(.5f,.5f)),
                "seven-corner hit test follows polygon, not bounding box");
            var crossed = new List<Vector2> {
                new Vector2(.1f,.1f),new Vector2(.8f,.8f),
                new Vector2(.1f,.8f),new Vector2(.8f,.1f)
            };
            Check(!WorldMapPolygons.ValidPolygon(crossed),
                "self-crossed cell rejected");
            var tooFew = new List<Vector2> {
                new Vector2(.1f,.1f),new Vector2(.9f,.9f)
            };
            Check(!WorldMapPolygons.ValidPolygon(tooFew),
                "two corners cannot form a clickable map");
        });
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
        Run("AoE leaves enemies outside the impact radius untouched",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var near=f.Monster(-200);var far=f.Monster(-480);f.Battle(near,far);f.hero.TickMovement(f.space,0);Time.deltaTime=2;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"no damage before arrival");Time.deltaTime=1.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP<f.Enemies[0].maxHP,"near hit");Check(f.Enemies[1].currentHP==f.Enemies[1].maxHP,"far untouched");
        });
        Run("charged magic waits for an eligible target without firing",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=1.9f;Call(f.combat,"Update");f.space.SetPosition(m.Rect,new Vector2(-480,0));Time.deltaTime=0.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"no ghost hit");Near(Get<float>(f.combat,"heroAttackTimer"),0,"charge resets when target leaves range");
        });
        Run("switching from magic to melee allows an immediate strike",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-20);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=1.9f;Call(f.combat,"Update");f.hero.ChangeAttackMode(0);Time.deltaTime=0.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP<f.Enemies[0].maxHP,"immediate melee strike");
        });
        Run("clear battle cancels timers and pooled respawn resets state",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=1.9f;Call(f.combat,"Update");f.combat.ForceClearAllMonsters();Near(Get<float>(f.combat,"heroAttackTimer"),0,"cleared timer");Check(MonsterController.ActiveMonsters.Count==0,"pool removed");m.gameObject.SetActive(true);Check(m.currentState==MonsterState.PassiveScroll&&m.CurrentTarget==null,"pool reset");f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=0.2f;Call(f.combat,"Update");Check(f.Enemies[0].currentHP==f.Enemies[0].maxHP,"fresh windup");
        });
        Run("in-range melee strikes without Y alignment, ranged keeps its lane",()=>{
            var f=new Field();var m=f.Monster(-35,AttackMode.Melee,80);f.hero.TickMovement(f.space,0.1f);Near(f.space.Position(f.hero.heroRect).y,0,"in range keeps lane");Check(f.hero.CanAttack(m,f.space),"Y does not delay melee");f.hero.ChangeAttackMode(1);f.hero.TickMovement(f.space,0.1f);Near(f.space.Position(f.hero.heroRect).y,0,"ranged keeps lane");
        });
        Run("pending wave is unique and cancelled on manual load",()=>{
            var f=new Field();var gm=new GameObject().AddComponent<GameManager>();
            Set(gm,"heroController",f.hero);Set(gm,"combatManager",f.combat);Set(gm,"saveManager",new GameObject().AddComponent<SaveManager>());
            Set(gm,"runtimeHeroData",new EntityDataSO());Set(gm,"hasDeployed",true);
            var save=Get<SaveManager>(gm,"saveManager");var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"idle-regression-"+Guid.NewGuid());System.IO.Directory.CreateDirectory(dir);Set(save,"saveDirectory",dir);save.SaveGame(new EntityDataSO(),SaveSlot.AutoSave);
            gm.preGameUI=new GameObject();gm.infoText=new GameObject().AddComponent<TMPro.TextMeshProUGUI>();
            var dead=new GameObject();gm.activeMonsters.Add(dead);gm.OnMonsterDied(dead);
            Check(gm.IsWaitingForExploration && Get<Coroutine>(gm,"nextWaveCoroutine")==null,"distance wait scheduled without a timer");
            gm.OnMonsterDied(dead);Check(gm.IsWaitingForExploration && gm.ExplorationDistance==0,"duplicate death ignored");
            gm.ForceManualLoad(0);Check(!gm.IsWaitingForExploration && gm.ExplorationDistance==0,"pending distance wave stopped");Check(Get<Coroutine>(gm,"nextWaveCoroutine")==null,"handle cleared");Check(!f.hero.IsDeployed,"loaded hero hidden");
        });
        Run("lethal monster hit stops hero and clears battle",()=>{
            var f=new Field();var m=f.Monster(-20);f.Battle(m);m.TickMovement(f.space,0);Set(f.combat,"currentHeroHP",1);
            Time.deltaTime=2;Call(f.combat,"Update");Check(f.hero.IsDead,"Die called");Check(!f.hero.IsDeployed,"AI disabled");Check(f.Enemies.Count==0,"wave cleared");
        });
        Run("explicit battle geometry changes recalculate home and range (not window resize)",()=>{
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
        Run("0.0.50 last Hero vanishes on death and camera stops immediately",()=>{
            var f=new Field(); f.space.scrollSpeed=1000; f.hero.Die();
            float corpseX=f.space.Position(f.hero.heroRect).x;
            Check(!f.hero.heroRect.gameObject.activeSelf,"no visible Hero body after death");
            Check(!f.hero.IsCameraSubject&&HeroController.ActiveHeroes.Count==0,"no dead camera subject");
            Check(!HeroController.VisibleHeroes.Contains(f.hero),"depth sorting excludes corpse");
            f.Step(0.1f);
            Near(f.space.Position(f.hero.heroRect).x,corpseX,"hidden corpse never follows Ground");
            Near(f.space.CurrentCameraSpeed,0,"no camera movement after last death",.02f);
            Check(!f.space.IsScrolling&&f.space.CurrentCameraFocusCount==0,"camera fully released");
            f.hero.SpawnHero(); f.hero.moveSpeed=1000; f.Step();
            Check(f.hero.heroRect.gameObject.activeSelf&&f.hero.IsDeployed,"retry visible");
            Near(f.space.Position(f.hero.heroRect).x,f.space.HomeX,"retry starts centred",.02f);
            Near(f.space.CurrentCameraSpeed,1000,"retry resumes camera",.02f);
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
        HeroDamagePopupTests();
        DesktopMenuTests();
        IdentityMapTests();
        ElementWorldTests();
        Patch54aTests();
        GroundRelativeMotionTests();
        Patch54bTests();
        Patch54cTests();
        Patch54dTests();
        Patch54fTests();
        Patch54gTests();
        Patch54hTests();
        CameraFollowTests();
        CameraAndLaneHotfixTests();
        HeroCombatStateTests();
        B2CombatRoutingTests();
        B2TestPartyTests();
        Version0050RegressionTests();
        Version0051StatsTests();
        PlainsPresentationTests();
        MultiTerrainTests();
        TerrainSeamTests();
        TerrainTests();
        MenuDifficultyTests();
        ManualSaveMenuTests();
        AttackModeSaveRegressionTests();
        InventoryEquipmentRegressionTests();
        SaveRollbackRegressionTests();
        MultiSlotEquipmentRegressionTests();
        EquipmentStatPreviewRegressionTests();
        AffixDiscoveryRegressionTests();
        ForgeRegressionTests();
        ManagementWindowRegressionTests();
        InventoryArtThemeRegressionTests();
        MusicVolumeRegressionTests();
        OpeningCinematicRegressionTests();
        OpeningSettingsRegressionTests();
        BalanceTests();
        BalanceSimulations();
        Console.WriteLine("PASS: "+tests+" regression scenarios (Unity test doubles; not Play Mode).");
    }
}
