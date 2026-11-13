using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Presentation
{
    public sealed class UnityEditorEnvironmentDiscoveryReport
    {
        private UnityEditorEnvironmentDiscoveryReport(
            string projectEditorVersion,
            string projectEditorVersionWithRevision,
            string editorExecutablePath,
            bool isUnity2DAnimationPackageDeclared,
            bool hasUnityHubPathHint)
        {
            ProjectEditorVersion = RequireText(projectEditorVersion, nameof(projectEditorVersion));
            ProjectEditorVersionWithRevision = RequireText(projectEditorVersionWithRevision, nameof(projectEditorVersionWithRevision));
            EditorExecutablePath = Optional(editorExecutablePath);
            IsUnity2DAnimationPackageDeclared = isUnity2DAnimationPackageDeclared;
            HasUnityHubPathHint = hasUnityHubPathHint;
            OpenBlockers = BuildOpenBlockers(HasEditorExecutablePath, isUnity2DAnimationPackageDeclared);
        }

        public string ProjectEditorVersion { get; }
        public string ProjectEditorVersionWithRevision { get; }
        public string EditorExecutablePath { get; }
        public bool HasEditorExecutablePath => EditorExecutablePath != null;
        public bool IsUnity2DAnimationPackageDeclared { get; }
        public bool HasUnityHubPathHint { get; }
        public bool CanRunEditorDryRun => HasEditorExecutablePath;
        public string EditorDryRunStatus => CanRunEditorDryRun ? "READY_FOR_BATCHMODE_DRY_RUN" : "NOOP_EDITOR_EXECUTABLE_NOT_DISCOVERED";
        public string RecommendedDryRunCommandKind => CanRunEditorDryRun ? "batchmode_editmode_tests_or_compile_smoke" : null;
        public IReadOnlyList<string> OpenBlockers { get; }
        public int OpenBlockerCount => OpenBlockers.Count;
        public bool RequiresPackageImport => false;
        public bool RequiresGoogleSdkImport => false;
        public bool RequiresSecureStoragePluginImport => false;
        public bool RequiresAndroidDeviceOperation => false;
        public bool HasRawTokenPayload => false;
        public bool HasAccountShortcut => false;
        public bool HasRawDeviceIdentifier => false;
        public bool HasRawAssetPayload => false;
        public bool HasServerTruthCalculation => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static UnityEditorEnvironmentDiscoveryReport FromDiscoveredHints(
            string projectEditorVersion,
            string projectEditorVersionWithRevision,
            string editorExecutablePath,
            bool isUnity2DAnimationPackageDeclared,
            bool hasUnityHubPathHint)
        {
            return new UnityEditorEnvironmentDiscoveryReport(
                projectEditorVersion,
                projectEditorVersionWithRevision,
                editorExecutablePath,
                isUnity2DAnimationPackageDeclared,
                hasUnityHubPathHint);
        }

        private static IReadOnlyList<string> BuildOpenBlockers(bool hasEditorExecutablePath, bool isUnity2DAnimationPackageDeclared)
        {
            var blockers = new List<string>();
            if (!hasEditorExecutablePath)
            {
                blockers.Add("unity_editor_executable_not_discovered");
            }

            if (!isUnity2DAnimationPackageDeclared)
            {
                blockers.Add("com_unity_2d_animation_package_resolve_pending");
            }

            blockers.Add("editor_device_screenshot_qa_unavailable");
            return blockers.AsReadOnly();
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

    public static class UnityEditorEnvironmentDiscoveryReportJsonAdapter
    {
        public static string ToJson(UnityEditorEnvironmentDiscoveryReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "projectEditorVersion", report.ProjectEditorVersion).Append(',');
            AppendString(builder, "projectEditorVersionWithRevision", report.ProjectEditorVersionWithRevision).Append(',');
            AppendBool(builder, "hasEditorExecutablePath", report.HasEditorExecutablePath).Append(',');
            AppendBool(builder, "hasUnityHubPathHint", report.HasUnityHubPathHint).Append(',');
            AppendBool(builder, "isUnity2DAnimationPackageDeclared", report.IsUnity2DAnimationPackageDeclared).Append(',');
            AppendBool(builder, "canRunEditorDryRun", report.CanRunEditorDryRun).Append(',');
            AppendString(builder, "editorDryRunStatus", report.EditorDryRunStatus).Append(',');
            AppendString(builder, "recommendedDryRunCommandKind", report.RecommendedDryRunCommandKind).Append(',');
            AppendInt(builder, "openBlockerCount", report.OpenBlockerCount).Append(',');
            AppendStringArray(builder, "openBlockers", report.OpenBlockers).Append(',');
            AppendBool(builder, "requiresPackageImport", report.RequiresPackageImport).Append(',');
            AppendBool(builder, "requiresGoogleSdkImport", report.RequiresGoogleSdkImport).Append(',');
            AppendBool(builder, "requiresSecureStoragePluginImport", report.RequiresSecureStoragePluginImport).Append(',');
            AppendBool(builder, "requiresAndroidDeviceOperation", report.RequiresAndroidDeviceOperation).Append(',');
            AppendBool(builder, "hasRawTokenPayload", report.HasRawTokenPayload).Append(',');
            AppendBool(builder, "hasAccountShortcut", report.HasAccountShortcut).Append(',');
            AppendBool(builder, "hasRawDeviceIdentifier", report.HasRawDeviceIdentifier).Append(',');
            AppendBool(builder, "hasRawAssetPayload", report.HasRawAssetPayload).Append(',');
            AppendBool(builder, "hasServerTruthCalculation", report.HasServerTruthCalculation).Append(',');
            AppendBool(builder, "hasRewardCalculation", report.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", report.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", report.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string fieldName, IReadOnlyList<string> values)
        {
            builder.Append('"').Append(fieldName).Append("\":[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, values[i]);
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendInt(StringBuilder builder, string fieldName, int value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value);
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                return builder.Append("null");
            }

            builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(character))
                        {
                            builder.Append("\\u").Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            return builder.Append('"');
        }
    }
}
