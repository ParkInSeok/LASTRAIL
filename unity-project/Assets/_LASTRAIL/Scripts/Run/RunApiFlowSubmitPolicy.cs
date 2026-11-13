using System;

namespace LastRail.Run
{
    public sealed class RunApiFlowSubmitPolicy
    {
        private RunApiFlowSubmitPolicy(
            RunApiFlowSubmitAction action,
            bool isFatalError,
            bool canRetryWithSameIdempotencyKey,
            RunApiFlowSubmitRejectionRecovery rejectionRecovery,
            RunResultSubmitResponseDto response,
            string errorMessage)
        {
            Action = action;
            IsFatalError = isFatalError;
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
            RejectionRecovery = rejectionRecovery;
            Response = response;
            ErrorMessage = errorMessage;
        }

        public RunApiFlowSubmitAction Action { get; }
        public bool IsFatalError { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public RunApiFlowSubmitRejectionRecovery RejectionRecovery { get; }
        public RunResultSubmitResponseDto Response { get; }
        public string ErrorMessage { get; }
        public string RewardClaimId => Response?.RewardClaimId;
        public string RewardPreviewId => Response?.RewardPreviewId;
        public string ErrorCode => Response?.ErrorCode;

        public static RunApiFlowSubmitPolicy Evaluate(RunApiFlowSubmitResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.Outcome)
            {
                case RunApiFlowSubmitOutcome.Accepted:
                    return new RunApiFlowSubmitPolicy(
                        RunApiFlowSubmitAction.ShowRewardReview,
                        false,
                        false,
                        RunApiFlowSubmitRejectionRecovery.None,
                        result.Response,
                        null);
                case RunApiFlowSubmitOutcome.DuplicateReplay:
                    return new RunApiFlowSubmitPolicy(
                        RunApiFlowSubmitAction.SyncRecognizedResult,
                        false,
                        false,
                        RunApiFlowSubmitRejectionRecovery.None,
                        result.Response,
                        null);
                case RunApiFlowSubmitOutcome.Rejected:
                    return Rejected(result);
                case RunApiFlowSubmitOutcome.NetworkError:
                    return new RunApiFlowSubmitPolicy(
                        RunApiFlowSubmitAction.RetryWithSameIdempotencyKey,
                        false,
                        true,
                        RunApiFlowSubmitRejectionRecovery.None,
                        null,
                        result.ErrorMessage);
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, "Unsupported run API flow submit outcome.");
            }
        }

        private static RunApiFlowSubmitPolicy Rejected(RunApiFlowSubmitResult result)
        {
            var recoverable = IsRecoverableRejection(result.Response?.ErrorCode);
            return new RunApiFlowSubmitPolicy(
                recoverable ? RunApiFlowSubmitAction.ShowRecoverableRejected : RunApiFlowSubmitAction.ShowFatalRejected,
                !recoverable,
                false,
                recoverable ? RunApiFlowSubmitRejectionRecovery.Recoverable : RunApiFlowSubmitRejectionRecovery.Unrecoverable,
                result.Response,
                null);
        }

        private static bool IsRecoverableRejection(string errorCode)
        {
            return string.Equals(errorCode, "run_state_conflict", StringComparison.Ordinal)
                || string.Equals(errorCode, "validator_temporarily_unavailable", StringComparison.Ordinal)
                || string.Equals(errorCode, "stale_client_state", StringComparison.Ordinal);
        }
    }
}
