#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Trial-only presentation on the existing gameplay canvas. Video cannot call Combat.
[DefaultExecutionOrder(250)]
internal sealed class HeavenlyTrialView : MonoBehaviour
{
    internal enum SkyVariant { A, B, Still }
    private const string Art = "Environment/Tribulation/";
    private const float GroundSurface = .344f; // Contact line in the supplied ground PNG.
    private static readonly Color Dark = new Color(.032f, .009f, .017f);
    private EnvironmentManager space;
    private RectTransform area, backdrop, terrain;
    private RawImage sky;
    private Texture still;
    private VideoPlayer video;
    private RenderTexture target;
    private bool hasFrame, failed, released;
    private int width, height;
    private float prepareDeadline, groundY;
    private Image seal;
    private float hitRemaining;
    private bool broken;
    internal HeavenlyLightningVisual Lightning { get; private set; }
    internal bool IsShowingVideo => hasFrame && !failed;
    internal bool IsShowingStill => sky != null && still != null && sky.texture == still;
    internal bool VideoPaused => video == null || !video.isPlaying;
    internal long VideoFrame => video == null ? -1 : video.frame;
    internal int Loops { get; private set; }
    internal float SkyTop { get; private set; }
    internal float GroundY => groundY;

    internal static HeavenlyTrialView Install(EnvironmentManager space, float groundY, SkyVariant variant)
    {
        var root = Layer<HeavenlyTrialView>(space.BattleArea, "Heavenly Trial Environment");
        root.transform.SetAsFirstSibling();
        root.space = space;
        root.area = space.BattleArea;
        root.groundY = groundY;

        var backing = Layer<Image>((RectTransform)root.transform, "Trial backdrop");
        backing.raycastTarget = false;
        backing.color = Dark;
        root.backdrop = backing.rectTransform;
        root.still = Resources.Load<Texture2D>(Art + "TribulationSkyOriginal");
        root.sky = Layer<RawImage>(root.backdrop, "Trial storm sky");
        root.sky.raycastTarget = false;
        root.sky.texture = root.still;
        root.sky.enabled = root.still != null;
        Stretch(root.sky.rectTransform);

        var ground = Layer<RawImage>((RectTransform)root.transform, "Trial ground");
        ground.raycastTarget = false;
        ground.texture = Resources.Load<Texture2D>(Art + "TribulationGround");
        ground.enabled = ground.texture != null;
        root.terrain = ground.rectTransform;
        root.terrain.pivot = new Vector2(.5f, GroundSurface);

        root.Lightning = CreateLightning(root.area, "Trial lightning");
        root.Layout();
        space.SetTrialTerrainHidden(true);
        root.StartVideo(variant);
        return root;
    }

    internal void AttachSeal(RectTransform targetRect)
    {
        seal = Layer<Image>(targetRect, "Heavenly Gate artwork");
        seal.sprite = Resources.Load<Sprite>(Art + "HeavenlyGate");
        seal.raycastTarget = false;
        seal.enabled = seal.sprite != null;
        seal.transform.SetAsFirstSibling();
        // Align the seal and base, while leaving the target rect at body size.
        seal.rectTransform.anchorMin = seal.rectTransform.anchorMax = new Vector2(.5f, 0);
        seal.rectTransform.pivot = new Vector2(.49f, .0064f);
        seal.rectTransform.anchoredPosition = new Vector2(CombatManager.TrialGateArtworkOffset, 0);
        float height = CombatManager.TrialGateArtworkHeight;
        float aspect = seal.sprite != null ? seal.sprite.rect.width / seal.sprite.rect.height : 1;
        seal.rectTransform.sizeDelta = new Vector2(height * aspect, height);
    }

    internal void HitSeal(bool lethal)
    {
        if (seal == null) return;
        broken = lethal;
        hitRemaining = lethal ? .20f : .12f;
        seal.color = lethal ? new Color(1f,.57f,.40f,.55f) : new Color(1f,.67f,.55f);
    }

    private void LateUpdate()
    {
        if (released) return;
        if (width != Screen.width || height != Screen.height) Layout();
        if (Lightning.transform.GetSiblingIndex() != area.childCount - 1)
            Lightning.transform.SetAsLastSibling();
        if (hitRemaining > 0 && (hitRemaining -= Time.deltaTime) <= 0 && !broken && seal != null)
            seal.color = Color.white;
        if (video == null || failed) return;
        if (!hasFrame && Time.realtimeSinceStartup > prepareDeadline)
        {
            if (Time.timeScale > 0) UseStill();
            else prepareDeadline = Time.realtimeSinceStartup + 5;
            return;
        }
        if (!video.isPrepared) return;
        if (Time.timeScale <= 0)
        {
            if (video.isPlaying) video.Pause();
        }
        else if (!video.isPlaying) video.Play();
    }

    private void Layout()
    {
        width = Screen.width;
        height = Screen.height;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, Vector2.zero, null, out var bottom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, new Vector2(width,height), null, out var top);
        float viewWidth = area.rect.width;
        float viewHeight = Mathf.Max(1, top.y - bottom.y);
        SkyTop = top.y;
        backdrop.sizeDelta = new Vector2(viewWidth, viewHeight + 2);
        backdrop.anchoredPosition = new Vector2(area.rect.center.x, (top.y + bottom.y) / 2);
        float aspect = viewWidth / viewHeight;
        const float source = 640f / 360f;
        float u = Mathf.Min(1, aspect / source);
        float v = Mathf.Min(1, source / aspect);
        sky.uvRect = new Rect((1-u)/2, (1-v)/2, u, v);
        terrain.sizeDelta = new Vector2(viewWidth, viewWidth * 941f / 1672);
        terrain.anchoredPosition = new Vector2(area.rect.center.x, groundY);
    }

    private void StartVideo(SkyVariant variant)
    {
        if (variant == SkyVariant.Still || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        string name = variant == SkyVariant.B ? "TribulationSky_B.mp4" : "TribulationSky_A.mp4";
        string path = Path.Combine(Application.streamingAssetsPath, "Tribulation", name);
        if (!File.Exists(path)) return;
        target = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32);
        target.Create();
        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = true;
        video.skipOnDrop = true;
        video.timeUpdateMode = VideoTimeUpdateMode.GameTime;
        video.source = VideoSource.Url;
        video.url = new Uri(path).AbsoluteUri;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.targetTexture = target;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.sendFrameReadyEvents = true;
        video.frameReady += FirstFrame;
        video.loopPointReached += OnLoop;
        video.errorReceived += OnVideoError;
        prepareDeadline = Time.realtimeSinceStartup + 5;
        video.Prepare();
    }

    private void FirstFrame(VideoPlayer source, long frame)
    {
        if (source != video || failed || released) return;
        hasFrame = true;
        source.sendFrameReadyEvents = false;
        sky.texture = target;
        sky.enabled = true;
    }

    private void OnLoop(VideoPlayer source)
    {
        if (source == video && !released) Loops++;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        if (source != video || released) return;
        Debug.LogWarning("[Trial sky] " + message);
        UseStill();
    }

    private void UseStill()
    {
        failed = true;
        sky.texture = still;
        sky.enabled = still != null;
        ReleaseVideo();
    }

    private void ReleaseVideo()
    {
        if (video != null)
        {
            video.frameReady -= FirstFrame;
            video.loopPointReached -= OnLoop;
            video.errorReceived -= OnVideoError;
            video.Stop();
            video.targetTexture = null;
            Destroy(video);
            video = null;
        }
        if (target == null) return;
        target.Release();
        Destroy(target);
        target = null;
    }

    internal void Release()
    {
        if (released) return;
        released = true;
        ReleaseVideo();
        if (Lightning != null)
        {
            Lightning.gameObject.SetActive(false);
            Destroy(Lightning.gameObject);
        }
        if (space != null) space.SetTrialTerrainHidden(false);
        gameObject.SetActive(false);
    }

    private void OnDestroy() { Release(); }

    internal static HeavenlyLightningVisual CreateLightning(RectTransform area, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(area, false);
        rect.pivot = area.pivot;
        Stretch(rect);
        var graphic = obj.AddComponent<HeavenlyLightningVisual>();
        graphic.raycastTarget = false;
        return graphic;
    }

    private static T Layer<T>(RectTransform parent, string name) where T : Component
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = parent.pivot;
        rect.anchoredPosition = Vector2.zero;
        return obj.AddComponent<T>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
#endif
