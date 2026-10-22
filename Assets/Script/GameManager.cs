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
    [Header("W2 MOVEMENT TEST")]
    public bool useW2StanceMoveSpeeds=true;

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
            actor.useStanceMoveSpeeds=useW2StanceMoveSpeeds;
            actor.meleeMoveSpeed=heroController.meleeMoveSpeed;
            actor.physicalMoveSpeed=heroController.physicalMoveSpeed;
            actor.magicMoveSpeed=heroController.magicMoveSpeed;
            actor.tracePositionChanges=heroController.tracePositionChanges;
            actor.positionTraceFrameCount=heroController.positionTraceFrameCount;
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
            data.combatStatModifiers.RemoveAll(mod=>mod!=null && mod.sourceId!=null &&
                mod.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                    System.StringComparison.Ordinal));
            data.equipmentItems = new List<EquipmentInstance>();
            data.inventory = new List<InventoryStack>();
            data.equippedItemId = "";
            data.testItemsGranted = false;
            data.entityName = "Đồng đội B2-" + number;
            data.currentExp = 0;
            data.isDirty = false;

            Vector2 origin = environmentManager.Position(heroController.heroRect);
            foreach(var peer in HeroController.ActiveHeroes)
                if(peer!=null && peer.IsDeployed)
                    origin.x=Mathf.Max(origin.x,environmentManager.Position(peer.heroRect).x);
            var formation=environmentManager.GetComponent<HeroFormation>();
            float gap=formation!=null?Mathf.Max(1f,formation.spacing):100f;
            Vector2 spawn=origin+new Vector2(gap,number%2==0?-25f:25f);
            rig.SetActive(true); // Deferred Awake binds damage popup to this actor.
            actor.SpawnHero(false,spawn); // One initial placement; never steal camera focus.

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
    private int waveWaitVersion;
    private int waveSpawnCount, retryWaveSize;
    [Tooltip("Scaled gameplay seconds after the last monster dies; independent of Hero movement speed.")]
    [Min(0f)] public float waveDelay = 5f;
    public float CurrentWaveDelay => Mathf.Max(0f,waveDelay);
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
    private bool enteringGameplay, openingRevealed;
    private bool entryCompletionPending;
    private bool resumeEntryWave;
    private float entryElapsed;
    private HeroSpawnVfx entryVfx;
    public bool IsEnteringGameplay => enteringGameplay;
    // Read-only snapshot for opt-in frame diagnostics; never advances entry/VFX.
    public string DescribeGameplayEntry()
    {
        return " deployed="+hasDeployed+" entry="+enteringGameplay+" revealed="+openingRevealed+
            " completionPending="+entryCompletionPending+
            " entryElapsed="+entryElapsed.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+
            " openingEntering="+(startMenu!=null && startMenu.IsEnteringCinematic)+
            " menuActive="+(preGameUI!=null && preGameUI.activeInHierarchy)+
            " vfxPlaying="+(entryVfx!=null && entryVfx.IsPlaying)+
            " waveMembers="+activeMonsters.Count+" waitingWave="+(nextWaveCoroutine!=null);
    }
    private static void TraceEntryFrame(string stage)
    {
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.TracePositionFrame(stage);
    }
    public bool IsHardMode => runtimeHeroData != null && runtimeHeroData.difficulty == 1;
    public bool HasPendingConfirmation => pendingChoice != MenuChoice.None;
    // Visible HUD only during active gameplay, never on start/confirmation/death screens.
    public bool IsGameplayHUDVisible => hasDeployed && !enteringGameplay &&
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
        if(heroController!=null)heroController.useStanceMoveSpeeds=useW2StanceMoveSpeeds;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        // Do not load or overwrite a save until the player confirms a choice.
        startMenu = StartMenuUI.Install(this);
        ConfigureEventLog();
        SetupPreGameUI();
        MapBgmController.Install(this);
        StartCoroutine(AutoSaveRoutine());
    }

    private void InitHeroData(bool showMenu = true)
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
        EquipmentRuntime.InitializeNewRun(runtimeHeroData);
        runtimeHeroData.originHealth = Mathf.Max(1,heroDataSO != null ? heroDataSO.baseHealth : CombatBalance.HeroHealth(1));
        runtimeHeroData.meleeGrowthBonus = 0;
        runtimeHeroData.meleeGrowthThroughLevel = 1;
        runtimeHeroData.balanceVersion = 0;
        runtimeHeroData.difficulty = 0;
        runtimeHeroData.mapNumber = 1; runtimeHeroData.completedWavesInMap = 0; runtimeHeroData.mapProgressVersion = 1;
        runtimeHeroData.mapVisits = 1;
        runtimeHeroData.regionIndex = -1; runtimeHeroData.mapName = ""; WorldNames.AssignMap(runtimeHeroData);
        // A NEW run starts in one of the five confirmed world-map births.
        // Old saves retain their unmodified legacy sequential maps.
        if (!WorldMapCatalog.StartNew(runtimeHeroData))
            Debug.LogWarning("[WorldMap] World catalog unavailable; new run remains in legacy mode.");

        {
            runtimeHeroData.gender = Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu;
            runtimeHeroData.entityName = NameDatabase.GetRandomName(runtimeHeroData.gender);
            runtimeHeroData.race = SynergyMath.GenerateRandomRace();
            SynergyMath.GenerateRootProfile(runtimeHeroData);
        }

        runtimeHeroData.ApplyHeroBalance();
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        if(showMenu)SetupPreGameUI();
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


    private void SetupPreGameUI(bool restartOpening = false)
    {
        CancelGameplayEntry();
        if (UIManager.Instance != null) UIManager.Instance.HideMapTitle();
        bool reopening = !preGameUI.activeInHierarchy;
        preGameUI.SetActive(true);
        preGameUI.transform.SetAsLastSibling();
        if (startMenu != null)
        {
            if (reopening || restartOpening) startMenu.RestoreMenu();
            else startMenu.Refresh();
        }
        if (infoText != null) infoText.text = CanContinue
            ? "Tiếp tục hành trình đã lưu hoặc bắt đầu một hành trình mới."
            : "Chưa có bản lưu. Chọn Chơi mới để bắt đầu.";
    }

    public void OnDeployClicked() { OnContinueClicked(); }

    private void DeployHero(bool showOpeningTransition = false)
    {
        if (UIManager.Instance != null) UIManager.Instance.SetPaused(false);
        if (hasDeployed) return;
        CancelNextWave();
        // Keep the menu canvas until the animated cloud reveal completes.
        // Manual LoadAndContinue is immediate; only confirmed title choices animate.
        bool cinematic = showOpeningTransition && startMenu != null && startMenu.IsCinematic &&
            preGameUI != null && preGameUI.activeInHierarchy;
        if (!cinematic)
        {
            if (startMenu != null) startMenu.EnterGameplayImmediately();
            if (preGameUI != null) preGameUI.SetActive(false);
        }
        hasDeployed = true;
        ClearTestCompanions();
        heroController.useStanceMoveSpeeds=useW2StanceMoveSpeeds;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        if(cinematic)
        {
            enteringGameplay=true;openingRevealed=false;entryElapsed=0f;
            entryVfx=HeroSpawnVfx.Prepare(heroController);
            SetEntryPaused(true);
        }
        RestoreProgress();
        if(cinematic)
        {
            if(!startMenu.BeginGameplayTransition())startMenu.EnterGameplayImmediately();
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
            CallNextWave();
        }
    }

    private static void SetEntryPaused(bool paused)
    {
        if(UIManager.Instance!=null)UIManager.Instance.SetPaused(paused);
        else Time.timeScale=paused?0f:1f;
    }
    public void OnOpeningRevealCompleted()
    {
        if(!enteringGameplay || openingRevealed)return;
        // Commit the final gameplay position while ALL actor visuals are hidden.
        // BattleArea units are independent of the Windows client/window size.
        Canvas.ForceUpdateCanvases();
        var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
        if(space!=null && space.BattleArea!=null && heroController!=null && heroController.heroRect!=null)
        {
            heroController.ArmPositionTrace();
            heroController.TracePosition("Opening reveal before intentional 80-percent placement");
            Vector2 position=space.Position(heroController.heroRect);
            position.x=space.BattleArea.rect.xMin+space.Width*.8f;
            space.SetPosition(heroController.heroRect,position);
            heroController.TracePosition("80-percent placement before VFX");
        }
        openingRevealed=true;
        if(entryVfx==null || !entryVfx.Begin())entryCompletionPending=true;
    }
    void Update()
    {
        TraceEntryFrame("GameManager.Update before entry");
        if(!enteringGameplay)return;
        // Own the pause across the handoff; no actor/camera advances in a gap.
        SetEntryPaused(true);
        entryElapsed+=Mathf.Max(0f,Time.unscaledDeltaTime);
        if(!openingRevealed && entryVfx!=null)entryVfx.HoldForCinematic();
        if(!openingRevealed && entryElapsed>OpeningCinematicUI.EntrySeconds+.5f)
        {
            // Missing/disabled opening cannot leave the game black or paused.
            if(startMenu!=null)startMenu.EnterGameplayImmediately();
            OnOpeningRevealCompleted();
        }
        if(enteringGameplay && openingRevealed && (entryVfx==null || !entryVfx.Advance(Time.unscaledDeltaTime)))
            entryCompletionPending=true;
        // Re-arm at the completion request, even if the earlier reveal window
        // has expired while the VFX played. Include the still-paused LateUpdate.
        if(entryCompletionPending)
            foreach(var hero in HeroController.ActiveHeroes)
                if(hero!=null)hero.ArmPositionTrace();
        TraceEntryFrame("GameManager.Update after entry");
    }
    private void CancelGameplayEntry()
    {
        if(!enteringGameplay)return;
        enteringGameplay=false;openingRevealed=false;entryCompletionPending=false;
        if(entryVfx!=null)entryVfx.Finish();entryVfx=null;
        SetEntryPaused(false);
    }
    private void CompleteGameplayEntry()
    {
        if(!enteringGameplay)return;
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.ArmPositionTrace();
        if(heroController!=null)heroController.TracePosition("VFX complete before gameplay release");
        CancelGameplayEntry();
        if(startMenu!=null)startMenu.EnterGameplayImmediately();
        if(hasDeployed && heroController!=null && heroController.IsDeployed)
        {
            if(UIManager.Instance!=null)UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
            CallNextWave();
        }
        TraceEntryFrame("GameManager after gameplay release and first wave");
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

    // V2 individual gear: all 8 positions share one canonical inventory.
    public bool GrantDemoSword()
    {
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentRuntime.GrantDemoSword(runtimeHeroData)) return false;
        UpdateEventLog("Đã nhận Thiết Kiếm thử nghiệm.");
        return true;
    }
    // Reusable test sword: preserves older saved swords; new ones have
    // fixed latent affixes and a demonstrative quality ceiling.
    public bool GrantForgeTestSword()
    {
        if(!IsGameplayHUDVisible || runtimeHeroData==null)return false;
        if(!EquipmentRuntime.AddInstance(runtimeHeroData,
            EquipmentRefinement.CreateForgeDemoSword()))return false;
        UpdateEventLog("Đã thêm kiếm rèn thử nghiệm.");
        return true;
    }

    public bool GrantForgeMaterials()
    {
        if(!IsGameplayHUDVisible || runtimeHeroData==null)return false;
        if(!EquipmentInventory.Add(runtimeHeroData,EquipmentRefinement.MaterialId,100))
            return false;
        UpdateEventLog("Đã thêm 100 Nguyên liệu rèn.");
        return true;
    }

    public bool ApplyForgeTest(string instanceId,
        EquipmentRefinement.Operation operation,out string message)
    {
        message="Chưa thể rèn lúc này.";
        if(!IsGameplayHUDVisible || runtimeHeroData==null)return false;
        bool success=EquipmentRefinement.TryApply(runtimeHeroData,instanceId,
            operation,out message);
        if(!success)return false;
        if(combatManager!=null)
            combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog(message);
        return true;
    }

    public bool EquipGear(string instanceId,EquipmentSlot slot)
    {
        if (!IsGameplayHUDVisible || runtimeHeroData == null ||
            !EquipmentRuntime.Equip(runtimeHeroData,instanceId,slot)) return false;
        if (combatManager != null)
            combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog("Đã trang bị vào ô " + slot + ".");
        return true;
    }
    public bool UnequipGear(EquipmentSlot slot)
    {
        if (!IsGameplayHUDVisible || runtimeHeroData == null ||
            !EquipmentRuntime.Unequip(runtimeHeroData,slot)) return false;
        if (combatManager != null)
            combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog("Đã tháo trang bị ở ô " + slot + ".");
        return true;
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
        CancelGameplayEntry();
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

        // Only the legacy non-deploying load returns to the waiting menu.
        // Immediate loads must not restart the opening/black curtain first.
        runtimeHeroData.ApplyHeroBalance();
        RestoreSelectedStance();
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        if (!deploy) SetupPreGameUI(true);
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
        CancelGameplayEntry();
        if (UIManager.Instance != null) UIManager.Instance.HideMapTitle();
        hasDeployed = false;
        CancelNextWave();
        activeMonsters.Clear();
        if (runtimeHeroData != null)
        {
            runtimeHeroData.RestartRegionAfterDefeat();
            RestoreProgress();
            UpdateEventLog(WorldMapCatalog.Enabled(runtimeHeroData)
                ? "Thất bại: đánh lại wave 1 tại " + runtimeHeroData.mapName +
                    ". Giữ cấp, trang bị và lịch sử map đã mở."
                : "Thất bại: quay về map đầu của " + WorldNames.Region(runtimeHeroData.regionTheme) +
                    ", đợt 1/5. Giữ cấp và chỉ số nhân vật.");
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
        heroController.useStanceMoveSpeeds=useW2StanceMoveSpeeds;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero();
        RestoreProgress();
        if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
        CallNextWave();
    }

    // Return to the interactive title without reloading the Unity scene.
    // Save BEFORE clearing combat state, preserving inventory, map and hero.
    // If persistence fails, leave the active game running rather than silently
    // destroying progress. Opening music resumes when preGameUI is visible.
    public bool ReturnToMainMenu()
    {
        if (HasPendingConfirmation) return false;
        if (hasDeployed && runtimeHeroData != null)
        {
            CaptureSelectedStance();
            if (saveManager == null || !saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave))
            {
                UpdateEventLog("Không thể lưu tiến trình. Vẫn ở lại trò chơi.");
                Debug.LogWarning("[Opening] Return to title aborted: autosave failed.");
                return false;
            }
        }
        CancelGameplayEntry();
        hasDeployed=false;
        CancelNextWave();
        ClearTestCompanions();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        if (heroController != null) heroController.HideHero();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideMapTitle();
            UIManager.Instance.CloseDamageInfo();
            UIManager.Instance.SetPaused(false);
        }
        manualSelection=false;
        continueSlot=SaveSlot.AutoSave;
        SetupPreGameUI();
        Debug.Log("[Opening] Returned to main menu after saving current progress.");
        return true;
    }

    public void OnContinueClicked()
    {
        if (hasDeployed || HasPendingConfirmation) return;
        if (!CanContinue) { UpdateEventLog("Không tìm thấy bản lưu để tiếp tục."); return; }
        pendingChoice = MenuChoice.Continue;
        int level,map;
        string name = continueSlot == SaveSlot.AutoSave ? "Tự động" :
            continueSlot == SaveSlot.ManualSave1 ? "Ô 1" : "Ô 2";
        ShowConfirmation(saveManager != null &&
            saveManager.TryGetSlotProgress(continueSlot,out level,out map)
                ? "Tiếp tục từ " + name + " — Cấp " + level + ", Map " + map +
                    "? (Hai ô lưu tay không bị thay đổi)"
                : "Bạn có muốn tiếp tục từ " + name + " không?");
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
        bool fromTitle=preGameUI!=null && preGameUI.activeInHierarchy;
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
            int storedLevel,storedMap;
            if (!saveManager.TryGetSlotProgress(continueSlot,out storedLevel,out storedMap) ||
                loaded.currentLevel != storedLevel)
            {
                Destroy(loaded);
                Debug.LogError("[SaveLoad] Continue snapshot level mismatch in " + continueSlot);
                if (startMenu != null) startMenu.Refresh();
                return;
            }
            Debug.Log("[SaveLoad] Continue from " + continueSlot +
                ": level=" + loaded.currentLevel + " EXP=" + loaded.currentExp +
                " map=" + loaded.mapNumber);
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
            CancelGameplayEntry();
            hasDeployed = false;
            CancelNextWave();
            ClearTestCompanions();
            if (combatManager != null) combatManager.ForceClearAllMonsters();
            activeMonsters.Clear();
            if (heroController != null) heroController.HideHero();
            manualSelection = false;
            InitHeroData(fromTitle);
            if (heroController != null) heroController.ChangeAttackMode(0);
        }
        manualSelection = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (heroController != null) heroController.SetIdentityVisual(runtimeHeroData);
        if (UIManager.Instance != null) UIManager.Instance.ApplyDifficultyVisual(IsHardMode);
        DeployHero(fromTitle);
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

    // Explicit/manual world navigation. The wave loop never changes map.
    public bool TryTravelToWorldMap(string targetId)
    {
        if (!hasDeployed || runtimeHeroData == null || !WorldMapCatalog.Enabled(runtimeHeroData))
            return false;
        string reason = WorldMapCatalog.TravelBlockReason(runtimeHeroData, targetId);
        if (reason != null) { UpdateEventLog(reason); return false; }

        // Change only after validation; cancel old wave and all stale projectiles
        // and monsters so they cannot score kills in the destination node.
        CancelNextWave();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        ClearTestCompanions();
        if (!WorldMapCatalog.TryTravel(runtimeHeroData, targetId)) return false;

        // Prototype: travel starts a fresh battle from full HP. Reevaluate this
        // explicitly when designing travel costs and injuries.
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        if (heroController != null) heroController.SpawnHero();
        RestoreProgress();
        if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
        UpdateEventLog("Đã đến " + runtimeHeroData.mapName + " (" + targetId + ").");
        if (saveManager != null && !saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave))
            UpdateEventLog("Chưa lưu được bản đồ vừa chọn. Kiểm tra quyền ghi Saves.");
        CallNextWave();
        return true;
    }

    private void CallNextWave()
    {
        if (!hasDeployed || monsterSpawner == null) return;
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.TracePosition("CallNextWave before spawn");
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
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.TracePosition("CallNextWave after StartBattle");
    }

    void LateUpdate()
    {
        TraceEntryFrame("GameManager.LateUpdate before entry handoff");
        // EnvironmentManager (-200) has already finished its paused LateUpdate.
        // Release entry AFTER camera/actor steps so no camera-only half-frame
        // can run with a stale delta. The first wave still starts this frame;
        // actors, combat and camera first advance together in the next frame.
        if(enteringGameplay && entryCompletionPending)CompleteGameplayEntry();
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
                    if (WorldMapCatalog.Enabled(runtimeHeroData))
                        UpdateEventLog("Đã vượt qua " + runtimeHeroData.mapName +
                            ". Mở Bản đồ để xem các map kề; vẫn tiếp tục farm tại đây.");
                    else
                    {
                        UpdateEventLog("Đã tới " + runtimeHeroData.mapName +
                            (IsHardMode ? ": quái gây sát thương ×2, EXP ×3." : ": bình thường."));
                        if (UIManager.Instance != null) UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
                    }
                }
            }
            nextWaveCoroutine = StartCoroutine(WaitAndCallNextWave());
        }
    }

    private IEnumerator WaitAndCallNextWave()
    {
        // Time.deltaTime already follows gameplay timeScale. Movement speed
        // (including a stationary/regrouping Hero) must never alter this clock.
        int version=waveWaitVersion;
        double elapsed=0d;
        while(true)
        {
            yield return null;
            if(version!=waveWaitVersion)yield break;
            if (!hasDeployed) { nextWaveCoroutine = null; yield break; }
            if (Time.timeScale == 0f) continue;
            elapsed+=Mathf.Max(0f,Time.deltaTime);
            if(elapsed>=CurrentWaveDelay)break;
        }
        nextWaveCoroutine = null;
        if (hasDeployed) CallNextWave();
    }

    private void CancelNextWave()
    {
        waveWaitVersion++;
        if (nextWaveCoroutine != null) StopCoroutine(nextWaveCoroutine);
        nextWaveCoroutine = null;
        retryWaveSize = 0;
    }

    private void OnDisable()
    {
        // Restore presentation on cancellation, but never spawn a wave while
        // disabling the scene/component (including application shutdown).
        if(enteringGameplay)
        {
            resumeEntryWave=hasDeployed;
            CancelGameplayEntry();
            if(startMenu!=null)startMenu.EnterGameplayImmediately();
        }
        CancelNextWave();
        ClearTestCompanions();
    }
    private void OnEnable()
    {
        if(!resumeEntryWave)return;
        resumeEntryWave=false;
        if(hasDeployed && heroController!=null && heroController.IsDeployed)CallNextWave();
    }

    private void OnApplicationQuit()
    {
        if (saveManager == null || !hasDeployed || runtimeHeroData == null) return;
        CaptureSelectedStance();
        saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
    }
}
