using System;

namespace LastRail.Run
{
    public sealed class RunResultSubmitRequest
    {
        public const string HttpMethod = "POST";
        public const string ResultSubmitEndpointPathTemplate = "/v1/runs/{runId}/finish";
        public const string IdempotencyHeaderNameConstant = "Idempotency-Key";

        private RunResultSubmitRequest(string runId, string idempotencyKey, string bodyJson)
        {
            RequireText(runId, nameof(runId));
            RequireText(idempotencyKey, nameof(idempotencyKey));
            RequireText(bodyJson, nameof(bodyJson));

            RunId = runId;
            IdempotencyKey = idempotencyKey;
            BodyJson = bodyJson;
        }

        public string Method => HttpMethod;
        public string EndpointPath => "/v1/runs/" + RunId + "/finish";
        public string RunId { get; }
        public string IdempotencyKey { get; }
        public string IdempotencyHeaderName => IdempotencyHeaderNameConstant;
        public string IdempotencyHeaderValue => IdempotencyKey;
        public string BodyJson { get; }

        public static RunResultSubmitRequest FromRuntimeState(RunRuntimeState runtimeState, string idempotencyKey)
        {
            return FromRuntimeState(runtimeState, idempotencyKey, RunResultOutcome.Victory, DateTime.UtcNow);
        }

        public static RunResultSubmitRequest FromRuntimeState(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            RunResultOutcome outcome,
            DateTime clientCompletedAt)
        {
            return FromRuntimeState(runtimeState, idempotencyKey, outcome, clientCompletedAt, RunEncounterType.Ordinary15Wave, null);
        }

        public static RunResultSubmitRequest FromRuntimeState(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            RunResultOutcome outcome,
            DateTime clientCompletedAt,
            RunEncounterType encounterType,
            RaidBossRunResult raidResult)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            var export = RunRuntimeStateExporter.ToResultSubmitExport(runtimeState);
            return FromExport(export, idempotencyKey, outcome, clientCompletedAt, encounterType, raidResult);
        }

        public static RunResultSubmitRequest FromExport(RunRuntimeStateExport export, string idempotencyKey)
        {
            return FromExport(export, idempotencyKey, RunResultOutcome.Victory, DateTime.UtcNow);
        }

        public static RunResultSubmitRequest FromExport(
            RunRuntimeStateExport export,
            string idempotencyKey,
            RunResultOutcome outcome,
            DateTime clientCompletedAt)
        {
            return FromExport(export, idempotencyKey, outcome, clientCompletedAt, RunEncounterType.Ordinary15Wave, null);
        }

        public static RunResultSubmitRequest FromExport(
            RunRuntimeStateExport export,
            string idempotencyKey,
            RunResultOutcome outcome,
            DateTime clientCompletedAt,
            RunEncounterType encounterType,
            RaidBossRunResult raidResult)
        {
            if (export == null)
            {
                throw new ArgumentNullException(nameof(export));
            }

            return new RunResultSubmitRequest(
                export.RunId,
                idempotencyKey,
                RunRuntimeStateResultSubmitJsonAdapter.ToJson(export, outcome, clientCompletedAt, encounterType, raidResult));
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }
        }
    }
}
