using UnityEngine;
using TuTienCore;

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

    private GroundPresentation groundPresentation;

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

    public void FollowHero(HeroController hero) { cameraTarget = hero; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (battleArea == null && groundDetails != null)
        {
            foreach (var detail in groundDetails)
                if (detail != null) { battleArea = detail.parent as RectTransform; break; }
        }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // All AI moves before CombatManager.Update. Camera presentation runs after both.
    void Update()
    {
        if (Time.timeScale == 0f || battleArea == null || Width <= 0f) return;
        float dt = Time.deltaTime;
        foreach (var hero in HeroController.ActiveHeroes)
            if (hero != null && hero.IsDeployed) hero.TickMovement(this, dt);
        float cameraPan, backgroundPan;
        PresentationPan(out cameraPan, out backgroundPan);
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
        PresentationPan(out pan, out backgroundPan);
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
        if (cameraTarget == null) return;
        cameraPan = BattleMotion.ZoneCameraStep(Position(cameraTarget.heroRect).x,
            ZoneLeftX, ZoneRightX, scrollSpeed, Time.deltaTime);
        backgroundPan = cameraTarget.IsDeployed && cameraTarget.MovementDistanceThisFrame > .001f
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
