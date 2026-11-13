using System.Threading.Tasks;
using LastRail.Run;

namespace LastRail.Auth
{
    public interface IGameApiAuthenticatedHttpTransport
    {
        Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request, GameApiAccessToken accessToken);
    }
}
