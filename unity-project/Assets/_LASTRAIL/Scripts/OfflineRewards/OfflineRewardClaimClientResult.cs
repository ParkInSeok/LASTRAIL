using System;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardClaimClientResult
    {
        private OfflineRewardClaimClientResult(
            OfflineRewardClaimClientOutcome outcome,
            OfflineRewardClaimResponseDto response,
            string errorCode,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public OfflineRewardClaimClientOutcome Outcome { get; }
        public OfflineRewardClaimResponseDto Response { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        public static OfflineRewardClaimClientResult Claimed(OfflineRewardClaimResponseDto response)
        {
            RequireResponse(response);
            return new OfflineRewardClaimClientResult(OfflineRewardClaimClientOutcome.Claimed, response, null, null);
        }

        public static OfflineRewardClaimClientResult DuplicateReplay(OfflineRewardClaimResponseDto response)
        {
            RequireResponse(response);
            return new OfflineRewardClaimClientResult(OfflineRewardClaimClientOutcome.DuplicateReplay, response, null, null);
        }

        public static OfflineRewardClaimClientResult Rejected(string errorCode, string errorMessage)
        {
            return new OfflineRewardClaimClientResult(
                OfflineRewardClaimClientOutcome.Rejected,
                null,
                Required(errorCode, nameof(errorCode)),
                errorMessage);
        }

        public static OfflineRewardClaimClientResult NetworkError(string errorMessage)
        {
            return new OfflineRewardClaimClientResult(
                OfflineRewardClaimClientOutcome.NetworkError,
                null,
                null,
                Required(errorMessage, nameof(errorMessage)));
        }

        private static void RequireResponse(OfflineRewardClaimResponseDto response)
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
    }
}
