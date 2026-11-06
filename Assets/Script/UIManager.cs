using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public partial class UIManager : MonoBehaviour
{
    [Header("Core References")]
    public static UIManager Instance;

    [Header("UI MENUS")]
    public GameObject systemMenu;
    public GameObject mapMenu;
    public Image groundImage;
    public TransparentWindow transparentWindow;

    [Header("0.0.52 - ASSET KHO DO / TU TIEN")]
    [Tooltip("Tao asset: Assets > Create > Tu Tien > UI > Inventory Art Theme. " +
        "Keo vao day de tu gan Sprite cho kho do, panel chi so, hero va tung map.")]
    public InventoryArtTheme inventoryArtTheme;
    [Tooltip("Tắt để trở về màu mặc định trong khi tiếp tục giữ Sprite/Theme cho sau này.")]
    public bool inventoryUseArtTheme = false;

    [Header("0.0.52 UI WORKSPACE - DO KICH THUOC ASSET")]
    [Tooltip("Bật hướng dẫn đo Cao x Rộng trực tiếp trên cửa sổ Kho đồ.")]
    public bool inventoryLayoutDebug = false;
    [Tooltip("Lớp phủ xanh tối trong suốt. Chỉ để đo bố cục, không nhận click.")]
    public Color inventoryDebugFill = new Color(.035f,.17f,.20f,.16f);
    [Tooltip("Viền xanh ngọc để xác định đúng kích thước từng RectTransform.")]
    public Color inventoryDebugOutline = new Color(.18f,.72f,.73f,.95f);
    [Tooltip("Màu chữ đo kích thước trên overlay.")]
    public Color inventoryDebugText = new Color(.87f,.98f,.92f,1f);
    [Tooltip("Màu đỏ riêng cho số đo ở góc dưới phải.")]
    public Color inventoryDebugSizeText = new Color(1f,.24f,.25f,1f);

    [ContextMenu("0.0.52 UI - Bat/Tat khung do kich thuoc (Play Mode)")]
    public void ToggleInventoryLayoutDebug()
    {
        inventoryLayoutDebug = !inventoryLayoutDebug;
        if (inventoryUI != null) inventoryUI.UpdateLayoutDebug();
    }

    // UI is created at runtime. Keep all artwork editable on this persistent
    // scene component, never on the temporary InventoryPanelUI component.
    [ContextMenu("0.0.52 - Ap dung anh Inventory (Play Mode)")]
    public void ApplyInventoryArtFromInspector()
    {
        if (inventoryUseArtTheme && inventoryArtTheme == null)
        {
            Debug.LogWarning("[InventoryArt] Đã bật Theme nhưng chưa gán InventoryArtTheme.");
            return;
        }
        if (inventoryUI != null)
        {
            inventoryUI.Refresh();
            inventoryUI.UpdateLayoutDebug();
            Debug.Log(inventoryUseArtTheme
                ? "[InventoryArt] Đã cập nhật Sprite từ Theme."
                : "[InventoryArt] Theme đang tắt; dùng màu nền mặc định.");
        }
        else Debug.LogWarning("[InventoryArt] InventoryPanelUI chưa được tạo; mở Play Mode trước.");
    }

    [ContextMenu("0.0.52 UI - In bang kich thuoc UI vao Console")]
    public void PrintInventoryLayoutSizes()
    {
        if (inventoryUI == null)
            Debug.LogWarning("[UI SIZE 0.0.52] Hãy chạy Play Mode và mở Kho đồ trước khi đo.");
        else inventoryUI.PrintLayoutSizes();
    }

    [Header("POPUP THOÁT GAME")]
    public GameObject exitConfirmPopup; // Bảng hỏi "Bạn có muốn thoát..."

    private List<CustomInteractable> interactables = new List<CustomInteractable>();

    void Awake()
    {
        Instance = this;
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if(!WindowsDevelopmentAcceptanceV10.UseOwnedDefaultBalance())CombatBalance.LoadDevProfiles();
#else
        CombatBalance.LoadDevProfiles();
#endif
    }

    void Start()
    {
        if (!inventoryUseArtTheme)
            Debug.Log("[InventoryArt] Theme tạm tắt; Inventory sử dụng màu xanh tối mặc định.");
        else if (inventoryArtTheme == null)
            Debug.LogWarning("[InventoryArt] Đã bật Theme nhưng UIManager chưa gắn InventoryArtTheme.");
        else
            Debug.Log("[InventoryArt] Đang dùng Sprite từ InventoryArtTheme.");
        ManagementWindowLayout.SetCompact(Screen.width,Screen.height);
        InstallReadableCanvas();
        ConfigureSystemMenu();
        ConfigureGameplayPanels();
        worldMapUI = WorldMapPanelUI.Install(this,hudCanvas);
        ConfigureHudW3();
        LayoutReadableUI();
        if (mapMenu != null) mapMenu.SetActive(false);
        foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rect.gameObject.name == "MapIcon") rect.gameObject.SetActive(false);
    }
    public void SetDifficulty(int mode)
    {
        var gm = GameForUI();
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
        if(musicVolumePanel!=null)musicVolumePanel.Tick();
        // Read scroll before native pointer early-return so map zoom also works
        // in transparent Windows Player, not only in Unity Editor.
        var mapMouse=UnityEngine.InputSystem.Mouse.current;
        if(worldMapUI!=null && worldMapUI.IsOpen && mapMouse!=null)
        {
            Vector2 mapPointer;
            if(transparentWindow==null || !transparentWindow.TryGetPointerPosition(out mapPointer))
                mapPointer=mapMouse.position.ReadValue();
            worldMapUI.HandleScroll(mapPointer,mapMouse.scroll.ReadValue().y / 120f);
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // TransparentWindow owns native pointer events, including drag gestures.
        if (transparentWindow != null) return;
#endif
        var mouse=UnityEngine.InputSystem.Mouse.current;
        if(mouse==null)return;
        Vector2 point;
        if(transparentWindow != null && transparentWindow.TryGetPointerPosition(out point)) { }
        else point=mouse.position.ReadValue();
        if(mouse.leftButton.wasPressedThisFrame)
        {
            if(!TryBeginOpeningVolumeDrag(point))HandleMouseClick(point);
        }
        else if(mouse.leftButton.isPressed)DragOpeningVolume(point);
        if(mouse.leftButton.wasReleasedThisFrame)EndOpeningVolumeDrag();
    }

    public void RegisterInteractable(CustomInteractable item) { if (!interactables.Contains(item)) interactables.Add(item); }
    public void UnregisterInteractable(CustomInteractable item) { interactables.Remove(item); }

    public bool CheckInteractableHover(Vector2 mousePos)
    {
        if (worldMapUI != null && worldMapUI.HitMapSurface(mousePos)) return true;
        if(martialButton!=null && martialButton.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(martialButton,mousePos,null))return true;
        if(!worldMapButtonAvailable && worldMapButton!=null && worldMapButton.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(worldMapButton,mousePos,null))return true;
        foreach (var item in interactables)
        {
            if (item != null && item.isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null))
                return true;
        }
        return false;
    }

    public CustomInteractable HitControl(Vector2 mousePos)
    {
        GameManager gm = GameForUI();
        if(gm!=null && gm.IsEnteringGameplay)return null;
        bool confirmation = gm != null && gm.HasPendingConfirmation;
        CustomInteractable hit = null;
        foreach (var item in interactables)
        {
            if (item == null || !item.isActiveAndEnabled || item.GetRect() == null) continue;
            // Confirmation owns input even if another panel remains open below.
            if (confirmation)
            {
                if (gm.confirmationPopup == null || !item.transform.IsChildOf(gm.confirmationPopup.transform)) continue;
            }
            else
            {
                // World map modal takes precedence over the underlying HUD/buttons.
                if (worldMapUI != null && worldMapUI.IsOpen &&
                    !item.transform.IsChildOf(worldMapUI.Root)) continue;
                // The full-screen opening owns input while its menu is visible.
                // Never allow HUD/MenuArea controls below it to steal a menu click.
                if (gm != null && gm.preGameUI != null && gm.preGameUI.activeInHierarchy &&
                    !item.transform.IsChildOf(gm.preGameUI.transform) &&
                    (gm.confirmationPopup == null || !item.transform.IsChildOf(gm.confirmationPopup.transform))) continue;
                if(musicVolumePanel!=null && musicVolumePanel.IsOpen && !item.transform.IsChildOf(musicVolumePanel.Root))continue;
                if (devUI != null && devUI.IsOpen && !item.transform.IsChildOf(devUI.Root)) continue;
                if (inventoryUI != null && inventoryUI.IsOpen &&
                    !item.transform.IsChildOf(inventoryUI.Root)) continue;
                // Forge is an exclusive modal. Without this guard the old
                // Inventory buttons registered first win overlapping hitboxes,
                // so the visible Forge_Close can never receive its click.
                if (inventoryUI != null && inventoryUI.IsForgeOpen &&
                    !item.transform.IsChildOf(inventoryUI.ForgeRoot)) continue;
                if (damagePanel != null && damagePanel.activeSelf && (gm == null || !gm.HasPendingConfirmation) && !item.transform.IsChildOf(damagePanel.transform)) continue;
                if (systemMenu != null && systemMenu.activeInHierarchy &&
                    !item.transform.IsChildOf(systemMenu.transform)) continue;
            }
            if (!RectTransformUtility.RectangleContainsScreenPoint(item.GetRect(), mousePos, null)) continue;
            if (hit == null || item.transform.IsChildOf(hit.transform)) hit = item;
        }
        return hit;
    }
    public void HandleMouseClick(Vector2 mousePos)
    {
        // Buttons drawn above the map (Close, route list, confirmation) win
        // over hex hit tests when their rectangles overlap the map artwork.
        var hit = HitControl(mousePos);
        if (hit != null) { hit.onClickEvent?.Invoke(); return; }
        if (worldMapUI != null && worldMapUI.HandleMapClick(mousePos)) return;
    }
    public void HandlePointerClick(Vector2 press, Vector2 release)
    {
        var hit = HitControl(press);
        if (hit != null)
        {
            if (hit == HitControl(release)) hit.onClickEvent?.Invoke();
            return;
        }
        if (worldMapUI != null && worldMapUI.HitMapSurface(press))
        {
            if (worldMapUI.HitMapSurface(release)) worldMapUI.HandleMapClick(release);
            return;
        }
    }

    private StartMenuUI OpeningSettingsMenu()
    {
        var gm=GameForUI();
        return gm!=null && gm.preGameUI!=null && gm.preGameUI.activeInHierarchy
            && (gm.confirmationPopup==null || !gm.confirmationPopup.activeInHierarchy)
            ? gm.GetComponent<StartMenuUI>() : null;
    }

    public bool TryBeginOpeningVolumeDrag(Vector2 point)
    {
        var menu=OpeningSettingsMenu();
        openingVolumeDragMenu=null;
        if(menu!=null && menu.TryBeginVolumeDrag(point)) {openingVolumeDragMenu=menu;return true;}
        var gm=GameForUI();
        return gm!=null && gm.IsGameplayHUDVisible && !gm.HasPendingConfirmation &&
            musicVolumePanel!=null && musicVolumePanel.IsVolumeTrack(HitControl(point)) && musicVolumePanel.TryBeginDrag(point);
    }
    public bool HasOpeningVolumeDrag => openingVolumeDragMenu!=null || (musicVolumePanel!=null && musicVolumePanel.IsDragging);
    private StartMenuUI openingVolumeDragMenu;
    public void DragOpeningVolume(Vector2 point)
    {
        if(openingVolumeDragMenu!=null)openingVolumeDragMenu.DragVolume(point);
        else if(musicVolumePanel!=null)
        {
            var gm=GameForUI();
            if(gm==null || !gm.IsGameplayHUDVisible || gm.HasPendingConfirmation) {EndOpeningVolumeDrag();return;}
            musicVolumePanel.Drag(point);
        }
    }
    public void EndOpeningVolumeDrag()
    {
        if(openingVolumeDragMenu!=null)openingVolumeDragMenu.EndVolumeDrag();
        if(musicVolumePanel!=null)musicVolumePanel.EndDrag();
        openingVolumeDragMenu=null;
    }
    public bool IsTextInputAt(Vector2 point)
    {
        return devUI != null && devUI.IsOpen && devUI.InputAt(point);
    }

    private void ConfigureSystemMenu()
    {
        if (transparentWindow == null) transparentWindow = Object.FindFirstObjectByType<TransparentWindow>();
        if (systemMenu == null)
        {
            systemMenu=new GameObject("System Menu",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            systemMenu.transform.SetParent(combatUIRoot,false);
            systemMenu.SetActive(false);
        }
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
        savePage = MakePage("SystemSave", "LƯU THỦ CÔNG", root);
        overwritePage = MakePage("SystemOverwrite", "XÁC NHẬN GHI ĐÈ", root);
        loadPage = MakePage("SystemLoad", "TẢI BẢN LƯU", root);
        sizePage = MakePage("SystemSize", "KÍCH THƯỚC CỬA SỔ", root);
        saveTitle = savePage.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        overwriteTitle = overwritePage.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        MakeMenuButton(mainPage, "Exit", "Thoát", 0, ConfirmExit);
        MakeMenuButton(mainPage, "Save", "Lưu thủ công", 1, OpenManualSaveMenu);
        MakeMenuButton(mainPage, "Load", "Tải bản lưu", 2, OpenLoadMenu);
        MakeMenuButton(mainPage, "Reset", "Đặt lại nhân vật", 3, () => {
            CloseSystemMenu(); var gm = GameForUI(); if (gm != null) gm.OnResetClicked();
        });
        MakeMenuButton(mainPage, "WindowSize", "Kích thước cửa sổ", 4, () => ShowPage(sizePage));
        MakeMenuButton(mainPage, "ReturnTitle", "Quay lại màn hình chính", 5, ReturnToTitle);
        MakeMenuButton(mainPage, "Close", "Đóng", 6, CloseSystemMenu);
        MakeMenuButton(savePage, "Save1", "Lưu vào ô 1", 0, () => SaveToSlot(SaveSlot.ManualSave1));
        MakeMenuButton(savePage, "Save2", "Lưu vào ô 2", 1, () => SaveToSlot(SaveSlot.ManualSave2));
        MakeMenuButton(savePage, "SaveBack", "Quay lại", 2, () => ShowPage(mainPage));
        MakeMenuButton(overwritePage, "OverwriteYes", "Xác nhận ghi đè", 0, ConfirmManualOverwrite);
        MakeMenuButton(overwritePage, "OverwriteNo", "Hủy", 1, OpenManualSaveMenu);
        MakeMenuButton(overwritePage, "OverwriteBack", "Quay lại", 2, () => ShowPage(mainPage));
        MakeMenuButton(loadPage, "LoadSave1", "Save 1", 0, () => LoadSlot(SaveSlot.ManualSave1));
        MakeMenuButton(loadPage, "LoadSave2", "Save 2", 1, () => LoadSlot(SaveSlot.ManualSave2));
        MakeMenuButton(loadPage, "LoadAuto", "Auto", 2, () => LoadSlot(SaveSlot.AutoSave));
        MakeMenuButton(loadPage, "LoadBack", "Quay lại", 3, () => ShowPage(mainPage));
        MakeMenuButton(sizePage, "Size600", "Nhỏ · 600", 0, () => ExecuteScale(600));
        MakeMenuButton(sizePage, "Size800", "Vừa · 800", 1, () => ExecuteScale(800));
        MakeMenuButton(sizePage, "Size1150", "Lớn · 1150", 2, () => ExecuteScale(1150));
        MakeMenuButton(sizePage, "SizeBack", "Quay lại", 3, () => ShowPage(mainPage));
        ShowPage(mainPage);
        LayoutSystemMenu(ManagementWindowLayout.CombatPixels(Screen.width),
            ManagementWindowLayout.CombatHeight(Screen.height));
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

    private GameObject mainPage, savePage, overwritePage, loadPage, sizePage;
    private TMPro.TextMeshProUGUI menuFontTemplate, saveTitle, overwriteTitle;
    private SaveSlot pendingManualSlot;
    private GameObject MakePage(string name, string title, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        PanelHeader(rect,26);
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
        Skin(rect,SurfaceKind.Button);
    }
    public bool IsPaused { get; private set; }
    private TMPro.TextMeshProUGUI pauseText, damageText;
    private GameObject damagePanel;
    // Set only by a verifier after it has proved ownership of its save directory.
    // Transient UI context, never a persisted profile or a loader switch.
    internal bool IsolatedValidationProfile;
    private TMPro.TextMeshProUGUI devSourceCaption;
    private TMPro.TextMeshProUGUI progressionSummary;
    private float progressionRefreshElapsed;
    private RectTransform allocateHealthButton,allocatePhysicalButton;
    private CustomInteractable allocateHealthInput,allocatePhysicalInput;
    public string CharacterSourceStatus =>
        (IsolatedValidationProfile ? "THỬ NGHIỆM CÔ LẬP · " : "") +
        (CombatBalance.HeroDev != null ? "DEVB Hero đang override HP/Attack/tốc đánh/tăng trưởng/di chuyển/tầm" : "Baseline Hero bình thường") +
        (CombatBalance.MonsterDev != null ? " · DEVB quái bật" : "");
    private HeroController heroForUI;
    private RectTransform testSummonButton;
    private TMPro.TextMeshProUGUI testSummonText;
    private string[] displayedDamage = new string[0];
    private int damagePage;
    public void SetPaused(bool paused)
    {
        if(!paused) {
            var gm=GameForUI();
            if(gm!=null && gm.IsEnteringGameplay)return;
        }
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
        var caption=MakeLabel(rect,label,Vector2.zero,Vector2.zero);
        caption.rectTransform.anchorMin=Vector2.zero;caption.rectTransform.anchorMax=Vector2.one;
        caption.rectTransform.pivot=new Vector2(.5f,.5f);
        caption.rectTransform.offsetMin=new Vector2(6,4);caption.rectTransform.offsetMax=new Vector2(-6,-4);
        Skin(rect,SurfaceKind.Button);
        BindClick(rect, action); return rect;
    }
    private void ConfigureGameplayPanels()
    {
        var gm = GameForUI();
        var hero = Object.FindFirstObjectByType<HeroController>();
        Transform canvas = combatUIRoot;
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
        inventoryButton = SmallButton(canvas,"InventoryButton","Kho đồ",new Vector2(1,1),
            new Vector2(1,1),new Vector2(-308,-4),new Vector2(62,24),OpenInventory);
        volumeButton = SmallButton(canvas,"MusicVolumeButton","Âm lượng",new Vector2(1,1),
            new Vector2(1,1),new Vector2(-244,-4),new Vector2(62,24),ToggleMusicVolume);
        worldMapButton = SmallButton(canvas,"WorldMapButton","Bản đồ",new Vector2(1,1),
            new Vector2(1,1),new Vector2(-376,-4),new Vector2(62,24),ToggleWorldMapMenu);
        // The label is longer than 'Kho đồ': keep the same button size without
        // introducing a second text line at the compact 600px window preset.
        var volumeCaption = volumeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (volumeCaption != null)
        {
            ReadableText(volumeCaption,11f);
            volumeCaption.enableWordWrapping = false;
            volumeCaption.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        }
        musicVolumePanel = MusicVolumePanelUI.Install(this, canvas);
        inventoryUI = InventoryPanelUI.Install(this,hudCanvas);
        devUI = new DevBalanceUI(this, combatUIRoot);
        heroForUI = hero;
        HideLegacyAttackModeButtons();
        // Only a B2 test control. Future permanent companions are a separate feature.
        testSummonButton = SmallButton(canvas, "B2TestSummon", "[B2] Gọi +1 (0/2)",
            new Vector2(0,1), new Vector2(0,1), new Vector2(6,-139),
            new Vector2(146,26), () => {
                var game = GameForUI();
                if (game != null) game.CycleTestCompanions();
            });
        testSummonText = testSummonButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        SetPaused(false);
        if (gm != null && gm.eventLog != null) {
            // Reserve space at the right of the log so Info cannot obscure a formula/name.
            var log = (RectTransform)gm.eventLog.transform;
            BindClick(log,OpenLogInfo);
            SmallButton(log,"DamageInfoButton","Chi tiết",new Vector2(1,1),new Vector2(1,1),new Vector2(-8,-8),new Vector2(100,42),OpenDamageInfo);
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
        progressionSummary=MakeLabel(body,"",Vector2.zero,Vector2.zero);
        progressionSummary.gameObject.name="CharacterProgressionSummary";
        progressionSummary.alignment=TMPro.TextAlignmentOptions.TopLeft;
        ReadableText(progressionSummary,13f);
        allocateHealthButton=SmallButton(body,"CharacterAllocateHP","+6 HP · 1 điểm",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,()=>{
            var owner=GameForUI();if(owner!=null)owner.AllocateHeroHealth();
        });
        allocatePhysicalButton=SmallButton(body,"CharacterAllocatePhysical","+1 Physical Attack · 1 điểm",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,()=>{
            var owner=GameForUI();if(owner!=null)owner.AllocateHeroPhysicalAttack();
        });
        allocateHealthInput=allocateHealthButton.GetComponent<CustomInteractable>();
        allocatePhysicalInput=allocatePhysicalButton.GetComponent<CustomInteractable>();
        LayoutCharacterProgression();
        if (transparentWindow != null) transparentWindow.RegisterClickable(body);
        damagePanel.SetActive(false);
    }
    private static void ConfigureCompactStatsText(TMPro.TextMeshProUGUI label)
    {
        // The full debug stat block wraps inside its own bounded panel.
        label.enableWordWrapping = true;
        label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        label.alignment = TMPro.TextAlignmentOptions.TopLeft;
    }
    public static bool ShouldShowCombatHUD(bool deployed, bool starting, bool gameOver, bool confirmation, bool modal)
    {
        return deployed && !starting && !gameOver && !confirmation && !modal;
    }
    private void RefreshCombatHUDVisibility()
    {
        var gm = GameForUI();
        // These panels do not pause combat. Once Game Over/entry/title owns
        // the screen, they must stop owning hit-tests over Retry/menu controls.
        if(gm!=null && !gm.IsGameplayHUDVisible)
        {
            CloseDamageInfo();
            CloseSystemMenu();
        }
        bool shown = ShouldShowCombatHUD(gm != null && gm.IsGameplayHUDVisible,
            gm != null && gm.preGameUI != null && gm.preGameUI.activeInHierarchy,
            gm != null && gm.gameOverPanel != null && gm.gameOverPanel.activeInHierarchy,
            gm != null && gm.confirmationPopup != null && gm.confirmationPopup.activeInHierarchy,
            (damagePanel != null && damagePanel.activeSelf) || (devUI != null && devUI.IsOpen) ||
            (systemMenu != null && systemMenu.activeInHierarchy) ||
            (inventoryUI != null && inventoryUI.IsOpen) ||
            (worldMapUI != null && worldMapUI.IsOpen));
        if (inventoryUI != null && inventoryUI.IsOpen &&
            (gm == null || !gm.IsGameplayHUDVisible)) inventoryUI.Close();
        if(w3!=null)w3.Root.gameObject.SetActive(shown);
        if(musicVolumePanel!=null)musicVolumePanel.SetVisibleInGameplay(shown);
        if(statsPanel!=null)statsPanel.gameObject.SetActive(false);
        if(gm!=null && gm.eventLog!=null)gm.eventLog.gameObject.SetActive(shown && showCompactLog);

    }
    private void HideLegacyAttackModeButtons()
    {
        // Keep scene data/GUIDs, remove these legacy controls from gameplay and input.
        foreach(var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(rect.gameObject.name=="Kiếm" || rect.gameObject.name=="Cung" || rect.gameObject.name=="Phép")
                rect.gameObject.SetActive(false);
    }
    public void OpenDamageInfo()
    {
        if (damagePanel == null) return;
        detailKind = 0; detailSlice = 0;
        var gm = GameForUI();displayedDamage = gm != null ? gm.DamageHistory : new string[0];damagePage = 0;
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
        LayoutCharacterProgression();RefreshCharacterProgression();
        string title = detailKind == 1 ? "CHỈ SỐ HERO" : detailKind == 2 ? "NHẬT KÝ" : "SÁT THƯƠNG " + (damagePage+1) + "/" + System.Math.Max(1,displayedDamage.Length);
        string content = detailKind != 0 ? detailSnapshot : displayedDamage.Length == 0 ? "Chưa có đòn đánh hoàn tất." : displayedDamage[damagePage];
        var pages = ReadablePages(content, Mathf.Max(12,ManagementWindowLayout.CombatPixels(Screen.width)-40), Mathf.Max(20,Screen.height-(detailKind==1?(CultivationVisible?262:196):92)));
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
        OpenTextDetail(1,CurrentHeroDetails());
    }
    private string CurrentHeroDetails()
    {
        var hero = heroForUI;
        var gm=GameForUI();
        string stats=hero!=null ? hero.FullStatDetails : "Chưa có nhân vật.";
        if (gm!=null)
        {
            if (gm.HeroData!=null) stats=TuTienCore.IdentityDisplay.Describe(gm.HeroData)+" · Cấp "+gm.HeroData.currentLevel+"\n"+stats;
        }
        return stats;
    }
    public void RefreshCharacterProgression()
    {using(TuTienCore.CultivationPerformance.Measure(TuTienCore.CultivationPerformance.Part.CharacterUI))RefreshCharacterProgressionCore();}
    private void RefreshCharacterProgressionCore()
    {
        if(devSourceCaption==null && devButton!=null)devSourceCaption=devButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        string label=CombatBalance.HeroDev!=null?"DEVB · Hero override":CombatBalance.MonsterDev!=null?"DEVB · Quái override":"DEVB · Baseline";
        if(devSourceCaption!=null && devSourceCaption.text!=label)devSourceCaption.text=label;
        if(damagePanel!=null && damagePanel.activeSelf && detailKind==1)
        {
            var gm=GameForUI();var data=gm!=null?gm.HeroData:null;
            var state=CombatManager.Instance!=null?CombatManager.Instance.CurrentHeroCombatState:null;
            string health=state!=null && state.Data==data?state.CurrentHP+"/"+state.MaxHP:"—";
            if(gm!=null && gm.Cultivation!=null)LayoutCultivationTrack(Mathf.Max(280,ManagementWindowLayout.CombatPixels(Screen.width)-12));
            if(progressionSummary!=null)progressionSummary.text=gm!=null && gm.Cultivation!=null?CultivationSummary(gm):(data==null?"Chưa có nhân vật.":"Level "+data.currentLevel+" · "+LegacyRealmSnapshotCompatibility.Describe(data)+
                "\nTu luyện mới chưa kích hoạt · Điểm thuộc tính: "+data.statPoints+
                "\nHP: "+health+" · Physical Attack: "+WeaponBasicAttack.Physical(data)+" · EXP: "+data.currentExp+"/"+data.expToNextLevel)+"\n"+CharacterSourceStatus;
            bool can=gm!=null && gm.CanAllocateHeroAttribute;
            SetAllocationEnabled(allocateHealthInput,can);SetAllocationEnabled(allocatePhysicalInput,can);
            string current=CurrentHeroDetails();
            if(current!=detailSnapshot){detailSnapshot=current;RefreshDamageInfo();}
        }
    }
    private static void SetAllocationEnabled(CustomInteractable input,bool available)
    {
        if(input==null || input.enabled==available)return;input.enabled=available;
        var image=input.GetComponent<Image>();var color=image.color;color.a=available?1f:.4f;image.color=color;
    }
    private void LayoutCharacterProgression()
    {
        if(progressionSummary==null)return;
        bool show=detailKind==1;
        progressionSummary.gameObject.SetActive(show);allocateHealthButton.gameObject.SetActive(show);allocatePhysicalButton.gameObject.SetActive(show);
        float cultivationWidth=Mathf.Max(280,ManagementWindowLayout.CombatPixels(Screen.width)-12);
        LayoutCultivationTrack(cultivationWidth);
        if(!show) {
            var ordinary=damageText.rectTransform;ordinary.anchorMin=Vector2.zero;ordinary.anchorMax=Vector2.one;
            ordinary.sizeDelta=new Vector2(-16,-40);ordinary.anchoredPosition=new Vector2(0,16);return;
        }
        float width=Mathf.Max(280,ManagementWindowLayout.CombatPixels(Screen.width)-12);
        PlaceUI(progressionSummary.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(8,-8),new Vector2(width-16,CultivationVisible?150:92));
        float half=(width-24)*.5f;
        PlaceUI(allocateHealthButton,Vector2.zero,Vector2.zero,new Vector2(8,36),new Vector2(half,28));
        PlaceUI(allocatePhysicalButton,Vector2.zero,Vector2.zero,new Vector2(16+half,36),new Vector2(half,28));
        var text=damageText.rectTransform;text.anchorMin=Vector2.zero;text.anchorMax=Vector2.one;text.sizeDelta=new Vector2(-16,CultivationVisible?-238:-172);text.anchoredPosition=new Vector2(0,CultivationVisible?-51:-18);
    }
    public void OpenLogInfo()
    {
        var gm = GameForUI();
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
        CloseDamageInfo(); CloseSystemMenu();
        if (inventoryUI != null) inventoryUI.Close();
        if (devUI != null) devUI.Open();
    }

    private bool wasPausedBeforeInventory;
    private bool inventoryPauseCaptured;

    public void ToggleMusicVolume()
    {
        var gm = GameForUI();
        if (gm == null || !gm.IsGameplayHUDVisible || musicVolumePanel == null) return;
        musicVolumePanel.Toggle();
    }

    public void OpenInventory()
    {
        if (inventoryUI == null || (devUI != null && devUI.IsOpen) ||
            inventoryUI.IsOpen) return;
        var gm=GameForUI();
        if (gm == null || !gm.IsGameplayHUDVisible) return;
        CloseDamageInfo(); CloseSystemMenu();
        wasPausedBeforeInventory=IsPaused;
        inventoryPauseCaptured=true;
        SetPaused(true);
        inventoryUI.Open();
        if (!inventoryUI.IsOpen) { OnInventoryDismissed(); return; }

        // Windows: add a right-side strip. If the monitor cannot fit it, the
        // same UI falls back to a bounded overlay inside the compact window.
        if (transparentWindow == null)
            transparentWindow=Object.FindFirstObjectByType<TransparentWindow>();
        int maxWidth=transparentWindow != null ?
            transparentWindow.GetMaximumUsableWidth() :
            Screen.width+ManagementWindowLayout.PreferredSidePanel;
        int maxHeight=transparentWindow != null ?
            transparentWindow.GetMaximumUsableHeight() : Mathf.Max(680,Screen.height);
        if (ManagementWindowLayout.Expand(maxWidth,maxHeight))
        {
            if (transparentWindow != null)
                transparentWindow.ResizeWindow(ManagementWindowLayout.TargetWidth,
                    ManagementWindowLayout.TargetHeight,true);
            else
                Screen.SetResolution(ManagementWindowLayout.TargetWidth,
                    ManagementWindowLayout.TargetHeight,FullScreenMode.Windowed);
        }
        LayoutReadableUI();
    }

    public void OnInventoryDismissed()
    {
        if (ManagementWindowLayout.Expanded)
        {
            int oldWidth=ManagementWindowLayout.CompactWidth;
            int oldHeight=ManagementWindowLayout.CompactHeight;
            ManagementWindowLayout.Collapse();
            if (transparentWindow != null)
                transparentWindow.ResizeWindow(oldWidth,oldHeight,true);
            else Screen.SetResolution(oldWidth,oldHeight,FullScreenMode.Windowed);
        }
        if (inventoryPauseCaptured)
        {
            inventoryPauseCaptured=false;
            SetPaused(wasPausedBeforeInventory);
        }
    }

    private RectTransform hudCanvas, combatUIRoot, pauseRect, devButton, statsButton, inventoryButton, volumeButton, worldMapButton, statsPanel;
    private WorldMapPanelUI worldMapUI;
    private MusicVolumePanelUI musicVolumePanel;
    private InventoryPanelUI inventoryUI;
    private DevBalanceUI devUI;
    private int layoutWidth, layoutHeight;

    // A separate presentation label: does not change map progress or world coordinates.
    // Compatibility entry points: the persistent W3 map view reads live world data.
    // The old animated title no longer writes over the W3 layout.
    public void UpdateMapDebug(EntityDataSO data) { TickHudW3(); }
    public void ShowMapTitle(string name) { }
    public void HideMapTitle() { }

    public static float StatsColumnWidth(float width)
    {
        return Mathf.Min(340f,Mathf.Max(220f,width*.40f));
    }
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
        var battlefieldHud = new GameObject("Compact HUD Viewport",typeof(RectTransform));
        combatUIRoot = (RectTransform)battlefieldHud.transform;
        combatUIRoot.SetParent(hudCanvas,false);
        combatUIRoot.anchorMin = combatUIRoot.anchorMax = Vector2.zero;
        combatUIRoot.pivot = Vector2.zero;
        combatUIRoot.anchoredPosition = Vector2.zero;
        combatUIRoot.sizeDelta = new Vector2(Screen.width,Screen.height);
        var gm = GameForUI();
        var oldRoot = systemMenu != null ? systemMenu.transform.parent : gm != null && gm.eventLog != null ? gm.eventLog.transform.parent : null;
        var move = new List<Transform>();
        if (oldRoot != null) foreach(Transform child in oldRoot) {
            if (child.gameObject.name == "Ground") continue;
            move.Add(child);
        }
        foreach(var child in move) child.SetParent(combatUIRoot,false);
        if (gm != null) {
            if (gm.preGameUI != null) gm.preGameUI.transform.SetParent(combatUIRoot,false);
            if (gm.confirmationPopup != null) gm.confirmationPopup.transform.SetParent(combatUIRoot,false);
            if (gm.gameOverPanel != null) gm.gameOverPanel.transform.SetParent(combatUIRoot,false);
            if (gm.eventLog != null) gm.eventLog.transform.SetParent(combatUIRoot,false);
        }
    }
    void LateUpdate()
    {
        // Bind scene controls once after Start; view updates use cached references.
        // Do NOT relearn the compact preset during asynchronous collapse:
        // Screen.width may still equal the expanded dimensions for a frame.
        ManagementWindowLayout.ResolveResize(Screen.width,Screen.height);
        // TransparentWindow sets its startup resolution asynchronously. Once
        // settled, learn the real compact size, but never learn a transient
        // expanded frame or an in-progress explicit size preset.
        if (!ManagementWindowLayout.Expanded &&
            !ManagementWindowLayout.CollapsePending &&
            !ManagementWindowLayout.CompactResizePending &&
            (ManagementWindowLayout.CompactWidth != Screen.width ||
             ManagementWindowLayout.CompactHeight != Screen.height))
            ManagementWindowLayout.SetCompact(Screen.width,Screen.height);
        if (Screen.width != layoutWidth || Screen.height != layoutHeight || (hudLayout!=null && hudLayout.Revision!=appliedHudLayout))
            LayoutReadableUI();
        RefreshCombatHUDVisibility();
        TickHudW3();
        progressionRefreshElapsed+=Time.unscaledDeltaTime;
        if(progressionRefreshElapsed>=.2f){progressionRefreshElapsed=0f;RefreshCharacterProgression();}
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
        var gm = GameForUI();
        float w = ManagementWindowLayout.CombatPixels(Screen.width);
        float h = ManagementWindowLayout.CombatHeight(Screen.height);
        if (combatUIRoot != null)
            PlaceUI(combatUIRoot,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(w,h));
        if(worldMapUI!=null)worldMapUI.Resize();
        if(musicVolumePanel!=null)musicVolumePanel.Layout();
        if(inventoryUI!=null)inventoryUI.Layout(w,h,Screen.width,Screen.height);
        LayoutSystemMenu(w,h);
        LayoutHudW3(w,h);
        if (damagePanel != null) {
            var rt = (RectTransform)damagePanel.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f,.5f); rt.sizeDelta = new Vector2(-12,-12); rt.anchoredPosition = Vector2.zero;
            var text = (RectTransform)damageText.transform; text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one; text.sizeDelta = new Vector2(-16,-40); text.anchoredPosition = new Vector2(0,16);
            foreach(var name in new[]{"DamageOlder","DamageNewer","DamageClose"}) {
                var button = FindNamedRect(name); float x = name == "DamageOlder" ? 6 : name == "DamageNewer" ? 76 : Mathf.Max(146,w-80);
                PlaceUI(button,Vector2.zero,Vector2.zero,new Vector2(x,6),new Vector2(64,24));
                // Compact footer captions must fit one line in the actual Player's24px controls.
                var caption=button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if(caption!=null){ReadableText(caption,12);caption.enableWordWrapping=false;}
            }
            if (damagePanel.activeSelf) RefreshDamageInfo();
        }
        if (gm != null && gm.preGameUI != null) {
            var titleUI = gm.GetComponent<StartMenuUI>();
            if (titleUI != null)
                titleUI.Layout(w,h);
            else {
                PlaceUI((RectTransform)gm.preGameUI.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(Mathf.Min(460,w-12),Mathf.Min(300,h-12)));
                LayoutStartMenu(gm.preGameUI.transform,w,h,false);
            }
            if (gm.confirmationPopup != null && titleUI==null) LayoutStartMenu(gm.confirmationPopup.transform,w,h,true);
        }
        if (devUI != null) devUI.Layout();
        RefreshCombatHUDVisibility();
    }
    private RectTransform FindNamedRect(string name)
    {
        RectTransform cached;
        if(namedUI.TryGetValue(name,out cached) && cached!=null)return cached;
        foreach(var rt in hudCanvas.GetComponentsInChildren<RectTransform>(true))
            if(rt.gameObject.name==name) {namedUI[name]=rt;return rt;}
        return null;
    }
    private void LayoutSystemMenu(float w,float h)
    {
        if (systemMenu == null) return;
        float mw = Mathf.Min(340,w-12), mh = Mathf.Min(320,h-12);
        PlaceUI((RectTransform)systemMenu.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-6,6),new Vector2(mw,mh));
        foreach(var page in new[]{mainPage,savePage,overwritePage,loadPage,sizePage,toolsPage}) if (page != null) {
            int buttonCount = 0;
            foreach(Transform child in page.transform)
                if (child.GetComponent<CustomInteractable>() != null) buttonCount++;
            float row = (mh-28)/Mathf.Max(1,buttonCount);
            int index = 0;
            foreach(Transform child in page.transform) {
                var rect = child as RectTransform; if (rect == null) continue;
                if(child.gameObject.name=="Header Background")continue;
                if (child.GetComponent<CustomInteractable>() == null)
                    PlaceUI(rect,new Vector2(0,1),new Vector2(.5f,.5f),
                        new Vector2(mw/2,-12),new Vector2(mw-12,22));
                else {
                    PlaceUI(rect,new Vector2(0,1),new Vector2(.5f,.5f),
                        new Vector2(mw/2,-28-row*(index+.5f)),new Vector2(mw-12,row-3));
                    foreach(var label in child.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)) {
                        PlaceUI((RectTransform)label.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),
                            Vector2.zero,new Vector2(mw-20,row-7));
                        ReadableText(label,14);
                    }
                    index++;
                }
            }
        }
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
        if (savePage != null) savePage.SetActive(savePage == page);
        if (overwritePage != null) overwritePage.SetActive(overwritePage == page);
        if (loadPage != null) loadPage.SetActive(loadPage == page);
        if (sizePage != null) sizePage.SetActive(sizePage == page);
        if (toolsPage != null) toolsPage.SetActive(toolsPage == page);
    }

    private void OpenLoadMenu()
    {
        // Read the ACTUAL snapshot on disk. AutoSave can be much newer than
        // ManualSave1/2, even if all three belong to the same character.
        var saves = Object.FindFirstObjectByType<SaveManager>();
        RefreshLoadChoice("LoadSave1","Ô 1",SaveSlot.ManualSave1,saves);
        RefreshLoadChoice("LoadSave2","Ô 2",SaveSlot.ManualSave2,saves);
        RefreshLoadChoice("LoadAuto","Tự động",SaveSlot.AutoSave,saves);
        ShowPage(loadPage);
    }

    private void RefreshLoadChoice(string objectName,string label,
        SaveSlot slot,SaveManager saves)
    {
        if (loadPage == null) return;
        foreach(Transform child in loadPage.transform)
        {
            if (child.gameObject.name != objectName) continue;
            var text = child.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (text == null) break;
            int level,map;
            text.text = saves != null && saves.TryGetSlotProgress(slot,out level,out map)
                ? label + "  ·  Cấp " + level + "  ·  Map " + map
                : label + "  ·  Trống";
            break;
        }
    }

    private void OpenManualSaveMenu()
    {
        if (saveTitle != null) saveTitle.text = "LƯU THỦ CÔNG";
        ShowPage(savePage);
    }

    private void SaveToSlot(SaveSlot slot)
    {
        if (slot != SaveSlot.ManualSave1 && slot != SaveSlot.ManualSave2) return;
        var gm = GameForUI();
        if (gm == null)
        {
            if (saveTitle != null) saveTitle.text = "KHÔNG TÌM THẤY GAME";
            return;
        }
        if (gm.HasSaveSlot(slot))
        {
            pendingManualSlot = slot;
            if (overwriteTitle != null) overwriteTitle.text =
                slot == SaveSlot.ManualSave1 ? "GHI ĐÈ Ô 1?" : "GHI ĐÈ Ô 2?";
            ShowPage(overwritePage);
            return;
        }
        WriteManualSlot(gm,slot);
    }

    private void ConfirmManualOverwrite()
    {
        // Do not trigger extra writes when the confirmation page is inactive.
        if (overwritePage == null || !overwritePage.activeSelf) return;
        var gm = GameForUI();
        WriteManualSlot(gm,pendingManualSlot);
    }

    private void WriteManualSlot(GameManager gm,SaveSlot slot)
    {
        if (gm != null && gm.TryManualSave(slot))
        {
            CloseSystemMenu();
            return;
        }
        if (saveTitle != null) saveTitle.text = "LƯU THẤT BẠI";
        ShowPage(savePage);
    }
    public void CloseSystemMenu() { if (systemMenu != null) systemMenu.SetActive(false); ShowPage(mainPage); }
    private void ReturnToTitle()
    {
        var gm=GameForUI();
        if(gm!=null && gm.ReturnToMainMenu())CloseSystemMenu();
    }
    private void LoadSlot(SaveSlot slot)
    {
        var gm = GameForUI();
        if (gm != null && gm.LoadAndContinue(slot)) CloseSystemMenu();
    }

    public void ToggleSystemMenu()
    {
        if (systemMenu == null) return;
        systemMenu.SetActive(!systemMenu.activeSelf);
        if (systemMenu.activeSelf) { ShowPage(mainPage); systemMenu.transform.SetAsLastSibling(); }
        if (mapMenu != null) mapMenu.SetActive(false);
        CloseWorldMapMenu();
    }

    public void ToggleWorldMapMenu() { if (worldMapUI != null) worldMapUI.Toggle(); }
    public void CloseWorldMapMenu() { if (worldMapUI != null) worldMapUI.Close(); }
    public void ToggleMapMenu() { ToggleWorldMapMenu(); }

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

    // Shared resize path: the title settings should remain open after choosing
    // a radio size, while the in-game legacy size page still closes itself.
    public void ApplyWindowSizeFromOpening(int width)
    {
        if(width!=600 && width!=800 && width!=1150)return;
        ResizeCompactWindow(width);
    }
    private void ResizeCompactWindow(int width)
    {
        if (transparentWindow == null) transparentWindow = FindObjectOfType<TransparentWindow>();
        int height = Mathf.RoundToInt(width * (9f / 16f));
        ManagementWindowLayout.RequestCompact(width,height);
        if (transparentWindow != null)
            transparentWindow.ResizeWindow(width,height);
        else
            Screen.SetResolution(width,height,FullScreenMode.Windowed);
    }
    private void ExecuteScale(int width)
    {
        ResizeCompactWindow(width);
        CloseSystemMenu();
    }

    public void Scale250() { ExecuteScale(600); }
    public void Scale800() { ExecuteScale(800); }
    public void Scale200() { ExecuteScale(600); }
    public void Scale500() { ExecuteScale(600); }
    public void Scale1000() { ExecuteScale(1150); }
}
