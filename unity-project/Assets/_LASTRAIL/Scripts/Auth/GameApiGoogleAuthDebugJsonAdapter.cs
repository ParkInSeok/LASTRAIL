using System;

namespace LastRail.Auth
{
    public static class GameApiGoogleAuthDebugJsonAdapter
    {
        public static string ToJson(GameApiGoogleAuthClientResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return "{"
                + "\"flow\":\"googleAuth\","
                + "\"outcome\":\"" + result.Outcome + "\","
                + "\"linkStatus\":null,"
                + "\"errorCode\":" + OptionalString(result.ErrorCode) + ","
                + "\"message\":" + OptionalString(result.Message) + ","
                + "\"canRetryNetwork\":" + Bool(result.CanRetryNetwork) + ","
                + "\"requiresAuthRefreshOrGuestLogin\":" + Bool(result.RequiresAuthRefreshOrGuestLogin) + ","
                + "\"requiresAccountRecovery\":" + Bool(result.RequiresAccountRecovery)
                + "}";
        }

        public static string ToJson(GameApiGoogleLinkClientResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return "{"
                + "\"flow\":\"googleLink\","
                + "\"outcome\":\"" + result.Outcome + "\","
                + "\"linkStatus\":" + OptionalLinkStatus(result.LinkStatus) + ","
                + "\"errorCode\":" + OptionalString(result.ErrorCode) + ","
                + "\"message\":" + OptionalString(result.Message) + ","
                + "\"canRetryNetwork\":" + Bool(result.CanRetryNetwork) + ","
                + "\"requiresAuthRefreshOrGuestLogin\":" + Bool(result.RequiresAuthRefreshOrGuestLogin) + ","
                + "\"requiresAccountRecovery\":" + Bool(result.RequiresAccountRecovery)
                + "}";
        }

        private static string OptionalLinkStatus(GameApiGoogleLinkStatus? status)
        {
            return status.HasValue ? "\"" + GameApiGoogleLinkStatusParser.ToServerString(status.Value) + "\"" : "null";
        }

        private static string OptionalString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "null" : "\"" + Escape(value) + "\"";
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
