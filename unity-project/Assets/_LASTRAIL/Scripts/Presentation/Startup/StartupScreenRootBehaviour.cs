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
        private Action supportHandler;
        private Func<bool> supportAvailable;
        public void SetSupportAction(Action action,Func<bool> available){supportHandler=action;supportAvailable=available;RefreshButtons();}
        private StartupPopupPresentation popupPresentation;

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
            GameLandscapeDisplay.Configure(GetComponent<CanvasScaler>());
            StartupMotionBackdrop.Attach(transform, false);
            var oldVisual = transform.Find("StartupKeyVisual");
            if (oldVisual != null) oldVisual.gameObject.SetActive(false);
            var actionBand = transform.Find("SafeArea/StartupActionBand")?.GetComponent<Image>();
            if (actionBand != null)
            {
                var tint = actionBand.color; tint.a = .55f; actionBand.color = tint;
            }
            if (CurrentState == null) RenderAuth(AuthGateUxState.FirstLaunch());
        }
        void ApplyLandscapeBase(){
            if(statusText==null)return;
            var band=(RectTransform)statusText.transform.parent;
            band.anchorMin=band.anchorMax=band.pivot=new Vector2(.5f,.5f);band.anchoredPosition=new Vector2(0,-100);band.sizeDelta=new Vector2(1320,460);
            LastRail.Auth.AuthFormVisuals.Place(statusText.rectTransform,60,45,1200,70);
            LastRail.Auth.AuthFormVisuals.Place(detailsText.rectTransform,60,130,1200,100);
            LastRail.Auth.AuthFormVisuals.Place(progressText.rectTransform,60,245,1200,40);
            LastRail.Auth.AuthFormVisuals.Place((RectTransform)primaryButton.transform,60,320,570,120);
            LastRail.Auth.AuthFormVisuals.Place((RectTransform)secondaryButton.transform,690,320,570,120);
            if(progressTrack!=null)LastRail.Auth.AuthFormVisuals.Place(progressTrack.rectTransform,60,290,1200,12);
            var brand=transform.Find("SafeArea/StartupBrand") as RectTransform;if(brand!=null){brand.anchorMin=brand.anchorMax=brand.pivot=new Vector2(.5f,1);brand.anchoredPosition=new Vector2(0,-35);brand.sizeDelta=new Vector2(540,150);}
        }
        private void OnEnable() { BindButtons(); RefreshButtons(); }
        private void OnDisable() { UnbindButtons(); actionPending = false; }
        private void OnRectTransformDimensionsChange() { popupPresentation?.RefreshLayout(); }

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
            if (popupPresentation == null) { ApplyLandscapeBase(); popupPresentation = new StartupPopupPresentation(transform, statusText, detailsText, progressText, statusIcon, primaryButton, secondaryButton); }
            popupPresentation.Render(state);
            BindButtons();
            RefreshButtons();
        }

        private void BindButtons()
        {
            UnbindButtons();
            if (primaryButton != null) primaryButton.onClick.AddListener(OnPrimary);
            if (secondaryButton != null) secondaryButton.onClick.AddListener(OnSecondary);
            if (popupPresentation?.SupportButton != null) popupPresentation.SupportButton.onClick.AddListener(OnSupport);
        }
        private void UnbindButtons()
        {
            if (primaryButton != null) primaryButton.onClick.RemoveListener(OnPrimary);
            if (secondaryButton != null) secondaryButton.onClick.RemoveListener(OnSecondary);
            if (popupPresentation?.SupportButton != null) popupPresentation.SupportButton.onClick.RemoveListener(OnSupport);
        }
        private void RefreshButtons()
        {
            var ready = CurrentState != null && !CurrentState.IsBusy && !actionPending;
            if (primaryButton != null) primaryButton.interactable = ready && primaryHandlers != null;
            if (secondaryButton != null) secondaryButton.interactable = ready && secondaryHandlers != null;
            if(popupPresentation?.SupportButton != null){bool canOpen=supportHandler!=null&&supportAvailable?.Invoke()==true;popupPresentation.SupportButton.interactable=ready&&canOpen;popupPresentation.SupportButton.GetComponentInChildren<TMP_Text>(true).text=canOpen?"고객문의":"고객문의 · 로그인 후 이용";}
        }
        private void OnSupport(){if(!isActiveAndEnabled||CurrentState?.PhaseKey!="network_interrupted"||actionPending||supportAvailable?.Invoke()!=true)return;supportHandler?.Invoke();}
        internal void CompletePendingAction() { actionPending = false; RefreshButtons(); }
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
