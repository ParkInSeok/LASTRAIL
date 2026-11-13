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
    public static class LaunchRewardOfflineResourceValidationRunner
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-reward-offline-resources.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/RewardOfflineResources/README.md";
        private const string SourceReportPath = "Assets/_LASTRAIL/UI/RewardOfflineResources/high-quality-replacement-sources.json";
        private const string RewardReviewPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab";
        private const string OfflineRewardPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        [MenuItem("LASTRAIL/Launch/Validate Reward Offline Resource Foundation")]
        public static void ValidateRewardOfflineResources()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailRewardOfflineQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "reward-offline-validation.json");
            var failures = 0;

            try
            {
                RequireFile(ManifestPath);
                RequireFile(ReadmePath);
                RequireFile(SourceReportPath);
                ValidateHighQualityMetadata();
                foreach (var resource in LaunchRewardOfflineResourceBuilder.Resources)
                {
                    ValidateTexture(resource);
                }

                ValidatePrefab(LaunchRewardOfflineResourceBuilder.ShowcasePrefabPath, true);
                ValidatePrefab(RewardReviewPrefabPath, false);
                ValidatePrefab(OfflineRewardPrefabPath, false);
                RequireSprite(RewardReviewPrefabPath, "reward_result_plate");
                RequireSprite(RewardReviewPrefabPath, "reward_line_item_frame");
                RequireSprite(RewardReviewPrefabPath, "claim_success_badge");
                RequireSprite(RewardReviewPrefabPath, "reward_chest_supply_crate_accent");
                RequireSprite(OfflineRewardPrefabPath, "offline_settlement_meter");
                RequireSprite(OfflineRewardPrefabPath, "offline_duration_accumulation_strip");
                RequireSprite(OfflineRewardPrefabPath, "offline_empty_state_visual");
                RequireSprite(OfflineRewardPrefabPath, "claim_pending_claimable_accent");
            }
            catch (Exception ex)
            {
                failures = 1;
                Debug.LogError("LASTRAIL reward/offline resource validation failed: " + ex.GetType().Name + ": " + ex.Message);
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"resourceCount\":9,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"reward_offline_resource_validation_failed\"]}");
                throw;
            }

            File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"resourceCount\":9,\"warningCount\":0,\"failCount\":" + failures + ",\"blockers\":[]}");
            Debug.Log("LASTRAIL reward/offline resource validation PASS: " + reportPath);
        }

        private static void RequireFile(string assetPath)
        {
            if (!File.Exists(ProjectPath(assetPath)))
            {
                throw new FileNotFoundException("Missing reward/offline file: " + assetPath);
            }
        }

        private static void ValidateTexture(LaunchRewardOfflineResourceSpec resource)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resource.Path);
            if (texture == null) throw new FileNotFoundException("Missing reward/offline texture: " + resource.Path);
            if (texture.width != resource.Width || texture.height != resource.Height)
            {
                throw new InvalidOperationException("Unexpected reward/offline texture dimensions: " + resource.Path);
            }

            var importer = AssetImporter.GetAtPath(resource.Path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                throw new InvalidOperationException("Reward/offline texture importer is not launch ready: " + resource.Path);
            }

            if (resource.QualityClass != "HIGH_QUALITY_2D_ILLUSTRATION")
            {
                throw new InvalidOperationException("Reward/offline resource is not marked high quality: " + resource.Path);
            }

            if (resource.ReplacementStatus != "REPLACED_HIGH_QUALITY_2D")
            {
                throw new InvalidOperationException("Reward/offline resource replacement status is not launch ready: " + resource.Path);
            }
        }

        private static void ValidateHighQualityMetadata()
        {
            var manifest = File.ReadAllText(ProjectPath(ManifestPath));
            if (manifest.Contains("Unity procedural texture builder"))
            {
                throw new InvalidOperationException("Reward/offline manifest still points at procedural placeholder generation.");
            }

            if (!manifest.Contains("\"qualityClass\": \"HIGH_QUALITY_2D_ILLUSTRATION\""))
            {
                throw new InvalidOperationException("Reward/offline manifest is missing high-quality qualityClass entries.");
            }

            if (!manifest.Contains("\"replacementStatus\": \"REPLACED_HIGH_QUALITY_2D\""))
            {
                throw new InvalidOperationException("Reward/offline manifest is missing replacement status entries.");
            }

            var sourceReport = File.ReadAllText(ProjectPath(SourceReportPath));
            foreach (var resource in LaunchRewardOfflineResourceBuilder.Resources)
            {
                if (!sourceReport.Contains("\"id\": \"" + resource.Id + "\""))
                {
                    throw new InvalidOperationException("Reward/offline source report is missing " + resource.Id);
                }
            }
        }

        private static void ValidatePrefab(string path, bool requireEventSystem)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new FileNotFoundException("Missing reward/offline prefab: " + path);
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
