#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class WorldClimateVendorSetup
{
    private const string SkyRoot = "Assets/Free HDR Skyboxes Pack/";
    private const string RainRoot = "Assets/RainMaker/Prefab/";
    private const string ConfigFolder = "Assets/Resources/WorldClimateVendor";

    [MenuItem("Bach Luyen/Environment/Link local weather assets")]
    public static void Generate()
    {
        var skies = new Cubemap[5];
        for (int i = 0; i < skies.Length; i++)
        {
            int index = i + 1;
            var sky = RequireAsset<Cubemap>(SkyRoot + "Texture/Sky-" + index + ".hdr");
            var material = RequireAsset<Material>(SkyRoot + "Material/sky-" + index + ".mat");
            if (!material.HasProperty("_Tex") || material.GetTexture("_Tex") != sky)
                throw new InvalidOperationException("HDR material and cubemap do not match: " + material.name);
            skies[i] = sky;
            Debug.Log($"[WorldClimate] Sky-{index}: {sky.width} per face, {sky.format}, {material.shader.name}.");
        }

        var rain = RequireAsset<GameObject>(RainRoot + "RainPrefab2D.prefab");
        var rainTexture = RequireAsset<Texture2D>(RainRoot + "RainTexture.png");
        ParticleSystem emitter = null;
        foreach (var behaviour in rain.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour.GetType().FullName != "DigitalRuby.RainMaker.RainScript2D") continue;
            emitter = new SerializedObject(behaviour).FindProperty("RainFallParticleSystem")?.objectReferenceValue as ParticleSystem;
            break;
        }
        if (emitter == null) throw new InvalidOperationException("RainPrefab2D has no linked RainFallParticleSystem.");
        var rainRenderer = emitter.GetComponent<ParticleSystemRenderer>();
        if (rainRenderer == null || rainRenderer.sharedMaterial == null || rainRenderer.sharedMaterial.mainTexture != rainTexture)
            throw new InvalidOperationException("Rain Maker 2D emitter does not reference the expected vendor texture.");
        Debug.Log($"[WorldClimate] RainPrefab2D/{emitter.name}: original shader {rainRenderer.sharedMaterial.shader.name}, " +
            $"rate {emitter.emission.rateOverTime.constant}, source max {emitter.main.maxParticles} particles.");

        if (!AssetDatabase.IsValidFolder(ConfigFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "WorldClimateVendor");
        string path = ConfigFolder + "/Assets.asset";
        var config = AssetDatabase.LoadAssetAtPath<WorldClimateVendorAssets>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<WorldClimateVendorAssets>();
            AssetDatabase.CreateAsset(config, path);
        }
        config.dawnSky = skies[0];
        config.clearSky = skies[1];
        config.cloudySky = skies[2];
        config.duskSky = skies[3];
        config.rainEmitter2D = emitter;
        config.rainTexture = rainTexture;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("[WorldClimate] Linked local vendor assets at " + path);
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException("Missing local asset: " + path);
        return asset;
    }
}
#endif
