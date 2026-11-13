using System.Threading.Tasks;

namespace LastRail.OfflineRewards
{
    public interface IOfflineRewardClaimClient
    {
        Task<OfflineRewardClaimClientResult> ClaimAsync(OfflineRewardClaimRequest request);
    }
}
