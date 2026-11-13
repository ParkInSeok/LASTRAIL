using System.IO;
using Newtonsoft.Json;

namespace LastRail.Run
{
    public static class RunApiErrorJsonAdapter
    {
        public static string OptionalString(string json, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrEmpty(fieldName)) return null;
            // Mono's shared Regex runner cache retains its last input. Error
            // classification must not keep a complete receipt alive after handoff.
            // This is a best-effort top-level field probe, not response validation;
            // each response owner still validates its full contract.
            try
            {
                using (var text = new StringReader(json))
                using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None, MaxDepth = 32 })
                {
                    if (!reader.Read() || reader.TokenType != JsonToken.StartObject) return null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.EndObject && reader.Depth == 0) return null;
                        if (reader.TokenType != JsonToken.PropertyName || reader.Depth != 1) continue;
                        bool requested = (string)reader.Value == fieldName;
                        if (!reader.Read()) return null;
                        if (requested) return reader.TokenType == JsonToken.String ? (string)reader.Value : null;
                        reader.Skip();
                    }
                }
            }
            catch (JsonException) { return null; }
            return null;
        }
    }
}
