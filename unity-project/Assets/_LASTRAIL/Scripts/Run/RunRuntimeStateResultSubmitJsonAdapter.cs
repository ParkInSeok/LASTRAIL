using System;
using System.Globalization;
using System.Text;
using LastRail.Sortie;

namespace LastRail.Run
{
    public static class RunRuntimeStateResultSubmitJsonAdapter
    {
        private const string UtcRoundTripFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

        public static string ToJson(RunRuntimeState runtimeState)
        {
            return ToJson(runtimeState, RunResultOutcome.Victory, DateTime.UtcNow);
        }

        public static string ToJson(RunRuntimeState runtimeState, RunResultOutcome outcome, DateTime clientCompletedAt)
        {
            return ToJson(runtimeState, outcome, clientCompletedAt, RunEncounterType.Ordinary15Wave, null);
        }

        public static string ToJson(
            RunRuntimeState runtimeState,
            RunResultOutcome outcome,
            DateTime clientCompletedAt,
            RunEncounterType encounterType,
            RaidBossRunResult raidResult)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            return ToJson(RunRuntimeStateExporter.ToResultSubmitExport(runtimeState), outcome, clientCompletedAt, encounterType, raidResult);
        }

        public static string ToJson(RunRuntimeStateExport export)
        {
            return ToJson(export, RunResultOutcome.Victory, DateTime.UtcNow);
        }

        public static string ToJson(RunRuntimeStateExport export, RunResultOutcome outcome, DateTime clientCompletedAt)
        {
            return ToJson(export, outcome, clientCompletedAt, RunEncounterType.Ordinary15Wave, null);
        }

        public static string ToJson(
            RunRuntimeStateExport export,
            RunResultOutcome outcome,
            DateTime clientCompletedAt,
            RunEncounterType encounterType,
            RaidBossRunResult raidResult)
        {
            if (export == null)
            {
                throw new ArgumentNullException(nameof(export));
            }

            EnsureCompleted(export);
            EnsureUtc(clientCompletedAt, nameof(clientCompletedAt));
            EnsureEncounterPayload(encounterType, raidResult);

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", export.RunId).Append(',');
            AppendString(builder, "encounterType", RunEncounterTypeFormatter.ToServerValue(encounterType)).Append(',');
            AppendString(builder, "currentStage", export.CurrentStage.ToString()).Append(',');
            builder.Append("\"currentWaveNumber\":").Append(export.CurrentWaveNumber.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"completedWaveCount\":").Append(export.CompletedWaveCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "outcome", OutcomeToServerValue(outcome)).Append(',');
            if (raidResult != null) AppendRaidResult(builder, raidResult).Append(',');
            AppendString(builder, "runtimeStateJson", ToRuntimeStateJson(export)).Append(',');
            AppendTacticSelections(builder, export);
            builder.Append(',');
            AppendString(builder, "clientCompletedAt", clientCompletedAt.ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture));
            builder.Append('}');
            return builder.ToString();
        }

        private static string ToRuntimeStateJson(RunRuntimeStateExport export)
        {
            EnsureCompleted(export);

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", export.RunId).Append(',');
            AppendString(builder, "balanceVersion", export.BalanceVersion).Append(',');
            AppendString(builder, "operationId", export.OperationId).Append(',');
            AppendString(builder, "actId", export.ActId).Append(',');
            AppendString(builder, "regionId", export.RegionId).Append(',');
            AppendString(builder, "enemyFactionId", export.EnemyFactionId).Append(',');
            builder.Append("\"riskLevel\":").Append(export.RiskLevel.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "wavePlanId", export.WavePlanId).Append(',');
            builder.Append("\"seed\":").Append(export.Seed.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "rewardTableId", export.RewardTableId).Append(',');
            AppendOptionalString(builder, "startAugmentPackageRef", export.StartAugmentPackageRef).Append(',');
            AppendOptionalString(builder, "startTacticPackageRef", export.StartTacticPackageRef).Append(',');
            AppendString(builder, "serverStartedAt", export.ServerStartedAt.ToUniversalTime().ToString(UtcRoundTripFormat, CultureInfo.InvariantCulture)).Append(',');
            AppendString(builder, "currentStage", export.CurrentStage.ToString()).Append(',');
            builder.Append("\"currentWaveNumber\":").Append(export.CurrentWaveNumber.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"completedWaveCount\":").Append(export.CompletedWaveCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"finalOperationCompleted\":").Append(export.FinalOperationCompleted ? "true" : "false").Append(',');
            AppendTacticSelections(builder, export);
            builder.Append('}');
            return builder.ToString();
        }

        private static void EnsureCompleted(RunRuntimeStateExport export)
        {
            if (export.CurrentStage != SortieStage.Complete || !export.FinalOperationCompleted)
            {
                throw new InvalidOperationException("Run result submit JSON requires a completed runtime state export.");
            }
        }

        private static void EnsureEncounterPayload(RunEncounterType encounterType, RaidBossRunResult raidResult)
        {
            if (encounterType == RunEncounterType.RaidBoss && raidResult == null)
            {
                throw new ArgumentNullException(nameof(raidResult));
            }

            if (encounterType == RunEncounterType.Ordinary15Wave && raidResult != null)
            {
                throw new ArgumentException("Ordinary 15-wave run finish cannot include raid result.", nameof(raidResult));
            }
        }

        private static void EnsureUtc(DateTime value, string name)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be UTC.", name);
            }
        }

        private static string OutcomeToServerValue(RunResultOutcome outcome)
        {
            switch (outcome)
            {
                case RunResultOutcome.Victory:
                    return "VICTORY";
                case RunResultOutcome.Failure:
                    return "FAILURE";
                case RunResultOutcome.Abandoned:
                    return "ABANDONED";
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported run result outcome.");
            }
        }

        private static void AppendTacticSelections(StringBuilder builder, RunRuntimeStateExport export)
        {
            builder.Append("\"tacticSelections\":[");
            for (var i = 0; i < export.TacticSelections.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var selection = export.TacticSelections[i];
                builder.Append('{');
                builder.Append("\"waveNumber\":").Append(selection.WaveNumber.ToString(CultureInfo.InvariantCulture)).Append(',');
                AppendString(builder, "tacticId", selection.TacticId).Append(',');
                AppendString(builder, "tacticKind", selection.TacticKind.ToString()).Append(',');
                builder.Append("\"draftContainedUltimateTactic\":").Append(selection.DraftContainedUltimateTactic ? "true" : "false").Append(',');
                AppendOptionalString(builder, "packageRef", export.StartTacticPackageRef);
                builder.Append('}');
            }

            builder.Append(']');
        }

        private static StringBuilder AppendRaidResult(StringBuilder builder, RaidBossRunResult raidResult)
        {
            builder.Append("\"raidResult\":");
            if (raidResult == null)
            {
                builder.Append("null");
                return builder;
            }

            builder.Append('{');
            AppendString(builder, "raidId", raidResult.RaidId).Append(',');
            AppendString(builder, "bossId", raidResult.BossId).Append(',');
            builder.Append("\"bossDefeated\":").Append(raidResult.BossDefeated ? "true" : "false").Append(',');
            builder.Append("\"completedPhaseCount\":").Append(raidResult.CompletedPhaseCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append("\"encounterDurationSeconds\":").Append(raidResult.EncounterDurationSeconds.ToString(CultureInfo.InvariantCulture));
            builder.Append('}');
            return builder;
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            return builder.Append('"')
                .Append(fieldName)
                .Append("\":\"")
                .Append(Escape(value))
                .Append('"');
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
