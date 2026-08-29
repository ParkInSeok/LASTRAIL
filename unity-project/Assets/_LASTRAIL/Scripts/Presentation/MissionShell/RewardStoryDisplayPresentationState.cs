using System;
using LastRail.Narrative;
using LastRail.OfflineRewards;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryDisplayPresentationState
    {
        private RewardStoryDisplayPresentationState(
            RunRewardDisplaySection runReward,
            OfflineRewardDisplaySection offlineReward,
            StoryProgressDisplaySection storyProgress)
        {
            RunReward = runReward ?? throw new ArgumentNullException(nameof(runReward));
            OfflineReward = offlineReward ?? throw new ArgumentNullException(nameof(offlineReward));
            StoryProgress = storyProgress ?? throw new ArgumentNullException(nameof(storyProgress));
        }

        public MissionShellSurface Surface => MissionShellSurface.RewardReview;
        public RunRewardDisplaySection RunReward { get; }
        public OfflineRewardDisplaySection OfflineReward { get; }
        public StoryProgressDisplaySection StoryProgress { get; }
        public bool IsBlockingDisplay => false;
        public bool IsPresentationOnly => true;
        public bool HasRewardAmountCalculation => false;
        public bool HasMultiplierCalculation => false;
        public bool HasUnlockTruthCalculation => false;
        public bool HasEconomyMutation => false;

        public static RewardStoryDisplayPresentationState Create(
            RunEndUxState runEndUxState,
            RunRewardPreviewClientResult runRewardPreviewResult,
            RunRewardClaimClientResult runRewardClaimResult,
            OfflineRewardPreviewFlowResult offlineRewardPreviewFlow,
            OfflineRewardClaimFlowResult offlineRewardClaimFlow,
            StoryProgressReadState storyProgress)
        {
            if (storyProgress == null)
            {
                throw new ArgumentNullException(nameof(storyProgress));
            }

            return new RewardStoryDisplayPresentationState(
                RunRewardDisplaySection.Create(runEndUxState, runRewardPreviewResult, runRewardClaimResult),
                OfflineRewardDisplaySection.Create(offlineRewardPreviewFlow, offlineRewardClaimFlow),
                StoryProgressDisplaySection.FromState(storyProgress));
        }
    }
}
