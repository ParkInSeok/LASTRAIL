#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed class RewardStoryScreenRootBehaviour : MonoBehaviour, IRewardStoryScreenView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text rewardLinesText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text primaryButtonText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text secondaryButtonText;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Text codexButtonText;
        [SerializeField] private Button codexButton;

        public RewardStoryScreenViewState CurrentState { get; private set; }
        public event Action PrimaryActionRequested;
        public event Action SecondaryActionRequested;
        public event Action CodexArchiveRequested;

        private void OnEnable()
        {
            BindInteractionCallbacks();
        }

        private void OnDisable()
        {
            UnbindInteractionCallbacks();
        }

        public void BindInteractionCallbacks()
        {
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
        }

        private void OnPrimaryActionClicked()
        {
            if (primaryButton == null || !primaryButton.interactable)
            {
                return;
            }

            PrimaryActionRequested?.Invoke();
        }

        private void OnSecondaryActionClicked()
        {
            if (secondaryButton == null || !secondaryButton.interactable)
            {
                return;
            }

            SecondaryActionRequested?.Invoke();
        }

        private void OnCodexArchiveClicked()
        {
            if (codexButton == null || !codexButton.interactable)
            {
                return;
            }

            CodexArchiveRequested?.Invoke();
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

        private static void SetText(Text target, string value)
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
