using System;
using NUnit.Framework;
using LastRail.Gacha;
using LastRail.Presentation;
using LastRail.Presentation.Animation;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchPlayerFacingQaReadinessGateTests
    {
        [Test]
        public void GateCompletedPlayerFacingPresenters_CoversSevenScreensWithMobilePerformanceAndPolishBudgets()
        {
            var gate = LaunchPlayerFacingQaReadinessGate.ForCompletedPresenters(
                MissionShellState(),
                GachaState(),
                CombatState(),
                CodexState(),
                RewardState(MissionShellSurface.RewardReview),
                RewardState(MissionShellSurface.OfflineReward),
                RewardState(MissionShellSurface.StoryProgressSummary),
                AnimationVfxBudgetSpec.ActOneLaunchRecommended(),
                AudioReport());

            Assert.That(gate.ScreenCount, Is.EqualTo(7));
            Assert.That(gate.PassCount, Is.EqualTo(7));
            Assert.That(gate.FailCount, Is.EqualTo(0));
            Assert.That(gate.WarningCount, Is.EqualTo(2));
            Assert.That(gate.BlockerCount, Is.EqualTo(2));
            Assert.That(gate.ScreenKeys[0], Is.EqualTo("mission_shell"));
            Assert.That(gate.ScreenKeys[1], Is.EqualTo("gacha_screen"));
            Assert.That(gate.ScreenKeys[2], Is.EqualTo("fully_animated_combat_hud"));
            Assert.That(gate.ScreenKeys[3], Is.EqualTo("codex_archive"));
            Assert.That(gate.ScreenKeys[4], Is.EqualTo("reward_review"));
            Assert.That(gate.ScreenKeys[5], Is.EqualTo("offline_reward"));
            Assert.That(gate.ScreenKeys[6], Is.EqualTo("story_progress_summary"));
            Assert.That(gate.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(gate.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(gate.RequiresTextOverflowVerification, Is.EqualTo(true));
            Assert.That(gate.KeepsHudReadableOverAnimation, Is.EqualTo(true));
            Assert.That(gate.RequiresTenPullGridOverflowQa, Is.EqualTo(true));
            Assert.That(gate.RequiresCodexDetailScrollOrClampQa, Is.EqualTo(true));
            Assert.That(gate.RequiresRewardStatusRetryLabelQa, Is.EqualTo(true));
            Assert.That(gate.TargetFps, Is.EqualTo(60));
            Assert.That(gate.LowEndFallbackFps, Is.EqualTo(30));
            Assert.That(gate.LargeVfxConcurrentLimit, Is.EqualTo(2));
            Assert.That(gate.HitSparkConcurrentMin, Is.EqualTo(8));
            Assert.That(gate.HitSparkConcurrentMax, Is.EqualTo(12));
            Assert.That(gate.FullScreenFlashMaxSeconds, Is.EqualTo(0.15f));
            Assert.That(gate.BossPhaseVfxMinSeconds, Is.EqualTo(1.0f));
            Assert.That(gate.BossPhaseVfxMaxSeconds, Is.EqualTo(2.0f));
            Assert.That(gate.ScreenShakeTierCount, Is.EqualTo(3));
            Assert.That(gate.UsesSingleBgmLoop, Is.EqualTo(true));
            Assert.That(gate.RequiresNativeVibrationBridge, Is.EqualTo(true));
            Assert.That(gate.Blockers[0], Is.EqualTo("editor_device_screenshot_qa_unavailable"));
            Assert.That(gate.Blockers[1], Is.EqualTo("native_vibration_bridge_pending"));
            Assert.That(gate.IsReadyForEditorBindingPass, Is.EqualTo(true));
            Assert.That(gate.IsLaunchQaComplete, Is.EqualTo(false));
        }

        [Test]
        public void ToDebugJson_WritesScreenKeysCountsAndBlockersWithoutCopyTokensTruthOrServerPayloads()
        {
            var gate = LaunchPlayerFacingQaReadinessGate.ForCompletedPresenters(
                MissionShellState(),
                GachaState(),
                CombatState(),
                CodexState(),
                RewardState(MissionShellSurface.RewardReview),
                RewardState(MissionShellSurface.OfflineReward),
                RewardState(MissionShellSurface.StoryProgressSummary),
                AnimationVfxBudgetSpec.ActOneLaunchRecommended(),
                AudioReport());

            var json = LaunchPlayerFacingQaReadinessGateJsonAdapter.ToJson(gate);

            Assert.That(json.Contains("\"screenCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"passCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"warningCount\":2"), Is.EqualTo(true));
            Assert.That(json.Contains("\"failCount\":0"), Is.EqualTo(true));
            Assert.That(json.Contains("\"screenKeys\":[\"mission_shell\",\"gacha_screen\",\"fully_animated_combat_hud\",\"codex_archive\",\"reward_review\",\"offline_reward\",\"story_progress_summary\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("\"blockers\":[\"editor_device_screenshot_qa_unavailable\",\"native_vibration_bridge_pending\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("\"targetFps\":60"), Is.EqualTo(true));
            Assert.That(json.Contains("\"lowEndFallbackFps\":30"), Is.EqualTo(true));
            Assert.That(json.Contains("\"largeVfxConcurrentLimit\":2"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hitSparkConcurrentMin\":8"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hitSparkConcurrentMax\":12"), Is.EqualTo(true));
            Assert.That(json.Contains("\"fullScreenFlashMaxSeconds\":0.15"), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesSingleBgmLoop\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresNativeVibrationBridge\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("마지막 출발"), Is.EqualTo(false));
            Assert.That(json.Contains("요원 모집"), Is.EqualTo(false));
            Assert.That(json.Contains("작전 보상"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("GoogleSignIn"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        [Test]
        public void GateRejectsNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() => LaunchPlayerFacingQaReadinessGate.ForCompletedPresenters(
                null,
                GachaState(),
                CombatState(),
                CodexState(),
                RewardState(MissionShellSurface.RewardReview),
                RewardState(MissionShellSurface.OfflineReward),
                RewardState(MissionShellSurface.StoryProgressSummary),
                AnimationVfxBudgetSpec.ActOneLaunchRecommended(),
                AudioReport()));
            Assert.Throws<ArgumentNullException>(() => LaunchPlayerFacingQaReadinessGateJsonAdapter.ToJson(null));
        }

        private static MissionShellScreenViewState MissionShellState()
        {
            return new MissionShellScreenViewState(
                MissionShellSurface.Briefing,
                "마지막 출발",
                "Act 1",
                new[] { "briefing" },
                new[] { "narrative.opn_01_last_departure.briefing.line_001" },
                "마지막 출발 시작",
                "narrative.opn_01_last_departure.mission_start.line_001",
                true,
                null,
                MissionShellSurface.LoadingHint,
                Array.Empty<MissionShellMissionRowState>(),
                null,
                null,
                false,
                false,
                false);
        }

        private static GachaScreenViewState GachaState()
        {
            var screen = GachaScreenPresentationState.Idle(
                "banner_act1_launch",
                "Act 1 구조 신호",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPityDisplayState("banner_act1_launch", 29, 29, 59, 9));
            return new GachaScreenViewState(screen);
        }

        private static FullyAnimatedCombatHudViewState CombatState()
        {
            var combat = FullyAnimatedCombatPresentationState.CreateRaidBoss(
                "raid_raider_king_engine",
                2,
                "boss_crown_engine",
                "aug_pkg_raid_001",
                "tactic_pkg_raid_001",
                new[]
                {
                    new CombatUnitAnimationState("op_vex", CombatUnitRole.Operator, "skill_cue", "operator_skill_cue"),
                    new CombatUnitAnimationState("boss_crown_engine", CombatUnitRole.RaidBoss, "boss_phase_attack", "boss_attack_cue")
                },
                CombatPresentationCueSet.ForRaidBossPhase(
                    "damage_number_pop",
                    "hit_flash",
                    "hit_stop_heavy",
                    "screen_shake_heavy",
                    "vfx_boss_phase",
                    "sfx_boss_phase",
                    "vibration_heavy",
                    "boss_phase_transition"));
            return FullyAnimatedCombatHudViewState.FromCombatState(
                combat,
                MobileCombatHudPresentationPolicy.FromCombatState(
                    combat,
                    MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48)));
        }

        private static CodexArchiveScreenViewState CodexState()
        {
            return new CodexArchiveScreenViewState(
                "기록 보관소",
                "subtitle",
                "act1.2026-08-30.001",
                new[]
                {
                    new CodexArchiveScreenEntryViewState(
                        "opn_01_last_departure",
                        "마지막 출발",
                        "narrative.opn_01_last_departure.codex.entry",
                        "codex",
                        false,
                        true,
                        true,
                        true,
                        80,
                        180,
                        false,
                        "READY")
                },
                0,
                true,
                true,
                true);
        }

        private static RewardStoryScreenViewState RewardState(MissionShellSurface surface)
        {
            return new RewardStoryScreenViewState(
                surface,
                surface == MissionShellSurface.OfflineReward ? "오프라인 보상" : surface == MissionShellSurface.StoryProgressSummary ? "스토리 진행" : "작전 보상",
                "subtitle",
                new[] { "display" },
                surface == MissionShellSurface.RewardReview
                    ? new[] { "narrative.raid_raider_king_engine.failure_refund.toast" }
                    : Array.Empty<string>(),
                surface == MissionShellSurface.StoryProgressSummary ? "기록 보관소" : "확인",
                null,
                surface == MissionShellSurface.StoryProgressSummary
                    ? Array.Empty<RewardStoryScreenRewardLineViewState>()
                    : new[] { new RewardStoryScreenRewardLineViewState("line_001", "cur_credits", "크레딧", "90개", "OFFLINE_REWARD", false) },
                "opn_03_water_stop",
                "급수 정차",
                "최대 7시간",
                4,
                "기록 4개 열람 가능",
                true,
                surface == MissionShellSurface.RewardReview,
                surface == MissionShellSurface.RewardReview ? "COMMITTED" : null,
                surface == MissionShellSurface.RewardReview ? "narrative.raid_raider_king_engine.failure_refund.toast" : null,
                null,
                null,
                surface == MissionShellSurface.OfflineReward,
                false,
                false,
                false);
        }

        private static LaunchAudioCueReadinessReport AudioReport()
        {
            return LaunchAudioCueReadinessReport.FromViewStates(
                MissionShellState(),
                GachaState(),
                CombatState());
        }
    }
}
