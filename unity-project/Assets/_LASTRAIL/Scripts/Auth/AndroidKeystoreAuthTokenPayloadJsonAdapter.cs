using System;
using System.Globalization;
using System.Text;

namespace LastRail.Auth
{
    public static class AndroidKeystoreAuthTokenPayloadJsonAdapter
    {
        public static string ToJson(AndroidKeystoreAuthTokenPayload payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "accountId", payload.AccountId).Append(',');
            AppendString(builder, "accountStatus", payload.AccountStatus).Append(',');
            AppendString(builder, "accessTokenValue", payload.AccessTokenValue).Append(',');
            AppendString(builder, "accessTokenExpiresAt", payload.AccessTokenExpiresAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "refreshTokenValue", payload.RefreshTokenValue).Append(',');
            AppendString(builder, "refreshTokenExpiresAt", payload.RefreshTokenExpiresAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            builder.Append('}');
            return builder.ToString();
        }

        public static AndroidKeystoreAuthTokenPayload FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return new AndroidKeystoreAuthTokenPayload(
                ExtractString(json, "accountId"),
                ExtractString(json, "accountStatus"),
                ExtractString(json, "accessTokenValue"),
                ParseUtc(ExtractString(json, "accessTokenExpiresAt"), "accessTokenExpiresAt"),
                ExtractString(json, "refreshTokenValue"),
                ParseUtc(ExtractString(json, "refreshTokenExpiresAt"), "refreshTokenExpiresAt"));
        }

        private static DateTime ParseUtc(string value, string fieldName)
        {
            if (!DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                throw new FormatException("Invalid UTC timestamp: " + fieldName);
            }

            return DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc);
        }

        private static string ExtractString(string json, string fieldName)
        {
            var marker = "\"" + fieldName + "\":";
            var index = json.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new FormatException("Missing field: " + fieldName);
            }

            index += marker.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }

            if (index >= json.Length || json[index] != '"')
            {
                throw new FormatException("Expected string field: " + fieldName);
            }

            index++;
            var builder = new StringBuilder();
            while (index < json.Length)
            {
                var character = json[index++];
                if (character == '"')
                {
                    return builder.ToString();
                }

                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }

                if (index >= json.Length)
                {
                    throw new FormatException("Invalid escape in field: " + fieldName);
                }

                var escaped = json[index++];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escaped);
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    default:
                        throw new FormatException("Unsupported escape in field: " + fieldName);
                }
            }

            throw new FormatException("Unterminated string field: " + fieldName);
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
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
