using System;

namespace LastRail.Presentation.Audio
{
    public sealed class LaunchBgmLoopPlaybackPolicy
    {
        public const string LaunchBgmLoopCueId = "bgm_act1_loop";

        private LaunchBgmLoopPlaybackPolicy(string currentLoopCueId, bool isPlaying)
        {
            CurrentLoopCueId = string.IsNullOrWhiteSpace(currentLoopCueId) ? null : currentLoopCueId.Trim();
            IsPlaying = isPlaying;
        }

        public string CurrentLoopCueId { get; }
        public bool IsPlaying { get; }
        public bool UsesSingleBgmLoop => true;
        public bool PreventsBgmOverlap => true;

        public static LaunchBgmLoopPlaybackPolicy Stopped()
        {
            return new LaunchBgmLoopPlaybackPolicy(null, false);
        }

        public LaunchBgmLoopPlaybackDecision RequestLoop(string loopCueId)
        {
            var requested = RequireText(loopCueId, nameof(loopCueId));
            if (IsPlaying && string.Equals(CurrentLoopCueId, requested, StringComparison.Ordinal))
            {
                return new LaunchBgmLoopPlaybackDecision(
                    false,
                    false,
                    requested,
                    "already_playing_single_loop",
                    this);
            }

            return new LaunchBgmLoopPlaybackDecision(
                true,
                IsPlaying,
                requested,
                IsPlaying ? "switch_single_loop" : "start_single_loop",
                new LaunchBgmLoopPlaybackPolicy(requested, true));
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
