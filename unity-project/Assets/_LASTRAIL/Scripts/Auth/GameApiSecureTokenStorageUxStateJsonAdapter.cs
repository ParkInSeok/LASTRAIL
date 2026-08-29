using System;
using System.Text;

namespace LastRail.Auth
{
    public static class GameApiSecureTokenStorageUxStateJsonAdapter
    {
        public static string ToJson(GameApiSecureTokenStorageUxState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "kind", state.Kind.ToString()).Append(',');
            AppendString(builder, "errorCode", state.ErrorCode).Append(',');
            AppendString(builder, "message", state.Message).Append(',');
            AppendBool(builder, "canRetryStorage", state.CanRetryStorage).Append(',');
            AppendBool(builder, "requiresGuestReauthOnNextLaunch", state.RequiresGuestReauthOnNextLaunch).Append(',');
            AppendBool(builder, "isBlockingCurrentPresentation", state.IsBlockingCurrentPresentation).Append(',');
            AppendBool(builder, "usesInsecureTokenStorage", state.UsesInsecureTokenStorage).Append(',');
            AppendBool(builder, "hasRawTokenDebugPayload", state.HasRawTokenDebugPayload).Append(',');
            AppendBool(builder, "hasAccountShortcut", state.HasAccountShortcut);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            if (value == null)
            {
                return builder.Append("null");
            }

            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
