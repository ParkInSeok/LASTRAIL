using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Auth;

namespace LastRail.Tests.EditMode
{
    public sealed class AndroidKeystoreAuthTokenStoreTests
    {
        [Test]
        public void Readiness_RequiresAndroidKeystoreBridgeAndForbidsPlayerPrefsFallback()
        {
            var readiness = AndroidKeystoreAuthTokenStoreReadiness.FromBridgeAvailability(
                AndroidKeystoreBridgeAvailability.Available("verified_keystore_plugin"));

            Assert.That(readiness.StoreImplementationName, Is.EqualTo("AndroidKeystoreGameApiAuthTokenStore"));
            Assert.That(readiness.NativeBridgePortName, Is.EqualTo("IAndroidKeystoreAuthTokenBridge"));
            Assert.That(readiness.StorageStrategy, Is.EqualTo("AndroidKeystoreFirstPartyBridge"));
            Assert.That(readiness.IsSecureStorageAvailable, Is.EqualTo(true));
            Assert.That(readiness.RequiresPluginApproval, Is.EqualTo(false));
            Assert.That(readiness.RequiresAndroidDeviceValidation, Is.EqualTo(false));
            Assert.That(readiness.UsesInsecureTokenStorage, Is.EqualTo(false));
            Assert.That(readiness.HasRawTokenDebugPayload, Is.EqualTo(false));
            Assert.That(readiness.HasAccountShortcut, Is.EqualTo(false));
        }

        [Test]
        public void Store_SaveLoadOverwriteAndClearUseNativeBridgeWithoutPlayerPrefsFallback()
        {
            var bridge = new InMemoryAndroidKeystoreBridge();
            var store = new AndroidKeystoreGameApiAuthTokenStore(bridge);
            var first = Snapshot("lst1.access.one", "refresh_one");
            var second = Snapshot("lst1.access.two", "refresh_two");

            Assert.That(store.SaveAsync(first).Result, Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            Assert.That(store.LoadAsync().Result.AccessToken.Value, Is.EqualTo("lst1.access.one"));

            Assert.That(store.SaveAsync(second).Result, Is.EqualTo(GameApiAuthTokenStoreResult.Saved));
            Assert.That(store.LoadAsync().Result.AccessToken.Value, Is.EqualTo("lst1.access.two"));
            Assert.That(store.LoadAsync().Result.RefreshToken.Value, Is.EqualTo("refresh_two"));

            Assert.That(store.ClearAsync().Result, Is.EqualTo(GameApiAuthTokenStoreResult.Cleared));
            Assert.That(store.LoadAsync().Result, Is.EqualTo(null));
            Assert.That(bridge.UsedPlayerPrefsFallback, Is.EqualTo(false));
        }

        [Test]
        public void Store_WriteFailureAfterRefreshMapsToFailSoftUxWithoutLeakingTokens()
        {
            var bridge = new InMemoryAndroidKeystoreBridge { NextSaveFails = true };
            var store = new AndroidKeystoreGameApiAuthTokenStore(bridge);

            var result = store.SaveAsync(Snapshot("lst1.access.rotated", "refresh_rotated")).Result;
            var ux = GameApiSecureTokenStorageUxState.FromSaveResult(result, isRefreshRotation: true, "keystore_write_failed");

            Assert.That(result, Is.EqualTo(GameApiAuthTokenStoreResult.Failed));
            Assert.That(ux.Kind, Is.EqualTo(GameApiSecureTokenStorageUxStateKind.RefreshWriteFailed));
            Assert.That(ux.ErrorCode, Is.EqualTo("keystore_write_failed"));
            Assert.That(ux.RequiresGuestReauthOnNextLaunch, Is.EqualTo(true));
            Assert.That(ux.CanRetryStorage, Is.EqualTo(true));
            Assert.That(ux.IsBlockingCurrentPresentation, Is.EqualTo(false));
            Assert.That(ux.HasRawTokenDebugPayload, Is.EqualTo(false));
            Assert.That(ux.HasAccountShortcut, Is.EqualTo(false));
        }

        [Test]
        public void DebugJson_WritesStatusOnlyWithoutRawTokensAccountShortcutsOrAuthPayload()
        {
            var readiness = AndroidKeystoreAuthTokenStoreReadiness.FromBridgeAvailability(
                AndroidKeystoreBridgeAvailability.Unavailable("android_keystore_android_device_required", "Android Keystore device validation pending."));
            var ux = GameApiSecureTokenStorageUxState.FromAvailability(readiness);

            var json = GameApiSecureTokenStorageUxStateJsonAdapter.ToJson(ux);

            Assert.That(json.Contains("\"kind\":\"StorageUnavailable\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"errorCode\":\"android_keystore_android_device_required\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresGuestReauthOnNextLaunch\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesInsecureTokenStorage\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("lst1."), Is.EqualTo(false));
            Assert.That(json.Contains("refresh_"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("Authorization"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("rawAuth"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("economyGrant"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        private static GameApiAuthSessionSnapshot Snapshot(string accessToken, string refreshToken)
        {
            return new GameApiAuthSessionSnapshot(
                "acct_guest_001",
                "ACTIVE",
                new GameApiAccessToken(accessToken),
                new DateTime(2026, 8, 30, 1, 0, 0, DateTimeKind.Utc),
                new GameApiRefreshToken(refreshToken),
                new DateTime(2026, 9, 30, 1, 0, 0, DateTimeKind.Utc));
        }

        private sealed class InMemoryAndroidKeystoreBridge : IAndroidKeystoreAuthTokenBridge
        {
            private AndroidKeystoreAuthTokenPayload payload;

            public bool NextSaveFails { get; set; }
            public bool UsedPlayerPrefsFallback { get; private set; }

            public Task<AndroidKeystoreBridgeAvailability> CheckAvailabilityAsync()
            {
                return Task.FromResult(AndroidKeystoreBridgeAvailability.Available("test_keystore"));
            }

            public Task<AndroidKeystoreBridgeWriteResult> SaveAsync(AndroidKeystoreAuthTokenPayload value)
            {
                if (NextSaveFails)
                {
                    NextSaveFails = false;
                    return Task.FromResult(AndroidKeystoreBridgeWriteResult.Failed("keystore_write_failed"));
                }

                payload = value;
                return Task.FromResult(AndroidKeystoreBridgeWriteResult.Saved());
            }

            public Task<AndroidKeystoreAuthTokenPayload> LoadAsync()
            {
                return Task.FromResult(payload);
            }

            public Task<AndroidKeystoreBridgeClearResult> ClearAsync()
            {
                payload = null;
                return Task.FromResult(AndroidKeystoreBridgeClearResult.Cleared());
            }
        }
    }
}
