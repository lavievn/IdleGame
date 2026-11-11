using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

public partial class GameManager : MonoBehaviour
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

    [Header("DEV ONLY — Controlled Loot Test A (default Off)")]
    [Tooltip("Editor/Development Build only: accepted kills give the primary Hero one Common demo sword. Not official progression.")]
    [SerializeField] private bool controlledLootTest;
    [Header("Production Loot v0.1 — prototype balance, configured explicitly in MainGame")]
    [SerializeField] private ProductionLootConfig productionLoot = new ProductionLootConfig();
    public ProductionLootConfig ProductionLoot => productionLoot;
    public static bool DevelopmentItemGrantsEnabled
    {
        get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
    public bool ControlledLootTestEnabled
    {
        get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return controlledLootTest;
#else
            return false;
#endif
        }
    }

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
    // Presentation-only access to existing B2 slots; includes a defeated actor.
    public HeroController TestCompanionAt(int index) => index>=0 && index<testCompanions.Count
        ? testCompanions[index].Controller : null;
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
        if (HasTribulationLiability || !IsGameplayHUDVisible || HasPendingConfirmation ||
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
            actor.traceVisualPositionChanges=heroController.traceVisualPositionChanges;
            actor.renderTraceFrameCount=heroController.renderTraceFrameCount;
            actor.enableRangedLaneMotion=heroController.enableRangedLaneMotion;
            actor.rangedFootMargin=heroController.rangedFootMargin;
            actor.rangedLaneAmplitude=heroController.rangedLaneAmplitude;
            actor.rangedLaneSpeed=heroController.rangedLaneSpeed;
            actor.rangedLaneHoldSeconds=heroController.rangedLaneHoldSeconds;
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

            rig.SetActive(true); // Deferred Awake binds damage popup to this actor.
            // Measure this clone AFTER Awake and sprite binding. Never inherit
            // the source's transient attack/bounce scale as spawn geometry.
            rect.localScale=Vector3.one;
            CultivationSpriteBridge.AttachHero(actor);
            Canvas.ForceUpdateCanvases();
            Vector2 spawn;
            if(!TryTestCompanionPosition(actor,out spawn))
            {
                Debug.LogWarning("[B2 TEST] No non-overlapping body position fits inside BattleArea.");
                Destroy(rig);Destroy(visual);Destroy(data);
                return null;
            }
            actor.SpawnHero(false,spawn); // One initial placement; never steal camera focus.
            actor.TracePosition("B2 companion after bounded spawn");

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

    // Full rendered frame is deliberately conservative: this also contains the
    // alpha silhouette in every pose, without requiring readable PNG textures.
    // Convert all four corners through BattleArea, just like Position/SetPosition;
    // neither screen pixels nor an anchoredPosition from a different parent enter here.
    private Rect TestCompanionBodyBounds(HeroController actor)
    {
        var bridge=actor.heroRect.GetComponent<CultivationSpriteBridge>();
        var graphic=bridge!=null?bridge.BodyGraphic:actor.heroRect.GetComponent<Image>();
        var rect=graphic!=null?graphic.rectTransform:actor.heroRect;
        float minX=float.PositiveInfinity,minY=minX,maxX=float.NegativeInfinity,maxY=maxX;
        for(int i=0;i<4;i++)
        {
            var p=environmentManager.BattleArea.InverseTransformPoint(rect.TransformPoint(new Vector3(
                i%2==0?rect.rect.xMin:rect.rect.xMax,i<2?rect.rect.yMin:rect.rect.yMax,0f)));
            minX=Mathf.Min(minX,p.x);minY=Mathf.Min(minY,p.y);
            maxX=Mathf.Max(maxX,p.x);maxY=Mathf.Max(maxY,p.y);
        }
        return new Rect(minX,minY,maxX-minX,maxY-minY);
    }

    private bool TryTestCompanionPosition(HeroController actor,out Vector2 spawn)
    {
        spawn=Vector2.zero;
        var area=environmentManager.BattleArea.rect;
        var feet=environmentManager.Position(actor.heroRect);
        var body=TestCompanionBodyBounds(actor);
        var offset=new Rect(body.xMin-feet.x,body.yMin-feet.y,body.width,body.height);
        float left=area.xMin-offset.xMin,right=area.xMax-offset.xMax;
        // Y is a foot lane, like Melee's existing lane movement. Keep the full
        // body bound on X, but do not force an alpha-padded 100-high frame into
        // the 100-high floor when ranged lanes are enabled.
        float bottom=actor.enableRangedLaneMotion?area.yMin:area.yMin-offset.yMin;
        float top=actor.enableRangedLaneMotion?area.yMax:area.yMax-offset.yMax;
        // World -> Canvas -> BattleArea round trips can differ by a few float
        // ULPs even when a 100-unit frame exactly fits the 100-unit Ground.
        const float conversionEpsilon=.001f;
        if(left>right+conversionEpsilon || bottom>top+conversionEpsilon)return false;
        if(left>right)left=right=(left+right)*.5f;
        if(bottom>top)bottom=top=(bottom+top)*.5f;
        var origin=environmentManager.Position(heroController.heroRect);
        float y=Mathf.Clamp(origin.y,bottom,top);
        y=actor.InitialFootY(environmentManager,y);
        var formation=environmentManager.GetComponent<HeroFormation>();
        float gap=formation!=null?Mathf.Max(1f,formation.spacing):100f;
        var peers=new List<Rect>();
        var candidates=new List<float>();
        float front=origin.x;
        foreach(var peer in HeroController.ActiveHeroes)
        {
            if(peer==null || !peer.IsDeployed)continue;
            var bounds=TestCompanionBodyBounds(peer);peers.Add(bounds);
            float x=environmentManager.Position(peer.heroRect).x;
            front=Mathf.Max(front,x);
            // First prefer the right, then use the left if the group is near an edge.
            candidates.Add(Mathf.Max(x+gap,bounds.xMax-offset.xMin+gap*.01f));
            candidates.Add(Mathf.Min(x-gap,bounds.xMin-offset.xMax-gap*.01f));
        }
        candidates.Insert(0,front+gap);
        foreach(float candidate in candidates)
        {
            float x=Mathf.Clamp(candidate,left,right);
            var next=new Rect(x+offset.xMin,y+offset.yMin,body.width,body.height);
            bool overlaps=false;
            foreach(var peer in HeroController.ActiveHeroes)
                if(peer!=null && peer.IsDeployed &&
                    Mathf.Abs(x-environmentManager.Position(peer.heroRect).x)<gap-conversionEpsilon)
                { overlaps=true;break; }
            foreach(var peer in peers)
                if(next.xMin<peer.xMax && next.xMax>peer.xMin && next.yMin<peer.yMax && next.yMax>peer.yMin)
                { overlaps=true;break; }
            if(overlaps)continue;
            spawn=new Vector2(x,y);return true;
        }
        return false;
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
    [Tooltip("Escape recovery only: gameplay seconds before replaying an unfinished escaped wave.")]
    [Min(0f)] public float waveDelay = 5f;
    public float CurrentWaveDelay => Mathf.Max(0f,waveDelay);
    // The timer above is retained ONLY for replaying an escaped unfinished wave.
    public const float DefaultDistancePerWave=1000f;
    [Header("EXPLORATION WAVES — Ground-local units, not camera distance")]
    [Min(1f)] public float distancePerWave=DefaultDistancePerWave;
    public float CurrentDistancePerWave => float.IsNaN(distancePerWave) ||
        float.IsInfinity(distancePerWave) || distancePerWave<=0f?DefaultDistancePerWave:distancePerWave;
    private double explorationDistance;
    private bool waitingForExploration;
    private int explorationFrame=-1;
    public float ExplorationDistance => (float)explorationDistance;
    public bool IsWaitingForExploration => waitingForExploration;
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
    private enum MenuChoice { None, Continue, NewGame, CultivationUpgrade, InterruptedTrial }
    private MenuChoice pendingChoice;
    private SaveSlot continueSlot;
    private bool manualSelection;
    private StartMenuUI startMenu;
    private bool enteringGameplay, openingRevealed;
    private bool entryCompletionPending;
    private bool resumeEntryWave;
    private float entryElapsed;
    private HeroSpawnVfx entryVfx;
    [Header("MAP ENTRY — unscaled presentation seconds, not combat timing")]
    [Min(.01f)] public float mapEntryCoverSeconds=.15f;
    private Image mapEntryCover;
    private bool shortMapEntry;
    private float mapEntryCoverElapsed;
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
            " shortMapEntry="+shortMapEntry+
            " entryCoverActive="+(mapEntryCover!=null && mapEntryCover.gameObject.activeInHierarchy)+
            " entryCoverAlpha="+(mapEntryCover!=null?mapEntryCover.color.a.ToString("R",System.Globalization.CultureInfo.InvariantCulture):"none")+
            " waveMembers="+activeMonsters.Count+" waitingWave="+(nextWaveCoroutine!=null)+
            " exploring="+waitingForExploration+" explorationDistance="+
            explorationDistance.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
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
    public bool CanContinue
    {
        get
        {
            if (saveManager == null) return false;
            if (saveManager.HasInterruptedDevelopmentTrial())
            {
                continueSlot = SaveSlot.AutoSave;
                return true;
            }
            return manualSelection ? saveManager.HasSave(continueSlot) : saveManager.TryGetLatestSlot(out continueSlot);
        }
    }
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
        ExplorationProgressHUD.Install(this);
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
        runtimeHeroData.worldClimate = WorldClimateState.AtDawn(System.Guid.NewGuid().GetHashCode());
        runtimeHeroData.currentLevel = 1;
        runtimeHeroData.currentExp = 0;
        runtimeHeroData.expToNextLevel = 100;
        LegacyRealmSnapshotCompatibility.Initialize(runtimeHeroData);
        runtimeHeroData.addedHealth = runtimeHeroData.addedDamage = runtimeHeroData.statPoints = 0;
        // New characters start with the established melee test stance.
        // Randomized combat aptitudes belong to future character generation.
        runtimeHeroData.savedAttackMode = AttackMode.Melee;
        EquipmentInventory.InitializeNewRun(runtimeHeroData);
        EquipmentRuntime.InitializeNewRun(runtimeHeroData);
        runtimeHeroData.combatSkills = new CombatSkillProgress();
        runtimeHeroData.originHealth = CombatBalance.HeroBaseHealth;
        runtimeHeroData.meleeGrowthBonus = 0;
        runtimeHeroData.meleeGrowthThroughLevel = 1;
        runtimeHeroData.balanceVersion = 0;
        runtimeHeroData.difficulty = 0;
        runtimeHeroData.mapNumber = 1; runtimeHeroData.completedWavesInMap = 0; runtimeHeroData.mapProgressVersion = 1;
        runtimeHeroData.mapVisits = 1;
        runtimeHeroData.regionIndex = -1; runtimeHeroData.mapName = ""; WorldNames.AssignMap(runtimeHeroData);
        StoryOrigin.InitializeNewHero(runtimeHeroData);
        if (!WorldMapCatalog.StartNew(runtimeHeroData, StoryOrigin.HanVu.Village))
        {
            runtimeHeroData.originStory = null;
            Debug.LogError("[StoryOrigin] Hàn Vụ map unavailable; new run remains in legacy mode.");
        }

        InitializeCultivation();
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
        EnsureCultivationBinding();
        CancelNextWave();
        // Only title choices run the long cinematic. Every actual map entry
        // shares the same paused, hidden spawn -> appear -> first-wave pipeline.
        bool cinematic = showOpeningTransition && startMenu != null && startMenu.IsCinematic &&
            preGameUI != null && preGameUI.activeInHierarchy;
        if (!cinematic)
        {
            ShowMapEntryCover();
            if (startMenu != null) startMenu.EnterGameplayImmediately();
            if (preGameUI != null) preGameUI.SetActive(false);
        }
        hasDeployed = true;
        ClearTestCompanions();
        if(combatManager!=null)combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        heroController.HideHero();
        heroController.useStanceMoveSpeeds=useW2StanceMoveSpeeds;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        if (runtimeHeroData.combatSkills.health == 0)
        {
            // A saved defeat stays a defeat until the player explicitly retries.
            hasDeployed = false;
            HideMapEntryCover();
            if (startMenu != null) startMenu.EnterGameplayImmediately();
            if (preGameUI != null) preGameUI.SetActive(false);
            RestoreProgress();
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            return;
        }
        ResetEntryCamera();
        var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
        Vector2 spawn=space!=null && space.BattleArea!=null
            ?new Vector2(space.BattleArea.rect.xMin+space.Width*.8f,0f):Vector2.zero;
        heroController.SpawnHero(true,spawn);
        enteringGameplay=true;openingRevealed=false;entryCompletionPending=false;entryElapsed=0f;
        shortMapEntry=!cinematic;
        entryVfx=HeroSpawnVfx.Prepare(heroController);
        SetEntryPaused(true);
        RestoreProgress();
        if(cinematic)
        {
            if(!startMenu.BeginGameplayTransition())startMenu.EnterGameplayImmediately();
        }
        else
        {
            OnOpeningRevealCompleted(); // VFX only; no title cinematic or stale opening overlay.
        }
    }

    private void ShowMapEntryCover()
    {
        if(mapEntryCover==null)
        {
            var root=new GameObject("Map Entry Cover",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            // Overlay canvases are top-level screen projections, like Readable
            // HUD. Do not inherit GameController's arbitrary scene transform.
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=1000;canvas.scaleFactor=1f;
            var fill=new GameObject("Map Entry Shade",typeof(RectTransform),typeof(Image));
            fill.transform.SetParent(root.transform,false);
            mapEntryCover=fill.GetComponent<Image>();mapEntryCover.raycastTarget=true;
            var rect=mapEntryCover.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        mapEntryCoverElapsed=0f;mapEntryCover.color=Color.black;
        mapEntryCover.transform.parent.gameObject.SetActive(true);
    }
    private void HideMapEntryCover()
    {
        if(mapEntryCover!=null)mapEntryCover.transform.parent.gameObject.SetActive(false);
        shortMapEntry=false;mapEntryCoverElapsed=0f;
    }

    private static void SetEntryPaused(bool paused)
    {
        if(UIManager.Instance!=null)UIManager.Instance.SetPaused(paused);
        else Time.timeScale=paused?0f:1f;
    }
    private void ResetEntryCamera()
    {
        var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
        if(space!=null)space.ResetCameraForMapEntry();
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
        try { if(entryVfx==null || !entryVfx.Begin())entryCompletionPending=true; }
        catch(System.Exception ex)
        {
            Debug.LogWarning("[MapEntry] Appear asset unavailable: "+ex.Message);
            entryCompletionPending=true;
        }
    }
    void Update()
    {
        TraceEntryFrame("GameManager.Update before entry");
        if(!enteringGameplay)return;
        // Own the pause across the handoff; no actor/camera advances in a gap.
        SetEntryPaused(true);
        entryElapsed+=Mathf.Max(0f,Time.unscaledDeltaTime);
        if(shortMapEntry && openingRevealed && mapEntryCover!=null)
        {
            mapEntryCoverElapsed+=Mathf.Max(0f,Time.unscaledDeltaTime);
            float seconds=float.IsNaN(mapEntryCoverSeconds)||float.IsInfinity(mapEntryCoverSeconds)
                ?.15f:Mathf.Clamp(mapEntryCoverSeconds,.01f,HeroSpawnVfx.Duration*.25f);
            mapEntryCover.color=new Color(0,0,0,1f-Mathf.Clamp01(mapEntryCoverElapsed/seconds));
        }
        if(!openingRevealed && entryVfx!=null)entryVfx.HoldForCinematic();
        if(!openingRevealed && entryElapsed>OpeningCinematicUI.EntrySeconds+.5f)
        {
            // Missing/disabled opening cannot leave the game black or paused.
            if(startMenu!=null)startMenu.EnterGameplayImmediately();
            OnOpeningRevealCompleted();
        }
        if(enteringGameplay && openingRevealed)
        {
            try { if(entryVfx==null || !entryVfx.Advance(Time.unscaledDeltaTime))entryCompletionPending=true; }
            catch(System.Exception ex)
            {
                Debug.LogWarning("[MapEntry] Appear interrupted: "+ex.Message);
                entryCompletionPending=true;
            }
        }
        // Re-arm at the completion request, even if the earlier reveal window
        // has expired while the VFX played. Include the still-paused LateUpdate.
        if(entryCompletionPending)
            foreach(var hero in HeroController.ActiveHeroes)
                if(hero!=null)hero.ArmPositionTrace();
        TraceEntryFrame("GameManager.Update after entry");
    }
    private void CancelGameplayEntry()
    {
        var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
        if(space!=null)space.CancelCameraEntryHandoff();
        HideMapEntryCover();
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
        if(heroController!=null)heroController.TraceVisualPosition("Gameplay release before",12);
        CancelGameplayEntry();
        if(startMenu!=null)startMenu.EnterGameplayImmediately();
        if(hasDeployed && heroController!=null && heroController.IsDeployed)
        {
            var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
            if(space!=null)space.BeginCameraEntryHandoff();
            if(UIManager.Instance!=null)UIManager.Instance.ShowMapTitle(runtimeHeroData.mapName);
            string narration = StoryOrigin.Arrive(runtimeHeroData);
            if (narration != null)
            {
                CommitOriginChange(narration);
                UIManager.Instance?.ShowOriginOpening(runtimeHeroData);
            }
            CallNextWave();
        }
        TraceEntryFrame("GameManager after gameplay release and first wave");
        if(heroController!=null)heroController.TraceVisualPosition("Gameplay release after first wave",12);
    }

    private void CaptureSelectedStance()
    {
        if (runtimeHeroData != null && heroController != null)
            runtimeHeroData.SetSavedAttackMode(WeaponBasicAttack.Resolve(runtimeHeroData));
    }

    private void RestoreSelectedStance()
    {
        if (runtimeHeroData != null && heroController != null)
            heroController.BindBasicAttackData(runtimeHeroData,false);
    }

    internal bool GrantWeaponSkillSamples()
    {
        if (!DevelopmentItemGrantsEnabled || HasTribulationLiability ||
            !IsGameplayHUDVisible || runtimeHeroData == null) return false;
        bool added = false;
        foreach (string id in EquipmentFoundation.SkillSampleIds)
        {
            if (runtimeHeroData.equipmentItems.Exists(item => item != null && item.templateId == id)) continue;
            added |= EquipmentRuntime.AddInstance(runtimeHeroData, EquipmentFoundation.CreateSkillSample(id));
        }
        UpdateEventLog(added ? "Đã nhận vũ khí Hoàng mẫu trong Hòm đồ." : "Vũ khí mẫu đã có trong Hòm đồ.");
        return added;
    }

    // V2 individual gear: all 8 positions share one canonical inventory.
    public bool GrantDemoSword()
    {
        if(HasTribulationLiability)return false;
        if(!DevelopmentItemGrantsEnabled)return false;
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentRuntime.GrantDemoSword(runtimeHeroData)) return false;
        UpdateEventLog("Đã nhận Thiết Kiếm thử nghiệm.");
        return true;
    }
    // Reusable test sword: preserves older saved swords; new ones have
    // fixed latent affixes and a demonstrative quality ceiling.
    public bool GrantForgeTestSword()
    {
        if(HasTribulationLiability)return false;
        if(!DevelopmentItemGrantsEnabled)return false;
        if(!IsGameplayHUDVisible || runtimeHeroData==null)return false;
        if(!EquipmentRuntime.AddInstance(runtimeHeroData,
            EquipmentRefinement.CreateForgeDemoSword()))return false;
        UpdateEventLog("Đã thêm kiếm rèn thử nghiệm.");
        return true;
    }

    public bool GrantForgeMaterials()
    {
        if(HasTribulationLiability)return false;
        if(!DevelopmentItemGrantsEnabled)return false;
        if(!IsGameplayHUDVisible || runtimeHeroData==null)return false;
        if(!EquipmentInventory.Add(runtimeHeroData,EquipmentRefinement.MaterialId,100))
            return false;
        UpdateEventLog("Đã thêm 100 Nguyên liệu rèn.");
        return true;
    }

    public bool ApplyForgeTest(string instanceId,
        EquipmentRefinement.Operation operation,out string message)
    {
        if(HasTribulationLiability){message="Đang Độ Kiếp";return false;}
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

    public bool CanAllocateHeroAttribute => !HasTribulationLiability && IsGameplayHUDVisible && !HasPendingConfirmation &&
        runtimeHeroData!=null && runtimeHeroData.statPoints>0 && combatManager!=null && heroController!=null &&
        combatManager.CurrentHeroCombatState!=null &&
        combatManager.CurrentHeroCombatState.Data==runtimeHeroData && combatManager.CurrentHeroCombatState.CanFight;

    public bool AllocateHeroHealth() { return AllocateHeroAttribute(true); }
    public bool AllocateHeroPhysicalAttack() { return AllocateHeroAttribute(false); }
    private bool AllocateHeroAttribute(bool health)
    {
        if(!CanAllocateHeroAttribute || (health ? runtimeHeroData.addedHealth>int.MaxValue-6 : runtimeHeroData.addedDamage==int.MaxValue))return false;
        if(health)runtimeHeroData.AllocateHealth();else runtimeHeroData.AllocateDamage();
        // Existing ratio-preserving refresh; leave attack timer, target and life intact.
        combatManager.RefreshRegisteredHeroStats(heroController,false);
        UpdateEventLog(health?"Đã dùng 1 điểm: +6 HP.":"Đã dùng 1 điểm: +1 Physical Attack.");
        if(UIManager.Instance!=null)UIManager.Instance.RefreshCharacterProgression();
        return true;
    }

    public bool EquipGear(string instanceId,EquipmentSlot slot)
    {
        if(HasTribulationLiability)return false;
        if (!IsGameplayHUDVisible || runtimeHeroData == null ||
            !EquipmentRuntime.Equip(runtimeHeroData,instanceId,slot)) return false;
        if (combatManager != null)
        {
            if(slot==EquipmentSlot.Weapon)combatManager.RefreshWeaponAttack(heroController);
            else combatManager.RefreshRegisteredHeroStats(heroController,true);
        }
        UpdateEventLog("Đã trang bị vào ô " + slot + ".");
        return true;
    }
    public bool UnequipGear(EquipmentSlot slot)
    {
        if(HasTribulationLiability)return false;
        if (!IsGameplayHUDVisible || runtimeHeroData == null ||
            !EquipmentRuntime.Unequip(runtimeHeroData,slot)) return false;
        if (combatManager != null)
        {
            if(slot==EquipmentSlot.Weapon)combatManager.RefreshWeaponAttack(heroController);
            else combatManager.RefreshRegisteredHeroStats(heroController,true);
        }
        UpdateEventLog("Đã tháo trang bị ở ô " + slot + ".");
        return true;
    }

    // Inventory and equipment UI only affects the current run and primary Hero.
    // HP is recalculated by the existing CombatManager ratio-preserving path.
    public bool EquipInventoryItem(string itemId)
    {
        if(HasTribulationLiability)return false;
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentInventory.Equip(runtimeHeroData,itemId)) return false;
        if (combatManager != null) combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog("Đã trang bị: " + EquipmentCatalog.Find(itemId).name);
        return true;
    }

    public bool UnequipInventoryItem()
    {
        if(HasTribulationLiability)return false;
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        if (!EquipmentInventory.Unequip(runtimeHeroData)) return false;
        if (combatManager != null) combatManager.RefreshRegisteredHeroStats(heroController,true);
        UpdateEventLog("Đã tháo vật phẩm về kho.");
        return true;
    }

    public bool GrantInventorySamples()
    {
        if(HasTribulationLiability)return false;
        if(!DevelopmentItemGrantsEnabled)return false;
        if (!IsGameplayHUDVisible || runtimeHeroData == null) return false;
        bool added=EquipmentInventory.GrantSamples(runtimeHeroData);
        foreach (string id in new[]{EquipmentFoundation.DefenseSample,EquipmentFoundation.ManaSample})
        {
            if (runtimeHeroData.equipmentItems.Exists(item=>item!=null && item.templateId==id)) continue;
            var template=EquipmentFoundation.FindTemplate(id);
            added |= EquipmentRuntime.AddInstance(runtimeHeroData,EquipmentFoundation.CreateCommon(id,1,template.sampleMainStat));
        }
        if (added) UpdateEventLog("Đã nhận đồ mẫu DEV trong Hòm đồ.");
        return added;
    }

    // --- CÁC HÀM XỬ LÝ SAVE/LOAD TỪ UI MỚI ---
    // UI 0.0.51: saving a manual slot is only available after deployment.
    // The result must come from actual IO rather than assuming a file was written.
    public bool TryManualSave(SaveSlot slot)
    {
        if(HasTribulationLiability)return false;
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
        if(HasTribulationLiability)return false;
        if (HasPendingConfirmation || (saveManager != null && saveManager.HasInterruptedDevelopmentTrial())) return false;
        if (saveManager == null || !saveManager.HasSave(slot)) { UpdateEventLog("Ô lưu này chưa có dữ liệu."); return false; }
        if(saveManager.NeedsCultivationMigration(slot)){RequestCultivationMigration(slot,deploy,false);return false;}

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
        CultivationGameplayBinding prepared;
        if(!PrepareCultivation(loaded,out prepared)){Destroy(loaded);return false;}
        return ApplyLoadedCandidate(loaded,slot,deploy,prepared);
    }
    bool ApplyLoadedCandidate(EntityDataSO loaded,SaveSlot slot,bool deploy,CultivationGameplayBinding prepared,bool fromTitle=false)
    {
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
        ReplaceCultivation(prepared);
        recentEvents.Clear(); recentDamage.Clear(); fullEventHistory.Clear();
        damageHistory.Clear();
        continueSlot = slot;
        manualSelection = !fromTitle;

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
            DeployHero(fromTitle);
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
            if (!HasTribulationLiability && hasDeployed && !HasPendingConfirmation && runtimeHeroData != null && saveManager != null)
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
        DisposeCultivation();
        CancelNextWave();
        activeMonsters.Clear();
        if (runtimeHeroData != null)
        {
            runtimeHeroData.RestartRegionAfterDefeat();
            RestoreProgress();
            UpdateEventLog(WorldMapCatalog.Enabled(runtimeHeroData)
                ? "Thất bại: đánh lại wave " + runtimeHeroData.worldMapLoopWave + " tại " + runtimeHeroData.mapName +
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
        if(HasTribulationLiability){RetryTribulationCommit();return;}
        if (hasDeployed || HasPendingConfirmation || runtimeHeroData == null) return;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        runtimeHeroData.combatSkills.health = -1; // Explicit Retry revives HP, retaining Mana and cooldowns.
        DeployHero();
    }

    // Return to the interactive title without reloading the Unity scene.
    // Save BEFORE clearing combat state, preserving inventory, map and hero.
    // If persistence fails, leave the active game running rather than silently
    // destroying progress. Opening music resumes when preGameUI is visible.
    public bool ReturnToMainMenu()
    {
        if(HasTribulationLiability)return false;
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
        DisposeCultivation();
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
        if(HasTribulationLiability)return;
        if (hasDeployed || HasPendingConfirmation) return;
        if (!CanContinue) { UpdateEventLog("Không tìm thấy bản lưu để tiếp tục."); return; }
        if (OfferInterruptedTrialRecovery()) return;
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
        if(HasTribulationLiability)return;
        if (HasPendingConfirmation) return;
        if (OfferInterruptedTrialRecovery()) return;
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
        if(HasTribulationLiability)return;
        if (HasPendingConfirmation) return;
        OnNewGameClicked();
        if (pendingChoice != MenuChoice.NewGame) return;
        ShowConfirmation("Đặt lại nhân vật sẽ tạo nhân vật cấp 1 mới và thay ô lưu tự động. Hai ô lưu thủ công được giữ nguyên. Bạn có muốn tiếp tục?");
    }
    public void OnConfirmResetClicked()
    {
        if(HasTribulationLiability)return;
        var choice = pendingChoice;
        if(choice==MenuChoice.CultivationUpgrade){ConfirmCultivationMigration();return;}
        if(choice==MenuChoice.InterruptedTrial){ConfirmInterruptedTrialRecovery();return;}
        if (choice == MenuChoice.None) return;
        bool fromTitle=preGameUI!=null && preGameUI.activeInHierarchy;
        pendingChoice = MenuChoice.None;
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (choice == MenuChoice.Continue)
        {
            if(saveManager!=null && saveManager.NeedsCultivationMigration(continueSlot)){RequestCultivationMigration(continueSlot,true,fromTitle);return;}
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
            CultivationGameplayBinding prepared;
            if(!PrepareCultivation(loaded,out prepared)){Destroy(loaded);return;}
            if (runtimeHeroData != null) Destroy(runtimeHeroData);
            runtimeHeroData = loaded;
            ReplaceCultivation(prepared);
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
        if(IsCultivationMigrationPending){CancelCultivationMigration();return;}
        interruptedTrialExpectedHash = null;
        pendingChoice = MenuChoice.None;
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }
    // Legacy scene bindings cannot override the automatic map sequence.
    public void SetDifficulty(int mode) { if (runtimeHeroData != null) RestoreProgress(); }

    public void SetHeroAttackMode(int modeIndex)
    {
        // Keep serialized UnityEvent bindings safe; manual stance no longer owns primary attacks.
        if(heroController!=null)heroController.SyncWeaponMode();
    }

    public bool GrantDevWeaponSamples()
    {
        if(HasTribulationLiability)return false;
        if(!IsGameplayHUDVisible || runtimeHeroData==null)return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool bow=EquipmentRuntime.AddInstance(runtimeHeroData,EquipmentFoundation.CreateDemoBow());
        bool staff=EquipmentRuntime.AddInstance(runtimeHeroData,EquipmentFoundation.CreateDemoStaff());
        if(bow || staff)UpdateEventLog("[WEAPON TEST] Đã cấp Cung (+10 Physical) / Trượng (+10 Magical), chưa trang bị.");
        return bow && staff;
#else
        return false;
#endif
    }
    [ContextMenu("DEV — Grant Bow and Staff (Play Mode)")]
    private void GrantDevWeaponSamplesFromInspector() {GrantDevWeaponSamples();}

    // Explicit/manual world navigation. The wave loop never changes map.
    public bool TryTravelToWorldMap(string targetId)
    {
        if(HasTribulationLiability)return false;
        if (!hasDeployed || runtimeHeroData == null || !WorldMapCatalog.Enabled(runtimeHeroData))
            return false;
        string reason = WorldMapCatalog.TravelBlockReason(runtimeHeroData, targetId);
        if (reason != null) { UpdateEventLog(reason); return false; }
        if(Cultivation!=null && Cultivation.SettlePending()!=CultivationReason.None)return false;

        // Change only after validation; cancel old wave and all stale projectiles
        // and monsters so they cannot score kills in the destination node.
        CancelGameplayEntry();ShowMapEntryCover();
        CancelNextWave();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        ClearTestCompanions();
        if (!WorldMapCatalog.TryTravel(runtimeHeroData, targetId)) { HideMapEntryCover();return false; }

        // Prototype: travel starts a fresh battle from full HP. Reevaluate this
        // explicitly when designing travel costs and injuries.
        hasDeployed=false;
        DeployHero();
        UpdateEventLog("Đã đến " + runtimeHeroData.mapName + " (" + targetId + ").");
        if (saveManager != null && !saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave))
            UpdateEventLog("Chưa lưu được bản đồ vừa chọn. Kiểm tra quyền ghi Saves.");
        return true;
    }

    internal bool TryOriginInteraction()
    {
        if (!IsGameplayHUDVisible || HasTribulationLiability) return false;
        var state = runtimeHeroData.originStory;
        if (state == null) return false;
        var stage = state.stage;
        string narration = StoryOrigin.Interact(runtimeHeroData);
        if (narration == null) return false;
        if (stage != state.stage) CommitOriginChange(narration);
        else if (UIManager.Instance != null) UIManager.Instance.ShowOriginNotice(runtimeHeroData, narration);
        return true;
    }

    internal void RecordOriginTrip()
    {
        if (IsGameplayHUDVisible && !HasTribulationLiability && HanVuNarrative.RecordTrip(runtimeHeroData))
            CommitOriginChange(null);
    }

    internal bool TryTalkToWoodcutter()
    {
        if (!IsGameplayHUDVisible || HasTribulationLiability || !HanVuNarrative.AtWoodcutter(runtimeHeroData))
            return false;
        if (HanVuNarrative.TalkToWoodcutter(runtimeHeroData)) CommitOriginChange(null);
        return true;
    }

    internal bool TryChooseHerbs(bool accept)
    {
        if (!IsGameplayHUDVisible || HasTribulationLiability || !StoryOrigin.ChooseHerbs(runtimeHeroData, accept))
            return false;
        CommitOriginChange(accept ? "Ta đã nhận lời tìm lá bạc." : "Ta xin để chuyện tìm thuốc lại sau.");
        return true;
    }

    internal OriginEncounter OpenOriginEncounter()
    {
        if (!IsGameplayHUDVisible || HasTribulationLiability) return null;
        var offer = HanVuEncounters.Available(runtimeHeroData);
        if (offer != null && HanVuEncounters.Observe(runtimeHeroData, offer.Id)) CommitOriginChange(null);
        return offer;
    }

    internal bool TryChooseOriginEncounter(string id, EncounterChoice choice)
    {
        if (!IsGameplayHUDVisible || HasTribulationLiability) return false;
        var offer = HanVuEncounters.Available(runtimeHeroData);
        if (offer == null || !HanVuEncounters.Choose(runtimeHeroData, id, choice)) return false;
        CommitOriginChange(choice == EncounterChoice.Participated ? offer.Result : offer.Decline + ".");
        return true;
    }

    internal void ToggleOriginPrompts()
    {
        if (!IsGameplayHUDVisible || HasTribulationLiability || StoryOrigin.Definition(runtimeHeroData) == null) return;
        runtimeHeroData.originStory.hideStoryPrompts = !runtimeHeroData.originStory.hideStoryPrompts;
        runtimeHeroData.isDirty = true;
        CommitOriginChange(null);
    }

    private void CommitOriginChange(string narration)
    {
        if (narration != null)
        {
            UpdateEventLog(narration);
            if (UIManager.Instance != null) UIManager.Instance.ShowOriginNotice(runtimeHeroData, narration);
        }
        CaptureSelectedStance();
        if (saveManager != null && !saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave))
            UpdateEventLog("Chưa lưu được mốc truyện Hàn Vụ. Có thể thử lưu lại.");
    }

    private void CallNextWave()
    {
        if (HasTribulationLiability || !hasDeployed || enteringGameplay || monsterSpawner == null ||
            activeMonsters.Count > 0 || StoryOrigin.IsSafe(runtimeHeroData)) return;
        waitingForExploration=false;explorationDistance=0d;
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.TracePosition("CallNextWave before spawn");
        activeMonsters.Clear();

        RestoreProgress();
        int spawnCount = StoryOrigin.IsEliteWave(runtimeHeroData) ? 1 :
            retryWaveSize > 0 ? retryWaveSize : Random.Range(1, 4);
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
        TickWorldClimate();
        if(HasTribulationLiability)return; // Isolated trial owns world/combat until durable result.
        TickCultivation(); // Sole cultivation clock; runs before entry can release the pause.
        TraceEntryFrame("GameManager.LateUpdate before entry handoff");
        // EnvironmentManager (-200) has already finished its paused LateUpdate.
        // Release entry AFTER camera/actor steps so no camera-only half-frame
        // can run with a stale delta. The first wave still starts this frame;
        // actors, combat and camera first advance together in the next frame.
        if(enteringGameplay && entryCompletionPending)CompleteGameplayEntry();
        TickExplorationProgress();
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
        if (combatManager != null) combatManager.ClearWaveKeepingMagicVisuals();
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

    public void OnMonsterDied(GameObject deadMonster) => FinishMonsterDeath(deadMonster, false, false);

    internal void OnAcceptedMonsterDied(GameObject deadMonster, bool storyElite) =>
        FinishMonsterDeath(deadMonster, true, storyElite);

    private void FinishMonsterDeath(GameObject deadMonster, bool accepted, bool storyElite)
    {
        if (!activeMonsters.Remove(deadMonster)) return;
        if (hasDeployed && activeMonsters.Count == 0 && nextWaveCoroutine == null)
        {
            if (runtimeHeroData != null)
            {
                bool changed = runtimeHeroData.CompleteWave();
                RestoreProgress();
                if (accepted)
                {
                    bool credited = StoryOrigin.CreditWave(runtimeHeroData, storyElite, out string narration);
                    bool sawNote = runtimeHeroData.worldMapWaveProgress >= 2 && HanVuNarrative.ObserveNote(runtimeHeroData);
                    if (credited || sawNote)
                        CommitOriginChange(narration ?? (sawNote ? HanVuNarrative.TrailNote : null));
                }
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
            waitingForExploration=true;explorationDistance=0d;
            // Movement in the death frame was calculated while that wave was
            // still alive. Never bank it for the next encounter.
            explorationFrame=Time.frameCount;
        }
    }

    private void TickExplorationProgress()
    {
        if(!waitingForExploration || explorationFrame==Time.frameCount)return;
        explorationFrame=Time.frameCount;
        if(!hasDeployed || enteringGameplay || Time.timeScale==0f || Time.deltaTime<=0f ||
            activeMonsters.Count!=0 || nextWaveCoroutine!=null)return;
        float common=float.MaxValue;int count=0;
        var seen=new HashSet<HeroController>();
        foreach(var hero in HeroController.ActiveHeroes)
        {
            var state=combatManager!=null?combatManager.StateFor(hero):null;
            if(hero==null || !hero.IsDeployed || state==null || !state.CanFight || !seen.Add(hero))continue;
            count++;
            float step=hero.MovementFrame==Time.frameCount && hero.IsExplorationMovement?
                hero.MovementDistanceThisFrame:0f;
            float budget=hero.MovementBudgetThisFrame;
            if(float.IsNaN(step) || float.IsInfinity(step) || float.IsNaN(budget) || float.IsInfinity(budget))step=0f;
            else step=Mathf.Clamp(step,0f,Mathf.Max(0f,budget));
            common=Mathf.Min(common,step);
        }
        if(count==0)return;
        // Per-frame minimum, never sum, centroid/camera delta or a fastest Hero.
        // New/unticked members contribute zero; death removes them next sample.
        explorationDistance+=common;
        if (common > 0f && explorationDistance >= CurrentDistancePerWave * HanVuNarrative.TrailExplorationFraction &&
            HanVuNarrative.ObserveKnot(runtimeHeroData)) CommitOriginChange(HanVuNarrative.TrailKnot);
        if(explorationDistance>=CurrentDistancePerWave)
        {
            CallNextWave(); // Drops excess distance; one threshold = one wave.
        }
    }

    private IEnumerator WaitAndCallNextWave()
    {
        // Escape recovery only. Normal completed waves use exploration distance.
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
        waitingForExploration=false;explorationDistance=0d;explorationFrame=Time.frameCount;
        waveWaitVersion++;
        if (nextWaveCoroutine != null) StopCoroutine(nextWaveCoroutine);
        nextWaveCoroutine = null;
        retryWaveSize = 0;
    }

    private bool applicationQuitting;

    private void OnDisable()
    {
        weatherAudio.Stop();
        if (environmentManager != null) environmentManager.ClearWorldClimate();
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        Application.wantsToQuit -= AllowQuitDuringTrial;
#endif
        if(HasTribulationLiability){trialInterrupted=true;LastTribulationNotice="Trial bị gián đoạn · checkpoint giữ nguyên, cần kiểm tra";}
        CancelCultivationMigration(false);
        DisposeCultivation();
        if(combatManager!=null)combatManager.SuspendLootEncounter();
        var space=environmentManager!=null?environmentManager:EnvironmentManager.Instance;
        if(space!=null)space.CancelCameraEntryHandoff();
        // During quit the menu's child objects may already be destroyed.
        // Entry presentation is only needed for a reversible component disable.
        if(enteringGameplay && !applicationQuitting)
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
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        Application.wantsToQuit += AllowQuitDuringTrial;
#endif
        if(hasDeployed)EnsureCultivationBinding();
        if(combatManager!=null)combatManager.ResumeLootEncounter();
        if(!resumeEntryWave)return;
        resumeEntryWave=false;
        if(hasDeployed && heroController!=null && heroController.IsDeployed)
        { hasDeployed=false;DeployHero(); }
    }

    private void OnApplicationQuit()
    {
        applicationQuitting = true;
        if (HasTribulationLiability || saveManager == null || !hasDeployed || runtimeHeroData == null) return;
        CaptureSelectedStance();
        saveManager.SaveGame(runtimeHeroData, SaveSlot.AutoSave);
    }
    private void OnDestroy()
    {
        trialPrepared?.Dispose();if(trialCandidate!=null)Destroy(trialCandidate);
        CancelCultivationMigration(false);
        DisposeCultivation();
        if(mapEntryCover!=null)Destroy(mapEntryCover.transform.parent.gameObject);
    }
}
