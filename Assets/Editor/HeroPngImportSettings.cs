#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Preserve the exact uploaded canvases. The existing bridge creates full-rect
// Sprites once on bind; automatic slicing/NPOT resizing would break registration.
public sealed class HeroPngImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/HeroAnimation/",System.StringComparison.Ordinal) ||
            !assetPath.EndsWith(".png",System.StringComparison.OrdinalIgnoreCase))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=false;
        importer.isReadable=false;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;
        importer.sRGBTexture=true;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=512;
    }
}
#endif
