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

    [Header("UI ELEMENTS")]
    public Image hpFillImage; 
    public TextMeshProUGUI dmgTextPrototype; 
    public TextMeshProUGUI atkStatusText; 

    public static List<HeroController> ActiveHeroes = new List<HeroController>();
    public HeroState CurrentState = HeroState.Returning;
    public bool IsDead => CurrentState == HeroState.Dead;

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private float groundWidth;
    private Coroutine fadeDmgCoroutine;
    private Coroutine attackFeedbackCoroutine; 
    private bool hasLockedScroll = false;

    void Awake()
    {
        if (heroRect != null) heroRect.gameObject.SetActive(false);
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f; 
        if (atkStatusText != null) atkStatusText.text = ""; 
    }

    void OnEnable()
    {
        if (!ActiveHeroes.Contains(this)) ActiveHeroes.Add(this);
        CurrentState = HeroState.Returning;
        hasLockedScroll = false;
        
        ApplyAttackRange();
    }

    void OnDisable()
    {
        if (ActiveHeroes.Contains(this)) ActiveHeroes.Remove(this);
        ReleaseScrollLock();
    }

    void Start()
    {
        if (heroRect == null) return;
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = heroRect.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = heroRect.gameObject.AddComponent<CanvasGroup>();
        if (heroRect.parent != null) groundWidth = ((RectTransform)heroRect.parent).rect.width;
    }

    private float GetCanvasScale() => parentCanvas != null ? parentCanvas.scaleFactor : 1f;

    public void ChangeAttackMode(int modeIndex)
    {
        attackMode = (AttackMode)modeIndex;
        ApplyAttackRange();
        
        if (CurrentState == HeroState.Combat)
        {
            CurrentState = HeroState.Idle; 
            ReleaseScrollLock();
        }
    }

    private void ApplyAttackRange()
    {
        if (attackMode == AttackMode.Melee) attackRange = 30f;
        else if (attackMode == AttackMode.RangedPhysical) attackRange = 150f;
        else if (attackMode == AttackMode.RangedMagic) attackRange = 250f;
    }

    void Update()
    {
        if (IsDead) return;

        MonsterController target = FindClosestMonster();

        if (target == null)
        {
            ReleaseScrollLock();
            CurrentState = HeroState.Returning;
            if (!IsAtCenter()) {
                MoveToCenter(100f);
                HandleAnimation(true, false);
            } else {
                CurrentState = HeroState.Idle;
                HandleAnimation(true, false);
            }
        }
        else
        {
            float distWorld = Mathf.Abs(heroRect.position.x - target.transform.position.x);
            float rangeWorld = attackRange * GetCanvasScale();

            if (distWorld <= rangeWorld)
            {
                CurrentState = HeroState.Combat;
                if (!hasLockedScroll) { EnvironmentManager.Instance?.LockScroll(); hasLockedScroll = true; }
                HandleAnimation(false, true);
                
                float step = 150f * Time.deltaTime;
                float newY = Mathf.MoveTowards(heroRect.anchoredPosition.y, target.transform.localPosition.y, step);
                heroRect.anchoredPosition = new Vector2(heroRect.anchoredPosition.x, newY);
            }
            else
            {
                CurrentState = HeroState.Approaching;
                ReleaseScrollLock(); 
                HandleAnimation(true, false);

                float step = 100f * Time.deltaTime;
                float worldDir = Mathf.Sign(target.transform.position.x - heroRect.position.x);
                heroRect.anchoredPosition += new Vector2(worldDir * step, 0);

                float newY = Mathf.MoveTowards(heroRect.anchoredPosition.y, target.transform.localPosition.y, step);
                heroRect.anchoredPosition = new Vector2(heroRect.anchoredPosition.x, newY);
            }
        }
    }

    private MonsterController FindClosestMonster()
    {
        float closestDist = float.MaxValue;
        MonsterController closest = null;
        float currentScale = GetCanvasScale();

        foreach (var mon in MonsterController.ActiveMonsters)
        {
            if (mon.currentState == MonsterState.Dead) continue;
            float dist = Mathf.Abs(heroRect.position.x - mon.transform.position.x) / currentScale;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = mon;
            }
        }
        return closest;
    }

    private void ReleaseScrollLock()
    {
        if (hasLockedScroll)
        {
            EnvironmentManager.Instance?.UnlockScroll();
            hasLockedScroll = false;
        }
    }

    public void Die() { CurrentState = HeroState.Dead; gameObject.SetActive(false); }

    public void SetGenderVisual(GenderType gender)
    {
        if (heroRect == null) return;
        Image img = heroRect.GetComponent<Image>();
        if (img != null) img.color = gender == GenderType.Nam ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.4f, 0.4f);
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
        Vector3 startPos = dmgTextPrototype.transform.localPosition;
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
        if (heroRect == null || !heroRect.gameObject.activeSelf) return; 
        if (isMoving && !isFighting) { float bounce = Mathf.Sin(Time.time * 8f) * 0.04f; heroRect.localScale = new Vector3(1 + bounce, 1 + bounce, 1); }
        else if (!isFighting) heroRect.localScale = Vector3.one;
    }
    public void SpawnHero() { if (heroRect != null) heroRect.gameObject.SetActive(true); }
    public void MoveToCenter(float speed) { if (heroRect == null) return; float targetX = -groundWidth / 2f; heroRect.anchoredPosition = Vector2.MoveTowards(heroRect.anchoredPosition, new Vector2(targetX, 0f), speed * Time.deltaTime); }
    public void MoveTowardsEnemy(float targetY, float speed) { if (heroRect == null) return; float step = speed * Time.deltaTime; float newY = Mathf.MoveTowards(heroRect.anchoredPosition.y, targetY, step); heroRect.anchoredPosition = new Vector2(heroRect.anchoredPosition.x - step, newY); }
    public void Pan(float amount) { if (heroRect != null) heroRect.anchoredPosition += new Vector2(amount, 0); }
    public bool IsAtCenter() { if (heroRect == null) return true; return Mathf.Abs(heroRect.anchoredPosition.x - (-groundWidth / 2f)) < 1f; }
    public float GetActualXPosition() => heroRect != null ? (groundWidth + heroRect.anchoredPosition.x) : 0f;
    public float GetYPosition() => heroRect != null ? heroRect.anchoredPosition.y : 0f;
}