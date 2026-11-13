using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LastRail.Run
{
    public static class RunResultSubmitResponseJsonAdapter
    {
        private const string UtcRoundTripFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
        private static readonly string[] AcceptedUtcDateTimeFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"
        };

        public static RunResultSubmitResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new RunResultSubmitResponseDto(
                RequiredString(json, "runId"),
                RequiredBool(json, "accepted"),
                RequiredString(json, "resultStatus"),
                OptionalString(json, "rewardClaimId"),
                OptionalString(json, "rewardPreviewId"),
                RequiredUtcDateTime(json, "serverReceivedAt"),
                RequiredString(json, "balanceVersion"),
                OptionalString(json, "message"),
                OptionalString(json, "errorCode"),
                OptionalStaminaRefund(json));
        }

        public static string ToJson(RunResultSubmitResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", response.RunId).Append(',');
            builder.Append("\"accepted\":").Append(response.Accepted ? "true" : "false").Append(',');
            AppendString(builder, "resultStatus", response.ResultStatus).Append(',');
            AppendOptionalString(builder, "rewardClaimId", response.RewardClaimId).Append(',');
            AppendOptionalString(builder, "rewardPreviewId", response.RewardPreviewId).Append(',');
            AppendString(builder, "serverReceivedAt", response.ServerReceivedAt.ToUniversalTime().ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "balanceVersion", response.BalanceVersion).Append(',');
            AppendStaminaRefund(builder, response.StaminaRefund).Append(',');
            AppendOptionalString(builder, "message", response.Message).Append(',');
            AppendOptionalString(builder, "errorCode", response.ErrorCode);
            builder.Append('}');
            return builder.ToString();
        }

        private static string RequiredString(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                throw new InvalidOperationException("Required JSON field is null: " + fieldName);
            }

            var value = OptionalString(json, fieldName);
            if (value == null)
            {
                throw new InvalidOperationException("Missing required JSON string field: " + fieldName);
            }

            return value;
        }

        private static string OptionalString(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                return null;
            }

            var match = Regex.Match(json, StringFieldPattern(fieldName));
            return match.Success ? Unescape(match.Groups[1].Value) : null;
        }

        private static bool RequiredBool(string json, string fieldName)
        {
            var match = Regex.Match(json, FieldPattern(fieldName, "true|false"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON bool field: " + fieldName);
            }

            return string.Equals(match.Groups[1].Value, "true", StringComparison.Ordinal);
        }

        private static DateTime RequiredUtcDateTime(string json, string fieldName)
        {
            var value = RequiredString(json, fieldName);
            return DateTime.SpecifyKind(
                DateTime.ParseExact(value, AcceptedUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None),
                DateTimeKind.Utc);
        }

        private static RunStaminaRefundMarker OptionalStaminaRefund(string json)
        {
            if (Regex.IsMatch(json, FieldPattern("staminaRefund", "null")))
            {
                return null;
            }

            var match = Regex.Match(json, "\\\"staminaRefund\\\"\\s*:\\s*\\{(?<body>[^}]*)\\}");
            if (!match.Success)
            {
                return null;
            }

            var body = match.Groups["body"].Value;
            return new RunStaminaRefundMarker(
                OptionalString(body, "status"),
                OptionalString(body, "message"));
        }

        private static string FieldPattern(string fieldName, string valuePattern)
        {
            return "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*(" + valuePattern + ")";
        }

        private static string StringFieldPattern(string fieldName)
        {
            return "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\"\\\\])*)\\\"";
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":\"")
                .Append(Escape(value))
                .Append('"');
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
        }

        private static StringBuilder AppendStaminaRefund(StringBuilder builder, RunStaminaRefundMarker marker)
        {
            builder.Append("\"staminaRefund\":");
            if (marker == null)
            {
                return builder.Append("null");
            }

            builder.Append('{');
            AppendOptionalString(builder, "status", marker.Status).Append(',');
            AppendOptionalString(builder, "message", marker.Message).Append(',');
            builder.Append("\"hasRewardCalculation\":").Append(marker.HasRewardCalculation ? "true" : "false").Append(',');
            builder.Append("\"hasEconomyMutation\":").Append(marker.HasEconomyMutation ? "true" : "false");
            return builder.Append('}');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Unescape(string value)
        {
            return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
