using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

partial class MotionRegression
{
    static void CameraEntryHandoffTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        float oldTime=Time.time,oldDelta=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime;
        using(var sprites=new SpriteResources())
        try
        {
            foreach(int width in new[]{600,800,1150})foreach(int count in new[]{1,2,3})foreach(int fps in new[]{30,60,144})
            Run("camera entry bounded pan, terrain and finite handoff "+width+"/"+count+"/"+fps,()=>{
                using(var f=new DesktopHotfixFixture(width,true))
                {
                    WaveEntryScene(f);var hero=f.field.hero;var space=f.field.space;
                    var p=space.Position(hero.heroRect);int life=hero.SpawnVersion;
                    Call(space,"Update");Call(space,"LateUpdate");
                    Near(space.Position(hero.heroRect).x,576,"80 percent remains fixed during VFX");
                    Check(!space.IsCameraEntryHandoff,"no follow handoff until VFX complete");
                    WaveEntryFinish(f);Check(space.IsCameraEntryHandoff,"entry phase armed");
                    Near(space.Position(hero.heroRect).x,p.x,"no position patch at wave creation");
                    for(int i=1;i<count;i++)f.manager.CycleTestCompanions();
                    var terrain=f.field.ground.GetComponent<GroundPresentation>();float totalPan=0;bool finished=false;
                    for(int i=0;i<fps*12;i++)
                    {
                        bool entry=space.IsCameraEntryHandoff;var actors=HeroController.ActiveHeroes.ToArray();
                        var before=new float[actors.Length];for(int j=0;j<actors.Length;j++)before[j]=space.Position(actors[j].heroRect).x;
                        float phase=terrain.TilePhase;Time.deltaTime=1f/fps;Time.frameCount++;Time.time+=Time.deltaTime;
                        float previousVelocity=Get<float>(space,"entryCameraVelocity");
                        Call(space,"Update");var walk=new float[actors.Length];
                        for(int j=0;j<actors.Length;j++)walk[j]=space.Position(actors[j].heroRect).x-before[j];
                        float plan=Get<float>(space,"plannedCameraPan");Call(space,"LateUpdate");Call(f.manager,"LateUpdate");
                        if(entry) {
                            Check(Mathf.Abs(plan)<=space.cameraEntryMaxStep+.001f,"hard maximum step even at lower FPS");
                            Check(Mathf.Abs(plan)<=space.cameraEntryMaxSpeed*Time.deltaTime+.001f,"speed limit");
                            float entryDt=Mathf.Min(Time.deltaTime,space.cameraEntryMaxStep/space.cameraEntryMaxSpeed);
                            Check(Mathf.Abs(plan/entryDt-previousVelocity)<=space.cameraEntryAcceleration*entryDt+.002f,"bounded acceleration/deceleration");
                            for(int j=0;j<actors.Length;j++)Near(space.Position(actors[j].heroRect).x-before[j],walk[j]+plan,"actor receives own walk plus exactly shared pan");
                        }
                        Near(space.LastAppliedCameraPan,plan,"plan applied exactly once");
                        Near(space.CurrentBackgroundSpeed,space.CurrentCameraSpeed,"same Ground/background pan");
                        Near(terrain.TilePhase,BattleMotion.Wrap(phase+plan,0,space.Width),"terrain phase synchronized");
                        totalPan+=Mathf.Abs(plan);
                        if(!space.IsCameraEntryHandoff) { finished=true;break; }
                    }
                    Check(finished && totalPan>0,"handoff completes, not stuck");Check(hero.SpawnVersion==life,"never respawn to hide pan");
                    // An arbitrary later game state must use the exact original
                    // formula, with original velocity and focus state.
                    Time.deltaTime=1f/fps;Time.frameCount++;Time.time+=Time.deltaTime;Call(space,"Update");
                    float expected=Get<float>(space,"plannedCameraPan");Call(space,"LateUpdate");
                    Near(space.LastAppliedCameraPan,expected,"normal shared plan after release");
                    Check(!space.IsCameraEntryHandoff,"does not re-arm during ordinary waves");
                }
            });
            foreach(float speed in new[]{0f,200f,450f,900f})
            Run("camera entry stalled or fast Hero still completes "+speed,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);f.field.hero.useStanceMoveSpeeds=false;f.field.hero.moveSpeed=speed;
                    for(int i=0;i<1200 && f.field.space.IsCameraEntryHandoff;i++)NativeOrderTraceFrame(f,1f/60f);
                    Check(!f.field.space.IsCameraEntryHandoff,"finite entry for actual speed");
                }
            });
            Run("camera entry explicitly discards previous phase momentum and invalid inspector values cannot stall it",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var space=f.field.space;
                    Set(Get<object>(space,"deadzoneCamera"),"velocity",10000f);
                    Set(Get<object>(space,"combatFocusSmoother"),"velocity",10000f);
                    Set(space,"stationaryCombatVelocity",10000f);space.BeginCameraEntryHandoff();
                    Near(space.CameraFollowVelocity,0,"no old camera momentum");
                    Near(Get<float>(space,"stationaryCombatVelocity"),0,"no old stationary momentum");
                    space.cameraEntryDuration=float.NaN;space.cameraEntryMaxSpeed=0;
                    space.cameraEntryAcceleration=float.PositiveInfinity;space.cameraEntryMaxStep=-1;
                    for(int i=0;i<1200 && space.IsCameraEntryHandoff;i++)NativeOrderTraceFrame(f,1f/60f);
                    Check(!space.IsCameraEntryHandoff,"safe fallbacks converge");
                }
            });
            Run("camera entry native first delta is bounded; pause freezes and long frame cannot produce a giant pan",()=>{
                using(var f=new DesktopHotfixFixture(1920,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var space=f.field.space;
                    NativeOrderTraceFrame(f,.0131111043f);
                    Check(Mathf.Abs(space.LastAppliedCameraPan)<.2f,"native delta no longer pulls world by 8.13191");
                    Near(Get<float>(space,"entryCameraRequestedPan"),-8.13191f,"same original request as native evidence");
                    Console.WriteLine("MONO FRAME TRACE: "+f.field.hero.DescribePosition("first bounded entry frame"));
                    float elapsed=Get<float>(space,"entryCameraElapsed"),x=space.Position(f.field.hero.heroRect).x;
                    f.ui.SetPaused(true);for(int i=0;i<20;i++)NativeOrderTraceFrame(f,.1f);
                    Near(Get<float>(space,"entryCameraElapsed"),elapsed,"pause freezes handoff clock");
                    Near(space.Position(f.field.hero.heroRect).x,x,"pause freezes actors/camera");
                    f.ui.SetPaused(false);NativeOrderTraceFrame(f,.25f);
                    Check(Mathf.Abs(space.LastAppliedCameraPan)<=space.cameraEntryMaxStep+.001f,"long frame step cap");
                    int oldWidth=Screen.width;Screen.width=800;space.SyncBattleProjection();Screen.width=oldWidth;space.SyncBattleProjection();
                    Check(space.IsCameraEntryHandoff,"resize does not restart or discard entry");
                    for(int i=0;i<12;i++) {
                        NativeOrderTraceFrame(f,1f/60f);
                        Console.WriteLine("MONO FRAME TRACE: "+f.field.hero.DescribePosition("entry sample "+i));
                    }
                }
            });
            foreach(string action in new[]{"Load","ReturnTitle","Reset","DeathRetry","Disable"})
            Run("camera entry canceled by "+action+" without leaking to next session",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);Check(f.field.space.IsCameraEntryHandoff,"entry active");
                    if(action=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"manual load");
                    if(action=="ReturnTitle")Check(f.manager.ReturnToMainMenu(),"return menu");
                    if(action=="Reset") { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    if(action=="DeathRetry") { f.manager.OnHeroDied();f.manager.OnRetryClicked(); }
                    if(action=="Disable")Call(f.manager,"OnDisable");
                    Check(!f.field.space.IsCameraEntryHandoff,"old handoff canceled");
                }
            });
        }
        finally { Resources.Unregister("Spawn/Appear");Time.time=oldTime;Time.deltaTime=oldDelta;Time.unscaledDeltaTime=oldUnscaled; }
    }
}
