using System;
using System.Threading.Tasks;

namespace LastRail.Run
{
    public sealed class RunRewardClaimHttpClient : IRunRewardClaimClient
    {
        private readonly IRunApiHttpTransport transport;

        public RunRewardClaimHttpClient(IRunApiHttpTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RunRewardClaimClientResult> ClaimAsync(RunRewardClaimRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = await transport.SendAsync(new RunApiHttpRequest(
                request.Method,
                request.EndpointPath,
                request.BodyJson,
                new[] { new RunApiHttpHeader(request.IdempotencyHeaderName, request.IdempotencyHeaderValue) }));

            if (response.IsNetworkError)
            {
                return RunRewardClaimClientResult.NetworkError(response.NetworkErrorMessage);
            }

            var dto = RunRewardClaimResponseJsonAdapter.FromJson(response.BodyJson);
            switch (dto.ClaimResultStatus)
            {
                case RunRewardClaimResultStatus.Claimed:
                    return RunRewardClaimClientResult.Claimed(dto);
                case RunRewardClaimResultStatus.DuplicateReplay:
                    return RunRewardClaimClientResult.DuplicateReplay(dto);
                case RunRewardClaimResultStatus.Rejected:
                    return RunRewardClaimClientResult.Rejected(dto);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dto), dto.ClaimResultStatus, "Unsupported reward claim result status.");
            }
        }
    }
}
