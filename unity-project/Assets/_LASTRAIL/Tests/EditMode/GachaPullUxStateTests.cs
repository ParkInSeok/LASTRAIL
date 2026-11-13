using System;
using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullUxStateTests
    {
        [Test]
        public void FromClientResult_AcceptedCreatesResultReviewReadyState()
        {
            var response = AcceptedResponse();
            var result = GachaPullClientResult.Accepted(response);

            var state = GachaPullUxState.FromClientResult(result);

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.ResultReviewReady));
            Assert.That(state.ClientOutcome, Is.EqualTo(GachaPullClientOutcome.Accepted));
            Assert.That(state.Batch == null, Is.EqualTo(false));
            Assert.That(state.Batch.BatchId, Is.EqualTo("batch_001"));
            Assert.That(state.Batch.Results.Count, Is.EqualTo(1));
            Assert.That(state.PostPityState.BannerId, Is.EqualTo("banner_act1_launch"));
            Assert.That(state.Cost.ItemId, Is.EqualTo("cur_command_ticket"));
            Assert.That(state.Cost.Quantity, Is.EqualTo(1));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(state.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(state.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void FromClientResult_DuplicateReplayCreatesRecognizedPreviousResultState()
        {
            var response = AcceptedResponse(resultStatus: GachaPullResultStatus.DuplicateReplay, accepted: false, errorCode: "duplicate_replay");
            var result = GachaPullClientResult.DuplicateReplay(response);

            var state = GachaPullUxState.FromClientResult(result);

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.RecognizedDuplicate));
            Assert.That(state.ClientOutcome, Is.EqualTo(GachaPullClientOutcome.DuplicateReplay));
            Assert.That(state.Batch.BatchId, Is.EqualTo("batch_001"));
            Assert.That(state.ErrorCode, Is.EqualTo("duplicate_replay"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(state.HasEconomyMutation, Is.EqualTo(false));
        }

        [Test]
        public void FromClientResult_RejectedClassifiesInsufficientTicketAsRecoverableDisplayState()
        {
            var response = RejectedResponse("insufficient_command_ticket", "지휘권이 부족합니다.");
            var result = GachaPullClientResult.Rejected(response);

            var state = GachaPullUxState.FromClientResult(result);

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.RecoverableRejected));
            Assert.That(state.ClientOutcome, Is.EqualTo(GachaPullClientOutcome.Rejected));
            Assert.That(state.RejectionRecovery, Is.EqualTo(GachaPullRejectionRecovery.Recoverable));
            Assert.That(state.ErrorCode, Is.EqualTo("insufficient_command_ticket"));
            Assert.That(state.ErrorMessage, Is.EqualTo("지휘권이 부족합니다."));
            Assert.That(state.Batch == null, Is.EqualTo(true));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
        }

        [Test]
        public void FromClientResult_RejectedClassifiesAuthRequiredAsAuthRefreshState()
        {
            var response = RejectedResponse("auth_required", "Authorization required.");
            var result = GachaPullClientResult.Rejected(response);

            var state = GachaPullUxState.FromClientResult(result);

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.AuthRequired));
            Assert.That(state.RejectionRecovery, Is.EqualTo(GachaPullRejectionRecovery.AuthRequired));
            Assert.That(state.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
            Assert.That(state.ErrorCode, Is.EqualTo("auth_required"));
            Assert.That(state.HasEconomyMutation, Is.EqualTo(false));
        }

        [Test]
        public void FromClientResult_RejectedClassifiesUnknownErrorAsFatal()
        {
            var response = RejectedResponse("contract_violation", "Invalid request.");
            var result = GachaPullClientResult.Rejected(response);

            var state = GachaPullUxState.FromClientResult(result);

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.FatalRejected));
            Assert.That(state.RejectionRecovery, Is.EqualTo(GachaPullRejectionRecovery.Unrecoverable));
            Assert.That(state.ErrorCode, Is.EqualTo("contract_violation"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(false));
        }

        [Test]
        public void FromClientResult_NetworkErrorCreatesRetryableState()
        {
            var result = GachaPullClientResult.NetworkError("timeout");

            var state = GachaPullUxState.FromClientResult(result);

            Assert.That(state.Kind, Is.EqualTo(GachaPullUxStateKind.RetryableNetworkError));
            Assert.That(state.ClientOutcome, Is.EqualTo(GachaPullClientOutcome.NetworkError));
            Assert.That(state.ErrorMessage, Is.EqualTo("timeout"));
            Assert.That(state.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(state.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(false));
            Assert.That(state.Batch == null, Is.EqualTo(true));
        }

        [Test]
        public void FromClientResult_RejectsNullResult()
        {
            Assert.Throws<ArgumentNullException>(() => GachaPullUxState.FromClientResult(null));
        }

        private static GachaPullResponseDto AcceptedResponse(
            GachaPullResultStatus resultStatus = GachaPullResultStatus.Accepted,
            bool accepted = true,
            string errorCode = null)
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
                        null,
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
                new List<GachaPullResultDto>(),
                new DateTime(2026, 8, 26, 6, 15, 0, DateTimeKind.Utc),
                errorCode,
                message);
        }
    }
}
