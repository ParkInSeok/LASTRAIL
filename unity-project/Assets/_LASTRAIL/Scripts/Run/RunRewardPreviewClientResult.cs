using System;

namespace LastRail.Run
{
    public sealed class RunRewardPreviewClientResult
    {
        private RunRewardPreviewClientResult(
            RunRewardPreviewClientOutcome outcome,
            RunRewardPreviewDto preview,
            string errorCode,
            string errorMessage)
        {
            Outcome = outcome;
            Preview = preview;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public RunRewardPreviewClientOutcome Outcome { get; }
        public RunRewardPreviewDto Preview { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        public static RunRewardPreviewClientResult Found(RunRewardPreviewDto preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            return new RunRewardPreviewClientResult(RunRewardPreviewClientOutcome.Found, preview, null, null);
        }

        public static RunRewardPreviewClientResult NotFound(string errorCode, string errorMessage)
        {
            return new RunRewardPreviewClientResult(RunRewardPreviewClientOutcome.NotFound, null, OptionalText(errorCode), OptionalText(errorMessage));
        }

        public static RunRewardPreviewClientResult Rejected(string errorCode, string errorMessage)
        {
            return new RunRewardPreviewClientResult(RunRewardPreviewClientOutcome.Rejected, null, OptionalText(errorCode), OptionalText(errorMessage));
        }

        public static RunRewardPreviewClientResult NetworkError(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be empty.", nameof(errorMessage));
            }

            return new RunRewardPreviewClientResult(RunRewardPreviewClientOutcome.NetworkError, null, null, errorMessage);
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
