using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static void UseHitchBudget(EnvironmentManager space)
    {
        space.maxHeroMovementDeltaTime=BattleMotion.DefaultMaxMovementDeltaTime;
    }
    static void StanceSpeeds(HeroController hero,int mode)
    {
        hero.useStanceMoveSpeeds=true;
        hero.meleeMoveSpeed=hero.physicalMoveSpeed=450f;hero.magicMoveSpeed=200f;
        hero.ChangeAttackMode(mode);
    }
    static void HeroMovementSpikeTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        int oldFrame=Time.frameCount,oldWidth=Screen.width,oldHeight=Screen.height;
        float oldTime=Time.time,oldDelta=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime;
        using(var sprites=new SpriteResources())
        try
        {
            Run("movement budget defaults to 1/30 and rejects invalid time/limits without a backlog",()=>{
                var space=new GameObject().AddComponent<EnvironmentManager>();
                Near(space.maxHeroMovementDeltaTime,1f/30f,"runtime default, not the legacy fixture override");
                foreach(float dt in new[]{0f,-1f,float.NaN,float.NegativeInfinity,float.PositiveInfinity})
                    Near(BattleMotion.MovementDeltaTime(dt,1f/30f),0,"invalid input cannot move");
                foreach(float limit in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
                    Near(BattleMotion.MovementDeltaTime(.28f,limit),1f/30f,"invalid limit uses safe default");
                Near(BattleMotion.MovementDeltaTime(.016f,1f/30f),.016f,"no deferred debt");
            });
            foreach(int mode in new[]{0,1,2})foreach(float input in new[]{.016f,.033f,.1f,.28f})
            Run("Hero render-frame walking budget "+(AttackMode)mode+" dt="+input,()=>{
                var f=new Field(limitRenderMovement:true);StanceSpeeds(f.hero,mode);
                f.hero.heroRect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachHero(f.hero);
                float speed=mode==2?200f:450f;
                float before=f.space.Position(f.hero.heroRect).x;
                Time.deltaTime=input;f.hero.TickMovement(f.space,input);
                float expected=speed*Mathf.Min(input,1f/30f);
                Near(before-f.space.Position(f.hero.heroRect).x,expected,"one rendered displacement");
                Near(f.hero.MovementBudgetThisFrame,expected,"distance-based consumers see actual budget");
                Near(f.hero.MovementInputDeltaThisFrame,input,"raw input retained for diagnostics");
                Near(f.hero.MovementDeltaThisFrame,Mathf.Min(input,1f/30f),"effective walking dt");
                Near(Time.deltaTime,input,"global clock is never clamped");
                Near(f.hero.EffectiveMoveSpeed(),speed,"nominal stance speed unchanged");
                CheckRenderFeet(f.hero,"bounded exploration",false);
                before=f.space.Position(f.hero.heroRect).x;
                f.hero.TickMovement(f.space,.016f);
                Near(before-f.space.Position(f.hero.heroRect).x,speed*.016f,"no catch-up speed next frame");
            });
            foreach(int mode in new[]{0,1,2})foreach(int fps in new[]{30,60,120})
            Run("nominal stance distance remains exact at "+fps+" FPS / "+(AttackMode)mode,()=>{
                var f=new Field(limitRenderMovement:true);StanceSpeeds(f.hero,mode);
                // Keep this numerical speed check near the coordinate origin;
                // avoid repeated cancellation of a -500 right-anchor offset.
                f.hero.heroRect.anchorMin=f.hero.heroRect.anchorMax=new Vector2(.5f,0);
                f.space.SetPosition(f.hero.heroRect,Vector2.zero);
                float before=f.space.Position(f.hero.heroRect).x;
                for(int i=0;i<fps;i++)FormationTick(f,1f/fps,f.hero);
                Near(before-f.space.Position(f.hero.heroRect).x,mode==2?200f:450f,"one second >=30 FPS retains nominal distance");
            });
            foreach(int count in new[]{1,2,3})
            Run("approaching and B2 use bounded movement, without limiting Monster dt / party="+count,()=>{
                var f=new Field(limitRenderMovement:true);StanceSpeeds(f.hero,0);
                f.space.SetPosition(f.hero.heroRect,Vector2.zero);
                var bow=count>=2?FormationHero(f,150,450,AttackMode.RangedPhysical):null;
                var mage=count>=3?FormationHero(f,300,200,AttackMode.RangedMagic):null;
                var enemy=f.Monster(-3000);var actors=count==3?new[]{f.hero,bow,mage}:count==2?new[]{f.hero,bow}:new[]{f.hero};
                foreach(var hero in actors)hero.heroRect.gameObject.AddComponent<Image>();
                FormationTick(f,.28f,actors);
                foreach(var hero in actors) {
                    Check(hero.CurrentState==HeroState.Approaching && hero.CurrentTarget==enemy,"combat approach, not ResetPosition");
                    Near(hero.MovementDistanceThisFrame,hero.EffectiveMoveSpeed()/30f,"own bounded walking");
                    Near(hero.MovementDeltaThisFrame,1f/30f,"shared policy for all Heroes");
                }
                float x=f.space.Position(enemy.Rect).x;enemy.TickMovement(f.space,.28f);
                Near(f.space.Position(enemy.Rect).x-x,enemy.moveSpeed*.28f,"Monster walking clock unchanged");
                FormationTick(f,.016f,actors);
                foreach(var hero in actors)Near(hero.MovementDistanceThisFrame,hero.EffectiveMoveSpeed()*.016f,"no per-actor debt");
                enemy.MarkDead();FormationTick(f,.28f,actors);
                foreach(var hero in actors)Check(hero.MovementDistanceThisFrame<=hero.EffectiveMoveSpeed()/30f+.002f,"regroup/march also bounded");
            });
            Run("Hero lane movement is capped too; pause/resume and reused life never replay stall time",()=>{
                var f=new Field(limitRenderMovement:true);StanceSpeeds(f.hero,0);
                f.space.SetPosition(f.hero.heroRect,new Vector2(576,0));f.Monster(-1000,AttackMode.RangedPhysical,90);
                f.hero.TickMovement(f.space,.28f);
                Near(f.space.Position(f.hero.heroRect).x,561,"X cap");
                Near(f.space.Position(f.hero.heroRect).y,5,"lane cap, no 42-unit Y jump");
                Time.timeScale=0;var p=f.space.Position(f.hero.heroRect);
                f.hero.TickMovement(f.space,.28f);SamePoint(f.space.Position(f.hero.heroRect),p,"pause");
                Time.timeScale=1;f.hero.TickMovement(f.space,.28f);
                Near(f.hero.MovementDistanceThisFrame,15,"resume long frame capped");
                f.hero.HideHero();f.hero.SpawnHero(false,new Vector2(576,0));
                Near(f.hero.MovementDeltaThisFrame,0,"new life clears diagnostic dt");
                f.hero.TickMovement(f.space,.016f);Near(f.hero.MovementDistanceThisFrame,7.2f,"respawn has no deferred movement");
            });
            Run("cooldown, regeneration, sprite pose time and global dt retain raw 0.28 seconds",()=>{
                var f=new Field(limitRenderMovement:true);StanceSpeeds(f.hero,2);
                f.hero.heroRect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachHero(f.hero);
                var enemy=f.Monster(-100);f.Battle(enemy);f.hero.TickMovement(f.space,.28f);
                Set(f.combat,"heroAttackTimer",0f);
                var state=f.combat.CurrentHeroCombatState;
                state.Data.combatStatModifiers.Add(new CombatStatModifier { stat=CombatStatId.HealthRegeneration,flat=100f });
                state.CurrentHP=state.MaxHP-100;
                Time.deltaTime=.28f;Call(f.combat,"Update");
                Near(Get<float>(f.combat,"heroAttackTimer"),.28f,"cooldown not limited to 1/30");
                Check(state.CurrentHP==state.MaxHP-72,"regeneration uses full 0.28");
                Near(Time.deltaTime,.28f,"global dt retained");
                CultivationSpriteBridge.Attack(f.hero.heroRect);Time.time+=.28f;
                var bridge=f.hero.heroRect.GetComponent<CultivationSpriteBridge>();Call(bridge,"Update");
                Check(bridge.CurrentPose==CultivationSpriteBridge.Pose.Normal,"presentation timer not slowed by walking cap");
            });
            foreach(int width in new[]{600,800,1150,1920})foreach(string route in new[]{"New","Continue","Load","Travel","Retry","Reset"})
            Run("post-VFX screen foot continuity in render model "+width+"/"+route,()=>{
                using(var f=new DesktopHotfixFixture(width,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);UseHitchBudget(f.field.space);
                    var hero=f.field.hero;var space=f.field.space;space.useMultiZoneCamera=true;
                    if(route=="New" || route=="Continue") { EntryTitle(f,route=="New");EntryReveal(f); }
                    else if(route=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"manual Load");
                    else if(route=="Travel")Check(f.manager.TryTravelToWorldMap(EntryNeighbor(f.manager)),"travel");
                    else if(route=="Retry") { hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked(); }
                    else { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    StanceSpeeds(hero,0);EntryTick(f,HeroSpawnVfx.Duration);
                    Near(space.Position(hero.heroRect).x,576,"VFX finishes at 80%, not HomeX");
                    int life=hero.SpawnVersion;var body=SpriteBody(hero.heroRect);
                    var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();
                    var terrain=f.field.ground.GetComponent<GroundPresentation>();
                    foreach(float input in new[]{.0131396f,.2800149f,.016f,.033f,.1f}) {
                        var before=RectTransformUtility.WorldToScreenPoint(null,HeroRenderProbe.LogicFootWorld(hero.heroRect));
                        float x=space.Position(hero.heroRect).x,y=space.Position(hero.heroRect).y,phase=terrain.TilePhase;
                        Time.frameCount++;Time.time+=input;Time.deltaTime=Time.unscaledDeltaTime=input;
                        Call(space,"Update");
                        float step=x-space.Position(hero.heroRect).x;
                        Check(step>=0 && step<=450f*Mathf.Min(input,1f/30f)+.002f,"pre-camera step bounded per render");
                        Check(Mathf.Abs(space.Position(hero.heroRect).y-y)<=hero.laneSpeed/30f+.002f,"Y bounded");
                        Check(hero.CurrentState==HeroState.Approaching,"wave selects approach target, not teleport/reset");
                        float pan=Get<float>(space,"plannedCameraPan");Call(space,"LateUpdate");
                        Call(bridge,"Update");Call(terrain,"LateUpdate");
                        var after=RectTransformUtility.WorldToScreenPoint(null,HeroRenderProbe.ImageReferenceWorld(body,bridge.BodyReferenceFootUV,true));
                        float scale=Get<Canvas>(space,"battleCanvas").scaleFactor;
                        Near(after.x-before.x,(-step+pan)*scale,"screen change = own bounded walk + one camera pan");
                        Near(space.LastAppliedCameraPan,pan,"unchanged shared camera plan");
                        Near(terrain.TilePhase,BattleMotion.Wrap(phase+pan,0,space.Width),"Ground follows same pan");
                        CheckRenderFeet(hero,"post VFX",true);
                        Check(hero.SpawnVersion==life,"no second spawn");
                        if(width==1920 && route=="New")Console.WriteLine("MONO MOVEMENT: "+hero.DescribePosition("post-VFX bounded walk"));
                    }
                }
            });
        }
        finally
        {
            Time.frameCount=oldFrame;Time.time=oldTime;Time.deltaTime=oldDelta;Time.unscaledDeltaTime=oldUnscaled;
            Screen.width=oldWidth;Screen.height=oldHeight;Resources.Unregister("Spawn/Appear");
        }
    }
}
