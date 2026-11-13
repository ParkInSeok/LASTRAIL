using System;

namespace LastRail.Auth
{
    public sealed class GameApiSecureTokenStorageUxState
    {
        private GameApiSecureTokenStorageUxState(
            GameApiSecureTokenStorageUxStateKind kind,
            string errorCode,
            string message,
            bool canRetryStorage,
            bool requiresGuestReauthOnNextLaunch,
            bool usesInsecureTokenStorage)
        {
            Kind = kind;
            ErrorCode = Optional(errorCode);
            Message = Optional(message);
            CanRetryStorage = canRetryStorage;
            RequiresGuestReauthOnNextLaunch = requiresGuestReauthOnNextLaunch;
            UsesInsecureTokenStorage = usesInsecureTokenStorage;
        }

        public GameApiSecureTokenStorageUxStateKind Kind { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool CanRetryStorage { get; }
        public bool RequiresGuestReauthOnNextLaunch { get; }
        public bool IsBlockingCurrentPresentation => false;
        public bool UsesInsecureTokenStorage { get; }
        public bool HasRawTokenDebugPayload => false;
        public bool HasAccountShortcut => false;

        public static GameApiSecureTokenStorageUxState FromAvailability(AndroidKeystoreAuthTokenStoreReadiness readiness)
        {
            if (readiness == null)
            {
                throw new ArgumentNullException(nameof(readiness));
            }

            return readiness.IsSecureStorageAvailable
                ? new GameApiSecureTokenStorageUxState(
                    GameApiSecureTokenStorageUxStateKind.Ready,
                    null,
                    null,
                    false,
                    false,
                    readiness.UsesInsecureTokenStorage)
                : new GameApiSecureTokenStorageUxState(
                    GameApiSecureTokenStorageUxStateKind.StorageUnavailable,
                    readiness.ErrorCode ?? "secure_storage_unavailable",
                    readiness.Message,
                    true,
                    true,
                    readiness.UsesInsecureTokenStorage);
        }

        public static GameApiSecureTokenStorageUxState FromSaveResult(
            GameApiAuthTokenStoreResult result,
            bool isRefreshRotation,
            string errorCode)
        {
            if (result == GameApiAuthTokenStoreResult.Saved)
            {
                return new GameApiSecureTokenStorageUxState(
                    GameApiSecureTokenStorageUxStateKind.Ready,
                    null,
                    null,
                    false,
                    false,
                    false);
            }

            return new GameApiSecureTokenStorageUxState(
                isRefreshRotation
                    ? GameApiSecureTokenStorageUxStateKind.RefreshWriteFailed
                    : GameApiSecureTokenStorageUxStateKind.SaveFailed,
                string.IsNullOrWhiteSpace(errorCode) ? "secure_storage_write_failed" : errorCode,
                null,
                true,
                true,
                false);
        }

        public static GameApiSecureTokenStorageUxState Cleared()
        {
            return new GameApiSecureTokenStorageUxState(
                GameApiSecureTokenStorageUxStateKind.Cleared,
                null,
                null,
                false,
                false,
                false);
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
