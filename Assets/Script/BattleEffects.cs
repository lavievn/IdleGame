using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One procedural UI mesh for all projectiles and short-lived particles. The
// battlefield uses Screen Space Overlay Canvas, so world ParticleSystems would
// not share its draw order. No texture, material or prefab assignment is needed.
public class BattleEffects : MaskableGraphic
{
    public class Bolt
    {
        public Vector2 position, direction;
        public bool magic, staffEmber;
        public float size, trailLength;
    }
    private class Spark
    {
        public Vector2 position, velocity;
        public float age, lifetime, size;
        public bool fire;
    }
    private readonly List<Bolt> bolts = new List<Bolt>();
    private readonly List<Spark> sparks = new List<Spark>();
    private const int MaxSparks = 256;
    struct CastLight
    {
        public Vector2 center;
        public float progress;
    }
    struct ThrustLight
    {
        public Vector2 origin;
        public float direction, width, height, elapsed;
    }
    readonly Dictionary<RectTransform, CastLight> casts = new Dictionary<RectTransform, CastLight>();
    readonly List<RectTransform> castKeys = new List<RectTransform>();
    readonly List<ThrustLight> thrusts = new List<ThrustLight>();
    struct BowFlash
    {
        public Vector2 origin;
        public float direction, elapsed;
    }

    readonly List<BowFlash> bowFlashes = new List<BowFlash>();
    const float BowFlashSeconds = .15f;
    const float ThrustSeconds = .17f;

    internal void BowRelease(Vector2 origin, float direction)
    {
        bowFlashes.Add(new BowFlash { origin = origin, direction = direction });
        SetVerticesDirty();
    }
    internal int ActiveCasts => casts.Count;

    internal void SetCast(RectTransform actor, Vector2 center, float progress)
    {
        casts[actor] = new CastLight { center = center, progress = progress };
        SetVerticesDirty();
    }

    internal void EndCast(RectTransform actor)
    {
        if (actor != null && casts.Remove(actor)) SetVerticesDirty();
    }

    internal void Thrust(Vector2 origin, float direction, float width, float height)
    {
        thrusts.Add(new ThrustLight { origin = origin, direction = direction, width = width, height = height });
        SetVerticesDirty();
    }

    public static BattleEffects Create(RectTransform ground)
    {
        var go = new GameObject("Battle Effects", typeof(RectTransform), typeof(CanvasRenderer));
        var rect = (RectTransform)go.transform;
        rect.SetParent(ground, false);
        rect.anchorMin = rect.anchorMax = ground.pivot;
        rect.pivot = ground.pivot;
        rect.sizeDelta = ground.rect.size;
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();
        var effect = go.AddComponent<BattleEffects>();
        effect.raycastTarget = false;
        return effect;
    }

    public Bolt AddBolt(Vector2 start, bool magic, float size)
    {
        var bolt = new Bolt { position = start, direction = new Vector2(-1, 0), magic = magic, size = size };
        bolts.Add(bolt);
        SetVerticesDirty();
        return bolt;
    }
    public void RemoveBolt(Bolt bolt) { bolts.Remove(bolt); SetVerticesDirty(); }
    public void Burst(Vector2 position, bool fire, float scale)
    {
        int count = fire ? 12 : 8;
        for (int i = 0; i < count && sparks.Count < MaxSparks; i++)
        {
            float angle = (float)(i * Math.PI * 2 / count + 0.35);
            float speed = (fire ? 44f : 65f) * scale;
            sparks.Add(new Spark { position = position,
                velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                lifetime = fire ? 0.32f : 0.18f, size = (fire ? 3.5f : 2f) * scale, fire = fire });
        }
        if (sparks.Count < MaxSparks)
            sparks.Add(new Spark { position = position, lifetime = 0.12f, size = 10f * scale, fire = fire });
        SetVerticesDirty();
    }
    public void Advance(float dt)
    {
        for (int i = thrusts.Count - 1; i >= 0; i--)
        {
            var thrust = thrusts[i];
            thrust.elapsed += dt;
            if (thrust.elapsed >= ThrustSeconds) thrusts.RemoveAt(i);
            else thrusts[i] = thrust;
        }
        for (int i = bowFlashes.Count - 1; i >= 0; i--)
        {
            var flash = bowFlashes[i];
            flash.elapsed += dt;
            if (flash.elapsed >= BowFlashSeconds) bowFlashes.RemoveAt(i);
            else bowFlashes[i] = flash;
        }
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var p = sparks[i]; p.age += dt;
            if (p.age >= p.lifetime) { sparks.RemoveAt(i); continue; }
            p.position = new Vector2(p.position.x + p.velocity.x * dt, p.position.y + p.velocity.y * dt);
        }
        // Resizing the parent must not cull geometry using an outdated UI rect.
        var parent = transform.parent as RectTransform;
        if (parent != null) rectTransform.sizeDelta = parent.rect.size;
        SetVerticesDirty();
    }
    public void Pan(float amount)
    {
        foreach (var bolt in bolts) bolt.position.x += amount;
        foreach (var spark in sparks) spark.position.x += amount;
        castKeys.Clear();
        castKeys.AddRange(casts.Keys);
        foreach (var key in castKeys)
        {
            var cast = casts[key];
            cast.center.x += amount;
            casts[key] = cast;
        }
        for (int i = 0; i < thrusts.Count; i++)
        {
            var thrust = thrusts[i];
            thrust.origin.x += amount;
            thrusts[i] = thrust;
        }
        for (int i = 0; i < bowFlashes.Count; i++)
        {
            var flash = bowFlashes[i];
            flash.origin.x += amount;
            bowFlashes[i] = flash;
        }
        SetVerticesDirty();
    }
    public void Clear() { bolts.Clear(); casts.Clear(); ClearParticles(); }
    public void ClearParticles() { sparks.Clear(); thrusts.Clear(); bowFlashes.Clear(); SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (var bolt in bolts)
        {
            float x = bolt.position.x, y = bolt.position.y, s = bolt.size;
            float length = (float)Math.Sqrt(bolt.direction.x * bolt.direction.x + bolt.direction.y * bolt.direction.y);
            float dx = length > 0.001f ? bolt.direction.x / length : -1f;
            float dy = length > 0.001f ? bolt.direction.y / length : 0f;
            if (bolt.staffEmber)
            {
                for (int k = 5; k >= 1; k--)
                {
                    float fraction = k / 5f;
                    Glow(vh, new Vector2(x - dx * bolt.trailLength * fraction, y - dy * bolt.trailLength * fraction),
                        s * (1.2f - fraction * .7f), new Color(1f, .32f, .1f, .32f * (1.1f - fraction)));
                }
                Glow(vh, bolt.position, s * 1.75f, new Color(1f, .22f, .05f, .48f));
                Glow(vh, bolt.position, s, new Color(1f, .62f, .18f, 1f));
                Glow(vh, bolt.position, s * .55f, new Color(1f, .98f, .77f, 1f));
            }
            else if (bolt.magic)
            {
                for (int k = 5; k >= 1; k--)
                    Glow(vh, new Vector2(x - dx * k * s * 0.7f, y - dy * k * s * 0.7f),
                        s * (1f - k * 0.11f), new Color(1f, 0.22f + k * 0.035f, 0.015f, 0.5f - k * 0.06f));
                Glow(vh, bolt.position, s * 1.8f, new Color(1f, 0.15f, 0.01f, 0.4f));
                Glow(vh, bolt.position, s, new Color(1f, 0.55f, 0.03f, 1f));
                Glow(vh, bolt.position, s * 0.48f, new Color(1f, 0.98f, 0.65f, 1f));
            }
            else
            {
                Beam(vh, bolt.position, dx, dy, s * 8f, s * 1.5f, new Color(0.3f, 0.8f, 1f, 0.25f));
                Beam(vh, bolt.position, dx, dy, s * 6f, s * 0.45f, new Color(0.85f, 0.98f, 1f, 1f));
            }
        }
        foreach (var cast in casts.Values) DrawCast(vh, cast);
        foreach (var flash in bowFlashes) DrawBowFlash(vh,flash);
        foreach (var thrust in thrusts) DrawThrust(vh, thrust);
        foreach (var spark in sparks)
        {
            float fade = 1f - spark.age / spark.lifetime;
            Glow(vh, spark.position, spark.size * (0.4f + fade),
                spark.fire ? new Color(1f, 0.45f, 0.04f, fade) : new Color(1f, 0.95f, 0.7f, fade));
        }
    }
    static void DrawCast(VertexHelper vh, CastLight cast)
    {
        const int count = 7;
        for (int i = 0; i < count; i++)
        {
            float angle = i * (float)(Math.PI * 2 / count) + .37f;
            float x = (float)Math.Cos(angle), y = (float)Math.Sin(angle);
            float progress = Mathf.Clamp01(cast.progress * 1.15f - (i % 3) * .07f);
            float distance = Mathf.Lerp(77f + 8f * (i % 3), 9f, progress);
            var point = cast.center + new Vector2(x, y * .8f) * distance;
            Beam(vh, point, -x, -.8f * y, 29f, 6f, new Color(.26f,.70f,1f,.40f));
            Beam(vh, point, -x, -.8f * y, 24f, 2.4f, new Color(.92f,1f,1f,.96f));
        }
    }

    static void DrawBowFlash(VertexHelper vh, BowFlash flash)
    {
        float t = flash.elapsed / BowFlashSeconds;
        float fade = 1f - t;
        Glow(vh,flash.origin,26f,new Color(.66f,.88f,1f,.50f * fade));
        for (int i = -2; i <= 2; i++)
        {
            float angle = i * .27f;
            var direction = new Vector2(flash.direction * Mathf.Cos(angle),Mathf.Sin(angle));
            var center = flash.origin + direction * (14f + 15f * t);
            Beam(vh,center,direction.x,direction.y,31f,6f,new Color(.49f,.77f,1f,.52f * fade));
            Beam(vh,center,direction.x,direction.y,26f,2.8f,new Color(1f,1f,1f,fade));
        }
    }

    static void DrawThrust(VertexHelper vh, ThrustLight thrust)
    {
        float fade = 1f - thrust.elapsed / ThrustSeconds;
        DrawSpear(vh, thrust, 1f, new Color(.33f,.81f,1f,.40f * fade));
        DrawSpear(vh, thrust, .58f, new Color(.66f,.90f,1f,.90f * fade));
        DrawSpear(vh, thrust, .23f, new Color(1f,1f,1f,fade));
    }

    static void DrawSpear(VertexHelper vh, ThrustLight t, float band, Color color)
    {
        float dx = t.width * t.direction;
        float h = t.height * .5f * band;
        int i = vh.currentVertCount;
        vh.AddVert(t.origin, new Color(color.r,color.g,color.b,0f), Vector2.zero);
        vh.AddVert(t.origin + new Vector2(dx * .24f, h), color, Vector2.zero);
        vh.AddVert(t.origin + new Vector2(dx, 0f), color, Vector2.zero);
        vh.AddVert(t.origin + new Vector2(dx * .24f, -h), color, Vector2.zero);
        vh.AddTriangle(i, i+1, i+2);
        vh.AddTriangle(i, i+2, i+3);
    }

    private static void Glow(VertexHelper vh, Vector2 p, float radius, Color color)
    {
        int index = vh.currentVertCount;
        vh.AddVert(p, color, Vector2.zero);
        var edge = new Color(color.r, color.g, color.b, 0f);
        const int segments = 12;
        for (int i = 0; i < segments; i++)
        {
            float a = (float)(i * Math.PI * 2 / segments);
            vh.AddVert(new Vector2(p.x + (float)Math.Cos(a) * radius, p.y + (float)Math.Sin(a) * radius), edge, Vector2.zero);
        }
        for (int i = 0; i < segments; i++) vh.AddTriangle(index, index + 1 + i, index + 1 + (i + 1) % segments);
    }
    private static void Beam(VertexHelper vh, Vector2 p, float dx, float dy, float length, float width, Color color)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector2(p.x + dx * length * 0.5f, p.y + dy * length * 0.5f), color, Vector2.zero);
        vh.AddVert(new Vector2(p.x - dy * width, p.y + dx * width), color, Vector2.zero);
        vh.AddVert(new Vector2(p.x - dx * length, p.y - dy * length), new Color(color.r, color.g, color.b, 0f), Vector2.zero);
        vh.AddVert(new Vector2(p.x + dy * width, p.y - dx * width), color, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
}

// Runtime-created UI beside the existing HP bar; no scene/prefab rewiring needed.
public sealed class MagicChargeBar
{
    public RectTransform Root { get; private set; }
    public Image Fill { get; private set; }
    public static MagicChargeBar Create(RectTransform actor, Image hpFill)
    {
        var go = new GameObject("Magic Charge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var bar = new MagicChargeBar { Root = (RectTransform)go.transform };
        RectTransform hp = hpFill != null ? hpFill.transform as RectTransform : null;
        bar.Root.SetParent(hp != null ? hp.parent : actor, false);
        bar.Root.anchorMin = hp != null ? hp.anchorMin : new Vector2(.5f, 1f);
        bar.Root.anchorMax = hp != null ? hp.anchorMax : new Vector2(.5f, 1f);
        bar.Root.pivot = hp != null ? hp.pivot : new Vector2(.5f, .5f);
        bar.Root.anchoredPosition = (hp != null ? hp.anchoredPosition : Vector2.zero) + new Vector2(0f, 12f);
        bar.Root.sizeDelta = new Vector2(hp != null ? hp.rect.width : Mathf.Max(40f, actor.rect.width), 5f);
        var background = go.GetComponent<Image>();
        background.color = new Color(.02f, .08f, .16f, .9f); background.raycastTarget = false;
        var fillGO = new GameObject("Charge Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)fillGO.transform; rect.SetParent(bar.Root, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, .5f); rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
        bar.Fill = fillGO.GetComponent<Image>(); bar.Fill.raycastTarget = false;
        bar.Fill.color = new Color(.1f, .55f, 1f, 1f);
        bar.Set(false, 0f);
        return bar;
    }
    public void Set(bool visible, float progress)
    {
        progress = visible ? Mathf.Clamp01(progress) : 0f;
        Fill.fillAmount = progress;
        ((RectTransform)Fill.transform).anchorMax = new Vector2(progress, 1f);
        Root.gameObject.SetActive(visible);
    }
}
