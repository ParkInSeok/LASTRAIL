using System.Threading.Tasks;
using LastRail.Auth;

namespace LastRail.Gacha
{
    public interface IGachaPullClient
    {
        Task<GachaPullClientResult> PullAsync(GachaPullRequest request, GameApiAccessToken accessToken);
    }
}
