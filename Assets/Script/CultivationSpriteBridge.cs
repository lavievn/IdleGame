using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

// W2 sprite-preview bridge. Presentation only; it never changes movement, AI or combat state.
public sealed class CultivationSpriteBridge : MonoBehaviour
{
    public enum Pose { Normal, Attack, Hit }
    [Header("Presentation only (does not resize the combat RectTransform)")]
    [Min(.01f)] public float heroScale = 1f;
    [Min(.01f)] public float monsterScale = 1f;
    public bool heroFlipHorizontal = true;
    public bool monsterFlipHorizontal = false;

    // All preview PNGs share a 900x600 canvas and feet near (300,10), measured
    // from bottom-left. 480 source pixels of body height become 80 Ground units.
    // Keep this scale across poses: wide weapons/effects must not shrink the body.
    private const float SourcePixelsPerUnit = 6f;
    private static readonly Vector2 SourceFeet = new Vector2(300f, 10f);
    // GameManager clones the active Hero visual for B2 companions. Serialize
    // these references so Unity remaps them to the clone's existing body/UI,
    // rather than creating a second body or retaining the original actor's UI.
    [SerializeField, HideInInspector] private Image sourceImage;
    [SerializeField, HideInInspector] private bool sourceWasEnabled;
    [SerializeField, HideInInspector] private Image image;
    private bool enemy;
    private string classKey;
    private Pose pose = Pose.Normal;
    private float returnAt;
    private Sprite normal, attack, hit;

    public static void AttachHero(HeroController controller)
    {
        if (controller == null || controller.heroRect == null) return;
        var bridge = controller.heroRect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) bridge = controller.heroRect.gameObject.AddComponent<CultivationSpriteBridge>();
        bridge.Init(false, ToClass(controller.attackMode));
    }
    public static void AttachMonster(MonsterController controller)
    {
        if (controller == null || controller.Rect == null) return;
        var bridge = controller.Rect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) bridge = controller.Rect.gameObject.AddComponent<CultivationSpriteBridge>();
        bridge.Init(true, ToClass(controller.attackMode));
    }
    public static void Attack(RectTransform rect)
    {
        if (rect == null) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.Show(Pose.Attack, .16f);
    }
    public static void Hit(RectTransform rect)
    {
        if (rect == null) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.Show(Pose.Hit, .2f);
    }
    private static string ToClass(AttackMode mode)
    {
        return mode == AttackMode.RangedPhysical ? "Archer" :
            mode == AttackMode.RangedMagic ? "Mage" : "Swordsman";
    }
    private void Init(bool isEnemy, string type)
    {
        if (sourceImage == null)
        {
            sourceImage = GetComponent<Image>();
            if (sourceImage == null) return;
            sourceWasEnabled = sourceImage.enabled;
        }
        if (enemy != isEnemy || classKey != type || normal == null)
        {
            ReleaseSprites();
            enemy = isEnemy;
            classKey = type;
            // Enemy PNGs are the same class/pose artwork with a baked 30% dark
            // red overlay. Keep white Image tint so that it is not applied twice.
            string prefix = enemy ? "Enemy_" : "";
            string path = "CultivationSprites/" + prefix + type;
            normal = LoadFrame(path + "_Normal");
            attack = LoadFrame(path + "_Attack");
            hit = LoadFrame(path + "_Hit");
        }
        if (normal == null)
        {
            // Missing preview assets must not hide the original actor image.
            sourceImage.enabled = sourceWasEnabled;
            if (image != null) image.enabled = false;
            return;
        }
        if (image == null)
        {
            var body = new GameObject("Cultivation Sprite Body", typeof(RectTransform), typeof(Image));
            body.layer = gameObject.layer;
            body.transform.SetParent(transform, false);
            body.transform.SetAsFirstSibling();
            image = body.GetComponent<Image>();
            image.raycastTarget = false;
        }
        // Only replace the body graphic. Existing bars/text stay on the logical
        // actor root and are never mirrored or resized by the preview bridge.
        sourceImage.enabled = false;
        image.enabled = true;
        pose = Pose.Normal;
        returnAt = 0;
        Apply();
    }
    private static Sprite LoadFrame(string path)
    {
        // Uploaded PNGs use Multiple/automatic slices, including tiny detached
        // effects. Load the texture, not an unspecified first Sprite subasset.
        var texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
    }
    private void Show(Pose requested, float duration)
    {
        if (image == null) return;
        // Hit always overrides Attack; repeated hits refresh a fixed duration.
        if (pose == Pose.Hit && requested == Pose.Attack && Time.time < returnAt) return;
        pose = requested;
        returnAt = Time.time + duration;
        Apply();
    }
    private void Update()
    {
        if (image == null) return;
        if (pose != Pose.Normal && Time.time >= returnAt)
        {
            pose = Pose.Normal;
            Apply();
        }
        ApplyLayout();
    }
    private void Apply()
    {
        if (image == null || normal == null) return;
        image.sprite = pose == Pose.Hit && hit != null ? hit :
                       pose == Pose.Attack && attack != null ? attack : normal;
        image.color = Color.white; // Enemy tint is already baked into PNG.
        image.preserveAspect = false; // rect uses the full frame's exact aspect ratio.
        ApplyLayout();
    }
    private void ApplyLayout()
    {
        if (image == null || image.sprite == null) return;
        Rect frame = image.sprite.rect;
        var body = image.rectTransform;
        body.anchorMin = body.anchorMax = new Vector2(.5f, 0f);
        body.pivot = new Vector2(SourceFeet.x / frame.width, SourceFeet.y / frame.height);
        body.anchoredPosition = Vector2.zero;
        body.sizeDelta = new Vector2(frame.width / SourcePixelsPerUnit, frame.height / SourcePixelsPerUnit);
        float scale = Mathf.Max(.01f, enemy ? monsterScale : heroScale);
        bool flip = enemy ? monsterFlipHorizontal : heroFlipHorizontal;
        body.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
    }
    private void ReleaseSprites()
    {
        if (normal != null) Destroy(normal);
        if (attack != null) Destroy(attack);
        if (hit != null) Destroy(hit);
        normal = attack = hit = null;
    }
    private void OnDestroy()
    {
        ReleaseSprites();
        if (image != null) Destroy(image.gameObject);
        if (sourceImage != null) sourceImage.enabled = sourceWasEnabled;
    }
}
