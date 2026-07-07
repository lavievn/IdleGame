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

    public MonsterState currentState = MonsterState.PassiveScroll;
    public static List<MonsterController> ActiveMonsters = new List<MonsterController>();

    private HeroController currentTarget;
    private RectTransform rect;
    private Canvas parentCanvas;
    private bool hasLockedScroll = false; 
    private Coroutine attackFeedbackCoroutine;
    private Coroutine fadeDmgCoroutine;

    void Awake() 
    { 
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f; 
        rect = GetComponent<RectTransform>();
    }

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
        
        // Bắt buộc gọi để fix lỗi Pooling làm mất thông số đánh xa
        InitRandomAttackMode(); 
        
        currentState = MonsterState.PassiveScroll;
        currentTarget = null;
        hasLockedScroll = false;
    }

    void OnDisable()
    {
        if (ActiveMonsters.Contains(this)) ActiveMonsters.Remove(this);
        ReleaseScrollLock();
    }

    private float GetCanvasScale() => parentCanvas != null ? parentCanvas.scaleFactor : 1f;

    void Update()
    {
        if (currentState == MonsterState.Dead) return;

        if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsScrolling)
        {
            rect.anchoredPosition += new Vector2(EnvironmentManager.Instance.scrollSpeed * Time.deltaTime, 0);
        }

        if (currentState == MonsterState.PassiveScroll)
        {
            // Vừa sinh ra là lập tức quét mục tiêu, bỏ điều kiện vướng ở mốc 0f
            FindTarget();
        }
        else 
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                ReleaseScrollLock();
                currentState = MonsterState.PassiveScroll;
                FindTarget(); 
                return;
            }

            float distWorld = Mathf.Abs(rect.position.x - currentTarget.heroRect.position.x);
            float rangeWorld = attackRange * GetCanvasScale();
            
            if (distWorld <= rangeWorld)
            {
                currentState = MonsterState.Attacking;
                // Khi một con quái xả skill, chỉ duy nhất con quái đó khóa màn hình, không ảnh hưởng vận tốc di chuyển của các con khác
                if (!hasLockedScroll) { EnvironmentManager.Instance?.LockScroll(); hasLockedScroll = true; }
            }
            else
            {
                currentState = MonsterState.Approaching;
                ReleaseScrollLock(); 
                
                // Mệnh lệnh sinh tử: Chưa đủ tầm đánh của mình thì PHẢI LẾT BỘ TIẾP
                float step = 150f * Time.deltaTime; 
                float worldDir = Mathf.Sign(currentTarget.heroRect.position.x - rect.position.x);
                rect.anchoredPosition += new Vector2(worldDir * step, 0);
            }
        }
    }

    private void FindTarget()
    {
        float closestDist = float.MaxValue;
        HeroController closestHero = null;
        float currentScale = GetCanvasScale();

        foreach (var hero in HeroController.ActiveHeroes)
        {
            if (hero.IsDead) continue;
            float dist = Mathf.Abs(rect.position.x - hero.heroRect.position.x) / currentScale;
            if (dist <= attackRange + 400f) 
            {
                if (dist < closestDist) { closestDist = dist; closestHero = hero; }
            }
        }

        if (closestHero != null)
        {
            currentTarget = closestHero;
            currentState = MonsterState.Approaching;
        }
    }

    private void ReleaseScrollLock()
    {
        if (hasLockedScroll)
        {
            EnvironmentManager.Instance?.UnlockScroll();
            hasLockedScroll = false;
        }
    }

    public void InitRandomAttackMode()
    {
        attackMode = (AttackMode)Random.Range(0, 3);
        if (attackMode == AttackMode.Melee) attackRange = 30f;
        else if (attackMode == AttackMode.RangedPhysical) attackRange = 100f;
        else if (attackMode == AttackMode.RangedMagic) attackRange = 250f;
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
    public void ShowDamage(int damageAmount)
    {
        if (dmgTextPrototype == null) return;
        dmgTextPrototype.text = $"-{damageAmount}";
        if (fadeDmgCoroutine != null) StopCoroutine(fadeDmgCoroutine);
        fadeDmgCoroutine = StartCoroutine(FadeDamageTextRoutine());
    }
    private IEnumerator FadeDamageTextRoutine()
    {
        dmgTextPrototype.alpha = 1f;
        yield return new WaitForSeconds(0.4f);
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f;
        fadeDmgCoroutine = null;
    }
}