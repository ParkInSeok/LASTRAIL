using System;
using System.Collections.Generic;
using LastRail.Train;

namespace LastRail.Run
{
    public static class RunStartResponseAdapter
    {
        public static RunSnapshot ToRunSnapshot(
            RunStartResponseDto response,
            IReadOnlyList<string> selectedOperatorIds,
            string selectedUltimateOperatorId,
            IReadOnlyList<CarriageType> selectedTrainCarSlots,
            IReadOnlyList<string> completedOperationIds,
            int engineOutput)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return RunStartSnapshotBuilder.BuildActOne(
                response.RunId,
                response.BalanceVersion,
                contentVersion: response.ContentVersion ?? response.BalanceVersion,
                selectedOperatorIds,
                selectedUltimateOperatorId,
                selectedTrainCarSlots,
                completedOperationIds,
                engineOutput,
                response.RiskLevel,
                response.OperationId,
                response.ActId,
                response.RegionId,
                response.EnemyFactionId,
                response.WavePlanId,
                response.RewardTableId,
                response.Seed,
                response.ServerStartedAt,
                response.StartAugmentPackageRef,
                response.StartTacticPackageRef,
                response.OperationPreviewId,
                response.EncounterType,
                response.OrdinaryTotalWaves,
                response.RaidId,
                response.BossId,
                response.BossPhaseRefs,
                response.AllowedOperatorSlots);
        }
    }
}
