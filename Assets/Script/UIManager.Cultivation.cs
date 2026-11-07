using System.Globalization;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManager
{
    Image cultivationTrack,cultivationFill;RectTransform cultivationUpgrade;
    bool CultivationVisible {get {var g=GameForUI();return detailKind==1 && g!=null && g.Cultivation!=null;}}
    static string QiNumber(double n){return n.ToString("0.0#",CultureInfo.InvariantCulture);}
    string CultivationSummary(GameManager gm)
    {
        var b=gm.Cultivation;var p=b.Profile;var s=b.Current;var realm=p.Catalog.Evaluate(s.Foundation.ActualRealmId,s.Foundation.Amount);
        CultivationRealmDefinition definition;p.Catalog.TryGet(s.Foundation.ActualRealmId,out definition);
        string stage=realm.MinorStage==RealmMinorStage.SoKy?"Sơ kỳ":realm.MinorStage==RealmMinorStage.TrungKy?"Trung kỳ":realm.MinorStage==RealmMinorStage.HauKy?"Hậu kỳ":"Viên mãn · Bình cảnh";
        var h=gm.HeroData;string rates;
        if(p.Rules.CapacityCatalog!=null){var cap=b.Capacity;var flow=b.LastStepFlow;
            rates="\nTrần hấp thu: "+QiNumber(cap.Absorption)+"/s · thực: "+QiNumber(flow.AbsorptionPerSecond)+"/s"+
                "\nTrần luyện hóa: "+QiNumber(cap.Refinement)+"/s · thực: "+QiNumber(flow.RefinementPerSecond)+"/s";
        }else rates="\nHấp thu: "+QiNumber(p.EnvironmentalQiPerSecond(h.worldMapId))+"/s · Luyện hóa: ≤"+QiNumber(p.Rules.RefinementRate)+"/s";
        return (p.IsProduction?"Tu luyện · ":(p.SaveVersion==1?"[PLAYTEST v":"[THỬ NGHIỆM v")+p.Version+"] ")+"Level "+h.currentLevel+" · EXP: "+h.currentExp+"/"+h.expToNextLevel+
            "\n"+p.RealmName(s.Foundation.ActualRealmId)+" · "+stage+
            "\nTu vi căn cơ: "+QiNumber(s.Foundation.Amount)+" / "+QiNumber(definition.Thresholds[3])+"\n"+
            "\nLinh khí dự trữ: "+QiNumber(s.Reserve.Amount)+
            (p.IsProduction?" · Qi map: "+QiNumber(p.EnvironmentalQiPerSecond(h.worldMapId))+"/s":" · Mật độ: "+QiNumber(p.Density(h.worldMapId)))+
            rates+
            "\nLinh lực: "+QiNumber(s.Energy.Current)+" / "+QiNumber(p.Rules.EnergyMaximum(s.Injury.Severity))+
            "\nHuyết khóa · Điểm: "+h.statPoints+(p.Rules.CapacityCatalog!=null?" · thực = bước gần nhất":"")+"\nĐộ Kiếp: "+(gm.TribulationLockedReason==""?"[FIXTURE] sẵn sàng":gm.TribulationLockedReason);
    }
    void LayoutCultivationTrack(float width)
    {
        bool show=CultivationVisible;
        var game=GameForUI();
        if(show && cultivationUpgrade==null) cultivationUpgrade=SmallButton(progressionSummary.transform.parent,"CultivationConfigurationUpgrade","Nâng cấp & tải ô lưu",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(220,24),()=>{var current=GameForUI();if(current!=null)current.RequestSelectedCultivationConfigurationUpgrade();});
        if(cultivationUpgrade!=null){cultivationUpgrade.gameObject.SetActive(show && game!=null && game.CanUpgradeSelectedCultivationSnapshot);PlaceUI(cultivationUpgrade,Vector2.zero,Vector2.zero,new Vector2(148,6),new Vector2(Mathf.Max(120,width-238),24));}

        if(show && cultivationTrack==null) {
            var root=new GameObject("CultivationFoundationTrack",typeof(RectTransform),typeof(Image));root.transform.SetParent(progressionSummary.transform.parent,false);
            cultivationTrack=root.GetComponent<Image>();cultivationTrack.color=new Color(.04f,.09f,.1f,1);cultivationTrack.raycastTarget=false;
            var fill=new GameObject("CultivationFoundationFill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(root.transform,false);
            cultivationFill=fill.GetComponent<Image>();cultivationFill.color=new Color(.22f,.65f,.53f,1);cultivationFill.raycastTarget=false;
        }
        if(cultivationTrack==null)return;cultivationTrack.gameObject.SetActive(show);if(!show)return;
        PlaceUI(cultivationTrack.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(8,-64),new Vector2(width-16,6));
        var gm=GameForUI();var s=gm.Cultivation.Current;CultivationRealmDefinition d;gm.Cultivation.Profile.Catalog.TryGet(s.Foundation.ActualRealmId,out d);
        PlaceUI(cultivationFill.rectTransform,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2((width-16)*(float)(s.Foundation.Amount/d.Thresholds[3]),6));
    }
}
