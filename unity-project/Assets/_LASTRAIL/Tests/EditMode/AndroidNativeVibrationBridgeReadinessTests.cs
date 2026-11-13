using System;
using NUnit.Framework;
using LastRail.Gacha;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Tests.EditMode
{
    public sealed class AndroidNativeVibrationBridgeReadinessTests
    {
        [Test]
        public void FromAudioReadiness_CollectsLaunchVibrationCueIdsAndMarksMissingNativeBridgeNonBlocking()
        {
            var report = LaunchAudioCueReadinessReport.FromViewStates(
                MissionStartState(),
                new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                    GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(GachaResponse())),
                    "Act 1 구조 신호")),
                FullyAnimatedCombatHudViewState.FromCombatState(
                    RaidCombat(),
                    MobileCombatHudPresentationPolicy.FromCombatState(
                        RaidCombat(),
                        MobileSafeAreaInsets.Portrait(1080, 1920, 72, 48))));

            var readiness = AndroidNativeVibrationBridgeReadiness.FromAudioReadiness(
                report,
                AndroidNativeVibrationBridgeStatus.NativeBridgeMissing,
                vibrationEnabled: true);

            Assert.That(readiness.Status, Is.EqualTo(AndroidNativeVibrationBridgeStatus.NativeBridgeMissing));
            Assert.That(readiness.VibrationCueIds, Does.Contain("vibration_mission_start"));
            Assert.That(readiness.VibrationCueIds, Does.Contain("vibration_gacha_heavy"));
            Assert.That(readiness.VibrationCueIds, Does.Contain("vibration_skill"));
            Assert.That(readiness.VibrationCueIds, Does.Contain("vibration_ultimate"));
            Assert.That(readiness.VibrationCueIds, Does.Contain("vibration_boss_phase"));
            Assert.That(readiness.CueCount, Is.EqualTo(readiness.VibrationCueIds.Count));
            Assert.That(readiness.RequiresNativeVibrationBridge, Is.EqualTo(true));
            Assert.That(readiness.HasNativeVibrationBridge, Is.EqualTo(false));
            Assert.That(readiness.FallsBackToNoVibration, Is.EqualTo(true));
            Assert.That(readiness.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(readiness.Blockers, Does.Contain("native_vibration_bridge_pending"));
            Assert.That(readiness.HasRawDeviceIdentifier, Is.EqualTo(false));
            Assert.That(readiness.HasRawLogPayload, Is.EqualTo(false));
        }

        [Test]
        public void DispatchPolicy_RespectsDisabledUnsupportedAndUnavailableWithoutBlockingGameplay()
        {
            var cue = AndroidVibrationCue.FromCueId("vibration_ultimate");

            var disabled = AndroidNativeVibrationDispatchPolicy.ForStatus(
                AndroidNativeVibrationBridgeStatus.DisabledByUser,
                vibrationEnabled: false).TryDispatch(cue);
            var unsupported = AndroidNativeVibrationDispatchPolicy.ForStatus(
                AndroidNativeVibrationBridgeStatus.UnsupportedPlatform,
                vibrationEnabled: true).TryDispatch(cue);
            var missing = AndroidNativeVibrationDispatchPolicy.ForStatus(
                AndroidNativeVibrationBridgeStatus.NativeBridgeMissing,
                vibrationEnabled: true).TryDispatch(cue);
            var ready = AndroidNativeVibrationDispatchPolicy.ForStatus(
                AndroidNativeVibrationBridgeStatus.Available,
                vibrationEnabled: true).TryDispatch(cue);

            Assert.That(disabled.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.SkippedDisabled));
            Assert.That(unsupported.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.SkippedUnsupportedPlatform));
            Assert.That(missing.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.FailedNativeBridgeMissing));
            Assert.That(ready.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.WouldDispatch));
            Assert.That(disabled.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(unsupported.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(missing.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(ready.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(missing.CanFallbackToNoVibration, Is.EqualTo(true));
            Assert.That(ready.CanFallbackToNoVibration, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesStableReadinessFieldsWithoutSensitiveDeviceOrServerPayloads()
        {
            var readiness = AndroidNativeVibrationBridgeReadiness.FromCueIds(
                "combat_raidboss",
                new[] { "vibration_heavy", "vibration_skill", "vibration_boss_phase" },
                AndroidNativeVibrationBridgeStatus.NativeBridgeMissing,
                vibrationEnabled: true);

            var json = AndroidNativeVibrationBridgeReadinessJsonAdapter.ToJson(readiness);

            Assert.That(json, Is.EqualTo(
                "{\"surfaceKey\":\"combat_raidboss\"," +
                "\"status\":\"NativeBridgeMissing\"," +
                "\"vibrationEnabled\":true," +
                "\"requiresNativeVibrationBridge\":true," +
                "\"hasNativeVibrationBridge\":false," +
                "\"fallsBackToNoVibration\":true," +
                "\"isBlockingGameplay\":false," +
                "\"cueCount\":3," +
                "\"vibrationCueIds\":[\"vibration_heavy\",\"vibration_skill\",\"vibration_boss_phase\"]," +
                "\"blockerCount\":1," +
                "\"blockers\":[\"native_vibration_bridge_pending\"]," +
                "\"hasRawDeviceIdentifier\":false," +
                "\"hasRawLogPayload\":false}"));
            Assert.That(json.Contains("deviceId"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("GoogleSignIn"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
            Assert.That(json.Contains("frameData"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        private static MissionShellScreenViewState MissionStartState()
        {
            return new MissionShellScreenViewState(
                MissionShellSurface.Briefing,
                "마지막 출발",
                "act1.2026-08-30.001",
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
                    new CombatUnitAnimationState("op_rin", CombatUnitRole.Operator, "ultimate_cue", "operator_ultimate_cue"),
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
