using System;
using System.Text;

namespace LastRail.Auth
{
    public static class AndroidKeystoreEncryptedTokenPayloadJsonAdapter
    {
        public static string ToJson(AndroidKeystoreEncryptedTokenPayload payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "cipherTextBase64", payload.CipherTextBase64).Append(',');
            AppendString(builder, "ivBase64", payload.IvBase64);
            builder.Append('}');
            return builder.ToString();
        }

        public static AndroidKeystoreEncryptedTokenPayload FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return new AndroidKeystoreEncryptedTokenPayload(
                ExtractString(json, "cipherTextBase64"),
                ExtractString(json, "ivBase64"));
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
