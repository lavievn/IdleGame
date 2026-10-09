using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

public enum MonsterState { PassiveScroll, Approaching, Attacking, Dead }

public class MonsterController : MonoBehaviour
{
    public Image hpFillImage;
    public TextMeshProUGUI dmgTextPrototype;
    public AttackMode attackMode;
    public float attackRange;
    [Min(0f)] public float moveSpeed = 150f;
    public MonsterState currentState = MonsterState.PassiveScroll;
    public static List<MonsterController> ActiveMonsters = new List<MonsterController>();
    public HeroController CurrentTarget { get; private set; }
    public RectTransform Rect => rect;
    public bool IsAlive => isActiveAndEnabled && currentState != MonsterState.Dead;

    private RectTransform rect;
    private Coroutine attackFeedbackCoroutine;
    private MagicChargeBar chargeBar;
    private Coroutine fadeDmgCoroutine;
    private Vector3 damageTextHome;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (dmgTextPrototype != null) { damageTextHome = dmgTextPrototype.transform.localPosition; dmgTextPrototype.alpha = 0f; }
    }

    void OnEnable()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        ResetFeedback();
        // Exactly one random roll per activation (including pooled respawns).
        InitRandomAttackMode();
        currentState = MonsterState.PassiveScroll;
        CurrentTarget = null;
        if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
    }

    void OnDisable()
    {
        ActiveMonsters.Remove(this);
        CurrentTarget = null;
        ResetFeedback();
    }

    public void TickMovement(EnvironmentManager space, float dt) { TickMovement(space, dt, 0f); }
    public void TickMovement(EnvironmentManager space, float dt, float groundMinusCamera)
    {
        if (!IsAlive) return;
        attackRange = space.AttackRange(attackMode, false);
        if (CurrentTarget == null || !CanAttack(CurrentTarget, space)) {
            CurrentTarget = null;
            FindTarget(space);
        }
        if (CurrentTarget == null) { currentState = MonsterState.PassiveScroll; return; }

        Vector2 p = space.Position(rect);
        float targetX = space.Position(CurrentTarget.heroRect).x;
        p.x = BattleMotion.MonsterApproach(p.x, targetX, attackRange, moveSpeed, dt, groundMinusCamera);
        space.SetPosition(rect, p);
        currentState = CanAttack(CurrentTarget, space) ? MonsterState.Attacking : MonsterState.Approaching;
    }

    public bool CanAttack(HeroController target, EnvironmentManager space)
    {
        if (!IsAlive || target == null || !target.IsDeployed || space == null || space.BattleArea == null) return false;
        Vector2 delta = space.Position(target.heroRect) - space.Position(rect);
        // Monster lanes are visual depth, not obstacles. A melee monster can reach
        // a ranged hero without forcing that hero to walk down its lane.
        return Mathf.Abs(delta.x) <= space.AttackRange(attackMode, false) + 0.1f;
    }

    private void FindTarget(EnvironmentManager space)
    {
        float nearest = float.MaxValue;
        foreach (var hero in HeroController.ActiveHeroes)
        {
            if (hero == null || !hero.IsDeployed) continue;
            float heroX = space.Position(hero.heroRect).x;
            float monsterX = space.Position(rect).x;
            if (heroX < monsterX && !CanAttack(hero, space)) continue;
            float distance = Mathf.Abs(monsterX - heroX);
            if (distance < nearest) { nearest = distance; CurrentTarget = hero; }
        }
    }

    public void InitRandomAttackMode()
    {
        attackMode = (AttackMode)Random.Range(0, 3);
        var space = EnvironmentManager.Instance;
        if (space != null && space.Width > 0f) attackRange = space.AttackRange(attackMode, false);
    }

    public void MarkDead()
    {
        currentState = MonsterState.Dead;
        UpdateChargeBar(false, 0f);
        CurrentTarget = null;
        ActiveMonsters.Remove(this);
    }

    private void ResetFeedback()
    {
        if (chargeBar != null) chargeBar.Set(false, 0f);
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        attackFeedbackCoroutine = null;
        fadeDmgCoroutine = null;
        transform.localScale = Vector3.one;
        if (dmgTextPrototype != null)
        {
            dmgTextPrototype.alpha = 0f;
            dmgTextPrototype.transform.localPosition = damageTextHome;
        }
    }

    private bool hasBodyColor;
    private Color originalBodyColor;
    public void SetIdentityVisual(EntityDataSO data)
    {
        if (Rect == null || data == null) return;
        Image image = Rect.GetComponent<Image>();
        if (image == null) return;
        if (!hasBodyColor) { originalBodyColor = image.color; hasBodyColor = true; }
        image.color = IdentityDisplay.Tint(originalBodyColor, data.spiritRoots);
    }
    public void UpdateChargeBar(bool visible, float progress)
    {
        visible = visible && IsAlive && attackMode == AttackMode.RangedMagic;
        if (chargeBar == null && visible && Rect != null)
            chargeBar = MagicChargeBar.Create(Rect, hpFillImage);
        if (chargeBar != null) chargeBar.Set(visible, progress);
    }
    public void PlayAttackFeedback()
    {
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        attackFeedbackCoroutine = StartCoroutine(AttackFeedbackRoutine());
    }
    private IEnumerator AttackFeedbackRoutine()
    {
        float duration = 0.1f; Vector3 originalScale = Vector3.one;
        transform.localScale = originalScale * 1.2f;
        yield return new WaitForSeconds(duration);
        transform.localScale = originalScale; attackFeedbackCoroutine = null;
    }
    public void UpdateHealthBar(int currentHP, int maxHP) { if (hpFillImage != null) hpFillImage.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f; }
    public void ShowDamage(int damageAmount) { ShowDamage(damageAmount, -1f); }
    public void ShowDamage(int damageAmount, float awaySign)
    {
        if (dmgTextPrototype == null) return;
        UIManager.ReadableWorldText(dmgTextPrototype);
        dmgTextPrototype.text = "-" + damageAmount;
        dmgTextPrototype.alpha = 1f;
        dmgTextPrototype.transform.localPosition = damageTextHome;
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        fadeDmgCoroutine = StartCoroutine(FadeDamageTextRoutine(awaySign));
    }
    private IEnumerator FadeDamageTextRoutine(float awaySign)
    {
        float elapsed = 0f;
        while (elapsed < DamagePopupMotion.Duration)
        {
            elapsed = Mathf.Min(DamagePopupMotion.Duration,elapsed+Time.deltaTime);
            if (dmgTextPrototype == null) yield break;
            dmgTextPrototype.transform.localPosition = damageTextHome + new Vector3(
                DamagePopupMotion.X(elapsed,awaySign),DamagePopupMotion.Y(elapsed),0f);
            dmgTextPrototype.alpha = DamagePopupMotion.Alpha(elapsed);
            yield return null;
        }
        if (dmgTextPrototype != null) dmgTextPrototype.transform.localPosition = damageTextHome;
        fadeDmgCoroutine = null;
    }
}
