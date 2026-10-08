using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

partial class MotionRegression
{
    static GroundPresentation TerrainField(Field f, out RectTransform[] decor)
    {
        var ground=f.ground.gameObject.AddComponent<Image>();ground.sprite=new Sprite();ground.color=new Color(.7f,.6f,.3f,1);
        f.grass.gameObject.name="Grass1";f.grass.sizeDelta=new Vector2(600,130);
        f.space.SetPosition(f.grass,new Vector2(-400,65));
        f.grass.gameObject.AddComponent<Image>().sprite=new Sprite();
        var all=new List<RectTransform>{f.grass};var items=new List<RectTransform>();
        for(int i=0;i<7;i++) {
            var go=new GameObject(true);go.name="Grass1 ("+(i+1)+")";
            var rect=(RectTransform)go.transform;rect.parent=f.ground;rect.sizeDelta=new Vector2(10,20);
            go.AddComponent<Image>();all.Add(rect);items.Add(rect);
        }
        decor=items.ToArray();Set(f.space,"groundDetails",all.ToArray());
        Call(f.space,"Start");return f.ground.GetComponent<GroundPresentation>();
    }
    static void AssertCoverage(GroundPresentation terrain, Field f)
    {
        var tiles=Get<List<RectTransform>>(terrain,"tiles");
        var intervals=new List<float[]>();foreach(var tile in tiles) {
            float left=f.space.Position(tile).x;intervals.Add(new[]{left,left+tile.rect.width});
            Near(tile.rect.height,130,"original grass height unchanged");
        }
        intervals.Sort((a,b)=>a[0].CompareTo(b[0]));float covered=f.ground.rect.xMin;
        foreach(var interval in intervals)if(interval[0]<=covered+0.001f)covered=Math.Max(covered,interval[1]);
        Check(covered>=f.ground.rect.xMax,"tiles cover viewport without holes");
    }
    static void TerrainTests()
    {
        Run("terrain visual clone grows 1.2 while logical ground and hero stats stay unchanged",()=>{
            var f=new Field();float range=f.space.AttackRange(TuTienCore.AttackMode.Melee,true);
            RectTransform[] decor;var t=TerrainField(f,out decor);Check(t!=null,"terrain installed");
            Near(f.space.Width,1000,"logical width unchanged");Near(f.ground.localScale.x,1,"root scale unchanged");
            Near(f.space.AttackRange(TuTienCore.AttackMode.Melee,true),range,"range unchanged");
            var fixedRect=Get<RectTransform>(t,"fixedRect");Near(fixedRect.rect.width,1200,"visual width 1.2");Near(fixedRect.rect.height,120,"visual height 1.2");
            Near(fixedRect.GetComponent<Image>().color.r,.17f,"dark grey tint");Check(!f.ground.GetComponent<Image>().enabled,"old root image hidden");
        });
        Run("terrain loop covers complete viewport through long scroll and resize",()=>{
            foreach(float scale in new[]{.2f,1f,2f}) {
                var f=new Field(scale);RectTransform[] decor;var t=TerrainField(f,out decor);
                foreach(float step in new[]{0f,1f,999f,1f,8000f,-25f}) {f.space.PanEnvironment(step);AssertCoverage(t,f);}
                f.ground.sizeDelta=new Vector2(2000,100);t.Scroll(0);AssertCoverage(t,f);
                f.ground.sizeDelta=new Vector2(200,100);t.Scroll(0);AssertCoverage(t,f);
                var tiles=Get<List<RectTransform>>(t,"tiles");foreach(var tile in tiles)Near(f.space.Position(tile).y,65,"vertical position preserved");
            }
        });
        Run("all seven grass placeholders receive sprites and separate random positions",()=>{
            var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);Check(t.DecorationCount==7,"all decorations included");
            var xs=new HashSet<int>();foreach(var d in decor) {
                Check(d.GetComponent<Image>().sprite!=null&&d.gameObject.activeInHierarchy,"visible image assigned");
                xs.Add((int)f.space.Position(d).x);Check(f.space.IsVisible(d),"initially in view");
                Check(d.rect.width>10,"placeholder size repaired");
            }
            Check(xs.Count==7,"not stacked at one x");
            f.space.PanEnvironment(10000);xs.Clear();foreach(var d in decor)xs.Add((int)f.space.Position(d).x);
            Check(xs.Count==7,"long frame does not restack decorations");
        });
        Run("difficulty changes scrolling terrain tint while fixed layer remains grey",()=>{
            var f=new Field();RectTransform[] decor;var t=TerrainField(f,out decor);
            f.space.SetTerrainTint(new Color(.3f,.8f,.2f));
            foreach(var tile in Get<List<RectTransform>>(t,"tiles"))Near(tile.GetComponent<Image>().color.g,.8f,"loop tinted");
            var c=Get<RectTransform>(t,"fixedRect").GetComponent<Image>().color;
            Near(c.r,.17f,"grey unchanged");Near(c.g,.17f,"grey unchanged green");Near(c.b,.17f,"grey unchanged blue");
        });
    }
}
