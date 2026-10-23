using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    [TestFixture]
    public sealed class TerrainSeamNativeTests
    {
        const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static Type Runtime(string name)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var type=assembly.GetType(name,false);if(type!=null)return type;
            }
            throw new TypeLoadException(name);
        }
        static object Get(object target,string name)
        {
            var field=target.GetType().GetField(name,Members);
            return field!=null?field.GetValue(target):target.GetType().GetProperty(name,Members).GetValue(target,null);
        }
        static void Set(object target,string name,object value)
        {
            var field=target.GetType().GetField(name,Members);
            if(field!=null)field.SetValue(target,value);
            else target.GetType().GetProperty(name,Members).SetValue(target,value,null);
        }
        static object Call(object target,string name,params object[] args)
        {
            return target.GetType().GetMethod(name,Members).Invoke(target,args);
        }
        static Vector2 Edges(RectTransform area,RectTransform tile)
        {
            var corners=new Vector3[4];tile.GetWorldCorners(corners);
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            foreach(var corner in corners) {
                float x=area.InverseTransformPoint(corner).x;min=Mathf.Min(min,x);max=Mathf.Max(max,x);
            }
            return new Vector2(min,max);
        }
        static void Verify(Component terrain,RectTransform area)
        {
            foreach(string name in new[]{"farTiles","distantTiles","tiles"}) {
                var ordered=new List<RectTransform>();foreach(RectTransform tile in (IEnumerable)Get(terrain,name))ordered.Add(tile);
                ordered.Sort((a,b)=>Edges(area,a).x.CompareTo(Edges(area,b).x));
                for(int i=0;i<ordered.Count;i++) {
                    var tile=ordered[i];var image=tile.GetComponent(Runtime("UnityEngine.UI.Image"));
                    var rect=(Rect)Call(image,"GetPixelAdjustedRect");
                    Assert.That(tile.parent,Is.EqualTo(area));
                    Assert.That(tile.anchorMin,Is.EqualTo(area.pivot));Assert.That(tile.anchorMax,Is.EqualTo(area.pivot));
                    Assert.That(tile.pivot.x,Is.EqualTo(0f));Assert.That((bool)Get(image,"preserveAspect"),Is.False);
                    var bounds=Edges(area,tile);
                    // Numerical tolerance is only for native matrix round trips;
                    // the production placement contains no epsilon or overlap.
                    Assert.That(bounds.y-bounds.x,Is.EqualTo(rect.width*Mathf.Abs(tile.localScale.x)).Within(.001f));
                    if(i>0)Assert.That(bounds.x,Is.EqualTo(Edges(area,ordered[i-1]).y).Within(.001f));
                }
                Assert.That(Edges(area,ordered[0]).x,Is.LessThanOrEqualTo(area.rect.xMin));
                Assert.That(Edges(area,ordered[ordered.Count-1]).y,Is.GreaterThanOrEqualTo(area.rect.xMax));
            }
        }
        [TestCase(600f,1f)]
        [TestCase(800f,1f)]
        [TestCase(1150f,1f)]
        [TestCase(600f,1.37f)]
        [TestCase(800f,1.37f)]
        [TestCase(1150f,1.37f)]
        public void NativeImageEdges_StayAdjacentAcrossRecycleAndResize(float projectionWidth,float horizontalScale)
        {
            Assert.That(Application.isPlaying,Is.True,"Run in Play Mode.");
            foreach(string name in new[]{"SaveManager","GameManager","CombatManager","EnvironmentManager","HeroController","MonsterController"})
                Assert.That(UnityEngine.Object.FindObjectsByType(Runtime(name),FindObjectsInactive.Include,FindObjectsSortMode.None),
                    Is.Empty,"Use a clean scene. This test never changes existing gameplay managers.");
            GameObject root=null,environment=null;
            try {
                var imageType=Runtime("UnityEngine.UI.Image");
                root=new GameObject("Owned terrain seam Canvas",typeof(RectTransform),typeof(Canvas));root.SetActive(false);
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.pixelPerfect=false;
                var area=(RectTransform)new GameObject("Owned BattleArea",typeof(RectTransform),imageType).transform;
                area.SetParent(root.transform,false);area.anchorMin=area.anchorMax=new Vector2(.5f,0);area.pivot=new Vector2(.5f,0);area.sizeDelta=new Vector2(1920,100);
                area.anchoredPosition=Vector2.zero;
                var sprite=Resources.Load<Sprite>("Environment/Plains/Ground");Assert.That(sprite,Is.Not.Null);
                Set(area.GetComponent(imageType),"sprite",sprite);
                var grass=(RectTransform)new GameObject("Grass1",typeof(RectTransform),imageType).transform;
                grass.SetParent(area,false);grass.sizeDelta=new Vector2(600,130);grass.anchoredPosition=new Vector2(0,65);Set(grass.GetComponent(imageType),"sprite",sprite);
                environment=new GameObject("Owned inactive terrain Environment");environment.SetActive(false);
                var space=environment.AddComponent(Runtime("EnvironmentManager"));
                // Never activate EnvironmentManager: Awake cannot install or
                // replace a global environment/canvas. No other manager exists.
                Set(space,"battleArea",area);Set(space,"groundDetails",new[]{grass});
                var terrain=(Component)Runtime("GroundPresentation").GetMethod("Install",BindingFlags.Public|BindingFlags.Static)
                    .Invoke(null,new object[]{space,new[]{grass}});
                Assert.That(terrain,Is.Not.Null);((Behaviour)terrain).enabled=false;
#if UNITY_EDITOR
                Set(terrain,"traceTileSeams",false);
#endif
                root.SetActive(true);canvas.scaleFactor=projectionWidth/1920f;Canvas.ForceUpdateCanvases();
                foreach(string name in new[]{"farTiles","distantTiles","tiles"})
                    foreach(RectTransform tile in (IEnumerable)Get(terrain,name))tile.localScale=new Vector3(horizontalScale,1,1);
                Call(terrain,"Scroll",0f);Canvas.ForceUpdateCanvases();Verify(terrain,area);
                foreach(float amount in new[]{-1f,2f,-300f,800f,-20000f,50000f,-1f}) {
                    Call(terrain,"Scroll",amount);Canvas.ForceUpdateCanvases();Verify(terrain,area);
                }
                area.sizeDelta=new Vector2(1280,100);Call(terrain,"Scroll",0f);Canvas.ForceUpdateCanvases();Verify(terrain,area);
                Assert.That(environment.activeSelf,Is.False,"Environment.Awake stayed blocked.");
                Assert.That(area.anchoredPosition,Is.EqualTo(Vector2.zero));
                Assert.That(area.sizeDelta.y,Is.EqualTo(100f));
            }
            finally {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);
                if(environment!=null)UnityEngine.Object.DestroyImmediate(environment);
            }
        }
    }
}
