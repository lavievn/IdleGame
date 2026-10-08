using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Core References")]
    public static UIManager Instance;

    [Header("UI MENUS")]
    public GameObject systemMenu;
    public GameObject mapMenu;
    public Image groundImage;
    public TransparentWindow transparentWindow;

    [Header("POPUP THOÁT GAME")]
    public GameObject exitConfirmPopup; // Bảng hỏi "Bạn có muốn thoát..."

    private List<CustomInteractable> interactables = new List<CustomInteractable>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ConfigureSystemMenu();
        if (mapMenu == null) return;
        var menuRect = mapMenu.GetComponent<RectTransform>();
        if (menuRect != null) menuRect.sizeDelta = new Vector2(210, 120);
        foreach (var item in mapMenu.GetComponentsInChildren<CustomInteractable>(true))
        {
            if (item.gameObject.name != "Map1" && item.gameObject.name != "Map2") continue;
            int mode = item.gameObject.name == "Map2" ? 1 : 0;
            var label = item.GetComponent<TMPro.TextMeshProUGUI>();
            if (label != null) { label.text = mode == 1 ? "Khó" : "Bình thường"; label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 28; }
            item.onClickEvent = new UnityEvent();
            item.onClickEvent.AddListener(() => SetDifficulty(mode));
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = new Vector2(190,50);
            rect.anchoredPosition = new Vector2(0,mode == 0 ? 27 : -27);
        }
    }
    public void SetDifficulty(int mode)
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null) gm.SetDifficulty(mode);
        if (mapMenu != null) mapMenu.SetActive(false);
    }
    public void ApplyDifficultyVisual(bool hard)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hard ? "#C2F250" : "#C2B280", out color))
        {
            if (groundImage != null) groundImage.color = color;
            if (EnvironmentManager.Instance != null) EnvironmentManager.Instance.SetTerrainTint(color);
        }
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 point;
            if (transparentWindow != null && transparentWindow.TryGetPointerPosition(out point)) HandleMouseClick(point);
            else HandleMouseClick(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        }
    }

    public void RegisterInteractable(CustomInteractable item) { if (!interactables.Contains(item)) interactables.Add(item); }
    public void UnregisterInteractable(CustomInteractable item) { interactables.Remove(item); }

    public bool CheckInteractableHover(Vector2 mousePos)
    {
        foreach (var item in interactables)
        {
            if (item != null && item.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
                return true;
        }
        return false;
    }

    public void HandleMouseClick(Vector2 mousePos)
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        CustomInteractable hit = null;
        foreach (var item in interactables)
        {
            if (item == null || !item.gameObject.activeInHierarchy || item.GetRect() == null) continue;
            if (gm != null && gm.HasPendingConfirmation && !item.transform.IsChildOf(gm.confirmationPopup.transform)) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null)) continue;
            // A menu container must not steal a click from its own scale buttons.
            if (hit == null || item.transform.IsChildOf(hit.transform)) hit = item;
        }
        if (hit != null) hit.onClickEvent?.Invoke();
    }

    private void ConfigureSystemMenu()
    {
        if (transparentWindow == null) transparentWindow = Object.FindFirstObjectByType<TransparentWindow>();
        if (systemMenu == null) return;
        var root = systemMenu.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(1f,0f);
        root.pivot = new Vector2(1f,0f);
        root.anchoredPosition = new Vector2(-16f,265f);
        root.sizeDelta = new Vector2(640f,150f);
        int other = 0;
        foreach (Transform child in root)
        {
            var rect = child as RectTransform; if (rect == null) continue;
            int column;
            int row;
            switch (child.gameObject.name)
            {
                case "Btn_Scale200": column=0;row=0;break;
                case "Btn_Scale500": column=1;row=0;break;
                case "Btn_Scale1000": column=2;row=0;break;
                default: column=other++ % 3;row=1;break;
            }
            rect.anchorMin = rect.anchorMax = new Vector2(0f,1f);
            rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = new Vector2(200f,54f);
            rect.anchoredPosition = new Vector2(110f+210f*column,-40f-65f*row);
            var label = child.GetComponent<TMPro.TextMeshProUGUI>();
            if (label != null) { label.enableAutoSizing=true;label.fontSizeMin=16;label.fontSizeMax=30; }
            // The existing Save/Load wrappers contain labels with old off-screen offsets.
            if (child.gameObject.name == "SaveUi" || child.gameObject.name == "loadUi")
            {
                foreach (Transform nested in child)
                {
                    var text = nested.GetComponent<TMPro.TextMeshProUGUI>();
                    var textRect = nested as RectTransform;
                    if (text == null || textRect == null) continue;
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.pivot = new Vector2(.5f,.5f);
                    textRect.anchoredPosition = Vector2.zero;
                    textRect.sizeDelta = Vector2.zero;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 16; text.fontSizeMax = 30;
                }
            }
        }
        // Scene lists can omit this trigger. Discover and register it explicitly.
        foreach (var item in Object.FindObjectsByType<CustomInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.gameObject.name != "MenuArea") continue;
            var rect = item.GetRect();
            rect.anchorMin = rect.anchorMax = new Vector2(1f,0f);
            rect.pivot = new Vector2(1f,0f);
            rect.anchoredPosition = new Vector2(-16f,110f);
            rect.sizeDelta = new Vector2(180f,96f);
            item.onClickEvent = new UnityEvent(); item.onClickEvent.AddListener(ToggleSystemMenu);
            RegisterInteractable(item);
            if (transparentWindow != null) transparentWindow.RegisterClickable(rect);
        }
        if (transparentWindow != null)
        {
            transparentWindow.RegisterClickable(root);
            foreach (var item in systemMenu.GetComponentsInChildren<CustomInteractable>(true))
                transparentWindow.RegisterClickable(item.GetRect());
        }
    }

    public void ToggleSystemMenu()
    {
        if (systemMenu == null) return;
        systemMenu.SetActive(!systemMenu.activeSelf);
        if (systemMenu.activeSelf) systemMenu.transform.SetAsLastSibling();
        if (mapMenu != null) mapMenu.SetActive(false);
    }

    public void ToggleMapMenu()
    {
        mapMenu.SetActive(!mapMenu.activeSelf);
        systemMenu.SetActive(false);
    }

    // --- LOGIC THOÁT GAME ---
    public void ClickExitButton()
    {
        if (exitConfirmPopup != null) exitConfirmPopup.SetActive(true);
    }

    public void ConfirmExit()
    {
        Application.Quit(); // Lệnh này chạy sẽ ngầm gọi Auto-save trong GameManager
    }

    public void CancelExit()
    {
        if (exitConfirmPopup != null) exitConfirmPopup.SetActive(false);
    }
    // -------------------------

    public void SetWindowScale(int size)
    {
        if (transparentWindow != null) transparentWindow.ResizeWindow(size, size);
        systemMenu.SetActive(false);
    }

    public void ChangeGroundColor(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color newColor))
        {
            if (groundImage != null) groundImage.color = newColor;
            if (EnvironmentManager.Instance != null) EnvironmentManager.Instance.SetTerrainTint(newColor);
        }
        mapMenu.SetActive(false);
    }

    private void ExecuteScale(int width)
    {
        if (transparentWindow == null) transparentWindow = FindObjectOfType<TransparentWindow>();
        if (transparentWindow != null)
        {
            int height = Mathf.RoundToInt(width * (9f / 16f));
            transparentWindow.ResizeWindow(width, height);
        }
        if (systemMenu != null) systemMenu.SetActive(false);
    }

    public void Scale200() { ExecuteScale(200); }
    public void Scale500() { ExecuteScale(500); }
    public void Scale1000() { ExecuteScale(1000); }
}
