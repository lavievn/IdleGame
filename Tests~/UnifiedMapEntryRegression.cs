using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static string EntryNeighbor(GameManager manager)
    {
        var data=manager.HeroData;WorldMapNode node;
        Check(WorldMapCatalog.TryGet(data.worldMapId,out node),"current world node exists");
        if(!data.worldMapCleared.Contains(node.Id))data.worldMapCleared.Add(node.Id);
        foreach(var id in node.Links) {
            WorldMapNode other;if(WorldMapCatalog.TryGet(id,out other) && !other.IsBoss)return id;
        }
        throw new Exception("No eligible neighbor for travel fixture");
    }
    static void UnifiedMapEntryTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
        using(var sprites=new SpriteResources())
        {
            foreach(int width in new[]{600,800,1150})foreach(string route in new[]{"New","Continue","Load","Retry","Reset","Travel"})
            Run("unified map entry locks actors/UI until appear, one first wave "+width+"/"+route,()=>{
                using(var f=new DesktopHotfixFixture(width,true)) {
                    WaveEntryScene(f);f.field.space.useMultiZoneCamera=true;WaveEntryFinish(f);
                    var hero=f.field.hero;var space=f.field.space;
                    var hp=new GameObject(true).AddComponent<Image>();hp.transform.SetParent(hero.heroRect,false);
                    var damage=new GameObject(true).AddComponent<TMPro.TextMeshProUGUI>();damage.transform.SetParent(hero.heroRect,false);
                    var charge=new GameObject(true).AddComponent<RawImage>();charge.transform.SetParent(hero.heroRect,false);
                    string one=File.ReadAllText(f.Slot("ManualSave1")),two=File.ReadAllText(f.Slot("ManualSave2"));
                    int oldLife=hero.SpawnVersion;
                    if(route=="New" || route=="Continue")EntryTitle(f,route=="New");
                    else if(route=="Load") { f.ui.SetPaused(true);Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"paused manual Load"); }
                    else if(route=="Retry") { hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked(); }
                    else if(route=="Reset") { f.manager.OnResetClicked();f.manager.OnConfirmResetClicked(); }
                    else Check(f.manager.TryTravelToWorldMap(EntryNeighbor(f.manager)),"manual World Map travel");
                    bool title=route=="New" || route=="Continue";
                    Check(f.manager.IsEnteringGameplay && Time.timeScale==0,"all entries are paused");
                    Check(hero.IsDeployed && hero.heroRect.gameObject.activeInHierarchy,"only presentation hidden, logic actor stays active");
                    Check(hero.SpawnVersion==oldLife+1,"one spawn for this map entry");
                    Near(space.Position(hero.heroRect).x,576,"80-percent final position before VFX");
                    Near(SpriteBody(hero.heroRect).color.a,0,"body starts invisible");
                    Check(!hp.enabled && !damage.enabled && !charge.enabled,"all attached UI hidden");
                    Check(f.manager.activeMonsters.Count==0 && f.field.combat.PendingProjectileCount==0,"no old monsters or projectiles");
                    Check(f.menu.IsEnteringCinematic==title,"only title routes run long cinematic");
                    if(title)EntryReveal(f);
                    var vfx=Get<HeroSpawnVfx>(f.manager,"entryVfx");Check(vfx!=null && vfx.IsPlaying,"real appear timeline starts");
                    int newLife=hero.SpawnVersion;
                    var before=space.Position(hero.heroRect);
                    EntryTick(f,.35f);
                    float alpha=SpriteBody(hero.heroRect).color.a;Check(alpha>0 && alpha<1,"body fades during light");
                    Check(!hp.enabled && !damage.enabled && !charge.enabled,"UI remains hidden until completion");
                    for(int i=0;i<5;i++) {
                        Call(f.manager,"CallNextWave");NativeOrderTraceFrame(f,1f/60f);
                        Near(space.Position(hero.heroRect).x,before.x,"no movement or camera pan under VFX");
                        Near(space.LastAppliedCameraPan,0,"paused camera");
                        Check(f.manager.activeMonsters.Count==0,"direct/stale wave calls cannot bypass entry lock");
                    }
                    EntryTick(f,HeroSpawnVfx.Duration);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1,"entry releases pause");
                    Check(!f.manager.preGameUI.activeInHierarchy && !f.menu.IsEnteringCinematic,"no opening left over");
                    Near(space.Position(hero.heroRect).x,576,"release does not reposition Hero");
                    Near(SpriteBody(hero.heroRect).color.a,1,"body visible after VFX");
                    Check(hp.enabled && damage.enabled && charge.enabled,"UI restored at completion");
                    var cover=Get<Image>(f.manager,"mapEntryCover");
                    Check(cover==null || !cover.transform.parent.gameObject.activeInHierarchy,"short curtain cannot remain over gameplay");
                    var wave=f.manager.activeMonsters.ToArray();Check(wave.Length>0,"first wave starts immediately after VFX");
                    EntryTick(f,HeroSpawnVfx.Duration*2);Call(f.manager,"CallNextWave");
                    Check(f.manager.activeMonsters.Count==wave.Length,"no duplicate first wave");
                    for(int i=0;i<wave.Length;i++)Check(f.manager.activeMonsters[i]==wave[i],"same active wave");
                    Check(hero.SpawnVersion==newLife,"no second spawn after appearance");
                    EndWave(f);
                    Check(!f.manager.IsEnteringGameplay && !vfx.IsPlaying,"ordinary wave completion never replays VFX");
                    Check(File.ReadAllText(f.Slot("ManualSave1"))==one && File.ReadAllText(f.Slot("ManualSave2"))==two,"manual slots remain byte-for-byte intact");
                }
            });
            Run("unified entry replacement and Return Menu discard old VFX and wave release",()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);
                    Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"first Load");EntryTick(f,.2f);
                    Check(f.manager.LoadAndContinue(SaveSlot.ManualSave2),"replacement Load");
                    var current=f.manager.HeroData;Call(f.manager,"LateUpdate");
                    Check(f.manager.IsEnteringGameplay && f.manager.activeMonsters.Count==0,"old completion cannot release replacement");
                    Check(f.manager.ReturnToMainMenu(),"Return Menu during short appear");EntryTick(f,5f);
                    Check(!f.manager.IsEnteringGameplay && f.manager.preGameUI.activeInHierarchy && !f.field.hero.IsDeployed,"no old VFX callback revives gameplay");
                    var cover=Get<Image>(f.manager,"mapEntryCover");Check(!cover.transform.parent.gameObject.activeInHierarchy,"cover cleared on cancel");
                    Check(f.manager.HeroData==current,"cancel preserves selected snapshot");
                    f.manager.OnContinueClicked();f.manager.OnConfirmResetClicked();EntryReveal(f);EntryTick(f,HeroSpawnVfx.Duration);
                    Check(f.manager.IsGameplayHUDVisible && f.field.hero.IsDeployed,"Continue after cancellation succeeds");
                }
            });
            Run("unified entry missing or invalid appear sheet cannot leave pause or black cover",()=>{
                foreach(bool invalid in new[]{false,true})using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);
                    if(invalid)f.field.hero.heroRect.GetComponent<HeroSpawnVfx>().sheetOverride=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    else Resources.Unregister("Spawn/Appear");
                    Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"Load still accepts valid data");
                    EntryTick(f,.01f);
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1 && f.field.hero.IsDeployed,"safe fallback resumes gameplay");
                    var cover=Get<Image>(f.manager,"mapEntryCover");Check(!cover.transform.parent.gameObject.activeInHierarchy,"no black cover after asset failure");
                    Near(SpriteBody(f.field.hero.heroRect).color.a,1,"fallback reveals Hero");
                    Check(f.manager.activeMonsters.Count>0,"fallback still begins one wave");
                    Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
                }
            });
        }
        Resources.Unregister("Spawn/Appear");
    }
}
