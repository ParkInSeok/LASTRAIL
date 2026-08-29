using System;

namespace LastRail.Run
{
    public sealed class RunStaminaRefundMarker
    {
        public RunStaminaRefundMarker(string status, string message)
        {
            Status = Optional(status);
            Message = Optional(message);
        }

        public string Status { get; }
        public string Message { get; }
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;

        private static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
