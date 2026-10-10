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
            dt,.2f,.1f,.5f);
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

        Run("54g deadzone: Hero moves freely INSIDE the central 10 percent",()=>{
            var c=new DeadzoneCamera();float x=0f;
            Near(StepDeadzone(c,ref x,200f,.1f),0f,"first 20-unit step",.0001f);
            Near(x,-20f,"Hero moves with camera stationary");
            Near(StepDeadzone(c,ref x,200f,.1f),0f,"second 20-unit step",.0001f);
            Near(x,-40f,"still in deadzone");
            float third=StepDeadzone(c,ref x,200f,.1f);
            Check(third>0f&&third<20f,"crossing left deadzone begins smooth following");
            Check(x < -50f,"deadzone edge is not an instant hard clamp");
        });

        Run("54g deadzone: smooth catch-up then smooth deceleration on aim",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                float dt=1f/fps,x=0f;var c=new DeadzoneCamera();
                for(int i=0;i<fps*2;i++)
                    StepDeadzone(c,ref x,320f,dt);
                Check(x < -50f && x > -150f,"Hero can lead while moving");
                Check(c.Velocity>200f,"camera gained following velocity");

                float before=c.Velocity;
                StepDeadzone(c,ref x,0f,dt);
                Check(c.Velocity<before,"brakes immediately once Hero stops");
                for(int i=0;i<fps;i++)
                    StepDeadzone(c,ref x,0f,dt);
                Near(c.Velocity,0f,"camera eventually comes to rest",.3f);
                Check(x>=-55f&&x<=-49f,"stops at deadzone edge, not across red border");
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
                    Check(x>=-55f&&x<=-49f,
                        "no overshoot/right-side shaking on cycle "+cycle);
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
                Check(x>-270f,"red border boosts recovery to keep fast Hero in frame");
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
                Check(x>=49f&&x<=51f,"right deadzone edge is resting location");
                Near(c.Velocity,0f,"no residual shake",.1f);
            }
        });

        Run("54g deadzone: pause and respawn reset camera momentum",()=>{
            var c=new DeadzoneCamera();float x=-100f;
            c.Pan(x,-227.5f,197.5f,-500f,500f,.1f,.2f,.1f,.5f);
            float velocity=c.Velocity;
            Near(c.Pan(x,-227.5f,197.5f,-500f,500f,0f,.2f,.1f,.5f),
                0f,"paused pan zero");
            Near(c.Velocity,velocity,"paused velocity unchanged");
            c.Reset();
            Near(c.Velocity,0f,"respawn clears follow inertia");
        });

        Run("54g Environment: Hero idle and inside deadzone keeps entire Ground still",()=>{
            var f=new Field();f.space.useSoftZoneCamera=true;
            f.space.cameraSmoothTime=.2f;f.space.cameraDeadzoneRatio=.1f;
            f.space.cameraPreferredX=.5f;
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

        Run("54g melee Hero approaching ranged quái: both share the same Ground pan",()=>{
            var f=new Field();
            f.space.useSoftZoneCamera=true;
            f.space.cameraSmoothTime=.2f;f.space.cameraDeadzoneRatio=.1f;
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
