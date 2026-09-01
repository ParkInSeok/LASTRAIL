using System;
using NUnit.Framework;
using LastRail.Presentation;
using UnityEngine;

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
        public void ScreenshotFidelity_RejectsUniformGrayFrameBeforeMarkingScreenCaptured()
        {
            var pixels = CreateFilledPixels(1080, 1920, new Color32(128, 128, 128, 255));

            var result = LaunchScreenshotFidelityAnalyzer.Analyze(
                "gacha_screen",
                1080,
                1920,
                pixels,
                expectedTextCount: 6,
                visibleGraphicCount: 12,
                missingAssetReferenceCount: 0);

            Assert.That(result.Passed, Is.EqualTo(false));
            Assert.That(result.Reasons, Does.Contain("uniform_or_blank_frame"));
            Assert.That(result.Reasons, Does.Contain("low_pixel_diversity"));
        }

        [Test]
        public void ScreenshotFidelity_AcceptsDiverseScreenWithVisibleTextAndGraphics()
        {
            var pixels = CreateDiversePixels(1080, 1920);

            var result = LaunchScreenshotFidelityAnalyzer.Analyze(
                "mission_shell",
                1080,
                1920,
                pixels,
                expectedTextCount: 8,
                visibleGraphicCount: 18,
                missingAssetReferenceCount: 0);

            Assert.That(result.Passed, Is.EqualTo(true));
            Assert.That(result.PixelDiversityScore, Is.GreaterThan(64));
            Assert.That(result.NonBackgroundPixelRatio, Is.GreaterThan(0.05f));
            Assert.That(result.Reasons, Is.Empty);
        }

        [Test]
        public void CapturedScreens_UseFidelityResultsForPassWarningAndFailCountsWithoutPathsInJson()
        {
            var results = new[]
            {
                LaunchScreenshotFidelityResult.Pass("mission_shell", "mission_shell.png", 160, 0.42f, 8, 18),
                LaunchScreenshotFidelityResult.Fail("gacha_screen", "gacha_screen.png", 1, 0f, 6, 0, new[] { "uniform_or_blank_frame" })
            };

            var report = LaunchScreenScreenshotQaReport.ForCapturedScreens(
                new[] { "mission_shell", "gacha_screen" },
                codeLayoutPassCount: 2,
                codeLayoutFailCount: 0,
                fidelityResults: results);

            var json = LaunchScreenScreenshotQaReportJsonAdapter.ToJson(report);

            Assert.That(report.ScreenshotPassCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(0));
            Assert.That(report.FailCount, Is.EqualTo(1));
            Assert.That(report.IsReadyForDeviceScreenshotQa, Is.EqualTo(false));
            Assert.That(report.FidelityReasons, Does.Contain("gacha_screen:uniform_or_blank_frame"));
            Assert.That(json.Contains("\"screenshotFailCount\":1"), Is.EqualTo(true));
            Assert.That(json.Contains("\"fidelityReasons\":[\"gacha_screen:uniform_or_blank_frame\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("mission_shell.png"), Is.EqualTo(false));
            Assert.That(json.Contains("gacha_screen.png"), Is.EqualTo(false));
        }

        [Test]
        public void RejectsNullOrIncompleteInputs()
        {
            Assert.Throws<ArgumentNullException>(() => LaunchScreenScreenshotQaReport.ForUnavailableCapture(null, 7, 0));
            Assert.Throws<ArgumentException>(() => LaunchScreenScreenshotQaReport.ForUnavailableCapture(new[] { "mission_shell", " " }, 1, 0));
            Assert.Throws<ArgumentException>(() => LaunchScreenScreenshotQaReport.ForCapturedScreens(SevenScreenKeys, 7, 0, new[] { "one.png" }));
            Assert.Throws<ArgumentNullException>(() => LaunchScreenScreenshotQaReportJsonAdapter.ToJson(null));
        }

        private static Color32[] CreateFilledPixels(int width, int height, Color32 color)
        {
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            return pixels;
        }

        private static Color32[] CreateDiversePixels(int width, int height)
        {
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var railStripe = (x / 96 + y / 160) % 2 == 0;
                    var r = (byte)(railStripe ? 32 + (x % 90) : 160 + (y % 70));
                    var g = (byte)(railStripe ? 52 + (y % 80) : 92 + (x % 80));
                    var b = (byte)(railStripe ? 72 + ((x + y) % 100) : 34 + (y % 110));
                    pixels[y * width + x] = new Color32(r, g, b, 255);
                }
            }

            return pixels;
        }
    }
}
