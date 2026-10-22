using System;
using System.IO;
using UnityEngine;

partial class MotionRegression
{
    // Bounds of the full body Image, including transparent padding. This is a
    // conservative containment check; HP bars/popups are deliberately excluded.
    static Rect B2BodyBounds(EnvironmentManager space,HeroController hero)
    {
        var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();
        var graphic=bridge!=null?bridge.BodyGraphic:hero.heroRect.GetComponent<UnityEngine.UI.Image>();
        var rect=graphic!=null?graphic.rectTransform:hero.heroRect;
        float minX=float.PositiveInfinity,minY=minX,maxX=float.NegativeInfinity,maxY=maxX;
        for(int i=0;i<4;i++) {
            var p=space.BattleArea.InverseTransformPoint(rect.TransformPoint(new Vector3(
                i%2==0?rect.rect.xMin:rect.rect.xMax,i<2?rect.rect.yMin:rect.rect.yMax,0)));
            minX=Mathf.Min(minX,p.x);minY=Mathf.Min(minY,p.y);
            maxX=Mathf.Max(maxX,p.x);maxY=Mathf.Max(maxY,p.y);
        }
        return new Rect(minX,minY,maxX-minX,maxY-minY);
    }
    static bool B2BodiesOverlap(Rect a,Rect b)
    {
        return a.xMin<b.xMax-.002f && a.xMax>b.xMin+.002f &&
            a.yMin<b.yMax-.002f && a.yMax>b.yMin+.002f;
    }
    static void CheckB2BodyInside(EnvironmentManager space,HeroController hero)
    {
        var body=B2BodyBounds(space,hero);var area=space.BattleArea.rect;
        Check(body.xMin>=area.xMin-.002f && body.xMax<=area.xMax+.002f,
            "whole body inside BattleArea X: "+body.xMin+".."+body.xMax);
        Check(body.yMin>=area.yMin-.002f && body.yMax<=area.yMax+.002f,
            "whole body inside BattleArea Y: "+body.yMin+".."+body.yMax);
    }
    static void B2SpawnPositionTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        using(var sprites=new SpriteResources())
        {
            foreach(int width in new[]{600,800,1150})foreach(float lane in new[]{0f,85f})foreach(float fraction in new[]{.8f,.985f})
            Run("B2 actual summon path bounds, independent bodies and resize "+width+"/"+lane+"/"+fraction,()=>{
                using(var f=new DesktopHotfixFixture(width,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var space=f.field.space;var primary=f.field.hero;
                    space.SetPosition(primary.heroRect,new Vector2(space.BattleArea.rect.xMin+space.Width*fraction,lane));
                    // Spawn during actual movement, rather than merely assigning
                    // a moving state. Capture the post-movement/pan position.
                    NativeOrderTraceFrame(f,1f/60f);
                    Check(primary.MovementDistanceThisFrame>0,"primary was walking immediately before summon");
                    var initial=space.Position(primary.heroRect);var primaryBody=SpriteBody(primary.heroRect);
                    var subject=Get<HeroController>(space,"cameraTarget");float velocity=space.CameraFollowVelocity;
                    float focus=space.CurrentCameraSmoothedFocusX;
                    for(int count=1;count<=2;count++) {
                        // Summoning must not borrow the primary's moving/attack scale.
                        primary.heroRect.localScale=Vector3.one*1.2f;
                        f.manager.CycleTestCompanions();
                        Check(f.manager.TestCompanionCount==count,"companion created via UI entry point");
                        Near(space.Position(primary.heroRect).x,initial.x,"primary X untouched");
                        Near(space.Position(primary.heroRect).y,initial.y,"primary Y untouched");
                        Check(Get<HeroController>(space,"cameraTarget")==subject,"camera subject unchanged");
                        Near(space.CameraFollowVelocity,velocity,"camera spring not reset by summon");
                        Near(space.CurrentCameraSmoothedFocusX,focus,"no focus mutation during summon");
                        foreach(var h in HeroController.ActiveHeroes)if(h!=primary) {
                            Check(h.heroRect.parent==space.BattleArea,"same gameplay parent");
                            CheckB2BodyInside(space,h);
                            Check(SpriteBody(h.heroRect)!=primaryBody,"independent Image");
                            Near(h.heroRect.localScale.y,1,"clone feedback reset");
                            foreach(var peer in HeroController.ActiveHeroes)if(peer!=h)
                                Check(!B2BodiesOverlap(B2BodyBounds(space,h),B2BodyBounds(space,peer)),"bodies do not overlap at summon");
                        }
                    }
                    foreach(int resized in new[]{600,800,1150}) {
                        Screen.width=resized;
                        space.SyncBattleProjection();
                        ModelCanvasProjection(space.BattleArea.parent.GetComponent<Canvas>());
                        foreach(var h in HeroController.ActiveHeroes)if(h!=primary)CheckB2BodyInside(space,h);
                    }
                    f.manager.CycleTestCompanions();
                    Check(f.manager.TestCompanionCount==0 && HeroController.ActiveHeroes.Count==1,"recall leaves primary only");
                }
            });
            Run("B2 rejects an area smaller than its sprite without moving primary or registering a broken actor",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);var space=f.field.space;
                    space.BattleArea.sizeDelta=new Vector2(100,100);
                    var primary=f.field.hero;space.SetPosition(primary.heroRect,Vector2.zero);
                    int registered=f.field.combat.RegisteredHeroCount;
                    f.manager.CycleTestCompanions();
                    Check(f.manager.TestCompanionCount==0,"refuse rather than scale/teleport outside area");
                    Check(HeroController.ActiveHeroes.Count==1,"no partial deployed actor");
                    Check(f.field.combat.RegisteredHeroCount==registered,"combat roster untouched");
                    Near(space.Position(primary.heroRect).x,0,"primary stays put");
                    Check(Get<HeroController>(space,"cameraTarget")==primary,"camera target untouched on refusal");
                }
            });
        }
    }
}
