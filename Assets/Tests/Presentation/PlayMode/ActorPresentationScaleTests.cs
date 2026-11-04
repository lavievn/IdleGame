using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    public sealed class ActorPresentationScaleTests
    {
        const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static Type Runtime(string n)
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){var t=a.GetType(n,false);if(t!=null)return t;}
            throw new TypeLoadException(n);
        }
        static object Get(object o,string n)
        {
            var f=o.GetType().GetField(n,All);return f!=null?f.GetValue(o):o.GetType().GetProperty(n,All).GetValue(o);
        }
        static void Set(object o,string n,object v){o.GetType().GetField(n,All).SetValue(o,v);}
        static object Call(object o,string n,params object[] args)
        {
            var type=o as Type??o.GetType();
            foreach(var m in type.GetMethods(All))if(m.Name==n&&m.GetParameters().Length==args.Length)return m.Invoke(o is Type?null:o,args);
            throw new MissingMethodException(type.Name,n);
        }
        static void RequireEmpty()
        {
            Assert.That(Application.isPlaying,Is.True);
            foreach(var n in new[]{"SaveManager","GameManager","CombatManager","EnvironmentManager","HeroController","MonsterController"})
                Assert.That(UnityEngine.Object.FindObjectsByType(Runtime(n),FindObjectsInactive.Include,FindObjectsSortMode.None),Is.Empty,"Use an empty scene.");
        }
        static Vector2 Bounds(RectTransform rect)
        {
            var c=new Vector3[4];rect.GetWorldCorners(c);return new Vector2(Vector3.Distance(c[0],c[3]),Vector3.Distance(c[0],c[1]));
        }
        [TestCase(false,0)] [TestCase(false,1)] [TestCase(false,2)]
        [TestCase(true,0)] [TestCase(true,1)] [TestCase(true,2)]
        public void EveryActorBodyDefaultsToDoubleSizeWithoutMovingItsFootOrLogicalRoot(bool enemy,int mode)
        {
            RequireEmpty();var random=UnityEngine.Random.state;
            var go=new GameObject("Owned actor scale",typeof(RectTransform),Runtime("UnityEngine.UI.Image"));go.SetActive(false);
            try
            {
                var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(50,100);rect.pivot=new Vector2(.5f,0);
                var bar=new GameObject("Owned HP bar",typeof(RectTransform)).GetComponent<RectTransform>();bar.SetParent(rect,false);bar.sizeDelta=new Vector2(44,4);
                var fill=new GameObject("Owned HP fill",typeof(RectTransform),Runtime("UnityEngine.UI.Image"));fill.transform.SetParent(bar,false);
                var actor=go.AddComponent(Runtime(enemy?"MonsterController":"HeroController"));
                if(!enemy)Set(actor,"heroRect",rect);
                Set(actor,"hpFillImage",fill.GetComponent(Runtime("UnityEngine.UI.Image")));
                Set(actor,"attackMode",Enum.ToObject(Runtime("TuTienCore.AttackMode"),mode));
                go.SetActive(true);
                Set(actor,"attackMode",Enum.ToObject(Runtime("TuTienCore.AttackMode"),mode));
                Call(Runtime("CultivationSpriteBridge"),enemy?"AttachMonster":"AttachHero",actor);
                var bridge=rect.GetComponent(Runtime("CultivationSpriteBridge"));
                var image=(Component)Get(bridge,"BodyGraphic");
                var body=(RectTransform)image.transform;
                Assert.That(image.gameObject,Is.Not.SameAs(go),"Imported sprite body must exist.");
                Assert.That(Get(bridge,enemy?"monsterScale":"heroScale"),Is.EqualTo(2f));
                Assert.That(rect.InverseTransformPoint(bar.position).y,Is.GreaterThan(((Vector2)Get(bridge,"VisibleBodySize")).y));
                Assert.That(bar.sizeDelta,Is.EqualTo(new Vector2(44,4)),"Health bar is repositioned, not enlarged.");
                Vector2 doubleSize=Bounds(body);Vector3 foot=rect.position;Vector2 rootSize=rect.sizeDelta;
                Set(bridge,enemy?"monsterScale":"heroScale",1f);Call(bridge,"ApplyLayout");
                Vector2 oldSize=Bounds(body);
                Assert.That(doubleSize.x,Is.EqualTo(oldSize.x*2).Within(.01f));
                Assert.That(doubleSize.y,Is.EqualTo(oldSize.y*2).Within(.01f));
                Assert.That(rect.position,Is.EqualTo(foot));Assert.That(rect.sizeDelta,Is.EqualTo(rootSize));
                Assert.That(rect.localScale,Is.EqualTo(Vector3.one));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Random.state=random;}
        }
        [Test]
        public void WholeBattlefieldMovesUp20PixelsWithLogicAndDetachedLootUnchanged()
        {
            RequireEmpty();var objects=new List<GameObject>();
            try
            {
                var source=new GameObject("Owned projection source",typeof(RectTransform),typeof(Canvas));objects.Add(source);
                source.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var area=new GameObject("Owned projection Ground",typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(source.transform,false);area.sizeDelta=new Vector2(1920,200);area.pivot=new Vector2(.5f,0);
                var env=new GameObject("Owned projection manager");objects.Add(env);env.SetActive(false);
                var space=env.AddComponent(Runtime("EnvironmentManager"));((Behaviour)space).enabled=false;
                Set(space,"battleArea",area);Set(space,"battleVerticalOffsetPixels",0f);env.SetActive(true);
                objects.Add(area.parent.gameObject); // Stable Battle Canvas created by native Awake.
                var child=new GameObject("Owned Ground decoration",typeof(RectTransform)).GetComponent<RectTransform>();
                child.SetParent(area,false);child.anchoredPosition=new Vector2(123,42);
                Canvas.ForceUpdateCanvases();
                var logic=(Vector2)Call(space,"Position",child);float width=(float)Get(space,"Width");
                Vector2 before=RectTransformUtility.WorldToScreenPoint(null,child.position);
                Set(space,"battleVerticalOffsetPixels",20f);Call(space,"SyncBattleProjection");Canvas.ForceUpdateCanvases();
                Vector2 after=RectTransformUtility.WorldToScreenPoint(null,child.position);
                Assert.That(after.y-before.y,Is.EqualTo(20f).Within(.01f));
                Assert.That(after.x,Is.EqualTo(before.x).Within(.01f));
                Assert.That((Vector2)Call(space,"Position",child),Is.EqualTo(logic));Assert.That(Get(space,"Width"),Is.EqualTo(width));
                var owner=new GameObject("Owned projection loot");objects.Add(owner);
                var loot=owner.AddComponent(Runtime("LootDropPresentation"));
                Call(loot,"Show",area,new Vector2(123,42),"sample:iron_sword","owned-visual",null);
                var root=(RectTransform)((System.Collections.IList)Get(loot,"Roots"))[0];
                Call(loot,"Advance",1.1f,null);before=RectTransformUtility.WorldToScreenPoint(null,root.position);
                Set(space,"battleVerticalOffsetPixels",40f);Call(space,"SyncBattleProjection");Canvas.ForceUpdateCanvases();Call(loot,"Advance",0f,null);
                after=RectTransformUtility.WorldToScreenPoint(null,root.position);
                Assert.That(Vector2.Distance(before,after),Is.LessThan(.01f),"Flying loot is independent of all battle view offsets.");
            }
            finally{for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UnityEngine.Object.DestroyImmediate(objects[i]);}
        }
    }
}
