using System;
using UnityEngine;

partial class MotionRegression
{
    static void CameraDelayTests()
    {
        Run("54g camera waits 1s at red border, then eases Hero to 62 percent",()=>{
            var recovery = new CameraEdgeRecovery();
            float x=200f;
            // Walk left relative to the normal scrolling background.
            for(int i=0;i<4;i++)
            {
                x-=30f;
                x+=recovery.Pan(x,200f,800f,0f,1000f,40f,.25f,1f,1.25f,.62f);
                if(i<3) Check(recovery.Phase==CameraReturnPhase.Waiting,"must wait full second");
            }
            Check(recovery.Phase==CameraReturnPhase.Returning,"recovery begins after 1s");
            Check(x<200f,"hero allowed past RED zone during the delay");
            float start=x;
            x-=30f;
            x+=recovery.Pan(x,200,800,0,1000,40,.25f,1f,1.25f,.62f);
            Check(x>start&&x<620f,"return begins smoothly without a teleport");
            for(int i=0;i<4;i++)
            {
                x-=30f;
                x+=recovery.Pan(x,200,800,0,1000,40,.25f,1f,1.25f,.62f);
            }
            Near(x,620f,"end at horizontal 62 percent",.05f);
            Check(recovery.Phase==CameraReturnPhase.Scrolling,"camera correction stops at target");
            x-=30f;
            x+=recovery.Pan(x,200,800,0,1000,40,.25f,1f,1.25f,.62f);
            Check(recovery.Phase==CameraReturnPhase.Scrolling,"no repeat while still within red zone");
        });
        Run("54g right red border also waits before returning towards center-right",()=>{
            var c=new CameraEdgeRecovery();
            float x=800;
            for(int i=0;i<4;i++)
                x+=c.Pan(x,200,800,0,1000,100,.25f,1,1.25f,.62f);
            Check(c.Phase==CameraReturnPhase.Returning,"right edge follows same one-second delay");
            Check(x>800f&&x<=970f,"wait inside physical screen even beyond red edge");
            for(int i=0;i<5;i++)x+=c.Pan(x,200,800,0,1000,100,.25f,1,1.25f,.62f);
            Near(x,620,"right edge returns to same target",.05f);
        });
        Run("54g camera delay freezes on pause and resets after respawn",()=>{
            var c=new CameraEdgeRecovery();
            c.Pan(170,200,800,0,1000,150,.1f,1f,1.25f,.62f);
            float elapsed=c.Elapsed;
            Near(c.Pan(170,200,800,0,1000,150,0f,1f,1.25f,.62f),0f,"paused pan zero");
            Near(c.Elapsed,elapsed,"pause does not advance countdown");
            c.Reset();
            Check(c.Phase==CameraReturnPhase.Scrolling&&c.Elapsed==0f,"respawn clears pending delay");
        });
        Run("54g real EnvironmentManager buffers the same pan for AI and LateUpdate",()=>{
            var f=new Field();
            f.space.delayedCameraReturn=true;
            f.space.cameraEdgeDelay=1f;
            f.space.cameraReturnSeconds=1.25f;
            f.hero.moveSpeed=120f;
            f.space.scrollSpeed=40f;
            f.space.SetPosition(f.hero.heroRect,new Vector2(f.space.ZoneLeftX+1f,0f));
            var walkingMonster=f.Monster(-1200f); // Deliberately well out of attack range.
            float monsterStart=f.space.Position(walkingMonster.Rect).x;
            f.Step(.1f);
            Check(f.space.CurrentCameraPhase==CameraReturnPhase.Waiting,"runtime entered edge delay");
            Near(f.space.Position(walkingMonster.Rect).x-monsterStart,
                walkingMonster.moveSpeed*.1f+f.space.CurrentBackgroundSpeed*.1f,
                "monster movement equals its walk plus ONE Ground pan in waiting phase",.05f);
            for(int i=0;i<9;i++)f.Step(.11f);
            Check(f.space.CurrentCameraPhase==CameraReturnPhase.Returning,
                "runtime must only advance phase ONCE per rendered frame");
            for(int i=0;i<5;i++)f.Step(.25f);
            float target=f.ground.rect.xMin+f.space.Width*.62f;
            Near(f.space.Position(f.hero.heroRect).x,target,
                "runtime recovery places Hero at desired near-right center",.5f);
            Check(f.space.CurrentCameraPhase==CameraReturnPhase.Scrolling,
                "runtime correction finished");
            f.hero.SpawnHero();
            Check(f.space.CurrentCameraPhase==CameraReturnPhase.Scrolling,
                "respawn resets recovery without moving old actors");
        });
    }
}
