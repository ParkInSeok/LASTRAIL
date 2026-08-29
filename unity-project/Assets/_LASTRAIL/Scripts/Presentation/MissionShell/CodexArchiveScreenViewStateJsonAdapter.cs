using System;
using System.Globalization;
using System.Text;

namespace LastRail.Presentation.MissionShell
{
    public static class CodexArchiveScreenViewStateJsonAdapter
    {
        public static string ToJson(CodexArchiveScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surface", state.Surface.ToString()).Append(',');
            AppendString(builder, "title", state.Title).Append(',');
            AppendString(builder, "narrativeVersion", state.NarrativeVersion).Append(',');
            AppendInt(builder, "entryCount", state.EntryCount).Append(',');
            AppendInt(builder, "overflowSlotCount", state.OverflowSlotCount).Append(',');
            AppendBool(builder, "requiresOverflowVerification", state.RequiresOverflowVerification).Append(',');
            AppendBool(builder, "isBlockingNavigation", state.IsBlockingNavigation).Append(',');
            AppendBool(builder, "canNavigateToMissionSelect", state.CanNavigateToMissionSelect).Append(',');
            AppendBool(builder, "canNavigateToResult", state.CanNavigateToResult).Append(',');
            AppendBool(builder, "canNavigateToRewardReview", state.CanNavigateToRewardReview).Append(',');
            AppendBool(builder, "usesPortraitSafeArea", state.UsesPortraitSafeArea).Append(',');
            AppendInt(builder, "minimumTouchTargetDp", state.MinimumTouchTargetDp).Append(',');
            AppendString(builder, "preferredAspectLabel", state.PreferredAspectWidth + ":" + state.PreferredAspectHeight).Append(',');
            builder.Append("\"entries\":[");
            for (var i = 0; i < state.Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendEntry(builder, state.Entries[i]);
            }

            builder.Append("],");
            AppendBool(builder, "isDisplayOnly", state.IsDisplayOnly).Append(',');
            AppendBool(builder, "hasServerTransport", state.HasServerTransport).Append(',');
            AppendBool(builder, "hasUnlockTruth", state.HasUnlockTruth).Append(',');
            AppendBool(builder, "hasStoryProgressMutation", state.HasStoryProgressMutation).Append(',');
            AppendBool(builder, "hasRewardCalculation", state.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", state.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", state.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendEntry(StringBuilder builder, CodexArchiveScreenEntryViewState entry)
        {
            builder.Append('{');
            AppendString(builder, "missionId", entry.MissionId).Append(',');
            AppendString(builder, "displayName", entry.DisplayName).Append(',');
            AppendString(builder, "codexStringId", entry.CodexStringId).Append(',');
            AppendBool(builder, "isFallbackText", entry.IsFallbackText).Append(',');
            AppendBool(builder, "isServerListedClearedForDisplay", entry.IsServerListedClearedForDisplay).Append(',');
            AppendBool(builder, "isServerListedUnlockedForDisplay", entry.IsServerListedUnlockedForDisplay).Append(',');
            AppendBool(builder, "canOpenForDisplay", entry.CanOpenForDisplay).Append(',');
            AppendInt(builder, "characterCount", entry.CharacterCount).Append(',');
            AppendInt(builder, "maxCharacterCount", entry.MaxCharacterCount).Append(',');
            AppendBool(builder, "exceedsRecommendedLength", entry.ExceedsRecommendedLength).Append(',');
            AppendString(builder, "readinessStatus", entry.ReadinessStatus);
            builder.Append('}');
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendInt(StringBuilder builder, string fieldName, int value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":")
                .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
