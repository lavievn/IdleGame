using System;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static HeroController FormationHero(Field f,float x,float speed,AttackMode mode)
    {
        var hero=new GameObject().AddComponent<HeroController>();
        hero.heroRect=(RectTransform)new GameObject(true).transform;
        hero.heroRect.SetParent(f.ground,false);Call(hero,"Awake");
        hero.attackMode=mode;hero.moveSpeed=speed;
        hero.SpawnHero(false,new Vector2(x,0));
        f.combat.RegisterAdditionalHero(hero,B2Data("formation"));
        return hero;
    }
    static void FormationTick(Field f,float dt,params HeroController[] order)
    {
        Time.frameCount++;Time.time+=dt;Time.deltaTime=dt;
        foreach(var hero in order)hero.TickMovement(f.space,dt);
    }
    static HeroFormation Formation(Field f) { return f.space.GetComponent<HeroFormation>(); }
    static void FormationTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(System.IO.File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(System.IO.File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Run("formation single Hero 450 retains unbounded solo exploration",()=>{
            var f=new Field();f.hero.moveSpeed=450;Time.timeScale=1;
            float x=f.space.Position(f.hero.heroRect).x;
            FormationTick(f,.1f,f.hero);
            Near(f.space.Position(f.hero.heroRect).x,x-45,"solo own speed");
            Check(Formation(f).CurrentPhase==HeroFormation.Phase.Single,"no group restriction");
        });
        foreach(int width in new[]{600,800,1150})foreach(int count in new[]{2,3})
        Run("formation "+count+" Heroes at "+width+" march at slowest 200 with preserved spacing",()=>{
            int oldWidth=Screen.width;Screen.width=width;
            try {
                var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
                var bow=count==3?FormationHero(f,100,450,AttackMode.RangedPhysical):null;
                var mage=FormationHero(f,(count-1)*100,200,AttackMode.RangedMagic);
                var order=count==3?new[]{mage,f.hero,bow}:new[]{mage,f.hero};
                for(int step=0;step<120;step++)
                {
                    float a=f.space.Position(f.hero.heroRect).x,b=f.space.Position(mage.heroRect).x;
                    FormationTick(f,1f/60f,order);
                    Near(a-f.space.Position(f.hero.heroRect).x,200f/60f,"fast actor slows to 200");
                    Near(b-f.space.Position(mage.heroRect).x,200f/60f,"slow actor never speeds up");
                    Near(f.space.Position(mage.heroRect).x-f.space.Position(f.hero.heroRect).x,(count-1)*100,"spacing survives arbitrary tick order");
                    if(bow!=null)Near(f.space.Position(bow.heroRect).x-f.space.Position(f.hero.heroRect).x,100,"third actor keeps distinct slot");
                }
                Near(Formation(f).MarchSpeed,200,"actual slowest speed");
                Check(Formation(f).CurrentPhase==HeroFormation.Phase.Marching,"marching phase");
            } finally { Screen.width=oldWidth; }
        });
        Run("formation combat releases all actors to own speeds and original nearest targeting",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var bow=FormationHero(f,100,450,AttackMode.RangedPhysical);
            var mage=FormationHero(f,200,200,AttackMode.RangedMagic);
            var far=f.Monster(-900);var near=f.Monster(-700);
            FormationTick(f,.1f,mage,bow,f.hero);
            Near(f.space.Position(f.hero.heroRect).x,-45,"melee uses 450");
            Near(f.space.Position(bow.heroRect).x,55,"bow uses 450");
            Near(f.space.Position(mage.heroRect).x,180,"mage uses 200");
            Check(f.hero.CurrentTarget==near && bow.CurrentTarget==near && mage.CurrentTarget==near,"original nearest eligible target");
            Check(Formation(f).CurrentPhase==HeroFormation.Phase.Combat && far.IsAlive,"combat phase");
        });
        Run("formation never pulls an in-range attacking Hero back into a slot",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var mage=FormationHero(f,350,200,AttackMode.RangedMagic);var monster=f.Monster(-10);
            FormationTick(f,.1f,f.hero,mage);
            Near(f.space.Position(f.hero.heroRect).x,0,"melee stays in range");
            Check(f.hero.CurrentState==HeroState.Combat && f.hero.CurrentTarget==monster,"attacking state retained");
        });
        Run("formation wave end slows moving leader and regroups without snap, overspeed or reverse oscillation",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,new Vector2(-200,0));
            var bow=FormationHero(f,200,450,AttackMode.RangedPhysical);
            var mage=FormationHero(f,600,200,AttackMode.RangedMagic);
            var monster=f.Monster(-2000);FormationTick(f,0,f.hero,bow,mage);monster.MarkDead();
            bool regrouped=false;int gathering=0;
            for(int i=0;i<600;i++)
            {
                var actors=new[]{f.hero,bow,mage};var before=new float[3];
                for(int j=0;j<3;j++)before[j]=f.space.Position(actors[j].heroRect).x;
                FormationTick(f,1f/60f,mage,f.hero,bow);
                for(int j=0;j<3;j++) {
                    float distance=before[j]-f.space.Position(actors[j].heroRect).x;
                    Check(distance>=-.002f && distance<=actors[j].EffectiveMoveSpeed()/60f+.002f,"forward only and bounded by own speed");
                    if(j>0)Check(f.space.Position(actors[j].heroRect).x-f.space.Position(actors[j-1].heroRect).x>=95,"no overlap or overtaking");
                }
                if(Formation(f).CurrentPhase==HeroFormation.Phase.Marching) { regrouped=true;break; }
                Check(f.hero.MovementDistanceThisFrame>0,"leader keeps moving while team gathers");gathering++;
            }
            Check(regrouped && gathering>60,"regroup converges after actual catch-up");
            Near(f.space.Position(bow.heroRect).x-f.space.Position(f.hero.heroRect).x,100,"bow slot",5f);
            Near(f.space.Position(mage.heroRect).x-f.space.Position(bow.heroRect).x,100,"mage slot",5f);
            for(int i=0;i<120;i++) {
                FormationTick(f,1f/60f,mage,bow,f.hero);
                Check(Formation(f).CurrentPhase==HeroFormation.Phase.Marching,"no phase chatter");
                Near(f.hero.MovementDistanceThisFrame,200f/60f,"resumed slowest march");
            }
        });
        Run("formation overlapping setup spreads at own speeds and fast rear never overtakes slow front",()=>{
            var f=new Field();f.hero.moveSpeed=100;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var rear=FormationHero(f,0,450,AttackMode.RangedPhysical);
            for(int i=0;i<180;i++) {
                FormationTick(f,1f/60f,rear,f.hero);
                Check(f.space.Position(rear.heroRect).x>=f.space.Position(f.hero.heroRect).x,"stable order from overlap");
                Check(rear.MovementDistanceThisFrame<=7.502f && f.hero.MovementDistanceThisFrame<=1.669f,"no acceleration to resolve overlap");
            }
            Check(Formation(f).CurrentPhase==HeroFormation.Phase.Marching,"initial overlap resolves");
            Near(f.space.Position(rear.heroRect).x-f.space.Position(f.hero.heroRect).x,100,"separated bodies",5f);
        });
        Run("formation new wave interrupts regroup immediately and does not wait for convergence",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var mage=FormationHero(f,900,200,AttackMode.RangedMagic);
            FormationTick(f,.1f,f.hero,mage);Check(Formation(f).CurrentPhase==HeroFormation.Phase.Regrouping,"regroup active");
            float x=f.space.Position(f.hero.heroRect).x;var monster=f.Monster(-900);
            FormationTick(f,.1f,mage,f.hero);
            Check(Formation(f).CurrentPhase==HeroFormation.Phase.Combat && f.hero.CurrentTarget==monster,"wave takes priority");
            Near(x-f.space.Position(f.hero.heroRect).x,45,"combat uses own speed immediately");
        });
        Run("formation dead or hidden slow actor is excluded and solo resumes own speed",()=>{
            foreach(bool dead in new[]{true,false}) {
                var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
                var mage=FormationHero(f,100,200,AttackMode.RangedMagic);FormationTick(f,.1f,f.hero,mage);
                if(dead)mage.Die();else mage.HideHero();
                float x=f.space.Position(f.hero.heroRect).x;FormationTick(f,.1f,f.hero);
                Near(x-f.space.Position(f.hero.heroRect).x,45,"remaining actor not capped at 200");
                Check(Formation(f).CurrentPhase==HeroFormation.Phase.Single,"solo after removal");
            }
        });
        Run("formation uses registered B2 combatants rather than unrelated camera-only actors",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var mage=FormationHero(f,100,200,AttackMode.RangedMagic);
            var visual=new GameObject().AddComponent<HeroController>();
            visual.heroRect=(RectTransform)new GameObject(true).transform;visual.heroRect.SetParent(f.ground,false);
            Call(visual,"Awake");visual.moveSpeed=0;visual.SpawnHero(false);
            Check(f.combat.StateFor(visual)==null && visual.IsCameraSubject,"fixture has camera-only visual");
            FormationTick(f,.1f,visual,mage,f.hero);
            Near(Formation(f).MarchSpeed,200,"unregistered visual does not cap the real party");
            Near(f.hero.MovementDistanceThisFrame,20,"registered party marches normally");
            Near(visual.MovementDistanceThisFrame,0,"independent visual remains stationary");
        });
        Run("formation recruitment never snaps existing actors and follows configured spacing",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            FormationTick(f,.1f,f.hero);Formation(f).spacing=120;Formation(f).tolerance=2;
            float x=f.space.Position(f.hero.heroRect).x;
            var mage=FormationHero(f,x+500,200,AttackMode.RangedMagic);
            Near(f.space.Position(f.hero.heroRect).x,x,"recruitment does not place existing actors");
            for(int i=0;i<1200 && Formation(f).CurrentPhase!=HeroFormation.Phase.Marching;i++)FormationTick(f,1f/60f,f.hero,mage);
            Near(f.space.Position(mage.heroRect).x-f.space.Position(f.hero.heroRect).x,120,"configured spacing",2f);
            Near(Formation(f).MarchSpeed,200,"new member changes march speed");
        });
        Run("formation W2 defaults and stance changes preserve passive/equipment speed modifiers and data",()=>{
            var f=new Field();f.hero.useStanceMoveSpeeds=true;f.hero.moveSpeed=300;
            var data=f.combat.CurrentHeroCombatState.Data;data.combatStatModifiers.Clear();data.isDirty=false;
            foreach(var mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic}) {
                f.hero.ChangeAttackMode((int)mode);
                Near(f.hero.EffectiveMoveSpeed(),mode==AttackMode.RangedMagic?200:450,"stance default");
            }
            data.combatStatModifiers.Add(new CombatStatModifier { sourceId="equipment:probe",stat=CombatStatId.MovementSpeed,flat=30,percent=.1f });
            data.combatStatModifiers.Add(new CombatStatModifier { sourceId="passive:probe",stat=CombatStatId.MovementSpeed,flat=5 });
            Near(f.hero.EffectiveMoveSpeed(),255,"gear and passive preserved on mage");
            f.hero.ChangeAttackMode(0);Near(f.hero.EffectiveMoveSpeed(),530,"mode updates base before modifiers");
            Check(!data.isDirty && data.combatStatModifiers.Count==2 && f.hero.moveSpeed==300,"no mutation to save data or serialized legacy base");
            CombatBalance.SetDevProfile(false,DevBalanceProfile.Defaults(false,f.space,300));
            Near(f.hero.EffectiveMoveSpeed(),365,"existing explicit DEVB base remains effective");
            CombatBalance.ResetDevProfiles();
        });
        Run("formation speed changes recompute slowest without moving another actor immediately",()=>{
            var f=new Field();f.hero.useStanceMoveSpeeds=true;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var other=FormationHero(f,100,450,AttackMode.RangedPhysical);
            FormationTick(f,.1f,f.hero,other);Near(Formation(f).MarchSpeed,450,"initial march");
            float x=f.space.Position(other.heroRect).x;f.hero.ChangeAttackMode(2);
            Near(f.space.Position(other.heroRect).x,x,"mode switch never snaps team");
            FormationTick(f,.1f,other,f.hero);Near(Formation(f).MarchSpeed,200,"new stance base is slowest");
            Near(other.MovementDistanceThisFrame,20,"peer slows next movement frame");
        });
        Run("formation zero-speed member is never accelerated and incoming combat still releases team",()=>{
            var f=new Field();f.hero.moveSpeed=450;f.space.SetPosition(f.hero.heroRect,Vector2.zero);
            var other=FormationHero(f,100,0,AttackMode.RangedMagic);
            FormationTick(f,.1f,other,f.hero);Near(f.hero.MovementDistanceThisFrame,0,"zero slowest halts march");
            var monster=f.Monster(-900);FormationTick(f,.1f,f.hero,other);
            Near(f.hero.MovementDistanceThisFrame,45,"combat is not blocked");
            Near(other.MovementDistanceThisFrame,0,"immobile actor stays immobile");
            Check(f.hero.CurrentTarget==monster,"normal target resumes");
        });
        Run("formation camera pan preserves group gaps and does not alter logical travel budget",()=>{
            var f=new Field();Time.timeScale=1;f.hero.moveSpeed=450;
            f.space.SetPosition(f.hero.heroRect,Vector2.zero);var mage=FormationHero(f,100,200,AttackMode.RangedMagic);
            float pan=0;
            for(int i=0;i<120;i++) {
                float before=f.space.Position(f.hero.heroRect).x;f.Step(.05f);
                Near(f.hero.MovementDistanceThisFrame,10,"movement budget before camera");
                Near(f.space.Position(mage.heroRect).x-f.space.Position(f.hero.heroRect).x,100,"shared pan preserves gaps");
                Near(f.space.Position(f.hero.heroRect).x,before-10+f.space.CurrentCameraSpeed*.05f,"camera applies its original shared pan");
                pan+=Mathf.Abs(f.space.CurrentCameraSpeed);
            }
            Check(pan>0,"exercise real pan path");
        });
        Run("formation real B2 creation uses independent mode bases, rear spacing and original camera subject",()=>{
            using(var f=new DesktopHotfixFixture(800,true)) {
                Set(f.manager,"environmentManager",f.field.space); // Normally resolved by GameManager.Start.
                EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,1.3f);
                var target=Get<HeroController>(f.field.space,"cameraTarget");
                Check(f.manager.IsGameplayHUDVisible && !f.manager.HasPendingConfirmation && Time.timeScale!=0,"B2 entry is playable");
                Check(Get<CombatManager>(f.manager,"combatManager")==f.field.combat && f.field.combat.HasLivingHero,"B2 has live registered combat");
                Check(Get<EnvironmentManager>(f.manager,"environmentManager")==f.field.space,"B2 environment fixture reference");
                f.manager.CycleTestCompanions();f.manager.CycleTestCompanions();
                Check(f.manager.TestCompanionCount==2 && HeroController.ActiveHeroes.Count==3,"real 3-Hero B2 path: "+f.manager.TestCompanionCount+" companions, "+HeroController.ActiveHeroes.Count+" active; "+f.manager.FullEventHistory);
                foreach(var hero in HeroController.ActiveHeroes) {
                    Near(hero.EffectiveMoveSpeed(),hero.attackMode==AttackMode.RangedMagic?200:450,"B2 role base");
                    if(hero!=f.field.hero)Check(hero.heroRect!=f.field.hero.heroRect,"independent actor");
                }
                Check(Get<HeroController>(f.field.space,"cameraTarget")==target,"B2 does not steal camera");
                var members=HeroController.ActiveHeroes;
                for(int i=1;i<members.Count;i++)Check(f.field.space.Position(members[i].heroRect).x-f.field.space.Position(members[i-1].heroRect).x>=100,"spawn starts separated, never snapped into front slot");
            }
        });
        Run("formation regroup follows existing soft camera pan while leader keeps walking",()=>{
            var f=new Field();Time.timeScale=1;f.space.useSoftZoneCamera=true;f.hero.moveSpeed=450;
            f.space.SetPosition(f.hero.heroRect,new Vector2(-200,0));
            var mage=FormationHero(f,500,200,AttackMode.RangedMagic);bool arrived=false;float pan=0;
            for(int i=0;i<600;i++) {
                f.Step(1f/60f);pan+=Mathf.Abs(f.space.CurrentCameraSpeed);
                if(Formation(f).CurrentPhase==HeroFormation.Phase.Marching) { arrived=true;break; }
                Check(f.hero.MovementDistanceThisFrame>0 && f.hero.MovementDistanceThisFrame<=450f/60f+.002f,"leader walks within own speed while camera pans");
                Check(mage.MovementDistanceThisFrame<=200f/60f+.002f,"rear actor respects own speed during camera motion");
            }
            Check(arrived && pan>0,"regroup converges while soft camera actually pans");
            Near(f.space.Position(mage.heroRect).x-f.space.Position(f.hero.heroRect).x,100,"camera-independent anchor",5);
        });
        Run("formation wave scheduler spawns while team still regrouping and pooled monster re-enters combat",()=>{
            using(var f=new DesktopHotfixFixture(800,true)) {
                Set(f.manager,"environmentManager",f.field.space);
                EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,1.3f);
                var mage=FormationHero(f.field,f.field.space.Position(f.field.hero.heroRect).x+2000,200,AttackMode.RangedMagic);
                var monsters=new System.Collections.Generic.List<GameObject>(f.manager.activeMonsters);
                foreach(var go in monsters) { go.GetComponent<MonsterController>().MarkDead();f.manager.OnMonsterDied(go); }
                FormationTick(f.field,.1f,f.field.hero,mage);
                Check(Formation(f.field).CurrentPhase==HeroFormation.Phase.Regrouping,"long catch-up in progress");
                var pending=Get<Coroutine>(f.manager,"nextWaveCoroutine");Check(pending!=null,"normal wave scheduled");
                for(int i=0;i<100 && f.manager.activeMonsters.Count==0;i++) { Time.deltaTime=.1f;pending.routine.MoveNext(); }
                Check(f.manager.activeMonsters.Count>0 && Formation(f.field).CurrentPhase==HeroFormation.Phase.Regrouping,"scheduler does not wait for formation");
                FormationTick(f.field,.1f,f.field.hero,mage);
                Check(Formation(f.field).CurrentPhase==HeroFormation.Phase.Combat,"new wave immediately breaks regroup");
                var pooled=f.manager.activeMonsters[0].GetComponent<MonsterController>();int life=pooled.SpawnVersion;
                foreach(var go in f.manager.activeMonsters)go.GetComponent<MonsterController>().MarkDead();
                FormationTick(f.field,.1f,f.field.hero,mage);
                Check(Formation(f.field).CurrentPhase==HeroFormation.Phase.Regrouping,"dead pooled actors do not lock combat phase");
                pooled.gameObject.SetActive(false);pooled.gameObject.SetActive(true);
                FormationTick(f.field,.1f,f.field.hero,mage);
                Check(pooled.SpawnVersion>life && Formation(f.field).CurrentPhase==HeroFormation.Phase.Combat,"reactivated pooled life joins combat normally");
            }
        });
    }
}
