using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Presentation.Audio
{
    public enum AndroidNativeVibrationBridgeStatus
    {
        Available,
        DisabledByUser,
        UnsupportedPlatform,
        NativeBridgeMissing
    }

    public enum AndroidVibrationIntensity
    {
        Light,
        Medium,
        Heavy
    }

    public enum AndroidVibrationDispatchOutcome
    {
        WouldDispatch,
        Dispatched,
        SkippedDisabled,
        SkippedUnsupportedPlatform,
        FailedNativeBridgeMissing,
        FailedNativeDispatch
    }

    public sealed class AndroidVibrationCue
    {
        private AndroidVibrationCue(string cueId, AndroidVibrationIntensity intensity, int durationMs)
        {
            CueId = RequireText(cueId, nameof(cueId));
            Intensity = intensity;
            DurationMs = durationMs;
            IsBlockingGameplay = false;
            HasRawDeviceIdentifier = false;
            HasRawLogPayload = false;
        }

        public string CueId { get; }
        public AndroidVibrationIntensity Intensity { get; }
        public int DurationMs { get; }
        public bool IsBlockingGameplay { get; }
        public bool HasRawDeviceIdentifier { get; }
        public bool HasRawLogPayload { get; }

        public static AndroidVibrationCue FromCueId(string cueId)
        {
            var normalized = RequireText(cueId, nameof(cueId));
            var lowered = normalized.ToLowerInvariant();

            if (lowered.Contains("ultimate") || lowered.Contains("boss") || lowered.Contains("heavy"))
            {
                return new AndroidVibrationCue(normalized, AndroidVibrationIntensity.Heavy, 80);
            }

            if (lowered.Contains("skill") || lowered.Contains("gacha") || lowered.Contains("result") || lowered.Contains("wave"))
            {
                return new AndroidVibrationCue(normalized, AndroidVibrationIntensity.Medium, 45);
            }

            return new AndroidVibrationCue(normalized, AndroidVibrationIntensity.Light, 25);
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

    public sealed class AndroidVibrationDispatchResult
    {
        public AndroidVibrationDispatchResult(AndroidVibrationDispatchOutcome outcome, string cueId, bool canFallbackToNoVibration)
        {
            Outcome = outcome;
            CueId = string.IsNullOrWhiteSpace(cueId) ? null : cueId.Trim();
            CanFallbackToNoVibration = canFallbackToNoVibration;
            IsBlockingGameplay = false;
            HasRawDeviceIdentifier = false;
            HasRawLogPayload = false;
        }

        public AndroidVibrationDispatchOutcome Outcome { get; }
        public string CueId { get; }
        public bool CanFallbackToNoVibration { get; }
        public bool IsBlockingGameplay { get; }
        public bool HasRawDeviceIdentifier { get; }
        public bool HasRawLogPayload { get; }
    }


    public interface IAndroidNativeVibrationBridge
    {
        AndroidNativeVibrationBridgeStatus Status { get; }
        bool VibrationEnabled { get; }
        bool IsBlockingGameplay { get; }
        AndroidVibrationDispatchResult TryVibrate(AndroidVibrationCue cue);
    }

    public sealed class AndroidNativeVibrationBridgeReadinessAdapter : IAndroidNativeVibrationBridge
    {
        private readonly AndroidNativeVibrationDispatchPolicy dispatchPolicy;

        public AndroidNativeVibrationBridgeReadinessAdapter(
            AndroidNativeVibrationBridgeStatus status,
            bool vibrationEnabled)
        {
            Status = status;
            VibrationEnabled = vibrationEnabled;
            dispatchPolicy = AndroidNativeVibrationDispatchPolicy.ForStatus(status, vibrationEnabled);
        }

        public AndroidNativeVibrationBridgeStatus Status { get; }
        public bool VibrationEnabled { get; }
        public bool IsBlockingGameplay => false;

        public AndroidVibrationDispatchResult TryVibrate(AndroidVibrationCue cue)
        {
            return dispatchPolicy.TryDispatch(cue);
        }
    }

    public sealed class AndroidNativeVibrationDispatchPolicy
    {
        private AndroidNativeVibrationDispatchPolicy(AndroidNativeVibrationBridgeStatus status, bool vibrationEnabled)
        {
            Status = status;
            VibrationEnabled = vibrationEnabled;
        }

        public AndroidNativeVibrationBridgeStatus Status { get; }
        public bool VibrationEnabled { get; }
        public bool IsBlockingGameplay => false;

        public static AndroidNativeVibrationDispatchPolicy ForStatus(
            AndroidNativeVibrationBridgeStatus status,
            bool vibrationEnabled)
        {
            return new AndroidNativeVibrationDispatchPolicy(status, vibrationEnabled);
        }

        public AndroidVibrationDispatchResult TryDispatch(AndroidVibrationCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (!VibrationEnabled || Status == AndroidNativeVibrationBridgeStatus.DisabledByUser)
            {
                return new AndroidVibrationDispatchResult(
                    AndroidVibrationDispatchOutcome.SkippedDisabled,
                    cue.CueId,
                    canFallbackToNoVibration: true);
            }

            if (Status == AndroidNativeVibrationBridgeStatus.UnsupportedPlatform)
            {
                return new AndroidVibrationDispatchResult(
                    AndroidVibrationDispatchOutcome.SkippedUnsupportedPlatform,
                    cue.CueId,
                    canFallbackToNoVibration: true);
            }

            if (Status == AndroidNativeVibrationBridgeStatus.NativeBridgeMissing)
            {
                return new AndroidVibrationDispatchResult(
                    AndroidVibrationDispatchOutcome.FailedNativeBridgeMissing,
                    cue.CueId,
                    canFallbackToNoVibration: true);
            }

            return new AndroidVibrationDispatchResult(
                AndroidVibrationDispatchOutcome.WouldDispatch,
                cue.CueId,
                canFallbackToNoVibration: false);
        }
    }

    public sealed class AndroidNativeVibrationBridgeReadiness
    {
        private AndroidNativeVibrationBridgeReadiness(
            string surfaceKey,
            IEnumerable<string> vibrationCueIds,
            AndroidNativeVibrationBridgeStatus status,
            bool vibrationEnabled)
        {
            SurfaceKey = RequireText(surfaceKey, nameof(surfaceKey));
            VibrationCueIds = CopyUniqueOptional(vibrationCueIds);
            Status = status;
            VibrationEnabled = vibrationEnabled;
            Blockers = BuildBlockers(status, vibrationEnabled);
        }

        public string SurfaceKey { get; }
        public IReadOnlyList<string> VibrationCueIds { get; }
        public AndroidNativeVibrationBridgeStatus Status { get; }
        public bool VibrationEnabled { get; }
        public bool RequiresNativeVibrationBridge => VibrationEnabled && Status != AndroidNativeVibrationBridgeStatus.UnsupportedPlatform;
        public bool HasNativeVibrationBridge => VibrationEnabled && Status == AndroidNativeVibrationBridgeStatus.Available;
        public bool FallsBackToNoVibration => !VibrationEnabled || Status != AndroidNativeVibrationBridgeStatus.Available;
        public bool IsBlockingGameplay => false;
        public int CueCount => VibrationCueIds.Count;
        public IReadOnlyList<string> Blockers { get; }
        public int BlockerCount => Blockers.Count;
        public bool HasRawDeviceIdentifier => false;
        public bool HasRawLogPayload => false;

        public static AndroidNativeVibrationBridgeReadiness FromAudioReadiness(
            LaunchAudioCueReadinessReport report,
            AndroidNativeVibrationBridgeStatus status,
            bool vibrationEnabled)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var cueIds = new List<string>();
            AddRange(cueIds, report.Mission.VibrationCueIds);
            AddRange(cueIds, report.Gacha.VibrationCueIds);
            AddRange(cueIds, report.Combat.VibrationCueIds);

            return new AndroidNativeVibrationBridgeReadiness(
                "launch_player_facing_feedback",
                cueIds,
                status,
                vibrationEnabled);
        }

        public static AndroidNativeVibrationBridgeReadiness FromCueIds(
            string surfaceKey,
            IEnumerable<string> vibrationCueIds,
            AndroidNativeVibrationBridgeStatus status,
            bool vibrationEnabled)
        {
            return new AndroidNativeVibrationBridgeReadiness(surfaceKey, vibrationCueIds, status, vibrationEnabled);
        }

        private static IReadOnlyList<string> BuildBlockers(AndroidNativeVibrationBridgeStatus status, bool vibrationEnabled)
        {
            var blockers = new List<string>();
            if (vibrationEnabled && status == AndroidNativeVibrationBridgeStatus.NativeBridgeMissing)
            {
                blockers.Add("native_vibration_bridge_pending");
            }

            return blockers.AsReadOnly();
        }

        private static void AddRange(ICollection<string> target, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (var value in values)
            {
                AddOptional(target, value);
            }
        }

        private static IReadOnlyList<string> CopyUniqueOptional(IEnumerable<string> values)
        {
            var copied = new List<string>();
            AddRange(copied, values);
            return copied.AsReadOnly();
        }

        private static void AddOptional(ICollection<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            if (!values.Contains(normalized))
            {
                values.Add(normalized);
            }
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

    public static class AndroidNativeVibrationBridgeReadinessJsonAdapter
    {
        public static string ToJson(AndroidNativeVibrationBridgeReadiness readiness)
        {
            if (readiness == null)
            {
                throw new ArgumentNullException(nameof(readiness));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "surfaceKey", readiness.SurfaceKey);
            AppendCommaString(builder, "status", readiness.Status.ToString());
            AppendCommaBool(builder, "vibrationEnabled", readiness.VibrationEnabled);
            AppendCommaBool(builder, "requiresNativeVibrationBridge", readiness.RequiresNativeVibrationBridge);
            AppendCommaBool(builder, "hasNativeVibrationBridge", readiness.HasNativeVibrationBridge);
            AppendCommaBool(builder, "fallsBackToNoVibration", readiness.FallsBackToNoVibration);
            AppendCommaBool(builder, "isBlockingGameplay", readiness.IsBlockingGameplay);
            builder.Append(',').Append("\"cueCount\":").Append(readiness.CueCount);
            builder.Append(',');
            AppendStringArray(builder, "vibrationCueIds", readiness.VibrationCueIds);
            builder.Append(',').Append("\"blockerCount\":").Append(readiness.BlockerCount);
            builder.Append(',');
            AppendStringArray(builder, "blockers", readiness.Blockers);
            AppendCommaBool(builder, "hasRawDeviceIdentifier", readiness.HasRawDeviceIdentifier);
            AppendCommaBool(builder, "hasRawLogPayload", readiness.HasRawLogPayload);
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendStringArray(StringBuilder builder, string name, IReadOnlyList<string> values)
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

            builder.Append(']');
        }

        private static void AppendString(StringBuilder builder, string name, string value)
        {
            builder.Append('"').Append(name).Append("\":");
            AppendJsonString(builder, value);
        }

        private static void AppendCommaString(StringBuilder builder, string name, string value)
        {
            builder.Append(',');
            AppendString(builder, name, value);
        }

        private static void AppendCommaBool(StringBuilder builder, string name, bool value)
        {
            builder.Append(',').Append('"').Append(name).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendJsonString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
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

            builder.Append('"');
        }
    }
}


