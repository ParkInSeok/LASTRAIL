using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;

namespace LastRail.Gacha
{
    public sealed class GachaPullHttpClient : IGachaPullClient
    {
        private readonly IRunApiHttpTransport transport;

        public GachaPullHttpClient(IRunApiHttpTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<GachaPullClientResult> PullAsync(GachaPullRequest request, GameApiAccessToken accessToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var baseRequest = new RunApiHttpRequest(
                request.Method,
                request.EndpointPath,
                request.BodyJson,
                new[] { new RunApiHttpHeader(request.IdempotencyHeaderName, request.IdempotencyHeaderValue) });
            var authenticatedRequest = GameApiBearerAuthRequestFactory.WithBearerToken(baseRequest, accessToken);

            var response = await transport.SendAsync(authenticatedRequest);
            if (response.IsNetworkError)
            {
                return GachaPullClientResult.NetworkError(response.NetworkErrorMessage);
            }

            var dto = GachaPullResponseJsonAdapter.FromJson(response.BodyJson);
            switch (dto.ResultStatus)
            {
                case GachaPullResultStatus.Accepted:
                    return GachaPullClientResult.Accepted(dto);
                case GachaPullResultStatus.DuplicateReplay:
                    return GachaPullClientResult.DuplicateReplay(dto);
                case GachaPullResultStatus.Rejected:
                    return GachaPullClientResult.Rejected(dto);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dto), dto.ResultStatus, "Unsupported gacha pull result status.");
            }
        }
    }
}
