using System;
using System.Collections.Generic;
using LastRail.Gacha;
using LastRail.Presentation.Animation;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation
{
    public sealed class LaunchPlayerFacingQaReadinessGate
    {
        private LaunchPlayerFacingQaReadinessGate(
            IEnumerable<string> screenKeys,
            int passCount,
            int failCount,
            int warningCount,
            IEnumerable<string> blockers,
            bool usesPortraitSafeArea,
            int minimumTouchTargetDp,
            bool requiresTextOverflowVerification,
            bool keepsHudReadableOverAnimation,
            bool requiresTenPullGridOverflowQa,
            bool requiresCodexDetailScrollOrClampQa,
            bool requiresRewardStatusRetryLabelQa,
            AnimationVfxBudgetSpec vfxBudget,
            LaunchAudioCueReadinessReport audio)
        {
            ScreenKeys = CopyText(screenKeys, nameof(screenKeys));
            PassCount = RequireNonNegative(passCount, nameof(passCount));
            FailCount = RequireNonNegative(failCount, nameof(failCount));
            WarningCount = RequireNonNegative(warningCount, nameof(warningCount));
            Blockers = CopyText(blockers, nameof(blockers));
            UsesPortraitSafeArea = usesPortraitSafeArea;
            MinimumTouchTargetDp = minimumTouchTargetDp;
            RequiresTextOverflowVerification = requiresTextOverflowVerification;
            KeepsHudReadableOverAnimation = keepsHudReadableOverAnimation;
            RequiresTenPullGridOverflowQa = requiresTenPullGridOverflowQa;
            RequiresCodexDetailScrollOrClampQa = requiresCodexDetailScrollOrClampQa;
            RequiresRewardStatusRetryLabelQa = requiresRewardStatusRetryLabelQa;
            VfxBudget = vfxBudget ?? throw new ArgumentNullException(nameof(vfxBudget));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        public IReadOnlyList<string> ScreenKeys { get; }
        public int ScreenCount => ScreenKeys.Count;
        public int PassCount { get; }
        public int FailCount { get; }
        public int WarningCount { get; }
        public IReadOnlyList<string> Blockers { get; }
        public int BlockerCount => Blockers.Count;
        public bool UsesPortraitSafeArea { get; }
        public int MinimumTouchTargetDp { get; }
        public bool RequiresTextOverflowVerification { get; }
        public bool KeepsHudReadableOverAnimation { get; }
        public bool RequiresTenPullGridOverflowQa { get; }
        public bool RequiresCodexDetailScrollOrClampQa { get; }
        public bool RequiresRewardStatusRetryLabelQa { get; }
        public int TargetFps => 60;
        public int LowEndFallbackFps => 30;
        public int LargeVfxConcurrentLimit => VfxBudget.LargeVfxConcurrentLimit;
        public int HitSparkConcurrentMin => VfxBudget.HitSparkConcurrentMin;
        public int HitSparkConcurrentMax => VfxBudget.HitSparkConcurrentMax;
        public float FullScreenFlashMaxSeconds => VfxBudget.FullScreenFlashMaxSeconds;
        public float BossPhaseVfxMinSeconds => VfxBudget.BossPhaseVfxMinSeconds;
        public float BossPhaseVfxMaxSeconds => VfxBudget.BossPhaseVfxMaxSeconds;
        public int ScreenShakeTierCount => VfxBudget.ScreenShakeTierCount;
        public bool UsesSingleBgmLoop => Audio.UsesSingleBgmLoop;
        public bool PreventsBgmOverlap => Audio.PreventsBgmOverlap;
        public bool RequiresNativeVibrationBridge => Audio.RequiresNativeVibrationBridge;
        public bool IsReadyForEditorBindingPass => FailCount == 0 && PassCount == ScreenCount;
        public bool IsLaunchQaComplete => IsReadyForEditorBindingPass && BlockerCount == 0;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasOfflineRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        private AnimationVfxBudgetSpec VfxBudget { get; }
        private LaunchAudioCueReadinessReport Audio { get; }

        public static LaunchPlayerFacingQaReadinessGate ForCompletedPresenters(
            MissionShellScreenViewState missionShell,
            GachaScreenViewState gacha,
            FullyAnimatedCombatHudViewState combatHud,
            CodexArchiveScreenViewState codexArchive,
            RewardStoryScreenViewState rewardReview,
            RewardStoryScreenViewState offlineReward,
            RewardStoryScreenViewState storyProgress,
            AnimationVfxBudgetSpec vfxBudget,
            LaunchAudioCueReadinessReport audio)
        {
            Require(missionShell, nameof(missionShell));
            Require(gacha, nameof(gacha));
            Require(combatHud, nameof(combatHud));
            Require(codexArchive, nameof(codexArchive));
            Require(rewardReview, nameof(rewardReview));
            Require(offlineReward, nameof(offlineReward));
            Require(storyProgress, nameof(storyProgress));
            Require(vfxBudget, nameof(vfxBudget));
            Require(audio, nameof(audio));

            var screenKeys = new[]
            {
                "mission_shell",
                "gacha_screen",
                "fully_animated_combat_hud",
                "codex_archive",
                "reward_review",
                "offline_reward",
                "story_progress_summary"
            };

            var passCount = 0;
            passCount += IsMissionShellReady(missionShell) ? 1 : 0;
            passCount += IsGachaReady(gacha) ? 1 : 0;
            passCount += IsCombatReady(combatHud) ? 1 : 0;
            passCount += IsCodexReady(codexArchive) ? 1 : 0;
            passCount += IsRewardScreenReady(rewardReview, MissionShellSurface.RewardReview) ? 1 : 0;
            passCount += IsRewardScreenReady(offlineReward, MissionShellSurface.OfflineReward) ? 1 : 0;
            passCount += IsRewardScreenReady(storyProgress, MissionShellSurface.StoryProgressSummary) ? 1 : 0;

            var failCount = screenKeys.Length - passCount;
            var blockers = new List<string> { "editor_device_screenshot_qa_unavailable" };
            if (audio.RequiresNativeVibrationBridge && !audio.HasNativeVibrationImplementation)
            {
                blockers.Add("native_vibration_bridge_pending");
            }
            else if (audio.RequiresNativeVibrationBridge)
            {
                blockers.Add("android_vibration_device_validation_pending");
            }

            return new LaunchPlayerFacingQaReadinessGate(
                screenKeys,
                passCount,
                failCount,
                warningCount: blockers.Count,
                blockers: blockers,
                usesPortraitSafeArea: missionShell.UsesPortraitSafeArea && gacha.UsesPortraitSafeArea && combatHud.UsesPortraitSafeArea && codexArchive.UsesPortraitSafeArea && rewardReview.UsesPortraitSafeArea && offlineReward.UsesPortraitSafeArea && storyProgress.UsesPortraitSafeArea,
                minimumTouchTargetDp: Min(missionShell.MinimumTouchTargetDp, gacha.MinimumTouchTargetDp, combatHud.MinimumTouchTargetDp, codexArchive.MinimumTouchTargetDp, rewardReview.MinimumTouchTargetDp, offlineReward.MinimumTouchTargetDp, storyProgress.MinimumTouchTargetDp),
                requiresTextOverflowVerification: true,
                keepsHudReadableOverAnimation: combatHud.KeepsHudAboveAnimationLayer && vfxBudget.HudReadabilityFirst,
                requiresTenPullGridOverflowQa: gacha.ResultGridEntries.Count >= 0,
                requiresCodexDetailScrollOrClampQa: codexArchive.RequiresOverflowVerification,
                requiresRewardStatusRetryLabelQa: rewardReview.RequiresTextClamp && offlineReward.RequiresTextClamp && storyProgress.RequiresTextClamp,
                vfxBudget: vfxBudget,
                audio: audio);
        }

        private static bool IsMissionShellReady(MissionShellScreenViewState state)
        {
            return state.UsesPortraitSafeArea && state.MinimumTouchTargetDp >= 48 && state.HasReadableHudLabels && !state.HasServerTransport && !state.HasRewardCalculation && !state.HasEconomyMutation && !state.HasClientSideRng;
        }

        private static bool IsGachaReady(GachaScreenViewState state)
        {
            return state.UsesPortraitSafeArea && state.MinimumTouchTargetDp >= 48 && state.UsesServerResultDisplayFallback && !state.HasOperatorRosterTruth && !state.HasEconomyMutation && !state.HasClientSideRng;
        }

        private static bool IsCombatReady(FullyAnimatedCombatHudViewState state)
        {
            return state.UsesPortraitSafeArea && state.MinimumTouchTargetDp >= 48 && state.KeepsHudAboveAnimationLayer && state.PreventsTextOverflow && !state.HasAnimationTimelinePayload && !state.HasFramePayload && !state.HasRewardCalculation && !state.HasEconomyMutation && !state.HasClientSideRng;
        }

        private static bool IsCodexReady(CodexArchiveScreenViewState state)
        {
            return state.UsesPortraitSafeArea && state.MinimumTouchTargetDp >= 48 && state.HasReadableTextSlots && state.RequiresOverflowVerification && !state.HasServerTransport && !state.HasUnlockTruth && !state.HasRewardCalculation && !state.HasEconomyMutation && !state.HasClientSideRng;
        }

        private static bool IsRewardScreenReady(RewardStoryScreenViewState state, MissionShellSurface expectedSurface)
        {
            return state.Surface == expectedSurface && state.UsesPortraitSafeArea && state.MinimumTouchTargetDp >= 48 && state.HasReadableTextSlots && state.RequiresTextClamp && !state.HasServerTransport && !state.HasUnlockTruth && !state.HasRewardCalculation && !state.HasOfflineRewardCalculation && !state.HasEconomyMutation && !state.HasClientSideRng;
        }

        private static int Min(params int[] values)
        {
            var min = values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] < min)
                {
                    min = values[i];
                }
            }

            return min;
        }

        private static IReadOnlyList<string> CopyText(IEnumerable<string> values, string name)
        {
            if (values == null)
            {
                throw new ArgumentNullException(name);
            }

            var copied = new List<string>();
            foreach (var value in values)
            {
                copied.Add(RequireText(value, name));
            }

            return copied.AsReadOnly();
        }

        private static int RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, value, "Value cannot be negative.");
            }

            return value;
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static void Require(object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}


