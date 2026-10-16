using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

// Runtime UI: map artwork is assigned in UIManager, geometry is read from
// Assets/Resources/WorldMapPolygons.json, authored using WorldMapPolygonEditorWindow.
public sealed class WorldMapWindowUI
{
    private readonly UIManager owner;
    private readonly RectTransform root, mapArea;
    private readonly WorldMapRevealedGraphic revealed;
    private readonly WorldMapOutlineGraphic outlines;
    private readonly TextMeshProUGUI title, detail, travelCaption;
    private readonly RectTransform travelButton;
    private string chosenMapId;
    private bool restorePause;
    private Sprite artwork;

    public RectTransform Root => root;
    public bool IsOpen => root != null && root.gameObject.activeSelf;

    private WorldMapWindowUI(UIManager ui, RectTransform host, Sprite mapSprite)
    {
        owner = ui;
        artwork = mapSprite;
        var panel = new GameObject("WorldMapWindow", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        root = (RectTransform)panel.transform;
        root.SetParent(host, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        var background = panel.GetComponent<Image>();
        background.color = new Color(.075f, .095f, .13f, .985f);
        background.raycastTarget = false;

        title = ui.MakeLabel(root, "BẢN ĐỒ THẾ GIỚI", Vector2.zero, new Vector2(560, 32));
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(-100f, 30f);
        title.fontSize = 19f;
        title.color = new Color(1f, .86f, .55f, 1f);

        var picture = new GameObject("WorldMapArtWithFog", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
        mapArea = (RectTransform)picture.transform;
        mapArea.SetParent(root, false);
        mapArea.anchorMin = new Vector2(.04f, .22f);
        mapArea.anchorMax = new Vector2(.96f, .88f);
        mapArea.offsetMin = Vector2.zero;
        mapArea.offsetMax = Vector2.zero;
        var fitter = picture.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = mapSprite != null ? (float)mapSprite.rect.width / mapSprite.rect.height : 1.5f;
        var image = picture.GetComponent<Image>();
        image.sprite = mapSprite;
        image.color = Color.white;
        image.raycastTarget = false;

        var fog = new GameObject("FullFog_Unvisited", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        var fogRect = (RectTransform)fog.transform;
        fogRect.SetParent(mapArea, false);
        Stretch(fogRect);
        // Plain opaque fog placeholder: artwork never leaks outside revealed hexes.
        // Cloud texture/animation can be provided later without altering hit geometry.
        fog.GetComponent<Image>().color = new Color(.74f, .80f, .82f, .98f);
        fog.GetComponent<Image>().raycastTarget = false;

        var revealObj = new GameObject("RevealedHexagonMeshes",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(WorldMapRevealedGraphic));
        var revealRect = (RectTransform)revealObj.transform;
        revealRect.SetParent(mapArea, false);
        Stretch(revealRect);
        revealed = revealObj.GetComponent<WorldMapRevealedGraphic>();
        revealed.raycastTarget = false;

        var outlineObj = new GameObject("UnlockedAndSelectedHexagonBorders",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(WorldMapOutlineGraphic));
        var outlineRect = (RectTransform)outlineObj.transform;
        outlineRect.SetParent(mapArea, false);
        Stretch(outlineRect);
        outlines = outlineObj.GetComponent<WorldMapOutlineGraphic>();
        outlines.raycastTarget = false;

        detail = ui.MakeLabel(root, "", Vector2.zero, new Vector2(540, 58));
        var detailsRect = (RectTransform)detail.transform;
        detailsRect.anchorMin = new Vector2(.04f, .035f);
        detailsRect.anchorMax = new Vector2(.80f, .19f);
        detailsRect.pivot = new Vector2(0f, .5f);
        detailsRect.anchoredPosition = Vector2.zero;
        detailsRect.sizeDelta = Vector2.zero;
        detail.alignment = TextAlignmentOptions.MidlineLeft;
        detail.fontSize = 12f;
        detail.enableWordWrapping = true;

        travelButton = ui.SmallButton(root, "WorldTravelButton", "Di chuyển",
            new Vector2(.99f, .10f), new Vector2(1f, .5f),
            Vector2.zero, new Vector2(90, 36), ConfirmTravel);
        travelCaption = travelButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (travelCaption != null) travelCaption.fontSize = 12f;
        ui.SmallButton(root, "WorldMapClose", "Đóng",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f, -8f), new Vector2(70, 32), Close);
        root.gameObject.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static WorldMapWindowUI Install(UIManager ui, RectTransform host, Sprite sprite)
        => ui == null || host == null ? null : new WorldMapWindowUI(ui, host, sprite);

    private static GameManager Game()
        => Object.FindFirstObjectByType<GameManager>();

    public void Open()
    {
        var game = Game();
        if (game == null || !game.IsGameplayHUDVisible ||
            !WorldMapCatalog.Enabled(game.HeroData)) return;
        restorePause = owner.IsPaused;
        owner.SetPaused(true);
        chosenMapId = game.HeroData.worldMapId;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        root.gameObject.SetActive(false);
        owner.SetPaused(restorePause);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public bool ContainsMapPoint(Vector2 screenPoint)
        => IsOpen && mapArea != null &&
           RectTransformUtility.RectangleContainsScreenPoint(mapArea, screenPoint, null);

    public bool HandleMapClick(Vector2 screenPoint)
    {
        if (!ContainsMapPoint(screenPoint)) return false;
        var game = Game();
        if (game == null || !WorldMapCatalog.Enabled(game.HeroData)) return true;
        var hero = game.HeroData;
        Vector2 p = WorldMapPolygons.NormalizedPoint(mapArea, screenPoint);
        string id = WorldMapPolygons.HitTest(p, candidate =>
        {
            WorldMapNode current;
            return hero.worldMapVisited != null && hero.worldMapVisited.Contains(candidate) ||
                WorldMapCatalog.TryGet(hero.worldMapId, out current) &&
                WorldMapCatalog.IsCleared(hero, current.Id) && current.Links.Contains(candidate);
        });
        if (id != null)
        {
            chosenMapId = id;
            Refresh();
        }
        else if (WorldMapPolygons.Load().polygons.Count == 0)
        {
            detail.text = "Chưa hiệu chỉnh 6 đỉnh cho các ô map. Vào Tools > Bach Luyen Tien Do > World Map Polygon Editor (.54).";
        }
        else detail.text = "Vùng chưa mở / chưa được gán tọa độ. Hoàn thành wave tối thiểu trước khi xem ô kề.";
        return true; // Never click-through the map into battlefield HUD.
    }

    public void Refresh()
    {
        var game = Game();
        if (!IsOpen || game == null || !WorldMapCatalog.Enabled(game.HeroData)) return;
        var hero = game.HeroData;
        revealed.SetSource(artwork, hero);
        outlines.Refresh(hero, chosenMapId);

        WorldMapNode current;
        if (!WorldMapCatalog.TryGet(hero.worldMapId, out current)) return;
        WorldMapNode chosen;
        if (!WorldMapCatalog.TryGet(chosenMapId, out chosen)) chosen = current;
        string reason = WorldMapCatalog.TravelBlockReason(hero, chosen.Id);
        bool canTravel = reason == null;
        if (travelButton != null) travelButton.gameObject.SetActive(canTravel);

        int progress = Mathf.Clamp(hero.worldMapWaveProgress, 0, 5);
        bool visited = hero.worldMapVisited != null && hero.worldMapVisited.Contains(chosen.Id);
        string neighbors = WorldMapCatalog.IsCleared(hero, current.Id)
            ? "Đã mở " + current.Links.Count + " hướng đi kề (đồ thị tạm)"
            : "Cần vượt " + (5 - progress) + " wave để mở các map kề";
        if (artwork == null)
            neighbors += " | Chưa gán Sprite bản đồ trong UIManager";
        detail.text = chosen.Id + " – " + chosen.Name +
            (chosen.IsBoss ? " [BOSS ENDGAME]" : "") +
            (visited ? "\n" + chosen.TerrainLabel + " · Cấp đề xuất " +
                chosen.SuggestedLevel : "\nChưa khám phá · Cấp đề xuất " + chosen.SuggestedLevel) +
            "\n" + (canTravel ? "Có thể di chuyển thủ công." :
                chosen.Id == current.Id ? neighbors : reason);
    }

    private void ConfirmTravel()
    {
        var game = Game();
        if (game == null || string.IsNullOrEmpty(chosenMapId)) return;
        if (game.TryTravelWorldMap(chosenMapId)) Close();
        else Refresh();
    }
}