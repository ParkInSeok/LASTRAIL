using System;
using System.Collections.Generic;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation.Audio
{
    public sealed class LaunchAudioCueReadinessState
    {
        private static readonly LaunchAudioCueCatalog AudioCueCatalog = LaunchAudioCueCatalog.CreateDefault();

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
        public string NativeVibrationBlockerMessage => "Android vibration device validation required.";
        public bool HasNativeVibrationImplementation => true;
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
                    AddSoundCue(soundCueIds, "ui_confirm");
                    vibrationCueIds.Add("vibration_mission_start");
                    presentationCueIds.Add(state.PrimaryActionStringId);
                    break;
                case MissionShellSurface.LoadingHint:
                    AddSoundCue(soundCueIds, "ui_tap");
                    vibrationCueIds.Add("vibration_light");
                    AddOptional(presentationCueIds, state.NextBattleSurface.HasValue ? state.NextBattleSurface.Value.ToString() : null);
                    break;
                case MissionShellSurface.Result:
                    AddSoundCue(soundCueIds, "ui_confirm");
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

            AddSoundCue(soundCueIds, "gacha_reveal_start");
            AddOptional(presentationCueIds, state.PrimaryCueId);
            foreach (var entry in state.ResultGridEntries)
            {
                AddSoundCue(soundCueIds, entry.SoundCueId);
                vibrationCueIds.Add(entry.VibrationCueId);
                presentationCueIds.Add(entry.RarityFlashCueId);
                presentationCueIds.Add(entry.ResultCardCueId);
                AddOptional(presentationCueIds, entry.GuaranteeBadgeCueId);
                AddOptional(presentationCueIds, entry.DuplicateConversionCueId);
                if (!string.IsNullOrEmpty(entry.DuplicateConversionCueId))
                {
                    AddSoundCue(soundCueIds, "gacha_duplicate_conversion");
                }
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

            var soundCueIds = new List<string>();
            AddSoundCue(soundCueIds, state.SfxCueId);
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
                AddSoundCue(soundCueIds, "combat_skill");
                vibrationCueIds.Add("vibration_skill");
                presentationCueIds.Add(state.SkillCueId);
            }

            if (!string.IsNullOrWhiteSpace(state.UltimateCueId))
            {
                AddSoundCue(soundCueIds, "combat_ultimate");
                vibrationCueIds.Add("vibration_ultimate");
                presentationCueIds.Add(state.UltimateCueId);
            }

            if (!string.IsNullOrWhiteSpace(state.WaveTransitionCueId))
            {
                AddSoundCue(soundCueIds, "ui_toggle_or_tab");
                vibrationCueIds.Add("vibration_wave_transition");
                presentationCueIds.Add(state.WaveTransitionCueId);
            }

            if (!string.IsNullOrWhiteSpace(state.BossPhaseTransitionCueId))
            {
                AddSoundCue(soundCueIds, "story_raid_warning");
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

        private static void AddSoundCue(ICollection<string> values, string value)
        {
            AddOptional(values, AudioCueCatalog.ResolveClipId(value) ?? value);
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

