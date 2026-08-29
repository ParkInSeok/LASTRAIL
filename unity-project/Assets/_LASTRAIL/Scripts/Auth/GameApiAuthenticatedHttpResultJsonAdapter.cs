using System.Text;

namespace LastRail.Auth
{
    public static class GameApiAuthenticatedHttpResultJsonAdapter
    {
        public static string ToJson(GameApiAuthenticatedHttpResult result)
        {
            if (result == null)
            {
                throw new System.ArgumentNullException(nameof(result));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "outcome", result.Outcome.ToString()).Append(',');
            builder.Append("\"statusCode\":").Append(result.Response == null ? 0 : result.Response.StatusCode).Append(',');
            AppendOptionalString(builder, "errorCode", result.ErrorCode).Append(',');
            AppendOptionalString(builder, "errorMessage", result.ErrorMessage).Append(',');
            builder.Append("\"requiresAuthRefreshOrGuestLogin\":").Append(result.RequiresAuthRefreshOrGuestLogin ? "true" : "false").Append(',');
            builder.Append("\"canRetryNetwork\":").Append(result.CanRetryNetwork ? "true" : "false").Append(',');
            builder.Append("\"hasRawTokenPayload\":false,");
            builder.Append("\"hasAccountShortcut\":false,");
            builder.Append("\"hasRewardCalculation\":false,");
            builder.Append("\"hasEconomyMutation\":false,");
            builder.Append("\"hasClientSideRng\":false");
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(value.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
        }
    }
}
