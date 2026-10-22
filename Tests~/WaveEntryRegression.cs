using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using TuTienCore;

partial class MotionRegression
{
    // Native Canvas transforms are modeled explicitly. Screen coordinates in
    // these checks/logs are NOT measurements from a Unity renderer.
    static Canvas WaveEntryScene(DesktopHotfixFixture f,bool fresh=true)
    {
        EntryBody(f);Set(f.manager,"environmentManager",f.field.space);
        var canvas=StableCanvasFixture(f.field);
        // MainGame.unity's camera configuration, without editing the runtime camera.
        var space=f.field.space;space.useSoftZoneCamera=true;
        space.speedZoneLeft=.196f;space.speedZoneRight=.91f;space.speedZoneInset=.25f;
        space.cameraCombatFocusSmoothTime=1.5f;space.stationaryCombatSmoothTime=1.5f;
        Set(f.field.spawner,"monsterPool",new ObjectPool<GameObject>(
            ()=>f.field.Monster(-100).gameObject,m=>{
                // Same anchor/position contract as MonsterSpawner.actionOnGet.
                var r=m.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=Vector2.zero;
                r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(-100,50);m.SetActive(true);
            },m=>m.SetActive(false),m=>{},1,10));
        RectTransform[] decor;TerrainField(f.field,out decor);
        EntryTitle(f,fresh);EntryReveal(f);EntryTick(f,.35f);
        return canvas;
    }
    static void WaveEntryFinish(DesktopHotfixFixture f) { EntryTick(f,HeroSpawnVfx.Duration); }
    static Coroutine EndWave(DesktopHotfixFixture f)
    {
        var wave=new List<GameObject>(f.manager.activeMonsters);Check(wave.Count>0,"wave running");
        foreach(var go in wave) { go.GetComponent<MonsterController>().MarkDead();f.manager.OnMonsterDied(go); }
        var wait=Get<Coroutine>(f.manager,"nextWaveCoroutine");Check(wait!=null,"last death schedules wait");
        Check(f.manager.activeMonsters.Count==0,"no next wave on death frame");return wait;
    }
    static void WaveEntryTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        float oldDelta=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime,oldScale=Time.timeScale;
        using(var sprites=new SpriteResources())
        try
        {
            foreach(int width in new[]{600,800,1150})foreach(bool fresh in new[]{false,true})foreach(bool b2 in new[]{false,true})
            Run("W2.2d "+width+(fresh?" New":" Continue")+(b2?" B2":" solo")+" releases after camera and preserves 80-percent entry",()=>{
                using(var f=new DesktopHotfixFixture(width,true))
                {
                    WaveEntryScene(f,fresh);var hero=f.field.hero;var space=f.field.space;
                    var terrain=f.field.ground.GetComponent<GroundPresentation>();
                    var anchor=hero.heroRect.anchoredPosition;var position=space.Position(hero.heroRect);
                    var screen=RectTransformUtility.WorldToScreenPoint(null,hero.heroRect.position);
                    var ground=f.field.ground.anchoredPosition;float phase=terrain.TilePhase;int life=hero.SpawnVersion;
                    Near(position.x,576,"80 percent of 1920-unit BattleArea");Near(screen.x,width*.8f,"modeled overlay screen X");
                    // Stress the exact handoff: a positive delta must not permit
                    // LateUpdate to advance a camera whose Update was paused.
                    Time.deltaTime=.1f;Call(space,"Update");
                    Time.unscaledDeltaTime=HeroSpawnVfx.Duration;Call(f.manager,"Update");
                    Check(f.manager.IsEnteringGameplay && Time.timeScale==0,"VFX completion keeps pause until after camera");
                    Check(f.manager.activeMonsters.Count==0,"combat cannot start midway through Update phase");
                    Call(f.field.combat,"Update");Call(space,"LateUpdate");
                    Near(space.Position(hero.heroRect).x,position.x,"no camera-only handoff pan");
                    Near(space.CurrentCameraSpeed,0,"camera paused throughout handoff frame");Near(terrain.TilePhase,phase,"terrain stays still");
                    Call(f.manager,"LateUpdate");
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1,"entry released after camera");
                    Check(f.manager.activeMonsters.Count>0 && Get<Coroutine>(f.manager,"nextWaveCoroutine")==null,"first wave immediate, no five-second wait");
                    Near(hero.heroRect.anchoredPosition.x,anchor.x,"no anchored-position reset at StartBattle");
                    Near(space.Position(hero.heroRect).x,position.x,"no reset to HomeX");
                    Near(RectTransformUtility.WorldToScreenPoint(null,hero.heroRect.position).x,screen.x,"screen position stays at reveal location");
                    Near(f.field.ground.anchoredPosition.x,ground.x,"projection origin unchanged");
                    Check(hero.SpawnVersion==life,"no second SpawnHero");
                    var firstWave=f.manager.activeMonsters[0];
                    Call(f.manager,"LateUpdate");Call(f.manager,"LateUpdate");
                    Check(f.manager.activeMonsters[0]==firstWave && Get<Coroutine>(f.manager,"nextWaveCoroutine")==null,"handoff completion is idempotent");
                    if(b2) { f.manager.CycleTestCompanions();f.manager.CycleTestCompanions();Check(f.manager.TestCompanionCount==2,"real B2 party"); }
                    foreach(var actor in HeroController.ActiveHeroes)actor.tracePositionChanges=true;
                    if(width==800 && fresh && !b2)Console.WriteLine("MONO OVERLAY MODEL: "+hero.DescribePosition("after first wave creation"));
                    for(int i=0;i<30;i++)
                    {
                        Time.deltaTime=1f/60f;Time.frameCount++;Time.time+=Time.deltaTime;
                        var actors=HeroController.ActiveHeroes.ToArray();var before=new float[actors.Length];
                        for(int j=0;j<actors.Length;j++)before[j]=space.Position(actors[j].heroRect).x;
                        Call(space,"Update");
                        for(int j=0;j<actors.Length;j++) {
                            float walk=before[j]-space.Position(actors[j].heroRect).x;
                            Check(walk>=-.002f && walk<=actors[j].EffectiveMoveSpeed()*Time.deltaTime+.002f,"movement obeys own speed, no snap");
                        }
                        float beforePan=space.Position(hero.heroRect).x;
                        Call(f.field.combat,"Update");Call(space,"LateUpdate");Call(f.manager,"LateUpdate");
                        Near(space.Position(hero.heroRect).x,beforePan+space.CurrentCameraSpeed*Time.deltaTime,"only original shared camera pan after walking");
                        Near(space.CurrentBackgroundSpeed,space.CurrentCameraSpeed,"Ground shares original soft camera pan");
                        Check(hero.SpawnVersion==life,"no respawn during first wave");
                        if(i==0 && width==800 && fresh && !b2)Console.WriteLine("MONO OVERLAY MODEL: "+hero.DescribePosition("after first live frame"));
                    }
                }
            });
            foreach(float speed in new[]{0f,75f,200f,450f,900f})
            Run("W2.2d five gameplay seconds independent of Hero speed "+speed,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);f.field.hero.useStanceMoveSpeeds=false;f.field.hero.moveSpeed=speed;
                    Near(f.manager.CurrentWaveDelay,5,"single runtime delay parameter");var wait=EndWave(f);
                    Time.deltaTime=.125f;
                    for(int i=1;i<40;i++)Check(wait.routine.MoveNext() && f.manager.activeMonsters.Count==0,"no early spawn before five seconds");
                    Check(!wait.routine.MoveNext() && f.manager.activeMonsters.Count>0,"next wave at exactly five gameplay seconds");
                    Check(Get<Coroutine>(f.manager,"nextWaveCoroutine")==null,"wait handle cleared");
                }
            });
            Run("W2.2d five-second wait at 60 FPS starts only at last death and ignores duplicate death notifications",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);
                    // Keep at least two live wave members to distinguish first/last death.
                    var extra=f.field.spawner.SpawnMonster();f.manager.activeMonsters.Add(extra);
                    var first=f.manager.activeMonsters[0];first.GetComponent<MonsterController>().MarkDead();f.manager.OnMonsterDied(first);
                    Check(Get<Coroutine>(f.manager,"nextWaveCoroutine")==null,"not counting from an earlier death");
                    var last=new List<GameObject>(f.manager.activeMonsters);var wait=EndWave(f);
                    foreach(var go in last)f.manager.OnMonsterDied(go);
                    Check(Get<Coroutine>(f.manager,"nextWaveCoroutine")==wait,"only one pending wave");
                    Time.deltaTime=1f/60f;
                    for(int i=1;i<300;i++)Check(wait.routine.MoveNext(),"wait until five seconds at 60 FPS");
                    Check(!wait.routine.MoveNext(),"300 scaled frames completes wait");
                    var wave=f.manager.activeMonsters[0];Check(!wait.routine.MoveNext() && f.manager.activeMonsters[0]==wave,"finished iterator cannot create another wave");
                }
            });
            Run("W2.2d pause excludes time and movement speed or stance changes cannot shorten pending wait",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var wait=EndWave(f);Time.deltaTime=.5f;
                    for(int i=0;i<4;i++)Check(wait.routine.MoveNext(),"two gameplay seconds");
                    f.ui.SetPaused(true);Time.deltaTime=10;
                    for(int i=0;i<50;i++)Check(wait.routine.MoveNext() && f.manager.activeMonsters.Count==0,"paused stale delta ignored");
                    f.ui.SetPaused(false);f.manager.SetHeroAttackMode(2);Time.deltaTime=.5f;
                    for(int i=0;i<5;i++)Check(wait.routine.MoveNext(),"wait to 4.5 despite stance change");
                    Check(!wait.routine.MoveNext() && f.manager.activeMonsters.Count>0,"five gameplay seconds total");
                }
            });
            Run("W2.2d zero configurable delay still cannot bypass gameplay pause",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);f.manager.waveDelay=0;
                    var wait=EndWave(f);f.ui.SetPaused(true);Time.deltaTime=10;
                    for(int i=0;i<20;i++)Check(wait.routine.MoveNext() && f.manager.activeMonsters.Count==0,"zero delay waits for resume");
                    f.ui.SetPaused(false);Check(!wait.routine.MoveNext() && f.manager.activeMonsters.Count>0,"zero delay completes only when unpaused");
                }
            });
            Run("W2.2d configured nonzero delay replaces the default without another hardcoded clock",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);f.manager.waveDelay=2.5f;
                    Near(f.manager.CurrentWaveDelay,2.5f,"configured gameplay duration");var wait=EndWave(f);Time.deltaTime=.125f;
                    for(int i=1;i<20;i++)Check(wait.routine.MoveNext() && f.manager.activeMonsters.Count==0,"respect configured boundary");
                    Check(!wait.routine.MoveNext() && f.manager.activeMonsters.Count>0,"wave uses 2.5 seconds when configured");
                }
            });
            foreach(float scale in new[]{.5f,1f,2f})
            Run("W2.2d timer uses gameplay seconds at timeScale "+scale,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var wait=EndWave(f);
                    Time.timeScale=scale;Time.deltaTime=.125f*scale;int steps=(int)(40/scale);
                    for(int i=1;i<steps;i++)Check(wait.routine.MoveNext(),"scaled duration not yet complete");
                    Check(!wait.routine.MoveNext() && f.manager.activeMonsters.Count>0,"five scaled seconds irrespective of real-time duration");
                }
            });
            Run("five-second gap allows soft 450/450/200 regroup progress without overspeed or delaying the wave",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);f.manager.CycleTestCompanions();f.manager.CycleTestCompanions();
                    HeroController sword=null,bow=null,mage=null;
                    foreach(var hero in HeroController.ActiveHeroes) {
                        if(hero.attackMode==AttackMode.Melee)sword=hero;
                        else if(hero.attackMode==AttackMode.RangedPhysical)bow=hero;else mage=hero;
                    }
                    Check(sword!=null && bow!=null && mage!=null,"three B2 roles");
                    f.field.space.SetPosition(sword.heroRect,new Vector2(-300,0));
                    f.field.space.SetPosition(bow.heroRect,new Vector2(100,25));
                    f.field.space.SetPosition(mage.heroRect,new Vector2(300,-25));
                    var wait=EndWave(f);var actors=new[]{sword,bow,mage};
                    for(int i=1;i<=40;i++) {
                        f.field.Step(.125f);
                        foreach(var actor in actors)Check(actor.MovementDistanceThisFrame<=actor.EffectiveMoveSpeed()*.125f+.002f,"own speed limit throughout wait");
                        Check(sword.MovementDistanceThisFrame>0,"leader keeps walking during the gap");
                        if(Formation(f.field).CurrentPhase==HeroFormation.Phase.Marching) {
                            Near(Formation(f.field).MarchSpeed,200,"slowest actual speed");
                            Near(f.field.space.Position(bow.heroRect).x-f.field.space.Position(sword.heroRect).x,100,"bow spacing",5);
                            Near(f.field.space.Position(mage.heroRect).x-f.field.space.Position(bow.heroRect).x,100,"mage spacing",5);
                        }
                        bool pending=wait.routine.MoveNext();Check(pending==(i<40),"no early or duplicate wave during regroup");
                    }
                    float error=Mathf.Max(Mathf.Abs(f.field.space.Position(bow.heroRect).x-f.field.space.Position(sword.heroRect).x-100),
                        Mathf.Abs(f.field.space.Position(mage.heroRect).x-f.field.space.Position(bow.heroRect).x-100));
                    Check(error<300 && f.manager.activeMonsters.Count>0,"gap shrinks and next wave still starts on schedule; soft regroup need not finish in five seconds");
                }
            });
            foreach(string action in new[]{"Load","Reset","ReturnTitle","DeathRetry","Travel","Disable"})
            Run("W2.2d "+action+" cancels pending wait and stale iterator cannot spawn into new state",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var old=EndWave(f);Time.deltaTime=1;Check(old.routine.MoveNext(),"one second elapsed");
                    if(action=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"manual Load");
                    else if(action=="Reset") { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    else if(action=="ReturnTitle")Check(f.manager.ReturnToMainMenu(),"return title");
                    else if(action=="DeathRetry") { f.field.hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked(); }
                    else if(action=="Travel") {
                        var data=f.manager.HeroData;WorldMapNode node;Check(WorldMapCatalog.TryGet(data.worldMapId,out node),"current map");
                        if(!data.worldMapCleared.Contains(data.worldMapId))data.worldMapCleared.Add(data.worldMapId);
                        string id=null;foreach(var link in node.Links) { WorldMapNode candidate;if(WorldMapCatalog.TryGet(link,out candidate) && !candidate.IsBoss) { id=link;break; } }
                        Check(id!=null && f.manager.TryTravelToWorldMap(id),"legal map travel");
                    }
                    else Call(f.manager,"OnDisable");
                    Check(old.stopped && Get<Coroutine>(f.manager,"nextWaveCoroutine")==null,"old wait cancelled");
                    var wave=new List<GameObject>(f.manager.activeMonsters);Time.deltaTime=10;
                    Check(!old.routine.MoveNext(),"even explicitly resumed stale iterator is rejected");
                    Check(f.manager.activeMonsters.Count==wave.Count,"no extra wave after cancellation");
                    for(int i=0;i<wave.Count;i++)Check(f.manager.activeMonsters[i]==wave[i],"current wave identity unchanged");
                    if(action=="Load" || action=="Reset" || action=="DeathRetry" || action=="Travel")Check(wave.Count>0,"direct entry starts first wave immediately");
                }
            });
            foreach(string action in new[]{"Load","Reset","ReturnTitle","Disable"})
            Run("W2.2d "+action+" cancels a VFX completion pending between Update and LateUpdate",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);Time.unscaledDeltaTime=HeroSpawnVfx.Duration;Call(f.manager,"Update");
                    Check(f.manager.IsEnteringGameplay && Time.timeScale==0 && f.manager.activeMonsters.Count==0,"completion is pending");
                    if(action=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"immediate Load");
                    else if(action=="Reset") { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    else if(action=="ReturnTitle")Check(f.manager.ReturnToMainMenu(),"return title");
                    else Call(f.manager,"OnDisable");
                    var wave=new List<GameObject>(f.manager.activeMonsters);
                    Call(f.field.space,"LateUpdate");Call(f.manager,"LateUpdate");
                    Check(!f.manager.IsEnteringGameplay && f.manager.activeMonsters.Count==wave.Count,"old completion cannot deploy another wave");
                    for(int i=0;i<wave.Count;i++)Check(f.manager.activeMonsters[i]==wave[i],"wave belongs to new state");
                }
            });
            Run("W2.2d stale wait cannot clear the newer wait handle or contribute elapsed time",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var old=EndWave(f);
                    Time.deltaTime=4;Check(old.routine.MoveNext(),"old wait has four seconds");
                    Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"start another session state");
                    var current=EndWave(f);Check(current!=old,"new wait identity");
                    Time.deltaTime=10;Check(!old.routine.MoveNext(),"old generation rejected");
                    Check(Get<Coroutine>(f.manager,"nextWaveCoroutine")==current,"new handle not cleared by old iterator");
                    Time.deltaTime=1;
                    for(int i=0;i<4;i++)Check(current.routine.MoveNext(),"fresh five-second clock");
                    Check(!current.routine.MoveNext() && f.manager.activeMonsters.Count>0,"only the new clock spawns");
                }
            });
            Run("W2.2d diagnostics read state without repositioning actor, camera, terrain or changing dirty data",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);var hero=f.field.hero;var p=hero.heroRect.position;float focus=f.field.space.CurrentCameraFocusX;
                    var terrain=f.field.ground.GetComponent<GroundPresentation>();float phase=terrain.TilePhase;bool dirty=f.manager.HeroData.isDirty;
                    var text=hero.DescribePosition("diagnostic snapshot");hero.tracePositionChanges=true;hero.TracePosition("read-only trace",true);
                    Check(text.Contains("anchor=") && text.Contains("battle=(576,0)") && text.Contains("HomeX=0") &&
                        text.Contains("focus=") && text.Contains("pan=") && text.Contains("terrainPhase=") && text.Contains("screen=(640,0)"),"all requested coordinate systems recorded");
                    Near(hero.heroRect.position.x,p.x,"no position write");Near(f.field.space.CurrentCameraFocusX,focus,"no focus change");
                    Near(terrain.TilePhase,phase,"no scrolling");Check(f.manager.HeroData.isDirty==dirty,"no data mutation");
                }
            });
        }
        finally { Resources.Unregister("Spawn/Appear");Time.deltaTime=oldDelta;Time.unscaledDeltaTime=oldUnscaled;Time.timeScale=oldScale; }
    }
}
