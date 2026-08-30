using System;

namespace LastRail.Auth
{
    public sealed class AndroidKeystoreDeviceValidationReadiness
    {
        private AndroidKeystoreDeviceValidationReadiness(AndroidKeystoreBridgeAvailability availability)
        {
            IsSecureStorageAvailable = availability.IsAvailable;
            ProviderName = availability.ProviderName;
            ErrorCode = availability.ErrorCode;
            Message = availability.Message;
        }

        public string StorageStrategy => "AndroidKeystoreFirstPartyBridge";
        public string NativeBridgePortName => nameof(IAndroidKeystoreNativeStoragePort);
        public bool IsSecureStorageAvailable { get; }
        public string ProviderName { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool RequiresPluginApproval => false;
        public bool RequiresAndroidDeviceValidation => !IsSecureStorageAvailable;
        public string BlockerCode => RequiresAndroidDeviceValidation ? "android_keystore_device_validation_pending" : null;
        public bool CanRetryStorage => !IsSecureStorageAvailable;
        public bool RequiresGuestReauthOnNextLaunch => !IsSecureStorageAvailable;
        public bool UsesInsecureTokenStorage => false;
        public bool HasRawTokenDebugPayload => false;
        public bool HasAccountShortcut => false;
        public bool HasRawDeviceIdentifier => false;
        public bool IsBlockingCurrentPresentation => false;

        public static AndroidKeystoreDeviceValidationReadiness FromBridgeAvailability(AndroidKeystoreBridgeAvailability availability)
        {
            if (availability == null)
            {
                throw new ArgumentNullException(nameof(availability));
            }

            return new AndroidKeystoreDeviceValidationReadiness(availability);
        }
    }
}
