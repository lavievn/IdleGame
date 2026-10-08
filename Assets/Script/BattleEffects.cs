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
        public bool magic;
        public float size;
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
        SetVerticesDirty();
    }
    public void Clear() { bolts.Clear(); sparks.Clear(); SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (var bolt in bolts)
        {
            float x = bolt.position.x, y = bolt.position.y, s = bolt.size;
            float length = (float)Math.Sqrt(bolt.direction.x * bolt.direction.x + bolt.direction.y * bolt.direction.y);
            float dx = length > 0.001f ? bolt.direction.x / length : -1f;
            float dy = length > 0.001f ? bolt.direction.y / length : 0f;
            if (bolt.magic)
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
        foreach (var spark in sparks)
        {
            float fade = 1f - spark.age / spark.lifetime;
            Glow(vh, spark.position, spark.size * (0.4f + fade),
                spark.fire ? new Color(1f, 0.45f, 0.04f, fade) : new Color(1f, 0.95f, 0.7f, fade));
        }
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
