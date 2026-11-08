using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cosmetic receipts only: inventory has already received the item. Never grants,
// equips, saves or holds a monster reference; pooled actors can disappear safely.
public sealed class LootDropPresentation : MonoBehaviour
{
    [SerializeField] private Sprite swordSprite;
    [Tooltip("Kích thước icon theo pixel màn hình, giữ dễ nhìn khi đổi cỡ cửa sổ.")]
    [SerializeField, Min(8f)] private float iconSize = 40f;
    [SerializeField, Min(0f)] private float jumpHeight = 42f;
    [SerializeField, Min(.05f)] private float dropDuration = .6f;
    [SerializeField, Min(0f)] private float collectDelay = .5f;
    [SerializeField, Min(.05f)] private float collectDuration = .55f;
    [SerializeField] private RectTransform inventoryTarget;
    private const int MaxVisibleDrops = 48;
    private const float GroundOffset = 8f;
    private sealed class Drop
    {
        public RectTransform Root, Icon;
        public Image Image;
        public float Age;
        public bool Landed, Collecting;
        public Vector2 FlightStart;
    }
    private readonly List<Drop> drops = new List<Drop>();
    private readonly List<RectTransform> roots = new List<RectTransform>();
    private RectTransform area;
    private Canvas areaCanvas;
    private Canvas collectionCanvas;
    private bool missingSpriteReported;
    public IReadOnlyList<RectTransform> Roots => roots;
    public int Count => drops.Count;
    public void SetInventoryTarget(RectTransform target) { inventoryTarget=target; }

    public static LootDropPresentation Install(CombatManager owner)
    {
        var existing = owner.GetComponent<LootDropPresentation>();
        var view=existing != null ? existing : owner.gameObject.AddComponent<LootDropPresentation>();
        if(UIManager.Instance!=null)view.SetInventoryTarget(UIManager.Instance.InventoryLootTarget);
        return view;
    }

    public void Show(RectTransform battlefield, Vector2 foot, string templateId,
        string instanceId, BattleEffects effects)
    {
        // Profile A currently grants only this template. Do not depict an unknown
        // future item as a sword or silently create additional inventory items.
        if (!isActiveAndEnabled || battlefield == null ||
            templateId != EquipmentFoundation.DemoSword || string.IsNullOrEmpty(instanceId)) return;
        if (swordSprite == null) swordSprite = Resources.Load<Sprite>("Loot/iron_sword");
        if (swordSprite == null)
        {
            if (!missingSpriteReported)
                Debug.LogWarning("[LOOT VISUAL] Missing Sprite Resources/Loot/iron_sword; inventory reward is unaffected.");
            missingSpriteReported = true;
            return;
        }
        if (area != battlefield)
        {
            Clear(); area = battlefield;
            for (Transform p = area; p != null; p = p.parent)
            {
                areaCanvas = p.GetComponent<Canvas>();
                if (areaCanvas != null) break;
            }
        }
        if (drops.Count >= MaxVisibleDrops) Retire(0);

        var root = new GameObject("Dropped Sword " + instanceId, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(area, false);
        root.anchorMin = root.anchorMax = area.pivot;
        root.pivot = new Vector2(.5f, .5f);
        root.sizeDelta = Vector2.zero;
        root.anchoredPosition = foot;
        var go = new GameObject("Sword", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var icon = (RectTransform)go.transform;
        icon.SetParent(root, false);
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
        SizeIcon(icon);
        var image = go.GetComponent<Image>();
        image.sprite = swordSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        var drop = new Drop { Root = root, Icon = icon, Image = image };
        drops.Add(drop); roots.Add(root);
        Pose(drop);
        if (effects != null) effects.Burst(foot + new Vector2(0f, 18f), false, 2f / ProjectionScale);
    }

    public void Advance(float dt, BattleEffects effects)
    {
        if (area == null) { Clear(); return; }
        dt = Mathf.Max(0f, dt);
        float duration = Mathf.Max(.05f, dropDuration);
        float collectAt = duration + Mathf.Max(0f, collectDelay);
        float flightDuration = Mathf.Max(.05f, collectDuration);
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            var drop = drops[i];
            if (drop.Root == null || drop.Icon == null || drop.Image == null)
            { Retire(i); continue; }
            SizeIcon(drop.Icon, drop.Collecting);
            drop.Age = Mathf.Min(collectAt + flightDuration, drop.Age + dt);
            if (!drop.Landed)
            {
                Pose(drop);
                if (drop.Age >= duration)
                {
                    drop.Landed = true;
                    if (effects != null)
                        effects.Burst(drop.Root.anchoredPosition + new Vector2(0f, GroundOffset), false, .65f / ProjectionScale);
                }
            }
            if (drop.Age < collectAt) continue;
            if (!drop.Collecting)
            {
                // Capture the rendered center before leaving the battlefield.
                // Flight coordinates are real screen pixels: battle projection
                // resizing, camera movement and ancestor masks cannot drag it.
                drop.FlightStart = RectTransformUtility.WorldToScreenPoint(AreaCamera, drop.Icon.position);
                EnsureCollectionCanvas();
                drop.Root.SetParent(collectionCanvas.transform, false);
                drop.Root.anchorMin = drop.Root.anchorMax = Vector2.zero;
                drop.Root.anchoredPosition = drop.FlightStart;
                drop.Icon.anchoredPosition = Vector2.zero;
                drop.Collecting = true;
                SizeIcon(drop.Icon, true);
                // Leave ground depth sorting and scroll coupling during UI flight.
                roots.Remove(drop.Root);
                drop.Root.SetAsLastSibling();
            }
            float t = Mathf.Clamp01((drop.Age - collectAt) / flightDuration);
            Vector2 end = CollectionTarget();
            float ease = t * t;
            drop.Root.anchoredPosition = new Vector2(
                Mathf.Lerp(drop.FlightStart.x, end.x, ease),
                Mathf.Lerp(drop.FlightStart.y, end.y, ease) + 4f * 30f * t * (1f - t));
            float scale = Mathf.Lerp(1f, .06f, ease);
            drop.Icon.localScale = new Vector3(scale, scale, 1f);
            drop.Image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t - .8f) / .2f));
            if (drop.Age >= collectAt + flightDuration) Retire(i);
        }
    }

    private float ProjectionScale => areaCanvas != null ? Mathf.Max(.01f, areaCanvas.scaleFactor) : 1f;
    private Camera AreaCamera => areaCanvas != null && areaCanvas.renderMode != RenderMode.ScreenSpaceOverlay
        ? areaCanvas.worldCamera : null;
    private void EnsureCollectionCanvas()
    {
        if (collectionCanvas != null) return;
        var go = new GameObject("Loot Collection Canvas", typeof(RectTransform), typeof(Canvas));
        collectionCanvas = go.GetComponent<Canvas>();
        collectionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        collectionCanvas.scaleFactor = 1f;
        // Readable HUD uses 100. Keep the receipt visible through the temporary
        // right-edge destination, including an expanded management window.
        collectionCanvas.sortingOrder = Mathf.Max(101, areaCanvas != null ? areaCanvas.sortingOrder + 1 : 101);
        if (areaCanvas != null) collectionCanvas.targetDisplay = areaCanvas.targetDisplay;
    }
    private void SizeIcon(RectTransform icon, bool collecting = false)
    {
        float size = Mathf.Max(8f, iconSize) / (collecting ? 1f : ProjectionScale);
        icon.sizeDelta = new Vector2(size, size);
    }

    private Vector2 CollectionTarget()
    {
        if (inventoryTarget != null && inventoryTarget.gameObject.activeInHierarchy)
        {
            Canvas targetCanvas = null;
            for (Transform p = inventoryTarget; p != null; p = p.parent)
            {
                targetCanvas = p.GetComponent<Canvas>();
                if (targetCanvas != null) break;
            }
            var camera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(camera, inventoryTarget.TransformPoint(inventoryTarget.rect.center));
        }
        // Re-read every frame: the battle viewport can be narrower than the
        // window, and the window can resize during this flight.
        return new Vector2(Screen.width + 16f, Screen.height * .35f);
    }

    private void Pose(Drop drop)
    {
        float t = Mathf.Clamp01(drop.Age / Mathf.Max(.05f, dropDuration));
        // Initial lift -> arc -> settle. No RNG and no horizontal displacement
        // from the victim's captured foot. Root stays fixed for Y-depth sorting.
        float height = 18f * (1f - t) + 4f * Mathf.Max(0f, jumpHeight) * t * (1f - t);
        drop.Icon.anchoredPosition = new Vector2(0f, GroundOffset + height);
        drop.Icon.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(20f, -30f, t));
        float scale = 1f + .18f * (1f - t);
        drop.Icon.localScale = new Vector3(scale, scale, 1f);
        drop.Image.color = Color.white;
    }

    // Ground view displacement, NOT camera-only projectile displacement.
    public void Pan(float amount)
    {
        foreach (var drop in drops)
            if (!drop.Collecting && drop.Root != null) drop.Root.anchoredPosition += new Vector2(amount, 0f);
    }

    private void Retire(int index)
    {
        var root = drops[index].Root;
        if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
        drops.RemoveAt(index); roots.Remove(root);
    }
    public void Clear()
    {
        for (int i = drops.Count - 1; i >= 0; i--) Retire(i);
        if (collectionCanvas != null)
        {
            collectionCanvas.gameObject.SetActive(false);
            Destroy(collectionCanvas.gameObject);
            collectionCanvas = null;
        }
        area = null;
        areaCanvas = null;
    }
    void OnDisable() { Clear(); }
    void OnDestroy() { Clear(); }
}
