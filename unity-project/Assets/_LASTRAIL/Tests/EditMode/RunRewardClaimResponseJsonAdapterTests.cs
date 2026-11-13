using System;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardClaimResponseJsonAdapterTests
    {
        [Test]
        public void FromJson_MapsClaimedResponseWithRewardLines()
        {
            var response = RunRewardClaimResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"rewardClaimId\":\"claim_001\"," +
                "\"status\":\"CLAIMED\"," +
                "\"serverClaimedAt\":\"2026-08-26T13:10:00.123Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"rewardLines\":[{\"rewardLineId\":\"line_credits\",\"rewardCode\":\"cur_credits\",\"displayName\":\"크레딧\",\"quantity\":120,\"unitLabel\":\"개\",\"balanceAfter\":840}]," +
                "\"errorCode\":null," +
                "\"message\":null}");

            Assert.That(response.RunId, Is.EqualTo("run_001"));
            Assert.That(response.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(response.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(response.ClaimResultStatus, Is.EqualTo(RunRewardClaimResultStatus.Claimed));
            Assert.That(response.ServerClaimedAt, Is.EqualTo(new DateTime(2026, 8, 26, 13, 10, 0, 123, DateTimeKind.Utc)));
            Assert.That(response.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(response.RewardLines.Count, Is.EqualTo(1));
            Assert.That(response.RewardLines[0].Quantity, Is.EqualTo(120));
            Assert.That(response.RewardLines[0].BalanceAfter, Is.EqualTo(840));
            Assert.That(response.ErrorCode, Is.EqualTo(null));
        }

        [Test]
        public void FromJson_MapsDuplicateReplayAndRejectedResponses()
        {
            var duplicate = RunRewardClaimResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\",\"rewardClaimId\":\"claim_001\",\"status\":\"DUPLICATE_REPLAY\",\"serverClaimedAt\":\"2026-08-26T13:10:00.0000000Z\",\"balanceVersion\":\"balance_v1\",\"rewardLines\":[],\"errorCode\":\"duplicate_replay\",\"message\":\"Already claimed.\"}");
            var rejected = RunRewardClaimResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\",\"rewardClaimId\":null,\"status\":\"REJECTED\",\"serverClaimedAt\":\"2026-08-26T13:10:00.0000000Z\",\"balanceVersion\":\"balance_v1\",\"rewardLines\":[],\"errorCode\":\"preview_expired\",\"message\":\"Preview expired.\"}");

            Assert.That(duplicate.ClaimResultStatus, Is.EqualTo(RunRewardClaimResultStatus.DuplicateReplay));
            Assert.That(duplicate.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(rejected.ClaimResultStatus, Is.EqualTo(RunRewardClaimResultStatus.Rejected));
            Assert.That(rejected.RewardClaimId, Is.EqualTo(null));
            Assert.That(rejected.ErrorCode, Is.EqualTo("preview_expired"));
        }

        [Test]
        public void ToJson_WritesStableClaimResponseFields()
        {
            var response = new RunRewardClaimResponseDto(
                "run_001",
                "preview_001",
                "claim_001",
                RunRewardClaimResultStatus.Claimed,
                new DateTime(2026, 8, 26, 13, 10, 0, DateTimeKind.Utc),
                "balance_v1",
                new[] { new RunRewardLineDto("line_credits", "cur_credits", "크레딧", 120, "개") },
                null,
                null);

            var json = RunRewardClaimResponseJsonAdapter.ToJson(response);

            Assert.That(json, Is.EqualTo(
                "{\"runId\":\"run_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"rewardClaimId\":\"claim_001\"," +
                "\"status\":\"CLAIMED\"," +
                "\"serverClaimedAt\":\"2026-08-26T13:10:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"rewardLines\":[{\"rewardLineId\":\"line_credits\",\"rewardCode\":\"cur_credits\",\"displayName\":\"크레딧\",\"quantity\":120,\"unitLabel\":\"개\"}]," +
                "\"errorCode\":null," +
                "\"message\":null}"));
        }

        [Test]
        public void FromJson_RejectsUnknownStatusOrNonUtcTime()
        {
            Assert.Throws<ArgumentException>(() => RunRewardClaimResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\",\"rewardClaimId\":null,\"status\":\"PENDING\",\"serverClaimedAt\":\"2026-08-26T13:10:00.0000000Z\",\"balanceVersion\":\"balance_v1\",\"rewardLines\":[],\"errorCode\":null,\"message\":null}"));
            Assert.Throws<FormatException>(() => RunRewardClaimResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\",\"rewardClaimId\":null,\"status\":\"REJECTED\",\"serverClaimedAt\":\"2026-08-26T22:10:00.0000000+09:00\",\"balanceVersion\":\"balance_v1\",\"rewardLines\":[],\"errorCode\":null,\"message\":null}"));
        }
    }
}
