using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManager
{
    [Header("HUD W3 — pixel màn hình, chỉnh trực tiếp trong Play Mode")]
    public HudW3Layout hudLayout = new HudW3Layout();
    [Tooltip("Chỉ bật/tắt phần hiển thị. Lịch sử sự kiện/sát thương vẫn được ghi.")]
    public bool showCompactLog;
    private GameManager hudGame;
    private bool gameResolved;
    private GameManager GameForUI()
    {
        if(!gameResolved) {hudGame=Object.FindFirstObjectByType<GameManager>();gameResolved=true;}
        return hudGame;
    }
    private HudW3View w3;
    private RectTransform martialButton, menuButton, logToggle;
    private GameObject toolsPage;
    private TextMeshProUGUI logToggleLabel;
    private int appliedHudLayout;
    private bool themeDirty=true;
    private bool worldMapButtonAvailable=true;
    private bool b2ControlPaused;
    private int b2DisplayedCount=-1;
    private InventoryArtTheme appliedTheme;
    private bool appliedArtEnabled;
    private int appliedThemeRevision;
    private readonly Dictionary<string,RectTransform> namedUI = new Dictionary<string,RectTransform>();

    internal enum SurfaceKind { Frame, Header, Card, Avatar, Track, Health, Progress, Button }
    private sealed class SurfaceBinding
    {
        internal Image Image;
        internal Outline Border;
        internal SurfaceKind Kind;
    }
    private readonly List<SurfaceBinding> surfaces = new List<SurfaceBinding>();
    internal RectTransform Surface(Transform parent,string name,SurfaceKind kind)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).transform;
        r.SetParent(parent,false); Skin(r,kind);return r;
    }
    internal void Skin(RectTransform rect,SurfaceKind kind)
    {
        if(rect==null)return;
        var image=rect.GetComponent<Image>();if(image==null)return;
        foreach(var old in surfaces)if(old.Image==image)return;
        var border=kind==SurfaceKind.Health || kind==SurfaceKind.Progress ? null : rect.GetComponent<Outline>();
        if(border==null && kind!=SurfaceKind.Health && kind!=SurfaceKind.Progress)border=rect.gameObject.AddComponent<Outline>();
        if(border!=null) {border.effectDistance=new Vector2(1,-1);border.useGraphicAlpha=false;}
        var binding=new SurfaceBinding {Image=image,Border=border,Kind=kind};
        surfaces.Add(binding);ApplySurface(binding);themeDirty=true;

    }
    internal RectTransform PanelHeader(RectTransform parent,float height=32)
    {
        var r=Surface(parent,"Header Background",SurfaceKind.Header);
        r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);
        r.sizeDelta=new Vector2(0,height);r.anchoredPosition=Vector2.zero;
        r.SetAsFirstSibling();return r;
    }
    internal static void ApplySurfaceArt(Image image,Sprite sprite,Color fallback,bool sliced)
    {
        if(image==null)return;
        image.sprite=sprite;image.color=sprite==null?fallback:Color.white;
        image.type=sliced && sprite!=null && sprite.border!=Vector4.zero?Image.Type.Sliced:Image.Type.Simple;
        image.preserveAspect=false;image.raycastTarget=false;
    }
    private void ApplySurface(SurfaceBinding b)
    {
        var art=inventoryUseArtTheme?inventoryArtTheme:null;
        Sprite sprite=null;Color fallback;
        switch(b.Kind)
        {
            case SurfaceKind.Header: sprite=art==null?null:art.hudHeader;fallback=new Color(.10f,.19f,.21f,.99f);break;
            case SurfaceKind.Button: sprite=art==null?null:art.actionButton;fallback=new Color(.16f,.29f,.30f,.99f);break;
            case SurfaceKind.Card: sprite=art==null?null:art.hudHeroCard;fallback=new Color(.046f,.082f,.098f,.96f);break;
            case SurfaceKind.Avatar: sprite=art==null?null:art.hudAvatarFrame;fallback=new Color(.10f,.17f,.18f,1);break;
            case SurfaceKind.Track: sprite=art==null?null:art.hudStatTrack;fallback=new Color(.025f,.055f,.065f,1);break;
            case SurfaceKind.Health: sprite=art==null?null:art.hudHealthFill;fallback=new Color(.24f,.56f,.39f,1);break;
            case SurfaceKind.Progress: sprite=art==null?null:art.hudProgressFill;fallback=new Color(.13f,.37f,.34f,1);break;
            default: sprite=art==null?null:art.hudPanelFrame;fallback=new Color(.046f,.082f,.098f,.98f);break;
        }
        ApplySurfaceArt(b.Image,sprite,fallback,art!=null && art.slicedResizablePanels);
        if(b.Border!=null)b.Border.effectColor=new Color(.30f,.44f,.41f,.72f);
    }
    [ContextMenu("HUD W3 - Ap dung layout va theme (Play Mode)")]
    public void ApplyHudFromInspector()
    {
        themeDirty=true;
        if(hudCanvas!=null) {LayoutReadableUI();RefreshHudTheme();}
    }
    private void OnValidate() {themeDirty=true;}
    private void RefreshHudTheme()
    {
        int revision=inventoryArtTheme==null?0:inventoryArtTheme.HudRevision;
        if(!themeDirty && appliedTheme==inventoryArtTheme && appliedArtEnabled==inventoryUseArtTheme && appliedThemeRevision==revision)return;
        themeDirty=false;appliedTheme=inventoryArtTheme;appliedArtEnabled=inventoryUseArtTheme;appliedThemeRevision=revision;
        foreach(var b in surfaces)if(b.Image!=null)ApplySurface(b);
        if(martialButton!=null) Skin(martialButton,SurfaceKind.Button);
        if(worldMapButton!=null)
        {
            var caption=worldMapButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if(caption!=null)caption.color=worldMapButtonAvailable?Color.white:new Color(.49f,.58f,.58f,1);
            if(!worldMapButtonAvailable)worldMapButton.GetComponent<Image>().color=new Color(.10f,.15f,.16f,1);
        }
        if(testSummonButton!=null && testSummonText!=null)
        {
            testSummonText.color=b2ControlPaused?new Color(.49f,.58f,.58f,1):Color.white;
            if(b2ControlPaused)testSummonButton.GetComponent<Image>().color=new Color(.10f,.15f,.16f,1);
        }
        if(inventoryUI!=null)inventoryUI.ApplyTheme();
    }
    // Keep the reference when hidden. Loot checks activeInHierarchy every flight tick.
    public RectTransform InventoryLootTarget => inventoryButton;
    public void ToggleCompactLog() {showCompactLog=!showCompactLog;RefreshCombatHUDVisibility();}

    private void ConfigureHudW3()
    {
        hudGame=GameForUI();
        w3=new HudW3View(this,combatUIRoot);
        // Reuse the one Inventory control; no duplicate callbacks or hitbox.
        pauseRect.gameObject.name="PauseButton";
        Skin(pauseRect,SurfaceKind.Button);
        statsButton.SetParent(w3.Root,false);inventoryButton.SetParent(w3.Root,false);
        worldMapButton=w3.MapButton;pauseRect.SetParent(w3.Root,false);
        statsButton.GetComponentInChildren<TextMeshProUGUI>(true).text="Nhân vật";
        inventoryButton.GetComponentInChildren<TextMeshProUGUI>(true).text="Hòm đồ";
        martialButton=SmallButton(w3.Root,"WeaponSkillsButton","Võ học",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,OpenWeaponSkills);
        menuButton=SmallButton(w3.Root,"SystemMenuButton","Menu",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,ToggleSystemMenu);
        logToggle=SmallButton(w3.Root,"CompactLogToggle","Nhật ký",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,ToggleCompactLog);
        logToggleLabel=logToggle.GetComponentInChildren<TextMeshProUGUI>(true);

        toolsPage=MakePage("SystemTools","CÔNG CỤ & HIỂN THỊ",(RectTransform)systemMenu.transform);
        MakeMenuButton(mainPage,"Tools","Công cụ & hiển thị",7,()=>ShowPage(toolsPage));
        MakeMenuButton(toolsPage,"ToolsPause","Tạm dừng / Tiếp tục",0,TogglePause);
        volumeButton.SetParent(toolsPage.transform,false);
        BindClick(volumeButton,()=>{CloseSystemMenu();ToggleMusicVolume();});
        MakeMenuButton(toolsPage,"ToolsCompactLog","Bật / tắt nhật ký ngắn",2,()=>{ToggleCompactLog();CloseSystemMenu();});
        MakeMenuButton(toolsPage,"ToolsHistory","Toàn bộ nhật ký",3,()=>{CloseSystemMenu();OpenLogInfo();});
        MakeMenuButton(toolsPage,"ToolsDamage","Chi tiết sát thương",4,()=>{CloseSystemMenu();OpenDamageInfo();});
        testSummonButton.SetParent(toolsPage.transform,false);
        devButton.SetParent(toolsPage.transform,false);
        MakeMenuButton(toolsPage,"ToolsBack","Quay lại",7,()=>ShowPage(mainPage));
        toolsPage.SetActive(false);
        Skin((RectTransform)systemMenu.transform,SurfaceKind.Frame);
        BindClick((RectTransform)systemMenu.transform,()=>{});
        Skin((RectTransform)damagePanel.transform,SurfaceKind.Frame);
        BindClick((RectTransform)damagePanel.transform,()=>{});
        PanelHeader((RectTransform)damagePanel.transform,26);
        if(worldMapUI!=null) {BindClick(worldMapUI.Root,()=>{});Skin(worldMapUI.Root,SurfaceKind.Frame);PanelHeader(worldMapUI.Root,42);}
        if(devUI!=null) {BindClick(devUI.Root,()=>{});Skin(devUI.Root,SurfaceKind.Frame);PanelHeader(devUI.Root,26);}
        if(hudGame!=null && hudGame.eventLog!=null)
        {
            var log=(RectTransform)hudGame.eventLog.transform;
            Skin(log,SurfaceKind.Frame);
            // The W3 view now owns the distance strip independently of log visibility.
            var legacy=log.Find("Exploration Progress");if(legacy!=null)legacy.gameObject.SetActive(false);
        }
        foreach(var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(rect.gameObject.name=="MenuArea" || rect.gameObject.name=="MapIcon")rect.gameObject.SetActive(false);
        StartCoroutine(FinishHudBinding());
    }
    private IEnumerator FinishHudBinding()
    {
        // Other scene Start methods may finish constructing their opening UI.
        // One initialization pass, never a scene scan in Update/LateUpdate.
        yield return null;
        foreach(var b in hudCanvas.GetComponentsInChildren<Button>(true))
        {
            if(!b.enabled)continue;
            var button=b;var item=b.GetComponent<CustomInteractable>();
            if(item==null) {item=b.gameObject.AddComponent<CustomInteractable>();item.onClickEvent=new UnityEngine.Events.UnityEvent();item.onClickEvent.AddListener(()=>{if(button.interactable)button.onClick.Invoke();});}
            RegisterInteractable(item);if(transparentWindow!=null)transparentWindow.RegisterClickable(item.GetRect());b.enabled=false;
        }
        LayoutReadableUI();
    }
    private void LayoutHudW3(float w,float h)
    {
        if(w3==null)return;
        if(hudLayout==null)hudLayout=new HudW3Layout();
        appliedHudLayout=hudLayout.Revision;
        w3.Layout(w,h,hudLayout);
        float m=Mathf.Max(4,hudLayout.margin),gap=Mathf.Max(2,hudLayout.gap);
        float cardsBottom=m+Mathf.Max(hudLayout.heroCardHeight,HudW3View.PrimaryCardHeight)+gap;
        float top=Mathf.Max(hudLayout.systemTop,cardsBottom);
        var buttons=new[]{statsButton,inventoryButton,martialButton,menuButton};
        for(int i=0;i<buttons.Length;i++)
            HudW3View.Place(buttons[i],w-m-hudLayout.systemButtonWidth,top+i*(hudLayout.systemButtonHeight+gap+2),hudLayout.systemButtonWidth,hudLayout.systemButtonHeight);
        HudW3View.Place(pauseRect,m,m,76,26);HudW3View.Place(logToggle,m+80,m,76,26);
        foreach(var r in buttons)LayoutHudButton(r,hudLayout.textSize);
        LayoutHudButton(pauseRect,12);LayoutHudButton(logToggle,12);
        if(hudGame!=null && hudGame.eventLog!=null)
        {
            float x=m+hudLayout.mapWidth+gap;
            var log=(RectTransform)hudGame.eventLog.transform;
            HudW3View.Place(log,x,cardsBottom,Mathf.Max(120,w-x-hudLayout.systemButtonWidth-2*m-gap),hudLayout.logHeight);
            if(hudGame.eventLogText!=null)
            {
                var t=hudGame.eventLogText;
                t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;
                t.rectTransform.offsetMin=new Vector2(6,3);t.rectTransform.offsetMax=new Vector2(-52,-3);
                ReadableText(t,12);t.enableAutoSizing=false;
            }
            var info=log.Find("DamageInfoButton") as RectTransform;
            if(info!=null) {HudW3View.Place(info,log.sizeDelta.x-48,4,44,22);LayoutHudButton(info,11);}
        }
    }
    private static void LayoutHudButton(RectTransform r,float font)
    {
        var t=r.GetComponentInChildren<TextMeshProUGUI>(true);if(t==null)return;
        t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;
        t.rectTransform.offsetMin=new Vector2(4,2);t.rectTransform.offsetMax=new Vector2(-4,-2);
        t.fontSize=font;t.enableAutoSizing=false;t.enableWordWrapping=false;t.overflowMode=TextOverflowModes.Ellipsis;
    }
    private void RefreshWorldMapAvailability()
    {
        if(worldMapButton==null)return;
        bool available=worldMapUI!=null && hudGame!=null && TuTienCore.WorldMapCatalog.Enabled(hudGame.HeroData);
        if(available==worldMapButtonAvailable)return;
        worldMapButtonAvailable=available;
        worldMapButton.GetComponent<CustomInteractable>().enabled=available;
        themeDirty=true;
    }
    private void RefreshB2PauseFeedback()
    {
        if(testSummonButton==null || testSummonText==null || hudGame==null)return;
        bool paused=Time.timeScale==0f;
        int count=hudGame.TestCompanionCount;
        if(paused==b2ControlPaused && count==b2DisplayedCount)return;
        b2ControlPaused=paused;b2DisplayedCount=count;
        testSummonButton.GetComponent<CustomInteractable>().enabled=!paused;
        testSummonText.text=paused?"[B2] Tiếp tục để sử dụng":
            count>=GameManager.TestCompanionLimit?"[B2] Thu hồi (2/2)":"[B2] Gọi +1 ("+count+"/2)";
        themeDirty=true;
    }
    private void TickHudW3()
    {
        RefreshWorldMapAvailability();
        RefreshB2PauseFeedback();
        RefreshHudTheme();
        if(w3!=null)w3.Render(hudGame,CombatManager.Instance,inventoryUseArtTheme?inventoryArtTheme:null);
        if(logToggleLabel!=null)
        {
            string value=showCompactLog?"Ẩn nhật ký":"Nhật ký";
            if(logToggleLabel.text!=value)logToggleLabel.text=value;
        }
    }
}
