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
    private readonly RectTransform viewport;
    private readonly TextMeshProUGUI zoomLabel;
    private readonly RawImage fog;
    private readonly WorldMapRevealedGraphic reveal;
    private readonly WorldMapOutlineGraphic outlines;
    private string selectedMapId = "";
    private readonly TextMeshProUGUI heading;
    private readonly TextMeshProUGUI summary;
    private readonly RectTransform[] choices = new RectTransform[8];
    private readonly TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[8];
    private readonly string[] choiceIds = new string[8];
    private readonly List<string> eligible = new List<string>();
    private Texture2D fogTexture;
    private bool previousPause;
    private bool mapPointerHeld;
    private bool hasPanned;
    private Vector2 pointerStart;
    private Vector2 lastPointer;
    private float zoom = 1f;
    private const float MinZoom = 1f;
    private const float MaxZoom = 14f;
    private const float PanDeadzone = 6f;
    private const float DefaultNodeWidth = .4f;
    private string cachedFogVisits = "";
    private int page;
    private readonly RectTransform navigation;
    private const int FogWidth = 256;
    private const int FogHeight = 192;

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

        var viewportGO = new GameObject("MapViewport", typeof(RectTransform),
            typeof(RectMask2D));
        viewport = (RectTransform)viewportGO.transform;
        viewport.SetParent(root, false);
        viewport.anchorMin = viewport.anchorMax = new Vector2(.5f, 1f);
        viewport.pivot = new Vector2(.5f, 1f);
        viewport.anchoredPosition = new Vector2(0f, -48f);
        viewport.sizeDelta = new Vector2(605f, 400f);

        var artGO = new GameObject("Artwork", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        artwork = artGO.GetComponent<Image>();
        var artRect = (RectTransform)artGO.transform;
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
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
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

        // Zoom toolbar stays outside the clipped artwork to remain interactive.
        owner.SmallButton(root,"WorldMapZoomOut","−",new Vector2(0f,1f),
            new Vector2(0f,1f),new Vector2(14f,-49f),new Vector2(34f,28f),
            ()=>ZoomAroundViewportCenter(1f / 1.25f));
        owner.SmallButton(root,"WorldMapZoomIn","+",new Vector2(0f,1f),
            new Vector2(0f,1f),new Vector2(52f,-49f),new Vector2(34f,28f),
            ()=>ZoomAroundViewportCenter(1.25f));
        owner.SmallButton(root,"WorldMapZoomCurrent","Về vị trí",new Vector2(0f,1f),
            new Vector2(0f,1f),new Vector2(148f,-49f),new Vector2(95f,28f),
            FocusCurrentMap);
        zoomLabel = owner.MakeLabel(root,"100%",new Vector2(111f,-63f),
            new Vector2(54f,24f));
        zoomLabel.alignment = TextAlignmentOptions.Center;
        zoomLabel.fontSize = 12f;

        owner.SmallButton(root,"WorldMapClose","Đóng",new Vector2(1f,1f),
            new Vector2(1f,1f),new Vector2(-14f,-14f),new Vector2(90f,32f),Close);
        owner.SmallButton(root,"WorldMapTravelSelected","Tới ô đã chọn",new Vector2(1f,0f),
            new Vector2(1f,0f),new Vector2(-14f,171f),new Vector2(130f,28f),
            TravelSelected);
        navigation = (RectTransform)new GameObject("WorldMapRoutes",typeof(RectTransform)).transform;
        navigation.SetParent(root,false);
        navigation.anchorMin = new Vector2(1f,0f);
        navigation.anchorMax = new Vector2(1f,0f);
        navigation.pivot = new Vector2(1f,0f);
        navigation.anchoredPosition = new Vector2(-12f,12f);
        navigation.sizeDelta = new Vector2(410f,150f);
        for(int i=0;i<8;i++)
        {
            int index = i;
            choices[i] = owner.SmallButton(navigation,"MapNeighbor"+i,"",
                new Vector2(0f,1f),new Vector2(0f,1f),
                new Vector2((i%2)*205f, -(i/2)*30f),new Vector2(201f,28f),
                ()=>Choose(index));
            choiceLabels[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if(choiceLabels[i]!=null)
            {
                choiceLabels[i].fontSize = 10f;
                choiceLabels[i].enableWordWrapping = false;
                choiceLabels[i].overflowMode = TextOverflowModes.Ellipsis;
            }
            choices[i].gameObject.SetActive(false);
        }
        root.gameObject.SetActive(false);
    }

    public static WorldMapPanelUI Install(UIManager owner, Transform parent)
    {
        GameManager host = Object.FindFirstObjectByType<GameManager>();
        return owner == null || parent == null || host == null
            ? null : new WorldMapPanelUI(owner,parent,host);
    }

    public bool IsOpen => root != null && root.gameObject.activeSelf;
    public bool PanIsCaptured => mapPointerHeld;
    public RectTransform Root => root;
    public bool HitMapSurface(Vector2 screenPoint) =>
        IsOpen && viewport != null &&
        RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, null);

    private Vector2 ToViewport(Vector2 screen)
    {
        Vector2 local;
        // anchoredPosition uses the viewport CENTER, not its top-center pivot.
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, screen, null, out local)
            ? local - viewport.rect.center : Vector2.zero;
    }

    public bool BeginPan(Vector2 screen)
    {
        if (!HitMapSurface(screen)) return false;
        mapPointerHeld = true;
        hasPanned = false;
        pointerStart = lastPointer = screen;
        return true;
    }

    public void ContinuePan(Vector2 screen)
    {
        if (!mapPointerHeld) return;
        if (!hasPanned && (screen - pointerStart).sqrMagnitude >=
            PanDeadzone * PanDeadzone) hasPanned = true;
        if (hasPanned)
        {
            RectTransform art = (RectTransform)artwork.transform;
            art.anchoredPosition += ToViewport(screen) - ToViewport(lastPointer);
            ClampPan();
        }
        lastPointer = screen;
    }

    public void EndPan(Vector2 screen)
    {
        if (!mapPointerHeld) return;
        ContinuePan(screen);
        mapPointerHeld = false;
        if (!hasPanned && HitMapSurface(screen)) HandleMapClick(screen);
    }

    public void CancelPan()
    {
        // Native Windows may lose the final pointer coordinate while the
        // window loses focus. Never treat (0,0) as a genuine drag position.
        mapPointerHeld = false;
        hasPanned = false;
    }

    public void ZoomAt(Vector2 screen, float scrollY)
    {
        if (!HitMapSurface(screen) || Mathf.Abs(scrollY) < .001f) return;
        float steps = Mathf.Abs(scrollY) >= 15f ? scrollY / 120f : scrollY;
        SetZoom(zoom * Mathf.Pow(1.2f, steps), ToViewport(screen));
    }

    private void ZoomAroundViewportCenter(float multiplier)
        => SetZoom(zoom * multiplier, Vector2.zero);

    private void SetZoom(float newZoom, Vector2 focus)
    {
        newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        if (Mathf.Abs(newZoom - zoom) < .00001f) return;
        RectTransform art = (RectTransform)artwork.transform;
        art.anchoredPosition = focus + (art.anchoredPosition - focus) * (newZoom / zoom);
        zoom = newZoom;
        art.localScale = Vector3.one * zoom;
        ClampPan();
        UpdateZoomLabel();
    }

    private void UpdateZoomLabel()
    {
        if (zoomLabel != null) zoomLabel.text = Mathf.RoundToInt(zoom * 100f) + "%";
    }

    private void ClampPan()
    {
        RectTransform art = (RectTransform)artwork.transform;
        float boundX = Mathf.Max(0f, (art.rect.width * zoom - viewport.rect.width) * .5f);
        float boundY = Mathf.Max(0f, (art.rect.height * zoom - viewport.rect.height) * .5f);
        art.anchoredPosition = new Vector2(
            Mathf.Clamp(art.anchoredPosition.x, -boundX, boundX),
            Mathf.Clamp(art.anchoredPosition.y, -boundY, boundY));
    }

    private void FocusCurrentMap()
    {
        if (host == null || host.HeroData == null) return;
        var shape = WorldMapPolygons.Find(host.HeroData.worldMapId);
        if (shape == null || !WorldMapPolygons.ValidPolygon(shape.vertices))
        {
            // Omitted polygons are deliberate (TM-03 / TM-13). Center on their
            // catalog seed instead, without fabricating clickable geometry.
            WorldMapNode node;
            var fallbackArt = (RectTransform)artwork.transform;
            if (WorldMapCatalog.TryGet(host.HeroData.worldMapId, out node))
            {
                const float typicalCellWidth = .065f; // visual fallback, not geometry
                zoom = Mathf.Clamp(viewport.rect.width * DefaultNodeWidth /
                    Mathf.Max(.0001f, typicalCellWidth * fallbackArt.rect.width),
                    MinZoom, MaxZoom);
                fallbackArt.localScale = Vector3.one * zoom;
                Vector2 center = node.PreviewPosition;
                fallbackArt.anchoredPosition = -new Vector2(
                    (center.x - .5f) * fallbackArt.rect.width,
                    (center.y - .5f) * fallbackArt.rect.height) * zoom;
                ClampPan();
            }
            else
            {
                zoom = MinZoom;
                fallbackArt.localScale = Vector3.one;
                fallbackArt.anchoredPosition = Vector2.zero;
            }
            UpdateZoomLabel();
            return;
        }
        float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
        foreach (Vector2 p in shape.vertices)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }
        var art = (RectTransform)artwork.transform;
        // 40% of the viewport WIDTH for the current cell's bounding box.
        zoom = Mathf.Clamp(viewport.rect.width * DefaultNodeWidth /
            Mathf.Max(.0001f, (maxX - minX) * art.rect.width), MinZoom, MaxZoom);
        art.localScale = Vector3.one * zoom;
        Vector2 center = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
        art.anchoredPosition = -new Vector2((center.x - .5f) * art.rect.width,
            (center.y - .5f) * art.rect.height) * zoom;
        ClampPan();
        UpdateZoomLabel();
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
        string id = WorldMapPolygons.HitTest(position, candidate =>
        {
            WorldMapNode current;
            return (hero.worldMapVisited != null && hero.worldMapVisited.Contains(candidate)) ||
                (WorldMapCatalog.TryGet(hero.worldMapId, out current) &&
                 WorldMapCatalog.IsCleared(hero, current.Id) &&
                 current.Links.Contains(candidate));
        });
        if (!string.IsNullOrEmpty(id))
        {
            selectedMapId = id;
            Refresh();
        }
        else summary.text = WorldMapPolygons.Load().polygons.Count == 0
            ? "Chưa có tọa độ polygon: mở Tools > Bach Luyen Tien Do > World Map Polygon Editor (.54)."
            : "Vùng chưa khám phá hoặc chưa có polygon. Hoàn thành 5 wave để mở hướng đi kề.";
        return true;
    }

    private void TravelSelected()
    {
        if (string.IsNullOrEmpty(selectedMapId)) return;
        if (host.TryTravelToWorldMap(selectedMapId)) Close();
        else Refresh();
    }

    public void Toggle() { if(IsOpen) Close(); else Open(); }

    public void Open()
    {
        if(IsOpen || host == null || !host.IsGameplayHUDVisible) return;
        if(!WorldMapCatalog.Enabled(host.HeroData)) return;
        previousPause=owner.IsPaused;
        owner.SetPaused(true);
        page=0;
        selectedMapId = host.HeroData.worldMapId;
        Resize();
        root.gameObject.SetActive(true);
        FocusCurrentMap();
        root.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if(!IsOpen) return;
        root.gameObject.SetActive(false);
        mapPointerHeld = false;
        owner.SetPaused(previousPause);
    }

    public void Resize()
    {
        if (root == null) return;
        float width = Mathf.Max(340f, Mathf.Min(Screen.width - 12f, 1070f));
        float height = Mathf.Max(260f, Mathf.Min(Screen.height - 12f, 650f));
        float oldW = viewport.rect.width, oldH = viewport.rect.height;
        Vector2 oldCenter = ((RectTransform)artwork.transform).anchoredPosition;
        root.sizeDelta = new Vector2(width, height);

        float aspect = artwork.sprite != null && artwork.sprite.rect.height > 0f
            ? artwork.sprite.rect.width / artwork.sprite.rect.height : 1.5f;
        float viewWidth = Mathf.Max(100f, width - 24f);
        float viewHeight = Mathf.Max(100f, height - 175f);
        viewport.sizeDelta = new Vector2(viewWidth, viewHeight);
        // Fit entire original artwork at zoom=1; scaling this child keeps the
        // artwork, fog, outline and nation labels in the same coordinate space.
        float fit = Mathf.Min(viewWidth / aspect, viewHeight);
        var art = (RectTransform)artwork.transform;
        art.sizeDelta = new Vector2(fit * aspect, fit);
        if (IsOpen && oldW > 0f && oldH > 0f)
        {
            art.anchoredPosition = new Vector2(
                oldCenter.x * viewWidth / oldW, oldCenter.y * viewHeight / oldH);
            ClampPan();
        }
        else art.anchoredPosition = Vector2.zero;
        ((RectTransform)summary.transform).anchoredPosition =
            new Vector2(Mathf.Min(270f, width * .36f), -height + 73f);
    }

    private static float SegmentDistancePixels(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return (p - (a + t * ab)).magnitude;
    }

    private static void OpenFogAround(Color32[] colors, IList<Vector2> vertices,
        float width, float height)
    {
        if (!WorldMapPolygons.ValidPolygon(vertices)) return;
        // Fog extends 20 pixels beyond visited borders, blending into the
        // unexplored area without unlocking neighboring map selections.
        const float rim = 20f;
        float xmin = width, xmax = 0f, ymin = height, ymax = 0f;
        foreach (var v in vertices)
        {
            xmin = Mathf.Min(xmin, v.x * width);
            xmax = Mathf.Max(xmax, v.x * width);
            ymin = Mathf.Min(ymin, v.y * height);
            ymax = Mathf.Max(ymax, v.y * height);
        }
        int x0 = Mathf.Max(0, Mathf.FloorToInt((xmin - rim) * FogWidth / width));
        int x1 = Mathf.Min(FogWidth - 1, Mathf.CeilToInt((xmax + rim) * FogWidth / width));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((ymin - rim) * FogHeight / height));
        int y1 = Mathf.Min(FogHeight - 1, Mathf.CeilToInt((ymax + rim) * FogHeight / height));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            Vector2 position = new Vector2((x + .5f) * width / FogWidth,
                (y + .5f) * height / FogHeight);
            Vector2 normalized = new Vector2(position.x / width, position.y / height);
            float distance = float.MaxValue;
            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector2 a = Vector2.Scale(vertices[i], new Vector2(width, height));
                Vector2 b = Vector2.Scale(vertices[(i + 1) % vertices.Count],
                    new Vector2(width, height));
                distance = Mathf.Min(distance, SegmentDistancePixels(position, a, b));
            }
            bool inside = WorldMapPolygons.Contains(vertices, normalized);
            // Smooth outskirts; no extra polygon becomes marked visited.
            float t = inside ? 0f : Mathf.Clamp01(distance / rim);
            t = t * t * (3f - 2f * t);
            int index = y * FogWidth + x;
            Color32 c = colors[index];
            c.a = (byte)Mathf.Min(c.a, Mathf.RoundToInt(240f * t));
            colors[index] = c;
        }
    }

    private void RebuildFog(EntityDataSO hero)
    {
        bool compatible = WorldMapPolygons.MatchesArtwork(artwork.sprite);
        string key = compatible && hero.worldMapVisited != null
            ? string.Join("|", hero.worldMapVisited) : "incompatible";
        if (key != cachedFogVisits)
        {
            cachedFogVisits = key;
            var colors = new Color32[FogWidth * FogHeight];
            for (int i = 0; i < colors.Length; ++i)
                colors[i] = new Color32(179, 191, 197, 240);
            if (compatible && hero.worldMapVisited != null)
            {
                var doc = WorldMapPolygons.Load();
                foreach (string id in hero.worldMapVisited)
                {
                    var polygon = WorldMapPolygons.Find(id);
                    if (polygon != null)
                        OpenFogAround(colors, polygon.vertices,
                            doc.artworkWidth, doc.artworkHeight);
                }
            }
            fogTexture.SetPixels32(colors);
            fogTexture.Apply(false);
        }
        reveal.SetSource(compatible ? artwork.sprite : null, compatible ? hero : null);
        outlines.Refresh(compatible ? hero : null, selectedMapId);
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
        bool selectedAccessible = selectedExists &&
            WorldMapCatalog.TravelBlockReason(hero, selectedMapId) == null;
        bool clear=WorldMapCatalog.IsCleared(hero,current.Id);
        string progress = clear ? "Đã vượt qua" :
            "Wave tối thiểu: "+hero.worldMapWaveProgress+"/5";
        heading.text="BẢN ĐỒ · "+current.Id+" · "+current.Name;
        summary.text="Đang ở: "+current.Name+" ("+current.TerrainLabel+")\n"+
            progress+"  |  Wave hiện tại: "+hero.worldMapLoopWave+"/5\n"+
            (!WorldMapPolygons.MatchesArtwork(artwork.sprite)
                ? "Chưa đúng ảnh 1448×1086 để dùng vùng click; danh sách map kề vẫn hoạt động.\n"
                : "") +
            (selectedExists ? "Đã chọn: "+selected.Id+" – "+selected.Name+
                (selectedAccessible ? " · Có thể đi" : " · Chưa đi được") :
                (clear ? "Chọn lục giác cạnh bên để di chuyển (đồ thị tạm)." :
                    "Chưa vượt qua map: đường đi kế tiếp vẫn ẩn."));
        // The country list and exact polygon masks are deliberately EMPTY until
        // approved nation IDs, labels, centers and hex vertices are supplied.
        eligible.Clear();
        if(clear) eligible.AddRange(current.Links);
        eligible.Sort(System.StringComparer.Ordinal);
        int start=page*choices.Length;
        if(start>=eligible.Count) { page=0;start=0; }
        for(int i=0;i<choices.Length;i++)
        {
            int n=start+i;
            bool visible=n<eligible.Count;
            choices[i].gameObject.SetActive(visible);
            choiceIds[i]=visible?eligible[n]:"";
            if(!visible)continue;
            WorldMapNode destination;
            if(!WorldMapCatalog.TryGet(choiceIds[i],out destination))continue;
            string marker=destination.IsBoss?" [BOSS]":"";
            if(choiceLabels[i]!=null)choiceLabels[i].text=destination.Id+" "+destination.Name+" Lv."+destination.SuggestedLevel+marker;
        }
    }

    private void Choose(int buttonIndex)
    {
        if(buttonIndex<0||buttonIndex>=choiceIds.Length)return;
        string id=choiceIds[buttonIndex];
        if(string.IsNullOrEmpty(id))return;
        if(host.TryTravelToWorldMap(id))Close();
        else Refresh();
    }
}
