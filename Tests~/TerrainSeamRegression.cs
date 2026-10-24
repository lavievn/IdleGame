using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static Vector2 SeamEdges(RectTransform tile)
    {
        var rect=tile.GetComponent<Image>().GetPixelAdjustedRect();
        float a=tile.localPosition.x+rect.xMin*tile.localScale.x;
        float b=tile.localPosition.x+rect.xMax*tile.localScale.x;
        return new Vector2(Math.Min(a,b),Math.Max(a,b));
    }
    static void CheckExactTileJoins(GroundPresentation terrain,Field f)
    {
        foreach(string field in new[]{"farTiles","distantTiles","tiles","skyTiles"}) {
            var layer=Get<List<RectTransform>>(terrain,field);
            if(layer.Count==0 || !layer[0].gameObject.activeInHierarchy)continue;
            var ordered=new List<RectTransform>(layer);ordered.Sort((a,b)=>SeamEdges(a).x.CompareTo(SeamEdges(b).x));
            for(int i=0;i<ordered.Count;i++) {
                var tile=ordered[i];var image=tile.GetComponent<Image>();
                Check(tile.parent==f.ground,"one BattleArea parent");Check(tile.anchorMin.Equals(f.ground.pivot) && tile.anchorMax.Equals(f.ground.pivot),"common fixed anchors");
                Near(tile.pivot.x,0,"left pivot");Check(image.type==Image.Type.Simple && !image.preserveAspect,"UI mesh fills the measured rect");
                if(i>0)Near(SeamEdges(tile).x,SeamEdges(ordered[i-1]).y,field+" exact edge equality, no overlap or gap",0f);
            }
            Check(SeamEdges(ordered[0]).x<=f.ground.rect.xMin,"left viewport covered / "+field);
            Check(SeamEdges(ordered[ordered.Count-1]).y>=f.ground.rect.xMax,"right viewport covered / "+field);
        }
    }
    static void TerrainSeamTests()
    {
        using(var art=new MultiTerrainResources()) {
            foreach(var type in new[]{TerrainType.DongBang,TerrainType.RungRam,TerrainType.DoiNui,TerrainType.SaMac,TerrainType.DamLay,TerrainType.HoNuoc})
            foreach(int window in new[]{600,800,1150})foreach(float scale in new[]{1.37f,1f,.83f})
            Run("exact rendered seams before/after recycle and resize / "+type+"/"+window+"/"+scale,()=>{
                int ow=Screen.width,oh=Screen.height;
                try {
                    Screen.width=window;Screen.height=(int)(window*9f/16);ManagementWindowLayout.SetCompact(Screen.width,Screen.height);
                    var f=new Field(window/1000f);f.combat.CurrentHeroCombatState.Data.mapTerrain=type;
                    RectTransform[] decor;var terrain=TerrainField(f,out decor);
                    Near(terrain.groundDropPixels,20,"Ground drop retained");Near(terrain.farParallaxSpeed,.09f,"approved background2 rate");
                    Near(terrain.distantParallaxSpeed,.03f,"approved background3 rate");Near(terrain.skyScale,3f,"approved enlarged Sky");
                    foreach(string field in new[]{"farTiles","distantTiles","tiles","skyTiles"})
                        foreach(var tile in Get<List<RectTransform>>(terrain,field))tile.localScale=new Vector3(scale,1,1);
                    terrain.Scroll(0);CheckExactTileJoins(terrain,f);
                    var hero=f.space.Position(f.hero.heroRect);var ground=f.ground.position;var size=f.ground.sizeDelta;
                    int objects=UnityEngine.Object.objects.Count,loads=Resources.LoadCallCount;
                    foreach(float amount in new[]{-1f,2f,-100f,200f,-10000f,30000f,-12001.25f,50f,-50f,0f}) {
                        float near=terrain.FarPhase,far=terrain.DistantPhase;
                        float nearWidth=Get<List<RectTransform>>(terrain,"farTiles")[0].GetComponent<Image>().GetPixelAdjustedRect().width*scale;
                        float farWidth=Get<List<RectTransform>>(terrain,"distantTiles")[0].GetComponent<Image>().GetPixelAdjustedRect().width*scale;
                        terrain.Scroll(amount);CheckExactTileJoins(terrain,f);
                        Near(terrain.FarPhase,BattleMotion.Wrap(near+amount*.09f,0,nearWidth),"wrap uses displayed width including scale");
                        Near(terrain.DistantPhase,BattleMotion.Wrap(far+amount*.03f,0,farWidth),"far wrap uses displayed width including scale");
                    }
                    for(int i=0;i<180;i++) {terrain.Scroll(i%2==0?-7f:11f);CheckExactTileJoins(terrain,f);}
                    Near(f.space.Position(f.hero.heroRect).x,hero.x,"tiling cannot move Hero X");Near(f.space.Position(f.hero.heroRect).y,hero.y,"tiling cannot move Hero Y");
                    Check(f.ground.position.Equals(ground) && f.ground.sizeDelta.Equals(size),"logical Ground unchanged");
                    Check(UnityEngine.Object.objects.Count==objects && Resources.LoadCallCount==loads,"recycle reuses objects and cached resources");
                    f.ground.sizeDelta=new Vector2(1920,100);terrain.Scroll(0);CheckExactTileJoins(terrain,f);
                    f.ground.sizeDelta=new Vector2(720,100);terrain.Scroll(0);CheckExactTileJoins(terrain,f);
                    f.combat.CurrentHeroCombatState.Data.mapTerrain=TerrainType.CaoNguyen;terrain.Scroll(0);CheckExactTileJoins(terrain,f);
                } finally {Screen.width=ow;Screen.height=oh;ManagementWindowLayout.SetCompact(ow,oh);}
            });
#if UNITY_EDITOR
            Run("Editor seam log emits once per recycled tile and is silent while idle or disabled",()=>{
                var f=new Field();RectTransform[] decor;var terrain=TerrainField(f,out decor);var lines=new List<string>();
                Debug.LogObserver=line=>{if(line.StartsWith("[TerrainSeam]"))lines.Add(line);};
                try {
                    Set(terrain,"traceTileSeams",true);for(int i=0;i<60;i++)terrain.Scroll(0);Check(lines.Count==0,"no Update log spam");
                    terrain.Scroll(-20000);Check(lines.Count>0 && lines.Count<=9,"at most three recycled tiles per moving layer");
                    bool near=false,far=false;
                    foreach(var line in lines) {
                        Check(line.Contains("tile=") && line.Contains("xMin=") && line.Contains("xMax=") && line.Contains("renderedWidth=") && line.Contains("neighborXMin=") && line.Contains("neighborXMax="),"all measured fields present");
                        near|=line.Contains("Background2");far|=line.Contains("Background3");
                        if(line.EndsWith("join=after"))Near(TraceValue(line,"xMin"),TraceValue(line,"neighborXMax"),"recycled tile starts at neighbor end",0f);
                        else Near(TraceValue(line,"xMax"),TraceValue(line,"neighborXMin"),"recycled tile ends at neighbor start",0f);
                    }
                    Check(near && far,"both background layers observed");
                    foreach(string label in new[]{"Background2","Background3"})foreach(var line in lines)if(line.Contains(label)){Console.WriteLine("MONO SAMPLE "+line);break;}
                    lines.Clear();Set(terrain,"traceTileSeams",false);terrain.Scroll(20000);Check(lines.Count==0,"switch disables temporary diagnostics");
                } finally {Debug.LogObserver=null;}
            });
#endif
        }
    }
}
