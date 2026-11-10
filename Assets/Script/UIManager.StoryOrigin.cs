using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManager
{
    private RectTransform originPanel, originTalk, originDismiss, originPrev, originNext;
    private TextMeshProUGUI originLabel, originMapCaption, originTalkLabel;
    private EntityDataSO originHero, originNoticeHero;
    private OriginStage originHintStage;
    private string originHintMap;
    private bool originDismissed, originJournalOpen, originMapHighlighted;
    private int originPage;
    private string originNotice, originNoticeMap;
    private float originNoticeUntil;

    internal void ShowOriginNotice(EntityDataSO data, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        originNotice = text;
        originNoticeHero = data;
        originNoticeMap = data.worldMapId;
        originNoticeUntil = Time.realtimeSinceStartup + 9f;
    }

    private void InstallOriginPresentation()
    {
        var panel = new GameObject("HanVuStory", typeof(RectTransform), typeof(Image));
        originPanel = (RectTransform)panel.transform;
        originPanel.SetParent(w3.Root, false);
        originPanel.anchorMin = originPanel.anchorMax = new Vector2(.5f, 0f);
        originPanel.pivot = new Vector2(.5f, 0f);
        originPanel.anchoredPosition = new Vector2(0f, 8f);
        originPanel.sizeDelta = new Vector2(330f, 102f);
        var backdrop = panel.GetComponent<Image>();
        backdrop.color = new Color(.07f, .12f, .16f, .88f);
        backdrop.raycastTarget = false;
        originLabel = MakeLabel(originPanel, "", new Vector2(165f, -35f), new Vector2(310f, 62f));
        originLabel.alignment = TextAlignmentOptions.TopLeft;
        originLabel.enableWordWrapping = true;
        originLabel.overflowMode = TextOverflowModes.Ellipsis;
        ReadableText(originLabel, 12f);
        var bottom = new Vector2(0f, 0f);
        originTalk = SmallButton(originPanel, "HanVuTalk", "Trò chuyện", bottom, bottom,
            new Vector2(6f, 4f), new Vector2(97f, 24f), () => GameForUI()?.TryOriginInteraction());
        originTalkLabel = originTalk.GetComponentInChildren<TextMeshProUGUI>(true);
        SmallButton(originPanel, "HanVuJournal", "Nhật ký", bottom, bottom,
            new Vector2(107f, 4f), new Vector2(70f, 24f), () => ToggleOriginJournal());
        originPrev = SmallButton(originPanel, "HanVuPrev", "‹", bottom, bottom,
            new Vector2(181f, 4f), new Vector2(30f, 24f), () => originPage--);
        originNext = SmallButton(originPanel, "HanVuNext", "›", bottom, bottom,
            new Vector2(215f, 4f), new Vector2(30f, 24f), () => originPage++);
        originDismiss = SmallButton(originPanel, "HanVuDismiss", "Ẩn gợi ý", bottom, bottom,
            new Vector2(249f, 4f), new Vector2(75f, 24f), DismissOriginHint);
        originMapCaption = worldMapButton?.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    internal void ToggleOriginJournal()
    {
        originJournalOpen = !originJournalOpen;
        originPage = 0;
    }

    internal void DismissOriginHint()
    {
        originJournalOpen = false;
        originDismissed = true;
        originNotice = null;
    }

    private void TickOriginPresentation()
    {
        var data = hudGame?.HeroData;
        bool available = w3 != null && w3.Root.gameObject.activeSelf &&
            StoryOrigin.Definition(data) != null && !hudGame.HasTribulationLiability;
        if (!available)
        {
            if (originPanel != null) originPanel.gameObject.SetActive(false);
            HighlightOriginMap(false);
            return;
        }
        if (originPanel == null) InstallOriginPresentation();
        originPanel.gameObject.SetActive(true);
        if (originHero != data || originHintStage != data.originStory.stage || originHintMap != data.worldMapId)
        {
            originHero = data;
            originHintStage = data.originStory.stage;
            originHintMap = data.worldMapId;
            originDismissed = false;
            originJournalOpen = false;
        }
        bool notice = !originDismissed && originNotice != null && originNoticeHero == data &&
            originNoticeMap == data.worldMapId && Time.realtimeSinceStartup < originNoticeUntil;
        bool talk = StoryOrigin.CanInteract(data) && !originJournalOpen;
        originTalk.gameObject.SetActive(talk);
        if (talk) originTalkLabel.text = StoryOrigin.InteractionLabel(data);
        originDismiss.gameObject.SetActive(originJournalOpen || !originDismissed);
        originPrev.gameObject.SetActive(originJournalOpen && originPage > 0);
        if (originJournalOpen)
        {
            var entries = StoryOrigin.Journal(data);
            originPage = Mathf.Clamp(originPage, 0, entries.Count - 1);
            originNext.gameObject.SetActive(originPage < entries.Count - 1);
            originLabel.text = "Nhật ký " + (originPage + 1) + "/" + entries.Count + "\n" + entries[originPage];
        }
        else
        {
            originNext.gameObject.SetActive(false);
            originLabel.text = notice ? originNotice : originDismissed ? "" : StoryOrigin.Hint(data);
        }
        // Keep a compact Journal entry when the player dismisses the hint.
        originPanel.sizeDelta = new Vector2(330f, originLabel.text.Length == 0 ? 30f : 102f);
        HighlightOriginMap(!originDismissed && StoryOrigin.HintMap(data));
    }

    private void HighlightOriginMap(bool active)
    {
        if (originMapCaption == null) return;
        if (active)
        {
            float pulse = .7f + .3f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
            originMapCaption.color = new Color(1f, 1f - .25f * pulse, 1f - .71f * pulse);
        }
        else if (originMapHighlighted)
            originMapCaption.color = Color.white;
        originMapHighlighted = active;
    }
}
