#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace LastRail.Presentation.Battle
{
    public sealed class BattleScreenRootBehaviour : MonoBehaviour
    {
        private BattleScreenInputPort inputPort;

        public BattleScreenPresentationState CurrentState => inputPort?.CurrentState;
        public BattleScreenInputResult LastInputResult => inputPort?.LastResult;
        public bool IsInitialized => inputPort != null;

        public void Bind(BattleScreenPresentationState initialState)
        {
            inputPort = new BattleScreenInputPort(initialState);
        }

        public BattleScreenInputResult SelectTrainCarSlot(int slotIndex)
        {
            RequireInitialized();
            return inputPort.SelectTrainCarSlot(slotIndex);
        }

        public BattleScreenInputResult ConfirmRedeploy()
        {
            RequireInitialized();
            return inputPort.ConfirmRedeploy();
        }

        public BattleScreenInputResult CancelRedeploy()
        {
            RequireInitialized();
            return inputPort.CancelRedeploy();
        }

        private void RequireInitialized()
        {
            if (inputPort == null)
            {
                throw new System.InvalidOperationException("Battle screen root must be bound before handling input.");
            }
        }
    }
}
#endif
