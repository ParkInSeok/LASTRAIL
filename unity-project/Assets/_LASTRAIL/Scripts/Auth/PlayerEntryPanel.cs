using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Auth
{
    public sealed class PlayerEntryPanel : MonoBehaviour
    {
        private TMP_InputField loginId, password, nickname;
        private TMP_Text title, status;
        private Button login, guest, google, register, resetPassword, submitName, retry, cancel;
        private RectTransform safe, form;
        public event Action<string, string> LoginSubmitted;
        public event Action GuestRequested, GoogleRequested, CancelRequested, RetryRequested;
        public event Action RegisterRequested, ResetPasswordRequested;
        public event Action<string> NicknameSubmitted;

        public void Initialize(TMP_FontAsset font)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (safe != null) throw new InvalidOperationException("Panel already initialized.");
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
            gameObject.AddComponent<GraphicRaycaster>();
            Rect("Background", transform).gameObject.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f);
            safe = Rect("SafeArea", transform);
            var scroll = safe.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", safe); viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            form = Rect("Form", viewport); form.anchorMin = new Vector2(0, 1); form.pivot = new Vector2(0.5f, 1);
            scroll.viewport = viewport; scroll.content = form; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var layout = form.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 64, 232); layout.spacing = 24;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            title = Text(form, "Title", font, 120, 56);
            status = Text(form, "Status", font, 160, 38);
            loginId = Input("LoginId", "\uC544\uC774\uB514", font, false);
            password = Input("Password", "\uBE44\uBC00\uBC88\uD638", font, true);
            nickname = Input("Nickname", "\uB2C9\uB124\uC784", font, false);
            login = Button("Login", "\uB85C\uADF8\uC778", font, SubmitLogin);
            guest = Button("Guest", "\uAC8C\uC2A4\uD2B8\uB85C \uC2DC\uC791", font, () => GuestRequested?.Invoke());
            google = Button("Google", "Google \uB85C\uADF8\uC778", font, () => GoogleRequested?.Invoke());
            register = Button("Register", "\uD68C\uC6D0\uAC00\uC785", font, () => { ClearSecrets(); RegisterRequested?.Invoke(); });
            resetPassword = Button("ResetPassword", "\uBE44\uBC00\uBC88\uD638 \uC7AC\uC124\uC815", font, () => { ClearSecrets(); ResetPasswordRequested?.Invoke(); });
            submitName = Button("SubmitNickname", "\uB2C9\uB124\uC784 \uD655\uC815", font, () => NicknameSubmitted?.Invoke(nickname.text));
            retry = Button("Retry", "\uB2E4\uC2DC \uD655\uC778", font, () => RetryRequested?.Invoke());
            cancel = Button("Cancel", "\uB85C\uADF8\uC778\uC73C\uB85C", font, () => { ClearSecrets(); CancelRequested?.Invoke(); });
            ShowLogin(false, false);
        }

        public void ShowLogin(bool allowGuest, bool allowGoogle)
        {
            Show("\uB85C\uADF8\uC778", "");
            loginId.gameObject.SetActive(true); password.gameObject.SetActive(true);
            login.gameObject.SetActive(true); guest.gameObject.SetActive(true); google.gameObject.SetActive(true);
            guest.interactable = allowGuest; google.interactable = allowGoogle;
            register.gameObject.SetActive(true); register.interactable = allowGuest;
            resetPassword.gameObject.SetActive(true);
            if (!allowGoogle) status.text = "Google \uB85C\uADF8\uC778\uC740 \uD604\uC7AC \uC774\uC6A9\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.";
        }
        public void ShowNickname()
        {
            Show("\uB2C9\uB124\uC784 \uC124\uC815", "\uC0AC\uC6A9\uD560 \uB2C9\uB124\uC784\uC744 \uC785\uB825\uD574 \uC8FC\uC138\uC694.");
            nickname.gameObject.SetActive(true); submitName.gameObject.SetActive(true); cancel.gameObject.SetActive(true);
        }
        public void ShowRecoveryLogin(bool canRetry)
        {
            ShowLogin(false, false);
            status.text = "\uC644\uB8CC\uB418\uC9C0 \uC54A\uC740 \uB85C\uADF8\uC778 \uC815\uBCF4\uAC00 \uC788\uC2B5\uB2C8\uB2E4.";
            retry.gameObject.SetActive(canRetry);
        }
        public void ShowStoredSessionLogin()
        {
            ShowLogin(false, false);
            status.text = "\uC800\uC7A5\uB41C \uACC4\uC815\uC73C\uB85C \uB2E4\uC2DC \uC2DC\uC791\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.";
            retry.gameObject.SetActive(true);
        }
        public void ShowBusy(string message) { Show("\uC5F0\uACB0 \uC911", message); cancel.gameObject.SetActive(true); }
        public void ShowError(string message, bool canRetry)
        { Show("\uD655\uC778 \uD544\uC694", message); retry.gameObject.SetActive(canRetry); cancel.gameObject.SetActive(true); }
        public void Hide() { ClearSecrets(); gameObject.SetActive(false); }
        private void Show(string heading, string message)
        {
            gameObject.SetActive(true); ClearSecrets(); title.text = heading; status.text = message;
            foreach (var control in new Component[] { loginId, password, nickname, login, guest, google, register, resetPassword, submitName, retry, cancel }) control.gameObject.SetActive(false);
            ApplySafeArea();
        }
        private void SubmitLogin()
        {
            var id = loginId.text; var secret = password.text; ClearSecrets(); LoginSubmitted?.Invoke(id, secret);
        }
        private void ClearSecrets() { if (password != null) password.SetTextWithoutNotify(""); }
        private void OnDisable() { ClearSecrets(); }
        private void Update() { ApplySafeArea(); }
        private void ApplySafeArea()
        {
            if (safe == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            form.sizeDelta = new Vector2(0, Mathf.Max(1900, safe.rect.height));
        }
        private TMP_InputField Input(string name, string hint, TMP_FontAsset font, bool secret)
        {
            var input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources()).GetComponent<TMP_InputField>();
            input.name = name; input.transform.SetParent(form, false); Size(input.gameObject, 144);
            input.contentType = secret ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.characterLimit = secret ? 128 : 64; input.richText = false;
            foreach (var text in input.GetComponentsInChildren<TMP_Text>(true)) { text.font = font; text.fontSize = 40; text.richText = false; }
            ((TMP_Text)input.placeholder).text = hint; return input;
        }
        private Button Button(string name, string label, TMP_FontAsset font, Action click)
        {
            var rect = Rect(name, form); Size(rect.gameObject, 144);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.12f, 0.30f, 0.27f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = Text(rect, "Label", font, 144, 40); text.text = label; text.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => click()); return button;
        }
        private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, float height, float size)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = Color.white; text.richText = false; text.raycastTarget = false;
            Size(text.gameObject, height); return text;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; return rect;
        }
        private static void Size(GameObject target, float height)
        { var size = target.AddComponent<LayoutElement>(); size.minHeight = size.preferredHeight = height; }
    }
}
