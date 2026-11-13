using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGoogleIdTokenProvider
    {
        Task<GoogleIdTokenProviderResult> GetIdTokenAsync();
    }
}
