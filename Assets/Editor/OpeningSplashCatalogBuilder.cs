#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// New media becomes available after Unity imports it, with no scene edits.
[InitializeOnLoad]
public sealed class OpeningSplashCatalogBuilder : AssetPostprocessor
{
    const string Folder = "Assets/Resources/OpeningSplashArt";
    const string Output = "Assets/Resources/OpeningSplashCatalog.asset";
    private static bool queued;

    static OpeningSplashCatalogBuilder() { Schedule(); }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] moved, string[] movedFrom)
    {
        if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
            Schedule();
    }
    static bool Touches(string[] paths)
    {
        foreach (var path in paths)
            if (path.StartsWith(Folder + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
    static void Schedule()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += Rebuild;
    }

    [MenuItem("Tools/IdleGame/Rebuild Opening Splash Catalog")]
    public static void Rebuild()
    {
        queued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Schedule();
            return;
        }
        var cat = AssetDatabase.LoadAssetAtPath<OpeningSplashCatalog>(Output);
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<OpeningSplashCatalog>();
            AssetDatabase.CreateAsset(cat, Output);
        }
        var images = new List<string>();
        var videos = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("", new[] { Folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var key = "OpeningSplashArt/" + Path.GetFileNameWithoutExtension(path);
            if ((ext == ".png" || ext == ".jpg" || ext == ".jpeg") &&
                AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                images.Add(key);
            else if (ext == ".mp4" && AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path) != null)
                videos.Add(key);
        }
        images.Sort(StringComparer.Ordinal);
        videos.Sort(StringComparer.Ordinal);
        if (Equal(cat.images, images) && Equal(cat.videos, videos)) return;
        cat.images = images.ToArray();
        cat.videos = videos.ToArray();
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        Debug.Log("[OpeningSplash] Catalog rebuilt: " + images.Count +
            " images, " + videos.Count + " videos");
    }
    static bool Equal(string[] previous, List<string> next)
    {
        if (previous == null || previous.Length != next.Count) return false;
        for (int i = 0; i < next.Count; i++)
            if (previous[i] != next[i]) return false;
        return true;
    }
}
#endif
