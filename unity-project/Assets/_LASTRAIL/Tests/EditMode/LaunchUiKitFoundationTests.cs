#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchUiKitFoundationTests
    {
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string StyleJsonPath = "Assets/_LASTRAIL/UI/Style/lastrail-ui-tokens.json";
        private const string ReadinessJsonPath = "Assets/_LASTRAIL/UI/Style/lastrail-ui-kit-readiness.json";
        private const string ShowcasePrefabPath = "Assets/_LASTRAIL/UI/Common/Showcase/LRLaunchUiKitShowcase.prefab";
        private const string ShowcaseScenePath = "Assets/_LASTRAIL/Scenes/LaunchUiKitShowcase.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string SkinRoot = "Assets/_LASTRAIL/UI/Common/Skins";

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

        private static readonly string[] RequiredSkinSpritePaths =
        {
            SkinRoot + "/ui_button_primary_9slice.png",
            SkinRoot + "/ui_button_secondary_9slice.png",
            SkinRoot + "/ui_button_danger_9slice.png",
            SkinRoot + "/ui_button_disabled_9slice.png",
            SkinRoot + "/ui_button_icon_9slice.png",
            SkinRoot + "/ui_slider_track_9slice.png",
            SkinRoot + "/ui_slider_fill_9slice.png",
            SkinRoot + "/ui_slider_handle_9slice.png",
            SkinRoot + "/ui_input_normal_9slice.png",
            SkinRoot + "/ui_input_focused_9slice.png",
            SkinRoot + "/ui_input_error_9slice.png",
            SkinRoot + "/ui_toggle_off_9slice.png",
            SkinRoot + "/ui_toggle_on_9slice.png",
            SkinRoot + "/ui_tab_selected_9slice.png",
            SkinRoot + "/ui_tab_unselected_9slice.png",
            SkinRoot + "/ui_modal_panel_9slice.png",
            SkinRoot + "/ui_toast_panel_9slice.png",
            SkinRoot + "/ui_tooltip_panel_9slice.png",
            SkinRoot + "/ui_list_row_9slice.png",
            SkinRoot + "/ui_reward_row_9slice.png",
            SkinRoot + "/ui_scroll_track_9slice.png",
            SkinRoot + "/ui_scroll_thumb_9slice.png",
            SkinRoot + "/ui_status_chip_9slice.png"
        };

        [Test]
        public void CommonUiKitPrefabs_ExistWithTmpPretendardAndNoLegacyText()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath), Is.Not.Null, "TMP Essential Resources must be imported for launch UI rendering.");
            Assert.That(TMP_Settings.defaultStyleSheet, Is.Not.Null, "TMP default style sheet must be available for TMP mesh generation.");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(material, Is.Not.Null);

            foreach (var path in RequiredPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true), Is.Empty, path + " uses legacy UnityEngine.UI.Text");

                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
                {
                    Assert.That(text.font, Is.EqualTo(font), path + ":" + text.name);
                    Assert.That(text.fontSharedMaterial, Is.EqualTo(material), path + ":" + text.name);
                    Assert.That(text.enableAutoSizing, Is.True, path + ":" + text.name);
                    Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Truncate), path + ":" + text.name);
                }
            }
        }

        [Test]
        public void CommonUiKitInteractiveControls_MeetFortyEightDpTouchTarget()
        {
            foreach (var path in RequiredPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                foreach (var selectable in prefab.GetComponentsInChildren<Selectable>(includeInactive: true))
                {
                    var rect = selectable.GetComponent<RectTransform>();
                    Assert.That(rect, Is.Not.Null, path + ":" + selectable.name);
                    Assert.That(rect.sizeDelta.x, Is.GreaterThanOrEqualTo(48f), path + ":" + selectable.name + " width");
                    Assert.That(rect.sizeDelta.y, Is.GreaterThanOrEqualTo(48f), path + ":" + selectable.name + " height");
                }
            }
        }

        [Test]
        public void CommonUiKitIcons_ExistAsSpriteImportableMobileAssets()
        {
            foreach (var path in RequiredIconPaths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(128), path + " width");
                Assert.That(texture.height, Is.EqualTo(128), path + " height");

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), path);
            }
        }

        [Test]
        public void CommonUiKitSkinSprites_ExistAsSlicedHighQualityMobileAssets()
        {
            foreach (var path in RequiredSkinSpritePaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteBorder.x + importer.spriteBorder.y + importer.spriteBorder.z + importer.spriteBorder.w, Is.GreaterThan(0f), path + " must be 9-slice ready");
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
            }
        }

        [Test]
        public void CommonUiKitPrefabs_UseHighQualitySlicedSkinSpritesOnPrimarySurfaces()
        {
            foreach (var path in RequiredPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);

                var styledImages = 0;
                foreach (var image in prefab.GetComponentsInChildren<Image>(includeInactive: true))
                {
                    if (image.sprite == null)
                    {
                        continue;
                    }

                    var spritePath = AssetDatabase.GetAssetPath(image.sprite);
                    if (!spritePath.StartsWith(SkinRoot, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    styledImages++;
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), path + ":" + image.name);
                    Assert.That(image.raycastTarget || image.transform.parent != null, Is.True, path + ":" + image.name);
                }

                Assert.That(styledImages, Is.GreaterThanOrEqualTo(1), path + " has no high-quality common skin sprite");
            }
        }

        [Test]
        public void CommonUiKitStyleTokens_ParseAndKeepLaunchReadabilityPolicy()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), StyleJsonPath);
            Assert.That(File.Exists(fullPath), Is.True, StyleJsonPath);

            var tokens = JsonUtility.FromJson<StyleTokenFixture>(File.ReadAllText(fullPath));
            Assert.That(tokens.name, Is.EqualTo("LAST RAIL Act 1 Launch UI Kit"));
            Assert.That(tokens.touch.minDp, Is.EqualTo(48));
            Assert.That(tokens.layout.referenceResolution, Is.EqualTo("1080x1920"));
            Assert.That(tokens.typography.fontAsset, Is.EqualTo(FontPath));
            Assert.That(tokens.typography.materialPreset, Is.EqualTo(MaterialPath));
            Assert.That(tokens.surface.maxCardRadiusPx, Is.LessThanOrEqualTo(8));
            Assert.That(tokens.palette.accentCopper, Is.Not.EqualTo(tokens.palette.railBlue));
            Assert.That(tokens.policy.decorativeOrbsAllowed, Is.False);
            Assert.That(tokens.policy.serverTruthFieldsAllowed, Is.False);
        }

        [Test]
        public void CommonUiKitReadiness_ReportIncludesSkinQualityAndLaunchApplicationCounts()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ReadinessJsonPath);
            Assert.That(File.Exists(fullPath), Is.True, ReadinessJsonPath);

            var json = File.ReadAllText(fullPath);
            Assert.That(json, Does.Contain(@"""qualityClass"": ""HIGH_QUALITY_2D_UI_SKIN"""));
            Assert.That(json, Does.Contain(@"""nineSliceSpriteCount"": 23"));
            Assert.That(json, Does.Contain(@"""appliedPrefabCount"": 20"));
            Assert.That(json, Does.Contain(@"""launchScreenConsumerCount"": 7"));
            Assert.That(json, Does.Contain(@"""sourceKind"": ""image_gen_contact_sheet_crops"""));
        }

        [Test]
        public void CommonUiKitShowcase_IsPortraitSafeAndUsesInputSystemEventModule()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            Assert.That(prefab, Is.Not.Null, ShowcasePrefabPath);
            var rootRect = prefab.GetComponent<RectTransform>();
            Assert.That(rootRect, Is.Not.Null);
            var canvasScaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(canvasScaler, Is.Not.Null);
            Assert.That(canvasScaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThan(20));
            Assert.That(prefab.GetComponentsInChildren<Image>(includeInactive: true).Length, Is.GreaterThanOrEqualTo(RequiredIconPaths.Length));

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath);
            Assert.That(sceneAsset, Is.Not.Null, ShowcaseScenePath);
            var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
            try
            {
                Assert.That(scene.IsValid(), Is.True);
                var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                Assert.That(eventSystems.Length, Is.EqualTo(1));
                Assert.That(eventSystems[0].GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                Assert.That(eventSystems[0].GetComponent<StandaloneInputModule>(), Is.Null);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }


        [Test]
        public void CommonUiKitPrefabs_ExposeCallbackFriendlyRootComponents()
        {
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Primary.prefab", "LastRailUiButton");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Secondary.prefab", "LastRailUiButton");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Danger.prefab", "LastRailUiButton");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Disabled.prefab", "LastRailUiButton");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/Buttons/LRButton_Icon.prefab", "LastRailUiButton");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/InputFields/LRInputField_Normal.prefab", "LastRailUiInputField");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/LRToggle_Checkbox.prefab", "LastRailUiToggle");
            AssertRootComponent("Assets/_LASTRAIL/UI/Common/LRSegmentedControl.prefab", "LastRailUiSegmentedControl");

            foreach (var path in RequiredPrefabPaths)
            {
                AssertRootComponent(path, "LastRailUiStyleTag");
            }
        }
        [Test]
        public void CommonUiKitDoesNotIntroduceServerTruthOrSecretPayloads()
        {
            var roots = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Common"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Icons"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Style")
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
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    foreach (var value in forbidden)
                    {
                        Assert.That(text, Does.Not.Contain(value), file + " leaked forbidden launch boundary token: " + value);
                    }
                }
            }
        }


        private static void AssertRootComponent(string prefabPath, string componentTypeName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(prefab.GetComponent(componentTypeName), Is.Not.Null, prefabPath + " missing " + componentTypeName);
        }
        [Serializable]
        private sealed class StyleTokenFixture
        {
            public string name;
            public LayoutFixture layout;
            public TouchFixture touch;
            public TypographyFixture typography;
            public PaletteFixture palette;
            public SurfaceFixture surface;
            public PolicyFixture policy;
        }

        [Serializable]
        private sealed class LayoutFixture
        {
            public string referenceResolution;
        }

        [Serializable]
        private sealed class TouchFixture
        {
            public int minDp;
        }

        [Serializable]
        private sealed class TypographyFixture
        {
            public string fontAsset;
            public string materialPreset;
        }

        [Serializable]
        private sealed class PaletteFixture
        {
            public string railBlue;
            public string accentCopper;
        }

        [Serializable]
        private sealed class SurfaceFixture
        {
            public int maxCardRadiusPx;
        }

        [Serializable]
        private sealed class PolicyFixture
        {
            public bool decorativeOrbsAllowed;
            public bool serverTruthFieldsAllowed;
        }
    }
}
#endif
