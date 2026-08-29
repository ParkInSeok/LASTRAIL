using System;
using System.Threading.Tasks;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiRefreshAuthHttpClient : IGameApiRefreshAuthClient
    {
        private readonly IRunApiHttpTransport transport;

        public GameApiRefreshAuthHttpClient(IRunApiHttpTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<GameApiRefreshAuthClientResult> RefreshAsync(GameApiRefreshAuthRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = await transport.SendAsync(new RunApiHttpRequest(
                request.Method,
                request.EndpointPath,
                request.BodyJson,
                request.Headers));

            if (response.IsNetworkError)
            {
                return GameApiRefreshAuthClientResult.NetworkError(response.NetworkErrorMessage);
            }

            if (response.StatusCode == 200)
            {
                return GameApiRefreshAuthClientResult.Refreshed(GameApiRefreshAuthResponseJsonAdapter.FromJson(response.BodyJson));
            }

            var errorCode = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "errorCode");
            var message = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "message");
            if (string.Equals(errorCode, "refresh_token_reused", StringComparison.Ordinal))
            {
                return GameApiRefreshAuthClientResult.RefreshTokenReused(
                    errorCode,
                    string.IsNullOrWhiteSpace(message) ? "Refresh token reused." : message);
            }

            return GameApiRefreshAuthClientResult.Rejected(
                string.IsNullOrWhiteSpace(errorCode) ? "auth_rejected" : errorCode,
                string.IsNullOrWhiteSpace(message) ? "Refresh auth rejected." : message);
        }
    }
}
