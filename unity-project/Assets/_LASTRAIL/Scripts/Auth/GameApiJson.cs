using System;
using System.Linq;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Auth
{
    public static class GameApiJson
    {
        public static JObject ReadObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 262144) throw new FormatException("Invalid API JSON size.");
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, MaxDepth = 32 })
            {
                var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
                if (reader.Read() || root.Descendants().Any(token => token.Type == JTokenType.Comment))
                    throw new FormatException("Invalid API JSON content.");
                return root;
            }
        }

        public static string String(JObject root, string field, bool optional = false)
        {
            var value = root[field];
            if (optional && (value == null || value.Type == JTokenType.Null)) return null;
            if (value == null || value.Type != JTokenType.String || (!optional && string.IsNullOrWhiteSpace((string)value)))
                throw new FormatException("Invalid API string field.");
            return (string)value;
        }

        public static JObject Object(JObject root, string field)
        {
            if (!(root[field] is JObject value)) throw new FormatException("Invalid API object field.");
            return value;
        }

        public static bool Boolean(JObject root, string field)
        {
            if (root[field]?.Type != JTokenType.Boolean) throw new FormatException("Invalid API boolean field.");
            return (bool)root[field];
        }

        public static string UtcText(DateTimeOffset value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        public static DateTimeOffset UtcTimestamp(JObject root, string field) => DateTimeOffset.ParseExact(String(root, field),
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        public static int RetryDelaySeconds(JObject root)
        {
            if (root["retryAfterSeconds"]?.Type != JTokenType.Integer) return 600;
            var value = (long)root["retryAfterSeconds"];
            return value > 0 && value <= 86400 ? (int)value : 600;
        }
    }
}
