using UnityEngine;
using TuTienCore;

[DefaultExecutionOrder(-200)]
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("MÔI TRƯỜNG")]
    [SerializeField] private RectTransform[] groundDetails;
    [SerializeField] private RectTransform battleArea;

    [Header("0.0.52 - NEN XA (GOUND SKY / MOUNTAIN)")]
    [Tooltip("Lop sau cung: SkyDistant.png. Mac dinh tai Resources/Parallax/SkyDistant; keo Sprite vao day neu muon thay theo map. Toc do 10%, scale 169%.")]
    public Sprite customDistantMountains;
    [Tooltip("Lop nui phia truoc: MountainNear.png. Mac dinh tai Resources/Parallax/MountainNear; co the override tu Inspector. Toc do 30%, scale 130%.")]
    public Sprite customFarTrees;

    [Min(0f)] public float scrollSpeed = 150f;

    [Header("HERO WALKING — render-frame safety (not the camera/combat clock)")]
    [Tooltip("Hero/B2 walking alone discards stall time above this limit; no catch-up backlog. Below 30 FPS walking intentionally slows. Monsters, camera and combat retain Time.deltaTime.")]
    [Min(.001f)] public float maxHeroMovementDeltaTime=BattleMotion.DefaultMaxMovementDeltaTime;
    public float HeroMovementDeltaTime(float elapsed)
    {
        return Time.timeScale<=0f?0f:BattleMotion.MovementDeltaTime(elapsed,maxHeroMovementDeltaTime);
    }

    [Header("VÙNG CAMERA - tỷ lệ chiều rộng Ground")]
    [Range(0.2f, 0.8f)] public float explorationHeroX = 0.5f;
    [Range(0f, 1f)] public float speedZoneLeft = 0.06f;
    [Range(0f, 1f)] public float speedZoneRight = 0.91f;

    // Applied inside the saved edges so upgrading a scene preserves serialized
    // 0.06/0.91 values while narrowing each side by 25% of their original span.
    [Range(0f, 0.49f)] public float speedZoneInset = 0.25f;
    public float CurrentBackgroundSpeed { get; private set; }

    [Header("CAMERA THEO HERO - DEADZONE + SMOOTHDAMP")]
    // Keep the old flag serialized name so existing .54g scenes keep this ON.
    public bool useSoftZoneCamera = true;
    [Min(.01f)] public float cameraSmoothTime = .2f;
    // Wider central deadzone: Hero may lead toward the red border before
    // SmoothDamp begins. The 0.2s damping response remains unchanged.
    [Range(.02f,.45f)] public float cameraDeadzoneRatio = .38f;
    [Range(.4f,.6f)] public float cameraPreferredX = .5f;
    [Header("CAMERA KHI HERO DỪNG ĐÁNH")]
    [Range(.3f,.5f)] public float cameraIdleRestX = .4f;
    [Min(.05f)] public float cameraIdleSmoothTime = .6f;
    [Header("0.0.50 - TRÔI CAMERA KHI ĐỘI ĐỨNG ĐÁNH")]
    public bool advanceStationaryCombatToRight = true;
    [Min(.05f)] public float stationaryCombatSmoothTime = .6f;
    private float stationaryCombatVelocity;
    [Header("CAMERA — THEO TÂM NHÓM NHÂN VẬT")]
    // OFF compares against the previous single-Hero camera without a revert.
    public bool cameraUseCombatCenter = true;
    [Header("LÀM MƯỢT KHI TÂM NHÓM THAY ĐỔI")]
    [Min(.05f)] public float cameraCombatFocusSmoothTime = .7f;
    [Min(1f)] public float cameraCombatFocusMaxSpeed = 240f;
    // Keep RAW combat mean available for debugging while the filtered
    // focus is the subject sent into DeadzoneCamera.
    public float CurrentCameraFocusX { get; private set; }
    public float CurrentCameraSmoothedFocusX { get; private set; }
    public int CurrentCameraFocusCount { get; private set; }
    private bool previousCombatCenterMode = true;
    private readonly CombatFocusSmoother combatFocusSmoother = new CombatFocusSmoother();
    private readonly DeadzoneCamera deadzoneCamera = new DeadzoneCamera();
    private float plannedCameraPan, plannedBackgroundPan;
    private bool hasPlannedPan;
    public float CameraFollowVelocity => useMultiZoneCamera?multiZoneCamera.Velocity:deadzoneCamera.Velocity;

    [Header("W2 MULTI-ZONE — widths are full fractions of BattleArea; speeds in Ground units/s")]
    public bool useMultiZoneCamera=true;
    [Range(.01f,.96f)] public float greenZoneWidth=.2f;
    [Range(.02f,.98f)] public float yellowZoneWidth=.45f;
    [Range(.03f,1f)] public float redZoneWidth=.75f;
    [Min(1f)] public float minFollowSpeed=30f;
    [Min(1f)] public float maxFollowSpeed=450f;
    [Min(1f)] public float maxAcceleration=900f;
    [Min(1f)] public float maxDeceleration=1200f;
    [Min(1f)] public float correctionSpeed=900f;
    [Min(.1f)] public float maxCameraStep=48f;
    private readonly MultiZoneCamera multiZoneCamera=new MultiZoneCamera();
    private HeroController multiZoneSubject;
    private bool multiZoneFocusReady;
    private float previousMultiZoneFocus;
    public float MultiZoneVelocity => multiZoneCamera.Velocity;

    [Header("DIRECTIONAL FRAMING — screen-space target, primary camera subject")]
    public bool directionalCameraFraming;
    [Range(.5f,.8f)] public float leftwardFramingX=.65f;
    [Min(.05f)] public float directionalFramingSmoothTime=.35f;
    private float framingRatio=.65f,framingDesired=.65f,framingPreviousHeroX;
    private readonly CombatFocusSmoother directionalFramingSmoother=new CombatFocusSmoother();
    private bool framingSampleReady;

    // Only call while a new map is hidden. Gameplay subject changes never reset velocity.
    public void ResetCameraForMapEntry()
    {
        multiZoneCamera.Reset();combatFocusSmoother.Reset();multiZoneFocusReady=false;multiZoneSubject=null;
        framingSampleReady=false;directionalFramingSmoother.Reset();framingRatio=framingDesired=leftwardFramingX;
        deadzoneCamera.Reset();stationaryCombatVelocity=0f;hasPlannedPan=false;
    }

    [Header("CAMERA ENTRY ONLY — normal gameplay camera is unchanged")]
    [Min(.1f)] public float cameraEntryDuration=.8f;
    [Min(1f)] public float cameraEntryMaxSpeed=450f;
    [Min(1f)] public float cameraEntryAcceleration=900f;
    [Min(.1f)] public float cameraEntryMaxStep=12f;
    public bool IsCameraEntryHandoff { get; private set; }
    private float entryCameraElapsed, entryCameraVelocity, entryCameraMatched, entryCameraRequestedPan;

    public void BeginCameraEntryHandoff()
    {
        if(useMultiZoneCamera)
        {
            // The same bounded controller starts from the hidden map's reset
            // state; no second velocity or release-to-another-controller phase.
            IsCameraEntryHandoff=false;
            traceEntryStartFrame=Time.frameCount;TraceEntryBoundary("unified-follow");return;
        }
        CancelCameraEntryHandoff();
        deadzoneCamera.Reset();combatFocusSmoother.Reset();stationaryCombatVelocity=0f;
        hasPlannedPan=false;
        if(cameraTarget!=null && cameraTarget.IsDeployed)IsCameraEntryHandoff=true;
        traceEntryStartFrame=Time.frameCount;
        TraceEntryBoundary(IsCameraEntryHandoff?"began":"not-started");
    }
    public void CancelCameraEntryHandoff()
    {
        if(!IsCameraEntryHandoff)return;
        IsCameraEntryHandoff=false;
        entryCameraElapsed=entryCameraVelocity=entryCameraMatched=0f;
        deadzoneCamera.Reset();combatFocusSmoother.Reset();stationaryCombatVelocity=0f;
        hasPlannedPan=false;
        TraceEntryBoundary("cancelled");
    }
    private static float EntrySetting(float value,float fallback)
    {
        return float.IsNaN(value)||float.IsInfinity(value)||value<=0f?fallback:value;
    }
    private void PlanPresentationPan(out float pan,out float background)
    {
        var combat = CombatManager.Instance;
        if (combat != null && combat.TryPlanGateCamera(this,
            Mathf.Min(maxCameraStep, maxFollowSpeed * Time.deltaTime), out pan))
        {
            background = pan;
            return;
        }
        if(traceEntryPlanFrame!=Time.frameCount)traceEntryPlanCount=0;
        traceEntryPlanFrame=Time.frameCount;traceEntryPlanCount++;
        traceEntryWasActive=IsCameraEntryHandoff;
        traceEntryVelocityBefore=entryCameraVelocity;
        traceCameraVelocityBefore=CameraFollowVelocity;
        if(useMultiZoneCamera)
        {
            MultiZonePresentationPan(out pan);background=pan;
            traceCameraVelocityAfter=multiZoneCamera.Velocity;
            entryCameraRequestedPan=pan;traceRawBackgroundPan=background;
            return;
        }
        if(IsCameraEntryHandoff && (cameraTarget==null || !cameraTarget.IsDeployed))CancelCameraEntryHandoff();
        PresentationPan(out pan,out background);
        traceCameraVelocityAfter=deadzoneCamera.Velocity;
        traceRawBackgroundPan=background;
        entryCameraRequestedPan=pan;
        if(!IsCameraEntryHandoff || Time.timeScale==0f || Time.deltaTime<=0f)return;
        float speed=EntrySetting(cameraEntryMaxSpeed,450f);
        // Entry alone limits a long render frame. Never change movement deltaTime
        // or the original camera's parameters/formula.
        float dt=Mathf.Min(Time.deltaTime,EntrySetting(cameraEntryMaxStep,12f)/speed);
        float wanted=Mathf.Clamp(pan/Time.deltaTime,-speed,speed);
        traceEntryStepDelta=dt;traceEntryWantedVelocity=wanted;
        entryCameraVelocity=Mathf.MoveTowards(entryCameraVelocity,wanted,
            EntrySetting(cameraEntryAcceleration,900f)*dt);
        entryCameraVelocity=Mathf.Clamp(entryCameraVelocity,-speed,speed);
        float limited=entryCameraVelocity*dt;
        entryCameraElapsed+=Time.deltaTime;
        bool matched=Mathf.Abs(limited-pan)<.0001f;
        if(!matched)
        {
            // The normal camera did not apply its requested movement: do not
            // store that unfulfilled spring impulse for the following frame.
            deadzoneCamera.Reset();stationaryCombatVelocity=0f;
        }
        entryCameraMatched=matched?entryCameraMatched+Time.deltaTime:0f;
        traceEntryMatchTime=entryCameraMatched;
        traceEntryLimitedPan=limited;
        pan=background=limited;
        if(entryCameraElapsed>=EntrySetting(cameraEntryDuration,.8f) && entryCameraMatched>=.1f)
        {
            // Do not reset here: several consecutive original plans were applied
            // exactly, so its current velocity is already safe to continue.
            IsCameraEntryHandoff=false;
            entryCameraElapsed=entryCameraVelocity=entryCameraMatched=0f;
            TraceEntryBoundary("matched-release");
        }
    }

    // Diagnostic bookkeeping only. Never step a camera smoother to sample it.
    private int traceUpdateFrame=-1, tracePlanFrame=-1, traceAppliedFrame=-1;
    private float traceActualPan, traceActualBackgroundPan;
    private string tracePanSource="not-run";
    private int traceEntryStartFrame=-1,traceEntryPlanFrame=-1,traceEntryPlanCount;
    private bool traceEntryWasActive;
    private float traceEntryVelocityBefore,traceCameraVelocityBefore,traceCameraVelocityAfter;
    private float traceRawBackgroundPan,traceEntryStepDelta,traceEntryWantedVelocity,traceEntryMatchTime,traceEntryLimitedPan;
    private string traceEntryBoundary="none";
    private int traceEntryBoundaryFrame=-1;
    private void TraceEntryBoundary(string reason)
    {
        traceEntryBoundary=reason;traceEntryBoundaryFrame=Time.frameCount;
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)
            {
                // Include the boundary frame plus 30 subsequent frames even if
                // the original reveal window expired before camera release.
                hero.ArmPositionTraceForFrames(31);
                hero.TracePosition("Camera entry "+reason);
            }
    }
    public float LastAppliedCameraPan => traceActualPan;
    public string DescribeCameraTrace()
    {
        return " updateFrame="+traceUpdateFrame+" planFrame="+tracePlanFrame+
            " hasPlan="+hasPlannedPan+" plannedPan="+TraceNumber(plannedCameraPan)+
            " plannedBackgroundPan="+TraceNumber(plannedBackgroundPan)+
            " appliedFrame="+traceAppliedFrame+" actualPan="+TraceNumber(traceActualPan)+
            " actualBackgroundPan="+TraceNumber(traceActualBackgroundPan)+" panSource="+tracePanSource+
            " deadzoneVelocity="+TraceNumber(deadzoneCamera.Velocity)+
            " combatOffset="+TraceNumber(combatFocusSmoother.Offset)+
            " combatVelocity="+TraceNumber(combatFocusSmoother.Velocity)+
            " stationaryVelocity="+TraceNumber(stationaryCombatVelocity)+
            " cameraEntry="+IsCameraEntryHandoff+" entryCameraElapsed="+TraceNumber(entryCameraElapsed)+
            " entryCameraVelocity="+TraceNumber(entryCameraVelocity)+
            " entryRequestedPan="+TraceNumber(entryCameraRequestedPan)+
            " entryRawBackgroundPan="+TraceNumber(traceRawBackgroundPan)+
            " entryPlanWasActive="+traceEntryWasActive+" entryPlanCount="+traceEntryPlanCount+
            " entryVelocityBefore="+TraceNumber(traceEntryVelocityBefore)+
            " cameraVelocityBeforePlan="+TraceNumber(traceCameraVelocityBefore)+
            " cameraVelocityAfterPlan="+TraceNumber(traceCameraVelocityAfter)+
            " entryStepDt="+TraceNumber(traceEntryStepDelta)+
            " entryWantedVelocity="+TraceNumber(traceEntryWantedVelocity)+
            " entryLimitedPan="+TraceNumber(traceEntryLimitedPan)+" entryMatchTime="+TraceNumber(traceEntryMatchTime)+
            " entryBoundary="+traceEntryBoundary+" entryBoundaryFrame="+traceEntryBoundaryFrame+
            " multiZone="+useMultiZoneCamera+" multiZoneVelocity="+TraceNumber(multiZoneCamera.Velocity)+
            " cameraTarget="+(cameraTarget!=null?cameraTarget.gameObject.name:"none");
    }
    private static string TraceNumber(float value)
    {
        return value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
    }
    private static void TraceFrame(string stage)
    {
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.TracePositionFrame(stage);
    }

    private GroundPresentation groundPresentation;
    private Canvas battleCanvas;
    private float logicalWidth;
    private int presentationWidth;
    private int presentationCombatWidth;
    [Header("BATTLE VIEW — screen pixels, presentation only")]
    [Tooltip("Nâng toàn bộ battleground; không thay đổi tọa độ logic hoặc camera combat.")]
    [SerializeField] private float battleVerticalOffsetPixels = 20f;
    private float presentationVerticalOffset = float.NaN;

    // The battle keeps one coordinate system for its entire lifetime. Resizing
    // changes only the screen-space projection, never anchors/ranges/shot endpoints.
    private void InstallStableBattleCanvas()
    {
        if (battleArea == null) return;
        Canvas sourceCanvas = null;
        UnityEngine.UI.CanvasScaler sourceScaler = null;
        for (Transform p = battleArea.parent; p != null; p = p.parent)
        {
            var canvas = p.GetComponent<Canvas>();
            if (canvas == null) continue;
            sourceCanvas = canvas;
            sourceScaler = p.GetComponent<UnityEngine.UI.CanvasScaler>();
            break;
        }
        logicalWidth = sourceScaler != null && sourceScaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize
            ? sourceScaler.referenceResolution.x : Width;
        if (logicalWidth <= 0f) logicalWidth = 1920f;
        float height = battleArea.rect.height;
        if (sourceCanvas != null)
        {
            var go = new GameObject("Stable Battle Canvas", typeof(RectTransform), typeof(Canvas));
            battleCanvas = go.GetComponent<Canvas>();
            battleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            battleCanvas.sortingOrder = sourceCanvas.sortingOrder;
            battleCanvas.sortingLayerID = sourceCanvas.sortingLayerID;
            battleCanvas.targetDisplay = sourceCanvas.targetDisplay;
            battleCanvas.additionalShaderChannels = sourceCanvas.additionalShaderChannels;
            battleArea.SetParent(go.transform, false);
        }
        battleArea.anchorMin = battleArea.anchorMax = new Vector2(.5f, 0f);
        battleArea.sizeDelta = new Vector2(logicalWidth, height);
        if (battleCanvas != null) battleArea.anchoredPosition = Vector2.zero;
        SyncBattleProjection();
    }

    public void SyncBattleProjection()
    {
        if (battleCanvas == null || Screen.width <= 0) return;
        int projectedWidth=ManagementWindowLayout.CombatPixels(Screen.width);
        if (presentationWidth == Screen.width &&
            presentationCombatWidth == projectedWidth &&
            presentationVerticalOffset == battleVerticalOffsetPixels) return;
        presentationWidth=Screen.width;
        presentationCombatWidth=projectedWidth;
        presentationVerticalOffset=battleVerticalOffsetPixels;
        battleCanvas.scaleFactor=
            ManagementWindowLayout.BattleCanvasScale(Screen.width,logicalWidth);
        // Only the projection's origin changes: the world-space Ground,
        // Hero, Monster and projectile coordinates remain untouched.
        battleArea.anchoredPosition=new Vector2(
            ManagementWindowLayout.BattleLocalOffsetX(Screen.width,logicalWidth),
            battleVerticalOffsetPixels / Mathf.Max(.01f, battleCanvas.scaleFactor));
    }

    internal void SetTrialTerrainHidden(bool hidden)
    {
        hasPlannedPan = false;
        if (hidden) EnsureGroundPresentation();
        if (groundPresentation != null) groundPresentation.SetTrialHidden(hidden);
    }

    internal void ApplyWorldClimate(WorldClimateFrame frame, bool lighting, bool hdrSky, bool rain2D,
        WorldClimateAssetAdapter adapter, WorldWeatherVisual visual = default)
    {
        if (groundPresentation != null) groundPresentation.ApplyWorldClimate(frame, lighting, hdrSky, rain2D, adapter, visual);
    }

    internal void ClearWorldClimate()
    {
        if (groundPresentation != null) groundPresentation.ClearWorldClimate();
    }

    public bool HasLoopingTerrain => groundPresentation != null;
    public void SetTerrainTint(Color color) { EnsureGroundPresentation(); if (groundPresentation != null) groundPresentation.SetTerrainTint(color); }
    private void EnsureGroundPresentation()
    {
        if (groundPresentation == null) groundPresentation = GroundPresentation.Install(this, groundDetails);
    }
    void Start() { EnsureGroundPresentation(); }

    // Only deployed living Heroes are eligible for camera follow.
    // Dead actors disappear immediately and cannot drive presentation pan.
    private HeroController cameraTarget;

    [Header("TẦM ĐÁNH - tỷ lệ chiều rộng Ground")]
    [Range(0.01f, 0.15f)] public float meleeRangeRatio = 0.035f;
    [Range(0.1f, 0.6f)] public float heroPhysicalRangeRatio = 0.36f;
    [Range(0.1f, 0.6f)] public float heroMagicRangeRatio = 0.42f;
    [Range(0.1f, 0.6f)] public float monsterPhysicalRangeRatio = 0.28f;
    [Range(0.1f, 0.6f)] public float monsterMagicRangeRatio = 0.36f;

    public RectTransform BattleArea => battleArea;
    public float Width => battleArea != null ? battleArea.rect.width : 0f;
    internal Vector2 ScreenPixelsToBattleUnits(Vector2 pixels)
    {
        float scale = battleCanvas != null ? battleCanvas.scaleFactor :
            ManagementWindowLayout.BattleCanvasScale(Screen.width, Width);
        return pixels / Mathf.Max(.01f, scale);
    }
    public float HomeX => battleArea.rect.xMin + Width * explorationHeroX;
    private float ZoneMin => Mathf.Clamp01(Mathf.Min(speedZoneLeft, speedZoneRight));
    private float ZoneMax => Mathf.Clamp01(Mathf.Max(speedZoneLeft, speedZoneRight));
    private float ZoneInset => Mathf.Min(0.49f, Mathf.Max(0f, speedZoneInset)) * (ZoneMax - ZoneMin);
    public float ZoneLeftX => battleArea.rect.xMin + Width * (ZoneMin + ZoneInset);
    public float ZoneRightX => battleArea.rect.xMin + Width * (ZoneMax - ZoneInset);
    public bool IsScrolling { get; private set; }
    // Ground units/second actually applied this frame, including zone correction.
    public float CurrentCameraSpeed { get; private set; }

    public void FollowHero(HeroController hero)
    {
        if(useMultiZoneCamera)
        {
            cameraTarget=hero;hasPlannedPan=false;return;
        }
        CancelCameraEntryHandoff();
        if(hero!=null)hero.TracePosition("FollowHero before camera reset");
        cameraTarget = hero;
        deadzoneCamera.Reset(); // Start or Retry must not inherit old momentum.
        combatFocusSmoother.Reset(); // New spawn/actor starts with fresh group offset.
        hasPlannedPan = false;
        stationaryCombatVelocity = 0f;
        if(hero!=null)hero.TracePosition("FollowHero after camera reset");
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (battleArea == null && groundDetails != null)
        {
            foreach (var detail in groundDetails)
                if (detail != null) { battleArea = detail.parent as RectTransform; break; }
        }
        InstallStableBattleCanvas();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
    void OnDisable() { CancelCameraEntryHandoff(); }

    // All AI moves before CombatManager.Update. Camera presentation runs after both.
    void Update()
    {
        // Cover the handoff itself (up to 120 frames) as well as its boundaries.
        // A stuck handoff cannot enable unbounded frame logging.
        if((IsCameraEntryHandoff || useMultiZoneCamera) && traceEntryStartFrame>=0 && Time.frameCount-traceEntryStartFrame<120)
            foreach(var hero in HeroController.ActiveHeroes)
                if(hero!=null)hero.ArmPositionTraceForFrames(31);
        TraceFrame("Environment.Update before projection/movement");
        traceUpdateFrame=Time.frameCount;
        hasPlannedPan = false;
        SyncBattleProjection(); // Also runs when paused or before deployment.
        if (Time.timeScale == 0f || battleArea == null || Width <= 0f)
        {
            TraceFrame("Environment.Update skipped");return;
        }
        TraceFrame("Environment.Update after projection before movement");
        float dt = Time.deltaTime;
        foreach (var hero in HeroController.ActiveHeroes)
            if (hero != null && hero.IsDeployed) hero.TickMovement(this, dt);
        float cameraPan, backgroundPan;
        PlanPresentationPan(out cameraPan, out backgroundPan);
        plannedCameraPan = cameraPan;
        plannedBackgroundPan = backgroundPan;
        hasPlannedPan = true;
        tracePlanFrame=Time.frameCount;
        TraceFrame("Environment.Update after camera plan before monsters");
        float groundMinusCamera = backgroundPan - cameraPan;
        if (CombatManager.Instance != null && CombatManager.Instance.isActiveAndEnabled)
            CombatManager.Instance.TickMonsterAwareness(this, dt);
        foreach (var monster in MonsterController.ActiveMonsters)
            if (monster != null && monster.IsAlive) monster.TickMovement(this, dt, groundMinusCamera);
        // Monsters may have entered a hero's range during this frame.
        foreach (var hero in HeroController.ActiveHeroes)
            if (hero != null && hero.IsDeployed) hero.RefreshCombatState(this);
        TraceFrame("Environment.Update after movement/plan");
    }

    void LateUpdate()
    {
        TraceFrame("Environment.LateUpdate before pan");
        traceAppliedFrame=Time.frameCount;
        traceActualPan=traceActualBackgroundPan=0f;
        tracePanSource="skipped";
        IsScrolling = false;
        CurrentCameraSpeed = 0f;
        CurrentBackgroundSpeed = 0f;
        if (Time.timeScale == 0f || battleArea == null || Width <= 0f)
        {
            TraceFrame("Environment.LateUpdate skipped");return;
        }
        float pan, backgroundPan;
        // Update used this same pan to compute the monsters' Ground-camera
        // compensation. Advancing the soft camera again here would desync them.
        // CombatManager can kill the camera subject AFTER EnvironmentManager.Update
        // but BEFORE this LateUpdate. Never apply a stale, pre-death camera pan.
        if(useMultiZoneCamera && hasPlannedPan)
        {
            // A casualty after Update cannot step the velocity controller twice.
            // Keep this bounded plan for surviving actors; rebase next Update.
            bool living=false;
            foreach(var hero in HeroController.ActiveHeroes)
                if(hero!=null && hero.IsDeployed) { living=true;break; }
            pan=living?plannedCameraPan:0f;backgroundPan=pan;
            if(!living)ResetCameraForMapEntry();
            tracePanSource="planned-multi-zone";
        }
        else if (hasPlannedPan && cameraTarget != null && cameraTarget.IsDeployed)
        {
            pan = plannedCameraPan;
            backgroundPan = plannedBackgroundPan;
            tracePanSource="planned";
        }
        else
        {
            PlanPresentationPan(out pan, out backgroundPan);
            tracePanSource="recomputed";
        }
        hasPlannedPan = false;
        PanWorld(pan);
        PanEnvironment(backgroundPan);
        traceActualPan=pan;
        traceActualBackgroundPan=backgroundPan;
        CurrentBackgroundSpeed = Time.deltaTime > 0f ? backgroundPan / Time.deltaTime : 0f;
        CurrentCameraSpeed = Time.deltaTime > 0f ? pan / Time.deltaTime : 0f;
        if(useMultiZoneCamera)previousMultiZoneFocus=CurrentCameraSmoothedFocusX+pan;
        if(useMultiZoneCamera && directionalCameraFraming && cameraTarget!=null && cameraTarget.IsDeployed) {
            framingPreviousHeroX=Position(cameraTarget.heroRect).x;framingSampleReady=true;
        }
        IsScrolling = Mathf.Abs(pan) > 0.01f;
        TraceFrame("Environment.LateUpdate after pan");
    }

    private void MultiZonePresentationPan(out float pan)
    {
        pan=0f;
        if(Time.timeScale==0f || Time.deltaTime<=0f || battleArea==null || Width<=0f)return;
        if(cameraTarget==null || !cameraTarget.IsDeployed)
        {
            cameraTarget=null;
            foreach(var hero in HeroController.ActiveHeroes)
                if(hero!=null && hero.IsDeployed) { cameraTarget=hero;break; }
        }
        if(cameraTarget==null)
        {
            ResetCameraForMapEntry();CurrentCameraFocusX=CurrentCameraSmoothedFocusX=0f;
            CurrentCameraFocusCount=0;return;
        }
        float heroX=Position(cameraTarget.heroRect).x;int count=1;
        float tracking=0f;
        if(directionalCameraFraming) {
            float step=framingSampleReady && multiZoneSubject==cameraTarget && cameraTarget.MovementFrame==Time.frameCount?heroX-framingPreviousHeroX:0f;
            float leftTarget=float.IsNaN(leftwardFramingX)||float.IsInfinity(leftwardFramingX)?.65f:Mathf.Clamp(leftwardFramingX,.5f,.8f);
            if(Mathf.Abs(step)>.001f)framingDesired=step<0?leftTarget:1f-leftTarget;
            // Retain the last direction while standing in combat; never snap back to center.
            framingRatio=leftTarget+directionalFramingSmoother.Step(framingDesired-leftTarget,Time.deltaTime,Mathf.Max(.05f,directionalFramingSmoothTime),1f);
            tracking=-step/Time.deltaTime;
        }
        float raw=cameraUseCombatCenter?CombatCenterX(heroX,out count):heroX;
        // Preserve the absolute filtered center when the anchor Hero dies or
        // changes. Retain focus AND pan velocities across gameplay roster changes.
        if(multiZoneFocusReady && multiZoneSubject!=cameraTarget)
            combatFocusSmoother.Rebase(previousMultiZoneFocus-heroX-combatFocusSmoother.Offset);
        multiZoneSubject=cameraTarget;multiZoneFocusReady=true;
        float offset=combatFocusSmoother.Step(raw-heroX,Time.deltaTime,
            cameraCombatFocusSmoothTime,cameraCombatFocusMaxSpeed);
        float focus=heroX+offset;
        CurrentCameraFocusX=raw;CurrentCameraSmoothedFocusX=focus;CurrentCameraFocusCount=count;
        pan=multiZoneCamera.Pan(focus,battleArea.rect.xMin,battleArea.rect.xMax,Time.deltaTime,
            greenZoneWidth,yellowZoneWidth,redZoneWidth,minFollowSpeed,maxFollowSpeed,
            maxAcceleration,maxDeceleration,correctionSpeed,maxCameraStep,
            directionalCameraFraming?framingRatio:.5f,tracking,directionalCameraFraming);
    }

    // Equal-weight center of all deployed Heroes and living, ON-SCREEN monsters.
    // Ignore terrain, effects, projectiles, pooled bodies and distant off-screen
    // spawn points. Sampling before Monster.TickMovement adds at most one frame
    // of lag but preserves the established Update/LateUpdate shared pan contract.
    private float CombatCenterX(float fallbackX, out int count)
    {
        float sum = 0f;
        count = 0;
        foreach (var hero in HeroController.ActiveHeroes)
        {
            if (hero == null || !hero.IsDeployed || hero.heroRect == null) continue;
            sum += Position(hero.heroRect).x;
            count++;
        }
        foreach (var monster in MonsterController.ActiveMonsters)
        {
            if (monster == null || !monster.IsAlive || monster.Rect == null) continue;
            float x = Position(monster.Rect).x;
            if (x < battleArea.rect.xMin || x > battleArea.rect.xMax) continue;
            sum += x;
            count++;
        }
        return count == 0 ? fallbackX : sum / count;
    }

    private void PresentationPan(out float cameraPan, out float backgroundPan)
    {
        cameraPan = backgroundPan = 0f;
        if (Time.timeScale == 0f || battleArea == null || Time.deltaTime <= 0f) return;
        // All fallen Hero visuals are hidden in 0.0.50; no corpse camera.
        if (cameraTarget == null || !cameraTarget.IsDeployed)
        {
            cameraTarget = null;
            foreach (var hero in HeroController.ActiveHeroes)
                if (hero != null && hero.IsDeployed) { cameraTarget = hero; break; }
            deadzoneCamera.Reset(); // Do not carry velocity from a dead subject.
            combatFocusSmoother.Reset();
            stationaryCombatVelocity = 0f;
        }
        if (cameraTarget == null)
        {
            CurrentCameraFocusX = 0f;
            CurrentCameraSmoothedFocusX = 0f;
            CurrentCameraFocusCount = 0;
            deadzoneCamera.Reset();
            combatFocusSmoother.Reset();
            stationaryCombatVelocity = 0f;
            return;
        }
        float heroX = Position(cameraTarget.heroRect).x;
        CurrentCameraFocusX = heroX;
        CurrentCameraSmoothedFocusX = heroX;
        CurrentCameraFocusCount = 1;
        if (useSoftZoneCamera && cameraTarget.IsDeployed && !cameraTarget.IsDead)
        {
            int actorCount = 1;
            float focusX = cameraUseCombatCenter
                ? CombatCenterX(heroX, out actorCount) : heroX;
            CurrentCameraFocusX = focusX;
            CurrentCameraFocusCount = actorCount;
            if (previousCombatCenterMode != cameraUseCombatCenter)
            {
                // Do not carry camera velocity or relative group offset across
                // toggles between Hero-only and all living combatants.
                deadzoneCamera.Reset();
                combatFocusSmoother.Reset();
                previousCombatCenterMode = cameraUseCombatCenter;
            }
            if (cameraUseCombatCenter)
            {
                // Same raw group mean as .54g. Smooth only its offset from
                // the current camera Hero, retaining quick Hero follow while
                // preventing a sudden centroid jump from entering monsters.
                float filteredOffset = combatFocusSmoother.Step(focusX - heroX,
                    Time.deltaTime,cameraCombatFocusSmoothTime,cameraCombatFocusMaxSpeed);
                focusX = heroX + filteredOffset;
            }
            else
                combatFocusSmoother.Reset();
            CurrentCameraSmoothedFocusX = focusX;
            bool anyHeroMoved = cameraTarget.MovementDistanceThisFrame > .001f;
            if (cameraUseCombatCenter && !anyHeroMoved)
                foreach (var hero in HeroController.ActiveHeroes)
                    if (hero != null && hero.IsDeployed &&
                        hero.MovementDistanceThisFrame > .001f)
                    { anyHeroMoved = true; break; }

            // While EVERY Hero has stopped to fight, advance the whole world
            // smoothly toward the RIGHT red zone (trailing/rightmost Hero).
            // This avoids wasting the last third of the screen during combat.
            // Exploration and moving combat still use the established
            // group-focus deadzone and SmoothDamp, unchanged.
            bool fighting = false;
            float rightmostHeroX = float.NegativeInfinity;
            foreach (var hero in HeroController.ActiveHeroes)
            {
                if (hero == null || !hero.IsDeployed) continue;
                if (hero.CurrentState == HeroState.Combat) fighting = true;
                rightmostHeroX = Mathf.Max(rightmostHeroX, Position(hero.heroRect).x);
            }
            bool advanceIdle = advanceStationaryCombatToRight && fighting &&
                !anyHeroMoved && !float.IsNegativeInfinity(rightmostHeroX);
            if (advanceIdle)
            {
                // The rightmost Hero is the LAST actor behind the team on a
                // left-to-right view where Heroes travel toward screen left.
                float distance = Mathf.Max(0f, ZoneRightX - rightmostHeroX);
                cameraPan = BattleMotion.StationaryCombatPan(
                    distance, ref stationaryCombatVelocity,
                    stationaryCombatSmoothTime, Time.deltaTime);
                deadzoneCamera.Reset(); // Idle advance owns camera smoothing.
            }
            else
            {
                stationaryCombatVelocity = 0f;
                cameraPan = deadzoneCamera.Pan(focusX,ZoneLeftX,ZoneRightX,
                    battleArea.rect.xMin,battleArea.rect.xMax,Time.deltaTime,
                    cameraSmoothTime,cameraDeadzoneRatio,cameraPreferredX,
                    anyHeroMoved,cameraIdleRestX,cameraIdleSmoothTime,heroX);
            }
        }
        else
        {
            // Legacy hard-zone camera can still be selected for debugging.
            // Its old dead-body tracking branch has been removed.
            deadzoneCamera.Reset();
            combatFocusSmoother.Reset();
            stationaryCombatVelocity = 0f;
            cameraPan = BattleMotion.ZoneCameraStep(heroX, ZoneLeftX, ZoneRightX,
                scrollSpeed, Time.deltaTime);
        }
        // In deadzone mode Ground and ALL actors share the same view pan.
        // The old formula (Ground = Hero movement) caused scenery to drift
        // even while the camera was stationary inside the deadzone.
        backgroundPan = useSoftZoneCamera && cameraTarget.IsDeployed
            && !cameraTarget.IsDead ? cameraPan
            : cameraTarget.IsDeployed && cameraTarget.MovementDistanceThisFrame > .001f
                ? cameraTarget.MovementDistanceThisFrame : cameraPan;
    }

    public Vector2 Position(RectTransform rect)
    {
        return battleArea.InverseTransformPoint(rect.position);
    }

    public void SetPosition(RectTransform rect, Vector2 position)
    {
        Vector3 local = battleArea.InverseTransformPoint(rect.position);
        local.x = position.x;
        local.y = position.y;
        rect.position = battleArea.TransformPoint(local);
    }

    public bool IsVisible(RectTransform rect)
    {
        float x = Position(rect).x;
        return x >= battleArea.rect.xMin && x <= battleArea.rect.xMax;
    }

    public bool HasEscapedRight(RectTransform rect)
    {
        if (rect == null || battleArea == null) return false;
        // Wait until the entire body has passed the edge, not just its pivot.
        Vector3 leftFoot = rect.TransformPoint(new Vector3(rect.rect.xMin, 0f, 0f));
        return battleArea.InverseTransformPoint(leftFoot).x > battleArea.rect.xMax + 32f;
    }

    public float AttackRange(AttackMode mode, bool hero)
    {
        var profile = hero ? CombatBalance.HeroDev : CombatBalance.MonsterDev;
        return profile != null ? profile.Range(mode) : DefaultAttackRange(mode,hero);
    }
    public float DefaultAttackRange(AttackMode mode, bool hero)
    {
        float ratio = meleeRangeRatio;
        if (mode == AttackMode.RangedPhysical)
            ratio = hero ? heroPhysicalRangeRatio : monsterPhysicalRangeRatio;
        else if (mode == AttackMode.RangedMagic)
            ratio = hero ? heroMagicRangeRatio : monsterMagicRangeRatio;
        return Mathf.Max(1f, Width * ratio);
    }

    private void PanWorld(float amount)
    {
        Vector3 worldDelta = battleArea.TransformVector(new Vector3(amount, 0f, 0f));
        foreach (var hero in HeroController.ActiveHeroes)
            if (hero != null && hero.IsDeployed) hero.heroRect.position += worldDelta;
        // 0.0.50: fallen Hero visuals are hidden, and must not be panned.
        foreach (var monster in MonsterController.ActiveMonsters)
            if (monster != null && monster.IsAlive) monster.Rect.position += worldDelta;
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.PanEffects(amount);
            CombatManager.Instance.PanGateGround(amount);
        }
    }

    public void PanEnvironment(float amount)
    {
        if (battleArea == null || groundDetails == null) return;
        if (CombatManager.Instance != null) CombatManager.Instance.PanLoot(amount);
        EnsureGroundPresentation();
        if (groundPresentation != null) { groundPresentation.Scroll(amount); return; }
        foreach (var detail in groundDetails)
        {
            if (detail == null) continue;
            Vector2 p = Position(detail);
            float width = detail.rect.width * Mathf.Abs(detail.localScale.x);
            // Wrap only once the ENTIRE decoration is outside the viewport.
            // Keep the overshoot so a low frame rate cannot bunch decorations together.
            float min = battleArea.rect.xMin - (1f - detail.pivot.x) * width - 20f;
            float max = battleArea.rect.xMax + detail.pivot.x * width + 20f;
            p.x = BattleMotion.Wrap(p.x + amount, min, max);
            SetPosition(detail, p);
        }
    }
}
