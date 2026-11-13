using System.Threading.Tasks;

namespace LastRail.Run
{
    public interface IRunResultSubmitClient
    {
        Task<RunResultSubmitClientResult> SubmitAsync(RunResultSubmitRequest request);
    }
}
