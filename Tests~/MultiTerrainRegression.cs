using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    sealed class MultiTerrainResources : IDisposable
    {
        readonly PlainsResources plains=new PlainsResources();
        public readonly Dictionary<GroundPresentation.ArtworkGroup,Sprite[]> Art=new Dictionary<GroundPresentation.ArtworkGroup,Sprite[]>();
        readonly List<string> paths=new List<string>();
        public MultiTerrainResources()
        {
            Art[GroundPresentation.ArtworkGroup.Plains]=plains.Art;
            foreach(string group in new[]{"Forest","Mountain"}) {
                string suffix=group=="Forest"?"j":"m";
                string[] names={"ground_"+suffix,"ground_"+suffix+"1","ground_"+suffix+"2","sky_"+suffix};
                var sprites=new Sprite[4];Art[group=="Forest"?GroundPresentation.ArtworkGroup.Forest:GroundPresentation.ArtworkGroup.Mountain]=sprites;
                for(int i=0;i<4;i++) {
                    string path="Environment/"+group+"/"+names[i];paths.Add(path);
                    sprites[i]=Sprite.Create(new Texture2D(2172,724,TextureFormat.RGBA32,false),new Rect(0,0,2172,724),new Vector2(.5f,.5f),100);
                    Resources.Register(path,sprites[i]);
                }
            }
        }
        public void Dispose() {plains.Dispose();foreach(string path in paths)Resources.Unregister(path);}
    }
    static GroundPresentation.ArtworkGroup ExpectedTerrain(TerrainType type)
    {
        switch(type) {
            case TerrainType.DongBang:return GroundPresentation.ArtworkGroup.Plains;
            case TerrainType.RungRam:return GroundPresentation.ArtworkGroup.Forest;
            case TerrainType.DoiNui:case TerrainType.CaoNguyen:return GroundPresentation.ArtworkGroup.Mountain;
            default:return GroundPresentation.ArtworkGroup.Legacy;
        }
    }
    static void CheckTerrainArt(GroundPresentation terrain,MultiTerrainResources resources,TerrainType type)
    {
        var expected=ExpectedTerrain(type);Check(terrain.CurrentArtworkGroup==expected,"terrain type chooses "+expected);
        string[] layers={"tiles","farTiles","distantTiles","skyTiles"};
        if(expected==GroundPresentation.ArtworkGroup.Legacy) {
            Check(!terrain.UsesTerrainArtwork,"no artwork from previous terrain");
            foreach(var sky in Get<List<RectTransform>>(terrain,"skyTiles"))Check(!sky.gameObject.activeSelf,"legacy sky hidden");
            return;
        }
        var art=resources.Art[expected];int previous=100;
        for(int i=0;i<layers.Length;i++) {
            var tiles=Get<List<RectTransform>>(terrain,layers[i]);Check(tiles.Count==3,"existing three-tile renderer reused");
            foreach(var rect in tiles) {
                var image=rect.GetComponent<Image>();
                Check(image.sprite==art[i],"correct sprite for all tiles of "+layers[i]);
                Check(rect.gameObject.activeSelf && image.enabled,"no blank/disabled replacement tile");
                Check(!image.raycastTarget,"non-blocking environment");
                Near(image.color.r,1,"original color R");Near(image.color.g,1,"original color G");Near(image.color.b,1,"original color B");Near(image.color.a,1,"PNG alpha not globally multiplied");
                Near(rect.rect.height/rect.rect.width,art[i].rect.height/art[i].rect.width,"source aspect preserved");
                Check(rect.GetSiblingIndex()<previous,"back-to-front order");
            }
            previous=tiles[0].GetSiblingIndex();
        }
    }
    static void MultiTerrainTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        using(var resources=new MultiTerrainResources()) {
            foreach(TerrainType type in Enum.GetValues(typeof(TerrainType)))
            Run("map terrain enum selects exact full artwork set or legacy fallback / "+type,()=>{
                var f=new Field();f.combat.CurrentHeroCombatState.Data.mapTerrain=type;
                f.combat.CurrentHeroCombatState.Data.mapName="Rừng Núi Đồng Bằng: name must not select art";
                f.combat.CurrentHeroCombatState.Data.worldMapId="not-an-artwork-key";
                RectTransform[] props;var t=TerrainField(f,out props);CheckTerrainArt(t,resources,type);
                foreach(var prop in props)Check(prop.gameObject.activeSelf==!t.UsesTerrainArtwork,"old props visible only on fallback");
            });
            Run("all 133 catalog nodes use their existing terrain types, not names or IDs",()=>{
                var f=new Field();RectTransform[] props;var t=TerrainField(f,out props);var data=f.combat.CurrentHeroCombatState.Data;
                Check(WorldMapCatalog.All.Count==133,"real catalog loaded");
                var families=new HashSet<GroundPresentation.ArtworkGroup>();
                foreach(var node in WorldMapCatalog.All) {
                    data.mapTerrain=node.Terrain;data.mapName=node.Name;data.worldMapId=node.Id;
                    t.Scroll(0);CheckTerrainArt(t,resources,node.Terrain);families.Add(t.CurrentArtworkGroup);
                }
                Check(families.Count==4,"catalog exercises three new families plus legacy fallback");
            });
            Run("repeated three-family swaps change all layers atomically without actors, camera, phase or RNG changes",()=>{
                var f=new Field();var m=f.Monster(-130,AttackMode.RangedMagic,40);RectTransform[] props;var t=TerrainField(f,out props);
                t.Scroll(70);var hero=f.space.Position(f.hero.heroRect);var monster=f.space.Position(m.Rect);
                var size=f.ground.sizeDelta;var groundPosition=f.ground.position;float home=f.space.HomeX;
                var target=Get<HeroController>(f.space,"cameraTarget");float focus=f.space.CurrentCameraSmoothedFocusX,velocity=f.space.CameraFollowVelocity;
                float phase=t.TilePhase,far=t.FarPhase,distant=t.DistantPhase;
                UnityEngine.Random.InitState(761);int next=UnityEngine.Random.Range(0,10000);UnityEngine.Random.InitState(761);
                for(int i=0;i<60;i++)foreach(var type in new[]{TerrainType.RungRam,TerrainType.DoiNui,TerrainType.DongBang,TerrainType.SaMac,TerrainType.CaoNguyen}) {
                    f.combat.CurrentHeroCombatState.Data.mapTerrain=type;t.Scroll(0);CheckTerrainArt(t,resources,type);
                }
                Check(UnityEngine.Random.Range(0,10000)==next,"visual swaps consume no RNG");
                Near(t.TilePhase,phase,"ground phase");Near(t.FarPhase,far,"near phase");Near(t.DistantPhase,distant,"far phase");
                Near(f.space.Position(f.hero.heroRect).x,hero.x,"Hero X");Near(f.space.Position(f.hero.heroRect).y,hero.y,"Hero Y");
                Near(f.space.Position(m.Rect).x,monster.x,"Enemy X");Near(f.space.Position(m.Rect).y,monster.y,"Enemy Y");
                Check(f.ground.sizeDelta.Equals(size) && f.ground.position.Equals(groundPosition),"Ground coordinate system unchanged");
                Near(f.space.HomeX,home,"HomeX");Check(Get<HeroController>(f.space,"cameraTarget")==target,"camera subject");
                Near(f.space.CurrentCameraSmoothedFocusX,focus,"camera focus");Near(f.space.CameraFollowVelocity,velocity,"camera velocity");
            });
            foreach(var type in new[]{TerrainType.DongBang,TerrainType.RungRam,TerrainType.DoiNui})foreach(int width in new[]{600,800,1150})
            Run("terrain resize keeps 20px Ground-only offset, top Sky and fixed parallax / "+type+"/"+width,()=>{
                int ow=Screen.width,oh=Screen.height;
                try {
                    var f=new Field();f.combat.CurrentHeroCombatState.Data.mapTerrain=type;RectTransform[] props;var t=TerrainField(f,out props);
                    Screen.width=width;Screen.height=(int)(width*9f/16);ManagementWindowLayout.SetCompact(Screen.width,Screen.height);
                    t.groundDropPixels=0;t.Scroll(0);
                    var ground=Get<List<RectTransform>>(t,"tiles");var before=new List<Vector2>();foreach(var tile in ground)before.Add(tile.anchoredPosition);
                    var near=Get<List<RectTransform>>(t,"farTiles")[1];var far=Get<List<RectTransform>>(t,"distantTiles")[1];var sky=Get<List<RectTransform>>(t,"skyTiles")[1];
                    var np=near.anchoredPosition;var fp=far.anchoredPosition;var sp=sky.anchoredPosition;
                    t.groundDropPixels=20;t.Scroll(0);float projection=ManagementWindowLayout.BattleCanvasScale(width,f.space.Width);
                    for(int i=0;i<ground.Count;i++)Near((ground[i].anchoredPosition.y-before[i].y)*projection,-20,"20px not 20 world units");
                    Check(near.anchoredPosition.Equals(np) && far.anchoredPosition.Equals(fp) && sky.anchoredPosition.Equals(sp),"only Ground shifted");
                    Near(sky.anchoredPosition.y,f.ground.rect.yMin+f.space.Width*Screen.height/Screen.width,"viewport top");Near(sky.rect.width,f.space.Width*3f,"approved 300 percent, no overlap");
                    foreach(float amount in new[]{200f,-140f,10000f,-25000f}) {
                        float gp=t.TilePhase,nearPhase=t.FarPhase,farPhase=t.DistantPhase;t.Scroll(amount);
                        Near(t.TilePhase,BattleMotion.Wrap(gp+amount,0,f.space.Width),"Ground loop");
                        Near(t.FarPhase,BattleMotion.Wrap(nearPhase+amount*.09f,0,f.space.Width*1.3f),"9 percent");
                        Near(t.DistantPhase,BattleMotion.Wrap(farPhase+amount*.03f,0,f.space.Width*1.69f),"3 percent");
                        Check(sky.anchoredPosition.Equals(sp),"Sky stationary");
                        var art=resources.Art[ExpectedTerrain(type)];string[] layers={"tiles","farTiles","distantTiles","skyTiles"};
                        for(int i=0;i<layers.Length;i++)PlainsCoverage(t,f,layers[i],art[i].rect.height/art[i].rect.width);
                    }
                } finally {Screen.width=ow;Screen.height=oh;ManagementWindowLayout.SetCompact(ow,oh);}
            });
            foreach(var type in new[]{TerrainType.DongBang,TerrainType.RungRam,TerrainType.DoiNui})for(int layer=0;layer<4;layer++) {
                int missing=layer;var group=ExpectedTerrain(type);string folder=group.ToString();
                string[] names=group==GroundPresentation.ArtworkGroup.Plains?new[]{"Ground","Ground2","Ground3","sky"}:
                    group==GroundPresentation.ArtworkGroup.Forest?new[]{"ground_j","ground_j1","ground_j2","sky_j"}:new[]{"ground_m","ground_m1","ground_m2","sky_m"};
                string path="Environment/"+folder+"/"+names[layer];
                Run("missing terrain layer replaces previous family with complete legacy fallback / "+group+"/"+missing,()=>{
                    Resources.Unregister(path);
                    try {
                        var f=new Field();f.combat.CurrentHeroCombatState.Data.mapTerrain=type==TerrainType.DongBang?TerrainType.RungRam:TerrainType.DongBang;
                        RectTransform[] props;var t=TerrainField(f,out props);Check(t.UsesTerrainArtwork,"another complete set starts visible");
                        f.combat.CurrentHeroCombatState.Data.mapTerrain=type;t.Scroll(0);
                        Check(t.CurrentArtworkGroup==GroundPresentation.ArtworkGroup.Legacy,"incomplete target refuses stale previous artwork");
                        Check(Get<List<RectTransform>>(t,"tiles")[0].GetComponent<Image>().sprite==Get<Sprite>(t,"legacyGround"),"complete old Ground restored");
                        Check(Get<List<RectTransform>>(t,"farTiles")[0].GetComponent<Image>().sprite==Get<Sprite>(t,"legacyNear"),"complete old near restored");
                        Check(Get<List<RectTransform>>(t,"distantTiles")[0].GetComponent<Image>().sprite==Get<Sprite>(t,"legacyFar"),"complete old far restored");
                        foreach(var sky in Get<List<RectTransform>>(t,"skyTiles"))Check(!sky.gameObject.activeSelf,"old sky not retained");
                    } finally {Resources.Register(path,resources.Art[group][missing]);}
                });
            }
            Run("artwork cache survives repeated load-style Hero data replacements without reloading or creating objects",()=>{
                var f=new Field();RectTransform[] props;var t=TerrainField(f,out props);
                var data=new[]{new EntityDataSO{mapTerrain=TerrainType.RungRam},new EntityDataSO{mapTerrain=TerrainType.CaoNguyen},new EntityDataSO{mapTerrain=TerrainType.DongBang}};
                int loads=Resources.LoadCallCount,objects=UnityEngine.Object.objects.Count;
                for(int i=0;i<120;i++) {
                    var next=data[i%3];f.combat.SetupHeroInfo(next);t.Scroll(0);CheckTerrainArt(t,resources,next.mapTerrain);
                    Call(t,"LateUpdate");
                }
                Check(Resources.LoadCallCount==loads,"twelve sprites loaded only at Install");Check(UnityEngine.Object.objects.Count==objects,"no leaked scene objects or textures during swaps");
                f.combat.CurrentHeroCombatState.Data.mapTerrain=(TerrainType)999;t.Scroll(0);
                Check(t.CurrentArtworkGroup==GroundPresentation.ArtworkGroup.Legacy,"unknown future terrain uses legacy renderer");
            });
            Resources.Register("Spawn/Appear",new Texture2D(4000,2500,TextureFormat.RGBA32,false));
            try {using(var sprites=new SpriteResources())foreach(string route in new[]{"New","Continue","Load","Retry","Reset","Travel"})
            Run("map entry selects terrain from live Hero snapshot while preserving appear pipeline / "+route,()=>{
                using(var f=new DesktopHotfixFixture(800,true)) {
                    WaveEntryScene(f);WaveEntryFinish(f);RectTransform[] props;var t=TerrainField(f.field,out props);t.Scroll(0);
                    if(route=="New" || route=="Continue")EntryTitle(f,route=="New");
                    else if(route=="Load")Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"manual load");
                    else if(route=="Retry") {f.field.hero.Die();f.manager.OnHeroDied();f.manager.OnRetryClicked();}
                    else if(route=="Reset") {f.manager.OnResetClicked();f.manager.OnConfirmResetClicked();}
                    else Check(f.manager.TryTravelToWorldMap(EntryNeighbor(f.manager)),"travel");
                    Check(f.manager.IsEnteringGameplay && Time.timeScale==0,"entry still pauses gameplay");
                    var before=f.field.space.Position(f.field.hero.heroRect);Call(t,"LateUpdate");
                    CheckTerrainArt(t,resources,f.manager.HeroData.mapTerrain);
                    Near(f.field.space.Position(f.field.hero.heroRect).x,before.x,"art switch does not move Hero X under appear");
                    Near(f.field.space.Position(f.field.hero.heroRect).y,before.y,"art switch does not move Hero Y under appear");
                    if(route=="New" || route=="Continue")EntryReveal(f);
                    EntryTick(f,HeroSpawnVfx.Duration);Call(t,"LateUpdate");
                    Check(!f.manager.IsEnteringGameplay && Time.timeScale==1,"VFX completes and gameplay resumes");
                    CheckTerrainArt(t,resources,f.manager.HeroData.mapTerrain);
                    Check(f.manager.activeMonsters.Count>0,"first wave unaffected");
                }
            });} finally {Resources.Unregister("Spawn/Appear");}
        }
    }
}
