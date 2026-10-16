#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TuTienCore;

// Trace directly on the user's existing map image; NO raster image is generated.
// Menu: Tools > Bach Luyen Tien Do > World Map Polygon Editor (.54)
public sealed class WorldMapPolygonEditorWindow : EditorWindow
{
    private const string OutputPath = "Assets/Resources/WorldMapPolygons.json";
    private const string CatalogPath = "Assets/Resources/WorldMapCatalog.txt";

    private Texture2D mapTexture;
    private WorldMapPolygonDocument document;
    private readonly List<string> ids = new List<string>();
    private int selectedIndex;
    private bool showAll = true;
    private bool snapToNeighbors = true;
    private bool isDragging;
    private int dragVertex = -1;
    private string status = "Chọn đúng ảnh bản đồ đã chốt và click 6 đỉnh mỗi ô.";

    [MenuItem("Tools/Bach Luyen Tien Do/World Map Polygon Editor (.54)")]
    public static void Open()
    {
        var window = GetWindow<WorldMapPolygonEditorWindow>("World Map Polygons .54");
        window.minSize = new Vector2(720, 470);
        window.Show();
    }

    private void OnEnable()
    {
        LoadDocument();
        LoadIds();
    }

    private void LoadIds()
    {
        ids.Clear();
        TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
        if (source == null) return;
        string[] lines = source.text.Split('\n');
        foreach (var raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            string[] columns = line.Split('\t');
            if (columns.Length == 8 && !ids.Contains(columns[0])) ids.Add(columns[0]);
        }
    }

    private void LoadDocument()
    {
        TextAsset file = AssetDatabase.LoadAssetAtPath<TextAsset>(OutputPath);
        document = file == null ? new WorldMapPolygonDocument() :
            JsonUtility.FromJson<WorldMapPolygonDocument>(file.text);
        if (document == null) document = new WorldMapPolygonDocument();
        if (document.polygons == null) document.polygons = new List<WorldMapPolygon>();
        WorldMapPolygons.InvalidateCache();
    }

    private WorldMapPolygon ActivePolygon(bool create = false)
    {
        if (selectedIndex < 0 || selectedIndex >= ids.Count) return null;
        string id = ids[selectedIndex];
        foreach (var p in document.polygons) if (p != null && p.mapId == id) return p;
        if (!create) return null;
        var result = new WorldMapPolygon { mapId = id };
        document.polygons.Add(result);
        return result;
    }

    private static Rect Fit(Rect viewport, Texture2D image)
    {
        float ratio = (float)image.width / image.height;
        float width = Mathf.Min(viewport.width, viewport.height * ratio);
        float height = width / ratio;
        return new Rect(viewport.x + (viewport.width - width) * .5f,
            viewport.y + (viewport.height - height) * .5f, width, height);
    }

    private static Vector2 ScreenPoint(Rect rect, Vector2 normalized)
        => new Vector2(rect.x + rect.width * normalized.x,
            rect.y + rect.height * (1f - normalized.y));

    private static Vector2 FromScreen(Rect rect, Vector2 point)
        => new Vector2(Mathf.Clamp01((point.x - rect.x) / rect.width),
            Mathf.Clamp01(1f - (point.y - rect.y) / rect.height));

    private Vector2 Snap(Rect rect, Vector2 screen, WorldMapPolygon current)
    {
        Vector2 value = FromScreen(rect, screen);
        if (!snapToNeighbors) return value;
        float bestPixels = 11f;
        foreach (var polygon in document.polygons)
        {
            if (polygon == null || polygon == current || polygon.vertices == null) continue;
            foreach (var neighbor in polygon.vertices)
            {
                float pixels = Vector2.Distance(ScreenPoint(rect, neighbor), screen);
                if (pixels >= bestPixels) continue;
                bestPixels = pixels;
                value = neighbor; // Copy exact coordinates of shared corner.
            }
        }
        return value;
    }

    private void DrawPolyline(WorldMapPolygon polygon, Rect rect, Color color)
    {
        if (polygon == null || polygon.vertices == null || polygon.vertices.Count == 0) return;
        var points = new Vector3[polygon.vertices.Count +
            (polygon.vertices.Count == 6 ? 1 : 0)];
        for (int i = 0; i < polygon.vertices.Count; i++)
        {
            Vector2 p = ScreenPoint(rect, polygon.vertices[i]);
            points[i] = new Vector3(p.x, p.y, 0f);
        }
        if (polygon.vertices.Count == 6) points[points.Length - 1] = points[0];
        Handles.color = color;
        Handles.DrawAAPolyLine(2.2f, points);
    }

    private bool CurrentTextureMatches()
    {
        return mapTexture != null &&
            (document.artworkWidth <= 0 || document.artworkWidth == mapTexture.width) &&
            (document.artworkHeight <= 0 || document.artworkHeight == mapTexture.height);
    }

    private void Save()
    {
        if (document == null) return;
        if (!CurrentTextureMatches())
        {
            EditorUtility.DisplayDialog("Không lưu",
                "Bạn đang mở ảnh có kích thước khác nguồn polygon đã lưu. " +
                "Không được sao chép tọa độ sang bản đồ có bố cục khác.",
                "OK");
            return;
        }
        if (mapTexture != null)
        {
            document.artworkWidth = mapTexture.width;
            document.artworkHeight = mapTexture.height;
            document.artworkReference = AssetDatabase.GetAssetPath(mapTexture);
        }
        var valid = new HashSet<string>(ids);
        var unique = new HashSet<string>();
        int completed = 0;
        foreach (var polygon in document.polygons)
        {
            if (polygon == null || !valid.Contains(polygon.mapId) || !unique.Add(polygon.mapId))
            {
                EditorUtility.DisplayDialog("Chưa lưu", "Mã map không hợp lệ hoặc trùng.", "OK");
                return;
            }
            if (polygon.vertices == null || polygon.vertices.Count == 0) continue;
            if (!WorldMapPolygons.ValidSixVertices(polygon.vertices))
            {
                EditorUtility.DisplayDialog("Chưa lưu",
                    polygon.mapId + " phải có đúng 6 đỉnh tạo thành đa giác đơn, không tự cắt (có thể lõm).",
                    "OK");
                return;
            }
            completed++;
        }
        document.polygons.RemoveAll(p => p == null || p.vertices == null || p.vertices.Count == 0);
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, JsonUtility.ToJson(document, true));
        AssetDatabase.ImportAsset(OutputPath);
        WorldMapPolygons.InvalidateCache();
        status = "Đã lưu " + completed + "/" + ids.Count +
                 " vùng tương tác vào " + OutputPath;
        Debug.Log("[WorldMapEditor] " + status);
    }

    private void OnGUI()
    {
        if (document == null) LoadDocument();
        if (ids.Count == 0) LoadIds();

        EditorGUILayout.BeginHorizontal();
        mapTexture = (Texture2D)EditorGUILayout.ObjectField("Ảnh map gốc", mapTexture,
            typeof(Texture2D), false, GUILayout.MinWidth(260));
        if (GUILayout.Button("Nạp lại JSON", GUILayout.Width(105))) LoadDocument();
        if (GUILayout.Button("Lưu các đỉnh", GUILayout.Width(108))) Save();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(ids.Count == 0);
        selectedIndex = EditorGUILayout.Popup("Mã map", Mathf.Clamp(selectedIndex, 0,
            Mathf.Max(0, ids.Count - 1)), ids.ToArray(), GUILayout.MinWidth(190));
        EditorGUI.EndDisabledGroup();

        showAll = GUILayout.Toggle(showAll, "Hiện viền đã ghi", GUILayout.Width(115));
        snapToNeighbors = GUILayout.Toggle(snapToNeighbors, "Dính đỉnh ô kề", GUILayout.Width(115));
        if (GUILayout.Button("Duyệt map này", GUILayout.Width(108)))
        {
            var polygon = ActivePolygon();
            if (polygon != null && WorldMapPolygons.ValidSixVertices(polygon.vertices))
            {
                polygon.reviewed = true;
                polygon.source = "verified-in-editor";
                status = polygon.mapId + ": đã đánh dấu duyệt 6 đỉnh. Nhớ bấm Lưu các đỉnh.";
                Repaint();
            }
        }
        if (GUILayout.Button("Vẽ lại map này", GUILayout.Width(115)))
        {
            if (EditorUtility.DisplayDialog("Xóa tọa độ", "Xóa 6 đỉnh đang chọn để vẽ lại?",
                "Xóa", "Hủy"))
            {
                var p = ActivePolygon();
                if (p != null) p.vertices.Clear();
                Repaint();
            }
        }
        EditorGUILayout.EndHorizontal();

        var current = ActivePolygon();
        if (mapTexture != null && !CurrentTextureMatches())
        {
            EditorGUILayout.HelpBox("LỆCH ẢNH: hệ tọa độ hiện tại dựa trên ảnh có mã " +
                document.artworkWidth + "×" + document.artworkHeight +
                " px. Ảnh bạn đang chọn là " + mapTexture.width + "×" +
                mapTexture.height + " px. Chỉ xem tham chiếu, không ghi/sửa dữ liệu " +
                "trừ khi tạo bộ polygon mới cho ảnh này.", MessageType.Error);
        }
        int done = 0;
        foreach (var p in document.polygons)
            if (p != null && WorldMapPolygons.ValidSixVertices(p.vertices)) done++;
        EditorGUILayout.HelpBox("Đã ghi " + done + "/" + ids.Count +
            " map. Nhấn lần lượt 6 góc của một ô theo chiều kim đồng hồ hoặc ngược chiều; " +
            "kéo chấm tròn để tinh chỉnh. Không tự gán ID dựa trên thứ tự ô trong ảnh.",
            MessageType.Info);
        EditorGUILayout.LabelField((current != null && current.reviewed
            ? "ĐÃ DUYỆT " : "CHƯA DUYỆT ") + status, EditorStyles.miniLabel);

        Rect viewport = GUILayoutUtility.GetRect(80, 99999, 120, 99999,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(viewport, new Color(.07f, .09f, .13f, 1f));

        if (mapTexture == null)
        {
            GUI.Label(viewport, "Kéo ảnh bản đồ đã chốt vào ô 'Ảnh map gốc'.",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            return;
        }

        Rect rect = Fit(viewport, mapTexture);
        GUI.DrawTexture(rect, mapTexture, ScaleMode.StretchToFill);
        Handles.BeginGUI();
        if (showAll)
            foreach (var p in document.polygons)
                if (p != current && p != null && p.vertices != null && p.vertices.Count == 6)
                    DrawPolyline(p, rect, new Color(.15f, 1f, .65f, .55f));

        if (current != null && current.vertices != null)
        {
            DrawPolyline(current, rect, Color.yellow);
            for (int i = 0; i < current.vertices.Count; ++i)
            {
                var pos = ScreenPoint(rect, current.vertices[i]);
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(pos, Vector3.forward, 5.4f);
                Handles.Label(pos + new Vector2(7f, -9f), (i + 1).ToString());
            }
        }
        Handles.EndGUI();

        var e = Event.current;
        if (CurrentTextureMatches() && e.type == EventType.MouseDown && e.button == 0 &&
            rect.Contains(e.mousePosition))
        {
            current = ActivePolygon(true);
            dragVertex = -1;
            for (int i = 0; i < current.vertices.Count; ++i)
                if (Vector2.Distance(ScreenPoint(rect, current.vertices[i]), e.mousePosition) < 12f)
                { dragVertex = i; break; }
            if (dragVertex >= 0) isDragging = true;
            else if (current.vertices.Count < 6)
            {
                current.vertices.Add(Snap(rect, e.mousePosition, current));
                current.reviewed = false;
                status = current.mapId + ": " + current.vertices.Count + "/6 đỉnh.";
            }
            else status = "Muốn thay đổi hình, hãy kéo đỉnh vàng hoặc chọn Vẽ lại map này.";
            e.Use(); Repaint();
        }
        else if (isDragging && e.type == EventType.MouseDrag && e.button == 0)
        {
            var p = ActivePolygon();
            if (p != null && dragVertex >= 0 && dragVertex < p.vertices.Count)
            {
                p.vertices[dragVertex] = Snap(rect, e.mousePosition, p);
                p.reviewed = false;
            }
            e.Use(); Repaint();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false; dragVertex = -1; Repaint();
        }
    }
}
#endif