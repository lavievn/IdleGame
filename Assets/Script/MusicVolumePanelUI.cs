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

    private MusicVolumePanelUI(UIManager owner, Transform parent)
    {
        var panel = new GameObject("MusicVolumePanel", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        root = (RectTransform)panel.transform;
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(1, 1);
        root.pivot = new Vector2(1, 1);
        root.anchoredPosition = new Vector2(-82, -32);
        root.sizeDelta = new Vector2(226, 112);
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(.08f, .13f, .20f, .96f);
        background.raycastTarget = false;

        var title = owner.MakeLabel(root, "ÂM LƯỢNG NHẠC", new Vector2(98, -16), new Vector2(176, 25));
        title.fontSize = 13f;
        valueText = owner.MakeLabel(root, "", new Vector2(113, -44), new Vector2(200, 28));
        valueText.fontSize = 18f;

        owner.SmallButton(root, "VolumeMinus", "−", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -75), new Vector2(46, 27), () => ChangeBy(-10));
        var mute = owner.SmallButton(root, "VolumeMute", "Tắt nhạc", new Vector2(0, 1),
            new Vector2(0, 1), new Vector2(62, -75), new Vector2(102, 27), ToggleMute);
        muteText = mute.GetComponentInChildren<TextMeshProUGUI>(true);
        owner.SmallButton(root, "VolumePlus", "+", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(170, -75), new Vector2(46, 27), () => ChangeBy(10));

        float volume = MapBgmController.CurrentVolume;
        lastAudibleVolume = volume > 0f ? volume : .6f;
        Refresh();
        root.gameObject.SetActive(false);
    }

    public static MusicVolumePanelUI Install(UIManager owner, Transform parent)
    {
        return owner == null || parent == null ? null : new MusicVolumePanelUI(owner, parent);
    }

    public bool IsOpen => root != null && root.gameObject.activeSelf;

    public void Toggle()
    {
        if (root == null) return;
        bool open = !root.gameObject.activeSelf;
        root.gameObject.SetActive(open);
        if (open)
        {
            Refresh();
            root.SetAsLastSibling();
        }
    }

    public void Hide()
    {
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
        root.sizeDelta = new Vector2(226, 112);
    }

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
    }
}
