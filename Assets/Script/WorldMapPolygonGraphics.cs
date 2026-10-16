using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

// Renders the original map texture ONLY inside polygons the hero has visited.
// The full opaque fog Image behind this Graphic covers every unvisited point,
// including polygons that have not been traced yet.
public sealed class WorldMapRevealedGraphic : MaskableGraphic
{
    private Sprite mapArt;
    private EntityDataSO hero;

    public override Texture mainTexture => mapArt != null ? mapArt.texture : s_WhiteTexture;

    public void SetSource(Sprite source, EntityDataSO currentHero)
    {
        mapArt = source;
        hero = currentHero;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (mapArt == null || !WorldMapCatalog.Enabled(hero) ||
            hero.worldMapVisited == null) return;
        Rect area = rectTransform.rect;
        Rect atlas = mapArt.textureRect;
        Texture tex = mapArt.texture;
        if (tex == null || area.width <= 0f || area.height <= 0f) return;

        foreach (var id in hero.worldMapVisited)
        {
            var polygon = WorldMapPolygons.Find(id);
            if (polygon == null || !WorldMapPolygons.ValidSixVertices(polygon.vertices)) continue;
            int start = vh.currentVertCount;
            for (int i = 0; i < polygon.vertices.Count; ++i)
            {
                Vector2 p = polygon.vertices[i];
                var vertex = UIVertex.simpleVert;
                vertex.color = Color.white;
                vertex.position = new Vector3(area.xMin + p.x * area.width,
                    area.yMin + p.y * area.height, 0f);
                vertex.uv0 = new Vector2((atlas.xMin + p.x * atlas.width) / tex.width,
                    (atlas.yMin + p.y * atlas.height) / tex.height);
                vh.AddVert(vertex);
            }
            var indices = WorldMapPolygons.Triangulate(polygon.vertices);
            for (int i = 0; i + 2 < indices.Count; i += 3)
                vh.AddTriangle(start + indices[i], start + indices[i + 1],
                    start + indices[i + 2]);
        }
    }
}

public sealed class WorldMapOutlineGraphic : MaskableGraphic
{
    private EntityDataSO hero;
    private string selectedId = "";
    public void Refresh(EntityDataSO currentHero, string chosenId)
    {
        hero = currentHero;
        selectedId = chosenId ?? "";
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (!WorldMapCatalog.Enabled(hero)) return;
        WorldMapNode current;
        if (!WorldMapCatalog.TryGet(hero.worldMapId, out current)) return;
        bool allowedToExplore = WorldMapCatalog.IsCleared(hero, current.Id);
        foreach (var shape in WorldMapPolygons.Load().polygons)
        {
            if (shape == null || !WorldMapPolygons.ValidSixVertices(shape.vertices)) continue;
            bool selected = shape.mapId == selectedId;
            bool adjacent = allowedToExplore && current.Links.Contains(shape.mapId);
            if (!selected && !adjacent) continue;
            // Pale border exposes possible destinations, not the covered image.
            Color stroke = selected ? new Color(1f, .85f, .23f, .98f) :
                new Color(.38f, .87f, 1f, .67f);
            for (int i = 0; i < shape.vertices.Count; i++)
            {
                Vector2 a = shape.vertices[i], b = shape.vertices[(i + 1) % shape.vertices.Count];
                AddLine(vh, a, b, selected ? 2.5f : 1.25f, stroke);
            }
        }
    }

    private void AddLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color tint)
    {
        Rect rect = rectTransform.rect;
        Vector2 a = new Vector2(rect.xMin + from.x * rect.width,
            rect.yMin + from.y * rect.height);
        Vector2 b = new Vector2(rect.xMin + to.x * rect.width,
            rect.yMin + to.y * rect.height);
        Vector2 delta = b - a;
        if (delta.sqrMagnitude < 1f) return;
        Vector2 perpendicular = new Vector2(-delta.y, delta.x).normalized * (thickness * .5f);
        int start = vh.currentVertCount;
        var vertex = UIVertex.simpleVert;
        vertex.color = tint;
        vertex.position = a - perpendicular; vh.AddVert(vertex);
        vertex.position = a + perpendicular; vh.AddVert(vertex);
        vertex.position = b + perpendicular; vh.AddVert(vertex);
        vertex.position = b - perpendicular; vh.AddVert(vertex);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}