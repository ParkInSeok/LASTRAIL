using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IAndroidKeystoreAuthTokenBridge
    {
        Task<AndroidKeystoreBridgeAvailability> CheckAvailabilityAsync();
        Task<AndroidKeystoreBridgeWriteResult> SaveAsync(AndroidKeystoreAuthTokenPayload value);
        Task<AndroidKeystoreAuthTokenPayload> LoadAsync();
        Task<AndroidKeystoreBridgeClearResult> ClearAsync();
    }
}
