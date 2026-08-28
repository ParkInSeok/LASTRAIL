using System;
using System.Collections.Generic;

namespace LastRail.Run
{
    public sealed class RunApiFlowDiagnosticSnapshot
    {
        private RunApiFlowDiagnosticSnapshot(
            RunRuntimeState runtimeState,
            RunApiFlowSubmitPolicy submitPolicy,
            string idempotencyKey)
        {
            var snapshot = runtimeState.Snapshot;

            RunId = snapshot.RunId;
            OperationId = snapshot.OperationId;
            ActId = snapshot.ActId;
            WavePlanId = snapshot.WavePlanId;
            BalanceVersion = snapshot.BalanceVersion;
            RiskLevel = snapshot.RiskLevel;
            StartAugmentPackageRef = snapshot.StartAugmentPackageRef;
            StartTacticPackageRef = snapshot.StartTacticPackageRef;
            Seed = snapshot.RngSeed;
            SelectedOperatorIds = CopyOperatorIds(snapshot.OperatorIds);
            SelectedUltimateOperatorId = snapshot.UltimateOperatorId;
            TrainCarSlots = CopyTrainCarSlots(snapshot.CarriageSlots);
            TacticSelections = CopyTacticSelections(runtimeState.TacticState.Selections);
            TacticSelectionCount = TacticSelections.Count;
            HasSubmitResult = submitPolicy != null;
            HasIdempotencyKey = !string.IsNullOrWhiteSpace(idempotencyKey);

            if (submitPolicy != null)
            {
                SubmitOutcome = ToNullable(submitPolicy);
                SubmitAction = ToNullable(submitPolicy.Action);
                RewardClaimId = submitPolicy.RewardClaimId;
                RewardPreviewId = submitPolicy.RewardPreviewId;
                SubmitErrorCode = submitPolicy.ErrorCode;
                SubmitErrorMessage = submitPolicy.ErrorMessage;
            }
        }

        public string RunId { get; }
        public string OperationId { get; }
        public string ActId { get; }
        public string WavePlanId { get; }
        public string BalanceVersion { get; }
        public int RiskLevel { get; }
        public string StartAugmentPackageRef { get; }
        public string StartTacticPackageRef { get; }
        public uint Seed { get; }
        public IReadOnlyList<string> SelectedOperatorIds { get; }
        public string SelectedUltimateOperatorId { get; }
        public IReadOnlyList<RunApiFlowTrainCarSlotDiagnostic> TrainCarSlots { get; }
        public IReadOnlyList<TacticSelectionEvent> TacticSelections { get; }
        public int TacticSelectionCount { get; }
        public bool HasSubmitResult { get; }
        public RunApiFlowSubmitOutcome? SubmitOutcome { get; }
        public RunApiFlowSubmitAction? SubmitAction { get; }
        public bool HasIdempotencyKey { get; }
        public string RewardClaimId { get; }
        public string RewardPreviewId { get; }
        public string SubmitErrorCode { get; }
        public string SubmitErrorMessage { get; }
        public bool HasRewardAmounts => false;

        public static RunApiFlowDiagnosticSnapshot FromRuntimeState(RunRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            return new RunApiFlowDiagnosticSnapshot(runtimeState, null, null);
        }

        public static RunApiFlowDiagnosticSnapshot FromSubmitPolicy(
            RunRuntimeState runtimeState,
            string idempotencyKey,
            RunApiFlowSubmitPolicy submitPolicy)
        {
            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            if (submitPolicy == null)
            {
                throw new ArgumentNullException(nameof(submitPolicy));
            }

            return new RunApiFlowDiagnosticSnapshot(runtimeState, submitPolicy, idempotencyKey);
        }

        private static IReadOnlyList<string> CopyOperatorIds(IReadOnlyList<string> operatorIds)
        {
            var copied = new string[operatorIds.Count];
            for (var i = 0; i < operatorIds.Count; i++)
            {
                copied[i] = operatorIds[i];
            }

            return copied;
        }

        private static IReadOnlyList<RunApiFlowTrainCarSlotDiagnostic> CopyTrainCarSlots(IReadOnlyList<CarriageSlotSnapshot> carriageSlots)
        {
            var copied = new RunApiFlowTrainCarSlotDiagnostic[carriageSlots.Count];
            for (var i = 0; i < carriageSlots.Count; i++)
            {
                copied[i] = new RunApiFlowTrainCarSlotDiagnostic(carriageSlots[i].SlotIndex, carriageSlots[i].CarriageId);
            }

            return copied;
        }

        private static IReadOnlyList<TacticSelectionEvent> CopyTacticSelections(IReadOnlyList<TacticSelectionEvent> selections)
        {
            var copied = new TacticSelectionEvent[selections.Count];
            for (var i = 0; i < selections.Count; i++)
            {
                copied[i] = selections[i];
            }

            return copied;
        }

        private static RunApiFlowSubmitOutcome? ToNullable(RunApiFlowSubmitPolicy submitPolicy)
        {
            switch (submitPolicy.Action)
            {
                case RunApiFlowSubmitAction.ShowRewardReview:
                    return RunApiFlowSubmitOutcome.Accepted;
                case RunApiFlowSubmitAction.SyncRecognizedResult:
                    return RunApiFlowSubmitOutcome.DuplicateReplay;
                case RunApiFlowSubmitAction.ShowRecoverableRejected:
                case RunApiFlowSubmitAction.ShowFatalRejected:
                    return RunApiFlowSubmitOutcome.Rejected;
                case RunApiFlowSubmitAction.RetryWithSameIdempotencyKey:
                    return RunApiFlowSubmitOutcome.NetworkError;
                default:
                    throw new ArgumentOutOfRangeException(nameof(submitPolicy), submitPolicy.Action, "Unsupported submit action.");
            }
        }

        private static RunApiFlowSubmitAction? ToNullable(RunApiFlowSubmitAction action)
        {
            return action;
        }
    }
}
