using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Presentation.Battle
{
    public sealed class FullyAnimatedCombatPresentationState
    {
        private FullyAnimatedCombatPresentationState(
            RunEncounterType encounterType,
            string missionId,
            int currentWaveNumber,
            int? bossPhase,
            string bossId,
            string startAugmentPackageRef,
            string startTacticPackageRef,
            IEnumerable<CombatUnitAnimationState> unitAnimationStates,
            CombatPresentationCueSet presentationCues,
            CombatHudReadabilityState hudReadability)
        {
            EncounterType = encounterType;
            MissionId = RequireText(missionId, nameof(missionId));
            CurrentWaveNumber = currentWaveNumber;
            BossPhase = bossPhase;
            BossId = NormalizeOptional(bossId);
            StartAugmentPackageRef = NormalizeOptional(startAugmentPackageRef);
            StartTacticPackageRef = NormalizeOptional(startTacticPackageRef);
            UnitAnimationStates = CopyUnits(unitAnimationStates);
            PresentationCues = presentationCues ?? throw new ArgumentNullException(nameof(presentationCues));
            HudReadability = hudReadability ?? CombatHudReadabilityState.LaunchDefault();
        }

        public RunEncounterType EncounterType { get; }
        public string MissionId { get; }
        public int CurrentWaveNumber { get; }
        public int? BossPhase { get; }
        public bool HasBossPhase => BossPhase.HasValue;
        public string BossId { get; }
        public string StartAugmentPackageRef { get; }
        public string StartTacticPackageRef { get; }
        public IReadOnlyList<CombatUnitAnimationState> UnitAnimationStates { get; }
        public CombatPresentationCueSet PresentationCues { get; }
        public CombatHudReadabilityState HudReadability { get; }
        public bool IsPresentationOnly => true;
        public bool HasServerTruthMutation => false;
        public bool HasRawTimelinePayload => false;
        public bool HasFramePayload => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static FullyAnimatedCombatPresentationState CreateOrdinary15Wave(
            string missionId,
            int currentWaveNumber,
            string startAugmentPackageRef,
            string startTacticPackageRef,
            IEnumerable<CombatUnitAnimationState> unitAnimationStates,
            CombatPresentationCueSet presentationCues)
        {
            if (currentWaveNumber < 1 || currentWaveNumber > 15)
            {
                throw new ArgumentException("Ordinary combat wave number must be between 1 and 15.", nameof(currentWaveNumber));
            }

            return new FullyAnimatedCombatPresentationState(
                RunEncounterType.Ordinary15Wave,
                missionId,
                currentWaveNumber,
                null,
                null,
                startAugmentPackageRef,
                startTacticPackageRef,
                unitAnimationStates,
                presentationCues,
                CombatHudReadabilityState.LaunchDefault());
        }

        public static FullyAnimatedCombatPresentationState CreateRaidBoss(
            string missionId,
            int bossPhase,
            string bossId,
            string startAugmentPackageRef,
            string startTacticPackageRef,
            IEnumerable<CombatUnitAnimationState> unitAnimationStates,
            CombatPresentationCueSet presentationCues)
        {
            if (bossPhase < 1)
            {
                throw new ArgumentException("Raid boss phase must be positive.", nameof(bossPhase));
            }

            return new FullyAnimatedCombatPresentationState(
                RunEncounterType.RaidBoss,
                missionId,
                0,
                bossPhase,
                RequireText(bossId, nameof(bossId)),
                startAugmentPackageRef,
                startTacticPackageRef,
                unitAnimationStates,
                presentationCues,
                CombatHudReadabilityState.LaunchDefault());
        }

        private static IReadOnlyList<CombatUnitAnimationState> CopyUnits(IEnumerable<CombatUnitAnimationState> unitAnimationStates)
        {
            if (unitAnimationStates == null)
            {
                throw new ArgumentNullException(nameof(unitAnimationStates));
            }

            var copied = new List<CombatUnitAnimationState>();
            foreach (var unit in unitAnimationStates)
            {
                if (unit == null)
                {
                    throw new ArgumentException("Unit animation state cannot be null.", nameof(unitAnimationStates));
                }

                copied.Add(unit);
            }

            if (copied.Count == 0)
            {
                throw new ArgumentException("At least one unit animation state is required.", nameof(unitAnimationStates));
            }

            return copied.AsReadOnly();
        }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
