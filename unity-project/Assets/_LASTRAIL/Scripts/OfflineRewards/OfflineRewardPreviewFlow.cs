using System;
using System.Threading.Tasks;

namespace LastRail.OfflineRewards
{
    public static class OfflineRewardPreviewFlow
    {
        public static async Task<OfflineRewardPreviewFlowResult> ReadForDebugAsync(IOfflineRewardPreviewClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var result = await client.ReadAsync(OfflineRewardPreviewRequest.Create());
            return OfflineRewardPreviewFlowResult.ForClientResult(result);
        }
    }
}
