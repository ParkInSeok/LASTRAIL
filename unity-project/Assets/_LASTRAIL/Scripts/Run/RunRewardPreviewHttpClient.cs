using System;
using System.Threading.Tasks;

namespace LastRail.Run
{
    public sealed class RunRewardPreviewHttpClient : IRunRewardPreviewClient
    {
        private readonly IRunApiHttpTransport transport;

        public RunRewardPreviewHttpClient(IRunApiHttpTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RunRewardPreviewClientResult> LookupAsync(RunRewardPreviewLookupRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = await transport.SendAsync(new RunApiHttpRequest(
                request.Method,
                request.EndpointPath,
                request.BodyJson,
                null));

            if (response.IsNetworkError)
            {
                return RunRewardPreviewClientResult.NetworkError(response.NetworkErrorMessage);
            }

            if (response.StatusCode == 200)
            {
                return RunRewardPreviewClientResult.Found(RunRewardPreviewJsonAdapter.FromJson(response.BodyJson));
            }

            var errorCode = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "errorCode");
            var message = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "message");
            return response.StatusCode == 404
                ? RunRewardPreviewClientResult.NotFound(errorCode, message)
                : RunRewardPreviewClientResult.Rejected(errorCode, message);
        }
    }
}
