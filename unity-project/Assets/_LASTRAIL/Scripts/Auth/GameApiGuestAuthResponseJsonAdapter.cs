using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

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

            var root = GameApiJson.ReadObject(json);
            var account = GameApiJson.Object(root, "account");
            return new GameApiGuestAuthResponseDto(
                ParseTokenType(GameApiJson.String(root, "tokenType")),
                new GameApiAccountDto(GameApiJson.String(account, "id"), GameApiJson.String(account, "status")),
                new GameApiAccessToken(GameApiJson.String(root, "accessToken")),
                RequiredUtcDateTime(root, "accessTokenExpiresAt"),
                new GameApiRefreshToken(GameApiJson.String(root, "refreshToken")),
                RequiredUtcDateTime(root, "refreshTokenExpiresAt"));
        }

        private static GameApiTokenType ParseTokenType(string value)
        {
            if (value == "Bearer")
            {
                return GameApiTokenType.Bearer;
            }

            throw new ArgumentException("Unsupported token type.", nameof(value));
        }

        private static DateTime RequiredUtcDateTime(JObject json, string fieldName)
        {
            return DateTime.SpecifyKind(
                DateTime.ParseExact(GameApiJson.String(json, fieldName), AcceptedUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None),
                DateTimeKind.Utc);
        }

    }
}
