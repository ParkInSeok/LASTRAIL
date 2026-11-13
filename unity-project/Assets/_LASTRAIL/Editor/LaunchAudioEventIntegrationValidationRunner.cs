#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Presentation.Audio;
using UnityEditor;
using UnityEngine;

namespace LastRail.Editor
{
    public static class LaunchAudioEventIntegrationValidationRunner
    {
        private const string IntegrationManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-audio-integrations.json";

        private static readonly string[] ScreenPrefabPaths =
        {
            "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/RewardReviewScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/OfflineRewardScreen.prefab",
            "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab"
        };

        [MenuItem("LASTRAIL/Launch/Validate Audio Event Integration")]
        public static void ValidateAudioEventIntegration()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailAudioQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "audio-event-integration-validation.json");
            try
            {
                ValidateManifest();
                var playerCount = 0;
                var bindingCount = 0;
                foreach (var prefabPath in ScreenPrefabPaths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab == null) throw new FileNotFoundException("Missing launch screen prefab: " + prefabPath);
                    var player = prefab.GetComponent<LastRailAudioCuePlayer>();
                    if (player == null) throw new InvalidOperationException("Missing LastRailAudioCuePlayer: " + prefabPath);
                    if (player.HasMissingClipBinding) throw new InvalidOperationException("Missing audio clip binding: " + prefabPath);
                    if (player.HasUnintendedAutoplay) throw new InvalidOperationException("Unintended audio autoplay: " + prefabPath);
                    if (prefab.GetComponentsInChildren<AudioSource>(true).Length < 2) throw new InvalidOperationException("Missing audio sources: " + prefabPath);
                    playerCount++;
                    bindingCount += player.BindingCount;
                }

                File.WriteAllText(
                    reportPath,
                    "{\"status\":\"PASS\",\"screenCount\":" + ScreenPrefabPaths.Length +
                    ",\"audioPlayerCount\":" + playerCount +
                    ",\"bindingCount\":" + bindingCount +
                    ",\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL audio event integration validation PASS: " + reportPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(
                    reportPath,
                    "{\"status\":\"FAIL\",\"screenCount\":" + ScreenPrefabPaths.Length +
                    ",\"warningCount\":0,\"failCount\":1,\"blockers\":[\"audio_event_integration_validation_failed\"]}");
                Debug.LogError("LASTRAIL audio event integration validation failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        private static void ValidateManifest()
        {
            var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, IntegrationManifestPath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("Missing audio integration manifest: " + IntegrationManifestPath);

            var json = File.ReadAllText(fullPath);
            var compact = json.Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
            RequireContains(compact, "\"connectedCueCount\":22");
            RequireContains(compact, "\"bgmAct1RailLoopAutoplay\":false");
            RequireContains(compact, "\"serverContractChanged\":false");
            RequireContains(compact, "\"gameplayTimingChanged\":false");
            RequireContains(compact, "\"rewardCalculationIncluded\":false");
            RequireContains(compact, "\"economyMutationIncluded\":false");
        }

        private static void RequireContains(string value, string expected)
        {
            if (!value.Contains(expected))
            {
                throw new InvalidOperationException("Audio integration manifest missing " + expected);
            }
        }
    }
}
#endif
