using System.Threading.Tasks;
using LastRail.Auth;

namespace LastRail.Run
{
    public interface IRunStartClient
    {
        Task<RunStartClientResult> StartAsync(RunStartRequest request, GameApiAccessToken accessToken);
    }
}
