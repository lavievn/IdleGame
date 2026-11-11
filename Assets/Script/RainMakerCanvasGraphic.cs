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
    private RainImpactCanvasGraphic impacts;
    private Rect viewport;
    private float intensity, dropSize = WorldWeatherPresentationSettings.DefaultDropSize, splashIntensity = 1f;
    private int liveCount;

    public override Texture mainTexture => drops != null ? drops : base.mainTexture;
    internal int VisibleDrops { get; private set; }
    internal int OccupiedColumns { get; private set; }
    internal float AverageWind { get; private set; }

    internal void Configure(ParticleSystem source, Texture2D texture, RainImpactCanvasGraphic impactGraphic)
    {
        emitter = source;
        drops = texture;
        impacts = impactGraphic;
        raycastTarget = false;
        color = new Color(.73f, .87f, 1f, .83f);
        SetMaterialDirty();
    }

    internal void SetPresentation(Rect view, float strength, WorldWeatherPresentationSettings settings)
    {
        viewport = view;
        intensity = strength;
        dropSize = settings != null ? settings.dropSize : WorldWeatherPresentationSettings.DefaultDropSize;
        splashIntensity = settings != null ? settings.splashIntensity : 1f;
    }

    private void LateUpdate()
    {
        if (emitter == null) return;
        liveCount = emitter.GetParticles(particles);
        if (impacts != null && impacts.DetectImpacts && Time.deltaTime > 0f && liveCount > 0) CollideWithGround();
        SetVerticesDirty();
    }

    private void CollideWithGround()
    {
        float scale = rectTransform.rect.height / WorldClimateVendorAdapter.RainWorldHeight;
        bool changed = false;
        for (int i = 0; i < liveCount; i++)
        {
            var particle = particles[i];
            Vector2 current = (Vector2)particle.position * scale + viewport.center;
            float ground = impacts.GroundAt(current.x, particle.randomSeed, out bool water);
            if (current.y > ground) continue;
            Vector2 movement = (Vector2)particle.totalVelocity * (Time.deltaTime * scale);
            if (movement.y < 0f && current.y - movement.y > ground)
            {
                float fraction = (current.y - ground) / movement.y;
                float impactX = current.x - movement.x * fraction;
                if (impactX >= viewport.xMin && impactX < viewport.xMax)
                {
                    float force = Mathf.Clamp01(Mathf.Abs(particle.totalVelocity.y) / 25f) *
                        Mathf.Lerp(.35f, 1.3f, intensity) * splashIntensity;
                    impacts.Emit(new Vector2(impactX, ground), force, water);
                }
            }
            particle.remainingLifetime = -1f;
            particles[i] = particle;
            changed = true;
        }
        if (changed) emitter.SetParticles(particles, liveCount);
    }

    protected override void OnDisable()
    {
        VisibleDrops = 0;
        OccupiedColumns = 0;
        liveCount = 0;
        AverageWind = 0f;
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
        float horizontal = 0f;
        for (int i = 0; i < liveCount; i++)
        {
            var particle = particles[i];
            if (particle.remainingLifetime <= 0f) continue;
            Vector2 position = (Vector2)particle.position * scale;
            float size = Mathf.Lerp(.68f, 1.18f, intensity) * dropSize;
            float length = Mathf.Lerp(.22f, .46f, (particle.randomSeed & 255) / 255f) * scale * size;
            if (position.x < bounds.xMin - length || position.x > bounds.xMax + length ||
                position.y < bounds.yMin - length || position.y > bounds.yMax + length) continue;
            Vector2 forward = ((Vector2)particle.totalVelocity).normalized;
            if (forward == Vector2.zero) forward = Vector2.down;
            Vector2 across = new Vector2(-forward.y, forward.x) * (.022f * scale * size);
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
                horizontal += particle.totalVelocity.x;
                int column = Mathf.Min(3, (int)(4f * (position.x - bounds.xMin) / bounds.width));
                OccupiedColumns |= 1 << column;
            }
        }
        AverageWind = VisibleDrops > 0 ? horizontal / VisibleDrops : 0f;
    }
}
