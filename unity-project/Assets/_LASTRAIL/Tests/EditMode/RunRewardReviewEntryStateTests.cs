using System;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardReviewEntryStateTests
    {
        [Test]
        public void FromSubmitPolicy_CreatesRewardReviewStateForAcceptedPolicy()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true, "claim_001", "preview_001", null),
                null));

            var state = RunRewardReviewEntryState.FromSubmitPolicy(policy);

            Assert.That(state.RunId, Is.EqualTo("run_001"));
            Assert.That(state.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(state.ResultStatus, Is.EqualTo("ACCEPTED"));
            Assert.That(state.SubmitOutcome, Is.EqualTo(RunApiFlowSubmitOutcome.Accepted));
            Assert.That(state.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(state.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(state.HasRewardClaimId, Is.EqualTo(true));
            Assert.That(state.HasRewardPreviewId, Is.EqualTo(true));
            Assert.That(state.PreferredRewardReferenceId, Is.EqualTo("preview_001"));
            Assert.That(state.PreferredRewardReferenceKind, Is.EqualTo("rewardPreviewId"));
            Assert.That(state.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void FromSubmitPolicy_AllowsOneRewardReferenceIdWhenAccepted()
        {
            var claimOnly = RunRewardReviewEntryState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(
                new RunApiFlowSubmitResult(
                    RunApiFlowSubmitOutcome.Accepted,
                    Response("ACCEPTED", true, "claim_001", null, null),
                    null)));
            var previewOnly = RunRewardReviewEntryState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(
                new RunApiFlowSubmitResult(
                    RunApiFlowSubmitOutcome.Accepted,
                    Response("ACCEPTED", true, null, "preview_001", null),
                    null)));

            Assert.That(claimOnly.HasRewardClaimId, Is.EqualTo(true));
            Assert.That(claimOnly.HasRewardPreviewId, Is.EqualTo(false));
            Assert.That(claimOnly.PreferredRewardReferenceId, Is.EqualTo("claim_001"));
            Assert.That(claimOnly.PreferredRewardReferenceKind, Is.EqualTo("rewardClaimId"));
            Assert.That(previewOnly.HasRewardClaimId, Is.EqualTo(false));
            Assert.That(previewOnly.HasRewardPreviewId, Is.EqualTo(true));
            Assert.That(previewOnly.PreferredRewardReferenceId, Is.EqualTo("preview_001"));
            Assert.That(previewOnly.PreferredRewardReferenceKind, Is.EqualTo("rewardPreviewId"));
        }

        [Test]
        public void FromSubmitPolicy_RejectsAcceptedPolicyWithoutRewardReferenceIds()
        {
            var policy = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Accepted,
                Response("ACCEPTED", true, null, null, null),
                null));

            Assert.Throws<InvalidOperationException>(() => RunRewardReviewEntryState.FromSubmitPolicy(policy));
        }

        [Test]
        public void FromSubmitPolicy_DoesNotCreateRewardReviewStateForDuplicateRejectedOrNetworkError()
        {
            var duplicate = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.DuplicateReplay,
                Response("DUPLICATE_REPLAY", false, null, null, "duplicate_replay"),
                null));
            var rejected = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.Rejected,
                Response("REJECTED", false, null, null, "contract_violation"),
                null));
            var network = RunApiFlowSubmitPolicy.Evaluate(new RunApiFlowSubmitResult(
                RunApiFlowSubmitOutcome.NetworkError,
                null,
                "timeout"));

            Assert.That(RunRewardReviewEntryState.TryFromSubmitPolicy(duplicate), Is.EqualTo(null));
            Assert.That(RunRewardReviewEntryState.TryFromSubmitPolicy(rejected), Is.EqualTo(null));
            Assert.That(RunRewardReviewEntryState.TryFromSubmitPolicy(network), Is.EqualTo(null));
            Assert.Throws<InvalidOperationException>(() => RunRewardReviewEntryState.FromSubmitPolicy(duplicate));
            Assert.Throws<InvalidOperationException>(() => RunRewardReviewEntryState.FromSubmitPolicy(rejected));
            Assert.Throws<InvalidOperationException>(() => RunRewardReviewEntryState.FromSubmitPolicy(network));
        }

        [Test]
        public void FromSubmitPolicy_RejectsNullPolicy()
        {
            Assert.Throws<ArgumentNullException>(() => RunRewardReviewEntryState.FromSubmitPolicy(null));
            Assert.Throws<ArgumentNullException>(() => RunRewardReviewEntryState.TryFromSubmitPolicy(null));
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
