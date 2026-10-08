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

    void Start()
    {
        if (monsterSpawner == null) monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (environmentManager == null) environmentManager = Object.FindFirstObjectByType<EnvironmentManager>();
        if (heroController == null) heroController = Object.FindFirstObjectByType<HeroController>();
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
        if (saveManager == null) saveManager = GetComponent<SaveManager>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        InitHeroData();
        StartCoroutine(AutoSaveRoutine());
    }

    private void InitHeroData()
    {
        if (runtimeHeroData != null) Destroy(runtimeHeroData);
        runtimeHeroData = Instantiate(heroDataSO);
        // Mặc định khởi động sẽ lấy AutoSave
        bool hasSaveData = saveManager != null && saveManager.LoadGame(runtimeHeroData, SaveSlot.AutoSave);

        if (!hasSaveData)
        {
            runtimeHeroData.gender = Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu;
            runtimeHeroData.entityName = NameDatabase.GetRandomName(runtimeHeroData.gender);
            runtimeHeroData.spiritRoots = SynergyMath.GenerateRandomRoots();
        }

        if (heroController != null) heroController.SetGenderVisual(runtimeHeroData.gender);
        SetupPreGameUI();
    }

    public void UpdateEventLog(string message) { if (eventLogText != null) eventLogText.text = message; }

    private void SetupPreGameUI()
    {
        preGameUI.SetActive(true);
        string rootStr = string.Join(" - ", runtimeHeroData.spiritRoots);
        infoText.text = $"Tên: {runtimeHeroData.entityName}\nLinh căn: {rootStr}\nHP: {runtimeHeroData.GetCalculatedHealth()}";
    }

    public void OnDeployClicked()
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
        if (saveManager != null && runtimeHeroData != null)
        {
            saveManager.SaveGame(runtimeHeroData, slot);
            UpdateEventLog($"Đã lưu tiến trình vào: {slot}");
        }
    }

    public void ForceManualLoad(int slotIndex)
    {
        SaveSlot slot = (SaveSlot)slotIndex;
        if (saveManager == null || !saveManager.HasSave(slot)) return;

        // 1. Dọn dẹp sạch sẽ chiến trường tránh kẹt Coroutine
        hasDeployed = false;
        CancelNextWave();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        if (heroController != null) heroController.HideHero();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 2. Nạp dữ liệu mới
        saveManager.LoadGame(runtimeHeroData, slot);

        // 3. Đưa người chơi về Màn Hình Chờ an toàn
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
            if (hasDeployed && runtimeHeroData != null && runtimeHeroData.isDirty && saveManager != null)
            {
                saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
            }
        }
    }

    public void OnHeroDied()
    {
        hasDeployed = false;
        CancelNextWave();
        activeMonsters.Clear();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void OnRetryClicked()
    {
        if (hasDeployed) return;
        CancelNextWave();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        hasDeployed = true;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        CallNextWave();
    }

    public void OnResetClicked() { if (confirmationPopup != null) confirmationPopup.SetActive(true); }
    public void OnConfirmResetClicked()
    {
        hasDeployed = false;
        CancelNextWave();
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (saveManager != null) saveManager.DeleteSave(SaveSlot.AutoSave);
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        if (heroController != null) heroController.HideHero();
        InitHeroData();
    }
    public void OnCancelResetClicked() { if (confirmationPopup != null) confirmationPopup.SetActive(false); }

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
