using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGameApiGoogleAuthClient
    {
        Task<GameApiGoogleAuthClientResult> SignInAsync(GameApiGoogleAuthRequest request);
    }
}
