using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

public enum MonsterState { PassiveScroll, Approaching, Attacking, Dead, Suspicious }
public enum MonsterAwareness { Unaware, Suspicious, Engaged }

public class MonsterController : MonoBehaviour
{
    public Image hpFillImage;
    public TextMeshProUGUI dmgTextPrototype;
    public AttackMode attackMode;
    public float attackRange;
    [Min(0f)] public float moveSpeed = 100f;
    public MonsterState currentState = MonsterState.PassiveScroll;
    public static List<MonsterController> ActiveMonsters = new List<MonsterController>();
    public HeroController CurrentTarget { get; private set; }
    public RectTransform Rect => rect;
    public bool IsHeavenlyGate;public float HeavenlyGateRange;
    public bool IsAlive => isActiveAndEnabled && currentState != MonsterState.Dead;
    // The same pooled GameObject can represent multiple monster lives.
    public int SpawnVersion { get; private set; }
    public float SightRange => attackMode == AttackMode.Melee ? 550f : 800f;
    public const float AlertDuration = .5f;
    public MonsterAwareness Awareness { get; private set; }
    public bool HasDetectedHero => Awareness != MonsterAwareness.Unaware;
    public bool IsAggressive => Awareness == MonsterAwareness.Engaged;
    public bool FacingRight { get; private set; }
    // Looking around is independent of combat/damage/spawn RNG.
    private static readonly System.Random awarenessRandom = new System.Random();
    private float nextLook, alertElapsed;
    private TextMeshProUGUI awarenessLabel;
    private Canvas awarenessCanvas;
    private CultivationSpriteBridge awarenessBody;

    private RectTransform rect;
    public string SpriteIdentityKey { get; private set; }
    private static System.Random appearanceRandom=new System.Random();
    private bool spriteIdentityChosen;
    private AttackMode spriteIdentityMode;
    public void RefreshSpriteIdentity()
    {
        if(spriteIdentityChosen && spriteIdentityMode==attackMode)return;
        spriteIdentityChosen=true;spriteIdentityMode=attackMode;
        SpriteIdentityKey=attackMode==AttackMode.Melee && appearanceRandom.Next(2)==0?"Wolf":null;
    }
    private Coroutine attackFeedbackCoroutine;
    private MagicChargeBar chargeBar;
    private Coroutine fadeDmgCoroutine;
    private RectTransform damageTextRect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        AlignDamageTextToBody();
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f;
    }

    private void AlignDamageTextToBody()
    {
        if (dmgTextPrototype == null || rect == null) return;
        // Prefab placed its label under Monster_Hp_Fill; that follows bar anchors,
        // not the monster's body. Anchor it directly to the damage recipient.
        var rt = dmgTextPrototype.transform as RectTransform;
        if (rt == null) return;
        rt.SetParent(rect, false);
        // Anchor the actual glyphs, not just their UI box, at the monster's
        // visual middle. Previously the label started above and to the left.
        rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f);
        rt.pivot = new Vector2(.5f,.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(130f,40f);
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        dmgTextPrototype.alignment = TextAlignmentOptions.Center;
        dmgTextPrototype.raycastTarget = false;
        damageTextRect = rt;
    }
    void OnEnable()
    {
        SpriteIdentityKey=null;spriteIdentityChosen=false;
        SpawnVersion++;
        if (rect == null) rect = GetComponent<RectTransform>();
        ResetFeedback();
        if(IsHeavenlyGate){currentState=MonsterState.PassiveScroll;CurrentTarget=null;Awareness=MonsterAwareness.Engaged;HideAwarenessLabel();if(!ActiveMonsters.Contains(this))ActiveMonsters.Add(this);return;}
        // Exactly one random roll per activation (including pooled respawns).
        InitRandomAttackMode();
        awarenessBody = rect.GetComponent<CultivationSpriteBridge>();
        awarenessCanvas = null;
        for (Transform p = rect; p != null; p = p.parent)
        {
            awarenessCanvas = p.GetComponent<Canvas>();
            if (awarenessCanvas != null) break;
        }
        currentState = MonsterState.PassiveScroll;
        CurrentTarget = null;
        Awareness = MonsterAwareness.Unaware;
        alertElapsed = 0f;
        nextLook = NextLookInterval();
        SetFacing(awarenessRandom.Next(2) == 0);
        HideAwarenessLabel();
        if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
    }

    void OnDisable()
    {
        ActiveMonsters.Remove(this);
        CurrentTarget = null;
        Awareness = MonsterAwareness.Unaware;
        HideAwarenessLabel();
        ResetFeedback();
    }

    public void TickMovement(EnvironmentManager space, float dt) { TickMovement(space, dt, 0f); }
    public void TickMovement(EnvironmentManager space, float dt, float groundMinusCamera)
    {
        if (!IsAlive) return;
        if(IsHeavenlyGate)return; // Fixed seal; no ordinary AI or scroll.
        attackRange = space.AttackRange(attackMode, false);
        if (!IsAggressive)
        {
            // Still on the floor: LateUpdate adds camera pan, so this
            // compensation produces exactly the same displacement as Ground.
            Vector2 idle = space.Position(rect);
            idle.x += groundMinusCamera;
            space.SetPosition(rect, idle);
            currentState = HasDetectedHero ? MonsterState.Suspicious : MonsterState.PassiveScroll;
            UpdateChargeBar(false, 0f);
            return;
        }
        if (CurrentTarget == null || !CanAttack(CurrentTarget, space)) {
            CurrentTarget = null;
            FindTarget(space);
        }
        if (CurrentTarget == null)
        {
            // Stay coupled to Ground even before acquiring a target.
            Vector2 idle = space.Position(rect);
            idle.x += groundMinusCamera;
            space.SetPosition(rect, idle);
            currentState = MonsterState.PassiveScroll;
            return;
        }

        Vector2 p = space.Position(rect);
        SetFacing(space.Position(CurrentTarget.heroRect).x >= p.x);
        // Ranged enemies spawned off the LEFT edge must enter the actual
        // viewport before they can plant their feet and start an attack.
        // The ordinary range test alone would stop them outside the screen.
        if (attackMode != AttackMode.Melee && p.x < space.BattleArea.rect.xMin)
        {
            float entryX = space.BattleArea.rect.xMin + 2f;
            float step = Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, dt);
            p.x = Mathf.Min(entryX, p.x + groundMinusCamera + step);
            space.SetPosition(rect, p);
            currentState = MonsterState.Approaching;
            UpdateChargeBar(false, 0f);
            return;
        }
        float targetX = space.Position(CurrentTarget.heroRect).x;
        p.x = BattleMotion.MonsterApproach(p.x, targetX, attackRange, moveSpeed, dt, groundMinusCamera);
        space.SetPosition(rect, p);
        currentState = CanAttack(CurrentTarget, space) ? MonsterState.Attacking : MonsterState.Approaching;
    }

    public bool CanAttack(HeroController target, EnvironmentManager space)
    {
        if (!IsAlive || !IsAggressive || target == null || !target.IsDeployed || space == null || space.BattleArea == null) return false;
        // Prevent ranged attacks from offscreen enemies, even when their
        // long attack ranges already overlap the Hero.
        if (attackMode != AttackMode.Melee && !space.IsVisible(rect)) return false;
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
            float distance = Mathf.Abs(monsterX - heroX);
            if (distance < nearest) { nearest = distance; CurrentTarget = hero; }
        }
    }

    private static float NextLookInterval() { return 1.5f + (float)awarenessRandom.NextDouble() * 3.5f; }
    private void SetFacing(bool right)
    {
        if (FacingRight == right) return;
        FacingRight = right;
        CultivationSpriteBridge.SetMonsterFacing(rect, right);
    }
    // CombatManager advances ALL members before broadcasting a new alert. No
    // member gets an extra tick of the 0.5s delay due to iteration order.
    internal HeroController TickAwareness(EnvironmentManager space, float dt)
    {
        if (!IsAlive || space == null || Time.timeScale == 0f) return null;
        dt = Mathf.Max(0f, dt);
        if (Awareness == MonsterAwareness.Suspicious)
        {
            alertElapsed += dt;
            if (alertElapsed >= AlertDuration) Awareness = MonsterAwareness.Engaged;
            RefreshAwarenessLabel();
            return null;
        }
        if (Awareness != MonsterAwareness.Unaware) return null;
        nextLook -= dt;
        while (nextLook <= 0f)
        {
            SetFacing(!FacingRight);
            nextLook += NextLookInterval();
        }
        HeroController seen = null;
        float nearest = SightRange;
        foreach (var hero in HeroController.ActiveHeroes)
        {
            if (hero == null || !hero.IsDeployed || hero.heroRect == null) continue;
            Vector2 delta = space.Position(hero.heroRect) - space.Position(rect);
            if ((FacingRight && delta.x < 0f) || (!FacingRight && delta.x > 0f)) continue;
            float distance = delta.magnitude;
            if (distance <= nearest) { nearest = distance; seen = hero; }
        }
        return seen;
    }
    internal void ReceiveAlert(HeroController hero, bool immediate)
    {
        if (!IsAlive || hero == null || !hero.IsDeployed) return;
        if (!HasDetectedHero)
        {
            Awareness = MonsterAwareness.Suspicious;
            alertElapsed = 0f;
        }
        if (immediate) Awareness = MonsterAwareness.Engaged;
        if (CurrentTarget == null || !CurrentTarget.IsDeployed) CurrentTarget = hero;
        var space = EnvironmentManager.Instance;
        if (space != null) SetFacing(space.Position(CurrentTarget.heroRect).x >= space.Position(rect).x);
        RefreshAwarenessLabel();
    }
    private void HideAwarenessLabel()
    {
        if (awarenessLabel != null) awarenessLabel.gameObject.SetActive(false);
    }
    private void RefreshAwarenessLabel()
    {
        if (!IsAlive || !HasDetectedHero || rect == null) { HideAwarenessLabel(); return; }
        if (awarenessLabel == null)
        {
            var go = new GameObject("Monster Awareness", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(rect, false);
            awarenessLabel = go.GetComponent<TextMeshProUGUI>();
            if (dmgTextPrototype != null) awarenessLabel.font = dmgTextPrototype.font;
            awarenessLabel.raycastTarget = false;
            awarenessLabel.alignment = TextAlignmentOptions.Center;
            awarenessLabel.fontStyle = FontStyles.Bold;
            awarenessLabel.enableWordWrapping = false;
            awarenessLabel.overflowMode = TextOverflowModes.Overflow;
        }
        awarenessLabel.gameObject.SetActive(true);
        awarenessLabel.text = IsAggressive ? "!" : "?";
        awarenessLabel.color = IsAggressive ? new Color(1f, .12f, .08f, 1f) : new Color(1f, .82f, .08f, 1f);
        float scale = awarenessCanvas != null ? Mathf.Max(.01f, awarenessCanvas.scaleFactor) : 1f;
        var labelRect = (RectTransform)awarenessLabel.transform;
        labelRect.anchorMin = labelRect.anchorMax = rect.pivot;
        labelRect.pivot = new Vector2(.5f, 0f);
        float height = awarenessBody != null ? awarenessBody.VisibleBodySize.y : rect.rect.height;
        labelRect.anchoredPosition = new Vector2(0f, height + 6f / scale);
        labelRect.sizeDelta = new Vector2(40f / scale, 32f / scale);
        awarenessLabel.fontSize = 24f / scale;
    }
    void LateUpdate() { if (!IsHeavenlyGate && HasDetectedHero) RefreshAwarenessLabel(); }

    public void InitRandomAttackMode()
    {
        attackMode = (AttackMode)Random.Range(0, 3);
        var space = EnvironmentManager.Instance;
        if (space != null && space.Width > 0f) attackRange = space.AttackRange(attackMode, false);
        CultivationSpriteBridge.AttachMonster(this);
    }

    public void MarkDead()
    {
        currentState = MonsterState.Dead;
        HideAwarenessLabel();
        CultivationSpriteBridge.ResetPose(rect);
        UpdateChargeBar(false, 0f);
        CurrentTarget = null;
        ActiveMonsters.Remove(this);
    }

    private void ResetFeedback()
    {
        CultivationSpriteBridge.ResetPose(rect);
        if (chargeBar != null) chargeBar.Set(false, 0f);
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        attackFeedbackCoroutine = null;
        fadeDmgCoroutine = null;
        transform.localScale = Vector3.one;
        if (dmgTextPrototype != null)
        {
            dmgTextPrototype.alpha = 0f;
            if (damageTextRect != null) damageTextRect.anchoredPosition = Vector2.zero;
        }
    }

    private bool hasBodyColor;
    private Color originalBodyColor;
    public void SetIdentityVisual(EntityDataSO data)
    {
        if (Rect == null || data == null) return;
        CultivationSpriteBridge.AttachMonster(this);
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
        if (!IsAlive) return;
        CultivationSpriteBridge.Attack(rect);
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
        if (IsAlive) CultivationSpriteBridge.Hit(rect);
        if (dmgTextPrototype == null) return;
        if (damageTextRect == null) AlignDamageTextToBody();
        if (damageTextRect == null) return;
        UIManager.ReadableWorldText(dmgTextPrototype);
        dmgTextPrototype.text = "-" + damageAmount;
        dmgTextPrototype.alpha = 1f;
        damageTextRect.anchoredPosition = Vector2.zero;
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        fadeDmgCoroutine = StartCoroutine(FadeDamageTextRoutine(awaySign));
    }
    private IEnumerator FadeDamageTextRoutine(float awaySign)
    {
        float elapsed = 0f;
        while (elapsed < DamagePopupMotion.Duration)
        {
            elapsed = Mathf.Min(DamagePopupMotion.Duration,elapsed+Time.deltaTime);
            if (dmgTextPrototype == null || damageTextRect == null) yield break;
            // Always rise vertically from the CURRENT monster body centre.
            // This works with all spawn pivots and pooled monsters.
            damageTextRect.anchoredPosition =
                new Vector2(0f, DamagePopupMotion.Y(elapsed));
            dmgTextPrototype.alpha = DamagePopupMotion.Alpha(elapsed);
            yield return null;
        }
        if (damageTextRect != null) damageTextRect.anchoredPosition = Vector2.zero;
        fadeDmgCoroutine = null;
    }
}
