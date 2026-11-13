using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LastRail.Run
{
    public static class RunRewardClaimResponseJsonAdapter
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

        public static RunRewardClaimResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new RunRewardClaimResponseDto(
                RequiredString(json, "runId"),
                RequiredString(json, "rewardPreviewId"),
                OptionalString(json, "rewardClaimId"),
                ParseClaimResultStatus(RequiredString(json, "status")),
                RequiredUtcDateTime(json, "serverClaimedAt"),
                RequiredString(json, "balanceVersion"),
                ParseRewardLines(json),
                OptionalString(json, "errorCode"),
                OptionalString(json, "message"));
        }

        public static string ToJson(RunRewardClaimResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", response.RunId).Append(',');
            AppendString(builder, "rewardPreviewId", response.RewardPreviewId).Append(',');
            AppendOptionalString(builder, "rewardClaimId", response.RewardClaimId).Append(',');
            AppendString(builder, "status", ClaimResultStatusToServerValue(response.ClaimResultStatus)).Append(',');
            AppendString(builder, "serverClaimedAt", response.ServerClaimedAt.ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "balanceVersion", response.BalanceVersion).Append(',');
            AppendRewardLines(builder, response.RewardLines).Append(',');
            AppendOptionalString(builder, "errorCode", response.ErrorCode).Append(',');
            AppendOptionalString(builder, "message", response.Message);
            builder.Append('}');
            return builder.ToString();
        }

        private static IReadOnlyList<RunRewardLineDto> ParseRewardLines(string json)
        {
            var match = Regex.Match(json, "\\\"rewardLines\\\"\\s*:\\s*\\[(.*?)\\]\\s*(?:,|})", RegexOptions.Singleline);
            if (!match.Success)
            {
                return new List<RunRewardLineDto>().AsReadOnly();
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

        private static RunRewardClaimResultStatus ParseClaimResultStatus(string value)
        {
            switch (value)
            {
                case "CLAIMED":
                    return RunRewardClaimResultStatus.Claimed;
                case "DUPLICATE_REPLAY":
                    return RunRewardClaimResultStatus.DuplicateReplay;
                case "REJECTED":
                    return RunRewardClaimResultStatus.Rejected;
                default:
                    throw new ArgumentException("Unsupported reward claim result status.", nameof(value));
            }
        }

        private static string ClaimResultStatusToServerValue(RunRewardClaimResultStatus status)
        {
            switch (status)
            {
                case RunRewardClaimResultStatus.Claimed:
                    return "CLAIMED";
                case RunRewardClaimResultStatus.DuplicateReplay:
                    return "DUPLICATE_REPLAY";
                case RunRewardClaimResultStatus.Rejected:
                    return "REJECTED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported reward claim result status.");
            }
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

        private static StringBuilder AppendRewardLines(StringBuilder builder, IReadOnlyList<RunRewardLineDto> rewardLines)
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
            return builder;
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
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
