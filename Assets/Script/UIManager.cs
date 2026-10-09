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
        CombatBalance.LoadDevProfiles();
    }

    void Start()
    {
        InstallReadableCanvas();
        ConfigureSystemMenu();
        ConfigureGameplayPanels();
        LayoutReadableUI();
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

    public CustomInteractable HitControl(Vector2 mousePos)
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        CustomInteractable hit = null;
        foreach (var item in interactables)
        {
            if (item == null || !item.isActiveAndEnabled || item.GetRect() == null) continue;
            if (gm != null && gm.HasPendingConfirmation && !item.transform.IsChildOf(gm.confirmationPopup.transform)) continue;
            if (devUI != null && devUI.IsOpen && !item.transform.IsChildOf(devUI.Root)) continue;
            if (damagePanel != null && damagePanel.activeSelf && (gm == null || !gm.HasPendingConfirmation) && !item.transform.IsChildOf(damagePanel.transform)) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null)) continue;
            if (hit == null || item.transform.IsChildOf(hit.transform)) hit = item;
        }
        return hit;
    }
    public void HandleMouseClick(Vector2 mousePos) { var hit = HitControl(mousePos); if (hit != null) hit.onClickEvent?.Invoke(); }
    public void HandlePointerClick(Vector2 press, Vector2 release)
    {
        var hit = HitControl(press);
        if (hit != null && hit == HitControl(release)) hit.onClickEvent?.Invoke();
    }
    public bool IsTextInputAt(Vector2 point)
    {
        return devUI != null && devUI.IsOpen && devUI.InputAt(point);
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
        MakeMenuButton(sizePage, "Size600", "Nhỏ · 600", 0, () => ExecuteScale(600));
        MakeMenuButton(sizePage, "Size800", "Vừa · 800", 1, () => ExecuteScale(800));
        MakeMenuButton(sizePage, "Size1150", "Lớn · 1150", 2, () => ExecuteScale(1150));
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
            var caption = rect.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (caption == null) caption = MakeLabel(rect,"Menu",new Vector2(32,-12),new Vector2(60,22));
            caption.text = "Menu"; caption.alignment = TMPro.TextAlignmentOptions.Center;
            caption.gameObject.SetActive(true); caption.raycastTarget = false;
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
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        MakeLabel(rect, title, new Vector2(170,-24), new Vector2(320,34));
        return go;
    }
    internal TMPro.TextMeshProUGUI MakeLabel(Transform parent, string title, Vector2 position, Vector2 size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<TMPro.TextMeshProUGUI>();
        if (menuFontTemplate != null && menuFontTemplate.font != null) { text.font = menuFontTemplate.font; text.fontSharedMaterial = menuFontTemplate.font.material; }
        text.text = title; text.color = new Color(1,1,1,1); text.raycastTarget = false;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        ReadableText(text, 14);
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
    private HeroController heroForUI;
    private readonly RectTransform[] attackButtons = new RectTransform[3];
    private string[] displayedDamage = new string[0];
    private int damagePage;
    public void SetPaused(bool paused)
    {
        IsPaused = paused; Time.timeScale = paused ? 0f : 1f;
        if (pauseText != null) pauseText.text = paused ? "Tiếp tục" : "Tạm dừng";
    }
    public void TogglePause() { SetPaused(!IsPaused); }
    void OnDestroy() { if (Instance == this) { Time.timeScale = 1f; Instance = null; } }

    internal CustomInteractable BindClick(RectTransform rect, UnityAction action)
    {
        var button = rect.GetComponent<Button>(); if (button != null) button.enabled = false;
        var item = rect.GetComponent<CustomInteractable>();
        if (item == null) item = rect.gameObject.AddComponent<CustomInteractable>();
        item.onClickEvent = new UnityEvent(); item.onClickEvent.AddListener(action); RegisterInteractable(item);
        if (transparentWindow != null) transparentWindow.RegisterClickable(rect);
        return item;
    }
    internal RectTransform SmallButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, UnityAction action)
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
        Transform canvas = hudCanvas;
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
        pauseRect = pause;
        devButton = SmallButton(canvas,"DEVBButton","DEVB",new Vector2(1,1),new Vector2(1,1),new Vector2(-120,-4),new Vector2(56,24),OpenDevBalance);
        statsButton = SmallButton(canvas,"StatsButton","Chỉ số",new Vector2(1,1),new Vector2(1,1),new Vector2(-180,-4),new Vector2(60,24),OpenHeroStats);
        devUI = new DevBalanceUI(this, hudCanvas);
        heroForUI = hero;
        InstallAttackModeButtons();
        SetPaused(false);
        if (gm != null && gm.eventLog != null) {
            // Reserve space at the right of the log so Info cannot obscure a formula/name.
            var log = (RectTransform)gm.eventLog.transform;
            BindClick(log,OpenLogInfo);
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
            panel.GetComponent<Image>().color = new Color(.04f,.06f,.1f,.95f);panel.GetComponent<Image>().raycastTarget = false;
            var text = MakeLabel(rect,"",new Vector2(220,-128),new Vector2(416,236));
            text.alignment = TMPro.TextAlignmentOptions.TopLeft; ReadableText(text,16);
            hero.atkStatusText = text; statsPanel = rect;
            ConfigureCompactStatsText(text);
            rect.gameObject.SetActive(false);
            if (transparentWindow != null) transparentWindow.RegisterClickable(rect);
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
    private static void ConfigureCompactStatsText(TMPro.TextMeshProUGUI label)
    {
        label.enableWordWrapping = false;
        label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        label.alignment = TMPro.TextAlignmentOptions.TopLeft;
    }
    public static bool ShouldShowCombatHUD(bool deployed, bool starting, bool gameOver, bool confirmation, bool modal)
    {
        return deployed && !starting && !gameOver && !confirmation && !modal;
    }
    private void RefreshCombatHUDVisibility()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        bool shown = ShouldShowCombatHUD(gm != null && gm.IsGameplayHUDVisible,
            gm != null && gm.preGameUI != null && gm.preGameUI.activeInHierarchy,
            gm != null && gm.gameOverPanel != null && gm.gameOverPanel.activeInHierarchy,
            gm != null && gm.confirmationPopup != null && gm.confirmationPopup.activeInHierarchy,
            (damagePanel != null && damagePanel.activeSelf) || (devUI != null && devUI.IsOpen) ||
            (systemMenu != null && systemMenu.activeInHierarchy));
        if (statsPanel != null) statsPanel.gameObject.SetActive(shown && Screen.height >= 290);
        for (int i=0;i<attackButtons.Length;i++)
            if (attackButtons[i] != null) attackButtons[i].gameObject.SetActive(shown);
        if (gm != null && gm.eventLog != null) gm.eventLog.gameObject.SetActive(shown);
    }
    private void InstallAttackModeButtons()
    {
        string[] names = { "Kiếm", "Cung", "Phép" };
        string[] captions = { "Cận chiến", "Cung", "Phép" };
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i=0;i<3;i++)
                if (rect.gameObject.name == names[i] && attackButtons[i] == null)
                {
                    // Old scene controls have x~1760; preserve original UnityEvent callbacks.
                    rect.SetParent(hudCanvas,false);
                    rect.gameObject.SetActive(true);
                    var click = rect.GetComponent<CustomInteractable>();
                    if (click != null) RegisterInteractable(click);
                    if (transparentWindow != null) transparentWindow.RegisterClickable(rect);
                    var caption=rect.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                    if (caption != null)
                    {
                        caption.gameObject.SetActive(true); caption.text=captions[i];
                        caption.alignment=TMPro.TextAlignmentOptions.Center;
                        var rt=(RectTransform)caption.transform;
                        rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,.5f);
                        rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(-4,-2);ReadableText(caption,14);
                    }
                    attackButtons[i]=rect;
                }
    }
    private void RefreshAttackModeSelection()
    {
        if (heroForUI==null) return;
        for(int i=0;i<attackButtons.Length;i++)
        {
            if (attackButtons[i]==null) continue;
            var bg=attackButtons[i].GetComponent<Image>();
            if (bg!=null) bg.color=(int)heroForUI.attackMode==i
                ? new Color(.55f,.34f,.08f,.96f) : new Color(.14f,.24f,.34f,.95f);
        }
    }
    public void OpenDamageInfo()
    {
        if (damagePanel == null) return;
        detailKind = 0; detailSlice = 0;
        var gm = Object.FindFirstObjectByType<GameManager>();displayedDamage = gm != null ? gm.DamageHistory : new string[0];damagePage = 0;
        damagePanel.SetActive(true);damagePanel.transform.SetAsLastSibling();RefreshDamageInfo();
    }
    public void CloseDamageInfo() { if (damagePanel != null) damagePanel.SetActive(false); }
    private void ChangeDamagePage(int step)
    {
        if (detailSlice + step >= 0 && detailSlice + step < detailSliceCount) detailSlice += step;
        else if (detailKind == 0) { damagePage = Mathf.Clamp(damagePage+step,0,System.Math.Max(0,displayedDamage.Length-1)); detailSlice = step < 0 ? int.MaxValue : 0; }
        RefreshDamageInfo();
    }
    private void RefreshDamageInfo()
    {
        if (damageText == null) return;
        string title = detailKind == 1 ? "CHỈ SỐ HERO" : detailKind == 2 ? "NHẬT KÝ" : "SÁT THƯƠNG " + (damagePage+1) + "/" + System.Math.Max(1,displayedDamage.Length);
        string content = detailKind != 0 ? detailSnapshot : displayedDamage.Length == 0 ? "Chưa có đòn đánh hoàn tất." : displayedDamage[damagePage];
        var pages = ReadablePages(content, Mathf.Max(12,Screen.width-40), Mathf.Max(20,Screen.height-92));
        detailSliceCount = pages.Count; detailSlice = Mathf.Clamp(detailSlice,0,pages.Count-1);
        damageText.text = title + " · " + (detailSlice+1) + "/" + pages.Count + "\n" + pages[detailSlice];
    }
    public static List<string> ReadablePages(string content, float width, float height)
    {
        int columns = System.Math.Max(12,(int)(width/11f));
        int rows = System.Math.Max(1,(int)(height/25f)-1);
        var lines = new List<string>();
        foreach (string line in (content ?? "").Split('\n')) {
            string rest = line;
            while (rest.Length > columns) {
                int cut = rest.LastIndexOf(' ',columns-1,columns); if (cut <= 0) cut = columns;
                lines.Add(rest.Substring(0,cut)); rest = rest.Substring(cut).TrimStart();
            }
            lines.Add(rest);
        }
        var pages = new List<string>();
        for (int i=0;i<lines.Count;i+=rows) pages.Add(string.Join("\n",lines.GetRange(i,System.Math.Min(rows,lines.Count-i)).ToArray()));
        if (pages.Count == 0) pages.Add(""); return pages;
    }
    private int detailKind, detailSlice, detailSliceCount = 1;
    private string detailSnapshot;
    public void OpenHeroStats()
    {
        var hero = Object.FindFirstObjectByType<HeroController>();
        var gm=Object.FindFirstObjectByType<GameManager>();
        string stats=hero!=null ? hero.FullStatDetails : "Chưa có nhân vật.";
        if (gm!=null)
        {
            if (gm.HeroData!=null) stats=TuTienCore.IdentityDisplay.Describe(gm.HeroData)+" · Cấp "+gm.HeroData.currentLevel+"\n"+stats;
            stats+="\n\nSỰ KIỆN\n"+gm.FullEventHistory;
            var details=gm.DamageHistory;
            stats+="\n\nCHI TIẾT SÁT THƯƠNG\n"+(details.Length==0?"Chưa có đòn đánh.":string.Join("\n\n",details));
        }
        OpenTextDetail(1,stats);
    }
    public void OpenLogInfo()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        OpenTextDetail(2,gm != null ? gm.FullEventHistory : "Chưa có nhật ký.");
    }
    private void OpenTextDetail(int kind,string text)
    {
        if (damagePanel == null) return;
        detailKind = kind; detailSlice = 0; detailSnapshot = text;
        damagePanel.SetActive(true); damagePanel.transform.SetAsLastSibling(); RefreshDamageInfo();
    }
    public void OpenDevBalance()
    {
        CloseDamageInfo(); CloseSystemMenu(); if (devUI != null) devUI.Open();
    }

    private RectTransform hudCanvas, pauseRect, devButton, statsButton, statsPanel;
    private DevBalanceUI devUI;
    private int layoutWidth, layoutHeight;
    public static void ReadableText(TMPro.TextMeshProUGUI text,float size = 14)
    {
        text.fontStyle = TMPro.FontStyles.Normal; text.fontWeight = TMPro.FontWeight.Regular;
        text.enableAutoSizing = false; text.fontSize = size; text.fontSizeMin = size; text.fontSizeMax = size;
        text.enableWordWrapping = true;
    }
    public static void ReadableWorldText(TMPro.TextMeshProUGUI text)
    {
        float scale = Screen.width/1920f;
        for(Transform parent=text.transform.parent;parent!=null;parent=parent.parent) {
            var canvas=parent.GetComponent<Canvas>();if(canvas!=null&&canvas.scaleFactor>0){scale=canvas.scaleFactor;break;}
        }
        ReadableText(text,Mathf.Clamp(12f/Mathf.Max(.01f,scale),12,160));
        text.enableWordWrapping=false;
        text.overflowMode=TMPro.TextOverflowModes.Overflow;
    }
    private void InstallReadableCanvas()
    {
        var go = new GameObject("Readable HUD",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100; canvas.scaleFactor = 1;
        hudCanvas = (RectTransform)go.transform;
        var gm = Object.FindFirstObjectByType<GameManager>();
        var oldRoot = systemMenu != null ? systemMenu.transform.parent : gm != null && gm.eventLog != null ? gm.eventLog.transform.parent : null;
        var move = new List<Transform>();
        if (oldRoot != null) foreach(Transform child in oldRoot) {
            if (child.gameObject.name == "Ground") continue;
            move.Add(child);
        }
        foreach(var child in move) child.SetParent(hudCanvas,false);
        if (gm != null) {
            if (gm.preGameUI != null) gm.preGameUI.transform.SetParent(hudCanvas,false);
            if (gm.confirmationPopup != null) gm.confirmationPopup.transform.SetParent(hudCanvas,false);
            if (gm.gameOverPanel != null) gm.gameOverPanel.transform.SetParent(hudCanvas,false);
            if (gm.eventLog != null) gm.eventLog.transform.SetParent(hudCanvas,false);
        }
    }
    void LateUpdate()
    {
        // UI objects created after our Start (e.g. the start menu) share the same
        // readable sizing. Button events use one dispatcher, including Windows.
        bool controlsChanged = false;
        foreach(var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if (!button.enabled) continue;
            controlsChanged = true;
            var b = button; var item = b.GetComponent<CustomInteractable>();
            if (item == null) { item = b.gameObject.AddComponent<CustomInteractable>(); item.onClickEvent = new UnityEvent(); item.onClickEvent.AddListener(()=>{if(b.interactable)b.onClick.Invoke();}); }
            RegisterInteractable(item); if (transparentWindow != null) transparentWindow.RegisterClickable(item.GetRect()); button.enabled = false;
        }
        if (controlsChanged || Screen.width != layoutWidth || Screen.height != layoutHeight) LayoutReadableUI();
        RefreshCombatHUDVisibility();
        RefreshAttackModeSelection();
    }
    private static void PlaceUI(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }
    private void LayoutReadableUI()
    {
        if (hudCanvas == null) return;
        layoutWidth = Screen.width; layoutHeight = Screen.height;
        // Canvas supplies its pixel rect in Unity; explicitly set the test double too.
        hudCanvas.pivot = new Vector2(.5f,.5f); hudCanvas.sizeDelta = new Vector2(Screen.width,Screen.height);
        hudCanvas.position = new Vector3(Screen.width*.5f,Screen.height*.5f,0);
        var gm = Object.FindFirstObjectByType<GameManager>();
        float w = Screen.width, h = Screen.height;
        PlaceUI(pauseRect,new Vector2(1,1),new Vector2(1,1),new Vector2(-4,-4),new Vector2(76,24));
        PlaceUI(devButton,new Vector2(1,1),new Vector2(1,1),new Vector2(-84,-4),new Vector2(52,24));
        PlaceUI(statsButton,new Vector2(1,1),new Vector2(1,1),new Vector2(-140,-4),new Vector2(60,24));
        for(int i=0;i<3;i++) PlaceUI(attackButtons[i],new Vector2(1,1),new Vector2(1,1),
            new Vector2(-6-(2-i)*78,-112),new Vector2(74,30));
        foreach(var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (rect.gameObject.name == "MenuArea") PlaceUI(rect,new Vector2(1,0),new Vector2(1,0),new Vector2(-4,4),new Vector2(64,24));
        if (systemMenu != null) {
            float mw = Mathf.Min(340,w-12), mh = Mathf.Min(260,h-12);
            PlaceUI((RectTransform)systemMenu.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-6,6),new Vector2(mw,mh));
            foreach(var page in new[]{mainPage,loadPage,sizePage}) if (page != null) {
                float row = (mh-28)/4; int index = 0;
                foreach(Transform child in page.transform) {
                    var rect = child as RectTransform; if (rect == null) continue;
                    if (child.GetComponent<CustomInteractable>() == null) PlaceUI(rect,new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(mw/2,-12),new Vector2(mw-12,22));
                    else { PlaceUI(rect,new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(mw/2,-28-row*(index+.5f)),new Vector2(mw-12,row-3)); index++; }
                }
            }
        }
        if (gm != null && gm.eventLog != null) {
            var log = (RectTransform)gm.eventLog.transform;
            PlaceUI(log,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-32),new Vector2(w-12,Mathf.Min(66,h*.19f)));
            if (transparentWindow != null) transparentWindow.RegisterClickable(log);
            if (gm.eventLogText != null) { var rt = (RectTransform)gm.eventLogText.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f,.5f); rt.sizeDelta = new Vector2(-58,-8); rt.anchoredPosition = new Vector2(-22,0); gm.eventLogText.overflowMode = TMPro.TextOverflowModes.Ellipsis; }
            var info = FindNamedRect("DamageInfoButton"); PlaceUI(info,new Vector2(1,1),new Vector2(1,1),new Vector2(-3,-3),new Vector2(42,22));
        }
        if (statsPanel != null) { PlaceUI(statsPanel,new Vector2(0,1),new Vector2(0,1),new Vector2(6,-106),new Vector2(Mathf.Min(470,w-250),90));
            var stat = statsPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true); if (stat != null) {var rt=(RectTransform)stat.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.sizeDelta=new Vector2(-16,-14);rt.anchoredPosition=Vector2.zero;rt.pivot=new Vector2(.5f,.5f);ConfigureCompactStatsText(stat);}
        }
        if (damagePanel != null) {
            var rt = (RectTransform)damagePanel.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f,.5f); rt.sizeDelta = new Vector2(-12,-12); rt.anchoredPosition = Vector2.zero;
            var text = (RectTransform)damageText.transform; text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one; text.sizeDelta = new Vector2(-16,-40); text.anchoredPosition = new Vector2(0,16);
            foreach(var name in new[]{"DamageOlder","DamageNewer","DamageClose"}) {
                var button = FindNamedRect(name); float x = name == "DamageOlder" ? 6 : name == "DamageNewer" ? 76 : Mathf.Max(146,w-80);
                PlaceUI(button,Vector2.zero,Vector2.zero,new Vector2(x,6),new Vector2(64,24));
            }
            if (damagePanel.activeSelf) RefreshDamageInfo();
        }
        if (gm != null && gm.preGameUI != null) {
            PlaceUI((RectTransform)gm.preGameUI.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(Mathf.Min(460,w-12),Mathf.Min(300,h-12)));
            LayoutStartMenu(gm.preGameUI.transform,w,h,false);
            if (gm.confirmationPopup != null) LayoutStartMenu(gm.confirmationPopup.transform,w,h,true);
        }
        // Text/button content stretches inside its resized parent at a fixed
        // readable pixel font, rather than shrinking a 1920px canvas to 250px.
        foreach(var text in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if (!text.transform.IsChildOf(hudCanvas)) continue;
            bool compact = text.transform.parent != null && text.transform.parent.GetComponent<CustomInteractable>() != null;
            float fontSize = text == (gm != null ? gm.eventLogText : null) ? 16f :
                text == (heroForUI != null ? heroForUI.atkStatusText : null) ? (w>=1000?17f:15f) :
                compact ? (w>=1000?16f:14f) : (w>=1000?17f:w>=750?16f:14f);
            ReadableText(text,fontSize);
            if (text.transform.parent != null && text.transform.parent.GetComponent<CustomInteractable>() != null && text != (gm != null ? gm.eventLogText : null)) {
                var rt = (RectTransform)text.transform; rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.sizeDelta=new Vector2(-8,-4);rt.anchoredPosition=Vector2.zero;rt.pivot=new Vector2(.5f,.5f);
            }
        }
        if (heroForUI != null && heroForUI.atkStatusText != null) ConfigureCompactStatsText(heroForUI.atkStatusText);
        if (devUI != null) devUI.Layout();
        RefreshCombatHUDVisibility();
    }
    private RectTransform FindNamedRect(string name)
    {
        foreach(var rt in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if (rt.gameObject.name == name && rt.IsChildOf(hudCanvas)) return rt; return null;
    }
    private void LayoutStartMenu(Transform root,float w,float h,bool confirmation)
    {
        var buttons = root.GetComponentsInChildren<Button>(true); float bw = Mathf.Min(200,(w-36)/2);
        for(int i=0;i<buttons.Length;i++) { var rt = (RectTransform)buttons[i].transform;
            PlaceUI(rt,new Vector2(.5f,.5f),new Vector2(.5f,.5f),confirmation ? new Vector2((i%2==0?-1:1)*(bw/2+5),-h*.23f) : new Vector2(0,-10-32*i),new Vector2(confirmation?bw:Mathf.Min(220,w-24),28)); }
        foreach(var text in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)) if (text.transform.parent.GetComponent<Button>() == null) {
            PlaceUI((RectTransform)text.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,confirmation?16:h*.18f),new Vector2(Mathf.Min(430,w-24),confirmation?h*.5f:Mathf.Min(90,h*.35f))); ReadableText(text,14);
        }
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

    public void Scale250() { ExecuteScale(600); }
    public void Scale800() { ExecuteScale(800); }
    public void Scale200() { ExecuteScale(600); }
    public void Scale500() { ExecuteScale(600); }
    public void Scale1000() { ExecuteScale(1150); }
}
