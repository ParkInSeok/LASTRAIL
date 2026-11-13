using System;
using System.Threading.Tasks;

namespace LastRail.Narrative
{
    public static class StoryProgressReadFlow
    {
        public static async Task<StoryProgressReadFlowResult> ReadForDebugAsync(IStoryProgressReadClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var result = await client.ReadAsync(StoryProgressReadRequest.Create());
            return StoryProgressReadFlowResult.ForClientResult(result);
        }
    }
}
