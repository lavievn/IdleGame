using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Opening buttons remain wired to GameManager's existing confirmed
// Continue/New Game/Save flow. The animated scenery is a separate sibling
// so gameplay can appear beneath it during the entry transition.
public class StartMenuUI : MonoBehaviour
{
    private GameManager manager;
    private Button continueButton,confirmChoiceButton,cancelChoiceButton;
    private Button[] menuButtons;
    private GameObject menuPage, settingsPage;
    private TextMeshProUGUI message, sizeLabel, musicLabel, volumeValueLabel, logoFallback;
    private RectTransform logoRoot;
    private Image logoImage;
    private Button[] sizeButtons = new Button[3];
    private RectTransform volumeTrack, volumeFill, volumeKnob;
    private AudioVolumeBar bgmVolumeBar,sfxVolumeBar;
    private TextMeshProUGUI sfxLabel,sfxValueLabel;
    private bool volumeDragging;
    private int selectedWindowWidth;
    private CanvasGroup rootFade;
    private OpeningCinematicUI cinematic;
    private RectTransform root;
    private int lastClickFrame = -1;
    private bool entryLocked;
    private bool openingMenuReady;
    private bool editorForceLayout;
    private static readonly Color TitleInkColor = new Color(74f/255f,18f/255f,20f/255f,1f);

    public bool IsCinematic => cinematic != null;
    public bool IsEnteringCinematic => cinematic != null && cinematic.IsEntering;

    public static StartMenuUI Install(GameManager gm)
    {
        if (gm == null || gm.preGameUI == null) return null;
        var template = gm.preGameUI.GetComponentInChildren<Button>(true);
        var textTemplate = gm.preGameUI.GetComponentInChildren<TextMeshProUGUI>(true);
        if (template == null || textTemplate == null) return null;

        var menu = gm.gameObject.AddComponent<StartMenuUI>();
        menu.manager = gm;
        foreach (Transform child in gm.preGameUI.transform) child.gameObject.SetActive(false);
        menu.root = gm.preGameUI.GetComponent<RectTransform>();
        menu.root.SetAsLastSibling();
        var panel = gm.preGameUI.GetComponent<Image>();
        if (panel != null) { panel.color = new Color(0,0,0,0); panel.raycastTarget = false; }
        menu.rootFade = gm.preGameUI.GetComponent<CanvasGroup>();
        if (menu.rootFade == null) menu.rootFade = gm.preGameUI.AddComponent<CanvasGroup>();
        menu.rootFade.alpha = 0f;

        menu.menuPage = new GameObject("Opening Main Buttons",typeof(RectTransform));
        menu.menuPage.transform.SetParent(menu.root,false);
        menu.settingsPage = new GameObject("Opening Settings Buttons",typeof(RectTransform));
        menu.settingsPage.transform.SetParent(menu.root,false);
        var menuRoot=(RectTransform)menu.menuPage.transform;
        var settingsRoot=(RectTransform)menu.settingsPage.transform;
        menu.menuButtons = new Button[4];

        // GameManager still writes menu status to its old disabled infoText.
        // The actual game logo is independent, so status updates can never
        // overwrite "Bách Luyện Tiên Đồ" after returning from gameplay.
        menu.CreateGameLogo(textTemplate,menuRoot);
        menu.menuButtons[0] = menu.MakeButton(template,menuRoot,"Tiếp tục",
            new Vector2(0,46),gm.OnContinueClicked);
        menu.continueButton=menu.menuButtons[0];
        menu.menuButtons[1] = menu.MakeButton(template,menuRoot,"Chơi mới",
            new Vector2(0,2),gm.OnNewGameClicked);
        menu.menuButtons[2] = menu.MakeButton(template,menuRoot,"Cài đặt",
            new Vector2(0,-42),menu.OpenSettings);
        menu.menuButtons[3] = menu.MakeButton(template,menuRoot,"Thoát",
            new Vector2(0,-86),menu.ExitGame);

        menu.sizeLabel=menu.Label(textTemplate,settingsRoot,"Kích thước màn hình",
            new Vector2(-160,44),new Vector2(170,32),15);
        int[] widths={600,800,1150};
        for(int i=0;i<widths.Length;i++)
        {
            int width=widths[i];
            menu.sizeButtons[i]=menu.MakeButton(template,settingsRoot,
                width.ToString(),new Vector2(-44+i*83,44),()=>menu.SelectWindowSize(width));
        }
        menu.musicLabel=menu.Label(textTemplate,settingsRoot,"Nhạc nền (BGM)",
            new Vector2(-160,-12),new Vector2(170,32),15);
        menu.sfxLabel=menu.Label(textTemplate,settingsRoot,"Hiệu ứng (SFX)",
            new Vector2(-160,-60),new Vector2(170,32),15);
        menu.BuildVolumeBar(textTemplate,settingsRoot);
        menu.MakeButton(template,settingsRoot,"Khôi phục mặc định",
            new Vector2(0,-86),menu.RestoreDefaultSettings);
        menu.MakeButton(template,settingsRoot,"Quay lại",
            new Vector2(0,-122),menu.CloseSettings);
        menu.settingsPage.SetActive(false);

        if (gm.confirmationPopup != null) gm.confirmationPopup.SetActive(false);
        var popup = new GameObject("Start Choice Confirmation",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var popRect=(RectTransform)popup.transform;
        popRect.SetParent(menu.root.parent,false);
        popRect.anchorMin=Vector2.zero; popRect.anchorMax=Vector2.one;
        popRect.offsetMin=Vector2.zero; popRect.offsetMax=Vector2.zero;
        popRect.sizeDelta=Vector2.zero;popRect.anchoredPosition=Vector2.zero;
        popup.GetComponent<Image>().color=new Color(.04f,.07f,.12f,.91f);
        popup.GetComponent<Image>().raycastTarget = false;
        menu.message=menu.Label(textTemplate,popRect,"",
            new Vector2(0,55),new Vector2(410,145),21);
        menu.confirmChoiceButton=menu.MakeButton(template,popRect,"Xác nhận",new Vector2(-110,-66),gm.OnConfirmResetClicked,true);
        menu.cancelChoiceButton=menu.MakeButton(template,popRect,"Hủy",new Vector2(110,-66),gm.OnCancelResetClicked,true);
        gm.confirmationPopup=popup;
        popup.SetActive(false);

        var window=Object.FindFirstObjectByType<TransparentWindow>();
        if(window != null)
        {
            var list=new System.Collections.Generic.List<RectTransform>(window.modalUI ?? new RectTransform[0]);
            list.Add(popRect);
            list.Add(menu.root);
            window.modalUI=list.ToArray();
        }
        menu.cinematic=OpeningCinematicUI.Install(gm,menu,menu.root,menu.rootFade,textTemplate);
        menu.Layout(ManagementWindowLayout.CombatPixels(Screen.width),
            ManagementWindowLayout.CombatHeight(Screen.height));
        menu.Refresh();
        return menu;
    }

    private void CreateGameLogo(TextMeshProUGUI template,Transform parent)
    {
        var holder=new GameObject("Game Title - Bách Luyện Tiên Đồ",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        logoRoot=(RectTransform)holder.transform;
        logoRoot.SetParent(parent,false);
        logoImage=holder.GetComponent<Image>();
        logoImage.raycastTarget=false;
        logoImage.preserveAspect=true;
        Sprite sprite=Resources.Load<Sprite>("OpeningTitleLogo");
        var theme=Resources.Load<OpeningArtTheme>("OpeningArtTheme");
        if(sprite==null && theme!=null)sprite=theme.gameTitle;
        logoImage.sprite=sprite;
        logoImage.enabled=sprite!=null;
        // OpeningTitleLogo.png now contains WHITE RGB + original brush alpha.
        // Tinting white preserves the brush texture and produces deep red ink.
        logoImage.color=TitleInkColor;
        logoFallback=Label(template,logoRoot,"Bách Luyện Tiên Đồ",
            Vector2.zero,new Vector2(500,75),62f);
        if(logoFallback!=null)
        {
            logoFallback.gameObject.SetActive(sprite==null);
            logoFallback.color=TitleInkColor;
        }
    }

    private static void Place(RectTransform r,Vector2 p,Vector2 size)
    {
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=p;r.sizeDelta=size;r.localScale=Vector3.one;
    }
    private TextMeshProUGUI Label(TextMeshProUGUI template,Transform parent,
        string title,Vector2 p,Vector2 size,float fontSize)
    {
        var go=Instantiate(template.gameObject,parent);go.SetActive(true);
        Place(go.GetComponent<RectTransform>(),p,size);
        var label=go.GetComponent<TextMeshProUGUI>();
        label.text=title;label.color=new Color(1,1,1,1);
        label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false;
        UIManager.ReadableText(label,fontSize);
        label.margin=new Vector4(0,0,0,0);
        return label;
    }
    private Button MakeButton(Button template,Transform parent,string title,Vector2 p,UnityAction action,
        bool confirmation=false)
    {
        var go=Instantiate(template.gameObject,parent);go.SetActive(true);
        go.name="Opening - "+title;
        Place(go.GetComponent<RectTransform>(),p,new Vector2(184,36));
        var button=go.GetComponent<Button>();
        var text=go.GetComponentInChildren<TextMeshProUGUI>(true);
        if(text!=null)
        {
            text.text=title;
            FitButtonText(button);
        }
        var background=go.GetComponent<Image>();
        if(background != null) { background.color=new Color(.06f,.18f,.24f,.94f);background.raycastTarget=false; }
        UnityAction clicked=()=>{
            if(!go.activeInHierarchy || !button.interactable || lastClickFrame==Time.frameCount)return;
            // The confirmation is a sibling of the opening, also used during
            // gameplay. Its controls must not inherit the cinematic entry lock.
            if(confirmation)
            {
                if(manager==null || !manager.HasPendingConfirmation ||
                   manager.confirmationPopup==null || !manager.confirmationPopup.activeInHierarchy)return;
            }
            else if(!openingMenuReady || entryLocked)return;
            lastClickFrame=Time.frameCount;
            action();
        };
        button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(clicked);
        var custom=go.GetComponent<CustomInteractable>();
        if(custom==null)custom=go.AddComponent<CustomInteractable>();
        custom.onClickEvent=new UnityEvent();custom.onClickEvent.AddListener(clicked);
        // Native Windows clicks use CustomInteractable, not EventSystem raycasts.
        // Bind immediately, including when the popup was built inactive.
        if(UIManager.Instance!=null)UIManager.Instance.RegisterInteractable(custom);
        var window=Object.FindFirstObjectByType<TransparentWindow>();
        if(window!=null)window.RegisterClickable(go.GetComponent<RectTransform>());
        return button;
    }

    // Only StartMenuUI owns these labels, including the sibling confirmation
    // used from gameplay. HUD-wide typography must not overwrite this layout.
    public bool OwnsText(TextMeshProUGUI text)
    {
        return text!=null && ((root!=null && text.transform.IsChildOf(root)) ||
            (manager!=null && manager.confirmationPopup!=null &&
             text.transform.IsChildOf(manager.confirmationPopup.transform)));
    }
    private static void FitButtonText(Button button)
    {
        if(button==null)return;
        var rect=(RectTransform)button.transform;
        foreach(var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            var label=(RectTransform)text.transform;
            label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;
            label.pivot=new Vector2(.5f,.5f);label.anchoredPosition=Vector2.zero;
            label.sizeDelta=new Vector2(-16f,-8f);label.localScale=Vector3.one;
            float font=Mathf.Clamp(rect.rect.height*.46f,14f,20f);
            UIManager.ReadableText(text,font);
            text.enableAutoSizing=true;text.fontSizeMin=14f;text.fontSizeMax=font;
            text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Overflow;
            text.margin=new Vector4(0,0,0,0);
            text.alignment=TextAlignmentOptions.Center;text.color=Color.white;
            text.raycastTarget=false;
        }
    }
    private void LayoutConfirmation(float w,float h)
    {
        if(manager==null || manager.confirmationPopup==null)return;
        var popup=(RectTransform)manager.confirmationPopup.transform;
        popup.anchorMin=Vector2.zero;popup.anchorMax=Vector2.one;
        popup.offsetMin=popup.offsetMax=Vector2.zero;
        popup.sizeDelta=Vector2.zero;popup.anchoredPosition=Vector2.zero;
        var buttons=popup.GetComponentsInChildren<Button>(true);
        float width=Mathf.Min(200f,(w-48f)/2f);
        for(int i=0;i<buttons.Length;i++)
        {
            Place((RectTransform)buttons[i].transform,new Vector2((i%2==0?-1:1)*(width*.5f+8f),-h*.3f),new Vector2(width,36f));
            FitButtonText(buttons[i]);
        }
        if(message!=null)
        {
            Place((RectTransform)message.transform,new Vector2(0,h*.10f),new Vector2(Mathf.Min(440f,w-40f),h*.56f));
            UIManager.ReadableText(message,18f);
            message.enableAutoSizing=true;message.fontSizeMin=16f;message.fontSizeMax=18f;
            message.margin=new Vector4(4,4,4,4);message.overflowMode=TextOverflowModes.Overflow;
        }
    }

    private void FitMainButtonGroup(float w,float h)
    {
        // Saved centers are preferred anchors. Only repair an unreadably small
        // or overlapping group, never write the theme or touch scenery/logo.
        var rects=new RectTransform[menuButtons.Length];
        float center=0f;bool repair=false;
        float minWidth=Mathf.Clamp(w*.24f,152f,208f);
        float minHeight=Mathf.Clamp(h*.08f,34f,42f);
        const float gap=6f;
        for(int i=0;i<rects.Length;i++)
        {
            var r=rects[i]=(RectTransform)menuButtons[i].transform;center+=r.anchoredPosition.y;
            var size=new Vector2(Mathf.Clamp(r.rect.width,minWidth,w-24f),Mathf.Max(r.rect.height,minHeight));
            if(size.x!=r.rect.width || size.y!=r.rect.height)repair=true;
            r.sizeDelta=size;
        }
        center/=rects.Length;
        // Preserve the saved vertical order (stable for coincident centers).
        for(int i=1;i<rects.Length;i++)
        {
            var item=rects[i];int j=i-1;
            while(j>=0 && rects[j].anchoredPosition.y<item.anchoredPosition.y)
            { rects[j+1]=rects[j];j--; }
            rects[j+1]=item;
        }
        for(int i=1;i<rects.Length;i++)
            if(rects[i-1].anchoredPosition.y-rects[i-1].rect.height*.5f-
               (rects[i].anchoredPosition.y+rects[i].rect.height*.5f)<gap)repair=true;
        if(!repair)return;
        var offsets=new float[rects.Length];float mean=0f;
        for(int i=1;i<rects.Length;i++)
        {
            offsets[i]=offsets[i-1]-(rects[i-1].rect.height+rects[i].rect.height)*.5f-gap;
            mean+=offsets[i];
        }
        mean/=rects.Length;
        float top=offsets[0]-mean+rects[0].rect.height*.5f;
        float bottom=offsets[rects.Length-1]-mean-rects[rects.Length-1].rect.height*.5f;
        float upper=h*.5f-8f;
        if(logoRoot!=null)
            upper=Mathf.Min(upper,logoRoot.anchoredPosition.y-logoRoot.rect.height*.5f-gap);
        // Keep the preferred group center whenever there is room. At 600px,
        // the authored tiny group would otherwise grow into the fixed logo.
        float lower=-h*.5f+8f-bottom;
        center=Mathf.Max(lower,Mathf.Min(center,upper-top));
        for(int i=0;i<rects.Length;i++)
        {
            var p=rects[i].anchoredPosition;
            p.x=Mathf.Clamp(p.x,-w*.5f+rects[i].rect.width*.5f+8f,w*.5f-rects[i].rect.width*.5f-8f);
            p.y=center+offsets[i]-mean;rects[i].anchoredPosition=p;
        }
    }

    // Stable order shared with OpeningCinematicUI.LayoutKeys.
    // These are real runtime RectTransforms: editable in Unity Play Mode.
    public RectTransform[] GetOpeningLayoutRects()
    {
        return new RectTransform[]{
            logoRoot,
            menuButtons!=null&&menuButtons.Length>0&&menuButtons[0]!=null
                ? (RectTransform)menuButtons[0].transform:null,
            menuButtons!=null&&menuButtons.Length>1&&menuButtons[1]!=null
                ? (RectTransform)menuButtons[1].transform:null,
            menuButtons!=null&&menuButtons.Length>2&&menuButtons[2]!=null
                ? (RectTransform)menuButtons[2].transform:null,
            menuButtons!=null&&menuButtons.Length>3&&menuButtons[3]!=null
                ? (RectTransform)menuButtons[3].transform:null
        };
    }

    public void PrepareLayoutEditing()
    {
        if(settingsPage!=null)settingsPage.SetActive(false);
        if(menuPage!=null)menuPage.SetActive(true);
        SetOpeningMenuReady(false);
    }

    public void ForceLayoutForEditor()
    {
        editorForceLayout=true;
        try
        {
            Layout(ManagementWindowLayout.CombatPixels(Screen.width),
                ManagementWindowLayout.CombatHeight(Screen.height));
        }
        finally { editorForceLayout=false; }
    }

    public void Layout(float w,float h)
    {
        if(root==null)return;
        LayoutConfirmation(w,h);
        // While dragging in Scene View, UIManager resize/layout passes must
        // not snap the live controls back to their authored positions.
        if(cinematic!=null&&cinematic.IsLayoutEditing&&!editorForceLayout)return;
        w=Mathf.Max(320f,w);h=Mathf.Max(240f,h);
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.zero;
        root.pivot=Vector2.zero;root.anchoredPosition=Vector2.zero;
        root.sizeDelta=new Vector2(w,h);
        foreach(var page in new[]{menuPage,settingsPage})
        {
            if(page==null)continue;
            var r=(RectTransform)page.transform;
            r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;
            r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
            r.sizeDelta=Vector2.zero; r.anchoredPosition=Vector2.zero;
        }
        float spacing=Mathf.Clamp(h*.11f,32f,46f);
        float bw=Mathf.Clamp(w*.31f,140f,208f);
        float bh=Mathf.Clamp(h*.092f,34f,42f);
        for(int i=0;i<4;i++)
        {
            // Move the buttons down slightly to make room for a 2x title.
            Place((RectTransform)menuButtons[i].transform,
                new Vector2(0,1.5f*spacing-i*spacing-29f),new Vector2(bw,bh));
        }
        if(logoRoot!=null)
        {
            // Preserve sprite aspect. The new 2x height provides almost twice
            // the painted width without spilling past the viewport sides.
            float logoW=Mathf.Min(w*.96f,1120f);
            float logoH=Mathf.Clamp(h*.34f,80f,230f);
            float topMargin=Mathf.Max(4f,h*.01f);
            Place(logoRoot,new Vector2(0,h*.5f-logoH*.5f-topMargin),
                new Vector2(logoW,logoH));
            if(logoFallback!=null)
                Place((RectTransform)logoFallback.transform,Vector2.zero,
                    new Vector2(logoW,logoH));
        }
        // The saved Play Mode positions override code defaults only when
        // the theme contains a snapshot. Existing scenes need no migration.
        var titleTheme=Resources.Load<OpeningArtTheme>("OpeningArtTheme");
        var editable=GetOpeningLayoutRects();
        for(int i=0;i<editable.Length;i++)
            OpeningCinematicUI.ApplySavedRect(titleTheme,
                OpeningCinematicUI.LayoutKeys[i+8],editable[i],w,h);
        FitMainButtonGroup(w,h);
        if(logoFallback!=null && logoRoot!=null)
        {
            // The fallback title follows its own saved logo box as well.
            var size=logoRoot.rect.size;
            Place((RectTransform)logoFallback.transform,Vector2.zero,size);
            UIManager.ReadableText(logoFallback,Mathf.Clamp(size.y*.65f,22f,62f));
            logoFallback.enableAutoSizing=true;logoFallback.fontSizeMin=20f;
            logoFallback.fontSizeMax=logoFallback.fontSize;
            logoFallback.enableWordWrapping=false;logoFallback.margin=new Vector4(0,0,0,0);
        }

        // Settings use a centered two-column grid, not the main menu's vertical list.
        // The bar is a custom hit target to work in native Windows overlay mode.
        float row1=Mathf.Clamp(h*.2f,60f,76f);
        float row2=row1-48f,row3=row2-48f;
        float labelX=-Mathf.Min(w*.265f,155f);
        float labelWidth=Mathf.Min(w*.32f,177f);
        Place((RectTransform)sizeLabel.transform,new Vector2(labelX,row1),new Vector2(labelWidth,30));
        Place((RectTransform)musicLabel.transform,new Vector2(labelX,row2),new Vector2(labelWidth,30));
        Place((RectTransform)sfxLabel.transform,new Vector2(labelX,row3),new Vector2(labelWidth,30));
        float choiceWidth=Mathf.Clamp(w*.12f,52f,76f);
        float gap=Mathf.Clamp(w*.01f,4f,9f);
        // Reserve the left column for the row label; radio boxes start at
        // the screen center to avoid clipping/overlap at 600px and below.
        float firstX=0f;
        for(int i=0;i<3;i++)
            Place((RectTransform)sizeButtons[i].transform,
                new Vector2(firstX+i*(choiceWidth+gap),row1),
                new Vector2(choiceWidth,30f));
        float barWidth=Mathf.Clamp(w*.33f,125f,200f);
        Place(volumeTrack,new Vector2(42f,row2),new Vector2(barWidth,28f));
        Place(volumeFill,Vector2.zero,new Vector2(barWidth*MapBgmController.CurrentVolume,12f));
        Place(volumeKnob,new Vector2(barWidth*(MapBgmController.CurrentVolume-.5f),0f),new Vector2(12f,24f));
        Place((RectTransform)volumeValueLabel.transform,new Vector2(42f+barWidth*.5f+27f,row2),new Vector2(60f,30f));
        Place(sfxVolumeBar.Track,new Vector2(42f,row3),new Vector2(barWidth,28f));
        Place((RectTransform)sfxValueLabel.transform,new Vector2(42f+barWidth*.5f+27f,row3),new Vector2(60f,30f));
        bgmVolumeBar.Refresh(true);sfxVolumeBar.Refresh(true);
        var settingsButtons=settingsPage.GetComponentsInChildren<Button>(true);
        // First three are radio choices, last two are the bottom actions.
        int actionIndex=0;
        for(int i=0;i<settingsButtons.Length;i++)
        {
            bool isRadio=false;
            foreach(var b in sizeButtons)if(settingsButtons[i]==b)isRadio=true;
            if(isRadio)continue;
            float actionY=row3-48f-actionIndex*46f;
            Place((RectTransform)settingsButtons[i].transform,
                new Vector2(0,actionY),new Vector2(Mathf.Min(210,w*.48f),bh));
            actionIndex++;
        }
        UpdateSettingVisuals();
        foreach(var page in new[]{menuPage,settingsPage})
            foreach(var button in page.GetComponentsInChildren<Button>(true))FitButtonText(button);
        // Only the menu is moved on resize. Art layer sizes are independent.
        if(cinematic!=null)cinematic.Layout();
    }

    private void OpenSettings()
    {
        menuPage.SetActive(false);
        settingsPage.SetActive(true);
        selectedWindowWidth=ManagementWindowLayout.CompactWidth>0
            ? ManagementWindowLayout.CompactWidth : Screen.width;
        UpdateSettingVisuals();
    }
    private void CloseSettings()
    {
        settingsPage.SetActive(false);
        menuPage.SetActive(true);
        EndVolumeDrag();
        Refresh();
    }
    private void BuildVolumeBar(TextMeshProUGUI template,Transform parent)
    {
        volumeValueLabel=Label(template,parent,"60%",new Vector2(0,-12),
            new Vector2(55,30),15);
        sfxValueLabel=Label(template,parent,"100%",new Vector2(0,-60),new Vector2(55,30),15);
        bgmVolumeBar=new AudioVolumeBar("Opening Volume Bar",parent,GameAudioChannel.Bgm,volumeValueLabel);
        sfxVolumeBar=new AudioVolumeBar("Opening SFX Volume Bar",parent,GameAudioChannel.Sfx,sfxValueLabel);
        // Preserve the existing BGM control references/API for native routing.
        volumeTrack=bgmVolumeBar.Track;volumeFill=bgmVolumeBar.Fill;volumeKnob=bgmVolumeBar.Knob;
        // Do not create a built-in uGUI Slider: native overlay mouse dragging
        // is forwarded through UIManager and TransparentWindow.
    }

    public bool TryBeginVolumeDrag(Vector2 screenPoint)
    {
        if(entryLocked || settingsPage==null || !settingsPage.activeInHierarchy ||
            manager==null || manager.HasPendingConfirmation || volumeTrack==null)
            return false;
        EndVolumeDrag();
        volumeDragging=bgmVolumeBar.TryBeginDrag(screenPoint)||sfxVolumeBar.TryBeginDrag(screenPoint);
        return volumeDragging;
    }

    public void DragVolume(Vector2 screenPoint)
    {
        if(!volumeDragging || volumeTrack==null || !settingsPage.activeInHierarchy)return;
        if(entryLocked || manager==null || manager.HasPendingConfirmation) {EndVolumeDrag();return;}
        bgmVolumeBar.Drag(screenPoint);sfxVolumeBar.Drag(screenPoint);
    }

    public void EndVolumeDrag()
    {
        if(bgmVolumeBar!=null)bgmVolumeBar.EndDrag();
        if(sfxVolumeBar!=null)sfxVolumeBar.EndDrag();
        volumeDragging=false;
    }

    private void SelectWindowSize(int width)
    {
        selectedWindowWidth=width;
        var ui=UIManager.Instance;
        if(ui!=null)ui.ApplyWindowSizeFromOpening(width);
        else
        {
            int height=Mathf.RoundToInt(width*9f/16f);
            ManagementWindowLayout.RequestCompact(width,height);
            Screen.SetResolution(width,height,FullScreenMode.Windowed);
        }
        UpdateSettingVisuals();
    }

    private void RestoreDefaultSettings()
    {
        EndVolumeDrag();AudioVolumeSettings.RestoreDefaults();
        SelectWindowSize(800);
        UpdateSettingVisuals();
    }

    private void UpdateSettingVisuals()
    {
        int[] widths={600,800,1150};
        for(int i=0;i<sizeButtons.Length;i++)
        {
            if(sizeButtons[i]==null)continue;
            var bg=sizeButtons[i].GetComponent<Image>();
            bool selected=selectedWindowWidth==widths[i];
            if(bg!=null)bg.color=selected
                ? new Color(.34f,.59f,.54f,.98f)
                : new Color(.06f,.18f,.24f,.94f);
            var label=sizeButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if(label!=null)label.text=(selected?"● ":"○ ")+widths[i];
        }
        bgmVolumeBar.Refresh(true);sfxVolumeBar.Refresh(true);
    }
    private void Update()
    {
        if(settingsPage!=null && settingsPage.activeInHierarchy)
        {bgmVolumeBar.Refresh();sfxVolumeBar.Refresh();}
    }
    private void ExitGame(){Application.Quit();}
    public void SetOpeningMenuReady(bool ready)
    {
        if(openingMenuReady==ready && rootFade!=null &&
            rootFade.interactable==ready && rootFade.blocksRaycasts==ready)return;
        openingMenuReady=ready;
        if(rootFade!=null)
        {
            rootFade.interactable=ready;
            rootFade.blocksRaycasts=ready;
        }
        Refresh();
    }

    public void Refresh()
    {
        if(continueButton!=null)
            continueButton.interactable=!entryLocked&&openingMenuReady&&manager.CanContinue;
    }
    public void SetConfirmationLabels(string confirm,string cancel)
    {
        if(confirmChoiceButton!=null)confirmChoiceButton.GetComponentInChildren<TextMeshProUGUI>(true).text=confirm;
        if(cancelChoiceButton!=null)cancelChoiceButton.GetComponentInChildren<TextMeshProUGUI>(true).text=cancel;
    }
    public void ShowConfirmation(string text)
    {
        SetConfirmationLabels("Xác nhận","Hủy");
        message.text=text;
        manager.confirmationPopup.transform.SetAsLastSibling();
        manager.confirmationPopup.SetActive(true);
    }

    public void RestoreMenu()
    {
        entryLocked = false;
        lastClickFrame = -1;
        settingsPage.SetActive(false);
        menuPage.SetActive(true);
        EndVolumeDrag();
        foreach (var b in menuButtons) if (b != null) b.interactable = true;
        if (cinematic != null) cinematic.ResetForMenu();
        SetOpeningMenuReady(false);
        Refresh();
    }

    public void LockButtonsForEntry()
    {
        entryLocked=true;
        SetOpeningMenuReady(false);
        foreach(var b in menuButtons)if(b!=null)b.interactable=false;
        settingsPage.SetActive(false);
    }
    public bool BeginGameplayTransition()
    {
        if(cinematic==null || cinematic.IsEntering)return false;
        cinematic.BeginEntering();
        return cinematic.IsEntering;
    }

    public void EnterGameplayImmediately()
    {
        LockButtonsForEntry();
        if(cinematic!=null)cinematic.CompleteGameplayTransition();
    }
}
