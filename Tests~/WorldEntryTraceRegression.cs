using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

partial class MotionRegression
{
    static float TraceValue(string line,string key)
    {
        string token=" "+key+"=";int start=line.IndexOf(token,StringComparison.Ordinal);
        Check(start>=0,"trace contains "+key);start+=token.Length;
        int end=line.IndexOf(' ',start);if(end<0)end=line.Length;
        return float.Parse(line.Substring(start,end-start),System.Globalization.CultureInfo.InvariantCulture);
    }
    static void NativeOrderTraceFrame(DesktopHotfixFixture f,float dt)
    {
        Time.frameCount++;Time.deltaTime=dt;Time.unscaledDeltaTime=dt;Time.time+=dt;
        Call(f.field.space,"Update");Call(f.manager,"Update");Call(f.field.combat,"Update");
        Call(f.field.space,"LateUpdate");Call(f.field.hero,"LateUpdate");Call(f.manager,"LateUpdate");
    }
    // The screen projection remains the existing Mono overlay MODEL. This
    // comparison proves diagnostics do not change that model, not native layout.
    static List<float> EntryMotionSample(int width,bool party,bool tracing)
    {
        UnityEngine.Random.InitState(2205);
        var sample=new List<float>();
        using(var f=new DesktopHotfixFixture(width,true))
        {
            f.field.hero.tracePositionChanges=tracing;WaveEntryScene(f);WaveEntryFinish(f);
            if(party) { f.manager.CycleTestCompanions();f.manager.CycleTestCompanions(); }
            for(int i=0;i<36;i++)
            {
                NativeOrderTraceFrame(f,1f/60f);
                foreach(var hero in HeroController.ActiveHeroes)
                {
                    var p=f.field.space.Position(hero.heroRect);sample.Add(p.x);sample.Add(p.y);
                    sample.Add(hero.MovementDistanceThisFrame);sample.Add((int)hero.CurrentState);
                }
                var space=f.field.space;var terrain=f.field.ground.GetComponent<GroundPresentation>();
                sample.Add(space.CurrentCameraSpeed);sample.Add(space.CurrentBackgroundSpeed);
                sample.Add(space.CurrentCameraFocusX);sample.Add(space.CurrentCameraSmoothedFocusX);
                sample.Add(space.CameraFollowVelocity);sample.Add(terrain.TilePhase);
                sample.Add(terrain.FarPhase);sample.Add(terrain.DistantPhase);
                sample.Add(Get<float>(space,"stationaryCombatVelocity"));
                sample.Add(f.field.combat.CurrentHeroCombatState.CurrentHP);
                sample.Add(f.field.combat.CurrentHeroCombatState.AttackTimer);
                sample.Add(f.manager.HeroData.isDirty?1:0);
                sample.Add(f.manager.activeMonsters.Count);
            }
        }
        return sample;
    }
    static void WorldEntryTraceTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        var oldObserver=Debug.LogObserver;float oldTime=Time.time,oldDelta=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime;
        int oldFrame=Time.frameCount;
        using(var sprites=new SpriteResources())
        try
        {
            Run("W2.2e diagnostics default off; frame window counts paused frames and is bounded",()=>{
                using(var f=new DesktopHotfixFixture(800,true))
                {
                    var logs=new List<string>();Debug.LogObserver=s=>{if(s.StartsWith("[HeroPosition]"))logs.Add(s);};
                    WaveEntryScene(f);var hero=f.field.hero;
                    Check(!hero.tracePositionChanges && logs.Count==0,"no default logs from any lifecycle stage");
                    hero.tracePositionChanges=true;hero.positionTraceFrameCount=2;hero.ArmPositionTrace();
                    int start=Time.frameCount;Time.timeScale=0;
                    for(int i=0;i<3;i++) { Time.frameCount=start+i;Call(hero,"LateUpdate"); }
                    Check(logs.Count==2,"exactly two paused frames, no unlimited logging");
                    Check(!hero.IsPositionTraceFrame,"expired diagnostic window");
                    hero.positionTraceFrameCount=0;hero.ArmPositionTrace();Time.frameCount++;
                    Check(!hero.IsPositionTraceFrame,"invalid low budget clamps to one");
                    hero.positionTraceFrameCount=999;hero.ArmPositionTrace();Time.frameCount+=119;
                    Check(hero.IsPositionTraceFrame,"maximum includes frame 120");Time.frameCount++;
                    Check(!hero.IsPositionTraceFrame,"high budget capped at 120");
                    hero.tracePositionChanges=false;hero.TracePosition("disabled explicit event");
                    Check(logs.Count==2,"disabling also silences explicit events");
                }
                Debug.LogObserver=null;
            });
            Run("W2.2e pending VFX completion rearms logs before paused LateUpdate and first wave",()=>{
                using(var f=new DesktopHotfixFixture(800,true))
                {
                    WaveEntryScene(f);var hero=f.field.hero;var space=f.field.space;
                    hero.tracePositionChanges=true;hero.positionTraceFrameCount=2;hero.ArmPositionTrace();
                    Time.frameCount+=10;Check(!hero.IsPositionTraceFrame,"previous reveal window expired");
                    var logs=new List<string>();Debug.LogObserver=s=>{if(s.StartsWith("[HeroPosition]"))logs.Add(s);};
                    Time.deltaTime=.1f;Call(space,"Update");
                    Time.unscaledDeltaTime=HeroSpawnVfx.Duration;Call(f.manager,"Update");
                    Call(space,"LateUpdate");Call(f.manager,"LateUpdate");
                    int request=logs.FindIndex(s=>s.Contains("GameManager.Update after entry"));
                    int skipped=logs.FindIndex(s=>s.Contains("Environment.LateUpdate skipped"));
                    int release=logs.FindIndex(s=>s.Contains("VFX complete before gameplay release"));
                    int wave=logs.FindIndex(s=>s.Contains("CallNextWave after StartBattle"));
                    Check(request>=0 && skipped>request && release>skipped && wave>release,"actual call order retained");
                    Check(logs[request].Contains("entry=True") && logs[request].Contains("completionPending=True"),"pending stage recorded");
                    Check(logs[skipped].Contains("timeScale=0") && logs[skipped].Contains("panSource=skipped"),"paused camera frame explicit");
                    Check(logs[wave].Contains("entry=False") && logs[wave].Contains("timeScale=1"),"release and wave state explicit");
                    Near(TraceValue(logs[skipped],"actualPan"),0,"actual pan zero in skipped frame");
                    Near(TraceValue(logs[skipped],"updateFrame"),Time.frameCount,"skipped Update frame known even after prior log window expired");
                    Check(logs[wave].Contains("openingEntering=False") && logs[wave].Contains("menuActive=False"),"opening state retained");
                }
                Debug.LogObserver=null;
            });
            Run("W2.2e trace distinguishes stale previous-frame pan from current plan and exact applied pan",()=>{
                using(var f=new DesktopHotfixFixture(800,true))
                {
                    WaveEntryScene(f);WaveEntryFinish(f);var hero=f.field.hero;var space=f.field.space;
                    hero.tracePositionChanges=true;hero.ArmPositionTrace();
                    var logs=new List<string>();Debug.LogObserver=s=>{if(s.StartsWith("[HeroPosition]"))logs.Add(s);};
                    NativeOrderTraceFrame(f,1f/60f);
                    var applied=logs.Find(s=>s.Contains("Environment.LateUpdate after pan"));
                    Check(applied!=null && applied.Contains("hasPlan=False") && applied.Contains("panSource=planned"),"one shared plan applied");
                    Near(TraceValue(applied,"planFrame"),Time.frameCount,"plan frame exact");
                    Near(TraceValue(applied,"appliedFrame"),Time.frameCount,"application frame exact");
                    Near(TraceValue(applied,"plannedPan"),TraceValue(applied,"actualPan"),"actual world pan equals planned pan");
                    Near(TraceValue(applied,"plannedBackgroundPan"),TraceValue(applied,"actualBackgroundPan"),"actual background pan equals plan");
                    float previous=TraceValue(applied,"actualPan");Check(Mathf.Abs(previous)>.01f,"nonzero fixture pan");
                    Time.frameCount++;Time.deltaTime=.04f;Call(space,"Update");
                    var pending=hero.DescribePosition("before LateUpdate");
                    Near(TraceValue(pending,"appliedFrame"),Time.frameCount-1,"old applied frame explicitly labeled");
                    Near(TraceValue(pending,"planFrame"),Time.frameCount,"new plan frame labeled");
                    Near(TraceValue(pending,"actualPan"),previous,"last applied amount not reconstructed using NEW deltaTime");
                    Near(TraceValue(pending,"pan"),previous,"legacy pan label also uses exact applied amount");
                    float before=space.Position(hero.heroRect).x;Call(space,"LateUpdate");
                    var current=hero.DescribePosition("after LateUpdate");
                    Near(space.Position(hero.heroRect).x-before,TraceValue(current,"actualPan"),"measured hero displacement equals captured applied pan");
                    foreach(var key in new[]{"heroLocal=","heroWorld=","groundLocal=","groundWorld=","groundScale=",
                        "canvasWorld=","renderMode=","deadzoneVelocity=","combatOffset=","combatVelocity=","stationaryVelocity=",
                        "unscaledDt=","terrainPhase=","farPhase=","distantPhase=","screen=","openingEntering=","entryElapsed="})
                        Check(current.Contains(key),"snapshot includes "+key);
                    float focus=space.CurrentCameraSmoothedFocusX,velocity=space.CameraFollowVelocity;
                    for(int i=0;i<20;i++) { hero.DescribePosition("repeat read");space.DescribeCameraTrace();f.manager.DescribeGameplayEntry(); }
                    Near(space.CurrentCameraSmoothedFocusX,focus,"reading does not step focus smoother");
                    Near(space.CameraFollowVelocity,velocity,"reading does not step deadzone smoother");
                }
                Debug.LogObserver=null;
            });
            Run("W2.2e native frame 5096 sample is reproduced by normal camera without entry handoff",()=>{
                using(var f=new DesktopHotfixFixture(1920,true))
                {
                    WaveEntryScene(f);WaveEntryFinish(f);var space=f.field.space;var hero=f.field.hero;
                    // Preserve the measured baseline as a regression for the
                    // original camera, outside the newly approved entry phase.
                    space.CancelCameraEntryHandoff();
                    int life=hero.SpawnVersion;
                    const float nativeDelta=.0131111043f;
                    Near(space.Position(hero.heroRect).x,576,"native reveal X at 80 percent");
                    Time.deltaTime=nativeDelta;Time.frameCount++;Call(space,"Update");
                    Near(hero.MovementDistanceThisFrame,450*nativeDelta,"walk obeys actual movement speed");
                    Near(space.Position(hero.heroRect).x,570.1f,"native after-approach X");
                    Near(space.ZoneRightX,444.48f,"80 percent spawn starts outside outer red boundary");
                    Call(space,"LateUpdate");
                    Near(space.LastAppliedCameraPan,-8.13191f,"native initial pan, with original camera formula");
                    Near(space.Position(hero.heroRect).x,561.968f,"native X after shared world pan");
                    Near(space.CurrentCameraSpeed,-620.2308f,"native camera speed");
                    Near(space.CurrentBackgroundSpeed,space.CurrentCameraSpeed,"native shared terrain pan");
                    Check(hero.SpawnVersion==life,"same deployed life, no second spawn");
                }
            });
            foreach(int width in new[]{600,800,1150})foreach(bool party in new[]{false,true})
            Run("W2.2e opt-in tracing leaves entry/movement/combat/camera/parallax identical "+width+(party?" three Heroes":" solo"),()=>{
                var logs=new List<string>();Debug.LogObserver=s=>{if(s.StartsWith("[HeroPosition]"))logs.Add(s);};
                var baseline=EntryMotionSample(width,party,false);
                Check(logs.Count==0,"baseline has no diagnostics");
                var observed=EntryMotionSample(width,party,true);
                Check(logs.Count>0 && baseline.Count==observed.Count,"traced simulation ran same number of steps and actors");
                for(int i=0;i<baseline.Count;i++)Near(observed[i],baseline[i],"unchanged model sample "+i);
                Debug.LogObserver=null;
            });
        }
        finally
        {
            Debug.LogObserver=oldObserver;Resources.Unregister("Spawn/Appear");
            Time.time=oldTime;Time.frameCount=oldFrame;Time.deltaTime=oldDelta;Time.unscaledDeltaTime=oldUnscaled;
        }
    }
}
