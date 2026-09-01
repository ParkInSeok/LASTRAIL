using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation
{
    public sealed class LaunchEditorFlowVideoQaReport
    {
        private const int DefaultWidth = 1080;
        private const int DefaultHeight = 1920;

        private LaunchEditorFlowVideoQaReport(
            string status,
            string outputRoot,
            string videoFormat,
            string captureMode,
            bool hasMp4Artifact,
            bool hasRecorderPackage,
            string mp4Path,
            string mp4Encoder,
            long mp4SizeBytes,
            int width,
            int height,
            int frameCount,
            int estimatedDurationSeconds,
            int encodedFps,
            bool usesFallbackComposite,
            int fallbackCompositeFrameCount,
            bool hasHighQualityArtResourceCoverage,
            IEnumerable<string> checkedSurfaces,
            int warningCount,
            int failCount,
            IEnumerable<string> blockers)
        {
            Status = RequireText(status, nameof(status));
            OutputRoot = RequireText(outputRoot, nameof(outputRoot));
            VideoFormat = RequireText(videoFormat, nameof(videoFormat));
            CaptureMode = RequireText(captureMode, nameof(captureMode));
            HasMp4Artifact = hasMp4Artifact;
            HasRecorderPackage = hasRecorderPackage;
            Mp4Path = OptionalText(mp4Path);
            Mp4Encoder = OptionalText(mp4Encoder);
            Mp4SizeBytes = mp4SizeBytes;
            Width = width;
            Height = height;
            FrameCount = frameCount;
            EstimatedDurationSeconds = estimatedDurationSeconds;
            EncodedFps = encodedFps;
            UsesFallbackComposite = usesFallbackComposite;
            FallbackCompositeFrameCount = fallbackCompositeFrameCount;
            HasHighQualityArtResourceCoverage = hasHighQualityArtResourceCoverage;
            CheckedSurfaces = CopyTextList(checkedSurfaces, nameof(checkedSurfaces));
            CheckedSurfaceCount = CheckedSurfaces.Count;
            WarningCount = warningCount;
            FailCount = failCount;
            Blockers = CopyTextList(blockers, nameof(blockers));
        }

        public string Status { get; }
        public string OutputRoot { get; }
        public string VideoFormat { get; }
        public string CaptureMode { get; }
        public bool HasMp4Artifact { get; }
        public bool HasRecorderPackage { get; }
        public string Mp4Path { get; }
        public string Mp4Encoder { get; }
        public long Mp4SizeBytes { get; }
        public int Width { get; }
        public int Height { get; }
        public int FrameCount { get; }
        public int EstimatedDurationSeconds { get; }
        public int EncodedFps { get; }
        public bool UsesFallbackComposite { get; }
        public int FallbackCompositeFrameCount { get; }
        public bool HasHighQualityArtResourceCoverage { get; }
        public IReadOnlyList<string> CheckedSurfaces { get; }
        public int CheckedSurfaceCount { get; }
        public int WarningCount { get; }
        public int FailCount { get; }
        public IReadOnlyList<string> Blockers { get; }
        public bool UsesPortraitSafeArea => true;
        public int MinimumTouchTargetDp => 48;
        public bool RequiresTmpKoreanReadability => true;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasStoryTruthCalculation => false;
        public bool HasOfflineRewardCalculation => false;
        public bool HasClientSideRng => false;

        public static LaunchEditorFlowVideoQaReport ForPngFrameSequence(
            string outputRoot,
            IEnumerable<string> checkedSurfaces,
            int frameCount,
            int warningCount,
            int failCount,
            IEnumerable<string> blockers,
            string captureMode = "png_frame_sequence",
            bool usesFallbackComposite = false,
            int fallbackCompositeFrameCount = 0,
            bool hasHighQualityArtResourceCoverage = false)
        {
            var status = failCount == 0 ? "PASS" : "FAIL";
            return new LaunchEditorFlowVideoQaReport(
                status,
                outputRoot,
                "png_frame_sequence",
                captureMode,
                hasMp4Artifact: false,
                hasRecorderPackage: false,
                mp4Path: null,
                mp4Encoder: null,
                mp4SizeBytes: 0,
                width: DefaultWidth,
                height: DefaultHeight,
                frameCount: frameCount,
                estimatedDurationSeconds: Math.Max(60, frameCount * 5),
                encodedFps: 0,
                usesFallbackComposite: usesFallbackComposite,
                fallbackCompositeFrameCount: fallbackCompositeFrameCount,
                hasHighQualityArtResourceCoverage: hasHighQualityArtResourceCoverage,
                checkedSurfaces: checkedSurfaces,
                warningCount: warningCount,
                failCount: failCount,
                blockers: blockers);
        }

        public static LaunchEditorFlowVideoQaReport ForMp4Artifact(
            string outputRoot,
            string mp4Path,
            string mp4Encoder,
            IEnumerable<string> checkedSurfaces,
            int frameCount,
            int durationSeconds,
            long mp4SizeBytes,
            bool hasRecorderPackage,
            int warningCount,
            int failCount,
            IEnumerable<string> blockers,
            string captureMode = "frame_sequence_encode",
            bool usesFallbackComposite = false,
            int fallbackCompositeFrameCount = 0,
            int encodedFps = 1,
            bool hasHighQualityArtResourceCoverage = false)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            if (mp4SizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mp4SizeBytes));
            }

            var status = failCount == 0 ? "PASS" : "FAIL";
            return new LaunchEditorFlowVideoQaReport(
                status,
                outputRoot,
                "mp4",
                captureMode,
                hasMp4Artifact: true,
                hasRecorderPackage: hasRecorderPackage,
                mp4Path: mp4Path,
                mp4Encoder: mp4Encoder,
                mp4SizeBytes: mp4SizeBytes,
                width: DefaultWidth,
                height: DefaultHeight,
                frameCount: frameCount,
                estimatedDurationSeconds: durationSeconds,
                encodedFps: encodedFps,
                usesFallbackComposite: usesFallbackComposite,
                fallbackCompositeFrameCount: fallbackCompositeFrameCount,
                hasHighQualityArtResourceCoverage: hasHighQualityArtResourceCoverage,
                checkedSurfaces: checkedSurfaces,
                warningCount: warningCount,
                failCount: failCount,
                blockers: blockers);
        }

        private static IReadOnlyList<string> CopyTextList(IEnumerable<string> values, string name)
        {
            if (values == null)
            {
                throw new ArgumentNullException(name);
            }

            var copy = new List<string>();
            foreach (var value in values)
            {
                copy.Add(RequireText(value, name));
            }

            return copy.AsReadOnly();
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public static class LaunchEditorFlowVideoQaReportJsonAdapter
    {
        public static string ToJson(LaunchEditorFlowVideoQaReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var b = new StringBuilder();
            b.Append('{');
            Field(b, "status", report.Status).Append(',');
            Field(b, "outputRoot", report.OutputRoot).Append(',');
            Field(b, "videoFormat", report.VideoFormat).Append(',');
            Field(b, "captureMode", report.CaptureMode).Append(',');
            b.Append("\"hasMp4Artifact\":").Append(Bool(report.HasMp4Artifact)).Append(',');
            b.Append("\"hasRecorderPackage\":").Append(Bool(report.HasRecorderPackage)).Append(',');
            OptionalField(b, "mp4Path", report.Mp4Path).Append(',');
            OptionalField(b, "mp4Encoder", report.Mp4Encoder).Append(',');
            b.Append("\"mp4SizeBytes\":").Append(report.Mp4SizeBytes.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"width\":").Append(report.Width.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"height\":").Append(report.Height.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"frameCount\":").Append(report.FrameCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"estimatedDurationSeconds\":").Append(report.EstimatedDurationSeconds.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"encodedFps\":").Append(report.EncodedFps.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"usesFallbackComposite\":").Append(Bool(report.UsesFallbackComposite)).Append(',');
            b.Append("\"fallbackCompositeFrameCount\":").Append(report.FallbackCompositeFrameCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"hasHighQualityArtResourceCoverage\":").Append(Bool(report.HasHighQualityArtResourceCoverage)).Append(',');
            b.Append("\"checkedSurfaceCount\":").Append(report.CheckedSurfaceCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"checkedSurfaces\":[");
            for (var i = 0; i < report.CheckedSurfaces.Count; i++)
            {
                if (i > 0)
                {
                    b.Append(',');
                }

                b.Append('{');
                Field(b, "screenKey", report.CheckedSurfaces[i]);
                b.Append('}');
            }

            b.Append("],");
            b.Append("\"warningCount\":").Append(report.WarningCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"failCount\":").Append(report.FailCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"blockers\":[");
            for (var i = 0; i < report.Blockers.Count; i++)
            {
                if (i > 0)
                {
                    b.Append(',');
                }

                StringValue(b, report.Blockers[i]);
            }

            b.Append("],");
            b.Append("\"usesPortraitSafeArea\":").Append(Bool(report.UsesPortraitSafeArea)).Append(',');
            b.Append("\"minimumTouchTargetDp\":").Append(report.MinimumTouchTargetDp.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"requiresTmpKoreanReadability\":").Append(Bool(report.RequiresTmpKoreanReadability)).Append(',');
            b.Append("\"hasServerTransport\":").Append(Bool(report.HasServerTransport)).Append(',');
            b.Append("\"hasRewardCalculation\":").Append(Bool(report.HasRewardCalculation)).Append(',');
            b.Append("\"hasEconomyMutation\":").Append(Bool(report.HasEconomyMutation)).Append(',');
            b.Append("\"hasStoryTruthCalculation\":").Append(Bool(report.HasStoryTruthCalculation)).Append(',');
            b.Append("\"hasOfflineRewardCalculation\":").Append(Bool(report.HasOfflineRewardCalculation)).Append(',');
            b.Append("\"hasClientSideRng\":").Append(Bool(report.HasClientSideRng));
            b.Append('}');
            return b.ToString();
        }

        private static StringBuilder Field(StringBuilder b, string name, string value)
        {
            StringValue(b, name);
            b.Append(':');
            StringValue(b, value);
            return b;
        }

        private static StringBuilder OptionalField(StringBuilder b, string name, string value)
        {
            StringValue(b, name);
            b.Append(':');
            if (value == null)
            {
                b.Append("null");
            }
            else
            {
                StringValue(b, value);
            }

            return b;
        }

        private static void StringValue(StringBuilder b, string value)
        {
            b.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\':
                        b.Append("\\\\");
                        break;
                    case '"':
                        b.Append("\\\"");
                        break;
                    case '\n':
                        b.Append("\\n");
                        break;
                    case '\r':
                        b.Append("\\r");
                        break;
                    case '\t':
                        b.Append("\\t");
                        break;
                    default:
                        b.Append(c);
                        break;
                }
            }

            b.Append('"');
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
