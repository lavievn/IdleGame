using UnityEngine;
using TuTienCore;

[DefaultExecutionOrder(-200)]
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("MÔI TRƯỜNG")]
    [SerializeField] private RectTransform[] groundDetails;
    [SerializeField] private RectTransform battleArea;
    [Min(0f)] public float scrollSpeed = 150f;

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
    private readonly DeadzoneCamera deadzoneCamera = new DeadzoneCamera();
    private float plannedCameraPan, plannedBackgroundPan;
    private bool hasPlannedPan;
    public float CameraFollowVelocity => deadzoneCamera.Velocity;

    private GroundPresentation groundPresentation;
    private Canvas battleCanvas;
    private float logicalWidth;
    private int presentationWidth;

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
        if (battleCanvas == null || Screen.width <= 0 || presentationWidth == Screen.width) return;
        presentationWidth = Screen.width;
        battleCanvas.scaleFactor = Screen.width / logicalWidth;
        // Unity owns the Canvas rect. Do not resize/reposition Ground or actors here.
    }

    public bool HasLoopingTerrain => groundPresentation != null;
    public void SetTerrainTint(Color color) { EnsureGroundPresentation(); if (groundPresentation != null) groundPresentation.SetTerrainTint(color); }
    private void EnsureGroundPresentation()
    {
        if (groundPresentation == null) groundPresentation = GroundPresentation.Install(this, groundDetails);
    }
    void Start() { EnsureGroundPresentation(); }

    // A dead, still-visible hero remains the camera subject until hidden/replaced.
    // It is intentionally NOT put back in the list of living combat actors.
    private HeroController cameraTarget;

    [Header("TẦM ĐÁNH - tỷ lệ chiều rộng Ground")]
    [Range(0.01f, 0.15f)] public float meleeRangeRatio = 0.035f;
    [Range(0.1f, 0.6f)] public float heroPhysicalRangeRatio = 0.36f;
    [Range(0.1f, 0.6f)] public float heroMagicRangeRatio = 0.42f;
    [Range(0.1f, 0.6f)] public float monsterPhysicalRangeRatio = 0.28f;
    [Range(0.1f, 0.6f)] public float monsterMagicRangeRatio = 0.36f;

    public RectTransform BattleArea => battleArea;
    public float Width => battleArea != null ? battleArea.rect.width : 0f;
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
        cameraTarget = hero;
        deadzoneCamera.Reset(); // Start or Retry must not inherit old momentum.
        hasPlannedPan = false;
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

    // All AI moves before CombatManager.Update. Camera presentation runs after both.
    void Update()
    {
        hasPlannedPan = false;
        SyncBattleProjection(); // Also runs when paused or before deployment.
        if (Time.timeScale == 0f || battleArea == null || Width <= 0f) return;
        float dt = Time.deltaTime;
        foreach (var hero in HeroController.ActiveHeroes)
            if (hero != null && hero.IsDeployed) hero.TickMovement(this, dt);
        float cameraPan, backgroundPan;
        PresentationPan(out cameraPan, out backgroundPan);
        plannedCameraPan = cameraPan;
        plannedBackgroundPan = backgroundPan;
        hasPlannedPan = true;
        float groundMinusCamera = backgroundPan - cameraPan;
        foreach (var monster in MonsterController.ActiveMonsters)
            if (monster != null && monster.IsAlive) monster.TickMovement(this, dt, groundMinusCamera);
        // Monsters may have entered a hero's range during this frame.
        foreach (var hero in HeroController.ActiveHeroes)
            if (hero != null && hero.IsDeployed) hero.RefreshCombatState(this);
    }

    void LateUpdate()
    {
        IsScrolling = false;
        CurrentCameraSpeed = 0f;
        CurrentBackgroundSpeed = 0f;
        if (Time.timeScale == 0f || battleArea == null || Width <= 0f) return;
        float pan, backgroundPan;
        // Update used this same pan to compute the monsters' Ground-camera
        // compensation. Advancing the soft camera again here would desync them.
        if (hasPlannedPan)
        {
            pan = plannedCameraPan;
            backgroundPan = plannedBackgroundPan;
        }
        else PresentationPan(out pan, out backgroundPan);
        hasPlannedPan = false;
        PanWorld(pan);
        PanEnvironment(backgroundPan);
        CurrentBackgroundSpeed = Time.deltaTime > 0f ? backgroundPan / Time.deltaTime : 0f;
        CurrentCameraSpeed = Time.deltaTime > 0f ? pan / Time.deltaTime : 0f;
        IsScrolling = Mathf.Abs(pan) > 0.01f;
    }

    private void PresentationPan(out float cameraPan, out float backgroundPan)
    {
        cameraPan = backgroundPan = 0f;
        if (Time.timeScale == 0f || battleArea == null || Time.deltaTime <= 0f) return;
        if (cameraTarget == null || !cameraTarget.IsCameraSubject)
        {
            cameraTarget = null;
            foreach (var hero in HeroController.ActiveHeroes)
                if (hero != null && hero.IsDeployed) { cameraTarget = hero; break; }
        }
        if (cameraTarget == null) { deadzoneCamera.Reset(); return; }
        float heroX = Position(cameraTarget.heroRect).x;
        if (useSoftZoneCamera && cameraTarget.IsDeployed && !cameraTarget.IsDead)
        {
            // Hero movement is applied first; camera does NOT auto-scroll.
            // It only corrects excursions outside the central deadzone.
            cameraPan = deadzoneCamera.Pan(heroX,ZoneLeftX,ZoneRightX,
                battleArea.rect.xMin,battleArea.rect.xMax,Time.deltaTime,
                cameraSmoothTime,cameraDeadzoneRatio,cameraPreferredX);
        }
        else
        {
            // Keep the established corpse/legacy camera behavior separate.
            deadzoneCamera.Reset();
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
        // Death removes the hero from ActiveHeroes, but the body still travels
        // with the world until it reaches the right zone edge.
        if (cameraTarget != null && cameraTarget.IsDead && cameraTarget.IsCameraSubject)
            cameraTarget.heroRect.position += worldDelta;
        foreach (var monster in MonsterController.ActiveMonsters)
            if (monster != null && monster.IsAlive) monster.Rect.position += worldDelta;
        if (CombatManager.Instance != null) CombatManager.Instance.PanEffects(amount);
    }

    public void PanEnvironment(float amount)
    {
        if (battleArea == null || groundDetails == null) return;
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
