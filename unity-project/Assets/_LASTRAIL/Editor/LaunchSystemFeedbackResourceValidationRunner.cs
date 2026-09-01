#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchSystemFeedbackResourceValidationRunner
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-system-feedback-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/SystemFeedbackResources/README.md";
        private const string MissionShellPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";
        private const string GachaPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab";
        private const string RewardReviewPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab";
        private const string OfflineRewardPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        [MenuItem("LASTRAIL/Launch/Validate System Feedback Resource Foundation")]
        public static void ValidateSystemFeedbackResources()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailSystemFeedbackQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "system-feedback-validation.json");
            var failures = 0;

            try
            {
                RequireFile(ManifestPath);
                RequireFile(ReadmePath);
                foreach (var resource in LaunchSystemFeedbackResourceBuilder.Resources)
                {
                    ValidateTexture(resource);
                }

                ValidatePrefab(LaunchSystemFeedbackResourceBuilder.ShowcasePrefabPath, true);
                ValidatePrefab(MissionShellPrefabPath, false);
                ValidatePrefab(GachaPrefabPath, false);
                ValidatePrefab(RewardReviewPrefabPath, false);
                ValidatePrefab(OfflineRewardPrefabPath, false);
                RequireSprite(MissionShellPrefabPath, "loading_rail_indicator");
                RequireSprite(MissionShellPrefabPath, "warning_strip_banner_accent");
                RequireSprite(GachaPrefabPath, "feedback_network_error_badge");
                RequireSprite(GachaPrefabPath, "retry_refresh_accent");
                RequireSprite(RewardReviewPrefabPath, "feedback_success_badge");
                RequireSprite(RewardReviewPrefabPath, "inline_status_dot_set");
                RequireSprite(OfflineRewardPrefabPath, "maintenance_notice_panel");
                RequireSprite(OfflineRewardPrefabPath, "disabled_unavailable_overlay");
            }
            catch (Exception ex)
            {
                failures = 1;
                Debug.LogError("LASTRAIL system feedback resource validation failed: " + ex.GetType().Name + ": " + ex.Message);
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"resourceCount\":9,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"system_feedback_resource_validation_failed\"]}");
                throw;
            }

            File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"resourceCount\":9,\"warningCount\":0,\"failCount\":" + failures + ",\"blockers\":[]}");
            Debug.Log("LASTRAIL system feedback resource validation PASS: " + reportPath);
        }

        private static void RequireFile(string assetPath)
        {
            if (!File.Exists(ProjectPath(assetPath)))
            {
                throw new FileNotFoundException("Missing system feedback file: " + assetPath);
            }
        }

        private static void ValidateTexture(LaunchSystemFeedbackResourceSpec resource)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resource.Path);
            if (texture == null) throw new FileNotFoundException("Missing system feedback texture: " + resource.Path);
            if (texture.width != resource.Width || texture.height != resource.Height)
            {
                throw new InvalidOperationException("Unexpected system feedback texture dimensions: " + resource.Path);
            }

            var importer = AssetImporter.GetAtPath(resource.Path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                throw new InvalidOperationException("System feedback texture importer is not launch ready: " + resource.Path);
            }
        }

        private static void ValidatePrefab(string path, bool requireEventSystem)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Missing system feedback prefab: " + path);
            if (prefab.GetComponentsInChildren<Text>(true).Length != 0) throw new InvalidOperationException("Legacy UI Text found: " + path);
            if (prefab.GetComponentsInChildren<TMP_Text>(true).Length == 0) throw new InvalidOperationException("Missing TMP text: " + path);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != font) throw new InvalidOperationException("Missing Pretendard font on " + text.name + " in " + path);
                if (text.fontSharedMaterial != material) throw new InvalidOperationException("Missing Pretendard material on " + text.name + " in " + path);
            }

            if (!requireEventSystem) return;
            var eventSystems = prefab.GetComponentsInChildren<EventSystem>(true);
            if (eventSystems.Length != 1) throw new InvalidOperationException("Expected one EventSystem in " + path);
            if (eventSystems[0].GetComponent<InputSystemUIInputModule>() == null) throw new InvalidOperationException("Missing InputSystemUIInputModule in " + path);
            if (eventSystems[0].GetComponent<StandaloneInputModule>() != null) throw new InvalidOperationException("Legacy StandaloneInputModule in " + path);
        }

        private static void RequireSprite(string prefabPath, string spriteName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && string.Equals(image.sprite.name, spriteName, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException("Missing visual-only sprite " + spriteName + " in " + prefabPath);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }
    }
}
#endif
