using System.Threading.Tasks;

namespace LastRail.Gacha
{
    public interface IGachaPullDiagnosticLogSink
    {
        Task<GachaPullDiagnosticLogSinkResult> WriteAsync(GachaPullUxState state);

        Task<GachaPullDiagnosticLogSinkResult> WriteJsonAsync(string diagnosticJson);
    }
}
