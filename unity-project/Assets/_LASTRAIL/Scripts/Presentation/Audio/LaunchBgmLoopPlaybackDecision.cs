using System;

namespace LastRail.Presentation.Audio
{
    public sealed class LaunchBgmLoopPlaybackDecision
    {
        public LaunchBgmLoopPlaybackDecision(
            bool shouldStartLoop,
            bool shouldStopCurrentLoop,
            string loopCueId,
            string decisionReason,
            LaunchBgmLoopPlaybackPolicy nextPolicy)
        {
            ShouldStartLoop = shouldStartLoop;
            ShouldStopCurrentLoop = shouldStopCurrentLoop;
            LoopCueId = string.IsNullOrWhiteSpace(loopCueId) ? null : loopCueId.Trim();
            DecisionReason = RequireText(decisionReason, nameof(decisionReason));
            NextPolicy = nextPolicy ?? throw new ArgumentNullException(nameof(nextPolicy));
        }

        public bool ShouldStartLoop { get; }
        public bool ShouldStopCurrentLoop { get; }
        public string LoopCueId { get; }
        public string DecisionReason { get; }
        public LaunchBgmLoopPlaybackPolicy NextPolicy { get; }
        public bool PreventsOverlap => true;
        public bool HasExternalAudioAsset => false;

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
