using System;
using System.Threading.Tasks;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiAuthenticatedHttpTransport : IGameApiAuthenticatedHttpTransport
    {
        private readonly IRunApiHttpTransport innerTransport;

        public GameApiAuthenticatedHttpTransport(IRunApiHttpTransport innerTransport)
        {
            this.innerTransport = innerTransport ?? throw new ArgumentNullException(nameof(innerTransport));
        }

        public async Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request, GameApiAccessToken accessToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var authenticatedRequest = GameApiBearerAuthRequestFactory.WithBearerToken(request, accessToken);
            var response = await innerTransport.SendAsync(authenticatedRequest);

            if (response.IsNetworkError)
            {
                return GameApiAuthenticatedHttpResult.NetworkError(response.NetworkErrorMessage);
            }

            var errorCode = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "errorCode");
            if (response.StatusCode == 401 || string.Equals(errorCode, "auth_required", StringComparison.Ordinal))
            {
                return GameApiAuthenticatedHttpResult.AuthRequired(
                    response,
                    string.IsNullOrWhiteSpace(errorCode) ? "auth_required" : errorCode,
                    RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "message"));
            }

            return GameApiAuthenticatedHttpResult.Succeeded(response);
        }
    }
}
