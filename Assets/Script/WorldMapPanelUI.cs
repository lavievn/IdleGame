using System.Collections.Generic;
using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;

// Provisional .54 atlas UI. Renders an existing illustration without drawing
// country borders. Reveal polygons when actual hex geometry is authored;
// until then use approximate soft fog openings for visited nodes ONLY.
public sealed class WorldMapPanelUI
{
    private readonly UIManager owner;
    private readonly GameManager host;
    private readonly RectTransform root;
    private readonly Image artwork;
    private readonly RawImage fog;
    private readonly WorldMapRevealedGraphic reveal;
    private readonly WorldMapOutlineGraphic outlines;
    private string selectedMapId = "";
    private readonly TextMeshProUGUI heading;
    private readonly TextMeshProUGUI summary;
    private readonly RectTransform viewport, artRect, travel;
    private bool pointerCaptured, panning;
    private Vector2 pressScreen, pressLocal, pressPan;
    private const float PanThreshold = 6f;
    private Texture2D fogTexture;
    private bool previousPause;
    private bool fogReady;
    private const float MapOpenZoom = 1.35f;
    private const float MinZoom = 1f;
    private const float MaxZoom = 2.5f;
    private float mapZoom = MapOpenZoom;

    private const int FogWidth = 160;
    private const int FogHeight = 92;

    private WorldMapPanelUI(UIManager owner, Transform parent, GameManager host)
    {
        this.owner = owner;
        this.host = host;
        var go = new GameObject("WorldMapPrototype", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        root = (RectTransform)go.transform;
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.pivot = new Vector2(.5f, .5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(900f, 560f);
        var backdrop = go.GetComponent<Image>();
        backdrop.color = new Color(.08f, .09f, .12f, .98f);
        backdrop.raycastTarget = false;

        viewport = (RectTransform)new GameObject("WorldMapViewport", typeof(RectTransform), typeof(RectMask2D)).transform;
        viewport.SetParent(root, false);
        viewport.anchorMin = viewport.anchorMax = new Vector2(.5f,.5f);
        viewport.pivot = new Vector2(.5f,.5f);
        var artGO = new GameObject("Artwork", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        artwork = artGO.GetComponent<Image>();
        artRect = (RectTransform)artGO.transform;
        artRect.SetParent(viewport, false);
        artRect.anchorMin = artRect.anchorMax = new Vector2(.5f,.5f);
        artRect.pivot = new Vector2(.5f,.5f);
        artRect.anchoredPosition = Vector2.zero;
        artRect.sizeDelta = new Vector2(605f,400f);
        artwork.color = new Color(.76f, .68f, .52f, 1f);
        artwork.raycastTarget = false;
        artwork.preserveAspect = true;
        // Import the approved map as Assets/Resources/WorldMapArtwork.png.
        artwork.sprite = Resources.Load<Sprite>("WorldMapArtwork");

        var cloud = new GameObject("FogOfWar", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        fog = cloud.GetComponent<RawImage>();
        RectTransform fogRect = (RectTransform)cloud.transform;
        fogRect.SetParent(artRect,false);
        fogRect.anchorMin = Vector2.zero;
        fogRect.anchorMax = Vector2.one;
        fogRect.pivot = new Vector2(.5f,.5f);
        fogRect.offsetMin = fogRect.offsetMax = Vector2.zero;
        fog.raycastTarget = false;
        fogTexture = new Texture2D(FogWidth,FogHeight,TextureFormat.RGBA32,false);
        fog.texture = fogTexture;
        // Every visited cell re-displays the original map texture inside its
        // six authored vertices; unknown cells remain covered by the full fog.
        var revealedGO = new GameObject("FogRevealedHexagons",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(WorldMapRevealedGraphic));
        var revealedRect = (RectTransform)revealedGO.transform;
        revealedRect.SetParent(artRect, false);
        revealedRect.anchorMin = Vector2.zero;
        revealedRect.anchorMax = Vector2.one;
        revealedRect.offsetMin = Vector2.zero;
        revealedRect.offsetMax = Vector2.zero;
        reveal = revealedGO.GetComponent<WorldMapRevealedGraphic>();
        reveal.raycastTarget = false;

        var outlineGO = new GameObject("HexagonTravelBoundaries",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(WorldMapOutlineGraphic));
        var outlineRect = (RectTransform)outlineGO.transform;
        outlineRect.SetParent(artRect, false);
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = Vector2.zero;
        outlineRect.offsetMax = Vector2.zero;
        outlines = outlineGO.GetComponent<WorldMapOutlineGraphic>();
        outlines.raycastTarget = false;
        // Render authored country names above the cloud layer, never borders.
        foreach (WorldMapNation nation in WorldMapCatalog.Nations)
        {
            TextMeshProUGUI label = owner.MakeLabel(artRect,nation.Name,Vector2.zero,new Vector2(158f,30f));
            label.fontSize = 13f;
            label.color = new Color(1f,.90f,.70f,1f);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = nation.LabelPosition;
            labelRect.pivot = new Vector2(.5f,.5f);
            labelRect.anchoredPosition = Vector2.zero;
            label.raycastTarget = false;
        }

        heading = owner.MakeLabel(root,"BẢN ĐỒ THẾ GIỚI",
            new Vector2(185f,-22f), new Vector2(350f,32f));
        heading.fontSize = 19f;
        heading.alignment = TextAlignmentOptions.Left;
        heading.color = new Color(1f,.85f,.50f,1f);

        summary = owner.MakeLabel(root,"", new Vector2(275f,-430f),
            new Vector2(520f,80f));
        summary.alignment = TextAlignmentOptions.TopLeft;
        summary.fontSize = 13f;

        owner.SmallButton(root,"WorldMapClose","Đóng",new Vector2(1f,1f),
            new Vector2(1f,1f),new Vector2(-14f,-14f),new Vector2(90f,32f),Close);
        travel = owner.SmallButton(root,"WorldMapTravelSelected","Tới ô đã chọn",new Vector2(1f,0f),
            new Vector2(1f,0f),new Vector2(-14f,12f),new Vector2(130f,30f),TravelSelected);
        travel.gameObject.SetActive(false);
        root.gameObject.SetActive(false);
    }

    public static WorldMapPanelUI Install(UIManager owner, Transform parent)
    {
        GameManager host = Object.FindFirstObjectByType<GameManager>();
        return owner == null || parent == null || host == null
            ? null : new WorldMapPanelUI(owner,parent,host);
    }

    public bool IsOpen => root != null && root.gameObject.activeSelf;
    // Zoom only the artwork subtree. Fog/hex masks/country labels share the
    // transformed coordinate space, so polygon hit testing remains aligned.
    public void HandleScroll(Vector2 mousePoint, float scroll)
    {
        if(!IsOpen || artwork==null || Mathf.Abs(scroll)<.001f || !HitMapSurface(mousePoint))return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, mousePoint, null, out var point)) return;
        float previous = mapZoom;
        mapZoom=Mathf.Clamp(mapZoom*Mathf.Pow(owner.WorldMapZoomPerNotch,scroll),MinZoom,MaxZoom);
        artRect.localScale=new Vector3(mapZoom,mapZoom,1f);
        SetPan(point + (artRect.anchoredPosition-point) * (mapZoom/previous));
    }
    public RectTransform Root => root;
    public bool HitMapSurface(Vector2 screenPoint) =>
        IsOpen && RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, null) &&
        RectTransformUtility.RectangleContainsScreenPoint(artRect, screenPoint, null);

    internal bool BeginPointer(Vector2 point)
    {
        if (!HitMapSurface(point) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, point, null, out pressLocal))
            return false;
        pressScreen = point;
        pressPan = artRect.anchoredPosition;
        pointerCaptured = true;
        panning = false;
        return true;
    }

    internal void DragPointer(Vector2 point)
    {
        if (!pointerCaptured || !IsOpen) return;
        if ((point - pressScreen).sqrMagnitude >= PanThreshold * PanThreshold) panning = true;
        if (panning && RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, point, null, out var local))
            SetPan(pressPan + local - pressLocal);
    }

    internal void EndPointer(Vector2 point)
    {
        if (!pointerCaptured) return;
        DragPointer(point);
        pointerCaptured = false;
        if (!panning) HandleMapClick(point);
    }

    void SetPan(Vector2 pan)
    {
        float x = Mathf.Max(0f, (artRect.rect.width * mapZoom - viewport.rect.width) / 2f);
        float y = Mathf.Max(0f, (artRect.rect.height * mapZoom - viewport.rect.height) / 2f);
        artRect.anchoredPosition = new Vector2(Mathf.Clamp(pan.x,-x,x), Mathf.Clamp(pan.y,-y,y));
    }

    // Hit against the exact six-corner region. Selection never auto-travels.
    public bool HandleMapClick(Vector2 screenPoint)
    {
        if (!HitMapSurface(screenPoint)) return false;
        EntityDataSO hero = host.HeroData;
        if (hero == null || !WorldMapCatalog.Enabled(hero)) return true;
        if (!WorldMapPolygons.MatchesArtwork(artwork.sprite))
        {
            summary.text = "Ảnh WorldMapArtwork không khớp hệ tọa độ lục giác. " +
                "Dùng ảnh có mã 1448×1086, hoặc hiệu chỉnh lại JSON theo ảnh hiện tại.";
            return true;
        }
        Vector2 position = WorldMapPolygons.NormalizedPoint(
            (RectTransform)artwork.transform, screenPoint);
        WorldMapCatalog.TryGet(hero.worldMapId, out var current);
        string id = WorldMapPolygons.HitTest(position, candidate =>
            (hero.worldMapVisited != null && hero.worldMapVisited.Contains(candidate)) ||
            (current != null && current.Links.Contains(candidate)));
        selectedMapId = id ?? "";
        Refresh();
        return true;
    }

    private void TravelSelected()
    {
        if (string.IsNullOrEmpty(selectedMapId)) return;
        if (host.TryTravelToWorldMap(selectedMapId))
        {
            pointerCaptured = false;
            root.gameObject.SetActive(false); // Map entry now owns pause.
        }
        else Refresh();
    }

    public void Toggle() { if(IsOpen) Close(); else Open(); }

    public void Open()
    {
        if(IsOpen || host == null || !host.IsGameplayHUDVisible) return;
        if(!WorldMapCatalog.Enabled(host.HeroData)) return;
        previousPause=owner.IsPaused;
        owner.SetPaused(true);
        pointerCaptured = false;
        artRect.anchoredPosition = Vector2.zero;
        selectedMapId = host.HeroData.worldMapId;
        mapZoom=MapOpenZoom;
        ((RectTransform)artwork.transform).localScale=new Vector3(mapZoom,mapZoom,1f);
        Resize();
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if(!IsOpen) return;
        pointerCaptured = false;
        root.gameObject.SetActive(false);
        owner.SetPaused(previousPause);
    }

    public void Resize()
    {
        if(root==null)return;
        float width = Mathf.Max(340f,Mathf.Min(Screen.width-12f,1070f));
        float height = Mathf.Max(260f,Mathf.Min(Screen.height-12f,650f));
        root.sizeDelta = new Vector2(width,height);
        viewport.sizeDelta = new Vector2(width - 20f, height - 98f);
        viewport.anchoredPosition = new Vector2(0f, -2f);
        var imageRect = artRect;
        // Exact image aspect is required: otherwise six-vertex hit regions
        // drift away from the visible polygons under preserveAspect.
        float aspect = artwork.sprite != null && artwork.sprite.rect.height > 0f
            ? artwork.sprite.rect.width / artwork.sprite.rect.height : 1.5f;
        float imageHeight = viewport.sizeDelta.y;
        float imageWidth = Mathf.Min(viewport.sizeDelta.x,imageHeight*aspect);
        imageHeight=Mathf.Min(imageHeight,imageWidth/aspect);
        imageRect.sizeDelta=new Vector2(imageWidth,imageHeight);
        summary.rectTransform.anchoredPosition = new Vector2((width - 145f) / 2f, -height + 27f);
        summary.rectTransform.sizeDelta = new Vector2(width - 165f, 31f);
        SetPan(artRect.anchoredPosition);
    }

    private static int Hash(string value)
    {
        unchecked { int h = 23; foreach (char ch in value) h = h * 31 + ch; return h & int.MaxValue; }
    }

    private void RebuildFog(EntityDataSO hero)
    {
        // Texture is only the CLOUD LAYER. Never punch approximate circular
        // holes; the polygon mesh above it owns every exact revealed area.
        if (!fogReady)
        {
        var colors = new Color32[FogWidth * FogHeight];
        for (int y = 0; y < FogHeight; ++y)
        for (int x = 0; x < FogWidth; ++x)
        {
            int noise = Hash((x / 4) + "-" + (y / 3)) % 36;
            colors[y * FogWidth + x] = new Color32(179, 191, 197,
                (byte)Mathf.Clamp(230 + noise / 2, 0, 255));
        }
        fogTexture.SetPixels32(colors);
        fogTexture.Apply(false);
        fogReady = true;
        }
        bool compatible = WorldMapPolygons.MatchesArtwork(artwork.sprite);
        reveal.SetSource(compatible ? artwork.sprite : null, compatible ? hero : null);
        outlines.Refresh(compatible ? hero : null, selectedMapId, StoryOrigin.GuidedDestination(hero));
    }

    public void Refresh()
    {
        if(!IsOpen)return;
        EntityDataSO hero=host.HeroData;
        if(hero==null || !WorldMapCatalog.Enabled(hero)) { Close(); return; }
        WorldMapNode current;
        if(!WorldMapCatalog.TryGet(hero.worldMapId,out current)) { Close(); return; }
        RebuildFog(hero);
        WorldMapNode selected;
        bool selectedExists = WorldMapCatalog.TryGet(selectedMapId, out selected);
        string blocked = selectedExists ? WorldMapCatalog.TravelBlockReason(hero, selectedMapId) : null;
        bool selectedAccessible = selectedExists && blocked == null;
        heading.text = "BẢN ĐỒ · " + current.Id + " · " + StoryOrigin.MapName(hero, current);
        travel.gameObject.SetActive(selectedAccessible);
        summary.text = selectedAccessible ? StoryOrigin.MapName(hero, selected) + " · " + selected.TerrainLabel +
            " · Cấp gợi ý " + selected.SuggestedLevel : selectedMapId == current.Id ? "" : blocked ?? "";
        string destination = StoryOrigin.GuidedDestination(hero);
        if (destination != null && WorldMapCatalog.TravelBlockReason(hero, destination) == null)
        {
            if (selectedMapId == destination)
                summary.text = "Đích nhiệm vụ · " + StoryOrigin.MapName(hero, selected) + "\nNhấn Tới ô đã chọn để lên đường.";
            else if (selectedMapId == current.Id || selectedAccessible || string.IsNullOrEmpty(summary.text))
                summary.text = (selectedAccessible ? StoryOrigin.MapName(hero, selected) + "\n" : "") +
                    "Chọn khu vực được đánh dấu để tiếp tục hành trình.";
        }
        if (!WorldMapPolygons.MatchesArtwork(artwork.sprite))
        {
            travel.gameObject.SetActive(false);
            summary.text = "Ảnh bản đồ chưa khớp dữ liệu lục giác.";
        }
    }
}
