using System;

namespace LastRail.Auth
{
    public sealed class GameApiGoogleAuthFlowResult
    {
        private GameApiGoogleAuthFlowResult(
            GameApiGoogleAuthFlowOutcome outcome,
            GoogleIdTokenProviderOutcome providerOutcome,
            GameApiAuthSessionSnapshot sessionSnapshot,
            string errorCode,
            string message,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin,
            bool requiresAccountRecovery)
        {
            Outcome = outcome;
            ProviderOutcome = providerOutcome;
            SessionSnapshot = sessionSnapshot;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode;
            Message = string.IsNullOrWhiteSpace(message) ? null : message;
            CanRetryNetwork = canRetryNetwork;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            RequiresAccountRecovery = requiresAccountRecovery;
        }

        public GameApiGoogleAuthFlowOutcome Outcome { get; }
        public GoogleIdTokenProviderOutcome ProviderOutcome { get; }
        public GameApiAuthSessionSnapshot SessionSnapshot { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool CanRetryNetwork { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool RequiresAccountRecovery { get; }

        public static GameApiGoogleAuthFlowResult FromProviderFailure(GoogleIdTokenProviderResult providerResult)
        {
            if (providerResult == null)
            {
                throw new ArgumentNullException(nameof(providerResult));
            }

            if (providerResult.Outcome == GoogleIdTokenProviderOutcome.Cancelled)
            {
                return new GameApiGoogleAuthFlowResult(GameApiGoogleAuthFlowOutcome.Cancelled, providerResult.Outcome, null, providerResult.ErrorCode, providerResult.Message, false, false, false);
            }

            if (providerResult.Outcome == GoogleIdTokenProviderOutcome.NetworkError)
            {
                return new GameApiGoogleAuthFlowResult(GameApiGoogleAuthFlowOutcome.NetworkError, providerResult.Outcome, null, providerResult.ErrorCode, providerResult.Message, true, false, false);
            }

            return new GameApiGoogleAuthFlowResult(GameApiGoogleAuthFlowOutcome.ProviderError, providerResult.Outcome, null, providerResult.ErrorCode, providerResult.Message, false, false, false);
        }

        public static GameApiGoogleAuthFlowResult FromClientResult(GoogleIdTokenProviderOutcome providerOutcome, GameApiGoogleAuthClientResult clientResult)
        {
            if (clientResult == null)
            {
                throw new ArgumentNullException(nameof(clientResult));
            }

            if (clientResult.Outcome == GameApiGoogleAuthClientOutcome.SignedIn)
            {
                return new GameApiGoogleAuthFlowResult(
                    GameApiGoogleAuthFlowOutcome.SignedIn,
                    providerOutcome,
                    GameApiAuthSessionSnapshot.FromGoogleAuthResponse(clientResult.Response),
                    null,
                    null,
                    false,
                    false,
                    false);
            }

            if (clientResult.Outcome == GameApiGoogleAuthClientOutcome.NetworkError)
            {
                return new GameApiGoogleAuthFlowResult(GameApiGoogleAuthFlowOutcome.NetworkError, providerOutcome, null, clientResult.ErrorCode, clientResult.Message, true, false, false);
            }

            if (clientResult.Outcome == GameApiGoogleAuthClientOutcome.Conflict)
            {
                return new GameApiGoogleAuthFlowResult(GameApiGoogleAuthFlowOutcome.Conflict, providerOutcome, null, clientResult.ErrorCode, clientResult.Message, false, false, true);
            }

            return new GameApiGoogleAuthFlowResult(GameApiGoogleAuthFlowOutcome.Rejected, providerOutcome, null, clientResult.ErrorCode, clientResult.Message, false, clientResult.RequiresAuthRefreshOrGuestLogin, false);
        }
    }
}
