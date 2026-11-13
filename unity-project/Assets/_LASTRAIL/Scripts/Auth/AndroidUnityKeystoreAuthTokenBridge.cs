using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public sealed class AndroidUnityKeystoreAuthTokenBridge : IAndroidKeystoreAuthTokenBridge, IGameApiAuthTokenReader
    {
        private const string ProviderFallback = "android_keystore_first_party_bridge";
        private readonly IAndroidKeystoreNativeStoragePort nativePort;

        public AndroidUnityKeystoreAuthTokenBridge(IAndroidKeystoreNativeStoragePort nativePort)
        {
            this.nativePort = nativePort ?? throw new ArgumentNullException(nameof(nativePort));
        }

        public Task<AndroidKeystoreBridgeAvailability> CheckAvailabilityAsync()
        {
            if (!nativePort.IsAndroidRuntime)
            {
                return Task.FromResult(AndroidKeystoreBridgeAvailability.Unavailable(
                    "android_keystore_android_device_required",
                    "Android Keystore token storage requires an Android runtime/device validation pass."));
            }

            if (!nativePort.IsKeystoreAvailable)
            {
                return Task.FromResult(AndroidKeystoreBridgeAvailability.Unavailable(
                    "android_keystore_unavailable",
                    "Android Keystore is unavailable on this runtime."));
            }

            return Task.FromResult(AndroidKeystoreBridgeAvailability.Available(
                string.IsNullOrWhiteSpace(nativePort.ProviderName) ? ProviderFallback : nativePort.ProviderName));
        }

        public Task<AndroidKeystoreBridgeWriteResult> SaveAsync(AndroidKeystoreAuthTokenPayload value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            try
            {
                var encrypted = nativePort.Encrypt(AndroidKeystoreAuthTokenPayloadJsonAdapter.ToJson(value));
                return Task.FromResult(nativePort.Save(encrypted)
                    ? AndroidKeystoreBridgeWriteResult.Saved()
                    : AndroidKeystoreBridgeWriteResult.Failed("keystore_write_failed"));
            }
            catch
            {
                return Task.FromResult(AndroidKeystoreBridgeWriteResult.Failed("keystore_write_failed"));
            }
        }

        public Task<AndroidKeystoreAuthTokenPayload> LoadAsync()
        {
            try
            {
                var encrypted = nativePort.Load();
                if (encrypted == null)
                {
                    return Task.FromResult<AndroidKeystoreAuthTokenPayload>(null);
                }

                var plaintext = nativePort.Decrypt(encrypted);
                return Task.FromResult(AndroidKeystoreAuthTokenPayloadJsonAdapter.FromJson(plaintext));
            }
            catch
            {
                return Task.FromResult<AndroidKeystoreAuthTokenPayload>(null);
            }
        }

        public Task<GameApiAuthTokenReadResult> ReadAsync()
        {
            GameApiAuthTokenReadResult Result(GameApiAuthTokenReadOutcome value) => new GameApiAuthTokenReadResult(value);
            try
            {
                if (!nativePort.IsAndroidRuntime || !nativePort.IsKeystoreAvailable)
                    return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Unavailable));
            }
            catch { return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Unavailable)); }

            AndroidKeystoreEncryptedTokenPayload encrypted;
            try { encrypted = nativePort.Load(); }
            catch (FormatException) { return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Corrupt)); }
            catch (ArgumentException) { return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Corrupt)); }
            catch { return Task.FromResult(Result(GameApiAuthTokenReadOutcome.ReadError)); }
            if (encrypted == null) return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Missing));
            try
            {
                var plaintext = nativePort.Decrypt(encrypted);
                var payload = AndroidKeystoreAuthTokenPayloadJsonAdapter.FromJson(plaintext);
                if (payload == null) return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Corrupt));
                return Task.FromResult(new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Loaded, payload.ToSnapshot()));
            }
            catch { return Task.FromResult(Result(GameApiAuthTokenReadOutcome.Corrupt)); }
        }

        public Task<AndroidKeystoreBridgeClearResult> ClearAsync()
        {
            try
            {
                return Task.FromResult(nativePort.Clear()
                    ? AndroidKeystoreBridgeClearResult.Cleared()
                    : AndroidKeystoreBridgeClearResult.Failed("keystore_clear_failed"));
            }
            catch
            {
                return Task.FromResult(AndroidKeystoreBridgeClearResult.Failed("keystore_clear_failed"));
            }
        }
    }
}
