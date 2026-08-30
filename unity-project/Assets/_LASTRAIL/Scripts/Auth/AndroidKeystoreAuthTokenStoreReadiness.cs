using System;

namespace LastRail.Auth
{
    public sealed class AndroidKeystoreAuthTokenStoreReadiness
    {
        private AndroidKeystoreAuthTokenStoreReadiness(
            bool isSecureStorageAvailable,
            string providerName,
            string errorCode,
            string message)
        {
            IsSecureStorageAvailable = isSecureStorageAvailable;
            ProviderName = Optional(providerName);
            ErrorCode = Optional(errorCode);
            Message = Optional(message);
        }

        public string StoreImplementationName => "AndroidKeystoreGameApiAuthTokenStore";
        public string NativeBridgePortName => "IAndroidKeystoreAuthTokenBridge";
        public string StorageStrategy => "AndroidKeystoreFirstPartyBridge";
        public bool IsSecureStorageAvailable { get; }
        public string ProviderName { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool RequiresPluginApproval => false;
        public bool RequiresAndroidDeviceValidation => !IsSecureStorageAvailable;
        public string BlockerCode => RequiresAndroidDeviceValidation ? "android_keystore_device_validation_pending" : null;
        public bool UsesInsecureTokenStorage => false;
        public bool HasRawTokenDebugPayload => false;
        public bool HasAccountShortcut => false;

        public static AndroidKeystoreAuthTokenStoreReadiness FromBridgeAvailability(AndroidKeystoreBridgeAvailability availability)
        {
            if (availability == null)
            {
                throw new ArgumentNullException(nameof(availability));
            }

            return new AndroidKeystoreAuthTokenStoreReadiness(
                availability.IsAvailable,
                availability.ProviderName,
                availability.ErrorCode,
                availability.Message);
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
