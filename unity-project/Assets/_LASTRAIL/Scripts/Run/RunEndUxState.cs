using System;

namespace LastRail.Run
{
    public sealed class RunEndUxState
    {
        private RunEndUxState(
            RunEndUxStateKind kind,
            RunApiFlowSubmitOutcome submitOutcome,
            RunRewardReviewEntryState rewardReviewEntry,
            string recognizedRewardPreviewId,
            RunStaminaRefundMarker staminaRefund,
            string errorCode,
            string errorMessage,
            bool canRetryWithSameIdempotencyKey)
        {
            Kind = kind;
            SubmitOutcome = submitOutcome;
            RewardReviewEntry = rewardReviewEntry;
            RecognizedRewardPreviewId = string.IsNullOrWhiteSpace(recognizedRewardPreviewId) ? null : recognizedRewardPreviewId;
            StaminaRefund = staminaRefund;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
        }

        public RunEndUxStateKind Kind { get; }
        public RunApiFlowSubmitOutcome SubmitOutcome { get; }
        public RunRewardReviewEntryState RewardReviewEntry { get; }
        public string RecognizedRewardPreviewId { get; }
        public bool HasRecognizedRewardPreviewId => RecognizedRewardPreviewId != null;
        public RunStaminaRefundMarker StaminaRefund { get; }
        public bool HasStaminaRefund => StaminaRefund != null;
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool HasRewardAmounts => false;

        public static RunEndUxState FromSubmitPolicy(RunApiFlowSubmitPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            switch (policy.Action)
            {
                case RunApiFlowSubmitAction.ShowRewardReview:
                    return new RunEndUxState(
                        RunEndUxStateKind.RewardReviewReady,
                        RunApiFlowSubmitOutcome.Accepted,
                        RunRewardReviewEntryState.FromSubmitPolicy(policy),
                        null,
                        policy.Response == null ? null : policy.Response.StaminaRefund,
                        null,
                        null,
                        false);
                case RunApiFlowSubmitAction.SyncRecognizedResult:
                    return new RunEndUxState(
                        RunEndUxStateKind.RecognizedDuplicate,
                        RunApiFlowSubmitOutcome.DuplicateReplay,
                        null,
                        policy.RewardPreviewId,
                        policy.Response == null ? null : policy.Response.StaminaRefund,
                        policy.ErrorCode,
                        policy.ErrorMessage,
                        policy.CanRetryWithSameIdempotencyKey);
                case RunApiFlowSubmitAction.ShowRecoverableRejected:
                    return new RunEndUxState(
                        RunEndUxStateKind.RecoverableRejected,
                        RunApiFlowSubmitOutcome.Rejected,
                        null,
                        null,
                        policy.Response == null ? null : policy.Response.StaminaRefund,
                        policy.ErrorCode,
                        policy.ErrorMessage,
                        policy.CanRetryWithSameIdempotencyKey);
                case RunApiFlowSubmitAction.ShowFatalRejected:
                    return new RunEndUxState(
                        RunEndUxStateKind.FatalRejected,
                        RunApiFlowSubmitOutcome.Rejected,
                        null,
                        null,
                        policy.Response == null ? null : policy.Response.StaminaRefund,
                        policy.ErrorCode,
                        policy.ErrorMessage,
                        policy.CanRetryWithSameIdempotencyKey);
                case RunApiFlowSubmitAction.RetryWithSameIdempotencyKey:
                    return new RunEndUxState(
                        RunEndUxStateKind.RetryableNetworkError,
                        RunApiFlowSubmitOutcome.NetworkError,
                        null,
                        null,
                        null,
                        policy.ErrorCode,
                        policy.ErrorMessage,
                        policy.CanRetryWithSameIdempotencyKey);
                default:
                    throw new ArgumentOutOfRangeException(nameof(policy), policy.Action, "Unsupported run end UX action.");
            }
        }
    }
}
