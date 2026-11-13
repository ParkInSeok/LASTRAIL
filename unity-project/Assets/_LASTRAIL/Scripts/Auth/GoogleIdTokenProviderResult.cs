using System;

namespace LastRail.Auth
{
    public sealed class GoogleIdTokenProviderResult
    {
        private GoogleIdTokenProviderResult(
            GoogleIdTokenProviderOutcome outcome,
            GoogleIdToken idToken,
            string errorCode,
            string message)
        {
            if (outcome == GoogleIdTokenProviderOutcome.Success && idToken == null)
            {
                throw new ArgumentNullException(nameof(idToken));
            }

            if (outcome != GoogleIdTokenProviderOutcome.Success && string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required for non-success outcomes.", nameof(message));
            }

            Outcome = outcome;
            IdToken = idToken;
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode;
            Message = string.IsNullOrWhiteSpace(message) ? null : message;
        }

        public GoogleIdTokenProviderOutcome Outcome { get; }
        public GoogleIdToken IdToken { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool CanRetryNetwork => Outcome == GoogleIdTokenProviderOutcome.NetworkError;

        public static GoogleIdTokenProviderResult Success(GoogleIdToken idToken)
        {
            return new GoogleIdTokenProviderResult(GoogleIdTokenProviderOutcome.Success, idToken, null, null);
        }

        public static GoogleIdTokenProviderResult Cancelled(string message)
        {
            return new GoogleIdTokenProviderResult(GoogleIdTokenProviderOutcome.Cancelled, null, null, message);
        }

        public static GoogleIdTokenProviderResult ProviderError(string errorCode, string message)
        {
            return new GoogleIdTokenProviderResult(GoogleIdTokenProviderOutcome.ProviderError, null, errorCode, message);
        }

        public static GoogleIdTokenProviderResult NetworkError(string message)
        {
            return new GoogleIdTokenProviderResult(GoogleIdTokenProviderOutcome.NetworkError, null, null, message);
        }
    }
}
