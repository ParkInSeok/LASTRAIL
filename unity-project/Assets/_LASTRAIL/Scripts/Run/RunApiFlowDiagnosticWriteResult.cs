using System;

namespace LastRail.Run
{
    public sealed class RunApiFlowDiagnosticWriteResult
    {
        public RunApiFlowDiagnosticWriteResult(
            RunApiFlowDiagnosticSnapshot snapshot,
            bool wasSinkProvided,
            bool wasWriteAttempted,
            RunApiFlowDiagnosticLogSinkResult sinkResult)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Snapshot = snapshot;
            WasSinkProvided = wasSinkProvided;
            WasWriteAttempted = wasWriteAttempted;
            SinkResult = sinkResult;
        }

        public RunApiFlowDiagnosticSnapshot Snapshot { get; }
        public bool WasSinkProvided { get; }
        public bool WasWriteAttempted { get; }
        public RunApiFlowDiagnosticLogSinkResult SinkResult { get; }
        public bool IsBlockingRunSubmit => false;
    }
}
