#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

internal sealed class IntegratedAmbienceImport : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/IntegratedAmbience/", StringComparison.Ordinal)) return;
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.loadType = assetPath.EndsWith("/GateThunder.ogg", StringComparison.Ordinal)
            ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .75f;
        importer.defaultSampleSettings = settings;
    }
}
#endif
