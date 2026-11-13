using System;
using LastRail.Gacha;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullApiContractTests
    {
        [Test]
        public void PullRequest_CreatesPostRequestWithPathBannerIdHeaderAndPullCountOnlyBody()
        {
            var request = GachaPullRequest.Create("banner_act1_launch", GachaPullCount.Ten, "gacha_attempt_001");

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.EndpointPath, Is.EqualTo("/v1/gacha/banners/banner_act1_launch/pulls"));
            Assert.That(request.IdempotencyHeaderName, Is.EqualTo("Idempotency-Key"));
            Assert.That(request.IdempotencyHeaderValue, Is.EqualTo("gacha_attempt_001"));
            Assert.That(request.BodyJson, Is.EqualTo("{\"pullCount\":10}"));
            Assert.That(request.BodyJson.Contains("bannerId"), Is.EqualTo(false));
        }

        [Test]
        public void ResponseJson_MapsAcceptedPullWithoutClientSideEconomyLogic()
        {
            var response = GachaPullResponseJsonAdapter.FromJson(AcceptedJson("ACCEPTED"));

            Assert.That(response.ResultStatus, Is.EqualTo(GachaPullResultStatus.Accepted));
            Assert.That(response.Accepted, Is.EqualTo(true));
            Assert.That(response.PullCount.Value, Is.EqualTo(10));
            Assert.That(response.CreatedAt, Is.EqualTo(new DateTime(2026, 8, 26, 14, 20, 0, 123, DateTimeKind.Utc)));
            Assert.That(response.Cost.ItemId, Is.EqualTo("cur_command_ticket"));
            Assert.That(response.Cost.Quantity, Is.EqualTo(10));
            Assert.That(response.PostPityState.Revision, Is.EqualTo(7));
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Results[0].PullSequence, Is.EqualTo(1));
            Assert.That(response.Results[0].ResultRarity, Is.EqualTo(GachaRarity.Elite));
            Assert.That(response.Results[0].ResultType, Is.EqualTo(GachaPullResultKind.Operator));
            Assert.That(response.Results[0].Reward.Id, Is.EqualTo("op_vex"));
            Assert.That(response.Results[0].IsPickup, Is.EqualTo(true));
            Assert.That(response.Results[0].WasDuplicate, Is.EqualTo(false));
            Assert.That(response.Results[1].WasDuplicate, Is.EqualTo(true));
            Assert.That(response.Results[1].DuplicateConversion.ItemId, Is.EqualTo("cur_operator_token"));
            Assert.That(response.HasRewardAmounts, Is.EqualTo(false));
        }

        [Test]
        public void ResponseJson_MapsDuplicateReplayAndRejectedStatuses()
        {
            var duplicate = GachaPullResponseJsonAdapter.FromJson(AcceptedJson("DUPLICATE_REPLAY"));
            var rejected = GachaPullResponseJsonAdapter.FromJson(
                "{\"bannerId\":\"banner_act1_launch\",\"accepted\":false,\"batchId\":null,\"resultStatus\":\"REJECTED\",\"pullCount\":10,\"balanceVersion\":\"balance_v1\",\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":10},\"postPityState\":{\"totalPullCount\":40,\"elitePityCount\":10,\"pickupElitePityCount\":40,\"rareOrHigherPityCount\":0,\"revision\":7},\"results\":[],\"createdAt\":\"2026-08-26T14:20:00.123Z\",\"errorCode\":\"contract_violation\",\"message\":\"Invalid request.\"}");

            Assert.That(duplicate.ResultStatus, Is.EqualTo(GachaPullResultStatus.DuplicateReplay));
            Assert.That(duplicate.Results.Count, Is.EqualTo(2));
            Assert.That(rejected.ResultStatus, Is.EqualTo(GachaPullResultStatus.Rejected));
            Assert.That(rejected.Accepted, Is.EqualTo(false));
            Assert.That(rejected.BatchId, Is.EqualTo(null));
            Assert.That(rejected.ErrorCode, Is.EqualTo("contract_violation"));
        }

        [Test]
        public void ResponseDto_ConnectsToExistingDisplayAndPresentationModels()
        {
            var response = GachaPullResponseJsonAdapter.FromJson(SingleAcceptedJson());

            var pityDisplay = response.ToPostPityDisplayState();
            var batch = response.ToBatchPresentation(result => "표시명:" + result.Reward.Id);

            Assert.That(pityDisplay.BannerId, Is.EqualTo("banner_act1_launch"));
            Assert.That(pityDisplay.TotalPullCount, Is.EqualTo(40));
            Assert.That(pityDisplay.PullsUntilEliteGuarantee, Is.EqualTo(20));
            Assert.That(batch.BatchId, Is.EqualTo("batch_single_001"));
            Assert.That(batch.PullCount.Value, Is.EqualTo(1));
            Assert.That(batch.Results[0].DisplayName, Is.EqualTo("표시명:op_vex"));
        }

        [Test]
        public void Ports_RepresentServerResponseAndTransportNetworkErrorSeparately()
        {
            var response = GachaPullResponseJsonAdapter.FromJson(AcceptedJson("DUPLICATE_REPLAY"));
            var duplicate = GachaPullClientResult.DuplicateReplay(response);
            var network = GachaPullClientResult.NetworkError("timeout");

            Assert.That(duplicate.Outcome, Is.EqualTo(GachaPullClientOutcome.DuplicateReplay));
            Assert.That(duplicate.Response.ResultStatus, Is.EqualTo(GachaPullResultStatus.DuplicateReplay));
            Assert.That(network.Outcome, Is.EqualTo(GachaPullClientOutcome.NetworkError));
            Assert.That(network.Response, Is.EqualTo(null));
            Assert.That(network.ErrorMessage, Is.EqualTo("timeout"));
        }

        [Test]
        public void Contracts_RejectInvalidInputsAndUnknownStatuses()
        {
            Assert.Throws<ArgumentException>(() => GachaPullRequest.Create("", GachaPullCount.Single, "key"));
            Assert.Throws<ArgumentException>(() => GachaPullRequest.Create("banner", GachaPullCount.Single, ""));
            Assert.Throws<ArgumentException>(() => GachaPullResponseJsonAdapter.FromJson(
                "{\"bannerId\":\"banner_act1_launch\",\"accepted\":false,\"batchId\":null,\"resultStatus\":\"PENDING\",\"pullCount\":1,\"balanceVersion\":\"balance_v1\",\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":1},\"postPityState\":{\"totalPullCount\":0,\"elitePityCount\":0,\"pickupElitePityCount\":0,\"rareOrHigherPityCount\":0,\"revision\":1},\"results\":[],\"createdAt\":\"2026-08-26T14:20:00.123Z\",\"errorCode\":null,\"message\":null}"));
        }

        private static string AcceptedJson(string status)
        {
            return "{\"bannerId\":\"banner_act1_launch\"," +
                "\"accepted\":" + (status == "ACCEPTED" ? "true" : "false") + "," +
                "\"batchId\":\"batch_001\"," +
                "\"resultStatus\":\"" + status + "\"," +
                "\"pullCount\":10," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":10}," +
                "\"postPityState\":{\"totalPullCount\":40,\"elitePityCount\":10,\"pickupElitePityCount\":40,\"rareOrHigherPityCount\":0,\"revision\":7}," +
                "\"results\":[" +
                "{\"pullSequence\":1,\"resultRarity\":\"ELITE\",\"resultType\":\"OPERATOR\",\"resultId\":\"result_001\",\"reward\":{\"kind\":\"OPERATOR\",\"id\":\"op_vex\",\"quantity\":1},\"isPickup\":true,\"guaranteeType\":\"pickup_elite_pity\",\"wasDuplicate\":false,\"duplicateConversion\":null}," +
                "{\"pullSequence\":2,\"resultRarity\":\"RARE\",\"resultType\":\"OPERATOR_TOKEN\",\"resultId\":\"result_002\",\"reward\":{\"kind\":\"ITEM\",\"id\":\"cur_operator_token\",\"quantity\":5},\"isPickup\":false,\"guaranteeType\":null,\"wasDuplicate\":true,\"duplicateConversion\":{\"sourceId\":\"op_reina\",\"itemId\":\"cur_operator_token\",\"quantity\":5}}" +
                "],\"createdAt\":\"2026-08-26T14:20:00.123Z\",\"errorCode\":null,\"message\":null}";
        }

        private static string SingleAcceptedJson()
        {
            return "{\"bannerId\":\"banner_act1_launch\"," +
                "\"accepted\":true," +
                "\"batchId\":\"batch_single_001\"," +
                "\"resultStatus\":\"ACCEPTED\"," +
                "\"pullCount\":1," +
                "\"balanceVersion\":\"balance_v1\"," +
                "\"cost\":{\"itemId\":\"cur_command_ticket\",\"quantity\":1}," +
                "\"postPityState\":{\"totalPullCount\":40,\"elitePityCount\":10,\"pickupElitePityCount\":40,\"rareOrHigherPityCount\":0,\"revision\":7}," +
                "\"results\":[" +
                "{\"pullSequence\":1,\"resultRarity\":\"ELITE\",\"resultType\":\"OPERATOR\",\"resultId\":\"result_001\",\"reward\":{\"kind\":\"OPERATOR\",\"id\":\"op_vex\",\"quantity\":1},\"isPickup\":true,\"guaranteeType\":\"pickup_elite_pity\",\"wasDuplicate\":false,\"duplicateConversion\":null}" +
                "],\"createdAt\":\"2026-08-26T14:20:00.123Z\",\"errorCode\":null,\"message\":null}";
        }
    }
}
