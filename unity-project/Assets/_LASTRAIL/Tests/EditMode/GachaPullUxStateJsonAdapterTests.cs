using System;
using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullUxStateJsonAdapterTests
    {
        [Test]
        public void ToJson_WritesAcceptedResultReviewStateForDebug()
        {
            var state = GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(AcceptedResponse()));

            var json = GachaPullUxStateJsonAdapter.ToJson(state);

            Assert.That(json, Is.EqualTo(
                "{\"kind\":\"ResultReviewReady\"," +
                "\"clientOutcome\":\"Accepted\"," +
                "\"rejectionRecovery\":\"None\"," +
                "\"hasBatch\":true," +
                "\"batch\":{\"batchId\":\"batch_001\",\"bannerId\":\"banner_act1_launch\",\"pullCount\":1,\"results\":[{\"resultId\":\"result_001\",\"pullIndex\":1,\"resultKind\":\"Operator\",\"rarity\":\"Elite\",\"rewardId\":\"op_vex\",\"displayName\":\"op_vex\",\"isDuplicateConversion\":false,\"duplicateSourceId\":null,\"conversionItemId\":null,\"conversionQuantity\":0,\"guaranteeType\":null}]}," +
                "\"postPityState\":{\"bannerId\":\"banner_act1_launch\",\"totalPullCount\":11,\"elitePityCount\":1,\"pickupElitePityCount\":11,\"rareOrHigherPityCount\":1,\"remainingEliteGuaranteeCount\":29,\"remainingPickupEliteGuaranteeCount\":49,\"remainingRareOrHigherGuaranteeCount\":9}," +
                "\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":1}," +
                "\"errorCode\":null," +
                "\"errorMessage\":null," +
                "\"canRetryWithSameIdempotencyKey\":false," +
                "\"requiresAuthRefreshOrGuestLogin\":false," +
                "\"hasEconomyMutation\":false," +
                "\"hasClientSideRng\":false}"));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("creditAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_PreservesServerGuaranteeTypeAsDisplayFieldOnly()
        {
            var state = GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(
                AcceptedResponse(guaranteeType: "PICKUP_ELITE_60")));

            var json = GachaPullUxStateJsonAdapter.ToJson(state);

            Assert.That(state.Batch.Results[0].GuaranteeType, Is.EqualTo("PICKUP_ELITE_60"));
            Assert.That(json.Contains("\"guaranteeType\":\"PICKUP_ELITE_60\""), Is.EqualTo(true));
            Assert.That(json.Contains("rngSeed"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("weighted"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesDuplicateReplayAsRecognizedPreviousResult()
        {
            var state = GachaPullUxState.FromClientResult(GachaPullClientResult.DuplicateReplay(
                AcceptedResponse(GachaPullResultStatus.DuplicateReplay, false, "duplicate_replay")));

            var json = GachaPullUxStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"kind\":\"RecognizedDuplicate\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"clientOutcome\":\"DuplicateReplay\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasBatch\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"errorCode\":\"duplicate_replay\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"canRetryWithSameIdempotencyKey\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEconomyMutation\":false"), Is.EqualTo(true));
        }

        [Test]
        public void ToJson_WritesRejectedAndNetworkStatesForDebug()
        {
            var recoverable = GachaPullUxState.FromClientResult(GachaPullClientResult.Rejected(
                RejectedResponse("insufficient_command_ticket", "지휘권이 부족합니다.")));
            var authRequired = GachaPullUxState.FromClientResult(GachaPullClientResult.Rejected(
                RejectedResponse("auth_required", "Authorization required.")));
            var network = GachaPullUxState.FromClientResult(GachaPullClientResult.NetworkError("timeout"));

            var recoverableJson = GachaPullUxStateJsonAdapter.ToJson(recoverable);
            var authJson = GachaPullUxStateJsonAdapter.ToJson(authRequired);
            var networkJson = GachaPullUxStateJsonAdapter.ToJson(network);

            Assert.That(recoverableJson.Contains("\"kind\":\"RecoverableRejected\""), Is.EqualTo(true));
            Assert.That(recoverableJson.Contains("\"rejectionRecovery\":\"Recoverable\""), Is.EqualTo(true));
            Assert.That(recoverableJson.Contains("\"errorCode\":\"insufficient_command_ticket\""), Is.EqualTo(true));
            Assert.That(recoverableJson.Contains("\"hasBatch\":false"), Is.EqualTo(true));
            Assert.That(authJson.Contains("\"kind\":\"AuthRequired\""), Is.EqualTo(true));
            Assert.That(authJson.Contains("\"requiresAuthRefreshOrGuestLogin\":true"), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"kind\":\"RetryableNetworkError\""), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"errorMessage\":\"timeout\""), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"canRetryWithSameIdempotencyKey\":true"), Is.EqualTo(true));
            Assert.That(networkJson.Contains("\"hasClientSideRng\":false"), Is.EqualTo(true));
            Assert.That(networkJson.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(networkJson.Contains("rawRng"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_RejectsNullState()
        {
            Assert.Throws<ArgumentNullException>(() => GachaPullUxStateJsonAdapter.ToJson(null));
        }

        private static GachaPullResponseDto AcceptedResponse(
            GachaPullResultStatus resultStatus = GachaPullResultStatus.Accepted,
            bool accepted = true,
            string errorCode = null,
            string guaranteeType = null)
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                accepted,
                "batch_001",
                resultStatus,
                GachaPullCount.FromValue(1),
                "1",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPostPityStateDto(11, 1, 11, 1, 2),
                new[]
                {
                    new GachaPullResultDto(
                        1,
                        GachaRarity.Elite,
                        GachaPullResultKind.Operator,
                        "result_001",
                        new GachaRewardDto("OPERATOR", "op_vex", 1),
                        true,
                        guaranteeType,
                        false,
                        null)
                },
                new DateTime(2026, 8, 26, 6, 15, 0, DateTimeKind.Utc),
                errorCode,
                null);
        }

        private static GachaPullResponseDto RejectedResponse(string errorCode, string message)
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                false,
                null,
                GachaPullResultStatus.Rejected,
                GachaPullCount.FromValue(1),
                "1",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPostPityStateDto(10, 0, 10, 0, 1),
                new GachaPullResultDto[0],
                new DateTime(2026, 8, 26, 6, 15, 0, DateTimeKind.Utc),
                errorCode,
                message);
        }
    }
}
