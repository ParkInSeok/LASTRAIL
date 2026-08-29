using System;
using System.Collections.Generic;
using LastRail.Run;

namespace LastRail.Presentation.Battle
{
    public sealed class FullyAnimatedCombatHudViewState
    {
        private FullyAnimatedCombatHudViewState(
            FullyAnimatedCombatPresentationState combatState,
            MobileCombatHudPresentationPolicy hudPolicy)
        {
            if (combatState == null)
            {
                throw new ArgumentNullException(nameof(combatState));
            }

            if (hudPolicy == null)
            {
                throw new ArgumentNullException(nameof(hudPolicy));
            }

            if (combatState.EncounterType != hudPolicy.EncounterType)
            {
                throw new ArgumentException("Combat state and HUD policy encounter type must match.", nameof(hudPolicy));
            }

            if (!string.Equals(combatState.MissionId, hudPolicy.MissionId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Combat state and HUD policy mission id must match.", nameof(hudPolicy));
            }

            Surface = hudPolicy.Surface;
            EncounterType = combatState.EncounterType;
            MissionId = combatState.MissionId;
            EncounterTitle = combatState.EncounterType == RunEncounterType.RaidBoss ? "스토리 레이드 보스전" : "15공세 작전";
            WaveCounterLabel = combatState.EncounterType == RunEncounterType.Ordinary15Wave
                ? combatState.CurrentWaveNumber + " / 15"
                : null;
            BossId = combatState.BossId;
            BossPhaseLabel = combatState.HasBossPhase ? "Phase " + combatState.BossPhase.Value : null;
            BossStatusPlaceholder = combatState.EncounterType == RunEncounterType.RaidBoss ? "보스 HP/상태 연동 대기" : null;
            BossObjectiveLabel = combatState.EncounterType == RunEncounterType.RaidBoss ? "목표: 보스 격파" : null;
            FailureStaminaRefundHint = combatState.EncounterType == RunEncounterType.RaidBoss ? "실패 시 스태미나 환급 표시 후보" : null;

            UnitAnimationRefs = CopyUnitRefs(combatState.UnitAnimationStates);
            OperatorSlotCount = CountRole(UnitAnimationRefs, CombatUnitRole.Operator);
            EnemyUnitCount = CountRole(UnitAnimationRefs, CombatUnitRole.Enemy);
            RaidBossUnitCount = CountRole(UnitAnimationRefs, CombatUnitRole.RaidBoss);
            SkillCueId = FirstOperatorCue(UnitAnimationRefs, "skill");
            UltimateCueId = FirstOperatorCue(UnitAnimationRefs, "ultimate");

            DamageCueId = combatState.PresentationCues.DamageCueId;
            HitFlashCueId = combatState.PresentationCues.HitFlashCueId;
            HitStopCueId = combatState.PresentationCues.HitStopCueId;
            ScreenShakeCueId = combatState.PresentationCues.ScreenShakeCueId;
            VfxCueId = combatState.PresentationCues.VfxCueId;
            SfxCueId = combatState.PresentationCues.SfxCueId;
            VibrationCueId = combatState.PresentationCues.VibrationCueId;
            WaveTransitionCueId = combatState.PresentationCues.WaveTransitionCueId;
            BossPhaseTransitionCueId = combatState.PresentationCues.BossPhaseTransitionCueId;

            BattlefieldRegionKey = hudPolicy.BattlefieldRegionKey;
            TacticalRegionKey = hudPolicy.TacticalRegionKey;
            HudLayerKey = hudPolicy.HudLayerKey;
            UsesPortraitSafeArea = hudPolicy.IsPortraitNineBySixteen && hudPolicy.Touch.RequiresSafeAreaPadding;
            MinimumTouchTargetDp = hudPolicy.Touch.MinimumTouchTargetDp;
            KeepsHudAboveAnimationLayer = hudPolicy.Readability.KeepsHudAboveAnimationLayer;
            PreventsTextOverflow = hudPolicy.Readability.PreventsTextOverflow;
            PreservesBattlefieldVisibility = hudPolicy.Readability.PreservesBattlefieldVisibility;
            PreservesCurrentCarTacticalVisibility = hudPolicy.Readability.PreservesCurrentCarTacticalVisibility;
        }

        public MobileCombatHudSurface Surface { get; }
        public RunEncounterType EncounterType { get; }
        public string MissionId { get; }
        public string EncounterTitle { get; }
        public string WaveCounterLabel { get; }
        public string BossId { get; }
        public string BossPhaseLabel { get; }
        public string BossStatusPlaceholder { get; }
        public string BossObjectiveLabel { get; }
        public string FailureStaminaRefundHint { get; }
        public int OperatorSlotCount { get; }
        public int EnemyUnitCount { get; }
        public int RaidBossUnitCount { get; }
        public IReadOnlyList<CombatHudUnitAnimationRefViewState> UnitAnimationRefs { get; }
        public string SkillCueId { get; }
        public string UltimateCueId { get; }
        public string DamageCueId { get; }
        public string HitFlashCueId { get; }
        public string HitStopCueId { get; }
        public string ScreenShakeCueId { get; }
        public string VfxCueId { get; }
        public string SfxCueId { get; }
        public string VibrationCueId { get; }
        public string WaveTransitionCueId { get; }
        public string BossPhaseTransitionCueId { get; }
        public string BattlefieldRegionKey { get; }
        public string TacticalRegionKey { get; }
        public string HudLayerKey { get; }
        public bool UsesPortraitSafeArea { get; }
        public int MinimumTouchTargetDp { get; }
        public bool KeepsHudAboveAnimationLayer { get; }
        public bool PreventsTextOverflow { get; }
        public bool PreservesBattlefieldVisibility { get; }
        public bool PreservesCurrentCarTacticalVisibility { get; }
        public bool IsPresentationOnly => true;
        public bool HasAnimationTimelinePayload => false;
        public bool HasRawTimelinePayload => false;
        public bool HasFramePayload => false;
        public bool HasServerTruthMutation => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static FullyAnimatedCombatHudViewState FromCombatState(
            FullyAnimatedCombatPresentationState combatState,
            MobileCombatHudPresentationPolicy hudPolicy)
        {
            return new FullyAnimatedCombatHudViewState(combatState, hudPolicy);
        }

        private static IReadOnlyList<CombatHudUnitAnimationRefViewState> CopyUnitRefs(
            IEnumerable<CombatUnitAnimationState> unitAnimationStates)
        {
            var copied = new List<CombatHudUnitAnimationRefViewState>();
            foreach (var unit in unitAnimationStates)
            {
                copied.Add(new CombatHudUnitAnimationRefViewState(unit));
            }

            return copied.AsReadOnly();
        }

        private static int CountRole(
            IEnumerable<CombatHudUnitAnimationRefViewState> refs,
            CombatUnitRole role)
        {
            var count = 0;
            foreach (var unitRef in refs)
            {
                if (unitRef.UnitRole == role)
                {
                    count++;
                }
            }

            return count;
        }

        private static string FirstOperatorCue(
            IEnumerable<CombatHudUnitAnimationRefViewState> refs,
            string marker)
        {
            foreach (var unitRef in refs)
            {
                if (unitRef.UnitRole != CombatUnitRole.Operator)
                {
                    continue;
                }

                if (ContainsMarker(unitRef.AnimationKey, marker) || ContainsMarker(unitRef.AnimationCueId, marker))
                {
                    return unitRef.AnimationCueId;
                }
            }

            return null;
        }

        private static bool ContainsMarker(string value, string marker)
        {
            return value != null && value.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
