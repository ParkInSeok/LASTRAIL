using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Presentation
{
    public sealed class LaunchBlockerClosureStep
    {
        public LaunchBlockerClosureStep(
            string stepKey,
            string blockerCode,
            bool isComplete,
            bool requiresEditorApproval,
            bool requiresUserApproval,
            bool requiresDeviceValidation,
            bool requiresOauthAppRegistration)
        {
            StepKey = RequireText(stepKey, nameof(stepKey));
            BlockerCode = RequireText(blockerCode, nameof(blockerCode));
            IsComplete = isComplete;
            RequiresEditorApproval = requiresEditorApproval;
            RequiresUserApproval = requiresUserApproval;
            RequiresDeviceValidation = requiresDeviceValidation;
            RequiresOauthAppRegistration = requiresOauthAppRegistration;
        }

        public string StepKey { get; }
        public string BlockerCode { get; }
        public bool IsComplete { get; }
        public bool RequiresEditorApproval { get; }
        public bool RequiresUserApproval { get; }
        public bool RequiresDeviceValidation { get; }
        public bool RequiresOauthAppRegistration { get; }
        public bool IsLaunchBlocking => !IsComplete;

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }
    }

    public sealed class LaunchBlockerClosurePlan
    {
        private LaunchBlockerClosurePlan(IEnumerable<LaunchBlockerClosureStep> steps)
        {
            Steps = CopySteps(steps);
            OpenBlockers = BuildOpenBlockers(Steps);
        }

        public IReadOnlyList<LaunchBlockerClosureStep> Steps { get; }
        public int StepCount => Steps.Count;
        public IReadOnlyList<string> OpenBlockers { get; }
        public int OpenBlockerCount => OpenBlockers.Count;
        public bool IsImplementationReadyForHandoff => true;
        public bool IsLaunchQaComplete => OpenBlockerCount == 0;
        public bool RequiresNewServerScope => false;
        public bool RequiresNewAdminScope => false;
        public bool RequiresNewNarrativeScope => false;
        public bool RequiresRailChoiceEvents => false;
        public bool RequiresWorldRaidUi => false;
        public bool HasServerTruthCalculation => false;
        public bool HasRewardCalculation => false;
        public bool HasOfflineRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;
        public bool HasRawTokenPayload => false;
        public bool HasAccountShortcut => false;
        public bool HasRawDeviceIdentifier => false;
        public bool HasRawAssetPayload => false;

        public static LaunchBlockerClosurePlan ForCurrentCodeReadiness()
        {
            return new LaunchBlockerClosurePlan(new[]
            {
                new LaunchBlockerClosureStep(
                    "unity_2d_animation_package_resolve",
                    "com_unity_2d_animation_package_resolve_pending",
                    isComplete: true,
                    requiresEditorApproval: false,
                    requiresUserApproval: false,
                    requiresDeviceValidation: false,
                    requiresOauthAppRegistration: false),
                new LaunchBlockerClosureStep(
                    "scene_prefab_binding_pass",
                    "scene_prefab_binding_pending",
                    isComplete: false,
                    requiresEditorApproval: true,
                    requiresUserApproval: false,
                    requiresDeviceValidation: false,
                    requiresOauthAppRegistration: false),
                new LaunchBlockerClosureStep(
                    "player_facing_screenshot_qa_7_screens",
                    "editor_device_screenshot_qa_unavailable",
                    isComplete: false,
                    requiresEditorApproval: true,
                    requiresUserApproval: false,
                    requiresDeviceValidation: true,
                    requiresOauthAppRegistration: false),
                new LaunchBlockerClosureStep(
                    "android_vibration_native_device_pass",
                    "native_vibration_bridge_pending",
                    isComplete: false,
                    requiresEditorApproval: true,
                    requiresUserApproval: false,
                    requiresDeviceValidation: true,
                    requiresOauthAppRegistration: false),
                new LaunchBlockerClosureStep(
                    "android_keystore_secure_storage_plugin_pass",
                    "android_keystore_secure_storage_plugin_pending",
                    isComplete: false,
                    requiresEditorApproval: true,
                    requiresUserApproval: true,
                    requiresDeviceValidation: true,
                    requiresOauthAppRegistration: false),
                new LaunchBlockerClosureStep(
                    "google_sign_in_sdk_pass_after_app_registration",
                    "google_sdk_oauth_app_registration_pending",
                    isComplete: false,
                    requiresEditorApproval: true,
                    requiresUserApproval: true,
                    requiresDeviceValidation: true,
                    requiresOauthAppRegistration: true),
                new LaunchBlockerClosureStep(
                    "android_build_smoke",
                    "android_build_smoke_pending",
                    isComplete: false,
                    requiresEditorApproval: true,
                    requiresUserApproval: false,
                    requiresDeviceValidation: true,
                    requiresOauthAppRegistration: false)
            });
        }

        private static IReadOnlyList<LaunchBlockerClosureStep> CopySteps(IEnumerable<LaunchBlockerClosureStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            var copied = new List<LaunchBlockerClosureStep>();
            foreach (var step in steps)
            {
                if (step == null)
                {
                    throw new ArgumentNullException(nameof(steps));
                }

                copied.Add(step);
            }

            return copied.AsReadOnly();
        }

        private static IReadOnlyList<string> BuildOpenBlockers(IReadOnlyList<LaunchBlockerClosureStep> steps)
        {
            var blockers = new List<string>();
            foreach (var step in steps)
            {
                if (step.IsComplete || blockers.Contains(step.BlockerCode))
                {
                    continue;
                }

                blockers.Add(step.BlockerCode);
            }

            return blockers.AsReadOnly();
        }
    }

    public static class LaunchBlockerClosurePlanJsonAdapter
    {
        public static string ToJson(LaunchBlockerClosurePlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendInt(builder, "stepCount", plan.StepCount).Append(',');
            AppendInt(builder, "openBlockerCount", plan.OpenBlockerCount).Append(',');
            AppendSteps(builder, plan.Steps).Append(',');
            AppendStringArray(builder, "openBlockers", plan.OpenBlockers).Append(',');
            AppendBool(builder, "isImplementationReadyForHandoff", plan.IsImplementationReadyForHandoff).Append(',');
            AppendBool(builder, "isLaunchQaComplete", plan.IsLaunchQaComplete).Append(',');
            AppendBool(builder, "requiresNewServerScope", plan.RequiresNewServerScope).Append(',');
            AppendBool(builder, "requiresNewAdminScope", plan.RequiresNewAdminScope).Append(',');
            AppendBool(builder, "requiresNewNarrativeScope", plan.RequiresNewNarrativeScope).Append(',');
            AppendBool(builder, "requiresRailChoiceEvents", plan.RequiresRailChoiceEvents).Append(',');
            AppendBool(builder, "requiresWorldRaidUi", plan.RequiresWorldRaidUi).Append(',');
            AppendBool(builder, "hasServerTruthCalculation", plan.HasServerTruthCalculation).Append(',');
            AppendBool(builder, "hasRewardCalculation", plan.HasRewardCalculation).Append(',');
            AppendBool(builder, "hasOfflineRewardCalculation", plan.HasOfflineRewardCalculation).Append(',');
            AppendBool(builder, "hasEconomyMutation", plan.HasEconomyMutation).Append(',');
            AppendBool(builder, "hasClientSideRng", plan.HasClientSideRng).Append(',');
            AppendBool(builder, "hasRawTokenPayload", plan.HasRawTokenPayload).Append(',');
            AppendBool(builder, "hasAccountShortcut", plan.HasAccountShortcut).Append(',');
            AppendBool(builder, "hasRawDeviceIdentifier", plan.HasRawDeviceIdentifier).Append(',');
            AppendBool(builder, "hasRawAssetPayload", plan.HasRawAssetPayload);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendSteps(StringBuilder builder, IReadOnlyList<LaunchBlockerClosureStep> steps)
        {
            builder.Append("\"steps\":[");
            for (var i = 0; i < steps.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var step = steps[i];
                builder.Append('{');
                AppendString(builder, "stepKey", step.StepKey).Append(',');
                AppendString(builder, "blockerCode", step.BlockerCode).Append(',');
                AppendBool(builder, "isComplete", step.IsComplete).Append(',');
                AppendBool(builder, "requiresEditorApproval", step.RequiresEditorApproval).Append(',');
                AppendBool(builder, "requiresUserApproval", step.RequiresUserApproval).Append(',');
                AppendBool(builder, "requiresDeviceValidation", step.RequiresDeviceValidation).Append(',');
                AppendBool(builder, "requiresOauthAppRegistration", step.RequiresOauthAppRegistration).Append(',');
                AppendBool(builder, "isLaunchBlocking", step.IsLaunchBlocking);
                builder.Append('}');
            }

            return builder.Append(']');
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
