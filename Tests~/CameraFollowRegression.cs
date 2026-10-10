using System;
using UnityEngine;

// Soft-zone camera regression. Unity stubs test coordinate/time logic;
// final visual feel still needs real Unity and Windows playback.
partial class MotionRegression
{
    static void CameraFollowTests()
    {
        Run("54g soft camera accelerates for 1s AFTER crossing the red LEFT edge",()=>{
            var camera = new SoftZoneCamera();
            float x = -205f;
            float initialSpeed=0f, speedAfterOneSecond=0f;
            for(int i=0;i<10;i++)
            {
                x -= 20f; // Hero moves faster than the normal 50-unit camera.
                float pan=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,
                    .1f,1f,.62f);
                x += pan;
                if (i==0) initialSpeed=pan/.1f;
                if (i==9) speedAfterOneSecond=pan/.1f;
                if (i==0) Check(x < -200f,"Hero passes red line, not pinned at it");
            }
            Check(camera.Phase == SoftCameraPhase.Accelerating,
                "a catch-up is triggered at the red boundary");
            Near(camera.AccelerationElapsed,1f,"one-second acceleration window",.001f);
            Check(speedAfterOneSecond > initialSpeed+100f,
                "camera accelerates progressively instead of an instant hard clamp");
            for(int i=0;i<30;i++)
            {
                x-=20f;
                x+=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,.1f,1f,.62f);
            }
            Check(camera.Phase==SoftCameraPhase.Following,"catch-up completes");
            Near(x,120f,"Hero rests around 62% of 1000-unit view",.5f);
            x-=20f;
            x+=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,.1f,1f,.62f);
            Near(x,120f,"camera keeps pace after catch-up, no loop",.5f);
        });

        Run("54g soft camera handles right edge without a hard red clamp",()=>{
            var camera = new SoftZoneCamera();
            float x=205f;
            bool crossedFartherRight=false;
            for(int i=0;i<50;i++)
            {
                float pan=camera.Pan(x,0f,-200f,200f,-500f,500f,150f,.1f,1f,.62f);
                x+=pan;
                if(x>210f) crossedFartherRight=true;
            }
            Check(crossedFartherRight,"stationary Hero can pass the red right edge");
            Check(camera.Phase==SoftCameraPhase.Following,"reached stable position");
            Near(x,120f,"right-side recovery also ends near 62%",.5f);
            Near(camera.Pan(x,0f,-200f,200f,-500f,500f,150f,.1f,1f,.62f),
                0f,"camera stops when Hero is stationary",.01f);
        });

        Run("54g normal scroll stays unchanged while Hero remains inside red",()=>{
            var camera=new SoftZoneCamera();
            float x=0f;
            for(int i=0;i<60;i++)
            {
                x-=15f;
                float pan=camera.Pan(x,15f,-200,200,-500,500,150,.1f,1f,.62f);
                x+=pan;
                Near(pan,15f,"normal camera unchanged",.0001f);
                Check(camera.Phase==SoftCameraPhase.Normal,"no unnecessary recenter");
            }
            Near(x,0,"Hero retains off-center within red area");
        });

        Run("54g pause/reset does not keep stale camera acceleration",()=>{
            var camera=new SoftZoneCamera();
            camera.Pan(-220f,20f,-200,200,-500,500,50,.1f,1f,.62f);
            float elapsed=camera.AccelerationElapsed;
            Near(camera.Pan(-220f,20f,-200,200,-500,500,50,0f,1f,.62f),
                0f,"pause produces no camera motion");
            Near(camera.AccelerationElapsed,elapsed,"acceleration clock frozen during pause");
            camera.Reset();
            Check(camera.Phase==SoftCameraPhase.Normal && camera.AccelerationElapsed==0,
                "retry/new Hero clears camera state");
        });

        Run("54g Environment uses same camera pan for moving quái and Ground",()=>{
            var f=new Field();
            f.space.useSoftZoneCamera=true;
            f.space.cameraAccelerationSeconds=1f;
            f.space.cameraPreferredX=.62f;
            f.space.scrollSpeed=50f;
            f.hero.moveSpeed=200f;
            f.space.SetPosition(f.hero.heroRect,new Vector2(f.space.ZoneLeftX-10f,0f));
            var m=f.Monster(-900f);
            float before=f.space.Position(m.Rect).x;
            f.Step(.1f);
            Check(f.space.CameraPhase==SoftCameraPhase.Accelerating,
                "real runtime enters soft catch-up once");
            float actual=f.space.Position(m.Rect).x-before;
            float expected=m.moveSpeed*.1f + f.space.CurrentBackgroundSpeed*.1f;
            Near(actual,expected,"monster walk plus Ground pan, not duplicate camera",.05f);
            Near(f.space.CurrentBackgroundSpeed,200f,"Ground follows actual hero motion",.05f);
            float elapsed=f.space.CameraPhase==SoftCameraPhase.Accelerating ? .1f : 0f;
            f.Step(.1f);
            Check(f.space.CameraPhase==SoftCameraPhase.Accelerating,
                "phase does not advance twice between Update/LateUpdate");
            f.space.FollowHero(f.hero);
            Check(f.space.CameraPhase==SoftCameraPhase.Normal,
                "respawn or explicit FollowHero resets camera");

            // Critical regression: a ranged monster can STOP to attack while
            // the melee Hero keeps advancing and soft-camera acceleration runs.
            var f2=new Field();
            f2.space.useSoftZoneCamera=true;
            f2.space.scrollSpeed=50f; f2.hero.moveSpeed=200f;
            f2.hero.ChangeAttackMode(0);
            f2.space.SetPosition(f2.hero.heroRect,new Vector2(f2.space.ZoneLeftX-10f,0f));
            var ranged=f2.Monster(f2.space.ZoneLeftX-210f,AttackMode.RangedPhysical);
            float rangedBefore=f2.space.Position(ranged.Rect).x;
            f2.Step(.1f);
            Check(ranged.currentState==MonsterState.Attacking,
                "ranged monster stops and fires before melee Hero arrives");
            Near(f2.space.Position(ranged.Rect).x-rangedBefore,
                f2.space.CurrentBackgroundSpeed*.1f,
                "standing ranged monster stays coupled to Ground during camera chase",.05f);
        });
    }
}
