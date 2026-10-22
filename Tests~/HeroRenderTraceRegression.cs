using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static void SamePoint(Vector3 a,Vector3 b,string label)
    {
        Near(a.x,b.x,label+" X");Near(a.y,b.y,label+" Y");Near(a.z,b.z,label+" Z");
    }
    static void CheckRenderFeet(HeroController hero,string stage,bool vfx)
    {
        var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();
        var body=bridge.BodyGraphic;
        Vector3 foot=HeroRenderProbe.LogicFootWorld(hero.heroRect);
        // Compare in ONE projected coordinate system, never child anchoredPositions.
        var screen=RectTransformUtility.WorldToScreenPoint(null,foot);
        foreach(bool pixel in new[]{false,true}) {
            var bodyScreen=RectTransformUtility.WorldToScreenPoint(null,
                HeroRenderProbe.ImageReferenceWorld(body,bridge.BodyReferenceFootUV,pixel));
            Near(bodyScreen.x,screen.x,stage+" body foot screen X");
            Near(bodyScreen.y,screen.y,stage+" body foot screen Y");
            if(vfx) {
                var effect=hero.heroRect.GetComponent<HeroSpawnVfx>().EffectGraphic;
                var effectScreen=RectTransformUtility.WorldToScreenPoint(null,
                    HeroRenderProbe.ImageReferenceWorld(effect,new Vector2(.5f,.09f),pixel));
                Near(effectScreen.x,screen.x,stage+" VFX foot screen X");
                Near(effectScreen.y,screen.y,stage+" VFX foot screen Y");
            }
        }
    }
    static void StopRenderTrace(HeroController hero)
    {
        var probe=hero.GetComponent<HeroRenderProbe>();if(probe!=null)Call(probe,"OnDisable");
    }
    static void RenderCallbacks(HeroController hero)
    {
        var probe=hero.GetComponent<HeroRenderProbe>();if(probe==null)return;
        Call(probe,"LateUpdate");Canvas.InvokeWillRenderCanvases();
        // UnityStub has no native scheduler. Resume the actual probe coroutine
        // explicitly; this tests its observation code, NOT Unity render timing.
        var routine=Get<Coroutine>(probe,"endOfFrame");
        if(routine!=null && !routine.stopped)routine.routine.MoveNext();
    }
    static List<float> VisualTraceMotionSample(bool tracing,bool party)
    {
        UnityEngine.Random.InitState(2219);
        // Walking feedback uses absolute Time.time. Both comparison runs must
        // start from the same clock, not just the same random seed.
        Time.time=0f;Time.frameCount=0;Time.deltaTime=Time.unscaledDeltaTime=1f/60f;
        var samples=new List<float>();
        using(var f=new DesktopHotfixFixture(800,true)) {
            f.field.hero.traceVisualPositionChanges=tracing;
            WaveEntryScene(f);f.field.space.useMultiZoneCamera=true;WaveEntryFinish(f);
            if(party) { f.manager.CycleTestCompanions();f.manager.CycleTestCompanions(); }
            for(int i=0;i<30;i++) {
                NativeOrderTraceFrame(f,1f/60f);
                foreach(var hero in HeroController.ActiveHeroes) {
                    var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();Call(bridge,"Update");
                    RenderCallbacks(hero);
                    var p=f.field.space.Position(hero.heroRect);
                    var foot=HeroRenderProbe.ImageReferenceWorld(bridge.BodyGraphic,bridge.BodyReferenceFootUV,false);
                    samples.Add(p.x);samples.Add(p.y);samples.Add(foot.x);samples.Add(foot.y);
                    samples.Add(hero.heroRect.localScale.x);samples.Add(bridge.BodyGraphic.rectTransform.localScale.x);
                    samples.Add(hero.MovementDistanceThisFrame);samples.Add((int)bridge.CurrentPose);
                }
                samples.Add(f.field.space.LastAppliedCameraPan);
                samples.Add(f.field.space.CurrentCameraSpeed);
                var ground=f.field.ground.GetComponent<GroundPresentation>();
                samples.Add(ground.TilePhase);samples.Add(ground.FarPhase);samples.Add(ground.DistantPhase);
                samples.Add(f.field.combat.CurrentHeroCombatState.CurrentHP);
                samples.Add(f.field.combat.CurrentHeroCombatState.AttackTimer);
                samples.Add(f.manager.HeroData.isDirty?1f:0f);
            }
            foreach(var hero in HeroController.ActiveHeroes)StopRenderTrace(hero);
        }
        return samples;
    }
    static void HeroRenderTraceTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        var previous=Debug.LogObserver;
        int oldFrame=Time.frameCount,oldWidth=Screen.width,oldHeight=Screen.height;
        float oldTime=Time.time,oldDelta=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime;
        using(var resources=new SpriteResources())
        try
        {
            Run("render trace is opt-in, bounded in paused frames and unsubscribes on disable/expiry",()=>{
                var f=new Field();f.hero.heroRect.gameObject.AddComponent<Image>();f.hero.ChangeAttackMode(0);
                var logs=new List<string>();Debug.LogObserver=s=>logs.Add(s);
                f.hero.TraceVisualPosition("off");
                Check(logs.Count==0 && f.hero.GetComponent<HeroRenderProbe>()==null,"off creates no probe/event/coroutine");
                f.hero.traceVisualPositionChanges=true;f.hero.renderTraceFrameCount=10;
                f.hero.TraceVisualPosition("armed");var probe=f.hero.GetComponent<HeroRenderProbe>();
                Time.timeScale=0;int start=Time.frameCount;
                for(int i=0;i<10;i++) { Time.frameCount=start+i;RenderCallbacks(f.hero); }
                Check(logs.FindAll(s=>s.Contains("EndOfFrame after rendering")).Count==10,"ten paused end-of-frame observations");
                Check(logs.Exists(s=>s.Contains("Canvas.willRenderCanvases pass=1")),"render callback sampled");
                int before=logs.Count;Time.frameCount++;RenderCallbacks(f.hero);
                Check(!probe.IsTracing && !Get<bool>(probe,"subscribed"),"expiry removes static event");
                Canvas.InvokeWillRenderCanvases();Check(logs.Count==before,"no expired logs");
                f.hero.TraceVisualPosition("again");Call(probe,"OnDisable");before=logs.Count;
                Canvas.InvokeWillRenderCanvases();Check(logs.Count==before && !Get<bool>(probe,"subscribed"),"disable removes hook");
                f.hero.TraceVisualPosition("again 2");f.hero.traceVisualPositionChanges=false;
                RenderCallbacks(f.hero);Check(!Get<bool>(probe,"subscribed"),"turning flag off stops tracing");
                Debug.LogObserver=null;
            });
            foreach(int width in new[]{600,800,1150})foreach(AttackMode mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic})
            Run("model render foot continuity through final VFX frame and Finish "+width+"/"+mode,()=>{
                Screen.width=width;Screen.height=Mathf.RoundToInt(width*9f/16);
                var f=new Field();StableCanvasFixture(f);
                var hero=f.hero;hero.heroRect.gameObject.AddComponent<Image>();hero.ChangeAttackMode((int)mode);
                f.space.SetPosition(hero.heroRect,new Vector2(f.space.BattleArea.rect.xMin+f.space.Width*.8f,0));
                hero.traceVisualPositionChanges=true;var logs=new List<string>();Debug.LogObserver=s=>logs.Add(s);
                var fx=HeroSpawnVfx.Prepare(hero);Check(fx.Begin(),"real VFX begin");
                var position=hero.heroRect.position;var body=SpriteBody(hero.heroRect);
                var layout=body.rectTransform;var pivot=layout.pivot;var scale=layout.localScale;var size=layout.sizeDelta;
                var alpha=body.color.a;RenderCallbacks(hero);Near(body.color.a,alpha,"probe doesn't reveal body");
                fx.Advance(.35f);CheckRenderFeet(hero,"during",true);
                fx.Advance((HeroSpawnVfx.FrameCount-.5f)/HeroSpawnVfx.FramesPerSecond-.35f);
                Check(fx.DisplayedFrame==37,"actually display frame 37, do not infer it from elapsed completion");
                CheckRenderFeet(hero,"last displayed frame",true);RenderCallbacks(hero);
                Check(!fx.Advance(.1f),"finish at duration");
                SamePoint(hero.heroRect.position,position,"Finish preserves logic world");
                SamePoint(layout.localScale,scale,"Finish preserves body scale");
                Near(layout.pivot.x,pivot.x,"Finish preserves pivot X");Near(layout.pivot.y,pivot.y,"Finish preserves pivot Y");
                Near(layout.sizeDelta.x,size.x,"Finish preserves size X");Near(layout.sizeDelta.y,size.y,"Finish preserves size Y");
                CheckRenderFeet(hero,"Finish",true);
                Near(body.color.a,1,"Finish restores alpha");
                Check(!fx.EffectGraphic.gameObject.activeSelf,"VFX disabled only after completion");
                for(int i=0;i<10;i++) {
                    Time.frameCount++;Call(hero.heroRect.GetComponent<CultivationSpriteBridge>(),"Update");RenderCallbacks(hero);
                    CheckRenderFeet(hero,"stationary after Finish "+i,true);
                    SamePoint(HeroRenderProbe.LogicFootWorld(hero.heroRect),position,"stationary foot");
                }
                Check(logs.Exists(s=>s.StartsWith("[HeroRender] VFX Begin before")),"begin stage present");
                Check(logs.Exists(s=>s.StartsWith("[HeroRender] VFX Finish before")) && logs.Exists(s=>s.StartsWith("[HeroRender] VFX Finish after")),"Finish brackets present");
                string sample=logs[logs.Count-1];
                foreach(string token in new[]{"logicFootScreen=","pixelReferenceBattle=","footDeltaScreen=","worldBasisX=","ancestry=[","canvasScale=","pan=","groundRect={"})
                    Check(sample.Contains(token),"render snapshot includes "+token);
                StopRenderTrace(hero);Debug.LogObserver=null;
            });
            Run("render probe detects body-only and parent movement in common space without writing layout",()=>{
                var f=new Field();StableCanvasFixture(f);var hero=f.hero;
                hero.heroRect.gameObject.AddComponent<Image>();hero.ChangeAttackMode(0);
                var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();var body=bridge.BodyGraphic;
                var logic=HeroRenderProbe.LogicFootWorld(hero.heroRect);
                body.rectTransform.anchoredPosition+=new Vector2(7,4);
                var changed=HeroRenderProbe.ImageReferenceWorld(body,bridge.BodyReferenceFootUV,true);
                Check((changed-logic).x!=0 && (changed-logic).y!=0,"body-only deviation is measurable");
                var saved=body.rectTransform.anchoredPosition;HeroRenderProbe.Describe(hero,"read only");
                Near(body.rectTransform.anchoredPosition.x,saved.x,"Describe must not call ApplyLayout");
                Near(body.rectTransform.anchoredPosition.y,saved.y,"Describe leaves Y unchanged");
                Call(bridge,"Update");CheckRenderFeet(hero,"after bridge resets its own layout",false);
                var before=HeroRenderProbe.ImageReferenceWorld(body,bridge.BodyReferenceFootUV,false);
                // Deliberately shift in WORLD units, regardless of Canvas scale.
                hero.heroRect.parent.position+=new Vector3(40,12,0);
                var after=HeroRenderProbe.ImageReferenceWorld(body,bridge.BodyReferenceFootUV,false);
                Near(after.x-before.x,40,"parent movement visible in common world X");
                Near(after.y-before.y,12,"parent movement visible in common world Y");
                CheckRenderFeet(hero,"parent moved",false);
            });
            foreach(bool party in new[]{false,true})
            Run("render tracing leaves Multi-Zone pan, body transforms, combat and dirty state unchanged party="+party,()=>{
                Debug.LogObserver=s=>{};
                var off=VisualTraceMotionSample(false,party);var on=VisualTraceMotionSample(true,party);
                Check(off.Count==on.Count,"same actors/frame count");
                for(int i=0;i<off.Count;i++)Near(on[i],off[i],"read-only sample "+i);
                Debug.LogObserver=null;
            });
            Run("body registration stays at logical feet during real walk/attack feedback and all three poses",()=>{
                var f=new Field();StableCanvasFixture(f);var hero=f.hero;
                hero.heroRect.gameObject.AddComponent<Image>();
                foreach(int mode in new[]{0,1,2}) {
                    hero.ChangeAttackMode(mode);var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();
                    var before=f.space.Position(hero.heroRect);
                    Time.deltaTime=1f/60;Time.time+=Time.deltaTime;hero.TickMovement(f.space,Time.deltaTime);
                    CheckRenderFeet(hero,"walking",false);
                    var after=f.space.Position(hero.heroRect);
                    Near(before.x-after.x,hero.EffectiveMoveSpeed()*Time.deltaTime,"continuous walking in BattleArea");
                    SamePoint(HeroRenderProbe.ImageReferenceWorld(bridge.BodyGraphic,bridge.BodyReferenceFootUV,false),
                        hero.heroRect.position,"walk scale preserves foot");
                    hero.PlayAttackFeedback();
                    Check(bridge.CurrentPose==CultivationSpriteBridge.Pose.Attack,"actual attack hook");
                    CheckRenderFeet(hero,"Attack root scale feedback",false);
                    Get<Coroutine>(hero,"attackFeedbackCoroutine").routine.MoveNext();
                    Time.time+=.17f;Call(bridge,"Update");
                    Check(bridge.CurrentPose==CultivationSpriteBridge.Pose.Normal,"Attack expires normally");
                    CultivationSpriteBridge.Hit(hero.heroRect);
                    Check(bridge.CurrentPose==CultivationSpriteBridge.Pose.Hit,"idle Hit still works");
                    CheckRenderFeet(hero,"Hit",false);
                }
            });
        }
        finally
        {
            foreach(var hero in new List<HeroController>(HeroController.ActiveHeroes))StopRenderTrace(hero);
            Debug.LogObserver=previous;Time.frameCount=oldFrame;Time.time=oldTime;Time.deltaTime=oldDelta;
            Time.unscaledDeltaTime=oldUnscaled;Screen.width=oldWidth;Screen.height=oldHeight;
            Resources.Unregister("Spawn/Appear");
        }
    }
}
