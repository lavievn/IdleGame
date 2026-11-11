using TMPro;
using UnityEngine;

// Pure presentation helper, intentionally separate from UIManager's SystemMenu.
// The same popup can be moved into System Menu later without modifying audio logic.
// Uses CustomInteractable via SmallButton: drag-based uGUI Sliders are unreliable
// with the current native Windows transparent-window pointer forwarding.
public sealed class MusicVolumePanelUI
{
    private readonly RectTransform root;
    private readonly TextMeshProUGUI valueText;
    private readonly TextMeshProUGUI muteText;
    private float lastAudibleVolume;
    private readonly AudioVolumeBar bgmBar,sfxBar,environmentBar;

    private MusicVolumePanelUI(UIManager owner, Transform parent)
    {
        var panel = new GameObject("MusicVolumePanel", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        root = (RectTransform)panel.transform;
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(1, 1);
        root.pivot = new Vector2(1, 1);
        root.anchoredPosition = new Vector2(-82, -32);
        root.sizeDelta = new Vector2(300, 281);
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(.08f, .13f, .20f, .96f);
        background.raycastTarget = false;

        owner.Skin(root,UIManager.SurfaceKind.Frame);
        owner.BindClick(root,()=>{});
        owner.PanelHeader(root,30);
        owner.SmallButton(root,"VolumeClose","×",new Vector2(1,1),new Vector2(1,1),new Vector2(-4,-4),new Vector2(25,22),Hide);
        var title = owner.MakeLabel(root, "ÂM LƯỢNG", new Vector2(150, -16), new Vector2(260, 25));
        title.fontSize = 13f;
        owner.MakeLabel(root,"Nhạc nền (BGM)",new Vector2(100,-40),new Vector2(180,25));
        owner.MakeLabel(root,"Hiệu ứng (SFX)",new Vector2(100,-98),new Vector2(180,25));
        valueText = owner.MakeLabel(root, "", new Vector2(255, -40), new Vector2(65, 25));
        var sfxValue=owner.MakeLabel(root,"",new Vector2(255,-98),new Vector2(65,25));
        owner.MakeLabel(root,"Âm thanh môi trường",new Vector2(108,-156),new Vector2(195,25));
        var environmentValue=owner.MakeLabel(root,"",new Vector2(255,-156),new Vector2(65,25));
        bgmBar=new AudioVolumeBar("Gameplay BGM Volume Bar",root,GameAudioChannel.Bgm,valueText);
        sfxBar=new AudioVolumeBar("Gameplay SFX Volume Bar",root,GameAudioChannel.Sfx,sfxValue);
        environmentBar=new AudioVolumeBar("Gameplay Environment Volume Bar",root,GameAudioChannel.Environment,environmentValue);

        owner.SmallButton(root, "VolumeMinus", "−", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(40, -210), new Vector2(46, 27), () => ChangeBy(-10));
        var mute = owner.SmallButton(root, "VolumeMute", "Tắt nhạc", new Vector2(0, 1),
            new Vector2(0, 1), new Vector2(92, -210), new Vector2(116, 27), ToggleMute);
        muteText = mute.GetComponentInChildren<TextMeshProUGUI>(true);
        owner.SmallButton(root, "VolumePlus", "+", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(214, -210), new Vector2(46, 27), () => ChangeBy(10));
        owner.SmallButton(root,"AudioDefaults","Khôi phục mặc định",new Vector2(0,1),new Vector2(0,1),
            new Vector2(40,-245),new Vector2(220,27),()=>{EndDrag();AudioVolumeSettings.RestoreDefaults();Refresh();});

        float volume = MapBgmController.CurrentVolume;
        lastAudibleVolume = volume > 0f ? volume : .6f;
        Layout();Refresh();
        root.gameObject.SetActive(false);
    }

    public static MusicVolumePanelUI Install(UIManager owner, Transform parent)
    {
        return owner == null || parent == null ? null : new MusicVolumePanelUI(owner, parent);
    }

    public RectTransform Root => root;
    public bool IsOpen => root != null && root.gameObject.activeSelf;

    public void Toggle()
    {
        if (root == null) return;
        bool open = !root.gameObject.activeSelf;
        if(!open)EndDrag();
        root.gameObject.SetActive(open);
        if (open)
        {
            Refresh();
            root.SetAsLastSibling();
        }
    }

    public void Hide()
    {
        EndDrag();
        if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
    }

    public void SetVisibleInGameplay(bool gameplayControlsVisible)
    {
        if (!gameplayControlsVisible) Hide();
    }

    public void Layout()
    {
        if (root == null) return;
        root.anchorMin = root.anchorMax = new Vector2(1, 1);
        root.pivot = new Vector2(1, 1);
        root.anchoredPosition = new Vector2(-82, -32);
        root.sizeDelta = new Vector2(300, 281);
        PlaceTrack(bgmBar.Track,-65f);PlaceTrack(sfxBar.Track,-123f);PlaceTrack(environmentBar.Track,-181f);
        bgmBar.Refresh(true);sfxBar.Refresh(true);environmentBar.Refresh(true);
    }
    private static void PlaceTrack(RectTransform track,float y)
    {
        track.anchorMin=track.anchorMax=new Vector2(0,1);track.pivot=new Vector2(.5f,.5f);
        track.anchoredPosition=new Vector2(150,y);track.sizeDelta=new Vector2(260,24);
    }
    public bool TryBeginDrag(Vector2 point)
    {
        if(!IsOpen)return false;
        EndDrag();return bgmBar.TryBeginDrag(point)||sfxBar.TryBeginDrag(point)||environmentBar.TryBeginDrag(point);
    }
    public bool IsDragging => bgmBar.IsDragging||sfxBar.IsDragging||environmentBar.IsDragging;
    public bool IsVolumeTrack(CustomInteractable item)
    {
        return item!=null && (item.transform==bgmBar.Track || item.transform==sfxBar.Track || item.transform==environmentBar.Track);
    }
    public void Drag(Vector2 point) {bgmBar.Drag(point);sfxBar.Drag(point);environmentBar.Drag(point);Refresh();}
    public void EndDrag() {bgmBar.EndDrag();sfxBar.EndDrag();environmentBar.EndDrag();}
    public void Tick() {if(IsOpen) {bgmBar.Refresh();sfxBar.Refresh();environmentBar.Refresh();}}

    private void ChangeBy(int percentagePoints)
    {
        int current = Mathf.RoundToInt(MapBgmController.CurrentVolume * 100f);
        int next = Mathf.Clamp(current + percentagePoints, 0, 100);
        MapBgmController.SetVolume(next / 100f);
        if (next > 0) lastAudibleVolume = next / 100f;
        Refresh();
    }

    private void ToggleMute()
    {
        float volume = MapBgmController.CurrentVolume;
        if (volume > .0001f)
        {
            lastAudibleVolume = volume;
            MapBgmController.SetVolume(0f);
        }
        else
        {
            MapBgmController.SetVolume(lastAudibleVolume > 0f ? lastAudibleVolume : .6f);
        }
        Refresh();
    }

    private void Refresh()
    {
        int value = Mathf.RoundToInt(MapBgmController.CurrentVolume * 100f);
        if (valueText != null) valueText.text = value + "%";
        if (muteText != null) muteText.text = value == 0 ? "Bật nhạc" : "Tắt nhạc";
        bgmBar.Refresh();sfxBar.Refresh();environmentBar.Refresh();
    }
}
