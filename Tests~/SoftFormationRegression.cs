using System;
using UnityEngine;

partial class MotionRegression
{
    static void SoftFormationTests()
    {
        foreach(int fps in new[]{30,60,144})foreach(int count in new[]{2,3})
        Run("soft formation continuous post-combat catch-up "+count+" Heroes / "+fps+" FPS",()=>{
            var f=new Field();Time.timeScale=1;f.hero.moveSpeed=450;
            f.space.SetPosition(f.hero.heroRect,new Vector2(-200,0));
            var bow=count==3?FormationHero(f,200,450,TuTienCore.AttackMode.RangedPhysical):null;
            var mage=FormationHero(f,600,200,TuTienCore.AttackMode.RangedMagic);
            var actors=count==3?new[]{f.hero,bow,mage}:new[]{f.hero,mage};
            var reverse=count==3?new[]{mage,bow,f.hero}:new[]{mage,f.hero};
            var enemy=f.Monster(-3000);FormationTick(f,1f/fps,actors);enemy.MarkDead();
            float previous=f.hero.MovementDistanceThisFrame;bool belowSlowest=false,finished=false;
            for(int i=0;i<fps*20;i++)
            {
                var before=new float[count];for(int j=0;j<count;j++)before[j]=f.space.Position(actors[j].heroRect).x;
                FormationTick(f,1f/fps,i%2==0?reverse:actors);
                for(int j=0;j<count;j++) {
                    float distance=before[j]-f.space.Position(actors[j].heroRect).x;
                    Check(distance>=-.002f && distance<=actors[j].EffectiveMoveSpeed()/fps+.002f,"forward movement within actual speed");
                    if(j>0)Check(f.space.Position(actors[j].heroRect).x-f.space.Position(actors[j-1].heroRect).x>=95,"distinct slots, no overtaking");
                }
                float step=f.hero.MovementDistanceThisFrame;
                Check(step>0,"front actor never uses hard waiting anchor");
                Check(Mathf.Abs(step-previous)<=Formation(f).regroupAcceleration/(fps*fps)+.002f,"smooth leader acceleration/deceleration");
                previous=step;if(step*fps<199)belowSlowest=true;
                Check(mage.MovementDistanceThisFrame<=200f/fps+.002f,"slow mage never boosted");
                if(Formation(f).CurrentPhase==HeroFormation.Phase.Marching) { finished=true;break; }
            }
            Check(finished && belowSlowest,"actually closes gap by moving below slowest speed, then converges");
            Near(Formation(f).MarchSpeed,200,"shared stable speed");
            for(int i=0;i<fps*2;i++) {
                FormationTick(f,1f/fps,reverse);Check(Formation(f).CurrentPhase==HeroFormation.Phase.Marching,"no hysteresis chatter");
                foreach(var hero in actors)Near(hero.MovementDistanceThisFrame,200f/fps,"all march at 200");
            }
        });
        Run("soft formation halted zero-speed party is explicit and resumes or fights without NaN",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var mage=FormationHero(f,500,0,TuTienCore.AttackMode.RangedMagic);
            for(int i=0;i<20;i++) {
                FormationTick(f,1f/60f,mage,f.hero);
                Check(Formation(f).CurrentPhase==HeroFormation.Phase.Halted,"deliberate halt rather than endless regroup");
                Near(f.hero.MovementDistanceThisFrame,0,"party does not abandon immobile member");
            }
            mage.moveSpeed=200;FormationTick(f,1f/60f,f.hero,mage);
            Check(f.hero.MovementDistanceThisFrame>0 && mage.MovementDistanceThisFrame>0,"buff expiry resumes march");
            var enemy=f.Monster(-1000);FormationTick(f,.1f,mage,f.hero);
            Check(Formation(f).CurrentPhase==HeroFormation.Phase.Combat && f.hero.CurrentTarget==enemy,"new enemy interrupts immediately");
            Near(f.hero.MovementDistanceThisFrame,45,"combat retains own 450 speed");
        });
        Run("soft formation recruitment and death never reposition existing lives",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            FormationTick(f,.1f,f.hero);float x=f.space.Position(f.hero.heroRect).x;
            var mage=FormationHero(f,x+500,200,TuTienCore.AttackMode.RangedMagic);
            Near(f.space.Position(f.hero.heroRect).x,x,"recruitment has no placement side effect");
            int life=f.hero.SpawnVersion;FormationTick(f,1f/60f,mage,f.hero);
            Check(x-f.space.Position(f.hero.heroRect).x<=450f/60f+.002f,"first group step is bounded");
            mage.Die();x=f.space.Position(f.hero.heroRect).x;FormationTick(f,.1f,f.hero);
            Near(x-f.space.Position(f.hero.heroRect).x,45,"remaining solo resumes own movement");
            Check(f.hero.SpawnVersion==life,"no spawn reset of existing Hero");
        });
    }
}
