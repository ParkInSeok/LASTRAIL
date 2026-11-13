using System.Threading.Tasks;

namespace LastRail.Run
{
    public interface IRunRewardPreviewClient
    {
        Task<RunRewardPreviewClientResult> LookupAsync(RunRewardPreviewLookupRequest request);
    }
}
