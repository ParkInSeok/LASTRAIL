#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Gacha
{
    public sealed class GachaScreenRootBehaviour : MonoBehaviour, IGachaScreenView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bannerText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text costText;
        [SerializeField] private Text pityText;
        [SerializeField] private Text revealModeText;
        [SerializeField] private Text resultGridText;
        [SerializeField] private Text errorText;
        [SerializeField] private Text onePullButtonText;
        [SerializeField] private Button onePullButton;
        [SerializeField] private Text tenPullButtonText;
        [SerializeField] private Button tenPullButton;
        [SerializeField] private Text primaryActionText;

        public GachaScreenViewState CurrentState { get; private set; }
        public event Action OnePullRequested;
        public event Action TenPullRequested;

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
            BindButton(onePullButton, OnOnePullClicked);
            BindButton(tenPullButton, OnTenPullClicked);
        }

        public void Render(GachaScreenViewState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            CurrentState = state;
            ApplySafeArea();
            SetText(titleText, state.Title);
            SetText(bannerText, state.BannerDisplayName);
            SetText(statusText, state.StatusLabel);
            SetText(costText, state.CostLabel);
            SetText(pityText, JoinLines(state.PitySummaryLines));
            SetText(revealModeText, state.RevealMode);
            SetText(resultGridText, FormatResultGrid(state));
            SetText(errorText, state.ErrorLabel ?? state.ErrorMessage);
            SetText(onePullButtonText, state.OnePullButtonLabel);
            SetButton(onePullButton, state.IsOnePullEnabled);
            SetText(tenPullButtonText, state.TenPullButtonLabel);
            SetButton(tenPullButton, state.IsTenPullEnabled);
            SetText(primaryActionText, state.PrimaryActionLabel);
            BindInteractionCallbacks();
        }

        private void OnOnePullClicked()
        {
            if (onePullButton == null || !onePullButton.interactable)
            {
                return;
            }

            OnePullRequested?.Invoke();
        }

        private void OnTenPullClicked()
        {
            if (tenPullButton == null || !tenPullButton.interactable)
            {
                return;
            }

            TenPullRequested?.Invoke();
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
            if (onePullButton != null)
            {
                onePullButton.onClick.RemoveListener(OnOnePullClicked);
            }

            if (tenPullButton != null)
            {
                tenPullButton.onClick.RemoveListener(OnTenPullClicked);
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

        private static string FormatResultGrid(GachaScreenViewState state)
        {
            if (state.ResultGridEntries.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < state.ResultGridEntries.Count; i++)
            {
                var entry = state.ResultGridEntries[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.PullIndex).Append(". ");
                builder.Append(entry.RarityLabel).Append(" / ");
                builder.Append(entry.ResultKindLabel).Append(" / ");
                builder.Append(entry.DisplayName);
                if (entry.GuaranteeBadgeLabel != null)
                {
                    builder.Append(" / ").Append(entry.GuaranteeBadgeLabel);
                }

                if (entry.DuplicateConversionLabel != null)
                {
                    builder.Append(" / ").Append(entry.DuplicateConversionLabel);
                }
            }

            return builder.ToString();
        }
    }
}
#endif
