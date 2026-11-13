using System;

namespace LastRail.Gacha
{
    public sealed class GachaScreenPresenter
    {
        private readonly IGachaScreenView view;

        public GachaScreenPresenter(IGachaScreenView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public GachaScreenViewState Render(GachaScreenPresentationState screen)
        {
            var state = new GachaScreenViewState(screen);
            view.Render(state);
            return state;
        }
    }
}
