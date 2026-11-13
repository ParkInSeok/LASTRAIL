using System;

namespace LastRail.Presentation.Battle
{
    public static class SquadRedeployPresenter
    {
        public static BattleScreenPresentationState PreviewRedeploy(
            BattleScreenPresentationState state,
            int targetSlotIndex)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var preview = SquadRedeployPreview.Create(
                state.CurrentSlotIndex,
                targetSlotIndex,
                state.TrainCars,
                state.SquadMembers);

            return new BattleScreenPresentationState(
                state.Layout,
                state.TrainCars,
                state.SquadMembers,
                state.CurrentSlotIndex,
                preview);
        }

        public static BattleScreenPresentationState ConfirmRedeploy(BattleScreenPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!state.HasRedeployPreview)
            {
                throw new InvalidOperationException("Cannot confirm squad redeploy without a preview.");
            }

            var preview = state.RedeployPreview;
            var trainCars = TrainCarStripPresenter.MarkCurrent(state.TrainCars, preview.TargetSlotIndex);

            return new BattleScreenPresentationState(
                state.Layout,
                trainCars,
                preview.PreviewMembers,
                preview.TargetSlotIndex);
        }
    }
}
