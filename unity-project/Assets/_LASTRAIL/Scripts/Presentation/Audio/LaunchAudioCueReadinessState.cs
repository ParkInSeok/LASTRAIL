using System;
using System.Collections.Generic;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation.Audio
{
    public sealed class LaunchAudioCueReadinessState
    {
        private LaunchAudioCueReadinessState(
            string surfaceKey,
            IEnumerable<string> soundCueIds,
            IEnumerable<string> vibrationCueIds,
            IEnumerable<string> presentationCueIds,
            string bgmLoopCueId)
        {
            SurfaceKey = RequireText(surfaceKey, nameof(surfaceKey));
            SoundCueIds = CopyUniqueOptional(soundCueIds);
            VibrationCueIds = CopyUniqueOptional(vibrationCueIds);
            PresentationCueIds = CopyUniqueOptional(presentationCueIds);
            BgmLoopCueId = RequireText(bgmLoopCueId, nameof(bgmLoopCueId));
        }

        public string SurfaceKey { get; }
        public IReadOnlyList<string> SoundCueIds { get; }
        public IReadOnlyList<string> VibrationCueIds { get; }
        public IReadOnlyList<string> PresentationCueIds { get; }
        public string BgmLoopCueId { get; }
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

        public static LaunchAudioCueReadinessState ForMissionShell(MissionShellScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var soundCueIds = new List<string>();
            var vibrationCueIds = new List<string>();
            var presentationCueIds = new List<string>();

            switch (state.Surface)
            {
                case MissionShellSurface.Briefing:
                    soundCueIds.Add("sfx_mission_start_confirm");
                    vibrationCueIds.Add("vibration_mission_start");
                    presentationCueIds.Add(state.PrimaryActionStringId);
                    break;
                case MissionShellSurface.LoadingHint:
                    soundCueIds.Add("sfx_mission_loading");
                    vibrationCueIds.Add("vibration_light");
                    AddOptional(presentationCueIds, state.NextBattleSurface.HasValue ? state.NextBattleSurface.Value.ToString() : null);
                    break;
                case MissionShellSurface.Result:
                    soundCueIds.Add("sfx_mission_result");
                    vibrationCueIds.Add("vibration_mission_result");
                    break;
            }

            return new LaunchAudioCueReadinessState(
                "mission_" + state.Surface.ToString().ToLowerInvariant(),
                soundCueIds,
                vibrationCueIds,
                presentationCueIds,
                LaunchBgmLoopPlaybackPolicy.LaunchBgmLoopCueId);
        }

        public static LaunchAudioCueReadinessState ForGachaScreen(GachaScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var soundCueIds = new List<string>();
            var vibrationCueIds = new List<string>();
            var presentationCueIds = new List<string>();

            AddOptional(presentationCueIds, state.PrimaryCueId);
            foreach (var entry in state.ResultGridEntries)
            {
                soundCueIds.Add(entry.SoundCueId);
                vibrationCueIds.Add(entry.VibrationCueId);
                presentationCueIds.Add(entry.RarityFlashCueId);
                presentationCueIds.Add(entry.ResultCardCueId);
                AddOptional(presentationCueIds, entry.GuaranteeBadgeCueId);
                AddOptional(presentationCueIds, entry.DuplicateConversionCueId);
            }

            return new LaunchAudioCueReadinessState(
                "gacha_" + state.RevealMode,
                soundCueIds,
                vibrationCueIds,
                presentationCueIds,
                LaunchBgmLoopPlaybackPolicy.LaunchBgmLoopCueId);
        }

        public static LaunchAudioCueReadinessState ForCombatHud(FullyAnimatedCombatHudViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var soundCueIds = new List<string>
            {
                state.SfxCueId
            };
            var vibrationCueIds = new List<string>
            {
                state.VibrationCueId
            };
            var presentationCueIds = new List<string>
            {
                state.DamageCueId,
                state.HitFlashCueId,
                state.HitStopCueId,
                state.ScreenShakeCueId,
                state.VfxCueId
            };

            if (!string.IsNullOrWhiteSpace(state.SkillCueId))
            {
                soundCueIds.Add("sfx_combat_skill_cue");
                vibrationCueIds.Add("vibration_skill");
                presentationCueIds.Add(state.SkillCueId);
            }

            if (!string.IsNullOrWhiteSpace(state.UltimateCueId))
            {
                soundCueIds.Add("sfx_combat_ultimate_cue");
                vibrationCueIds.Add("vibration_ultimate");
                presentationCueIds.Add(state.UltimateCueId);
            }

            if (!string.IsNullOrWhiteSpace(state.WaveTransitionCueId))
            {
                soundCueIds.Add("sfx_wave_transition");
                vibrationCueIds.Add("vibration_wave_transition");
                presentationCueIds.Add(state.WaveTransitionCueId);
            }

            if (!string.IsNullOrWhiteSpace(state.BossPhaseTransitionCueId))
            {
                soundCueIds.Add("sfx_boss_phase");
                vibrationCueIds.Add("vibration_boss_phase");
                presentationCueIds.Add(state.BossPhaseTransitionCueId);
            }

            return new LaunchAudioCueReadinessState(
                "combat_" + state.Surface.ToString().ToLowerInvariant(),
                soundCueIds,
                vibrationCueIds,
                presentationCueIds,
                LaunchBgmLoopPlaybackPolicy.LaunchBgmLoopCueId);
        }

        private static IReadOnlyList<string> CopyUniqueOptional(IEnumerable<string> values)
        {
            var copied = new List<string>();
            if (values == null)
            {
                return copied.AsReadOnly();
            }

            foreach (var value in values)
            {
                AddOptional(copied, value);
            }

            return copied.AsReadOnly();
        }

        private static void AddOptional(ICollection<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            if (!values.Contains(normalized))
            {
                values.Add(normalized);
            }
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
