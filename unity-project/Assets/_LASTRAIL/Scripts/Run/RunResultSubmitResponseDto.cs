using System;

namespace LastRail.Run
{
    public sealed class RunResultSubmitResponseDto
    {
        public RunResultSubmitResponseDto(
            string runId,
            bool accepted,
            string resultStatus,
            string rewardClaimId,
            string rewardPreviewId,
            DateTime serverReceivedAt,
            string balanceVersion,
            string message,
            string errorCode,
            RunStaminaRefundMarker staminaRefund = null)
        {
            RequireText(runId, nameof(runId));
            RequireText(resultStatus, nameof(resultStatus));
            RequireText(balanceVersion, nameof(balanceVersion));
            RequireKnownResultStatus(resultStatus);

            RunId = runId;
            Accepted = accepted;
            ResultStatus = resultStatus;
            RewardClaimId = OptionalText(rewardClaimId);
            RewardPreviewId = OptionalText(rewardPreviewId);
            ServerReceivedAt = serverReceivedAt;
            BalanceVersion = balanceVersion;
            Message = OptionalText(message);
            ErrorCode = OptionalText(errorCode);
            StaminaRefund = staminaRefund;
        }

        public string RunId { get; }
        public bool Accepted { get; }
        public string ResultStatus { get; }
        public string RewardClaimId { get; }
        public string RewardPreviewId { get; }
        public DateTime ServerReceivedAt { get; }
        public string BalanceVersion { get; }
        public string Message { get; }
        public string ErrorCode { get; }
        public RunStaminaRefundMarker StaminaRefund { get; }
        public bool HasStaminaRefund => StaminaRefund != null;

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }

        private static void RequireKnownResultStatus(string value)
        {
            if (!string.Equals(value, "ACCEPTED", StringComparison.Ordinal)
                && !string.Equals(value, "DUPLICATE_REPLAY", StringComparison.Ordinal)
                && !string.Equals(value, "REJECTED", StringComparison.Ordinal))
            {
                throw new ArgumentException("Unsupported result status.", nameof(value));
            }
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
