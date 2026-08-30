#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

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
            foreach (var text in prefab.GetComponentsInChildren<Text>(includeInactive: true))
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
