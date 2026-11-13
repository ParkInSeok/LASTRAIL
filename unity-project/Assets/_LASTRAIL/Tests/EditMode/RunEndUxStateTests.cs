using System;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunEndUxStateTests
    {
        [Test]
        public void FromSubmitPolicy_CreatesRewardReviewReadyStateForAcceptedPolicy()
        {
            var policy = Policy(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true, "claim_001", "preview_001", null),
                null);

            var state = RunEndUxState.FromSubmitPolicy(policy);

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RewardReviewReady));
            Assert.That(state.RewardReviewEntry.RunId, Is.EqualTo("run_001"));
            Assert.That(state.RewardReviewEntry.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(state.RewardReviewEntry.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.Accepted));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_CreatesRecognizedDuplicateStateWithoutRewardReviewEntry()
        {
            var policy = Policy(
                RunApiFlowSubmitOutcome.DuplicateReplay,
                Response("DUPLICATE_REPLAY", false, null, "preview_previous_001", "duplicate_replay"),
                null);

            var state = RunEndUxState.FromSubmitPolicy(policy);

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RecognizedDuplicate));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.DuplicateReplay));
            Assert.That(state.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(state.RecognizedRewardPreviewId, Is.EqualTo("preview_previous_001"));
            Assert.That(state.HasRecognizedRewardPreviewId, Is.EqualTo(true));
            Assert.That(state.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_CreatesRecoverableRejectedStateWithErrorCode()
        {
            var policy = Policy(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false, null, null, "run_state_conflict"),
                null);

            var state = RunEndUxState.FromSubmitPolicy(policy);

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RecoverableRejected));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.Rejected));
            Assert.That(state.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(state.ErrorCode, Is.EqualTo("run_state_conflict"));
            Assert.That(state.ErrorMessage, Is.EqualTo(null));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_CreatesFatalRejectedStateWithErrorCode()
        {
            var policy = Policy(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false, null, null, "contract_violation"),
                null);

            var state = RunEndUxState.FromSubmitPolicy(policy);

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.FatalRejected));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.Rejected));
            Assert.That(state.ErrorCode, Is.EqualTo("contract_violation"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_CreatesRetryableNetworkErrorStateWithErrorMessage()
        {
            var policy = Policy(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout");

            var state = RunEndUxState.FromSubmitPolicy(policy);

            Assert.That(state.Kind, Is.EqualTo(RunEndUxStateKind.RetryableNetworkError));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.NetworkError));
            Assert.That(state.RewardReviewEntry, Is.EqualTo(null));
            Assert.That(state.ErrorCode, Is.EqualTo(null));
            Assert.That(state.ErrorMessage, Is.EqualTo("timeout"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_RejectsNullPolicy()
        {
            Assert.Throws<ArgumentNullException>(() => RunEndUxState.FromSubmitPolicy(null));
        }

        private static RunApiFlowSubmitPolicy Policy(
            RunApiFlowSubmitOutcome outcome,
            RunResultSubmitResponseDto response,
            string errorMessage)
        {
            return RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(outcome, response, errorMessage));
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
