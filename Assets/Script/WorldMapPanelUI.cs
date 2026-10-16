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
    private readonly RectTransform[] choices = new RectTransform[8];
    private readonly TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[8];
    private readonly string[] choiceIds = new string[8];
    private readonly List<string> eligible = new List<string>();
    private Texture2D fogTexture;
    private bool previousPause;
    private int page;
    private readonly RectTransform navigation;
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

        var artGO = new GameObject("Artwork", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        artwork = artGO.GetComponent<Image>();
        var artRect = (RectTransform)artGO.transform;
        artRect.SetParent(root, false);
        artRect.anchorMin = artRect.anchorMax = new Vector2(.5f,1f);
        artRect.pivot = new Vector2(.5f,1f);
        artRect.anchoredPosition = new Vector2(0f,-48f);
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
    public RectTransform Root => root;
    public bool HitMapSurface(Vector2 screenPoint) =>
        IsOpen && artwork != null &&
        RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)artwork.transform, screenPoint, null);

    // Hit against the exact six-corner region. Selection never auto-travels.
    public bool HandleMapClick(Vector2 screenPoint)
    {
        if (!HitMapSurface(screenPoint)) return false;
        EntityDataSO hero = host.HeroData;
        if (hero == null || !WorldMapCatalog.Enabled(hero)) return true;
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
            ? "Chưa có tọa độ lục giác: mở Tools > Bach Luyen Tien Do > World Map Polygon Editor (.54)."
            : "Map chưa khám phá hoặc chưa có viền 6 đỉnh. Hoàn thành 5 wave để mở hướng đi kề.";
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
        root.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if(!IsOpen) return;
        root.gameObject.SetActive(false);
        owner.SetPaused(previousPause);
    }

    public void Resize()
    {
        if(root==null)return;
        float width = Mathf.Max(340f,Mathf.Min(Screen.width-12f,1070f));
        float height = Mathf.Max(260f,Mathf.Min(Screen.height-12f,650f));
        root.sizeDelta = new Vector2(width,height);
        var imageRect=(RectTransform)artwork.transform;
        // Exact image aspect is required: otherwise six-vertex hit regions
        // drift away from the visible polygons under preserveAspect.
        float aspect = artwork.sprite != null && artwork.sprite.rect.height > 0f
            ? artwork.sprite.rect.width / artwork.sprite.rect.height : 1.5f;
        float imageHeight=Mathf.Max(100f,height-175f);
        float imageWidth=Mathf.Min(width-24f,imageHeight*aspect);
        imageHeight=Mathf.Min(imageHeight,imageWidth/aspect);
        imageRect.sizeDelta=new Vector2(imageWidth,imageHeight);
        ((RectTransform)summary.transform).anchoredPosition=new Vector2(Mathf.Min(270f,width*.36f),-height+73f);
    }

    private static int Hash(string value)
    {
        unchecked { int h = 23; foreach (char ch in value) h = h * 31 + ch; return h & int.MaxValue; }
    }

    private void RebuildFog(EntityDataSO hero)
    {
        // Texture is only the CLOUD LAYER. Never punch approximate circular
        // holes; the polygon mesh above it owns every exact revealed area.
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
        reveal.SetSource(artwork.sprite, hero);
        outlines.Refresh(hero, selectedMapId);
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
