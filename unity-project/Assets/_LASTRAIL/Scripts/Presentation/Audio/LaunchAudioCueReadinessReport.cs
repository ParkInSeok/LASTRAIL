using System;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation.Audio
{
    public sealed class LaunchAudioCueReadinessReport
    {
        private LaunchAudioCueReadinessReport(
            LaunchAudioCueReadinessState mission,
            LaunchAudioCueReadinessState gacha,
            LaunchAudioCueReadinessState combat)
        {
            Mission = mission ?? throw new ArgumentNullException(nameof(mission));
            Gacha = gacha ?? throw new ArgumentNullException(nameof(gacha));
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        public LaunchAudioCueReadinessState Mission { get; }
        public LaunchAudioCueReadinessState Gacha { get; }
        public LaunchAudioCueReadinessState Combat { get; }
        public string BgmLoopCueId => LaunchBgmLoopPlaybackPolicy.LaunchBgmLoopCueId;
        public bool UsesSingleBgmLoop => true;
        public bool PreventsBgmOverlap => true;
        public bool RequiresNativeVibrationBridge => true;
        public string NativeVibrationBlockerMessage => "Android vibration native bridge/permission verification required.";
        public bool HasNativeVibrationImplementation => false;
        public bool HasExternalAudioAsset => false;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static LaunchAudioCueReadinessReport FromViewStates(
            MissionShellScreenViewState mission,
            GachaScreenViewState gacha,
            FullyAnimatedCombatHudViewState combat)
        {
            return new LaunchAudioCueReadinessReport(
                LaunchAudioCueReadinessState.ForMissionShell(mission),
                LaunchAudioCueReadinessState.ForGachaScreen(gacha),
                LaunchAudioCueReadinessState.ForCombatHud(combat));
        }
    }
}
