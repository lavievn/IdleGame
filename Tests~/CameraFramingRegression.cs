using System;
using UnityEngine;
partial class MotionRegression
{
    static float FramedPan(MultiZoneCamera c,float x,float dt,float ratio,float velocity)
    {return c.Pan(x,-960,960,dt,.2f,.45f,.75f,30,450,900,1200,900,48,ratio,velocity,true);}
    static void CameraFramingTests()
    {
        foreach(int fps in new[]{30,60,144})foreach(int direction in new[]{-1,1})
        Run("camera framing moving normalized target "+fps+"/"+direction,()=>{
            float x=576,dt=1f/fps,target=direction<0?.65f:.35f;var c=new MultiZoneCamera();
            for(int i=0;i<fps*12;i++) {x+=direction*450*dt;float pan=FramedPan(c,x,dt,target,-direction*450);Check(Mathf.Abs(pan)<=48.001f,"bounded camera step");x+=pan;}
            float viewport=(x+960)/1920;Near(viewport,target,"actual post-pan viewport position",.015f);
            Console.WriteLine("CAMERA fps="+fps+" direction="+direction+" viewport="+viewport);
            for(int i=0;i<fps*6;i++)x+=FramedPan(c,x,dt,target,0);
            Console.WriteLine("CAMERA stop6 fps="+fps+" direction="+direction+" viewport="+((x+960)/1920));
            Near((x+960)/1920,target,"stationary framing retains side",.002f);
        });
        Run("camera framing reversal brakes and opposite motion does not invert lookahead",()=>{
            var c=new MultiZoneCamera();float x=576,dt=1f/60f;
            for(int i=0;i<600;i++){x-=450*dt;x+=FramedPan(c,x,dt,.65f,450);}
            float ratio=.65f;var look=new CombatFocusSmoother();
            for(int i=0;i<600;i++) {
                ratio=.65f+look.Step(-.3f,dt,.35f,1);x+=450*dt;
                float old=c.Velocity;x+=FramedPan(c,x,dt,ratio,-450);
                Check(Mathf.Abs(c.Velocity-old)<=1200*dt+.01f,"bounded reversal acceleration");
            }
            Near((x+960)/1920,.35f,"rightward target",.015f);
            float v=c.Velocity;Near(FramedPan(c,x,0,.35f,-450),0,"pause pan");Near(c.Velocity,v,"pause velocity");
        });
        Run("camera framing explains old trailing error and keeps relative world distances",()=>{
            var old=new MultiZoneCamera();float x=576;
            for(int i=0;i<1200;i++){x-=450f/60;x+=MultiPan(old,x,1f/60);}
            float baseline=(x+960)/1920;Console.WriteLine("CAMERA old viewport="+baseline);Check(baseline<.3f,"reproduces left-side trailing, not a speed-only problem");
            var c=new MultiZoneCamera();float hero=300,monster=-500;float relative=hero-monster;
            float pan=FramedPan(c,hero,1f/60,.65f,450);hero+=pan;monster+=pan;Near(hero-monster,relative,"shared translation preserves targeting/sight distance");
            float stalled=FramedPan(c,hero,2,.65f,450);Check(Mathf.Abs(stalled)<=48.001f,"stall cannot teleport camera");
        });
    }
}
