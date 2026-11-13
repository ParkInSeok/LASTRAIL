using System;

namespace LastRail.Run
{
    public sealed class RunStartClientResult
    {
        private RunStartClientResult(
            RunStartClientOutcome outcome,
            RunStartResponseDto response,
            string errorCode,
            string errorMessage)
        {
            Outcome = outcome;
            Response = response;
            ErrorCode = Optional(errorCode);
            ErrorMessage = Optional(errorMessage);
        }

        public RunStartClientOutcome Outcome { get; }
        public RunStartResponseDto Response { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool RequiresAuthRefreshOrGuestLogin => Outcome == RunStartClientOutcome.AuthRequired;
        public bool RequiresStateResync => Outcome == RunStartClientOutcome.StateConflict;
        public bool CanRetryWithSameIdempotencyKey => Outcome == RunStartClientOutcome.NetworkError;
        public bool HasClientTruthOverride => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static RunStartClientResult Started(RunStartResponseDto response)
        {
            RequireResponse(response);
            return new RunStartClientResult(RunStartClientOutcome.Started, response, null, null);
        }

        public static RunStartClientResult DuplicateReplay(RunStartResponseDto response)
        {
            RequireResponse(response);
            return new RunStartClientResult(RunStartClientOutcome.DuplicateReplay, response, "duplicate_replay", null);
        }

        public static RunStartClientResult OperationPreviewAlreadyConsumed(string errorCode, string errorMessage)
        {
            return new RunStartClientResult(
                RunStartClientOutcome.OperationPreviewAlreadyConsumed,
                null,
                Required(errorCode, nameof(errorCode)),
                errorMessage);
        }

        public static RunStartClientResult StateConflict(string errorCode, string errorMessage)
        {
            return new RunStartClientResult(
                RunStartClientOutcome.StateConflict,
                null,
                Required(errorCode, nameof(errorCode)),
                errorMessage);
        }

        public static RunStartClientResult AuthRequired(string errorCode, string errorMessage)
        {
            return new RunStartClientResult(
                RunStartClientOutcome.AuthRequired,
                null,
                Required(errorCode, nameof(errorCode)),
                errorMessage);
        }

        public static RunStartClientResult Rejected(string errorCode, string errorMessage)
        {
            return new RunStartClientResult(
                RunStartClientOutcome.Rejected,
                null,
                Optional(errorCode),
                errorMessage);
        }

        public static RunStartClientResult NetworkError(string errorMessage)
        {
            return new RunStartClientResult(
                RunStartClientOutcome.NetworkError,
                null,
                null,
                Required(errorMessage, nameof(errorMessage)));
        }

        private static void RequireResponse(RunStartResponseDto response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
        }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
