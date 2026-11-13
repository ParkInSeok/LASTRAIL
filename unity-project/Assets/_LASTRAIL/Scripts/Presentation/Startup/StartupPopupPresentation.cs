using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Startup
{
    // Borrows existing controls; rendering never registers callbacks or computes gate state.
    public sealed class StartupPopupPresentation
    {
        readonly RectTransform band;
        readonly Image backing;
        readonly IndependentMetalSurface surface;
        readonly TMP_Text eyebrow;
        readonly RectState[] original;
        readonly TMP_Text title, details, progress;
        readonly TMP_Text followup;
        readonly StartupPopupBodyPresentation bodyPresentation;
        readonly Image icon;
        readonly Button primary, secondary;
        readonly IndependentMetalSurface primarySurface, secondarySurface;
        readonly Graphic primaryTarget, secondaryTarget;
        readonly Image primaryImage, secondaryImage;
        readonly Color originalBacking;
        readonly Color primaryColor, secondaryColor;
        readonly TextState[] originalText;
        readonly RectTransform brand, scrim;
        readonly TMP_Text popupBrand;
        readonly RectTransform eyebrowAccent, separator;
        public Button SupportButton { get; }
        bool popupVisible;

        sealed class RectState
        {
            readonly RectTransform rect;
            readonly Vector2 min, max, pivot, size, position;
            readonly Vector3 scale;
            public RectState(RectTransform target) { rect = target; min = target.anchorMin; max = target.anchorMax; pivot = target.pivot; size = target.sizeDelta; position = target.anchoredPosition; scale = target.localScale; }
            public void Restore() { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = scale; }
        }

        sealed class TextState
        {
            readonly TMP_Text text; readonly float size, lineSpacing; readonly TextAlignmentOptions alignment; readonly bool autoSize; readonly FontStyles style;
            public TextState(TMP_Text target) { text = target; size = target.fontSize; lineSpacing = target.lineSpacing; alignment = target.alignment; autoSize = target.enableAutoSizing; style = target.fontStyle; }
            public void Restore() { text.fontSize = size; text.lineSpacing = lineSpacing; text.alignment = alignment; text.enableAutoSizing = autoSize; text.fontStyle = style; }
        }

        public StartupPopupPresentation(Transform root, TMP_Text title, TMP_Text details, TMP_Text progress, Image icon, Button primary, Button secondary)
        {
            this.title = title; this.details = details; this.progress = progress; this.icon = icon; this.primary = primary; this.secondary = secondary;
            band = (RectTransform)title.transform.parent; backing = band.GetComponent<Image>(); originalBacking = backing.color;
            brand = root.Find("SafeArea/StartupBrand") as RectTransform; scrim = root.Find("SafeArea/TitleScrim") as RectTransform;
            var primaryLabel = primary.GetComponentInChildren<TMP_Text>(true); var secondaryLabel = secondary.GetComponentInChildren<TMP_Text>(true);
            original = new[] { new RectState(band), new RectState(title.rectTransform), new RectState(details.rectTransform), new RectState(progress.rectTransform), new RectState((RectTransform)primary.transform), new RectState((RectTransform)secondary.transform), new RectState(primaryLabel.rectTransform), new RectState(secondaryLabel.rectTransform) };
            originalText = new[] { new TextState(title), new TextState(details), new TextState(primaryLabel), new TextState(secondaryLabel) };
            surface = IndependentMetalSurface.Create(band, false, Color.white);
            surface.Frame.BorderScale=.9f;surface.Frame.color=new Color(.46f,.49f,.51f);surface.Backplate.Inset=new Vector2(48,48);
            eyebrow = AuthFormVisuals.Text(band, "PopupEyebrow", "", title.font, 24, true);
            followup = AuthFormVisuals.Text(band, "PopupFollowup", "", details.font, 30);
            primaryTarget = primary.targetGraphic; secondaryTarget = secondary.targetGraphic;
            primaryImage = primary.GetComponent<Image>(); secondaryImage = secondary.GetComponent<Image>();
            primaryColor = primaryImage.color; secondaryColor = secondaryImage.color;
            primarySurface = IndependentMetalSurface.Create((RectTransform)primary.transform, true, new Color(.13f, .68f, .87f));
            primarySurface.EmphasizePrimary(1.25f);
            primarySurface.Frame.color=new Color(.66f,.69f,.70f);
            secondarySurface = IndependentMetalSurface.Create((RectTransform)secondary.transform, true, new Color(.65f, .67f, .68f));
            secondarySurface.Frame.color=new Color(.66f,.69f,.70f);
            secondarySurface.SetLightColor(new Color(1, .61f, .18f));
            popupBrand = AuthFormVisuals.Text(root.Find("SafeArea"), "PopupSmallBrand", "LAST RAIL", title.font, 54, true);
            popupBrand.alignment = TextAlignmentOptions.Center;
            popupBrand.rectTransform.anchorMin = popupBrand.rectTransform.anchorMax = new Vector2(.5f, 1);
            popupBrand.rectTransform.pivot = new Vector2(.5f, 1); popupBrand.rectTransform.anchoredPosition = new Vector2(0, -90); popupBrand.rectTransform.sizeDelta = new Vector2(600, 90);
            popupBrand.gameObject.AddComponent<PopupBrandViewport>().Root = (RectTransform)root;
            eyebrowAccent = Accent(band, "EyebrowAccent", 100, 155, 48, 3, new Color(.15f, .9f, 1));
            separator = Accent(band, "BodySeparator", 100, 520, 700, 2, new Color(.35f, .43f, .45f));
            Accent(separator, "SeparatorGlow", 310, -2, 80, 5, new Color(.15f, .9f, 1));
            bodyPresentation=new StartupPopupBodyPresentation(band,details,followup,separator);
            var supportRect=AuthFormVisuals.Rect("StartupSupport",band);AuthFormVisuals.Place(supportRect,100,955,700,48);
            supportRect.gameObject.AddComponent<Image>().color=Color.clear;SupportButton=supportRect.gameObject.AddComponent<Button>();
            var supportColors=SupportButton.colors; supportColors.disabledColor=new Color(.72f,.77f,.80f,1); SupportButton.colors=supportColors;
            SupportButton.targetGraphic=AuthFormVisuals.Text(supportRect,"Label","고객문의 · 로그인 후 이용",title.font,24,false,TextAlignmentOptions.Center);
        }

        static RectTransform Accent(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var rect = AuthFormVisuals.Rect(name, parent); AuthFormVisuals.Place(rect, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }

        public void Render(StartupScreenViewState state)
        {
            bodyPresentation.Prepare();
            bool network = state.PhaseKey == "network_interrupted";
            bool startOptions=state.PhaseKey=="auth:StartOptions";
            var popup = state.Kind == StartupScreenKind.UpdateNotice || state.Kind == StartupScreenKind.DownloadConsent || state.Kind == StartupScreenKind.DownloadError || state.Kind == StartupScreenKind.InternalAccessDenied;
            popupVisible = popup;
            popupBrand.gameObject.SetActive(popup); eyebrowAccent.gameObject.SetActive(popup); separator.gameObject.SetActive(network || state.Kind == StartupScreenKind.UpdateNotice || state.Kind == StartupScreenKind.InternalAccessDenied);
            AuthFormVisuals.Place(separator, 100, network ? 345 : state.Kind == StartupScreenKind.InternalAccessDenied ? 415 : 520, 700, 2);SupportButton.gameObject.SetActive(network);
            surface.gameObject.SetActive(popup); primarySurface.gameObject.SetActive(popup); secondarySurface.gameObject.SetActive(popup); eyebrow.gameObject.SetActive(popup);
            backing.color = popup||startOptions ? Color.clear : originalBacking;
            // Original Images remain the input targets even when their old skins are transparent.
            primaryImage.color = popup ? Color.clear : primaryColor; secondaryImage.color = popup ? Color.clear : secondaryColor;
            primary.targetGraphic = popup ? primarySurface.Backplate : primaryTarget;
            secondary.targetGraphic = popup ? secondarySurface.Backplate : secondaryTarget;
            icon.gameObject.SetActive(!popup&&!startOptions);
            title.gameObject.SetActive(!startOptions);
            if (brand != null) brand.gameObject.SetActive(!popup);
            if (scrim != null) scrim.gameObject.SetActive(!popup&&!startOptions);
            followup.gameObject.SetActive(false);
            if (!popup) { bodyPresentation.ClearReadingPosition(); foreach (var saved in original) saved.Restore(); foreach (var saved in originalText) saved.Restore(); return; }
            band.anchorMin = band.anchorMax = band.pivot = new Vector2(.5f, .5f); band.anchoredPosition = new Vector2(0, -100); band.sizeDelta = new Vector2(900, 1080);
            AuthFormVisuals.Place(eyebrow.rectTransform, 100, 105, 700, 48);
            eyebrow.text = state.Kind == StartupScreenKind.UpdateNotice ? "UPDATE REQUIRED" : state.Kind == StartupScreenKind.DownloadConsent ? "RESOURCE DOWNLOAD" : state.Kind == StartupScreenKind.InternalAccessDenied ? "INTERNAL TEST" : "CONNECTION CHECK";
            eyebrow.color = new Color(.55f, .75f, .8f);
            AuthFormVisuals.Place(title.rectTransform, 100, 185, 700, 250); title.enableAutoSizing = false; title.fontSize = 88; title.fontStyle = FontStyles.Bold; title.alignment = TextAlignmentOptions.MidlineLeft;
            if (state.Kind == StartupScreenKind.InternalAccessDenied) { title.fontSize = 54; title.rectTransform.sizeDelta = new Vector2(700, 150); }
            AuthFormVisuals.Place(details.rectTransform, 100, 465, 700, 215); details.enableAutoSizing = false; details.fontSize = state.Kind == StartupScreenKind.UpdateNotice ? 32 : 30; details.alignment = TextAlignmentOptions.TopLeft;
            details.lineSpacing = state.Kind == StartupScreenKind.UpdateNotice ? 4 : 0;
            if (state.Kind == StartupScreenKind.UpdateNotice) details.rectTransform.sizeDelta = new Vector2(700, 185);
            if (state.Kind == StartupScreenKind.InternalAccessDenied) AuthFormVisuals.Place(details.rectTransform, 100, 370, 700, 240);
            AuthFormVisuals.Place(progress.rectTransform, 100, state.Kind == StartupScreenKind.InternalAccessDenied ? 970 : 680, 700, 35);
            AuthFormVisuals.Place((RectTransform)primary.transform, 100, 710, 700, 144);
            AuthFormVisuals.Place((RectTransform)secondary.transform, 100, 880, 700, 144);
            if (state.Kind == StartupScreenKind.UpdateNotice)
            {
                AuthFormVisuals.Place((RectTransform)primary.transform, 100, 660, 700, 144);
                AuthFormVisuals.Place((RectTransform)secondary.transform, 100, 830, 700, 144);
            }
            if (state.Kind == StartupScreenKind.InternalAccessDenied)
            {
                AuthFormVisuals.Place((RectTransform)primary.transform, 100, 650, 700, 144);
                AuthFormVisuals.Place((RectTransform)secondary.transform, 100, 810, 700, 144);
            }
            if (network)
            {
                eyebrow.text = "CONNECTION INTERRUPTED";
                title.fontSize = 54; AuthFormVisuals.Place(title.rectTransform, 100, 185, 700, 150);
                AuthFormVisuals.Place(details.rectTransform, 100, 370, 700, 255);
                AuthFormVisuals.Place((RectTransform)primary.transform, 100, 635, 700, 144);
                AuthFormVisuals.Place((RectTransform)secondary.transform, 100, 800, 700, 144);
            }
            foreach (var button in new[] { primary, secondary })
            {
                var label = button.GetComponentInChildren<TMP_Text>(true); label.fontSize = 36; label.fontStyle = FontStyles.Bold; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(48, 12); label.rectTransform.offsetMax = new Vector2(-48, -12);
                label.transform.SetAsLastSibling();
            }
            // Separate text and actions into landscape columns; retain the existing scroll owner.
            band.anchoredPosition=Vector2.zero;band.sizeDelta=new Vector2(1400,700);
            AuthFormVisuals.Place(eyebrow.rectTransform,100,40,700,40);
            AuthFormVisuals.Place(eyebrowAccent,100,87,48,3);
            AuthFormVisuals.Place(title.rectTransform,100,105,1200,100);title.fontSize=48;title.text=state.Title.Replace("\r\n"," ").Replace("\n"," ");
            AuthFormVisuals.Place(details.rectTransform,100,230,700,300);
            AuthFormVisuals.Place((RectTransform)primary.transform,900,230,400,120);
            AuthFormVisuals.Place((RectTransform)secondary.transform,900,370,400,120);
            AuthFormVisuals.Place((RectTransform)SupportButton.transform,900,540,400,120);
            AuthFormVisuals.Place(progress.rectTransform,100,610,700,40);
            popupBrand.fontSize=36;popupBrand.rectTransform.sizeDelta=new Vector2(600,60);
            if(state.Kind==StartupScreenKind.DownloadError||state.Kind==StartupScreenKind.UpdateNotice||state.Kind==StartupScreenKind.InternalAccessDenied)
                bodyPresentation.Render(state.Details,230,590);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (!popupVisible || band == null || !(band.parent is RectTransform parent)) return;
            var available = parent.rect.size;
            if (available.x <= 0 || available.y <= 0) return;
            // Fit the landscape popup within safe-area margins.
            float scale = Mathf.Min(1f, available.x / 1480f, available.y / 780f);
            band.localScale = Vector3.one * scale;
            band.anchoredPosition = Vector2.zero;
            surface.AlignPopupLights(true);
        }
    }
}

