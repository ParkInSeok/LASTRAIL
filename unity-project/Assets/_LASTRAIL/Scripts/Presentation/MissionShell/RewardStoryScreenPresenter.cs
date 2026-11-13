using System;
using System.Collections.Generic;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryScreenPresenter
    {
        private readonly IRewardStoryScreenView view;

        public RewardStoryScreenPresenter(IRewardStoryScreenView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public RewardStoryScreenViewState RenderRewardReview(RewardStoryDisplayPresentationState presentation)
        {
            Require(presentation, nameof(presentation));
            var runReward = presentation.RunReward;
            var body = new List<string>();
            var stringIds = new List<string>();
            if (IsCommittedStaminaRefund(runReward))
            {
                body.Add("스토리 레이드 실패 보급이 환급되었습니다.");
                stringIds.Add(LaunchNarrativeLocalizationStringIds.ForRaidFailureRefundToast());
            }
            else if (runReward.Kind.HasValue)
            {
                body.Add("서버가 확정한 작전 보상 미리보기를 표시합니다.");
            }
            else
            {
                body.Add("작전 보상 정보를 기다리는 중입니다.");
            }

            var state = new RewardStoryScreenViewState(
                MissionShellSurface.RewardReview,
                "작전 보상",
                "서버 보상 미리보기와 수령 상태만 표시합니다.",
                body,
                stringIds,
                ResolveRewardPrimaryAction(runReward),
                "기록 보관소",
                BuildRewardLines(runReward.RewardLines),
                presentation.StoryProgress.HighestStoryProgressId,
                presentation.StoryProgress.HighestStoryProgressLabel,
                null,
                presentation.StoryProgress.UnlockedOperationCount,
                BuildCodexStatus(presentation.StoryProgress.UnlockedOperationCount),
                true,
                IsCommittedStaminaRefund(runReward),
                runReward.StaminaRefundStatus,
                IsCommittedStaminaRefund(runReward) ? LaunchNarrativeLocalizationStringIds.ForRaidFailureRefundToast() : null,
                runReward.ErrorCode,
                runReward.ErrorMessage,
                false,
                runReward.CanRetryWithSameIdempotencyKey,
                runReward.CanRetryNetwork,
                runReward.RequiresAuthRefreshOrGuestLogin);

            view.Render(state);
            return state;
        }

        public RewardStoryScreenViewState RenderOfflineReward(RewardStoryDisplayPresentationState presentation)
        {
            Require(presentation, nameof(presentation));
            var offline = presentation.OfflineReward;
            var body = new List<string>
            {
                "오프라인 반복 보상에서는 프리미엄 재화를 지급하지 않습니다.",
                (offline.StoryProgressLabel ?? "스토리 진행 없음") + " 기준 오프라인 보상을 표시합니다."
            };
            if (offline.OfflineStorageCapLabel != null)
            {
                body.Add(offline.OfflineStorageCapLabel);
            }

            if (offline.IsNoClaimAvailable)
            {
                body.Add("현재 수령할 오프라인 보상이 없습니다.");
            }

            var state = new RewardStoryScreenViewState(
                MissionShellSurface.OfflineReward,
                "오프라인 보상",
                "서버가 계산한 미리보기와 수령 상태만 표시합니다.",
                body,
                Array.Empty<string>(),
                ResolveOfflinePrimaryAction(offline),
                "스토리 진행",
                BuildRewardLines(offline.RewardLines),
                offline.StoryProgressId,
                offline.StoryProgressLabel,
                offline.OfflineStorageCapLabel,
                presentation.StoryProgress.UnlockedOperationCount,
                BuildCodexStatus(presentation.StoryProgress.UnlockedOperationCount),
                true,
                false,
                null,
                null,
                offline.ErrorCode,
                offline.ErrorMessage,
                offline.IsNoClaimAvailable,
                false,
                offline.CanRetryNetwork,
                offline.RequiresAuthRefreshOrGuestLogin);

            view.Render(state);
            return state;
        }

        public RewardStoryScreenViewState RenderStoryProgress(RewardStoryDisplayPresentationState presentation)
        {
            Require(presentation, nameof(presentation));
            var story = presentation.StoryProgress;
            var state = new RewardStoryScreenViewState(
                MissionShellSurface.StoryProgressSummary,
                "스토리 진행",
                story.NarrativeVersion,
                new[]
                {
                    (story.HighestStoryProgressLabel ?? "스토리 진행 없음") + "까지 확인되었습니다.",
                    BuildCodexStatus(story.UnlockedOperationCount)
                },
                Array.Empty<string>(),
                "기록 보관소",
                "작전 선택",
                Array.Empty<RewardStoryScreenRewardLineViewState>(),
                story.HighestStoryProgressId,
                story.HighestStoryProgressLabel,
                null,
                story.UnlockedOperationCount,
                BuildCodexStatus(story.UnlockedOperationCount),
                true,
                false,
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                false);

            view.Render(state);
            return state;
        }

        private static bool IsCommittedStaminaRefund(RunRewardDisplaySection runReward)
        {
            return string.Equals(runReward.StaminaRefundStatus, "COMMITTED", StringComparison.Ordinal);
        }

        private static IReadOnlyList<RewardStoryScreenRewardLineViewState> BuildRewardLines(
            IReadOnlyList<RewardStoryRewardLinePresentation> source)
        {
            var lines = new List<RewardStoryScreenRewardLineViewState>();
            foreach (var line in source)
            {
                lines.Add(RewardStoryScreenRewardLineViewState.FromPresentation(line));
            }

            return lines.AsReadOnly();
        }

        private static string ResolveRewardPrimaryAction(RunRewardDisplaySection runReward)
        {
            if (runReward.RequiresAuthRefreshOrGuestLogin)
            {
                return "로그인 확인";
            }

            if (runReward.CanRetryWithSameIdempotencyKey || runReward.CanRetryNetwork)
            {
                return "같은 요청으로 재시도";
            }

            if (string.Equals(runReward.ClaimStatus, "Claimed", StringComparison.Ordinal))
            {
                return "확인";
            }

            return "보상 수령";
        }

        private static string ResolveOfflinePrimaryAction(OfflineRewardDisplaySection offline)
        {
            if (offline.RequiresAuthRefreshOrGuestLogin)
            {
                return "로그인 확인";
            }

            if (offline.CanRetryNetwork || offline.IsNoClaimAvailable)
            {
                return "보상 다시 확인";
            }

            if (string.Equals(offline.ClaimStatus, "Claimed", StringComparison.Ordinal))
            {
                return "확인";
            }

            return "오프라인 보상 수령";
        }

        private static string BuildCodexStatus(int unlockedOperationCount)
        {
            return "기록 " + unlockedOperationCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + "개 열람 가능";
        }

        private static void Require(object value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}


