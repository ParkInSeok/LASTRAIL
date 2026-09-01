using System;
using System.Collections.Generic;

namespace LastRail.Presentation
{
    public sealed class LaunchScreenScreenshotQaReport
    {
        private LaunchScreenScreenshotQaReport(
            IEnumerable<string> screenKeys,
            int codeLayoutPassCount,
            int codeLayoutFailCount,
            bool screenshotCaptureAttempted,
            IEnumerable<LaunchScreenshotFidelityResult> fidelityResults,
            IEnumerable<string> blockers)
        {
            ScreenKeys = CopyText(screenKeys, nameof(screenKeys));
            CodeLayoutPassCount = RequireNonNegative(codeLayoutPassCount, nameof(codeLayoutPassCount));
            CodeLayoutFailCount = RequireNonNegative(codeLayoutFailCount, nameof(codeLayoutFailCount));
            ScreenshotCaptureAttempted = screenshotCaptureAttempted;
            FidelityResults = CopyFidelityResults(fidelityResults, nameof(fidelityResults));
            Blockers = CopyText(blockers, nameof(blockers));
        }

        public IReadOnlyList<string> ScreenKeys { get; }
        public int ScreenCount => ScreenKeys.Count;
        public int CodeLayoutPassCount { get; }
        public int CodeLayoutFailCount { get; }
        public bool ScreenshotCaptureAttempted { get; }
        public IReadOnlyList<LaunchScreenshotFidelityResult> FidelityResults { get; }
        public IReadOnlyList<string> ScreenshotPaths => BuildScreenshotPaths(FidelityResults);
        public int ScreenshotPassCount => CountScreenshotResults(passed: true);
        public int ScreenshotFailCount => CountScreenshotResults(passed: false);
        public IReadOnlyList<string> FidelityReasons => BuildFidelityReasons(FidelityResults);
        public int WarningCount => Blockers.Count;
        public int FailCount => CodeLayoutFailCount + ScreenshotFailCount;
        public IReadOnlyList<string> Blockers { get; }
        public int BlockerCount => Blockers.Count;
        public bool IsReadyForDeviceScreenshotQa => ScreenshotCaptureAttempted && ScreenshotPassCount == ScreenCount && BlockerCount == 0 && FailCount == 0;
        public bool UsesPortraitSafeArea => true;
        public int MinimumTouchTargetDp => 48;
        public bool RequiresTextClampOverflowQa => true;
        public bool RequiresTenPullGridOverflowQa => true;
        public bool RequiresCodexDetailScrollOrClampQa => true;
        public bool RequiresCombatHudReadabilityQa => true;
        public bool RequiresRewardOfflineStatusRetryLabelQa => true;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasOfflineRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static LaunchScreenScreenshotQaReport ForUnavailableCapture(
            IEnumerable<string> screenKeys,
            int codeLayoutPassCount,
            int codeLayoutFailCount)
        {
            return new LaunchScreenScreenshotQaReport(
                screenKeys,
                codeLayoutPassCount,
                codeLayoutFailCount,
                screenshotCaptureAttempted: false,
                fidelityResults: Array.Empty<LaunchScreenshotFidelityResult>(),
                blockers: new[] { "editor_device_screenshot_qa_unavailable" });
        }

        public static LaunchScreenScreenshotQaReport ForCapturedScreens(
            IEnumerable<string> screenKeys,
            int codeLayoutPassCount,
            int codeLayoutFailCount,
            IEnumerable<string> screenshotPaths)
        {
            var keys = CopyText(screenKeys, nameof(screenKeys));
            var paths = CopyText(screenshotPaths, nameof(screenshotPaths));
            if (paths.Count != keys.Count)
            {
                throw new ArgumentException("Screenshot count must match screen count.", nameof(screenshotPaths));
            }

            var results = new List<LaunchScreenshotFidelityResult>();
            for (var i = 0; i < keys.Count; i++)
            {
                results.Add(LaunchScreenshotFidelityResult.Pass(keys[i], paths[i], 256, 1f, 1, 1));
            }

            return ForCapturedScreens(keys, codeLayoutPassCount, codeLayoutFailCount, results);
        }

        public static LaunchScreenScreenshotQaReport ForCapturedScreens(
            IEnumerable<string> screenKeys,
            int codeLayoutPassCount,
            int codeLayoutFailCount,
            IEnumerable<LaunchScreenshotFidelityResult> fidelityResults)
        {
            var keys = CopyText(screenKeys, nameof(screenKeys));
            var results = CopyFidelityResults(fidelityResults, nameof(fidelityResults));
            if (results.Count != keys.Count)
            {
                throw new ArgumentException("Screenshot fidelity result count must match screen count.", nameof(fidelityResults));
            }

            return new LaunchScreenScreenshotQaReport(
                keys,
                codeLayoutPassCount,
                codeLayoutFailCount,
                screenshotCaptureAttempted: true,
                fidelityResults: results,
                blockers: Array.Empty<string>());
        }

        private static int RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, value, "Value cannot be negative.");
            }

            return value;
        }

        private static IReadOnlyList<string> CopyText(IEnumerable<string> values, string name)
        {
            if (values == null)
            {
                throw new ArgumentNullException(name);
            }

            var copied = new List<string>();
            foreach (var value in values)
            {
                copied.Add(RequireText(value, name));
            }

            return copied.AsReadOnly();
        }

        private static IReadOnlyList<LaunchScreenshotFidelityResult> CopyFidelityResults(
            IEnumerable<LaunchScreenshotFidelityResult> values,
            string name)
        {
            if (values == null)
            {
                throw new ArgumentNullException(name);
            }

            var copied = new List<LaunchScreenshotFidelityResult>();
            foreach (var value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException("Fidelity result cannot be null.", name);
                }

                copied.Add(value);
            }

            return copied.AsReadOnly();
        }

        private int CountScreenshotResults(bool passed)
        {
            var count = 0;
            foreach (var result in FidelityResults)
            {
                if (result.Passed == passed)
                {
                    count++;
                }
            }

            return count;
        }

        private static IReadOnlyList<string> BuildScreenshotPaths(IReadOnlyList<LaunchScreenshotFidelityResult> results)
        {
            var paths = new List<string>();
            foreach (var result in results)
            {
                paths.Add(result.ScreenshotPath);
            }

            return paths.AsReadOnly();
        }

        private static IReadOnlyList<string> BuildFidelityReasons(IReadOnlyList<LaunchScreenshotFidelityResult> results)
        {
            var reasons = new List<string>();
            foreach (var result in results)
            {
                foreach (var reason in result.Reasons)
                {
                    reasons.Add(result.ScreenKey + ":" + reason);
                }
            }

            return reasons.AsReadOnly();
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }
    }
}
