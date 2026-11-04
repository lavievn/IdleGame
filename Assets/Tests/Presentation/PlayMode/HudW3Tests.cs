using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Own inactive managers, synthetic combat data; no SaveManager or save I/O.
    public sealed class HudW3Tests
    {
        const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static Type R(string n)
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()) {var t=a.GetType(n,false);if(t!=null)return t;}
            throw new Exception(n);
        }
        static object G(object o,string n)
        {
            var f=o.GetType().GetField(n,All);return f!=null?f.GetValue(o):o.GetType().GetProperty(n,All).GetValue(o);
        }
        static void S(object o,string n,object v)
        {
            var f=o.GetType().GetField(n,All);if(f!=null)f.SetValue(o,v);else o.GetType().GetProperty(n,All).SetValue(o,v);
        }
        static object C(object o,string n,params object[] args)=>o.GetType().GetMethod(n,All).Invoke(o,args);
        sealed class Fixture:IDisposable
        {
            readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
            internal Component UI,Game,Combat;
            internal object View,Layout;
            internal RectTransform Root;
            internal ScriptableObject Primary;
            internal Fixture()
            {
                foreach(string n in new[]{"SaveManager","GameManager","CombatManager","UIManager","HeroController"})
                    Assert.That(UnityEngine.Object.FindObjectsByType(R(n),FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.Zero,"requires an empty test scene: "+n);
                var canvas=Own(new GameObject("HUD fixture",typeof(RectTransform),typeof(Canvas)));
                canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                Root=(RectTransform)canvas.transform;
                UI=Manager("UIManager");Game=Manager("GameManager");Combat=Manager("CombatManager");
                Layout=Activator.CreateInstance(R("HudW3Layout"));
                View=Activator.CreateInstance(R("HudW3View"),All,null,new object[]{UI,Root},null);
                Primary=Data("Chính",8);
                C(G(Combat,"CurrentHeroCombatState"),"BeginLife",Primary,200);
            }
            T Own<T>(T o) where T:UnityEngine.Object {owned.Add(o);return o;}
            Component Manager(string n)
            {
                var go=Own(new GameObject(n));go.SetActive(false);return go.AddComponent(R(n));
            }
            internal ScriptableObject Data(string name,int level)
            {
                var d=Own(ScriptableObject.CreateInstance(R("EntityDataSO")));S(d,"entityName",name);S(d,"currentLevel",level);return d;
            }
            internal object Companion(string name,int hp)
            {
                var actor=Manager("HeroController");
                var state=Activator.CreateInstance(R("HeroCombatState"));S(state,"Controller",actor);
                var data=Data(name,3);C(state,"BeginLife",data,100);S(state,"CurrentHP",hp);
                ((IDictionary)G(Combat,"heroStates")).Add(actor,state);
                var record=Activator.CreateInstance(R("GameManager").GetNestedType("TestCompanion",All));
                S(record,"Controller",actor);S(record,"Data",data);
                ((IList)G(Game,"testCompanions")).Add(record);
                return state;
            }
            internal void Render()=>C(View,"Render",Game,Combat,null);
            internal RectTransform Card(int i)=>(RectTransform)((RectTransform)G(View,"Root")).Find("Hero Card "+i);
            internal string Text(Transform root,string path)=>(string)G(root.Find(path).GetComponent(R("TMPro.TextMeshProUGUI")),"text");
            public void Dispose() {for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UnityEngine.Object.DestroyImmediate(owned[i]);}
        }

        [TestCase(600f,338f)] [TestCase(800f,450f)] [TestCase(1150f,647f)]
        public void CardsFitCompactViewportAndKeepAvatarAspect(float width,float height)
        {
            using(var f=new Fixture())
            {
                C(f.View,"Layout",width,height,f.Layout);f.Render();
                float previous=0;
                for(int i=0;i<3;i++)
                {
                    var card=f.Card(i);float left=card.anchoredPosition.x,right=left+card.sizeDelta.x;
                    Assert.That(left,Is.GreaterThanOrEqualTo(previous));Assert.That(right,Is.LessThanOrEqualTo(width-7));
                    Assert.That(-card.anchoredPosition.y+card.sizeDelta.y,Is.LessThan(100));previous=right+3;
                    var portrait=card.Find("Avatar Frame/Portrait").GetComponent(R("UnityEngine.UI.Image"));
                    Assert.That(G(portrait,"preserveAspect"),Is.True);
                    Assert.That(f.Text(card,"Mana Disabled/Mana Value"),Is.EqualTo("Mana —"));
                    Canvas.ForceUpdateCanvases();
                    foreach(string path in new[]{"HP Track/HP Value","Mana Disabled/Mana Value"})
                    {
                        var label=card.Find(path).GetComponent(R("TMPro.TextMeshProUGUI"));C(label,"ForceMeshUpdate",false,false);
                        var info=G(label,"textInfo");var characters=(Array)G(info,"characterInfo");int visible=0;
                        for(int c=0;c<(int)G(info,"characterCount");c++)if((bool)G(characters.GetValue(c),"isVisible"))visible++;
                        Assert.That(visible,Is.GreaterThan(0),"the HP/Mana line must actually render: "+path);
                    }
                }
                Assert.That(f.Text(f.Card(1),"Header/Hero Name"),Is.EqualTo("Chưa có Hero"));
                Assert.That(((IDictionary)G(f.Combat,"heroStates")).Count,Is.Zero,"view cannot recruit/register anyone");
            }
        }
        [Test]
        public void EachRealSlotUsesItsOwnHPAndRebindsWithoutStaleCompanions()
        {
            using(var f=new Fixture())
            {
                var a=f.Companion("Đồng đội tên rất dài kiểm tra tràn chữ",37);var b=f.Companion("B2 thứ hai",80);
                f.Render();Assert.That(f.Text(f.Card(0),"HP Track/HP Value"),Is.EqualTo("HP 200/200"));
                Assert.That(f.Text(f.Card(1),"HP Track/HP Value"),Is.EqualTo("HP 37/100"));
                Assert.That(f.Text(f.Card(2),"HP Track/HP Value"),Is.EqualTo("HP 80/100"));
                S(a,"CurrentHP",0);S(b,"CurrentHP",61);f.Render();
                Assert.That(f.Text(f.Card(1),"Level"),Does.Contain("Đã gục"));
                Assert.That(f.Text(f.Card(2),"HP Track/HP Value"),Is.EqualTo("HP 61/100"));
                ((IList)G(f.Game,"testCompanions")).Clear();
                C(G(f.Combat,"CurrentHeroCombatState"),"BeginLife",f.Data("Sau Load",12),300);f.Render();
                Assert.That(f.Text(f.Card(0),"Header/Hero Name"),Is.EqualTo("Sau Load"));
                Assert.That(f.Text(f.Card(0),"HP Track/HP Value"),Is.EqualTo("HP 300/300"));
                Assert.That(f.Text(f.Card(1),"Level"),Is.EqualTo("Ô trống"));
            }
        }
        [Test]
        public void WorldMapNameAndWaveIgnoreConflictingLegacyNumber()
        {
            using(var f=new Fixture())
            {
                var d=f.Primary;S(f.Game,"runtimeHeroData",d);S(d,"worldMapVersion",1);S(d,"worldMapId","TV-20");
                S(d,"mapNumber",999);S(d,"mapName","Sai legacy");S(d,"completedWavesInMap",0);S(d,"worldMapLoopWave",4);
                f.Render();var root=(RectTransform)G(f.View,"Root");
                object[] args={"TV-20",null};R("TuTienCore.WorldMapCatalog").GetMethod("TryGet").Invoke(null,args);
                var expected=((string)G(args[1],"Name")).Replace(' ','\n');
                Assert.That(f.Text(root,"Map Information/Vertical Map Name"),Is.EqualTo(expected));
                Assert.That(f.Text(root,"Map Information/Current Wave"),Is.EqualTo("Wave 4/5"));
                Assert.That(f.Text(root,"Map Information/Current Wave"),Does.Not.Contain("Địa hình"));
            }
        }
        [Test]
        public void MissingArtFallsBackAndOnlyBorderedSpritesUseNineSlice()
        {
            using(var f=new Fixture())
            {
                var image=f.Card(0).GetComponent(R("UnityEngine.UI.Image"));
                var fallback=new Color(.1f,.2f,.3f,1);
                var texture=new Texture2D(16,16);
                var plain=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f);
                var bordered=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(2,2,2,2));
                try
                {
                    var apply=R("UIManager").GetMethod("ApplySurfaceArt",All);
                    apply.Invoke(null,new object[]{image,plain,fallback,true});Assert.That(G(image,"type").ToString(),Is.EqualTo("Simple"));
                    apply.Invoke(null,new object[]{image,bordered,fallback,true});Assert.That(G(image,"type").ToString(),Is.EqualTo("Sliced"));
                    apply.Invoke(null,new object[]{image,null,fallback,true});Assert.That(G(image,"color"),Is.EqualTo(fallback));
                    Assert.That(G(image,"raycastTarget"),Is.False);
                }
                finally {UnityEngine.Object.DestroyImmediate(plain);UnityEngine.Object.DestroyImmediate(bordered);UnityEngine.Object.DestroyImmediate(texture);}
            }
        }
        [Test]
        public void ButtonCaptionFollowsResizedHitboxWithoutChangingItsCallback()
        {
            using(var f=new Fixture())
            {
                int calls=0;
                var button=(RectTransform)C(f.UI,"SmallButton",f.Root,"Resize Button","Bản đồ",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(60,36),new UnityEngine.Events.UnityAction(()=>calls++));
                button.sizeDelta=new Vector2(140,30);Canvas.ForceUpdateCanvases();
                var label=(RectTransform)button.GetChild(0);
                Assert.That(label.rect.width,Is.EqualTo(128).Within(.01));
                Assert.That(label.rect.height,Is.EqualTo(22).Within(.01));
                var control=button.GetComponent(R("CustomInteractable"));
                ((UnityEngine.Events.UnityEvent)G(control,"onClickEvent")).Invoke();Assert.That(calls,Is.EqualTo(1));
            }
        }
        [Test]
        public void DecorativeFrameCannotStealASeparateButtonHit()
        {
            using(var f=new Fixture())
            {
                var frame=(RectTransform)C(f.UI,"Surface",f.Root,"Decoration",Enum.Parse(R("UIManager+SurfaceKind"),"Frame"));
                frame.anchorMin=Vector2.zero;frame.anchorMax=Vector2.one;frame.offsetMin=frame.offsetMax=Vector2.zero;
                int clicks=0;
                var button=(RectTransform)C(f.UI,"SmallButton",f.Root,"Close over decoration","Đóng",new Vector2(0,1),new Vector2(0,1),new Vector2(100,-100),new Vector2(100,30),new UnityEngine.Events.UnityAction(()=>clicks++));
                Canvas.ForceUpdateCanvases();var point=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
                Assert.That(C(f.UI,"HitControl",point),Is.SameAs(button.GetComponent(R("CustomInteractable"))));
                C(f.UI,"HandlePointerClick",point,point);Assert.That(clicks,Is.EqualTo(1));
            }
        }
        [Test]
        public void LootTargetTracksRectAndUsesRealWindowEdgeWhenHidden()
        {
            using(var f=new Fixture())
            {
                var owner=new GameObject("HUD loot fixture");
                try
                {
                    var loot=owner.AddComponent(R("LootDropPresentation"));var target=f.Card(0);
                    C(loot,"SetInventoryTarget",target);
                    Assert.That((Vector2)C(loot,"CollectionTarget"),Is.EqualTo(RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center))));
                    target.anchoredPosition+=new Vector2(-70,-20);
                    Assert.That((Vector2)C(loot,"CollectionTarget"),Is.EqualTo(RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center))));
                    target.gameObject.SetActive(false);
                    Assert.That((Vector2)C(loot,"CollectionTarget"),Is.EqualTo(new Vector2(Screen.width+16,Screen.height*.35f)));
                }
                finally {UnityEngine.Object.DestroyImmediate(owner);}
            }
        }
    }
}
