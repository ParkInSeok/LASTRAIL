using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.OfflineRewards;

namespace LastRail.Tests.EditMode
{
    public sealed class OfflineRewardPreviewFlowTests
    {
        [Test]
        public void Request_UsesPreviewEndpointWithoutBodyQueryOrClientTruthFields()
        {
            var request = OfflineRewardPreviewRequest.Create();

            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/offline-rewards/preview"));
            Assert.That(request.BodyJson, Is.EqualTo(null));
            Assert.That(request.HasQueryParameters, Is.EqualTo(false));
            Assert.That(request.HasAccountShortcut, Is.EqualTo(false));
            Assert.That(request.EndpointPath.Contains("?"), Is.EqualTo(false));
        }

        [Test]
        public void JsonAdapter_MapsAvailablePreviewIntoDisplayStateWithServerValuesOnly()
        {
            var dto = OfflineRewardPreviewJsonAdapter.FromJson(AvailableJson());
            var state = OfflineRewardPreviewState.FromResponse(dto);

            Assert.That(dto.Accepted, Is.EqualTo(true));
            Assert.That(dto.PreviewStatus, Is.EqualTo(OfflineRewardPreviewStatus.Available));
            Assert.That(state.StoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.StoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.ClaimableSeconds, Is.EqualTo(7200));
            Assert.That(state.ClaimableHours, Is.EqualTo(2.0));
            Assert.That(state.OfflineStorageCapLabel, Is.EqualTo("최대 7시간"));
            Assert.That(state.Calculation.StoryProgressMultiplierLabel, Is.EqualTo("1.52x"));
            Assert.That(state.Calculation.CapSource, Is.EqualTo("offlineStorageCapHours"));
            Assert.That(state.RewardLines.Count, Is.EqualTo(2));
            Assert.That(state.RewardLines[0].Source, Is.EqualTo("OFFLINE_REWARD"));
            Assert.That(state.RewardLines[0].RewardCode, Is.EqualTo("cur_credits"));
            Assert.That(state.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
            Assert.That(state.ServerCalculatedAt, Is.EqualTo(new DateTime(2026, 8, 28, 3, 15, 0, 123, DateTimeKind.Utc)));
        }

        [Test]
        public void JsonAdapter_MapsEmptyAndRejectedPreviewWithoutClientCalculation()
        {
            var empty = OfflineRewardPreviewState.FromResponse(OfflineRewardPreviewJsonAdapter.FromJson(EmptyJson()));
            var rejected = OfflineRewardPreviewClientResult.Rejected("offline_story_progress_missing", "Story progress missing.");

            Assert.That(empty.PreviewStatus, Is.EqualTo(OfflineRewardPreviewStatus.Empty));
            Assert.That(empty.Accepted, Is.EqualTo(true));
            Assert.That(empty.RewardLines.Count, Is.EqualTo(0));
            Assert.That(empty.StoryProgressLabel, Is.EqualTo("스토리 진행 없음"));
            Assert.That(rejected.Outcome, Is.EqualTo(OfflineRewardPreviewClientOutcome.Rejected));
            Assert.That(rejected.ErrorCode, Is.EqualTo("offline_story_progress_missing"));
        }

        [Test]
        public async Task Flow_ReadsClientPreviewAndProducesDisplayDebugJson()
        {
            var client = new FakeOfflineRewardPreviewClient(
                OfflineRewardPreviewClientResult.Available(OfflineRewardPreviewJsonAdapter.FromJson(AvailableJson())));

            var result = await OfflineRewardPreviewFlow.ReadForDebugAsync(client);

            Assert.That(result.ClientResult.Outcome, Is.EqualTo(OfflineRewardPreviewClientOutcome.Available));
            Assert.That(result.State.PreviewStatus, Is.EqualTo(OfflineRewardPreviewStatus.Available));
            Assert.That(result.DiagnosticJson.Contains("\"previewStatus\":\"AVAILABLE\""), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"storyProgressLabel\":\"급수 정차\""), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"storyProgressMultiplierLabel\":\"1.52x\""), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"source\":\"OFFLINE_REWARD\""), Is.EqualTo(true));
            Assert.That(result.IsBlockingOfflineRewardPreview, Is.EqualTo(false));
        }

        [Test]
        public async Task Flow_SeparatesAuthRejectedContractViolationAndNetworkError()
        {
            var auth = await OfflineRewardPreviewFlow.ReadForDebugAsync(new FakeOfflineRewardPreviewClient(
                OfflineRewardPreviewClientResult.Rejected("auth_required", "Authorization header is required.")));
            var contract = await OfflineRewardPreviewFlow.ReadForDebugAsync(new FakeOfflineRewardPreviewClient(
                OfflineRewardPreviewClientResult.Rejected("contract_violation", "Client supplied forbidden fields.")));
            var network = await OfflineRewardPreviewFlow.ReadForDebugAsync(new FakeOfflineRewardPreviewClient(
                OfflineRewardPreviewClientResult.NetworkError("timeout")));

            Assert.That(auth.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(auth.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(contract.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(contract.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(network.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
            Assert.That(network.State, Is.EqualTo(null));
            Assert.That(network.DiagnosticJson.Contains("\"errorMessage\":\"timeout\""), Is.EqualTo(true));
        }

        [Test]
        public void DebugJson_DoesNotContainClientTruthSensitiveOrRawCalculationFields()
        {
            var stateJson = OfflineRewardPreviewStateJsonAdapter.ToJson(
                OfflineRewardPreviewState.FromResponse(OfflineRewardPreviewJsonAdapter.FromJson(AvailableJson())));
            var errorJson = OfflineRewardPreviewFlowResult.ForClientResult(
                OfflineRewardPreviewClientResult.Rejected("contract_violation", "Forbidden client supplied field.")).DiagnosticJson;
            var combined = stateJson + errorJson;

            Assert.That(combined.Contains("accountId"), Is.EqualTo(false));
            Assert.That(combined.Contains("storyProgressMultiplierBps"), Is.EqualTo(false));
            Assert.That(combined.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(combined.Contains("currencyGrants"), Is.EqualTo(false));
            Assert.That(combined.Contains("itemGrants"), Is.EqualTo(false));
            Assert.That(combined.Contains("economyGrants"), Is.EqualTo(false));
            Assert.That(combined.Contains("offlineSeconds"), Is.EqualTo(false));
            Assert.That(combined.Contains("rawBps"), Is.EqualTo(false));
            Assert.That(combined.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(combined.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(combined.Contains("secret"), Is.EqualTo(false));
            Assert.That(combined.Contains("rawRng"), Is.EqualTo(false));
        }

        private static string AvailableJson()
        {
            return "{"
                + "\"accepted\":true,"
                + "\"previewStatus\":\"AVAILABLE\","
                + "\"storyProgressId\":\"opn_03_water_stop\","
                + "\"claimableSeconds\":7200,"
                + "\"claimableHours\":2.0,"
                + "\"offlineStorageCapHours\":7,"
                + "\"minimumClaimSeconds\":600,"
                + "\"maxClaimableHours\":7,"
                + "\"rewardLines\":["
                + "{\"rewardLineId\":\"offline_credits\",\"rewardCode\":\"cur_credits\",\"displayName\":\"크레딧\",\"quantity\":120,\"unitLabel\":\"개\",\"source\":\"OFFLINE_REWARD\"},"
                + "{\"rewardLineId\":\"offline_parts\",\"rewardCode\":\"cur_machine_parts\",\"displayName\":\"기계 부품\",\"quantity\":8,\"unitLabel\":\"개\",\"source\":\"OFFLINE_REWARD\"}"
                + "],"
                + "\"calculation\":{\"storyProgressId\":\"opn_03_water_stop\",\"storyProgressMultiplierLabel\":\"1.52x\",\"cappedOfflineSeconds\":7200,\"capSource\":\"offlineStorageCapHours\"},"
                + "\"narrativeVersion\":\"act1.2026-08-27.001\","
                + "\"serverCalculatedAt\":\"2026-08-28T03:15:00.123Z\","
                + "\"errorCode\":null,"
                + "\"message\":null"
                + "}";
        }

        private static string EmptyJson()
        {
            return "{"
                + "\"accepted\":true,"
                + "\"previewStatus\":\"EMPTY\","
                + "\"storyProgressId\":\"none\","
                + "\"claimableSeconds\":0,"
                + "\"claimableHours\":0,"
                + "\"offlineStorageCapHours\":4,"
                + "\"minimumClaimSeconds\":600,"
                + "\"maxClaimableHours\":4,"
                + "\"rewardLines\":[],"
                + "\"calculation\":{\"storyProgressId\":\"none\",\"storyProgressMultiplierLabel\":\"1.00x\",\"cappedOfflineSeconds\":0,\"capSource\":\"minimumClaimSeconds\"},"
                + "\"narrativeVersion\":\"act1.2026-08-27.001\","
                + "\"serverCalculatedAt\":\"2026-08-28T03:15:00.123Z\","
                + "\"errorCode\":null,"
                + "\"message\":\"10분 미만 오프라인 보상 없음\""
                + "}";
        }

        private sealed class FakeOfflineRewardPreviewClient : IOfflineRewardPreviewClient
        {
            private readonly OfflineRewardPreviewClientResult result;

            public FakeOfflineRewardPreviewClient(OfflineRewardPreviewClientResult result)
            {
                this.result = result;
            }

            public Task<OfflineRewardPreviewClientResult> ReadAsync(OfflineRewardPreviewRequest request)
            {
                Assert.That(request.EndpointPath, Is.EqualTo("/v1/offline-rewards/preview"));
                Assert.That(request.BodyJson, Is.EqualTo(null));
                Assert.That(request.HasQueryParameters, Is.EqualTo(false));
                Assert.That(request.HasAccountShortcut, Is.EqualTo(false));
                return Task.FromResult(result);
            }
        }
    }
}
