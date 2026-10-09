using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void GroundRelativeMotionTests()
    {
        Run("approaching monster moves at base speed plus current ground pan, without double camera",()=>{
            foreach(int fps in new[]{30,60,144})foreach(float scale in new[]{.2f,1f,2f})foreach(float speed in new[]{75f,150f,300f,600f}) {
                var f=new Field(scale);f.hero.moveSpeed=speed;f.space.scrollSpeed=150;
                var m=f.Monster(-450);m.moveSpeed=150;
                float start=f.space.Position(m.Rect).x;f.Step(1f/fps);
                Near((f.space.Position(m.Rect).x-start)*fps,150+f.space.CurrentBackgroundSpeed,"base plus ground",.03f);
                Near(((f.space.Position(m.Rect).x-start)*fps)-f.space.CurrentBackgroundSpeed,150,"walk relative to ground",.03f);
            }
        });
        Run("ground compensation uses current frame after hero speed changes and handles slower ground than camera",()=>{
            var f=new Field();var m=f.Monster(-450);m.moveSpeed=50;f.space.scrollSpeed=1000;
            f.hero.moveSpeed=75;float x=f.space.Position(m.Rect).x;f.Step(.01f);
            Near((f.space.Position(m.Rect).x-x)/.01f,125,"carrier correction can be negative before camera pan",.02f);
            f.hero.moveSpeed=600;x=f.space.Position(m.Rect).x;f.Step(.01f);
            Near((f.space.Position(m.Rect).x-x)/.01f,650,"no one-frame speed lag",.03f);
        });
        Run("compensated monster clamps at attack range on long frames and stationary fighters share camera",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);f.hero.moveSpeed=600;var m=f.Monster(-450);m.moveSpeed=600;
            f.Step(2);Check(f.space.Position(m.Rect).x<=f.space.Position(f.hero.heroRect).x,"no crossing after compensation");
            Check(m.CanAttack(f.hero,f.space),"stops in attack range");
            float old=f.space.Position(m.Rect).x;f.Step(.1f);
            Near(f.space.Position(m.Rect).x-old,f.space.CurrentCameraSpeed*.1f,"attacking monster does not receive extra approach offset",.01f);
        });
    }
}
