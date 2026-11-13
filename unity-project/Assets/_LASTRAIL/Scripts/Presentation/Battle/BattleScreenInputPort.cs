using System;

namespace LastRail.Presentation.Battle
{
    public sealed class BattleScreenInputPort
    {
        public BattleScreenInputPort(BattleScreenPresentationState initialState)
        {
            ReplaceState(initialState);
        }

        public BattleScreenPresentationState CurrentState { get; private set; }
        public BattleScreenInputResult LastResult { get; private set; }

        public BattleScreenInputResult ReplaceState(BattleScreenPresentationState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            CurrentState = state;
            LastResult = BattleScreenInputResult.NonBlocking(state, BattleScreenInputAction.None);
            return LastResult;
        }

        public BattleScreenInputResult SelectTrainCarSlot(int slotIndex)
        {
            return Apply(BattleScreenInputHandler.SelectTrainCarSlot(CurrentState, slotIndex));
        }

        public BattleScreenInputResult ConfirmRedeploy()
        {
            return Apply(BattleScreenInputHandler.ConfirmRedeploy(CurrentState));
        }

        public BattleScreenInputResult CancelRedeploy()
        {
            return Apply(BattleScreenInputHandler.CancelRedeploy(CurrentState));
        }

        private BattleScreenInputResult Apply(BattleScreenInputResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            CurrentState = result.State;
            LastResult = result;
            return result;
        }
    }
}
