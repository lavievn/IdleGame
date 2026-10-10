using System;
using UnityEngine;
using TuTienCore;

// Camera tests run with Unity stubs, not actual Editor/Windows Play Mode.
partial class MotionRegression
{
    // HeroController applies its walking step BEFORE the camera evaluates pan.
    private static float StepDeadzone(DeadzoneCamera camera, ref float heroX,
        float speed, float dt, float width=1000f)
    {
        heroX -= speed*dt;
        float pan=camera.Pan(heroX,-227.5f,197.5f,-width*.5f,width*.5f,
            dt,.2f,.38f,.5f,speed*dt>.001f,.4f,.6f);
        heroX += pan;
        return pan;
    }

    static void CameraFollowTests()
    {
        Run("54g deadzone: stationary Hero cannot cause independent camera scrolling",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var c=new DeadzoneCamera();float x=0f;
                for(int i=0;i<fps*3;i++)
                {
                    Near(StepDeadzone(c,ref x,0f,1f/fps),0f,"idle pan",.0001f);
                    Near(c.Velocity,0f,"no velocity from legacy scrollSpeed",.0001f);
                }
                Near(x,0f,"Hero stays where deployed",.0001f);
            }
        });

        Run("54g deadzone: Hero approaches RED zone before camera follows",()=>{
            var c=new DeadzoneCamera();float x=0f;
            // Default 38% Ground width: first 180 units are freely walkable.
            for(int i=0;i<9;i++)
                Near(StepDeadzone(c,ref x,200f,.1f),0f,
                    "inside broad deadzone frame "+i,.0001f);
            Near(x,-180f,"Hero almost reaches left red region unaided");
            float tenth=StepDeadzone(c,ref x,200f,.1f);
            Check(tenth>0f&&tenth<20f,"camera starts gently once Hero passes -190");
            Check(x < -190f,"camera does not snap Hero to deadzone border");
        });

        Run("54g deadzone: smooth catch-up then smooth deceleration on aim",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                float dt=1f/fps,x=0f;var c=new DeadzoneCamera();
                for(int i=0;i<fps*2;i++)
                    StepDeadzone(c,ref x,320f,dt);
                Check(x < -190f && x > -330f,"Hero can lead near the red border while moving");
                Check(c.Velocity>200f,"camera gained following velocity");

                float before=c.Velocity;
                StepDeadzone(c,ref x,0f,dt);
                Check(c.Velocity<before,"brakes immediately once Hero stops");
                Check(c.IsIdleRecovering,"running Hero stopping left of 40% starts idle recovery");
                float previous=x;
                for(int i=0;i<fps*2;i++)
                {
                    StepDeadzone(c,ref x,0f,dt);
                    Check(x>=previous-.01f,"idle recovery does not shake camera backwards");
                    previous=x;
                }
                Near(c.Velocity,0f,"camera eventually comes to rest",.3f);
                Check(x>=-101f&&x<=-99f,"settles gently at 40% of Ground width");
                Check(!c.IsIdleRecovering,"return to rest is a finite, not perpetual, action");
                Near(StepDeadzone(c,ref x,0f,dt),0f,
                    "stationary aiming no autonomous background pan",.01f);
            }
        });

        Run("54g deadzone: two move-stop cycles do not freeze or oscillate",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var c=new DeadzoneCamera();float x=0f,dt=1f/fps;
                for(int cycle=0;cycle<2;cycle++)
                {
                    bool hadMovement=false;
                    for(int i=0;i<fps*2;i++)
                        if(StepDeadzone(c,ref x,350f,dt)>0f) hadMovement=true;
                    Check(hadMovement,"camera follows again on cycle "+cycle);
                    for(int i=0;i<fps*2;i++)
                        StepDeadzone(c,ref x,0f,dt);
                    Near(c.Velocity,0f,"stopped after cycle "+cycle,.02f);
                    Check(x>=-101f&&x<=-99f,
                        "settles at 40% without shaking on cycle "+cycle);
                }
            }
        });

        Run("54g red border: speed up follow without hard-clamping Hero",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                float dt=1f/fps,x=-235f;var c=new DeadzoneCamera();
                StepDeadzone(c,ref x,1500f,dt);
                Check(x< -227.5f,"Hero can go past RED edge");
                Check(x > -470f,"but still visible in physical screen");
                for(int i=0;i<fps*2;i++)
                {
                    StepDeadzone(c,ref x,1500f,dt);
                    Check(x>=-470f&&x<=470f,"physical viewport protects only true edges");
                }
                Check(x>-460f,"red border boosts recovery without touching real viewport edge");
            }
        });

        Run("54g deadzone: right-side excursion recovers without camera oscillation",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var c=new DeadzoneCamera();float x=220f,last=x,dt=1f/fps;
                for(int i=0;i<fps*3;i++)
                {
                    StepDeadzone(c,ref x,0f,dt);
                    Check(x<=last+.01f,"never snaps back right while recovering");
                    last=x;
                }
                Check(x>=189f&&x<=191f,"right deadzone edge is resting location");
                Near(c.Velocity,0f,"no residual shake",.1f);
            }
        });

        Run("54g deadzone: pause and respawn reset camera momentum",()=>{
            var c=new DeadzoneCamera();float x=-100f;
            c.Pan(x,-227.5f,197.5f,-500f,500f,.1f,.2f,.38f,.5f,
                true,.4f,.6f);
            float velocity=c.Velocity;
            Near(c.Pan(x,-227.5f,197.5f,-500f,500f,0f,.2f,.38f,.5f,
                false,.4f,.6f),
                0f,"paused pan zero");
            Near(c.Velocity,velocity,"paused velocity unchanged");
            c.Reset();
            Near(c.Velocity,0f,"respawn clears follow inertia");
        });

        Run("54g Environment: Hero idle and inside deadzone keeps entire Ground still",()=>{
            var f=new Field();f.space.useSoftZoneCamera=true;
            f.space.cameraSmoothTime=.2f;f.space.cameraDeadzoneRatio=.38f;
            f.space.cameraPreferredX=.5f;
            f.space.cameraIdleRestX=.4f;
            f.space.cameraIdleSmoothTime=.6f;
            f.space.scrollSpeed=150f; // Must have no effect on living Hero.
            f.hero.moveSpeed=0f;
            float hero=f.space.Position(f.hero.heroRect).x;
            float ground=f.space.Position(f.grass).x;
            for(int i=0;i<90;i++)f.Step(1f/60f);
            Near(f.space.Position(f.hero.heroRect).x,hero,"stationary Hero",.01f);
            Near(f.space.Position(f.grass).x,ground,"Ground does not auto-scroll",.01f);
            Near(f.space.CurrentCameraSpeed,0f,"no legacy camera base speed",.01f);

            f.hero.moveSpeed=200f;
            for(int i=0;i<3;i++)f.Step(.05f); // 30 Ground units, inside deadzone.
            Near(f.space.Position(f.grass).x,ground,
                "Ground remains still while Hero walks inside deadzone",.01f);
            Near(f.space.CurrentBackgroundSpeed,0f,
                "Ground does not scroll by heroWalkDistance",.01f);

            for(int i=0;i<60;i++)f.Step(1f/60f);
            Check(f.space.CurrentCameraSpeed>0f,"camera starts following after zone exit");
            Near(f.space.CurrentBackgroundSpeed,f.space.CurrentCameraSpeed,
                "Ground pan equals actor and projectile camera pan",.01f);

            f.hero.moveSpeed=0f;
            for(int i=0;i<90;i++)f.Step(1f/60f);
            Near(f.space.CurrentCameraSpeed,0f,"camera stops after Hero stops",.1f);
            Near(f.space.CurrentBackgroundSpeed,0f,"Ground also stops",.1f);
            f.space.FollowHero(f.hero);
            Near(f.space.CameraFollowVelocity,0f,"Retry resets inertia",.01f);
        });

        Run("54g speed 700 camera engages near RED and stops smoothly",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var c=new DeadzoneCamera();
                float dt=1f/fps,x=0f;
                bool everFollowed=false, movedBeforeThreshold=false;
                float firstFollowHeroX=0f, peak=0f;
                for(int i=0;i<fps*2;i++)
                {
                    float pre=x-700f*dt;
                    float pan=StepDeadzone(c,ref x,700f,dt);
                    if(pre > -190f && pan > .001f) movedBeforeThreshold=true;
                    if(!everFollowed && pan > .001f)
                    {
                        everFollowed=true;
                        firstFollowHeroX=x;
                    }
                    peak=Math.Min(peak,x);
                }
                Check(!movedBeforeThreshold,"camera is still until Hero reaches left -190");
                Check(everFollowed,"camera eventually follows fast Hero");
                Check(firstFollowHeroX < -189f && firstFollowHeroX > -260f,
                    "initial follow is close to red rather than center");
                Check(peak < -227.5f,"red border remains SOFT at speed 700");
                for(int i=0;i<2*fps;i++)StepDeadzone(c,ref x,0f,dt);
                Near(c.Velocity,0f,"standing Hero has no camera drift",.1f);
                Check(x>-101f && x< -99f,"fast Hero returns softly to 40% when stopped");
            }
        });

        Run("54g slow Hero stopping before 40% causes no autonomous repositioning",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var c=new DeadzoneCamera();float x=0f,dt=1f/fps;
                for(int i=0;i<fps/2;i++)StepDeadzone(c,ref x,100f,dt);
                Check(x>-100f && x<0f,"slow Hero did not reach idle rest point");
                float original=x;
                for(int i=0;i<fps*2;i++)
                    Near(StepDeadzone(c,ref x,0f,dt),0f,
                        "slow Hero stopping does not activate camera",.0001f);
                Near(x,original,"slow Hero remains near central area",.001f);
                Check(!c.IsIdleRecovering,"no unnecessary idle recenter");
            }
        });

        Run("54g idle return is interrupted by resumed Hero walking without a shake",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var c=new DeadzoneCamera();float x=0f,dt=1f/fps;
                for(int i=0;i<2*fps;i++) StepDeadzone(c,ref x,700f,dt);
                for(int i=0;i<fps/4;i++) StepDeadzone(c,ref x,0f,dt);
                Check(c.IsIdleRecovering,"idle return active before resuming movement");
                StepDeadzone(c,ref x,700f,dt);
                Check(!c.IsIdleRecovering,"moving immediately cancels idle recenter");
                for(int i=0;i<2*fps;i++)StepDeadzone(c,ref x,700f,dt);
                for(int i=0;i<2*fps;i++)StepDeadzone(c,ref x,0f,dt);
                Near(x,-100f,"second stop also converges to 40%",1f);
                Near(c.Velocity,0f,"no lingering camera motion",.1f);
            }
        });

        Run("54g scene camera recovery: stationary Hero and Ground move together",()=>{
            var f=new Field();
            f.space.useSoftZoneCamera=true;
            f.space.cameraDeadzoneRatio=.38f;
            f.space.cameraIdleRestX=.4f;
            f.space.cameraIdleSmoothTime=.6f;
            f.hero.moveSpeed=700f;
            for(int i=0;i<120;i++)f.Step(1f/60f);
            float before=f.space.Position(f.hero.heroRect).x;
            Check(before < -190f,"fast Hero leads camera before stopping");
            f.hero.moveSpeed=0f;
            float heroBefore=f.space.Position(f.hero.heroRect).x;
            float groundBefore=f.space.Position(f.grass).x;
            f.Step(1f/60f);
            float heroPan=f.space.Position(f.hero.heroRect).x-heroBefore;
            float groundPan=f.space.Position(f.grass).x-groundBefore;
            Check(heroPan>0f,"after stopping, Hero drifts right with camera");
            Near(groundPan,heroPan,"Ground drifts by same camera pan",.05f);
            for(int i=0;i<120;i++)f.Step(1f/60f);
            Near(f.space.Position(f.hero.heroRect).x,-100f,
                "physical Hero recovers to 40% while stopped",1f);
            Near(f.space.CurrentCameraSpeed,0f,"camera stops after return",.2f);
        });

        Run("54g melee Hero approaching ranged quái: both share the same Ground pan",()=>{
            var f=new Field();
            f.space.useSoftZoneCamera=true;
            f.space.cameraSmoothTime=.2f;f.space.cameraDeadzoneRatio=.38f;
            f.hero.ChangeAttackMode(0);f.hero.moveSpeed=600f;
            var ranged=f.Monster(-110f,AttackMode.RangedPhysical);
            float before=f.space.Position(ranged.Rect).x;
            f.Step(.1f);
            Check(ranged.currentState==MonsterState.Attacking,
                "ranged defender stands and shoots");
            Near(f.space.Position(ranged.Rect).x-before,
                f.space.CurrentBackgroundSpeed*.1f,
                "stationary ranged defender moves exactly with Ground",.05f);
            for(int i=0;i<90;i++)f.Step(1f/60f);
            Near(f.space.CurrentCameraSpeed,0f,
                "camera eventually settles when melee Hero stops in range",.3f);
        });
    }
}
