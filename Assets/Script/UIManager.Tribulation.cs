using UnityEngine;
using UnityEngine.UI;
using TMPro;
public partial class UIManager
{
    RectTransform trialButton;TextMeshProUGUI trialHud;
    void RefreshTribulationUI()
    {
        var gm=GameForUI();if(gm==null)return;
        if(trialHud==null&&hudCanvas!=null){var panel=new GameObject("HeavenlyGateStatus",typeof(RectTransform),typeof(Image));panel.transform.SetParent(hudCanvas,false);panel.GetComponent<Image>().color=new Color(.035f,.08f,.11f,.95f);panel.GetComponent<Image>().raycastTarget=false;
            var label=new GameObject("HeavenlyGateStatusText",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(panel.transform,false);trialHud=label.GetComponent<TextMeshProUGUI>();trialHud.fontSize=13;trialHud.enableAutoSizing=true;trialHud.fontSizeMin=11;trialHud.fontSizeMax=13;trialHud.raycastTarget=false;trialHud.alignment=TextAlignmentOptions.Center;
        }
        if(trialHud!=null){var rect=(RectTransform)trialHud.transform.parent;PlaceUI(rect,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-(Mathf.Max(hudLayout.heroCardHeight,HudW3View.PrimaryCardHeight)+hudLayout.margin+4)),new Vector2(Mathf.Clamp(Screen.width-200,160,460),68));PlaceUI(trialHud.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(rect.sizeDelta.x-12,62));
            bool show=gm.HasTribulationLiability;bool characterOpen=damagePanel!=null&&damagePanel.activeInHierarchy;rect.gameObject.SetActive(!characterOpen&&(show||gm.ShowTribulationNotice));var combat=CombatManager.Instance;
            trialHud.text=show?(gm.IsDevelopmentTrial?"[DEV 0.1.5] ":"[FIXTURE] ")+(combat!=null?combat.HeavenlyGateName:"Thiên Quan")+"\nThiên Quan: "+(combat!=null?combat.HeavenlyGateHP+"/"+combat.HeavenlyGateMaxHP:"—")+" HP · Hero: "+(combat!=null?combat.CurrentHeroHP+"/"+combat.MaxHeroHP:"—")+" HP\n"+(gm.TribulationFighting?(gm.ShowTribulationNotice?gm.LastTribulationNotice:combat!=null&&combat.HeavenlyGateApproaching?"Đang tiến đến Thiên Quan":"Đang chiến đấu"):gm.LastTribulationNotice??"Chờ ghi kết quả"):(gm.ShowTribulationNotice?gm.LastTribulationNotice:"");
        }
        bool visible=CultivationVisible&&!gm.CanUpgradeSelectedCultivationSnapshot;
        if(visible&&trialButton==null)trialButton=SmallButton(progressionSummary.transform.parent,"CharacterTrialStart","Độ Kiếp",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(150,24),()=>{var game=GameForUI();if(game!=null)game.RequestTribulation();});
        if(trialButton!=null){trialButton.gameObject.SetActive(visible);PlaceUI(trialButton,Vector2.zero,Vector2.zero,new Vector2(148,6),new Vector2(Mathf.Max(120,ManagementWindowLayout.CombatPixels(Screen.width)-250),24));var label=trialButton.GetComponentInChildren<TextMeshProUGUI>(true);if(label!=null)label.text=gm.TribulationLockedReason==""?"Độ Kiếp":gm.CanRetryTribulationCommit?"Thử lưu kết quả":gm.HasTribulationLiability?"Đang Độ Kiếp":"Độ Kiếp · khóa";}
    }
}
