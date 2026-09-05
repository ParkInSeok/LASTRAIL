#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Gacha;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Presentation.Startup;
using LastRail.Presentation.AuthGate;
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
        private const string CombatAccentRoot = "Assets/_LASTRAIL/UI/CombatResources/Accents";
        private const string CombatVfxRoot = "Assets/_LASTRAIL/UI/CombatResources/VFX";
        private const string CombatSkillIconRoot = "Assets/_LASTRAIL/UI/CombatResources/SkillIcons";
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
        private const string MissionShellPolishRoot = "Assets/_LASTRAIL/UI/MissionShellResources";
        private const string MissionShellPolishFramesRoot = MissionShellPolishRoot + "/Frames";
        private const string MissionShellPolishPanelsRoot = MissionShellPolishRoot + "/Panels";
        private const string MissionShellPolishBadgesRoot = MissionShellPolishRoot + "/Badges";
        private const string MissionShellPolishProgressRoot = MissionShellPolishRoot + "/Progress";
        private const string MissionShellPolishButtonsRoot = MissionShellPolishRoot + "/Buttons";
        private const string CodexArchivePolishRoot = "Assets/_LASTRAIL/UI/CodexArchiveResources";
        private const string CodexArchivePolishPanelsRoot = CodexArchivePolishRoot + "/Panels";
        private const string CodexArchivePolishFramesRoot = CodexArchivePolishRoot + "/Frames";
        private const string CodexArchivePolishBadgesRoot = CodexArchivePolishRoot + "/Badges";
        private const string CodexArchivePolishButtonsRoot = CodexArchivePolishRoot + "/Buttons";
        private const string StoryProgressPolishRoot = "Assets/_LASTRAIL/UI/StoryProgressResources";
        private const string StoryProgressPolishPanelsRoot = StoryProgressPolishRoot + "/Panels";
        private const string StoryProgressPolishFramesRoot = StoryProgressPolishRoot + "/Frames";
        private const string StoryProgressPolishBadgesRoot = StoryProgressPolishRoot + "/Badges";
        private const string StoryProgressPolishOverlaysRoot = StoryProgressPolishRoot + "/Overlays";
        private const string StoryProgressPolishButtonsRoot = StoryProgressPolishRoot + "/Buttons";
        private const string AuthProfileRoot = "Assets/_LASTRAIL/UI/AuthProfileResources";
        private const string AuthProfilePanelsRoot = AuthProfileRoot + "/Panels";
        private const string AuthProfileFramesRoot = AuthProfileRoot + "/Frames";
        private const string AuthProfileBadgesRoot = AuthProfileRoot + "/Badges";
        private const string AuthProfileButtonsRoot = AuthProfileRoot + "/Buttons";
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

        [MenuItem("LASTRAIL/Launch/Build CodexArchive Polish Prefab")]
        public static void BuildCodexArchivePolishPrefab()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateCodexPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("LASTRAIL/Launch/Build StoryProgress Polish Prefab")]
        public static void BuildStoryProgressPolishPrefab()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateRewardStoryPrefab("StoryProgressSummaryScreen", "스토리 진행");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
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

        [MenuItem("LASTRAIL/Launch/Build AuthProfile MissionShell Prefab")]
        public static void BuildAuthProfileMissionShellPrefab()
        {
            EnsureDirectory("Assets/_LASTRAIL/Prefabs");
            EnsureDirectory(PrefabRoot);

            CreateMissionShellPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void CreateMissionShellPrefab()
        {
            var root = CreateScreenRoot("MissionShellScreen");
            try
            {
                var behaviour = root.AddComponent<MissionShellScreenRootBehaviour>();
                var audio = AttachAudioCuePlayer(root, CommonUiCueIds());
                var safeArea = CreateSafeArea(root.transform);
                CreateMissionSelectSurface(safeArea);
                CreateAuthProfileVisualPolish(safeArea);
                CreateMissionShellSystemFeedbackPolish(safeArea);
                var title = CreateText(safeArea, "TitleText", "작전 선택");
                var subtitle = CreateText(safeArea, "SubtitleText", "Act 1");
                var body = CreateText(safeArea, "BodyText", "브리핑");
                var missionRows = CreateText(safeArea, "MissionRowsText", "작전 목록");
                var primary = CreateButton(safeArea, "PrimaryButton", "시작", out var primaryText);
                var secondary = CreateButton(safeArea, "SecondaryButton", "돌아가기", out var secondaryText);
                Place(title, 0f, 780f, 960f, 96f);
                Place(subtitle, 0f, 700f, 960f, 72f);
                Place(body, 0f, 500f, 900f, 72f);
                CreateMissionThumbnailStrip(safeArea);
                Place(missionRows, 0f, 245f, 900f, 72f);
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

        private static void CreateAuthProfileVisualPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "AuthProfileVisualPolishLayer");

            var statusPanel = CreateImage(
                layer,
                "AuthProfileStatusPanelImage",
                AuthProfilePanelsRoot + "/auth_profile_status_panel.png",
                new Color(1f, 1f, 1f, 0.32f));
            statusPanel.preserveAspect = false;
            Place(statusPanel, 300f, 760f, 430f, 86f);

            var nameplate = CreateImage(
                layer,
                "ProfileNameplateFrameImage",
                AuthProfileFramesRoot + "/profile_nameplate_frame.png",
                new Color(1f, 1f, 1f, 0.44f));
            nameplate.preserveAspect = false;
            Place(nameplate, 232f, 756f, 255f, 45f);

            Place(CreateImage(
                layer,
                "AuthGuestBadgeImage",
                AuthProfileBadgesRoot + "/auth_guest_badge.png",
                new Color(1f, 1f, 1f, 0.52f)), 90f, 756f, 44f, 44f);

            Place(CreateImage(
                layer,
                "AuthGoogleProviderBadgeImage",
                AuthProfileBadgesRoot + "/auth_google_provider_badge.png",
                new Color(1f, 1f, 1f, 0.42f)), 432f, 756f, 104f, 52f);

            Place(CreateImage(
                layer,
                "AuthKeystoreLockBadgeImage",
                AuthProfileBadgesRoot + "/auth_keystore_lock_badge.png",
                new Color(1f, 1f, 1f, 0.48f)), 468f, 700f, 42f, 42f);

            Place(CreateImage(
                layer,
                "AuthSessionSafeBadgeImage",
                AuthProfileBadgesRoot + "/auth_session_safe_badge.png",
                new Color(1f, 1f, 1f, 0.48f)), 416f, 700f, 42f, 42f);

            var settingsPanel = CreateImage(
                layer,
                "SettingsTogglePanelImage",
                AuthProfilePanelsRoot + "/settings_toggle_panel.png",
                new Color(1f, 1f, 1f, 0.34f));
            settingsPanel.preserveAspect = false;
            Place(settingsPanel, -405f, -590f, 150f, 112f);

            Place(CreateImage(
                layer,
                "SettingsAudioHapticTogglesImage",
                AuthProfileButtonsRoot + "/settings_audio_haptic_toggles.png",
                new Color(1f, 1f, 1f, 0.48f)), -405f, -590f, 136f, 68f);

            var navAccent = CreateImage(
                layer,
                "AuthBottomNavAccentImage",
                AuthProfileButtonsRoot + "/auth_bottom_nav_accent.png",
                new Color(1f, 1f, 1f, 0.28f));
            navAccent.preserveAspect = false;
            Place(navAccent, 0f, -815f, 720f, 54f);
        }

        private static void CreateMissionSelectSurface(Transform parent)
        {
            var background = CreateImage(
                parent,
                "MissionSelectBackgroundImage",
                BackgroundRoot + "/bg_mission_select_rail_map.png",
                new Color(0.62f, 0.70f, 0.76f, 0.34f));
            background.preserveAspect = false;
            Place(background, 0f, 0f, 1080f, 1920f);

            var shade = CreateSolidImage(parent, "MissionSelectBackgroundShade", new Color(0.02f, 0.04f, 0.045f, 0.68f));
            Place(shade, 0f, 0f, 1080f, 1920f);

            var briefing = CreateImage(
                parent,
                "MissionBriefingPanelImage",
                MissionShellPolishPanelsRoot + "/mission_briefing_panel.png",
                new Color(1f, 1f, 1f, 0.92f));
            briefing.preserveAspect = false;
            Place(briefing, 0f, 438f, 940f, 310f);

            var roster = CreateImage(
                parent,
                "MissionRosterPanelImage",
                MissionShellPolishPanelsRoot + "/mission_objective_panel.png",
                new Color(1f, 1f, 1f, 0.88f));
            roster.preserveAspect = false;
            Place(roster, 0f, 102f, 940f, 270f);

            var objective = CreateImage(
                parent,
                "MissionObjectivePlateImage",
                MissionShellPolishBadgesRoot + "/mission_objective_plate.png",
                new Color(1f, 1f, 1f, 0.92f));
            Place(objective, -270f, 514f, 310f, 92f);

            var wave = CreateImage(
                parent,
                "MissionWaveBadgeImage",
                MissionShellPolishBadgesRoot + "/mission_wave_badge.png",
                new Color(1f, 1f, 1f, 0.94f));
            Place(wave, 326f, 514f, 82f, 82f);

            var risk = CreateImage(
                parent,
                "MissionRiskBadgeImage",
                MissionShellPolishBadgesRoot + "/mission_risk_badge.png",
                new Color(1f, 1f, 1f, 0.92f));
            Place(risk, 420f, 514f, 82f, 82f);

            var firstClear = CreateImage(
                parent,
                "MissionFirstClearBadgeImage",
                MissionShellPolishBadgesRoot + "/mission_first_clear_badge.png",
                new Color(1f, 1f, 1f, 0.90f));
            Place(firstClear, 420f, 270f, 86f, 86f);

            var startAccent = CreateImage(
                parent,
                "MissionStartButtonAccentImage",
                MissionShellPolishButtonsRoot + "/mission_start_button_accent.png",
                new Color(1f, 1f, 1f, 0.86f));
            Place(startAccent, -190f, -720f, 342f, 84f);

            var backAccent = CreateImage(
                parent,
                "MissionBackButtonAccentImage",
                MissionShellPolishButtonsRoot + "/mission_back_button_accent.png",
                new Color(1f, 1f, 1f, 0.72f));
            Place(backAccent, 190f, -720f, 342f, 84f);
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
                var x = -384f + i * 128f;
                var surface = CreateImage(
                    parent,
                    "MissionCardSurface_" + missionIds[i],
                    MissionShellPolishFramesRoot + "/" + (i == 0 ? "mission_operation_card_selected_frame" : "mission_operation_card_frame") + ".png",
                    new Color(1f, 1f, 1f, i == 0 ? 0.94f : 0.82f));
                surface.preserveAspect = false;
                Place(surface, x, 388f, 128f, 106f);

                var image = CreateImage(
                    parent,
                    "MissionThumbnail_" + missionIds[i],
                    MissionThumbnailRoot + "/" + missionIds[i] + "_thumbnail.png",
                    new Color(1f, 1f, 1f, 0.92f));
                Place(image, x, 390f, 104f, 60f);
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
                var frame = CreateImage(
                    parent,
                    "CodexArchiveEntryFrame_" + missions[i].MissionId,
                    CodexArchivePolishFramesRoot + "/" + (i == 0 ? "codex_archive_entry_selected_frame" : "codex_archive_entry_card_frame") + ".png",
                    new Color(1f, 1f, 1f, i == 0 ? 0.92f : 0.78f));
                frame.preserveAspect = false;
                Place(frame, x, y, width + 20f, height + 18f);

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
                var waveCounter = CreateText(layout.TopStatusPanel, "WaveCounterText", "웨이브 1/15");
                var bossStatus = CreateText(layout.ExteriorLane, "BossStatusText", "보스 위협 감지");
                var objective = CreateText(layout.InteriorLane, "ObjectiveText", "목표: 열차 방어");
                var refundHint = CreateText(layout.InteriorLane, "RefundHintText", "");
                var unitRefs = CreateText(layout.BottomStatusPanel, "UnitRefsText", "분대 5 / 침입 4");
                var cueRefs = CreateText(layout.BottomStatusPanel, "CueRefsText", "스킬 대기 / 충돌 감지");
                ConfigureText(encounterTitle, "전투", 28f, TextAlignmentOptions.MidlineLeft);
                ConfigureText(waveCounter, "웨이브 1/15", 24f, TextAlignmentOptions.MidlineRight);
                ConfigureText(bossStatus, "보스 위협 감지", 22f, TextAlignmentOptions.MidlineRight);
                ConfigureText(objective, "목표: 열차 방어", 24f, TextAlignmentOptions.MidlineLeft);
                ConfigureText(unitRefs, "분대 5 / 침입 4", 24f, TextAlignmentOptions.MidlineLeft);
                ConfigureText(cueRefs, "스킬 대기 / 충돌 감지", 22f, TextAlignmentOptions.MidlineLeft);
                ConfigureText(refundHint, "", 22f, TextAlignmentOptions.MidlineLeft);
                Place(encounterTitle, -292f, 0f, 330f, 58f);
                Place(waveCounter, 292f, 0f, 330f, 58f);
                Place(bossStatus, 284f, -254f, 360f, 54f);
                Place(objective, -244f, 404f, 390f, 52f);
            Place(unitRefs, 0f, 70f, 430f, 48f);
            Place(cueRefs, 0f, 28f, 430f, 48f);
                Place(refundHint, 252f, 404f, 320f, 52f);
                var skillIconSlots = CreateCombatSkillIconSlots(layout.BottomStatusPanel);

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
                BindCombatSkillIconSprites(behaviour, skillIconSlots);
                BindCombatStatusSprites(behaviour);
                BindCombatTransitionResources(behaviour);
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
            EnsureSpriteImporter(CombatInteriorRoot + "/combat_train_interior_initial_squad.png");
            foreach (var definition in CombatOperatorSkillIconCatalog.CreateLaunchDefinitions())
            {
                EnsureSpriteImporter(definition.IconPath);
            }
        }

        [MenuItem("LASTRAIL/Launch/Build Startup Resource Prefab")]
        public static void BuildStartupScreenPrefab()
        {
            var root = CreateScreenRoot("StartupScreen");
            try
            {
                var background = CreateImage(root.transform, "StartupKeyVisual",
                    BackgroundRoot + "/bg_combat_wasteland_rail.png", Color.white);
                Place(background, 0, 0, 1080, 1920);
                background.preserveAspect = false;
                var safe = CreateSafeArea(root.transform);
                var scrim = CreateSolidImage(safe, "TitleScrim", new Color(0, 0, 0, 0.48f));
                Place(scrim, 0, 670, 1000, 240);
                var brand = CreateText(safe, "StartupBrand", "LAST RAIL");
                ConfigureText(brand, "LAST RAIL", 96, TextAlignmentOptions.Center);
                Place(brand, 0, 670, 920, 180);
                var band = CreateSolidImage(safe, "StartupActionBand", new Color(0.025f, 0.035f, 0.04f, 0.94f));
                var bandRect = band.rectTransform;
                bandRect.anchorMin = bandRect.anchorMax = new Vector2(0.5f, 0);
                bandRect.pivot = new Vector2(0.5f, 0);
                bandRect.anchoredPosition = new Vector2(0, 36);
                bandRect.sizeDelta = new Vector2(960, 720);
                var status = CreateText(bandRect, "StartupStatus", "");
                ConfigureText(status, "", 36, TextAlignmentOptions.MidlineLeft);
                Place(status, 60, 270, 660, 88);
                var details = CreateText(bandRect, "StartupDetails", "");
                ConfigureText(details, "", 36, TextAlignmentOptions.Center);
                Place(details, 0, 146, 850, 120);
                details.richText = false;
                details.overflowMode = TextOverflowModes.Ellipsis;
                var statusPaths = new[] {
                    "Assets/_LASTRAIL/UI/StartupResources/download_surface_accent.png",
                    "Assets/_LASTRAIL/UI/Icons/ic_warning.png", "Assets/_LASTRAIL/UI/Icons/ic_retry.png",
                    "Assets/_LASTRAIL/UI/Icons/ic_info.png", "Assets/_LASTRAIL/UI/Icons/ic_lock.png" };
                EnsureSpriteImporter(statusPaths[0]);
                var icon = CreateImage(bandRect, "StartupStatusIcon", statusPaths[4], Color.white);
                Place(icon, -370, 270, 96, 96);
                var progress = CreateText(bandRect, "StartupProgressText", "");
                ConfigureText(progress, "", 36, TextAlignmentOptions.Center);
                Place(progress, 0, 52, 850, 44);
                var track = CreateSlicedImage(bandRect, "StartupProgressTrack", CommonSkinRoot + "/ui_slider_track_9slice.png", Color.white);
                Place(track, 0, 8, 850, 26);
                var fill = CreateImage(track.transform, "StartupProgressFill", CommonSkinRoot + "/ui_slider_fill_9slice.png", Color.white);
                Place(fill, 0, 0, 850, 26);
                fill.preserveAspect = false;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = 0;
                var primary = CreateStartupButton(bandRect, "StartupPrimary", -108, true);
                var secondary = CreateStartupButton(bandRect, "StartupSecondary", -272, false);
                var behaviour = root.AddComponent<StartupScreenRootBehaviour>();
                Bind(behaviour, ("safeAreaRoot", safe), ("statusText", status), ("detailsText", details),
                    ("progressText", progress), ("progressTrack", track), ("progressFill", fill), ("statusIcon", icon),
                    ("primaryButton", primary), ("secondaryButton", secondary),
                    ("primaryText", primary.GetComponentInChildren<TMP_Text>()), ("secondaryText", secondary.GetComponentInChildren<TMP_Text>()));
                var serialized = new SerializedObject(behaviour);
                var sprites = serialized.FindProperty("statusSprites");
                sprites.arraySize = statusPaths.Length;
                for (var i = 0; i < statusPaths.Length; i++)
                    sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(statusPaths[i]);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                behaviour.RenderAuth(AuthGateUxState.FirstLaunch());
                SavePrefab(root, "StartupScreen");
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Button CreateStartupButton(Transform parent, string name, float y, bool primary)
        {
            var image = CreateSlicedImage(parent, name, CommonSkinRoot + (primary ? "/ui_button_primary_9slice.png" : "/ui_button_secondary_9slice.png"), Color.white);
            Place(image, 0, y, 850, 144);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(image.transform, name + "Label", "");
            ConfigureText(text, "", 42, TextAlignmentOptions.Center);
            Place(text, 0, 0, 770, 120);
            text.raycastTarget = false;
            return button;
        }

        private static void BindCombatStatusSprites(FullyAnimatedCombatHudRootBehaviour behaviour)
        {
            var serialized = new SerializedObject(behaviour);
            var sprites = serialized.FindProperty("statusIconSprites");
            sprites.arraySize = CombatStatusIconCatalog.IconPaths.Count;
            for (var i = 0; i < sprites.arraySize; i++)
            {
                var path = CombatStatusIconCatalog.IconPaths[i];
                EnsureSpriteImporter(path);
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindCombatTransitionResources(FullyAnimatedCombatHudRootBehaviour behaviour)
        {
            var serialized = new SerializedObject(behaviour);
            var sprites = serialized.FindProperty("transitionSprites");
            sprites.arraySize = CombatTransitionResourceCatalog.SpritePaths.Count;
            for (var i = 0; i < sprites.arraySize; i++)
            {
                var path = CombatTransitionResourceCatalog.SpritePaths[i];
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var clips = serialized.FindProperty("transitionClips");
            clips.arraySize = CombatTransitionResourceCatalog.AudioPaths.Count;
            for (var i = 0; i < clips.arraySize; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(CombatTransitionResourceCatalog.AudioPaths[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

            var topStatusPanel = CreateSolidImage(
                exteriorLane,
                "CombatHudTopStatusPanel",
                new Color(0.04f, 0.07f, 0.08f, 0.78f));
            Place(topStatusPanel, 0f, 248f, 874f, 72f);

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

            var interiorLane = CreateTransparentGroup(safeArea, "InteriorBoardingCombatLane");
            Place(interiorLane, 0f, -348f, 1000f, 1080f);

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
            Place(squadSlots, -218f, 138f, 420f, 690f);
            var enemySlots = CreateTransparentGroup(interiorLane, "InvadingEnemySlots");
            Place(enemySlots, 292f, 118f, 360f, 620f);
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

            var bottomStatusPanel = CreateSolidImage(
                interiorLane,
                "CombatBottomStatusHudPanel",
                new Color(0.03f, 0.05f, 0.06f, 0.76f));
            Place(bottomStatusPanel, -210f, -432f, 520f, 190f);

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
                topStatusPanel.GetComponent<RectTransform>(),
                bottomStatusPanel.GetComponent<RectTransform>());
        }

        private static Image[] CreateCombatSkillIconSlots(Transform bottomStatusPanel)
        {
            var slots = new Image[5];
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = CreateImage(
                    bottomStatusPanel,
                    "SkillIconSlot_" + i,
                    CombatOperatorSkillIconCatalog.CreateLaunchDefinitions()[i].IconPath,
                    new Color(1f, 1f, 1f, 0.92f));
                Place(slot, -204f + i * 102f, -66f, 64f, 64f);
                slots[i] = slot;
            }

            return slots;
        }

        private static void BindCombatSkillIconSprites(FullyAnimatedCombatHudRootBehaviour behaviour, Image[] skillIconSlots)
        {
            var definitions = CombatOperatorSkillIconCatalog.CreateLaunchDefinitions();
            var serialized = new SerializedObject(behaviour);
            var slots = serialized.FindProperty("skillIconSlotImages");
            slots.arraySize = skillIconSlots.Length;
            for (var i = 0; i < skillIconSlots.Length; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = skillIconSlots[i];
            }

            var sprites = serialized.FindProperty("skillIconSprites");
            sprites.arraySize = definitions.Count;
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var element = sprites.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("operatorId").stringValue = definition.OperatorId;
                element.FindPropertyRelative("sprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(definition.IconPath);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateOperatorCombatSlots(Transform squadSlots)
        {
            var xPositions = new[] { -158f, -158f, -158f, 34f, 34f };
            var yPositions = new[] { -96f, -188f, -280f, -142f, -238f };
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
                Place(unit, xPositions[i], yPositions[i], 164f, 164f);
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
            var xPositions = new[] { -12f, 74f, -8f, 86f };
            var yPositions = new[] { -104f, -156f, -210f, -238f };
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
                Place(sprite, xPositions[i], yPositions[i], 140f, 140f);
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

        private static Image CreateSolidImage(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
                RectTransform topStatusPanel,
                RectTransform bottomStatusPanel)
            {
                ExteriorLane = exteriorLane;
                InteriorLane = interiorLane;
                PlayerSquadSlots = playerSquadSlots;
                InvadingEnemySlots = invadingEnemySlots;
                EngagementZone = engagementZone;
                TopStatusPanel = topStatusPanel;
                BottomStatusPanel = bottomStatusPanel;
            }

            public RectTransform ExteriorLane { get; }
            public RectTransform InteriorLane { get; }
            public RectTransform PlayerSquadSlots { get; }
            public RectTransform InvadingEnemySlots { get; }
            public RectTransform EngagementZone { get; }
            public RectTransform TopStatusPanel { get; }
            public RectTransform BottomStatusPanel { get; }
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
                CreateCodexArchivePolishSurface(safeArea);
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

        private static void CreateCodexArchivePolishSurface(Transform parent)
        {
            var mainPanel = CreateImage(
                parent,
                "CodexArchiveMainPanelImage",
                CodexArchivePolishPanelsRoot + "/codex_archive_main_panel.png",
                new Color(1f, 1f, 1f, 0.82f));
            mainPanel.preserveAspect = false;
            Place(mainPanel, 0f, 238f, 960f, 980f);

            var detailPanel = CreateImage(
                parent,
                "CodexArchiveDetailPanelImage",
                CodexArchivePolishPanelsRoot + "/codex_archive_record_detail_panel.png",
                new Color(1f, 1f, 1f, 0.76f));
            detailPanel.preserveAspect = false;
            Place(detailPanel, 0f, -318f, 920f, 260f);

            var loreTag = CreateImage(
                parent,
                "CodexArchiveLoreTagImage",
                CodexArchivePolishBadgesRoot + "/codex_archive_lore_tag.png",
                new Color(1f, 1f, 1f, 0.88f));
            Place(loreTag, -305f, -162f, 270f, 84f);

            var unlockSeal = CreateImage(
                parent,
                "CodexArchiveUnlockSealImage",
                CodexArchivePolishBadgesRoot + "/codex_archive_unlock_seal.png",
                new Color(1f, 1f, 1f, 0.88f));
            Place(unlockSeal, 350f, 665f, 78f, 78f);

            var lockedSeal = CreateImage(
                parent,
                "CodexArchiveLockedSealImage",
                CodexArchivePolishBadgesRoot + "/codex_archive_locked_seal.png",
                new Color(1f, 1f, 1f, 0.82f));
            Place(lockedSeal, 424f, -162f, 74f, 74f);

            var topEdge = CreateImage(
                parent,
                "CodexArchiveScrollEdgeTopImage",
                CodexArchivePolishFramesRoot + "/codex_archive_scroll_edge.png",
                new Color(1f, 1f, 1f, 0.78f));
            topEdge.preserveAspect = false;
            Place(topEdge, 0f, 636f, 850f, 64f);

            var bottomEdge = CreateImage(
                parent,
                "CodexArchiveScrollEdgeBottomImage",
                CodexArchivePolishFramesRoot + "/codex_archive_scroll_edge.png",
                new Color(1f, 1f, 1f, 0.70f));
            bottomEdge.preserveAspect = false;
            Place(bottomEdge, 0f, -538f, 820f, 54f);

            var bottomNav = CreateImage(
                parent,
                "CodexArchiveBottomNavAccentImage",
                CodexArchivePolishButtonsRoot + "/codex_archive_bottom_nav_accent.png",
                new Color(1f, 1f, 1f, 0.62f));
            bottomNav.preserveAspect = false;
            Place(bottomNav, 0f, -720f, 860f, 82f);
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
                    CreateStoryProgressVisualPolish(safeArea);
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

        private static void CreateStoryProgressVisualPolish(Transform parent)
        {
            var layer = CreateVisualLayer(parent, "StoryProgressVisualPolishLayer");
            var timelinePanel = CreateImage(
                layer,
                "StoryProgressTimelinePanelImage",
                StoryProgressPolishPanelsRoot + "/story_progress_timeline_panel.png",
                new Color(1f, 1f, 1f, 0.72f));
            timelinePanel.preserveAspect = false;
            Place(timelinePanel, 0f, 285f, 900f, 430f);

            var routeSpine = CreateImage(
                layer,
                "StoryProgressRouteSpineImage",
                StoryProgressPolishFramesRoot + "/story_progress_route_spine.png",
                new Color(1f, 1f, 1f, 0.82f));
            routeSpine.preserveAspect = false;
            Place(routeSpine, 0f, 365f, 820f, 112f);

            var currentNode = CreateImage(
                layer,
                "StoryProgressCurrentNodeGlowImage",
                StoryProgressPolishBadgesRoot + "/story_progress_current_node_glow.png",
                new Color(1f, 1f, 1f, 0.92f));
            Place(currentNode, -320f, 286f, 92f, 92f);

            var clearedSeal = CreateImage(
                layer,
                "StoryProgressClearedNodeSealImage",
                StoryProgressPolishBadgesRoot + "/story_progress_cleared_node_seal.png",
                new Color(1f, 1f, 1f, 0.82f));
            Place(clearedSeal, 305f, 282f, 86f, 86f);

            var lockedMist = CreateImage(
                layer,
                "StoryProgressLockedNodeMistImage",
                StoryProgressPolishOverlaysRoot + "/story_progress_locked_node_mist.png",
                new Color(1f, 1f, 1f, 0.58f));
            lockedMist.preserveAspect = false;
            Place(lockedMist, 298f, 160f, 285f, 112f);

            var raidGate = CreateImage(
                layer,
                "StoryProgressRaidGateBadgeImage",
                StoryProgressPolishBadgesRoot + "/story_progress_raid_gate_badge.png",
                new Color(1f, 1f, 1f, 0.86f));
            Place(raidGate, -318f, -114f, 260f, 130f);

            var statusRibbon = CreateImage(
                layer,
                "StoryProgressNarrativeStatusRibbonImage",
                StoryProgressPolishBadgesRoot + "/story_progress_narrative_status_ribbon.png",
                new Color(1f, 1f, 1f, 0.78f));
            statusRibbon.preserveAspect = false;
            Place(statusRibbon, 258f, -158f, 335f, 96f);

            var codexHint = CreateImage(
                layer,
                "StoryProgressCodexUnlockHintPanelImage",
                StoryProgressPolishPanelsRoot + "/story_progress_codex_unlock_hint_panel.png",
                new Color(1f, 1f, 1f, 0.66f));
            codexHint.preserveAspect = false;
            Place(codexHint, 0f, -350f, 780f, 205f);

            var bottomNav = CreateImage(
                layer,
                "StoryProgressBottomNavAccentImage",
                StoryProgressPolishButtonsRoot + "/story_progress_bottom_nav_accent.png",
                new Color(1f, 1f, 1f, 0.58f));
            bottomNav.preserveAspect = false;
            Place(bottomNav, 0f, -720f, 860f, 82f);
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
                "MissionLoadingRailIndicatorImage",
                SystemFeedbackLoadingRoot + "/loading_rail_indicator.png",
                new Color(1f, 1f, 1f, 0.46f));
            var warning = CreateImage(
                layer,
                "MissionWarningStripBannerAccentImage",
                SystemFeedbackWarningRoot + "/warning_strip_banner_accent.png",
                new Color(1f, 1f, 1f, 0.36f));
            var progress = CreateImage(
                parent,
                "MissionProgressHudImage",
                MissionShellPolishProgressRoot + "/mission_progress_rail_strip.png",
                new Color(1f, 1f, 1f, 0.88f));
            var dots = CreateImage(
                parent,
                "MissionInlineStatusDotsImage",
                SystemFeedbackStatusRoot + "/inline_status_dot_set.png",
                new Color(1f, 1f, 1f, 0.82f));

            Place(loading, -32f, -702f, 360f, 90f);
            Place(warning, -32f, -704f, 420f, 66f);
            Place(progress, 235f, 650f, 360f, 64f);
            Place(dots, 365f, 646f, 190f, 48f);
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
