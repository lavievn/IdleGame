using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: never scale/move the Ground coordinate system used by AI.
[DefaultExecutionOrder(200)] // Sort after combat movement, Ground pan, and spawned visuals.
public class GroundPresentation : MonoBehaviour
{
    private EnvironmentManager space;
    private RectTransform originalGrass;
    private Image sourceImage;
    private readonly List<RectTransform> distantTiles = new List<RectTransform>();
    private readonly List<RectTransform> farTiles = new List<RectTransform>();
    private Sprite generatedTreeSprite, generatedMountainSprite;
    private UnityEngine.Object generatedTreeTexture, generatedMountainTexture;
    private Color farTint, distantTint;
    private readonly List<RectTransform> tiles = new List<RectTransform>();
    private readonly List<RectTransform> decorations = new List<RectTransform>();
    private readonly List<Vector2> decorationBaseSizes = new List<Vector2>();
    private readonly List<DepthVisual> depthVisuals = new List<DepthVisual>();
    private readonly List<RectTransform> previousDepthOrder = new List<RectTransform>();
    private float phase, farPhase, distantPhase, previousWidth, previousHeight, grassHeight, grassY;
    private Color terrainTint;
    private readonly List<RectTransform> skyTiles = new List<RectTransform>();
    private Sprite plainsGround, plainsNear, plainsFar, plainsSky;
    private Sprite legacyGround, legacyNear, legacyFar;
    private bool plainsArtwork;
    public bool UsesPlainsArtwork => plainsArtwork;

    [Header("ĐỒNG BẰNG — SKY (chỉ hiển thị)")]
    [Tooltip("1 = ảnh rộng bằng BattleArea; 2.5 = 250%. Giữ tỷ lệ PNG, lặp ngang, đứng yên theo viewport.")]
    [Min(1f)] public float skyScale = 2.5f;
    [Tooltip("Độ lệch từ góc trên trái viewport, tính bằng đơn vị BattleArea.")]
    public Vector2 skyOffset = Vector2.zero;

    [Header("ARTWORK GROUND — chỉ dịch phần hình, không dịch sàn logic")]
    [Tooltip("Số pixel màn hình hạ Ground. Quy đổi qua canvas gameplay khi resize; không áp dụng cho nền thử nghiệm cũ.")]
    [Min(0f)] public float groundDropPixels = 20f;

    [Header("NỀN XA - THỊ SAI (CHỈ HIỂN THỊ)")]
    [Range(0f, 1f)] public float farParallaxSpeed = .3f;
    [Min(1f)] public float farLayerScale = 1.3f; // trees: 130% of Ground
    [Range(0f, 1f)] public float distantParallaxSpeed = .1f;
    [Min(1f)] public float distantLayerScale = 1.69f; // mountains: 130% x 130%

    [Header("VỊ TRÍ CHÂN CỦA HAI LỚP XA")]
    [Range(-.5f, 1f)] public float parallaxBaseHeightRatio = .25f;
    // Preview texture includes large transparent margins. Crop pixels, not Ground units.
    private struct DepthVisual
    {
        public RectTransform Rect;
        public float Y;
        public int StableOrder;
    }
    private static int CompareDepth(DepthVisual a, DepthVisual b)
    {
        // Larger foot Y = farther away = painted first.
        int byY = b.Y.CompareTo(a.Y);
        return byY != 0 ? byY : a.StableOrder.CompareTo(b.StableOrder);
    }
    public float FarPhase => farPhase;
    public float DistantPhase => distantPhase;
    public int DecorationCount => decorations.Count;
    public float TilePhase => phase;
    public float ScrollingHeight => grassHeight;
    public Image SourceImage => sourceImage;
    // Read-only diagnostics sampled both before and after this component's
    // order-200 layout. Phase wraps alone do not imply a visible world jump.
    public string DescribeLayoutTrace()
    {
        return " terrainLayoutSize="+TracePair(new Vector2(previousWidth,previousHeight))+
            TraceTile("terrainTile0",tiles)+TraceTile("farTile0",farTiles)+TraceTile("distantTile0",distantTiles);
    }
    private static string TraceTile(string label,List<RectTransform> layer)
    {
        if(layer.Count==0 || layer[0]==null)return " "+label+"=none";
        var rect=layer[0];
        return " "+label+"Anchor="+TracePair(rect.anchoredPosition)+
            " "+label+"World="+TracePair(rect.position)+" "+label+"Size="+TracePair(rect.rect.size);
    }
    private static string TracePair(Vector2 value)
    {
        return "("+value.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+
            value.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+")";
    }
    public static readonly Color FixedTint = new Color(0.17f, 0.17f, 0.17f, 1f);

    public static GroundPresentation Install(EnvironmentManager environment, RectTransform[] details)
    {
        var area = environment.BattleArea;
        if (area == null || environment.Width <= 0f) return null;
        var source = area.GetComponent<Image>();
        if (source == null || source.sprite == null || details == null || details.Length == 0) return null;
        RectTransform main = null;
        foreach (var d in details) if (d != null && d.gameObject.name == "Grass1") { main = d; break; }
        if (main == null) foreach (var d in details) if (d != null) { main = d; break; }
        if (main == null) return null;
        var primaryImage = main.GetComponent<Image>();
        if (primaryImage == null) return null;
        var presentation = area.gameObject.AddComponent<GroundPresentation>();
        presentation.space = environment;
        presentation.sourceImage = source;
        presentation.originalGrass = main;
        presentation.terrainTint = source.color;
        presentation.grassY = environment.Position(main).y;
        presentation.grassHeight = main.rect.height * Mathf.Abs(main.localScale.y);
        Sprite decorationSprite = primaryImage.sprite;
        // Back-to-front layering: distant SKY silhouette (10%, 169% scale),
        // near MOUNTAIN silhouette (30%, 130% scale), Grass1, then actors.
        // These are visual layers only: combat and Ground coordinates are not moved.
        // Priority: Inspector override > production Sprite from Resources
        // > the old small base64 preview > old source image.
        Sprite sky = environment.customDistantMountains;
        Sprite mountain = environment.customFarTrees;
#if UNITY_5_3_OR_NEWER
        if (sky == null) sky = Resources.Load<Sprite>("Parallax/SkyDistant");
        if (mountain == null) mountain = Resources.Load<Sprite>("Parallax/MountainNear");
#endif
        if (sky == null)
        {
            presentation.generatedMountainSprite = LoadPreviewSprite("Parallax/ParallaxMountainsTest",
                out presentation.generatedMountainTexture);
            sky = presentation.generatedMountainSprite;
        }
        if (mountain == null)
        {
            presentation.generatedTreeSprite = LoadPreviewSprite("Parallax/ParallaxTreesTest",
                out presentation.generatedTreeTexture);
            mountain = presentation.generatedTreeSprite;
        }
        presentation.distantTint = sky != null
            ? Color.white : new Color(.07f, .07f, .08f, 1f);
        presentation.farTint = mountain != null
            ? Color.white : FixedTint;
        if (sky == null) sky = source.sprite;
        if (mountain == null) mountain = source.sprite;
        presentation.legacyGround = source.sprite;
        presentation.legacyNear = mountain;
        presentation.legacyFar = sky;
        // Load once, never decode/allocate textures in the scrolling loop.
        presentation.plainsGround = Resources.Load<Sprite>("Environment/Plains/Ground");
        presentation.plainsNear = Resources.Load<Sprite>("Environment/Plains/Ground2");
        presentation.plainsFar = Resources.Load<Sprite>("Environment/Plains/Ground3");
        presentation.plainsSky = Resources.Load<Sprite>("Environment/Plains/sky");
        for (int i = 0; i < 3; i++)
        {
            var distant = presentation.CreateImage("Ground Sky Parallax " + (i + 1),
                sky, presentation.distantTint);
            distant.pivot = new Vector2(0f, area.pivot.y);
            presentation.distantTiles.Add(distant);
            distant.SetSiblingIndex(i);
        }
        for (int i = 0; i < 3; i++)
        {
            var far = presentation.CreateImage("Ground Mountain Parallax " + (i + 1),
                mountain, presentation.farTint);
            far.pivot = new Vector2(0f, area.pivot.y);
            presentation.farTiles.Add(far);
            far.SetSiblingIndex(presentation.distantTiles.Count + i);
        }
        source.enabled = false;
        primaryImage.sprite = source.sprite;
        primaryImage.type = Image.Type.Simple;
        primaryImage.preserveAspect = false;
        primaryImage.raycastTarget = false;
        main.gameObject.SetActive(true);
        main.localScale = Vector3.one;
        main.anchorMin = main.anchorMax = area.pivot;
        main.pivot = new Vector2(0f, 0.5f);
        presentation.tiles.Add(main);
        for (int i = 1; i <= 2; i++) presentation.tiles.Add(presentation.CreateImage("Grass1 Loop " + i, source.sprite, source.color));
        // Furthest mountains -> gray tree band -> Ground -> Y-sorted actors.
        for (int i = 0; i < presentation.tiles.Count; i++)
            presentation.tiles[i].SetSiblingIndex(
                presentation.distantTiles.Count + presentation.farTiles.Count + i);
        var seen = new HashSet<RectTransform>();
        foreach (var d in details) presentation.AddDecoration(d, main, decorationSprite, seen);
        foreach (var d in area.GetComponentsInChildren<RectTransform>(true))
            if (d.parent == area && d.gameObject.name != null && d.gameObject.name.StartsWith("Grass"))
                presentation.AddDecoration(d, main, decorationSprite, seen);
        if (presentation.plainsSky != null)
            for (int i = 0; i < 3; i++)
            {
                var rect = presentation.CreateImage(i == 1 ? "Sky" : "Sky Loop " + i,
                    presentation.plainsSky, Color.white);
                rect.pivot = new Vector2(0f, 1f);
                rect.SetSiblingIndex(i);
                rect.gameObject.SetActive(false);
                presentation.skyTiles.Add(rect);
            }
        presentation.RefreshTerrain();
        presentation.Reflow(true);
        return presentation;
    }
    // Source bitmaps are bundled as base64 TextAssets for this temporary preview.
    // Avoids depending on custom TextureImporter settings or unstable sprite GUIDs.
    private static Sprite LoadPreviewSprite(string resource, out UnityEngine.Object texture)
    {
        texture = null;
#if UNITY_5_3_OR_NEWER
        TextAsset asset = Resources.Load<TextAsset>(resource);
        if (asset == null) { Debug.LogWarning("Parallax preview not found: " + resource); return null; }
        try
        {
            byte[] bytes = System.Convert.FromBase64String(asset.text.Trim());
            var candidate = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!candidate.LoadImage(bytes)) { Destroy(candidate); return null; }
            candidate.wrapMode = TextureWrapMode.Clamp;
            candidate.filterMode = FilterMode.Bilinear;
            texture = candidate;
            // Only the visible silhouette should fill the target RectTransform.
            // Otherwise 50-70% of the scaled height is invisible padding and
            // both mountains and trees disappear behind the near Ground strip.
            Rect bounds = VisibleSpriteBounds(candidate);
            return Sprite.Create(candidate, bounds,
                new Vector2(.5f, 0f), 100f);
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("Cannot decode parallax preview " + resource + ": " + error.Message);
            return null;
        }
#else
        return null;
#endif
    }

#if UNITY_5_3_OR_NEWER
    private static Rect VisibleSpriteBounds(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int w = texture.width, h = texture.height;
        int left = w, bottom = h, right = -1, top = -1;
        const byte alphaThreshold = 32;
        for (int y = 0; y < h; y++)
        {
            int offset = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[offset + x].a <= alphaThreshold) continue;
                if (x < left) left = x;
                if (x > right) right = x;
                if (y < bottom) bottom = y;
                if (y > top) top = y;
            }
        }
        if (right < left || top < bottom)
            return new Rect(0f, 0f, w, h);
        // A one-pixel guard preserves antialiased tree / ridge edges.
        left = Mathf.Max(0, left - 1);
        right = Mathf.Min(w - 1, right + 1);
        bottom = Mathf.Max(0, bottom - 1);
        top = Mathf.Min(h - 1, top + 1);
        return new Rect(left, bottom, right - left + 1, top - bottom + 1);
    }
#endif

    private RectTransform CreateImage(string name, Sprite sprite, Color tint)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(space.BattleArea, false);
        rect.anchorMin = rect.anchorMax = space.BattleArea.pivot;
        rect.pivot = new Vector2(0f, 0.5f);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = tint; img.type = Image.Type.Simple;
        img.preserveAspect = false; img.raycastTarget = false;
        return rect;
    }
    private void AddDecoration(RectTransform d, RectTransform main, Sprite fallback, HashSet<RectTransform> seen)
    {
        if (d == null || d == main || tiles.Contains(d) || !seen.Add(d)) return;
        var img = d.GetComponent<Image>();
        if (img == null) return;
        bool missing = img.sprite == null;
        if (missing && fallback != null)
        {
            img.sprite = fallback; img.color = new Color(0.65f, 0.78f, 0.38f, 1f);
        }
        // Preserve assigned custom images/sizes; repair empty 10x20 placeholders.
        Vector2 size = missing ? new Vector2(65f, 26f) : new Vector2(d.rect.width * Mathf.Abs(d.localScale.x), d.rect.height * Mathf.Abs(d.localScale.y));
        d.anchorMin = d.anchorMax = space.BattleArea.pivot;
        d.pivot = new Vector2(0.5f, 0f);
        d.localScale = Vector3.one;
        img.raycastTarget = false;
        img.type = Image.Type.Simple; img.preserveAspect = false;
        d.gameObject.SetActive(true);
        decorations.Add(d); decorationBaseSizes.Add(size);
    }
    public void SetTerrainTint(Color color)
    {
        terrainTint = color;
        foreach (var rect in tiles) rect.GetComponent<Image>().color = plainsArtwork ? Color.white : color;
        foreach (var far in farTiles)
            if (far != null) far.GetComponent<Image>().color = plainsArtwork ? Color.white : farTint;
        foreach (var distant in distantTiles)
            if (distant != null) distant.GetComponent<Image>().color = plainsArtwork ? Color.white : distantTint;
    }
    public void Scroll(float amount)
    {
        RefreshTerrain();
        Reflow(false); // Also reacts to window resize while no hero is active.
        float width = space.Width;
        if (width <= 0f) return;
        phase = BattleMotion.Wrap(phase + amount, 0f, width);
        float farWidth = width * Mathf.Max(1f, farLayerScale);
        farPhase = BattleMotion.Wrap(farPhase + amount * Mathf.Clamp01(farParallaxSpeed), 0f, farWidth);
        float distantWidth = width * Mathf.Max(1f, distantLayerScale);
        distantPhase = BattleMotion.Wrap(
            distantPhase + amount * Mathf.Clamp01(distantParallaxSpeed), 0f, distantWidth);
        LayoutTiles();
        LayoutFarTiles();
        LayoutDistantTiles();
        LayoutSky();
        for (int i = 0; i < decorations.Count; i++)
        {
            var d = decorations[i]; if (d == null) continue;
            var p = space.Position(d);
            float half = d.rect.width * 0.5f;
            float min = space.BattleArea.rect.xMin - half - 12f;
            float max = space.BattleArea.rect.xMax + half + 12f;
            float next = p.x + amount;
            bool wrapped = next > max || next < min;
            p.x = BattleMotion.Wrap(next, min, max);
            if (wrapped) p.y = RandomY();
            space.SetPosition(d, p);
        }
    }
    private float RandomY() { return space.BattleArea.rect.yMin + Random.Range(0.2f, 0.75f) * space.BattleArea.rect.height; }
    private void Reflow(bool force)
    {
        float width = space.Width;
        float height = space.BattleArea.rect.height;
        if (width <= 0f || (!force &&
            Mathf.Abs(previousWidth - width) < .01f &&
            Mathf.Abs(previousHeight - height) < .01f)) return;
        if (previousWidth > 0f)
        {
            phase = phase / previousWidth * width;
            farPhase = farPhase / previousWidth * width;
            distantPhase = distantPhase / previousWidth * width;
        }
        previousWidth = width;
        previousHeight = height;
        for (int i = 0; i < decorations.Count; i++)
        {
            float size = Random.Range(0.8f, 1.15f);
            decorations[i].sizeDelta = new Vector2(decorationBaseSizes[i].x * size, decorationBaseSizes[i].y * size);
            // Each gets its own slot with jitter, never all stacked at x=0.
            float fraction = (i + Random.Range(0.2f, 0.8f)) / Mathf.Max(1, decorations.Count);
            space.SetPosition(decorations[i], new Vector2(space.BattleArea.rect.xMin + width * fraction, RandomY()));
        }
        LayoutTiles();
        LayoutFarTiles();
        LayoutDistantTiles();
        LayoutSky();
    }

    private void RefreshTerrain()
    {
        // SetupHeroInfo installs the authoritative primary Hero data before map
        // entry. Reading it also covers Travel/Load/Retry without new gameplay hooks.
        var combat = CombatManager.Instance;
        var state = combat != null ? combat.CurrentHeroCombatState : null;
        bool use = state != null && state.Data != null &&
            state.Data.mapTerrain == TuTienCore.TerrainType.DongBang &&
            plainsGround != null && plainsNear != null && plainsFar != null && plainsSky != null;
        if (use == plainsArtwork) return;
        plainsArtwork = use;
        SetLayerSprite(tiles, use ? plainsGround : legacyGround, use ? Color.white : terrainTint);
        SetLayerSprite(farTiles, use ? plainsNear : legacyNear, use ? Color.white : farTint);
        SetLayerSprite(distantTiles, use ? plainsFar : legacyFar, use ? Color.white : distantTint);
        foreach (var rect in skyTiles) rect.gameObject.SetActive(use);
        // The old small grass props belong to the preview set, not the PNG art.
        foreach (var rect in decorations) if (rect != null) rect.gameObject.SetActive(!use);
        previousDepthOrder.Clear();
        // Switching artwork must not reroll decoration positions/consume the
        // gameplay random stream. The normal resize reflow stays unchanged.
        LayoutTiles();
        LayoutFarTiles();
        LayoutDistantTiles();
        LayoutSky();
    }
    private static void SetLayerSprite(List<RectTransform> layer, Sprite sprite, Color color)
    {
        foreach (var rect in layer)
        {
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
        }
    }
    private static float AspectHeight(Sprite sprite, float width)
    {
        return width * sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
    }
    private void LayoutSky()
    {
        if (!plainsArtwork) return;
        // BattleArea is the 100-unit FLOOR, not the visible viewport. Convert
        // the compact gameplay pixel height through its stable width projection.
        float viewportHeight = space.Width * ManagementWindowLayout.CombatHeight(Screen.height) /
            Mathf.Max(1, ManagementWindowLayout.CombatPixels(Screen.width));
        float width = space.Width * Mathf.Max(1f, skyScale);
        for (int i = 0; i < skyTiles.Count; i++)
        {
            var rect = skyTiles[i];
            rect.sizeDelta = new Vector2(width + 1f, AspectHeight(plainsSky, width + 1f));
            rect.anchoredPosition = new Vector2(space.BattleArea.rect.xMin + (i - 1) * width + skyOffset.x,
                space.BattleArea.rect.yMin + viewportHeight + skyOffset.y);
        }
    }
    private void LayoutFarTiles() { LayoutParallaxTiles(farTiles, farPhase, farLayerScale); }
    private void LayoutDistantTiles() { LayoutParallaxTiles(distantTiles, distantPhase, distantLayerScale); }

    private void LayoutParallaxTiles(List<RectTransform> layer, float layerPhase, float scale)
    {
        float width = space.Width * Mathf.Max(1f, scale);
        float height = space.BattleArea.rect.height * Mathf.Max(1f, scale);
        float left = space.BattleArea.rect.xMin;
        for (int i = 0; i < layer.Count; i++)
        {
            RectTransform tile = layer[i];
            if (tile == null) continue;
            tile.sizeDelta = new Vector2(width + 1f, plainsArtwork
                ? AspectHeight(tile.GetComponent<Image>().sprite, width + 1f) : height);
            // Place the *visible* bottom of each silhouette near the back edge
            // of the main Grass strip. This stays independent of logic/world Y.
            float baselineY = grassY + grassHeight * parallaxBaseHeightRatio;
            tile.anchoredPosition = new Vector2(
                left + layerPhase + (i - 1) * width, baselineY);
        }
    }
    private void LayoutTiles()
    {
        float width = space.Width;
        for (int i = 0; i < tiles.Count; i++)
        {
            // Adjacent tiles overlap by one Ground unit to avoid subpixel cracks.
            float height = plainsArtwork ? AspectHeight(plainsGround, width + 1f) : grassHeight;
            tiles[i].sizeDelta = new Vector2(width + 1f, height);
            // Preserve the old floor bottom rather than centering a taller PNG
            // through the logical walking band. Never move/resize BattleArea.
            tiles[i].anchoredPosition = new Vector2(space.BattleArea.rect.xMin + phase + (i - 1) * width,
                plainsArtwork ? grassY - grassHeight * .5f + height * .5f - GroundDropUnits() : grassY);
            tiles[i].GetComponent<Image>().color = plainsArtwork ? Color.white : terrainTint;
        }
    }
    private float GroundDropUnits()
    {
        // Stable Battle Canvas maps logical width onto compact gameplay pixels.
        // At 600px / 1920 units, a 20px drop is 64 units, not 20 world units.
        return Mathf.Max(0f, groundDropPixels) / Mathf.Max(.0001f,
            ManagementWindowLayout.BattleCanvasScale(Screen.width, space.Width));
    }
    private void AddDepth(RectTransform rect)
    {
        if (rect == null || rect.parent != transform || !rect.gameObject.activeInHierarchy) return;
        depthVisuals.Add(new DepthVisual {
            Rect = rect, Y = space.Position(rect).y, StableOrder = depthVisuals.Count
        });
    }

    private void SortDepth()
    {
        depthVisuals.Clear();
        foreach (var rect in decorations) AddDepth(rect);
        // Include visible corpses, not only active fighters.
        foreach (var hero in HeroController.VisibleHeroes)
            if (hero != null) AddDepth(hero.heroRect);
        foreach (var monster in MonsterController.ActiveMonsters)
            if (monster != null && monster.IsAlive) AddDepth(monster.Rect);
        depthVisuals.Sort(CompareDepth);

        // Rebuild Canvas draw order only if depth relationships changed.
        bool changed = previousDepthOrder.Count != depthVisuals.Count;
        if (!changed)
            for (int i = 0; i < depthVisuals.Count; i++)
                if (previousDepthOrder[i] != depthVisuals[i].Rect) { changed = true; break; }
        if (!changed) return;

        int firstActorIndex = skyTiles.Count + distantTiles.Count + farTiles.Count + tiles.Count;
        for (int i = 0; i < depthVisuals.Count; i++)
            depthVisuals[i].Rect.SetSiblingIndex(firstActorIndex + i);
        previousDepthOrder.Clear();
        for (int i = 0; i < depthVisuals.Count; i++)
            previousDepthOrder.Add(depthVisuals[i].Rect);
    }

    void LateUpdate()
    {
        Scroll(0f);
        SortDepth();
        foreach(var hero in HeroController.ActiveHeroes)
            if(hero!=null)hero.TracePositionFrame("Ground.LateUpdate after layout/depth");
    }
    void OnDestroy()
    {
        if (sourceImage != null) sourceImage.enabled = true;
        foreach (var far in farTiles)
            if (far != null) Destroy(far.gameObject);
        foreach (var distant in distantTiles)
            if (distant != null) Destroy(distant.gameObject);
        foreach (var sky in skyTiles)
            if (sky != null) Destroy(sky.gameObject);
        foreach (var t in tiles)
            if (t != null && t != originalGrass) Destroy(t.gameObject);
        if (generatedTreeSprite != null) Destroy(generatedTreeSprite);
        if (generatedMountainSprite != null) Destroy(generatedMountainSprite);
        if (generatedTreeTexture != null) Destroy(generatedTreeTexture);
        if (generatedMountainTexture != null) Destroy(generatedMountainTexture);
    }
}
