using NUnit.Framework;
using LastRail.Presentation.Audio;

namespace LastRail.Tests.EditMode
{
    public sealed class AndroidVibrationDeviceValidationReadinessTests
    {
        [Test]
        public void FromBridgeReadiness_CountsCueIntensitiesAndMarksDeviceValidationPending()
        {
            var bridgeReadiness = AndroidNativeVibrationBridgeReadiness.FromCueIds(
                "launch_player_facing_feedback",
                new[]
                {
                    "vibration_light_hit",
                    "vibration_skill_ready",
                    "vibration_gacha_heavy",
                    "vibration_boss_phase"
                },
                AndroidNativeVibrationBridgeStatus.UnsupportedPlatform,
                vibrationEnabled: true);

            var readiness = AndroidVibrationDeviceValidationReadiness.FromBridgeReadiness(bridgeReadiness);

            Assert.That(readiness.CueCount, Is.EqualTo(4));
            Assert.That(readiness.LightCueCount, Is.EqualTo(1));
            Assert.That(readiness.MediumCueCount, Is.EqualTo(1));
            Assert.That(readiness.HeavyCueCount, Is.EqualTo(2));
            Assert.That(readiness.CanRunOnCurrentDevice, Is.EqualTo(false));
            Assert.That(readiness.RequiresManualAndroidDevicePass, Is.EqualTo(true));
            Assert.That(readiness.FallsBackToNoVibration, Is.EqualTo(true));
            Assert.That(readiness.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(readiness.Blockers, Does.Contain("android_vibration_device_validation_pending"));
            Assert.That(readiness.HasRawDeviceIdentifier, Is.EqualTo(false));
            Assert.That(readiness.HasRawLogPayload, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesCueCountsStatusAndBlockersWithoutDeviceIdsTokensOrServerPayloads()
        {
            var readiness = AndroidVibrationDeviceValidationReadiness.FromCueIds(
                "combat_raidboss",
                new[] { "vibration_light_hit", "vibration_skill", "vibration_boss_phase" },
                AndroidNativeVibrationBridgeStatus.UnsupportedPlatform,
                vibrationEnabled: true);

            var json = AndroidVibrationDeviceValidationReadinessJsonAdapter.ToJson(readiness);

            Assert.That(json, Is.EqualTo(
                "{\"surfaceKey\":\"combat_raidboss\"," +
                "\"bridgeStatus\":\"UnsupportedPlatform\"," +
                "\"vibrationEnabled\":true," +
                "\"canRunOnCurrentDevice\":false," +
                "\"requiresManualAndroidDevicePass\":true," +
                "\"fallsBackToNoVibration\":true," +
                "\"isBlockingGameplay\":false," +
                "\"cueCount\":3," +
                "\"lightCueCount\":1," +
                "\"mediumCueCount\":1," +
                "\"heavyCueCount\":1," +
                "\"cueIds\":[\"vibration_light_hit\",\"vibration_skill\",\"vibration_boss_phase\"]," +
                "\"blockerCount\":1," +
                "\"blockers\":[\"android_vibration_device_validation_pending\"]," +
                "\"hasRawDeviceIdentifier\":false," +
                "\"hasRawLogPayload\":false}"));
            Assert.That(json.Contains("deviceId"), Is.EqualTo(false));
            Assert.That(json.Contains("androidId"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
        }
    }
}
