using System;
using System.Threading.Tasks;
using LastRail.Auth;

namespace LastRail.Run
{
    public sealed class RunStartHttpClient : IRunStartClient
    {
        private readonly IGameApiAuthenticatedHttpTransport transport;

        public RunStartHttpClient(IGameApiAuthenticatedHttpTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RunStartClientResult> StartAsync(RunStartRequest request, GameApiAccessToken accessToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            var httpRequest = new RunApiHttpRequest(
                request.Method,
                request.EndpointPath,
                request.BodyJson,
                new[] { new RunApiHttpHeader(request.IdempotencyHeaderName, request.IdempotencyHeaderValue) });
            var result = await transport.SendAsync(httpRequest, accessToken);

            if (result.Outcome == GameApiAuthenticatedHttpOutcome.NetworkError)
            {
                return RunStartClientResult.NetworkError(result.ErrorMessage);
            }

            if (result.Outcome == GameApiAuthenticatedHttpOutcome.AuthRequired)
            {
                return RunStartClientResult.AuthRequired(result.ErrorCode, result.ErrorMessage);
            }

            var response = result.Response;
            var errorCode = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "errorCode");
            var message = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "message");

            if (string.Equals(errorCode, "operation_preview_already_consumed", StringComparison.Ordinal))
            {
                return RunStartClientResult.OperationPreviewAlreadyConsumed(errorCode, message);
            }

            if (string.Equals(errorCode, "run_start_state_conflict", StringComparison.Ordinal))
            {
                return RunStartClientResult.StateConflict(errorCode, message);
            }

            if (response.StatusCode < 200 || response.StatusCode >= 300)
            {
                return RunStartClientResult.Rejected(errorCode, message);
            }

            var dto = RunStartResponseJsonAdapter.FromJson(response.BodyJson);
            var status = RunApiErrorJsonAdapter.OptionalString(response.BodyJson, "resultStatus");
            return string.Equals(status, "DUPLICATE_REPLAY", StringComparison.Ordinal)
                ? RunStartClientResult.DuplicateReplay(dto)
                : RunStartClientResult.Started(dto);
        }
    }
}
