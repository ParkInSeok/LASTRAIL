#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using LastRail.Presentation.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryScreenRootBehaviour : MonoBehaviour, IRewardStoryScreenView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text rewardLinesText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text primaryButtonText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text secondaryButtonText;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TMP_Text codexButtonText;
        [SerializeField] private Button codexButton;
        [SerializeField] private LastRailAudioCuePlayer audioCuePlayer;

        public RewardStoryScreenViewState CurrentState { get; private set; }
        public event Action PrimaryActionRequested;
        public event Action SecondaryActionRequested;
        public event Action CodexArchiveRequested;

        private bool layoutDirty = true;
        private Rect lastScreenSafeArea;
        private Vector2 lastScreenSize, lastSafeSize;
        private object lastLayoutState;
        private bool applyingLayout;
        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled || applyingLayout) return;
            layoutDirty = true;
            RefreshLandscapeLayout();
        }
        private void OnApplicationFocus(bool focused) { if (focused) OnRectTransformDimensionsChange(); }
        private void OnApplicationPause(bool paused) { if (!paused) OnRectTransformDimensionsChange(); }
        private void RefreshLandscapeLayout()
        {
            if (safeAreaRoot == null || applyingLayout) return;
            applyingLayout = true;
            try {
            var screenSize = new Vector2(Screen.width, Screen.height);
            var screenSafeArea = Screen.safeArea;
            if (!layoutDirty && lastScreenSize == screenSize && lastScreenSafeArea == screenSafeArea
                && lastSafeSize == safeAreaRoot.rect.size && ReferenceEquals(lastLayoutState, CurrentState)) return;
            ApplySafeArea();
            layoutDirty = false;
            lastScreenSize = screenSize; lastScreenSafeArea = screenSafeArea;
            lastSafeSize = safeAreaRoot.rect.size; lastLayoutState = CurrentState;
            LayoutArtwork();
            var scaler = GetComponentInParent<CanvasScaler>();
            if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 1; }
            LastRail.Presentation.LandscapeOverlayLayout.Place(titleText, safeAreaRoot, 0f, 0.86f, 1f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(subtitleText, safeAreaRoot, 0f, 0.72f, 0.5f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(statusText, safeAreaRoot, 0.5f, 0.72f, 0.5f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(primaryButton, safeAreaRoot, 0f, 0.02f, 0.33f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(secondaryButton, safeAreaRoot, 0.33f, 0.02f, 0.33f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(codexButton, safeAreaRoot, 0.66f, 0.02f, 0.34f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.ScrollText(bodyText, safeAreaRoot, 0.02f, 0.61f, 0.44f, 0.11f);
            LastRail.Presentation.LandscapeOverlayLayout.ScrollText(rewardLinesText, safeAreaRoot, 0.55f, 0.30f, 0.36f, 0.22f);
            } finally { applyingLayout = false; }
        }

        private bool artworkInitialized;
        private void LayoutArtwork()
        {
            if(!artworkInitialized) { LastRail.Presentation.LandscapeOverlayLayout.HideUnboundArt(transform); artworkInitialized=true; }
            void Art(string name,float x,float y,float w,float h,bool show=true) => LastRail.Presentation.LandscapeOverlayLayout.Art(transform,safeAreaRoot,name,x,y,w,h,show);
            Art("RewardResultPlateImage",.48f,.18f,.50f,.55f);
            Art("RewardChestSupplyCrateAccentImage",.04f,.28f,.20f,.28f);
            Art("OfflineSettlementMeterImage",.50f,.25f,.45f,.38f);
            Art("OfflineDurationAccumulationStripImage",.02f,.50f,.42f,.17f);
            Art("OfflineEmptyStateVisualImage",.06f,.24f,.28f,.24f,CurrentState?.IsNoClaimAvailable==true);
            Art("StoryProgressTimelinePanelImage",.48f,.24f,.50f,.48f);
            // The retained sample contains unrelated atlas fragments and has no bound route state.
            Art("StoryProgressRouteSpineImage",.03f,.26f,.39f,.24f,false);
            // A single current node is supplied by the existing display state, never all sample clears.
            foreach(var image in GetComponentsInChildren<Image>(true))
                if(image.name.StartsWith("StoryProgressThumbnail_")) image.enabled=false;
            if(CurrentState?.StoryProgressId!=null) Art("StoryProgressThumbnail_"+CurrentState.StoryProgressId,.57f,.37f,.28f,.22f);
            foreach(var text in GetComponentsInChildren<TMP_Text>(true))
                if(text.name=="StoryProgressNoneStateText") text.gameObject.SetActive(false);
            LastRail.Presentation.LandscapeOverlayLayout.Backdrop(safeAreaRoot);
            safeAreaRoot.Find("LandscapeBackdrop").SetAsFirstSibling();
        }

        private void OnEnable()
        {
            layoutDirty = true;
            BindInteractionCallbacks();
            RefreshLandscapeLayout();
        }

        private void OnDisable()
        {
            UnbindInteractionCallbacks();
        }

        public void BindInteractionCallbacks()
        {
            if (!isActiveAndEnabled) return;
            BindButton(primaryButton, OnPrimaryActionClicked);
            BindButton(secondaryButton, OnSecondaryActionClicked);
            BindButton(codexButton, OnCodexArchiveClicked);
        }

        public void Render(RewardStoryScreenViewState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            CurrentState = state;
            ApplySafeArea();
            SetText(titleText, state.Title);
            SetText(subtitleText, state.Subtitle);
            SetText(bodyText, JoinLines(state.BodyLines));
            SetText(rewardLinesText, FormatRewardLines(state));
            SetText(statusText, FormatStatus(state));
            SetText(primaryButtonText, state.PrimaryActionLabel);
            SetButton(primaryButton, state.PrimaryActionLabel != null);
            SetText(secondaryButtonText, state.SecondaryActionLabel);
            SetButton(secondaryButton, state.SecondaryActionLabel != null);
            SetText(codexButtonText, "기록 보관소");
            SetButton(codexButton, state.CanNavigateToCodexArchive);
            BindInteractionCallbacks();
            RefreshLandscapeLayout();
        }

        private void OnPrimaryActionClicked()
        {
            if (!isActiveAndEnabled || primaryButton == null || !primaryButton.interactable)
            {
                return;
            }

            PrimaryActionRequested?.Invoke();
            audioCuePlayer?.TryPlayCue(PrimaryCueId());
        }

        private void OnSecondaryActionClicked()
        {
            if (!isActiveAndEnabled || secondaryButton == null || !secondaryButton.interactable)
            {
                return;
            }

            SecondaryActionRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_back");
        }

        private void OnCodexArchiveClicked()
        {
            if (!isActiveAndEnabled || codexButton == null || !codexButton.interactable)
            {
                return;
            }

            CodexArchiveRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_toggle_or_tab");
        }

        private string PrimaryCueId()
        {
            if (CurrentState == null)
            {
                return "ui_confirm";
            }

            if (CurrentState.Surface == MissionShellSurface.RewardReview ||
                CurrentState.Surface == MissionShellSurface.OfflineReward)
            {
                return "reward_claim";
            }

            return CurrentState.RequiresAuthRefreshOrGuestLogin ||
                   CurrentState.CanRetryNetwork ||
                   CurrentState.CanRetryWithSameIdempotencyKey
                ? "ui_error"
                : "ui_confirm";
        }

        private static void BindButton(Button target, UnityEngine.Events.UnityAction action)
        {
            if (target == null)
            {
                return;
            }

            target.onClick.RemoveListener(action);
            target.onClick.AddListener(action);
        }

        private void UnbindInteractionCallbacks()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveListener(OnPrimaryActionClicked);
            }

            if (secondaryButton != null)
            {
                secondaryButton.onClick.RemoveListener(OnSecondaryActionClicked);
            }

            if (codexButton != null)
            {
                codexButton.onClick.RemoveListener(OnCodexArchiveClicked);
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetButton(Button target, bool enabled)
        {
            if (target != null)
            {
                target.interactable = enabled;
            }
        }

        private static string JoinLines(System.Collections.Generic.IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        private static string FormatRewardLines(RewardStoryScreenViewState state)
        {
            if (state.RewardLines.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < state.RewardLines.Count; i++)
            {
                var line = state.RewardLines[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(line.DisplayName);
                if (line.QuantityText != null)
                {
                    builder.Append(" ").Append(line.QuantityText);
                }
            }

            return builder.ToString();
        }

        private static string FormatStatus(RewardStoryScreenViewState state)
        {
            if (state.HasCommittedStaminaRefund)
            {
                return "스태미나 환급 완료";
            }

            if (state.IsNoClaimAvailable)
            {
                return "수령 가능 보상 없음";
            }

            if (state.RequiresAuthRefreshOrGuestLogin)
            {
                return "인증 필요";
            }

            if (state.CanRetryNetwork || state.CanRetryWithSameIdempotencyKey)
            {
                return "재시도 가능";
            }

            return state.CodexUnlockStatusLabel ?? string.Empty;
        }
    }
}
#endif
