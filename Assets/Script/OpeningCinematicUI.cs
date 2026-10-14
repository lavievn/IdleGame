using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 0.0.53 prototype: artwork placeholders and all animation live in Unity,
// without movie files, modifying the battle world, or advancing the save.
public sealed class OpeningCinematicUI : MonoBehaviour
{
    public const float ShowcaseCueSeconds = 27f;
    public const float EntrySeconds = 2.7f;
    private const float MenuRevealSeconds = 1.5f;

    private GameManager game;
    private StartMenuUI menu;
    private RectTransform canvasArea;
    private CanvasGroup menuFade, backdropFade;
    private float menuAlphaBeforeEntry;
    private RectTransform backdrop, sky, far, near, backCloud, cloudLeft, cloudRight;
    private RectTransform splashLeft, splashRight, title;
    private Image baseImage, leftCloudImage, rightCloudImage, splashLeftImage, splashRightImage;
    private CanvasGroup splashLeftFade, splashRightFade;
    private TextMeshProUGUI splashLeftLabel, splashRightLabel;
    private float elapsed, enterElapsed;
    private bool entering, finished;
    private OpeningArtTheme theme;

    public static OpeningCinematicUI Install(GameManager gm, StartMenuUI startMenu,
        RectTransform menuRoot, CanvasGroup menuGroup, TextMeshProUGUI font)
    {
        if (gm == null || startMenu == null || menuRoot == null) return null;
        var layer = new GameObject("Opening Cinematic - Placeholder Layers",
            typeof(RectTransform), typeof(CanvasRenderer));
        var rect = (RectTransform)layer.transform;
        rect.SetParent(menuRoot.parent, false);
        // Draw above the old HUD/ground, but below the active start buttons.
        rect.SetAsLastSibling();
        menuRoot.SetAsLastSibling();
        var cinematic = layer.AddComponent<OpeningCinematicUI>();
        cinematic.game = gm;
        cinematic.menu = startMenu;
        cinematic.canvasArea = menuRoot;
        cinematic.menuFade = menuGroup;
        cinematic.backdrop = rect;
        cinematic.backdropFade = layer.AddComponent<CanvasGroup>();
        cinematic.backdropFade.blocksRaycasts = false;
        cinematic.theme = Resources.Load<OpeningArtTheme>("OpeningArtTheme");
        cinematic.Build(font);
        cinematic.Layout();
        return cinematic;
    }

    private RectTransform MakeLayer(string name, Color color, Sprite sprite,
        TextMeshProUGUI font, string caption, out Image image, out TextMeshProUGUI label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var r = (RectTransform)go.transform;
        r.SetParent(backdrop, false);
        image = go.GetComponent<Image>();
        image.color = sprite == null ? color : Color.white;
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        label = null;
        if (font != null && sprite == null && !string.IsNullOrEmpty(caption))
        {
            var text = Instantiate(font.gameObject, r);
            text.name = "PLACEHOLDER - " + caption;
            text.SetActive(true);
            label = text.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = caption;
                label.fontSize = 14;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(1, 1, 1, .76f);
                label.raycastTarget = false;
                var tr = (RectTransform)label.transform;
                tr.anchorMin = tr.anchorMax = new Vector2(.5f,.5f);
                tr.pivot = new Vector2(.5f,.5f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = new Vector2(210, 54);
            }
        }
        return r;
    }

    private static void Place(RectTransform r, Vector2 center, Vector2 size)
    {
        if (r == null) return;
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = center;
        r.sizeDelta = size;
    }

    private void Build(TextMeshProUGUI font)
    {
        Image img; TextMeshProUGUI caption;
        var art = theme;
        sky = MakeLayer("01 SKY - full background", new Color(.23f,.48f,.59f,1f),
            art != null ? art.sky : null, font, "BẦU TRỜI / SKY", out baseImage, out caption);
        far = MakeLayer("02 FAR MOUNTAINS", new Color(.42f,.64f,.68f,1f),
            art != null ? art.farMountains : null, font, "NÚI XA / FAR MOUNTAINS", out img, out caption);
        near = MakeLayer("03 NEAR MOUNTAINS", new Color(.24f,.43f,.47f,1f),
            art != null ? art.nearMountains : null, font, "NÚI GẦN / NEAR MOUNTAINS", out img, out caption);
        backCloud = MakeLayer("04 BACK CLOUDS", new Color(.83f,.93f,.94f,.42f),
            art != null ? art.backClouds : null, font, "MÂY XA / BACK CLOUDS", out img, out caption);
        cloudLeft = MakeLayer("05 FRONT CLOUDS LEFT", new Color(.93f,.96f,1f,.84f),
            art != null ? art.leftClouds : null, font, "MÂY TRƯỚC / LEFT", out leftCloudImage, out caption);
        cloudRight = MakeLayer("06 FRONT CLOUDS RIGHT", new Color(.86f,.96f,1f,.84f),
            art != null ? art.rightClouds : null, font, "MÂY TRƯỚC / RIGHT", out rightCloudImage, out caption);
        splashLeft = MakeLayer("07 SPLASH LEFT", new Color(.38f,.32f,.56f,.95f),
            art != null ? art.splashLeft : null, font, "NHÂN VẬT TRÁI", out splashLeftImage, out splashLeftLabel);
        splashRight = MakeLayer("08 SPLASH RIGHT", new Color(.54f,.28f,.42f,.95f),
            art != null ? art.splashRight : null, font, "NHÂN VẬT PHẢI", out splashRightImage, out splashRightLabel);
        splashLeftFade = splashLeft.gameObject.AddComponent<CanvasGroup>();
        splashRightFade = splashRight.gameObject.AddComponent<CanvasGroup>();
        title = MakeLayer("09 GAME TITLE - optional", new Color(.34f,.43f,.48f,.30f),
            art != null ? art.gameTitle : null, font,
            "TÊN GAME (CHƯA CHỐT)", out img, out caption);
        // A placeholder for the future title; hidden until an actual title sprite exists.
        title.gameObject.SetActive(art != null && art.gameTitle != null);
        UpdateShowcase(0f);
    }

    public void Layout()
    {
        if (backdrop == null) return;
        float w = Mathf.Max(320f, ManagementWindowLayout.CombatPixels(Screen.width));
        float h = Mathf.Max(240f, ManagementWindowLayout.CombatHeight(Screen.height));
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.zero;
        backdrop.pivot = Vector2.zero;
        backdrop.anchoredPosition = Vector2.zero;
        backdrop.sizeDelta = new Vector2(w,h);
        // Rectangles are deliberately named for swapping art later.
        Place(sky, Vector2.zero, new Vector2(w * 1.1f, h * 1.12f));
        Place(far, new Vector2(0,-h*.13f), new Vector2(w*1.18f,h*.62f));
        Place(near, new Vector2(0,-h*.33f), new Vector2(w*1.24f,h*.34f));
        Place(backCloud, new Vector2(0,h*.16f), new Vector2(w*1.2f,h*.44f));
        Place(cloudLeft, new Vector2(-w*.3f,-h*.04f), new Vector2(w*.67f,h*.91f));
        Place(cloudRight, new Vector2(w*.3f,h*.04f), new Vector2(w*.67f,h*.91f));
        Place(splashLeft, new Vector2(-w*.37f,-h*.06f), new Vector2(w*.31f,h*.76f));
        Place(splashRight, new Vector2(w*.37f,-h*.06f), new Vector2(w*.31f,h*.76f));
        Place(title, new Vector2(0,h*.30f), new Vector2(w*.48f,h*.17f));
    }

    private static void Tint(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    // All movement remains unscaled even if the game pauses underneath.
    private void Update()
    {
        if (finished || backdrop == null) return;
        float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
        if (entering) { AnimateEntry(dt); return; }
        if (game == null || game.preGameUI == null || !game.preGameUI.activeInHierarchy) return;
        elapsed += dt;
        float t = Mathf.Clamp01((elapsed - .65f) / MenuRevealSeconds);
        if (menuFade != null) menuFade.alpha = t * t * (3f - 2f * t);
        float w = backdrop.rect.width;
        float h = backdrop.rect.height;
        backCloud.anchoredPosition = new Vector2(Mathf.Sin(elapsed*.09f)*w*.035f, h*.16f);
        cloudLeft.anchoredPosition = new Vector2(-w*.30f+Mathf.Sin(elapsed*.12f)*w*.035f,-h*.04f+Mathf.Sin(elapsed*.19f)*h*.014f);
        cloudRight.anchoredPosition = new Vector2(w*.30f+Mathf.Sin(elapsed*.11f+1.9f)*w*.032f,h*.04f+Mathf.Sin(elapsed*.16f)*h*.017f);
        far.localScale = new Vector3(1f+Mathf.Sin(elapsed*.13f)*.008f,1f+Mathf.Sin(elapsed*.13f)*.008f,1f);
        UpdateShowcase(Mathf.Clamp01((elapsed - ShowcaseCueSeconds) / 2.2f));
    }

    private void UpdateShowcase(float t)
    {
        if (splashLeft == null || splashRight == null) return;
        splashLeftFade.alpha = splashRightFade.alpha = t;
        float w = Mathf.Max(320f, backdrop.rect.width);
        float h = Mathf.Max(240f, backdrop.rect.height);
        splashLeft.anchoredPosition = new Vector2(-w*(.52f-.15f*t),-h*.06f);
        splashRight.anchoredPosition = new Vector2(w*(.52f-.15f*t),-h*.06f);
    }

    public bool IsEntering => entering;
    public void BeginEntering()
    {
        if (entering || finished) return;
        entering = true;
        enterElapsed = 0f;
        menuAlphaBeforeEntry = menuFade != null ? menuFade.alpha : 1f;
        if (menu != null) menu.LockButtonsForEntry();
        if (menuFade != null) menuFade.interactable = false;
        Debug.Log("[Opening] Transition started; gameplay ready beneath cloud layers.");
    }

    private void AnimateEntry(float dt)
    {
        enterElapsed += dt;
        float t = Mathf.Clamp01(enterElapsed / EntrySeconds);
        float eased = t * t * (3f - 2f*t);
        float w = backdrop.rect.width;
        float h = backdrop.rect.height;
        if (menuFade != null) menuFade.alpha = menuAlphaBeforeEntry * (1f - Mathf.Clamp01(t*2.2f));
        if (backdropFade != null) backdropFade.alpha = 1f - eased;
        splashLeftFade.alpha = splashRightFade.alpha = (1f-t)*(1f-t);
        splashLeft.anchoredPosition = new Vector2(-w*(.37f+.6f*eased),-h*.06f);
        splashRight.anchoredPosition = new Vector2(w*(.37f+.6f*eased),-h*.06f);
        cloudLeft.anchoredPosition = new Vector2(-w*(.3f+1.2f*eased),-h*.04f);
        cloudRight.anchoredPosition = new Vector2(w*(.3f+1.2f*eased),h*.04f);
        backCloud.anchoredPosition = new Vector2(0,h*(.16f-.6f*eased));
        // A 2D parallax/scale illusion; never scale the live combat canvas or
        // alter camera/attack coordinates (the 0.0.52 resize bug must not recur).
        float scale = 1f + .7f * eased;
        far.localScale = near.localScale = new Vector3(scale,scale,1f);
        sky.localScale = new Vector3(1f + .15f*eased,1f + .15f*eased,1f);
        var c = baseImage.color; c.a = 1f - eased; baseImage.color = c;
        if (t < 1f) return;
        finished = true;
        if (game != null && game.preGameUI != null) game.preGameUI.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.SetPaused(false);
        backdrop.gameObject.SetActive(false);
        Debug.Log("[Opening] Transition ended. Gameplay resumed.");
    }

    public void ResetForMenu()
    {
        if (backdrop == null) return;
        finished = entering = false;
        elapsed = enterElapsed = 0f;
        if (backdropFade != null) backdropFade.alpha = 1f;
        backdrop.gameObject.SetActive(true);
        backdrop.SetAsLastSibling();
        if (canvasArea != null) canvasArea.SetAsLastSibling();
        if (menuFade != null) { menuFade.alpha = 0f; menuFade.interactable = true; }
        baseImage.color = new Color(baseImage.color.r,baseImage.color.g,baseImage.color.b,1f);
        sky.localScale = far.localScale = near.localScale = Vector3.one;
        Layout();
        UpdateShowcase(0f);
    }
}
