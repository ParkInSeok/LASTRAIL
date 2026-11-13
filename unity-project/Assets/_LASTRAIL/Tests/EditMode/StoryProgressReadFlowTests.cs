using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Narrative;

namespace LastRail.Tests.EditMode
{
    public sealed class StoryProgressReadFlowTests
    {
        [Test]
        public void Request_UsesReadOnlyStoryProgressEndpointWithoutBodyQueryOrAccountShortcut()
        {
            var request = StoryProgressReadRequest.Create();

            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/story-progress"));
            Assert.That(request.BodyJson, Is.EqualTo(null));
            Assert.That(request.HasAccountShortcut, Is.EqualTo(false));
            Assert.That(request.EndpointPath.Contains("accountId"), Is.EqualTo(false));
            Assert.That(request.EndpointPath.Contains("?"), Is.EqualTo(false));
        }

        [Test]
        public void JsonAdapter_MapsNoRowDefaultResponseIntoDto()
        {
            var dto = StoryProgressReadResponseJsonAdapter.FromJson(NoRowJson());
            var state = StoryProgressReadState.FromResponse(dto);

            Assert.That(dto.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(dto.HighestStoryProgressId, Is.EqualTo("none"));
            Assert.That(dto.Order, Is.EqualTo(0));
            Assert.That(dto.UnlockedOperations.Count, Is.EqualTo(1));
            Assert.That(dto.UnlockedOperations[0], Is.EqualTo("opn_01_last_departure"));
            Assert.That(state.HighestStoryProgressLabel, Is.EqualTo("스토리 진행 없음"));
            Assert.That(state.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
        }

        [Test]
        public void JsonAdapter_MapsExistingProgressAndKeepsNarrativeMetadataRaw()
        {
            var dto = StoryProgressReadResponseJsonAdapter.FromJson(ExistingJson());
            var state = StoryProgressReadState.FromResponse(dto);
            var debugJson = StoryProgressReadStateJsonAdapter.ToJson(state);

            Assert.That(state.HighestStoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.HighestStoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.ClearedOperationIds.Count, Is.EqualTo(3));
            Assert.That(state.UnlockedOperations[0], Is.EqualTo("opn_04_scrapstar_show"));
            Assert.That(state.ContentStatus, Is.EqualTo("READY"));
            Assert.That(state.LocalizationStatus, Is.EqualTo("LOCALIZED"));
            Assert.That(state.FixtureStatus, Is.EqualTo("LOCAL_FIXTURE_READY"));
            Assert.That(state.MissingContentStatus, Is.EqualTo("NONE"));
            Assert.That(debugJson.Contains("\"highestStoryProgressLabel\":\"급수 정차\""), Is.EqualTo(true));
            Assert.That(debugJson.Contains("\"unlockedOperations\":[\"opn_04_scrapstar_show\"]"), Is.EqualTo(true));
        }

        [Test]
        public async Task Flow_ReadsClientResponseAndProducesStateAndDebugJson()
        {
            var client = new FakeStoryProgressReadClient(
                StoryProgressReadClientResult.Found(StoryProgressReadResponseJsonAdapter.FromJson(ExistingJson())));

            var result = await StoryProgressReadFlow.ReadForDebugAsync(client);

            Assert.That(result.ClientResult.Outcome, Is.EqualTo(StoryProgressReadClientOutcome.Found));
            Assert.That(result.State.HighestStoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(result.DiagnosticJson.Contains("\"isDisplayOnly\":true"), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"hasRewardCalculation\":false"), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"hasUnlockTruth\":false"), Is.EqualTo(true));
            Assert.That(result.IsBlockingStoryProgressRead, Is.EqualTo(false));
        }

        [Test]
        public async Task Flow_SeparatesAuthRejectedAndNetworkErrorWithoutStateOrCalculation()
        {
            var rejected = await StoryProgressReadFlow.ReadForDebugAsync(new FakeStoryProgressReadClient(
                StoryProgressReadClientResult.Rejected("auth_required", "Authorization header is required.")));
            var network = await StoryProgressReadFlow.ReadForDebugAsync(new FakeStoryProgressReadClient(
                StoryProgressReadClientResult.NetworkError("timeout")));

            Assert.That(rejected.ClientResult.Outcome, Is.EqualTo(StoryProgressReadClientOutcome.Rejected));
            Assert.That(rejected.State, Is.EqualTo(null));
            Assert.That(rejected.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(rejected.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(rejected.DiagnosticJson.Contains("\"errorCode\":\"auth_required\""), Is.EqualTo(true));
            Assert.That(network.ClientResult.Outcome, Is.EqualTo(StoryProgressReadClientOutcome.NetworkError));
            Assert.That(network.State, Is.EqualTo(null));
            Assert.That(network.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
            Assert.That(network.DiagnosticJson.Contains("\"errorMessage\":\"timeout\""), Is.EqualTo(true));
            Assert.That(network.IsBlockingStoryProgressRead, Is.EqualTo(false));
        }

        [Test]
        public void FlowDebugJson_DoesNotContainForbiddenClientTruthOrSensitiveFields()
        {
            var json = StoryProgressReadFlowResult.ForClientResult(
                StoryProgressReadClientResult.Rejected("contract_violation", "account id body is not allowed.")).DiagnosticJson;

            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("X-Account-Id"), Is.EqualTo(false));
            Assert.That(json.Contains("X-Lastrail-Account-Id"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("offlineReward"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("firstClearEligible"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
        }

        private static string NoRowJson()
        {
            return "{"
                + "\"actId\":\"act_01_wasteland_railway\","
                + "\"highestStoryProgressId\":\"none\","
                + "\"highestClearedOperationId\":null,"
                + "\"order\":0,"
                + "\"clearedOperationIds\":[],"
                + "\"unlockedOperations\":[\"opn_01_last_departure\"],"
                + "\"narrativeVersion\":\"act1.2026-08-27.001\","
                + "\"contentStatus\":\"READY\","
                + "\"localizationStatus\":\"LOCALIZED\","
                + "\"fixtureStatus\":\"LOCAL_FIXTURE_READY\","
                + "\"missingContentStatus\":\"NONE\""
                + "}";
        }

        private static string ExistingJson()
        {
            return "{"
                + "\"actId\":\"act_01_wasteland_railway\","
                + "\"highestStoryProgressId\":\"opn_03_water_stop\","
                + "\"highestClearedOperationId\":\"opn_03_water_stop\","
                + "\"order\":3,"
                + "\"clearedOperationIds\":[\"opn_01_last_departure\",\"opn_02_first_signal\",\"opn_03_water_stop\"],"
                + "\"unlockedOperations\":[\"opn_04_scrapstar_show\"],"
                + "\"narrativeVersion\":\"act1.2026-08-27.001\","
                + "\"contentStatus\":\"READY\","
                + "\"localizationStatus\":\"LOCALIZED\","
                + "\"fixtureStatus\":\"LOCAL_FIXTURE_READY\","
                + "\"missingContentStatus\":\"NONE\""
                + "}";
        }

        private sealed class FakeStoryProgressReadClient : IStoryProgressReadClient
        {
            private readonly StoryProgressReadClientResult result;

            public FakeStoryProgressReadClient(StoryProgressReadClientResult result)
            {
                this.result = result;
            }

            public Task<StoryProgressReadClientResult> ReadAsync(StoryProgressReadRequest request)
            {
                Assert.That(request.EndpointPath, Is.EqualTo("/v1/story-progress"));
                Assert.That(request.BodyJson, Is.EqualTo(null));
                Assert.That(request.HasAccountShortcut, Is.EqualTo(false));
                return Task.FromResult(result);
            }
        }
    }
}
