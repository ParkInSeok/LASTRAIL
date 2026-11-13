using System.Threading.Tasks;

namespace LastRail.Run
{
    public interface IRunApiFlowDiagnosticLogSink
    {
        Task<RunApiFlowDiagnosticLogSinkResult> WriteAsync(RunApiFlowDiagnosticSnapshot snapshot);

        Task<RunApiFlowDiagnosticLogSinkResult> WriteJsonAsync(string diagnosticJson);
    }
}
