using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void DistanceReady(DesktopHotfixFixture f,float speed)
    {
        WaveEntryScene(f);WaveEntryFinish(f);
        f.field.hero.useStanceMoveSpeeds=false;f.field.hero.moveSpeed=speed;
        EndWave(f);
    }
    static void DistanceWaveTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        float oldTime=Time.time,oldDelta=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime;
        using(var sprites=new SpriteResources())
        try
        {
            foreach(float speed in new[]{0f,75f,200f,450f,900f})
            Run("distance waves solo actual march speed "+speed,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,speed);Near(f.manager.CurrentDistancePerWave,1000,"one configurable default");
                    int expected=speed==0?80:(int)Math.Ceiling(1000/(speed*.125));
                    for(int i=1;i<expected;i++) {
                        NativeOrderTraceFrame(f,.125f);
                        Check(f.manager.activeMonsters.Count==0,"no wave before threshold");
                        Near(f.manager.ExplorationDistance,speed*.125f*i,"actual solo distance, independent of camera",.002f);
                    }
                    NativeOrderTraceFrame(f,.125f);
                    if(speed==0)Check(f.manager.activeMonsters.Count==0 && f.manager.ExplorationDistance==0,"stationary Hero cannot discover an encounter");
                    else {
                        // Floating-point actor coordinates can leave the actual
                        // sum just BELOW 1000 at the mathematical boundary.
                        // Never lower the runtime threshold or change tolerance.
                        if(f.manager.activeMonsters.Count==0) {
                            Check(f.manager.ExplorationDistance<1000,"only an actually incomplete threshold may wait another frame");
                            NativeOrderTraceFrame(f,.125f);
                        }
                        Check(f.manager.activeMonsters.Count>0 && !f.manager.IsWaitingForExploration,"one wave at distance threshold");
                        Near(f.manager.ExplorationDistance,0,"overflow discarded");
                    }
                }
            });
            foreach(int fps in new[]{30,60,144})
            Run("distance waves frame quantization and no early encounter "+fps+" FPS",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,200);int steps=0;
                    while(f.manager.activeMonsters.Count==0 && steps<fps*6) {
                        Check(f.manager.ExplorationDistance<1000,"still below threshold before last step");
                        NativeOrderTraceFrame(f,1f/fps);steps++;
                    }
                    Check(steps>=fps*5 && steps<=fps*5+2,"approximately five gameplay seconds, quantized without early wave");
                }
            });
            foreach(int count in new[]{2,3})foreach(bool regroup in new[]{false,true})
            Run("distance waves common progress not a sum "+count+(regroup?" regroup":" stable march"),()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,450);var space=f.field.space;space.CancelCameraEntryHandoff();
                    float x=space.Position(f.field.hero.heroRect).x;
                    if(count==3)FormationHero(f.field,x+(regroup?400:100),450,AttackMode.RangedPhysical);
                    FormationHero(f.field,x+(regroup?800:(count-1)*100),200,AttackMode.RangedMagic);
                    f.manager.distancePerWave=100000;double expected=0;bool advancing=false;
                    for(int i=0;i<240;i++) {
                        Time.frameCount++;Time.time+=1f/60f;Time.deltaTime=1f/60f;
                        if(i%2==0)HeroController.ActiveHeroes.Reverse();
                        Call(space,"Update");float least=float.MaxValue;
                        foreach(var hero in HeroController.ActiveHeroes)least=Mathf.Min(least,Mathf.Min(hero.MovementDistanceThisFrame,hero.EffectiveMoveSpeed()*Time.deltaTime));
                        expected+=least;if(least>0)advancing=true;
                        Call(space,"LateUpdate");Call(f.manager,"LateUpdate");
                        Near(f.manager.ExplorationDistance,(float)expected,"minimum actual group step, order independent");
                        Check(f.manager.ExplorationDistance<=200f*(i+1)/60f+.002f,"fast member cannot pull progress beyond slowest");
                    }
                    Check(advancing && f.manager.ExplorationDistance>0,"regrouping also discovers distance while whole team advances");
                }
            });
            foreach(float scale in new[]{.5f,1f,2f})
            Run("distance waves pause and gameplay timeScale "+scale,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,200);Time.timeScale=scale;
                    NativeOrderTraceFrame(f,.125f*scale);float d=f.manager.ExplorationDistance;
                    f.ui.SetPaused(true);for(int i=0;i<20;i++)NativeOrderTraceFrame(f,.25f);
                    Near(f.manager.ExplorationDistance,d,"pause cannot consume stale movement sample");
                    f.ui.SetPaused(false);Time.timeScale=scale;NativeOrderTraceFrame(f,.125f*scale);
                    Near(f.manager.ExplorationDistance,d+25*scale,"scaled actual movement resumes");
                }
            });
            Run("distance waves death frame, duplicate notifications and duplicate LateUpdate do not add progress",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var last=new List<GameObject>(f.manager.activeMonsters);
                    var extra=f.field.spawner.SpawnMonster();f.manager.activeMonsters.Add(extra);
                    foreach(var go in last) { go.GetComponent<MonsterController>().MarkDead();f.manager.OnMonsterDied(go); }
                    Check(!f.manager.IsWaitingForExploration,"not from an earlier death");
                    extra.GetComponent<MonsterController>().MarkDead();f.manager.OnMonsterDied(extra);
                    f.manager.OnMonsterDied(extra);Call(f.manager,"LateUpdate");
                    Near(f.manager.ExplorationDistance,0,"no combat movement from death frame");
                    NativeOrderTraceFrame(f,.125f);float d=f.manager.ExplorationDistance;
                    for(int i=0;i<10;i++)Call(f.manager,"LateUpdate");
                    Near(f.manager.ExplorationDistance,d,"one contribution per frame");
                }
            });
            Run("distance waves camera, parallax, teleport and projection are not exploration",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,200);f.manager.distancePerWave=100000;
                    Time.frameCount++;Time.deltaTime=.125f;Time.time+=Time.deltaTime;Call(f.field.space,"Update");
                    typeof(EnvironmentManager).GetMethod("PanWorld",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(f.field.space,new object[]{5000f});
                    var terrain=f.field.ground.GetComponent<GroundPresentation>();terrain.Scroll(10000);
                    f.field.space.SetPosition(f.field.hero.heroRect,new Vector2(0,0));
                    int width=Screen.width;Screen.width=1150;f.field.space.SyncBattleProjection();Screen.width=width;f.field.space.SyncBattleProjection();
                    Call(f.field.space,"LateUpdate");Call(f.manager,"LateUpdate");
                    Near(f.manager.ExplorationDistance,25,"only real 200*.125 own movement, not presentation or warp");
                    f.field.hero.SpawnHero();Time.frameCount++;Call(f.manager,"LateUpdate");
                    Near(f.manager.ExplorationDistance,25,"spawn without movement cannot add distance");
                }
            });
            Run("distance waves active combat never banks movement for later encounters",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);
                    for(int i=0;i<40;i++) {
                        Time.frameCount++;Time.deltaTime=1f/60f;Time.time+=Time.deltaTime;f.field.Step(Time.deltaTime);
                        Call(f.manager,"LateUpdate");Near(f.manager.ExplorationDistance,0,"no combat distance");
                    }
                    EndWave(f);NativeOrderTraceFrame(f,.125f);
                    Check(f.manager.ExplorationDistance<=f.field.hero.EffectiveMoveSpeed()*.125f+.002f,"only fresh post-combat sample");
                }
            });
            Run("distance waves speed changes, zero-speed member, recruitment and death do not corrupt progress",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,450);f.manager.distancePerWave=100000;
                    NativeOrderTraceFrame(f,.125f);float before=f.manager.ExplorationDistance;
                    float x=f.field.space.Position(f.field.hero.heroRect).x;
                    var mage=FormationHero(f.field,x+100,0,AttackMode.RangedMagic);
                    Call(f.manager,"LateUpdate");Near(f.manager.ExplorationDistance,before,"new life / repeated sample adds no spawn distance");
                    NativeOrderTraceFrame(f,.125f);Near(f.manager.ExplorationDistance,before,"immobile member halts common progress");
                    mage.moveSpeed=200;NativeOrderTraceFrame(f,.125f);Check(f.manager.ExplorationDistance>before,"restored speed resumes");
                    before=f.manager.ExplorationDistance;mage.Die();NativeOrderTraceFrame(f,.125f);
                    Near(f.manager.ExplorationDistance,before+56.25f,"living solo now uses 450, no negative or inherited offset");
                }
            });
            Run("distance waves configured threshold and huge frame spawn once without queued overflow",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,450);f.manager.distancePerWave=40;
                    NativeOrderTraceFrame(f,.25f);Check(f.manager.activeMonsters.Count>0,"threshold crossed");
                    var wave=f.manager.activeMonsters.ToArray();
                    for(int i=0;i<10;i++)Call(f.manager,"LateUpdate");
                    Check(f.manager.activeMonsters.Count==wave.Length,"no duplicate spawn");
                    for(int i=0;i<wave.Length;i++)Check(f.manager.activeMonsters[i]==wave[i],"same wave identity");
                    Near(f.manager.ExplorationDistance,0,"excess is not queued for wave after this");
                    EndWave(f);Call(f.manager,"LateUpdate");Check(f.manager.activeMonsters.Count==0,"cannot chain another wave from old overflow");
                }
            });
            Run("distance waves real B2 recruit/removal keeps common progress and primary life",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,450);f.manager.distancePerWave=100000;NativeOrderTraceFrame(f,1f/60f);
                    float before=f.manager.ExplorationDistance;
                    Time.frameCount++;Time.deltaTime=1f/60f;Time.time+=Time.deltaTime;
                    Call(f.field.space,"Update");float x=f.field.space.Position(f.field.hero.heroRect).x;
                    int life=f.field.hero.SpawnVersion;
                    f.manager.CycleTestCompanions();f.manager.CycleTestCompanions();
                    Check(f.manager.TestCompanionCount==2,"real three-member party");
                    Near(f.field.space.Position(f.field.hero.heroRect).x,x,"recruit does not teleport primary");
                    Call(f.field.space,"LateUpdate");Call(f.manager,"LateUpdate");
                    Near(f.manager.ExplorationDistance,before,"unticked new B2 member prevents counting its spawn as travel");
                    NativeOrderTraceFrame(f,1f/60f);
                    Check(f.manager.ExplorationDistance>=before && f.manager.ExplorationDistance<=before+200f/60f+.002f,"B2 common step capped by slow mage");
                    before=f.manager.ExplorationDistance;f.manager.CycleTestCompanions();
                    Check(f.manager.TestCompanionCount==0,"real roster removal");
                    NativeOrderTraceFrame(f,.125f);Near(f.manager.ExplorationDistance,before+56.25f,"solo resumes without cumulative-roster offset");
                    Check(f.field.hero.SpawnVersion==life,"primary life unchanged");
                }
            });
            Run("distance waves movement modifiers are honored once and never dirty save data",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,200);f.manager.distancePerWave=100000;
                    var data=f.field.combat.CurrentHeroCombatState.Data;data.combatStatModifiers.Clear();
                    data.combatStatModifiers.Add(new CombatStatModifier { stat=CombatStatId.MovementSpeed,flat=30,percent=.1f });
                    data.isDirty=false;NativeOrderTraceFrame(f,.125f);
                    Near(f.field.hero.EffectiveMoveSpeed(),250,"base plus flat plus base-percent");
                    Near(f.manager.ExplorationDistance,31.25f,"actual modified movement, not double-applied");
                    Check(!data.isDirty,"distance is runtime-only");
                    Time.frameCount++;Time.deltaTime=.125f;Time.time+=Time.deltaTime;
                    Call(f.field.space,"Update");data.combatStatModifiers.Clear();
                    Call(f.field.space,"LateUpdate");Call(f.manager,"LateUpdate");
                    Near(f.manager.ExplorationDistance,62.5f,"buff removed later in frame does not rewrite distance already walked");
                }
            });
            Run("distance waves a temporarily missing spawner cannot discard a pending encounter",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,450);f.manager.distancePerWave=40;
                    Set(f.manager,"monsterSpawner",null);NativeOrderTraceFrame(f,.125f);
                    Check(f.manager.IsWaitingForExploration && f.manager.activeMonsters.Count==0,"pending encounter retained");
                    Set(f.manager,"monsterSpawner",f.field.spawner);NativeOrderTraceFrame(f,.125f);
                    Check(!f.manager.IsWaitingForExploration && f.manager.activeMonsters.Count>0,"valid spawner resumes exactly one encounter");
                }
            });
            Run("distance waves game over and live-wave reentry cannot spawn duplicates",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var wave=f.manager.activeMonsters.ToArray();
                    Call(f.manager,"CallNextWave");Check(f.manager.activeMonsters.Count==wave.Length,"live wave blocks second spawn");
                    for(int i=0;i<wave.Length;i++)Check(f.manager.activeMonsters[i]==wave[i],"live identities preserved");
                    EndWave(f);NativeOrderTraceFrame(f,.125f);f.field.hero.Die();f.manager.OnHeroDied();
                    Check(!f.manager.IsWaitingForExploration && f.manager.ExplorationDistance==0,"game over cancels progress");
                    for(int i=0;i<30;i++)Call(f.manager,"LateUpdate");
                    Check(f.manager.activeMonsters.Count==0,"no wave behind Game Over");
                }
            });
            foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
            Run("distance waves invalid threshold uses safe single default "+invalid,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,200);f.manager.distancePerWave=invalid;
                    Near(f.manager.CurrentDistancePerWave,1000,"safe threshold");NativeOrderTraceFrame(f,.125f);
                    Check(f.manager.activeMonsters.Count==0 && f.manager.ExplorationDistance>0,"no instant loop or NaN");
                }
            });
            foreach(string action in new[]{"Load","Reset","ReturnTitle","DeathRetry","Travel","Disable"})
            Run("distance waves "+action+" cancels previous session progress",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    DistanceReady(f,200);NativeOrderTraceFrame(f,.125f);Check(f.manager.ExplorationDistance>0,"progress seeded");
                    if(action=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"Load");
                    else if(action=="Reset") { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    else if(action=="ReturnTitle")Check(f.manager.ReturnToMainMenu(),"title");
                    else if(action=="DeathRetry") { f.field.hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked(); }
                    else if(action=="Travel") {
                        var data=f.manager.HeroData;WorldMapNode node;Check(WorldMapCatalog.TryGet(data.worldMapId,out node),"map");
                        if(!data.worldMapCleared.Contains(data.worldMapId))data.worldMapCleared.Add(data.worldMapId);
                        string id=null;foreach(var link in node.Links) { WorldMapNode n;if(WorldMapCatalog.TryGet(link,out n) && !n.IsBoss) { id=link;break; } }
                        Check(id!=null && f.manager.TryTravelToWorldMap(id),"travel");
                    }
                    else Call(f.manager,"OnDisable");
                    Near(f.manager.ExplorationDistance,0,"old distance reset");Check(!f.manager.IsWaitingForExploration,"old schedule canceled");
                    var wave=f.manager.activeMonsters.ToArray();Call(f.manager,"LateUpdate");Call(f.manager,"LateUpdate");
                    Check(f.manager.activeMonsters.Count==wave.Length,"no latent spawn");
                    if(wave.Length>0) {
                        EndWave(f);Call(f.manager,"LateUpdate");Near(f.manager.ExplorationDistance,0,"next wait starts fresh, no prior progress");
                    }
                }
            });
        }
        finally { Resources.Unregister("Spawn/Appear");Time.time=oldTime;Time.deltaTime=oldDelta;Time.unscaledDeltaTime=oldUnscaled; }
    }
}
