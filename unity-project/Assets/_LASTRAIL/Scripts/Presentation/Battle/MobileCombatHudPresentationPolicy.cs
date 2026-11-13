using System;
using LastRail.Run;

namespace LastRail.Presentation.Battle
{
    public sealed class MobileCombatHudPresentationPolicy
    {
        private const string TopBattlefieldRegionKey = "top_battlefield";
        private const string BottomTacticalRegionKey = "bottom_current_car_tactical";
        private const string HudLayerKeyOverAnimatedCombat = "hud_layer_over_fully_animated_combat";

        private MobileCombatHudPresentationPolicy(
            MobileCombatHudSurface surface,
            RunEncounterType encounterType,
            string missionId,
            MobileSafeAreaInsets safeArea,
            MobileCombatHudWaveState waveHud,
            MobileCombatHudBossState bossHud,
            MobileCombatTouchPolicy touch,
            MobileCombatHudReadabilityPolicy readability,
            MobileCombatVibrationCuePolicy vibration)
        {
            Surface = surface;
            EncounterType = encounterType;
            MissionId = RequireText(missionId, nameof(missionId));
            SafeArea = safeArea ?? throw new ArgumentNullException(nameof(safeArea));
            WaveHud = waveHud ?? throw new ArgumentNullException(nameof(waveHud));
            BossHud = bossHud ?? throw new ArgumentNullException(nameof(bossHud));
            Touch = touch ?? throw new ArgumentNullException(nameof(touch));
            Readability = readability ?? throw new ArgumentNullException(nameof(readability));
            Vibration = vibration ?? throw new ArgumentNullException(nameof(vibration));
        }

        public MobileCombatHudSurface Surface { get; }
        public RunEncounterType EncounterType { get; }
        public string MissionId { get; }
        public MobileSafeAreaInsets SafeArea { get; }
        public bool IsPortraitNineBySixteen => SafeArea.IsPortraitNineBySixteen;
        public string BattlefieldRegionKey => TopBattlefieldRegionKey;
        public string TacticalRegionKey => BottomTacticalRegionKey;
        public string HudLayerKey => HudLayerKeyOverAnimatedCombat;
        public MobileCombatHudWaveState WaveHud { get; }
        public MobileCombatHudBossState BossHud { get; }
        public MobileCombatTouchPolicy Touch { get; }
        public MobileCombatHudReadabilityPolicy Readability { get; }
        public MobileCombatVibrationCuePolicy Vibration { get; }
        public bool IsPresentationOnly => true;
        public bool HasServerTruthMutation => false;
        public bool HasAnimationTimelinePayload => false;
        public bool HasFramePayload => false;
        public bool HasTransportPayload => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static MobileCombatHudPresentationPolicy ForOrdinary15Wave(
            string missionId,
            int currentWaveNumber,
            int viewportWidth,
            int viewportHeight,
            int safeAreaTop,
            int safeAreaBottom)
        {
            return new MobileCombatHudPresentationPolicy(
                MobileCombatHudSurface.Ordinary15Wave,
                RunEncounterType.Ordinary15Wave,
                missionId,
                MobileSafeAreaInsets.Portrait(viewportWidth, viewportHeight, safeAreaTop, safeAreaBottom),
                MobileCombatHudWaveState.Ordinary15Wave(currentWaveNumber),
                MobileCombatHudBossState.HiddenForOrdinary(),
                MobileCombatTouchPolicy.LaunchDefault(),
                MobileCombatHudReadabilityPolicy.LaunchDefault(),
                MobileCombatVibrationCuePolicy.LaunchDefault());
        }

        public static MobileCombatHudPresentationPolicy ForRaidBoss(
            string missionId,
            string bossId,
            int bossPhase,
            int viewportWidth,
            int viewportHeight,
            int safeAreaTop,
            int safeAreaBottom)
        {
            return new MobileCombatHudPresentationPolicy(
                MobileCombatHudSurface.RaidBoss,
                RunEncounterType.RaidBoss,
                missionId,
                MobileSafeAreaInsets.Portrait(viewportWidth, viewportHeight, safeAreaTop, safeAreaBottom),
                MobileCombatHudWaveState.HiddenForRaidBoss(),
                MobileCombatHudBossState.RaidBoss(bossId, bossPhase),
                MobileCombatTouchPolicy.LaunchDefault(),
                MobileCombatHudReadabilityPolicy.LaunchDefault(),
                MobileCombatVibrationCuePolicy.LaunchDefault());
        }

        public static MobileCombatHudPresentationPolicy FromCombatState(
            FullyAnimatedCombatPresentationState combatState,
            MobileSafeAreaInsets safeArea)
        {
            if (combatState == null)
            {
                throw new ArgumentNullException(nameof(combatState));
            }

            if (safeArea == null)
            {
                throw new ArgumentNullException(nameof(safeArea));
            }

            if (combatState.EncounterType == RunEncounterType.RaidBoss)
            {
                return new MobileCombatHudPresentationPolicy(
                    MobileCombatHudSurface.RaidBoss,
                    RunEncounterType.RaidBoss,
                    combatState.MissionId,
                    safeArea,
                    MobileCombatHudWaveState.HiddenForRaidBoss(),
                    MobileCombatHudBossState.RaidBoss(combatState.BossId, combatState.BossPhase.Value),
                    MobileCombatTouchPolicy.LaunchDefault(),
                    MobileCombatHudReadabilityPolicy.LaunchDefault(),
                    MobileCombatVibrationCuePolicy.LaunchDefault());
            }

            return new MobileCombatHudPresentationPolicy(
                MobileCombatHudSurface.Ordinary15Wave,
                RunEncounterType.Ordinary15Wave,
                combatState.MissionId,
                safeArea,
                MobileCombatHudWaveState.Ordinary15Wave(combatState.CurrentWaveNumber),
                MobileCombatHudBossState.HiddenForOrdinary(),
                MobileCombatTouchPolicy.LaunchDefault(),
                MobileCombatHudReadabilityPolicy.LaunchDefault(),
                MobileCombatVibrationCuePolicy.LaunchDefault());
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }
    }
}
