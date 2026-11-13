using System;

namespace LastRail.Tactics
{
    public readonly struct TacticDraftRefreshState
    {
        private TacticDraftRefreshState(int waveNumber, bool hasUsedFreeRefresh, bool hasUsedAdRefresh)
        {
            WaveNumber = waveNumber;
            HasUsedFreeRefresh = hasUsedFreeRefresh;
            HasUsedAdRefresh = hasUsedAdRefresh;
        }

        public int WaveNumber { get; }
        public bool HasUsedFreeRefresh { get; }
        public bool HasUsedAdRefresh { get; }
        public bool CanUseFreeRefresh { get { return !HasUsedFreeRefresh; } }
        public bool CanUseAdRefresh { get { return !HasUsedAdRefresh; } }

        public static TacticDraftRefreshState CreateForSelection(int waveNumber)
        {
            if (!TacticDraftRules.IsCommandTacticDraftWave(waveNumber))
            {
                throw new InvalidOperationException("Command tactic selection is not available at this wave.");
            }

            return new TacticDraftRefreshState(waveNumber, false, false);
        }

        public TacticDraftRefreshState UseFreeRefresh()
        {
            if (HasUsedFreeRefresh)
            {
                throw new InvalidOperationException("Free refresh was already used for this tactic selection.");
            }

            return new TacticDraftRefreshState(WaveNumber, true, HasUsedAdRefresh);
        }

        public TacticDraftRefreshState UseAdRefresh()
        {
            if (HasUsedAdRefresh)
            {
                throw new InvalidOperationException("Ad refresh was already used for this tactic selection.");
            }

            return new TacticDraftRefreshState(WaveNumber, HasUsedFreeRefresh, true);
        }
    }
}
