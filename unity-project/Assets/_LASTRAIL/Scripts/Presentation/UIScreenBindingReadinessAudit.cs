using System;
using System.Collections.Generic;
using LastRail.Gacha;
using LastRail.Presentation.Battle;
using LastRail.Presentation.MissionShell;

namespace LastRail.Presentation
{
    public sealed class UIScreenBindingReadinessAudit
    {
        private UIScreenBindingReadinessAudit(
            string screenKey,
            bool usesPortraitSafeArea,
            string preferredAspectLabel,
            int minimumTouchTargetDp,
            bool requiresOverflowVerification,
            string overflowRiskKey,
            bool keepsHudAboveAnimationLayer,
            IEnumerable<string> requiredBindingKeys,
            IEnumerable<string> editorVerificationChecklist)
        {
            ScreenKey = RequireText(screenKey, nameof(screenKey));
            UsesPortraitSafeArea = usesPortraitSafeArea;
            PreferredAspectLabel = RequireText(preferredAspectLabel, nameof(preferredAspectLabel));
            MinimumTouchTargetDp = minimumTouchTargetDp;
            RequiresOverflowVerification = requiresOverflowVerification;
            OverflowRiskKey = RequireText(overflowRiskKey, nameof(overflowRiskKey));
            KeepsHudAboveAnimationLayer = keepsHudAboveAnimationLayer;
            RequiredBindingKeys = CopyText(requiredBindingKeys, nameof(requiredBindingKeys));
            EditorVerificationChecklist = CopyText(editorVerificationChecklist, nameof(editorVerificationChecklist));
        }

        public string ScreenKey { get; }
        public bool UsesPortraitSafeArea { get; }
        public string PreferredAspectLabel { get; }
        public int MinimumTouchTargetDp { get; }
        public bool RequiresOverflowVerification { get; }
        public string OverflowRiskKey { get; }
        public bool KeepsHudAboveAnimationLayer { get; }
        public IReadOnlyList<string> RequiredBindingKeys { get; }
        public IReadOnlyList<string> MissingBindingKeys { get; } = Array.Empty<string>();
        public IReadOnlyList<string> EditorVerificationChecklist { get; }
        public bool HasRequiredBindings => MissingBindingKeys.Count == 0;
        public bool RequiresEditorVisualVerification => true;
        public bool HasSceneOrPrefabAsset => false;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static UIScreenBindingReadinessAudit ForMissionShell(MissionShellScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new UIScreenBindingReadinessAudit(
                "mission_shell",
                state.UsesPortraitSafeArea,
                state.PreferredAspectWidth + ":" + state.PreferredAspectHeight,
                state.MinimumTouchTargetDp,
                requiresOverflowVerification: true,
                overflowRiskKey: "mission_briefing_text",
                keepsHudAboveAnimationLayer: false,
                requiredBindingKeys: new[]
                {
                    "safeAreaRoot",
                    "titleText",
                    "subtitleText",
                    "bodyText",
                    "missionRowsText",
                    "primaryButtonText",
                    "primaryButton",
                    "secondaryButtonText"
                },
                editorVerificationChecklist: new[]
                {
                    "safe area anchors cover portrait notch/home indicator",
                    "mission briefing text wraps without covering primary action"
                });
        }

        public static UIScreenBindingReadinessAudit ForGachaScreen(GachaScreenViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new UIScreenBindingReadinessAudit(
                "gacha_screen",
                state.UsesPortraitSafeArea,
                state.PreferredAspectWidth + ":" + state.PreferredAspectHeight,
                state.MinimumTouchTargetDp,
                requiresOverflowVerification: true,
                overflowRiskKey: "gacha_ten_pull_result_grid",
                keepsHudAboveAnimationLayer: false,
                requiredBindingKeys: new[]
                {
                    "safeAreaRoot",
                    "titleText",
                    "bannerText",
                    "statusText",
                    "costText",
                    "pityText",
                    "revealModeText",
                    "resultGridText",
                    "errorText",
                    "onePullButtonText",
                    "onePullButton",
                    "tenPullButtonText",
                    "tenPullButton",
                    "primaryActionText"
                },
                editorVerificationChecklist: new[]
                {
                    "safe area anchors cover portrait notch/home indicator",
                    "10-pull result grid wraps or scrolls without covering actions"
                });
        }

        public static UIScreenBindingReadinessAudit ForCombatHud(FullyAnimatedCombatHudViewState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new UIScreenBindingReadinessAudit(
                "fully_animated_combat_hud",
                state.UsesPortraitSafeArea,
                "9:16",
                state.MinimumTouchTargetDp,
                requiresOverflowVerification: true,
                overflowRiskKey: "combat_boss_wave_labels",
                keepsHudAboveAnimationLayer: state.KeepsHudAboveAnimationLayer,
                requiredBindingKeys: new[]
                {
                    "safeAreaRoot",
                    "encounterTitleText",
                    "waveCounterText",
                    "bossStatusText",
                    "objectiveText",
                    "refundHintText",
                    "unitRefsText",
                    "cueRefsText"
                },
                editorVerificationChecklist: new[]
                {
                    "safe area anchors cover portrait notch/home indicator",
                    "combat boss/wave labels remain readable over animated combat"
                });
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
