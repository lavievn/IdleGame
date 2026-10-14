using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Headless checks for the two-column settings controls.
// Audio output and native Windows drag routing still require Play Mode.
partial class MotionRegression
{
    static void OpeningSettingsRegressionTests()
    {
        Run("0.0.53 opening settings: three exclusive sizes, draggable BGM, reset",()=>{
            var previousUI=UIManager.Instance;
            UIManager.Instance=null;
            var gm=new GameObject("Settings Test Game").AddComponent<GameManager>();
            gm.preGameUI=new GameObject("PreGame",typeof(RectTransform),typeof(Image));
            var canvas=new GameObject("UI Root",typeof(RectTransform));
            gm.preGameUI.transform.SetParent(canvas.transform,false);
            var template=new GameObject("Template",typeof(RectTransform),typeof(Image),typeof(Button));
            template.transform.SetParent(gm.preGameUI.transform,false);
            var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            label.transform.SetParent(template.transform,false);
            var menu=StartMenuUI.Install(gm);
            Check(menu!=null,"settings menu installed");
            menu.Layout(800,450);
            Call(menu,"OpenSettings");
            var choices=Get<Button[]>(menu,"sizeButtons");
            Check(choices!=null&&choices.Length==3,"three radio size controls");
            var sizes=new[]{600,800,1150};
            for(int i=0;i<3;i++)
                Check(choices[i]!=null&&choices[i].GetComponentInChildren<TextMeshProUGUI>(true).text.Contains(sizes[i].ToString()),"size "+sizes[i]);
            var track=Get<RectTransform>(menu,"volumeTrack");
            Check(track!=null&&track.gameObject.activeInHierarchy,"volume track visible");
            MapBgmController.SetVolume(.6f);
            var bounds=track.rect;
            var first=track.TransformPoint(new Vector3(bounds.xMin+.75f*bounds.width,0,0));
            Check(menu.TryBeginVolumeDrag((Vector2)first),"pointer begins on the track");
            Near(MapBgmController.CurrentVolume,.75f,"75% by dragging",.025f);
            var second=track.TransformPoint(new Vector3(bounds.xMin+.25f*bounds.width,0,0));
            menu.DragVolume((Vector2)second);
            Near(MapBgmController.CurrentVolume,.25f,"25% while holding",.025f);
            menu.EndVolumeDrag();
            Call(menu,"RestoreDefaultSettings");
            Near(MapBgmController.CurrentVolume,.6f,"restores default BGM volume");
            Check(Get<int>(menu,"selectedWindowWidth")==800,"restores default width 800");
            Call(menu,"CloseSettings");
            Check(!Get<GameObject>(menu,"settingsPage").activeSelf,"back returns to main menu");
            MapBgmController.SetVolume(.6f);
            UIManager.Instance=previousUI;
        });
    }
}
