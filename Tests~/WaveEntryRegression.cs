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
    static void EndWave(DesktopHotfixFixture f)
    {
        var wave=new List<GameObject>(f.manager.activeMonsters);Check(wave.Count>0,"wave running");
        foreach(var go in wave) { go.GetComponent<MonsterController>().MarkDead();f.manager.OnMonsterDied(go); }
        var wait=Get<Coroutine>(f.manager,"nextWaveCoroutine");Check(wait==null && f.manager.IsWaitingForExploration,"last death arms distance wait without a timer");
        Check(f.manager.activeMonsters.Count==0,"no next wave on death frame");
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
            Run("escaped-wave replay stale wait cannot clear the newer wait handle or contribute elapsed time",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);Call(f.manager,"RecoverEscapedWave");var old=Get<Coroutine>(f.manager,"nextWaveCoroutine");
                    Time.deltaTime=4;Check(old.routine.MoveNext(),"old wait has four seconds");
                    Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"start another session state");
                    Call(f.manager,"RecoverEscapedWave");var current=Get<Coroutine>(f.manager,"nextWaveCoroutine");Check(current!=old,"new wait identity");
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
