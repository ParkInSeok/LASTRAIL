#if UNITY_5_3_OR_NEWER
using System;
using LastRail.Presentation.AuthGate;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Startup
{
    public sealed class StartupScreenRootBehaviour : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressTrack;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image statusIcon;
        [SerializeField] private Sprite[] statusSprites = new Sprite[0];
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TMP_Text primaryText;
        [SerializeField] private TMP_Text secondaryText;
        private Action primaryHandlers;
        private Action secondaryHandlers;
        private bool actionPending;

        public StartupScreenViewState CurrentState { get; private set; }
        public event Action PrimaryActionRequested
        {
            add { primaryHandlers += value; RefreshButtons(); }
            remove { primaryHandlers -= value; RefreshButtons(); }
        }
        public event Action SecondaryActionRequested
        {
            add { secondaryHandlers += value; RefreshButtons(); }
            remove { secondaryHandlers -= value; RefreshButtons(); }
        }

        private void Awake()
        {
            if (CurrentState == null) RenderAuth(AuthGateUxState.FirstLaunch());
        }
        private void OnEnable() { BindButtons(); RefreshButtons(); }
        private void OnDisable() { UnbindButtons(); actionPending = false; }

        public void RenderAuth(AuthGateUxState state) => Render(StartupScreenViewState.FromAuth(state));

        public void Render(StartupScreenViewState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (CurrentState == null || state.PhaseKey != CurrentState.PhaseKey) actionPending = false;
            CurrentState = state;
            if (safeAreaRoot != null && Screen.width > 0 && Screen.height > 0)
            {
                var area = Screen.safeArea;
                safeAreaRoot.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safeAreaRoot.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }
            statusText.text = state.Title;
            detailsText.text = state.Details;
            progressText.text = state.ProgressLabel ?? string.Empty;
            progressTrack.gameObject.SetActive(state.Progress.HasValue);
            progressFill.fillAmount = state.Progress.GetValueOrDefault();
            statusIcon.sprite = state.IconIndex < statusSprites.Length ? statusSprites[state.IconIndex] : null;
            primaryText.text = state.PrimaryLabel ?? string.Empty;
            secondaryText.text = state.SecondaryLabel ?? string.Empty;
            primaryButton.gameObject.SetActive(!string.IsNullOrEmpty(state.PrimaryLabel));
            secondaryButton.gameObject.SetActive(!string.IsNullOrEmpty(state.SecondaryLabel));
            BindButtons();
            RefreshButtons();
        }

        private void BindButtons()
        {
            UnbindButtons();
            if (primaryButton != null) primaryButton.onClick.AddListener(OnPrimary);
            if (secondaryButton != null) secondaryButton.onClick.AddListener(OnSecondary);
        }
        private void UnbindButtons()
        {
            if (primaryButton != null) primaryButton.onClick.RemoveListener(OnPrimary);
            if (secondaryButton != null) secondaryButton.onClick.RemoveListener(OnSecondary);
        }
        private void RefreshButtons()
        {
            var ready = CurrentState != null && !CurrentState.IsBusy && !actionPending;
            if (primaryButton != null) primaryButton.interactable = ready && primaryHandlers != null;
            if (secondaryButton != null) secondaryButton.interactable = ready && secondaryHandlers != null;
        }
        private void OnPrimary()
        {
            if (!isActiveAndEnabled || !primaryButton.interactable || !primaryButton.gameObject.activeSelf) return;
            actionPending = true;
            RefreshButtons();
            primaryHandlers?.Invoke();
        }
        private void OnSecondary()
        {
            if (!isActiveAndEnabled || !secondaryButton.interactable || !secondaryButton.gameObject.activeSelf) return;
            actionPending = true;
            RefreshButtons();
            secondaryHandlers?.Invoke();
        }
    }
}
#endif
