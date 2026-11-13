using System;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleLinkClientResult
    {
        private GameApiGoogleLinkClientResult(
            GameApiGoogleLinkClientOutcome outcome,
            GameApiGoogleLinkResponseDto response,
            string errorCode,
            string message)
        {
            if ((outcome == GameApiGoogleLinkClientOutcome.Linked || outcome == GameApiGoogleLinkClientOutcome.AlreadyLinked) && response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (outcome != GameApiGoogleLinkClientOutcome.Linked
                && outcome != GameApiGoogleLinkClientOutcome.AlreadyLinked
                && string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required for non-success outcomes.", nameof(message));
            }

            Outcome = outcome;
            Response = response;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode;
            Message = string.IsNullOrWhiteSpace(message) ? null : message;
        }

        public GameApiGoogleLinkClientOutcome Outcome { get; }
        public GameApiGoogleLinkResponseDto Response { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public GameApiGoogleLinkStatus? LinkStatus => Response == null ? (GameApiGoogleLinkStatus?)null : Response.LinkStatus;
        public bool CanRetryNetwork => Outcome == GameApiGoogleLinkClientOutcome.NetworkError;
        public bool RequiresAuthRefreshOrGuestLogin => Outcome == GameApiGoogleLinkClientOutcome.AuthRequired;
        public bool RequiresAccountRecovery => Outcome == GameApiGoogleLinkClientOutcome.Conflict;

        public static GameApiGoogleLinkClientResult Linked(GameApiGoogleLinkResponseDto response)
        {
            return new GameApiGoogleLinkClientResult(GameApiGoogleLinkClientOutcome.Linked, response, null, null);
        }

        public static GameApiGoogleLinkClientResult AlreadyLinked(GameApiGoogleLinkResponseDto response)
        {
            return new GameApiGoogleLinkClientResult(GameApiGoogleLinkClientOutcome.AlreadyLinked, response, null, null);
        }

        public static GameApiGoogleLinkClientResult Conflict(string errorCode, string message)
        {
            return new GameApiGoogleLinkClientResult(GameApiGoogleLinkClientOutcome.Conflict, null, errorCode, message);
        }

        public static GameApiGoogleLinkClientResult AuthRequired(string errorCode, string message)
        {
            return new GameApiGoogleLinkClientResult(GameApiGoogleLinkClientOutcome.AuthRequired, null, errorCode, message);
        }

        public static GameApiGoogleLinkClientResult Rejected(string errorCode, string message)
        {
            return new GameApiGoogleLinkClientResult(GameApiGoogleLinkClientOutcome.Rejected, null, errorCode, message);
        }

        public static GameApiGoogleLinkClientResult NetworkError(string message)
        {
            return new GameApiGoogleLinkClientResult(GameApiGoogleLinkClientOutcome.NetworkError, null, null, message);
        }
    }
}
