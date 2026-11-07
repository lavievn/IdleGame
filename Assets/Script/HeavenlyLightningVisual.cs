#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.UI;

// Fixed topology and storage. Combat supplies the accepted impact position.
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class HeavenlyLightningVisual : MaskableGraphic
{
    private const float ImpactSeconds = .23f;

    public HeavenlyLightningVisual() { useLegacyMeshGeneration = false; }

    internal int DrawnVertices { get; private set; }
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
    internal bool ProbeTiming;
    internal long ProbeMeshTicks, ProbeStrikeTicks;
    internal int ProbeMeshBuilds, ProbePeakVertices;

    internal void ResetProbeTiming()
    {
        ProbeMeshTicks = ProbeStrikeTicks = 0;
        ProbeMeshBuilds = ProbePeakVertices = 0;
    }
#endif
    private readonly Vector2[] trunk = new Vector2[10];
    private readonly Vector2[] branches = new Vector2[9];
    private readonly Vector2[] sparks = new Vector2[10];
    private Vector2 foot, impact, seal;
    private float life, warningAge, sealLife;
    private bool warning, committedBreak;
    internal void BeginCommittedBreak(Vector2 center)
    {
        BreakSeal(center);
        committedBreak = true;
    }

    private void LateUpdate()
    {
        if (!committedBreak) return;
        Advance(Time.unscaledDeltaTime);
        if (sealLife <= 0) Destroy(gameObject);
    }
    internal bool HasImpact => life > 0;
    internal bool HasWarning => warning;

    internal void Advance(float dt)
    {
        if (life <= 0 && sealLife <= 0 && !warning) return;
        life = Mathf.Max(0, life - dt);
        sealLife = Mathf.Max(0, sealLife - dt);
        if (warning) warningAge += dt;
        SetVerticesDirty();
    }

    internal void Warn(Vector2 footPosition)
    {
        if (!warning) warningAge = 0;
        warning = true;
        foot = footPosition;
        SetVerticesDirty();
    }

    internal void Strike(Vector2 body, Vector2 footPosition, float top, long id)
    {
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        long began = ProbeTiming ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
#endif
        warning = false;
        life = ImpactSeconds;
        foot = footPosition;
        impact = body;
        uint seed = unchecked((uint)id * 747796405u + 2891336453u);
        float height = Mathf.Max(80, top - body.y);
        for (int i = 0; i < trunk.Length - 1; i++)
        {
            float t = (float)i / (trunk.Length - 1);
            trunk[i] = new Vector2(body.x + RandomSigned(ref seed) * (32 + (1-t) * 65),
                body.y + height * (1-t));
        }
        trunk[trunk.Length - 1] = body;
        for (int i = 0; i < branches.Length; i += 3)
        {
            var start = trunk[1 + i / 3 * 2];
            float sign = i == 3 ? -1 : 1;
            branches[i] = start;
            branches[i+1] = start + new Vector2(sign * (42 + RandomSigned(ref seed)*20), -45);
            branches[i+2] = branches[i+1] + new Vector2(sign * (33 + RandomSigned(ref seed)*12), -40);
        }
        for (int i = 0; i < sparks.Length; i++)
            sparks[i] = new Vector2(RandomSigned(ref seed)*110, 15 + Mathf.Abs(RandomSigned(ref seed))*90);
        SetVerticesDirty();
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (began != 0) ProbeStrikeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
#endif
    }

    internal void BreakSeal(Vector2 center)
    {
        seal = center;
        sealLife = ImpactSeconds;
        SetVerticesDirty();
    }

    private static float RandomSigned(ref uint state)
    {
        state = unchecked(state * 1664525u + 1013904223u);
        return ((state >> 8) / 8388608f) - 1f;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        long began = ProbeTiming ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
#endif
        mesh.Clear();
        if (warning)
        {
            float pulse = .5f + .5f * Mathf.Sin(18 * warningAge);
            Ring(mesh, foot, 33 + pulse * 8, 10, 3, new Color(1f,.16f,.27f,.8f));
            Ring(mesh, foot, 22, 6, 1.5f, new Color(1f,.52f,.53f,.45f));
        }
        if (life > 0)
        {
            float a = life / ImpactSeconds;
            for (int i = 1; i < trunk.Length; i++)
            {
                Line(mesh,trunk[i-1],trunk[i],16,new Color(.88f,.05f,.38f,.53f*a));
                Line(mesh,trunk[i-1],trunk[i],4.2f,new Color(1f,.92f,.93f,a));
            }
            for (int i = 0; i < branches.Length; i += 3)
                for (int j = i+1; j <= i+2; j++)
                    Line(mesh,branches[j-1],branches[j],2,new Color(1f,.43f,.57f,.84f*a));
            Ring(mesh, impact, 16+38*(1-a), 11+18*(1-a), 5, new Color(1f,.65f,.68f,a));
            Ring(mesh, foot, 30+45*(1-a), 9, 3, new Color(1f,.29f,.43f,a));
            foreach (var spark in sparks)
                Line(mesh,foot+spark*(1-a),foot+spark*(1.15f-a),2.6f,new Color(1f,.78f,.66f,a));
        }
        if (sealLife > 0)
        {
            float a = sealLife / ImpactSeconds;
            var flare = new Color(1f,.57f,.36f,a);
            Ring(mesh,seal,22+53*(1-a),40+54*(1-a),4,flare);
            Line(mesh,seal+new Vector2(-9,-80),seal+new Vector2(9,80),4,flare);
        }
        DrawnVertices = mesh.currentVertCount;
#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (began != 0)
        {
            ProbeMeshTicks += System.Diagnostics.Stopwatch.GetTimestamp() - began;
            ProbeMeshBuilds++;
            ProbePeakVertices = Mathf.Max(ProbePeakVertices, DrawnVertices);
        }
#endif
    }

    private static void Ring(VertexHelper mesh, Vector2 center, float x, float y, float width, Color color)
    {
        const int segments = 18;
        Vector2 last = center + new Vector2(x, 0);
        for (int i = 1; i <= segments; i++)
        {
            float theta = i * 2 * Mathf.PI / segments;
            Vector2 p = center + new Vector2(Mathf.Cos(theta)*x, Mathf.Sin(theta)*y);
            Line(mesh,last,p,width,color);
            last = p;
        }
    }

    private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < .01f) return;
        var n = new Vector2(-d.y, d.x).normalized * (width / 2);
        int start = mesh.currentVertCount;
        mesh.AddVert(a+n,color,Vector2.zero);
        mesh.AddVert(a-n,color,Vector2.zero);
        mesh.AddVert(b-n,color,Vector2.zero);
        mesh.AddVert(b+n,color,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);
        mesh.AddTriangle(start,start+2,start+3);
    }
}
#endif
