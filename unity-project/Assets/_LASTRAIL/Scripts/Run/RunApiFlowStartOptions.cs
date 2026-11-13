using System;
using System.Collections.Generic;
using LastRail.Train;

namespace LastRail.Run
{
    public sealed class RunApiFlowStartOptions
    {
        public RunApiFlowStartOptions(
            IReadOnlyList<string> selectedOperatorIds,
            string selectedUltimateOperatorId,
            IReadOnlyList<CarriageType> selectedTrainCarSlots,
            IReadOnlyCollection<string> completedOperationIds,
            int engineOutput)
        {
            if (completedOperationIds == null)
            {
                throw new ArgumentNullException(nameof(completedOperationIds));
            }

            SelectedOperatorIds = selectedOperatorIds;
            SelectedUltimateOperatorId = selectedUltimateOperatorId;
            SelectedTrainCarSlots = selectedTrainCarSlots;
            CompletedOperationIds = CopyCompletedOperationIds(completedOperationIds);
            EngineOutput = engineOutput;
        }

        public IReadOnlyList<string> SelectedOperatorIds { get; }
        public string SelectedUltimateOperatorId { get; }
        public IReadOnlyList<CarriageType> SelectedTrainCarSlots { get; }
        public IReadOnlyList<string> CompletedOperationIds { get; }
        public int EngineOutput { get; }

        private static IReadOnlyList<string> CopyCompletedOperationIds(IReadOnlyCollection<string> completedOperationIds)
        {
            var copied = new string[completedOperationIds.Count];
            var index = 0;
            foreach (var completedOperationId in completedOperationIds)
            {
                copied[index] = completedOperationId;
                index++;
            }

            return copied;
        }
    }
}
