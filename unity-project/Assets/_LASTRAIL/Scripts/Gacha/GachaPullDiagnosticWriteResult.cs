using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullDiagnosticWriteResult
    {
        public GachaPullDiagnosticWriteResult(
            GachaPullUxState state,
            string diagnosticJson,
            bool wasSinkProvided,
            bool wasWriteAttempted,
            GachaPullDiagnosticLogSinkResult sinkResult)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(diagnosticJson))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(diagnosticJson));
            }

            DiagnosticJson = diagnosticJson;
            WasSinkProvided = wasSinkProvided;
            WasWriteAttempted = wasWriteAttempted;
            SinkResult = sinkResult;
        }

        public GachaPullUxState State { get; }
        public string DiagnosticJson { get; }
        public bool WasSinkProvided { get; }
        public bool WasWriteAttempted { get; }
        public GachaPullDiagnosticLogSinkResult SinkResult { get; }
        public bool IsBlockingGachaPull => false;
    }
}
