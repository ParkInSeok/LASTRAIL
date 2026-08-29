using System;
using System.Text;

namespace LastRail.Presentation
{
    public static class UIScreenBindingReadinessAuditJsonAdapter
    {
        public static string ToJson(UIScreenBindingReadinessAudit audit)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "screenKey", audit.ScreenKey).Append(',');
            AppendBool(builder, "usesPortraitSafeArea", audit.UsesPortraitSafeArea).Append(',');
            AppendString(builder, "preferredAspectLabel", audit.PreferredAspectLabel).Append(',');
            AppendInt(builder, "minimumTouchTargetDp", audit.MinimumTouchTargetDp).Append(',');
            AppendBool(builder, "requiresOverflowVerification", audit.RequiresOverflowVerification).Append(',');
            AppendString(builder, "overflowRiskKey", audit.OverflowRiskKey).Append(',');
            AppendBool(builder, "keepsHudAboveAnimationLayer", audit.KeepsHudAboveAnimationLayer).Append(',');
            AppendStringArray(builder, "requiredBindingKeys", audit.RequiredBindingKeys).Append(',');
            AppendStringArray(builder, "missingBindingKeys", audit.MissingBindingKeys).Append(',');
            AppendBool(builder, "hasRequiredBindings", audit.HasRequiredBindings).Append(',');
            AppendBool(builder, "requiresEditorVisualVerification", audit.RequiresEditorVisualVerification).Append(',');
            AppendStringArray(builder, "editorVerificationChecklist", audit.EditorVerificationChecklist).Append(',');
            AppendBool(builder, "hasSceneOrPrefabAsset", audit.HasSceneOrPrefabAsset).Append(',');
            AppendBool(builder, "hasServerTransport", audit.HasServerTransport).Append(',');
            AppendBool(builder, "hasRewardCalculation", audit.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", audit.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", audit.HasClientSideRng);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string fieldName, System.Collections.Generic.IReadOnlyList<string> values)
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
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
