using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static void EnableLanes(HeroController hero,int mode)
    {
        hero.enableRangedLaneMotion=true;StanceSpeeds(hero,mode);
    }
    static void CheckFootLane(EnvironmentManager space,HeroController hero)
    {
        float low,high;hero.RangedFootBounds(space,out low,out high);
        float y=space.Position(hero.heroRect).y;
        Check(y>=low-.002f && y<=high+.002f,"ranged foot in safe floor interval: "+y);
    }
    static List<float> LaneXReference(bool enabled)
    {
        UnityEngine.Random.InitState(22211);Time.time=0;Time.frameCount=0;
        var result=new List<float>();
        using(var f=new DesktopHotfixFixture(800,true)) {
            WaveEntryScene(f);WaveEntryFinish(f);f.field.space.useMultiZoneCamera=true;
            f.field.hero.enableRangedLaneMotion=enabled;
            f.manager.CycleTestCompanions();f.manager.CycleTestCompanions();EndWave(f);
            f.manager.distancePerWave=1000000;
            for(int i=0;i<600;i++) {
                NativeOrderTraceFrame(f,1f/60f);
                foreach(var hero in HeroController.ActiveHeroes) {
                    result.Add(f.field.space.Position(hero.heroRect).x);
                    result.Add(hero.MovementDistanceThisFrame);result.Add(hero.MovementBudgetThisFrame);
                }
                var terrain=f.field.ground.GetComponent<GroundPresentation>();
                result.Add(f.manager.ExplorationDistance);result.Add(f.field.space.LastAppliedCameraPan);
                result.Add(f.field.space.CurrentCameraFocusX);result.Add(terrain.TilePhase);
                result.Add(terrain.FarPhase);result.Add(terrain.DistantPhase);
            }
        }
        return result;
    }
    static void RangedLaneTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        int oldFrame=Time.frameCount,oldWidth=Screen.width,oldHeight=Screen.height;
        float oldTime=Time.time,oldDt=Time.deltaTime,oldUnscaled=Time.unscaledDeltaTime;
        using(var sprites=new SpriteResources())
        try
        {
            foreach(int mode in new[]{1,2})
            Run("Y diagnostic reproduces bottom initialization/retention without a cumulative subtraction / "+mode,()=>{
                var f=new Field();StanceSpeeds(f.hero,mode);
                for(int map=0;map<4;map++) {
                    f.hero.SpawnHero(false,new Vector2(100,0));
                    for(int i=0;i<60;i++)FormationTick(f,1f/60f,f.hero);
                    Near(f.space.Position(f.hero.heroRect).y,0,"old ranged path retains requested zero");
                }
                f.hero.enableRangedLaneMotion=true;f.hero.rangedLaneAmplitude=0;
                f.hero.SpawnHero(false,new Vector2(100,0));
                Near(f.space.Position(f.hero.heroRect).y,50,"safe spawn works even with exploratory motion disabled");
                for(int i=0;i<120;i++)FormationTick(f,1f/60f,f.hero);
                Near(f.space.Position(f.hero.heroRect).y,50,"initialization fix independent of lane animation");
            });
            Run("lane target holds after arrival, combat is stable, pause does not advance, no global random consumption",()=>{
                var lane=new HeroLaneMotion();lane.Reset(false);float y=50;
                UnityEngine.Random.InitState(1981);int expected=UnityEngine.Random.Range(0,10000);UnityEngine.Random.InitState(1981);
                for(int i=0;i<120;i++)y=lane.Step(y,12,88,true,1f/60,1f/60,10,10,4);
                Near(y,60,"first goal");Near(lane.TargetY,60,"holds goal");
                for(int i=0;i<120;i++)y=lane.Step(y,12,88,true,1f/60,1f/60,10,10,4);
                Near(y,60,"still holds four seconds after arrival, not re-randomized per frame");
                float hold=lane.HoldRemaining;
                for(int i=0;i<120;i++)y=lane.Step(y,12,88,true,0,0,10,10,4);
                Near(lane.HoldRemaining,hold,"pause freezes hold");
                for(int i=0;i<600;i++)y=lane.Step(y,12,88,false,1f/60,1f/60,10,10,4);
                Near(y,60,"combat never bobs");
                Check(UnityEngine.Random.Range(0,10000)==expected,"Unity random/damage rolls untouched");
            });
            foreach(int mode in new[]{1,2})foreach(int fps in new[]{30,60,120})
            Run("ranged lanes bounded/continuous for four fresh lives / "+mode+"/"+fps,()=>{
                var f=new Field(limitRenderMovement:true);EnableLanes(f.hero,mode);
                f.hero.heroRect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachHero(f.hero);
                for(int life=0;life<4;life++) {
                    f.hero.SpawnHero(false,new Vector2(100,0));Near(f.space.Position(f.hero.heroRect).y,50,"no life-to-life Y drift");
                    float min=50,max=50;
                    for(int i=0;i<fps*16;i++) {
                        float x=f.space.Position(f.hero.heroRect).x,y=f.space.Position(f.hero.heroRect).y;
                        FormationTick(f,1f/fps,f.hero);var p=f.space.Position(f.hero.heroRect);
                        Near(x-p.x,f.hero.EffectiveMoveSpeed()/fps,"X speed unaffected");
                        Check(Mathf.Abs(p.y-y)<=f.hero.rangedLaneSpeed/fps+.002f,"bounded vertical step");
                        CheckFootLane(f.space,f.hero);CheckRenderFeet(f.hero,"ranged walking",false);
                        Near(f.hero.UnexpectedLaneDeltaY,0,"no external Y write in modeled loop");
                        min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);
                    }
                    Check(max-min>15,"actually traverses stable goals, not stuck at floor");
                    f.hero.HideHero();
                }
            });
            foreach(int mode in new[]{1,2})
            Run("ranged firing stays at current lane across target changes, Attack/Hit, and detects external Y writes / "+mode,()=>{
                var f=new Field(limitRenderMovement:true);EnableLanes(f.hero,mode);
                f.hero.heroRect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachHero(f.hero);
                f.hero.SpawnHero(false,new Vector2(0,50));var enemy=f.Monster(-100,AttackMode.RangedPhysical,90);
                for(int i=0;i<120;i++) {
                    FormationTick(f,1f/60f,f.hero);f.hero.PlayAttackFeedback();CultivationSpriteBridge.Hit(f.hero.heroRect);
                    Near(f.space.Position(f.hero.heroRect).y,50,"no Y animation during firing");
                    Check(f.hero.CanAttack(enemy,f.space),"range still only uses X");
                }
                enemy.MarkDead();f.Monster(-150,AttackMode.RangedPhysical,10);
                FormationTick(f,1f/60f,f.hero);Near(f.space.Position(f.hero.heroRect).y,50,"changing target does not move firing lane");
                f.space.SetPosition(f.hero.heroRect,new Vector2(0,47));FormationTick(f,1f/60f,f.hero);
                Near(f.hero.UnexpectedLaneDeltaY,-3,"unexpected pre-step writer remains observable");
                Check(f.hero.DescribeLaneTrace().Contains("laneUnexpectedY=-3"),"diagnostic carries evidence");
            });
            Run("mode change boundary recovery is smooth; zero speed freezes Y; oversized margins cannot invert interval",()=>{
                var f=new Field(limitRenderMovement:true);f.hero.enableRangedLaneMotion=true;
                StanceSpeeds(f.hero,0);f.hero.SpawnHero(false,new Vector2(0,0));StanceSpeeds(f.hero,1);
                var enemy=f.Monster(-100);FormationTick(f,.28f,f.hero);
                float y=f.space.Position(f.hero.heroRect).y;
                Check(y>0 && y<=8f/30f+.002f,"mode change recovers without teleport");
                enemy.MarkDead();f.hero.useStanceMoveSpeeds=false;f.hero.moveSpeed=0;
                FormationTick(f,.28f,f.hero);Near(f.space.Position(f.hero.heroRect).y,y,"immobilized Hero cannot sidestep");
                f.hero.rangedFootMargin=1000;f.hero.SpawnHero(false,new Vector2(0,0));
                Near(f.space.Position(f.hero.heroRect).y,50,"safe interval still centered");
                CheckFootLane(f.space,f.hero);
            });
            Run("lane-only changes leave full party X/exploration/Multi-Zone/parallax reference identical",()=>{
                var off=LaneXReference(false);var on=LaneXReference(true);
                Check(off.Count==on.Count,"same party and sample count");
                for(int i=0;i<off.Count;i++)Near(on[i],off[i],"X-only reference "+i);
            });
            foreach(int width in new[]{600,800,1150})
            Run("three-Hero floor lanes across three maps and projection resize / "+width,()=>{
                using(var f=new DesktopHotfixFixture(width,true)) {
                    f.field.hero.enableRangedLaneMotion=true;UseHitchBudget(f.field.space);
                    WaveEntryScene(f);WaveEntryFinish(f);f.field.space.useMultiZoneCamera=true;f.manager.distancePerWave=1000000;
                    for(int map=0;map<3;map++) {
                        f.manager.CycleTestCompanions();f.manager.CycleTestCompanions();EndWave(f);
                        Check(HeroController.ActiveHeroes.Count==3,"recruit B2 for this map (existing map entry clears temporary companions)");
                        double expected=0;
                        for(int i=0;i<900;i++) {
                            NativeOrderTraceFrame(f,1f/60f);float least=float.MaxValue;
                            foreach(var hero in HeroController.ActiveHeroes) {
                                // Progress deliberately caps float round-trip
                                // overshoot at the nominal X budget, even if
                                // the measured RectTransform step is a few ULPs larger.
                                least=Mathf.Min(least,Mathf.Min(hero.MovementDistanceThisFrame,hero.MovementBudgetThisFrame));
                                if(hero.UsesRangedLanes)CheckFootLane(f.field.space,hero);
                                Near(hero.UnexpectedLaneDeltaY,0,"no modeled coordinate writer drift");
                            }
                            expected+=least;Near(f.manager.ExplorationDistance,(float)expected,"only minimum actual X counted");
                            if(i==300) {
                                Screen.width=width==600?1150:600;Screen.height=Mathf.RoundToInt(Screen.width*9f/16);
                                f.field.space.SyncBattleProjection();ModelCanvasProjection(Get<Canvas>(f.field.space,"battleCanvas"));
                            }
                        }
                        if(map<2) {
                            Check(f.manager.TryTravelToWorldMap(EntryNeighbor(f.manager)),"next map");EntryTick(f,HeroSpawnVfx.Duration);
                            Near(f.field.space.Position(f.field.hero.heroRect).x,576,"80% X retained");
                        }
                    }
                }
            });
            foreach(int mode in new[]{1,2})foreach(string route in new[]{"New","Continue","Load","Travel","Retry","Reset"})
            Run("ranged safe Y before VFX and lane state reset / "+mode+"/"+route,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);EnableLanes(f.field.hero,mode);
                    f.manager.HeroData.SetSavedAttackMode((AttackMode)mode);
                    Check(f.saves.SaveGame(f.manager.HeroData,SaveSlot.ManualSave1),"fixture slot holds selected mode");
                    Check(f.saves.SaveGame(f.manager.HeroData,SaveSlot.AutoSave),"fixture autosave");
                    if(route=="New" || route=="Continue")EntryTitle(f,route=="New");
                    else if(route=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"Load");
                    else if(route=="Travel")Check(f.manager.TryTravelToWorldMap(EntryNeighbor(f.manager)),"Travel");
                    else if(route=="Retry") { f.field.hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked(); }
                    else { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    var hero=f.field.hero;var space=f.field.space;
                    if(hero.UsesRangedLanes) { CheckFootLane(space,hero);Near(space.Position(hero.heroRect).y,50,"spawn initialization, not animated compensation"); }
                    else Check(hero.attackMode==AttackMode.Melee,"New/Reset retain their existing default stance");
                    float y=space.Position(hero.heroRect).y;
                    if(route=="New" || route=="Continue")EntryReveal(f);
                    EntryTick(f,.35f);Near(space.Position(hero.heroRect).y,y,"VFX does not move foot");
                    EntryTick(f,HeroSpawnVfx.Duration);Near(space.Position(hero.heroRect).y,y,"release has no second Y placement");
                    Near(hero.RangedLaneHoldRemaining,0,"new life clears lane timer");
                }
            });
        }
        finally
        {
            Time.frameCount=oldFrame;Time.time=oldTime;Time.deltaTime=oldDt;Time.unscaledDeltaTime=oldUnscaled;
            Screen.width=oldWidth;Screen.height=oldHeight;Resources.Unregister("Spawn/Appear");
        }
    }
}
