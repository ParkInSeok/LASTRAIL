using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LastRail.Gacha
{
    public static class GachaPullResponseJsonAdapter
    {
        private static readonly string[] AcceptedUtcDateTimeFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"
        };

        public static GachaPullResponseDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be empty.", nameof(json));
            }

            return new GachaPullResponseDto(
                RequiredString(json, "bannerId"),
                RequiredBool(json, "accepted"),
                OptionalString(json, "batchId"),
                ParseResultStatus(RequiredString(json, "resultStatus")),
                GachaPullCount.FromValue(RequiredInt(json, "pullCount")),
                RequiredString(json, "balanceVersion"),
                ParseCost(RequiredObject(json, "cost")),
                ParsePostPityState(RequiredObject(json, "postPityState")),
                ParseResults(json),
                RequiredUtcDateTime(json, "createdAt"),
                OptionalString(json, "errorCode"),
                OptionalString(json, "message"));
        }

        private static GachaPullCostDto ParseCost(string json)
        {
            return new GachaPullCostDto(
                RequiredString(json, "itemId"),
                RequiredInt(json, "quantity"));
        }

        private static GachaPostPityStateDto ParsePostPityState(string json)
        {
            return new GachaPostPityStateDto(
                RequiredInt(json, "totalPullCount"),
                RequiredInt(json, "elitePityCount"),
                RequiredInt(json, "pickupElitePityCount"),
                RequiredInt(json, "rareOrHigherPityCount"),
                RequiredInt(json, "revision"));
        }

        private static IReadOnlyList<GachaPullResultDto> ParseResults(string json)
        {
            var arrayBody = RequiredArray(json, "results");
            var results = new List<GachaPullResultDto>();
            foreach (var objectJson in SplitTopLevelObjects(arrayBody))
            {
                results.Add(new GachaPullResultDto(
                    RequiredInt(objectJson, "pullSequence"),
                    ParseRarity(RequiredString(objectJson, "resultRarity")),
                    ParseResultKind(RequiredString(objectJson, "resultType")),
                    RequiredString(objectJson, "resultId"),
                    ParseReward(RequiredObject(objectJson, "reward")),
                    RequiredBool(objectJson, "isPickup"),
                    OptionalString(objectJson, "guaranteeType"),
                    RequiredBool(objectJson, "wasDuplicate"),
                    ParseOptionalDuplicateConversion(objectJson)));
            }

            return results.AsReadOnly();
        }

        private static GachaRewardDto ParseReward(string json)
        {
            return new GachaRewardDto(
                RequiredString(json, "kind"),
                RequiredString(json, "id"),
                RequiredInt(json, "quantity"));
        }

        private static GachaDuplicateConversionDto ParseOptionalDuplicateConversion(string json)
        {
            if (Regex.IsMatch(json, FieldPattern("duplicateConversion", "null")))
            {
                return null;
            }

            var objectJson = RequiredObject(json, "duplicateConversion");
            return new GachaDuplicateConversionDto(
                RequiredString(objectJson, "sourceId"),
                RequiredString(objectJson, "itemId"),
                RequiredInt(objectJson, "quantity"));
        }

        private static GachaPullResultStatus ParseResultStatus(string value)
        {
            switch (value)
            {
                case "ACCEPTED":
                    return GachaPullResultStatus.Accepted;
                case "DUPLICATE_REPLAY":
                    return GachaPullResultStatus.DuplicateReplay;
                case "REJECTED":
                    return GachaPullResultStatus.Rejected;
                default:
                    throw new ArgumentException("Unsupported gacha pull result status.", nameof(value));
            }
        }

        private static GachaRarity ParseRarity(string value)
        {
            switch (value)
            {
                case "COMMON":
                    return GachaRarity.Common;
                case "RARE":
                    return GachaRarity.Rare;
                case "ELITE":
                    return GachaRarity.Elite;
                default:
                    throw new ArgumentException("Unsupported gacha rarity.", nameof(value));
            }
        }

        private static GachaPullResultKind ParseResultKind(string value)
        {
            switch (value)
            {
                case "OPERATOR":
                    return GachaPullResultKind.Operator;
                case "OPERATOR_TOKEN":
                    return GachaPullResultKind.OperatorToken;
                case "ITEM":
                    return GachaPullResultKind.Item;
                case "CURRENCY":
                    return GachaPullResultKind.Currency;
                default:
                    throw new ArgumentException("Unsupported gacha pull result type.", nameof(value));
            }
        }

        private static string RequiredObject(string json, string fieldName)
        {
            var fieldIndex = FindFieldValueStart(json, fieldName);
            if (fieldIndex < 0 || fieldIndex >= json.Length || json[fieldIndex] != '{')
            {
                throw new InvalidOperationException("Missing required JSON object field: " + fieldName);
            }

            return ReadBalanced(json, fieldIndex, '{', '}');
        }

        private static string RequiredArray(string json, string fieldName)
        {
            var fieldIndex = FindFieldValueStart(json, fieldName);
            if (fieldIndex < 0 || fieldIndex >= json.Length || json[fieldIndex] != '[')
            {
                throw new InvalidOperationException("Missing required JSON array field: " + fieldName);
            }

            var value = ReadBalanced(json, fieldIndex, '[', ']');
            return value.Substring(1, value.Length - 2);
        }

        private static IEnumerable<string> SplitTopLevelObjects(string arrayBody)
        {
            var objects = new List<string>();
            var index = 0;
            while (index < arrayBody.Length)
            {
                while (index < arrayBody.Length && (char.IsWhiteSpace(arrayBody[index]) || arrayBody[index] == ','))
                {
                    index++;
                }

                if (index >= arrayBody.Length)
                {
                    break;
                }

                if (arrayBody[index] != '{')
                {
                    throw new InvalidOperationException("Expected JSON object in results array.");
                }

                var objectJson = ReadBalanced(arrayBody, index, '{', '}');
                objects.Add(objectJson);
                index += objectJson.Length;
            }

            return objects;
        }

        private static int FindFieldValueStart(string json, string fieldName)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:");
            if (!match.Success)
            {
                return -1;
            }

            var index = match.Index + match.Length;
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }

            return index;
        }

        private static string ReadBalanced(string json, int startIndex, char open, char close)
        {
            var depth = 0;
            var inString = false;
            var escaping = false;
            for (var i = startIndex; i < json.Length; i++)
            {
                var c = json[i];
                if (inString)
                {
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (c == '\\')
                    {
                        escaping = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == open)
                {
                    depth++;
                }
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return json.Substring(startIndex, i - startIndex + 1);
                    }
                }
            }

            throw new InvalidOperationException("Unbalanced JSON value.");
        }

        private static string RequiredString(string json, string fieldName)
        {
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

            var match = Regex.Match(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\"\\\\])*)\\\"");
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

        private static bool RequiredBool(string json, string fieldName)
        {
            var match = Regex.Match(json, FieldPattern(fieldName, "true|false"));
            if (!match.Success)
            {
                throw new InvalidOperationException("Missing required JSON bool field: " + fieldName);
            }

            return bool.Parse(match.Groups[1].Value);
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

        private static string Unescape(string value)
        {
            return value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
