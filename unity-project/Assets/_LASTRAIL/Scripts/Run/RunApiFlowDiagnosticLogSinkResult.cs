using System;

namespace LastRail.Run
{
    public sealed class RunApiFlowDiagnosticLogSinkResult
    {
        private RunApiFlowDiagnosticLogSinkResult(
            RunApiFlowDiagnosticLogSinkOutcome outcome,
            string message,
            bool canRetryLater)
        {
            Outcome = outcome;
            Message = message;
            CanRetryLater = canRetryLater;
        }

        public RunApiFlowDiagnosticLogSinkOutcome Outcome { get; }
        public string Message { get; }
        public bool CanRetryLater { get; }
        public bool IsBlockingRunSubmit => false;

        public static RunApiFlowDiagnosticLogSinkResult Written()
        {
            return new RunApiFlowDiagnosticLogSinkResult(RunApiFlowDiagnosticLogSinkOutcome.Written, null, false);
        }

        public static RunApiFlowDiagnosticLogSinkResult Buffered(string message)
        {
            RequireMessage(message, nameof(message));
            return new RunApiFlowDiagnosticLogSinkResult(RunApiFlowDiagnosticLogSinkOutcome.Buffered, message, true);
        }

        public static RunApiFlowDiagnosticLogSinkResult Failed(string message)
        {
            RequireMessage(message, nameof(message));
            return new RunApiFlowDiagnosticLogSinkResult(RunApiFlowDiagnosticLogSinkOutcome.Failed, message, true);
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
