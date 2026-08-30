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
            IEnumerable<string> screenshotPaths,
            IEnumerable<string> blockers)
        {
            ScreenKeys = CopyText(screenKeys, nameof(screenKeys));
            CodeLayoutPassCount = RequireNonNegative(codeLayoutPassCount, nameof(codeLayoutPassCount));
            CodeLayoutFailCount = RequireNonNegative(codeLayoutFailCount, nameof(codeLayoutFailCount));
            ScreenshotCaptureAttempted = screenshotCaptureAttempted;
            ScreenshotPaths = CopyText(screenshotPaths, nameof(screenshotPaths));
            Blockers = CopyText(blockers, nameof(blockers));
        }

        public IReadOnlyList<string> ScreenKeys { get; }
        public int ScreenCount => ScreenKeys.Count;
        public int CodeLayoutPassCount { get; }
        public int CodeLayoutFailCount { get; }
        public bool ScreenshotCaptureAttempted { get; }
        public IReadOnlyList<string> ScreenshotPaths { get; }
        public int ScreenshotPassCount => ScreenshotPaths.Count;
        public int WarningCount => Blockers.Count;
        public int FailCount => CodeLayoutFailCount;
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
                screenshotPaths: Array.Empty<string>(),
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

            return new LaunchScreenScreenshotQaReport(
                keys,
                codeLayoutPassCount,
                codeLayoutFailCount,
                screenshotCaptureAttempted: true,
                screenshotPaths: paths,
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
