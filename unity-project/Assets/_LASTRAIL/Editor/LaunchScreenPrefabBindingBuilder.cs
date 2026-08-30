#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchScreenPrefabBindingBuilder
    {
        private const string PrefabRoot = "Assets/_LASTRAIL/Prefabs/Screens";

        [MenuItem("LASTRAIL/Launch/Build Screen Binding Prefabs")]
        public static void BuildLaunchScreenPrefabs()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateMissionShellPrefab();
            CreateGachaPrefab();
            CreateCombatHudPrefab();
            CreateCodexPrefab();
            CreateRewardStoryPrefab("RewardReviewScreen", "작전 보상");
            CreateRewardStoryPrefab("OfflineRewardScreen", "오프라인 보상");
            CreateRewardStoryPrefab("StoryProgressSummaryScreen", "스토리 진행");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateMissionShellPrefab()
        {
            var root = CreateScreenRoot("MissionShellScreen");
            try
            {
                var behaviour = root.AddComponent<MissionShellScreenRootBehaviour>();
                var safeArea = CreateSafeArea(root.transform);
                var title = CreateText(safeArea, "TitleText", "작전 선택");
                var subtitle = CreateText(safeArea, "SubtitleText", "Act 1");
                var body = CreateText(safeArea, "BodyText", "브리핑");
                var missionRows = CreateText(safeArea, "MissionRowsText", "작전 목록");
                var primary = CreateButton(safeArea, "PrimaryButton", "시작", out var primaryText);
                var secondary = CreateButton(safeArea, "SecondaryButton", "돌아가기", out var secondaryText);

                Bind(behaviour,
                    ("safeAreaRoot", safeArea),
                    ("titleText", title),
                    ("subtitleText", subtitle),
                    ("bodyText", body),
                    ("missionRowsText", missionRows),
                    ("primaryButtonText", primaryText),
                    ("primaryButton", primary),
                    ("secondaryButtonText", secondaryText),
                    ("secondaryButton", secondary));
                SavePrefab(root, "MissionShellScreen");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateGachaPrefab()
        {
            var root = CreateScreenRoot("GachaScreen");
            try
            {
                var behaviour = root.AddComponent<GachaScreenRootBehaviour>();
                var safeArea = CreateSafeArea(root.transform);
                var title = CreateText(safeArea, "TitleText", "요원 모집");
                var banner = CreateText(safeArea, "BannerText", "Act 1 픽업");
                var status = CreateText(safeArea, "StatusText", "모집 대기");
                var cost = CreateText(safeArea, "CostText", "비용");
                var pity = CreateText(safeArea, "PityText", "보장 정보");
                var revealMode = CreateText(safeArea, "RevealModeText", "Reveal");
                var resultGrid = CreateText(safeArea, "ResultGridText", "결과");
                var error = CreateText(safeArea, "ErrorText", string.Empty);
                var onePull = CreateButton(safeArea, "OnePullButton", "1회 모집", out var onePullText);
                var tenPull = CreateButton(safeArea, "TenPullButton", "10회 모집", out var tenPullText);
                var primaryAction = CreateText(safeArea, "PrimaryActionText", "결과 확인");

                Bind(behaviour,
                    ("safeAreaRoot", safeArea),
                    ("titleText", title),
                    ("bannerText", banner),
                    ("statusText", status),
                    ("costText", cost),
                    ("pityText", pity),
                    ("revealModeText", revealMode),
                    ("resultGridText", resultGrid),
                    ("errorText", error),
                    ("onePullButtonText", onePullText),
                    ("onePullButton", onePull),
                    ("tenPullButtonText", tenPullText),
                    ("tenPullButton", tenPull),
                    ("primaryActionText", primaryAction));
                SavePrefab(root, "GachaScreen");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCombatHudPrefab()
        {
            var root = CreateScreenRoot("FullyAnimatedCombatHud");
            try
            {
                var behaviour = root.AddComponent<FullyAnimatedCombatHudRootBehaviour>();
                var safeArea = CreateSafeArea(root.transform);
                var encounterTitle = CreateText(safeArea, "EncounterTitleText", "전투");
                var waveCounter = CreateText(safeArea, "WaveCounterText", "Wave 1/15");
                var bossStatus = CreateText(safeArea, "BossStatusText", "Boss");
                var objective = CreateText(safeArea, "ObjectiveText", "목표");
                var refundHint = CreateText(safeArea, "RefundHintText", string.Empty);
                var unitRefs = CreateText(safeArea, "UnitRefsText", "Unit refs");
                var cueRefs = CreateText(safeArea, "CueRefsText", "Cue refs");

                Bind(behaviour,
                    ("safeAreaRoot", safeArea),
                    ("encounterTitleText", encounterTitle),
                    ("waveCounterText", waveCounter),
                    ("bossStatusText", bossStatus),
                    ("objectiveText", objective),
                    ("refundHintText", refundHint),
                    ("unitRefsText", unitRefs),
                    ("cueRefsText", cueRefs));
                SavePrefab(root, "FullyAnimatedCombatHud");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCodexPrefab()
        {
            var root = CreateScreenRoot("CodexArchiveScreen");
            try
            {
                var behaviour = root.AddComponent<CodexArchiveScreenRootBehaviour>();
                var safeArea = CreateSafeArea(root.transform);
                var title = CreateText(safeArea, "TitleText", "기록 보관소");
                var subtitle = CreateText(safeArea, "SubtitleText", "Act 1");
                var entries = CreateText(safeArea, "EntriesText", "기록");
                var overflow = CreateText(safeArea, "OverflowStatusText", "문구 길이 확인 완료");
                var missionSelect = CreateButton(safeArea, "MissionSelectButton", "작전 선택", out var missionSelectText);
                var result = CreateButton(safeArea, "ResultButton", "결과", out var resultText);
                var rewardReview = CreateButton(safeArea, "RewardReviewButton", "보상", out var rewardReviewText);

                Bind(behaviour,
                    ("safeAreaRoot", safeArea),
                    ("titleText", title),
                    ("subtitleText", subtitle),
                    ("entriesText", entries),
                    ("overflowStatusText", overflow),
                    ("missionSelectButtonText", missionSelectText),
                    ("missionSelectButton", missionSelect),
                    ("resultButtonText", resultText),
                    ("resultButton", result),
                    ("rewardReviewButtonText", rewardReviewText),
                    ("rewardReviewButton", rewardReview));
                SavePrefab(root, "CodexArchiveScreen");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateRewardStoryPrefab(string prefabName, string titleTextValue)
        {
            var root = CreateScreenRoot(prefabName);
            try
            {
                var behaviour = root.AddComponent<RewardStoryScreenRootBehaviour>();
                var safeArea = CreateSafeArea(root.transform);
                var title = CreateText(safeArea, "TitleText", titleTextValue);
                var subtitle = CreateText(safeArea, "SubtitleText", "서버 응답 표시");
                var body = CreateText(safeArea, "BodyText", "상태");
                var rewardLines = CreateText(safeArea, "RewardLinesText", "보상 라인");
                var status = CreateText(safeArea, "StatusText", "표시 상태");
                var primary = CreateButton(safeArea, "PrimaryButton", "확인", out var primaryText);
                var secondary = CreateButton(safeArea, "SecondaryButton", "돌아가기", out var secondaryText);
                var codex = CreateButton(safeArea, "CodexButton", "기록 보관소", out var codexText);

                Bind(behaviour,
                    ("safeAreaRoot", safeArea),
                    ("titleText", title),
                    ("subtitleText", subtitle),
                    ("bodyText", body),
                    ("rewardLinesText", rewardLines),
                    ("statusText", status),
                    ("primaryButtonText", primaryText),
                    ("primaryButton", primary),
                    ("secondaryButtonText", secondaryText),
                    ("secondaryButton", secondary),
                    ("codexButtonText", codexText),
                    ("codexButton", codex));
                SavePrefab(root, prefabName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateScreenRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080f, 1920f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            return root;
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            var safeArea = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(parent, false);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = new Vector2(24f, 24f);
            safeArea.offsetMax = new Vector2(-24f, -24f);
            return safeArea;
        }

        private static Text CreateText(Transform parent, string name, string value)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(960f, 96f);

            var text = gameObject.GetComponent<Text>();
            text.text = value ?? string.Empty;
            text.font = GetDefaultFont();
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 32;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, out Text labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 64f);
            buttonObject.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.16f, 1f);

            var labelObject = new GameObject(name + "Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            labelText = labelObject.GetComponent<Text>();
            labelText.text = label;
            labelText.font = GetDefaultFont();
            labelText.fontSize = 28;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 18;
            labelText.resizeTextMaxSize = 28;
            return buttonObject.GetComponent<Button>();
        }

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void Bind(UnityEngine.Object target, params (string FieldName, UnityEngine.Object Value)[] bindings)
        {
            var serialized = new SerializedObject(target);
            foreach (var binding in bindings)
            {
                var property = serialized.FindProperty(binding.FieldName);
                if (property == null)
                {
                    throw new InvalidOperationException("Missing serialized field: " + binding.FieldName);
                }

                property.objectReferenceValue = binding.Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject root, string prefabName)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Path.Combine(PrefabRoot, prefabName + ".prefab").Replace('\\', '/'));
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                var folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
