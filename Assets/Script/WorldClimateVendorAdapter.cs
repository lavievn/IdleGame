using UnityEngine;
using UnityEngine.UI;

// Composites the licensed cubemaps and the Rain Maker 2D emitter into the
// existing battle Canvas. Neither vendor asset owns a camera or gameplay.
public sealed class WorldClimateVendorAdapter : WorldClimateAssetAdapter
{
    private const float SkyUpdatesPerSecond = 18f;
    internal const float RainWorldHeight = 8f;
    internal const int MaxRainParticles = 420;

    private WorldClimateVendorAssets assets;
    private Material projection;
    private RenderTexture projectedSky;
    private float nextSkyUpdate;
    private float skyScroll, lastSkyScroll;

    internal override void SetSkyScroll(float viewportWidths) => skyScroll = viewportWidths;
    private Vector4 lastSkyMix;
    private Cubemap lastTwilight;
    private ParticleSystem emitter;
    private GameObject rainSimulation;
    private RectTransform rainLayer;
    private RainMakerCanvasGraphic rainGraphic;
    private float maxRainRate, rainWorldWidth;
    private float lastRainRate;

    internal int ActiveParticles => emitter != null ? emitter.particleCount : 0;
    internal RainMakerCanvasGraphic RainGraphic => rainGraphic;

    internal static WorldClimateVendorAdapter TryInstall(GameObject owner)
    {
        var configuration = Resources.Load<WorldClimateVendorAssets>(WorldClimateVendorAssets.ResourcePath);
        if (configuration == null) return null;
        var adapter = owner.AddComponent<WorldClimateVendorAdapter>();
        adapter.assets = configuration;
        return adapter;
    }

    public override bool TryGetProjectedSky(WorldClimateFrame frame, Rect viewport, out Texture texture)
    {
        texture = null;
        if (assets == null || assets.clearSky == null || assets.cloudySky == null ||
            assets.dawnSky == null || assets.duskSky == null) return false;
        if (projection == null)
        {
            var shader = Resources.Load<Shader>("WorldClimateSkyProjection");
            if (shader == null || !shader.isSupported || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
                return false;
            projection = new Material(shader) { hideFlags = HideFlags.DontSave };
            projection.SetTexture("_ClearSky", assets.clearSky);
            projection.SetTexture("_CloudySky", assets.cloudySky);
        }

        float aspect = viewport.width / viewport.height;
        int width = Mathf.Min(640, Mathf.Max(1, ManagementWindowLayout.CombatPixels(Screen.width)));
        int height = Mathf.Clamp(Mathf.RoundToInt(width / aspect), 1, 480);
        bool resized = projectedSky == null || !projectedSky.IsCreated() ||
            projectedSky.width != width || projectedSky.height != height;
        if (resized)
        {
            ReleaseProjection();
            projectedSky = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "BG Studio HDR projection",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            if (!projectedSky.Create()) { ReleaseProjection(); return false; }
        }

        Vector4 mix = SkyMix(frame);
        Cubemap twilight = frame.Hour < 12f ? assets.dawnSky : assets.duskSky;
        if (resized || (Time.unscaledTime >= nextSkyUpdate && (mix != lastSkyMix || twilight != lastTwilight || skyScroll != lastSkyScroll)))
        {
            projection.SetTexture("_TwilightSky", twilight);
            projection.SetVector("_SkyMix", mix);
            projection.SetFloat("_Aspect", aspect);
            projection.SetFloat("_SkyScroll", skyScroll);
            lastSkyScroll = skyScroll;
            Graphics.Blit(null, projectedSky, projection);
            lastSkyMix = mix;
            lastTwilight = twilight;
            nextSkyUpdate = Time.unscaledTime + 1f / SkyUpdatesPerSecond;
        }
        texture = projectedSky;
        return true;
    }

    private static Vector4 SkyMix(WorldClimateFrame frame)
    {
        float warm = 0f;
        float night = 0f;
        switch (frame.Phase)
        {
            case WorldDayPhase.Dawn:
                night = 1f - Smooth((frame.Hour - 5f) / 1.5f);
                warm = frame.Hour < 6.5f ? 1f - night : 1f - Smooth((frame.Hour - 6.5f) / 1.5f);
                break;
            case WorldDayPhase.Dusk:
                night = Smooth((frame.Hour - 18.5f) / 1.5f);
                warm = frame.Hour < 18.5f ? Smooth((frame.Hour - 17f) / 1.5f) : 1f - night;
                break;
            case WorldDayPhase.Night:
                night = 1f;
                break;
        }
        float cloudy = Mathf.Clamp01(frame.Cloud * .9f + frame.Fog * .2f);
        return new Vector4(warm, night, cloudy, 1f - frame.Cloud * .27f - frame.Fog * .15f);
    }

    private static float Smooth(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }

    public override void SetRain2D(float intensity, RectTransform battleArea, Rect viewport)
    {
        if (assets == null || assets.rainEmitter2D == null || assets.rainTexture == null) return;
        if (emitter == null) CreateRain(battleArea);
        if (rainLayer.parent != battleArea)
        {
            rainLayer.SetParent(battleArea, false);
            rainLayer.anchorMin = rainLayer.anchorMax = battleArea.pivot;
        }
        if (rainLayer.anchoredPosition != viewport.center) rainLayer.anchoredPosition = viewport.center;
        if (rainLayer.sizeDelta != viewport.size) rainLayer.sizeDelta = viewport.size;
        if (rainLayer.GetSiblingIndex() != battleArea.childCount - 1) rainLayer.SetAsLastSibling();

        float worldWidth = RainWorldHeight * viewport.width / viewport.height;
        bool resized = !Mathf.Approximately(worldWidth, rainWorldWidth);
        if (resized)
        {
            var shape = emitter.shape;
            shape.scale = new Vector3(worldWidth * 1.16f, .03f, .03f);
            rainWorldWidth = worldWidth;
        }
        float rate = maxRainRate * Mathf.Clamp01(intensity);
        if (lastRainRate != rate)
        {
            var emission = emitter.emission;
            emission.rateOverTime = rate;
            lastRainRate = rate;
        }
        if (!emitter.isPlaying || resized)
        {
            // Prime the visible window after a resize instead of waiting for
            // the newly exposed edges to fill from the top.
            emitter.Simulate(.6f, false, true, true);
            emitter.Play(false);
        }
        if (!rainLayer.gameObject.activeSelf) rainLayer.gameObject.SetActive(true);
    }

    private void CreateRain(RectTransform battleArea)
    {
        // Instantiate only the authored RainFall child of RainPrefab2D, not
        // the controller that attaches to Camera.main or creates audio.
        rainSimulation = new GameObject("Rain Maker 2D simulation") { hideFlags = HideFlags.DontSave };
        rainSimulation.transform.SetParent(transform, true);
        rainSimulation.SetActive(false);
        emitter = Instantiate(assets.rainEmitter2D, rainSimulation.transform, false);
        emitter.transform.localPosition = new Vector3(0, RainWorldHeight * .58f, 0);
        emitter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        emitter.transform.localScale = Vector3.one;
        foreach (var behaviour in emitter.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (var renderer in emitter.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var source in emitter.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
        var main = emitter.main;
        main.playOnAwake = false;
        main.prewarm = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = MaxRainParticles;
        main.startSpeed = new ParticleSystem.MinMaxCurve(12f, 20f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(.75f, 1.1f);
        main.gravityModifier = 0f;
        var shape = emitter.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        var collision = emitter.collision;
        collision.enabled = false;
        var velocity = emitter.velocityOverLifetime;
        velocity.enabled = false;
        var external = emitter.externalForces;
        external.enabled = false;
        var emission = emitter.emission;
        maxRainRate = Mathf.Clamp(emission.rateOverTime.constant, 220f, 360f);
        emission.enabled = true;
        emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
        rainSimulation.SetActive(true);

        var layer = new GameObject("Rain Maker 2D viewport", typeof(RectTransform), typeof(RectMask2D));
        layer.SetActive(false);
        rainLayer = (RectTransform)layer.transform;
        rainLayer.SetParent(battleArea, false);
        rainLayer.anchorMin = rainLayer.anchorMax = battleArea.pivot;
        var visual = new GameObject("Rain Maker 2D drops", typeof(RectTransform), typeof(RainMakerCanvasGraphic));
        rainGraphic = visual.GetComponent<RainMakerCanvasGraphic>();
        var rect = rainGraphic.rectTransform;
        rect.SetParent(rainLayer, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rainGraphic.Configure(emitter, assets.rainTexture);
    }

    public override void StopRain2D()
    {
        if (emitter != null) emitter.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (rainLayer != null) rainLayer.gameObject.SetActive(false);
    }

    private void OnDisable() => StopRain2D();

    private void OnDestroy()
    {
        StopRain2D();
        if (rainLayer != null) Destroy(rainLayer.gameObject);
        if (rainSimulation != null) Destroy(rainSimulation);
        if (projection != null) Destroy(projection);
        ReleaseProjection();
    }

    private void ReleaseProjection()
    {
        if (projectedSky == null) return;
        projectedSky.Release();
        Destroy(projectedSky);
        projectedSky = null;
    }
}
