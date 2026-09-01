#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchMissionStoryThumbnailResourceTests
    {
        private const string ManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-mission-story-thumbnails.json";
        private const string ReadmePath = "Assets/_LASTRAIL/UI/MissionStoryThumbnails/README.md";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Showcase/LRLaunchMissionStoryThumbnailShowcase.prefab";
        private const string MissionShellPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab";
        private const string StoryProgressPrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/StoryProgressSummaryScreen.prefab";
        private const string CodexArchivePrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/CodexArchiveScreen.prefab";

        private static readonly MissionThumbnailSpec[] Missions =
        {
            new MissionThumbnailSpec("opn_01_last_departure", "ordinary"),
            new MissionThumbnailSpec("opn_02_first_signal", "ordinary"),
            new MissionThumbnailSpec("opn_03_water_stop", "ordinary"),
            new MissionThumbnailSpec("opn_04_scrapstar_show", "ordinary"),
            new MissionThumbnailSpec("opn_05_night_boarders", "ordinary"),
            new MissionThumbnailSpec("opn_06_broken_line", "ordinary"),
            new MissionThumbnailSpec("raid_raider_king_engine", "story_raid")
        };

        private static readonly string[] OverlayIds =
        {
            "mission_state_locked_overlay",
            "mission_state_available_accent",
            "mission_state_cleared_stamp",
            "mission_state_first_clear_badge",
            "mission_state_risk_accent",
            "mission_state_boss_only_badge"
        };

        [Test]
        public void ManifestDefinesActOneSixOperationsAndStoryRaid()
        {
            var manifest = File.ReadAllText(ProjectPath(ManifestPath));
            Assert.That(manifest.Contains("\"missionCount\": 7", StringComparison.Ordinal), Is.True);
            Assert.That(manifest.Contains("\"ordinaryCount\": 6", StringComparison.Ordinal), Is.True);
            Assert.That(manifest.Contains("\"storyRaidCount\": 1", StringComparison.Ordinal), Is.True);

            foreach (var mission in Missions)
            {
                Assert.That(manifest.Contains("\"missionId\": \"" + mission.MissionId + "\"", StringComparison.Ordinal), Is.True, mission.MissionId);
                Assert.That(manifest.Contains("\"category\": \"" + mission.Category + "\"", StringComparison.Ordinal), Is.True, mission.Category);
            }
        }

        [Test]
        public void ThumbnailsAndStateOverlaysAreImportableSprites()
        {
            foreach (var mission in Missions)
            {
                AssertSprite("Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails/" + mission.MissionId + "_thumbnail.png", 1024, 576);
            }

            foreach (var overlay in OverlayIds)
            {
                var isFullThumbnailOverlay = overlay == "mission_state_locked_overlay" || overlay == "mission_state_available_accent";
                AssertSprite("Assets/_LASTRAIL/UI/MissionStoryThumbnails/Overlays/" + overlay + ".png", isFullThumbnailOverlay ? 1024 : 256, isFullThumbnailOverlay ? 576 : 96);
            }
        }

        [Test]
        public void ShowcaseUsesPretendardTmpInputSystemAndVisualOnlySprites()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Assert.That(prefab.GetComponentInChildren<UnityEngine.UI.Text>(true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<StandaloneInputModule>(true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<InputSystemUIInputModule>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThanOrEqualTo(8));

            foreach (var mission in Missions)
            {
                Assert.That(ContainsSprite(prefab, mission.MissionId + "_thumbnail"), Is.True, mission.MissionId);
            }

            Assert.That(ContainsSprite(prefab, "mission_state_locked_overlay"), Is.True);
            Assert.That(ContainsSprite(prefab, "mission_state_cleared_stamp"), Is.True);
            Assert.That(ContainsSprite(prefab, "mission_state_first_clear_badge"), Is.True);
            Assert.That(ContainsSprite(prefab, "mission_state_boss_only_badge"), Is.True);
        }

        [Test]
        public void MissionShellConsumesThumbnailsWithoutTakingProgressDecisions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionShellPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            foreach (var mission in Missions)
            {
                Assert.That(ContainsSprite(prefab, mission.MissionId + "_thumbnail"), Is.True, mission.MissionId);
            }
        }

        [Test]
        public void StoryProgressAndCodexArchiveConsumeThumbnailsAsVisualOnlyResources()
        {
            var storyProgress = AssetDatabase.LoadAssetAtPath<GameObject>(StoryProgressPrefabPath);
            var codexArchive = AssetDatabase.LoadAssetAtPath<GameObject>(CodexArchivePrefabPath);
            Assert.That(storyProgress, Is.Not.Null);
            Assert.That(codexArchive, Is.Not.Null);

            foreach (var mission in Missions)
            {
                Assert.That(ContainsSprite(storyProgress, mission.MissionId + "_thumbnail"), Is.True, mission.MissionId);
                Assert.That(ContainsSprite(codexArchive, mission.MissionId + "_thumbnail"), Is.True, mission.MissionId);
            }

            Assert.That(ContainsSprite(storyProgress, "mission_state_available_accent"), Is.True);
            Assert.That(ContainsSprite(storyProgress, "mission_state_cleared_stamp"), Is.True);
            Assert.That(ContainsSprite(storyProgress, "mission_state_first_clear_badge"), Is.True);
            Assert.That(ContainsSprite(codexArchive, "mission_state_locked_overlay"), Is.True);
            Assert.That(ContainsSprite(codexArchive, "mission_state_boss_only_badge"), Is.True);
        }

        [Test]
        public void ReadmeAndManifestKeepGeneratedSourceNoteAndForbiddenPayloadsOut()
        {
            var combined = File.ReadAllText(ProjectPath(ManifestPath)) + "\n" + File.ReadAllText(ProjectPath(ReadmePath));
            Assert.That(combined.Contains("Unity procedural texture builder", StringComparison.Ordinal), Is.True);
            Assert.That(combined.Contains("presentation-only", StringComparison.OrdinalIgnoreCase), Is.True);

            AssertForbidden(combined, "accessToken");
            AssertForbidden(combined, "refreshToken");
            AssertForbidden(combined, "idToken");
            AssertForbidden(combined, "accountId");
            AssertForbidden(combined, "Authorization");
            AssertForbidden(combined, "rawRng");
            AssertForbidden(combined, "probability");
            AssertForbidden(combined, "full weights");
            AssertForbidden(combined, "server submit");
            AssertForbidden(combined, "reward amount");
            AssertForbidden(combined, "economy grant");
            AssertForbidden(combined, "Rail Choice");
            AssertForbidden(combined, "World Raid");
            AssertForbidden(combined, "PlayerPrefs");
        }

        private static void AssertSprite(string path, int width, int height)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(width), path);
            Assert.That(texture.height, Is.EqualTo(height), path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(2048), path);
        }

        private static bool ContainsSprite(GameObject root, string spriteName)
        {
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.sprite != null && image.sprite.name == spriteName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertForbidden(string value, string token)
        {
            Assert.That(value.Contains(token, StringComparison.OrdinalIgnoreCase), Is.False, token);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private readonly struct MissionThumbnailSpec
        {
            public MissionThumbnailSpec(string missionId, string category)
            {
                MissionId = missionId;
                Category = category;
            }

            public string MissionId { get; }
            public string Category { get; }
        }
    }
}
#endif
