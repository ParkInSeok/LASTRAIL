using System;

namespace LastRail.Auth
{
    public sealed class GameApiRefreshAuthClientResult
    {
        private GameApiRefreshAuthClientResult(
            GameApiRefreshAuthClientOutcome outcome,
            GameApiGuestAuthResponseDto response,
            string errorCode,
            string message)
        {
            if (outcome == GameApiRefreshAuthClientOutcome.Refreshed && response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (outcome != GameApiRefreshAuthClientOutcome.Refreshed && string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required for non-refreshed outcomes.", nameof(message));
            }

            Outcome = outcome;
            Response = response;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode;
            Message = string.IsNullOrWhiteSpace(message) ? null : message;
        }

        public GameApiRefreshAuthClientOutcome Outcome { get; }
        public GameApiGuestAuthResponseDto Response { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool RequiresGuestReauth => Outcome == GameApiRefreshAuthClientOutcome.RefreshTokenReused;
        public bool CanRetryNetwork => Outcome == GameApiRefreshAuthClientOutcome.NetworkError;

        public static GameApiRefreshAuthClientResult Refreshed(GameApiGuestAuthResponseDto response)
        {
            return new GameApiRefreshAuthClientResult(GameApiRefreshAuthClientOutcome.Refreshed, response, null, null);
        }

        public static GameApiRefreshAuthClientResult RefreshTokenReused(string errorCode, string message)
        {
            return new GameApiRefreshAuthClientResult(GameApiRefreshAuthClientOutcome.RefreshTokenReused, null, errorCode, message);
        }

        public static GameApiRefreshAuthClientResult Rejected(string errorCode, string message)
        {
            return new GameApiRefreshAuthClientResult(GameApiRefreshAuthClientOutcome.Rejected, null, errorCode, message);
        }

        public static GameApiRefreshAuthClientResult NetworkError(string message)
        {
            return new GameApiRefreshAuthClientResult(GameApiRefreshAuthClientOutcome.NetworkError, null, null, message);
        }
    }
}
