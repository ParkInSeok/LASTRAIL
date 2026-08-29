using System;
using System.Threading.Tasks;

namespace LastRail.Run
{
    public sealed class UnityWebRequestRunApiHttpTransport : IRunApiHttpTransport
    {
        private readonly IUnityWebRequestAdapter adapter;

        public UnityWebRequestRunApiHttpTransport(IUnityWebRequestAdapter adapter)
        {
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return adapter.SendAsync(UnityWebRequestTransportRequest.FromRunApiRequest(request));
        }
    }
}
