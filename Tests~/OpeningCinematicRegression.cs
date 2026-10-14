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
            bool hasSky=false,hasCloud=false,hasLeft=false,hasRight=false;
            foreach (var obj in UnityEngine.Object.objects)
            {
                var go=obj as GameObject;
                if(go==null)continue;
                if(go.name=="01 SKY - full background")hasSky=true;
                if(go.name=="05 FRONT CLOUDS LEFT")hasCloud=true;
                if(go.name=="07 SPLASH LEFT")hasLeft=true;
                if(go.name=="08 SPLASH RIGHT")hasRight=true;
            }
            Check(hasSky&&hasCloud&&hasLeft&&hasRight,"replaceable named art layers");

            // Full-screen menu and art operate independently of combat transforms.
            ui.Layout(600f,338f);
            Check(ui.BeginGameplayTransition(),"transition can start");
            Check(!ui.BeginGameplayTransition(),"no overlapping transitions");
            Check(OpeningCinematicUI.ShowcaseCueSeconds>=25f &&
                  OpeningCinematicUI.ShowcaseCueSeconds<=30f,"cinematic art cue");
        });
    }
}
