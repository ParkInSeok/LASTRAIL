#if UNITY_5_3_OR_NEWER
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;
using TMPro;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchScreenPrefabBindingTests
    {
        private const string ScreenPrefabRoot = "Assets/_LASTRAIL/Prefabs/Screens/";

        [Test]
        public void LaunchScreenPrefabs_ExistForSevenPlayerFacingScreens()
        {
            AssertPrefabExists<MissionShellScreenRootBehaviour>("MissionShellScreen.prefab");
            AssertPrefabExists<GachaScreenRootBehaviour>("GachaScreen.prefab");
            AssertPrefabExists<FullyAnimatedCombatHudRootBehaviour>("FullyAnimatedCombatHud.prefab");
            AssertPrefabExists<CodexArchiveScreenRootBehaviour>("CodexArchiveScreen.prefab");
            AssertPrefabExists<RewardStoryScreenRootBehaviour>("RewardReviewScreen.prefab");
            AssertPrefabExists<RewardStoryScreenRootBehaviour>("OfflineRewardScreen.prefab");
            AssertPrefabExists<RewardStoryScreenRootBehaviour>("StoryProgressSummaryScreen.prefab");
        }

        [Test]
        public void LaunchScreenPrefabs_HaveBoundSafeAreaAndReadableTextSurfaces()
        {
            AssertBoundFields<MissionShellScreenRootBehaviour>(
                "MissionShellScreen.prefab",
                "safeAreaRoot",
                "titleText",
                "subtitleText",
                "bodyText",
                "missionRowsText",
                "primaryButtonText",
                "primaryButton",
                "secondaryButtonText",
                "secondaryButton");
            AssertBoundFields<GachaScreenRootBehaviour>(
                "GachaScreen.prefab",
                "safeAreaRoot",
                "titleText",
                "bannerText",
                "statusText",
                "costText",
                "pityText",
                "revealModeText",
                "resultGridText",
                "errorText",
                "onePullButtonText",
                "onePullButton",
                "tenPullButtonText",
                "tenPullButton",
                "primaryActionText");
            AssertBoundFields<FullyAnimatedCombatHudRootBehaviour>(
                "FullyAnimatedCombatHud.prefab",
                "safeAreaRoot",
                "encounterTitleText",
                "waveCounterText",
                "bossStatusText",
                "objectiveText",
                "refundHintText",
                "unitRefsText",
                "cueRefsText");
            AssertBoundFields<CodexArchiveScreenRootBehaviour>(
                "CodexArchiveScreen.prefab",
                "safeAreaRoot",
                "titleText",
                "subtitleText",
                "entriesText",
                "overflowStatusText",
                "missionSelectButtonText",
                "missionSelectButton",
                "resultButtonText",
                "resultButton",
                "rewardReviewButtonText",
                "rewardReviewButton");
            AssertRewardStoryBindings("RewardReviewScreen.prefab");
            AssertRewardStoryBindings("OfflineRewardScreen.prefab");
            AssertRewardStoryBindings("StoryProgressSummaryScreen.prefab");
        }

        [Test]
        public void LaunchScreenPrefabs_KeepTouchTargetsAtLeastFortyEightDp()
        {
            AssertButtonsMeetMinimumTouchTarget("MissionShellScreen.prefab");
            AssertButtonsMeetMinimumTouchTarget("GachaScreen.prefab");
            AssertButtonsMeetMinimumTouchTarget("CodexArchiveScreen.prefab");
            AssertButtonsMeetMinimumTouchTarget("RewardReviewScreen.prefab");
            AssertButtonsMeetMinimumTouchTarget("OfflineRewardScreen.prefab");
            AssertButtonsMeetMinimumTouchTarget("StoryProgressSummaryScreen.prefab");
        }

        [Test]
        public void LaunchScreenPrefabs_DistributeReadableTextSlotsAcrossPortraitSafeArea()
        {
            AssertReadableTextSlotsAreDistributed("MissionShellScreen.prefab", 4);
            AssertReadableTextSlotsAreDistributed("GachaScreen.prefab", 7);
            AssertReadableTextSlotsAreDistributed("FullyAnimatedCombatHud.prefab", 7);
            AssertReadableTextSlotsAreDistributed("CodexArchiveScreen.prefab", 4);
            AssertReadableTextSlotsAreDistributed("RewardReviewScreen.prefab", 5);
            AssertReadableTextSlotsAreDistributed("OfflineRewardScreen.prefab", 5);
            AssertReadableTextSlotsAreDistributed("StoryProgressSummaryScreen.prefab", 5);
        }

        [Test]
        public void LaunchScreenPrefabs_UseTextMeshProTextSurfaces()
        {
            AssertUsesTextMeshProWithoutLegacyText("MissionShellScreen.prefab");
            AssertUsesTextMeshProWithoutLegacyText("GachaScreen.prefab");
            AssertUsesTextMeshProWithoutLegacyText("FullyAnimatedCombatHud.prefab");
            AssertUsesTextMeshProWithoutLegacyText("CodexArchiveScreen.prefab");
            AssertUsesTextMeshProWithoutLegacyText("RewardReviewScreen.prefab");
            AssertUsesTextMeshProWithoutLegacyText("OfflineRewardScreen.prefab");
            AssertUsesTextMeshProWithoutLegacyText("StoryProgressSummaryScreen.prefab");
        }

        [Test]
        public void LaunchScreenPrefabs_UseLaunchKoreanTextMeshProFontAsset()
        {
            AssertUsesLaunchKoreanFont("MissionShellScreen.prefab");
            AssertUsesLaunchKoreanFont("GachaScreen.prefab");
            AssertUsesLaunchKoreanFont("FullyAnimatedCombatHud.prefab");
            AssertUsesLaunchKoreanFont("CodexArchiveScreen.prefab");
            AssertUsesLaunchKoreanFont("RewardReviewScreen.prefab");
            AssertUsesLaunchKoreanFont("OfflineRewardScreen.prefab");
            AssertUsesLaunchKoreanFont("StoryProgressSummaryScreen.prefab");
        }

        [Test]
        public void LaunchKoreanFontLicenseNotice_ExistsForReleasePackaging()
        {
            var licensePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Fonts", "Pretendard", "LICENSE-Pretendard-OFL.txt");
            var readmePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "_LASTRAIL", "UI", "Fonts", "Pretendard", "README-LASTRAIL.md");
            Assert.That(File.Exists(licensePath), Is.True, "Pretendard OFL license notice must ship near launch font assets.");
            Assert.That(File.ReadAllText(licensePath), Does.Contain("SIL OPEN FONT LICENSE Version 1.1"));
            Assert.That(File.Exists(readmePath), Is.True, "LASTRAIL font attribution README missing.");
            Assert.That(File.ReadAllText(readmePath), Does.Contain("Release package must include"));
        }
        [Test]
        public void ProjectSettings_UseValidNewInputSystemHandler()
        {
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "ProjectSettings.asset");
            var settings = File.ReadAllText(settingsPath);
            Assert.That(settings, Does.Not.Contain("activeInputHandler: -1"));
            Assert.That(settings, Does.Contain("activeInputHandler: 1").Or.Contain("activeInputHandler: 2"));
        }

        [Test]
        public void AndroidSmokeScene_UsesInputSystemUiModuleInsteadOfStandaloneInputModule()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_LASTRAIL/Scenes/AndroidBuildSmoke.unity");
            Assert.That(sceneAsset, Is.Not.Null);

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                AssetDatabase.GetAssetPath(sceneAsset),
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            try
            {
                var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                Assert.That(eventSystems.Length, Is.EqualTo(1));
                Assert.That(eventSystems[0].GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                Assert.That(eventSystems[0].GetComponent<StandaloneInputModule>(), Is.Null);
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
            }
        }

        [Test]
        public void LaunchScreenRoots_ExposeCallbackActionsWithoutParentReadingChildButtons()
        {
            AssertMissionShellCallbacks();
            AssertGachaCallbacks();
            AssertCodexCallbacks();
            AssertRewardStoryCallbacks("RewardReviewScreen.prefab");
            AssertRewardStoryCallbacks("OfflineRewardScreen.prefab");
            AssertRewardStoryCallbacks("StoryProgressSummaryScreen.prefab");
        }

        [Test]
        public void LaunchScreenRootCallbacks_DoNotDuplicateButtonListenersWhenRebound()
        {
            var root = InstantiateScreenRoot<MissionShellScreenRootBehaviour>("MissionShellScreen.prefab");
            try
            {
                var primaryCount = 0;
                root.PrimaryActionRequested += () => primaryCount++;
                root.BindInteractionCallbacks();
                root.BindInteractionCallbacks();

                var button = GetSerializedObjectReference<Button>(root, "primaryButton");
                button.interactable = true;
                button.onClick.Invoke();

                Assert.That(primaryCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void AssertRewardStoryBindings(string prefabName)
        {
            AssertBoundFields<RewardStoryScreenRootBehaviour>(
                prefabName,
                "safeAreaRoot",
                "titleText",
                "subtitleText",
                "bodyText",
                "rewardLinesText",
                "statusText",
                "primaryButtonText",
                "primaryButton",
                "secondaryButtonText",
                "secondaryButton",
                "codexButtonText",
                "codexButton");
        }

        private static void AssertMissionShellCallbacks()
        {
            var root = InstantiateScreenRoot<MissionShellScreenRootBehaviour>("MissionShellScreen.prefab");
            try
            {
                var primaryCount = 0;
                var secondaryCount = 0;
                root.PrimaryActionRequested += () => primaryCount++;
                root.SecondaryActionRequested += () => secondaryCount++;
                root.BindInteractionCallbacks();

                ClickSerializedButton(root, "primaryButton");
                ClickSerializedButton(root, "secondaryButton");

                Assert.That(primaryCount, Is.EqualTo(1));
                Assert.That(secondaryCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void AssertGachaCallbacks()
        {
            var root = InstantiateScreenRoot<GachaScreenRootBehaviour>("GachaScreen.prefab");
            try
            {
                var onePullCount = 0;
                var tenPullCount = 0;
                root.OnePullRequested += () => onePullCount++;
                root.TenPullRequested += () => tenPullCount++;
                root.BindInteractionCallbacks();

                ClickSerializedButton(root, "onePullButton");
                ClickSerializedButton(root, "tenPullButton");

                Assert.That(onePullCount, Is.EqualTo(1));
                Assert.That(tenPullCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void AssertCodexCallbacks()
        {
            var root = InstantiateScreenRoot<CodexArchiveScreenRootBehaviour>("CodexArchiveScreen.prefab");
            try
            {
                var missionSelectCount = 0;
                var resultCount = 0;
                var rewardReviewCount = 0;
                root.MissionSelectRequested += () => missionSelectCount++;
                root.ResultRequested += () => resultCount++;
                root.RewardReviewRequested += () => rewardReviewCount++;
                root.BindInteractionCallbacks();

                ClickSerializedButton(root, "missionSelectButton");
                ClickSerializedButton(root, "resultButton");
                ClickSerializedButton(root, "rewardReviewButton");

                Assert.That(missionSelectCount, Is.EqualTo(1));
                Assert.That(resultCount, Is.EqualTo(1));
                Assert.That(rewardReviewCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void AssertRewardStoryCallbacks(string prefabName)
        {
            var root = InstantiateScreenRoot<RewardStoryScreenRootBehaviour>(prefabName);
            try
            {
                var primaryCount = 0;
                var secondaryCount = 0;
                var codexCount = 0;
                root.PrimaryActionRequested += () => primaryCount++;
                root.SecondaryActionRequested += () => secondaryCount++;
                root.CodexArchiveRequested += () => codexCount++;
                root.BindInteractionCallbacks();

                ClickSerializedButton(root, "primaryButton");
                ClickSerializedButton(root, "secondaryButton");
                ClickSerializedButton(root, "codexButton");

                Assert.That(primaryCount, Is.EqualTo(1));
                Assert.That(secondaryCount, Is.EqualTo(1));
                Assert.That(codexCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void AssertPrefabExists<TComponent>(string prefabName)
            where TComponent : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            Assert.That(prefab.GetComponent<TComponent>(), Is.Not.Null, prefabName);
        }

        private static void AssertBoundFields<TComponent>(string prefabName, params string[] fieldNames)
            where TComponent : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            var component = prefab.GetComponent<TComponent>();
            Assert.That(component, Is.Not.Null, prefabName);
            var serialized = new SerializedObject(component);
            foreach (var fieldName in fieldNames)
            {
                var property = serialized.FindProperty(fieldName);
                Assert.That(property, Is.Not.Null, fieldName);
                Assert.That(property.objectReferenceValue, Is.Not.Null, prefabName + ":" + fieldName);
            }
        }

        private static void AssertButtonsMeetMinimumTouchTarget(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            foreach (var button in prefab.GetComponentsInChildren<Button>(includeInactive: true))
            {
                var rectTransform = button.GetComponent<RectTransform>();
                Assert.That(rectTransform, Is.Not.Null, button.name);
                Assert.That(rectTransform.sizeDelta.x, Is.GreaterThanOrEqualTo(48f), button.name);
                Assert.That(rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(48f), button.name);
            }
        }

        private static void AssertReadableTextSlotsAreDistributed(string prefabName, int minimumDistinctY)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            var yPositions = new HashSet<int>();
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                if (text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null)
                {
                    continue;
                }

                var rectTransform = text.GetComponent<RectTransform>();
                Assert.That(rectTransform, Is.Not.Null, text.name);
                yPositions.Add(Mathf.RoundToInt(rectTransform.anchoredPosition.y));
                Assert.That(rectTransform.sizeDelta.x, Is.GreaterThanOrEqualTo(720f), prefabName + ":" + text.name);
                Assert.That(rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(64f), prefabName + ":" + text.name);
            }

            Assert.That(
                yPositions.Count,
                Is.GreaterThanOrEqualTo(minimumDistinctY),
                prefabName + " text slots overlap instead of using the portrait safe area.");
        }

        private static void AssertUsesTextMeshProWithoutLegacyText(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length, Is.GreaterThan(0), prefabName);
            Assert.That(prefab.GetComponentsInChildren<Text>(includeInactive: true).Length, Is.EqualTo(0), prefabName);
        }

        private static void AssertUsesLaunchKoreanFont(string prefabName)
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
            Assert.That(fontAsset, Is.Not.Null, "Launch Korean TMP SDF font asset missing.");
            Assert.That(fontAsset.name, Does.Contain("Pretendard-Regular SDF"));

            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat");
            Assert.That(material, Is.Not.Null, "Launch Korean TMP SDF material preset missing.");
            Assert.That(material.name, Does.Contain("Pretendard-Regular SDF UI"));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                Assert.That(text.font, Is.EqualTo(fontAsset), prefabName + ":" + text.name);
                Assert.That(text.fontSharedMaterial, Is.EqualTo(material), prefabName + ":" + text.name);
            }
        }
        private static TComponent InstantiateScreenRoot<TComponent>(string prefabName)
            where TComponent : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPrefabRoot + prefabName);
            Assert.That(prefab, Is.Not.Null, prefabName);
            var instance = Object.Instantiate(prefab);
            var component = instance.GetComponent<TComponent>();
            Assert.That(component, Is.Not.Null, prefabName);
            return component;
        }

        private static void ClickSerializedButton(Component owner, string fieldName)
        {
            var button = GetSerializedObjectReference<Button>(owner, fieldName);
            button.interactable = true;
            button.onClick.Invoke();
        }

        private static TReference GetSerializedObjectReference<TReference>(Component owner, string fieldName)
            where TReference : Object
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, fieldName);
            var value = property.objectReferenceValue as TReference;
            Assert.That(value, Is.Not.Null, fieldName);
            return value;
        }
    }
}
#endif
