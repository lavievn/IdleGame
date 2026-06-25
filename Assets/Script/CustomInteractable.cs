using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
public class CustomInteractable : MonoBehaviour
{
    private RectTransform rectTransform;
    public UnityEvent onClickEvent; // Vẫn cho phép kéo thả hàm trong Inspector bình thường

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (UIManager.Instance != null) UIManager.Instance.RegisterInteractable(this);
    }

    void OnDestroy()
    {
        if (UIManager.Instance != null) UIManager.Instance.UnregisterInteractable(this);
    }

    public RectTransform GetRect() => rectTransform;
}