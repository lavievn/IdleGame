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
            bool hasSky=false,hasCloud=false,hasVideoBackground=false,hasFixedSplash=false;
            foreach (var obj in UnityEngine.Object.objects)
            {
                var go=obj as GameObject;
                if(go==null)continue;
                if(go.name=="01 SKY - full background")hasSky=true;
                if(go.name=="05 FRONT CLOUDS LEFT")hasCloud=true;
                if(go.name=="00 VIDEO BACKGROUND - full window")hasVideoBackground=true;
                if(go.name=="07 SPLASH VIDEO - FIXED NO PAN")hasFixedSplash=true;
            }
            Check(hasSky&&hasCloud&&hasVideoBackground&&hasFixedSplash,
                "fixed splash and bottom background video layers");
            OpeningHeroSplashVideo fixedVideo = null;
            foreach(var obj in UnityEngine.Object.objects)
            {
                var go=obj as GameObject;
                if(go!=null && go.name=="07 SPLASH VIDEO - FIXED NO PAN")
                    fixedVideo=go.GetComponent<OpeningHeroSplashVideo>();
            }
            Check(fixedVideo!=null,"single fixed splash video script installed");
            var group=fixedVideo.GetComponent<CanvasGroup>();
            var frame=(RectTransform)fixedVideo.transform;
            Vector2 position=frame.anchoredPosition;
            fixedVideo.Tick(26f);
            Near(group.alpha,0f,"splash hidden before cue");
            fixedVideo.Tick(27f);
            Near(group.alpha,0f,"splash begins fade at 27s");
            fixedVideo.Tick(27.7f);
            Check(group.alpha>0f && group.alpha<1f,"one initial fade in progress");
            fixedVideo.Tick(28.5f);
            Near(group.alpha,1f,"initial fade finished");
            Near(frame.anchoredPosition.x,position.x,"no horizontal pan");
            Near(frame.anchoredPosition.y,position.y,"no vertical pan");
            fixedVideo.ResetToTitle();
            Near(group.alpha,0f,"return to menu resets splash cue");

            // Full-screen menu and art operate independently of combat transforms.
            ui.Layout(600f,338f);
            Check(ui.BeginGameplayTransition(),"transition can start");
            Check(!ui.BeginGameplayTransition(),"no overlapping transitions");
            Check(OpeningCinematicUI.ShowcaseCueSeconds>=25f &&
                  OpeningCinematicUI.ShowcaseCueSeconds<=30f,"cinematic art cue");
        });
    }
}
