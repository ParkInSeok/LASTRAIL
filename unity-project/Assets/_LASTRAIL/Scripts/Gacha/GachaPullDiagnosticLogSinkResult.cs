using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullDiagnosticLogSinkResult
    {
        private GachaPullDiagnosticLogSinkResult(
            GachaPullDiagnosticLogSinkOutcome outcome,
            string message,
            bool canRetryLater)
        {
            Outcome = outcome;
            Message = message;
            CanRetryLater = canRetryLater;
        }

        public GachaPullDiagnosticLogSinkOutcome Outcome { get; }
        public string Message { get; }
        public bool CanRetryLater { get; }
        public bool IsBlockingGachaPull => false;

        public static GachaPullDiagnosticLogSinkResult Written()
        {
            return new GachaPullDiagnosticLogSinkResult(GachaPullDiagnosticLogSinkOutcome.Written, null, false);
        }

        public static GachaPullDiagnosticLogSinkResult Buffered(string message)
        {
            RequireMessage(message, nameof(message));
            return new GachaPullDiagnosticLogSinkResult(GachaPullDiagnosticLogSinkOutcome.Buffered, message, true);
        }

        public static GachaPullDiagnosticLogSinkResult Failed(string message)
        {
            RequireMessage(message, nameof(message));
            return new GachaPullDiagnosticLogSinkResult(GachaPullDiagnosticLogSinkOutcome.Failed, message, true);
        }

        private static void RequireMessage(string message, string name)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
