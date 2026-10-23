using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    sealed class PlainsResources : IDisposable
    {
        public readonly Sprite[] Art=new Sprite[4];
        public readonly string[] Names={"Ground","Ground2","Ground3","sky"};
        public PlainsResources()
        {
            int[] heights={530,724,724,274};
            for(int i=0;i<4;i++) {
                Art[i]=Sprite.Create(new Texture2D(2172,heights[i],TextureFormat.RGBA32,false),
                    new Rect(0,0,2172,heights[i]),new Vector2(.5f,.5f),100);
                Resources.Register("Environment/Plains/"+Names[i],Art[i]);
            }
        }
        public void Dispose() {foreach(string name in Names)Resources.Unregister("Environment/Plains/"+name);}
    }
    static void PlainsCoverage(GroundPresentation t,Field f,string field,float ratio)
    {
        var list=Get<List<RectTransform>>(t,field);var spans=new List<float[]>();
        foreach(var rect in list) {
            float x=f.space.Position(rect).x;
            spans.Add(new[]{x,x+rect.rect.width});
            Near(rect.rect.height/rect.rect.width,ratio,"PNG aspect preserved");
            Check(!rect.GetComponent<Image>().raycastTarget,"no interaction blocker");
        }
        spans.Sort((a,b)=>a[0].CompareTo(b[0]));float edge=f.ground.rect.xMin;
        foreach(var s in spans)if(s[0]<=edge+.001f)edge=Math.Max(edge,s[1]);
        Check(edge>=f.ground.rect.xMax,"full horizontal rect coverage (not proof of seamless artwork)");
    }
    static void PlainsPresentationTests()
    {
        using(var art=new PlainsResources()) {
            Run("Plains maps use all four exact sprites in back-to-front order and hide preview props",()=>{
                var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);
                Check(t.UsesPlainsArtwork,"DongBang selects plains");
                string[] fields={"tiles","farTiles","distantTiles","skyTiles"};
                int previous=100;
                for(int i=0;i<4;i++) {
                    var layer=Get<List<RectTransform>>(t,fields[i]);
                    foreach(var rect in layer) {
                        Check(rect.GetComponent<Image>().sprite==art.Art[i],"exact layer sprite mapping");
                        Check(rect.GetSiblingIndex()<previous,"each farther layer painted before nearer layer");
                    }
                    previous=layer[0].GetSiblingIndex();
                }
                foreach(var rect in decor)Check(!rect.gameObject.activeSelf,"no old preview decoration on plains");
                Check(!t.SourceImage.enabled,"old root image disabled");
                Call(t,"LateUpdate");
                Check(f.hero.heroRect.GetSiblingIndex()>Get<List<RectTransform>>(t,"tiles")[2].GetSiblingIndex(),"actor painted after ground");
                t.SetTerrainTint(new Color(.1f,.2f,.3f));
                foreach(string field in fields)foreach(var rect in Get<List<RectTransform>>(t,field)) {
                    var color=rect.GetComponent<Image>().color;Near(color.r,1,"art untinted");Near(color.g,1,"art original green");
                }
            });
            Run("terrain switches restore legacy sprites and do not reposition actors or reroll RNG",()=>{
                var f=new Field();f.combat.CurrentHeroCombatState.Data.mapTerrain=TerrainType.DoiNui;
                RectTransform[] decor;var t=TerrainField(f,out decor);var legacy=Get<List<RectTransform>>(t,"tiles")[0].GetComponent<Image>().sprite;
                var p=f.space.Position(f.hero.heroRect);var groundSize=f.ground.sizeDelta;var home=f.space.HomeX;
                var m=f.Monster(80,AttackMode.RangedPhysical,30);var mp=f.space.Position(m.Rect);
                UnityEngine.Random.InitState(864);int expected=UnityEngine.Random.Range(0,10000);UnityEngine.Random.InitState(864);
                for(int repeat=0;repeat<4;repeat++) {
                    f.combat.CurrentHeroCombatState.Data.mapTerrain=TerrainType.DongBang;t.Scroll(0);
                    Check(t.UsesPlainsArtwork,"plains enabled");
                    f.combat.CurrentHeroCombatState.Data.mapTerrain=TerrainType.SaMac;t.Scroll(0);
                    Check(!t.UsesPlainsArtwork,"other terrain restored");
                    Check(Get<List<RectTransform>>(t,"tiles")[0].GetComponent<Image>().sprite==legacy,"old source restored");
                    foreach(var rect in decor)Check(rect.gameObject.activeSelf,"preview props restored on other maps");
                    foreach(var rect in Get<List<RectTransform>>(t,"skyTiles"))Check(!rect.gameObject.activeSelf,"no sky on other terrain");
                }
                Check(UnityEngine.Random.Range(0,10000)==expected,"art transitions do not consume random values");
                Near(f.space.Position(f.hero.heroRect).x,p.x,"Hero X");Near(f.space.Position(f.hero.heroRect).y,p.y,"Hero Y");
                Near(f.space.Position(m.Rect).x,mp.x,"Monster X");Near(f.space.Position(m.Rect).y,mp.y,"Monster Y");
                Check(f.ground.sizeDelta.Equals(groundSize),"logical floor unchanged");Near(f.space.HomeX,home,"HomeX unchanged");
            });
            Run("Plains parallax keeps original 30/10 percent rates with stationary sky and complete tile bounds",()=>{
                var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);
                var sky=Get<List<RectTransform>>(t,"skyTiles")[1];var before=sky.anchoredPosition;
                foreach(float amount in new[]{100f,-13f,10000f,-25000f,999.25f}) {
                    float old=t.TilePhase,near=t.FarPhase,far=t.DistantPhase;
                    t.Scroll(amount);
                    Near(t.TilePhase,BattleMotion.Wrap(old+amount,0,f.space.Width),"ground period");
                    Near(t.FarPhase,BattleMotion.Wrap(near+amount*.3f,0,f.space.Width*1.3f),"near parallax");
                    Near(t.DistantPhase,BattleMotion.Wrap(far+amount*.1f,0,f.space.Width*1.69f),"far parallax");
                    PlainsCoverage(t,f,"tiles",530f/2172);PlainsCoverage(t,f,"farTiles",724f/2172);
                    PlainsCoverage(t,f,"distantTiles",724f/2172);PlainsCoverage(t,f,"skyTiles",274f/2172);
                    Near(sky.anchoredPosition.x,before.x,"sky ignores camera pan");Near(sky.anchoredPosition.y,before.y,"sky top stable");
                }
            });
            foreach(int width in new[]{600,800,1150})
            Run("Sky 250 percent reference width follows viewport top at "+width,()=>{
                int ow=Screen.width,oh=Screen.height;
                try {
                    Screen.width=width;Screen.height=(int)(width*9f/16);ManagementWindowLayout.SetCompact(Screen.width,Screen.height);
                    var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);Near(t.skyScale,2.5f,"default scale");
                    var p=f.space.Position(f.hero.heroRect);
                    var sky=Get<List<RectTransform>>(t,"skyTiles")[1];
                    Near(sky.rect.width,f.space.Width*2.5f+1,"250 percent plus one-unit overlap");
                    float top=f.ground.rect.yMin+f.space.Width*Screen.height/Screen.width;
                    Near(sky.anchoredPosition.y,top,"top at gameplay viewport, not 100-unit floor");
                    t.skyOffset=new Vector2(12,-7);t.skyScale=3;t.Scroll(0);
                    Near(sky.anchoredPosition.y,top-7,"Inspector offset");Near(sky.rect.width,f.space.Width*3+1,"Inspector scale");
                    Screen.height+=81;t.Scroll(0);
                    Near(sky.anchoredPosition.y,f.ground.rect.yMin+f.space.Width*Screen.height/Screen.width-7,"height-only resize recalculates sky");
                    Near(f.space.Position(f.hero.heroRect).x,p.x,"resize leaves Hero X");Near(f.space.Position(f.hero.heroRect).y,p.y,"resize leaves Hero Y");
                } finally {Screen.width=ow;Screen.height=oh;ManagementWindowLayout.SetCompact(ow,oh);}
            });
            Run("scrolling and repeated map changes perform no new resource loads or object creation",()=>{
                var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);
                int loads=Resources.LoadCallCount;int objects=UnityEngine.Object.objects.Count;
                for(int i=0;i<180;i++) {f.combat.CurrentHeroCombatState.Data.mapTerrain=i%2==0?TerrainType.DongBang:TerrainType.DoiNui;t.Scroll(0);}
                Check(Resources.LoadCallCount==loads,"all assets cached during installation");Check(UnityEngine.Object.objects.Count==objects,"reuse layer objects");
            });
            Run("missing one Plains image keeps the entire legacy set rather than a mixed broken renderer",()=>{
                Resources.Unregister("Environment/Plains/Ground2");
                try {var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);Check(!t.UsesPlainsArtwork,"complete set required");}
                finally {Resources.Register("Environment/Plains/Ground2",art.Art[1]);}
            });
        }
    }
}
