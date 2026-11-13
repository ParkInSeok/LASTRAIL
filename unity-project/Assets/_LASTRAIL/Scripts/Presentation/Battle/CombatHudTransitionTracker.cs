using System;
using LastRail.Run;

namespace LastRail.Presentation.Battle
{
    public enum CombatHudTransitionKind { WaveStarted, BossPhaseChanged }

    public sealed class CombatHudTransition
    {
        internal CombatHudTransition(CombatHudTransitionKind kind, int number)
        {
            Kind = kind;
            Number = number;
        }

        public CombatHudTransitionKind Kind { get; }
        public int Number { get; }
        public string AudioCueId => Kind == CombatHudTransitionKind.WaveStarted ? "ui_toggle_or_tab" : "story_raid_warning";
        public string Label => Kind == CombatHudTransitionKind.WaveStarted
            ? "\uC6E8\uC774\uBE0C " + Number + " / 15"
            : "\uBCF4\uC2A4 Phase " + Number;
    }

    public sealed class CombatHudTransitionTracker
    {
        private FullyAnimatedCombatHudViewState previous;
        private int highWater;
        public bool RequiresClear { get; private set; }

        public CombatHudTransition Observe(FullyAnimatedCombatHudViewState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            RequiresClear = false;
            var number = Progress(state);
            if (previous == null || previous.MissionId != state.MissionId
                || previous.EncounterType != state.EncounterType || previous.BossId != state.BossId)
            {
                previous = state;
                highWater = number;
                RequiresClear = true;
                return null;
            }

            var lastNumber = Progress(previous);
            previous = state;
            var adjacentAdvance = number == lastNumber + 1 && number > highWater;
            highWater = Math.Max(highWater, number);
            if (!adjacentAdvance)
            {
                RequiresClear = number != lastNumber;
                return null;
            }

            // Cue ids identify presets, not events. Numeric continuity is required;
            // jumps/replayed snapshots are baselines, never inferred completed waves.
            var ordinary = state.EncounterType == RunEncounterType.Ordinary15Wave;
            var cue = ordinary ? state.WaveTransitionCueId : state.BossPhaseTransitionCueId;
            if (string.IsNullOrEmpty(cue))
            {
                RequiresClear = true;
                return null;
            }
            return new CombatHudTransition(ordinary ? CombatHudTransitionKind.WaveStarted
                : CombatHudTransitionKind.BossPhaseChanged, number);
        }

        public void Reset()
        {
            previous = null;
            highWater = 0;
            RequiresClear = true;
        }

        private static int Progress(FullyAnimatedCombatHudViewState state) =>
            state.EncounterType == RunEncounterType.Ordinary15Wave ? state.CurrentWaveNumber : state.BossPhase.GetValueOrDefault();
    }
}
