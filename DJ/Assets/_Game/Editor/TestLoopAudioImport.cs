using System;
using UnityEditor;
using UnityEngine;

namespace DeckRogue.EditorTools
{
    public sealed class TestLoopAudioPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (assetPath.IndexOf("/TestLoops/", System.StringComparison.Ordinal) < 0)
                return;

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.ambisonic = false;
        }
    }

    [InitializeOnLoad]
    public static class TestLoopImportEnforcer
    {
        static TestLoopImportEnforcer()
        {
            EditorApplication.delayCall += EnsurePcm;
        }

        private static void EnsurePcm()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Audio/Resources/TestLoops" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                    continue;

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                if (settings.compressionFormat == AudioCompressionFormat.PCM
                    && settings.loadType == AudioClipLoadType.DecompressOnLoad
                    && settings.preloadAudioData)
                    continue;

                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = false;
                importer.SaveAndReimport();
            }
        }
    }
}
