using System;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleLinkFlowResult
    {
        private GameApiGoogleLinkFlowResult(
            GameApiGoogleLinkFlowOutcome outcome,
            GoogleIdTokenProviderOutcome providerOutcome,
            GameApiAuthSessionSnapshot sessionSnapshot,
            GameApiGoogleLinkStatus? linkStatus,
            string errorCode,
            string message,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin,
            bool requiresAccountRecovery)
        {
            Outcome = outcome;
            ProviderOutcome = providerOutcome;
            SessionSnapshot = sessionSnapshot;
            LinkStatus = linkStatus;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode;
            Message = string.IsNullOrWhiteSpace(message) ? null : message;
            CanRetryNetwork = canRetryNetwork;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            RequiresAccountRecovery = requiresAccountRecovery;
        }

        public GameApiGoogleLinkFlowOutcome Outcome { get; }
        public GoogleIdTokenProviderOutcome ProviderOutcome { get; }
        public GameApiAuthSessionSnapshot SessionSnapshot { get; }
        public GameApiGoogleLinkStatus? LinkStatus { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool CanRetryNetwork { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool RequiresAccountRecovery { get; }

        public static GameApiGoogleLinkFlowResult FromProviderFailure(GoogleIdTokenProviderResult providerResult)
        {
            if (providerResult == null)
            {
                throw new ArgumentNullException(nameof(providerResult));
            }

            if (providerResult.Outcome == GoogleIdTokenProviderOutcome.Cancelled)
            {
                return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.Cancelled, providerResult.Outcome, null, null, providerResult.ErrorCode, providerResult.Message, false, false, false);
            }

            if (providerResult.Outcome == GoogleIdTokenProviderOutcome.NetworkError)
            {
                return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.NetworkError, providerResult.Outcome, null, null, providerResult.ErrorCode, providerResult.Message, true, false, false);
            }

            return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.ProviderError, providerResult.Outcome, null, null, providerResult.ErrorCode, providerResult.Message, false, false, false);
        }

        public static GameApiGoogleLinkFlowResult FromClientResult(GoogleIdTokenProviderOutcome providerOutcome, GameApiGoogleLinkClientResult clientResult)
        {
            if (clientResult == null)
            {
                throw new ArgumentNullException(nameof(clientResult));
            }

            if (clientResult.Outcome == GameApiGoogleLinkClientOutcome.Linked || clientResult.Outcome == GameApiGoogleLinkClientOutcome.AlreadyLinked)
            {
                return new GameApiGoogleLinkFlowResult(
                    clientResult.Outcome == GameApiGoogleLinkClientOutcome.Linked ? GameApiGoogleLinkFlowOutcome.Linked : GameApiGoogleLinkFlowOutcome.AlreadyLinked,
                    providerOutcome,
                    GameApiAuthSessionSnapshot.FromGoogleLinkResponse(clientResult.Response),
                    clientResult.Response.LinkStatus,
                    null,
                    null,
                    false,
                    false,
                    false);
            }

            if (clientResult.Outcome == GameApiGoogleLinkClientOutcome.NetworkError)
            {
                return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.NetworkError, providerOutcome, null, null, clientResult.ErrorCode, clientResult.Message, true, false, false);
            }

            if (clientResult.Outcome == GameApiGoogleLinkClientOutcome.AuthRequired)
            {
                return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.AuthRequired, providerOutcome, null, null, clientResult.ErrorCode, clientResult.Message, false, true, false);
            }

            if (clientResult.Outcome == GameApiGoogleLinkClientOutcome.Conflict)
            {
                return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.Conflict, providerOutcome, null, null, clientResult.ErrorCode, clientResult.Message, false, false, true);
            }

            return new GameApiGoogleLinkFlowResult(GameApiGoogleLinkFlowOutcome.Rejected, providerOutcome, null, null, clientResult.ErrorCode, clientResult.Message, false, false, false);
        }
    }
}
