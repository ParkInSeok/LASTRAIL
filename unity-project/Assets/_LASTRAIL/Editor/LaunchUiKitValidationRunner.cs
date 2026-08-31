#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchUiKitValidationRunner
    {
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string StyleJsonPath = "Assets/_LASTRAIL/UI/Style/lastrail-ui-tokens.json";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/Common/Showcase/LRLaunchUiKitShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchUiKitShowcase.unity";

        private static readonly string[] RequiredPrefabPaths =
        {
            "Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Primary.prefab",
            "Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Secondary.prefab",
            "Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Danger.prefab",
            "Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Disabled.prefab",
            "Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Icon.prefab",
            "Assets/_LASTRAIL/UI/Common/Sliders/LRSlider_Normal.prefab",
            "Assets/_LASTRAIL/UI/Common/Sliders/LRSlider_Disabled.prefab",
            "Assets/_LASTRAIL/UI/Common/Sliders/LRSlider_Compact.prefab",
            "Assets/_LASTRAIL/UI/Common/InputFields/LRInputField_Normal.prefab",
            "Assets/_LASTRAIL/UI/Common/InputFields/LRInputField_Focused.prefab",
            "Assets/_LASTRAIL/UI/Common/InputFields/LRInputField_Error.prefab",
            "Assets/_LASTRAIL/UI/Common/InputFields/LRInputField_Disabled.prefab",
            "Assets/_LASTRAIL/UI/Common/LRToggle_Checkbox.prefab",
            "Assets/_LASTRAIL/UI/Common/LRSegmentedControl.prefab",
            "Assets/_LASTRAIL/UI/Common/LRModalPanel.prefab",
            "Assets/_LASTRAIL/UI/Common/LRToastNotification.prefab",
            "Assets/_LASTRAIL/UI/Common/LRTooltipCallout.prefab",
            "Assets/_LASTRAIL/UI/Common/Rows/LRListRow.prefab",
            "Assets/_LASTRAIL/UI/Common/Rows/LRCardRow.prefab",
            "Assets/_LASTRAIL/UI/Common/Rows/LRRewardRow.prefab"
        };

        private static readonly string[] RequiredIconPaths =
        {
            "Assets/_LASTRAIL/UI/Icons/ic_back.png",
            "Assets/_LASTRAIL/UI/Icons/ic_close.png",
            "Assets/_LASTRAIL/UI/Icons/ic_settings.png",
            "Assets/_LASTRAIL/UI/Icons/ic_retry.png",
            "Assets/_LASTRAIL/UI/Icons/ic_info.png",
            "Assets/_LASTRAIL/UI/Icons/ic_warning.png",
            "Assets/_LASTRAIL/UI/Icons/ic_ticket.png",
            "Assets/_LASTRAIL/UI/Icons/ic_reward.png",
            "Assets/_LASTRAIL/UI/Icons/ic_codex.png",
            "Assets/_LASTRAIL/UI/Icons/ic_operator.png",
            "Assets/_LASTRAIL/UI/Icons/ic_mission.png",
            "Assets/_LASTRAIL/UI/Icons/ic_gacha.png",
            "Assets/_LASTRAIL/UI/Icons/ic_sound.png",
            "Assets/_LASTRAIL/UI/Icons/ic_vibration.png",
            "Assets/_LASTRAIL/UI/Icons/ic_lock.png",
            "Assets/_LASTRAIL/UI/Icons/ic_unlock.png"
        };

        [MenuItem("LASTRAIL/Launch/Validate Common UI Kit")]
        public static void ValidateCommonUiKit()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailUiKitQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "ui-kit-validation.json");

            try
            {
                ValidateTmp();
                ValidatePrefabs();
                ValidateIcons();
                ValidateStyleJson();
                ValidateShowcaseScene();
                ValidateForbiddenText();

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"prefabCount\":20,\"iconCount\":16,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL common UI kit validation PASS: " + reportPath);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"prefabCount\":20,\"iconCount\":16,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"ui_kit_validation_failed\"]}");
                Debug.LogError("LASTRAIL common UI kit validation failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void ValidateTmp()
        {
            Require(AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null, "TMP Settings missing.");
            Require(TMP_Settings.defaultStyleSheet != null, "TMP default style sheet missing.");
            Require(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null, "Pretendard TMP SDF font asset missing.");
            Require(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null, "Pretendard TMP material preset missing.");
        }

        private static void ValidatePrefabs()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            foreach (var path in RequiredPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab != null, "Missing prefab: " + path);
                Require(prefab.GetComponentsInChildren<Text>(includeInactive: true).Length == 0, "Legacy Text in " + path);
                Require(prefab.GetComponent<LastRailUiStyleTag>() != null, "Missing style tag in " + path);

                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
                {
                    Require(text.font == font, "Unexpected font in " + path + ":" + text.name);
                    Require(text.fontSharedMaterial == material, "Unexpected material in " + path + ":" + text.name);
                    Require(text.enableAutoSizing, "Autosize disabled in " + path + ":" + text.name);
                    Require(text.overflowMode == TextOverflowModes.Truncate, "Overflow policy mismatch in " + path + ":" + text.name);
                }

                foreach (var selectable in prefab.GetComponentsInChildren<Selectable>(includeInactive: true))
                {
                    var rect = selectable.GetComponent<RectTransform>();
                    Require(rect != null, "Selectable without RectTransform in " + path + ":" + selectable.name);
                    Require(rect.sizeDelta.x >= 48f && rect.sizeDelta.y >= 48f, "Touch target below 48dp in " + path + ":" + selectable.name);
                }
            }

            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Primary.prefab").GetComponent<LastRailUiButton>() != null, "Button wrapper missing.");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/UI/Common/InputFields/LRInputField_Normal.prefab").GetComponent<LastRailUiInputField>() != null, "Input wrapper missing.");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/UI/Common/LRToggle_Checkbox.prefab").GetComponent<LastRailUiToggle>() != null, "Toggle wrapper missing.");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/UI/Common/LRSegmentedControl.prefab").GetComponent<LastRailUiSegmentedControl>() != null, "Segmented wrapper missing.");
        }

        private static void ValidateIcons()
        {
            foreach (var path in RequiredIconPaths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Require(texture != null, "Missing icon: " + path);
                Require(texture.width == 128 && texture.height == 128, "Icon must be 128x128: " + path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null, "Missing icon importer: " + path);
                Require(importer.textureType == TextureImporterType.Sprite, "Icon is not Sprite: " + path);
                Require(Mathf.Approximately(importer.spritePixelsPerUnit, 100f), "Icon PPU mismatch: " + path);
            }
        }

        private static void ValidateStyleJson()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), StyleJsonPath);
            Require(File.Exists(fullPath), "Style JSON missing.");
            var json = File.ReadAllText(fullPath);
            Require(json.Contains("\"referenceResolution\": \"1080x1920\""), "Reference resolution token missing.");
            Require(json.Contains("\"minDp\": 48"), "Touch token missing.");
            Require(json.Contains("\"decorativeOrbsAllowed\": false"), "Orb policy missing.");
            Require(json.Contains("\"serverTruthFieldsAllowed\": false"), "Server truth policy missing.");
        }

        private static void ValidateShowcaseScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Require(prefab != null, "Showcase prefab missing.");
            var canvasScaler = prefab.GetComponent<CanvasScaler>();
            Require(canvasScaler != null, "Showcase CanvasScaler missing.");
            Require(canvasScaler.referenceResolution == new Vector2(1080f, 1920f), "Showcase reference resolution mismatch.");
            Require(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length > 20, "Showcase TMP text coverage too low.");

            var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            Require(scene.IsValid(), "Showcase scene invalid.");
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Require(eventSystems.Length == 1, "Showcase scene must have one EventSystem.");
            Require(eventSystems[0].GetComponent<InputSystemUIInputModule>() != null, "Input System UI module missing.");
            Require(eventSystems[0].GetComponent<StandaloneInputModule>() == null, "Legacy StandaloneInputModule found.");
        }

        private static void ValidateForbiddenText()
        {
            var roots = new[]
            {
                "Assets/_LASTRAIL/UI/Common",
                "Assets/_LASTRAIL/UI/Icons",
                "Assets/_LASTRAIL/UI/Style"
            };
            var forbidden = new[]
            {
                "accessToken",
                "refreshToken",
                "idToken",
                "client_secret",
                "accountId",
                "rawRng",
                "probability",
                "rewardAmount",
                "storyProgressMultiplier",
                "offlineReward",
                "RailChoice",
                "World Raid"
            };

            foreach (var root in roots)
            {
                var fullRoot = Path.Combine(Directory.GetCurrentDirectory(), root.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(fullRoot))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(fullRoot, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    foreach (var value in forbidden)
                    {
                        Require(!text.Contains(value), file + " contains forbidden boundary token: " + value);
                    }
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
