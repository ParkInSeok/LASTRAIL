using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGameApiGoogleLinkClient
    {
        Task<GameApiGoogleLinkClientResult> LinkAsync(GameApiGoogleLinkRequest request, GameApiAccessToken accessToken);
    }
}
