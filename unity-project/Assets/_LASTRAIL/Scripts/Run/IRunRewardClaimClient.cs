using System.Threading.Tasks;

namespace LastRail.Run
{
    public interface IRunRewardClaimClient
    {
        Task<RunRewardClaimClientResult> ClaimAsync(RunRewardClaimRequest request);
    }
}
