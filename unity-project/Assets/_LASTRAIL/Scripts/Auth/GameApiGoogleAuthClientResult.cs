using System;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleAuthClientResult
    {
        private GameApiGoogleAuthClientResult(
            GameApiGoogleAuthClientOutcome outcome,
            GameApiGoogleAuthResponseDto response,
            string errorCode,
            string message)
        {
            if (outcome == GameApiGoogleAuthClientOutcome.SignedIn && response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (outcome != GameApiGoogleAuthClientOutcome.SignedIn && string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required for non-success outcomes.", nameof(message));
            }

            Outcome = outcome;
            Response = response;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode;
            Message = string.IsNullOrWhiteSpace(message) ? null : message;
        }

        public GameApiGoogleAuthClientOutcome Outcome { get; }
        public GameApiGoogleAuthResponseDto Response { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool CanRetryNetwork => Outcome == GameApiGoogleAuthClientOutcome.NetworkError;
        public bool RequiresAuthRefreshOrGuestLogin => ErrorCode == "auth_required";
        public bool RequiresAccountRecovery => Outcome == GameApiGoogleAuthClientOutcome.Conflict;

        public static GameApiGoogleAuthClientResult SignedIn(GameApiGoogleAuthResponseDto response)
        {
            return new GameApiGoogleAuthClientResult(GameApiGoogleAuthClientOutcome.SignedIn, response, null, null);
        }

        public static GameApiGoogleAuthClientResult Conflict(string errorCode, string message)
        {
            return new GameApiGoogleAuthClientResult(GameApiGoogleAuthClientOutcome.Conflict, null, errorCode, message);
        }

        public static GameApiGoogleAuthClientResult Rejected(string errorCode, string message)
        {
            return new GameApiGoogleAuthClientResult(GameApiGoogleAuthClientOutcome.Rejected, null, errorCode, message);
        }

        public static GameApiGoogleAuthClientResult NetworkError(string message)
        {
            return new GameApiGoogleAuthClientResult(GameApiGoogleAuthClientOutcome.NetworkError, null, null, message);
        }
    }
}
