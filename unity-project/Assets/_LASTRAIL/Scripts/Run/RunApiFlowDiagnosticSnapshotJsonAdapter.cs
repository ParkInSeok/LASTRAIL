using System;
using System.Globalization;
using System.Text;

namespace LastRail.Run
{
    public static class RunApiFlowDiagnosticSnapshotJsonAdapter
    {
        public static string ToJson(RunApiFlowDiagnosticSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var builder = new StringBuilder();
            builder.Append('{');
            AppendString(builder, "runId", snapshot.RunId).Append(',');
            AppendString(builder, "operationId", snapshot.OperationId).Append(',');
            AppendString(builder, "actId", snapshot.ActId).Append(',');
            AppendString(builder, "wavePlanId", snapshot.WavePlanId).Append(',');
            AppendString(builder, "balanceVersion", snapshot.BalanceVersion).Append(',');
            builder.Append("\"riskLevel\":").Append(snapshot.RiskLevel.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendOptionalString(builder, "startAugmentPackageRef", snapshot.StartAugmentPackageRef).Append(',');
            AppendOptionalString(builder, "startTacticPackageRef", snapshot.StartTacticPackageRef).Append(',');
            builder.Append("\"seed\":").Append(snapshot.Seed.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendStringArray(builder, "selectedOperatorIds", snapshot.SelectedOperatorIds).Append(',');
            AppendString(builder, "selectedUltimateOperatorId", snapshot.SelectedUltimateOperatorId).Append(',');
            AppendTrainCarSlots(builder, snapshot).Append(',');
            builder.Append("\"tacticSelectionCount\":").Append(snapshot.TacticSelectionCount.ToString(CultureInfo.InvariantCulture)).Append(',');
            AppendTacticSelections(builder, snapshot).Append(',');
            builder.Append("\"hasSubmitResult\":").Append(snapshot.HasSubmitResult ? "true" : "false").Append(',');
            AppendOptionalEnum(builder, "submitOutcome", snapshot.SubmitOutcome).Append(',');
            AppendOptionalEnum(builder, "submitAction", snapshot.SubmitAction).Append(',');
            builder.Append("\"hasIdempotencyKey\":").Append(snapshot.HasIdempotencyKey ? "true" : "false").Append(',');
            AppendOptionalString(builder, "rewardClaimId", snapshot.RewardClaimId).Append(',');
            AppendOptionalString(builder, "rewardPreviewId", snapshot.RewardPreviewId).Append(',');
            AppendOptionalString(builder, "submitErrorCode", snapshot.SubmitErrorCode).Append(',');
            AppendOptionalString(builder, "submitErrorMessage", snapshot.SubmitErrorMessage);
            builder.Append('}');
            return builder.ToString();
        }

        private static StringBuilder AppendStringArray(StringBuilder builder, string fieldName, System.Collections.Generic.IReadOnlyList<string> values)
        {
            builder.Append('"').Append(fieldName).Append("\":[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                AppendJsonString(builder, values[i]);
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendTrainCarSlots(StringBuilder builder, RunApiFlowDiagnosticSnapshot snapshot)
        {
            builder.Append("\"trainCarSlots\":[");
            for (var i = 0; i < snapshot.TrainCarSlots.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var slot = snapshot.TrainCarSlots[i];
                builder.Append('{');
                builder.Append("\"slotIndex\":").Append(slot.SlotIndex.ToString(CultureInfo.InvariantCulture)).Append(',');
                AppendString(builder, "trainCarId", slot.TrainCarId);
                builder.Append('}');
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendTacticSelections(StringBuilder builder, RunApiFlowDiagnosticSnapshot snapshot)
        {
            builder.Append("\"tacticSelections\":[");
            for (var i = 0; i < snapshot.TacticSelections.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var selection = snapshot.TacticSelections[i];
                builder.Append('{');
                builder.Append("\"waveNumber\":").Append(selection.WaveNumber.ToString(CultureInfo.InvariantCulture)).Append(',');
                AppendString(builder, "tacticId", selection.TacticId).Append(',');
                AppendString(builder, "tacticKind", selection.TacticKind.ToString()).Append(',');
                builder.Append("\"draftContainedUltimateTactic\":").Append(selection.DraftContainedUltimateTactic ? "true" : "false");
                builder.Append('}');
            }

            return builder.Append(']');
        }

        private static StringBuilder AppendOptionalEnum<T>(StringBuilder builder, string fieldName, T? value)
            where T : struct
        {
            if (!value.HasValue)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value.Value.ToString());
        }

        private static StringBuilder AppendString(StringBuilder builder, string fieldName, string value)
        {
            builder.Append('"').Append(fieldName).Append("\":");
            return AppendJsonString(builder, value);
        }

        private static StringBuilder AppendOptionalString(StringBuilder builder, string fieldName, string value)
        {
            if (value == null)
            {
                return builder.Append('"').Append(fieldName).Append("\":null");
            }

            return AppendString(builder, fieldName, value);
        }

        private static StringBuilder AppendJsonString(StringBuilder builder, string value)
        {
            return builder.Append('"').Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
