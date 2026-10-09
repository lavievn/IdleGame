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
    [Header("COMBAT STATS")]
    public AttackMode attackMode = AttackMode.Melee;
    public float attackRange = 30f;
    [Min(0f)] public float moveSpeed = 150f;
    [Min(0f)] public float laneSpeed = 150f;
    [Header("UI ELEMENTS")]
    public Image hpFillImage;
    public TextMeshProUGUI dmgTextPrototype;
    public TextMeshProUGUI atkStatusText;

    public static List<HeroController> ActiveHeroes = new List<HeroController>();
    public HeroState CurrentState = HeroState.Idle;
    public bool IsDead => CurrentState == HeroState.Dead;
    public bool IsDeployed => deployed && !IsDead && isActiveAndEnabled &&
        heroRect != null && heroRect.gameObject.activeInHierarchy;
    public bool IsCameraSubject => (deployed || IsDead) && isActiveAndEnabled &&
        heroRect != null && heroRect.gameObject.activeInHierarchy;
    public MonsterController CurrentTarget { get; private set; }

    public float MovementDistanceThisFrame { get; private set; }

    private bool deployed;
    private Coroutine fadeDmgCoroutine;
    private Coroutine attackFeedbackCoroutine;
    private MagicChargeBar chargeBar;
    private Vector3 damageTextHome;

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
        if (dmgTextPrototype != null)
        {
            damageTextHome = dmgTextPrototype.transform.localPosition;
            dmgTextPrototype.alpha = 0f;
        }
        if (atkStatusText != null) atkStatusText.text = "";
    }

    void OnDisable()
    {
        MovementDistanceThisFrame = 0f;
        deployed = false;
        ActiveHeroes.Remove(this);
        CurrentTarget = null;
        ResetFeedback();
    }

    public void ChangeAttackMode(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex > 2) return;
        attackMode = (AttackMode)modeIndex;
        UpdateChargeBar(false, 0f);
        if (!IsDead) CurrentState = HeroState.Idle;
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
        if (CurrentTarget == null)
        {
            CurrentState = HeroState.Returning;
            // Hero speed is independent from the camera's base scroll speed.
            p.x -= Mathf.Max(0f, moveSpeed) * dt;
            p.y = Mathf.MoveTowards(p.y, 0f, laneSpeed * dt);
            MovementDistanceThisFrame = Mathf.Abs(p.x - startX);
            space.SetPosition(heroRect, p);
            HandleAnimation(true, false);
            return;
        }

        Vector2 target = space.Position(CurrentTarget.Rect);
        p.x = BattleMotion.ForwardApproach(p.x, target.x, attackRange, moveSpeed, dt, -1);
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

    public void SpawnHero()
    {
        if (heroRect == null) return;
        gameObject.SetActive(true);
        ResetFeedback();
        CurrentTarget = null;
        MovementDistanceThisFrame = 0f;
        CurrentState = HeroState.Returning;
        deployed = true;
        heroRect.gameObject.SetActive(true);
        if (!ActiveHeroes.Contains(this)) ActiveHeroes.Add(this);
        Canvas.ForceUpdateCanvases();
        var space = EnvironmentManager.Instance;
        if (space != null && space.BattleArea != null)
        {
            space.SetPosition(heroRect, new Vector2(space.HomeX, 0f));
            space.FollowHero(this);
        }
    }

    public void HideHero()
    {
        MovementDistanceThisFrame = 0f;
        deployed = false;
        CurrentTarget = null;
        CurrentState = HeroState.Idle;
        ActiveHeroes.Remove(this);
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
        ResetFeedback();
    }

    private void ResetFeedback()
    {
        if (chargeBar != null) chargeBar.Set(false, 0f);
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        fadeDmgCoroutine = null;
        attackFeedbackCoroutine = null;
        if (heroRect != null) heroRect.localScale = Vector3.one;
        if (dmgTextPrototype != null)
        {
            dmgTextPrototype.alpha = 0f;
            dmgTextPrototype.transform.localPosition = damageTextHome;
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
        if (heroRect == null) return;
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
    public void UpdateStats(EntityDataSO data, int currentHP, int maxHP)
    {
        if (atkStatusText == null || data == null) return;
        data.NormalizeRoots();
        var lines = new List<string> { "HP: " + currentHP + "/" + maxHP,
            "ATK cơ bản: " + data.baseDamage + " + " + data.addedDamage + " = " + data.GetCalculatedDamage() };
        for (int i = 0; i < data.spiritRoots.Count; i++)
            lines.Add("ATK " + IdentityDisplay.Element(data.spiritRoots[i]) + ": " +
                (data.GetCalculatedDamage() * data.rootWeights[i]).ToString("0.##") + " (" +
                IdentityDisplay.Tier(data.rootTiers[i]) + ", " + data.rootWeights[i].ToString("P0") + ")");
        float interval = CombatBalance.AttackInterval(attackMode, data.baseAttackSpeed);
        lines.Add("Di chuyển: " + moveSpeed.ToString("0.##") + " đơn vị/giây");
        lines.Add("Tốc đánh: " + (1f / interval).ToString("0.##") + " đòn/giây · " + interval.ToString("0.##") + " giây/đòn");
        atkStatusText.text = string.Join("\n", lines.ToArray());
    }
    public void ShowDamage(int damageAmount)
    {
        if (dmgTextPrototype == null) return;
        dmgTextPrototype.text = $"-{damageAmount}";
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        fadeDmgCoroutine = StartCoroutine(FadeDamageTextRoutine());
    }
    private IEnumerator FadeDamageTextRoutine()
    {
        float duration = 0.8f; float elapsed = 0f;
        Vector3 startPos = damageTextHome;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            dmgTextPrototype.alpha = 1f - (elapsed / duration);
            dmgTextPrototype.transform.localPosition = startPos + new Vector3(0, (elapsed / duration) * 30f, 0);
            yield return null;
        }
        dmgTextPrototype.transform.localPosition = startPos; fadeDmgCoroutine = null;
    }
    public void HandleAnimation(bool isMoving, bool isFighting)
    {
        if (heroRect == null || !heroRect.gameObject.activeSelf || attackFeedbackCoroutine != null) return;
        if (isMoving && !isFighting) { float bounce = Mathf.Sin(Time.time * 8f) * 0.04f; heroRect.localScale = new Vector3(1 + bounce, 1 + bounce, 1); }
        else if (!isFighting) heroRect.localScale = Vector3.one;
    }
}
