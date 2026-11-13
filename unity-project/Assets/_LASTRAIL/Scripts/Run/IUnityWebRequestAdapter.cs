using System.Threading.Tasks;

namespace LastRail.Run
{
    public interface IUnityWebRequestAdapter
    {
        Task<RunApiHttpResponse> SendAsync(UnityWebRequestTransportRequest request);
    }
}
