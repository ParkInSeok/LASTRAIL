using System;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunApiFlowSubmitPolicyTests
    {
        [Test]
        public void Evaluate_AcceptedRoutesToRewardReviewWithClaimAndPreviewIds()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(FlowResult(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true, "claim_001", "preview_001", null),
                null));

            Assert.That(policy.Action, Is.EqualTo(RunApiFlowSubmitAction.ShowRewardReview));
            Assert.That(policy.IsFatalError, Is.EqualTo(false));
            Assert.That(policy.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(policy.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(policy.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(policy.ErrorCode, Is.EqualTo(null));
        }

        [Test]
        public void Evaluate_DuplicateReplayRoutesToResultSyncWithoutFatalError()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(FlowResult(
                RunApiFlowSubmitOutcome.DuplicateReplay,
                Response("DUPLICATE_REPLAY", false, null, "preview_previous_001", "duplicate_replay"),
                null));

            Assert.That(policy.Action, Is.EqualTo(RunApiFlowSubmitAction.SyncRecognizedResult));
            Assert.That(policy.IsFatalError, Is.EqualTo(false));
            Assert.That(policy.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(policy.RewardClaimId, Is.EqualTo(null));
            Assert.That(policy.RewardPreviewId, Is.EqualTo("preview_previous_001"));
            Assert.That(policy.ErrorCode, Is.EqualTo("duplicate_replay"));
        }

        [Test]
        public void Evaluate_RejectedClassifiesRecoverableErrorCodes()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(FlowResult(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false, null, null, "run_state_conflict"),
                null));

            Assert.That(policy.Action, Is.EqualTo(RunApiFlowSubmitAction.ShowRecoverableRejected));
            Assert.That(policy.IsFatalError, Is.EqualTo(false));
            Assert.That(policy.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(policy.RejectionRecovery, Is.EqualTo(RunApiFlowSubmitRejectionRecovery.Recoverable));
            Assert.That(policy.ErrorCode, Is.EqualTo("run_state_conflict"));
        }

        [Test]
        public void Evaluate_RejectedClassifiesUnrecoverableErrorCodes()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(FlowResult(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false, null, null, "contract_violation"),
                null));

            Assert.That(policy.Action, Is.EqualTo(RunApiFlowSubmitAction.ShowFatalRejected));
            Assert.That(policy.IsFatalError, Is.EqualTo(true));
            Assert.That(policy.RejectionRecovery, Is.EqualTo(RunApiFlowSubmitRejectionRecovery.Unrecoverable));
            Assert.That(policy.ErrorCode, Is.EqualTo("contract_violation"));
        }

        [Test]
        public void Evaluate_NetworkErrorAllowsRetryWithSameIdempotencyKey()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(FlowResult(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout"));

            Assert.That(policy.Action, Is.EqualTo(RunApiFlowSubmitAction.RetryWithSameIdempotencyKey));
            Assert.That(policy.IsFatalError, Is.EqualTo(false));
            Assert.That(policy.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(policy.Response, Is.EqualTo(null));
            Assert.That(policy.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public void Evaluate_RejectsNullResult()
        {
            Assert.Throws<ArgumentNullException>(() => RunApiFlowSubmitPolicy.Evaluate(null));
        }

        private static RunApiFlowSubmitResult FlowResult(
            RunApiFlowSubmitOutcome outcome,
            RunResultSubmitResponseDto response,
            string errorMessage)
        {
            return new RunApiFlowSubmitResult(outcome, response, errorMessage);
        }

        private static RunResultSubmitResponseDto Response(
            string resultStatus,
            bool accepted,
            string rewardClaimId,
            string rewardPreviewId,
            string errorCode)
        {
            return new RunResultSubmitResponseDto(
                "run_001",
                accepted,
                resultStatus,
                rewardClaimId,
                rewardPreviewId,
                new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                "balance_v1",
                null,
                errorCode);
        }
    }
}
