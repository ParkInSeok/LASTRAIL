using System;

namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardPreviewClientResult
    {
        private OfflineRewardPreviewClientResult(
            OfflineRewardPreviewClientOutcome outcome,
            OfflineRewardPreviewDto response,
            string errorCode,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public OfflineRewardPreviewClientOutcome Outcome { get; }
        public OfflineRewardPreviewDto Response { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        public static OfflineRewardPreviewClientResult Available(OfflineRewardPreviewDto response)
        {
            RequireResponse(response);
            return new OfflineRewardPreviewClientResult(OfflineRewardPreviewClientOutcome.Available, response, null, null);
        }

        public static OfflineRewardPreviewClientResult Empty(OfflineRewardPreviewDto response)
        {
            RequireResponse(response);
            return new OfflineRewardPreviewClientResult(OfflineRewardPreviewClientOutcome.Empty, response, null, null);
        }

        public static OfflineRewardPreviewClientResult Rejected(string errorCode, string errorMessage)
        {
            return new OfflineRewardPreviewClientResult(
                OfflineRewardPreviewClientOutcome.Rejected,
                null,
                Required(errorCode, nameof(errorCode)),
                errorMessage);
        }

        public static OfflineRewardPreviewClientResult NetworkError(string errorMessage)
        {
            return new OfflineRewardPreviewClientResult(
                OfflineRewardPreviewClientOutcome.NetworkError,
                null,
                null,
                Required(errorMessage, nameof(errorMessage)));
        }

        private static void RequireResponse(OfflineRewardPreviewDto response)
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
