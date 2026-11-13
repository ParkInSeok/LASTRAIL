using System;

namespace LastRail.Run
{
    public sealed class RunRewardReviewEntryState
    {
        private RunRewardReviewEntryState(
            string runId,
            string balanceVersion,
            string resultStatus,
            RunApiFlowSubmitOutcome submitOutcome,
            string rewardClaimId,
            string rewardPreviewId)
        {
            RunId = runId;
            BalanceVersion = balanceVersion;
            ResultStatus = resultStatus;
            SubmitOutcome = submitOutcome;
            RewardClaimId = rewardClaimId;
            RewardPreviewId = rewardPreviewId;
        }

        public string RunId { get; }
        public string BalanceVersion { get; }
        public string ResultStatus { get; }
        public RunApiFlowSubmitOutcome SubmitOutcome { get; }
        public string RewardClaimId { get; }
        public string RewardPreviewId { get; }
        public bool HasRewardClaimId => !string.IsNullOrWhiteSpace(RewardClaimId);
        public bool HasRewardPreviewId => !string.IsNullOrWhiteSpace(RewardPreviewId);
        public string PreferredRewardReferenceId => HasRewardPreviewId ? RewardPreviewId : RewardClaimId;
        public string PreferredRewardReferenceKind => HasRewardPreviewId ? "rewardPreviewId" : "rewardClaimId";
        public bool HasRewardAmounts => false;

        public static RunRewardReviewEntryState FromSubmitPolicy(RunApiFlowSubmitPolicy policy)
        {
            var state = TryFromSubmitPolicy(policy);
            if (state == null)
            {
                throw new InvalidOperationException("Submit policy cannot enter reward review.");
            }

            return state;
        }

        public static RunRewardReviewEntryState TryFromSubmitPolicy(RunApiFlowSubmitPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (policy.Action != RunApiFlowSubmitAction.ShowRewardReview)
            {
                return null;
            }

            var response = policy.Response;
            if (response == null)
            {
                throw new InvalidOperationException("Reward review requires a submit response.");
            }

            if (string.IsNullOrWhiteSpace(response.RewardClaimId)
                && string.IsNullOrWhiteSpace(response.RewardPreviewId))
            {
                throw new InvalidOperationException("Reward review requires a reward reference id.");
            }

            return new RunRewardReviewEntryState(
                response.RunId,
                response.BalanceVersion,
                response.ResultStatus,
                RunApiFlowSubmitOutcome.Accepted,
                response.RewardClaimId,
                response.RewardPreviewId);
        }
    }
}
