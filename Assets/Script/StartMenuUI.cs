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
    private TextMeshProUGUI message, musicLabel;
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

        menu.musicLabel = menu.Label(textTemplate,settingsRoot,"ÂM LƯỢNG NHẠC",
            new Vector2(0,125),new Vector2(380,40),18);
        menu.MakeButton(template,settingsRoot,"Giảm 10%",
            new Vector2(0,52),()=>menu.AdjustMusic(-.1f));
        menu.MakeButton(template,settingsRoot,"Tăng 10%",
            new Vector2(0,8),()=>menu.AdjustMusic(.1f));
        menu.MakeButton(template,settingsRoot,"Tắt/Bật nhạc",
            new Vector2(0,-36),menu.ToggleMusic);
        menu.MakeButton(template,settingsRoot,"Quay lại",
            new Vector2(0,-80),menu.CloseSettings);
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
        var settingsButtons=settingsPage.GetComponentsInChildren<Button>(true);
        for(int i=0;i<settingsButtons.Length;i++)
            Place((RectTransform)settingsButtons[i].transform,
                new Vector2(0,1.5f*spacing-i*spacing-7f),new Vector2(bw,bh));
        // Only the menu is moved on resize. Art layer sizes are independent.
        if(cinematic!=null)cinematic.Layout();
    }

    private void OpenSettings()
    {
        menuPage.SetActive(false);
        settingsPage.SetActive(true);
        UpdateMusicLabel();
    }
    private void CloseSettings()
    {
        settingsPage.SetActive(false);
        menuPage.SetActive(true);
        Refresh();
    }
    private void UpdateMusicLabel()
    {
        if(musicLabel!=null)
            musicLabel.text="ÂM LƯỢNG NHẠC: "+Mathf.RoundToInt(MapBgmController.CurrentVolume*100f)+"%";
    }
    private void AdjustMusic(float step)
    {
        MapBgmController.SetVolume(Mathf.RoundToInt(
            Mathf.Clamp01(MapBgmController.CurrentVolume+step)*10f)/10f);
        UpdateMusicLabel();
    }
    private float lastAudible=.6f;
    private void ToggleMusic()
    {
        float volume=MapBgmController.CurrentVolume;
        if(volume>.0001f){lastAudible=volume;MapBgmController.SetVolume(0f);}
        else MapBgmController.SetVolume(lastAudible);
        UpdateMusicLabel();
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
