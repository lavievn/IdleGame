using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public partial class UIManager
{
    private RectTransform originPanel, originTalk, originEncounterButton, originJournal, originDismiss;
    private RectTransform originPrev, originNext, originPromptButton, originAccept, originDecline;
    private TextMeshProUGUI originLabel, originMapCaption, originTalkLabel;
    private TextMeshProUGUI originPromptLabel, originAcceptLabel, originDeclineLabel;
    private EntityDataSO originHero, originNoticeHero;
    private OriginStage originHintStage;
    private string originHintMap;
    private bool originDismissed, originJournalOpen, originMapHighlighted, originManualNotice, offeringHerbs;
    private OriginEncounter openEncounter;
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
        originManualNotice = false;
    }

    private RectTransform StoryButton(string name, string label, float x, float width, UnityAction action) =>
        SmallButton(originPanel, name, label, Vector2.zero, Vector2.zero,
            new Vector2(x, 4f), new Vector2(width, 24f), action);

    private void InstallOriginPresentation()
    {
        var panel = new GameObject("HanVuStory", typeof(RectTransform), typeof(Image));
        originPanel = (RectTransform)panel.transform;
        originPanel.SetParent(w3.Root, false);
        originPanel.anchorMin = originPanel.anchorMax = new Vector2(.5f, 0f);
        originPanel.pivot = new Vector2(.5f, 0f);
        originPanel.anchoredPosition = new Vector2(0f, 8f);
        var backdrop = panel.GetComponent<Image>();
        backdrop.color = new Color(.07f, .12f, .16f, .88f);
        backdrop.raycastTarget = false;
        originLabel = MakeLabel(originPanel, "", new Vector2(165f, -35f), new Vector2(310f, 62f));
        originLabel.alignment = TextAlignmentOptions.TopLeft;
        originLabel.enableWordWrapping = true;
        originLabel.overflowMode = TextOverflowModes.Ellipsis;
        ReadableText(originLabel, 12f);
        originTalk = StoryButton("HanVuTalk", "Trò chuyện", 6f, 89f, OpenOriginTalk);
        originTalkLabel = originTalk.GetComponentInChildren<TextMeshProUGUI>(true);
        originEncounterButton = StoryButton("HanVuEncounter", "Kỳ ngộ", 99f, 70f, OpenOriginEncounter);
        originJournal = StoryButton("HanVuJournal", "Nhật ký", 173f, 70f, ToggleOriginJournal);
        originDismiss = StoryButton("HanVuDismiss", "Đóng", 249f, 75f, DismissOriginHint);
        originPrev = StoryButton("HanVuPrev", "‹", 6f, 30f, () => originPage--);
        originNext = StoryButton("HanVuNext", "›", 40f, 30f, () => originPage++);
        originPromptButton = StoryButton("HanVuPrompts", "Tắt gợi ý", 77f, 135f,
            () => GameForUI()?.ToggleOriginPrompts());
        originPromptLabel = originPromptButton.GetComponentInChildren<TextMeshProUGUI>(true);
        originAccept = StoryButton("HanVuAccept", "Nhận lời", 6f, 107f, () => ChooseOriginOffer(true));
        originDecline = StoryButton("HanVuDecline", "Từ chối", 117f, 116f, () => ChooseOriginOffer(false));
        originAcceptLabel = originAccept.GetComponentInChildren<TextMeshProUGUI>(true);
        originDeclineLabel = originDecline.GetComponentInChildren<TextMeshProUGUI>(true);
        originMapCaption = worldMapButton?.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void OpenOriginTalk()
    {
        var game = GameForUI();
        if (game?.TryOriginInteraction() != true) return;
        originManualNotice = true;
        originDismissed = false;
        offeringHerbs = StoryOrigin.CanOfferHerbs(game.HeroData);
    }

    private void OpenOriginEncounter()
    {
        openEncounter = GameForUI()?.OpenOriginEncounter();
        if (openEncounter != null) originDismissed = false;
    }

    private void ChooseOriginOffer(bool accept)
    {
        var game = GameForUI();
        if (game == null) return;
        bool chosen = offeringHerbs ? game.TryChooseHerbs(accept) : openEncounter != null &&
            game.TryChooseOriginEncounter(openEncounter.Id, accept ? EncounterChoice.Participated : EncounterChoice.Declined);
        offeringHerbs = false;
        openEncounter = null;
        originManualNotice = chosen;
    }

    internal void ToggleOriginJournal()
    {
        originJournalOpen = !originJournalOpen;
        originPage = 0;
    }

    internal void DismissOriginHint()
    {
        bool dismissHint = !originJournalOpen && !offeringHerbs && openEncounter == null;
        originJournalOpen = false;
        offeringHerbs = false;
        openEncounter = null;
        if (dismissHint) originDismissed = true;
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
        if (originHero != data || originHintMap != data.worldMapId)
        {
            originHero = data;
            originHintMap = data.worldMapId;
            originDismissed = false;
            originJournalOpen = false;
            offeringHerbs = false;
            openEncounter = null;
        }
        if (originHintStage != data.originStory.stage)
        {
            originHintStage = data.originStory.stage;
            originDismissed = false;
        }
        var encounter = HanVuEncounters.Available(data);
        if (openEncounter != null && openEncounter != encounter) openEncounter = null;
        bool choice = offeringHerbs || openEncounter != null;
        bool idle = !originJournalOpen && !choice;
        bool talk = idle && StoryOrigin.CanInteract(data);
        originTalk.gameObject.SetActive(talk);
        if (talk) originTalkLabel.text = StoryOrigin.InteractionLabel(data);
        originEncounterButton.gameObject.SetActive(idle && encounter != null);
        originJournal.gameObject.SetActive(idle);
        originPromptButton.gameObject.SetActive(originJournalOpen);
        originAccept.gameObject.SetActive(choice);
        originDecline.gameObject.SetActive(choice && (!offeringHerbs ||
            data.originStory.herbalParticipation != HerbalParticipation.Declined));
        originPrev.gameObject.SetActive(originJournalOpen && originPage > 0);
        originNext.gameObject.SetActive(false);
        if (originJournalOpen)
        {
            var entries = StoryOrigin.Journal(data);
            originPage = Mathf.Clamp(originPage, 0, entries.Count - 1);
            originNext.gameObject.SetActive(originPage < entries.Count - 1);
            originLabel.text = "Nhật ký " + (originPage + 1) + "/" + entries.Count + "\n" + entries[originPage];
            originPromptLabel.text = data.originStory.hideStoryPrompts ? "Bật gợi ý truyện" : "Tắt gợi ý truyện";
        }
        else if (choice)
        {
            originLabel.text = offeringHerbs ? originNotice ?? "Có thể tìm lá bạc để sắc thuốc." :
                openEncounter.Title + "\n" + openEncounter.Observation;
            originAcceptLabel.text = offeringHerbs ? "Nhận lời" : openEncounter.Participate;
            originDeclineLabel.text = offeringHerbs ? "Để sau" : openEncounter.Decline;
        }
        else
        {
            bool notice = !originDismissed && originNotice != null && originNoticeHero == data &&
                originNoticeMap == data.worldMapId && Time.realtimeSinceStartup < originNoticeUntil &&
                (!data.originStory.hideStoryPrompts || originManualNotice);
            originLabel.text = notice ? originNotice : originDismissed ? "" : StoryOrigin.Hint(data);
        }
        bool hasText = originLabel.text.Length > 0;
        originDismiss.gameObject.SetActive(hasText || choice);
        originPanel.sizeDelta = new Vector2(330f, hasText ? 102f : 30f);
        HighlightOriginMap(idle && !originDismissed && StoryOrigin.HintMap(data));
    }

    private void HighlightOriginMap(bool active)
    {
        if (originMapCaption == null || active == originMapHighlighted) return;
        originMapCaption.color = active ? new Color(1f, .91f, .71f) : Color.white;
        originMapHighlighted = active;
    }
}
