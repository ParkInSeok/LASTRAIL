using System;
using NUnit.Framework;
using LastRail.Presentation;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchScreenScreenshotQaReportTests
    {
        private static readonly string[] SevenScreenKeys =
        {
            "mission_shell",
            "gacha_screen",
            "fully_animated_combat_hud",
            "codex_archive",
            "reward_review",
            "offline_reward",
            "story_progress_summary"
        };

        [Test]
        public void UnavailableCapture_KeepsSevenScreenCodePassesAndBlocksScreenshotQaOnly()
        {
            var report = LaunchScreenScreenshotQaReport.ForUnavailableCapture(
                SevenScreenKeys,
                codeLayoutPassCount: 7,
                codeLayoutFailCount: 0);

            Assert.That(report.ScreenCount, Is.EqualTo(7));
            Assert.That(report.CodeLayoutPassCount, Is.EqualTo(7));
            Assert.That(report.CodeLayoutFailCount, Is.EqualTo(0));
            Assert.That(report.ScreenshotCaptureAttempted, Is.EqualTo(false));
            Assert.That(report.ScreenshotPassCount, Is.EqualTo(0));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.FailCount, Is.EqualTo(0));
            Assert.That(report.BlockerCount, Is.EqualTo(1));
            Assert.That(report.Blockers[0], Is.EqualTo("editor_device_screenshot_qa_unavailable"));
            Assert.That(report.IsReadyForDeviceScreenshotQa, Is.EqualTo(false));
            Assert.That(report.HasServerTransport, Is.EqualTo(false));
            Assert.That(report.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(report.HasOfflineRewardCalculation, Is.EqualTo(false));
            Assert.That(report.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(report.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void CapturedPaths_MarkAllSevenScreensCapturedWithoutEmbeddingPathsInDebugJson()
        {
            var report = LaunchScreenScreenshotQaReport.ForCapturedScreens(
                SevenScreenKeys,
                codeLayoutPassCount: 7,
                codeLayoutFailCount: 0,
                screenshotPaths: new[]
                {
                    "Temp/LastRailScreenQa/mission_shell.png",
                    "Temp/LastRailScreenQa/gacha_screen.png",
                    "Temp/LastRailScreenQa/fully_animated_combat_hud.png",
                    "Temp/LastRailScreenQa/codex_archive.png",
                    "Temp/LastRailScreenQa/reward_review.png",
                    "Temp/LastRailScreenQa/offline_reward.png",
                    "Temp/LastRailScreenQa/story_progress_summary.png"
                });

            Assert.That(report.ScreenshotCaptureAttempted, Is.EqualTo(true));
            Assert.That(report.ScreenshotPassCount, Is.EqualTo(7));
            Assert.That(report.WarningCount, Is.EqualTo(0));
            Assert.That(report.BlockerCount, Is.EqualTo(0));
            Assert.That(report.IsReadyForDeviceScreenshotQa, Is.EqualTo(true));
            Assert.That(report.ScreenshotPaths[0], Does.EndWith("mission_shell.png"));
        }

        [Test]
        public void ToJson_WritesStableCountsStatusesAndBlockersWithoutSensitiveOrTruthPayloads()
        {
            var report = LaunchScreenScreenshotQaReport.ForUnavailableCapture(
                SevenScreenKeys,
                codeLayoutPassCount: 7,
                codeLayoutFailCount: 0);

            var json = LaunchScreenScreenshotQaReportJsonAdapter.ToJson(report);

            Assert.That(json.Contains("\"screenCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"codeLayoutPassCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"screenshotCaptureAttempted\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"screenshotPassCount\":0"), Is.EqualTo(true));
            Assert.That(json.Contains("\"blockers\":[\"editor_device_screenshot_qa_unavailable\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("\"screenKeys\":[\"mission_shell\",\"gacha_screen\",\"fully_animated_combat_hud\",\"codex_archive\",\"reward_review\",\"offline_reward\",\"story_progress_summary\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("Temp/LastRailScreenQa"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        [Test]
        public void RejectsNullOrIncompleteInputs()
        {
            Assert.Throws<ArgumentNullException>(() => LaunchScreenScreenshotQaReport.ForUnavailableCapture(null, 7, 0));
            Assert.Throws<ArgumentException>(() => LaunchScreenScreenshotQaReport.ForUnavailableCapture(new[] { "mission_shell", " " }, 1, 0));
            Assert.Throws<ArgumentException>(() => LaunchScreenScreenshotQaReport.ForCapturedScreens(SevenScreenKeys, 7, 0, new[] { "one.png" }));
            Assert.Throws<ArgumentNullException>(() => LaunchScreenScreenshotQaReportJsonAdapter.ToJson(null));
        }
    }
}
