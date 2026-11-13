using System;

namespace LastRail.Presentation.Battle
{
    public sealed class BattleScreenInputResult
    {
        public BattleScreenInputResult(
            BattleScreenPresentationState state,
            BattleScreenInputAction action,
            bool isBlockingRunFlow)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            State = state;
            Action = action;
            IsBlockingRunFlow = isBlockingRunFlow;
        }

        public BattleScreenPresentationState State { get; }
        public BattleScreenInputAction Action { get; }
        public bool IsBlockingRunFlow { get; }

        public static BattleScreenInputResult NonBlocking(
            BattleScreenPresentationState state,
            BattleScreenInputAction action)
        {
            return new BattleScreenInputResult(state, action, false);
        }
    }
}
