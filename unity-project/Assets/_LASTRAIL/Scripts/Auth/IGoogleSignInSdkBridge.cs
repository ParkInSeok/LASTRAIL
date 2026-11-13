using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGoogleSignInSdkBridge
    {
        Task<GoogleSignInSdkBridgeResult> RequestIdTokenAsync(GoogleSignInSdkConfigurationSlot configuration);
    }
}
