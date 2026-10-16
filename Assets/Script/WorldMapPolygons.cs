using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuTienCore
{
    [Serializable]
    public sealed class WorldMapPolygon
    {
        public string mapId = "";
        // Image-space coordinates, bottom-left (0,0), top-right (1,1).
        public List<Vector2> vertices = new List<Vector2>(6);
    }

    [Serializable]
    public sealed class WorldMapPolygonDocument
    {
        public int version = 1;
        public List<WorldMapPolygon> polygons = new List<WorldMapPolygon>();
    }

    // Geometry is kept independent of RectTransform, Canvas and any PNG.
    public static class WorldMapPolygons
    {
        private static WorldMapPolygonDocument cached;

        public static WorldMapPolygonDocument Load()
        {
            if (cached != null) return cached;
            var text = Resources.Load<TextAsset>("WorldMapPolygons");
            cached = text == null ? new WorldMapPolygonDocument() :
                JsonUtility.FromJson<WorldMapPolygonDocument>(text.text);
            if (cached == null) cached = new WorldMapPolygonDocument();
            if (cached.polygons == null) cached.polygons = new List<WorldMapPolygon>();
            return cached;
        }

        public static void InvalidateCache() { cached = null; }

        public static bool Contains(IList<Vector2> polygon, Vector2 point)
        {
            if (polygon == null || polygon.Count < 3) return false;
            // Include points on polygon edges so neighboring hit regions have no gaps.
            const float epsilon = 0.000001f;
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[j], b = polygon[i];
                Vector2 ab = b - a, ap = point - a;
                float cross = ab.x * ap.y - ab.y * ap.x;
                float dot = ap.x * ab.x + ap.y * ab.y;
                if (Mathf.Abs(cross) < epsilon && dot >= -epsilon &&
                    dot <= ab.sqrMagnitude + epsilon) return true;
                if ((a.y > point.y) == (b.y > point.y)) continue;
                float xIntersection = a.x + (point.y - a.y) *
                    (b.x - a.x) / (b.y - a.y);
                if (point.x < xIntersection) inside = !inside;
            }
            return inside;
        }

        public static bool ValidSixVertices(IList<Vector2> points)
        {
            if (points == null || points.Count != 6) return false;
            float areaTwice = 0f;
            for (int i = 0; i < 6; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % 6];
                if (float.IsNaN(a.x) || float.IsNaN(a.y) ||
                    a.x < 0f || a.y < 0f || a.x > 1f || a.y > 1f ||
                    (a - b).sqrMagnitude < .000001f) return false;
                areaTwice += a.x * b.y - b.x * a.y;
            }
            if (Mathf.Abs(areaTwice) < .00001f) return false;
            // Reject crossed hexes, which would break the UI triangle fan.
            for (int i = 0; i < 6; i++)
            for (int j = i + 1; j < 6; j++)
            {
                if (j == i + 1 || (i == 0 && j == 5)) continue;
                if (Intersects(points[i], points[(i + 1) % 6],
                    points[j], points[(j + 1) % 6])) return false;
            }
            return true; // Concave six-sided maps are supported.
        }

        // Ear clipping supports irregular and concave six-sided shapes.
        public static List<int> Triangulate(IList<Vector2> points)
        {
            var triangles = new List<int>();
            if (!ValidSixVertices(points)) return triangles;
            float signedArea = 0f;
            for (int i = 0; i < points.Count; i++)
                signedArea += Cross(points[i], points[(i + 1) % points.Count]);
            int winding = signedArea > 0f ? 1 : -1;
            var remaining = new List<int>();
            for (int i = 0; i < points.Count; i++) remaining.Add(i);
            int guard = 0;
            while (remaining.Count > 3 && guard++ < 24)
            {
                bool removed = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int ai = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int bi = remaining[i];
                    int ci = remaining[(i + 1) % remaining.Count];
                    Vector2 a = points[ai], b = points[bi], c = points[ci];
                    if (Cross(b - a, c - a) * winding <= .0000001f) continue;
                    bool occupied = false;
                    foreach (int other in remaining)
                    {
                        if (other == ai || other == bi || other == ci) continue;
                        if (InsideTriangle(points[other], a, b, c, winding))
                        { occupied = true; break; }
                    }
                    if (occupied) continue;
                    triangles.Add(ai); triangles.Add(bi); triangles.Add(ci);
                    remaining.RemoveAt(i);
                    removed = true;
                    break;
                }
                if (!removed) { triangles.Clear(); return triangles; }
            }
            if (remaining.Count != 3) { triangles.Clear(); return triangles; }
            triangles.Add(remaining[0]); triangles.Add(remaining[1]); triangles.Add(remaining[2]);
            return triangles;
        }

        private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b,
            Vector2 c, int winding)
        {
            const float epsilon = .0000001f;
            return Cross(b - a, p - a) * winding >= -epsilon &&
                   Cross(c - b, p - b) * winding >= -epsilon &&
                   Cross(a - c, p - c) * winding >= -epsilon;
        }

        private static bool Intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float c1 = Cross(b - a, c - a), c2 = Cross(b - a, d - a);
            float c3 = Cross(d - c, a - c), c4 = Cross(d - c, b - c);
            return c1 * c2 < 0f && c3 * c4 < 0f;
        }

        private static float Cross(Vector2 a, Vector2 b)
            => a.x * b.y - a.y * b.x;

        public static WorldMapPolygon Find(string id)
        {
            foreach (var polygon in Load().polygons)
                if (polygon != null && polygon.mapId == id) return polygon;
            return null;
        }

        public static string HitTest(Vector2 normalizedPosition,
            Predicate<string> isVisible = null)
        {
            if (normalizedPosition.x < 0f || normalizedPosition.x > 1f ||
                normalizedPosition.y < 0f || normalizedPosition.y > 1f) return null;
            foreach (var polygon in Load().polygons)
                if (polygon != null && ValidSixVertices(polygon.vertices) &&
                    (isVisible == null || isVisible(polygon.mapId)) &&
                    Contains(polygon.vertices, normalizedPosition)) return polygon.mapId;
            return null;
        }

        public static Vector2 NormalizedPoint(RectTransform imageRect,
            Vector2 screenPosition, Camera uiCamera = null)
        {
            Vector2 local;
            if (imageRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRect, screenPosition, uiCamera, out local)) return new Vector2(-1f, -1f);
            Rect area = imageRect.rect;
            if (area.width <= 0 || area.height <= 0) return new Vector2(-1f, -1f);
            return new Vector2((local.x - area.xMin) / area.width,
                (local.y - area.yMin) / area.height);
        }
    }
}