using System;
using UnityEngine;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static UIManager ProgressionUi(out Field f,out GameManager gm)
    {
        f=WeaponField(out gm);
        var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;Call(ui,"Start");
        return ui;
    }
    static void ProgressionUiTests()
    {
        Run("progression DEVB visibility reads live configuration without mutating Hero",()=>{
            Field f;GameManager gm;var ui=ProgressionUi(out f,out gm);
            var h=gm.HeroData;string before=JsonUtility.ToJson(h,false);
            ui.OpenHeroStats();Check(Get<string>(ui,"detailSnapshot").Contains("Baseline Hero"),"baseline label");
            var dev=DevBalanceProfile.Defaults(false,f.space,150);dev.attack=1000;CombatBalance.SetDevProfile(false,dev);
            ui.RefreshCharacterProgression();Check(Get<string>(ui,"detailSnapshot").Contains("DEVB Hero đang override"),"live override label");
            ui.IsolatedValidationProfile=true;ui.RefreshCharacterProgression();Check(Get<string>(ui,"detailSnapshot").Contains("THỬ NGHIỆM CÔ LẬP"),"explicit verifier context");
            Check(JsonUtility.ToJson(h,false)==before,"visibility never changes Hero/save data");
            CombatBalance.ResetDevProfiles();ui.RefreshCharacterProgression();Check(ui.CharacterSourceStatus.Contains("Baseline Hero"),"live baseline after runtime fixture reset");
        });
    }
}
