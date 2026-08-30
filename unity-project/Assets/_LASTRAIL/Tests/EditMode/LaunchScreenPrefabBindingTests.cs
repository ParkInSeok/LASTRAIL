#if UNITY_5_3_OR_NEWER
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
    }
}
#endif
