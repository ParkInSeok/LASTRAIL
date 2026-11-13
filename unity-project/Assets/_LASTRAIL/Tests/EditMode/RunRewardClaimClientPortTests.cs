using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardClaimClientPortTests
    {
        [Test]
        public async Task ClaimAsync_PortAcceptsRequestAndReturnsClaimedResponseResult()
        {
            var request = RunRewardClaimRequest.Create("run_001", "preview_001", "claim_run_001_attempt_001");
            IRunRewardClaimClient client = new CapturingRunRewardClaimClient(RunRewardClaimClientResult.Claimed(Response(RunRewardClaimResultStatus.Claimed, "claim_001", null)));

            var result = await client.ClaimAsync(request);

            Assert.That(((CapturingRunRewardClaimClient)client).LastRequest, Is.EqualTo(request));
            Assert.That(result.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.Claimed));
            Assert.That(result.Response.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public void DuplicateReplay_StoresServerResponseWithoutThrowing()
        {
            var response = Response(RunRewardClaimResultStatus.DuplicateReplay, "claim_001", "duplicate_replay");

            var result = RunRewardClaimClientResult.DuplicateReplay(response);

            Assert.That(result.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.DuplicateReplay));
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public void Rejected_StoresServerRejectedResponseWithoutThrowing()
        {
            var response = Response(RunRewardClaimResultStatus.Rejected, null, "preview_expired");

            var result = RunRewardClaimClientResult.Rejected(response);

            Assert.That(result.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.Rejected));
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public void NetworkError_StoresMessageWithoutServerResponse()
        {
            var result = RunRewardClaimClientResult.NetworkError("timeout");

            Assert.That(result.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.NetworkError));
            Assert.That(result.Response, Is.EqualTo(null));
            Assert.That(result.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public void ResultFactories_RejectNullOrInvalidInputs()
        {
            Assert.Throws<ArgumentNullException>(() => RunRewardClaimClientResult.Claimed(null));
            Assert.Throws<ArgumentNullException>(() => RunRewardClaimClientResult.DuplicateReplay(null));
            Assert.Throws<ArgumentNullException>(() => RunRewardClaimClientResult.Rejected(null));
            Assert.Throws<ArgumentException>(() => RunRewardClaimClientResult.NetworkError(""));
        }

        private static RunRewardClaimResponseDto Response(
            RunRewardClaimResultStatus status,
            string rewardClaimId,
            string errorCode)
        {
            return new RunRewardClaimResponseDto(
                "run_001",
                "preview_001",
                rewardClaimId,
                status,
                new DateTime(2026, 8, 26, 13, 10, 0, DateTimeKind.Utc),
                "balance_v1",
                Array.Empty<RunRewardLineDto>(),
                errorCode,
                null);
        }

        private sealed class CapturingRunRewardClaimClient : IRunRewardClaimClient
        {
            private readonly RunRewardClaimClientResult result;

            public CapturingRunRewardClaimClient(RunRewardClaimClientResult result)
            {
                this.result = result;
            }

            public RunRewardClaimRequest LastRequest { get; private set; }

            public Task<RunRewardClaimClientResult> ClaimAsync(RunRewardClaimRequest request)
            {
                LastRequest = request;
                return Task.FromResult(result);
            }
        }
    }
}
