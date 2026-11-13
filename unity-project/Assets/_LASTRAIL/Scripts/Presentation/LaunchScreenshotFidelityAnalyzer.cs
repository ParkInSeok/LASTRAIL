using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastRail.Presentation
{
    public static class LaunchScreenshotFidelityAnalyzer
    {
        private const int MinimumPixelDiversityScore = 8;
        private const float MinimumNonBackgroundPixelRatio = 0.015f;
        private const int MinimumVisibleTextCount = 1;
        private const int MinimumVisibleGraphicCount = 1;

        public static LaunchScreenshotFidelityResult Analyze(
            string screenKey,
            int width,
            int height,
            Color32[] pixels,
            int expectedTextCount,
            int visibleGraphicCount,
            int missingAssetReferenceCount)
        {
            return Analyze(
                screenKey,
                screenKey + ".png",
                width,
                height,
                pixels,
                expectedTextCount,
                visibleGraphicCount,
                missingAssetReferenceCount);
        }

        public static LaunchScreenshotFidelityResult Analyze(
            string screenKey,
            string screenshotPath,
            int width,
            int height,
            Color32[] pixels,
            int expectedTextCount,
            int visibleGraphicCount,
            int missingAssetReferenceCount)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            }

            if (pixels == null)
            {
                throw new ArgumentNullException(nameof(pixels));
            }

            if (pixels.Length != width * height)
            {
                throw new ArgumentException("Pixel count must match width * height.", nameof(pixels));
            }

            var reasons = new List<string>();
            var pixelDiversityScore = CountColorBuckets(pixels);
            var nonBackgroundPixelRatio = CalculateNonBackgroundRatio(pixels);
            var hasEnoughText = expectedTextCount >= MinimumVisibleTextCount;
            var hasEnoughGraphics = visibleGraphicCount >= MinimumVisibleGraphicCount;

            if (pixelDiversityScore <= 1 || nonBackgroundPixelRatio <= 0.0001f)
            {
                reasons.Add("uniform_or_blank_frame");
            }

            if (pixelDiversityScore < MinimumPixelDiversityScore)
            {
                reasons.Add("low_pixel_diversity");
            }

            if (nonBackgroundPixelRatio < MinimumNonBackgroundPixelRatio)
            {
                reasons.Add("low_non_background_ratio");
            }

            if (!hasEnoughText)
            {
                reasons.Add("missing_visible_tmp_text");
            }

            if (!hasEnoughGraphics)
            {
                reasons.Add("missing_visible_graphics");
            }

            if (missingAssetReferenceCount > 0)
            {
                reasons.Add("missing_asset_references");
            }

            return reasons.Count == 0
                ? LaunchScreenshotFidelityResult.Pass(
                    screenKey,
                    screenshotPath,
                    pixelDiversityScore,
                    nonBackgroundPixelRatio,
                    expectedTextCount,
                    visibleGraphicCount)
                : LaunchScreenshotFidelityResult.Fail(
                    screenKey,
                    screenshotPath,
                    pixelDiversityScore,
                    nonBackgroundPixelRatio,
                    expectedTextCount,
                    visibleGraphicCount,
                    reasons);
        }

        private static int CountColorBuckets(Color32[] pixels)
        {
            var buckets = new HashSet<int>();
            var stride = Math.Max(1, pixels.Length / 120000);
            for (var i = 0; i < pixels.Length; i += stride)
            {
                var pixel = pixels[i];
                if (pixel.a < 8)
                {
                    continue;
                }

                var key = (pixel.r >> 4) << 8 | (pixel.g >> 4) << 4 | (pixel.b >> 4);
                buckets.Add(key);
                if (buckets.Count >= 256)
                {
                    break;
                }
            }

            return buckets.Count;
        }

        private static float CalculateNonBackgroundRatio(Color32[] pixels)
        {
            var baseline = pixels.Length == 0 ? new Color32(0, 0, 0, 255) : pixels[0];
            var stride = Math.Max(1, pixels.Length / 120000);
            var sampled = 0;
            var different = 0;

            for (var i = 0; i < pixels.Length; i += stride)
            {
                var pixel = pixels[i];
                if (pixel.a < 8)
                {
                    continue;
                }

                sampled++;
                var diff = Math.Abs(pixel.r - baseline.r) + Math.Abs(pixel.g - baseline.g) + Math.Abs(pixel.b - baseline.b);
                if (diff > 24)
                {
                    different++;
                }
            }

            return sampled == 0 ? 0f : (float)different / sampled;
        }
    }
}
