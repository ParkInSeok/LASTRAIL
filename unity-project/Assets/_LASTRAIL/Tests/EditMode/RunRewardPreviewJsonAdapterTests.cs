using System;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunRewardPreviewJsonAdapterTests
    {
        [Test]
        public void FromJson_MapsRewardPreviewForReviewUx()
        {
            var preview = RunRewardPreviewJsonAdapter.FromJson(
                "{\"runId\":\"run_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"status\":\"ACTIVE\"," +
                "\"expiresAt\":\"2026-08-27T12:40:00.123Z\"," +
                "\"serverCalculatedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"claimability\":\"CLAIMABLE\"," +
                "\"rewardLines\":[" +
                "{\"rewardLineId\":\"line_credits\",\"rewardCode\":\"cur_credits\",\"displayName\":\"크레딧\",\"quantity\":120,\"unitLabel\":\"개\"}," +
                "{\"rewardLineId\":\"line_parts\",\"rewardCode\":\"cur_machine_parts\",\"displayName\":\"기계 부품\",\"quantity\":4,\"unitLabel\":\"개\"}]," +
                "\"claim\":null}");

            Assert.That(preview.RunId, Is.EqualTo("run_001"));
            Assert.That(preview.RewardPreviewId, Is.EqualTo("preview_001"));
            Assert.That(preview.PreviewStatus, Is.EqualTo(RunRewardPreviewStatus.Active));
            Assert.That(preview.ExpiresAt, Is.EqualTo(new DateTime(2026, 8, 27, 12, 40, 0, 123, DateTimeKind.Utc)));
            Assert.That(preview.ServerCalculatedAt, Is.EqualTo(new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc)));
            Assert.That(preview.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(preview.Claimability, Is.EqualTo(RunRewardClaimability.Claimable));
            Assert.That(preview.RewardLines.Count, Is.EqualTo(2));
            Assert.That(preview.RewardLines[0].RewardCode, Is.EqualTo("cur_credits"));
            Assert.That(preview.RewardLines[0].DisplayName, Is.EqualTo("크레딧"));
            Assert.That(preview.RewardLines[0].Quantity, Is.EqualTo(120));
        }

        [Test]
        public void ToJson_WritesStablePreviewFieldsWithoutClaimDecision()
        {
            var preview = new RunRewardPreviewDto(
                "run_001",
                "preview_001",
                RunRewardPreviewStatus.Active,
                new DateTime(2026, 8, 27, 12, 40, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 26, 12, 40, 0, DateTimeKind.Utc),
                "balance_v1",
                RunRewardClaimability.Claimable,
                new[]
                {
                    new RunRewardLineDto("line_credits", "cur_credits", "크레딧", 120, "개")
                });

            var json = RunRewardPreviewJsonAdapter.ToJson(preview);

            Assert.That(json, Is.EqualTo(
                "{\"runId\":\"run_001\"," +
                "\"rewardPreviewId\":\"preview_001\"," +
                "\"status\":\"ACTIVE\"," +
                "\"expiresAt\":\"2026-08-27T12:40:00.0000000Z\"," +
                "\"serverCalculatedAt\":\"2026-08-26T12:40:00.0000000Z\"," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"claimability\":\"CLAIMABLE\"," +
                "\"rewardLines\":[{\"rewardLineId\":\"line_credits\",\"rewardCode\":\"cur_credits\",\"displayName\":\"크레딧\",\"quantity\":120,\"unitLabel\":\"개\"}]}"));
            Assert.That(json.Contains("rewardClaimId"), Is.EqualTo(false));
        }

        [Test]
        public void FromJson_RejectsUnknownStatusOrNonUtcTimes()
        {
            Assert.Throws<ArgumentException>(() => RunRewardPreviewJsonAdapter.FromJson(
                "{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\",\"status\":\"PENDING\",\"expiresAt\":null,\"serverCalculatedAt\":\"2026-08-26T12:40:00.0000000Z\",\"balanceVersion\":\"balance_v1\",\"claimability\":\"CLAIMABLE\",\"rewardLines\":[]}"));
            Assert.Throws<FormatException>(() => RunRewardPreviewJsonAdapter.FromJson(
                "{\"runId\":\"run_001\",\"rewardPreviewId\":\"preview_001\",\"status\":\"ACTIVE\",\"expiresAt\":\"2026-08-27T21:40:00.0000000+09:00\",\"serverCalculatedAt\":\"2026-08-26T12:40:00.0000000Z\",\"balanceVersion\":\"balance_v1\",\"claimability\":\"CLAIMABLE\",\"rewardLines\":[]}"));
        }

        [Test]
        public void Dto_CopiesRewardLinesAndRejectsInvalidRewardLines()
        {
            var lines = new[] { new RunRewardLineDto("line_credits", "cur_credits", "크레딧", 120, "개") };
            var preview = new RunRewardPreviewDto("run_001", "preview_001", RunRewardPreviewStatus.Active, null, DateTime.UtcNow, "balance_v1", RunRewardClaimability.Claimable, lines);

            lines[0] = new RunRewardLineDto("line_parts", "cur_machine_parts", "기계 부품", 4, "개");

            Assert.That(preview.RewardLines[0].RewardCode, Is.EqualTo("cur_credits"));
            Assert.Throws<ArgumentException>(() => new RunRewardLineDto("line_bad", "cur_credits", "크레딧", -1, "개"));
        }
    }
}
