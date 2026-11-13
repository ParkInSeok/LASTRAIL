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
            bool resourcePlacementFlowReady,
            IEnumerable<LaunchEditorFlowCombatSurfaceEvidence> actualFlowVisibleOperatorIdsBySurface,
            IEnumerable<LaunchEditorFlowCombatInteriorEvidence> combatInteriorBackgroundBySurface,
            bool manifestOnlyAnimationCoverageAllowed,
            bool combatSegmentFrameHashesDiffer,
            bool combatInteriorBackgroundsDiffer,
            int retainedMp4Count,
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
            ResourcePlacementFlowReady = resourcePlacementFlowReady;
            ActualFlowVisibleOperatorIdsBySurface = CopyCombatEvidence(actualFlowVisibleOperatorIdsBySurface);
            CombatInteriorBackgroundBySurface = CopyInteriorEvidence(combatInteriorBackgroundBySurface);
            OperatorAnimationResourceCoverageCount = 15;
            OperatorAnimationActualFlowCoverageCount = CountUniqueVisibleOperators(ActualFlowVisibleOperatorIdsBySurface);
            InitialOperatorActualFlowCoverageCount = CountVisibleOperators(
                ActualFlowVisibleOperatorIdsBySurface,
                new[] { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" });
            EliteOperatorActualFlowCoverageCount = CountVisibleOperators(
                ActualFlowVisibleOperatorIdsBySurface,
                new[]
                {
                    "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris",
                    "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden"
                });
            ManifestOnlyAnimationCoverageAllowed = manifestOnlyAnimationCoverageAllowed;
            CombatSegmentFrameHashesDiffer = combatSegmentFrameHashesDiffer;
            CombatInteriorBackgroundsDiffer = combatInteriorBackgroundsDiffer;
            RetainedMp4Count = retainedMp4Count;
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
        public bool ResourcePlacementFlowReady { get; }
        public IReadOnlyList<LaunchEditorFlowCombatSurfaceEvidence> ActualFlowVisibleOperatorIdsBySurface { get; }
        public IReadOnlyList<LaunchEditorFlowCombatInteriorEvidence> CombatInteriorBackgroundBySurface { get; }
        public int OperatorAnimationResourceCoverageCount { get; }
        public int OperatorAnimationActualFlowCoverageCount { get; }
        public int EliteOperatorActualFlowCoverageCount { get; }
        public int InitialOperatorActualFlowCoverageCount { get; }
        public bool ManifestOnlyAnimationCoverageAllowed { get; }
        public bool CombatSegmentFrameHashesDiffer { get; }
        public bool CombatInteriorBackgroundsDiffer { get; }
        public int RetainedMp4Count { get; }
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
            bool hasHighQualityArtResourceCoverage = false,
            bool resourcePlacementFlowReady = false,
            IEnumerable<LaunchEditorFlowCombatSurfaceEvidence> actualFlowVisibleOperatorIdsBySurface = null,
            bool combatSegmentFrameHashesDiffer = true,
            IEnumerable<LaunchEditorFlowCombatInteriorEvidence> combatInteriorBackgroundBySurface = null,
            bool combatInteriorBackgroundsDiffer = true,
            int retainedMp4Count = 0)
        {
            var resolvedEvidence = CopyCombatEvidence(actualFlowVisibleOperatorIdsBySurface);
            var resolvedInteriorEvidence = CopyInteriorEvidence(combatInteriorBackgroundBySurface);
            var resolvedBlockers = BuildValidatedBlockers(
                blockers,
                resolvedEvidence,
                combatSegmentFrameHashesDiffer,
                resolvedInteriorEvidence,
                combatInteriorBackgroundsDiffer);
            failCount += resolvedBlockers.Count - CountTextList(blockers);
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
                resourcePlacementFlowReady: resourcePlacementFlowReady,
                actualFlowVisibleOperatorIdsBySurface: resolvedEvidence,
                combatInteriorBackgroundBySurface: resolvedInteriorEvidence,
                manifestOnlyAnimationCoverageAllowed: false,
                combatSegmentFrameHashesDiffer: combatSegmentFrameHashesDiffer,
                combatInteriorBackgroundsDiffer: combatInteriorBackgroundsDiffer,
                retainedMp4Count: retainedMp4Count,
                checkedSurfaces: checkedSurfaces,
                warningCount: warningCount,
                failCount: failCount,
                blockers: resolvedBlockers);
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
            bool hasHighQualityArtResourceCoverage = false,
            bool resourcePlacementFlowReady = false,
            IEnumerable<LaunchEditorFlowCombatSurfaceEvidence> actualFlowVisibleOperatorIdsBySurface = null,
            bool combatSegmentFrameHashesDiffer = true,
            IEnumerable<LaunchEditorFlowCombatInteriorEvidence> combatInteriorBackgroundBySurface = null,
            bool combatInteriorBackgroundsDiffer = true,
            int retainedMp4Count = 0)
        {
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            if (mp4SizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mp4SizeBytes));
            }

            var resolvedEvidence = CopyCombatEvidence(actualFlowVisibleOperatorIdsBySurface);
            var resolvedInteriorEvidence = CopyInteriorEvidence(combatInteriorBackgroundBySurface);
            var resolvedBlockers = BuildValidatedBlockers(
                blockers,
                resolvedEvidence,
                combatSegmentFrameHashesDiffer,
                resolvedInteriorEvidence,
                combatInteriorBackgroundsDiffer);
            failCount += resolvedBlockers.Count - CountTextList(blockers);
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
                resourcePlacementFlowReady: resourcePlacementFlowReady,
                actualFlowVisibleOperatorIdsBySurface: resolvedEvidence,
                combatInteriorBackgroundBySurface: resolvedInteriorEvidence,
                manifestOnlyAnimationCoverageAllowed: false,
                combatSegmentFrameHashesDiffer: combatSegmentFrameHashesDiffer,
                combatInteriorBackgroundsDiffer: combatInteriorBackgroundsDiffer,
                retainedMp4Count: retainedMp4Count,
                checkedSurfaces: checkedSurfaces,
                warningCount: warningCount,
                failCount: failCount,
                blockers: resolvedBlockers);
        }

        private static IReadOnlyList<LaunchEditorFlowCombatSurfaceEvidence> CopyCombatEvidence(
            IEnumerable<LaunchEditorFlowCombatSurfaceEvidence> values)
        {
            var copy = new List<LaunchEditorFlowCombatSurfaceEvidence>();
            if (values == null)
            {
                return copy.AsReadOnly();
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException("Combat surface evidence cannot contain null.", nameof(values));
                }

                copy.Add(value);
            }

            return copy.AsReadOnly();
        }

        private static IReadOnlyList<LaunchEditorFlowCombatInteriorEvidence> CopyInteriorEvidence(
            IEnumerable<LaunchEditorFlowCombatInteriorEvidence> values)
        {
            var copy = new List<LaunchEditorFlowCombatInteriorEvidence>();
            if (values == null)
            {
                return copy.AsReadOnly();
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException("Combat interior evidence cannot contain null.", nameof(values));
                }

                copy.Add(value);
            }

            return copy.AsReadOnly();
        }

        private static IReadOnlyList<string> BuildValidatedBlockers(
            IEnumerable<string> blockers,
            IReadOnlyList<LaunchEditorFlowCombatSurfaceEvidence> evidence,
            bool combatSegmentFrameHashesDiffer,
            IReadOnlyList<LaunchEditorFlowCombatInteriorEvidence> interiorEvidence,
            bool combatInteriorBackgroundsDiffer)
        {
            var copy = new List<string>(CopyTextList(blockers, nameof(blockers)));
            var hasMismatch = false;
            foreach (var item in evidence)
            {
                if (!item.ExpectedIdsMatchActualIds)
                {
                    hasMismatch = true;
                    break;
                }
            }

            if (hasMismatch && !copy.Contains("combat_actual_flow_operator_ids_mismatch"))
            {
                copy.Add("combat_actual_flow_operator_ids_mismatch");
            }

            if (!combatSegmentFrameHashesDiffer && !copy.Contains("combat_segment_frame_hashes_identical"))
            {
                copy.Add("combat_segment_frame_hashes_identical");
            }

            if (evidence.Count > 0 && CountUniqueVisibleOperators(evidence) != 15 && !copy.Contains("combat_actual_flow_operator_coverage_incomplete"))
            {
                copy.Add("combat_actual_flow_operator_coverage_incomplete");
            }

            if (!combatInteriorBackgroundsDiffer && !copy.Contains("combat_interior_backgrounds_not_unique"))
            {
                copy.Add("combat_interior_backgrounds_not_unique");
            }

            if (interiorEvidence.Count > 0 && !HasRequiredCombatInteriorEvidence(interiorEvidence) && !copy.Contains("combat_interior_background_evidence_incomplete"))
            {
                copy.Add("combat_interior_background_evidence_incomplete");
            }

            return copy.AsReadOnly();
        }

        private static bool HasRequiredCombatInteriorEvidence(IEnumerable<LaunchEditorFlowCombatInteriorEvidence> evidence)
        {
            var required = new HashSet<string>(StringComparer.Ordinal)
            {
                "combat_ordinary_wave_demo",
                "combat_elite_squad_a_demo",
                "combat_elite_squad_b_demo",
                "combat_story_raid_boss_demo"
            };

            foreach (var item in evidence)
            {
                required.Remove(item.ScreenKey);
            }

            return required.Count == 0;
        }

        private static int CountTextList(IEnumerable<string> values)
        {
            var count = 0;
            foreach (var unused in CopyTextList(values, nameof(values)))
            {
                count++;
            }

            return count;
        }

        private static int CountUniqueVisibleOperators(IEnumerable<LaunchEditorFlowCombatSurfaceEvidence> evidence)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var surface in evidence)
            {
                foreach (var id in surface.ActualVisibleOperatorIds)
                {
                    ids.Add(id);
                }
            }

            return ids.Count;
        }

        private static int CountVisibleOperators(IEnumerable<LaunchEditorFlowCombatSurfaceEvidence> evidence, IEnumerable<string> expected)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var surface in evidence)
            {
                foreach (var id in surface.ActualVisibleOperatorIds)
                {
                    ids.Add(id);
                }
            }

            var count = 0;
            foreach (var id in expected)
            {
                if (ids.Contains(id))
                {
                    count++;
                }
            }

            return count;
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
            b.Append("\"resourcePlacementFlowReady\":").Append(Bool(report.ResourcePlacementFlowReady)).Append(',');
            b.Append("\"actualFlowVisibleOperatorIdsBySurface\":{");
            for (var i = 0; i < report.ActualFlowVisibleOperatorIdsBySurface.Count; i++)
            {
                if (i > 0)
                {
                    b.Append(',');
                }

                var evidence = report.ActualFlowVisibleOperatorIdsBySurface[i];
                StringValue(b, evidence.ScreenKey);
                b.Append(":{");
                StringArrayField(b, "expectedOperatorIds", evidence.ExpectedOperatorIds).Append(',');
                StringArrayField(b, "actualVisibleOperatorIds", evidence.ActualVisibleOperatorIds).Append(',');
                OptionalField(b, "frameSha256", evidence.FrameSha256).Append(',');
                b.Append("\"expectedIdsMatchActualIds\":").Append(Bool(evidence.ExpectedIdsMatchActualIds));
                b.Append('}');
            }

            b.Append("},");
            b.Append("\"combatInteriorBackgroundBySurface\":{");
            for (var i = 0; i < report.CombatInteriorBackgroundBySurface.Count; i++)
            {
                if (i > 0)
                {
                    b.Append(',');
                }

                var evidence = report.CombatInteriorBackgroundBySurface[i];
                StringValue(b, evidence.ScreenKey);
                b.Append(":{");
                Field(b, "backgroundId", evidence.BackgroundId).Append(',');
                Field(b, "backgroundPath", evidence.BackgroundPath);
                b.Append('}');
            }

            b.Append("},");
            b.Append("\"operatorAnimationResourceCoverageCount\":").Append(report.OperatorAnimationResourceCoverageCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"operatorAnimationActualFlowCoverageCount\":").Append(report.OperatorAnimationActualFlowCoverageCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"eliteOperatorActualFlowCoverageCount\":").Append(report.EliteOperatorActualFlowCoverageCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"initialOperatorActualFlowCoverageCount\":").Append(report.InitialOperatorActualFlowCoverageCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            b.Append("\"manifestOnlyAnimationCoverageAllowed\":").Append(Bool(report.ManifestOnlyAnimationCoverageAllowed)).Append(',');
            b.Append("\"combatSegmentFrameHashesDiffer\":").Append(Bool(report.CombatSegmentFrameHashesDiffer)).Append(',');
            b.Append("\"combatInteriorBackgroundsDiffer\":").Append(Bool(report.CombatInteriorBackgroundsDiffer)).Append(',');
            b.Append("\"retainedMp4Count\":").Append(report.RetainedMp4Count.ToString(CultureInfo.InvariantCulture)).Append(',');
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

        private static StringBuilder StringArrayField(StringBuilder b, string name, IReadOnlyList<string> values)
        {
            StringValue(b, name);
            b.Append(':');
            b.Append('[');
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    b.Append(',');
                }

                StringValue(b, values[i]);
            }

            b.Append(']');
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

    public sealed class LaunchEditorFlowCombatSurfaceEvidence
    {
        private LaunchEditorFlowCombatSurfaceEvidence(
            string screenKey,
            IEnumerable<string> expectedOperatorIds,
            IEnumerable<string> actualVisibleOperatorIds,
            string frameSha256)
        {
            ScreenKey = RequireText(screenKey, nameof(screenKey));
            ExpectedOperatorIds = CopyTextList(expectedOperatorIds, nameof(expectedOperatorIds));
            ActualVisibleOperatorIds = CopyTextList(actualVisibleOperatorIds, nameof(actualVisibleOperatorIds));
            FrameSha256 = OptionalText(frameSha256);
            ExpectedIdsMatchActualIds = ListsEqual(ExpectedOperatorIds, ActualVisibleOperatorIds);
        }

        public string ScreenKey { get; }
        public IReadOnlyList<string> ExpectedOperatorIds { get; }
        public IReadOnlyList<string> ActualVisibleOperatorIds { get; }
        public string FrameSha256 { get; }
        public bool ExpectedIdsMatchActualIds { get; }

        public static LaunchEditorFlowCombatSurfaceEvidence ForSurface(
            string screenKey,
            IEnumerable<string> expectedOperatorIds,
            IEnumerable<string> actualVisibleOperatorIds,
            string frameSha256)
        {
            return new LaunchEditorFlowCombatSurfaceEvidence(
                screenKey,
                expectedOperatorIds,
                actualVisibleOperatorIds,
                frameSha256);
        }

        private static bool ListsEqual(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
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

    public sealed class LaunchEditorFlowCombatInteriorEvidence
    {
        private LaunchEditorFlowCombatInteriorEvidence(
            string screenKey,
            string backgroundId,
            string backgroundPath)
        {
            ScreenKey = RequireText(screenKey, nameof(screenKey));
            BackgroundId = RequireText(backgroundId, nameof(backgroundId));
            BackgroundPath = RequireText(backgroundPath, nameof(backgroundPath));
        }

        public string ScreenKey { get; }
        public string BackgroundId { get; }
        public string BackgroundPath { get; }

        public static LaunchEditorFlowCombatInteriorEvidence ForSurface(
            string screenKey,
            string backgroundId,
            string backgroundPath)
        {
            return new LaunchEditorFlowCombatInteriorEvidence(screenKey, backgroundId, backgroundPath);
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
