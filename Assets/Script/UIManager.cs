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
        ConfigureGameplayPanels();
        if (mapMenu != null) mapMenu.SetActive(false);
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rect.gameObject.name == "MapIcon") rect.gameObject.SetActive(false);
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
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // TransparentWindow owns native clicks, including while Unity has no focus.
        if (transparentWindow != null) return;
#endif
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
            if (item != null && item.isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
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
            if (item == null || !item.isActiveAndEnabled || item.GetRect() == null) continue;
            if (gm != null && gm.HasPendingConfirmation && !item.transform.IsChildOf(gm.confirmationPopup.transform)) continue;
            if (damagePanel != null && damagePanel.activeSelf && (gm == null || !gm.HasPendingConfirmation) && !item.transform.IsChildOf(damagePanel.transform)) continue;
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
        root.anchoredPosition = new Vector2(-16f,220f);
        root.sizeDelta = new Vector2(340f,310f);
        var template = systemMenu.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        menuFontTemplate = template;
        // Existing scene submenus are retained as hidden templates; only one new
        // page is visible at a time. Old serialized events cannot leak into it.
        foreach (Transform child in root) child.gameObject.SetActive(false);
        foreach (var old in systemMenu.GetComponentsInChildren<SaveMenuUI>(true)) old.enabled = false;
        var rootClick = systemMenu.GetComponent<CustomInteractable>();
        if (rootClick != null) rootClick.onClickEvent = new UnityEvent();
        mainPage = MakePage("SystemMain", "MENU HỆ THỐNG", root);
        loadPage = MakePage("SystemLoad", "TẢI BẢN LƯU", root);
        sizePage = MakePage("SystemSize", "KÍCH THƯỚC CỬA SỔ", root);
        MakeMenuButton(mainPage, "Exit", "Thoát", 0, ConfirmExit);
        MakeMenuButton(mainPage, "Load", "Tải bản lưu", 1, () => ShowPage(loadPage));
        MakeMenuButton(mainPage, "Reset", "Đặt lại nhân vật", 2, () => {
            CloseSystemMenu(); var gm = Object.FindFirstObjectByType<GameManager>(); if (gm != null) gm.OnResetClicked();
        });
        MakeMenuButton(mainPage, "WindowSize", "Kích thước cửa sổ", 3, () => ShowPage(sizePage));
        MakeMenuButton(loadPage, "LoadSave1", "Save 1", 0, () => LoadSlot(SaveSlot.ManualSave1));
        MakeMenuButton(loadPage, "LoadSave2", "Save 2", 1, () => LoadSlot(SaveSlot.ManualSave2));
        MakeMenuButton(loadPage, "LoadAuto", "Auto", 2, () => LoadSlot(SaveSlot.AutoSave));
        MakeMenuButton(loadPage, "LoadBack", "Quay lại", 3, () => ShowPage(mainPage));
        MakeMenuButton(sizePage, "Size800", "800", 0, () => ExecuteScale(800));
        MakeMenuButton(sizePage, "Size500", "500", 1, () => ExecuteScale(500));
        MakeMenuButton(sizePage, "Size250", "250", 2, () => ExecuteScale(250));
        MakeMenuButton(sizePage, "SizeBack", "Quay lại", 3, () => ShowPage(mainPage));
        ShowPage(mainPage);
        // Scene lists can omit this trigger. Discover and register it explicitly.
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rect.gameObject.name != "MenuArea") continue;
            // The actual scene uses a uGUI Button here, not CustomInteractable.
            // Use one dispatch path so EventSystem cannot toggle it a second time.
            var button = rect.GetComponent<Button>();
            if (button != null) button.enabled = false;
            var item = rect.GetComponent<CustomInteractable>();
            if (item == null) item = rect.gameObject.AddComponent<CustomInteractable>();
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

    private GameObject mainPage, loadPage, sizePage;
    private TMPro.TextMeshProUGUI menuFontTemplate;
    private GameObject MakePage(string name, string title, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        MakeLabel(rect, title, new Vector2(170,-24), new Vector2(320,34));
        return go;
    }
    private TMPro.TextMeshProUGUI MakeLabel(Transform parent, string title, Vector2 position, Vector2 size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<TMPro.TextMeshProUGUI>();
        if (menuFontTemplate != null) { text.font = menuFontTemplate.font; text.fontSharedMaterial = menuFontTemplate.fontSharedMaterial; }
        text.text = title; text.color = new Color(1,1,1,1); text.raycastTarget = false;
        text.alignment = TMPro.TextAlignmentOptions.Center; text.enableAutoSizing = true;
        text.fontSizeMin = 16; text.fontSizeMax = 26;
        return text;
    }
    private void MakeMenuButton(GameObject page, string name, string label, int row, UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CustomInteractable));
        var rect = (RectTransform)go.transform; rect.SetParent(page.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = new Vector2(170,-74-60*row); rect.sizeDelta = new Vector2(300,50);
        go.GetComponent<Image>().color = new Color(.12f,.18f,.26f,1); go.GetComponent<Image>().raycastTarget = false;
        MakeLabel(rect, label, new Vector2(150,-25), new Vector2(284,44));
        var click = go.GetComponent<CustomInteractable>(); click.onClickEvent = new UnityEvent(); click.onClickEvent.AddListener(action);
        RegisterInteractable(click);
    }
    public bool IsPaused { get; private set; }
    private TMPro.TextMeshProUGUI pauseText, damageText;
    private GameObject damagePanel;
    private string[] displayedDamage = new string[0];
    private int damagePage;
    public void SetPaused(bool paused)
    {
        IsPaused = paused; Time.timeScale = paused ? 0f : 1f;
        if (pauseText != null) pauseText.text = paused ? "Tiếp tục" : "Tạm dừng";
    }
    public void TogglePause() { SetPaused(!IsPaused); }
    void OnDestroy() { if (Instance == this) { Time.timeScale = 1f; Instance = null; } }

    private CustomInteractable BindClick(RectTransform rect, UnityAction action)
    {
        var button = rect.GetComponent<Button>(); if (button != null) button.enabled = false;
        var item = rect.GetComponent<CustomInteractable>();
        if (item == null) item = rect.gameObject.AddComponent<CustomInteractable>();
        item.onClickEvent = new UnityEvent(); item.onClickEvent.AddListener(action); RegisterInteractable(item);
        if (transparentWindow != null) transparentWindow.RegisterClickable(rect);
        return item;
    }
    private RectTransform SmallButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(.15f,.25f,.35f,1f); go.GetComponent<Image>().raycastTarget = false;
        MakeLabel(rect, label, new Vector2(size.x * .5f,-size.y * .5f), new Vector2(size.x-12,size.y-8));
        BindClick(rect, action); return rect;
    }
    private void ConfigureGameplayPanels()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        var hero = Object.FindFirstObjectByType<HeroController>();
        Transform canvas = systemMenu != null ? systemMenu.transform.parent : gm != null && gm.eventLog != null ? gm.eventLog.transform.parent : null;
        if (canvas == null) return;
        if (menuFontTemplate == null && gm != null) menuFontTemplate = gm.eventLogText;
        if (menuFontTemplate == null && hero != null) menuFontTemplate = hero.atkStatusText;
        RectTransform pause = null;
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rect.gameObject.name == "BuffIcon") { pause = rect; break; }
        if (pause == null) pause = SmallButton(canvas,"PauseButton","Tạm dừng",new Vector2(1,1),new Vector2(1,1),new Vector2(-16,-214),new Vector2(180,64),TogglePause);
        else {
            foreach (Transform child in pause) child.gameObject.SetActive(false);
            pause.anchorMin = pause.anchorMax = new Vector2(1,1); pause.pivot = new Vector2(1,1);
            pause.anchoredPosition = new Vector2(-16,-214); pause.sizeDelta = new Vector2(180,64);
            pauseText = MakeLabel(pause,"Tạm dừng",new Vector2(90,-32),new Vector2(168,56));
            BindClick(pause,TogglePause);
        }
        if (pauseText == null) pauseText = pause.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        SetPaused(false);
        if (gm != null && gm.eventLog != null) {
            // Reserve space at the right of the log so Info cannot obscure a formula/name.
            var log = (RectTransform)gm.eventLog.transform;
            SmallButton(log,"DamageInfoButton","Info",new Vector2(1,1),new Vector2(1,1),new Vector2(-8,-8),new Vector2(100,42),OpenDamageInfo);
            if (gm.eventLogText != null) {
                var textRect = (RectTransform)gm.eventLogText.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = new Vector2(-136,-16); textRect.anchoredPosition = new Vector2(-52,0);
            }
        }
        if (hero != null) {
            if (hero.atkStatusText != null) hero.atkStatusText.gameObject.SetActive(false);
            var panel = new GameObject("HeroStatsPanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            var rect = (RectTransform)panel.transform;rect.SetParent(canvas,false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(16,166);rect.sizeDelta = new Vector2(440,256);
            panel.GetComponent<Image>().color = new Color(.04f,.06f,.1f,.8f);panel.GetComponent<Image>().raycastTarget = false;
            var text = MakeLabel(rect,"",new Vector2(220,-128),new Vector2(416,236));
            text.alignment = TMPro.TextAlignmentOptions.TopLeft;text.fontSizeMin = 16;text.fontSizeMax = 24;
            hero.atkStatusText = text;
        }
        damagePanel = new GameObject("DamageInfoPanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var body = (RectTransform)damagePanel.transform;body.SetParent(canvas,false);
        body.anchorMin = new Vector2(.05f,.15f);body.anchorMax = new Vector2(.95f,.85f);
        body.sizeDelta = Vector2.zero;body.anchoredPosition = Vector2.zero;
        damagePanel.GetComponent<Image>().color = new Color(.025f,.04f,.07f,.98f);
        damagePanel.GetComponent<Image>().raycastTarget = false;
        damageText = MakeLabel(body,"",Vector2.zero,Vector2.zero);
        var detail = (RectTransform)damageText.transform;
        detail.anchorMin = Vector2.zero;detail.anchorMax = Vector2.one;detail.pivot = new Vector2(.5f,.5f);
        detail.sizeDelta = new Vector2(-36,-104);detail.anchoredPosition = new Vector2(0,30);
        damageText.alignment = TMPro.TextAlignmentOptions.TopLeft;damageText.fontSizeMin = 14;damageText.fontSizeMax = 24;
        SmallButton(body,"DamageOlder","Đòn trước",Vector2.zero,Vector2.zero,new Vector2(18,12),new Vector2(160,54),()=>ChangeDamagePage(1));
        SmallButton(body,"DamageNewer","Đòn sau",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,12),new Vector2(160,54),()=>ChangeDamagePage(-1));
        SmallButton(body,"DamageClose","Đóng",new Vector2(1,0),new Vector2(1,0),new Vector2(-18,12),new Vector2(160,54),CloseDamageInfo);
        if (transparentWindow != null) transparentWindow.RegisterClickable(body);
        damagePanel.SetActive(false);
    }
    public void OpenDamageInfo()
    {
        if (damagePanel == null) return;
        var gm = Object.FindFirstObjectByType<GameManager>();displayedDamage = gm != null ? gm.DamageHistory : new string[0];damagePage = 0;
        damagePanel.SetActive(true);damagePanel.transform.SetAsLastSibling();RefreshDamageInfo();
    }
    public void CloseDamageInfo() { if (damagePanel != null) damagePanel.SetActive(false); }
    private void ChangeDamagePage(int step)
    {
        damagePage = Mathf.Clamp(damagePage+step,0,System.Math.Max(0,displayedDamage.Length-1));RefreshDamageInfo();
    }
    private void RefreshDamageInfo()
    {
        if (damageText == null) return;
        damageText.text = displayedDamage.Length == 0 ? "CHI TIẾT SÁT THƯƠNG\nChưa có đòn đánh gây sát thương." :
            "CHI TIẾT SÁT THƯƠNG · " + (damagePage+1) + "/" + displayedDamage.Length + " (mới nhất trước)\n" + displayedDamage[damagePage];
    }

    private void ShowPage(GameObject page)
    {
        if (mainPage != null) mainPage.SetActive(mainPage == page);
        if (loadPage != null) loadPage.SetActive(loadPage == page);
        if (sizePage != null) sizePage.SetActive(sizePage == page);
    }
    public void CloseSystemMenu() { if (systemMenu != null) systemMenu.SetActive(false); ShowPage(mainPage); }
    private void LoadSlot(SaveSlot slot)
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null && gm.LoadAndContinue(slot)) CloseSystemMenu();
    }

    public void ToggleSystemMenu()
    {
        if (systemMenu == null) return;
        systemMenu.SetActive(!systemMenu.activeSelf);
        if (systemMenu.activeSelf) { ShowPage(mainPage); systemMenu.transform.SetAsLastSibling(); }
        if (mapMenu != null) mapMenu.SetActive(false);
    }

    public void ToggleMapMenu() { if (mapMenu != null) mapMenu.SetActive(false); }

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
        ExecuteScale(size);
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
        CloseSystemMenu();
    }

    public void Scale250() { ExecuteScale(250); }
    public void Scale800() { ExecuteScale(800); }
    public void Scale200() { ExecuteScale(250); }
    public void Scale500() { ExecuteScale(500); }
    public void Scale1000() { ExecuteScale(800); }
}
