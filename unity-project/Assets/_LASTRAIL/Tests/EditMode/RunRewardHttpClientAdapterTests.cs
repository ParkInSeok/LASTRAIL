using System;
using System.Threading.Tasks;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardHttpClientAdapterTests
    {
        [Test]
        public async Task PreviewLookupAsync_SendsGetRequestAndParsesFoundPreview()
        {
            var transport = new CapturingRunApiHttpTransport(RunApiHttpResponse.Success(200, PreviewJson()));
            IRunRewardPreviewClient client = new RunRewardPreviewHttpClient(transport);
            var request = RunRewardPreviewLookupRequest.Create("run_001", "preview_001");

            var result = await client.LookupAsync(request);

            Assert.That(transport.LastRequest.Method, Is.EqualTo("GET"));
            Assert.That(transport.LastRequest.EndpointPath, Is.EqualTo("/v1/runs/run_001/reward-preview/preview_001"));
            Assert.That(transport.LastRequest.BodyJson, Is.EqualTo(null));
            Assert.That(transport.LastRequest.Headers.Count, Is.EqualTo(0));
            Assert.That(result.Outcome, Is.EqualTo(RunRewardPreviewClientOutcome.Found));
            Assert.That(result.Preview.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(result.ErrorCode, Is.EqualTo(null));
        }

        [Test]
        public async Task PreviewLookupAsync_MapsNotFoundAndNetworkError()
        {
            var notFoundClient = new RunRewardPreviewHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(404, "{\"errorCode\":\"reward_preview_not_found\",\"message\":\"Missing preview.\"}")));
            var networkClient = new RunRewardPreviewHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.NetworkError("timeout")));

            var notFound = await notFoundClient.LookupAsync(RunRewardPreviewLookupRequest.Create("run_001", "preview_001"));
            var network = await networkClient.LookupAsync(RunRewardPreviewLookupRequest.Create("run_001", "preview_001"));

            Assert.That(notFound.Outcome, Is.EqualTo(RunRewardPreviewClientOutcome.NotFound));
            Assert.That(notFound.ErrorCode, Is.EqualTo("reward_preview_not_found"));
            Assert.That(network.Outcome, Is.EqualTo(RunRewardPreviewClientOutcome.NetworkError));
            Assert.That(network.Preview, Is.EqualTo(null));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public async Task ClaimAsync_SendsPostRequestWithIdempotencyHeaderAndParsesClaimed()
        {
            var transport = new CapturingRunApiHttpTransport(RunApiHttpResponse.Success(200, ClaimJson("CLAIMED", "claim_001", null)));
            IRunRewardClaimClient client = new RunRewardClaimHttpClient(transport);
            var request = RunRewardClaimRequest.Create("run_001", "preview_001", "claim_run_001_attempt_001");

            var result = await client.ClaimAsync(request);

            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.EndpointPath, Is.EqualTo("/v1/runs/run_001/reward-preview/preview_001/claim"));
            Assert.That(transport.LastRequest.BodyJson, Is.EqualTo("{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\"}"));
            Assert.That(transport.LastRequest.Headers.Count, Is.EqualTo(1));
            Assert.That(transport.LastRequest.Headers[0].Name, Is.EqualTo("Idempotency-Key"));
            Assert.That(transport.LastRequest.Headers[0].Value, Is.EqualTo("claim_run_001_attempt_001"));
            Assert.That(result.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.Claimed));
            Assert.That(result.Response.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(result.ErrorMessage, Is.EqualTo(null));
        }

        [Test]
        public async Task ClaimAsync_MapsDuplicateReplayRejectedAndNetworkError()
        {
            var duplicateClient = new RunRewardClaimHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(200, ClaimJson("DUPLICATE_REPLAY", "claim_001", "duplicate_replay"))));
            var rejectedClient = new RunRewardClaimHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(400, ClaimJson("REJECTED", null, "contract_violation"))));
            var networkClient = new RunRewardClaimHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.NetworkError("timeout")));

            var request = RunRewardClaimRequest.Create("run_001", "preview_001", "claim_run_001_attempt_001");
            var duplicate = await duplicateClient.ClaimAsync(request);
            var rejected = await rejectedClient.ClaimAsync(request);
            var network = await networkClient.ClaimAsync(request);

            Assert.That(duplicate.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.DuplicateReplay));
            Assert.That(duplicate.Response.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(rejected.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.Rejected));
            Assert.That(rejected.Response.ErrorCode, Is.EqualTo("contract_violation"));
            Assert.That(network.Outcome, Is.EqualTo(RunRewardClaimClientOutcome.NetworkError));
            Assert.That(network.Response, Is.EqualTo(null));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public async Task Adapters_RejectNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() => new RunRewardPreviewHttpClient(null));
            Assert.Throws<ArgumentNullException>(() => new RunRewardClaimHttpClient(null));
            await AssertArgumentNullAsync(() => new RunRewardPreviewHttpClient(new CapturingRunApiHttpTransport(RunApiHttpResponse.NetworkError("timeout"))).LookupAsync(null));
            await AssertArgumentNullAsync(() => new RunRewardClaimHttpClient(new CapturingRunApiHttpTransport(RunApiHttpResponse.NetworkError("timeout"))).ClaimAsync(null));
        }

        private static string PreviewJson()
        {
            return "{\"runId\":\"run_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"status\":\"ACTIVE\"," +
                "\"expiresAt\":\"2026-08-27T12:40:00.0000000Z\"," +
                "\"serverCalculatedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"claimability\":\"CLAIMABLE\"," +
                "\"rewardLines\":[]," +
                "\"claim\":null}";
        }

        private static string ClaimJson(string status, string rewardClaimId, string errorCode)
        {
            return "{\"runId\":\"run_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"rewardClaimId\":" + OptionalJsonString(rewardClaimId) + "," +
                "\"status\":\"" + status + "\"," +
                "\"serverClaimedAt\":\"2026-08-26T13:10:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"rewardLines\":[]," +
                "\"errorCode\":" + OptionalJsonString(errorCode) + "," +
                "\"message\":null}";
        }

        private static string OptionalJsonString(string value)
        {
            return value == null ? "null" : "\"" + value + "\"";
        }

        private static async Task AssertArgumentNullAsync(Func<Task> action)
        {
            try
            {
                await action();
                throw new InvalidOperationException("Expected ArgumentNullException.");
            }
            catch (ArgumentNullException)
            {
            }
        }

        private sealed class CapturingRunApiHttpTransport : IRunApiHttpTransport
        {
            private readonly RunApiHttpResponse response;

            public CapturingRunApiHttpTransport(RunApiHttpResponse response)
            {
                this.response = response;
            }

            public RunApiHttpRequest LastRequest { get; private set; }

            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                LastRequest = request;
                return Task.FromResult(response);
            }
        }
    }
}
