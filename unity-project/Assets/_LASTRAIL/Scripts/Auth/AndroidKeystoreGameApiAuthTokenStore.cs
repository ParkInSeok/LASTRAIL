using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public sealed class AndroidKeystoreGameApiAuthTokenStore : IGameApiAuthTokenStore, IGameApiAuthTokenReader
    {
        private readonly IAndroidKeystoreAuthTokenBridge bridge;

        public AndroidKeystoreGameApiAuthTokenStore(IAndroidKeystoreAuthTokenBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public async Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var result = await bridge.SaveAsync(AndroidKeystoreAuthTokenPayload.FromSnapshot(value));
            return result != null && result.IsSaved
                ? GameApiAuthTokenStoreResult.Saved
                : GameApiAuthTokenStoreResult.Failed;
        }

        public async Task<GameApiAuthSessionSnapshot> LoadAsync()
        {
            var payload = await bridge.LoadAsync();
            return payload == null ? null : payload.ToSnapshot();
        }

        public async Task<GameApiAuthTokenReadResult> ReadAsync()
        {
            if (!(bridge is IGameApiAuthTokenReader reader))
                return new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Unavailable);
            try { return await reader.ReadAsync() ?? new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.ReadError); }
            catch { return new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.ReadError); }
        }

        public async Task<GameApiAuthTokenStoreResult> ClearAsync()
        {
            var result = await bridge.ClearAsync();
            return result != null && result.IsCleared
                ? GameApiAuthTokenStoreResult.Cleared
                : GameApiAuthTokenStoreResult.Failed;
        }

        public async Task<AndroidKeystoreAuthTokenStoreReadiness> CheckReadinessAsync()
        {
            return AndroidKeystoreAuthTokenStoreReadiness.FromBridgeAvailability(
                await bridge.CheckAvailabilityAsync());
        }
    }
}
