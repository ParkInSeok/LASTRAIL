using System;
using System.Collections.Generic;

namespace LastRail.Presentation
{
    public sealed class LaunchScreenshotFidelityResult
    {
        private LaunchScreenshotFidelityResult(
            string screenKey,
            string screenshotPath,
            bool passed,
            int pixelDiversityScore,
            float nonBackgroundPixelRatio,
            int visibleTextCount,
            int visibleGraphicCount,
            IEnumerable<string> reasons)
        {
            ScreenKey = RequireText(screenKey, nameof(screenKey));
            ScreenshotPath = RequireText(screenshotPath, nameof(screenshotPath));
            Passed = passed;
            PixelDiversityScore = RequireNonNegative(pixelDiversityScore, nameof(pixelDiversityScore));
            NonBackgroundPixelRatio = ClampRatio(nonBackgroundPixelRatio);
            VisibleTextCount = RequireNonNegative(visibleTextCount, nameof(visibleTextCount));
            VisibleGraphicCount = RequireNonNegative(visibleGraphicCount, nameof(visibleGraphicCount));
            Reasons = CopyReasons(reasons);
        }

        public string ScreenKey { get; }
        public string ScreenshotPath { get; }
        public bool Passed { get; }
        public int PixelDiversityScore { get; }
        public float NonBackgroundPixelRatio { get; }
        public int VisibleTextCount { get; }
        public int VisibleGraphicCount { get; }
        public IReadOnlyList<string> Reasons { get; }

        public static LaunchScreenshotFidelityResult Pass(
            string screenKey,
            string screenshotPath,
            int pixelDiversityScore,
            float nonBackgroundPixelRatio,
            int visibleTextCount,
            int visibleGraphicCount)
        {
            return new LaunchScreenshotFidelityResult(
                screenKey,
                screenshotPath,
                passed: true,
                pixelDiversityScore,
                nonBackgroundPixelRatio,
                visibleTextCount,
                visibleGraphicCount,
                Array.Empty<string>());
        }

        public static LaunchScreenshotFidelityResult Fail(
            string screenKey,
            string screenshotPath,
            int pixelDiversityScore,
            float nonBackgroundPixelRatio,
            int visibleTextCount,
            int visibleGraphicCount,
            IEnumerable<string> reasons)
        {
            return new LaunchScreenshotFidelityResult(
                screenKey,
                screenshotPath,
                passed: false,
                pixelDiversityScore,
                nonBackgroundPixelRatio,
                visibleTextCount,
                visibleGraphicCount,
                reasons);
        }

        private static IReadOnlyList<string> CopyReasons(IEnumerable<string> reasons)
        {
            if (reasons == null)
            {
                throw new ArgumentNullException(nameof(reasons));
            }

            var copied = new List<string>();
            foreach (var reason in reasons)
            {
                copied.Add(RequireText(reason, nameof(reasons)));
            }

            return copied.AsReadOnly();
        }

        private static int RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, value, "Value cannot be negative.");
            }

            return value;
        }

        private static float ClampRatio(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
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
