using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tests structural opening flow with the project's Unity doubles.
// Rendering, canvas draw order and Windows native hit testing still
// require Unity Play Mode / Windows build verification.
partial class MotionRegression
{
    static void OpeningCinematicRegressionTests()
    {
        Run("0.0.53 black scene reveal and delayed main menu timing", () =>
        {
            Near(OpeningCinematicUI.BlackCurtainAlphaAt(0f),1f,
                "starts entirely black");
            Check(OpeningCinematicUI.BlackCurtainAlphaAt(.9f) > 0f &&
                  OpeningCinematicUI.BlackCurtainAlphaAt(.9f) < 1f,
                "black fade interpolates smoothly");
            Near(OpeningCinematicUI.BlackCurtainAlphaAt(
                OpeningCinematicUI.BlackFadeSeconds),0f,
                "entire scene revealed after 1.8 seconds");
            float menuStart=OpeningCinematicUI.BlackFadeSeconds+
                OpeningCinematicUI.MenuDelayAfterSceneSeconds;
            Near(OpeningCinematicUI.MenuAlphaAt(menuStart-.01f),0f,
                "menu hidden throughout the 3-second hold");
            Near(OpeningCinematicUI.MenuAlphaAt(menuStart),0f,
                "menu begins only after 3s hold");
            Check(OpeningCinematicUI.MenuAlphaAt(menuStart+.75f) > 0f,
                "menu is gradually becoming visible");
            Near(OpeningCinematicUI.MenuAlphaAt(menuStart+
                OpeningCinematicUI.MenuRevealSeconds),1f,
                "menu fully visible after fade");
            Check(OpeningCinematicUI.ShowcaseCueSeconds==27f,
                "splash timing remains unchanged");
        });

        Run("0.0.53 opening creates four controls and art placeholders", () =>
        {
            var gm=new GameObject("Opening Test Game").AddComponent<GameManager>();
            gm.preGameUI=new GameObject("PreGame",typeof(RectTransform),typeof(Image));
            var parent=new GameObject("UI Root",typeof(RectTransform));
            gm.preGameUI.transform.SetParent(parent.transform,false);

            var template=new GameObject("Template",typeof(RectTransform),typeof(Image),typeof(Button));
            template.transform.SetParent(gm.preGameUI.transform,false);
            var label=new GameObject("Template Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            label.transform.SetParent(template.transform,false);

            var ui=StartMenuUI.Install(gm);
            Check(ui!=null&&ui.IsCinematic,"cinematic menu installed");
            Check(gm.confirmationPopup!=null,"confirmation retained");
            Check(gm.preGameUI.GetComponent<CanvasGroup>()!=null,"root fade present");

            var startButtons=gm.preGameUI.GetComponentsInChildren<Button>(true);
            bool cont=false,fresh=false,settings=false,exit=false;
            foreach(var button in startButtons)
            {
                switch(button.gameObject.name)
                {
                    case "Opening - Tiếp tục":cont=true;break;
                    case "Opening - Chơi mới":fresh=true;break;
                    case "Opening - Cài đặt":settings=true;break;
                    case "Opening - Thoát":exit=true;break;
                }
            }
            Check(cont&&fresh&&settings&&exit,"four menu actions");
            var menuGroup=gm.preGameUI.GetComponent<CanvasGroup>();
            Check(!menuGroup.interactable && !menuGroup.blocksRaycasts,
                "menu is not clickable before fade in completes");
            bool foundCurtain=false;
            foreach(var obj in UnityEngine.Object.objects)
            {
                var go=obj as GameObject;
                if(go!=null && go.name=="Opening Black Curtain - Full Window")
                {
                    foundCurtain=true;
                    Near(go.GetComponent<Image>().color.a,1f,
                        "opening curtain starts opaque black");
                }
            }
            Check(foundCurtain,"full-window black curtain exists");
            foreach(var button in startButtons)
            {
                if(!button.gameObject.name.StartsWith("Opening - "))continue;
                var text=button.GetComponentInChildren<TextMeshProUGUI>(true);
                Check(text!=null && text.color.r>.99f && text.color.g>.99f &&
                      text.color.b>.99f,"all opening buttons use white text");
            }
            bool hasSky=false,hasCloud=false,hasBackground=false;
            OpeningHeroSplashVideo left=null,right=null;
            foreach (var obj in UnityEngine.Object.objects)
            {
                var go=obj as GameObject;
                if(go==null)continue;
                if(go.name=="01 SKY - full background")hasSky=true;
                if(go.name=="05 FRONT CLOUDS LEFT")hasCloud=true;
                if(go.name=="00 VIDEO BACKGROUND - full window")hasBackground=true;
                if(go.name=="07 SPLASH LEFT - OVERSCAN")
                    left=go.GetComponent<OpeningHeroSplashVideo>();
                if(go.name=="08 SPLASH RIGHT - OVERSCAN")
                    right=go.GetComponent<OpeningHeroSplashVideo>();
            }
            Check(hasSky&&hasCloud&&hasBackground&&left!=null&&right!=null,
                "two fixed side splash panels and background video");
            ui.Layout(600f,338f);
            var leftBox=(RectTransform)left.transform;
            var rightBox=(RectTransform)right.transform;
            Check(leftBox.anchoredPosition.x<0f && rightBox.anchoredPosition.x>0f,
                "splash frames remain on opposite sides");
            // The red-marked target is the LOWER portion of the window:
            // no overflow above its top line or below the game bottom.
            foreach(float w in new[]{600f,800f,1150f})
            {
                float h=Mathf.RoundToInt(w*9f/16f);
                left.Layout(w,h);
                right.Layout(w,h);
                foreach(var panel in new[]{leftBox,rightBox})
                {
                    float bottom=panel.anchoredPosition.y-panel.sizeDelta.y*.5f;
                    float top=panel.anchoredPosition.y+panel.sizeDelta.y*.5f;
                    Near(bottom,-h*.5f,"splash bottom aligned to viewport",.02f);
                    Near(top,h*(OpeningHeroSplashVideo.FrameHeightFraction-.5f),
                        "splash top remains below upper quarter",.02f);
                    Near(panel.sizeDelta.y,h*OpeningHeroSplashVideo.FrameHeightFraction,
                        "splash uses only 76% of viewport height",.02f);
                }
                float leftInner=leftBox.anchoredPosition.x+leftBox.sizeDelta.x*.5f;
                float rightInner=rightBox.anchoredPosition.x-rightBox.sizeDelta.x*.5f;
                Check(leftInner<-w*.2f && rightInner>w*.2f,
                    "inner edges stay clear of central menu");
                Check(leftBox.anchoredPosition.x-leftBox.sizeDelta.x*.5f<-w*.5f &&
                      rightBox.anchoredPosition.x+rightBox.sizeDelta.x*.5f>w*.5f,
                    "overscan is horizontal only");
            }
            Check(leftBox.sizeDelta.x==rightBox.sizeDelta.x,
                "the two panels have equal width");
            var leftPos=leftBox.anchoredPosition;
            var rightPos=rightBox.anchoredPosition;
            var alphaLeft=left.GetComponent<CanvasGroup>();
            var alphaRight=right.GetComponent<CanvasGroup>();
            left.Tick(26f,null);right.Tick(26f,null);
            Near(alphaLeft.alpha,0f,"both sides hidden before cue");
            Near(alphaRight.alpha,0f,"right side hidden before cue");
            left.Tick(27f,null);right.Tick(27f,left.CurrentPath);
            Near(alphaLeft.alpha,0f,"left fade starts at 27s");
            left.Tick(27.7f,null);right.Tick(27.7f,left.CurrentPath);
            Check(alphaLeft.alpha>0f && alphaLeft.alpha<1f,
                "initial fade is in progress");
            left.Tick(29f,null);right.Tick(29f,left.CurrentPath);
            Near(alphaLeft.alpha,1f,"fade reaches full opacity");
            Near(alphaRight.alpha,1f,"right side fade reaches full opacity");
            Near(leftBox.anchoredPosition.x,leftPos.x,"left frame does not pan");
            Near(rightBox.anchoredPosition.x,rightPos.x,"right frame does not pan");
            Near(leftBox.anchoredPosition.y,leftPos.y,"left frame stays vertically fixed");
            Check(left.GetComponent<RectMask2D>()!=null &&
                  right.GetComponent<RectMask2D>()!=null,
                "oversized frames crop overflow artwork to their own bounds");
            left.ResetToTitle();right.ResetToTitle();
            Near(alphaLeft.alpha,0f,"return to title resets both splash cues");

            // Full-screen menu and art operate independently of combat transforms.
            ui.Layout(600f,338f);
            Check(ui.BeginGameplayTransition(),"transition can start");
            Check(!ui.BeginGameplayTransition(),"no overlapping transitions");
            Check(OpeningCinematicUI.ShowcaseCueSeconds>=25f &&
                  OpeningCinematicUI.ShowcaseCueSeconds<=30f,"cinematic art cue");
        });

        Run("0.0.53 splash draws two distinct media from the drop-in folder", () =>
        {
            string previous=Application.streamingAssetsPath;
            string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "opening-splash-test-"+Guid.NewGuid().ToString("N"));
            string dir=System.IO.Path.Combine(temp,"OpeningSplashArt");
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,"a.png"),new byte[]{1,2,3});
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,"b.jpg"),new byte[]{4,5,6});
                Application.streamingAssetsPath=temp;
                var parent=new GameObject("Two Splash Layers",typeof(RectTransform));
                ((RectTransform)parent.transform).sizeDelta=new Vector2(800f,450f);
                var left=OpeningHeroSplashVideo.Install((RectTransform)parent.transform,null,true);
                var right=OpeningHeroSplashVideo.Install((RectTransform)parent.transform,null,false);
                left.Tick(27f,null);
                right.Tick(27f,left.CurrentPath);
                Check(!string.IsNullOrEmpty(left.CurrentPath) &&
                      !string.IsNullOrEmpty(right.CurrentPath),"both panels select media");
                Check(left.CurrentPath!=right.CurrentPath,"avoid duplicate choices");
                string previousLeft=left.CurrentPath;
                left.Tick(100f,null);
                Check(left.CurrentPath==previousLeft,"no automatic slideshow or scene cuts");
                left.ResetToTitle();right.ResetToTitle();
                Check(left.CurrentPath==null && right.CurrentPath==null,
                    "return to menu permits new choices");
            }
            finally
            {
                Application.streamingAssetsPath=previous;
                if(System.IO.Directory.Exists(temp))System.IO.Directory.Delete(temp,true);
            }
        });

        Run("0.0.53 cloud feather can be configured without changing source assets",()=>{
            Resources.Register("OpeningSoftEdge",new Shader());
            try
            {
                var go=new GameObject("Feather Test",typeof(RectTransform),typeof(UnityEngine.UI.Image));
                ((RectTransform)go.transform).sizeDelta=new Vector2(400f,210f);
                var image=go.GetComponent<UnityEngine.UI.Image>();
                var fade=go.AddComponent<OpeningEdgeFeather>();
                fade.Configure(image,48f);
                Check(image.material!=null,"feather material assigned to Image");
                fade.Refresh();
            }
            finally{Resources.Unregister("OpeningSoftEdge");}
        });
    }
}
