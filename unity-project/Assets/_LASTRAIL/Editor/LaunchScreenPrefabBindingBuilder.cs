#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Gacha;
using LastRail.Presentation.Audio;
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
        private const string CommonSkinRoot = "Assets/_LASTRAIL/UI/Common/Skins";
        private const string BackgroundRoot = "Assets/_LASTRAIL/UI/Backgrounds";
        private const string KeyVisualRoot = "Assets/_LASTRAIL/UI/KeyVisuals";
        private const string GachaIconRoot = "Assets/_LASTRAIL/UI/GachaResources/Icons";
        private const string GachaFxRoot = "Assets/_LASTRAIL/UI/GachaResources/GachaFx";
        private const string CombatEnemyRoot = "Assets/_LASTRAIL/UI/CombatResources/Enemies";
        private const string CombatEliteRoot = "Assets/_LASTRAIL/UI/CombatResources/Elites";
        private const string CombatBossRoot = "Assets/_LASTRAIL/UI/CombatResources/Bosses";
        private const string CombatVfxRoot = "Assets/_LASTRAIL/UI/CombatResources/VFX";
        private const string CombatLaneRoot = "Assets/_LASTRAIL/UI/CombatResources/Lanes";
        private const string CombatInteriorRoot = "Assets/_LASTRAIL/UI/CombatResources/Interiors";
        private const string CombatAnimationRoot = "Assets/_LASTRAIL/UI/CombatAnimations";
        private const string OperatorCardRoot = "Assets/_LASTRAIL/UI/Operators/Cards";
        private const string MissionThumbnailRoot = "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Thumbnails";
        private const string MissionThumbnailOverlayRoot = "Assets/_LASTRAIL/UI/MissionStoryThumbnails/Overlays";
        private const string OperatorCardPolishRoot = "Assets/_LASTRAIL/UI/Operators/CardPolish";
        private const string OperatorCardPolishFramesRoot = OperatorCardPolishRoot + "/Frames";
        private const string OperatorCardPolishBadgesRoot = OperatorCardPolishRoot + "/Badges";
        private const string OperatorCardPolishRarityRoot = OperatorCardPolishRoot + "/Rarity";
        private const string OperatorCardPolishOverlaysRoot = OperatorCardPolishRoot + "/Overlays";
        private const string RewardOfflineRoot = "Assets/_LASTRAIL/UI/RewardOfflineResources";
        private const string RewardOfflineFramesRoot = RewardOfflineRoot + "/Frames";
        private const string RewardOfflineBadgesRoot = RewardOfflineRoot + "/Badges";
        private const string RewardOfflineMetersRoot = RewardOfflineRoot + "/Meters";
        private const string RewardOfflineEmptyRoot = RewardOfflineRoot + "/Empty";
        private const string RewardOfflineAccentsRoot = RewardOfflineRoot + "/Accents";
        private const string SystemFeedbackRoot = "Assets/_LASTRAIL/UI/SystemFeedbackResources";
        private const string SystemFeedbackLoadingRoot = SystemFeedbackRoot + "/Loading";
        private const string SystemFeedbackBadgesRoot = SystemFeedbackRoot + "/Badges";
        private const string SystemFeedbackNoticesRoot = SystemFeedbackRoot + "/Notices";
        private const string SystemFeedbackRetryRoot = SystemFeedbackRoot + "/Retry";
        private const string SystemFeedbackWarningRoot = SystemFeedbackRoot + "/Warning";
        private const string SystemFeedbackDisabledRoot = SystemFeedbackRoot + "/Disabled";
        private const string SystemFeedbackStatusRoot = SystemFeedbackRoot + "/Status";
        private const string AudioIntegrationManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-audio-integrations.json";

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
            WriteAudioIntegrationManifest();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch screen prefabs build complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("LASTRAIL/Launch/Build Combat HUD Prefab")]
        public static void BuildCombatHudPrefabOnly()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateCombatHudPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL combat HUD prefab build complete.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        [MenuItem("LASTRAIL/Launch/Build Story Progress And Codex Thumbnail Prefabs")]
        public static void BuildStoryProgressAndCodexThumbnailPrefabs()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateCodexPrefab();
            CreateRewardStoryPrefab("StoryProgressSummaryScreen", "스토리 진행");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("LASTRAIL/Launch/Build Reward And Offline Polish Prefabs")]
        public static void BuildRewardAndOfflinePolishPrefabs()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateRewardStoryPrefab("RewardReviewScreen", "작전 보상");
            CreateRewardStoryPrefab("OfflineRewardScreen", "오프라인 보상");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("LASTRAIL/Launch/Build System Feedback Polish Prefabs")]
        public static void BuildSystemFeedbackPolishPrefabs()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateMissionShellPrefab();
            CreateGachaPrefab();
            CreateRewardStoryPrefab("RewardReviewScreen", "작전 보상");
            CreateRewardStoryPrefab("OfflineRewardScreen", "오프라인 보상");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateMissionShellPrefab()
        {
            var root = CreateScreenRoot("MissionShellScreen");
            try
            {
                var behaviour = root.AddComponent<MissionShellScreenRootBehaviour>();
                var audio = AttachAudioCuePlayer(root, CommonUiCueIds());
                var safeArea = CreateSafeArea(root.transform);
                CreateMissionShellSystemFeedbackPolish(safeArea);
                var title = CreateText(safeArea, "TitleText", "작전 선택");
                var subtitle = CreateText(safeArea, "SubtitleText", "Act 1");
                var body = CreateText(safeArea, "BodyText", "브리핑");
                var missionRows = CreateText(safeArea, "MissionRowsText", "작전 목록");
                var primary = CreateButton(safeArea, "PrimaryButton", "시작", out var primaryText);
                var secondary = CreateButton(safeArea, "SecondaryButton", "돌아가기", out var secondaryText);
                Place(title, 0f, 780f, 960f, 96f);
                Place(subtitle, 0f, 700f, 960f, 72f);
                Place(body, 0f, 560f, 960f, 160f);
                CreateMissionThumbnailStrip(safeArea);
                Place(missionRows, 0f, 210f, 960f, 220f);
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
                    ("secondaryButton", secondary),
                    ("audioCuePlayer", audio));
                SavePrefab(root, "MissionShellScreen");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateMissionThumbnailStrip(Transform parent)
        {
            var missionIds = new[]
            {
                "opn_01_last_departure",
                "opn_02_first_signal",
                "opn_03_water_stop",
                "opn_04_scrapstar_show",
                "opn_05_night_boarders",
                "opn_06_broken_line",
                "raid_raider_king_engine"
            };

            for (var i = 0; i < missionIds.Length; i++)
            {
                var image = CreateImage(
                    parent,
                    "MissionThumbnail_" + missionIds[i],
                    MissionThumbnailRoot + "/" + missionIds[i] + "_thumbnail.png",
                    new Color(1f, 1f, 1f, 0.82f));
                Place(image, -420f + i * 140f, 390f, 124f, 70f);
            }
        }

        private static void CreateStoryProgressThumbnailTimeline(Transform parent)
        {
            var missions = ActOneMissionThumbnailEntries();
            var startX = -360f;
            for (var i = 0; i < missions.Length; i++)
            {
                var column = i % 4;
                var row = i / 4;
                var x = startX + column * 240f;
                var y = 390f - row * 150f;
                CreateMissionThumbnailCard(parent, "StoryProgress", missions[i], x, y, 208f, 118f, "mission_state_available_accent");

                if (i < 3)
                {
                    CreateMissionThumbnailOverlay(parent, "StoryProgressCleared_" + missions[i].MissionId, "mission_state_cleared_stamp", x + 48f, y - 40f, 104f, 38f);
                }
                else if (i == 3)
                {
                    CreateMissionThumbnailOverlay(parent, "StoryProgressFirstClear_" + missions[i].MissionId, "mission_state_first_clear_badge", x + 44f, y - 40f, 112f, 38f);
                }
                else if (missions[i].IsRaid)
                {
                    CreateMissionThumbnailOverlay(parent, "StoryProgressBossOnly_" + missions[i].MissionId, "mission_state_boss_only_badge", x + 46f, y - 40f, 114f, 38f);
                }
            }

            var emptyState = CreateText(parent, "StoryProgressNoneStateText", "스토리 진행 없음");
            ConfigureText(emptyState, "스토리 진행 없음", 22f, TextAlignmentOptions.Center);
            Place(emptyState, 0f, 140f, 960f, 64f);
        }

        private static void CreateCodexArchiveThumbnailCards(Transform parent)
        {
            var missions = ActOneMissionThumbnailEntries();
            for (var i = 0; i < missions.Length; i++)
            {
                var isRaid = missions[i].IsRaid;
                var column = i % 2;
                var row = i / 2;
                var x = isRaid ? 0f : -235f + column * 470f;
                var y = isRaid ? 65f : 545f - row * 148f;
                var width = isRaid ? 910f : 430f;
                var height = isRaid ? 128f : 118f;
                var overlay = i == 0 ? "mission_state_locked_overlay" : "mission_state_available_accent";
                CreateMissionThumbnailCard(parent, "CodexArchive", missions[i], x, y, width, height, overlay);

                if (i > 0 && i < 4)
                {
                    CreateMissionThumbnailOverlay(parent, "CodexArchiveCleared_" + missions[i].MissionId, "mission_state_cleared_stamp", x + width * 0.32f, y - height * 0.34f, 104f, 38f);
                }

                if (isRaid)
                {
                    CreateMissionThumbnailOverlay(parent, "CodexArchiveBossOnly_" + missions[i].MissionId, "mission_state_boss_only_badge", x + 150f, y - 44f, 120f, 40f);
                }
            }
        }

        private static MissionThumbnailEntry[] ActOneMissionThumbnailEntries()
        {
            return new[]
            {
                new MissionThumbnailEntry("opn_01_last_departure", false),
                new MissionThumbnailEntry("opn_02_first_signal", false),
                new MissionThumbnailEntry("opn_03_water_stop", false),
                new MissionThumbnailEntry("opn_04_scrapstar_show", false),
                new MissionThumbnailEntry("opn_05_night_boarders", false),
                new MissionThumbnailEntry("opn_06_broken_line", false),
                new MissionThumbnailEntry("raid_raider_king_engine", true)
            };
        }

        private static void CreateMissionThumbnailCard(
            Transform parent,
            string namePrefix,
            MissionThumbnailEntry entry,
            float x,
            float y,
            float width,
            float height,
            string overlayId)
        {
            var thumbnail = CreateImage(
                parent,
                namePrefix + "Thumbnail_" + entry.MissionId,
                MissionThumbnailRoot + "/" + entry.MissionId + "_thumbnail.png",
                Color.white);
            Place(thumbnail, x, y, width, height);

            if (!string.IsNullOrWhiteSpace(overlayId))
            {
                CreateMissionThumbnailOverlay(parent, namePrefix + "Overlay_" + entry.MissionId + "_" + overlayId, overlayId, x, y, width, height);
            }
        }

        private static void CreateMissionThumbnailOverlay(
            Transform parent,
            string name,
            string overlayId,
            float x,
            float y,
            float width,
            float height)
        {
            var overlay = CreateImage(
                parent,
                name,
                MissionThumbnailOverlayRoot + "/" + overlayId + ".png",
                Color.white);
            Place(overlay, x, y, width, height);
        }

        private static void CreateGachaPrefab()
        {
                var root = CreateScreenRoot("GachaScreen");
            try
            {
                var behaviour = root.AddComponent<GachaScreenRootBehaviour>();
                var audio = AttachAudioCuePlayer(root, GachaCueIds());
                var safeArea = CreateSafeArea(root.transform);
                CreateGachaSystemFeedbackPolish(safeArea);
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
                var operatorCardSlotBackground = CreateImage(
                    safeArea,
                    "OperatorCardSlotBackgroundImage",
                    OperatorCardPolishFramesRoot + "/operator_card_slot_background.png",
                    new Color(1f, 1f, 1f, 0.92f));
                var resultCardFrame = CreateImage(
                    safeArea,
                    "ResultCardFrameImage",
                    GachaFxRoot + "/gacha_result_card_frame.png",
                    new Color(1f, 1f, 1f, 0.78f));
                var operatorRarityAccent = CreateImage(
                    safeArea,
                    "OperatorRarityAccentImage",
                    OperatorCardPolishRarityRoot + "/operator_rarity_pickup_elite.png",
                    new Color(1f, 1f, 1f, 0.95f));
                var operatorSelectedFrame = CreateImage(
                    safeArea,
                    "OperatorSelectedFrameImage",
                    OperatorCardPolishFramesRoot + "/operator_card_selected_frame.png",
                    new Color(1f, 1f, 1f, 0.9f));
                var operatorNewBadge = CreateImage(
                    safeArea,
                    "OperatorNewBadgeImage",
                    OperatorCardPolishBadgesRoot + "/operator_badge_new.png",
                    new Color(1f, 1f, 1f, 0.95f));
                var operatorOwnedBadge = CreateImage(
                    safeArea,
                    "OperatorOwnedBadgeImage",
                    OperatorCardPolishBadgesRoot + "/operator_badge_owned.png",
                    new Color(1f, 1f, 1f, 0.95f));
                var operatorLevelBadge = CreateImage(
                    safeArea,
                    "OperatorLevelBadgeImage",
                    OperatorCardPolishBadgesRoot + "/operator_badge_level.png",
                    new Color(1f, 1f, 1f, 0.95f));
                var operatorDuplicateTokenBadge = CreateImage(
                    safeArea,
                    "OperatorDuplicateTokenBadgeImage",
                    GachaIconRoot + "/ui_operator_duplicate_token.png",
                    new Color(1f, 1f, 1f, 0.96f));
                var operatorGridSafeMask = CreateImage(
                    safeArea,
                    "OperatorGridSafeMaskImage",
                    OperatorCardPolishOverlaysRoot + "/operator_grid_safe_mask.png",
                    new Color(1f, 1f, 1f, 0.42f));
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
                Place(operatorCardSlotBackground, 0f, 5f, 318f, 426f);
                Place(resultCardFrame, 0f, 5f, 320f, 426f);
                Place(operatorSelectedFrame, 0f, 5f, 322f, 428f);
                Place(operatorRarityAccent, 0f, -168f, 168f, 62f);
                Place(operatorNewBadge, 116f, 168f, 94f, 36f);
                Place(operatorOwnedBadge, 116f, 130f, 94f, 36f);
                Place(operatorLevelBadge, -112f, -166f, 86f, 32f);
                Place(operatorDuplicateTokenBadge, 242f, -322f, 92f, 92f);
                Place(operatorGridSafeMask, 0f, -112f, 770f, 500f);
                Place(duplicateConversion, 356f, -430f, 144f, 144f);
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
                    ("resultCardFrameImage", resultCardFrame),
                    ("operatorCardSlotBackgroundImage", operatorCardSlotBackground),
                    ("operatorRarityAccentImage", operatorRarityAccent),
                    ("operatorSelectedFrameImage", operatorSelectedFrame),
                    ("operatorNewBadgeImage", operatorNewBadge),
                    ("operatorOwnedBadgeImage", operatorOwnedBadge),
                    ("operatorLevelBadgeImage", operatorLevelBadge),
                    ("operatorDuplicateTokenBadgeImage", operatorDuplicateTokenBadge),
                    ("operatorGridSafeMaskImage", operatorGridSafeMask),
                    ("audioCuePlayer", audio));
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
                EnsureCombatLaneResourceImportSettings();
                var behaviour = root.AddComponent<FullyAnimatedCombatHudRootBehaviour>();
                var audio = AttachAudioCuePlayer(root, CombatCueIds());
                var safeArea = CreateSafeArea(root.transform);
                var layout = CreateCombatHudVisualPolish(safeArea);
                var encounterTitle = CreateText(layout.TopStatusPanel, "EncounterTitleText", "전투");
                var waveCounter = CreateText(layout.TopStatusPanel, "WaveCounterText", "Wave 1/15");
                var bossStatus = CreateText(layout.ExteriorLane, "BossStatusText", "보스 위협 감지");
                var objective = CreateText(layout.InteriorLane, "ObjectiveText", "목표: 열차 방어");
                var refundHint = CreateText(layout.InteriorLane, "RefundHintText", "스토리 레이드 실패 시 스태미나 환급 표시");
                var unitRefs = CreateText(layout.InteriorLane, "UnitRefsText", "초기 분대 5인 애니메이션 배치");
                var cueRefs = CreateText(layout.InteriorLane, "CueRefsText", "스킬 / 피격 / 궁극기 / 진동 큐 연결");
                Place(encounterTitle, 0f, 34f, 860f, 68f);
                Place(waveCounter, 0f, -34f, 860f, 64f);
                Place(bossStatus, 0f, -252f, 860f, 64f);
                Place(objective, 0f, 404f, 860f, 64f);
                Place(unitRefs, 0f, -416f, 860f, 64f);
                Place(cueRefs, 0f, -486f, 860f, 64f);
                Place(refundHint, 0f, -556f, 860f, 64f);

                Bind(behaviour,
                    ("safeAreaRoot", safeArea),
                    ("exteriorTrainAdvanceLane", layout.ExteriorLane),
                    ("interiorBoardingCombatLane", layout.InteriorLane),
                    ("playerSquadSlots", layout.PlayerSquadSlots),
                    ("invadingEnemySlots", layout.InvadingEnemySlots),
                    ("engagementZoneRoot", layout.EngagementZone),
                    ("encounterTitleText", encounterTitle),
                    ("waveCounterText", waveCounter),
                    ("bossStatusText", bossStatus),
                    ("objectiveText", objective),
                    ("refundHintText", refundHint),
                    ("unitRefsText", unitRefs),
                    ("cueRefsText", cueRefs),
                    ("audioCuePlayer", audio));
                SavePrefab(root, "FullyAnimatedCombatHud");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureCombatLaneResourceImportSettings()
        {
            EnsureSpriteImporter(CombatLaneRoot + "/combat_train_exterior_advance_lane.png");
            EnsureSpriteImporter(CombatLaneRoot + "/combat_train_interior_boarding_lane.png");
            EnsureSpriteImporter(CombatInteriorRoot + "/combat_train_interior_initial_squad.png");
        }

        private static void EnsureSpriteImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Missing launch texture importer: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static CombatHudLayoutBindings CreateCombatHudVisualPolish(Transform safeArea)
        {
            var backdrop = CreateImage(
                safeArea,
                "CombatHudBackdropPanel",
                BackgroundRoot + "/bg_main_lobby_train_interior.png",
                new Color(0.38f, 0.44f, 0.48f, 0.44f));
            backdrop.preserveAspect = false;
            Place(backdrop, 0f, 0f, 1000f, 1770f);

            var exteriorLaneImage = CreateImage(
                safeArea,
                "ExteriorTrainAdvanceLane",
                CombatLaneRoot + "/combat_train_exterior_advance_lane.png",
                Color.white);
            exteriorLaneImage.preserveAspect = false;
            Place(exteriorLaneImage, 0f, 530f, 1000f, 650f);
            var exteriorLane = exteriorLaneImage.GetComponent<RectTransform>();

            var topStatusPanel = CreateSlicedImage(
                exteriorLane,
                "CombatHudTopStatusPanel",
                CommonSkinRoot + "/ui_status_chip_9slice.png",
                new Color(0.92f, 0.98f, 1f, 0.88f));
            Place(topStatusPanel, 0f, 245f, 908f, 108f);

            var exteriorThreatVfx = CreateImage(
                exteriorLane,
                "ExteriorThreatVfxImage",
                CombatVfxRoot + "/vfx_story_raid_danger.png",
                new Color(1f, 1f, 1f, 0.30f));
            Place(exteriorThreatVfx, 326f, 30f, 190f, 190f);

            var raidSurface = CreateTransparentGroup(exteriorLane, "CombatStoryRaidBossDemoSurface");
            Place(raidSurface, 0f, -42f, 940f, 360f);
            var raidWake = CreateImage(
                raidSurface,
                "StoryRaidThreatWakeImage",
                CombatVfxRoot + "/vfx_ultimate_activation.png",
                new Color(1f, 1f, 1f, 0.24f));
            Place(raidWake, 268f, -12f, 260f, 260f);

            var interiorLaneImage = CreateImage(
                safeArea,
                "InteriorBoardingCombatLane",
                CombatLaneRoot + "/combat_train_interior_boarding_lane.png",
                Color.white);
            interiorLaneImage.preserveAspect = false;
            Place(interiorLaneImage, 0f, -348f, 1000f, 1080f);
            var interiorLane = interiorLaneImage.GetComponent<RectTransform>();

            var ordinarySurface = CreateTransparentGroup(interiorLane, "CombatOrdinaryWaveDemoSurface");
            Place(ordinarySurface, 0f, 0f, 960f, 1010f);

            var interiorBackground = CreateImage(
                interiorLane,
                "InteriorTrainCarBackgroundImage",
                CombatInteriorRoot + "/combat_train_interior_initial_squad.png",
                Color.white);
            interiorBackground.preserveAspect = false;
            Place(interiorBackground, 0f, 8f, 958f, 704f);

            var squadSlots = CreateTransparentGroup(interiorLane, "PlayerSquadSlots");
            Place(squadSlots, -250f, -8f, 420f, 690f);
            var enemySlots = CreateTransparentGroup(interiorLane, "InvadingEnemySlots");
            Place(enemySlots, 292f, -12f, 360f, 620f);
            var engagementZone = CreateTransparentGroup(interiorLane, "EngagementZoneMarkerImage");
            var engagementBurst = CreateImage(
                engagementZone,
                "EngagementContactBurstImage",
                CombatVfxRoot + "/vfx_basic_hit_spark.png",
                new Color(1f, 1f, 1f, 0.46f));
            Place(engagementBurst, 0f, 18f, 214f, 214f);
            var engagementSkill = CreateImage(
                engagementZone,
                "EngagementSkillWakeImage",
                CombatVfxRoot + "/vfx_skill_activation.png",
                new Color(1f, 1f, 1f, 0.34f));
            Place(engagementSkill, -42f, -70f, 210f, 210f);
            Place(
                engagementZone,
                22f,
                -18f,
                260f,
                520f);

            var objectiveBand = CreateSlicedImage(
                interiorLane,
                "CombatObjectiveHudBand",
                CommonSkinRoot + "/ui_status_chip_9slice.png",
                new Color(0.08f, 0.13f, 0.15f, 0.58f));
            Place(objectiveBand, 0f, 404f, 900f, 82f);

            CreateOperatorCombatSlots(squadSlots);
            CreateEnemyBoardingSlots(enemySlots);
            CreateCombatPreviewSprites(interiorLane, squadSlots, enemySlots);

            var skillVfx = CreateImage(
                interiorLane,
                "CombatSkillVfxPreviewImage",
                CombatVfxRoot + "/vfx_skill_activation.png",
                new Color(1f, 1f, 1f, 0.68f));
            Place(skillVfx, -26f, 36f, 188f, 188f);

            var hitVfx = CreateImage(
                interiorLane,
                "CombatHitSparkPreviewImage",
                CombatVfxRoot + "/vfx_basic_hit_spark.png",
                new Color(1f, 1f, 1f, 0.78f));
            Place(hitVfx, 110f, 40f, 150f, 150f);

            return new CombatHudLayoutBindings(
                exteriorLane,
                interiorLane,
                squadSlots,
                enemySlots,
                engagementZone,
                topStatusPanel.GetComponent<RectTransform>());
        }

        private static void CreateOperatorCombatSlots(Transform squadSlots)
        {
            var xPositions = new[] { -92f, 68f, -136f, 28f, -80f };
            var yPositions = new[] { 230f, 132f, 18f, -82f, -218f };
            var rotations = new[] { 0f, 0f, 0f, 0f, 0f };
            var operatorIds = new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" };
            for (var i = 0; i < 5; i++)
            {
                var unit = CreateAnimatedImage(
                    squadSlots,
                    "OperatorAnimated_" + operatorIds[i],
                    "Operators",
                    operatorIds[i],
                    "idle",
                    new Color(1f, 1f, 1f, 0.96f));
                Place(unit, xPositions[i], yPositions[i], 198f, 198f);
                unit.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
            }
        }

        private static void CreateEnemyBoardingSlots(Transform enemySlots)
        {
            var enemySpecs = new[]
            {
                new CombatAnimatedSpriteSpec("Enemies", "enemy_raider_scout", "attack"),
                new CombatAnimatedSpriteSpec("Enemies", "enemy_spike_cart_gunner", "attack"),
                new CombatAnimatedSpriteSpec("Enemies", "enemy_scrapshield_brute", "hit"),
                new CombatAnimatedSpriteSpec("EliteEnemies", "elite_ironclad_boarder", "skill_or_special")
            };
            var yPositions = new[] { 218f, 74f, -70f, -214f };
            for (var i = 0; i < enemySpecs.Length; i++)
            {
                var spec = enemySpecs[i];
                var sprite = CreateAnimatedImage(
                    enemySlots,
                    "EnemyBoardingAnimated_" + spec.Id,
                    spec.Folder,
                    spec.Id,
                    spec.State,
                    new Color(1f, 1f, 1f, 0.98f));
                Place(sprite, i % 2 == 0 ? -8f : 54f, yPositions[i], 166f, 166f);
            }
        }

        private static void CreateCombatPreviewSprites(Transform interiorLane, Transform squadSlots, Transform enemySlots)
        {
            var dangerCue = CreateImage(
                interiorLane,
                "StoryRaidDangerCueImage",
                CombatVfxRoot + "/vfx_story_raid_danger.png",
                new Color(1f, 1f, 1f, 0.34f));
            Place(dangerCue, 170f, 220f, 160f, 160f);
        }

        private static Image CreateAnimatedImage(Transform parent, string name, string folder, string id, string state, Color color)
        {
            var image = CreateImage(
                parent,
                name,
                CombatAnimationRoot + "/" + folder + "/" + id + "/" + state + "/" + id + "_" + state + "_001.png",
                color);
            var animator = image.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                CombatAnimationRoot + "/Animators/" + folder + "/" + id + ".controller");
            if (animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("Missing launch combat animator controller: " + folder + "/" + id);
            }

            return image;
        }

        private readonly struct CombatAnimatedSpriteSpec
        {
            public CombatAnimatedSpriteSpec(string folder, string id, string state)
            {
                Folder = folder;
                Id = id;
                State = state;
            }

            public string Folder { get; }
            public string Id { get; }
            public string State { get; }
        }
        private static RectTransform CreateTransparentGroup(Transform parent, string name)
        {
            var group = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(parent, false);
            return group;
        }

        private static Image CreateSlicedImage(Transform parent, string name, string spritePath, Color color)
        {
            var image = CreateImage(parent, name, spritePath, color);
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            return image;
        }

        private sealed class CombatHudLayoutBindings
        {
            public CombatHudLayoutBindings(
                RectTransform exteriorLane,
                RectTransform interiorLane,
                RectTransform playerSquadSlots,
                RectTransform invadingEnemySlots,
                RectTransform engagementZone,
                RectTransform topStatusPanel)
            {
                ExteriorLane = exteriorLane;
                InteriorLane = interiorLane;
                PlayerSquadSlots = playerSquadSlots;
                InvadingEnemySlots = invadingEnemySlots;
                EngagementZone = engagementZone;
                TopStatusPanel = topStatusPanel;
            }

            public RectTransform ExteriorLane { get; }
            public RectTransform InteriorLane { get; }
            public RectTransform PlayerSquadSlots { get; }
            public RectTransform InvadingEnemySlots { get; }
            public RectTransform EngagementZone { get; }
            public RectTransform TopStatusPanel { get; }
        }
        private static void CreateCodexPrefab()
        {
            var root = CreateScreenRoot("CodexArchiveScreen");
            try
            {
                var behaviour = root.AddComponent<CodexArchiveScreenRootBehaviour>();
                var audio = AttachAudioCuePlayer(root, CommonUiCueIds());
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
                CreateCodexArchiveThumbnailCards(safeArea);
                Place(entries, 0f, -305f, 960f, 260f);
                Place(overflow, 0f, -500f, 960f, 72f);
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
                    ("rewardReviewButton", rewardReview),
                    ("audioCuePlayer", audio));
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
                var audio = AttachAudioCuePlayer(root, RewardCueIds());
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
                if (string.Equals(prefabName, "StoryProgressSummaryScreen", StringComparison.Ordinal))
                {
                    Place(body, 0f, 610f, 960f, 92f);
                    CreateStoryProgressThumbnailTimeline(safeArea);
                    Place(rewardLines, 0f, -60f, 960f, 260f);
                    Place(status, 0f, -365f, 960f, 120f);
                }
                else if (string.Equals(prefabName, "RewardReviewScreen", StringComparison.Ordinal))
                {
                    CreateRewardReviewVisualPolish(safeArea);
                    CreateRewardReviewSystemFeedbackPolish(safeArea);
                    Place(body, 0f, 545f, 960f, 128f);
                    Place(rewardLines, 110f, 120f, 720f, 330f);
                    Place(status, 0f, -335f, 960f, 128f);
                }
                else if (string.Equals(prefabName, "OfflineRewardScreen", StringComparison.Ordinal))
                {
                    CreateOfflineRewardVisualPolish(safeArea);
                    CreateOfflineRewardSystemFeedbackPolish(safeArea);
                    Place(body, 0f, 545f, 960f, 128f);
                    Place(rewardLines, 110f, 115f, 720f, 330f);
                    Place(status, 0f, -335f, 960f, 128f);
                }
                else
                {
                    Place(body, 0f, 520f, 960f, 220f);
                    Place(rewardLines, 0f, 140f, 960f, 380f);
                    Place(status, 0f, -300f, 960f, 160f);
                }
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
                    ("codexButton", codex),
                    ("audioCuePlayer", audio));
                SavePrefab(root, prefabName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateRewardReviewVisualPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "RewardReviewVisualPolishLayer");
            var resultPlate = CreateImage(
                layer,
                "RewardResultPlateImage",
                RewardOfflineFramesRoot + "/reward_result_plate.png",
                new Color(1f, 1f, 1f, 0.92f));
            var chestAccent = CreateImage(
                layer,
                "RewardChestSupplyCrateAccentImage",
                RewardOfflineAccentsRoot + "/reward_chest_supply_crate_accent.png",
                new Color(1f, 1f, 1f, 0.94f));
            var materialGlow = CreateImage(
                layer,
                "RewardMaterialGlowImage",
                RewardOfflineAccentsRoot + "/reward_material_glow.png",
                new Color(1f, 1f, 1f, 0.56f));
            var successBadge = CreateImage(
                layer,
                "ClaimSuccessBadgeImage",
                RewardOfflineBadgesRoot + "/claim_success_badge.png",
                new Color(1f, 1f, 1f, 0.94f));

            Place(resultPlate, 0f, 190f, 900f, 450f);
            Place(chestAccent, -345f, 260f, 150f, 150f);
            Place(materialGlow, 344f, 260f, 168f, 168f);
            Place(successBadge, 310f, 405f, 190f, 72f);

            CreateRewardLineFrame(layer, "RewardLineItemFrameTopImage", 0f, 215f);
            CreateRewardLineFrame(layer, "RewardLineItemFrameMiddleImage", 0f, 105f);
            CreateRewardLineFrame(layer, "RewardLineItemFrameBottomImage", 0f, -5f);
        }

        private static void CreateOfflineRewardVisualPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "OfflineRewardVisualPolishLayer");
            var settlementMeter = CreateImage(
                layer,
                "OfflineSettlementMeterImage",
                RewardOfflineMetersRoot + "/offline_settlement_meter.png",
                new Color(1f, 1f, 1f, 0.93f));
            var durationStrip = CreateImage(
                layer,
                "OfflineDurationAccumulationStripImage",
                RewardOfflineMetersRoot + "/offline_duration_accumulation_strip.png",
                new Color(1f, 1f, 1f, 0.92f));
            var emptyState = CreateImage(
                layer,
                "OfflineEmptyStateVisualImage",
                RewardOfflineEmptyRoot + "/offline_empty_state_visual.png",
                new Color(1f, 1f, 1f, 0.86f));
            var claimableAccent = CreateImage(
                layer,
                "ClaimPendingClaimableAccentImage",
                RewardOfflineBadgesRoot + "/claim_pending_claimable_accent.png",
                new Color(1f, 1f, 1f, 0.92f));

            Place(settlementMeter, 0f, 310f, 860f, 216f);
            Place(durationStrip, 0f, 150f, 860f, 126f);
            Place(emptyState, -305f, 5f, 260f, 174f);
            Place(claimableAccent, 278f, 15f, 220f, 84f);
            CreateRewardLineFrame(layer, "OfflineRewardLineItemFrameImage", 125f, -70f);
        }

        private static void CreateMissionShellSystemFeedbackPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "MissionShellSystemFeedbackLayer");
            var loading = CreateImage(
                layer,
                "LoadingRailIndicatorImage",
                SystemFeedbackLoadingRoot + "/loading_rail_indicator.png",
                new Color(1f, 1f, 1f, 0.88f));
            var warning = CreateImage(
                layer,
                "WarningStripBannerAccentImage",
                SystemFeedbackWarningRoot + "/warning_strip_banner_accent.png",
                new Color(1f, 1f, 1f, 0.58f));
            var dots = CreateImage(
                layer,
                "InlineStatusDotSetImage",
                SystemFeedbackStatusRoot + "/inline_status_dot_set.png",
                new Color(1f, 1f, 1f, 0.7f));

            Place(loading, 0f, 535f, 560f, 132f);
            Place(warning, 0f, -600f, 760f, 92f);
            Place(dots, 310f, 692f, 180f, 45f);
        }

        private static void CreateGachaSystemFeedbackPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "GachaSystemFeedbackLayer");
            var connectionBadge = CreateImage(
                layer,
                "NetworkErrorBadgeImage",
                SystemFeedbackBadgesRoot + "/feedback_network_error_badge.png",
                new Color(1f, 1f, 1f, 0.82f));
            var retry = CreateImage(
                layer,
                "RetryRefreshAccentImage",
                SystemFeedbackRetryRoot + "/retry_refresh_accent.png",
                new Color(1f, 1f, 1f, 0.62f));
            var disabled = CreateImage(
                layer,
                "DisabledUnavailableOverlayImage",
                SystemFeedbackDisabledRoot + "/disabled_unavailable_overlay.png",
                new Color(1f, 1f, 1f, 0.34f));

            Place(connectionBadge, 292f, -356f, 230f, 86f);
            Place(retry, 398f, -566f, 96f, 96f);
            Place(disabled, 0f, -112f, 730f, 470f);
        }

        private static void CreateRewardReviewSystemFeedbackPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "RewardReviewSystemFeedbackLayer");
            var success = CreateImage(
                layer,
                "FeedbackSuccessBadgeImage",
                SystemFeedbackBadgesRoot + "/feedback_success_badge.png",
                new Color(1f, 1f, 1f, 0.78f));
            var dots = CreateImage(
                layer,
                "InlineStatusDotSetImage",
                SystemFeedbackStatusRoot + "/inline_status_dot_set.png",
                new Color(1f, 1f, 1f, 0.7f));

            Place(success, -320f, -334f, 188f, 70f);
            Place(dots, 330f, -336f, 190f, 48f);
        }

        private static void CreateOfflineRewardSystemFeedbackPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "OfflineRewardSystemFeedbackLayer");
            var maintenance = CreateImage(
                layer,
                "MaintenanceNoticePanelImage",
                SystemFeedbackNoticesRoot + "/maintenance_notice_panel.png",
                new Color(1f, 1f, 1f, 0.38f));
            var disabled = CreateImage(
                layer,
                "DisabledUnavailableOverlayImage",
                SystemFeedbackDisabledRoot + "/disabled_unavailable_overlay.png",
                new Color(1f, 1f, 1f, 0.34f));
            var retry = CreateImage(
                layer,
                "RetryRefreshAccentImage",
                SystemFeedbackRetryRoot + "/retry_refresh_accent.png",
                new Color(1f, 1f, 1f, 0.56f));

            Place(maintenance, 0f, 198f, 750f, 300f);
            Place(disabled, -305f, 5f, 240f, 160f);
            Place(retry, 390f, -338f, 88f, 88f);
        }

        private static void CreateRewardLineFrame(Transform parent, string name, float x, float y)
        {
            var frame = CreateImage(
                parent,
                name,
                RewardOfflineFramesRoot + "/reward_line_item_frame.png",
                new Color(1f, 1f, 1f, 0.9f));
            Place(frame, x, y, 790f, 98f);
        }

        private static Transform CreateVisualLayer(Transform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;
            layer.SetAsFirstSibling();
            return layer;
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
            var buttonImage = buttonObject.GetComponent<Image>();
            ApplyButtonSkin(buttonImage, name);

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

        private static void ApplyButtonSkin(Image image, string name)
        {
            image.sprite = LoadSprite(CommonSkinRoot + "/" + ButtonSkinFor(name) + ".png");
            image.color = Color.white;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }

        private static string ButtonSkinFor(string name)
        {
            if (name.IndexOf("Danger", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "ui_button_danger_9slice";
            }

            if (name.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "ui_button_disabled_9slice";
            }

            if (name.IndexOf("Primary", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("OnePull", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("MissionSelect", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "ui_button_primary_9slice";
            }

            return "ui_button_secondary_9slice";
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

        private static LastRailAudioCuePlayer AttachAudioCuePlayer(GameObject root, IReadOnlyList<string> cueIds)
        {
            var sfxSource = root.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 0.8f;

            var bgmSource = root.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.35f;

            var player = root.AddComponent<LastRailAudioCuePlayer>();
            var serialized = new SerializedObject(player);
            serialized.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            serialized.FindProperty("bgmSource").objectReferenceValue = bgmSource;

            var bindings = serialized.FindProperty("clipBindings");
            bindings.arraySize = cueIds.Count;
            for (var i = 0; i < cueIds.Count; i++)
            {
                var cueId = cueIds[i];
                var element = bindings.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cueId").stringValue = cueId;
                element.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioClipPath(cueId));
                element.FindPropertyRelative("loop").boolValue = string.Equals(cueId, "bgm_act1_rail_loop", StringComparison.Ordinal);
                element.FindPropertyRelative("volume").floatValue = string.Equals(cueId, "bgm_act1_rail_loop", StringComparison.Ordinal) ? 0.35f : 0.8f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        private static IReadOnlyList<string> CommonUiCueIds()
        {
            return new[]
            {
                "ui_tap",
                "ui_back",
                "ui_confirm",
                "ui_error",
                "ui_toggle_or_tab",
                "bgm_act1_rail_loop"
            };
        }

        private static IReadOnlyList<string> GachaCueIds()
        {
            return new[]
            {
                "ui_tap",
                "ui_back",
                "ui_confirm",
                "ui_error",
                "gacha_reveal_start",
                "gacha_result_rare",
                "gacha_result_elite",
                "gacha_result_pickup_elite",
                "gacha_duplicate_conversion",
                "bgm_act1_rail_loop"
            };
        }

        private static IReadOnlyList<string> CombatCueIds()
        {
            return new[]
            {
                "combat_hit",
                "combat_skill",
                "combat_ultimate",
                "combat_shield",
                "combat_heal",
                "combat_buff",
                "combat_debuff",
                "story_raid_warning",
                "boss_defeat",
                "bgm_act1_rail_loop"
            };
        }

        private static IReadOnlyList<string> RewardCueIds()
        {
            return new[]
            {
                "ui_tap",
                "ui_back",
                "ui_confirm",
                "ui_error",
                "ui_toggle_or_tab",
                "reward_claim",
                "reward_line_tick",
                "bgm_act1_rail_loop"
            };
        }

        private static string AudioClipPath(string cueId)
        {
            switch (cueId)
            {
                case "ui_tap": return "Assets/_LASTRAIL/Audio/Sfx/UI/ui_tap.wav";
                case "ui_back": return "Assets/_LASTRAIL/Audio/Sfx/UI/ui_back.wav";
                case "ui_confirm": return "Assets/_LASTRAIL/Audio/Sfx/UI/ui_confirm.wav";
                case "ui_error": return "Assets/_LASTRAIL/Audio/Sfx/UI/ui_error.wav";
                case "ui_toggle_or_tab": return "Assets/_LASTRAIL/Audio/Sfx/UI/ui_toggle_or_tab.wav";
                case "gacha_reveal_start": return "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_reveal_start.wav";
                case "gacha_result_rare": return "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_result_rare.wav";
                case "gacha_result_elite": return "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_result_elite.wav";
                case "gacha_result_pickup_elite": return "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_result_pickup_elite.wav";
                case "gacha_duplicate_conversion": return "Assets/_LASTRAIL/Audio/Sfx/Gacha/gacha_duplicate_conversion.wav";
                case "reward_claim": return "Assets/_LASTRAIL/Audio/Sfx/Reward/reward_claim.wav";
                case "reward_line_tick": return "Assets/_LASTRAIL/Audio/Sfx/Reward/reward_line_tick.wav";
                case "combat_hit": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_hit.wav";
                case "combat_skill": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_skill.wav";
                case "combat_ultimate": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_ultimate.wav";
                case "combat_shield": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_shield.wav";
                case "combat_heal": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_heal.wav";
                case "combat_buff": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_buff.wav";
                case "combat_debuff": return "Assets/_LASTRAIL/Audio/Sfx/Combat/combat_debuff.wav";
                case "story_raid_warning": return "Assets/_LASTRAIL/Audio/Sfx/StoryRaid/story_raid_warning.wav";
                case "boss_defeat": return "Assets/_LASTRAIL/Audio/Sfx/StoryRaid/boss_defeat.wav";
                case "bgm_act1_rail_loop": return "Assets/_LASTRAIL/Audio/Bgm/bgm_act1_rail_loop.wav";
                default:
                    throw new InvalidOperationException("Unknown launch audio cue id: " + cueId);
            }
        }

        private static void WriteAudioIntegrationManifest()
        {
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, AudioIntegrationManifestPath), @"{
  ""name"": ""LAST RAIL Act 1 Launch Audio Event Integration"",
  ""presentationOnly"": true,
  ""serverContractChanged"": false,
  ""gameplayTimingChanged"": false,
  ""rewardCalculationIncluded"": false,
  ""economyMutationIncluded"": false,
  ""connectedCueCount"": 22,
  ""bgmAct1RailLoopAutoplay"": false,
  ""surfaces"": [
    { ""surface"": ""common_ui"", ""events"": [""button_tap"", ""back"", ""confirm"", ""error"", ""tab_toggle""], ""cueIds"": [""ui_tap"", ""ui_back"", ""ui_confirm"", ""ui_error"", ""ui_toggle_or_tab""] },
    { ""surface"": ""gacha_screen"", ""events"": [""reveal_start"", ""rare_result"", ""elite_result"", ""pickup_elite_result"", ""duplicate_conversion""], ""cueIds"": [""gacha_reveal_start"", ""gacha_result_rare"", ""gacha_result_elite"", ""gacha_result_pickup_elite"", ""gacha_duplicate_conversion""] },
    { ""surface"": ""reward_offline"", ""events"": [""claim"", ""line_tick""], ""cueIds"": [""reward_claim"", ""reward_line_tick""] },
    { ""surface"": ""combat_hud"", ""events"": [""hit"", ""skill"", ""ultimate"", ""shield"", ""heal"", ""buff"", ""debuff""], ""cueIds"": [""combat_hit"", ""combat_skill"", ""combat_ultimate"", ""combat_shield"", ""combat_heal"", ""combat_buff"", ""combat_debuff""] },
    { ""surface"": ""story_raid"", ""events"": [""warning"", ""boss_defeat""], ""cueIds"": [""story_raid_warning"", ""boss_defeat""] },
    { ""surface"": ""launch_bgm"", ""events"": [""opt_in_single_loop""], ""cueIds"": [""bgm_act1_rail_loop""] }
  ]
}
");
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

        private readonly struct MissionThumbnailEntry
        {
            public MissionThumbnailEntry(string missionId, bool isRaid)
            {
                MissionId = missionId;
                IsRaid = isRaid;
            }

            public string MissionId { get; }

            public bool IsRaid { get; }
        }
    }
}
#endif
