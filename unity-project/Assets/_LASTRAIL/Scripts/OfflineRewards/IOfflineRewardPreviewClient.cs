using System.Threading.Tasks;

namespace LastRail.OfflineRewards
{
    public interface IOfflineRewardPreviewClient
    {
        Task<OfflineRewardPreviewClientResult> ReadAsync(OfflineRewardPreviewRequest request);
    }
}
