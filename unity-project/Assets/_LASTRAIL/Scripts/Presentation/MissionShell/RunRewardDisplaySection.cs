using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RunRewardDisplaySection
    {
        private RunRewardDisplaySection(
            RunEndUxStateKind? kind,
            RunApiFlowSubmitOutcome? submitOutcome,
            string recognizedRewardPreviewId,
            RunStaminaRefundMarker staminaRefund,
            RunRewardPreviewClientOutcome? previewOutcome,
            string previewStatus,
            string claimability,
            RunRewardClaimClientOutcome? claimOutcome,
            string claimStatus,
            IEnumerable<RewardStoryRewardLinePresentation> rewardLines,
            string errorCode,
            string errorMessage,
            bool canRetryWithSameIdempotencyKey,
            bool canRetryNetwork,
            bool requiresAuthRefreshOrGuestLogin)
        {
            Kind = kind;
            SubmitOutcome = submitOutcome;
            RecognizedRewardPreviewId = OptionalText(recognizedRewardPreviewId);
            StaminaRefund = staminaRefund;
            PreviewOutcome = previewOutcome;
            PreviewStatus = OptionalText(previewStatus);
            Claimability = OptionalText(claimability);
            ClaimOutcome = claimOutcome;
            ClaimStatus = OptionalText(claimStatus);
            RewardLines = CopyRewardLines(rewardLines);
            ErrorCode = OptionalText(errorCode);
            ErrorMessage = OptionalText(errorMessage);
            CanRetryWithSameIdempotencyKey = canRetryWithSameIdempotencyKey;
            CanRetryNetwork = canRetryNetwork;
            RequiresAuthRefreshOrGuestLogin = requiresAuthRefreshOrGuestLogin;
        }

        public RunEndUxStateKind? Kind { get; }
        public RunApiFlowSubmitOutcome? SubmitOutcome { get; }
        public string RecognizedRewardPreviewId { get; }
        public bool HasRecognizedRewardPreviewId => RecognizedRewardPreviewId != null;
        public RunStaminaRefundMarker StaminaRefund { get; }
        public bool HasStaminaRefund => StaminaRefund != null;
        public string StaminaRefundStatus => StaminaRefund == null ? null : StaminaRefund.Status;
        public string StaminaRefundMessage => StaminaRefund == null ? null : StaminaRefund.Message;
        public RunRewardPreviewClientOutcome? PreviewOutcome { get; }
        public string PreviewStatus { get; }
        public string Claimability { get; }
        public RunRewardClaimClientOutcome? ClaimOutcome { get; }
        public string ClaimStatus { get; }
        public IReadOnlyList<RewardStoryRewardLinePresentation> RewardLines { get; }
        public bool HasBalanceAfterDiagnostic => HasBalanceAfter(RewardLines);
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public bool CanRetryWithSameIdempotencyKey { get; }
        public bool CanRetryNetwork { get; }
        public bool RequiresAuthRefreshOrGuestLogin { get; }
        public bool HasRewardAmounts => false;

        public static RunRewardDisplaySection Create(
            RunEndUxState endUxState,
            RunRewardPreviewClientResult previewResult,
            RunRewardClaimClientResult claimResult)
        {
            var rewardLines = new List<RewardStoryRewardLinePresentation>();
            var preview = previewResult == null ? null : previewResult.Preview;
            var claimResponse = claimResult == null ? null : claimResult.Response;

            if (claimResponse != null && claimResponse.RewardLines.Count > 0)
            {
                foreach (var line in claimResponse.RewardLines)
                {
                    rewardLines.Add(RewardStoryRewardLinePresentation.FromRunRewardLine(line));
                }
            }
            else if (preview != null)
            {
                foreach (var line in preview.RewardLines)
                {
                    rewardLines.Add(RewardStoryRewardLinePresentation.FromRunRewardLine(line));
                }
            }

            return new RunRewardDisplaySection(
                endUxState == null ? (RunEndUxStateKind?)null : endUxState.Kind,
                endUxState == null ? (RunApiFlowSubmitOutcome?)null : endUxState.SubmitOutcome,
                endUxState == null ? null : endUxState.RecognizedRewardPreviewId,
                endUxState == null ? null : endUxState.StaminaRefund,
                previewResult == null ? (RunRewardPreviewClientOutcome?)null : previewResult.Outcome,
                preview == null ? null : preview.PreviewStatus.ToString(),
                preview == null ? null : preview.Claimability.ToString(),
                claimResult == null ? (RunRewardClaimClientOutcome?)null : claimResult.Outcome,
                claimResponse == null ? null : claimResponse.ClaimResultStatus.ToString(),
                rewardLines,
                ResolveErrorCode(endUxState, previewResult, claimResponse),
                ResolveErrorMessage(endUxState, previewResult, claimResult, claimResponse),
                (endUxState != null && endUxState.CanRetryWithSameIdempotencyKey) || (claimResult != null && claimResult.Outcome == RunRewardClaimClientOutcome.NetworkError),
                (previewResult != null && previewResult.Outcome == RunRewardPreviewClientOutcome.NetworkError) || (claimResult != null && claimResult.Outcome == RunRewardClaimClientOutcome.NetworkError),
                IsAuthRequired(previewResult, claimResponse));
        }

        private static string ResolveErrorCode(
            RunEndUxState endUxState,
            RunRewardPreviewClientResult previewResult,
            RunRewardClaimResponseDto claimResponse)
        {
            if (endUxState != null && endUxState.ErrorCode != null)
            {
                return endUxState.ErrorCode;
            }

            if (previewResult != null && previewResult.ErrorCode != null)
            {
                return previewResult.ErrorCode;
            }

            return claimResponse == null ? null : claimResponse.ErrorCode;
        }

        private static string ResolveErrorMessage(
            RunEndUxState endUxState,
            RunRewardPreviewClientResult previewResult,
            RunRewardClaimClientResult claimResult,
            RunRewardClaimResponseDto claimResponse)
        {
            if (endUxState != null && endUxState.ErrorMessage != null)
            {
                return endUxState.ErrorMessage;
            }

            if (previewResult != null && previewResult.ErrorMessage != null)
            {
                return previewResult.ErrorMessage;
            }

            if (claimResult != null && claimResult.ErrorMessage != null)
            {
                return claimResult.ErrorMessage;
            }

            return claimResponse == null ? null : claimResponse.Message;
        }

        private static bool IsAuthRequired(RunRewardPreviewClientResult previewResult, RunRewardClaimResponseDto claimResponse)
        {
            return string.Equals(previewResult == null ? null : previewResult.ErrorCode, "auth_required", StringComparison.Ordinal) ||
                string.Equals(claimResponse == null ? null : claimResponse.ErrorCode, "auth_required", StringComparison.Ordinal);
        }

        private static IReadOnlyList<RewardStoryRewardLinePresentation> CopyRewardLines(IEnumerable<RewardStoryRewardLinePresentation> rewardLines)
        {
            var copy = new List<RewardStoryRewardLinePresentation>();
            foreach (var line in rewardLines)
            {
                if (line == null)
                {
                    throw new ArgumentException("Reward line cannot be null.", nameof(rewardLines));
                }

                copy.Add(line);
            }

            return copy.AsReadOnly();
        }

        private static bool HasBalanceAfter(IEnumerable<RewardStoryRewardLinePresentation> rewardLines)
        {
            foreach (var line in rewardLines)
            {
                if (line.HasBalanceAfter)
                {
                    return true;
                }
            }

            return false;
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
