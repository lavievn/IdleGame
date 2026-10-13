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

    // B2-only test harness; NOT the future permanent companion/encounter system.
    // These objects and their copied progression never enter SaveManager.
    private sealed class TestCompanion
    {
        public GameObject ControllerRoot, VisualRoot;
        public HeroController Controller;
        public EntityDataSO Data;
    }
    private readonly List<TestCompanion> testCompanions = new List<TestCompanion>();
    public int TestCompanionCount => testCompanions.Count;
    public const int TestCompanionLimit = 2;

    private static T CorrespondingVisual<T>(
        RectTransform original, RectTransform clone, T originalComponent) where T : Component
    {
        if (original == null || clone == null || originalComponent == null) return null;
        // Same prefab hierarchy; use component ordinal rather than transform names
        // (duplicate names are common in Unity UI prefab children).
        T[] originals = original.GetComponentsInChildren<T>(true);
        T[] copies = clone.GetComponentsInChildren<T>(true);
        for (int i = 0; i < originals.Length && i < copies.Length; i++)
            if (originals[i] == originalComponent) return copies[i];
        return null; // External references such as main Hero's stats panel.
    }

    private AttackMode RandomUnusedTestMode()
    {
        var available = new List<AttackMode>();
        for (int i = 0; i < 3; i++)
        {
            var mode = (AttackMode)i;
            if (heroController != null && mode == heroController.attackMode) continue;
            bool alreadyUsed = false;
            foreach (var member in testCompanions)
                if (member.Controller != null && member.Controller.attackMode == mode)
                { alreadyUsed = true; break; }
            if (!alreadyUsed) available.Add(mode);
        }
        // A primary mode change during the test may consume all modes.
        if (available.Count == 0)
            for (int i = 0; i < 3; i++)
                if (heroController == null || (AttackMode)i != heroController.attackMode)
                    available.Add((AttackMode)i);
        return available[Random.Range(0, available.Count)];
    }

    // Button sequence: 1 Hero -> 2 Heroes -> 3 Heroes -> 1 Hero.
    public void CycleTestCompanions()
    {
        if (!IsGameplayHUDVisible || HasPendingConfirmation ||
            Time.timeScale == 0f || combatManager == null ||
            !combatManager.HasLivingHero || environmentManager == null ||
            environmentManager.BattleArea == null || heroController == null ||
            heroController.heroRect == null || runtimeHeroData == null) return;

        if (testCompanions.Count >= TestCompanionLimit)
        {
            ClearTestCompanions();
            UpdateEventLog("[B2 TEST] Đã thu hồi đồng đội thử nghiệm.");
            return;
        }

        var extra = CreateTestCompanion();
        if (extra == null)
        {
            UpdateEventLog("[B2 TEST] Không tạo được đồng đội; xem Unity Console.");
            return;
        }
        testCompanions.Add(extra);
        UpdateEventLog("[B2 TEST] Gọi đồng đội " + testCompanions.Count +
            "/2, dạng " + extra.Controller.attackMode + ". Tổng: " +
            (testCompanions.Count + 1) + " Hero.");
    }

    private TestCompanion CreateTestCompanion()
    {
        // Clone only the visible UI actor; cloning the scene's HeroController
        // GameObject would keep external scene references pointed at Hero #1.
        int number = testCompanions.Count + 1;
        GameObject visual = null, rig = null;
        EntityDataSO data = null;
        try
        {
            var src = heroController.heroRect;
            visual = Instantiate(src.gameObject, environmentManager.BattleArea);
            visual.name = "B2 Test Hero Visual " + number;
            visual.SetActive(false);
            var rect = visual.GetComponent<RectTransform>();
            if (rect == null) { Destroy(visual); return null; }

            rig = new GameObject("B2 Test Hero Controller " + number);
            rig.SetActive(false);
            var actor = rig.AddComponent<HeroController>();
            actor.heroRect = rect;
            actor.hpFillImage = CorrespondingVisual(src, rect, heroController.hpFillImage);
            actor.dmgTextPrototype = CorrespondingVisual(src, rect, heroController.dmgTextPrototype);
            actor.atkStatusText = null; // Independent data, no duplicate HUD.
            actor.moveSpeed = heroController.moveSpeed;
            actor.laneSpeed = heroController.laneSpeed;
            actor.attackMode = RandomUnusedTestMode();

            data = Instantiate(runtimeHeroData);
            // B2 companions are temporary test combatants, not persistent
            // equipped characters. Copy the modifier collection first so
            // removing gear never mutates the primary Hero (test doubles may
            // shallow-clone ScriptableObjects).
            data.combatStatModifiers = data.combatStatModifiers == null
                ? new List<CombatStatModifier>()
                : new List<CombatStatModifier>(data.combatStatModifiers);
            data.RemoveModifiersBySource(EquipmentInventory.ModifierSource);
            data.inventory = new List<InventoryStack>();
            data.equippedItemId = "";
            data.testItemsGranted = false;
            data.entityName = "Đồng đội B2-" + number;
            data.currentExp = 0;
            data.isDirty = false;

            rig.SetActive(true); // Deferred Awake binds damage popup to this actor.
            actor.SpawnHero(false); // IMPORTANT: never steal the camera subject.

            Vector2 origin = environmentManager.Position(heroController.heroRect);
            environmentManager.SetPosition(rect, origin +
                new Vector2(30f + number * 25f, number % 2 == 0 ? -25f : 25f));

            actor.SetIdentityVisual(data);
            var image = rect.GetComponent<Image>();
            if (image != null)
                image.color = number == 1 ? new Color(.22f,.72f,1f,1f) :
                                            new Color(.9f,.46f,1f,1f);

            if (combatManager.RegisterAdditionalHero(actor, data) == null)
            {
                actor.HideHero();
                Destroy(rig); Destroy(visual); Destroy(data);
                return null;
            }
            return new TestCompanion {
                ControllerRoot = rig, VisualRoot = visual, Controller = actor, Data = data
            };
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[B2 TEST] Companion spawn failed: " + ex);
            if (rig != null) Destroy(rig);
            if (visual != null) Destroy(visual);
            if (data != null) Destroy(data);
            return null;
        }
    }

    private void ClearTestCompanions()
    {
        foreach (var member in testCompanions)
        {
            if (member.Controller != null && combatManager != null)
                combatManager.UnregisterAdditionalHero(member.Controller);
            if (member.Controller != null) member.Controller.HideHero();
            if (member.ControllerRoot != null) Destroy(member.ControllerRoot);
            if (member.VisualRoot != null) Destroy(member.VisualRoot);
            if (member.Data != null) Destroy(member.Data);
        }
        testCompanions.Clear();
    }
    private Coroutine nextWaveCoroutine;
    private int waveSpawnCount, retryWaveSize;
    [Min(0f)] public float waveDelay = 1.5f;
    public float CurrentWaveDelay => BattleMotion.WaveDelay(waveDelay,
        combatManager != null ? Mathf.Max(0f,combatManager.ActiveHeroMovementSpeed) :
        heroController != null ? Mathf.Max(0f,heroController.moveSpeed) : 150f);
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
    // Visible HUD only during active gameplay, never on start/confirmation/death screens.
    public bool IsGameplayHUDVisible => hasDeployed &&
        (preGameUI == null || !preGameUI.activeInHierarchy) &&
        (gameOverPanel == null || !gameOverPanel.activeInHierarchy) &&
        (confirmationPopup == null || !confirmationPopup.activeInHierarchy);
    public bool CanContinue => saveManager != null && (manualSelection ? saveManager.HasSave(continueSlot) : saveManager.TryGetLatestSlot(out continueSlot));
    public bool HasSaveSlot(SaveSlot slot) => saveManager != null && saveManager.HasSave(slot);

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
        // New characters start with the established melee test stance.
        // Randomized combat aptitudes belong to future character generation.
        runtimeHeroData.savedAttackMode = AttackMode.Melee;
        EquipmentInventory.InitializeNewRun(runtimeHeroData);
        runtimeHeroData.originHealth = Mathf.Max(1,heroDataSO != null ? heroDataSO.baseHealth : CombatBalance.HeroHealth(1));
        runtimeHeroData.meleeGrowthBonus = 0;
        runtimeHeroData.meleeGrowthThroughLevel = 1;
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ApplyDifficultyVisual(IsHardMode);
            UIManager.Instance.UpdateMapDebug(runtimeHeroData);
        }
        UpdateEventLog(null);
    }


    private void SetupPreGameUI()
    {
        if (UIManager.Instance != null) UIManager.Instance.HideMapTitle();
        preGameUI.SetActive(true);
        preGameUI.transform.SetAsLastSibling();
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
        ClearTestCompanions();
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        RestoreProgress();
        if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
        CallNextWave();
    }

    private void CaptureSelectedStance()
    {
        if (runtimeHeroData != null && heroController != null)
            runtimeHeroData.SetSavedAttackMode(heroController.attackMode);
    }

    private void RestoreSelectedStance()
    {
        if (runtimeHeroData != null && heroController != null)
            heroController.ChangeAttackMode((int)runtimeHeroData.savedAttackMode);
    }

    // Inventory and equipment UI only affects the current run and primary Hero.
    // HP is recalculated by the existing CombatManager ratio-preserving path.
    public bool EquipInventoryItem(string itemId)
    {
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentInventory.Equip(runtimeHeroData,itemId)) return false;
        if (combatManager != null) combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog("Đã trang bị: " + EquipmentCatalog.Find(itemId).name);
        return true;
    }

    public bool UnequipInventoryItem()
    {
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentInventory.Unequip(runtimeHeroData)) return false;
        if (combatManager != null) combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog("Đã tháo vật phẩm về kho.");
        return true;
    }

    public bool GrantInventorySamples()
    {
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentInventory.GrantSamples(runtimeHeroData)) return false;
        UpdateEventLog("Đã nhận hai hộ phù thử nghiệm.");
        return true;
    }

    // --- CÁC HÀM XỬ LÝ SAVE/LOAD TỪ UI MỚI ---
    // UI 0.0.51: saving a manual slot is only available after deployment.
    // The result must come from actual IO rather than assuming a file was written.
    public bool TryManualSave(SaveSlot slot)
    {
        if (slot != SaveSlot.ManualSave1 && slot != SaveSlot.ManualSave2)
            return false;
        if (!hasDeployed || HasPendingConfirmation || runtimeHeroData == null ||
            saveManager == null)
        {
            UpdateEventLog("Chưa thể lưu thủ công tại thời điểm này.");
            return false;
        }
        CaptureSelectedStance();
        bool saved = saveManager.SaveGame(runtimeHeroData, slot);
        if (saved)
            Debug.Log("[SaveLoad] Saved " + slot + ": level=" +
                runtimeHeroData.currentLevel + " EXP=" + runtimeHeroData.currentExp +
                " map=" + runtimeHeroData.mapNumber +
                " ATK=" + runtimeHeroData.GetCalculatedDamage(heroController != null ?
                    heroController.attackMode : AttackMode.Melee));
        UpdateEventLog(saved ?
            "Đã lưu tiến trình vào " + (slot == SaveSlot.ManualSave1 ? "ô 1." : "ô 2.") :
            "Lỗi lưu thủ công: chưa ghi được tệp. Kiểm tra thư mục Saves.");
        return saved;
    }

    // Keep old scene/SaveMenuUI method bindings compatible.
    public void ForceManualSave(int slotIndex)
    {
        TryManualSave((SaveSlot)slotIndex);
    }

    public void ForceManualLoad(int slotIndex) { TryLoadSlot((SaveSlot)slotIndex, false); }
    public bool LoadAndContinue(SaveSlot slot) { return TryLoadSlot(slot, true); }
    private bool TryLoadSlot(SaveSlot slot, bool deploy)
    {
        if (HasPendingConfirmation) return false;
        if (saveManager == null || !saveManager.HasSave(slot)) { UpdateEventLog("Ô lưu này chưa có dữ liệu."); return false; }

        var loaded = Instantiate(heroDataSO != null ? heroDataSO : runtimeHeroData);
        if (!saveManager.LoadGame(loaded, slot)) { Destroy(loaded); UpdateEventLog("Không đọc được bản lưu."); return false; }
        // A manual load must roll back ALL progression, not just the equipment.
        // Verify the actual JSON snapshot before replacing the live character.
        int fileLevel,fileMap;
        if (!saveManager.TryGetSlotProgress(slot,out fileLevel,out fileMap) ||
            loaded.currentLevel != fileLevel)
        {
            Debug.LogError("[SaveLoad] Snapshot level mismatch in " + slot);
            Destroy(loaded);
            return false;
        }
        // 1. Dọn dẹp sạch sẽ chiến trường tránh kẹt Coroutine
        hasDeployed = false;
        CancelNextWave();
        ClearTestCompanions();
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
        RestoreSelectedStance();
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        SetupPreGameUI();
        RestoreProgress();
        UpdateEventLog("Đã tải " + slot + ": cấp " + loaded.currentLevel +
            ", EXP " + loaded.currentExp + ", map " + loaded.mapNumber + ".");
        if (deploy)
        {
            DeployHero();
            if (combatManager != null)
            {
                var state = combatManager.CurrentHeroCombatState;
                if (state.Data != runtimeHeroData ||
                    state.MaxHP != runtimeHeroData.GetCalculatedHealth(
                        heroController != null ? heroController.attackMode : AttackMode.Melee))
                    Debug.LogError("[SaveLoad] Combat state did not bind to the loaded Hero. " +
                        "Report this to the developer before continuing.");
            }
        }
        Debug.Log("[SaveLoad] Loaded " + slot + ": level=" +
            runtimeHeroData.currentLevel + " EXP=" + runtimeHeroData.currentExp +
            " map=" + runtimeHeroData.mapNumber);
        return true;
    }
    // ------------------------------------------

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (hasDeployed && !HasPendingConfirmation && runtimeHeroData != null && saveManager != null)
            {
                CaptureSelectedStance();
                if (runtimeHeroData.isDirty)
                    saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
            }
        }
    }

    public void OnHeroDied()
    {
        if (!hasDeployed) return;
        if (UIManager.Instance != null) UIManager.Instance.HideMapTitle();
        hasDeployed = false;
        CancelNextWave();
        activeMonsters.Clear();
        if (runtimeHeroData != null)
        {
            runtimeHeroData.RestartRegionAfterDefeat();
            RestoreProgress();
            UpdateEventLog("Thất bại: quay về map đầu của " + WorldNames.Region(runtimeHeroData.regionTheme) + ", đợt 1/5. Giữ cấp và chỉ số nhân vật.");
            CaptureSelectedStance();
            if (saveManager != null) saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
        }
        if (gameOverPanel != null) { gameOverPanel.SetActive(true); gameOverPanel.transform.SetAsLastSibling(); }
    }

    public void OnRetryClicked()
    {
        if (hasDeployed || HasPendingConfirmation || runtimeHeroData == null) return;
        if (UIManager.Instance != null) UIManager.Instance.SetPaused(false);
        CancelNextWave();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        hasDeployed = true;
        ClearTestCompanions();
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        RestoreProgress();
        if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
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
        ShowConfirmation("Chơi mới sẽ đặt lại ô lưu tự động để bắt đầu nhân vật mới. Hai ô lưu thủ công được giữ nguyên. Bạn có muốn tiếp tục?");
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
        ShowConfirmation("Đặt lại nhân vật sẽ tạo nhân vật cấp 1 mới và thay ô lưu tự động. Hai ô lưu thủ công được giữ nguyên. Bạn có muốn tiếp tục?");
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
            RestoreSelectedStance();
        }
        else
        {
            // A new run owns the autosave only. Manual slots belong to the
            // player and must never be deleted or overwritten by New Game.
            if (saveManager == null || !saveManager.ResetAutoSave())
            { UpdateEventLog("Không đặt lại được ô lưu tự động. Chưa tạo trò chơi mới."); return; }
            hasDeployed = false;
            CancelNextWave();
            ClearTestCompanions();
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
        CaptureSelectedStance();
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
        if (heroController == null || modeIndex < 0 || modeIndex > 2) return;
        heroController.ChangeAttackMode(modeIndex);
        CaptureSelectedStance();
        if (combatManager != null) combatManager.OnHeroAttackModeChanged();
        UpdateEventLog($"Đổi thế: {(AttackMode)modeIndex}");
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
        if (!hasDeployed || Time.timeScale == 0f ||
            (combatManager != null ? !combatManager.HasLivingHero :
             heroController == null || !heroController.IsDeployed) ||
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
        // Preserve .54g when primary lives. If only a companion survives,
        // do not silently resurrect the primary while repeating a wave.
        if (heroController != null && heroController.IsDeployed)
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
                if (changed)
                {
                    UpdateEventLog("Đã tới " + runtimeHeroData.mapName + (IsHardMode ? ": quái gây sát thương ×2, EXP ×3." : ": bình thường."));
                    if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
                }
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

    private void OnDisable()
    {
        CancelNextWave();
        ClearTestCompanions();
    }

    private void OnApplicationQuit()
    {
        if (saveManager == null || !hasDeployed || runtimeHeroData == null) return;
        CaptureSelectedStance();
        saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
    }
}
