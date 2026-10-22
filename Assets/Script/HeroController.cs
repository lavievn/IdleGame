using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

public enum HeroState { Idle, Returning, Approaching, Combat, Dead }

public class HeroController : MonoBehaviour
{
    [Header("REFERENCES")]
    public RectTransform heroRect;
    public string spawnSkin = "FadeIn_01";
    [Header("SPAWN PRESENTATION")]
    [Min(.1f)] public float spawnVfxScale = 1.4f;
    [Header("COMBAT STATS")]
    public AttackMode attackMode = AttackMode.Melee;
    public float attackRange = 30f;
    [Min(0f)] public float moveSpeed = 150f;
    [Min(0f)] public float laneSpeed = 150f;
    [Header("W2 MOVEMENT TEST DEFAULTS (not saved)")]
    public bool useStanceMoveSpeeds;
    [Min(0f)] public float meleeMoveSpeed=450f;
    [Min(0f)] public float physicalMoveSpeed=450f;
    [Min(0f)] public float magicMoveSpeed=200f;
    [Header("UI ELEMENTS")]
    public Image hpFillImage;
    public TextMeshProUGUI dmgTextPrototype;
    public TextMeshProUGUI atkStatusText;

    public static List<HeroController> ActiveHeroes = new List<HeroController>();
    // Includes fallen but still visible actors for Y-depth sorting; separate from combat participation.
    public static List<HeroController> VisibleHeroes = new List<HeroController>();
    public HeroState CurrentState = HeroState.Idle;
    public bool IsDead => CurrentState == HeroState.Dead;
    public bool IsDeployed => deployed && !IsDead && isActiveAndEnabled &&
        heroRect != null && heroRect.gameObject.activeInHierarchy;
    // Fallen Heroes never remain camera subjects. Corpses are hidden instantly.
    public bool IsCameraSubject => IsDeployed;
    public MonsterController CurrentTarget { get; private set; }

    public float MovementDistanceThisFrame { get; private set; }
    // Identity of the CURRENT deployed life, not the reusable GameObject.
    public int SpawnVersion { get; private set; }

    private bool deployed;
    private Coroutine fadeDmgCoroutine;
    private Coroutine attackFeedbackCoroutine;
    private MagicChargeBar chargeBar;
    private RectTransform damageTextRect;

    void Awake()
    {
        if (heroRect != null)
        {
            // Use the feet centre as the logical origin. Scaling feedback no longer
            // moves the visual centre sideways when the old pivot was bottom-right.
            Vector3 feet = heroRect.TransformPoint(new Vector3(heroRect.rect.center.x, heroRect.rect.yMin, 0f));
            heroRect.pivot = new Vector2(0.5f, 0f);
            heroRect.position = feet;
            heroRect.gameObject.SetActive(false);
        }
        AlignDamageTextToBody();
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f;
        if (atkStatusText != null) atkStatusText.text = "";
    }

    private void AlignDamageTextToBody()
    {
        if (heroRect == null || dmgTextPrototype == null) return;
        // Hero_DMG_Text used to be an independent Ground child with a fixed
        // design-time position, so it was far away from the hurt Hero.
        var rt = dmgTextPrototype.transform as RectTransform;
        if (rt == null) return;
        rt.SetParent(heroRect, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f);
        rt.pivot = new Vector2(.5f,.5f);
        // Both the TEXT GLYPHS and its RectTransform must be centred.
        // The scene's Hero_DMG_Text originally used left-aligned TMP text:
        // a wide centred box still drew the numbers to the side of the Hero.
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(130f,40f);
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        dmgTextPrototype.alignment = TextAlignmentOptions.Center;
        dmgTextPrototype.raycastTarget = false;
        damageTextRect = rt;
    }
    void OnDisable()
    {
        MovementDistanceThisFrame = 0f;
        deployed = false;
        ActiveHeroes.Remove(this);
        VisibleHeroes.Remove(this);
        CurrentTarget = null;
        ResetFeedback();
    }

    public void ChangeAttackMode(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex > 2) return;
        HeroSpawnVfx.Cancel(heroRect);
        attackMode = (AttackMode)modeIndex;
        UpdateChargeBar(false, 0f);
        CultivationSpriteBridge.AttachHero(this);
        if (!IsDead) CurrentState = HeroState.Idle;
    }

    public float EffectiveMoveSpeed()
    {
        var manager = CombatManager.Instance;
        var state = manager != null ? manager.StateFor(this) : null;
        // DEVB remains an explicit override. Gear/passive/stance modifiers are
        // applied AFTER choosing the base; neither data nor isDirty is changed.
        float baseSpeed=useStanceMoveSpeeds && CombatBalance.HeroDev==null
            ? attackMode==AttackMode.Melee?meleeMoveSpeed:
                attackMode==AttackMode.RangedPhysical?physicalMoveSpeed:magicMoveSpeed
            : moveSpeed;
        return state == null || state.Data == null ? Mathf.Max(0f,baseSpeed) :
            Mathf.Max(0f,CombatStats.Get(state.Data,CombatStatId.MovementSpeed,
                baseSpeed,ElementType.Vo,new [] { CombatStats.StanceTag(attackMode) }));
    }

    public void TickMovement(EnvironmentManager space, float dt)
    {
        MovementDistanceThisFrame = 0f;
        if (!IsDeployed) return;
        attackRange = space.AttackRange(attackMode, true);
        // Keep an in-range target during a strike; otherwise rescan the closest eligible opponent.
        if (CurrentTarget == null || !CanAttack(CurrentTarget, space)) CurrentTarget = FindClosestMonster(space);
        Vector2 p = space.Position(heroRect);
        float startX = p.x;
        float formationX;
        if(HeroFormation.TryStep(this,space,dt,out formationX))
        {
            CurrentState=HeroState.Returning;
            p.x=formationX;
            MovementDistanceThisFrame=Mathf.Abs(p.x-startX);
            space.SetPosition(heroRect,p);
            HandleAnimation(MovementDistanceThisFrame>.001f,false);
            return;
        }
        if (CurrentTarget == null)
        {
            CurrentState = HeroState.Returning;
            // No current enemy: explore straight left at the current Y.
            // Y only changes while a melee Hero approaches a selected target.
            p.x -= EffectiveMoveSpeed() * dt;
            MovementDistanceThisFrame = Mathf.Abs(p.x - startX);
            space.SetPosition(heroRect, p);
            HandleAnimation(true, false);
            return;
        }

        Vector2 target = space.Position(CurrentTarget.Rect);
        p.x = BattleMotion.ForwardApproach(p.x, target.x, attackRange, EffectiveMoveSpeed(), dt, -1);
        if (attackMode == AttackMode.Melee && Mathf.Abs(target.x - p.x) > attackRange + .1f)
            p.y = Mathf.MoveTowards(p.y, target.y, laneSpeed * dt);
        MovementDistanceThisFrame = Mathf.Abs(p.x - startX);
        space.SetPosition(heroRect, p);
        RefreshCombatState(space);
    }

    public void RefreshCombatState(EnvironmentManager space)
    {
        if (!IsDeployed || CurrentTarget == null || !CurrentTarget.IsAlive) return;
        CurrentState = CanAttack(CurrentTarget, space) ? HeroState.Combat : HeroState.Approaching;
        HandleAnimation(CurrentState == HeroState.Approaching, CurrentState == HeroState.Combat);
    }

    public bool CanAttack(MonsterController target, EnvironmentManager space)
    {
        if (!IsDeployed || target == null || !target.IsAlive || space == null || space.BattleArea == null) return false;
        Vector2 delta = space.Position(target.Rect) - space.Position(heroRect);
        float range = space.AttackRange(attackMode, true);
        return Mathf.Abs(delta.x) <= range + 0.1f;
    }

    private MonsterController FindClosestMonster(EnvironmentManager space)
    {
        float distance = float.MaxValue;
        MonsterController closest = null;
        float x = space.Position(heroRect).x;
        foreach (var monster in MonsterController.ActiveMonsters)
        {
            if (monster == null || !monster.IsAlive) continue;
            float targetX = space.Position(monster.Rect).x;
            if (targetX > x && !CanAttack(monster, space)) continue;
            float d = Mathf.Abs(x - targetX);
            if (d < distance) { distance = d; closest = monster; }
        }
        return closest;
    }

    // Existing single-Hero callers still follow the main actor.
    public void SpawnHero() { SpawnHero(true); }

    // Test companions must not steal camera focus from the active subject.
    public void SpawnHero(bool followCamera) { SpawnHero(followCamera,null); }

    public void SpawnHero(bool followCamera,Vector2? initialPosition)
    {
        if (heroRect == null) return;
        SpawnVersion++;
        gameObject.SetActive(true);
        ResetFeedback();
        CurrentTarget = null;
        MovementDistanceThisFrame = 0f;
        CurrentState = HeroState.Returning;
        deployed = true;
        heroRect.gameObject.SetActive(true);
        CultivationSpriteBridge.AttachHero(this);
        if (!ActiveHeroes.Contains(this)) ActiveHeroes.Add(this);
        if (!VisibleHeroes.Contains(this)) VisibleHeroes.Add(this);
        Canvas.ForceUpdateCanvases();
        var space = EnvironmentManager.Instance;
        if (space != null && space.BattleArea != null)
        {
            space.SetPosition(heroRect, initialPosition ?? new Vector2(space.HomeX, 0f));
            if (followCamera) space.FollowHero(this);
        }
    }

    public void HideHero()
    {
        MovementDistanceThisFrame = 0f;
        deployed = false;
        CurrentTarget = null;
        CurrentState = HeroState.Idle;
        ActiveHeroes.Remove(this);
        VisibleHeroes.Remove(this);
        ResetFeedback();
        if (heroRect != null) heroRect.gameObject.SetActive(false);
    }

    public void Die()
    {
        MovementDistanceThisFrame = 0f;
        deployed = false;
        CurrentTarget = null;
        CurrentState = HeroState.Dead;
        ActiveHeroes.Remove(this);
        VisibleHeroes.Remove(this);
        ResetFeedback();
        // Game rule since 0.0.50: death has no persistent body on the map.
        if (heroRect != null) heroRect.gameObject.SetActive(false);
    }

    private void ResetFeedback()
    {
        HeroSpawnVfx.Cancel(heroRect);
        CultivationSpriteBridge.ResetPose(heroRect);
        if (chargeBar != null) chargeBar.Set(false, 0f);
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        fadeDmgCoroutine = null;
        attackFeedbackCoroutine = null;
        if (heroRect != null) heroRect.localScale = Vector3.one;
        if (dmgTextPrototype != null)
        {
            dmgTextPrototype.alpha = 0f;
            if (damageTextRect != null) damageTextRect.anchoredPosition = Vector2.zero;
        }
    }

    public void SetGenderVisual(GenderType gender)
    {
        if (heroRect == null) return;
        Image img = heroRect.GetComponent<Image>();
        if (img != null) img.color = gender == GenderType.Nam ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.4f, 0.4f);
    }
    private bool hasBodyColor;
    private Color originalBodyColor;
    public void SetIdentityVisual(EntityDataSO data)
    {
        if (heroRect == null || data == null) return;
        Image image = heroRect.GetComponent<Image>();
        if (image == null) return;
        if (!hasBodyColor) { originalBodyColor = image.color; hasBodyColor = true; }
        image.color = IdentityDisplay.Tint(originalBodyColor, data.spiritRoots);
    }
    public void UpdateChargeBar(bool visible, float progress)
    {
        visible = visible && IsDeployed && attackMode == AttackMode.RangedMagic;
        if (chargeBar == null && visible && heroRect != null)
            chargeBar = MagicChargeBar.Create(heroRect, hpFillImage);
        if (chargeBar != null) chargeBar.Set(visible, progress);
    }
    public void PlayAttackFeedback()
    {
        if (heroRect == null || IsDead) return;
        CultivationSpriteBridge.Attack(heroRect);
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        attackFeedbackCoroutine = StartCoroutine(AttackFeedbackRoutine());
    }
    private IEnumerator AttackFeedbackRoutine()
    {
        float duration = 0.1f;
        Vector3 originalScale = Vector3.one;
        heroRect.localScale = originalScale * 1.2f;
        yield return new WaitForSeconds(duration);
        if (heroRect != null) heroRect.localScale = originalScale;
        attackFeedbackCoroutine = null;
    }
    public void UpdateHealthBar(int currentHP, int maxHP) { if (hpFillImage != null) hpFillImage.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f; }
    public void UpdateAtkUI(int currentAtk) { if (atkStatusText != null) atkStatusText.text = $"ATK: {currentAtk}"; }
    public string FullStatDetails { get; private set; } = "Chưa có nhân vật.";
    public void UpdateStats(EntityDataSO data, int currentHP, int maxHP)
    {
        if (atkStatusText == null || data == null) return;
        data.NormalizeRoots();
        var lines = new List<string> { "Cấp: " + data.currentLevel + " · " + (attackMode == AttackMode.Melee ? "Cận chiến" : attackMode == AttackMode.RangedPhysical ? "Cung" : "Phép"),
            "HP: " + currentHP + "/" + maxHP,
            "EXP: " + data.currentExp + "/" + data.expToNextLevel,
            "ATK: " + data.baseDamage + " + " + data.addedDamage +
            " (đặc tính ±) = " + data.GetCalculatedDamage(attackMode) };
        for (int i = 0; i < data.spiritRoots.Count; i++)
            lines.Add("ATK " + IdentityDisplay.Element(data.spiritRoots[i]) + ": " +
                (data.GetCalculatedDamage(attackMode) * data.rootWeights[i]).ToString("0.##") + " (" +
                IdentityDisplay.Tier(data.rootTiers[i]) + ", " + data.rootWeights[i].ToString("P0") + ")");
        float effectiveSpeed = CombatStats.Get(data,CombatStatId.AttackSpeed,
            data.baseAttackSpeed,ElementType.Vo,new [] { CombatStats.StanceTag(attackMode) });
        float interval = CombatBalance.AttackInterval(attackMode,effectiveSpeed,CombatBalance.HeroDev != null);
        lines.Add("Di chuyển: " + EffectiveMoveSpeed().ToString("0.##") + " đơn vị/giây");
        lines.Add("Giáp: " + CombatStats.Get(data,CombatStatId.Armor).ToString("0.##") +
            " · Kháng phép: " + CombatStats.Get(data,CombatStatId.MagicResistance).ToString("0.##"));
        lines.Add("Hồi HP/giây: " + CombatStats.Get(data,CombatStatId.HealthRegeneration).ToString("0.##"));
        lines.Add("Tốc đánh: " + (1f / interval).ToString("0.##") + " đòn/giây");
        lines.Add("Nhịp đánh: " + interval.ToString("0.##") + " giây/đòn");
        lines.Add("Tầm đánh: " + attackRange.ToString("0.##"));
        FullStatDetails = string.Join("\n",lines.ToArray());
        // Debug HUD intentionally keeps ALL calculated stats until a dedicated button exists.
        atkStatusText.text = FullStatDetails;
    }
    public void ShowDamage(int damageAmount) { ShowDamage(damageAmount, 1f); }
    public void ShowDamage(int damageAmount, float awaySign)
    {
        if (!IsDead) CultivationSpriteBridge.Hit(heroRect);
        if (dmgTextPrototype == null) return;
        if (damageTextRect == null) AlignDamageTextToBody();
        if (damageTextRect == null) return;
        UIManager.ReadableWorldText(dmgTextPrototype);
        dmgTextPrototype.text = "-" + damageAmount;
        dmgTextPrototype.alpha = 1f;
        // AnchoredPosition remains relative to the CURRENT body midpoint,
        // even if the actor rect or window is resized during combat.
        damageTextRect.anchoredPosition = Vector2.zero;
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        fadeDmgCoroutine = StartCoroutine(FadeDamageTextRoutine(awaySign));
    }
    private IEnumerator FadeDamageTextRoutine(float awaySign)
    {
        float elapsed = 0f;
        while (elapsed < DamagePopupMotion.Duration)
        {
            elapsed = Mathf.Min(DamagePopupMotion.Duration, elapsed + Time.deltaTime);
            if (dmgTextPrototype == null || damageTextRect == null) yield break;
            // Animate relative to the body's own middle, never in cached
            // local/world coordinates or toward the Hero's facing direction.
            damageTextRect.anchoredPosition =
                new Vector2(0f, DamagePopupMotion.Y(elapsed));
            dmgTextPrototype.alpha = DamagePopupMotion.Alpha(elapsed);
            yield return null;
        }
        if (damageTextRect != null) damageTextRect.anchoredPosition = Vector2.zero;
        fadeDmgCoroutine = null;
    }
    public void HandleAnimation(bool isMoving, bool isFighting)
    {
        if (heroRect == null || !heroRect.gameObject.activeSelf || attackFeedbackCoroutine != null) return;
        if (isMoving && !isFighting) { float bounce = Mathf.Sin(Time.time * 8f) * 0.04f; heroRect.localScale = new Vector3(1 + bounce, 1 + bounce, 1); }
        else if (!isFighting) heroRect.localScale = Vector3.one;
    }
}
