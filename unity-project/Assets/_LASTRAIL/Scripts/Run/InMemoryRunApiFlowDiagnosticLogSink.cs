using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LastRail.Run
{
    public sealed class InMemoryRunApiFlowDiagnosticLogSink : IRunApiFlowDiagnosticLogSink
    {
        private const string RotationMessage = "In-memory diagnostic log retained the most recent entries.";

        private readonly List<RunApiFlowDiagnosticSnapshot> snapshots = new List<RunApiFlowDiagnosticSnapshot>();
        private readonly List<string> jsonEntries = new List<string>();

        public InMemoryRunApiFlowDiagnosticLogSink(int maxEntries)
        {
            if (maxEntries < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntries), "Value must be greater than zero.");
            }

            MaxEntries = maxEntries;
        }

        public int MaxEntries { get; }
        public IReadOnlyList<RunApiFlowDiagnosticSnapshot> Snapshots => snapshots;
        public IReadOnlyList<string> JsonEntries => jsonEntries;

        public Task<RunApiFlowDiagnosticLogSinkResult> WriteAsync(RunApiFlowDiagnosticSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var rotated = AddBounded(snapshots, snapshot);
            return Task.FromResult(ResultFor(rotated));
        }

        public Task<RunApiFlowDiagnosticLogSinkResult> WriteJsonAsync(string diagnosticJson)
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

        private static RunApiFlowDiagnosticLogSinkResult ResultFor(bool rotated)
        {
            return rotated
                ? RunApiFlowDiagnosticLogSinkResult.Buffered(RotationMessage)
                : RunApiFlowDiagnosticLogSinkResult.Written();
        }
    }
}
