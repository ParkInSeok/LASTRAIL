using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LastRail.Run
{
    public static class RunRewardPreviewJsonAdapter
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

        public static RunRewardPreviewDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new RunRewardPreviewDto(
                RequiredString(json, "runId"),
                RequiredString(json, "rewardPreviewId"),
                ParsePreviewStatus(RequiredString(json, "status")),
                OptionalUtcDateTime(json, "expiresAt"),
                RequiredUtcDateTime(json, "serverCalculatedAt"),
                RequiredString(json, "balanceVersion"),
                ParseClaimability(RequiredString(json, "claimability")),
                ParseRewardLines(json));
        }

        public static string ToJson(RunRewardPreviewDto preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", preview.RunId).Append(',');
            AppendString(builder, "rewardPreviewId", preview.RewardPreviewId).Append(',');
            AppendString(builder, "status", PreviewStatusToServerValue(preview.PreviewStatus)).Append(',');
            AppendOptionalDateTime(builder, "expiresAt", preview.ExpiresAt).Append(',');
            AppendString(builder, "serverCalculatedAt", preview.ServerCalculatedAt.ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "balanceVersion", preview.BalanceVersion).Append(',');
            AppendString(builder, "claimability", ClaimabilityToServerValue(preview.Claimability)).Append(',');
            AppendRewardLines(builder, preview.RewardLines);
            builder.Append('}');
            return builder.ToString();
        }

        private static IReadOnlyList<RunRewardLineDto> ParseRewardLines(string json)
        {
            var match = Regex.Match(json, "\\\"rewardLines\\\"\\s*:\\s*\\[(.*?)\\]\\s*(?:,|})", RegexOptions.Singleline);
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON array field: rewardLines");
            }

            var lines = new List<RunRewardLineDto>();
            foreach (Match objectMatch in Regex.Matches(match.Groups[1].Value, "\\{([^{}]*)\\}"))
            {
                var objectJson = "{" + objectMatch.Groups[1].Value + "}";
                lines.Add(new RunRewardLineDto(
                    RequiredString(objectJson, "rewardLineId"),
                    RequiredString(objectJson, "rewardCode"),
                    RequiredString(objectJson, "displayName"),
                    RequiredInt(objectJson, "quantity"),
                    RequiredString(objectJson, "unitLabel"),
                    OptionalInt(objectJson, "balanceAfter")));
            }

            return lines;
        }

        private static RunRewardPreviewStatus ParsePreviewStatus(string value)
        {
            switch (value)
            {
                case "ACTIVE":
                    return RunRewardPreviewStatus.Active;
                case "CLAIMED":
                    return RunRewardPreviewStatus.Claimed;
                case "EXPIRED":
                    return RunRewardPreviewStatus.Expired;
                case "INVALIDATED":
                    return RunRewardPreviewStatus.Invalidated;
                default:
                    throw new ArgumentException("Unsupported reward preview status.", nameof(value));
            }
        }

        private static RunRewardClaimability ParseClaimability(string value)
        {
            switch (value)
            {
                case "CLAIMABLE":
                    return RunRewardClaimability.Claimable;
                case "ALREADY_CLAIMED":
                    return RunRewardClaimability.AlreadyClaimed;
                case "EXPIRED":
                    return RunRewardClaimability.Expired;
                case "INVALIDATED":
                    return RunRewardClaimability.Invalidated;
                default:
                    throw new ArgumentException("Unsupported reward claimability.", nameof(value));
            }
        }

        private static string PreviewStatusToServerValue(RunRewardPreviewStatus status)
        {
            switch (status)
            {
                case RunRewardPreviewStatus.Active:
                    return "ACTIVE";
                case RunRewardPreviewStatus.Claimed:
                    return "CLAIMED";
                case RunRewardPreviewStatus.Expired:
                    return "EXPIRED";
                case RunRewardPreviewStatus.Invalidated:
                    return "INVALIDATED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported reward preview status.");
            }
        }

        private static string ClaimabilityToServerValue(RunRewardClaimability claimability)
        {
            switch (claimability)
            {
                case RunRewardClaimability.Claimable:
                    return "CLAIMABLE";
                case RunRewardClaimability.AlreadyClaimed:
                    return "ALREADY_CLAIMED";
                case RunRewardClaimability.Expired:
                    return "EXPIRED";
                case RunRewardClaimability.Invalidated:
                    return "INVALIDATED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(claimability), claimability, "Unsupported reward claimability.");
            }
        }

        private static string RequiredString(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                throw new InvalidOperationException("Required JSON field is null: " + fieldName);
            }

            var match = Regex.Match(json, StringFieldPattern(fieldName));
            if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                throw new InvalidOperationException("Missing required JSON string field: " + fieldName);
            }

            return Unescape(match.Groups[1].Value);
        }

        private static int RequiredInt(string json, string fieldName)
        {
            var match = Regex.Match(json, FieldPattern(fieldName, "-?\\d+"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON int field: " + fieldName);
            }

            return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static int? OptionalInt(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                return null;
            }

            var match = Regex.Match(json, FieldPattern(fieldName, "-?\\d+"));
            return match.Success
                ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
                : (int?)null;
        }

        private static DateTime RequiredUtcDateTime(string json, string fieldName)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(RequiredString(json, fieldName), AcceptedUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None),
                DateTimeKind.Utc);
        }

        private static DateTime? OptionalUtcDateTime(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                return null;
            }

            return RequiredUtcDateTime(json, fieldName);
        }

        private static void AppendRewardLines(StringBuilder builder, IReadOnlyList<RunRewardLineDto> rewardLines)
        {
            builder.Append("\"rewardLines\":[");
            for (var i = 0; i < rewardLines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var line = rewardLines[i];
                builder.Append('{');
                AppendString(builder, "rewardLineId", line.RewardLineId).Append(',');
                AppendString(builder, "rewardCode", line.RewardCode).Append(',');
                AppendString(builder, "displayName", line.DisplayName).Append(',');
                builder.Append("\"quantity\":").Append(line.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',');
                AppendString(builder, "unitLabel", line.UnitLabel);
                if (line.BalanceAfter.HasValue)
                {
                    builder.Append(',');
                    builder.Append("\"balanceAfter\":").Append(line.BalanceAfter.Value.ToString(CultureInfo.InvariantCulture));
                }

                builder.Append('}');
            }

            builder.Append(']');
        }

        private static StringBuilder AppendOptionalDateTime(StringBuilder builder, string fieldName, DateTime? value)
        {
            if (!value.HasValue)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value.Value.ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture));
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":\"")
                .Append(Escape(value))
                .Append('"');
        }

        private static string FieldPattern(string fieldName, string valuePattern)
        {
            return "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*(" + valuePattern + ")";
        }

        private static string StringFieldPattern(string fieldName)
        {
            return "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\"\\\\])*)\\\"";
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
