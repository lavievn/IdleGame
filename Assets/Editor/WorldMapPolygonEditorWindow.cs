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
    private bool showIds;
    private bool snapToNeighbors = true;
    private bool isDragging;
    private bool isDrawing;
    private int dragVertex = -1;
    private string status = "Chọn ảnh đúng nguồn. Kéo đỉnh, Shift+click chèn góc, chuột phải xóa góc.";

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
        mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/WorldMapArtwork.png");
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
            (polygon.vertices.Count >= 3 ? 1 : 0)];
        for (int i = 0; i < polygon.vertices.Count; i++)
        {
            Vector2 p = ScreenPoint(rect, polygon.vertices[i]);
            points[i] = new Vector3(p.x, p.y, 0f);
        }
        if (polygon.vertices.Count >= 3) points[points.Length - 1] = points[0];
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
            if (!WorldMapPolygons.ValidPolygon(polygon.vertices))
            {
                EditorUtility.DisplayDialog("Chưa lưu",
                    polygon.mapId + " phải có 3–24 đỉnh tạo thành đa giác đơn, không tự cắt (có thể lõm).",
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
        int oldSelection = selectedIndex;
        selectedIndex = EditorGUILayout.Popup("Mã map", Mathf.Clamp(selectedIndex, 0,
            Mathf.Max(0, ids.Count - 1)), ids.ToArray(), GUILayout.MinWidth(190));
        if (selectedIndex != oldSelection && isDrawing)
        {
            isDrawing = false; // Never accidentally add a corner to another map.
            status = "Đã hủy chế độ vẽ khi đổi mã map. Các đỉnh cũ chưa bị xóa.";
        }
        EditorGUI.EndDisabledGroup();

        showAll = GUILayout.Toggle(showAll, "Hiện viền đã ghi", GUILayout.Width(115));
        showIds = GUILayout.Toggle(showIds, "Hiện mã ô", GUILayout.Width(87));
        snapToNeighbors = GUILayout.Toggle(snapToNeighbors, "Dính đỉnh ô kề", GUILayout.Width(115));
        if (GUILayout.Button("Duyệt map này", GUILayout.Width(108)))
        {
            var polygon = ActivePolygon();
            if (polygon != null && !isDrawing && CurrentTextureMatches() && WorldMapPolygons.ValidPolygon(polygon.vertices))
            {
                polygon.reviewed = true;
                polygon.source = "verified-in-editor";
                status = polygon.mapId + ": đã duyệt " + polygon.vertices.Count + " đỉnh. Nhớ bấm Lưu.";
                Repaint();
            }
        }
        if (GUILayout.Button("Vẽ lại map này", GUILayout.Width(115)))
        {
            if (EditorUtility.DisplayDialog("Xóa tọa độ", "Xóa các đỉnh đang chọn để vẽ lại?",
                "Xóa", "Hủy"))
            {
                var p = ActivePolygon();
                if (p != null) { p.vertices.Clear(); p.reviewed = false; }
                isDrawing = true;
                status = "Vẽ theo một chiều, từ 3 đến 24 đỉnh; bấm Chốt viền để đóng.";
                Repaint();
            }
        }
        if (GUILayout.Button("Chốt viền", GUILayout.Width(90)))
        {
            var p = ActivePolygon();
            if (p != null && WorldMapPolygons.ValidPolygon(p.vertices))
            {
                isDrawing = false;
                status = p.mapId + ": khép kín " + p.vertices.Count + " đỉnh, CHƯA duyệt.";
            }
            else status = "Đa giác chưa hợp lệ: cần 3–24 đỉnh, không tự cắt.";
        }
        if (GUILayout.Button("Ô chưa duyệt kế", GUILayout.Width(118)))
        {
            int count = ids.Count;
            for (int offset = 1; offset <= count; offset++)
            {
                int index = (selectedIndex + offset) % count;
                WorldMapPolygon match = document.polygons.Find(
                    p => p != null && p.mapId == ids[index]);
                if (match == null || !match.reviewed)
                {
                    selectedIndex = index;
                    isDrawing = false;
                    status = "Chọn " + ids[index] + ": chưa duyệt.";
                    break;
                }
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
        int done = 0, reviewed = 0;
        foreach (var p in document.polygons)
            if (p != null && WorldMapPolygons.ValidPolygon(p.vertices))
            {
                done++;
                if (p.reviewed) reviewed++;
            }
        EditorGUILayout.HelpBox("Đã có tọa độ " + done + "/" + ids.Count +
            " map; ĐÃ CĂN VÀ DUYỆT " + reviewed + "/" + ids.Count +
            ". Tọa độ sinh tự động là bản nháp, không được xem là đã duyệt. " +
            "Vẽ 3–24 góc theo một chiều rồi Chốt viền; kéo để sửa, Shift+click cạnh để thêm góc, " +
            "chuột phải vào đỉnh để bỏ góc. Không tự gán ID từ thứ tự ô trong ảnh.",
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
                if (p != current && p != null && WorldMapPolygons.ValidPolygon(p.vertices))
                    DrawPolyline(p, rect, new Color(.15f, 1f, .65f, .55f));

        if (showIds)
        {
            var labelStyle = new GUIStyle(EditorStyles.miniBoldLabel);
            labelStyle.normal.textColor = Color.white;
            foreach (var p in document.polygons)
            {
                if (p == null || p.vertices == null || p.vertices.Count < 3) continue;
                Vector2 sum = Vector2.zero;
                foreach (var vertex in p.vertices) sum += vertex;
                var at = ScreenPoint(rect, sum / p.vertices.Count);
                Handles.Label(at, p.mapId, labelStyle);
            }
        }
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
        if (CurrentTextureMatches() && current != null &&
            e.type == EventType.MouseDown && e.button == 1 &&
            rect.Contains(e.mousePosition))
        {
            int closest = -1;
            float distance = 12f;
            for (int i = 0; i < current.vertices.Count; ++i)
            {
                float d = Vector2.Distance(ScreenPoint(rect, current.vertices[i]), e.mousePosition);
                if (d < distance) { distance = d; closest = i; }
            }
            if (closest >= 0 && (current.vertices.Count > 3 || isDrawing))
            {
                current.vertices.RemoveAt(closest);
                current.reviewed = false;
                status = current.mapId + ": đã xóa đỉnh " + (closest + 1);
                e.Use(); Repaint();
            }
        }
        else if (CurrentTextureMatches() && e.type == EventType.MouseDown &&
            e.button == 0 && rect.Contains(e.mousePosition))
        {
            current = ActivePolygon(true);
            dragVertex = -1;
            if (e.shift && current.vertices.Count >= 3 &&
                current.vertices.Count < 24 && !isDrawing)
            {
                int edgeIndex = -1;
                float distance = 12f;
                // Insert the corner ON an existing edge, preserving the polygon winding.
                for (int i = 0; i < current.vertices.Count; i++)
                {
                    Vector2 a = ScreenPoint(rect, current.vertices[i]);
                    Vector2 b = ScreenPoint(rect,
                        current.vertices[(i + 1) % current.vertices.Count]);
                    Vector2 ab = b - a;
                    if (ab.sqrMagnitude < 0.01f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(e.mousePosition - a, ab) / ab.sqrMagnitude);
                    float d = Vector2.Distance(e.mousePosition, a + t * ab);
                    if (d < distance) { distance = d; edgeIndex = i; }
                }
                if (edgeIndex >= 0)
                {
                    current.vertices.Insert(edgeIndex + 1,
                        Snap(rect, e.mousePosition, current));
                    current.reviewed = false;
                    status = current.mapId + ": chèn góc vào cạnh " + (edgeIndex + 1);
                }
                else status = "Giữ Shift và click gần một cạnh để thêm đỉnh.";
            }
            else
            {
                for (int i = 0; i < current.vertices.Count; ++i)
                    if (Vector2.Distance(ScreenPoint(rect, current.vertices[i]),
                        e.mousePosition) < 12f)
                    { dragVertex = i; break; }
                if (dragVertex >= 0) isDragging = true;
                else if ((isDrawing || current.vertices.Count == 0) &&
                    current.vertices.Count < 24)
                {
                    isDrawing = true;
                    current.vertices.Add(Snap(rect, e.mousePosition, current));
                    current.reviewed = false;
                    status = current.mapId + ": đang vẽ " +
                        current.vertices.Count + "/24 đỉnh; bấm Chốt viền khi xong.";
                }
                else status = "Kéo đỉnh, giữ Shift+click sát cạnh để chèn đỉnh mới.";
            }
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