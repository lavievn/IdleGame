using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

partial class MotionRegression
{
    static void EntryTitle(DesktopHotfixFixture f,bool fresh)
    {
        Check(f.manager.ReturnToMainMenu(),"return to title");
        if(fresh)f.manager.OnNewGameClicked();else f.manager.OnContinueClicked();
        f.manager.OnConfirmResetClicked();
    }
    static void EntryTick(DesktopHotfixFixture f,float dt)
    {
        Time.unscaledDeltaTime=dt;Call(f.manager,"Update");
    }
    static void EntryReveal(DesktopHotfixFixture f)
    {
        Time.unscaledDeltaTime=OpeningCinematicUI.EntrySeconds;
        Call(Get<OpeningCinematicUI>(f.menu,"cinematic"),"Update");
    }
    static void EntryBody(DesktopHotfixFixture f)
    {
        f.field.hero.heroRect.gameObject.AddComponent<Image>();
        f.field.hero.ChangeAttackMode(0);
        Set(f.manager,"monsterSpawner",f.field.spawner);
        Set(f.field.spawner,"monsterPool",new ObjectPool<GameObject>(
            ()=>f.field.Monster(-100).gameObject,m=>{},m=>m.SetActive(false),m=>{},1,10));
        f.manager.currentMonsterDataSO=HeroAt(1);
    }
    static void SpawnEntryTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        float oldDelta=Time.unscaledDeltaTime;
        using(var sprites=new SpriteResources())
        try
        {
            Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
            foreach(int width in new[]{600,800,1150})foreach(bool fresh in new[]{false,true})
            Run("W2.2 "+width+(fresh?" New Game":" Continue")+" hides only body, waits for reveal/VFX then starts combat",()=>{
                using(var f=new DesktopHotfixFixture(width,true))
                {
                    EntryBody(f);
                    var hp=new GameObject(true).AddComponent<Image>();hp.transform.SetParent(f.field.hero.heroRect,false);hp.color=Color.white;
                    EntryTitle(f,fresh);
                    var rect=f.field.hero.heroRect;float x=f.field.space.Position(rect).x;
                    Check(f.manager.IsEnteringGameplay && Time.timeScale==0f,"entry locks gameplay");
                    Check(f.field.hero.IsDeployed && rect.gameObject.activeInHierarchy,"logic actor stays active");
                    Near(SpriteBody(rect).color.a,0,"body hidden before reveal");Near(hp.color.a,1,"HP bar not faded");
                    Near(x,f.field.space.HomeX,"one canonical spawn location");
                    Check(MonsterController.ActiveMonsters.Count==0,"no wave spawned under opening");
                    Time.deltaTime=.1f;Call(f.field.space,"Update");Call(f.field.space,"LateUpdate");Call(f.field.combat,"Update");
                    Near(f.field.space.Position(rect).x,x,"no movement/pan during reveal");
                    f.ui.SetPaused(false);Check(Time.timeScale==0f,"UI cannot break entry pause");
                    var menuTrigger=f.ui.systemMenu.GetComponent<RectTransform>();
                    Check(f.ui.HitControl(ControlCenter(menuTrigger))==null,"entry owns pointer input");
                    EntryReveal(f);Check(f.manager.IsEnteringGameplay && Time.timeScale==0f,"handoff remains paused");
                    Check(!f.manager.preGameUI.activeInHierarchy,"opening gone before VFX");
                    EntryTick(f,.35f);
                    float alpha=SpriteBody(rect).color.a;Check(alpha>0 && alpha<1,"Hero fades during light burst");
                    var fx=rect.GetComponent<HeroSpawnVfx>();var image=Get<Image>(fx,"effect");var glow=Get<Image>(fx,"glow");
                    Near(image.color.a,1,"frame Image does not inherit hidden body alpha");
                    Near(image.rectTransform.pivot.y,.09f,"effect anchored to source ground ring");
                    Near(image.rectTransform.anchoredPosition.x,0,"effect follows foot x");
                    var body=rect.GetComponent<CultivationSpriteBridge>().VisibleBodySize;
                    Check(glow.rectTransform.rect.width>=body.x*1.3f && glow.rectTransform.rect.height>=body.y*1.3f,"body light covers silhouette");
                    Check(image.rectTransform.rect.height<body.y*1.6f,"effect is not viewport sized");
                    EntryTick(f,1f);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1f,"entry completed");
                    Near(SpriteBody(rect).color.a,1,"body restored");Near(f.field.space.Position(rect).x,x,"no second spawn or coordinate correction");
                    Check(MonsterController.ActiveMonsters.Count>0,"wave begins only after VFX");
                    Check(!image.gameObject.activeSelf && !glow.gameObject.activeSelf,"VFX hidden at completion");
                }
            });
            Run("W2.2 manual Load during appearance cancels old entry and remains immediate",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,.2f);
                    Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"manual load succeeds");
                    Check(!f.manager.IsEnteringGameplay && !f.manager.preGameUI.activeInHierarchy && Time.timeScale==1,"manual Load never replays opening");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"Load restores body");EntryTick(f,8);
                    Check(!f.manager.IsEnteringGameplay && f.manager.HeroData.currentLevel==6,"old entry cannot resume");
                }
            });
            Run("W2.2 return-to-title during VFX cancels fade and can Continue again",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,.2f);
                    Check(f.manager.ReturnToMainMenu(),"return while entering");
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1f && f.manager.preGameUI.activeInHierarchy,"title restored");
                    f.manager.OnContinueClicked();f.manager.OnConfirmResetClicked();EntryReveal(f);EntryTick(f,1.3f);
                    Check(!f.manager.IsEnteringGameplay && f.field.hero.IsDeployed,"second entry finishes");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"no stale alpha");
                }
            });
            Run("W2.2 missing asset and disabled opening cannot leave a black paused screen",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);Resources.Unregister("Spawn/Appear");EntryTitle(f,false);
                    EntryTick(f,OpeningCinematicUI.EntrySeconds+1f);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1f && !f.manager.preGameUI.activeInHierarchy,"watchdog dismisses opening and falls back");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"missing asset reveals Hero");
                }
                Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
            });
            Run("W2.2 Reset and Retry during gameplay do not replay title cinematic",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);f.manager.OnResetClicked();f.manager.OnConfirmResetClicked();
                    Check(!f.manager.IsEnteringGameplay && !f.manager.preGameUI.activeInHierarchy,"gameplay Reset is immediate");
                    f.field.hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked();
                    Check(!f.manager.IsEnteringGameplay && f.field.hero.IsDeployed && Time.timeScale==1,"Retry retains existing entry behaviour");
                }
            });
            Run("W2.2 mode change cancels appearance and preserves Attack/Hit presentation",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,.2f);
                    f.field.hero.ChangeAttackMode(2);EntryTick(f,.01f);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1f,"cancelled VFX releases entry lock");
                    f.field.hero.PlayAttackFeedback();f.field.hero.ShowDamage(1);
                    SpritePose(f.field.hero.heroRect,sprites,"Mage","Attack");Near(SpriteBody(f.field.hero.heroRect).color.a,1,"no stale transparency");
                }
            });
            Run("W2.2 disabling coordinator restores visibility and a valid wave",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,.2f);Call(f.manager,"OnDisable");
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1f,"disable cannot strand pause");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"disable restores body");
                    Check(MonsterController.ActiveMonsters.Count==0,"disable/shutdown never spawns a new wave");
                    Call(f.manager,"OnEnable");Check(MonsterController.ActiveMonsters.Count>0,"reenable resumes the deferred wave");
                }
            });
            Run("W2.2 resize during VFX keeps foot anchor and Hero-relative scale",()=>{
                using(var f=new DesktopHotfixFixture(600,true)) {
                    EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,.2f);
                    var fx=f.field.hero.heroRect.GetComponent<HeroSpawnVfx>();var image=Get<Image>(fx,"effect");
                    float size=image.rectTransform.rect.height;
                    foreach(int width in new[]{800,1150,600}) {
                        Screen.width=width;Screen.height=Mathf.RoundToInt(width*9f/16);
                        ManagementWindowLayout.SetCompact(width,Screen.height);Call(f.field.space,"Update");EntryTick(f,.02f);
                        Near(image.rectTransform.rect.height,size,"logical ratio unaffected by projection resize");
                        Check(image.transform.parent==f.field.hero.heroRect,"effect stays attached to same actor");
                        Near(image.rectTransform.anchoredPosition.x,0,"no cached screen offset");
                    }
                    EntryTick(f,1.3f);
                }
            });
            Run("W2.2 invalid sheet and disabled VFX fall back without hiding actor",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);EntryTitle(f,false);
                    var fx=f.field.hero.heroRect.GetComponent<HeroSpawnVfx>();
                    fx.sheetOverride=new Texture2D(2,2,TextureFormat.RGBA32,false);EntryReveal(f);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1,"invalid texture releases pause");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"invalid texture restores alpha");
                    EntryTitle(f,false);fx.enabled=false;Call(fx,"OnDisable");EntryReveal(f);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1,"disabled effect releases pause");
                }
            });
            Run("W2.2 explicit Reset during VFX cancels the old entry without a duplicate wave",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    EntryBody(f);EntryTitle(f,false);EntryReveal(f);EntryTick(f,.2f);
                    f.manager.OnResetClicked();f.manager.OnConfirmResetClicked();
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1f,"API reset cancels entry");
                    EntryTick(f,8f);
                    Check(MonsterController.ActiveMonsters.Count==Get<int>(f.manager,"waveSpawnCount"),"old entry cannot spawn another wave");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"reset body restored");
                }
            });
        }
        finally { Resources.Unregister("Spawn/Appear");Time.unscaledDeltaTime=oldDelta; }
    }
}
