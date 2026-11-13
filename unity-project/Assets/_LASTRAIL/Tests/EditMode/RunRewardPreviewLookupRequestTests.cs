using System;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardPreviewLookupRequestTests
    {
        [Test]
        public void FromRewardReviewEntry_BuildsGetRequestForPreviewLookup()
        {
            var entry = RunRewardReviewEntryState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(
                new RunApiFlowSubmitResult(
                    RunApiFlowSubmitOutcome.Accepted,
                    new RunResultSubmitResponseDto(
                        "run_001",
                        true,
                        "ACCEPTED",
                        null,
                        "preview_001",
                        new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                        "balance_v1",
                        null,
                        null),
                    null)));

            var request = RunRewardPreviewLookupRequest.FromRewardReviewEntry(entry);

            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/reward-preview/preview_001"));
            Assert.That(request.RunId, Is.EqualTo("run_001"));
            Assert.That(request.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(request.BodyJson, Is.EqualTo(null));
            Assert.That(request.RequiresIdempotencyKey, Is.EqualTo(false));
        }

        [Test]
        public void Create_RejectsEmptyInputsOrEntryWithoutPreviewId()
        {
            Assert.Throws<ArgumentException>(() => RunRewardPreviewLookupRequest.Create(null, "preview_001"));
            Assert.Throws<ArgumentException>(() => RunRewardPreviewLookupRequest.Create("run_001", ""));

            var claimOnlyEntry = RunRewardReviewEntryState.FromSubmitPolicy(RunApiFlowSubmitPolicy.Evaluate(
                new RunApiFlowSubmitResult(
                    RunApiFlowSubmitOutcome.Accepted,
                    new RunResultSubmitResponseDto(
                        "run_001",
                        true,
                        "ACCEPTED",
                        "claim_001",
                        null,
                        new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                        "balance_v1",
                        null,
                        null),
                    null)));

            Assert.Throws<InvalidOperationException>(() => RunRewardPreviewLookupRequest.FromRewardReviewEntry(claimOnlyEntry));
        }
    }
}
