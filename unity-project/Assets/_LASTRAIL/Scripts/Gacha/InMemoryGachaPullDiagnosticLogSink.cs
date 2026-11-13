using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LastRail.Gacha
{
    public sealed class InMemoryGachaPullDiagnosticLogSink : IGachaPullDiagnosticLogSink
    {
        private const string RotationMessage = "In-memory gacha pull diagnostic log retained the most recent entries.";

        private readonly List<GachaPullUxState> states = new List<GachaPullUxState>();
        private readonly List<string> jsonEntries = new List<string>();

        public InMemoryGachaPullDiagnosticLogSink(int maxEntries)
        {
            if (maxEntries < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntries), "Value must be greater than zero.");
            }

            MaxEntries = maxEntries;
        }

        public int MaxEntries { get; }
        public IReadOnlyList<GachaPullUxState> States => states;
        public IReadOnlyList<string> JsonEntries => jsonEntries;

        public Task<GachaPullDiagnosticLogSinkResult> WriteAsync(GachaPullUxState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var rotated = AddBounded(states, state);
            return Task.FromResult(ResultFor(rotated));
        }

        public Task<GachaPullDiagnosticLogSinkResult> WriteJsonAsync(string diagnosticJson)
        {
            if (string.IsNullOrWhiteSpace(diagnosticJson))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(diagnosticJson));
            }

            var rotated = AddBounded(jsonEntries, diagnosticJson);
            return Task.FromResult(ResultFor(rotated));
        }

        private bool AddBounded<T>(List<T> entries, T entry)
        {
            entries.Add(entry);
            if (entries.Count <= MaxEntries)
            {
                return false;
            }

            entries.RemoveAt(0);
            return true;
        }

        private static GachaPullDiagnosticLogSinkResult ResultFor(bool rotated)
        {
            return rotated
                ? GachaPullDiagnosticLogSinkResult.Buffered(RotationMessage)
                : GachaPullDiagnosticLogSinkResult.Written();
        }
    }
}
