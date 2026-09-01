#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchScreenPrefabBindingBuilder
    {
        private const string PrefabRoot = "Assets/_LASTRAIL/Prefabs/Screens";
        private const string FontRoot = "Assets/_LASTRAIL/UI/Fonts/Pretendard";
        private const string KoreanFontSourcePath = FontRoot + "/Pretendard-Regular.otf";
        private const string KoreanFontAssetPath = FontRoot + "/TMP/Pretendard-Regular SDF.asset";
        private const string KoreanFontMaterialPath = FontRoot + "/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string KeyVisualRoot = "Assets/_LASTRAIL/UI/KeyVisuals";
        private const string GachaIconRoot = "Assets/_LASTRAIL/UI/GachaResources/Icons";
        private const string GachaFxRoot = "Assets/_LASTRAIL/UI/GachaResources/GachaFx";

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
                Place(title, 0f, 780f, 960f, 96f);
                Place(subtitle, 0f, 700f, 960f, 72f);
                Place(body, 0f, 520f, 960f, 220f);
                Place(missionRows, 0f, 260f, 960f, 260f);
                Place(primary.GetComponent<RectTransform>(), -190f, -720f, 320f, 64f);
                Place(secondary.GetComponent<RectTransform>(), 190f, -720f, 320f, 64f);

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
                var bannerKeyVisual = CreateImage(
                    safeArea,
                    "BannerKeyVisualImage",
                    KeyVisualRoot + "/kv_gacha_recruitment_chamber.png",
                    new Color(0.72f, 0.78f, 0.82f, 0.46f));
                var singlePullRevealPlate = CreateImage(
                    safeArea,
                    "SinglePullRevealPlateImage",
                    GachaFxRoot + "/gacha_single_pull_reveal_plate.png",
                    new Color(1f, 1f, 1f, 0.88f));
                var tenPullGridFrame = CreateImage(
                    safeArea,
                    "TenPullGridFrameImage",
                    GachaFxRoot + "/gacha_ten_pull_result_grid_frame.png",
                    new Color(1f, 1f, 1f, 0.92f));
                var rareGlow = CreateImage(
                    safeArea,
                    "RareGlowImage",
                    GachaFxRoot + "/gacha_rare_glow.png",
                    new Color(0.55f, 0.75f, 1f, 0.55f));
                var eliteGlow = CreateImage(
                    safeArea,
                    "EliteGlowImage",
                    GachaFxRoot + "/gacha_elite_glow.png",
                    new Color(1f, 0.86f, 0.42f, 0.55f));
                var pickupEliteGlow = CreateImage(
                    safeArea,
                    "PickupEliteGlowImage",
                    GachaFxRoot + "/gacha_pickup_elite_glow.png",
                    new Color(0.82f, 1f, 0.72f, 0.55f));
                var duplicateConversion = CreateImage(
                    safeArea,
                    "DuplicateConversionImage",
                    GachaFxRoot + "/gacha_duplicate_conversion_visual.png",
                    new Color(1f, 1f, 1f, 0.9f));
                var ticketSpend = CreateImage(
                    safeArea,
                    "TicketSpendImage",
                    GachaFxRoot + "/gacha_ticket_spend_pieces.png",
                    new Color(1f, 1f, 1f, 0.8f));
                var resultCardFrame = CreateImage(
                    safeArea,
                    "ResultCardFrameImage",
                    GachaFxRoot + "/gacha_result_card_frame.png",
                    new Color(1f, 1f, 1f, 0.78f));
                var ticketIcon = CreateImage(
                    safeArea,
                    "TicketIconImage",
                    GachaIconRoot + "/cur_command_ticket.png",
                    Color.white);
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
                ConfigureText(resultGrid, "결과", 21f, TextAlignmentOptions.TopLeft);
                resultGrid.margin = new Vector4(24f, 12f, 24f, 12f);
                resultGrid.lineSpacing = -14f;
                Place(bannerKeyVisual, 0f, 335f, 960f, 760f);
                Place(singlePullRevealPlate, 0f, 130f, 560f, 560f);
                Place(tenPullGridFrame, 0f, -112f, 840f, 520f);
                Place(rareGlow, -240f, 165f, 380f, 380f);
                Place(eliteGlow, 0f, 165f, 410f, 410f);
                Place(pickupEliteGlow, 240f, 165f, 420f, 420f);
                Place(resultCardFrame, 0f, 5f, 320f, 426f);
                Place(duplicateConversion, 318f, -398f, 160f, 160f);
                Place(ticketSpend, -318f, -398f, 160f, 160f);
                Place(ticketIcon, -420f, 500f, 72f, 72f);
                Place(title, 0f, 800f, 960f, 96f);
                Place(banner, 0f, 690f, 960f, 96f);
                Place(status, 0f, 590f, 960f, 72f);
                Place(cost, 46f, 500f, 840f, 72f);
                Place(pity, 0f, 410f, 960f, 72f);
                Place(revealMode, 0f, 300f, 960f, 96f);
                Place(resultGrid, 0f, 110f, 880f, 300f);
                Place(error, 0f, -360f, 960f, 96f);
                Place(primaryAction, 0f, -585f, 960f, 72f);
                Place(onePull.GetComponent<RectTransform>(), -190f, -720f, 320f, 64f);
                Place(tenPull.GetComponent<RectTransform>(), 190f, -720f, 320f, 64f);

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
                    ("primaryActionText", primaryAction),
                    ("bannerKeyVisualImage", bannerKeyVisual),
                    ("ticketIconImage", ticketIcon),
                    ("singlePullRevealPlateImage", singlePullRevealPlate),
                    ("tenPullGridFrameImage", tenPullGridFrame),
                    ("rareGlowImage", rareGlow),
                    ("eliteGlowImage", eliteGlow),
                    ("pickupEliteGlowImage", pickupEliteGlow),
                    ("duplicateConversionImage", duplicateConversion),
                    ("ticketSpendImage", ticketSpend),
                    ("resultCardFrameImage", resultCardFrame));
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
                Place(encounterTitle, 0f, 800f, 960f, 96f);
                Place(waveCounter, 0f, 710f, 960f, 72f);
                Place(bossStatus, 0f, 615f, 960f, 96f);
                Place(objective, 0f, 500f, 960f, 96f);
                Place(unitRefs, 0f, 80f, 960f, 560f);
                Place(cueRefs, 0f, -360f, 960f, 240f);
                Place(refundHint, 0f, -700f, 960f, 96f);

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
                Place(title, 0f, 800f, 960f, 96f);
                Place(subtitle, 0f, 720f, 960f, 72f);
                Place(entries, 0f, 170f, 960f, 940f);
                Place(overflow, 0f, -370f, 960f, 96f);
                Place(missionSelect.GetComponent<RectTransform>(), -300f, -720f, 280f, 64f);
                Place(result.GetComponent<RectTransform>(), 0f, -720f, 240f, 64f);
                Place(rewardReview.GetComponent<RectTransform>(), 300f, -720f, 240f, 64f);

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
                Place(title, 0f, 800f, 960f, 96f);
                Place(subtitle, 0f, 720f, 960f, 72f);
                Place(body, 0f, 520f, 960f, 220f);
                Place(rewardLines, 0f, 140f, 960f, 380f);
                Place(status, 0f, -300f, 960f, 160f);
                Place(primary.GetComponent<RectTransform>(), -300f, -720f, 280f, 64f);
                Place(secondary.GetComponent<RectTransform>(), 0f, -720f, 240f, 64f);
                Place(codex.GetComponent<RectTransform>(), 300f, -720f, 280f, 64f);

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

        private static TMP_Text CreateText(Transform parent, string name, string value)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(960f, 96f);

            var text = gameObject.GetComponent<TextMeshProUGUI>();
            ConfigureText(text, value, 32f, TextAlignmentOptions.MidlineLeft);
            return text;
        }

        private static Image CreateImage(Transform parent, string name, string spritePath, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static void Place(Component component, float x, float y, float width, float height)
        {
            Place(component.GetComponent<RectTransform>(), x, y, width, height);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Button CreateButton(Transform parent, string name, string label, out TMP_Text labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 64f);
            buttonObject.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.16f, 1f);

            var labelObject = new GameObject(name + "Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            labelText = labelObject.GetComponent<TextMeshProUGUI>();
            ConfigureText(labelText, label, 28f, TextAlignmentOptions.Center);
            return buttonObject.GetComponent<Button>();
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException("Missing launch sprite asset: " + assetPath);
            }

            return sprite;
        }

        private static void ConfigureText(TMP_Text text, string value, float maxFontSize, TextAlignmentOptions alignment)
        {
            text.text = value ?? string.Empty;
            text.font = GetLaunchKoreanFontAsset();
            text.fontSharedMaterial = GetLaunchKoreanFontMaterialPreset();
            text.fontSize = maxFontSize;
            text.fontSizeMax = maxFontSize;
            text.fontSizeMin = 18f;
            text.enableAutoSizing = true;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = alignment;
            text.raycastTarget = false;
        }

        private static TMP_FontAsset GetLaunchKoreanFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            if (fontAsset == null)
            {
                throw new InvalidOperationException("Missing copied Pretendard TMP SDF font asset: " + KoreanFontAssetPath);
            }

            return fontAsset;
        }

        private static Material GetLaunchKoreanFontMaterialPreset()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(KoreanFontMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException("Missing copied Pretendard TMP SDF material preset: " + KoreanFontMaterialPath);
            }

            return material;
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
