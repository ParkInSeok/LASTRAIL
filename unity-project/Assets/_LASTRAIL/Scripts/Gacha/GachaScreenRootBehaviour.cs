#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using LastRail.Presentation.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Gacha
{
    public sealed class GachaScreenRootBehaviour : MonoBehaviour, IGachaScreenView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bannerText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text pityText;
        [SerializeField] private TMP_Text revealModeText;
        [SerializeField] private TMP_Text resultGridText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text onePullButtonText;
        [SerializeField] private Button onePullButton;
        [SerializeField] private TMP_Text tenPullButtonText;
        [SerializeField] private Button tenPullButton;
        [SerializeField] private TMP_Text primaryActionText;
        [SerializeField] private Image bannerKeyVisualImage;
        [SerializeField] private Image ticketIconImage;
        [SerializeField] private Image singlePullRevealPlateImage;
        [SerializeField] private Image tenPullGridFrameImage;
        [SerializeField] private Image rareGlowImage;
        [SerializeField] private Image eliteGlowImage;
        [SerializeField] private Image pickupEliteGlowImage;
        [SerializeField] private Image duplicateConversionImage;
        [SerializeField] private Image ticketSpendImage;
        [SerializeField] private Image resultCardFrameImage;
        [SerializeField] private Image operatorCardSlotBackgroundImage;
        [SerializeField] private Image operatorRarityAccentImage;
        [SerializeField] private Image operatorSelectedFrameImage;
        [SerializeField] private Image operatorNewBadgeImage;
        [SerializeField] private Image operatorOwnedBadgeImage;
        [SerializeField] private Image operatorLevelBadgeImage;
        [SerializeField] private Image operatorDuplicateTokenBadgeImage;
        [SerializeField] private Image operatorGridSafeMaskImage;
        [SerializeField] private LastRailAudioCuePlayer audioCuePlayer;

        public GachaScreenViewState CurrentState { get; private set; }
        public event Action OnePullRequested;
        public event Action TenPullRequested;

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
            LastRail.Presentation.LandscapeOverlayLayout.Place(titleText, safeAreaRoot, 0f, 0.88f, 0.48f, 0.12f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(bannerText, safeAreaRoot, 0f, 0.77f, 0.48f, 0.11f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(costText, safeAreaRoot, 0f, 0.2f, 0.48f, 0.1f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(statusText, safeAreaRoot, 0.48f, 0.77f, 0.52f, 0.11f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(errorText, safeAreaRoot, 0.48f, 0.12f, 0.52f, 0.12f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(onePullButton, safeAreaRoot, 0f, 0f, 0.24f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(tenPullButton, safeAreaRoot, 0.24f, 0f, 0.24f, 0.14f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(primaryActionText, safeAreaRoot, 0.48f, 0f, 0.52f, 0.12f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(revealModeText, safeAreaRoot, 0.48f, 0.88f, 0.52f, 0.06f);
            LastRail.Presentation.LandscapeOverlayLayout.Place(bannerKeyVisualImage, safeAreaRoot, 0.02f, 0.40f, 0.43f, 0.36f);
            LastRail.Presentation.LandscapeOverlayLayout.ScrollText(pityText, safeAreaRoot, 0f, 0.3f, 0.48f, 0.12f);
            LastRail.Presentation.LandscapeOverlayLayout.ScrollText(resultGridText, safeAreaRoot, 0.72f, 0.24f, 0.26f, 0.53f);
            } finally { applyingLayout = false; }
        }

        private bool artworkInitialized;
        private void LayoutArtwork()
        {
            if(!artworkInitialized) { LastRail.Presentation.LandscapeOverlayLayout.HideUnboundArt(transform); artworkInitialized=true; }
            if(CurrentState!=null) ApplyVisualSkin(CurrentState);
            void Position(Image image,float x,float y,float w,float h)
            {
                if(image==null) return;
                LastRail.Presentation.LandscapeOverlayLayout.Place(image,safeAreaRoot,x,y,w,h);
                image.preserveAspect=true;image.raycastTarget=false;image.transform.SetAsFirstSibling();
            }
            if(bannerKeyVisualImage!=null) bannerKeyVisualImage.enabled=true;
            if(ticketIconImage!=null) ticketIconImage.enabled=true;
            Position(bannerKeyVisualImage,.02f,.40f,.43f,.36f);
            Position(ticketIconImage,.38f,.20f,.06f,.08f);
            Position(singlePullRevealPlateImage,.49f,.30f,.22f,.40f);
            Position(tenPullGridFrameImage,.49f,.30f,.22f,.40f);
            foreach(var image in new[]{rareGlowImage,eliteGlowImage,pickupEliteGlowImage}) Position(image,.49f,.30f,.22f,.40f);
            foreach(var image in new[]{resultCardFrameImage,operatorCardSlotBackgroundImage,operatorRarityAccentImage,operatorSelectedFrameImage,operatorGridSafeMaskImage}) Position(image,.53f,.35f,.14f,.30f);
            foreach(var image in new[]{operatorNewBadgeImage,operatorOwnedBadgeImage,operatorLevelBadgeImage}) Position(image,.58f,.58f,.09f,.05f);
            Position(operatorDuplicateTokenBadgeImage,.57f,.35f,.10f,.05f);
            Position(duplicateConversionImage,.52f,.22f,.15f,.09f);
            Position(ticketSpendImage,.50f,.71f,.08f,.08f);
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
            if (!isActiveAndEnabled) { UnbindInteractionCallbacks(); return; }
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
            SetText(revealModeText, FormatRevealModeLabel(state.RevealMode));
            SetText(resultGridText, FormatResultGrid(state));
            SetText(errorText, state.ErrorLabel ?? state.ErrorMessage);
            SetText(onePullButtonText, state.OnePullButtonLabel);
            SetButton(onePullButton, state.IsOnePullEnabled);
            SetText(tenPullButtonText, state.TenPullButtonLabel);
            SetButton(tenPullButton, state.IsTenPullEnabled);
            SetText(primaryActionText, state.PrimaryActionLabel);
            ApplyVisualSkin(state);
            BindInteractionCallbacks();
            RefreshLandscapeLayout();
        }

        private void OnOnePullClicked()
        {
            if (!isActiveAndEnabled || onePullButton == null || !onePullButton.interactable)
            {
                return;
            }

            OnePullRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_confirm");
            audioCuePlayer?.TryPlayCue("gacha_reveal_start");
        }

        private void OnTenPullClicked()
        {
            if (!isActiveAndEnabled || tenPullButton == null || !tenPullButton.interactable)
            {
                return;
            }

            TenPullRequested?.Invoke();
            audioCuePlayer?.TryPlayCue("ui_confirm");
            audioCuePlayer?.TryPlayCue("gacha_reveal_start");
        }

        public void PlayCurrentRevealAudioCues()
        {
            if (CurrentState == null || audioCuePlayer == null)
            {
                return;
            }

            audioCuePlayer.TryPlayCue("gacha_reveal_start");
            foreach (var entry in CurrentState.ResultGridEntries)
            {
                audioCuePlayer.TryPlayCue(entry.SoundCueId);
                if (!string.IsNullOrEmpty(entry.DuplicateConversionCueId))
                {
                    audioCuePlayer.TryPlayCue("gacha_duplicate_conversion");
                }
            }
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
            if (Screen.width <= 0 || Screen.height <= 0 || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                return;
            }

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

        private static string FormatRevealModeLabel(string revealMode)
        {
            switch (revealMode)
            {
                case "Idle":
                    return "모집 준비";
                case "SingleCardReveal":
                    return "단일 모집 결과";
                case "QuickTenPullGrid":
                    return "10연차 빠른 결과";
                case "NoReveal":
                    return "결과 없음";
                default:
                    return string.IsNullOrWhiteSpace(revealMode) ? string.Empty : revealMode;
            }
        }

        private static string FormatResultGrid(GachaScreenViewState state)
        {
            if (state.ResultGridEntries.Count == 0)
            {
                return string.Empty;
            }

            if (state.RevealMode == "QuickTenPullGrid")
            {
                return FormatTenPullResultSummary(state);
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
                builder.Append(entry.RarityLabel).Append(' ');
                builder.Append(entry.ResultKindLabel).Append(' ');
                builder.Append(entry.DisplayName);
                if (entry.GuaranteeBadgeLabel != null)
                {
                    builder.Append(" / 보장");
                }

                if (entry.DuplicateConversionLabel != null)
                {
                    builder.Append(" / 중복 전환");
                }
            }

            return builder.ToString();
        }

        private static string FormatTenPullResultSummary(GachaScreenViewState state)
        {
            var eliteCount = 0;
            var rareCount = 0;
            var commonCount = 0;
            var guaranteeCount = 0;
            var duplicateCount = 0;

            foreach (var entry in state.ResultGridEntries)
            {
                if (entry.RarityLabel == "정예")
                {
                    eliteCount++;
                }
                else if (entry.RarityLabel == "희귀")
                {
                    rareCount++;
                }
                else
                {
                    commonCount++;
                }

                if (entry.GuaranteeBadgeLabel != null)
                {
                    guaranteeCount++;
                }

                if (entry.DuplicateConversionLabel != null)
                {
                    duplicateCount++;
                }
            }

            var builder = new StringBuilder();
            builder.Append("결과 ").Append(state.ResultGridEntries.Count).Append("개");
            builder.Append('\n').Append("정예 ").Append(eliteCount);
            builder.Append(" / 희귀 ").Append(rareCount);
            builder.Append(" / 일반 ").Append(commonCount);
            if (guaranteeCount > 0 || duplicateCount > 0)
            {
                builder.Append('\n').Append("보장 ").Append(guaranteeCount);
                builder.Append(" / 중복 전환 ").Append(duplicateCount);
            }

            return builder.ToString();
        }

        private void ApplyVisualSkin(GachaScreenViewState state)
        {
            SetGraphicVisible(bannerKeyVisualImage, true);
            SetGraphicVisible(ticketIconImage, true);

            var hasResults = state.ResultGridEntries.Count > 0;
            var isTenPull = state.RevealMode == "QuickTenPullGrid";
            var isSinglePull = state.RevealMode == "SingleCardReveal";
            var hasDuplicate = HasDuplicateConversion(state);
            var hasPickupGuarantee = HasGuaranteeType(state, "PICKUP_ELITE_60");
            var hasElite = HasRarityLabel(state, "정예") || hasPickupGuarantee;
            var hasRare = HasRarityLabel(state, "희귀") || HasGuaranteeType(state, "RARE_OR_HIGHER_10");

            SetGraphicVisible(singlePullRevealPlateImage, !isTenPull);
            SetGraphicVisible(tenPullGridFrameImage, isTenPull);
            SetGraphicVisible(resultCardFrameImage, hasResults && !isTenPull);
            SetGraphicVisible(operatorCardSlotBackgroundImage, hasResults && !isTenPull);
            SetGraphicVisible(operatorRarityAccentImage, hasResults && !isTenPull);
            SetGraphicVisible(operatorSelectedFrameImage, hasResults && !isTenPull && hasElite);
            SetGraphicVisible(operatorNewBadgeImage, hasResults && !isTenPull && !hasDuplicate);
            SetGraphicVisible(operatorOwnedBadgeImage, false);
            SetGraphicVisible(operatorLevelBadgeImage, false);
            SetGraphicVisible(operatorDuplicateTokenBadgeImage, hasDuplicate);
            SetGraphicVisible(operatorGridSafeMaskImage, isTenPull);
            SetGraphicVisible(duplicateConversionImage, hasDuplicate);
            SetGraphicVisible(ticketSpendImage, hasResults);
            SetGraphicVisible(rareGlowImage, !isTenPull && hasRare && !hasElite);
            SetGraphicVisible(eliteGlowImage, !isTenPull && hasElite && !hasPickupGuarantee);
            SetGraphicVisible(pickupEliteGlowImage, !isTenPull && (hasPickupGuarantee || (isSinglePull && hasElite)));
        }

        private static bool HasDuplicateConversion(GachaScreenViewState state)
        {
            foreach (var entry in state.ResultGridEntries)
            {
                if (!string.IsNullOrEmpty(entry.DuplicateConversionCueId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGuaranteeType(GachaScreenViewState state, string guaranteeType)
        {
            foreach (var entry in state.ResultGridEntries)
            {
                if (entry.GuaranteeType == guaranteeType)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRarityLabel(GachaScreenViewState state, string rarityLabel)
        {
            foreach (var entry in state.ResultGridEntries)
            {
                if (entry.RarityLabel == rarityLabel)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetGraphicVisible(Graphic target, bool visible)
        {
            if (target != null)
            {
                target.enabled = visible;
            }
        }
    }
}
#endif
