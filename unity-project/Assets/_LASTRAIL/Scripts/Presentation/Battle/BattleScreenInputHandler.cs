using System;

namespace LastRail.Presentation.Battle
{
    public static class BattleScreenInputHandler
    {
        public static BattleScreenInputResult SelectTrainCarSlot(
            BattleScreenPresentationState state,
            int slotIndex)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (slotIndex == state.CurrentSlotIndex)
            {
                return BattleScreenInputResult.NonBlocking(state, BattleScreenInputAction.None);
            }

            var previewed = SquadRedeployPresenter.PreviewRedeploy(state, slotIndex);
            return BattleScreenInputResult.NonBlocking(previewed, BattleScreenInputAction.PreviewRedeploy);
        }

        public static BattleScreenInputResult ConfirmRedeploy(BattleScreenPresentationState state)
        {
            var confirmed = SquadRedeployPresenter.ConfirmRedeploy(state);
            return BattleScreenInputResult.NonBlocking(confirmed, BattleScreenInputAction.ConfirmRedeploy);
        }

        public static BattleScreenInputResult CancelRedeploy(BattleScreenPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!state.HasRedeployPreview)
            {
                return BattleScreenInputResult.NonBlocking(state, BattleScreenInputAction.None);
            }

            var cleared = new BattleScreenPresentationState(
                state.Layout,
                state.TrainCars,
                state.SquadMembers,
                state.CurrentSlotIndex);

            return BattleScreenInputResult.NonBlocking(cleared, BattleScreenInputAction.CancelRedeploy);
        }
    }
}
