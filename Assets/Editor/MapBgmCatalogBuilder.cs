#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Unity import automation: Assets/BGM/*.ogg -> Resources/MapBgmCatalog.asset.
// Never duplicates audio. The catalog holds references so clips enter builds.
[InitializeOnLoad]
public sealed class MapBgmCatalogBuilder : AssetPostprocessor
{
    private const string Folder = "Assets/BGM";
    private const string CatalogPath = "Assets/Resources/MapBgmCatalog.asset";
    private static bool pending;

    static MapBgmCatalogBuilder() { Schedule(); }

    private static void Schedule()
    {
        if (pending) return;
        pending = true;
        EditorApplication.delayCall += Refresh;
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (TouchesBgm(importedAssets) || TouchesBgm(deletedAssets) ||
            TouchesBgm(movedAssets) || TouchesBgm(movedFromAssetPaths))
            Schedule();
    }

    private static bool TouchesBgm(string[] paths)
    {
        foreach (string path in paths)
            if (path.StartsWith(Folder + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    [MenuItem("Tools/IdleGame/Rebuild BGM Catalog")]
    public static void Refresh()
    {
        pending = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Schedule();
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        MapBgmCatalog catalog = AssetDatabase.LoadAssetAtPath<MapBgmCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MapBgmCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var groups = new List<AudioClip>[5];
        for (int i = 0; i < groups.Length; i++) groups[i] = new List<AudioClip>();
        AudioClip opening = null;
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetExtension(path), ".ogg", StringComparison.OrdinalIgnoreCase))
                continue;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, "opening", StringComparison.OrdinalIgnoreCase))
            {
                opening = clip;
                continue;
            }
            if (name.Length < 4 || !name.StartsWith("BGM", StringComparison.OrdinalIgnoreCase))
                continue;
            int group = name[3] - '1';
            if (group < 0 || group >= 5) continue;
            string suffix = name.Substring(4);
            if (suffix.Length != 0 && !System.Text.RegularExpressions.Regex.IsMatch(suffix, @"^ \([1-9][0-9]*\)$"))
                continue;
            groups[group].Add(clip);
        }

        bool changed = catalog.opening != opening;
        var arrays = new AudioClip[5][];
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i].Sort((a,b) => StringComparer.Ordinal.Compare(a.name,b.name));
            arrays[i] = groups[i].ToArray();
        }
        if (!Same(catalog.bgm1,arrays[0]) || !Same(catalog.bgm2,arrays[1]) ||
            !Same(catalog.bgm3,arrays[2]) || !Same(catalog.bgm4,arrays[3]) ||
            !Same(catalog.bgm5,arrays[4])) changed = true;
        if (!changed) return;
        catalog.opening = opening;
        catalog.bgm1 = arrays[0];
        catalog.bgm2 = arrays[1];
        catalog.bgm3 = arrays[2];
        catalog.bgm4 = arrays[3];
        catalog.bgm5 = arrays[4];
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[BGM] Audio catalog rebuilt: " +
            arrays[0].Length + "/" + arrays[1].Length + "/" + arrays[2].Length +
            "/" + arrays[3].Length + "/" + arrays[4].Length + " tracks; opening=" + (opening != null));
    }

    private static bool Same(AudioClip[] oldItems, AudioClip[] newItems)
    {
        if (oldItems == null || oldItems.Length != newItems.Length) return false;
        for (int i = 0; i < oldItems.Length; i++)
            if (oldItems[i] != newItems[i]) return false;
        return true;
    }
}
#endif
