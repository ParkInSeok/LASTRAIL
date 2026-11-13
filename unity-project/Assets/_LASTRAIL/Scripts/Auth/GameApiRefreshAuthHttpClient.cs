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

            RunApiHttpResponse response;
            try
            {
                response = await transport.SendAsync(new RunApiHttpRequest(request.Method, request.EndpointPath, request.BodyJson, request.Headers));
            }
            catch { return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.UnknownError); }
            if (response == null) return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.UnknownError);

            if (response.IsNetworkError)
            {
                return GameApiRefreshAuthClientResult.NetworkError(response.NetworkErrorMessage);
            }

            if (response.StatusCode == 200)
            {
                try { return GameApiRefreshAuthClientResult.Refreshed(GameApiRefreshAuthResponseJsonAdapter.FromJson(response.BodyJson)); }
                catch { return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.InvalidResponse); }
            }

            if (response.StatusCode == 429) return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.RateLimited);
            if (response.StatusCode >= 500) return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.ServiceUnavailable);

            string errorCode;
            string message;
            try
            {
                errorCode = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "errorCode");
                message = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "message");
            }
            catch { return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.InvalidResponse); }
            if (response.StatusCode == 401 && string.Equals(errorCode, "refresh_token_reused", StringComparison.Ordinal))
            {
                return GameApiRefreshAuthClientResult.RefreshTokenReused(
                    errorCode,
                    string.IsNullOrWhiteSpace(message) ? "Refresh token reused." : message);
            }

            if (response.StatusCode == 401 || response.StatusCode == 403)
                return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.AuthRejected, errorCode);
            if (response.StatusCode < 400 || response.StatusCode >= 500)
                return GameApiRefreshAuthClientResult.Failure(GameApiRefreshAuthClientOutcome.UnknownError);

            return GameApiRefreshAuthClientResult.Rejected(
                string.IsNullOrWhiteSpace(errorCode) ? "auth_rejected" : errorCode,
                string.IsNullOrWhiteSpace(message) ? "Refresh auth rejected." : message);
        }
    }
}
