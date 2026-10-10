using System;
using UnityEngine;

// Soft-zone camera regression. Unity stubs test coordinate/time logic;
// final visual feel still needs real Unity and Windows playback.
partial class MotionRegression
{
    static void CameraFollowTests()
    {
        Run("54g soft camera accelerates on LEFT breach, then releases to normal scroll",()=>{
            var camera = new SoftZoneCamera();
            float x = -205f;
            float initialSpeed=0f, speedAfterOneSecond=0f;
            for(int i=0;i<10;i++)
            {
                x -= 20f; // Hero faster than the default camera.
                float pan=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,
                    .1f,1f,.62f);
                x += pan;
                if (i==0) initialSpeed=pan/.1f;
                if (i==9) speedAfterOneSecond=pan/.1f;
                if (i==0) Check(x < -200f,"red border is NOT a hard clamp");
            }
            Check(camera.Phase == SoftCameraPhase.Accelerating,"first chase active");
            Near(camera.AccelerationElapsed,1f,"one-second acceleration window",.001f);
            Check(speedAfterOneSecond > initialSpeed+100f,"speed ramps gradually");

            bool sawRelease=false, sawNormal=false;
            for(int i=0;i<80;i++)
            {
                x-=20f;
                x+=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,.1f,1f,.62f);
                if (camera.Phase==SoftCameraPhase.Releasing) sawRelease=true;
                if (sawRelease && camera.Phase==SoftCameraPhase.Normal) { sawNormal=true; break; }
            }
            Check(sawRelease&&sawNormal,"chase ends and restores baseline camera mode");
            float previous=x;
            x-=20f;
            float restoredPan=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,.1f,1f,.62f);
            x+=restoredPan;
            Near(restoredPan,5f,"back to ORIGINAL 50-units/s scroll");
            Check(x<previous-10f,"Hero is free to drift after recovery");

            bool secondChase=false;
            for(int i=0;i<70;i++)
            {
                x-=20f;
                x+=camera.Pan(x,20f,-200f,200f,-500f,500f,50f,.1f,1f,.62f);
                if(camera.Phase==SoftCameraPhase.Accelerating){secondChase=true;break;}
            }
            Check(secondChase,"next red-border crossing triggers another catch-up");
        });

        Run("54g RIGHT breach returns smoothly, then camera scrolls again",()=>{
            var camera = new SoftZoneCamera();
            float x=205f;
            bool crossedFartherRight=false, sawRelease=false, sawNormal=false;
            for(int i=0;i<100;i++)
            {
                x+=camera.Pan(x,0f,-200f,200f,-500f,500f,150f,.1f,1f,.62f);
                if(x>210f) crossedFartherRight=true;
                if(camera.Phase==SoftCameraPhase.Releasing) sawRelease=true;
                if(sawRelease&&camera.Phase==SoftCameraPhase.Normal){sawNormal=true;break;}
            }
            Check(crossedFartherRight,"Hero can move beyond red right edge");
            Check(sawRelease&&sawNormal,"right-side recovery has a finite end");
            float position=x;
            float normalPan=camera.Pan(x,0f,-200f,200f,-500f,500f,150f,.1f,1f,.62f);
            Near(normalPan,15f,"idle Hero does not freeze camera forever");
            Check(position+normalPan>position,"Hero drifts right as normal scrolling resumes");
        });

        Run("54g slower Hero reaches return target without asymptotic Following lock",()=>{
            var camera=new SoftZoneCamera();
            float x=205f;
            bool released=false, normalAgain=false;
            for(int i=0;i<100;i++)
            {
                x-=4f; // 40 units/s movement against 150 units/s scrolling.
                x+=camera.Pan(x,4f,-200,200,-500,500,150,.1f,1f,.62f);
                if(camera.Phase==SoftCameraPhase.Releasing) released=true;
                if(released&&camera.Phase==SoftCameraPhase.Normal){normalAgain=true;break;}
            }
            Check(released&&normalAgain,"corrected pre-walk error eventually ends chase");
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
