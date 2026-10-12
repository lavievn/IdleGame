using UnityEngine;
using TuTienCore;

// Hotfix on top of .54g: camera group-focus transitions and untargeted Y.
partial class MotionRegression
{
    static void CameraAndLaneHotfixTests()
    {
        Run("54g lane: Hero without a monster walks LEFT but never drifts toward Y=0",()=>{
            foreach(int fps in new[]{30,60,144})
            {
                var f=new Field();
                f.space.useSoftZoneCamera=true;
                f.space.cameraUseCombatCenter=true;
                f.hero.moveSpeed=700f;
                f.space.SetPosition(f.hero.heroRect,new Vector2(0f,67f));
                float initialX=f.space.Position(f.hero.heroRect).x;
                for(int i=0;i<fps*2;i++)
                {
                    f.Step(1f/fps);
                    Near(f.space.Position(f.hero.heroRect).y,67f,
                        "no target: Y must remain exactly on its previous lane",.02f);
                    Check(f.hero.CurrentTarget==null,"no phantom target");
                }
                Check(f.space.Position(f.hero.heroRect).x<initialX,
                    "untargeted Hero still travels toward the left");
            }
        });

        Run("54g lane: melee Hero seeks monster Y only while approaching it",()=>{
            var f=new Field();
            f.hero.moveSpeed=150f;f.hero.laneSpeed=150f;
            f.space.SetPosition(f.hero.heroRect,new Vector2(0f,30f));
            var target=f.Monster(-200f,AttackMode.RangedMagic,85f);
            f.hero.TickMovement(f.space,.1f);
            Check(f.hero.CurrentTarget==target,"melee Hero picks closest monster");
            Check(f.space.Position(f.hero.heroRect).y>30f,
                "approaching melee Hero may move toward target's Y");
            float lane=f.space.Position(f.hero.heroRect).y;
            float x=f.space.Position(f.hero.heroRect).x;
            target.MarkDead();
            for(int i=0;i<15;i++)
            {
                f.hero.TickMovement(f.space,.1f);
                Near(f.space.Position(f.hero.heroRect).y,lane,
                    "after target disappears, Hero keeps the last Y",.02f);
            }
            Check(f.space.Position(f.hero.heroRect).x<x,
                "after kill, untargeted Hero continues straight left");
        });

        Run("54g lane: ranged Hero keeps its visual lane even before entering range",()=>{
            var f=new Field();f.hero.moveSpeed=150f;
            f.hero.ChangeAttackMode(1);
            f.space.SetPosition(f.hero.heroRect,new Vector2(0f,54f));
            var target=f.Monster(-470f,AttackMode.RangedMagic,88f);
            f.hero.TickMovement(f.space,.1f);
            Check(f.hero.CurrentTarget==target,"ranged Hero still acquires monster");
            Check(f.space.Position(f.hero.heroRect).x<0f,
                "ranged Hero can approach monster out of range");
            Near(f.space.Position(f.hero.heroRect).y,54f,
                "ranged Hero does not need Y alignment for X-only attack range",.02f);
            target.MarkDead();
            f.hero.TickMovement(f.space,.1f);
            Near(f.space.Position(f.hero.heroRect).y,54f,
                "ranged Hero also preserves Y after its target vanishes",.02f);
        });

        Run("54g lane: two independent untargeted Heroes retain separate Y",()=>{
            var f=new Field();f.space.useSoftZoneCamera=true;
            f.hero.moveSpeed=150f;
            f.space.SetPosition(f.hero.heroRect,new Vector2(0f,22f));
            var second=new GameObject().AddComponent<HeroController>();
            second.heroRect=(RectTransform)new GameObject(true).transform;
            second.heroRect.parent=f.ground;
            Call(second,"Awake");second.SpawnHero();second.moveSpeed=320f;
            f.space.SetPosition(second.heroRect,new Vector2(0f,78f));
            for(int i=0;i<60;i++)
            {
                f.Step(1f/60f);
                Near(f.space.Position(f.hero.heroRect).y,22f,
                    "first Hero keeps lane",.02f);
                Near(f.space.Position(second.heroRect).y,78f,
                    "second Hero keeps its different lane",.02f);
            }
        });
    }
}
