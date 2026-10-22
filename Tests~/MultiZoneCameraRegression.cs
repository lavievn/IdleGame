using System;
using System.IO;
using UnityEngine;

partial class MotionRegression
{
    static float MultiPan(MultiZoneCamera camera,float x,float dt)
    {
        return camera.Pan(x,-960,960,dt,.2f,.45f,.75f,30,450,900,1200,900,48);
    }
    static void MultiZoneCameraTests()
    {
        foreach(int fps in new[]{30,60,144})foreach(float initial in new[]{-900f,-576f,-220f,-20f,20f,220f,576f,900f})
        Run("multi-zone continuous acceleration, finite rest and no strong overshoot "+fps+"/"+initial,()=>{
            var camera=new MultiZoneCamera();float x=initial,dt=1f/fps;bool rested=false;
            for(int i=0;i<fps*45;i++) {
                float old=camera.Velocity;float pan=MultiPan(camera,x,dt);x+=pan;
                Check(Mathf.Abs(camera.Velocity-old)<=1200*dt+.01f,"bounded acceleration/deceleration");
                Check(Mathf.Abs(pan)<=900*dt+.01f,"bounded pan speed");
                Check(initial*x>=-.1f,"no visible oscillation beyond center");
                if(Mathf.Abs(x)<.002f && Mathf.Abs(camera.Velocity)<.01f) { rested=true;break; }
            }
            Check(rested,"finite settling, not endless Lerp");
            for(int i=0;i<30;i++)Near(MultiPan(camera,x,dt),0,"no drift after rest");
        });
        Run("multi-zone symmetric continuous response at all boundaries",()=>{
            foreach(float edge in new[]{192f,432f,720f}) {
                float a=MultiZoneCamera.ResponseSpeed(edge-.001f,1920,.2f,.45f,.75f,30,450,900);
                float b=MultiZoneCamera.ResponseSpeed(edge+.001f,1920,.2f,.45f,.75f,30,450,900);
                Near(a,b,"no discontinuity at zone edge",.01f);
            }
            var left=new MultiZoneCamera();var right=new MultiZoneCamera();
            for(int i=0;i<120;i++)Near(MultiPan(left,-576,1f/60f),-MultiPan(right,576,1f/60f),"symmetric pan");
        });
        Run("multi-zone direction reversal brakes and pause retains velocity without moving",()=>{
            var camera=new MultiZoneCamera();for(int i=0;i<60;i++)MultiPan(camera,-900,1f/60f);
            Check(camera.Velocity>0,"initial positive velocity");
            float old=camera.Velocity;Near(MultiPan(camera,900,0),0,"no paused movement");Near(camera.Velocity,old,"pause retains state");
            for(int i=0;i<120;i++) {
                old=camera.Velocity;MultiPan(camera,900,1f/60f);
                Check(Mathf.Abs(camera.Velocity-old)<=1200/60f+.01f,"no reversal snap");
            }
            Check(camera.Velocity<0,"eventually reverses");
        });
        Run("multi-zone reversal crossing zero uses acceleration only for the remaining time",()=>{
            var camera=new MultiZoneCamera();Set(camera,"Velocity",.1f);
            const float dt=1f/120f;
            MultiPan(camera,900,dt);
            Near(camera.Velocity,-900f*(dt-.1f/1200f),"deceleration budget not reused as opposite acceleration");
        });
        foreach(int fps in new[]{30,60,144})foreach(float speed in new[]{200f,450f})
        Run("multi-zone follows moving Hero without escaping viewport "+fps+"/"+speed,()=>{
            var camera=new MultiZoneCamera();float x=576f,lastSecondPan=0f;
            for(int i=0;i<fps*20;i++) {
                x-=speed/fps;float applied=MultiPan(camera,x,1f/fps);x+=applied;
                if(i>=fps*19)lastSecondPan+=applied;
                Check(x>-960f && x<960f,"live moving focus stays on screen");
            }
            // Velocity is sampled after the last integration substep. The
            // actual presentation speed is total applied pan / elapsed time.
            Check(Mathf.Abs(lastSecondPan-speed)<2f,"applied camera speed keeps up: "+lastSecondPan+" vs "+speed+" x="+x);
            float old=camera.Velocity;float pan=MultiPan(camera,x,2f);
            Check(Mathf.Abs(pan)<=48.001f,"render stall has bounded step");
            Check(Mathf.Abs(camera.Velocity-old)<=1200f*(48f/900f)+.01f,"stall does not bypass acceleration limit");
        });
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        using(var sprites=new SpriteResources())
        foreach(int width in new[]{600,800,1150})foreach(int count in new[]{1,3})
        Run("multi-zone entry, shared Ground pan, casualty rebase and B2 "+width+"/"+count,()=>{
            using(var f=new DesktopHotfixFixture(width,true)) {
                WaveEntryScene(f);var space=f.field.space;space.useMultiZoneCamera=true;space.ResetCameraForMapEntry();
                WaveEntryFinish(f);var primary=f.field.hero;
                Near(space.Position(primary.heroRect).x,576,"80 percent preserved at release");
                Check(!space.IsCameraEntryHandoff,"no independent handoff controller");
                for(int i=1;i<count;i++)f.manager.CycleTestCompanions();
                var terrain=f.field.ground.GetComponent<GroundPresentation>();
                for(int i=0;i<45;i++) {
                    float before=terrain.TilePhase,velocity=space.MultiZoneVelocity;
                    NativeOrderTraceFrame(f,1f/60f);Call(terrain,"LateUpdate");
                    Near(space.CurrentCameraSpeed,space.CurrentBackgroundSpeed,"same Ground and actor pan");
                    Near(terrain.TilePhase,BattleMotion.Wrap(before+space.LastAppliedCameraPan,0,space.Width),"tile phase follows shared pan");
                    Check(Mathf.Abs(space.MultiZoneVelocity-velocity)<=1200/60f+.01f,"entry and gameplay use bounded velocity");
                }
                if(count==3) {
                    // Death BETWEEN the shared Update plan and LateUpdate must
                    // neither reset nor integrate the camera for a second time.
                    Time.frameCount++;Time.deltaTime=1f/60f;Call(space,"Update");
                    float velocity=space.MultiZoneVelocity;
                    primary.Die();Call(space,"LateUpdate");
                    Near(space.MultiZoneVelocity,velocity,"death does not reset or double-step velocity");
                    float expectedFocus=space.CurrentCameraSmoothedFocusX+space.LastAppliedCameraPan;
                    foreach(var h in HeroController.ActiveHeroes)h.moveSpeed=0;
                    Time.frameCount++;Call(space,"Update");Call(space,"LateUpdate");
                    Check(Get<HeroController>(space,"cameraTarget")!=primary,"survivor becomes subject");
                    Check(Mathf.Abs(space.CurrentCameraSmoothedFocusX-expectedFocus)<10,"absolute focus rebased, no anchor jump");
                }
            }
        });
    }
}
