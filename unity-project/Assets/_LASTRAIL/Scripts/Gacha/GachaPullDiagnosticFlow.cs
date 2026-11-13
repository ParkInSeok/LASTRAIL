using System;
using System.Threading.Tasks;
using LastRail.Auth;

namespace LastRail.Gacha
{
    public static class GachaPullDiagnosticFlow
    {
        public static async Task<GachaPullDiagnosticFlowResult> PullForUxAndWriteDiagnosticAsync(
            GachaPullRequest request,
            GameApiAccessToken accessToken,
            IGachaPullClient client,
            IGachaPullDiagnosticLogSink sink)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var clientResult = await client.PullAsync(request, accessToken);
            var uxState = GachaPullUxState.FromClientResult(clientResult);
            var diagnosticWrite = await GachaPullDiagnosticLogWriter.WriteJsonAsync(uxState, sink);
            return new GachaPullDiagnosticFlowResult(clientResult, uxState, diagnosticWrite);
        }
    }
}
