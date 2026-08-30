using System;
using NUnit.Framework;
using LastRail.Auth;

namespace LastRail.Tests.EditMode
{
    public sealed class AndroidKeystoreNativeBridgeImplementationTests
    {
        [Test]
        public void FirstPartyBridge_SaveLoadOverwriteAndClearUseEncryptedAndroidKeystoreStorageWithoutPlayerPrefsFallback()
        {
            var nativePort = new RecordingAndroidKeystoreNativeStoragePort(isAndroidRuntime: true, isKeystoreAvailable: true);
            var bridge = new AndroidUnityKeystoreAuthTokenBridge(nativePort);
            var store = new AndroidKeystoreGameApiAuthTokenStore(bridge);
            var first = Snapshot("lst1.access.first", "refresh_first");
            var second = Snapshot("lst1.access.second", "refresh_second");

            Assert.That(store.SaveAsync(first).Result, Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            Assert.That(nativePort.StoredRecordForAudit.Contains("lst1."), Is.EqualTo(false));
            Assert.That(nativePort.StoredRecordForAudit.Contains("refresh_"), Is.EqualTo(false));

            Assert.That(store.SaveAsync(second).Result, Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            var loaded = store.LoadAsync().Result;

            Assert.That(loaded.AccessToken.Value, Is.EqualTo("lst1.access.second"));
            Assert.That(loaded.RefreshToken.Value, Is.EqualTo("refresh_second"));
            Assert.That(store.ClearAsync().Result, Is.EqualTo(GameApiAuthTokenStoreResult.Cleared));
            Assert.That(store.LoadAsync().Result, Is.EqualTo(null));
            Assert.That(nativePort.UsedPlayerPrefsFallback, Is.EqualTo(false));
        }

        [Test]
        public void FirstPartyBridge_ReadinessReportsAndroidDeviceValidationBlockerUntilKeystoreIsAvailable()
        {
            var notAndroid = new RecordingAndroidKeystoreNativeStoragePort(isAndroidRuntime: false, isKeystoreAvailable: false);
            var bridge = new AndroidUnityKeystoreAuthTokenBridge(notAndroid);

            var readiness = AndroidKeystoreDeviceValidationReadiness.FromBridgeAvailability(
                bridge.CheckAvailabilityAsync().Result);
            var json = AndroidKeystoreDeviceValidationReadinessJsonAdapter.ToJson(readiness);

            Assert.That(readiness.StorageStrategy, Is.EqualTo("AndroidKeystoreFirstPartyBridge"));
            Assert.That(readiness.RequiresPluginApproval, Is.EqualTo(false));
            Assert.That(readiness.RequiresAndroidDeviceValidation, Is.EqualTo(true));
            Assert.That(readiness.BlockerCode, Is.EqualTo("android_keystore_device_validation_pending"));
            Assert.That(json.Contains("lst1."), Is.EqualTo(false));
            Assert.That(json.Contains("refresh_"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("Authorization"), Is.EqualTo(false));
            Assert.That(json.Contains("deviceId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawPayload"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
        }

        [Test]
        public void FirstPartyBridge_WriteFailureAfterRefreshKeepsExistingPayloadAndMapsToReauthUx()
        {
            var nativePort = new RecordingAndroidKeystoreNativeStoragePort(isAndroidRuntime: true, isKeystoreAvailable: true);
            var bridge = new AndroidUnityKeystoreAuthTokenBridge(nativePort);
            var store = new AndroidKeystoreGameApiAuthTokenStore(bridge);

            Assert.That(store.SaveAsync(Snapshot("lst1.access.old", "refresh_old")).Result, Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            nativePort.NextSaveFails = true;

            var result = store.SaveAsync(Snapshot("lst1.access.rotated", "refresh_rotated")).Result;
            var loaded = store.LoadAsync().Result;
            var ux = GameApiSecureTokenStorageUxState.FromSaveResult(result, isRefreshRotation: true, "keystore_write_failed");

            Assert.That(result, Is.EqualTo(GameApiAuthTokenStoreResult.Failed));
            Assert.That(loaded.AccessToken.Value, Is.EqualTo("lst1.access.old"));
            Assert.That(loaded.RefreshToken.Value, Is.EqualTo("refresh_old"));
            Assert.That(ux.Kind, Is.EqualTo(GameApiSecureTokenStorageUxStateKind.RefreshWriteFailed));
            Assert.That(ux.RequiresGuestReauthOnNextLaunch, Is.EqualTo(true));
            Assert.That(ux.CanRetryStorage, Is.EqualTo(true));
            Assert.That(ux.IsBlockingCurrentPresentation, Is.EqualTo(false));
            Assert.That(ux.HasRawTokenDebugPayload, Is.EqualTo(false));
            Assert.That(ux.HasAccountShortcut, Is.EqualTo(false));
        }

        private static GameApiAuthSessionSnapshot Snapshot(string accessToken, string refreshToken)
        {
            return new GameApiAuthSessionSnapshot(
                "acct_guest_secure_001",
                "ACTIVE",
                new GameApiAccessToken(accessToken),
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                new GameApiRefreshToken(refreshToken),
                new DateTime(2026, 9, 30, 1, 0, 0, DateTimeKind.Utc));
        }

        private sealed class RecordingAndroidKeystoreNativeStoragePort : IAndroidKeystoreNativeStoragePort
        {
            private readonly System.Collections.Generic.Dictionary<string, string> encryptedPlaintextMap =
                new System.Collections.Generic.Dictionary<string, string>();
            private string encryptedPayload;

            public RecordingAndroidKeystoreNativeStoragePort(bool isAndroidRuntime, bool isKeystoreAvailable)
            {
                IsAndroidRuntime = isAndroidRuntime;
                IsKeystoreAvailable = isKeystoreAvailable;
            }

            public bool IsAndroidRuntime { get; }
            public bool IsKeystoreAvailable { get; }
            public string ProviderName => "android_keystore_first_party_bridge";
            public bool NextSaveFails { get; set; }
            public bool UsedPlayerPrefsFallback { get; private set; }
            public string StoredRecordForAudit => encryptedPayload ?? string.Empty;

            public AndroidKeystoreEncryptedTokenPayload Encrypt(string plaintext)
            {
                var cipherText = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("encrypted:" + plaintext.Length + ":" + encryptedPlaintextMap.Count));
                var iv = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("iv:" + plaintext.Length + ":" + encryptedPlaintextMap.Count));
                encryptedPlaintextMap[cipherText] = plaintext;
                return new AndroidKeystoreEncryptedTokenPayload(cipherText, iv);
            }

            public string Decrypt(AndroidKeystoreEncryptedTokenPayload encrypted)
            {
                if (encrypted == null)
                {
                    return null;
                }

                return encryptedPlaintextMap.TryGetValue(encrypted.CipherTextBase64, out var plaintext)
                    ? plaintext
                    : null;
            }

            public bool Save(AndroidKeystoreEncryptedTokenPayload encrypted)
            {
                if (NextSaveFails)
                {
                    NextSaveFails = false;
                    return false;
                }

                encryptedPayload = AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(encrypted);
                return true;
            }

            public AndroidKeystoreEncryptedTokenPayload Load()
            {
                return string.IsNullOrEmpty(encryptedPayload)
                    ? null
                    : AndroidKeystoreEncryptedTokenPayloadJsonAdapter.FromJson(encryptedPayload);
            }

            public bool Clear()
            {
                encryptedPayload = null;
                return true;
            }
        }
    }
}
