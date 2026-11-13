using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullUxState
    {
        private GachaPullUxState(
            GachaPullUxStateKind kind,
            GachaPullClientOutcome clientOutcome,
            GachaPullBatchPresentation batch,
            GachaPityDisplayState postPityState,
            GachaPullCostDto cost,
            GachaPullRejectionRecovery rejectionRecovery,
            string errorCode,
            string errorMessage,
            bool canRetryWithSameIdempotencyKey,
            bool requiresAuthRefreshOrGuestLogin)
        {
            Kind = kind;
            ClientOutcome = clientOutcome;
            Batch = batch;
            PostPityState = postPityState;
            Cost = cost;
            RejectionRecovery = rejectionRecovery;
            ErrorCode = OptionalText(errorCode);
            ErrorMessage = OptionalText(errorMessage);
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
        }

        public GachaPullUxStateKind Kind { get; }
        public GachaPullClientOutcome ClientOutcome { get; }
        public GachaPullBatchPresentation Batch { get; }
        public GachaPityDisplayState PostPityState { get; }
        public GachaPullCostDto Cost { get; }
        public GachaPullRejectionRecovery RejectionRecovery { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static GachaPullUxState FromClientResult(GachaPullClientResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.Outcome)
            {
                case GachaPullClientOutcome.Accepted:
                    return FromSuccessfulResponse(
                        GachaPullUxStateKind.ResultReviewReady,
                        GachaPullClientOutcome.Accepted,
                        result.Response,
                        GachaPullRejectionRecovery.None);
                case GachaPullClientOutcome.DuplicateReplay:
                    return FromSuccessfulResponse(
                        GachaPullUxStateKind.RecognizedDuplicate,
                        GachaPullClientOutcome.DuplicateReplay,
                        result.Response,
                        GachaPullRejectionRecovery.None);
                case GachaPullClientOutcome.Rejected:
                    return FromRejectedResponse(result.Response);
                case GachaPullClientOutcome.NetworkError:
                    return new GachaPullUxState(
                        GachaPullUxStateKind.RetryableNetworkError,
                        GachaPullClientOutcome.NetworkError,
                        null,
                        null,
                        null,
                        GachaPullRejectionRecovery.None,
                        null,
                        result.ErrorMessage,
                        true,
                        false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, "Unsupported gacha pull client outcome.");
            }
        }

        private static GachaPullUxState FromSuccessfulResponse(
            GachaPullUxStateKind kind,
            GachaPullClientOutcome outcome,
            GachaPullResponseDto response,
            GachaPullRejectionRecovery rejectionRecovery)
        {
            return new GachaPullUxState(
                kind,
                outcome,
                response.ToBatchPresentation(DefaultDisplayName),
                response.ToPostPityDisplayState(),
                response.Cost,
                rejectionRecovery,
                response.ErrorCode,
                response.Message,
                false,
                false);
        }

        private static GachaPullUxState FromRejectedResponse(GachaPullResponseDto response)
        {
            if (string.Equals(response.ErrorCode, "auth_required", StringComparison.OrdinalIgnoreCase))
            {
                return new GachaPullUxState(
                    GachaPullUxStateKind.AuthRequired,
                    GachaPullClientOutcome.Rejected,
                    null,
                    null,
                    response.Cost,
                    GachaPullRejectionRecovery.AuthRequired,
                    response.ErrorCode,
                    response.Message,
                    false,
                    true);
            }

            if (IsRecoverableRejection(response.ErrorCode))
            {
                return new GachaPullUxState(
                    GachaPullUxStateKind.RecoverableRejected,
                    GachaPullClientOutcome.Rejected,
                    null,
                    null,
                    response.Cost,
                    GachaPullRejectionRecovery.Recoverable,
                    response.ErrorCode,
                    response.Message,
                    false,
                    false);
            }

            return new GachaPullUxState(
                GachaPullUxStateKind.FatalRejected,
                GachaPullClientOutcome.Rejected,
                null,
                null,
                response.Cost,
                GachaPullRejectionRecovery.Unrecoverable,
                response.ErrorCode,
                response.Message,
                false,
                false);
        }

        private static bool IsRecoverableRejection(string errorCode)
        {
            return string.Equals(errorCode, "insufficient_command_ticket", StringComparison.OrdinalIgnoreCase)
                || string.Equals(errorCode, "banner_inactive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(errorCode, "gacha_pull_not_implemented", StringComparison.OrdinalIgnoreCase);
        }

        private static string DefaultDisplayName(GachaPullResultDto result)
        {
            return result.Reward.Id;
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
