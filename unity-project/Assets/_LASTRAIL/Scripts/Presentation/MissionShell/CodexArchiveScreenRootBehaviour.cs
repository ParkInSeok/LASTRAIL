#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using LastRail.Presentation.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed class CodexArchiveScreenRootBehaviour : MonoBehaviour, ICodexArchiveScreenView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text entriesText;
        [SerializeField] private TMP_Text overflowStatusText;
        [SerializeField] private TMP_Text missionSelectButtonText;
        [SerializeField] private Button missionSelectButton;
        [SerializeField] private TMP_Text resultButtonText;
        [SerializeField] private Button resultButton;
        [SerializeField] private TMP_Text rewardReviewButtonText;
        [SerializeField] private Button rewardReviewButton;
        [SerializeField] private LastRailAudioCuePlayer audioCuePlayer;

        public CodexArchiveScreenViewState CurrentState { get; private set; }
        public event Action MissionSelectRequested;
        public event Action ResultRequested;
        public event Action RewardReviewRequested;

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
            BindButton(missionSelectButton, OnMissionSelectClicked);
            BindButton(resultButton, OnResultClicked);
            BindButton(rewardReviewButton, OnRewardReviewClicked);
        }

        public void Render(CodexArchiveScreenViewState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            CurrentState = state;
            ApplySafeArea();
            SetText(titleText, state.Title);
            SetText(subtitleText, state.Subtitle);
            SetText(entriesText, FormatEntries(state));
            SetText(overflowStatusText, state.OverflowSlotCount > 0 ? "문구 길이 확인 필요" : "문구 길이 확인 완료");
            SetText(missionSelectButtonText, "작전 선택");
            SetButton(missionSelectButton, state.CanNavigateToMissionSelect);
            SetText(resultButtonText, "결과");
            SetButton(resultButton, state.CanNavigateToResult);
            SetText(rewardReviewButtonText, "보상");
            SetButton(rewardReviewButton, state.CanNavigateToRewardReview);
            BindInteractionCallbacks();
        }

        private void OnMissionSelectClicked()
        {
            if (missionSelectButton == null || !missionSelectButton.interactable)
            {
                return;
            }

            MissionSelectRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_back");
        }

        private void OnResultClicked()
        {
            if (resultButton == null || !resultButton.interactable)
            {
                return;
            }

            ResultRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_toggle_or_tab");
        }

        private void OnRewardReviewClicked()
        {
            if (rewardReviewButton == null || !rewardReviewButton.interactable)
            {
                return;
            }

            RewardReviewRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_toggle_or_tab");
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
            if (missionSelectButton != null)
            {
                missionSelectButton.onClick.RemoveListener(OnMissionSelectClicked);
            }

            if (resultButton != null)
            {
                resultButton.onClick.RemoveListener(OnResultClicked);
            }

            if (rewardReviewButton != null)
            {
                rewardReviewButton.onClick.RemoveListener(OnRewardReviewClicked);
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

        private static string FormatEntries(CodexArchiveScreenViewState state)
        {
            if (state.Entries.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < state.Entries.Count; i++)
            {
                var entry = state.Entries[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.CanOpenForDisplay ? "[열림] " : "[잠김] ");
                builder.Append(entry.DisplayName).Append(" - ");
                builder.Append(entry.DisplayText);
            }

            return builder.ToString();
        }
    }
}
#endif
