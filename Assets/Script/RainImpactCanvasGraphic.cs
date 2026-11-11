using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class RainImpactCanvasGraphic : MaskableGraphic
{
    internal const int Capacity = 72;
    private struct Impact
    {
        internal double X;
        internal float Y, Strength, EndTime;
        internal bool Water;
    }

    private readonly Impact[] impacts = new Impact[Capacity];
    private WorldWeatherVisual weather;
    private Rect viewport;
    private double scroll;
    private float floor, tileWidth;
    private int next;
    internal int Generation { get; private set; }
    internal int VisibleImpacts { get; private set; }
    internal int VisibleRipples { get; private set; }
    internal float PeakStrength { get; private set; }
    internal float SoundStrength { get; private set; }
    internal bool DetectImpacts => weather.Settings != null &&
        (weather.Settings.groundSplash && weather.Settings.splashIntensity > 0f || weather.Settings.waterRipples);

    internal void SetSurface(WorldWeatherVisual visual, Rect view, double groundScroll, float groundY)
    {
        weather = visual;
        viewport = view;
        floor = groundY;
        scroll = groundScroll;
        tileWidth = view.width;
    }

    internal void SetScroll(double groundScroll)
    {
        if (scroll == groundScroll) return;
        scroll = groundScroll;
        if (VisibleImpacts > 0) SetVerticesDirty();
    }

    internal float GroundAt(float battleX, out bool water)
    {
        water = false;
        float ground = floor;
        var surfaces = weather.Settings?.surfaces;
        if (surfaces == null || tileWidth <= 0f) return ground;
        float x = Mathf.Repeat((float)(battleX - scroll - viewport.xMin), tileWidth);
        bool matched = false;
        foreach (var surface in surfaces)
        {
            if (surface.mapId != weather.MapId || x < surface.horizontalRange.x || x >= surface.horizontalRange.y ||
                surface.height < viewport.yMin || surface.height >= viewport.yMax || matched && surface.height < ground) continue;
            matched = true;
            ground = surface.height;
            water = surface.water;
        }
        return ground;
    }

    internal void Emit(Vector2 battle, float strength, bool water)
    {
        if (weather.Settings == null) return;
        bool splash = weather.Settings.groundSplash && weather.Settings.splashIntensity > 0f;
        bool ripple = water && weather.Settings.waterRipples;
        if (!splash && !ripple) return;
        float now = Time.time;
        if (impacts[next].EndTime > now) return; // Cap work instead of evicting a live splash.
        impacts[next] = new Impact
        {
            X = battle.x - scroll, Y = battle.y, Strength = Mathf.Max(.1f, strength),
            EndTime = now + .35f, Water = ripple
        };
        next = (next + 1) % impacts.Length;
        SoundStrength = Mathf.Max(SoundStrength, strength);
        SetVerticesDirty();
    }

    internal float ConsumeSound()
    {
        float strength = SoundStrength;
        SoundStrength = 0f;
        return strength;
    }

    internal void Clear()
    {
        Generation++;
        System.Array.Clear(impacts, 0, impacts.Length);
        next = VisibleImpacts = VisibleRipples = 0;
        PeakStrength = SoundStrength = 0f;
        SetVerticesDirty();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal bool TryGetImpactPosition(bool water, out Vector2 battle)
    {
        foreach (var impact in impacts)
        {
            if (impact.EndTime <= Time.time || impact.Water != water) continue;
            battle = new Vector2((float)(impact.X + scroll), impact.Y);
            return true;
        }
        battle = default;
        return false;
    }
#endif

    private void LateUpdate()
    {
        if (VisibleImpacts > 0) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        VisibleImpacts = VisibleRipples = 0;
        PeakStrength = 0f;
        float now = Time.time;
        Rect bounds = rectTransform.rect;
        foreach (var impact in impacts)
        {
            if (impact.EndTime <= now) continue;
            var point = new Vector2((float)(impact.X + scroll), impact.Y) - viewport.center;
            if (!bounds.Contains(point)) continue;
            float t = 1f - (impact.EndTime - now) / .35f;
            float size = Mathf.Lerp(4.5f, 15f, Mathf.Clamp01(impact.Strength));
            var tint = (Color32)new Color(.79f, .91f, 1f, .61f * (1f - t));
            VisibleImpacts++;
            PeakStrength = Mathf.Max(PeakStrength, impact.Strength);
            if (weather.Settings != null && weather.Settings.groundSplash && weather.Settings.splashIntensity > 0f)
            {
                for (int ray = -1; ray <= 1; ray++)
                {
                    Vector2 head = point + new Vector2(ray * size * t, size * Mathf.Sin(t * Mathf.PI) * (1f - Mathf.Abs(ray) * .24f));
                    Segment(mesh, head, Vector2.Lerp(head, point, .36f), tint);
                }
            }
            if (!impact.Water) continue;
            VisibleRipples++;
            float radius = size * (.35f + t);
            for (int i = 0; i < 10; i++)
            {
                float a = i * (.2f * Mathf.PI), b = (i + 1) * (.2f * Mathf.PI);
                Vector2 aPoint = point + new Vector2(Mathf.Cos(a), .25f * Mathf.Sin(a)) * radius;
                Vector2 bPoint = point + new Vector2(Mathf.Cos(b), .25f * Mathf.Sin(b)) * radius;
                Segment(mesh, aPoint, bPoint, tint);
            }
        }
    }

    private static void Segment(VertexHelper mesh, Vector2 a, Vector2 b, Color32 tint)
    {
        Vector2 axis = (b - a).normalized;
        Vector2 edge = new Vector2(-axis.y, axis.x) * 1.15f;
        int i = mesh.currentVertCount;
        mesh.AddVert(a - edge, tint, Vector2.zero);
        mesh.AddVert(a + edge, tint, Vector2.zero);
        mesh.AddVert(b + edge, tint, Vector2.zero);
        mesh.AddVert(b - edge, tint, Vector2.zero);
        mesh.AddTriangle(i, i + 1, i + 2);
        mesh.AddTriangle(i + 2, i + 3, i);
    }
}
