#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

internal sealed class WorldMapMusicImport : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/WorldMapMusic/", StringComparison.Ordinal)) return;
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .75f;
        importer.defaultSampleSettings = settings;
    }
}
#endif
