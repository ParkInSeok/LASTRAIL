using System;
using NUnit.Framework;
using LastRail.Gacha;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchAudioCueReadinessTests
    {
        [Test]
        public void FromMissionGachaAndCombat_CollectsLaunchSfxBgmAndVibrationCueRefsWithoutAssets()
        {
            var mission = MissionStartState();
            var gacha = new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(GachaResponse())),
                "Act 1 구조 신호"));
            var combat = FullyAnimatedCombatHudViewState.FromCombatState(
                RaidCombat(),
                MobileCombatHudPresentationPolicy.FromCombatState(
                    RaidCombat(),
                    MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48)));

            var report = LaunchAudioCueReadinessReport.FromViewStates(mission, gacha, combat);

            Assert.That(report.BgmLoopCueId, Is.EqualTo("bgm_act1_loop"));
            Assert.That(report.UsesSingleBgmLoop, Is.EqualTo(true));
            Assert.That(report.PreventsBgmOverlap, Is.EqualTo(true));
            Assert.That(report.Mission.SoundCueIds, Does.Contain("sfx_mission_start_confirm"));
            Assert.That(report.Mission.VibrationCueIds, Does.Contain("vibration_mission_start"));
            Assert.That(report.Gacha.SoundCueIds, Does.Contain("sfx_gacha_elite"));
            Assert.That(report.Gacha.VibrationCueIds, Does.Contain("vibration_gacha_heavy"));
            Assert.That(report.Combat.SoundCueIds, Does.Contain("sfx_boss_phase"));
            Assert.That(report.Combat.VibrationCueIds, Does.Contain("vibration_boss_phase"));
            Assert.That(report.Combat.PresentationCueIds, Does.Contain("boss_phase_transition"));
            Assert.That(report.RequiresNativeVibrationBridge, Is.EqualTo(true));
            Assert.That(report.HasNativeVibrationImplementation, Is.EqualTo(false));
            Assert.That(report.HasExternalAudioAsset, Is.EqualTo(false));
            Assert.That(report.HasServerTransport, Is.EqualTo(false));
            Assert.That(report.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(report.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(report.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void BgmLoopPlaybackPolicy_DoesNotRestartSameLaunchLoopAcrossSceneTransitions()
        {
            var stopped = LaunchBgmLoopPlaybackPolicy.Stopped();

            var first = stopped.RequestLoop("bgm_act1_loop");
            var same = first.NextPolicy.RequestLoop("bgm_act1_loop");

            Assert.That(first.ShouldStartLoop, Is.EqualTo(true));
            Assert.That(first.ShouldStopCurrentLoop, Is.EqualTo(false));
            Assert.That(first.PreventsOverlap, Is.EqualTo(true));
            Assert.That(same.ShouldStartLoop, Is.EqualTo(false));
            Assert.That(same.ShouldStopCurrentLoop, Is.EqualTo(false));
            Assert.That(same.DecisionReason, Is.EqualTo("already_playing_single_loop"));
            Assert.That(same.NextPolicy.CurrentLoopCueId, Is.EqualTo("bgm_act1_loop"));
            Assert.That(same.NextPolicy.IsPlaying, Is.EqualTo(true));
        }

        [Test]
        public void ToJson_WritesStableReadinessFieldsWithoutSensitiveTruthOrRawPayload()
        {
            var report = LaunchAudioCueReadinessReport.FromViewStates(
                MissionResultState(),
                new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                    GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(GachaResponse())),
                    "Act 1 구조 신호")),
                FullyAnimatedCombatHudViewState.FromCombatState(
                    RaidCombat(),
                    MobileCombatHudPresentationPolicy.FromCombatState(
                        RaidCombat(),
                        MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48))));

            var json = LaunchAudioCueReadinessJsonAdapter.ToJson(report);

            Assert.That(json.Contains("\"bgmLoopCueId\":\"bgm_act1_loop\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesSingleBgmLoop\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"preventsBgmOverlap\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"surfaceKey\":\"mission_result\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"sfx_mission_result\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"sfx_gacha_elite\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"vibration_boss_phase\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresNativeVibrationBridge\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasExternalAudioAsset\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasNativeVibrationImplementation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("GoogleSignIn"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("economyGrant"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
            Assert.That(json.Contains("rawAudio"), Is.EqualTo(false));
            Assert.That(json.Contains("audioPath"), Is.EqualTo(false));
        }

        private static MissionShellScreenViewState MissionStartState()
        {
            return new MissionShellScreenViewState(
                MissionShellSurface.Briefing,
                "마지막 출발",
                "act1.2026-08-27.001",
                new[] { "조금 더 기다리는 위험이 사람을 살립니다." },
                new[] { "narrative.opn_01_last_departure.briefing.line_001" },
                "마지막 출발 시작",
                "narrative.opn_01_last_departure.mission_start.line_001",
                true,
                "작전 선택으로",
                null,
                null,
                null,
                null,
                false,
                false,
                false);
        }

        private static MissionShellScreenViewState MissionResultState()
        {
            return new MissionShellScreenViewState(
                MissionShellSurface.Result,
                "작전 결과",
                null,
                new[] { "구조 신호가 이어졌습니다." },
                Array.Empty<string>(),
                "보상 확인",
                null,
                true,
                "작전 선택으로",
                null,
                null,
                null,
                null,
                false,
                false,
                false);
        }

        private static FullyAnimatedCombatPresentationState RaidCombat()
        {
            return FullyAnimatedCombatPresentationState.CreateRaidBoss(
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
        }

        private static GachaPullResponseDto GachaResponse()
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                true,
                "batch_001",
                GachaPullResultStatus.Accepted,
                GachaPullCount.FromValue(1),
                "1",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPostPityStateDto(42, 12, 22, 2, 4),
                new[]
                {
                    new GachaPullResultDto(
                        1,
                        GachaRarity.Elite,
                        GachaPullResultKind.Operator,
                        "result_001",
                        new GachaRewardDto("OPERATOR", "op_vex", 1),
                        true,
                        "PICKUP_ELITE_60",
                        false,
                        null)
                },
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                null,
                null);
        }
    }
}
