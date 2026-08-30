using System;
using System.Collections.Generic;
using LastRail.Presentation.AuthGate;

namespace LastRail.Auth
{
    public sealed class GoogleSignInSdkIntegrationReadiness
    {
        private static readonly string[] ProviderOutcomes =
        {
            nameof(GoogleIdTokenProviderOutcome.Success),
            nameof(GoogleIdTokenProviderOutcome.Cancelled),
            nameof(GoogleIdTokenProviderOutcome.ProviderError),
            nameof(GoogleIdTokenProviderOutcome.NetworkError)
        };

        private GoogleSignInSdkIntegrationReadiness(
            string authGateKind,
            bool canStartGoogleSignIn,
            bool canContinueAsGuest,
            bool canLinkGuestToGoogle,
            bool hasSession)
        {
            AuthGateKind = RequireText(authGateKind, nameof(authGateKind));
            CanStartGoogleSignIn = canStartGoogleSignIn;
            CanContinueAsGuest = canContinueAsGuest;
            CanLinkGuestToGoogle = canLinkGuestToGoogle;
            HasSession = hasSession;
            ProviderOutcomeNames = Copy(ProviderOutcomes);
        }

        public string AuthGateKind { get; }
        public string ProviderPortName => nameof(IGoogleIdTokenProvider);
        public string SignInFlowName => nameof(GameApiGoogleAuthFlow);
        public string LinkFlowName => nameof(GameApiGoogleLinkFlow);
        public string TokenStorePortName => nameof(IGameApiAuthTokenStore);
        public string SecureStorageStrategy => "AndroidKeystoreFirstPartyBridge";
        public IReadOnlyList<string> ProviderOutcomeNames { get; }
        public bool CanStartGoogleSignIn { get; }
        public bool CanContinueAsGuest { get; }
        public bool CanLinkGuestToGoogle { get; }
        public bool HasSession { get; }
        public bool RequiresGoogleSignInSdkImport => true;
        public bool RequiresOAuthClientConfiguration => true;
        public bool HasRealOAuthClientId => false;
        public bool HasSecureStorageImplementation => true;
        public bool UsesInsecureTokenStorage => false;
        public bool HasRawTokenDebugPayload => false;
        public bool HasAccountShortcut => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;
        public bool HasBacklogChoiceEvents => false;
        public bool HasBacklogRaidModeUi => false;

        public static GoogleSignInSdkIntegrationReadiness FromAuthGate(AuthGateUxState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new GoogleSignInSdkIntegrationReadiness(
                state.Kind.ToString(),
                state.CanStartGoogleSignIn,
                state.CanContinueAsGuest,
                state.CanLinkGoogle,
                state.HasSession);
        }

        private static IReadOnlyList<string> Copy(IReadOnlyList<string> values)
        {
            var copied = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                copied[i] = values[i];
            }

            return copied;
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
