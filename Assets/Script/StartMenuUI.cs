using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Reuses the scene's font/button templates and builds only the requested menu.
// No scene replacement is needed, preserving locally adjusted Inspector values.
public class StartMenuUI : MonoBehaviour
{
    private GameManager manager;
    private Button continueButton;
    private TextMeshProUGUI message;
    private int lastClickFrame = -1;

    public static StartMenuUI Install(GameManager gm)
    {
        if (gm.preGameUI == null) return null;
        var template = gm.preGameUI.GetComponentInChildren<Button>(true);
        var textTemplate = gm.preGameUI.GetComponentInChildren<TextMeshProUGUI>(true);
        if (template == null || textTemplate == null) return null;
        var menu = gm.gameObject.AddComponent<StartMenuUI>();
        menu.manager = gm;
        // Hide old start/instruction elements after retaining their templates.
        foreach (Transform child in gm.preGameUI.transform) child.gameObject.SetActive(false);
        var root = gm.preGameUI.GetComponent<RectTransform>();
        Place(root, new Vector2(0, 0), new Vector2(460, 300));
        root.SetAsLastSibling();
        var image = gm.preGameUI.GetComponent<Image>();
        if (image != null) image.color = new Color(.08f,.12f,.18f,.98f);
        gm.infoText = menu.Label(textTemplate, root, "HÀNH TRÌNH", new Vector2(0,85), new Vector2(400,95), 24);
        menu.continueButton = menu.MakeButton(template, root, "Tiếp tục", new Vector2(0,-5), gm.OnContinueClicked);
        menu.MakeButton(template, root, "Chơi mới", new Vector2(0,-75), gm.OnNewGameClicked);

        if (gm.confirmationPopup != null) gm.confirmationPopup.SetActive(false);
        var popup = new GameObject("Start Choice Confirmation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var popupRect = (RectTransform)popup.transform;
        popupRect.SetParent(root.parent, false);
        popupRect.anchorMin = Vector2.zero; popupRect.anchorMax = Vector2.one;
        popupRect.sizeDelta = Vector2.zero; popupRect.anchoredPosition = Vector2.zero;
        popup.GetComponent<Image>().color = new Color(0,0,0,.9f);
        menu.message = menu.Label(textTemplate, popupRect, "", new Vector2(0,55), new Vector2(410,145), 24);
        menu.MakeButton(template, popupRect, "Xác nhận", new Vector2(-110,-65), gm.OnConfirmResetClicked);
        menu.MakeButton(template, popupRect, "Hủy", new Vector2(110,-65), gm.OnCancelResetClicked);
        gm.confirmationPopup = popup;
        popup.SetActive(false);
        // Register the new modal with the existing transparent-window hit testing.
        var window = Object.FindFirstObjectByType<TransparentWindow>();
        if (window != null)
        {
            var list = new System.Collections.Generic.List<RectTransform>(window.modalUI ?? new RectTransform[0]);
            list.Add(popupRect); list.Add(root); window.modalUI = list.ToArray();
        }
        menu.Refresh();
        return menu;
    }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }
    private TextMeshProUGUI Label(TextMeshProUGUI template, Transform parent, string text, Vector2 p, Vector2 size, float fontSize)
    {
        var go = Instantiate(template.gameObject, parent);
        go.SetActive(true);
        Place(go.GetComponent<RectTransform>(),p,size);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text=text; label.fontSize=fontSize; label.enableAutoSizing=true;
        label.fontSizeMin=16; label.fontSizeMax=fontSize; label.color=new Color(1,1,1,1);
        label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false;
        return label;
    }
    private Button MakeButton(Button template, Transform parent, string title, Vector2 p, UnityAction action)
    {
        var go = Instantiate(template.gameObject, parent); go.SetActive(true);
        Place(go.GetComponent<RectTransform>(),p,new Vector2(200,52));
        var button = go.GetComponent<Button>();
        var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) { label.text=title; label.enableAutoSizing=true; label.fontSizeMin=16; label.fontSizeMax=26; label.alignment=TextAlignmentOptions.Center; }
        UnityAction click = () => {
            if (!button.interactable || lastClickFrame == Time.frameCount) return;
            lastClickFrame = Time.frameCount; action();
        };
        button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(click);
        var custom = go.GetComponent<CustomInteractable>();
        if (custom == null) custom = go.AddComponent<CustomInteractable>();
        custom.onClickEvent = new UnityEvent(); custom.onClickEvent.AddListener(click);
        return button;
    }
    public void Refresh() { if (continueButton != null) continueButton.interactable = manager.CanContinue; }
    public void ShowConfirmation(string text)
    {
        message.text=text;
        manager.confirmationPopup.transform.SetAsLastSibling();
        manager.confirmationPopup.SetActive(true);
    }
}
