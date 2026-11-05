using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // No SaveManager, scene loads, file I/O or gameplay ticks. Data replacements
    // below model the view's binding boundary, not an end-to-end Save/Load test.
    public sealed class InventoryMapContextTests
    {
        const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static Type R(string name)
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()) {var t=a.GetType(name,false);if(t!=null)return t;}
            throw new Exception(name);
        }
        static object G(object o,string name)
        {
            var f=o.GetType().GetField(name,All);return f!=null?f.GetValue(o):o.GetType().GetProperty(name,All).GetValue(o);
        }
        static void S(object o,string name,object value)=>o.GetType().GetField(name,All).SetValue(o,value);
        static object C(object o,string name,params object[] args)
        {
            var type=o as Type ?? o.GetType();
            return type.GetMethod(name,All).Invoke(o is Type?null:o,args);
        }
        sealed class Fixture:IDisposable
        {
            readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
            readonly float originalTime=Time.timeScale;
            internal Component UI,Game;
            internal RectTransform Root;
            internal ScriptableObject Hero,Theme;
            internal object Panel,Map;
            internal Sprite Legacy,First,Second,Default;
            internal Fixture(bool inventory=false,bool worldMap=false)
            {
                foreach(string name in new[]{"SaveManager","GameManager","UIManager","CombatManager","EnvironmentManager"})
                    Assert.That(UnityEngine.Object.FindObjectsByType(R(name),FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.Zero,"requires empty test scene: "+name);
                Time.timeScale=1;
                var canvas=Own(new GameObject("Map context fixture",typeof(RectTransform),typeof(Canvas)));
                canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;Root=(RectTransform)canvas.transform;
                var ui=Own(new GameObject("UI fixture"));ui.SetActive(false);UI=ui.AddComponent(R("UIManager"));
                var gm=Own(new GameObject("Disabled manager, no save owner"));gm.SetActive(false);
                Game=gm.AddComponent(R("GameManager"));((Behaviour)Game).enabled=false;gm.SetActive(true);
                Hero=Data("TV-20",4);S(Game,"runtimeHeroData",Hero);
                S(UI,"hudGame",Game);S(UI,"gameResolved",true);
                Theme=Own(ScriptableObject.CreateInstance(R("InventoryArtTheme")));
                var texture=Own(new Texture2D(8,8));
                Legacy=Sprite(texture);First=Sprite(texture);Second=Sprite(texture);Default=Sprite(texture);
                S(Theme,"defaultMapSplash",Default);S(UI,"inventoryArtTheme",Theme);S(UI,"inventoryUseArtTheme",true);
                // The two modern entries intentionally retain the default legacy
                // number 1. Only an explicit ID may select them for modern saves.
                Entry("TV-20",1,First);Entry("XD-11",1,Second);Entry("",1,Legacy);
                if(inventory)
                {
                    Panel=C(R("InventoryPanelUI"),"Install",UI,Root);
                    S(UI,"inventoryUI",Panel);
                }
                if(worldMap)
                {
                    Map=C(R("WorldMapPanelUI"),"Install",UI,Root);Assert.That(Map,Is.Not.Null);
                    Own((UnityEngine.Object)G(Map,"fogTexture"));S(UI,"worldMapUI",Map);
                    var button=C(UI,"SmallButton",Root,"WorldMapButton","Bản đồ",new Vector2(0,1),new Vector2(0,1),new Vector2(100,-100),new Vector2(78,28),new UnityEngine.Events.UnityAction(()=>C(UI,"ToggleWorldMapMenu")));
                    S(UI,"worldMapButton",button);S(Game,"hasDeployed",true);
                }
            }
            T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
            Sprite Sprite(Texture2D texture)=>Own(UnityEngine.Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f));
            internal ScriptableObject Data(string id,int wave)
            {
                var hero=Own(ScriptableObject.CreateInstance(R("EntityDataSO")));
                C(hero,"ApplyHeroBalance");C(R("EquipmentInventory"),"InitializeNewRun",hero);C(R("EquipmentRuntime"),"InitializeNewRun",hero);
                S(hero,"worldMapVersion",1);S(hero,"worldMapId",id);S(hero,"worldMapLoopWave",wave);
                S(hero,"mapNumber",1);S(hero,"mapName","WRONG LEGACY NAME");S(hero,"completedWavesInMap",0);
                return hero;
            }
            internal void Entry(string id,int number,Sprite sprite)
            {
                var entry=Activator.CreateInstance(R("InventoryMapArt"));S(entry,"worldMapId",id);S(entry,"mapNumber",number);S(entry,"mapSplash",sprite);
                ((IList)G(Theme,"mapArts")).Add(entry);
            }
            internal void Refresh()=>C(Panel,"Refresh");
            internal object SelectedArt()=>C(Theme,"FindMapForHero",Hero);
            internal object Image()=>((RectTransform)G(Panel,"mapArtwork")).GetComponent(R("UnityEngine.UI.Image"));
            public void Dispose()
            {
                for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UnityEngine.Object.DestroyImmediate(owned[i]);
                Time.timeScale=originalTime;
            }
        }
        [Test]
        public void WorldMapArtworkUsesIdAndDoesNotMutateHero()
        {
            using(var f=new Fixture(true))
            {
                string before=JsonUtility.ToJson(f.Hero);f.Refresh();
                Assert.That(G(f.SelectedArt(),"mapSplash"),Is.SameAs(f.First));
                Assert.That(G(f.Image(),"sprite"),Is.SameAs(f.First));
                Assert.That(JsonUtility.ToJson(f.Hero),Is.EqualTo(before));
            }
        }
        [TestCase(null)] [TestCase("")] [TestCase("UNKNOWN")]
        public void MissingWorldIdUsesDefaultArtInsteadOfLegacyNumber(string id)
        {
            using(var f=new Fixture(true))
            {
                S(f.Hero,"worldMapId",id);string before=JsonUtility.ToJson(f.Hero);f.Refresh();
                Assert.That(f.SelectedArt(),Is.Null);
                Assert.That(G(f.Image(),"sprite"),Is.SameAs(f.Default));
                Assert.That(JsonUtility.ToJson(f.Hero),Is.EqualTo(before));
                S(f.UI,"inventoryUseArtTheme",false);f.Refresh();
                Assert.That(G(f.Image(),"sprite"),Is.Null,"theme off keeps color fallback instead of stale art");
            }
        }
        [Test]
        public void LegacyThemeEntriesRemainCompatibleAndIgnoreWorldEntries()
        {
            using(var f=new Fixture(true))
            {
                S(f.Hero,"worldMapVersion",0);f.Refresh();
                Assert.That(G(C(f.Theme,"FindMap",1),"mapSplash"),Is.SameAs(f.Legacy));
                Assert.That(G(f.Image(),"sprite"),Is.SameAs(f.Legacy));
                S(f.Hero,"mapNumber",7);f.Refresh();
                Assert.That(f.SelectedArt(),Is.Null);Assert.That(G(f.Image(),"sprite"),Is.SameAs(f.Default));
            }
        }
        [Test]
        public void InventoryBannerAndArtRebindToCurrentWorldSnapshot()
        {
            using(var f=new Fixture(true))
            {
                var root=(RectTransform)G(f.Panel,"root");root.gameObject.SetActive(true);
                var geometry=R("ManagementWindowLayout");
                var names=new[]{"CompactWidth","CompactHeight","Expanded","CollapsePending","CompactResizePending","TargetWidth","TargetHeight"};
                var prior=new Dictionary<string,object>();foreach(var n in names)prior[n]=GStatic(geometry,n);
                Assert.That(GStatic(geometry,"Expanded"),Is.False,"test must not inherit an expanded window");
                try
                {
                    C(geometry,"SetCompact",800,450);C(geometry,"Expand",1360,680);
                    C(f.Panel,"Layout",800f,450f,1360f,680f);
                    AssertBanner(f,"TV-20",4);
                    f.Hero=f.Data("XD-11",2);S(f.Game,"runtimeHeroData",f.Hero);f.Refresh();
                    AssertBanner(f,"XD-11",2);Assert.That(G(f.Image(),"sprite"),Is.SameAs(f.Second));
                }
                finally {foreach(var n in names)geometry.GetField("<"+n+">k__BackingField",All).SetValue(null,prior[n]);}
            }
        }
        static object GStatic(Type type,string name)=>type.GetProperty(name,All).GetValue(null);
        static void AssertBanner(Fixture f,string id,int wave)
        {
            object[] args={id,null};R("TuTienCore.WorldMapCatalog").GetMethod("TryGet").Invoke(null,args);
            var label=G(f.Panel,"bannerText");string text=(string)G(label,"text");
            Assert.That(text,Does.Contain((string)G(args[1],"Name")));
            Assert.That(text,Does.Contain("Wave "+wave+"/5"));Assert.That(text,Does.Not.Contain("WRONG LEGACY NAME"));
            C(label,"ForceMeshUpdate",true,false);
            Assert.That(G(label,"isTextOverflowing"),Is.False,"map/wave banner must fit its allocated region");
        }
        [Test]
        public void MapControlDisablesOnLegacyAndReenablesAfterModernRebind()
        {
            using(var f=new Fixture(worldMap:true))
            {
                var button=(RectTransform)G(f.UI,"worldMapButton");var control=(Behaviour)button.GetComponent(R("CustomInteractable"));
                S(f.Hero,"worldMapVersion",0);C(f.UI,"TickHudW3");Canvas.ForceUpdateCanvases();
                var point=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
                Assert.That(control.enabled,Is.False);Assert.That(C(f.UI,"HitControl",point),Is.Null);
                Assert.That(C(f.UI,"CheckInteractableHover",point),Is.True,"disabled UI still blocks desktop click-through");
                C(f.UI,"HandlePointerClick",point,point);Assert.That(G(f.Map,"IsOpen"),Is.False);
                // Theme refresh must not make an unavailable action look enabled.
                S(f.Theme,"actionButton",f.First);C(f.UI,"ApplyHudFromInspector");C(f.UI,"TickHudW3");
                var caption=button.GetComponentInChildren(R("TMPro.TextMeshProUGUI"));
                Assert.That(((Color)G(caption,"color")).r,Is.LessThan(.6f));
                f.Hero=f.Data("TV-20",1);S(f.Game,"runtimeHeroData",f.Hero);C(f.UI,"TickHudW3");
                Assert.That(control.enabled,Is.True);Assert.That(C(f.UI,"HitControl",point),Is.SameAs(control));
                C(f.UI,"HandlePointerClick",point,point);Assert.That(G(f.Map,"IsOpen"),Is.True,"real world-map handler opens after rebind");
                C(f.Map,"Close");Assert.That(Time.timeScale,Is.EqualTo(1));
            }
        }
    }
}
