using System;

namespace LastRail.Auth
{
    public sealed class GoogleSignInSdkBridgeResult
    {
        private GoogleSignInSdkBridgeResult(
            GoogleSignInSdkBridgeOutcome outcome,
            GoogleIdToken idToken,
            string errorCode,
            string message)
        {
            if (outcome == GoogleSignInSdkBridgeOutcome.Success && idToken == null)
            {
                throw new ArgumentNullException(nameof(idToken));
            }

            if (outcome != GoogleSignInSdkBridgeOutcome.Success && string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required for non-success outcomes.", nameof(message));
            }

            Outcome = outcome;
            IdToken = idToken;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        }

        public GoogleSignInSdkBridgeOutcome Outcome { get; }
        public GoogleIdToken IdToken { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        public static GoogleSignInSdkBridgeResult Success(GoogleIdToken idToken)
        {
            return new GoogleSignInSdkBridgeResult(GoogleSignInSdkBridgeOutcome.Success, idToken, null, null);
        }

        public static GoogleSignInSdkBridgeResult Cancelled(string message)
        {
            return new GoogleSignInSdkBridgeResult(GoogleSignInSdkBridgeOutcome.Cancelled, null, null, message);
        }

        public static GoogleSignInSdkBridgeResult ProviderError(string errorCode, string message)
        {
            return new GoogleSignInSdkBridgeResult(GoogleSignInSdkBridgeOutcome.ProviderError, null, errorCode, message);
        }

        public static GoogleSignInSdkBridgeResult NetworkError(string message)
        {
            return new GoogleSignInSdkBridgeResult(GoogleSignInSdkBridgeOutcome.NetworkError, null, null, message);
        }
    }
}
