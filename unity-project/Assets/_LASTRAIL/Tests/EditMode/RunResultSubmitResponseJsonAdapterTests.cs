using System;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunResultSubmitResponseJsonAdapterTests
    {
        [Test]
        public void FromJson_MapsAcceptedResultSubmitResponse()
        {
            var response = RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"accepted\":true," +
                "\"resultStatus\":\"ACCEPTED\"," +
                "\"rewardClaimId\":\"claim_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"message\":null," +
                "\"errorCode\":null}");

            Assert.That(response.RunId, Is.EqualTo("run_001"));
            Assert.That(response.Accepted, Is.EqualTo(true));
            Assert.That(response.ResultStatus, Is.EqualTo("ACCEPTED"));
            Assert.That(response.RewardClaimId, Is.EqualTo("claim_001"));
            Assert.That(response.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(response.ServerReceivedAt, Is.EqualTo(ReceivedAt()));
            Assert.That(response.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(response.Message, Is.EqualTo(null));
            Assert.That(response.ErrorCode, Is.EqualTo(null));
        }

        [Test]
        public void FromJson_AcceptsServerIsoUtcMillisecondsReceivedAt()
        {
            var response = RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"accepted\":true," +
                "\"resultStatus\":\"ACCEPTED\"," +
                "\"rewardClaimId\":\"claim_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.123Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"message\":null," +
                "\"errorCode\":null}");

            Assert.That(response.ServerReceivedAt, Is.EqualTo(new DateTime(2026, 8, 26, 12, 40, 0, 123, DateTimeKind.Utc)));
        }

        [Test]
        public void FromJson_MapsDuplicateReplayResponseWithPreviousRewardPreviewId()
        {
            var response = RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"accepted\":false," +
                "\"resultStatus\":\"DUPLICATE_REPLAY\"," +
                "\"rewardClaimId\":null," +
                "\"rewardPreviewId\":\"preview_previous_001\"," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"message\":\"Already submitted.\"," +
                "\"errorCode\":\"duplicate_replay\"}");

            Assert.That(response.Accepted, Is.EqualTo(false));
            Assert.That(response.ResultStatus, Is.EqualTo("DUPLICATE_REPLAY"));
            Assert.That(response.RewardClaimId, Is.EqualTo(null));
            Assert.That(response.RewardPreviewId, Is.EqualTo("preview_previous_001"));
            Assert.That(response.Message, Is.EqualTo("Already submitted."));
            Assert.That(response.ErrorCode, Is.EqualTo("duplicate_replay"));
        }

        [Test]
        public void ToJson_WritesStableResponseFieldNames()
        {
            var response = new RunResultSubmitResponseDto(
                "run_001",
                false,
                "REJECTED",
                null,
                "preview_001",
                ReceivedAt(),
                "balance_v1",
                "Validation failed.",
                "validator_rejected");

            var json = RunResultSubmitResponseJsonAdapter.ToJson(response);

            Assert.That(json, Is.EqualTo(
                "{\"runId\":\"run_001\"," +
                "\"accepted\":false," +
                "\"resultStatus\":\"REJECTED\"," +
                "\"rewardClaimId\":null," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"staminaRefund\":null," +
                "\"message\":\"Validation failed.\"," +
                "\"errorCode\":\"validator_rejected\"}"));
            Assert.That(json.Contains("creditAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
        }

        [Test]
        public void FromJson_MapsStoryRaidFailureStaminaRefundMarkerAsDisplayOnly()
        {
            var response = RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_raid_001\"," +
                "\"accepted\":true," +
                "\"resultStatus\":\"ACCEPTED\"," +
                "\"rewardClaimId\":null," +
                "\"rewardPreviewId\":\"preview_raid_001\"," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"staminaRefund\":{\"status\":\"AVAILABLE\",\"message\":\"Story raid failure refund available.\"}," +
                "\"message\":null," +
                "\"errorCode\":null}");

            Assert.That(response.HasStaminaRefund, Is.EqualTo(true));
            Assert.That(response.StaminaRefund.Status, Is.EqualTo("AVAILABLE"));
            Assert.That(response.StaminaRefund.Message, Is.EqualTo("Story raid failure refund available."));
            Assert.That(response.StaminaRefund.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(response.StaminaRefund.HasRewardCalculation, Is.EqualTo(false));

            var json = RunResultSubmitResponseJsonAdapter.ToJson(response);

            Assert.That(json.Contains("\"staminaRefund\":{\"status\":\"AVAILABLE\",\"message\":\"Story raid failure refund available.\",\"hasRewardCalculation\":false,\"hasEconomyMutation\":false}"), Is.EqualTo(true));
            Assert.That(json.Contains("staminaRefundAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("animationTimeline"), Is.EqualTo(false));
        }

        [Test]
        public void FromJson_RejectsMissingRequiredField()
        {
            Assert.Throws<InvalidOperationException>(() => RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"accepted\":true," +
                "\"resultStatus\":\"ACCEPTED\"," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"}"));
        }

        [Test]
        public void FromJson_RejectsNonUtcServerReceivedAt()
        {
            Assert.Throws<FormatException>(() => RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"accepted\":true," +
                "\"resultStatus\":\"ACCEPTED\"," +
                "\"serverReceivedAt\":\"2026-08-26T21:40:00.0000000+09:00\"," +
                "\"balanceVersion\":\"balance_v1\"}"));
        }

        [Test]
        public void FromJson_RejectsUnknownResultStatus()
        {
            Assert.Throws<ArgumentException>(() => RunResultSubmitResponseJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"accepted\":false," +
                "\"resultStatus\":\"PENDING\"," +
                "\"rewardClaimId\":null," +
                "\"rewardPreviewId\":null," +
                "\"serverReceivedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"message\":null," +
                "\"errorCode\":null}"));
        }

        [Test]
        public void ToJson_RejectsNullResponse()
        {
            Assert.Throws<ArgumentNullException>(() => RunResultSubmitResponseJsonAdapter.ToJson(null));
        }

        private static DateTime ReceivedAt()
        {
            return new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc);
        }
    }
}
