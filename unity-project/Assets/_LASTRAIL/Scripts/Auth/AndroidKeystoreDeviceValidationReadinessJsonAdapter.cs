using System;
using System.Text;

namespace LastRail.Auth
{
    public static class AndroidKeystoreDeviceValidationReadinessJsonAdapter
    {
        public static string ToJson(AndroidKeystoreDeviceValidationReadiness readiness)
        {
            if (readiness == null)
            {
                throw new ArgumentNullException(nameof(readiness));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "storageStrategy", readiness.StorageStrategy).Append(',');
            AppendString(builder, "nativeBridgePortName", readiness.NativeBridgePortName).Append(',');
            AppendBool(builder, "isSecureStorageAvailable", readiness.IsSecureStorageAvailable).Append(',');
            AppendString(builder, "providerName", readiness.ProviderName).Append(',');
            AppendString(builder, "errorCode", readiness.ErrorCode).Append(',');
            AppendString(builder, "blockerCode", readiness.BlockerCode).Append(',');
            AppendBool(builder, "requiresPluginApproval", readiness.RequiresPluginApproval).Append(',');
            AppendBool(builder, "requiresAndroidDeviceValidation", readiness.RequiresAndroidDeviceValidation).Append(',');
            AppendBool(builder, "canRetryStorage", readiness.CanRetryStorage).Append(',');
            AppendBool(builder, "requiresGuestReauthOnNextLaunch", readiness.RequiresGuestReauthOnNextLaunch).Append(',');
            AppendBool(builder, "isBlockingCurrentPresentation", readiness.IsBlockingCurrentPresentation).Append(',');
            AppendBool(builder, "usesInsecureTokenStorage", readiness.UsesInsecureTokenStorage).Append(',');
            AppendBool(builder, "hasRawTokenDebugPayload", readiness.HasRawTokenDebugPayload).Append(',');
            AppendBool(builder, "hasAccountShortcut", readiness.HasAccountShortcut).Append(',');
            AppendBool(builder, "hasRawDeviceIdentifier", readiness.HasRawDeviceIdentifier);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendBool(StringBuilder builder, string fieldName, bool value)
        {
            return builder.Append('"').Append(fieldName).Append("\":").Append(value ? "true" : "false");
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
