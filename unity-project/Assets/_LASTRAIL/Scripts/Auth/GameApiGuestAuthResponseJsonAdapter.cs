using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LastRail.Auth
{
    public static class GameApiGuestAuthResponseJsonAdapter
    {
        private static readonly string[] AcceptedUtcDateTimeFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"
        };

        public static GameApiGuestAuthResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new GameApiGuestAuthResponseDto(
                ParseTokenType(RequiredString(json, "tokenType")),
                ParseAccount(RequiredObject(json, "account")),
                new GameApiAccessToken(RequiredString(json, "accessToken")),
                RequiredUtcDateTime(json, "accessTokenExpiresAt"),
                new GameApiRefreshToken(RequiredString(json, "refreshToken")),
                RequiredUtcDateTime(json, "refreshTokenExpiresAt"));
        }

        private static GameApiAccountDto ParseAccount(string json)
        {
            return new GameApiAccountDto(
                RequiredString(json, "id"),
                RequiredString(json, "status"));
        }

        private static GameApiTokenType ParseTokenType(string value)
        {
            if (value == "Bearer")
            {
                return GameApiTokenType.Bearer;
            }

            throw new ArgumentException("Unsupported token type.", nameof(value));
        }

        private static DateTime RequiredUtcDateTime(string json, string fieldName)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(RequiredString(json, fieldName), AcceptedUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None),
                DateTimeKind.Utc);
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

        private static string RequiredString(string json, string fieldName)
        {
            if (Regex.IsMatch(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*null"))
            {
                throw new InvalidOperationException("Required JSON field is null: " + fieldName);
            }

            var match = Regex.Match(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\"\\\\])*)\\\"");
            if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                throw new InvalidOperationException("Missing required JSON string field: " + fieldName);
            }

            return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
