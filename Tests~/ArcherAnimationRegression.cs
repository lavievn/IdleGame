using System;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    sealed class ArcherFrames : IDisposable
    {
        public readonly Texture2D[,] Frames=new Texture2D[3,3];
        public ArcherFrames()
        {
            for(int p=0;p<3;p++)for(int i=0;i<3;i++) {
                var texture=Frames[p,i]=new Texture2D(900,600,TextureFormat.RGBA32,false);
                Resources.Register(Path(p,i),texture);
            }
        }
        static string Path(int p,int i) {return "CultivationSprites/Archer_"+new[]{"Normal","Attack","Hit"}[p]+"_"+(i+1);}
        public void Dispose() {for(int p=0;p<3;p++)for(int i=0;i<3;i++)Resources.Unregister(Path(p,i));}
    }
    static void ArcherFrame(SpriteSubject s,ArcherFrames frames,int pose,int frame)
    {
        Check(s.bridge.BodyGraphic.sprite.texture==frames.Frames[pose,frame],"ordered frame "+pose+"/"+frame);
        CheckRenderFeet(s.hero,"Archer animation frame",false);
        Near(s.bridge.BodyGraphic.rectTransform.rect.width,150,"stable canvas width");
        Near(s.bridge.BodyGraphic.rectTransform.rect.height,100,"stable canvas height");
    }
    static void ArcherAnimationTests()
    {
        using(var resources=new SpriteResources())using(var frames=new ArcherFrames())
        {
            Run("Archer Normal loops 1-2-3 without changing registered feet",()=>{
                var s=new SpriteSubject(false,AttackMode.RangedPhysical);
                foreach(float t in new[]{10f,10.15f,10.29f,10.43f,10.57f,10.71f}) {
                    s.At(t);ArcherFrame(s,frames,0,Mathf.FloorToInt((t-10f)/.14f)%3);
                }
            });
            Run("Archer Attack and Hit play 1-2-3 and return cleanly to Normal",()=>{
                var s=new SpriteSubject(false,AttackMode.RangedPhysical);
                s.Attack();ArcherFrame(s,frames,1,0);
                s.At(10.06f);ArcherFrame(s,frames,1,1);
                s.At(10.12f);ArcherFrame(s,frames,1,2);
                s.At(10.161f);ArcherFrame(s,frames,0,0);
                s.Hit();ArcherFrame(s,frames,2,0);
                s.At(10.24f);ArcherFrame(s,frames,2,1);
                s.At(10.31f);ArcherFrame(s,frames,2,2);
                s.At(10.362f);ArcherFrame(s,frames,0,0);
            });
            Run("Archer rapid attack callbacks cannot reset or indefinitely extend animation",()=>{
                var s=new SpriteSubject(false,AttackMode.RangedPhysical);s.Attack();
                float end=Get<float>(s.bridge,"returnAt");
                for(int i=1;i<=15;i++) {
                    s.At(10f+i*.01f);s.Attack();s.Hit();
                    Near(Get<float>(s.bridge,"returnAt"),end,"visual window never extends");
                    ArcherFrame(s,frames,1,Mathf.Min(2,Mathf.FloorToInt(i*.01f/(.16f/3f))));
                }
                s.At(10.17f);ArcherFrame(s,frames,0,0);s.Hit();s.Attack();ArcherFrame(s,frames,1,0);
            });
            Run("Archer pause/reset/mode change/death clear visual state and preserve other roles",()=>{
                var s=new SpriteSubject(false,AttackMode.RangedPhysical);s.Attack();s.At(10.07f);
                var image=s.bridge.BodyGraphic;Time.timeScale=0;Call(s.bridge,"Update");ArcherFrame(s,frames,1,1);
                Time.timeScale=1;s.hero.ChangeAttackMode(2);SpritePose(s.rect,resources,"Mage","Normal");
                s.hero.ChangeAttackMode(1);ArcherFrame(s,frames,0,0);s.Attack();s.hero.Die();
                Check(s.bridge.CurrentPose==CultivationSpriteBridge.Pose.Normal,"dead Attack canceled");
                s.hero.SpawnHero();ArcherFrame(s,frames,0,0);Check(s.bridge.BodyGraphic==image,"body reused");
                var monster=new SpriteSubject(true,AttackMode.RangedPhysical);monster.Attack();
                SpritePose(monster.rect,resources,"Enemy_Archer","Attack");
            });
            Run("partial Archer sequence falls back per-pose without loading incomplete animation",()=>{
                Resources.Unregister("CultivationSprites/Archer_Attack_2");
                try {
                    var s=new SpriteSubject(false,AttackMode.RangedPhysical);s.Attack();
                    SpritePose(s.rect,resources,"Archer","Attack");
                    s.At(10.161f);ArcherFrame(s,frames,0,0);
                }
                finally {Resources.Register("CultivationSprites/Archer_Attack_2",frames.Frames[1,1]);}
            });
        }
    }
}
