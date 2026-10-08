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
    [Min(0f)] public float waveDelay = 1.5f;
    public float CurrentWaveDelay => BattleMotion.WaveDelay(waveDelay,
        heroController != null ? Mathf.Max(0f, heroController.moveSpeed) : 150f);
    private bool hasDeployed = false;
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
        SetupPreGameUI();
        StartCoroutine(AutoSaveRoutine());
    }

    private void InitHeroData()
    {
        if (runtimeHeroData != null) Destroy(runtimeHeroData);
        runtimeHeroData = Instantiate(heroDataSO);
        runtimeHeroData.currentLevel = 1;
        runtimeHeroData.currentExp = 0;
        runtimeHeroData.expToNextLevel = 100;
        runtimeHeroData.addedHealth = runtimeHeroData.addedDamage = runtimeHeroData.statPoints = 0;
        runtimeHeroData.balanceVersion = 0;
        runtimeHeroData.difficulty = 0;

        {
            runtimeHeroData.gender = Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu;
            runtimeHeroData.entityName = NameDatabase.GetRandomName(runtimeHeroData.gender);
            runtimeHeroData.spiritRoots = SynergyMath.GenerateRandomRoots();
        }

        runtimeHeroData.ApplyHeroBalance();
        if (heroController != null) heroController.SetGenderVisual(runtimeHeroData.gender);
        SetupPreGameUI();
    }

    public void UpdateEventLog(string message) { if (eventLogText != null) eventLogText.text = message; }

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
        if (hasDeployed) return;
        CancelNextWave();
        preGameUI.SetActive(false);
        hasDeployed = true;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
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

    public void ForceManualLoad(int slotIndex)
    {
        if (HasPendingConfirmation) return;
        SaveSlot slot = (SaveSlot)slotIndex;
        if (saveManager == null || !saveManager.HasSave(slot)) return;

        var loaded = Instantiate(heroDataSO != null ? heroDataSO : runtimeHeroData);
        if (!saveManager.LoadGame(loaded, slot)) { Destroy(loaded); UpdateEventLog("Không đọc được bản lưu."); return; }
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
        continueSlot = slot;
        manualSelection = true;

        // 3. Đưa người chơi về Màn Hình Chờ an toàn
        runtimeHeroData.ApplyHeroBalance();
        if (heroController != null) heroController.SetGenderVisual(runtimeHeroData.gender);
        SetupPreGameUI();
        UpdateEventLog($"Đã tải dữ liệu từ: {slot}");
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
        if (saveManager != null && runtimeHeroData != null) saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
        hasDeployed = false;
        CancelNextWave();
        activeMonsters.Clear();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void OnRetryClicked()
    {
        if (hasDeployed || HasPendingConfirmation || runtimeHeroData == null) return;
        CancelNextWave();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        hasDeployed = true;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
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
    public void OnResetClicked() { OnNewGameClicked(); }
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
        if (heroController != null) heroController.SetGenderVisual(runtimeHeroData.gender);
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
    public void SetDifficulty(int mode)
    {
        if (HasPendingConfirmation || runtimeHeroData == null) return;
        runtimeHeroData.difficulty = mode == 1 ? 1 : 0;
        runtimeHeroData.isDirty = true;
        if (UIManager.Instance != null) UIManager.Instance.ApplyDifficultyVisual(IsHardMode);
        UpdateEventLog(IsHardMode ? "Khó: sát thương quái ×2, EXP nhận ×3." : "Bình thường: sát thương quái ×1, EXP nhận ×1.");
    }

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

        int spawnCount = Random.Range(1, 4);
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

    public void OnMonsterDied(GameObject deadMonster)
    {
        if (!activeMonsters.Remove(deadMonster)) return;
        if (hasDeployed && activeMonsters.Count == 0 && nextWaveCoroutine == null)
            nextWaveCoroutine = StartCoroutine(WaitAndCallNextWave());
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
    }

    private void OnDisable() { CancelNextWave(); }

    private void OnApplicationQuit() { if (saveManager != null && hasDeployed && runtimeHeroData != null) saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave); }
}
