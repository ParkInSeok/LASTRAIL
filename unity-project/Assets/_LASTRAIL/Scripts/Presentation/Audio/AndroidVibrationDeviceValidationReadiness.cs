using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Presentation.Audio
{
    public sealed class AndroidVibrationDeviceValidationReadiness
    {
        private AndroidVibrationDeviceValidationReadiness(
            string surfaceKey,
            IEnumerable<string> cueIds,
            AndroidNativeVibrationBridgeStatus bridgeStatus,
            bool vibrationEnabled)
        {
            SurfaceKey = RequireText(surfaceKey, nameof(surfaceKey));
            CueIds = CopyCueIds(cueIds);
            BridgeStatus = bridgeStatus;
            VibrationEnabled = vibrationEnabled;
            LightCueCount = CountIntensity(CueIds, AndroidVibrationIntensity.Light);
            MediumCueCount = CountIntensity(CueIds, AndroidVibrationIntensity.Medium);
            HeavyCueCount = CountIntensity(CueIds, AndroidVibrationIntensity.Heavy);
            Blockers = BuildBlockers(bridgeStatus, vibrationEnabled);
        }

        public string SurfaceKey { get; }
        public IReadOnlyList<string> CueIds { get; }
        public AndroidNativeVibrationBridgeStatus BridgeStatus { get; }
        public bool VibrationEnabled { get; }
        public int CueCount => CueIds.Count;
        public int LightCueCount { get; }
        public int MediumCueCount { get; }
        public int HeavyCueCount { get; }
        public bool CanRunOnCurrentDevice => VibrationEnabled && BridgeStatus == AndroidNativeVibrationBridgeStatus.Available;
        public bool RequiresManualAndroidDevicePass => VibrationEnabled;
        public bool FallsBackToNoVibration => !CanRunOnCurrentDevice;
        public bool IsBlockingGameplay => false;
        public IReadOnlyList<string> Blockers { get; }
        public int BlockerCount => Blockers.Count;
        public bool HasRawDeviceIdentifier => false;
        public bool HasRawLogPayload => false;

        public static AndroidVibrationDeviceValidationReadiness FromBridgeReadiness(AndroidNativeVibrationBridgeReadiness readiness)
        {
            if (readiness == null)
            {
                throw new ArgumentNullException(nameof(readiness));
            }

            return new AndroidVibrationDeviceValidationReadiness(
                readiness.SurfaceKey,
                readiness.VibrationCueIds,
                readiness.Status,
                readiness.VibrationEnabled);
        }

        public static AndroidVibrationDeviceValidationReadiness FromCueIds(
            string surfaceKey,
            IEnumerable<string> cueIds,
            AndroidNativeVibrationBridgeStatus bridgeStatus,
            bool vibrationEnabled)
        {
            return new AndroidVibrationDeviceValidationReadiness(surfaceKey, cueIds, bridgeStatus, vibrationEnabled);
        }

        private static int CountIntensity(IReadOnlyList<string> cueIds, AndroidVibrationIntensity intensity)
        {
            var count = 0;
            foreach (var cueId in cueIds)
            {
                if (AndroidVibrationCue.FromCueId(cueId).Intensity == intensity)
                {
                    count++;
                }
            }

            return count;
        }

        private static IReadOnlyList<string> BuildBlockers(AndroidNativeVibrationBridgeStatus status, bool vibrationEnabled)
        {
            var blockers = new List<string>();
            if (!vibrationEnabled)
            {
                blockers.Add("vibration_disabled_for_device_validation");
                return blockers.AsReadOnly();
            }

            if (status != AndroidNativeVibrationBridgeStatus.Available)
            {
                blockers.Add("android_vibration_device_validation_pending");
            }

            return blockers.AsReadOnly();
        }

        private static IReadOnlyList<string> CopyCueIds(IEnumerable<string> cueIds)
        {
            var copied = new List<string>();
            if (cueIds == null)
            {
                return copied.AsReadOnly();
            }

            foreach (var cueId in cueIds)
            {
                if (string.IsNullOrWhiteSpace(cueId))
                {
                    continue;
                }

                var normalized = cueId.Trim();
                if (!copied.Contains(normalized))
                {
                    copied.Add(normalized);
                }
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

    public static class AndroidVibrationDeviceValidationReadinessJsonAdapter
    {
        public static string ToJson(AndroidVibrationDeviceValidationReadiness readiness)
        {
            if (readiness == null)
            {
                throw new ArgumentNullException(nameof(readiness));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surfaceKey", readiness.SurfaceKey).Append(',');
            AppendString(builder, "bridgeStatus", readiness.BridgeStatus.ToString()).Append(',');
            AppendBool(builder, "vibrationEnabled", readiness.VibrationEnabled).Append(',');
            AppendBool(builder, "canRunOnCurrentDevice", readiness.CanRunOnCurrentDevice).Append(',');
            AppendBool(builder, "requiresManualAndroidDevicePass", readiness.RequiresManualAndroidDevicePass).Append(',');
            AppendBool(builder, "fallsBackToNoVibration", readiness.FallsBackToNoVibration).Append(',');
            AppendBool(builder, "isBlockingGameplay", readiness.IsBlockingGameplay).Append(',');
            AppendInt(builder, "cueCount", readiness.CueCount).Append(',');
            AppendInt(builder, "lightCueCount", readiness.LightCueCount).Append(',');
            AppendInt(builder, "mediumCueCount", readiness.MediumCueCount).Append(',');
            AppendInt(builder, "heavyCueCount", readiness.HeavyCueCount).Append(',');
            AppendStringArray(builder, "cueIds", readiness.CueIds).Append(',');
            AppendInt(builder, "blockerCount", readiness.BlockerCount).Append(',');
            AppendStringArray(builder, "blockers", readiness.Blockers).Append(',');
            AppendBool(builder, "hasRawDeviceIdentifier", readiness.HasRawDeviceIdentifier).Append(',');
            AppendBool(builder, "hasRawLogPayload", readiness.HasRawLogPayload);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string name, IReadOnlyList<string> values)
        {
            builder.Append('"').Append(name).Append("\":[");
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

        private static StringBuilder AppendString(StringBuilder builder, string name, string value)
        {
            builder.Append('"').Append(name).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendBool(StringBuilder builder, string name, bool value)
        {
            return builder.Append('"').Append(name).Append("\":").Append(value ? "true" : "false");
        }

        private static StringBuilder AppendInt(StringBuilder builder, string name, int value)
        {
            return builder.Append('"').Append(name).Append("\":").Append(value);
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
