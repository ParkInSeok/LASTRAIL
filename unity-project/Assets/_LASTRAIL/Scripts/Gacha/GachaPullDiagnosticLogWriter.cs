using System;
using System.Threading.Tasks;

namespace LastRail.Gacha
{
    public static class GachaPullDiagnosticLogWriter
    {
        public static async Task<GachaPullDiagnosticWriteResult> WriteJsonAsync(
            GachaPullUxState state,
            IGachaPullDiagnosticLogSink sink)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var json = GachaPullUxStateJsonAdapter.ToJson(state);
            if (sink == null)
            {
                return new GachaPullDiagnosticWriteResult(state, json, false, false, null);
            }

            var sinkResult = await sink.WriteJsonAsync(json);
            return new GachaPullDiagnosticWriteResult(state, json, true, true, sinkResult);
        }
    }
}
