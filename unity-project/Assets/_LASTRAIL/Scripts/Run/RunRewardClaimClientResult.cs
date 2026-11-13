using System;

namespace LastRail.Run
{
    public sealed class RunRewardClaimClientResult
    {
        private RunRewardClaimClientResult(
            RunRewardClaimClientOutcome outcome,
            RunRewardClaimResponseDto response,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorMessage = errorMessage;
        }

        public RunRewardClaimClientOutcome Outcome { get; }
        public RunRewardClaimResponseDto Response { get; }
        public string ErrorMessage { get; }

        public static RunRewardClaimClientResult Claimed(RunRewardClaimResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new RunRewardClaimClientResult(RunRewardClaimClientOutcome.Claimed, response, null);
        }

        public static RunRewardClaimClientResult DuplicateReplay(RunRewardClaimResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new RunRewardClaimClientResult(RunRewardClaimClientOutcome.DuplicateReplay, response, null);
        }

        public static RunRewardClaimClientResult Rejected(RunRewardClaimResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new RunRewardClaimClientResult(RunRewardClaimClientOutcome.Rejected, response, null);
        }

        public static RunRewardClaimClientResult NetworkError(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be empty.", nameof(errorMessage));
            }

            return new RunRewardClaimClientResult(RunRewardClaimClientOutcome.NetworkError, null, errorMessage);
        }
    }
}
