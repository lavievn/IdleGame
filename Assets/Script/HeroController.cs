using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using TuTienCore; 

public class HeroController : MonoBehaviour
{
    [Header("REFERENCES")]
    public RectTransform heroRect;
    public string spawnSkin = "FadeIn_01";

    [Header("UI ELEMENTS")]
    public Image hpFillImage; 
    public TextMeshProUGUI dmgTextPrototype; 
    public TextMeshProUGUI atkStatusText; 

    private CanvasGroup canvasGroup;
    private float groundWidth;
    private Coroutine fadeDmgCoroutine;
    private Coroutine attackFeedbackCoroutine; 

    void Awake()
    {
        if (heroRect != null) heroRect.gameObject.SetActive(false);
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f; 
        if (atkStatusText != null) atkStatusText.text = ""; 
    }

    void Start()
    {
        if (heroRect == null) return;
        canvasGroup = heroRect.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = heroRect.gameObject.AddComponent<CanvasGroup>();
        if (heroRect.parent != null) groundWidth = ((RectTransform)heroRect.parent).rect.width;
    }

    public void SetGenderVisual(GenderType gender)
    {
        if (heroRect == null) return;
        Image img = heroRect.GetComponent<Image>();
        if (img != null)
        {
            img.color = gender == GenderType.Nam ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.4f, 0.4f);
        }
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
        Vector3 targetScale = originalScale * 1.2f;
        heroRect.localScale = targetScale;
        yield return new WaitForSeconds(duration);
        if (heroRect != null) heroRect.localScale = originalScale;
        attackFeedbackCoroutine = null;
    }

    public void UpdateHealthBar(int currentHP, int maxHP)
    {
        if (hpFillImage != null) 
            hpFillImage.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
    }

    public void UpdateAtkUI(int currentAtk)
    {
        if (atkStatusText != null) atkStatusText.text = $"ATK: {currentAtk}";
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
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startPos = dmgTextPrototype.transform.localPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            dmgTextPrototype.alpha = 1f - progress;
            dmgTextPrototype.transform.localPosition = startPos + new Vector3(0, progress * 30f, 0);
            yield return null;
        }
        dmgTextPrototype.transform.localPosition = startPos; 
        fadeDmgCoroutine = null;
    }

    public void HandleAnimation(bool isMoving, bool isFighting)
    {
        if (heroRect == null || !heroRect.gameObject.activeSelf) return; 
        if (isMoving && !isFighting)
        {
            float bounce = Mathf.Sin(Time.time * 8f) * 0.04f;
            heroRect.localScale = new Vector3(1 + bounce, 1 + bounce, 1);
        }
        else if (!isFighting)
        {
            heroRect.localScale = Vector3.one;
        }
    }

    public void SpawnHero() { if (heroRect != null) heroRect.gameObject.SetActive(true); }

    // [CẬP NHẬT 2.5D]: Về giữa trục X và reset trục Y về 0 (đứng giữa đường)
    public void MoveToCenter(float speed) 
    { 
        if (heroRect == null) return;
        float targetX = -groundWidth / 2f; 
        heroRect.anchoredPosition = Vector2.MoveTowards(heroRect.anchoredPosition, new Vector2(targetX, 0f), speed * Time.deltaTime); 
    }

    // [CẬP NHẬT 2.5D]: Tiến lại gần trục X của quái, đồng thời leo lên/xuống theo trục Y của quái
    public void MoveTowardsEnemy(float targetY, float speed) 
    { 
        if (heroRect == null) return;
        float step = speed * Time.deltaTime;
        float newY = Mathf.MoveTowards(heroRect.anchoredPosition.y, targetY, step);
        heroRect.anchoredPosition = new Vector2(heroRect.anchoredPosition.x - step, newY); 
    }

    public void Pan(float amount) { if (heroRect != null) heroRect.anchoredPosition += new Vector2(amount, 0); }
    
    public bool IsAtCenter() 
    { 
        if (heroRect == null) return true;
        float targetX = -groundWidth / 2f; 
        return Mathf.Abs(heroRect.anchoredPosition.x - targetX) < 1f; 
    }
    public float GetActualXPosition() => heroRect != null ? (groundWidth + heroRect.anchoredPosition.x) : 0f;
    public float GetYPosition() => heroRect != null ? heroRect.anchoredPosition.y : 0f;
}