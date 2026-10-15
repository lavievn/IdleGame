#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Import the approved world map artwork without touching gameplay data,
// scene objects, national borders, or Unity serialised layout coordinates.
public static class WorldMapArtworkImporter
{
    [MenuItem("Tools/Bach Luyen Tien Do/Import World Map Artwork")]
    private static void Import()
    {
        string source = EditorUtility.OpenFilePanel("Chọn ảnh bản đồ thế giới (.png)", "", "png");
        if (string.IsNullOrEmpty(source)) return;
        const string folder = "Assets/Resources";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        const string assetPath = folder + "/WorldMapArtwork.png";
        if (File.Exists(assetPath) &&
            !EditorUtility.DisplayDialog("Thay ảnh bản đồ?",
                "WorldMapArtwork.png đã tồn tại. Ghi đè ảnh cũ?", "Thay thế", "Hủy"))
            return;
        File.Copy(source,assetPath,true);
        AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        Debug.Log("[WorldMap] Artwork ready: " + assetPath +
            " (fog and nation polygons are overlaid at runtime).");
    }
}
#endif
