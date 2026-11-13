using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.OfflineRewards;

namespace LastRail.Tests.EditMode
{
    public sealed class OfflineRewardClaimFlowTests
    {
        [Test]
        public void Request_UsesClaimEndpointWithIdempotencyHeaderAndNoBodyQueryOrAccountShortcut()
        {
            var request = OfflineRewardClaimRequest.Create("offline_claim_attempt_001");

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/offline-rewards/claim"));
            Assert.That(request.BodyJson, Is.EqualTo(null));
            Assert.That(request.HasQueryParameters, Is.EqualTo(false));
            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("offline_claim_attempt_001"));
            Assert.That(request.HasAccountShortcut, Is.EqualTo(false));
            Assert.Throws<ArgumentException>(() => OfflineRewardClaimRequest.Create(" "));
        }

        [Test]
        public void JsonAdapter_MapsClaimedResponseAndPreservesServerReferenceIdsAndBalanceAfter()
        {
            var dto = OfflineRewardClaimResponseJsonAdapter.FromJson(ClaimedJson());
            var state = OfflineRewardClaimState.FromResponse(dto);

            Assert.That(dto.Accepted, Is.EqualTo(true));
            Assert.That(dto.ClaimStatus, Is.EqualTo(OfflineRewardClaimStatus.Claimed));
            Assert.That(state.OfflineRewardClaimId, Is.EqualTo("offline_claim_001"));
            Assert.That(state.RewardTransactionId, Is.EqualTo("reward_tx_001"));
            Assert.That(state.EconomyTransactionId, Is.EqualTo("economy_tx_001"));
            Assert.That(state.AccountMutationId, Is.EqualTo("mutation_001"));
            Assert.That(state.StoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.StoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.OfflineStorageCapLabel, Is.EqualTo("최대 7시간"));
            Assert.That(state.RewardLines.Count, Is.EqualTo(1));
            Assert.That(state.RewardLines[0].Source, Is.EqualTo("OFFLINE_REWARD"));
            Assert.That(state.RewardLines[0].BalanceAfter, Is.EqualTo(1240));
            Assert.That(state.Calculation.StoryProgressMultiplierLabel, Is.EqualTo("1.52x"));
            Assert.That(state.ServerClaimedAt, Is.EqualTo(new DateTime(2026, 8, 29, 3, 15, 0, 123, DateTimeKind.Utc)));
            Assert.That(state.HasClientCalculation, Is.EqualTo(false));
            Assert.That(state.HasEconomyGrant, Is.EqualTo(false));
        }

        [Test]
        public void JsonAdapter_MapsDuplicateReplayAndRejectedResponses()
        {
            var duplicate = OfflineRewardClaimResponseJsonAdapter.FromJson(DuplicateJson());
            var rejected = OfflineRewardClaimClientResult.Rejected("offline_reward_not_claimable", "Nothing to claim.");

            Assert.That(duplicate.Accepted, Is.EqualTo(false));
            Assert.That(duplicate.ClaimStatus, Is.EqualTo(OfflineRewardClaimStatus.DuplicateReplay));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(duplicate.RewardLines.Count, Is.EqualTo(0));
            Assert.That(duplicate.OfflineRewardClaimId, Is.EqualTo("offline_claim_001"));
            Assert.That(rejected.Outcome, Is.EqualTo(OfflineRewardClaimClientOutcome.Rejected));
            Assert.That(rejected.ErrorCode, Is.EqualTo("offline_reward_not_claimable"));
        }

        [Test]
        public async Task Flow_ClaimsWithFakeClientAndProducesDisplayDebugJson()
        {
            var client = new FakeOfflineRewardClaimClient(
                OfflineRewardClaimClientResult.Claimed(OfflineRewardClaimResponseJsonAdapter.FromJson(ClaimedJson())));

            var result = await OfflineRewardClaimFlow.ClaimForDebugAsync(OfflineRewardClaimRequest.Create("offline_claim_attempt_001"), client);

            Assert.That(result.ClientResult.Outcome, Is.EqualTo(OfflineRewardClaimClientOutcome.Claimed));
            Assert.That(result.State.ClaimStatus, Is.EqualTo(OfflineRewardClaimStatus.Claimed));
            Assert.That(result.DiagnosticJson.Contains("\"claimStatus\":\"CLAIMED\""), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"offlineRewardClaimId\":\"offline_claim_001\""), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"balanceAfter\":1240"), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"source\":\"OFFLINE_REWARD\""), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"hasClientCalculation\":false"), Is.EqualTo(true));
            Assert.That(result.DiagnosticJson.Contains("\"hasEconomyGrant\":false"), Is.EqualTo(true));
            Assert.That(result.IsBlockingOfflineRewardClaim, Is.EqualTo(false));
        }

        [Test]
        public async Task Flow_SeparatesDuplicateReplayRejectedAuthAndNetworkError()
        {
            var duplicate = await OfflineRewardClaimFlow.ClaimForDebugAsync(
                OfflineRewardClaimRequest.Create("same_key"),
                new FakeOfflineRewardClaimClient(OfflineRewardClaimClientResult.DuplicateReplay(OfflineRewardClaimResponseJsonAdapter.FromJson(DuplicateJson()))));
            var notClaimable = await OfflineRewardClaimFlow.ClaimForDebugAsync(
                OfflineRewardClaimRequest.Create("not_claimable_key"),
                new FakeOfflineRewardClaimClient(OfflineRewardClaimClientResult.Rejected("offline_reward_not_claimable", "Nothing to claim.")));
            var auth = await OfflineRewardClaimFlow.ClaimForDebugAsync(
                OfflineRewardClaimRequest.Create("auth_key"),
                new FakeOfflineRewardClaimClient(OfflineRewardClaimClientResult.Rejected("auth_required", "Authorization header is required.")));
            var network = await OfflineRewardClaimFlow.ClaimForDebugAsync(
                OfflineRewardClaimRequest.Create("retry_key"),
                new FakeOfflineRewardClaimClient(OfflineRewardClaimClientResult.NetworkError("timeout")));

            Assert.That(duplicate.ClientResult.Outcome, Is.EqualTo(OfflineRewardClaimClientOutcome.DuplicateReplay));
            Assert.That(duplicate.State.ClaimStatus, Is.EqualTo(OfflineRewardClaimStatus.DuplicateReplay));
            Assert.That(duplicate.CanRetryNetwork, Is.EqualTo(false));
            Assert.That(notClaimable.IsNoClaimAvailable, Is.EqualTo(true));
            Assert.That(notClaimable.IsBlockingOfflineRewardClaim, Is.EqualTo(false));
            Assert.That(auth.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(network.CanRetryNetwork, Is.EqualTo(true));
            Assert.That(network.State, Is.EqualTo(null));
            Assert.That(network.DiagnosticJson.Contains("\"errorMessage\":\"timeout\""), Is.EqualTo(true));
        }

        [Test]
        public void DebugJson_DoesNotContainClientTruthSensitiveRawOrBlockedRewardFields()
        {
            var stateJson = OfflineRewardClaimStateJsonAdapter.ToJson(
                OfflineRewardClaimState.FromResponse(OfflineRewardClaimResponseJsonAdapter.FromJson(ClaimedJson())));
            var errorJson = OfflineRewardClaimFlowResult.ForClientResult(
                OfflineRewardClaimClientResult.Rejected("contract_violation", "Forbidden client supplied field.")).DiagnosticJson;
            var combined = stateJson + errorJson;

            Assert.That(combined.Contains("accountId"), Is.EqualTo(false));
            Assert.That(combined.Contains("storyProgressMultiplierBps"), Is.EqualTo(false));
            Assert.That(combined.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(combined.Contains("currencyGrants"), Is.EqualTo(false));
            Assert.That(combined.Contains("itemGrants"), Is.EqualTo(false));
            Assert.That(combined.Contains("economyGrants"), Is.EqualTo(false));
            Assert.That(combined.Contains("ledger"), Is.EqualTo(false));
            Assert.That(combined.Contains("rawBps"), Is.EqualTo(false));
            Assert.That(combined.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(combined.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(combined.Contains("secret"), Is.EqualTo(false));
            Assert.That(combined.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(combined.Contains("cur_special_core"), Is.EqualTo(false));
            Assert.That(combined.Contains("cur_command_ticket"), Is.EqualTo(false));
            Assert.That(combined.Contains("cur_command_ticket_fragment"), Is.EqualTo(false));
        }

        private static string ClaimedJson()
        {
            return "{"
                + "\"accepted\":true,"
                + "\"claimStatus\":\"CLAIMED\","
                + "\"offlineRewardClaimId\":\"offline_claim_001\","
                + "\"rewardTransactionId\":\"reward_tx_001\","
                + "\"economyTransactionId\":\"economy_tx_001\","
                + "\"accountMutationId\":\"mutation_001\","
                + "\"storyProgressId\":\"opn_03_water_stop\","
                + "\"claimableSeconds\":7200,"
                + "\"claimableHours\":2.0,"
                + "\"offlineStorageCapHours\":7,"
                + "\"minimumClaimSeconds\":600,"
                + "\"maxClaimableHours\":7,"
                + "\"rewardLines\":[{\"rewardLineId\":\"offline_credits\",\"rewardCode\":\"cur_credits\",\"displayName\":\"크레딧\",\"quantity\":120,\"unitLabel\":\"개\",\"source\":\"OFFLINE_REWARD\",\"balanceAfter\":1240}],"
                + "\"calculation\":{\"storyProgressId\":\"opn_03_water_stop\",\"storyProgressMultiplierLabel\":\"1.52x\",\"cappedOfflineSeconds\":7200,\"capSource\":\"offlineStorageCapHours\"},"
                + "\"serverClaimedAt\":\"2026-08-29T03:15:00.123Z\","
                + "\"narrativeVersion\":\"act1.2026-08-27.001\","
                + "\"errorCode\":null,"
                + "\"message\":null"
                + "}";
        }

        private static string DuplicateJson()
        {
            return "{"
                + "\"accepted\":false,"
                + "\"claimStatus\":\"DUPLICATE_REPLAY\","
                + "\"offlineRewardClaimId\":\"offline_claim_001\","
                + "\"rewardTransactionId\":\"reward_tx_001\","
                + "\"economyTransactionId\":\"economy_tx_001\","
                + "\"accountMutationId\":\"mutation_001\","
                + "\"storyProgressId\":\"opn_03_water_stop\","
                + "\"claimableSeconds\":7200,"
                + "\"claimableHours\":2.0,"
                + "\"offlineStorageCapHours\":7,"
                + "\"minimumClaimSeconds\":600,"
                + "\"maxClaimableHours\":7,"
                + "\"rewardLines\":[],"
                + "\"calculation\":{\"storyProgressId\":\"opn_03_water_stop\",\"storyProgressMultiplierLabel\":\"1.52x\",\"cappedOfflineSeconds\":7200,\"capSource\":\"offlineStorageCapHours\"},"
                + "\"serverClaimedAt\":\"2026-08-29T03:15:00.123Z\","
                + "\"narrativeVersion\":\"act1.2026-08-27.001\","
                + "\"errorCode\":\"duplicate_replay\","
                + "\"message\":\"duplicate replay\""
                + "}";
        }

        private sealed class FakeOfflineRewardClaimClient : IOfflineRewardClaimClient
        {
            private readonly OfflineRewardClaimClientResult result;

            public FakeOfflineRewardClaimClient(OfflineRewardClaimClientResult result)
            {
                this.result = result;
            }

            public Task<OfflineRewardClaimClientResult> ClaimAsync(OfflineRewardClaimRequest request)
            {
                Assert.That(request.EndpointPath, Is.EqualTo("/v1/offline-rewards/claim"));
                Assert.That(request.BodyJson, Is.EqualTo(null));
                Assert.That(request.HasQueryParameters, Is.EqualTo(false));
                Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
                Assert.That(request.HasAccountShortcut, Is.EqualTo(false));
                return Task.FromResult(result);
            }
        }
    }
}
