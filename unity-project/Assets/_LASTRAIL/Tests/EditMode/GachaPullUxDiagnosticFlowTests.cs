using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Gacha;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullUxDiagnosticFlowTests
    {
        [Test]
        public async Task PullForUxAndWriteDiagnosticAsync_RecordsAcceptedFlowFromHttpAdapterToSink()
        {
            var transport = new CapturingRunApiHttpTransport(Response("ACCEPTED", true, null));
            var client = new GachaPullHttpClient(transport);
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 2);
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Single, "gacha_attempt_001");

            var result = await GachaPullDiagnosticFlow.PullForUxAndWriteDiagnosticAsync(
                request,
                new GameApiAccessToken("lst1.payload.signature"),
                client,
                sink);

            Assert.That(transport.LastRequest.EndpointPath, Is.EqualTo("/v1/gacha/banners/banner_act1_launch/pulls"));
            Assert.That(transport.LastRequest.BodyJson, Is.EqualTo("{\"pullCount\":1}"));
            Assert.That(transport.LastRequest.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(result.ClientResult.Outcome, Is.EqualTo(GachaPullClientOutcome.Accepted));
            Assert.That(result.UxState.Kind, Is.EqualTo(GachaPullUxStateKind.ResultReviewReady));
            Assert.That(result.DiagnosticWrite.WasWriteAttempted, Is.EqualTo(true));
            Assert.That(result.DiagnosticWrite.SinkResult.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Written));
            Assert.That(result.IsBlockingGachaPull, Is.EqualTo(false));
            Assert.That(sink.JsonEntries.Count, Is.EqualTo(1));
            Assert.That(sink.JsonEntries[0].Contains("\"kind\":\"ResultReviewReady\""), Is.EqualTo(true));
            Assert.That(sink.JsonEntries[0].Contains("accessToken"), Is.EqualTo(false));
            Assert.That(sink.JsonEntries[0].Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(sink.JsonEntries[0].Contains("rawRng"), Is.EqualTo(false));
        }

        [Test]
        public async Task PullForUxAndWriteDiagnosticAsync_MapsDuplicateRejectedAndNetworkWithoutBlocking()
        {
            var duplicate = await FlowFor(Response("DUPLICATE_REPLAY", false, "duplicate_replay"));
            var rejected = await FlowFor(Response("REJECTED", false, "insufficient_command_ticket"));
            var network = await FlowFor(RunApiHttpResponse.NetworkError("timeout"));

            Assert.That(duplicate.UxState.Kind, Is.EqualTo(GachaPullUxStateKind.RecognizedDuplicate));
            Assert.That(duplicate.DiagnosticWrite.DiagnosticJson.Contains("\"errorCode\":\"duplicate_replay\""), Is.EqualTo(true));
            Assert.That(duplicate.IsBlockingGachaPull, Is.EqualTo(false));
            Assert.That(rejected.UxState.Kind, Is.EqualTo(GachaPullUxStateKind.RecoverableRejected));
            Assert.That(rejected.DiagnosticWrite.DiagnosticJson.Contains("\"errorCode\":\"insufficient_command_ticket\""), Is.EqualTo(true));
            Assert.That(rejected.IsBlockingGachaPull, Is.EqualTo(false));
            Assert.That(network.UxState.Kind, Is.EqualTo(GachaPullUxStateKind.RetryableNetworkError));
            Assert.That(network.UxState.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(network.DiagnosticWrite.DiagnosticJson.Contains("\"canRetryWithSameIdempotencyKey\":true"), Is.EqualTo(true));
            Assert.That(network.IsBlockingGachaPull, Is.EqualTo(false));
        }

        [Test]
        public async Task PullForUxAndWriteDiagnosticAsync_NoOpsWhenSinkIsMissing()
        {
            var transport = new CapturingRunApiHttpTransport(Response("REJECTED", false, "auth_required"));
            var client = new GachaPullHttpClient(transport);
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Single, "gacha_attempt_001");

            var result = await GachaPullDiagnosticFlow.PullForUxAndWriteDiagnosticAsync(
                request,
                new GameApiAccessToken("lst1.payload.signature"),
                client,
                null);

            Assert.That(result.UxState.Kind, Is.EqualTo(GachaPullUxStateKind.AuthRequired));
            Assert.That(result.UxState.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(result.DiagnosticWrite.WasSinkProvided, Is.EqualTo(false));
            Assert.That(result.DiagnosticWrite.WasWriteAttempted, Is.EqualTo(false));
            Assert.That(result.IsBlockingGachaPull, Is.EqualTo(false));
        }

        [Test]
        public async Task PullForUxAndWriteDiagnosticAsync_BufferedSinkStillReturnsUxState()
        {
            var sink = new InMemoryGachaPullDiagnosticLogSink(maxEntries: 1);
            await sink.WriteJsonAsync("{\"kind\":\"old\"}");

            var result = await FlowFor(Response("ACCEPTED", true, null), sink);

            Assert.That(result.UxState.Kind, Is.EqualTo(GachaPullUxStateKind.ResultReviewReady));
            Assert.That(result.DiagnosticWrite.SinkResult.Outcome, Is.EqualTo(GachaPullDiagnosticLogSinkOutcome.Buffered));
            Assert.That(result.DiagnosticWrite.SinkResult.CanRetryLater, Is.EqualTo(true));
            Assert.That(result.IsBlockingGachaPull, Is.EqualTo(false));
            Assert.That(sink.JsonEntries.Count, Is.EqualTo(1));
            Assert.That(sink.JsonEntries[0].Contains("\"kind\":\"ResultReviewReady\""), Is.EqualTo(true));
        }

        [Test]
        public void PullForUxAndWriteDiagnosticAsync_RejectsNullRequiredInputs()
        {
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Single, "gacha_attempt_001");
            var token = new GameApiAccessToken("lst1.payload.signature");
            var client = new GachaPullHttpClient(new CapturingRunApiHttpTransport(Response("ACCEPTED", true, null)));

            AssertArgumentNullAsync(() => GachaPullDiagnosticFlow.PullForUxAndWriteDiagnosticAsync(null, token, client, null));
            AssertArgumentNullAsync(() => GachaPullDiagnosticFlow.PullForUxAndWriteDiagnosticAsync(request, null, client, null));
            AssertArgumentNullAsync(() => GachaPullDiagnosticFlow.PullForUxAndWriteDiagnosticAsync(request, token, null, null));
        }

        private static async Task<GachaPullDiagnosticFlowResult> FlowFor(RunApiHttpResponse response)
        {
            return await FlowFor(response, new InMemoryGachaPullDiagnosticLogSink(maxEntries: 2));
        }

        private static async Task<GachaPullDiagnosticFlowResult> FlowFor(
            RunApiHttpResponse response,
            IGachaPullDiagnosticLogSink sink)
        {
            return await GachaPullDiagnosticFlow.PullForUxAndWriteDiagnosticAsync(
                GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Single, "gacha_attempt_001"),
                new GameApiAccessToken("lst1.payload.signature"),
                new GachaPullHttpClient(new CapturingRunApiHttpTransport(response)),
                sink);
        }

        private static RunApiHttpResponse Response(string status, bool accepted, string errorCode)
        {
            return RunApiHttpResponse.Success(accepted ? 200 : 409, "{\"bannerId\":\"banner_act1_launch\"," +
                "\"accepted\":" + (accepted ? "true" : "false") + "," +
                "\"batchId\":\"batch_001\"," +
                "\"resultStatus\":\"" + status + "\"," +
                "\"pullCount\":1," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":1}," +
                "\"postPityState\":{\"totalPullCount\":11,\"elitePityCount\":1,\"pickupElitePityCount\":11,\"rareOrHigherPityCount\":1,\"revision\":2}," +
                "\"results\":[{\"pullSequence\":1,\"resultRarity\":\"ELITE\",\"resultType\":\"OPERATOR\",\"resultId\":\"result_001\",\"reward\":{\"kind\":\"OPERATOR\",\"id\":\"op_vex\",\"quantity\":1},\"isPickup\":true,\"guaranteeType\":null,\"wasDuplicate\":false,\"duplicateConversion\":null}]," +
                "\"createdAt\":\"2026-08-26T14:20:00.123Z\",\"errorCode\":" + OptionalJsonString(errorCode) + ",\"message\":null}");
        }

        private static string OptionalJsonString(string value)
        {
            return value == null ? "null" : "\"" + value + "\"";
        }

        private static void AssertArgumentNullAsync(Func<Task> action)
        {
            try
            {
                action().GetAwaiter().GetResult();
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
