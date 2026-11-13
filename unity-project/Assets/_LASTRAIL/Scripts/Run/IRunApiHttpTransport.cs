using System.Threading.Tasks;

namespace LastRail.Run
{
    public interface IRunApiHttpTransport
    {
        Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request);
    }
}
