using System;

namespace LastRail.Gacha
{
    public sealed class GachaPullDiagnosticFlowResult
    {
        public GachaPullDiagnosticFlowResult(
            GachaPullClientResult clientResult,
            GachaPullUxState uxState,
            GachaPullDiagnosticWriteResult diagnosticWrite)
        {
            ClientResult = clientResult ?? throw new ArgumentNullException(nameof(clientResult));
            UxState = uxState ?? throw new ArgumentNullException(nameof(uxState));
            DiagnosticWrite = diagnosticWrite ?? throw new ArgumentNullException(nameof(diagnosticWrite));
        }

        public GachaPullClientResult ClientResult { get; }
        public GachaPullUxState UxState { get; }
        public GachaPullDiagnosticWriteResult DiagnosticWrite { get; }
        public bool IsBlockingGachaPull => false;
    }
}
