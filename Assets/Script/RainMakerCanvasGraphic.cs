using UnityEngine;
using UnityEngine.UI;

// A single clipped UI mesh paints the Rain Maker texture over the battle
// viewport, independent of URP 2D scene lighting and below the HUD Canvas.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RainMakerCanvasGraphic : MaskableGraphic
{
    private readonly ParticleSystem.Particle[] particles =
        new ParticleSystem.Particle[WorldClimateVendorAdapter.MaxRainParticles];
    private ParticleSystem emitter;
    private Texture2D drops;

    public override Texture mainTexture => drops != null ? drops : base.mainTexture;
    internal int VisibleDrops { get; private set; }
    internal int OccupiedColumns { get; private set; }

    internal void Configure(ParticleSystem source, Texture2D texture)
    {
        emitter = source;
        drops = texture;
        raycastTarget = false;
        color = new Color(.73f, .87f, 1f, .83f);
        SetMaterialDirty();
    }

    private void LateUpdate() => SetVerticesDirty();

    protected override void OnDisable()
    {
        VisibleDrops = 0;
        OccupiedColumns = 0;
        base.OnDisable();
    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        VisibleDrops = 0;
        OccupiedColumns = 0;
        if (emitter == null) return;
        Rect bounds = rectTransform.rect;
        if (bounds.width <= 0f || bounds.height <= 0f) return;
        float scale = bounds.height / WorldClimateVendorAdapter.RainWorldHeight;
        int count = emitter.GetParticles(particles);
        for (int i = 0; i < count; i++)
        {
            var particle = particles[i];
            Vector2 position = (Vector2)particle.position * scale;
            float length = Mathf.Lerp(.22f, .46f, (particle.randomSeed & 255) / 255f) * scale;
            if (position.x < bounds.xMin - length || position.x > bounds.xMax + length ||
                position.y < bounds.yMin - length || position.y > bounds.yMax + length) continue;
            Vector2 forward = new Vector2(particle.velocity.x - 1.6f, particle.velocity.y).normalized;
            if (forward == Vector2.zero) forward = Vector2.down;
            Vector2 across = new Vector2(-forward.y, forward.x) * (.022f * scale);
            Vector2 top = position - forward * (length * .5f);
            Vector2 bottom = position + forward * (length * .5f);
            var tint = (Color32)color;
            int index = vertices.currentVertCount;
            vertices.AddVert(bottom - across, tint, Vector2.zero);
            vertices.AddVert(top - across, tint, Vector2.up);
            vertices.AddVert(top + across, tint, Vector2.one);
            vertices.AddVert(bottom + across, tint, Vector2.right);
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index + 2, index + 3, index);
            if (bounds.Contains(position))
            {
                VisibleDrops++;
                int column = Mathf.Min(3, (int)(4f * (position.x - bounds.xMin) / bounds.width));
                OccupiedColumns |= 1 << column;
            }
        }
    }
}
