using UnityEngine;

// Logic-only regression; Windows pointer routing and audible output require
// explicit Unity Play Mode / Windows build testing.
partial class MotionRegression
{
    static void MusicVolumeRegressionTests()
    {
        Run("0.0.53 BGM volume clamps and survives settings reads", () =>
        {
            MapBgmController.SetVolume(.4f);
            Near(MapBgmController.CurrentVolume,.4f,"saved 40%");
            Near(PlayerPrefs.GetFloat("IdleGame.BgmVolume",0f),.4f,"PlayerPrefs updated");
            MapBgmController.SetVolume(2f);
            Near(MapBgmController.CurrentVolume,1f,"clamp upper");
            MapBgmController.SetVolume(-1f);
            Near(MapBgmController.CurrentVolume,0f,"clamp lower");
            MapBgmController.SetVolume(.6f);
        });

        Run("0.0.53 compact volume popup opens, changes volume, mutes", () =>
        {
            var owner = new GameObject("UI").AddComponent<UIManager>();
            var root = new GameObject("CombatUI", typeof(RectTransform));
            var panel = MusicVolumePanelUI.Install(owner,root.transform);
            Check(panel != null && !panel.IsOpen,"panel starts hidden");
            panel.Toggle();
            Check(panel.IsOpen,"panel opens");
            var controls = root.GetComponentsInChildren<CustomInteractable>(true);
            CustomInteractable plus=null,minus=null,mute=null;
            foreach (var c in controls)
            {
                if (c.gameObject.name == "VolumePlus") plus=c;
                if (c.gameObject.name == "VolumeMinus") minus=c;
                if (c.gameObject.name == "VolumeMute") mute=c;
            }
            Check(plus != null && minus != null && mute != null,"all controls bound");
            MapBgmController.SetVolume(.6f);
            plus.onClickEvent.Invoke();
            Near(MapBgmController.CurrentVolume,.7f,"+10%");
            minus.onClickEvent.Invoke();
            Near(MapBgmController.CurrentVolume,.6f,"-10%");
            mute.onClickEvent.Invoke();
            Near(MapBgmController.CurrentVolume,0f,"mute");
            mute.onClickEvent.Invoke();
            Near(MapBgmController.CurrentVolume,.6f,"restore previous");
            panel.SetVisibleInGameplay(false);
            Check(!panel.IsOpen,"panel closes when gameplay HUD hidden");
            MapBgmController.SetVolume(.6f);
        });
    }
}
