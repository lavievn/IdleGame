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
    private Button continueButton;
    private Button[] menuButtons;
    private GameObject menuPage, settingsPage;
    private TextMeshProUGUI message, sizeLabel, musicLabel, volumeValueLabel;
    private Button[] sizeButtons = new Button[3];
    private RectTransform volumeTrack, volumeFill, volumeKnob;
    private bool volumeDragging;
    private int selectedWindowWidth;
    private CanvasGroup rootFade;
    private OpeningCinematicUI cinematic;
    private RectTransform root;
    private int lastClickFrame = -1;
    private bool entryLocked;

    public bool IsCinematic => cinematic != null;

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

        gm.infoText = menu.Label(textTemplate, menuRoot,
            "MỘT HÀNH TRÌNH TU TIÊN", new Vector2(0,130), new Vector2(440,38),18);
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
        menu.musicLabel=menu.Label(textTemplate,settingsRoot,"Âm thanh",
            new Vector2(-160,-12),new Vector2(170,32),15);
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
        popup.GetComponent<Image>().color=new Color(.04f,.07f,.12f,.91f);
        popup.GetComponent<Image>().raycastTarget = false;
        menu.message=menu.Label(textTemplate,popRect,"",
            new Vector2(0,55),new Vector2(410,145),21);
        menu.MakeButton(template,popRect,"Có",new Vector2(-110,-66),gm.OnConfirmResetClicked);
        menu.MakeButton(template,popRect,"Không",new Vector2(110,-66),gm.OnCancelResetClicked);
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
        return label;
    }
    private Button MakeButton(Button template,Transform parent,string title,Vector2 p,UnityAction action)
    {
        var go=Instantiate(template.gameObject,parent);go.SetActive(true);
        go.name="Opening - "+title;
        Place(go.GetComponent<RectTransform>(),p,new Vector2(184,36));
        var button=go.GetComponent<Button>();
        var text=go.GetComponentInChildren<TextMeshProUGUI>(true);
        if(text!=null){text.text=title;UIManager.ReadableText(text,16);text.alignment=TextAlignmentOptions.Center; }
        var background=go.GetComponent<Image>();
        if(background != null) { background.color=new Color(.06f,.18f,.24f,.94f);background.raycastTarget=false; }
        UnityAction clicked=()=>{
            if(!button.interactable || entryLocked || lastClickFrame==Time.frameCount)return;
            lastClickFrame=Time.frameCount;
            action();
        };
        button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(clicked);
        var custom=go.GetComponent<CustomInteractable>();
        if(custom==null)custom=go.AddComponent<CustomInteractable>();
        custom.onClickEvent=new UnityEvent();custom.onClickEvent.AddListener(clicked);
        return button;
    }

    public void Layout(float w,float h)
    {
        if(root==null)return;
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
        float bh=Mathf.Clamp(h*.092f,26f,38f);
        for(int i=0;i<4;i++)
        {
            Place((RectTransform)menuButtons[i].transform,
                new Vector2(0,1.5f*spacing-i*spacing-7f),new Vector2(bw,bh));
        }
        // Settings use a centered two-column grid, not the main menu's vertical list.
        // The bar is a custom hit target to work in native Windows overlay mode.
        float row1=Mathf.Clamp(h*.155f,35f,56f);
        float row2=row1-56f;
        float labelX=-Mathf.Min(w*.265f,155f);
        float labelWidth=Mathf.Min(w*.32f,177f);
        Place((RectTransform)sizeLabel.transform,new Vector2(labelX,row1),new Vector2(labelWidth,30));
        Place((RectTransform)musicLabel.transform,new Vector2(labelX,row2),new Vector2(labelWidth,30));
        float choiceWidth=Mathf.Clamp(w*.12f,52f,76f);
        float gap=Mathf.Clamp(w*.01f,4f,9f);
        float firstX=-choiceWidth-gap;
        for(int i=0;i<3;i++)
            Place((RectTransform)sizeButtons[i].transform,
                new Vector2(firstX+i*(choiceWidth+gap),row1),
                new Vector2(choiceWidth,30f));
        float barWidth=Mathf.Clamp(w*.33f,125f,200f);
        Place(volumeTrack,new Vector2(42f,row2),new Vector2(barWidth,28f));
        Place(volumeFill,Vector2.zero,new Vector2(barWidth*MapBgmController.CurrentVolume,12f));
        Place(volumeKnob,new Vector2(barWidth*(MapBgmController.CurrentVolume-.5f),0f),new Vector2(12f,24f));
        Place((RectTransform)volumeValueLabel.transform,new Vector2(42f+barWidth*.5f+27f,row2),new Vector2(60f,30f));
        var settingsButtons=settingsPage.GetComponentsInChildren<Button>(true);
        // First three are radio choices, last two are the bottom actions.
        int actionIndex=0;
        for(int i=0;i<settingsButtons.Length;i++)
        {
            bool isRadio=false;
            foreach(var b in sizeButtons)if(settingsButtons[i]==b)isRadio=true;
            if(isRadio)continue;
            float actionY=actionIndex==0 ? -h*.235f : -h*.385f;
            Place((RectTransform)settingsButtons[i].transform,
                new Vector2(0,actionY),new Vector2(Mathf.Min(210,w*.48f),bh));
            actionIndex++;
        }
        UpdateSettingVisuals();
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
        var go=new GameObject("Opening Volume Bar",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image),typeof(CustomInteractable));
        volumeTrack=(RectTransform)go.transform;volumeTrack.SetParent(parent,false);
        var bg=go.GetComponent<Image>();bg.color=new Color(.11f,.20f,.25f,.96f);
        bg.raycastTarget=false;
        var hit=go.GetComponent<CustomInteractable>();hit.onClickEvent=new UnityEvent();
        var fill=new GameObject("Opening Volume Fill",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image));
        volumeFill=(RectTransform)fill.transform;volumeFill.SetParent(volumeTrack,false);
        fill.GetComponent<Image>().color=new Color(.49f,.76f,.80f,.95f);
        fill.GetComponent<Image>().raycastTarget=false;
        var thumb=new GameObject("Opening Volume Handle",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image));
        volumeKnob=(RectTransform)thumb.transform;volumeKnob.SetParent(volumeTrack,false);
        thumb.GetComponent<Image>().color=new Color(.94f,.96f,.99f,1f);
        thumb.GetComponent<Image>().raycastTarget=false;
        volumeValueLabel=Label(template,parent,"60%",new Vector2(0,-12),
            new Vector2(55,30),15);
        // Do not create a built-in uGUI Slider: native overlay mouse dragging
        // is forwarded through UIManager and TransparentWindow.
    }

    public bool TryBeginVolumeDrag(Vector2 screenPoint)
    {
        if(entryLocked || settingsPage==null || !settingsPage.activeInHierarchy ||
            manager==null || manager.HasPendingConfirmation || volumeTrack==null ||
            !RectTransformUtility.RectangleContainsScreenPoint(volumeTrack,screenPoint,null))
            return false;
        volumeDragging=true;
        DragVolume(screenPoint);
        return true;
    }

    public void DragVolume(Vector2 screenPoint)
    {
        if(!volumeDragging || volumeTrack==null || !settingsPage.activeInHierarchy)return;
        Vector2 local;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            volumeTrack,screenPoint,null,out local))return;
        var rect=volumeTrack.rect;
        float fraction=(local.x-rect.xMin)/Mathf.Max(1f,rect.width);
        MapBgmController.PreviewVolume(Mathf.Clamp01(fraction));
        UpdateSettingVisuals();
    }

    public void EndVolumeDrag()
    {
        if(volumeDragging)MapBgmController.SaveVolumePreference();
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
        MapBgmController.SetVolume(.6f);
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
        float volume=MapBgmController.CurrentVolume;
        if(volumeValueLabel!=null)
            volumeValueLabel.text=Mathf.RoundToInt(volume*100f)+"%";
        if(volumeTrack!=null && volumeFill!=null && volumeKnob!=null)
        {
            float width=volumeTrack.rect.width;
            // Both fill and handle remain purely visual; only the track hits.
            var fill=volumeFill;
            fill.anchorMin=new Vector2(0f,.5f);fill.anchorMax=new Vector2(0f,.5f);
            fill.pivot=new Vector2(0f,.5f);
            fill.anchoredPosition=new Vector2(-width*.5f,0f);
            fill.sizeDelta=new Vector2(width*volume,12f);
            var knob=volumeKnob;
            knob.anchorMin=knob.anchorMax=new Vector2(.5f,.5f);
            knob.pivot=new Vector2(.5f,.5f);
            knob.anchoredPosition=new Vector2(width*(volume-.5f),0f);
            knob.sizeDelta=new Vector2(12f,24f);
        }
    }
    private void ExitGame(){Application.Quit();}
    public void Refresh()
    {
        if(continueButton!=null)continueButton.interactable=!entryLocked&&manager.CanContinue;
    }
    public void ShowConfirmation(string text)
    {
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
        Refresh();
    }

    public void LockButtonsForEntry()
    {
        entryLocked=true;
        foreach(var b in menuButtons)if(b!=null)b.interactable=false;
        settingsPage.SetActive(false);
    }
    public bool BeginGameplayTransition()
    {
        if(cinematic==null || cinematic.IsEntering)return false;
        cinematic.BeginEntering();
        return true;
    }
}
