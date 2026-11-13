using System;
using LastRail.Run;

namespace LastRail.Auth
{
    public sealed class GameApiAuthenticatedHttpResult
    {
        private GameApiAuthenticatedHttpResult(
            GameApiAuthenticatedHttpOutcome outcome,
            RunApiHttpResponse response,
            string errorCode,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public GameApiAuthenticatedHttpOutcome Outcome { get; }
        public RunApiHttpResponse Response { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool RequiresAuthRefreshOrGuestLogin => Outcome == GameApiAuthenticatedHttpOutcome.AuthRequired;
        public bool CanRetryNetwork => Outcome == GameApiAuthenticatedHttpOutcome.NetworkError;
        public bool HasRawTokenPayload => false;
        public bool HasAccountShortcut => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static GameApiAuthenticatedHttpResult Succeeded(RunApiHttpResponse response)
        {
            RequireResponse(response);
            return new GameApiAuthenticatedHttpResult(GameApiAuthenticatedHttpOutcome.Succeeded, response, null, null);
        }

        public static GameApiAuthenticatedHttpResult AuthRequired(RunApiHttpResponse response, string errorCode, string errorMessage)
        {
            RequireResponse(response);
            return new GameApiAuthenticatedHttpResult(
                GameApiAuthenticatedHttpOutcome.AuthRequired,
                response,
                Required(errorCode, nameof(errorCode)),
                Optional(errorMessage));
        }

        public static GameApiAuthenticatedHttpResult NetworkError(string errorMessage)
        {
            return new GameApiAuthenticatedHttpResult(
                GameApiAuthenticatedHttpOutcome.NetworkError,
                null,
                null,
                Required(errorMessage, nameof(errorMessage)));
        }

        private static void RequireResponse(RunApiHttpResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
        }

        private static string Required(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
