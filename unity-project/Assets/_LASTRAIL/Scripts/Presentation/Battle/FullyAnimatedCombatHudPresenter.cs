using System;

namespace LastRail.Presentation.Battle
{
    public sealed class FullyAnimatedCombatHudPresenter
    {
        private readonly IFullyAnimatedCombatHudView view;

        public FullyAnimatedCombatHudPresenter(IFullyAnimatedCombatHudView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public FullyAnimatedCombatHudViewState Render(
            FullyAnimatedCombatPresentationState combatState,
            MobileCombatHudPresentationPolicy hudPolicy)
        {
            var state = FullyAnimatedCombatHudViewState.FromCombatState(combatState, hudPolicy);
            view.Render(state);
            return state;
        }
    }
}
