using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

public class MonsterController : MonoBehaviour
{
    public Image hpFillImage; 
    public TextMeshProUGUI dmgTextPrototype; 

    public AttackMode attackMode;
    public float attackRange;

    private Coroutine attackFeedbackCoroutine;
    private Coroutine fadeDmgCoroutine;

    void Awake() { if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f; }

    public void InitRandomAttackMode()
    {
        attackMode = (AttackMode)Random.Range(0, 3);
        if (attackMode == AttackMode.Melee) attackRange = 120f;
        else if (attackMode == AttackMode.RangedPhysical) attackRange = 300f;
        else if (attackMode == AttackMode.RangedMagic) attackRange = 350f;
    }

    public void PlayAttackFeedback()
    {
        if (attackFeedbackCoroutine != null) StopCoroutine(attackFeedbackCoroutine);
        attackFeedbackCoroutine = StartCoroutine(AttackFeedbackRoutine());
    }

    private IEnumerator AttackFeedbackRoutine()
    {
        float duration = 0.1f;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * 1.2f;
        transform.localScale = targetScale;
        yield return new WaitForSeconds(duration);
        transform.localScale = originalScale;
        attackFeedbackCoroutine = null;
    }

    public void UpdateHealthBar(int currentHP, int maxHP)
    {
        if (hpFillImage != null) hpFillImage.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
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
        dmgTextPrototype.alpha = 1f;
        yield return new WaitForSeconds(0.4f);
        if (dmgTextPrototype != null) dmgTextPrototype.alpha = 0f;
        fadeDmgCoroutine = null;
    }
}