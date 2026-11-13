using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGameApiRefreshAuthClient
    {
        Task<GameApiRefreshAuthClientResult> RefreshAsync(GameApiRefreshAuthRequest request);
    }
}
