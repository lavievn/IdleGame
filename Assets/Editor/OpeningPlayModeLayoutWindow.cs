#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

// A non-invasive editing window: interact with REAL runtime RectTransforms
// in Scene View, then serialize their reference-space positions/sizes into
// the persistent Resources/OpeningArtTheme.asset BEFORE exiting Play Mode.
public sealed class OpeningPlayModeLayoutWindow : EditorWindow
{
    private Vector2 scroll;
    private string status="";

    [MenuItem("Tools/Bach Luyen Tien Do/Opening Layout (Play Mode)")]
    public static void Open()
    {
        var window=GetWindow<OpeningPlayModeLayoutWindow>("Opening Layout");
        window.minSize=new Vector2(340,420);
        window.Show();
    }

    private static OpeningCinematicUI FindOpening()
    {
        var all=Object.FindObjectsByType<OpeningCinematicUI>(
            FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var x in all)
            if(x!=null&&x.gameObject.activeInHierarchy)return x;
        return null;
    }

    private static bool TryBackupLayoutAsset(
        OpeningArtTheme theme,out string backup)
    {
        backup="";
        if(theme==null)return false;
        string assetPath=AssetDatabase.GetAssetPath(theme);
        if(string.IsNullOrEmpty(assetPath)||!assetPath.StartsWith("Assets/"))
            return false;
        try
        {
            var projectRoot=Directory.GetParent(Application.dataPath).FullName;
            string original=Path.Combine(projectRoot,
                assetPath.Replace('/',Path.DirectorySeparatorChar));
            if(!File.Exists(original))return false;
            string destination=Path.Combine(projectRoot,"OpeningLayoutBackups");
            Directory.CreateDirectory(destination);
            backup=Path.Combine(destination,
                "OpeningArtTheme_before_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+
                ".asset");
            File.Copy(original,backup,false);
            return true;
        }
        catch(Exception ex)
        {
            Debug.LogError("[Opening Layout] Backup failed: "+ex.Message);
            return false;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BACH LUYEN TIEN DO - OPENING",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) Bấm Play. 2) Bật chế độ căn chỉnh. "+
            "3) Chọn layer bên dưới, mở Scene View (phím T / Rect Tool) và "+
            "chỉnh RectTransform trực tiếp. 4) LƯU trước khi Stop Play. "+
            "Bản lưu nằm trong Assets/Resources/OpeningArtTheme.asset.",
            MessageType.Info);

        if(!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Chưa ở Play Mode. Mở Scene game và bấm Play trước. "+
                "Cửa sổ này dùng đúng đối tượng UI đang được game tạo khi chạy.",
                MessageType.Warning);
            return;
        }
        var opening=FindOpening();
        if(opening==null)
        {
            EditorGUILayout.HelpBox(
                "Không thấy opening đang hoạt động. Hãy trở về màn hình chính "+
                "trước khi căn chỉnh.",
                MessageType.Warning);
            return;
        }
        var theme=opening.LayoutTheme;
        bool editing=opening.IsLayoutEditing;

        EditorGUILayout.LabelField("Trạng thái",
            editing?"ĐANG CĂN CHỈNH - hoạt ảnh tạm dừng":"Đang chạy opening");
        if(theme==null)
            EditorGUILayout.HelpBox(
                "Không tìm thấy Resources/OpeningArtTheme.asset. "+
                "Cần asset này để lưu bố cục.",MessageType.Error);
        EditorGUILayout.Space(5);
        if(!editing)
        {
            if(GUILayout.Button("BẬT CHẾ ĐỘ CĂN CHỈNH",GUILayout.Height(36)))
            {
                status=opening.SetLayoutEditing(true)
                    ?"Đã khóa animation; hãy kéo RectTransform trong Scene View."
                    :"Chỉ có thể căn chỉnh khi đang ở màn hình opening.";
                Repaint();
            }
        }
        else
        {
            string saveProblem;
            bool geometryValid=opening.ValidateLayoutForSave(out saveProblem);
            if(!geometryValid)
                EditorGUILayout.HelpBox(
                    "ĐÃ KHÓA LƯU ĐỂ TRÁNH GHI ĐÈ BỐ CỤC: "+saveProblem,
                    MessageType.Error);
            using(new EditorGUI.DisabledScope(theme==null||!geometryValid))
            {
                if(GUILayout.Button("LƯU BỐ CỤC VÀO ASSET",GUILayout.Height(40)))
                {
                    if(!opening.ValidateLayoutForSave(out saveProblem))
                        status="Không thể lưu: "+saveProblem;
                    else
                    {
                        var snapshot=opening.CaptureLayout();
                        if(snapshot==null||snapshot.Length!=OpeningCinematicUI.LayoutKeys.Length)
                            status="Không lấy được đầy đủ các khung để lưu.";
                        else if(!EditorUtility.DisplayDialog(
                            "Lưu bố cục opening?",
                            "Sẽ lưu 13 RectTransform hiện tại. "+
                            "Một bản sao an toàn của asset cũ được tạo "+
                            "trước khi ghi. Bạn đã kiểm tra Game View chưa?",
                            "Lưu và sao lưu","Hủy"))
                            status="Đã hủy lưu. Dữ liệu cũ được giữ nguyên.";
                        else
                        {
                            string backupPath;
                            if(!TryBackupLayoutAsset(theme,out backupPath))
                            {
                                status="Không thể tạo bản sao dự phòng. "+
                                    "Asset cũ không bị sửa.";
                                Debug.LogError("[Opening Layout] "+status);
                            }
                            else
                            {
                                Undo.RecordObject(theme,"Save Opening Play Mode Layout");
                                theme.playModeLayouts=snapshot;
                                // Keep the earlier inspector size fields coherent.
                                theme.farMountainsReferenceSize=snapshot[1].size;
                                theme.nearMountainsReferenceSize=snapshot[2].size;
                                theme.backCloudsReferenceSize=snapshot[3].size;
                                theme.leftCloudsReferenceSize=snapshot[4].size;
                                theme.rightCloudsReferenceSize=snapshot[5].size;
                                EditorUtility.SetDirty(theme);
                                AssetDatabase.SaveAssets();
                                status="Đã lưu "+snapshot.Length+" phần tử vào "+
                                    AssetDatabase.GetAssetPath(theme)+
                                    ". Sao lưu bản trước: "+backupPath;
                                Debug.Log("[Opening Layout] "+status);
                            }
                        }
                    }
                    Repaint();
                }
            }
            if(GUILayout.Button("HOÀN TÁC THỬ - ĐỌC LẠI BỐ CỤC ĐÃ LƯU"))
            {
                opening.ReloadSavedLayoutForEditing();
                status="Đã nạp lại bố cục từ asset. Các chỉnh sửa chưa lưu sẽ mất.";
            }
            if(GUILayout.Button("XEM LẠI ANIMATION TỪ ĐẦU",GUILayout.Height(30)))
            {
                opening.SetLayoutEditing(false);
                status="Đang phát lại opening. Muốn chỉnh tiếp hãy bật chế độ căn chỉnh.";
                Repaint();
            }
        }

        if(!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status,MessageType.None);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Chọn phần tử trong Play Mode",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chỉ thay đổi Pos X/Y và Width/Height của đối tượng trong "+
            "Hierarchy. Không kéo cả parent Canvas, không sửa Anchor/Pivot. "+
            "Hai splash vẫn cắt ảnh theo khung và tải file L_/R_ như cũ. "+
            "Các vị trí được lưu theo cửa sổ tham chiếu 800×450.",
            MessageType.None);

        var rects=opening.GetLayoutRects();
        scroll=EditorGUILayout.BeginScrollView(scroll);
        for(int i=0;i<OpeningCinematicUI.LayoutKeys.Length;i++)
        {
            var r=i<rects.Length?rects[i]:null;
            if(r==null)continue;
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button((i+1).ToString("00")+" · "+
                    OpeningCinematicUI.LayoutKeys[i],GUILayout.Width(210)))
                {
                    Selection.activeGameObject=r.gameObject;
                    EditorGUIUtility.PingObject(r.gameObject);
                }
                EditorGUILayout.LabelField(
                    Mathf.RoundToInt(r.anchoredPosition.x)+","+
                    Mathf.RoundToInt(r.anchoredPosition.y)+"  /  "+
                    Mathf.RoundToInt(r.sizeDelta.x)+"×"+
                    Mathf.RoundToInt(r.sizeDelta.y),
                    EditorStyles.miniLabel);
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif
