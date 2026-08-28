using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LastRail.Run
{
    public static class RunStartResponseJsonAdapter
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

        public static RunStartResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new RunStartResponseDto(
                RequiredString(json, "runId"),
                RequiredString(json, "balanceVersion"),
                RequiredString(json, "operationId"),
                RequiredString(json, "actId"),
                RequiredString(json, "regionId"),
                RequiredString(json, "enemyFactionId"),
                RequiredInt(json, "riskLevel"),
                RequiredString(json, "wavePlanId"),
                RequiredUInt(json, "seed"),
                RequiredString(json, "rewardTableId"),
                RequiredUtcDateTime(json, "serverStartedAt"),
                RequiredBool(json, "firstClearEligible"),
                OptionalString(json, "startAugmentPackageRef"),
                OptionalString(json, "startTacticPackageRef"));
        }

        public static string ToJson(RunStartResponseDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", dto.RunId).Append(',');
            AppendString(builder, "balanceVersion", dto.BalanceVersion).Append(',');
            AppendString(builder, "operationId", dto.OperationId).Append(',');
            AppendString(builder, "actId", dto.ActId).Append(',');
            AppendString(builder, "regionId", dto.RegionId).Append(',');
            AppendString(builder, "enemyFactionId", dto.EnemyFactionId).Append(',');
            builder.Append("\"riskLevel\":").Append(dto.RiskLevel.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "wavePlanId", dto.WavePlanId).Append(',');
            builder.Append("\"seed\":").Append(dto.Seed.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "rewardTableId", dto.RewardTableId).Append(',');
            AppendOptionalString(builder, "startAugmentPackageRef", dto.StartAugmentPackageRef).Append(',');
            AppendOptionalString(builder, "startTacticPackageRef", dto.StartTacticPackageRef).Append(',');
            AppendString(builder, "serverStartedAt", dto.ServerStartedAt.ToUniversalTime().ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"firstClearEligible\":").Append(dto.FirstClearEligible ? "true" : "false");
            builder.Append('}');
            return builder.ToString();
        }

        private static string RequiredString(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                throw new InvalidOperationException("Required JSON field is null: " + fieldName);
            }

            var match = Regex.Match(json, StringFieldPattern(fieldName));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON string field: " + fieldName);
            }

            return Unescape(match.Groups[1].Value);
        }

        private static string OptionalString(string json, string fieldName)
        {
            if (Regex.IsMatch(json, FieldPattern(fieldName, "null")))
            {
                return null;
            }

            var match = Regex.Match(json, StringFieldPattern(fieldName));
            if (!match.Success)
            {
                return null;
            }

            var value = Unescape(match.Groups[1].Value);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static int RequiredInt(string json, string fieldName)
        {
            var match = Regex.Match(json, FieldPattern(fieldName, "-?[0-9]+"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON int field: " + fieldName);
            }

            return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static uint RequiredUInt(string json, string fieldName)
        {
            var match = Regex.Match(json, FieldPattern(fieldName, "[0-9]+"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON uint field: " + fieldName);
            }

            return uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
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
