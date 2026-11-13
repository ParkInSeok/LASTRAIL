using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LastRail.OfflineRewards
{
    public static class OfflineRewardPreviewJsonAdapter
    {
        private static readonly string[] AcceptedUtcDateTimeFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"
        };

        public static OfflineRewardPreviewDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new OfflineRewardPreviewDto(
                RequiredBool(json, "accepted"),
                ParseStatus(RequiredString(json, "previewStatus")),
                RequiredString(json, "storyProgressId"),
                RequiredInt(json, "claimableSeconds"),
                RequiredDouble(json, "claimableHours"),
                RequiredInt(json, "offlineStorageCapHours"),
                RequiredInt(json, "minimumClaimSeconds"),
                RequiredInt(json, "maxClaimableHours"),
                ParseRewardLines(json),
                ParseCalculation(json),
                RequiredString(json, "narrativeVersion"),
                RequiredUtcDateTime(json, "serverCalculatedAt"),
                OptionalString(json, "errorCode"),
                OptionalString(json, "message"));
        }

        private static OfflineRewardPreviewCalculationDto ParseCalculation(string json)
        {
            var objectJson = RequiredObject(json, "calculation");
            return new OfflineRewardPreviewCalculationDto(
                RequiredString(objectJson, "storyProgressId"),
                RequiredString(objectJson, "storyProgressMultiplierLabel"),
                RequiredInt(objectJson, "cappedOfflineSeconds"),
                RequiredString(objectJson, "capSource"));
        }

        private static IReadOnlyList<OfflineRewardLineDto> ParseRewardLines(string json)
        {
            var match = Regex.Match(json, "\\\"rewardLines\\\"\\s*:\\s*\\[(.*?)\\]\\s*,\\s*\\\"calculation\\\"", RegexOptions.Singleline);
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON array field: rewardLines");
            }

            var lines = new List<OfflineRewardLineDto>();
            foreach (Match objectMatch in Regex.Matches(match.Groups[1].Value, "\\{([^{}]*)\\}"))
            {
                var objectJson = "{" + objectMatch.Groups[1].Value + "}";
                lines.Add(new OfflineRewardLineDto(
                    RequiredString(objectJson, "rewardLineId"),
                    RequiredString(objectJson, "rewardCode"),
                    RequiredString(objectJson, "displayName"),
                    RequiredInt(objectJson, "quantity"),
                    RequiredString(objectJson, "unitLabel"),
                    RequiredString(objectJson, "source")));
            }

            return lines.AsReadOnly();
        }

        private static OfflineRewardPreviewStatus ParseStatus(string value)
        {
            switch (value)
            {
                case "AVAILABLE":
                    return OfflineRewardPreviewStatus.Available;
                case "EMPTY":
                    return OfflineRewardPreviewStatus.Empty;
                case "REJECTED":
                    return OfflineRewardPreviewStatus.Rejected;
                default:
                    throw new ArgumentException("Unsupported offline reward preview status.", nameof(value));
            }
        }

        public static string StatusToServerValue(OfflineRewardPreviewStatus status)
        {
            switch (status)
            {
                case OfflineRewardPreviewStatus.Available:
                    return "AVAILABLE";
                case OfflineRewardPreviewStatus.Empty:
                    return "EMPTY";
                case OfflineRewardPreviewStatus.Rejected:
                    return "REJECTED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported offline reward preview status.");
            }
        }

        private static string RequiredObject(string json, string fieldName)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\{([^{}]*)\\}", RegexOptions.Singleline);
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON object field: " + fieldName);
            }

            return "{" + match.Groups[1].Value + "}";
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

        private static string RequiredString(string json, string fieldName)
        {
            var value = OptionalString(json, fieldName);
            if (string.IsNullOrWhiteSpace(value))
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

        private static double RequiredDouble(string json, string fieldName)
        {
            var match = Regex.Match(json, FieldPattern(fieldName, "-?\\d+(?:\\.\\d+)?"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON number field: " + fieldName);
            }

            return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static DateTime RequiredUtcDateTime(string json, string fieldName)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(RequiredString(json, fieldName), AcceptedUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None),
                DateTimeKind.Utc);
        }

        private static string FieldPattern(string fieldName, string valuePattern)
        {
            return "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*(" + valuePattern + ")";
        }

        private static string StringFieldPattern(string fieldName)
        {
            return "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\"\\\\])*)\\\"";
        }

        private static string Unescape(string value)
        {
            return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
