using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Gacha;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullHttpClientAdapterTests
    {
        [Test]
        public async Task PullAsync_SendsPostWithIdempotencyAndBearerHeaders()
        {
            var transport = new CapturingRunApiHttpTransport(RunApiHttpResponse.Success(200, ResponseJson("ACCEPTED", true, null)));
            IGachaPullClient client = new GachaPullHttpClient(transport);
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Ten, "gacha_attempt_001");

            var result = await client.PullAsync(request, new GameApiAccessToken("lst1.payload.signature"));

            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.EndpointPath, Is.EqualTo("/v1/gacha/banners/banner_act1_launch/pulls"));
            Assert.That(transport.LastRequest.BodyJson, Is.EqualTo("{\"pullCount\":10}"));
            Assert.That(transport.LastRequest.BodyJson.Contains("bannerId"), Is.EqualTo(false));
            Assert.That(transport.LastRequest.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(transport.LastRequest.Headers.Count, Is.EqualTo(2));
            Assert.That(transport.LastRequest.Headers[0].Name, Is.EqualTo("Idempotency-Key"));
            Assert.That(transport.LastRequest.Headers[0].Value, Is.EqualTo("gacha_attempt_001"));
            Assert.That(transport.LastRequest.Headers[1].Name, Is.EqualTo("Authorization"));
            Assert.That(transport.LastRequest.Headers[1].Value, Is.EqualTo("Bearer lst1.payload.signature"));
            Assert.That(result.Outcome, Is.EqualTo(GachaPullClientOutcome.Accepted));
            Assert.That(result.Response.Cost.ItemId, Is.EqualTo("cur_command_ticket"));
        }

        [Test]
        public async Task PullAsync_MapsDuplicateReplayAndRejectedResponses()
        {
            var duplicateClient = new GachaPullHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(200, ResponseJson("DUPLICATE_REPLAY", false, "duplicate_replay"))));
            var rejectedClient = new GachaPullHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(409, ResponseJson("REJECTED", false, "insufficient_command_ticket"))));
            var token = new GameApiAccessToken("lst1.payload.signature");
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Ten, "gacha_attempt_001");

            var duplicate = await duplicateClient.PullAsync(request, token);
            var rejected = await rejectedClient.PullAsync(request, token);

            Assert.That(duplicate.Outcome, Is.EqualTo(GachaPullClientOutcome.DuplicateReplay));
            Assert.That(duplicate.Response.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(rejected.Outcome, Is.EqualTo(GachaPullClientOutcome.Rejected));
            Assert.That(rejected.Response.ErrorCode, Is.EqualTo("insufficient_command_ticket"));
        }

        [Test]
        public async Task PullAsync_MapsAuthRequiredAndNetworkErrorSeparately()
        {
            var authRequiredClient = new GachaPullHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.Success(401, ResponseJson("REJECTED", false, "auth_required"))));
            var networkClient = new GachaPullHttpClient(new CapturingRunApiHttpTransport(
                RunApiHttpResponse.NetworkError("timeout")));
            var token = new GameApiAccessToken("lst1.payload.signature");
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Ten, "gacha_attempt_001");

            var authRequired = await authRequiredClient.PullAsync(request, token);
            var network = await networkClient.PullAsync(request, token);

            Assert.That(authRequired.Outcome, Is.EqualTo(GachaPullClientOutcome.Rejected));
            Assert.That(authRequired.Response.ErrorCode, Is.EqualTo("auth_required"));
            Assert.That(network.Outcome, Is.EqualTo(GachaPullClientOutcome.NetworkError));
            Assert.That(network.Response, Is.EqualTo(null));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public async Task PullAsync_RejectsNullInputs()
        {
            Assert.Throws<ArgumentNullException>(() => new GachaPullHttpClient(null));
            var client = new GachaPullHttpClient(new CapturingRunApiHttpTransport(RunApiHttpResponse.NetworkError("timeout")));
            await AssertArgumentNullAsync(() => client.PullAsync(null, new GameApiAccessToken("lst1.payload.signature")));
            await AssertArgumentNullAsync(() => client.PullAsync(GachaPullRequest.Create("banner", GachaPullCount.Single, "key"), null));
        }

        private static string ResponseJson(string status, bool accepted, string errorCode)
        {
            return "{\"bannerId\":\"banner_act1_launch\"," +
                "\"accepted\":" + (accepted ? "true" : "false") + "," +
                "\"batchId\":\"batch_001\"," +
                "\"resultStatus\":\"" + status + "\"," +
                "\"pullCount\":10," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":10}," +
                "\"postPityState\":{\"totalPullCount\":40,\"elitePityCount\":10,\"pickupElitePityCount\":40,\"rareOrHigherPityCount\":0,\"revision\":7}," +
                "\"results\":[" +
                "{\"pullSequence\":1,\"resultRarity\":\"ELITE\",\"resultType\":\"OPERATOR\",\"resultId\":\"result_001\",\"reward\":{\"kind\":\"OPERATOR\",\"id\":\"op_vex\",\"quantity\":1},\"isPickup\":true,\"guaranteeType\":null,\"wasDuplicate\":false,\"duplicateConversion\":null}" +
                "],\"createdAt\":\"2026-08-26T14:20:00.123Z\",\"errorCode\":" + OptionalJsonString(errorCode) + ",\"message\":null}";
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
