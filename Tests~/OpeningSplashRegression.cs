using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// Logic-only checks of the dynamic asset library. Video codec playback,
// frame timing and crossfades still require Unity Play Mode on Windows.
partial class MotionRegression
{
    static void OpeningSplashRegressionTests()
    {
        Run("0.0.53 random splash photos, return-to-title reset",()=>{
            var catalog=new OpeningSplashCatalog{
                images=new[]{"OpeningSplashArt/heroA","OpeningSplashArt/heroB","OpeningSplashArt/heroC"},
                videos=new string[0]};
            Resources.Register("OpeningSplashCatalog",catalog);
            foreach(var name in catalog.images)Resources.Register(name,new Sprite());
            var game=new GameObject("Opening Showcase");
            var controller=game.AddComponent<OpeningSplashShowcase>();
            var left=new GameObject("Left",typeof(RectTransform),typeof(Image));
            var right=new GameObject("Right",typeof(RectTransform),typeof(Image));
            var leftCaption=new GameObject("Left Caption",typeof(RectTransform),typeof(TextMeshProUGUI));
            var rightCaption=new GameObject("Right Caption",typeof(RectTransform),typeof(TextMeshProUGUI));
            leftCaption.transform.SetParent(left.transform,false);
            rightCaption.transform.SetParent(right.transform,false);
            ((RectTransform)left.transform).sizeDelta=new Vector2(250,500);
            ((RectTransform)right.transform).sizeDelta=new Vector2(250,500);
            var color=new Color(.38f,.32f,.56f,.95f);
            left.GetComponent<Image>().color=color;
            right.GetComponent<Image>().color=new Color(.54f,.28f,.42f,.95f);
            controller.Configure((RectTransform)left.transform,(RectTransform)right.transform,
                left.GetComponent<Image>(),right.GetComponent<Image>(),
                leftCaption.GetComponent<TextMeshProUGUI>(),rightCaption.GetComponent<TextMeshProUGUI>());
            Check(controller.HasMedia,"catalog discovers three imported images");
            controller.Resize();
            controller.Tick(27f,.1f,1f);
            Check(!leftCaption.activeSelf && !rightCaption.activeSelf,"loaded media hides placeholders");
            Check(left.GetComponentsInChildren<Image>(true).Length>=3,"two crossfade layers created");
            for(int i=0;i<100;i++)controller.Tick(27f+i*.1f,.1f,1f);
            controller.ResetShowcase();
            Check(leftCaption.activeSelf && rightCaption.activeSelf,"reset restores labels");
            Near(left.GetComponent<Image>().color.r,color.r,"placeholder color restored");
            foreach(var name in catalog.images)Resources.Unregister(name);
            Resources.Unregister("OpeningSplashCatalog");
        });

        Run("0.0.53 one video decoder when both splash panels have MP4",()=>{
            var catalog=new OpeningSplashCatalog{
                images=new string[0],
                videos=new[]{"OpeningSplashArt/clipA","OpeningSplashArt/clipB"}};
            Resources.Register("OpeningSplashCatalog",catalog);
            foreach(var name in catalog.videos)Resources.Register(name,new VideoClip());
            var game=new GameObject("Opening Video Test");
            var library=game.AddComponent<OpeningSplashShowcase>();
            var a=new GameObject("Left",typeof(RectTransform),typeof(Image));
            var b=new GameObject("Right",typeof(RectTransform),typeof(Image));
            library.Configure((RectTransform)a.transform,(RectTransform)b.transform,
                a.GetComponent<Image>(),b.GetComponent<Image>(),null,null);
            library.Resize();
            library.Tick(27f,.02f,1f);
            Check(library.HasMedia,"catalog recognizes videos");
            Check(game.GetComponent<VideoPlayer>()!=null,"video player created lazily");
            Check(game.GetComponentsInChildren<VideoPlayer>(true).Length==1,"at most one video decoder");
            library.Deactivate();
            foreach(var name in catalog.videos)Resources.Unregister(name);
            Resources.Unregister("OpeningSplashCatalog");
        });
    }
}
