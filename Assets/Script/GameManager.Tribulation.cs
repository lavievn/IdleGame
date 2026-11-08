using System;
using UnityEngine;
using TuTienCore;
public partial class GameManager
{
    HeavenlyGateEncounter trial;TribulationSaveReceipt trialReceipt;EntityDataSO trialCandidate;CultivationGameplayBinding trialPrepared;string trialExpectedHash;long trialStep;bool trialInterrupted;float trialNoticeUntil;
    string interruptedTrialExpectedHash;
    public bool IsDevelopmentTrial => saveManager != null && saveManager.HasDevelopmentCultivationLoop;
    public bool CanRetryTribulationCommit => trialCandidate != null && !trialInterrupted;
    public bool ShowTribulationNotice=>LastTribulationNotice!=null&&Time.realtimeSinceStartup<trialNoticeUntil;
    public bool HasTribulationLiability=>trial!=null;
    public bool TribulationFighting=>trial!=null&&!trialInterrupted&&trial.Outcome==HeavenlyGateOutcome.Fighting&&trialCandidate==null;
    public string LastTribulationNotice {get;private set;}public double LastTribulationCombatSeconds {get;private set;}public int LastTribulationHeroHP {get;private set;}
    public string TribulationLockedReason
    {
        get{
            if(HasTribulationLiability)return trialInterrupted?"Trial bị gián đoạn · giữ checkpoint để kiểm tra":trialCandidate!=null?"Chờ ghi kết quả · bấm Độ Kiếp để thử lưu lại":"Đang Phá Thiên Quan";
            if(saveManager==null||saveManager.TrialConfiguration==null)return "Thiên Kiếp chỉ mở trong cấu hình Development";
            if(!hasDeployed||enteringGameplay||runtimeHeroData==null||heroController==null||!heroController.IsDeployed||combatManager==null||!combatManager.HasLivingHero||HasPendingConfirmation)return "Hero chưa sẵn sàng";
            var b=Cultivation;if(b==null||b.Profile.IsProduction||b.Profile.Id!=saveManager.TrialConfiguration.cultivationConfiguration)return "Cấu hình chưa hỗ trợ";
            var s=b.Current;if(s.Burning!=null)return "Tinh Huyết chưa quyết toán";if(s.Injury.Severity>b.Profile.Rules.MaximumInjuryForTrial)return "Thương tổn căn cơ";
            if(s.Foundation.ActualRealmId!=saveManager.TrialConfiguration.sourceRealm||b.Profile.ExplicitSuccessor(s.Foundation.ActualRealmId)!=saveManager.TrialConfiguration.targetRealm)return "Chưa có cảnh giới kế tiếp được cấu hình";
            if(!b.Profile.Catalog.Evaluate(s.Foundation.ActualRealmId,s.Foundation.Amount).AtBottleneck)return "Chưa tới bình cảnh";
            if(combatManager.RegisteredHeroCount!=1)return "Chỉ hỗ trợ Hero chính";return "";
        }
    }
    public bool RequestTribulation()
    {
        if(trialCandidate!=null)return RetryTribulationCommit();
        string reason=TribulationLockedReason;if(reason!=""){UpdateEventLog(reason);return false;}
        CaptureSelectedStance();string expectedCheckpoint=JsonUtility.ToJson(runtimeHeroData,true);if(!saveManager.SaveGame(runtimeHeroData,SaveSlot.AutoSave)){UpdateEventLog("Không ghi được checkpoint trước thử thách");return false;}
        string sourceHash;if(!saveManager.TryReadTrialCheckpoint(expectedCheckpoint,out sourceHash)){LastTribulationNotice="Checkpoint đã thay đổi · chưa bắt đầu";UpdateEventLog(LastTribulationNotice);return false;}CultivationSession source;string why;
        if(!CultivationGameplayState.TryRestore(runtimeHeroData.cultivationJson,Cultivation.Profile,out source,out why))return false;
        var c=saveManager.TrialConfiguration;var attempt=new HeavenlyGateEncounter(source,c,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),combatManager.CurrentHeroCombatState.LifeVersion);
        var r=new TribulationSaveReceipt{version=1,configurationVersion=Cultivation.Profile.Version,heroLife=attempt.HeroLife,penaltyVersion=saveManager.TrialPenalty.Version,encounterId=attempt.EncounterId.ToString("N"),attemptId=attempt.AttemptId.ToString("N"),ownerId=attempt.Owner.ToString("N"),epoch=Cultivation.Epoch.ToString("N"),targetId=attempt.TargetId.ToString("N"),sourceRealm=c.sourceRealm,targetRealm=c.targetRealm,configurationId=Cultivation.Profile.Id,encounterConfiguration=c.id,encounterVersion=c.version,encounterConfigHash=TribulationSaveTransaction.Hash(JsonUtility.ToJson(c,false)),requestedPenalty=saveManager.TrialPenalty.RequestedLoss,policyId=c.penaltyPolicyId,phase=c.phase,outcome="Active",appliedId="",sourceCultivation=runtimeHeroData.cultivationJson,resultCultivation="",sourceSnapshotHash=sourceHash,penalty=0};
        var staged=Instantiate(runtimeHeroData);staged.tribulationJson=JsonUtility.ToJson(r,false);string activeHash=TribulationSaveTransaction.Hash(JsonUtility.ToJson(staged,true));
        bool committed=saveManager.CommitTrialCandidate(staged,sourceHash,out why);if(!committed){Destroy(staged);LastTribulationNotice="Chưa bắt đầu: "+why;UpdateEventLog(LastTribulationNotice);return false;}
        runtimeHeroData.tribulationJson=staged.tribulationJson;Destroy(staged);trial=attempt;trialReceipt=r;trialExpectedHash=activeHash;trialStep=0;trialInterrupted=false;LastTribulationNotice=null;
        CancelNextWave();ClearTestCompanions();combatManager.ForceClearAllMonsters();activeMonsters.Clear();waitingForExploration=false;
        combatManager.BeginHeavenlyGate(c,attempt.EncounterId,()=>TribulationFighting,ObserveTribulationCombat,TribulationCombatInvalidated);UIManager.Instance?.CloseDamageInfo();UIManager.Instance?.SetPaused(false);UpdateEventLog((IsDevelopmentTrial ? "[DEV 0.1.5]" : "[FIXTURE v1.3]")+" Phá Thiên Quan · không EXP/loot");return true;
    }
    internal void TribulationCombatInvalidated(){if(trial==null)return;trialInterrupted=true;UIManager.Instance?.SetPaused(true);LastTribulationNotice="Đời Hero đã đổi · giữ checkpoint chờ kiểm tra";}
    internal void ObserveTribulationCombat(int heroHP,int gateHP,double dt)
    {
        if(!TribulationFighting||!combatManager.HeavenlyGateActive||gateHP!=combatManager.HeavenlyGateHP||heroHP!=combatManager.CurrentHeroHP)return;
        var current=combatManager.CurrentHeroCombatState;
        if(current.Data!=runtimeHeroData||current.LifeVersion!=trial.HeroLife||Cultivation==null||Cultivation.OwnerId!=trial.Owner||Cultivation.Epoch!=Guid.ParseExact(trialReceipt.epoch,"N")){TribulationCombatInvalidated();return;}
        trial.Advance(++trialStep,Math.Min(1,dt),false);
        // A live HP sample updates the trial clock only. Creating a receipt
        // every frame grows the deduplication set for the entire attempt.
        if (heroHP > 0 && gateHP > 0) return;
        trial.Accept(trial.Owner,trial.Epoch,trial.AttemptId,trial.TargetId,current.LifeVersion,Guid.NewGuid(),trial.Step,heroHP,gateHP);
        if(trial.Outcome==HeavenlyGateOutcome.Fighting)return;
        UIManager.Instance?.SetPaused(true); // Keep the entry-time expected hash, never adopt a competing snapshot.
        trialCandidate=Instantiate(runtimeHeroData);
        PrepareTrialResult(trialCandidate, trialReceipt, trial.Outcome == HeavenlyGateOutcome.Success);
        if(!PrepareCultivation(trialCandidate,out trialPrepared))throw new InvalidOperationException("Trial candidate binding rejected");RetryTribulationCommit();
    }
    void PrepareTrialResult(EntityDataSO candidate, TribulationSaveReceipt active, bool success)
    {
        var profile = saveManager.CultivationProfile;
        if (!CultivationGameplayState.TryRestore(active.sourceCultivation, profile, out var source, out var why))
            throw new InvalidOperationException(why);
        var result = JsonUtility.FromJson<TribulationSaveReceipt>(JsonUtility.ToJson(active));
        var attempt = Guid.ParseExact(active.attemptId, "N");
        result.appliedId = result.attemptId;
        result.outcome = success ? "Success" : "Failure";
        result.penalty = success ? 0 : saveManager.TrialPenalty.Loss(source.Realms, source.Current);
        var status = success
            ? new RealmBreakthroughTransition(source).ApplySuccess(source.OwnerId, source.Epoch, attempt,
                source.RealmRevision, result.sourceRealm, result.targetRealm, 0)
            : source.ApplyGatePenalty(source.OwnerId, source.Epoch, attempt, source.RealmRevision,
                result.sourceRealm, saveManager.TrialPenalty);
        if (status != CultivationReason.None)
            throw new InvalidOperationException("Trial candidate rejected: " + status);
        result.resultCultivation = CultivationGameplayState.Capture(source, profile);
        candidate.cultivationJson = result.resultCultivation;
        candidate.tribulationJson = JsonUtility.ToJson(result);
    }

    bool OfferInterruptedTrialRecovery()
    {
        if (saveManager == null || !saveManager.HasInterruptedDevelopmentTrial()) return false;
        if (!saveManager.TryReadInterruptedDevelopmentTrial(out var receipt, out _,
                out interruptedTrialExpectedHash, out var reason) ||
            !CultivationGameplayState.TryRestore(receipt.sourceCultivation, saveManager.CultivationProfile,
                out var source, out reason))
        {
            Debug.LogWarning("[DEV 0.1.5] Active checkpoint retained: " + reason);
            pendingChoice = MenuChoice.InterruptedTrial;
            ShowConfirmation("Không đọc được checkpoint Độ Kiếp. Bản lưu được giữ để kiểm tra.");
            return true;
        }
        var loss = saveManager.TrialPenalty.Loss(source.Realms, source.Current);
        pendingChoice = MenuChoice.InterruptedTrial;
        ShowConfirmation("Lần Độ Kiếp trước bị gián đoạn. Nhận thua để hồi sinh?\nTrừ " +
            loss.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
            " căn cơ, giữ cảnh giới và Qi dự trữ. Hủy sẽ giữ checkpoint.");
        startMenu?.SetConfirmationLabels("Nhận thua", "Hủy");
        return true;
    }

    void ConfirmInterruptedTrialRecovery()
    {
        string expectedHash = interruptedTrialExpectedHash;
        OnCancelResetClicked();
        if (expectedHash == null) return;
        if (!saveManager.TryReadInterruptedDevelopmentTrial(out var receipt, out var json,
                out var currentHash, out _) || expectedHash != currentHash)
        {
            ShowTrialRecoveryError("Checkpoint đã thay đổi. Bấm Tiếp tục để kiểm tra lại.");
            return;
        }

        var loaded = Instantiate(heroDataSO);
        if (!saveManager.TryLoadInterruptedTrialCandidate(loaded, json))
        {
            Destroy(loaded);
            ShowTrialRecoveryError("Checkpoint chưa thể tải. Bản lưu được giữ để kiểm tra.");
            return;
        }
        PrepareTrialResult(loaded, receipt, false);
        if (!PrepareCultivation(loaded, out var prepared) ||
            !saveManager.CommitTrialCandidate(loaded, expectedHash, out _))
        {
            prepared?.Dispose();
            Destroy(loaded);
            ShowTrialRecoveryError("Chưa ghi được kết quả. Bấm Tiếp tục để thử lại.");
            return;
        }
        SetTribulationResultNotice(TribulationSaveTransaction.Read(loaded.tribulationJson), prepared);
        ApplyLoadedCandidate(loaded, SaveSlot.AutoSave, true, prepared);
        UpdateEventLog(LastTribulationNotice);
    }

    void ShowTrialRecoveryError(string message)
    {
        pendingChoice = MenuChoice.InterruptedTrial;
        ShowConfirmation(message);
    }

    internal bool BlockExitDuringTrial()
    {
        if (!IsDevelopmentTrial || !TribulationFighting) return false;
        UIManager.Instance?.SetPaused(false);
        LastTribulationNotice = "Đang Độ Kiếp · hãy đợi trận kết thúc trước khi thoát";
        trialNoticeUntil = Time.realtimeSinceStartup + 5;
        return true;
    }

#if DEVELOPMENT_BUILD && !UNITY_EDITOR
    bool AllowQuitDuringTrial() => !BlockExitDuringTrial();
#endif

    public bool RetryTribulationCommit()
    {
        if(trial==null||trialInterrupted||trialCandidate==null||trialPrepared==null)return false;
        var actor=combatManager.CurrentHeroCombatState;
        if(actor.Data!=runtimeHeroData||actor.LifeVersion!=trial.HeroLife||Cultivation==null||Cultivation.OwnerId!=trial.Owner||Cultivation.Epoch!=Guid.ParseExact(trialReceipt.epoch,"N"))return false;
        if(runtimeHeroData.cultivationJson!=trialReceipt.sourceCultivation||trial.Outcome==HeavenlyGateOutcome.Success&&actor.CurrentHP<=0)return false;
        string why;if(!saveManager.CommitTrialCandidate(trialCandidate,trialExpectedHash,out why)){LastTribulationNotice="Chưa ghi kết quả · "+why;UpdateEventLog(LastTribulationNotice);return false;}
        var receipt=TribulationSaveTransaction.Read(trialCandidate.tribulationJson);var loaded=trialCandidate;var prepared=trialPrepared;
        LastTribulationCombatSeconds=trial.Seconds;LastTribulationHeroHP=actor.CurrentHP;
        SetTribulationResultNotice(receipt, prepared);
        trial=null;trialCandidate=null;trialPrepared=null;trialReceipt=null;trialExpectedHash=null;
#if UNITY_5_3_OR_NEWER
        var breakPoint = receipt.outcome == "Success" ? combatManager.CaptureGateBreak() : null;
#endif
        bool restored=ApplyLoadedCandidate(loaded,SaveSlot.AutoSave,true,prepared);
        if (restored)
        {
#if UNITY_5_3_OR_NEWER
            if (breakPoint.HasValue && mapEntryCover != null)
                combatManager.ShowCommittedGateBreak(breakPoint.Value, mapEntryCover.canvas.sortingOrder + 1);
#endif
            UpdateEventLog(LastTribulationNotice);
        }
        return restored;
    }

    void SetTribulationResultNotice(TribulationSaveReceipt receipt, CultivationGameplayBinding prepared)
    {
        var state=prepared.Current;var stage=prepared.Profile.Catalog.Evaluate(state.Foundation.ActualRealmId,state.Foundation.Amount).MinorStage;
        string stageLabel=stage==RealmMinorStage.SoKy?"Sơ kỳ":stage==RealmMinorStage.TrungKy?"Trung kỳ":stage==RealmMinorStage.HauKy?"Hậu kỳ":"Viên mãn";
        LastTribulationNotice=receipt.outcome=="Success"?"Đột phá · "+prepared.Profile.RealmName(state.Foundation.ActualRealmId)+" "+stageLabel+" · căn cơ 0":"Thất bại · trừ "+receipt.penalty.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" căn cơ · "+stageLabel;
        LastTribulationNotice+="\nQi dự trữ giữ nguyên: "+state.Reserve.Amount.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);trialNoticeUntil=Time.realtimeSinceStartup+10;
    }
}
