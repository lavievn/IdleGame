using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void Version0050RegressionTests()
    {
        Run("0.0.50 offscreen ranged enemies cannot begin shooting",()=>{
            var f=new Field();
            f.space.SetPosition(f.hero.heroRect,new Vector2(-220f,0f));
            f.hero.moveSpeed=0f;
            var m=f.Monster(-520f,AttackMode.RangedMagic);
            f.Battle(m);
            Check(!f.space.IsVisible(m.Rect),"spawn starts to left of camera");
            Check(!m.CanAttack(f.hero,f.space),
                "within range is NOT enough to attack before entering screen");
            m.TickMovement(f.space,.1f);
            Check(f.space.Position(m.Rect).x>-520f,
                "offscreen ranged attacker walks toward screen entry");
            Time.deltaTime=3f; Call(f.combat,"Update");
            Check(f.combat.PendingProjectileCount==0,
                "offscreen monster cannot create shot or charge projectile");
            for(int i=0;i<12 && !f.space.IsVisible(m.Rect);i++)
                m.TickMovement(f.space,.1f);
            Check(f.space.IsVisible(m.Rect),"ranged monster can actually enter view");
            Check(m.CanAttack(f.hero,f.space),"normal ranged combat resumes after entry");
        });

        Run("0.0.50 stationary combat camera moves toward trailing Hero right boundary",()=>{
            var f=new Field();
            f.space.useSoftZoneCamera=true;
            f.space.advanceStationaryCombatToRight=true;
            f.hero.ChangeAttackMode(1);
            f.hero.moveSpeed=0f;
            var m=f.Monster(-180f,AttackMode.RangedPhysical);
            float start=f.space.Position(f.hero.heroRect).x;
            for(int i=0;i<180;i++) f.Step(1f/60f);
            float after=f.space.Position(f.hero.heroRect).x;
            Check(f.hero.CurrentState==HeroState.Combat,"Hero remains attacking");
            Check(after>start+100f,"standing fighters no longer leave right side empty");
            Check(after<=f.space.ZoneRightX+.1f,"idle pan stops at right red-zone edge");
            Near(after,f.space.ZoneRightX,"smooth idle target reached",.5f);
            Near(f.space.Position(f.hero.heroRect).x-f.space.Position(m.Rect).x,
                180f,"shared pan does not change melee/ranged gap",.1f);
        });

        Run("0.0.50 last Hero death instantly clears camera and visual",()=>{
            var f=new Field();f.space.useSoftZoneCamera=true;
            f.hero.Die();
            Check(!f.hero.heroRect.gameObject.activeSelf && f.hero.IsDead,
                "death must hide the body immediately");
            Check(!f.hero.IsCameraSubject,
                "dead hero is never eligible for camera");
            f.Step(.1f);
            Check(f.space.CurrentCameraFocusCount==0&&!f.space.IsScrolling,
                "camera drops to zero subjects and stops");
        });

        Run("0.0.50 when primary dies companion stays visible and gets camera",()=>{
            var f=new Field(); f.space.useSoftZoneCamera=true;
            var second=B2Hero(f,-100f,AttackMode.RangedPhysical);
            var state=f.combat.RegisterAdditionalHero(second,B2Data("Survivor 0.0.50"));
            f.space.FollowHero(f.hero);
            Check(f.combat.ApplyDamageToRegisteredHero(f.hero,1000000,"test"),
                "main receives lethal damage");
            Check(!f.hero.heroRect.gameObject.activeSelf &&
                !HeroController.VisibleHeroes.Contains(f.hero),
                "primary corpse is not displayed or depth sorted");
            Check(state.CanFight && second.heroRect.gameObject.activeSelf,
                "survivor stays deployed");
            f.Step(.05f);
            Check(f.space.CurrentCameraFocusCount==1,
                "only living companion is camera subject");
        });

        Run("0.0.50 stationary pan spring never overshoots and respects zero dt",()=>{
            float velocity=0f;
            Near(BattleMotion.StationaryCombatPan(200f,ref velocity,.6f,0f),
                0f,"pause does not advance camera");
            float position=0f;
            for(int i=0;i<180;i++)
            {
                float remaining=200f-position;
                float pan=BattleMotion.StationaryCombatPan(remaining,ref velocity,.6f,1f/60f);
                Check(pan>=0f && pan<=remaining,"no overshoot/negative pan");
                position+=pan;
            }
            Near(position,200f,"damped pan reaches boundary",.5f);
            Near(BattleMotion.StationaryCombatPan(0f,ref velocity,.6f,.1f),
                0f,"no drifting after reaching boundary");
        });
    }
}
