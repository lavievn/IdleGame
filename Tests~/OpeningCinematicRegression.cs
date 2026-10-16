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
                if(go.name.StartsWith("01 SKY - full background ["))hasSky=true;
                if(go.name.StartsWith("05 FRONT CLOUDS LEFT ["))hasCloud=true;
                if(go.name=="00 VIDEO BACKGROUND - full window")hasBackground=true;
                if(go.name.StartsWith("07 SPLASH LEFT - OVERSCAN ["))
                    left=go.GetComponent<OpeningHeroSplashVideo>();
                if(go.name.StartsWith("08 SPLASH RIGHT - OVERSCAN ["))
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

        Run("0.0.54 title confirmation remains clickable after direct load and menu replay", () =>
        {
            new Field();
            var gm = new GameObject("Title Confirmation GM").AddComponent<GameManager>();
            gm.preGameUI = new GameObject("Title Confirmation Root",
                typeof(RectTransform), typeof(Image));
            var canvas = new GameObject("Title Confirmation Canvas",
                typeof(RectTransform));
            gm.preGameUI.transform.SetParent(canvas.transform,false);
            var template = new GameObject("Title Confirmation Template",
                typeof(RectTransform), typeof(Image), typeof(Button));
            template.transform.SetParent(gm.preGameUI.transform,false);
            var label = new GameObject("Title Confirmation Label",
                typeof(RectTransform),typeof(TextMeshProUGUI));
            label.transform.SetParent(template.transform,false);
            var menu = StartMenuUI.Install(gm);
            Check(menu!=null && gm.confirmationPopup!=null,
                "title and independent confirmation popup created");

            menu.CompleteGameplayImmediately(); // manual load skips the transition
            menu.RestoreMenu(); // return-to-title restarts fade from zero
            Check(!gm.preGameUI.GetComponent<CanvasGroup>().blocksRaycasts,
                "opening menu still fading; menu input initially gated");
            gm.OnNewGameClicked();
            Check(gm.HasPendingConfirmation && gm.confirmationPopup.activeSelf,
                "modal appears even before title fade finishes");
            CustomInteractable no=null;
            foreach(var item in gm.confirmationPopup
                .GetComponentsInChildren<CustomInteractable>(true))
                if(item.gameObject.name=="Opening - Không")no=item;
            Check(no!=null,"modal No control exists");
            Time.frameCount++; // distinct physical click frame
            no.onClickEvent.Invoke();
            Check(!gm.HasPendingConfirmation && !gm.confirmationPopup.activeSelf,
                "No closes modal despite menuFade and entry state");
        });

        Run("0.0.54 direct manual load cannot leave the opening curtain over combat", () =>
        {
            new Field(); // Clear prior stub scene and open a fresh UI hierarchy.
            var gm = new GameObject("Direct Load GM").AddComponent<GameManager>();
            gm.preGameUI = new GameObject("Direct Load Menu",
                typeof(RectTransform), typeof(Image));
            var canvas = new GameObject("Direct Load Canvas",typeof(RectTransform));
            gm.preGameUI.transform.SetParent(canvas.transform,false);
            var template = new GameObject("Direct Load Template",
                typeof(RectTransform),typeof(Image),typeof(Button));
            template.transform.SetParent(gm.preGameUI.transform,false);
            var caption = new GameObject("Direct Load Caption",
                typeof(RectTransform),typeof(TextMeshProUGUI));
            caption.transform.SetParent(template.transform,false);
            var menu = StartMenuUI.Install(gm);
            Check(menu != null && menu.IsCinematic,"opening installed");

            GameObject curtain=null, backdrop=null;
            foreach(var o in UnityEngine.Object.objects)
            {
                var go = o as GameObject;
                if(go==null)continue;
                if(go.name=="Opening Black Curtain - Full Window")curtain=go;
                if(go.name=="Opening Cinematic - Placeholder Layers")backdrop=go;
            }
            Check(curtain!=null && backdrop!=null,"opening siblings created");
            menu.RestoreMenu();
            Check(curtain.activeSelf && backdrop.activeSelf,
                "menu restoration enables both opening layers");
            menu.CompleteGameplayImmediately();
            Check(!curtain.activeSelf && !backdrop.activeSelf,
                "manual load removes curtain and overlay without entry animation");
            Check(!gm.preGameUI.GetComponent<CanvasGroup>().blocksRaycasts,
                "hidden title does not intercept gameplay clicks");
            menu.RestoreMenu();
            Check(curtain.activeSelf && backdrop.activeSelf,
                "returning to title re-enables cinematic layers");
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
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,"L_a.png"),new byte[]{1,2,3});
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,"R_b.jpg"),new byte[]{4,5,6});
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,"ignore.png"),new byte[]{7,8,9});
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,"L_extra.mp4"),new byte[]{10,11,12});
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
                Check(System.IO.Path.GetFileName(left.CurrentPath).StartsWith("L",
                    StringComparison.OrdinalIgnoreCase),"left side only loads L prefix");
                Check(System.IO.Path.GetFileName(right.CurrentPath).StartsWith("R",
                    StringComparison.OrdinalIgnoreCase),"right side only loads R prefix");
                string previousLeft=left.CurrentPath;
                left.Tick(100f,null);
                Check(left.CurrentPath==previousLeft,"no automatic slideshow or scene cuts");
                left.ResetToTitle();right.ResetToTitle();
                Check(left.CurrentPath==null && right.CurrentPath==null,
                    "return to menu permits new choices");
                // Strict sides do not fall back to unprefixed legacy files.
                System.IO.File.Delete(System.IO.Path.Combine(dir,"R_b.jpg"));
                right.Tick(27f,null);
                Check(right.CurrentPath==null,
                    "missing R file keeps placeholder, never loads L or unprefixed");
                right.ResetToTitle();
            }
            finally
            {
                Application.streamingAssetsPath=previous;
                if(System.IO.Directory.Exists(temp))System.IO.Directory.Delete(temp,true);
            }
        });

        Run("0.0.53 Play Mode drag captures and restores persisted positions",()=>{
            var theme=new OpeningArtTheme();
            Resources.Register("OpeningArtTheme",theme);
            try
            {
                var gm=new GameObject("Layout Edit GM").AddComponent<GameManager>();
                gm.preGameUI=new GameObject("Layout Edit Menu",
                    typeof(RectTransform),typeof(Image));
                var uiRoot=new GameObject("Layout Edit Canvas",typeof(RectTransform));
                gm.preGameUI.transform.SetParent(uiRoot.transform,false);
                var template=new GameObject("Edit Template",
                    typeof(RectTransform),typeof(Image),typeof(Button));
                template.transform.SetParent(gm.preGameUI.transform,false);
                var label=new GameObject("Edit Label",
                    typeof(RectTransform),typeof(TextMeshProUGUI));
                label.transform.SetParent(template.transform,false);

                var ui=StartMenuUI.Install(gm);
                OpeningCinematicUI opening=null;
                foreach(var obj in UnityEngine.Object.objects)
                    if(obj is OpeningCinematicUI)opening=(OpeningCinematicUI)obj;
                Check(ui!=null&&opening!=null,"opening installed for layout editor");
                var before=opening.GetLayoutRects();
                // Simulate previously hand-adjusted positions before opening
                // the edit tool; opening editing must never snap them back.
                before[1].anchoredPosition=new Vector2(23f,-31f);
                before[6].anchoredPosition=new Vector2(-333f,-28f);
                before[8].anchoredPosition=new Vector2(-12f,143f);
                Check(opening.SetLayoutEditing(true),"edit mode can start");
                Near(before[1].anchoredPosition.x,23f,
                    "edit toggle preserves the previously adjusted mountain X");
                Near(before[1].anchoredPosition.y,-31f,
                    "edit toggle preserves the previously adjusted mountain Y");
                Near(before[6].anchoredPosition.x,-333f,
                    "edit toggle preserves previously adjusted splash position");
                Near(before[8].anchoredPosition.y,143f,
                    "edit toggle preserves existing logo position");
                var rects=opening.GetLayoutRects();
                Check(rects.Length==OpeningCinematicUI.LayoutKeys.Length,
                    "all 13 elements are editable");
                string problem;
                Check(opening.ValidateLayoutForSave(out problem),
                    "aligned opening canvases are safe for saving");
                var outer=(RectTransform)gm.preGameUI.transform;
                // Simulate UIManager.Start moving the title to another HUD
                // AFTER the scenery was built. All layers must follow it.
                var movedCanvas=new GameObject("Runtime HUD Reparent",
                    typeof(RectTransform));
                outer.SetParent(movedCanvas.transform,false);
                opening.EnsureOpeningSiblingsParent();
                Check(opening.transform.parent==outer.parent,
                    "opening backdrop is sibling of menu after HUD reparent");
                Check(opening.ValidateLayoutForSave(out problem),
                    "HUD reparent keeps save coordinate space valid");
                var savedParentPosition=outer.anchoredPosition;
                outer.anchoredPosition=new Vector2(400f,0f);
                Check(!opening.ValidateLayoutForSave(out problem),
                    "an accidentally shifted menu root blocks unsafe saving");
                Check(opening.CaptureLayout()==null,
                    "invalid coordinate spaces cannot be snapshotted");
                outer.anchoredPosition=savedParentPosition;
                Check(opening.ValidateLayoutForSave(out problem),
                    "restored root permits saving again");
                var sky=rects[0];
                var logo=rects[8];
                var originalSize=sky.sizeDelta;
                sky.anchoredPosition=new Vector2(41f,-24f);
                sky.sizeDelta=new Vector2(543f,234f);
                logo.anchoredPosition=new Vector2(-18f,122f);
                opening.Layout(); // UIManager may call Layout repeatedly.
                Near(sky.anchoredPosition.x,41f,
                    "regular layout calls do not undo manual Play Mode drag");
                Near(sky.sizeDelta.x,543f,
                    "regular layout calls do not undo Play Mode sizing");
                var snapshot=opening.CaptureLayout();
                Check(snapshot!=null&&snapshot.Length==13,
                    "capture all modified rects");
                Near(snapshot[0].position.x,41f*800f/Mathf.Max(1f,originalSize.x/1.1f),
                    "capture normalized sky position",2f);
                theme.playModeLayouts=snapshot;
                sky.anchoredPosition=Vector2.zero;
                sky.sizeDelta=originalSize;
                opening.ReloadSavedLayoutForEditing();
                Near(sky.anchoredPosition.x,41f,
                    "reload saved X on reference viewport",2f);
                Near(sky.anchoredPosition.y,-24f,
                    "reload saved Y on reference viewport",2f);
                Near(sky.sizeDelta.x,543f,
                    "reload saved manual width",2f);
                Near(logo.anchoredPosition.x,-18f,
                    "menu title uses the same layout snapshot",2f);
                opening.SetLayoutEditing(false);
                Check(!opening.IsLayoutEditing,
                    "animation resumes after leaving edit mode");
            }
            finally { Resources.Unregister("OpeningArtTheme"); }
        });

        Run("0.0.53 inspector reference sizes retain responsive layout",()=>{
            var far=new Vector2(944f,279f);
            var original=OpeningCinematicUI.ResolveLayerSize(far,800f,450f,
                Vector2.zero);
            Near(original.x,944f,"reference width");
            Near(original.y,279f,"reference height");
            var scaled=OpeningCinematicUI.ResolveLayerSize(far,600f,337.5f,
                Vector2.zero);
            Near(scaled.x,708f,"width scales with window");
            Near(scaled.y,209.25f,"height scales with window");
            var fallback=OpeningCinematicUI.ResolveLayerSize(Vector2.zero,600f,
                337.5f,new Vector2(720f,210f));
            Near(fallback.x,720f,"zero width uses old layout fallback");
            Near(fallback.y,210f,"zero height uses old layout fallback");
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
