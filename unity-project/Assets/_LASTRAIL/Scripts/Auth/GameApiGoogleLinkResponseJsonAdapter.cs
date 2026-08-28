using System;
using System.Text.RegularExpressions;

namespace LastRail.Auth
{
    public static class GameApiGoogleLinkResponseJsonAdapter
    {
        public static GameApiGoogleLinkResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new GameApiGoogleLinkResponseDto(
                GameApiGuestAuthResponseJsonAdapter.FromJson(json),
                GameApiGoogleLinkStatusParser.Parse(RequiredString(json, "linkStatus")));
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
