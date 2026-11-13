using System.Threading.Tasks;

namespace LastRail.Narrative
{
    public interface IStoryProgressReadClient
    {
        Task<StoryProgressReadClientResult> ReadAsync(StoryProgressReadRequest request);
    }
}
