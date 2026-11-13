using System;
using System.Threading.Tasks;

namespace LastRail.OfflineRewards
{
    public static class OfflineRewardClaimFlow
    {
        public static async Task<OfflineRewardClaimFlowResult> ClaimForDebugAsync(
            OfflineRewardClaimRequest request,
            IOfflineRewardClaimClient client)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var clientResult = await client.ClaimAsync(request);
            return OfflineRewardClaimFlowResult.FromClientResult(clientResult);
        }
    }
}
