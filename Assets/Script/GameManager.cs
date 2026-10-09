using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

public class GameManager : MonoBehaviour
{
    [Header("UI CỐT LÕI")]
    public Image eventLog;
    public TextMeshProUGUI eventLogText;
    public GameObject preGameUI;
    public TextMeshProUGUI infoText;

    [Header("UI GAME OVER")]
    public GameObject gameOverPanel;
    public GameObject confirmationPopup;

    [Header("DỮ LIỆU GỐC")]
    public EntityDataSO heroDataSO;
    public EntityDataSO currentMonsterDataSO;
    private EntityDataSO runtimeHeroData;

    [Header("SYSTEMS")]
    [SerializeField] private HeroController heroController;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private EnvironmentManager environmentManager;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private SaveManager saveManager;

    public List<GameObject> activeMonsters = new List<GameObject>();
    private Coroutine nextWaveCoroutine;
    private int waveSpawnCount, retryWaveSize;
    [Min(0f)] public float waveDelay = 1.5f;
    public float CurrentWaveDelay => BattleMotion.WaveDelay(waveDelay,
        heroController != null ? Mathf.Max(0f, heroController.moveSpeed) : 150f);
    private bool hasDeployed = false;
    private readonly Queue<string> recentEvents = new Queue<string>();
    private readonly Queue<string> recentDamage = new Queue<string>();
    private readonly List<string> fullEventHistory = new List<string>();
    public string FullEventHistory => fullEventHistory.Count==0 ? "Chưa có sự kiện." : string.Join("\n",fullEventHistory.ToArray());
    private readonly List<string> damageHistory = new List<string>();
    public string[] DamageHistory => damageHistory.ToArray();
    public void RecordDamage(string detail)
    {
        damageHistory.Insert(0, detail);
        if (damageHistory.Count > 30) damageHistory.RemoveAt(damageHistory.Count - 1);
    }
    public void RecordDamageEvent(string source,string target,int damage)
    {
        recentDamage.Enqueue(ShortName(source)+" → "+ShortName(target)+" - "+damage);
        while(recentDamage.Count>2) recentDamage.Dequeue();
        RefreshCompactLog();
    }
    private static string ShortName(string s)
    {
        s=string.IsNullOrWhiteSpace(s)?"?":s.Trim();
        return s.Length<=16?s:s.Substring(0,15)+"…";
    }
    private void RefreshCompactLog()
    {
        if (eventLogText!=null) eventLogText.text=recentDamage.Count==0
            ?"Chưa có sát thương":string.Join("\n",recentDamage.ToArray());
    }
    public EntityDataSO HeroData => runtimeHeroData;
    private enum MenuChoice { None, Continue, NewGame }
    private MenuChoice pendingChoice;
    private SaveSlot continueSlot;
    private bool manualSelection;
    private StartMenuUI startMenu;
    public bool IsHardMode => runtimeHeroData != null && runtimeHeroData.difficulty == 1;
    public bool HasPendingConfirmation => pendingChoice != MenuChoice.None;
    public bool CanContinue => saveManager != null && (manualSelection ? saveManager.HasSave(continueSlot) : saveManager.TryGetLatestSlot(out continueSlot));

    void Start()
    {
        if (monsterSpawner == null) monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (environmentManager == null) environmentManager = Object.FindFirstObjectByType<EnvironmentManager>();
        if (heroController == null) heroController = Object.FindFirstObjectByType<HeroController>();
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
        if (saveManager == null) saveManager = GetComponent<SaveManager>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        // Do not load or overwrite a save until the player confirms a choice.
        startMenu = StartMenuUI.Install(this);
        ConfigureEventLog();
        SetupPreGameUI();
        StartCoroutine(AutoSaveRoutine());
    }

    private void InitHeroData()
    {
        recentEvents.Clear(); recentDamage.Clear(); fullEventHistory.Clear();
        damageHistory.Clear();
        if (runtimeHeroData != null) Destroy(runtimeHeroData);
        runtimeHeroData = Instantiate(heroDataSO);
        runtimeHeroData.currentLevel = 1;
        runtimeHeroData.currentExp = 0;
        runtimeHeroData.expToNextLevel = 100;
        runtimeHeroData.addedHealth = runtimeHeroData.addedDamage = runtimeHeroData.statPoints = 0;
        runtimeHeroData.balanceVersion = 0;
        runtimeHeroData.difficulty = 0;
        runtimeHeroData.mapNumber = 1; runtimeHeroData.completedWavesInMap = 0; runtimeHeroData.mapProgressVersion = 1;
        runtimeHeroData.mapVisits = 1;
        runtimeHeroData.regionIndex = -1; runtimeHeroData.mapName = ""; WorldNames.AssignMap(runtimeHeroData);

        {
            runtimeHeroData.gender = Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu;
            runtimeHeroData.entityName = NameDatabase.GetRandomName(runtimeHeroData.gender);
            runtimeHeroData.race = SynergyMath.GenerateRandomRace();
            SynergyMath.GenerateRootProfile(runtimeHeroData);
        }

        runtimeHeroData.ApplyHeroBalance();
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        SetupPreGameUI();
    }

    private void ConfigureEventLog()
    {
        if (eventLog != null)
        {
            var rect = eventLog.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.1f,1f); rect.anchorMax = new Vector2(.9f,1f);
            rect.pivot = new Vector2(.5f,1f); rect.anchoredPosition = new Vector2(0,-18);
            rect.sizeDelta = new Vector2(0,180); eventLog.raycastTarget = false;
            eventLog.color = new Color(.06f,.08f,.12f,.85f);
        }
        if (eventLogText != null)
        {
            var rect = eventLogText.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = new Vector2(-52,0);
            rect.sizeDelta = new Vector2(-136,-16);
            UIManager.ReadableText(eventLogText,16);
            eventLogText.alignment = TextAlignmentOptions.TopLeft; eventLogText.raycastTarget = false;
            eventLogText.color = new Color(1,1,1,1);
        }
    }
    public void UpdateEventLog(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            recentEvents.Enqueue(message);
            while(recentEvents.Count>2)recentEvents.Dequeue();
            fullEventHistory.Insert(0,message);
            if(fullEventHistory.Count>80)fullEventHistory.RemoveAt(fullEventHistory.Count-1);
        }
        // Only compact damage lines are shown on the battlefield.
        RefreshCompactLog();
    }
    private void RestoreProgress()
    {
        runtimeHeroData.NormalizeMapProgress();
        runtimeHeroData.NormalizeRoots();
        if (UIManager.Instance != null) UIManager.Instance.ApplyDifficultyVisual(IsHardMode);
        UpdateEventLog(null);
    }


    private void SetupPreGameUI()
    {
        preGameUI.SetActive(true);
        if (startMenu != null) startMenu.Refresh();
        if (infoText != null) infoText.text = CanContinue
            ? "Tiếp tục hành trình đã lưu hoặc bắt đầu một hành trình mới."
            : "Chưa có bản lưu. Chọn Chơi mới để bắt đầu.";
    }

    public void OnDeployClicked() { OnContinueClicked(); }

    private void DeployHero()
    {
        if (UIManager.Instance != null) UIManager.Instance.SetPaused(false);
        if (hasDeployed) return;
        CancelNextWave();
        preGameUI.SetActive(false);
        hasDeployed = true;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        RestoreProgress();
        CallNextWave();
    }

    // --- CÁC HÀM XỬ LÝ SAVE/LOAD TỪ UI MỚI ---
    public void ForceManualSave(int slotIndex)
    {
        SaveSlot slot = (SaveSlot)slotIndex;
        if (saveManager != null && runtimeHeroData != null && !HasPendingConfirmation)
        {
            saveManager.SaveGame(runtimeHeroData, slot);
            UpdateEventLog($"Đã lưu tiến trình vào: {slot}");
        }
    }

    public void ForceManualLoad(int slotIndex) { TryLoadSlot((SaveSlot)slotIndex, false); }
    public bool LoadAndContinue(SaveSlot slot) { return TryLoadSlot(slot, true); }
    private bool TryLoadSlot(SaveSlot slot, bool deploy)
    {
        if (HasPendingConfirmation) return false;
        if (saveManager == null || !saveManager.HasSave(slot)) { UpdateEventLog("Ô lưu này chưa có dữ liệu."); return false; }

        var loaded = Instantiate(heroDataSO != null ? heroDataSO : runtimeHeroData);
        if (!saveManager.LoadGame(loaded, slot)) { Destroy(loaded); UpdateEventLog("Không đọc được bản lưu."); return false; }
        // 1. Dọn dẹp sạch sẽ chiến trường tránh kẹt Coroutine
        hasDeployed = false;
        CancelNextWave();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        if (heroController != null) heroController.HideHero();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 2. Nạp dữ liệu mới
        if (runtimeHeroData != null) Destroy(runtimeHeroData);
        runtimeHeroData = loaded;
        recentEvents.Clear(); recentDamage.Clear(); fullEventHistory.Clear();
        damageHistory.Clear();
        continueSlot = slot;
        manualSelection = true;

        // 3. Đưa người chơi về Màn Hình Chờ an toàn
        runtimeHeroData.ApplyHeroBalance();
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        SetupPreGameUI();
        RestoreProgress();
        UpdateEventLog($"Đã tải dữ liệu từ: {slot}");
        if (deploy) DeployHero();
        return true;
    }
    // ------------------------------------------

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (hasDeployed && !HasPendingConfirmation && runtimeHeroData != null && runtimeHeroData.isDirty && saveManager != null)
            {
                saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
            }
        }
    }

    public void OnHeroDied()
    {
        if (!hasDeployed) return;
        hasDeployed = false;
        CancelNextWave();
        activeMonsters.Clear();
        if (runtimeHeroData != null)
        {
            runtimeHeroData.RestartRegionAfterDefeat();
            RestoreProgress();
            UpdateEventLog("Thất bại: quay về map đầu của " + WorldNames.Region(runtimeHeroData.regionTheme) + ", đợt 1/5. Giữ cấp và chỉ số nhân vật.");
            if (saveManager != null) saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void OnRetryClicked()
    {
        if (hasDeployed || HasPendingConfirmation || runtimeHeroData == null) return;
        if (UIManager.Instance != null) UIManager.Instance.SetPaused(false);
        CancelNextWave();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        hasDeployed = true;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        RestoreProgress();
        CallNextWave();
    }

    public void OnContinueClicked()
    {
        if (hasDeployed || HasPendingConfirmation) return;
        if (!CanContinue) { UpdateEventLog("Không tìm thấy bản lưu để tiếp tục."); return; }
        pendingChoice = MenuChoice.Continue;
        ShowConfirmation(manualSelection ? "Bạn có muốn tiếp tục từ ô lưu vừa chọn không?" : "Bạn có muốn tiếp tục từ bản lưu gần nhất không?");
    }
    public void OnNewGameClicked()
    {
        if (HasPendingConfirmation) return;
        pendingChoice = MenuChoice.NewGame;
        ShowConfirmation("Chơi mới sẽ xóa toàn bộ bản lưu tự động và hai ô lưu tay. Bạn có chắc muốn bắt đầu lại từ đầu?");
    }
    private void ShowConfirmation(string message)
    {
        if (startMenu != null) startMenu.ShowConfirmation(message);
        else if (confirmationPopup != null) confirmationPopup.SetActive(true);
    }
    public void OnResetClicked()
    {
        if (HasPendingConfirmation) return;
        OnNewGameClicked();
        ShowConfirmation("Đặt lại nhân vật sẽ xóa bản lưu tự động và hai ô lưu tay, tạo nhân vật mới từ cấp 1. Bạn có muốn tiếp tục?");
    }
    public void OnConfirmResetClicked()
    {
        var choice = pendingChoice;
        if (choice == MenuChoice.None) return;
        pendingChoice = MenuChoice.None;
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (choice == MenuChoice.Continue)
        {
            var loaded = Instantiate(heroDataSO);
            if (saveManager == null || !saveManager.LoadGame(loaded, continueSlot))
            {
                Destroy(loaded);
                UpdateEventLog("Không đọc được bản lưu. Chưa bắt đầu trò chơi.");
                if (startMenu != null) startMenu.Refresh();
                return;
            }
            if (runtimeHeroData != null) Destroy(runtimeHeroData);
            runtimeHeroData = loaded;
            recentEvents.Clear(); recentDamage.Clear(); fullEventHistory.Clear();
            runtimeHeroData.ApplyHeroBalance();
        }
        else
        {
            if (saveManager == null || !saveManager.DeleteAllSaves())
            { UpdateEventLog("Không xóa được bản lưu. Chưa tạo trò chơi mới."); return; }
            hasDeployed = false;
            CancelNextWave();
            if (combatManager != null) combatManager.ForceClearAllMonsters();
            activeMonsters.Clear();
            if (heroController != null) heroController.HideHero();
            manualSelection = false;
            InitHeroData();
            if (heroController != null) heroController.ChangeAttackMode(0);
        }
        manualSelection = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        if (UIManager.Instance != null) UIManager.Instance.ApplyDifficultyVisual(IsHardMode);
        DeployHero();
        if (saveManager != null && !saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave))
            UpdateEventLog("Đã bắt đầu nhưng chưa ghi được bản lưu. Kiểm tra quyền ghi thư mục Saves.");
    }
    public void OnCancelResetClicked()
    {
        pendingChoice = MenuChoice.None;
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }
    // Legacy scene bindings cannot override the automatic map sequence.
    public void SetDifficulty(int mode) { if (runtimeHeroData != null) RestoreProgress(); }

    public void SetHeroAttackMode(int modeIndex)
    {
        if (heroController != null)
        {
            heroController.ChangeAttackMode(modeIndex);
            UpdateEventLog($"Đổi thế: {(AttackMode)modeIndex}");
        }
    }

    private void CallNextWave()
    {
        if (!hasDeployed || monsterSpawner == null) return;
        activeMonsters.Clear();

        RestoreProgress();
        int spawnCount = retryWaveSize > 0 ? retryWaveSize : Random.Range(1, 4);
        retryWaveSize = 0;
        waveSpawnCount = spawnCount;
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject m = monsterSpawner.SpawnMonster();
            if (m != null)
            {
                RectTransform rect = m.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-100f - (i * 60f), rect.anchoredPosition.y);
                activeMonsters.Add(m);
            }
        }

        if (combatManager != null) combatManager.StartBattle(activeMonsters, currentMonsterDataSO);
    }

    void LateUpdate()
    {
        if (!hasDeployed || Time.timeScale == 0f || heroController == null || !heroController.IsDeployed ||
            environmentManager == null || nextWaveCoroutine != null || activeMonsters.Count == 0) return;
        foreach (var monster in activeMonsters)
        {
            if (monster == null || environmentManager.HasEscapedRight(monster.GetComponent<RectTransform>()))
            { RecoverEscapedWave(); return; }
        }
    }

    private void RecoverEscapedWave()
    {
        CancelNextWave();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        // Keep HP/EXP/map progress. Cancel old life identities and aim points,
        // then replay the unfinished wave after the normal delay.
        heroController.SpawnHero();
        retryWaveSize = Mathf.Clamp(waveSpawnCount, 1, 3);
        UpdateEventLog("Quái thoát mép phải: sinh lại đợt đang dở, không cộng tiến trình map.");
        nextWaveCoroutine = StartCoroutine(WaitAndCallNextWave());
    }

    public void OnMonsterDied(GameObject deadMonster)
    {
        if (!activeMonsters.Remove(deadMonster)) return;
        if (hasDeployed && activeMonsters.Count == 0 && nextWaveCoroutine == null)
        {
            if (runtimeHeroData != null)
            {
                bool changed = runtimeHeroData.CompleteWave();
                RestoreProgress();
                if (changed) UpdateEventLog("Đã tới " + runtimeHeroData.mapName + (IsHardMode ? ": quái gây sát thương ×2, EXP ×3." : ": bình thường."));
            }
            nextWaveCoroutine = StartCoroutine(WaitAndCallNextWave());
        }
    }

    private IEnumerator WaitAndCallNextWave()
    {
        // Accumulate normalized progress so speed changes affect a pending wave.
        // At speed 0, wait without resetting the progress already earned.
        float progress = 0f;
        do
        {
            yield return null;
            if (!hasDeployed) { nextWaveCoroutine = null; yield break; }
            if (Time.timeScale == 0f) continue;
            float delay = CurrentWaveDelay;
            if (delay <= 0f) break;
            progress += Time.deltaTime / delay;
        } while (progress < 1f);
        nextWaveCoroutine = null;
        if (hasDeployed) CallNextWave();
    }

    private void CancelNextWave()
    {
        if (nextWaveCoroutine != null) StopCoroutine(nextWaveCoroutine);
        nextWaveCoroutine = null;
        retryWaveSize = 0;
    }

    private void OnDisable() { CancelNextWave(); }

    private void OnApplicationQuit() { if (saveManager != null && hasDeployed && runtimeHeroData != null) saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave); }
}
