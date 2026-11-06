using TuTienCore;
using UnityEngine;

public partial class GameManager
{
    CultivationMigrationPlan pendingCultivationMigration;
    EntityDataSO pendingCultivationCandidate,pendingCanonicalHero;
    CultivationGameplayBinding pendingCultivationBinding;
    bool pendingMigrationDeploy,pendingMigrationFromTitle;
    float migrationPreviousTimeScale;
    string pendingCanonicalSnapshot;
    public bool IsCultivationMigrationPending {get{return pendingCultivationMigration!=null;}}
    bool RequestCultivationMigration(SaveSlot slot,bool deploy,bool fromTitle)
    {
        if(saveManager==null || !saveManager.NeedsCultivationMigration(slot))return false;
        CultivationMigrationPlan plan;EntityDataSO candidate;CultivationMigrationReason reason;
        if(startMenu==null || !saveManager.TryPrepareCultivationMigration(slot,out plan,out candidate,out reason))
        {UpdateEventLog("Chưa thể nâng cấp bản lưu. Dữ liệu gốc được giữ nguyên.");return false;}
        CultivationGameplayBinding prepared;
        if(!PrepareCultivation(candidate,out prepared)){SaveManager.ReleaseMigrationCandidate(candidate);return false;}
        pendingCultivationMigration=plan;pendingCultivationCandidate=candidate;pendingCultivationBinding=prepared;
        pendingCanonicalHero=runtimeHeroData;pendingCanonicalSnapshot=runtimeHeroData==null?null:JsonUtility.ToJson(runtimeHeroData);
        pendingMigrationDeploy=deploy;pendingMigrationFromTitle=fromTitle;migrationPreviousTimeScale=Time.timeScale;Time.timeScale=0;
        pendingChoice=MenuChoice.CultivationUpgrade;
        ShowConfirmation("Nâng cấp bản lưu để mở tu luyện Luyện Khí.\nGiữ Level, EXP, chỉ số, trang bị và tiến trình.\nTu vi bắt đầu từ 0; không cấp Tinh Huyết.\nSẽ tạo và kiểm tra bản sao lưu trước khi ghi.\nBạn có đồng ý nâng cấp ô "+slot+"?");
        startMenu.SetConfirmationLabels("Đồng ý nâng cấp","Hủy / quay lại");return true;
    }
    void CancelCultivationMigration(bool restoreTime=true)
    {
        if(pendingCultivationMigration==null)return;
        if(pendingCultivationBinding!=null)pendingCultivationBinding.Dispose();
        SaveManager.ReleaseMigrationCandidate(pendingCultivationCandidate);
        pendingCultivationBinding=null;pendingCultivationCandidate=null;pendingCultivationMigration=null;pendingCanonicalHero=null;pendingCanonicalSnapshot=null;
        pendingChoice=MenuChoice.None;if(confirmationPopup!=null)confirmationPopup.SetActive(false);
        if(restoreTime)Time.timeScale=migrationPreviousTimeScale;
    }
    void ConfirmCultivationMigration()
    {
        if(pendingCultivationMigration==null)return;
        if(runtimeHeroData!=pendingCanonicalHero || (runtimeHeroData!=null && JsonUtility.ToJson(runtimeHeroData)!=pendingCanonicalSnapshot))
        {CancelCultivationMigration();UpdateEventLog("Nhân vật hiện tại đã thay đổi; chưa nâng cấp bản lưu.");return;}
        CultivationMigrationReason reason;
        if(!saveManager.CommitCultivationMigration(pendingCultivationMigration,true,out reason))
        {string backup=pendingCultivationMigration.BackupPath;CancelCultivationMigration();UpdateEventLog("Nâng cấp chưa hoàn tất: "+reason+". Giữ bản gốc/bản sao lưu: "+System.IO.Path.GetFileName(backup));return;}
        var plan=pendingCultivationMigration;var data=pendingCultivationCandidate;var prepared=pendingCultivationBinding;
        bool deploy=pendingMigrationDeploy,fromTitle=pendingMigrationFromTitle;
        pendingCultivationMigration=null;pendingCultivationCandidate=null;pendingCultivationBinding=null;pendingCanonicalHero=null;pendingCanonicalSnapshot=null;
        pendingChoice=MenuChoice.None;if(confirmationPopup!=null)confirmationPopup.SetActive(false);Time.timeScale=migrationPreviousTimeScale;
        ApplyLoadedCandidate(data,plan.Slot,deploy,prepared,fromTitle);
        UpdateEventLog("Đã nâng cấp bản lưu; bản sao lưu gốc được giữ lại.");
    }
}
