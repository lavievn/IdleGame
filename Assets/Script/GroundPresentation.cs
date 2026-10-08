using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: never scale/move the Ground coordinate system used by AI.
public class GroundPresentation : MonoBehaviour
{
    private EnvironmentManager space;
    private RectTransform fixedRect, originalGrass;
    private Image sourceImage;
    private readonly List<RectTransform> tiles = new List<RectTransform>();
    private readonly List<RectTransform> decorations = new List<RectTransform>();
    private readonly List<Vector2> decorationBaseSizes = new List<Vector2>();
    private float phase, previousWidth, grassHeight, grassY;
    private Color terrainTint;
    public int DecorationCount => decorations.Count;
    public float TilePhase => phase;
    public float ScrollingHeight => grassHeight;
    public Image SourceImage => sourceImage;
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
        // A visual clone grows independently; the root stays the logical width.
        presentation.fixedRect = presentation.CreateImage("Ground Fixed 1.2", source.sprite, FixedTint);
        presentation.fixedRect.pivot = area.pivot;
        presentation.fixedRect.SetAsFirstSibling();
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
        // Keep every terrain strip behind the decoration/actor objects.
        for (int i = 0; i < presentation.tiles.Count; i++) presentation.tiles[i].SetSiblingIndex(i + 1);
        var seen = new HashSet<RectTransform>();
        foreach (var d in details) presentation.AddDecoration(d, main, decorationSprite, seen);
        foreach (var d in area.GetComponentsInChildren<RectTransform>(true))
            if (d.parent == area && d.gameObject.name != null && d.gameObject.name.StartsWith("Grass"))
                presentation.AddDecoration(d, main, decorationSprite, seen);
        presentation.Reflow(true);
        return presentation;
    }
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
        foreach (var rect in tiles) rect.GetComponent<Image>().color = color;
        if (fixedRect != null) fixedRect.GetComponent<Image>().color = FixedTint;
    }
    public void Scroll(float amount)
    {
        Reflow(false); // Also reacts to window resize while no hero is active.
        float width = space.Width;
        if (width <= 0f) return;
        phase = BattleMotion.Wrap(phase + amount, 0f, width);
        LayoutTiles();
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
        if (width <= 0f || (!force && Mathf.Abs(previousWidth - width) < 0.01f)) return;
        if (previousWidth > 0f) phase = phase / previousWidth * width;
        previousWidth = width;
        fixedRect.sizeDelta = new Vector2(width * 1.2f, space.BattleArea.rect.height * 1.2f);
        fixedRect.anchoredPosition = Vector2.zero;
        for (int i = 0; i < decorations.Count; i++)
        {
            float size = Random.Range(0.8f, 1.15f);
            decorations[i].sizeDelta = new Vector2(decorationBaseSizes[i].x * size, decorationBaseSizes[i].y * size);
            // Each gets its own slot with jitter, never all stacked at x=0.
            float fraction = (i + Random.Range(0.2f, 0.8f)) / Mathf.Max(1, decorations.Count);
            space.SetPosition(decorations[i], new Vector2(space.BattleArea.rect.xMin + width * fraction, RandomY()));
        }
        LayoutTiles();
    }
    private void LayoutTiles()
    {
        float width = space.Width;
        for (int i = 0; i < tiles.Count; i++)
        {
            // Adjacent tiles overlap by one Ground unit to avoid subpixel cracks.
            tiles[i].sizeDelta = new Vector2(width + 1f, grassHeight);
            tiles[i].anchoredPosition = new Vector2(space.BattleArea.rect.xMin + phase + (i - 1) * width, grassY);
            tiles[i].GetComponent<Image>().color = terrainTint;
        }
    }
    void LateUpdate() { Scroll(0f); }
    void OnDestroy()
    {
        if (sourceImage != null) sourceImage.enabled = true;
        if (fixedRect != null) Destroy(fixedRect.gameObject);
        foreach (var t in tiles) if (t != null && t != originalGrass) Destroy(t.gameObject);
    }
}
