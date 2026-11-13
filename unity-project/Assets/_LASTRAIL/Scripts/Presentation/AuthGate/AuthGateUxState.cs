using System;
using LastRail.Auth;

namespace LastRail.Presentation.AuthGate
{
    public sealed class AuthGateUxState
    {
        private AuthGateUxState(
            AuthGateUxStateKind kind,
            string title,
            string primaryActionLabel,
            string secondaryActionLabel,
            string providerOutcome,
            string googleAuthOutcome,
            string googleLinkOutcome,
            string refreshOutcome,
            string linkStatus,
            string errorCode,
            string message,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin,
            bool requiresGuestReauth,
            bool requiresAccountRecovery,
            bool canStartGoogleSignIn,
            bool canContinueAsGuest,
            bool canLinkGoogle,
            bool hasSession,
            string accountStatus)
        {
            Kind = kind;
            Title = RequireText(title, nameof(title));
            PrimaryActionLabel = RequireText(primaryActionLabel, nameof(primaryActionLabel));
            SecondaryActionLabel = OptionalText(secondaryActionLabel);
            ProviderOutcome = OptionalText(providerOutcome);
            GoogleAuthOutcome = OptionalText(googleAuthOutcome);
            GoogleLinkOutcome = OptionalText(googleLinkOutcome);
            RefreshOutcome = OptionalText(refreshOutcome);
            LinkStatus = OptionalText(linkStatus);
            ErrorCode = OptionalText(errorCode);
            Message = OptionalText(message);
            CanRetryNetwork = canRetryNetwork;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
            RequiresGuestReauth = requiresGuestReauth;
            RequiresAccountRecovery = requiresAccountRecovery;
            CanStartGoogleSignIn = canStartGoogleSignIn;
            CanContinueAsGuest = canContinueAsGuest;
            CanLinkGoogle = canLinkGoogle;
            HasSession = hasSession;
            AccountStatus = OptionalText(accountStatus);
        }

        public AuthGateUxStateKind Kind { get; }
        public string Title { get; }
        public string PrimaryActionLabel { get; }
        public string SecondaryActionLabel { get; }
        public string ProviderOutcome { get; }
        public string GoogleAuthOutcome { get; }
        public string GoogleLinkOutcome { get; }
        public string RefreshOutcome { get; }
        public string LinkStatus { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool CanRetryNetwork { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool RequiresGuestReauth { get; }
        public bool RequiresAccountRecovery { get; }
        public bool CanStartGoogleSignIn { get; }
        public bool CanContinueAsGuest { get; }
        public bool CanLinkGoogle { get; }
        public bool HasSession { get; }
        public string AccountStatus { get; }
        public bool IsDisplayOnly => true;
        public bool HasRawTokenPayload => false;
        public bool HasAccountShortcut => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static AuthGateUxState FirstLaunch()
        {
            return new AuthGateUxState(
                AuthGateUxStateKind.StartOptions,
                "LAST RAIL 시작",
                "Google로 시작",
                "게스트로 계속",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                true,
                true,
                false,
                false,
                null);
        }

        public static AuthGateUxState FromGuestSession(GameApiAuthSessionSnapshot session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return new AuthGateUxState(
                AuthGateUxStateKind.GuestReady,
                "게스트 진행 중",
                "계속하기",
                "Google 계정 연결",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                session.AccountStatus);
        }

        public static AuthGateUxState FromGoogleAuthFlow(GameApiGoogleAuthFlowResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.Outcome)
            {
                case GameApiGoogleAuthFlowOutcome.SignedIn:
                    return SessionState(
                        AuthGateUxStateKind.GoogleSignedIn,
                        "Google 로그인 완료",
                        "계속하기",
                        null,
                        result.ProviderOutcome.ToString(),
                        result.Outcome.ToString(),
                        null,
                        null,
                        null,
                        result.SessionSnapshot);
                case GameApiGoogleAuthFlowOutcome.Cancelled:
                    return ErrorState(AuthGateUxStateKind.Cancelled, "로그인이 취소되었습니다", "다시 시도", "게스트로 계속", result.ProviderOutcome.ToString(), result.Outcome.ToString(), null, null, null, result.ErrorCode, result.Message, false, false, false, false, true, true);
                case GameApiGoogleAuthFlowOutcome.ProviderError:
                    return ErrorState(AuthGateUxStateKind.ProviderError, "Google 로그인을 완료하지 못했습니다", "다시 시도", "게스트로 계속", result.ProviderOutcome.ToString(), result.Outcome.ToString(), null, null, null, result.ErrorCode, result.Message, false, false, false, false, true, true);
                case GameApiGoogleAuthFlowOutcome.Conflict:
                    return ErrorState(AuthGateUxStateKind.AccountRecoveryRequired, "계정 확인 필요", "기존 Google 계정으로 로그인", "게스트로 돌아가기", result.ProviderOutcome.ToString(), result.Outcome.ToString(), null, null, null, result.ErrorCode, result.Message, false, false, false, true, true, false);
                case GameApiGoogleAuthFlowOutcome.NetworkError:
                    return ErrorState(AuthGateUxStateKind.RetryableNetworkError, "네트워크 재시도 필요", "다시 시도", "게스트로 계속", result.ProviderOutcome.ToString(), result.Outcome.ToString(), null, null, null, result.ErrorCode, result.Message, true, false, false, false, true, true);
                default:
                    return ErrorState(AuthGateUxStateKind.Rejected, "로그인 요청이 거부되었습니다", "다시 시도", "게스트로 계속", result.ProviderOutcome.ToString(), result.Outcome.ToString(), null, null, null, result.ErrorCode, result.Message, result.CanRetryNetwork, result.RequiresAuthRefreshOrGuestLogin, false, result.RequiresAccountRecovery, true, true);
            }
        }

        public static AuthGateUxState FromGoogleLinkFlow(GameApiGoogleLinkFlowResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.Outcome)
            {
                case GameApiGoogleLinkFlowOutcome.Linked:
                    return SessionState(AuthGateUxStateKind.GoogleLinked, "Google 계정 연결 완료", "계속하기", null, result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), LinkStatusToServerValue(result.LinkStatus), null, result.SessionSnapshot);
                case GameApiGoogleLinkFlowOutcome.AlreadyLinked:
                    return SessionState(AuthGateUxStateKind.GoogleAlreadyLinked, "이미 연결된 계정입니다", "계속하기", null, result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), LinkStatusToServerValue(result.LinkStatus), null, result.SessionSnapshot);
                case GameApiGoogleLinkFlowOutcome.Cancelled:
                    return ErrorState(AuthGateUxStateKind.Cancelled, "연결이 취소되었습니다", "다시 시도", "게스트로 돌아가기", result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), null, null, result.ErrorCode, result.Message, false, false, false, false, true, true);
                case GameApiGoogleLinkFlowOutcome.ProviderError:
                    return ErrorState(AuthGateUxStateKind.ProviderError, "Google 연결을 완료하지 못했습니다", "다시 시도", "게스트로 돌아가기", result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), null, null, result.ErrorCode, result.Message, false, false, false, false, true, true);
                case GameApiGoogleLinkFlowOutcome.AuthRequired:
                    return ErrorState(AuthGateUxStateKind.AuthRequired, "인증 갱신 필요", "다시 로그인", "게스트로 돌아가기", result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), null, null, result.ErrorCode, result.Message, false, true, false, false, true, false);
                case GameApiGoogleLinkFlowOutcome.Conflict:
                    return ErrorState(AuthGateUxStateKind.AccountRecoveryRequired, "계정 확인 필요", "기존 Google 계정으로 로그인", "게스트로 돌아가기", result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), null, null, result.ErrorCode, result.Message, false, false, false, true, true, false);
                case GameApiGoogleLinkFlowOutcome.NetworkError:
                    return ErrorState(AuthGateUxStateKind.RetryableNetworkError, "네트워크 재시도 필요", "다시 시도", "게스트로 돌아가기", result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), null, null, result.ErrorCode, result.Message, true, false, false, false, true, true);
                default:
                    return ErrorState(AuthGateUxStateKind.Rejected, "계정 연결이 거부되었습니다", "다시 시도", "게스트로 돌아가기", result.ProviderOutcome.ToString(), null, result.Outcome.ToString(), null, null, result.ErrorCode, result.Message, result.CanRetryNetwork, result.RequiresAuthRefreshOrGuestLogin, false, result.RequiresAccountRecovery, true, true);
            }
        }

        public static AuthGateUxState FromRefreshResult(GameApiRefreshAuthClientResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.Outcome)
            {
                case GameApiRefreshAuthClientOutcome.Refreshed:
                    return SessionState(AuthGateUxStateKind.Restored, "계정 복구 완료", "계속하기", null, null, null, null, null, result.Outcome.ToString(), GameApiAuthSessionSnapshot.FromGuestResponse(result.Response));
                case GameApiRefreshAuthClientOutcome.RefreshTokenReused:
                    return ErrorState(AuthGateUxStateKind.ReLoginRequired, "다시 로그인 필요", "로그인", null, null, null, null, null, result.Outcome.ToString(), result.ErrorCode, result.Message, false, false, true, false, true, false);
                case GameApiRefreshAuthClientOutcome.NetworkError:
                    return ErrorState(AuthGateUxStateKind.RetryableNetworkError, "네트워크 재시도 필요", "다시 시도", null, null, null, null, null, result.Outcome.ToString(), result.ErrorCode, result.Message, true, false, false, false, false, false);
                case GameApiRefreshAuthClientOutcome.RateLimited:
                case GameApiRefreshAuthClientOutcome.ServiceUnavailable:
                case GameApiRefreshAuthClientOutcome.InvalidResponse:
                case GameApiRefreshAuthClientOutcome.UnknownError:
                    return ErrorState(AuthGateUxStateKind.RetryableNetworkError, "연결 확인 필요", "다시 확인", null, null, null, null, null, result.Outcome.ToString(), result.ErrorCode, result.Message, false, false, false, false, false, false);
                default:
                    return ErrorState(AuthGateUxStateKind.AuthRequired, "인증 필요", "로그인", null, null, null, null, null, result.Outcome.ToString(), result.ErrorCode, result.Message, false, true, false, false, true, false);
            }
        }

        private static AuthGateUxState SessionState(
            AuthGateUxStateKind kind,
            string title,
            string primaryActionLabel,
            string secondaryActionLabel,
            string providerOutcome,
            string googleAuthOutcome,
            string googleLinkOutcome,
            string linkStatus,
            string refreshOutcome,
            GameApiAuthSessionSnapshot session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return new AuthGateUxState(kind, title, primaryActionLabel, secondaryActionLabel, providerOutcome, googleAuthOutcome, googleLinkOutcome, refreshOutcome, linkStatus, null, null, false, false, false, false, false, false, false, true, session.AccountStatus);
        }

        private static AuthGateUxState ErrorState(
            AuthGateUxStateKind kind,
            string title,
            string primaryActionLabel,
            string secondaryActionLabel,
            string providerOutcome,
            string googleAuthOutcome,
            string googleLinkOutcome,
            string linkStatus,
            string refreshOutcome,
            string errorCode,
            string message,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin,
            bool requiresGuestReauth,
            bool requiresAccountRecovery,
            bool canStartGoogleSignIn,
            bool canContinueAsGuest)
        {
            return new AuthGateUxState(kind, title, primaryActionLabel, secondaryActionLabel, providerOutcome, googleAuthOutcome, googleLinkOutcome, refreshOutcome, linkStatus, errorCode, message, canRetryNetwork, requiresAuthRefreshOrGuestLogin, requiresGuestReauth, requiresAccountRecovery, canStartGoogleSignIn, canContinueAsGuest, false, false, null);
        }

        private static string LinkStatusToServerValue(GameApiGoogleLinkStatus? linkStatus)
        {
            if (!linkStatus.HasValue)
            {
                return null;
            }

            switch (linkStatus.Value)
            {
                case GameApiGoogleLinkStatus.Linked:
                    return "LINKED";
                case GameApiGoogleLinkStatus.AlreadyLinked:
                    return "ALREADY_LINKED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(linkStatus), linkStatus, "Unsupported Google link status.");
            }
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
