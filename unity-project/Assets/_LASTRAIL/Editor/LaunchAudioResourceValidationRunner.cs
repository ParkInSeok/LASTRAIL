#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Presentation.Audio;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchAudioResourceValidationRunner
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-audio-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/Audio/README.md";
        private const string IntegrationManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-audio-integrations.json";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        [MenuItem("LASTRAIL/Launch/Validate Audio Resource Foundation")]
        public static void ValidateAudioResources()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailAudioQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "audio-resource-validation.json");
            try
            {
                RequireFile(ManifestPath);
                RequireFile(ReadmePath);
                RequireFile(IntegrationManifestPath);

                foreach (var clip in LaunchAudioResourceBuilder.Clips)
                {
                    ValidateClip(clip);
                }

                ValidateShowcasePrefab();
                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"clipCount\":" + LaunchAudioResourceBuilder.Clips.Length + ",\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL audio resource validation PASS: " + reportPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"clipCount\":" + LaunchAudioResourceBuilder.Clips.Length + ",\"warningCount\":0,\"failCount\":1,\"blockers\":[\"audio_resource_validation_failed\"]}");
                Debug.LogError("LASTRAIL audio resource validation failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        private static void ValidateClip(LaunchAudioResourceSpec expected)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(expected.Path);
            if (clip == null) throw new FileNotFoundException("Missing audio clip: " + expected.Path);
            if (clip.frequency != 44100) throw new InvalidOperationException("Unexpected sample rate: " + expected.Path);
            if (clip.channels != 1) throw new InvalidOperationException("Unexpected channel count: " + expected.Path);
            if (clip.length <= (expected.Loop ? 8f : 0.04f)) throw new InvalidOperationException("Unexpected duration: " + expected.Path);

            var importer = AssetImporter.GetAtPath(expected.Path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing AudioImporter: " + expected.Path);
            if (!importer.forceToMono) throw new InvalidOperationException("Audio clip must force mono: " + expected.Path);
            if (!importer.defaultSampleSettings.preloadAudioData) throw new InvalidOperationException("Audio clip must preload data: " + expected.Path);
        }

        private static void ValidateShowcasePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LaunchAudioResourceBuilder.ShowcasePrefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing audio showcase prefab.");
            if (prefab.GetComponentsInChildren<Text>(true).Length != 0) throw new InvalidOperationException("Legacy UI Text found in audio showcase.");
            if (prefab.GetComponentsInChildren<AudioSource>(true).Length < LaunchAudioResourceBuilder.Clips.Length) throw new InvalidOperationException("Audio showcase source coverage too low.");
            var player = prefab.GetComponent<LastRailAudioCuePlayer>();
            if (player == null) throw new InvalidOperationException("Audio showcase requires LastRailAudioCuePlayer.");
            if (player.BindingCount != LaunchAudioResourceBuilder.Clips.Length) throw new InvalidOperationException("Audio showcase cue player binding coverage mismatch.");
            if (player.HasMissingClipBinding) throw new InvalidOperationException("Audio showcase has missing cue player clip binding.");
            if (player.HasUnintendedAutoplay) throw new InvalidOperationException("Audio showcase has unintended autoplay.");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != font) throw new InvalidOperationException("Missing Pretendard font on " + text.name);
                if (text.fontSharedMaterial != material) throw new InvalidOperationException("Missing Pretendard material on " + text.name);
                if (text.overflowMode != TextOverflowModes.Ellipsis && text.overflowMode != TextOverflowModes.Truncate) throw new InvalidOperationException("Text overflow policy mismatch on " + text.name);
            }

            var eventSystems = prefab.GetComponentsInChildren<EventSystem>(true);
            if (eventSystems.Length != 1) throw new InvalidOperationException("Audio showcase requires one EventSystem.");
            if (eventSystems[0].GetComponent<InputSystemUIInputModule>() == null) throw new InvalidOperationException("Audio showcase requires InputSystemUIInputModule.");
            if (eventSystems[0].GetComponent<StandaloneInputModule>() != null) throw new InvalidOperationException("Audio showcase must not use StandaloneInputModule.");
        }

        private static void RequireFile(string assetPath)
        {
            if (!File.Exists(ProjectPath(assetPath))) throw new FileNotFoundException("Missing audio foundation file: " + assetPath);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }
    }
}
#endif
