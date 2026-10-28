using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Synchronous Play Mode tests: native Instantiate/Awake/RectTransform/Canvas,
    // with no scene load, SaveManager, save I/O or GameManager.Start.
    [TestFixture]
    public sealed class B2SpawnPositionTests
    {
        const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static Type Runtime(string name)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type=assembly.GetType(name,false);
                if(type!=null)return type;
            }
            throw new TypeLoadException(name);
        }
        static object Get(object target,string name)
        {
            var field=target.GetType().GetField(name,Members);
            return field!=null?field.GetValue(target):target.GetType().GetProperty(name,Members).GetValue(target,null);
        }
        static void Set(object target,string name,object value)
        {
            target.GetType().GetField(name,Members).SetValue(target,value);
        }
        static object Call(object target,string name,params object[] args)
        {
            var type=target as Type ?? target.GetType();
            foreach(var method in type.GetMethods(Members|BindingFlags.Static))
                if(method.Name==name && method.GetParameters().Length==args.Length)
                    return method.Invoke(target is Type?null:target,args);
            throw new MissingMethodException(target.GetType().Name,name);
        }
        static Vector2 Position(Component space,RectTransform rect)
        {
            return (Vector2)Call(space,"Position",rect);
        }
        static Rect BodyBounds(RectTransform area,Component hero)
        {
            var root=(RectTransform)Get(hero,"heroRect");
            var bridge=root.GetComponent(Runtime("CultivationSpriteBridge"));
            var image=(Component)Get(bridge,"BodyGraphic");
            Assert.That(image,Is.Not.Null,"Real imported sprite must bind to its own Image.");
            var rect=(RectTransform)image.transform;
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var low=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var high=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var corner in corners)
            {
                Vector2 p=area.InverseTransformPoint(corner);
                low=Vector2.Min(low,p);high=Vector2.Max(high,p);
            }
            return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
        }
        sealed class Fixture : IDisposable
        {
            readonly List<GameObject> roots=new List<GameObject>();
            readonly List<ScriptableObject> data=new List<ScriptableObject>();
            internal Component Manager,Space,Primary;
            internal RectTransform Area;
            internal Canvas Canvas;
            internal IList Heroes;
            GameObject Root(string name,params Type[] components)
            {
                var root=new GameObject(name,components);roots.Add(root);return root;
            }
            internal Fixture(float projectionWidth)
            {
                Assert.That(Application.isPlaying,Is.True,"Run this fixture in Play Mode.");
                Assert.That(Time.timeScale,Is.GreaterThan(0f),"Do not unpause a scene owned by the user.");
                // Refuse an existing gameplay environment; never hide/deactivate
                // managers from the user's scene and never install SaveManager.
                foreach(string name in new[]{"SaveManager","GameManager","CombatManager","EnvironmentManager","HeroController","MonsterController"})
                    Assert.That(UnityEngine.Object.FindObjectsByType(Runtime(name),FindObjectsInactive.Include,FindObjectsSortMode.None),
                        Is.Empty,"Use an empty scene; existing "+name+" is not owned by this fixture.");
                Heroes=(IList)Runtime("HeroController").GetField("ActiveHeroes").GetValue(null);
                Assert.That(Heroes.Count,Is.EqualTo(0),"Do not clear somebody else's actor registry.");
                try
                {
                    Canvas=Root("B2 native test Canvas",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();
                    Canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                    Area=Root("B2 native test BattleArea",typeof(RectTransform)).GetComponent<RectTransform>();
                    // It is already a standalone gameplay canvas: no CanvasScaler
                    // or source-Canvas migration is needed by this fixture.
                    Area.sizeDelta=new Vector2(1920,100);Area.pivot=new Vector2(.5f,0);
                    var environment=Root("B2 native test Environment");environment.SetActive(false);
                    Space=environment.AddComponent(Runtime("EnvironmentManager"));
                    ((Behaviour)Space).enabled=false;Set(Space,"battleArea",Area);
                    environment.SetActive(true); // Real Awake, with area assigned.
                    Area.SetParent(Canvas.transform,false);
                    Canvas.scaleFactor=projectionWidth/1920f;
                    Area.anchorMin=Area.anchorMax=new Vector2(.5f,0);
                    Area.anchoredPosition=Vector2.zero;

                    var body=Root("B2 native test primary body",typeof(RectTransform),Runtime("UnityEngine.UI.Image"));
                    var rect=(RectTransform)body.transform;rect.SetParent(Area,false);
                    rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);
                    rect.sizeDelta=new Vector2(50,100);
                    var rig=Root("B2 native test primary controller");rig.SetActive(false);
                    Primary=rig.AddComponent(Runtime("HeroController"));Set(Primary,"heroRect",rect);
                    // Legacy full-frame containment reference. New foot-lane
                    // cases below opt into the production movement policy.
                    Set(Primary,"enableRangedLaneMotion",false);
                    rig.SetActive(true); // Native deferred Awake, not a reflection imitation.
                    Call(Primary,"SpawnHero",true,(Vector2?)new Vector2(576,0));

                    var managers=Root("B2 native test managers");managers.SetActive(false);
                    Manager=managers.AddComponent(Runtime("GameManager"));((Behaviour)Manager).enabled=false;
                    var combat=managers.AddComponent(Runtime("CombatManager"));((Behaviour)combat).enabled=false;
                    Set(Manager,"heroController",Primary);Set(Manager,"environmentManager",Space);
                    Set(Manager,"combatManager",combat);Set(Manager,"hasDeployed",true);
                    var heroData=ScriptableObject.CreateInstance(Runtime("EntityDataSO"));data.Add(heroData);
                    Set(heroData,"baseHealth",10000);Set(heroData,"expToNextLevel",1000000);
                    Set(Manager,"runtimeHeroData",heroData);
                    managers.SetActive(true);
                    Call(combat,"Start");Call(combat,"SetupHeroInfo",heroData);
                    UnityEngine.Canvas.ForceUpdateCanvases();
                }
                catch { Dispose();throw; }
            }
            public void Dispose()
            {
                // Only fixture-owned objects. No filesystem operations, singleton
                // resets, scene unloads, automatic hooks or deferred Start calls.
                if(Manager!=null)
                {
                    // Runtime cleanup schedules Destroy for the end of frame.
                    // Record these owned objects first, then finish synchronous
                    // teardown so the next NUnit case cannot see leftover rigs.
                    foreach(var member in (IEnumerable)Get(Manager,"testCompanions"))
                    {
                        roots.Add((GameObject)Get(member,"ControllerRoot"));
                        roots.Add((GameObject)Get(member,"VisualRoot"));
                        data.Add((ScriptableObject)Get(member,"Data"));
                    }
                    Call(Manager,"ClearTestCompanions");
                }
                for(int i=roots.Count-1;i>=0;i--)
                    if(roots[i]!=null)UnityEngine.Object.DestroyImmediate(roots[i]);
                foreach(var item in data)if(item!=null)UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [TestCase(600f)]
        [TestCase(800f)]
        [TestCase(1150f)]
        public void NativeClone_SpawnsTwoIndependentBodiesInsideArea_WithoutRetargeting(float projectionWidth)
        {
            using(var f=new Fixture(projectionWidth))
            {
                var primaryRect=(RectTransform)Get(f.Primary,"heroRect");
                // A high lane reproduces the old +25 Y overflow; near-right
                // placement exercises the fallback to the other side of the group.
                Call(f.Space,"SetPosition",primaryRect,new Vector2(930,85));
                primaryRect.localScale=Vector3.one*1.2f; // transient Attack scale
                var before=Position(f.Space,primaryRect);
                var subject=Get(f.Space,"cameraTarget");
                var velocity=Get(f.Space,"CameraFollowVelocity");
                var focus=Get(f.Space,"CurrentCameraSmoothedFocusX");
                var bodies=new HashSet<Component>();
                for(int count=1;count<=2;count++)
                {
                    Call(f.Manager,"CycleTestCompanions");Canvas.ForceUpdateCanvases();
                    Assert.That(Get(f.Manager,"TestCompanionCount"),Is.EqualTo(count));
                    Assert.That(f.Heroes.Count,Is.EqualTo(count+1));
                    Assert.That(Position(f.Space,primaryRect),Is.EqualTo(before));
                    Assert.That(Get(f.Space,"cameraTarget"),Is.SameAs(subject));
                    Assert.That(Get(f.Space,"CameraFollowVelocity"),Is.EqualTo(velocity));
                    Assert.That(Get(f.Space,"CurrentCameraSmoothedFocusX"),Is.EqualTo(focus));
                    bodies.Clear();
                    foreach(Component hero in f.Heroes)
                    {
                        var root=(RectTransform)Get(hero,"heroRect");
                        var bridge=root.GetComponent(Runtime("CultivationSpriteBridge"));
                        var image=(Component)Get(bridge,"BodyGraphic");
                        Assert.That(bodies.Add(image),Is.True,"Each actor owns a different Image.");
                        Assert.That(image.transform,Is.Not.SameAs(root),"Imported sprite body, not the fallback root Image.");
                        Assert.That(image.transform.IsChildOf(root),Is.True,"No reference to primary's Image.");
                        if(hero==f.Primary)continue;
                        Assert.That(root.parent,Is.SameAs(f.Area));
                        Assert.That(root.anchorMin,Is.EqualTo(primaryRect.anchorMin));
                        Assert.That(root.anchorMax,Is.EqualTo(primaryRect.anchorMax));
                        Assert.That(root.pivot,Is.EqualTo(new Vector2(.5f,0)));
                        Assert.That(root.localScale,Is.EqualTo(Vector3.one));
                        var bounds=BodyBounds(f.Area,hero);
                        Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(f.Area.rect.xMin-.002f));
                        Assert.That(bounds.xMax,Is.LessThanOrEqualTo(f.Area.rect.xMax+.002f));
                        Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(f.Area.rect.yMin-.002f));
                        Assert.That(bounds.yMax,Is.LessThanOrEqualTo(f.Area.rect.yMax+.002f));
                        foreach(Component peer in f.Heroes)if(peer!=hero)
                            Assert.That(bounds.Overlaps(BodyBounds(f.Area,peer)),Is.False,"No body overlap at spawn.");
                    }
                }
                // Real Canvas reprojection at each supported scale; this does
                // not claim to resize an OS window or execute a Windows Player.
                foreach(float width in new[]{600f,800f,1150f})
                {
                    f.Canvas.scaleFactor=width/1920f;Canvas.ForceUpdateCanvases();
                    foreach(Component hero in f.Heroes)if(hero!=f.Primary)
                    {
                        var bounds=BodyBounds(f.Area,hero);
                        Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(f.Area.rect.xMin-.002f));
                        Assert.That(bounds.xMax,Is.LessThanOrEqualTo(f.Area.rect.xMax+.002f));
                        Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(f.Area.rect.yMin-.002f));
                        Assert.That(bounds.yMax,Is.LessThanOrEqualTo(f.Area.rect.yMax+.002f));
                    }
                }
            }
        }

        static Vector2 NativeFootScreen(Component hero)
        {
            var root=(RectTransform)Get(hero,"heroRect");
            var bridge=root.GetComponent(Runtime("CultivationSpriteBridge"));
            var image=(Component)Get(bridge,"BodyGraphic");
            var rect=(RectTransform)image.transform;
            Vector2 uv=(Vector2)Get(bridge,"BodyReferenceFootUV");
            var bounds=rect.rect;
            return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(
                new Vector3(bounds.xMin+bounds.width*uv.x,bounds.yMin+bounds.height*uv.y,0)));
        }

        static void EquipPrimaryWeapon(Fixture f,int mode)
        {
            var hero=Get(f.Manager,"HeroData");
            var factory=Runtime("EquipmentFoundation");
            Call(Runtime("EquipmentRuntime"),"InitializeNewRun",hero);
            if(mode!=0) {
                var item=Call(factory,mode==1?"CreateDemoBow":"CreateDemoStaff");
                Assert.That(Call(Runtime("EquipmentRuntime"),"AddInstance",hero,item),Is.True);
                Assert.That(Call(Runtime("EquipmentRuntime"),"Equip",hero,Get(item,"instanceId"),
                    Enum.Parse(Runtime("EquipmentSlot"),"Weapon")),Is.True);
            }
            Call(Get(f.Manager,"combatManager"),"RefreshWeaponAttack",f.Primary);
        }
        [TestCase(1,600f)]
        [TestCase(1,800f)]
        [TestCase(1,1150f)]
        [TestCase(2,600f)]
        [TestCase(2,800f)]
        [TestCase(2,1150f)]
        public void NativeRangedFootLanes_ResetContinuously_AndRecruitIndependentBodies(int mode,float projectionWidth)
        {
            // Native transforms and imported art; injected simulation steps do
            // not constitute a rendered multi-map/Windows acceptance test.
            using(var f=new Fixture(projectionWidth))
            {
                Set(f.Primary,"enableRangedLaneMotion",true);
                EquipPrimaryWeapon(f,mode);
                var rect=(RectTransform)Get(f.Primary,"heroRect");
                float low=f.Area.rect.yMin+12f,high=f.Area.rect.yMax-12f;
                for(int life=0;life<3;life++)
                {
                    Call(f.Primary,"SpawnHero",false,(Vector2?)new Vector2(576,0));
                    Assert.That(Position(f.Space,rect).y,Is.EqualTo(50f).Within(.002f),"Reset before reveal, independent of previous lane.");
                    Assert.That((float)Get(f.Primary,"RangedLaneHoldRemaining"),Is.EqualTo(0f));
                    float min=50f,max=50f;
                    for(int i=0;i<960;i++)
                    {
                        var before=Position(f.Space,rect);
                        Call(f.Primary,"TickMovement",f.Space,1f/60f);
                        var after=Position(f.Space,rect);
                        Assert.That(after.y,Is.InRange(low-.002f,high+.002f));
                        Assert.That(Mathf.Abs(after.y-before.y),Is.LessThanOrEqualTo(8f/60f+.002f));
                        Assert.That((float)Get(f.Primary,"UnexpectedLaneDeltaY"),Is.EqualTo(0f).Within(.002f));
                        min=Mathf.Min(min,after.y);max=Mathf.Max(max,after.y);
                    }
                    Assert.That(max-min,Is.GreaterThan(15f),"Stable held targets actually traverse the floor.");
                }
                var paused=Position(f.Space,rect);
                Call(f.Primary,"TickMovement",f.Space,0f);
                Assert.That(Position(f.Space,rect),Is.EqualTo(paused),"Zero movement dt cannot change lane.");
                Call(f.Primary,"SpawnHero",false,(Vector2?)new Vector2(576,0));
                var focus=Get(f.Space,"CurrentCameraSmoothedFocusX");
                Call(f.Manager,"CycleTestCompanions");Call(f.Manager,"CycleTestCompanions");
                Assert.That(f.Heroes.Count,Is.EqualTo(3));
                var images=new HashSet<Component>();
                foreach(Component hero in f.Heroes)
                {
                    var root=(RectTransform)Get(hero,"heroRect");
                    var bridge=root.GetComponent(Runtime("CultivationSpriteBridge"));
                    Assert.That(images.Add((Component)Get(bridge,"BodyGraphic")),Is.True);
                    if(!(bool)Get(hero,"UsesRangedLanes"))continue;
                    Assert.That(Position(f.Space,root).y,Is.InRange(low-.002f,high+.002f));
                    Canvas.ForceUpdateCanvases();
                    var foot=RectTransformUtility.WorldToScreenPoint(null,root.position);
                    Assert.That(Vector2.Distance(NativeFootScreen(hero),foot),Is.LessThanOrEqualTo(.01f));
                }
                Assert.That(Get(f.Space,"CurrentCameraSmoothedFocusX"),Is.EqualTo(focus),"Recruiting does not retarget camera.");
            }
        }

        [TestCase(0,450f)]
        [TestCase(1,450f)]
        [TestCase(2,200f)]
        public void NativeVfxFinish_InjectedLongWalkingStep_IsBoundedWithoutCatchUp(int mode,float speed)
        {
            // Real Unity objects/imported sprites/Canvas/VFX, but injected dt.
            // Synchronous: does NOT reproduce a 280-ms native render/CPU stall.
            using(var f=new Fixture(1150f))
            {
                Assert.That((float)Get(f.Space,"maxHeroMovementDeltaTime"),Is.EqualTo(1f/30f));
                Set(f.Primary,"useStanceMoveSpeeds",true);
                Set(f.Primary,"meleeMoveSpeed",450f);Set(f.Primary,"physicalMoveSpeed",450f);Set(f.Primary,"magicMoveSpeed",200f);
                EquipPrimaryWeapon(f,mode);
                var rect=(RectTransform)Get(f.Primary,"heroRect");
                Call(f.Space,"SetPosition",rect,new Vector2(576,0));
                var fx=(Component)Runtime("HeroSpawnVfx").GetMethod("Prepare",BindingFlags.Public|BindingFlags.Static)
                    .Invoke(null,new object[]{f.Primary});
                Assert.That(Call(fx,"Begin"),Is.True,"Use the real imported Appear sheet.");
                Call(fx,"Advance",.4f);Canvas.ForceUpdateCanvases();
                var before=NativeFootScreen(f.Primary);
                Call(fx,"Advance",1f);Canvas.ForceUpdateCanvases();
                Assert.That(NativeFootScreen(f.Primary).x,Is.EqualTo(before.x).Within(.01f),"Finish itself does not reposition body.");
                Assert.That(NativeFootScreen(f.Primary).y,Is.EqualTo(before.y).Within(.01f));
                Assert.That(Position(f.Space,rect).x,Is.EqualTo(576f).Within(.002f),"80-percent spawn.");
                foreach(float dt in new[]{.016f,.033f,.1f,.28f,.016f})
                {
                    var p=Position(f.Space,rect);before=NativeFootScreen(f.Primary);
                    Call(f.Primary,"TickMovement",f.Space,dt);Canvas.ForceUpdateCanvases();
                    float expected=speed*Mathf.Min(dt,1f/30f);
                    Assert.That(p.x-Position(f.Space,rect).x,Is.EqualTo(expected).Within(.002f));
                    Assert.That((float)Get(f.Primary,"MovementDeltaThisFrame"),Is.EqualTo(Mathf.Min(dt,1f/30f)));
                    Assert.That(NativeFootScreen(f.Primary).x-before.x,
                        Is.EqualTo(-expected*f.Canvas.scaleFactor).Within(.01f),"Native screen displacement is bounded; no residual debt.");
                }
            }
        }

        [TestCase(600f)]
        [TestCase(800f)]
        [TestCase(1150f)]
        public void NativeB2Party_InjectedLongWalkingStep_UsesSameBudget(float projectionWidth)
        {
            using(var f=new Fixture(projectionWidth))
            {
                Call(f.Manager,"CycleTestCompanions");Call(f.Manager,"CycleTestCompanions");
                Assert.That(f.Heroes.Count,Is.EqualTo(3));
                var positions=new Dictionary<Component,Vector2>();
                foreach(Component hero in f.Heroes)positions.Add(hero,Position(f.Space,(RectTransform)Get(hero,"heroRect")));
                foreach(Component hero in f.Heroes)Call(hero,"TickMovement",f.Space,.28f);
                Canvas.ForceUpdateCanvases();
                foreach(Component hero in f.Heroes)
                {
                    float speed=(float)Call(hero,"EffectiveMoveSpeed");
                    var rect=(RectTransform)Get(hero,"heroRect");
                    float distance=positions[hero].x-Position(f.Space,rect).x;
                    Assert.That(distance,Is.GreaterThanOrEqualTo(0f));
                    Assert.That(distance,Is.LessThanOrEqualTo(speed/30f+.002f));
                    Assert.That((float)Get(hero,"MovementDeltaThisFrame"),Is.EqualTo(1f/30f));
                }
            }
        }
    }
}
