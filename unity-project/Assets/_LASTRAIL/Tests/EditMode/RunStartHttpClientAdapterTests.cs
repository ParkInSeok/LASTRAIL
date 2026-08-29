using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RunStartHttpClientAdapterTests
    {
        [Test]
        public async Task StartAsync_SendsOperationPreviewOnlyWithIdempotencyAndBearerBoundary()
        {
            var transport = new CapturingAuthenticatedTransport(GameApiAuthenticatedHttpResult.Succeeded(
                RunApiHttpResponse.Success(200, RunStartJson())));
            IRunStartClient client = new RunStartHttpClient(transport);
            var request = RunStartRequest.Create("op_preview_001", "start_attempt_001");
            var token = new GameApiAccessToken("lst1.payload.signature");

            var result = await client.StartAsync(request, token);

            Assert.That(transport.LastToken, Is.SameAs(token));
            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.EndpointPath, Is.EqualTo("/v1/runs/start"));
            Assert.That(transport.LastRequest.BodyJson, Is.EqualTo("{\"operationPreviewId\":\"op_preview_001\"}"));
            Assert.That(transport.LastRequest.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(transport.LastRequest.BodyJson.Contains("storyProgress"), Is.EqualTo(false));
            Assert.That(transport.LastRequest.Headers.Count, Is.EqualTo(1));
            Assert.That(transport.LastRequest.Headers[0].Name, Is.EqualTo("Idempotency-Key"));
            Assert.That(transport.LastRequest.Headers[0].Value, Is.EqualTo("start_attempt_001"));
            Assert.That(result.Outcome, Is.EqualTo(RunStartClientOutcome.Started));
            Assert.That(result.Response.OperationPreviewId, Is.EqualTo("op_preview_001"));
            Assert.That(result.Response.EncounterType, Is.EqualTo(RunEncounterType.Ordinary15Wave));
            Assert.That(result.Response.OrdinaryTotalWaves, Is.EqualTo(15));
            Assert.That(result.Response.StartAugmentPackageRef, Is.EqualTo("aug_pkg_act1_opn01"));
            Assert.That(result.Response.StartTacticPackageRef, Is.EqualTo("tac_pkg_act1_opn01"));
            Assert.That(result.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(result.RequiresStateResync, Is.EqualTo(false));
            Assert.That(result.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
        }

        [Test]
        public async Task StartAsync_MapsConsumedStateConflictAuthRequiredAndNetworkErrorForPresenter()
        {
            var consumedClient = new RunStartHttpClient(new CapturingAuthenticatedTransport(
                GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(409, ErrorJson("operation_preview_already_consumed", "Preview already consumed.")))));
            var conflictClient = new RunStartHttpClient(new CapturingAuthenticatedTransport(
                GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(409, ErrorJson("run_start_state_conflict", "State conflict.")))));
            var authClient = new RunStartHttpClient(new CapturingAuthenticatedTransport(
                GameApiAuthenticatedHttpResult.AuthRequired(
                    RunApiHttpResponse.Success(401, ErrorJson("auth_required", "Auth required.")),
                    "auth_required",
                    "Auth required.")));
            var networkClient = new RunStartHttpClient(new CapturingAuthenticatedTransport(
                GameApiAuthenticatedHttpResult.NetworkError("timeout")));
            var request = RunStartRequest.Create("op_preview_001", "start_attempt_001");
            var token = new GameApiAccessToken("lst1.payload.signature");

            var consumed = await consumedClient.StartAsync(request, token);
            var conflict = await conflictClient.StartAsync(request, token);
            var auth = await authClient.StartAsync(request, token);
            var network = await networkClient.StartAsync(request, token);

            Assert.That(consumed.Outcome, Is.EqualTo(RunStartClientOutcome.OperationPreviewAlreadyConsumed));
            Assert.That(consumed.ErrorCode, Is.EqualTo("operation_preview_already_consumed"));
            Assert.That(consumed.RequiresStateResync, Is.EqualTo(false));
            Assert.That(conflict.Outcome, Is.EqualTo(RunStartClientOutcome.StateConflict));
            Assert.That(conflict.ErrorCode, Is.EqualTo("run_start_state_conflict"));
            Assert.That(conflict.RequiresStateResync, Is.EqualTo(true));
            Assert.That(auth.Outcome, Is.EqualTo(RunStartClientOutcome.AuthRequired));
            Assert.That(auth.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(network.Outcome, Is.EqualTo(RunStartClientOutcome.NetworkError));
            Assert.That(network.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public void RunStartRequest_RequiresIdempotencyAndNeverCarriesClientTruthFields()
        {
            var request = RunStartRequest.Create("op_preview_001", "start_attempt_001");

            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("start_attempt_001"));
            Assert.That(request.BodyJson, Is.EqualTo("{\"operationPreviewId\":\"op_preview_001\"}"));
            Assert.That(request.BodyJson.Contains("accountId"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("unlockedOperations"), Is.EqualTo(false));
            Assert.That(request.BodyJson.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.Throws<ArgumentException>(() => RunStartRequest.Create("op_preview_001", " "));
            Assert.Throws<ArgumentException>(() => RunStartRequest.Create(" ", "start_attempt_001"));
        }

        private static string RunStartJson()
        {
            return "{\"runId\":\"run_001\"," +
                "\"operationPreviewId\":\"op_preview_001\"," +
                "\"balanceVersion\":\"1\"," +
                "\"contentVersion\":\"act1.2026-08-27.001\"," +
                "\"operationId\":\"opn_01_last_departure\"," +
                "\"actId\":\"act_1\"," +
                "\"regionId\":\"region_last_rail\"," +
                "\"enemyFactionId\":\"raiders\"," +
                "\"encounterType\":\"ORDINARY_15_WAVE\"," +
                "\"riskLevel\":2," +
                "\"wavePlanId\":\"wave_plan_opn01\"," +
                "\"seed\":12345," +
                "\"rewardTableId\":\"reward_table_opn01\"," +
                "\"ordinary\":{\"totalWaves\":15}," +
                "\"raid\":null," +
                "\"allowedOperatorSlots\":[\"slot_1\",\"slot_2\",\"slot_3\",\"slot_4\",\"slot_5\"]," +
                "\"startAugmentPackageRef\":\"aug_pkg_act1_opn01\"," +
                "\"startTacticPackageRef\":\"tac_pkg_act1_opn01\"," +
                "\"serverStartedAt\":\"2026-08-29T01:00:00.0000000Z\"," +
                "\"firstClearEligible\":true}";
        }

        private static string ErrorJson(string errorCode, string message)
        {
            return "{\"accepted\":false,\"resultStatus\":\"REJECTED\",\"errorCode\":\"" + errorCode + "\",\"message\":\"" + message + "\"}";
        }

        private sealed class CapturingAuthenticatedTransport : IGameApiAuthenticatedHttpTransport
        {
            private readonly GameApiAuthenticatedHttpResult result;

            public CapturingAuthenticatedTransport(GameApiAuthenticatedHttpResult result)
            {
                this.result = result;
            }

            public RunApiHttpRequest LastRequest { get; private set; }
            public GameApiAccessToken LastToken { get; private set; }

            public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request, GameApiAccessToken accessToken)
            {
                LastRequest = request;
                LastToken = accessToken;
                return Task.FromResult(result);
            }
        }
    }
}
