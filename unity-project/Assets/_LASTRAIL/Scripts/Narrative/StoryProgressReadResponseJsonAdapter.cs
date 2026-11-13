using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LastRail.Narrative
{
    public static class StoryProgressReadResponseJsonAdapter
    {
        public static StoryProgressReadResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new StoryProgressReadResponseDto(
                RequiredString(json, "actId"),
                OptionalString(json, "highestStoryProgressId"),
                OptionalString(json, "highestClearedOperationId"),
                RequiredInt(json, "order"),
                RequiredStringArray(json, "clearedOperationIds"),
                RequiredStringArray(json, "unlockedOperations"),
                RequiredString(json, "narrativeVersion"),
                RequiredString(json, "contentStatus"),
                RequiredString(json, "localizationStatus"),
                RequiredString(json, "fixtureStatus"),
                RequiredString(json, "missingContentStatus"));
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
            var match = Regex.Match(json, FieldPattern(fieldName, "-?[0-9]+"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON int field: " + fieldName);
            }

            return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static IReadOnlyList<string> RequiredStringArray(string json, string fieldName)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON array field: " + fieldName);
            }

            var values = new List<string>();
            var itemMatches = Regex.Matches(match.Groups[1].Value, "\\\"((?:\\\\.|[^\"\\\\])*)\\\"");
            foreach (Match item in itemMatches)
            {
                values.Add(Unescape(item.Groups[1].Value));
            }

            return values.AsReadOnly();
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
