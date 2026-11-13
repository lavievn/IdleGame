using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class UIManager
{
    private RectTransform originPanel, originTalk, originWoodcutter, originEncounterButton, originJournal;
    private RectTransform originDismiss, originPrev, originNext, originPromptButton, originAccept, originDecline, originReplay;
    private TextMeshProUGUI originLabel, originMapCaption, originTalkLabel;
    private TextMeshProUGUI originPromptLabel, originAcceptLabel, originDeclineLabel, originNextLabel, originDismissLabel;
    private EntityDataSO originHero, originNoticeHero, openingHero;
    private string originMap;
    private bool originJournalOpen, originMapHighlighted, originManualNotice;
    private bool originControlsVisible;
    private OriginEncounter openEncounter;
    private OriginInteractionContext originContext;
    private HanVuConversation conversation;
    private HanVuLine[] dialogue;
    private int originPage, dialogueIndex;
    private string originNotice, originNoticeMap;
    private float originNoticeUntil;

    internal void ShowOriginOpening(EntityDataSO data) => openingHero = data;

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
        originLabel = MakeLabel(originPanel, "", new Vector2(150f, -29f), new Vector2(286f, 52f));
        originLabel.alignment = TextAlignmentOptions.TopLeft;
        originLabel.enableWordWrapping = true;
        originLabel.overflowMode = TextOverflowModes.Ellipsis;
        ReadableText(originLabel, 12f);
        originTalk = StoryButton("HanVuTalk", "Trò chuyện", 6f, 101f, OpenOriginTalk);
        originTalkLabel = originTalk.GetComponentInChildren<TextMeshProUGUI>(true);
        originEncounterButton = StoryButton("HanVuEncounter", "Kỳ ngộ", 111f, 75f, OpenOriginEncounter);
        originDismiss = StoryButton("HanVuDismiss", "Đóng", 229f, 65f, CloseOriginPresentation);
        originPrev = StoryButton("HanVuPrev", "Trước", 6f, 50f, () => MoveOriginLine(-1));
        originNext = StoryButton("HanVuNext", "Tiếp", 60f, 50f, () => MoveOriginLine(1));
        originNextLabel = originNext.GetComponentInChildren<TextMeshProUGUI>(true);
        originDismissLabel = originDismiss.GetComponentInChildren<TextMeshProUGUI>(true);
        originAccept = StoryButton("HanVuAccept", "Nhận lời", 60f, 85f, () => ChooseOriginOffer(true, null));
        originDecline = StoryButton("HanVuDecline", "Từ chối", 149f, 76f, () => ChooseOriginOffer(false, null));
        originReplay = StoryButton("HanVuReplay", "Xem lại mở đầu", 115f, 110f, () =>
        {
            var data = GameForUI()?.HeroData;
            if (StoryOrigin.IsSafe(data)) BeginOriginConversation(data, HanVuConversation.Opening);
        });
        originAcceptLabel = originAccept.GetComponentInChildren<TextMeshProUGUI>(true);
        originDeclineLabel = originDecline.GetComponentInChildren<TextMeshProUGUI>(true);
        originWoodcutter = SmallButton(w3.Root, "HanVuWoodcutter", "Người đốn củi", Vector2.zero, Vector2.zero,
            new Vector2(8f, 8f), new Vector2(114f, 25f), OpenWoodcutter);
        originJournal = SmallButton(w3.Root, "HanVuJournal", "Nhật ký", Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.one, ToggleOriginJournal);
        originPromptButton = SmallButton(w3.Root, "HanVuPrompts", "Ẩn gợi ý", Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.one, () => GameForUI()?.ToggleOriginPrompts());
        originPromptLabel = originPromptButton.GetComponentInChildren<TextMeshProUGUI>(true);
        originMapCaption = worldMapButton?.GetComponentInChildren<TextMeshProUGUI>(true);
        originPanel.gameObject.SetActive(false);
        originWoodcutter.gameObject.SetActive(false);
        originJournal.gameObject.SetActive(false);
        originPromptButton.gameObject.SetActive(false);
    }

    private void LayoutOriginControls(float width, float margin, float top, float cardWidth)
    {
        float buttonWidth = (cardWidth - 4f) / 2f;
        float left = width - margin - cardWidth;
        HudW3View.Place(originJournal, left, top, buttonWidth, 24f);
        HudW3View.Place(originPromptButton, left + buttonWidth + 4f, top, buttonWidth, 24f);
        LayoutHudButton(originJournal, 11f);
        LayoutHudButton(originPromptButton, 11f);
    }

    private void OpenOriginTalk()
    {
        var game = GameForUI();
        if (game == null) return;
        var data = game.HeroData;
        if (StoryOrigin.IsSafe(data))
        {
            var stage = data.originStory.stage;
            BeginOriginConversation(data, stage == OriginStage.Return ? HanVuConversation.BeforeMedicine :
                stage > OriginStage.Return ? HanVuConversation.AfterMedicine : HanVuConversation.Opening);
        }
        else if (game.TryOriginInteraction())
            originManualNotice = true;
    }

    private void OpenWoodcutter()
    {
        var game = GameForUI();
        if (game != null && game.IsGameplayHUDVisible && !game.HasTribulationLiability &&
            HanVuNarrative.AtWoodcutter(game.HeroData))
            BeginOriginConversation(game.HeroData, HanVuConversation.Woodcutter);
    }

    private void BeginOriginConversation(EntityDataSO data, HanVuConversation kind)
    {
        var game = GameForUI();
        originContext = game != null ? game.CaptureOriginInteraction(data) : default;
        BindOriginChoices();
        originJournalOpen = false;
        openEncounter = null;
        conversation = kind;
        dialogueIndex = 0;
        dialogue = HanVuNarrative.Lines(data, kind);
    }

    private void OpenOriginEncounter()
    {
        var game = GameForUI();
        openEncounter = game?.OpenOriginEncounter();
        originContext = game != null ? game.CaptureOriginInteraction(game.HeroData) : default;
        BindOriginChoices();
    }

    private void BindOriginChoices()
    {
        // Capture this intent, not the mutable context of a later conversation.
        var context = originContext;
        BindClick(originAccept, () => ChooseOriginOffer(true, context));
        BindClick(originDecline, () => ChooseOriginOffer(false, context));
    }

    private void MoveOriginLine(int direction)
    {
        if (originJournalOpen)
            originPage += direction;
        else if (dialogue != null)
        {
            if (direction > 0 && LastLine)
            {
                if (!OriginLineRequiresChoice(GameForUI()?.HeroData)) CloseOriginPresentation();
                return;
            }
            dialogueIndex = Mathf.Clamp(dialogueIndex + direction, 0, dialogue.Length - 1);
        }
    }

    private bool LastLine => dialogue != null && dialogueIndex == dialogue.Length - 1;

    private bool OriginLineRequiresChoice(EntityDataSO data) => LastLine &&
        ((conversation == HanVuConversation.Opening && StoryOrigin.CanOfferHerbs(data)) ||
            conversation == HanVuConversation.BeforeMedicine);

    private void ChooseOriginOffer(bool accept, OriginInteractionContext context)
    {
        if (!ReferenceEquals(context, originContext)) return;
        var game = GameForUI();
        if (game == null || !game.IsCurrentOriginInteraction(context))
        {
            CloseOriginPresentation();
            return;
        }
        if (conversation == HanVuConversation.BeforeMedicine && LastLine && accept)
        {
            if (game.TryOriginInteraction())
                BeginOriginConversation(game.HeroData, HanVuConversation.AfterMedicine);
            return;
        }
        bool chosen = conversation == HanVuConversation.Opening && LastLine
            ? game.TryChooseHerbs(accept)
            : openEncounter != null &&
              game.TryChooseOriginEncounter(openEncounter.Id, accept ? EncounterChoice.Participated : EncounterChoice.Declined);
        if (!chosen) return;
        conversation = HanVuConversation.None;
        dialogue = null;
        openEncounter = null;
        originContext = default;
        originManualNotice = true;
    }

    internal void ToggleOriginJournal()
    {
        bool open = !originJournalOpen;
        CloseOriginPresentation();
        originJournalOpen = open;
        originPage = 0;
    }

    private void CloseOriginPresentation()
    {
        originJournalOpen = false;
        conversation = HanVuConversation.None;
        dialogue = null;
        openEncounter = null;
        originNotice = null;
        originContext = default;
    }

    private void TickOriginPresentation()
    {
        if (w3 == null) return;
        var data = hudGame?.HeroData;
        bool visible = w3.Root.gameObject.activeSelf && StoryOrigin.Definition(data) != null &&
            !hudGame.HasTribulationLiability;
        if (originControlsVisible != visible)
        {
            originControlsVisible = visible;
            LayoutReadableUI();
        }
        originJournal.gameObject.SetActive(visible);
        originPromptButton.gameObject.SetActive(visible);
        if (!visible)
        {
            // Entry, death and Retry may reuse the same Hero and map. A stale
            // conversation must not survive that boundary and commit unseen lines.
            // Temporary menus only hide the view; they can resume the conversation.
            if (hudGame == null || !hudGame.IsGameplayHUDVisible || hudGame.HasTribulationLiability)
                CloseOriginPresentation();
            originPanel.gameObject.SetActive(false);
            originWoodcutter.gameObject.SetActive(false);
            HighlightOriginMap(false);
            return;
        }
        if (originHero != data || originMap != data.worldMapId)
        {
            originHero = data;
            originMap = data.worldMapId;
            CloseOriginPresentation();
        }
        if ((dialogue != null || openEncounter != null) && !hudGame.IsCurrentOriginInteraction(originContext))
            CloseOriginPresentation();
        if (openingHero == data)
        {
            openingHero = null;
            if (StoryOrigin.IsSafe(data) && !data.originStory.hideStoryPrompts)
                BeginOriginConversation(data, HanVuConversation.Opening);
        }
        var encounter = HanVuEncounters.Available(data);
        if (openEncounter != null && openEncounter != encounter) openEncounter = null;
        bool idle = !originJournalOpen && dialogue == null && openEncounter == null;
        bool mapOpen = worldMapUI != null && worldMapUI.IsOpen;
        if (mapOpen)
        {
            originPanel.gameObject.SetActive(false);
            originWoodcutter.gameObject.SetActive(false);
            HighlightOriginMap(false);
            return;
        }
        bool talk = idle && StoryOrigin.CanInteract(data);
        string destination = idle ? StoryOrigin.GuidedDestination(data) : null;
        bool guideMap = destination != null && WorldMapCatalog.TravelBlockReason(data, destination) == null;
        bool canOfferHerbs = conversation == HanVuConversation.Opening && LastLine && StoryOrigin.CanOfferHerbs(data);
        bool canPrepare = conversation == HanVuConversation.BeforeMedicine && LastLine;
        originTalk.gameObject.SetActive(talk);
        if (talk) originTalkLabel.text = StoryOrigin.InteractionLabel(data);
        originWoodcutter.gameObject.SetActive(idle && HanVuNarrative.AtWoodcutter(data));
        originEncounterButton.gameObject.SetActive(idle && encounter != null);
        originPromptLabel.text = data.originStory.hideStoryPrompts ? "Hiện gợi ý" : "Ẩn gợi ý";
        originAccept.gameObject.SetActive(openEncounter != null || canOfferHerbs || canPrepare);
        originDecline.gameObject.SetActive(openEncounter != null ||
            (canOfferHerbs && data.originStory.herbalParticipation != HerbalParticipation.Declined));
        originPrev.gameObject.SetActive(originJournalOpen ? originPage > 0 : dialogue != null && dialogueIndex > 0);
        originNext.gameObject.SetActive(dialogue != null && !OriginLineRequiresChoice(data));
        originNextLabel.text = dialogue != null && LastLine ? "Xong" : "Tiếp";
        originReplay.gameObject.SetActive(originJournalOpen && StoryOrigin.IsSafe(data));
        if (originJournalOpen)
        {
            var entries = StoryOrigin.Journal(data);
            originPage = Mathf.Clamp(originPage, 0, entries.Count - 1);
            originNext.gameObject.SetActive(originPage < entries.Count - 1);
            originLabel.text = "Nhật ký " + (originPage + 1) + "/" + entries.Count + "\n" + entries[originPage];
        }
        else if (dialogue != null)
        {
            var line = dialogue[dialogueIndex];
            originLabel.text = line.Speaker + " · " + (dialogueIndex + 1) + "/" + dialogue.Length + "\n" + line.Text;
            if (!data.originStory.heardOpeningTrip && HanVuNarrative.TellsOfTrip(conversation, dialogueIndex))
                hudGame.RecordOriginTrip();
            if (!data.originStory.heardWoodcutter && HanVuNarrative.TellsOfWoodcutter(conversation, dialogueIndex))
                hudGame.TryTalkToWoodcutter(dialogueIndex);
            originAcceptLabel.text = canPrepare ? "Sắc thuốc" : "Nhận lời";
            originDeclineLabel.text = "Từ chối";
        }
        else if (openEncounter != null)
        {
            originLabel.text = openEncounter.Title + "\n" + openEncounter.Observation;
            originAcceptLabel.text = openEncounter.Participate;
            originDeclineLabel.text = openEncounter.Decline;
        }
        else
        {
            bool notice = originNotice != null && originNoticeHero == data && originNoticeMap == data.worldMapId &&
                Time.realtimeSinceStartup < originNoticeUntil && (!data.originStory.hideStoryPrompts || originManualNotice);
            originLabel.text = guideMap ? "Nhấn vào tên bản đồ để mở bản đồ thế giới." :
                notice ? originNotice : StoryOrigin.Hint(data);
            if (!guideMap && !notice && destination != null && WorldMapCatalog.TryGet(data.worldMapId, out var current) &&
                !StoryOrigin.IsSafe(data) && !WorldMapCatalog.IsCleared(data, current.Id))
                originLabel.text = "Hoàn thành 5 đợt quái ở đây để mở đường đi tiếp.";
        }
        bool hasText = originLabel.text.Length > 0;
        originDismiss.gameObject.SetActive(hasText && (!idle || originNotice != null));
        originDismissLabel.text =
            conversation == HanVuConversation.Opening && !LastLine ? "Để sau" : "Đóng";
        originPanel.sizeDelta = new Vector2(300f, hasText ? 88f : 30f);
        originPanel.GetComponent<Image>().enabled = hasText;
        originPanel.gameObject.SetActive(hasText || talk || (idle && encounter != null));
        HighlightOriginMap(guideMap);
    }

    private void HighlightOriginMap(bool active)
    {
        if (originMapCaption == null) return;
        if (active) originMapCaption.color = new Color(1f, .91f, .71f);
        else if (originMapHighlighted) originMapCaption.color = Color.white;
        originMapHighlighted = active;
    }
}
