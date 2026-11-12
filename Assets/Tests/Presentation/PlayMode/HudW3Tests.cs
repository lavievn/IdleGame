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
                S(G(Combat,"CurrentHeroCombatState"),"Controller",Actor());
                C(G(Combat,"CurrentHeroCombatState"),"BeginLife",Primary,200,false);
            }
            internal T Own<T>(T o) where T:UnityEngine.Object {owned.Add(o);return o;}
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
                var actor=Actor();
                var state=Activator.CreateInstance(R("HeroCombatState"));S(state,"Controller",actor);
                var data=Data(name,3);C(state,"BeginLife",data,100,false);S(state,"CurrentHP",hp);
                ((IDictionary)G(Combat,"heroStates")).Add(actor,state);
                var record=Activator.CreateInstance(R("GameManager").GetNestedType("TestCompanion",All));
                S(record,"Controller",actor);S(record,"Data",data);
                ((IList)G(Game,"testCompanions")).Add(record);
                return state;
            }
            Component Actor()
            {
                var actor=Manager("HeroController");
                // Synchronous view test: Awake runs with no body, then supply
                // a synthetic deployed body. No Spawn/VFX/Update or manager Start.
                actor.gameObject.SetActive(true);
                var body=Own(new GameObject("HUD actor body",typeof(RectTransform)));
                body.transform.SetParent(actor.transform,false);
                S(actor,"heroRect",(RectTransform)body.transform);S(actor,"deployed",true);
                return actor;
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
                Assert.That(f.Card(0).gameObject.activeSelf,Is.True);
                Assert.That(f.Card(1).gameObject.activeSelf,Is.False);
                Assert.That(f.Card(2).gameObject.activeSelf,Is.False);
                f.Companion("Thứ hai",100);f.Render();
                Assert.That(f.Card(1).gameObject.activeSelf,Is.True);
                Assert.That(f.Card(2).gameObject.activeSelf,Is.False);
                f.Companion("Thứ ba",100);f.Render();
                float previous=width;
                for(int i=0;i<3;i++)
                {
                    var card=f.Card(i);float left=card.anchoredPosition.x,right=left+card.sizeDelta.x;
                    Assert.That(card.gameObject.activeSelf,Is.True);
                    Assert.That(left,Is.GreaterThanOrEqualTo(0));Assert.That(right,Is.LessThanOrEqualTo(previous));
                    Assert.That(card.sizeDelta.y,Is.EqualTo(i==0?110f:76f));
                    Assert.That(-card.anchoredPosition.y+card.sizeDelta.y,Is.LessThan(height));previous=left-3;
                    var portrait=card.Find("Avatar Frame/Portrait").GetComponent(R("UnityEngine.UI.Image"));
                    Assert.That(G(portrait,"preserveAspect"),Is.True);
                    if(i>0)Assert.That(f.Text(card,"Mana Track/Mana Value"),Is.EqualTo("Mana —"));
                    Canvas.ForceUpdateCanvases();
                    foreach(string path in new[]{"HP Track/HP Value","Mana Track/Mana Value"})
                    {
                        var label=card.Find(path).GetComponent(R("TMPro.TextMeshProUGUI"));C(label,"ForceMeshUpdate",false,false);
                        var info=G(label,"textInfo");var characters=(Array)G(info,"characterInfo");int visible=0;
                        for(int c=0;c<(int)G(info,"characterCount");c++)if((bool)G(characters.GetValue(c),"isVisible"))visible++;
                        Assert.That(visible,Is.GreaterThan(0),"the HP/Mana line must actually render: "+path);
                    }
                }
                Assert.That(f.Text(f.Card(1),"Header/Hero Name"),Is.EqualTo("Thứ hai"));
                Assert.That(((IDictionary)G(f.Combat,"heroStates")).Count,Is.EqualTo(2),"only fixture-deployed companions are registered");
            }
        }
        [Test]
        public void EachRealSlotUsesItsOwnHPAndRebindsWithoutStaleCompanions()
        {
            using(var f=new Fixture())
            {
                C(f.View,"Layout",600f,338f,f.Layout);
                var a=f.Companion("Đồng đội tên rất dài kiểm tra tràn chữ",37);var b=f.Companion("B2 thứ hai",80);
                f.Render();Assert.That(f.Text(f.Card(0),"HP Track/HP Value"),Is.EqualTo("HP 200/200"));
                Assert.That(f.Text(f.Card(1),"HP Track/HP Value"),Is.EqualTo("HP 37/100"));
                Assert.That(f.Text(f.Card(2),"HP Track/HP Value"),Is.EqualTo("HP 80/100"));
                S(a,"CurrentHP",0);S(b,"CurrentHP",61);f.Render();
                Assert.That(f.Card(1).gameObject.activeSelf,Is.False);
                Assert.That(f.Card(2).anchoredPosition.x,Is.EqualTo(f.Card(0).anchoredPosition.x-f.Card(2).sizeDelta.x-4).Within(.01));
                Assert.That(f.Text(f.Card(2),"HP Track/HP Value"),Is.EqualTo("HP 61/100"));
                S(a,"CurrentHP",100);f.Render();
                Assert.That(f.Card(1).gameObject.activeSelf,Is.True);
                Assert.That(f.Card(2).anchoredPosition.x,Is.LessThan(f.Card(1).anchoredPosition.x));
                ((IList)G(f.Game,"testCompanions")).Clear();
                C(G(f.Combat,"CurrentHeroCombatState"),"BeginLife",f.Data("Sau Load",12),300,false);f.Render();
                Assert.That(f.Text(f.Card(0),"Header/Hero Name"),Is.EqualTo("Sau Load"));
                Assert.That(f.Text(f.Card(0),"HP Track/HP Value"),Is.EqualTo("HP 300/300"));
                Assert.That(f.Card(1).gameObject.activeSelf,Is.False);
                Assert.That(f.Card(2).gameObject.activeSelf,Is.False);
                S(G(f.Combat,"CurrentHeroCombatState"),"CurrentHP",0);f.Render();
                Assert.That(f.Card(0).gameObject.activeSelf,Is.False);
                Assert.That(G(f.View,"VisibleCardHeight"),Is.EqualTo(0f),"no empty HUD height after the final actor leaves");
                C(G(f.Combat,"CurrentHeroCombatState"),"BeginLife",f.Primary,200,false);f.Render();
                Assert.That(f.Card(0).gameObject.activeSelf,Is.True);
                Assert.That(G(f.View,"VisibleCardHeight"),Is.EqualTo(110f));
            }
        }
        [Test]
        public void StoryDialogueSurvivesTemporaryMenusButClosesAtGameplayBoundary()
        {
            using(var f=new Fixture())
            {
                R("TuTienCore.StoryOrigin").GetMethod("InitializeNewHero",All).Invoke(null,new object[]{f.Primary});
                S(f.Primary,"worldMapId","TV-20");S(f.Game,"runtimeHeroData",f.Primary);S(f.Game,"hasDeployed",true);
                S(f.UI,"w3",f.View);S(f.UI,"hudGame",f.Game);S(f.UI,"gameResolved",true);
                C(f.UI,"InstallOriginPresentation");
                C(f.UI,"BeginOriginConversation",f.Primary,Enum.Parse(R("TuTienCore.HanVuConversation"),"Opening"));
                ((RectTransform)G(f.View,"Root")).gameObject.SetActive(false);
                C(f.UI,"TickOriginPresentation");
                Assert.That(G(f.UI,"dialogue"),Is.Not.Null,"a temporary menu can resume the same line");
                S(f.Game,"enteringGameplay",true);C(f.UI,"TickOriginPresentation");
                Assert.That(G(f.UI,"dialogue"),Is.Null,"entry/Retry cannot resume stale dialogue");
                Assert.That(G(G(f.Primary,"originStory"),"heardOpeningTrip"),Is.False);
                Assert.That(G(G(f.Primary,"originStory"),"herbalParticipation").ToString(),Is.EqualTo("Offered"));
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
                Assert.That(f.Text(root,"WorldMapButton/Vertical Map Name"),Is.EqualTo(expected));
                Assert.That(f.Text(root,"WorldMapButton/Current Wave"),Is.EqualTo("Wave 4/5"));
                Assert.That(f.Text(root,"WorldMapButton/Current Wave"),Does.Not.Contain("Địa hình"));
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
        [TestCase("system",false)] [TestCase("system",true)]
        [TestCase("hero",false)] [TestCase("hero",true)]
        [TestCase("log",false)] [TestCase("log",true)]
        [TestCase("damage",false)] [TestCase("damage",true)]
        public void GameplayBoundaryDismissesNonPausingModalsAndUnblocksRetry(string modal,bool paused)
        {
            float before=Time.timeScale;
            try
            {
                using(var f=new Fixture())
                {
                    S(f.UI,"hudGame",f.Game);S(f.UI,"gameResolved",true);S(f.Game,"hasDeployed",true);
                    var menu=f.Own(new GameObject("System menu boundary fixture",typeof(RectTransform)));
                    menu.transform.SetParent(f.Root,false);menu.SetActive(false);S(f.UI,"systemMenu",menu);
                    var details=f.Own(new GameObject("Detail boundary fixture",typeof(RectTransform)));
                    details.transform.SetParent(f.Root,false);details.SetActive(false);S(f.UI,"damagePanel",details);
                    var label=C(f.UI,"MakeLabel",details.transform,"",Vector2.zero,new Vector2(300,200));S(f.UI,"damageText",label);
                    var over=f.Own(new GameObject("Game over boundary fixture",typeof(RectTransform)));
                    var overRect=(RectTransform)over.transform;overRect.SetParent(f.Root,false);
                    overRect.anchorMin=Vector2.zero;overRect.anchorMax=Vector2.one;overRect.offsetMin=overRect.offsetMax=Vector2.zero;
                    over.SetActive(false);S(f.Game,"gameOverPanel",over);
                    int calls=0;
                    var retry=(RectTransform)C(f.UI,"SmallButton",overRect,"Retry","Retry",new Vector2(0,1),new Vector2(0,1),new Vector2(100,-100),new Vector2(100,30),new UnityEngine.Events.UnityAction(()=>calls++));
                    C(f.UI,"SetPaused",paused);
                    if(modal=="system")menu.SetActive(true);
                    else if(modal=="damage")C(f.UI,"OpenDamageInfo");
                    else C(f.UI,"OpenTextDetail",modal=="hero"?1:2,"old live-game snapshot");
                    C(f.UI,"RefreshCombatHUDVisibility");
                    Assert.That(modal=="system"?menu.activeSelf:details.activeSelf,Is.True,"normal gameplay keeps the chosen panel open");
                    // Equivalent UI boundary to OnHeroDied, without combat/save I/O.
                    S(f.Game,"hasDeployed",false);over.SetActive(true);over.transform.SetAsLastSibling();
                    C(f.UI,"RefreshCombatHUDVisibility");Canvas.ForceUpdateCanvases();
                    Assert.That(menu.activeSelf,Is.False);Assert.That(details.activeSelf,Is.False);
                    Assert.That(Time.timeScale,Is.EqualTo(paused?0f:1f),"dismissing a non-pausing panel cannot change Pause");
                    var point=RectTransformUtility.WorldToScreenPoint(null,retry.TransformPoint(retry.rect.center));
                    Assert.That(C(f.UI,"HitControl",point),Is.SameAs(retry.GetComponent(R("CustomInteractable"))));
                    C(f.UI,"HandlePointerClick",point,point);Assert.That(calls,Is.EqualTo(1),"visible Retry receives exactly one callback");
                }
            }
            finally {Time.timeScale=before;}
        }
        [Test]
        public void EntryDismissesOldDetailSnapshotWithoutReleasingPause()
        {
            float before=Time.timeScale;
            try
            {
                using(var f=new Fixture())
                {
                    S(f.UI,"hudGame",f.Game);S(f.UI,"gameResolved",true);S(f.Game,"hasDeployed",true);
                    var details=f.Own(new GameObject("Entry detail fixture",typeof(RectTransform)));
                    details.transform.SetParent(f.Root,false);S(f.UI,"damagePanel",details);
                    var label=C(f.UI,"MakeLabel",details.transform,"",Vector2.zero,new Vector2(300,200));S(f.UI,"damageText",label);
                    C(f.UI,"OpenTextDetail",1,"old snapshot");C(f.UI,"SetPaused",true);
                    S(f.Game,"enteringGameplay",true);C(f.UI,"RefreshCombatHUDVisibility");
                    Assert.That(details.activeSelf,Is.False);Assert.That(Time.timeScale,Is.EqualTo(0));
                    S(f.Game,"enteringGameplay",false);C(f.UI,"OpenTextDetail",1,"new snapshot");
                    Assert.That(details.activeSelf,Is.True);Assert.That((string)G(label,"text"),Does.Contain("new snapshot"));
                    Assert.That((string)G(label,"text"),Does.Not.Contain("old snapshot"));
                }
            }
            finally {Time.timeScale=before;}
        }
        [TestCase(false)] [TestCase(true)]
        public void PausedB2ControlExplainsItsStateAndRetainsCallbackAfterResume(bool fullParty)
        {
            float before=Time.timeScale;
            try
            {
                using(var f=new Fixture())
                {
                    S(f.UI,"hudGame",f.Game);S(f.UI,"gameResolved",true);S(f.Game,"hasDeployed",true);
                    if(fullParty){f.Companion("B2 one",100);f.Companion("B2 two",100);}
                    var menu=f.Own(new GameObject("B2 tools fixture",typeof(RectTransform)));
                    var menuRect=(RectTransform)menu.transform;menuRect.SetParent(f.Root,false);
                    menuRect.anchorMin=Vector2.zero;menuRect.anchorMax=Vector2.one;menuRect.offsetMin=menuRect.offsetMax=Vector2.zero;
                    S(f.UI,"systemMenu",menu);C(f.UI,"BindClick",menuRect,new UnityEngine.Events.UnityAction(()=>{}));
                    int calls=0;
                    var button=(RectTransform)C(f.UI,"SmallButton",menuRect,"B2TestSummon","B2",new Vector2(0,1),new Vector2(0,1),new Vector2(100,-100),new Vector2(240,30),new UnityEngine.Events.UnityAction(()=>calls++));
                    var label=button.GetComponentInChildren(R("TMPro.TextMeshProUGUI"));
                    S(f.UI,"testSummonButton",button);S(f.UI,"testSummonText",label);
                    var control=(Behaviour)button.GetComponent(R("CustomInteractable"));
                    int count=(int)G(f.Game,"TestCompanionCount");string data=JsonUtility.ToJson(f.Primary);
                    C(f.UI,"SetPaused",true);C(f.UI,"TickHudW3");Canvas.ForceUpdateCanvases();
                    Assert.That(control.enabled,Is.False);Assert.That((string)G(label,"text"),Does.Contain("Tiếp tục"));
                    Assert.That(((Color)G(label,"color")).r,Is.LessThan(.6f));
                    var point=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
                    Assert.That(C(f.UI,"CheckInteractableHover",point),Is.True,"parent menu still blocks click-through");
                    C(f.UI,"HandlePointerClick",point,point);Assert.That(calls,Is.Zero);
                    C(f.UI,"ApplyHudFromInspector");C(f.UI,"TickHudW3");
                    Assert.That(control.enabled,Is.False);Assert.That(((Color)G(label,"color")).r,Is.LessThan(.6f));
                    C(f.UI,"SetPaused",false);C(f.UI,"TickHudW3");
                    Assert.That(control.enabled,Is.True);
                    Assert.That((string)G(label,"text"),Does.Contain(fullParty?"Thu hồi":"Gọi +1"));
                    Assert.That(((Color)G(label,"color")).r,Is.GreaterThan(.8f));
                    C(f.UI,"HandlePointerClick",point,point);Assert.That(calls,Is.EqualTo(1),"original callback remains bound exactly once");
                    Assert.That((int)G(f.Game,"TestCompanionCount"),Is.EqualTo(count));
                    Assert.That(JsonUtility.ToJson(f.Primary),Is.EqualTo(data));
                }
            }
            finally {Time.timeScale=before;}
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
