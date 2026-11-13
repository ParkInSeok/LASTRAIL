using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Auth
{
    public sealed class PlayerEntryPanel : MonoBehaviour
    {
        private TMP_InputField loginId, password, nickname;
        private TMP_Text title, status, footerNotice;
        private Button login, guest, google, register, resetPassword, submitName, retry, cancel, chooseId, back;
        private RectTransform safe, form, methods, credentials, state, nicknamePage;
        private ScrollRect scroll;
        private bool guestAllowed, googleAllowed;
        private string recoveryMessage = "";
        private bool recoveryRetry;
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
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            Rect("Background", transform).gameObject.AddComponent<Image>().color = new Color(.03f, .04f, .05f);
            LastRail.Presentation.Startup.StartupMotionBackdrop.Attach(transform, true).transform.SetSiblingIndex(1);
            Rect("ReadabilityShade", transform).gameObject.AddComponent<EntryBackdropShade>().raycastTarget = false;
            safe = Rect("SafeArea", transform);
            scroll = safe.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", safe); viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            form = Rect("Form", viewport); form.anchorMin = new Vector2(0, 1); form.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport; scroll.content = form; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            methods = Rect("LoginMethods", form);
            credentials = Rect("IdLoginPage", form);
            state = Rect("EntryState", form);
            nicknamePage = Rect("NicknamePage", form);

            var brand = Rect("Brand", methods);
            Place(brand, 140, 262, 800, 267);
            BrandLogoView.Create(brand);
            var welcome = Text(methods, "Welcome", "탑승을 환영합니다, 지휘관.", font, 36, false, TextAlignmentOptions.Center);
            Place(welcome.rectTransform, 60, 484, 960, 60);
            var actions = Rect("MethodActions", methods);
            actions.anchorMin = new Vector2(0, 0); actions.anchorMax = new Vector2(1, 0); actions.pivot = new Vector2(.5f, 0);
            actions.anchoredPosition = new Vector2(0, 232); actions.sizeDelta = new Vector2(0, 748);
            Place(Text(actions, "ChooseMethodPrompt", "여정을 이어갈 방법을 선택하세요", font, 38, true, TextAlignmentOptions.Center).rectTransform, 84, 0, 912, 68);
            guest = MetalButton(actions, "Guest", "게스트로 시작", font, true, () => GuestRequested?.Invoke()); Place((RectTransform)guest.transform, 84, 108, 912, 134);
            chooseId = MetalButton(actions, "ChooseIdLogin", "아이디로 로그인", font, false, OpenIdLogin); Place((RectTransform)chooseId.transform, 84, 272, 912, 134);
            google = MetalButton(actions, "Google", "Google로 로그인", font, false, () => GoogleRequested?.Invoke()); Place((RectTransform)google.transform, 84, 436, 912, 134);
            var guestNotice=Text(actions,"GuestStorageNotice","게스트 계정은 이 기기에 저장됩니다.\n계정을 연결하면 다른 기기에서도 이어갈 수 있습니다.",font,28,false,TextAlignmentOptions.Center);
            Place(guestNotice.rectTransform,84,610,912,82);
            var terms = Link(actions, "Terms", "이용약관", font, () => footerNotice.text = "이용약관 연결을 준비 중입니다."); Place((RectTransform)terms.transform, 84, 706, 200, 52);
            var privacy = Link(actions, "Privacy", "개인정보 처리방침", font, () => footerNotice.text = "개인정보 처리방침 연결을 준비 중입니다."); Place((RectTransform)privacy.transform, 354, 706, 372, 52);
            var support = Link(actions, "Support", "고객문의", font, () => footerNotice.text = "로그인 전 고객문의 연결을 준비 중입니다."); Place((RectTransform)support.transform, 796, 706, 200, 52);
            footerNotice = Text(methods, "FooterNotice", "", font, 24, false, TextAlignmentOptions.Center);
            footerNotice.rectTransform.anchorMin = new Vector2(0, 0); footerNotice.rectTransform.anchorMax = new Vector2(1, 0);
            footerNotice.rectTransform.pivot = new Vector2(.5f, 0); footerNotice.rectTransform.anchoredPosition = new Vector2(0, 144); footerNotice.rectTransform.sizeDelta = new Vector2(-120, 70);

            back = Link(credentials, "BackToMethods", "‹ 로그인", font, BackToMethods, 38, true); Place((RectTransform)back.transform, 72, 98, 250, 88);
            back.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
            var eyebrow = Text(credentials, "Eyebrow", "WELCOME BACK", font, 24, true); eyebrow.color = new Color(.53f, .84f, .87f); Place(eyebrow.rectTransform, 84, 452, 912, 48);
            Place(Text(credentials, "IdHeading", "다시 만나 반갑습니다", font, 62, true).rectTransform, 84, 512, 912, 96);
            Place(Text(credentials, "IdDescription", "계정으로 로그인해 여정을 이어가세요.", font, 34).rectTransform, 84, 628, 912, 70);
            Place(Text(credentials, "IdLabel", "아이디", font, 28).rectTransform, 84, 718, 912, 50);
            loginId = Input(credentials, "LoginId", "아이디를 입력해 주세요", font, false); Place((RectTransform)loginId.transform, 84, 780, 912, 114);
            Place(Text(credentials, "PasswordLabel", "비밀번호", font, 28).rectTransform, 84, 920, 912, 50);
            password = Input(credentials, "Password", "비밀번호를 입력해 주세요", font, true); Place((RectTransform)password.transform, 84, 982, 912, 114);
            resetPassword = Link(credentials, "ResetPassword", "비밀번호를 잊으셨나요?", font, () => { ClearSecrets(); ResetPasswordRequested?.Invoke(); }, 28);
            Place((RectTransform)resetPassword.transform, 484, 1124, 512, 68); resetPassword.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Right;
            login = MetalButton(credentials, "Login", "로그인", font, true, SubmitLogin); Place((RectTransform)login.transform, 84, 1218, 912, 134);
            Place(Text(credentials, "RegisterPrompt", "아직 계정이 없으신가요?", font, 28).rectTransform, 344, 1380, 292, 76);
            register = Link(credentials, "Register", "회원가입", font, () => { ClearSecrets(); RegisterRequested?.Invoke(); }, 28, true); Place((RectTransform)register.transform, 636, 1380, 120, 76);

            title = Text(state, "Title", "", font, 58, true); Place(title.rectTransform, 84, 450, 912, 100);
            status = Text(state, "Status", "", font, 34); Place(status.rectTransform, 84, 572, 912, 180);
            var nameHeader=Link(nicknamePage,"NicknameBack","‹ 지휘관 등록",font,()=>{ClearSecrets(); CancelRequested?.Invoke();},38,true); Place((RectTransform)nameHeader.transform,72,98,400,88); nameHeader.GetComponentInChildren<TMP_Text>().alignment=TextAlignmentOptions.Left;
            // Preserve historical pixels, but exclude the unrelated atlas row below y492.
            // Composite portrait remains an open resource audit item until independent assets exist.
            var profileCrop=Rect("CommanderProfileCrop",nicknamePage);Place(profileCrop,84,350,912,410f*492/512);profileCrop.gameObject.AddComponent<RectMask2D>();
            var portrait=Rect("CommanderProfile",profileCrop).gameObject.AddComponent<RawImage>(); portrait.raycastTarget=false; portrait.gameObject.AddComponent<LastRail.Resources.PublicEntryTexture>().Bind("profile",texture=>{if(portrait!=null){portrait.texture=texture;portrait.enabled=texture!=null;}}); Place(portrait.rectTransform,0,0,912,410);
            var nameEyebrow=Text(nicknamePage,"NicknameEyebrow","YOUR CALLSIGN",font,24,true); nameEyebrow.color=new Color(.53f,.84f,.87f); Place(nameEyebrow.rectTransform,84,814,912,50);
            Place(Text(nicknamePage,"NicknameHeading","어떻게 불러드릴까요?",font,62,true).rectTransform,84,876,912,100);
            Place(Text(nicknamePage,"NicknameDescription","함께할 지휘관의 이름을 정해 주세요.",font,34).rectTransform,84,990,912,80);
            Place(Text(nicknamePage,"NicknameLabel","닉네임",font,28).rectTransform,84,1080,912,50);
            nickname = Input(nicknamePage, "Nickname", "닉네임을 입력해 주세요", font, false); Place((RectTransform)nickname.transform, 84, 1142, 912, 114);
            var publicNotice=Text(nicknamePage,"PublicNameNotice","이 이름은 다른 플레이어에게 표시됩니다.",font,32); publicNotice.color=new Color(.5f,.89f,.93f); Place(publicNotice.rectTransform,84,1290,912,144);
            foreach(float y in new[]{1284f,1438f}){var divider=Rect("NicknameNoticeDivider"+y,nicknamePage);Place(divider,84,y,912,2);var line=divider.gameObject.AddComponent<Image>();line.color=new Color(.3f,.5f,.54f);line.raycastTarget=false;}
            submitName = MetalButton(nicknamePage, "SubmitNickname", "출격", font, true, () => NicknameSubmitted?.Invoke(nickname.text)); Place((RectTransform)submitName.transform, 84, 1484, 912, 134);
            retry = MetalButton(state, "Retry", "다시 확인", font, true, () => RetryRequested?.Invoke()); Place((RectTransform)retry.transform, 84, 960, 912, 134);
            cancel = MetalButton(state, "Cancel", "로그인으로", font, false, () => { ClearSecrets(); CancelRequested?.Invoke(); }); Place((RectTransform)cancel.transform, 84, 1130, 912, 134);
            gameObject.AddComponent<AuthKeyboardAvoidance>().Initialize(scroll);
            ShowLogin(false, false);
        }

        public void ShowLogin(bool allowGuest, bool allowGoogle)
        {
            guestAllowed = allowGuest; googleAllowed = allowGoogle; recoveryMessage = ""; recoveryRetry = false;
            ShowMethods();
        }
        private void ShowMethods()
        {
            Show("", ""); state.gameObject.SetActive(false); methods.gameObject.SetActive(true);
            guest.gameObject.SetActive(true); google.gameObject.SetActive(true); chooseId.gameObject.SetActive(true);
            guest.interactable = guestAllowed; google.interactable = googleAllowed; register.interactable = guestAllowed;
            footerNotice.text = recoveryMessage;
            if (recoveryRetry)
            {
                retry.transform.SetParent(methods, false); Place((RectTransform)retry.transform, 84, 650, 912, 134); retry.gameObject.SetActive(true);
            }
        }
        private void OpenIdLogin()
        {
            Show("", ""); state.gameObject.SetActive(false); credentials.gameObject.SetActive(true);
            loginId.gameObject.SetActive(true); password.gameObject.SetActive(true); login.gameObject.SetActive(true);
            register.gameObject.SetActive(true); register.interactable = guestAllowed; resetPassword.gameObject.SetActive(true); back.gameObject.SetActive(true);
        }
        private void BackToMethods() { ClearSecrets(); ShowMethods(); }
        public void ShowNickname()
        {
            Show("닉네임 설정", "사용할 닉네임을 입력해 주세요."); state.gameObject.SetActive(false); nicknamePage.gameObject.SetActive(true); nickname.gameObject.SetActive(true); submitName.gameObject.SetActive(true); cancel.gameObject.SetActive(false);
            cancel.transform.SetParent(nicknamePage,false); Place((RectTransform)cancel.transform,84,1640,912,134);
        }
        public void ShowRecoveryLogin(bool canRetry)
        {
            guestAllowed = googleAllowed = false; recoveryMessage = "완료되지 않은 로그인 정보가 있습니다."; recoveryRetry = canRetry; ShowMethods();
        }
        public void ShowStoredSessionLogin()
        {
            guestAllowed = googleAllowed = false; recoveryMessage = "저장된 계정으로 다시 시작할 수 있습니다."; recoveryRetry = true; ShowMethods();
        }
        public void ShowBusy(string message) { Show("연결 중", message); cancel.gameObject.SetActive(true); }
        public void ShowError(string message, bool canRetry) { Show("확인 필요", message); retry.gameObject.SetActive(canRetry); cancel.gameObject.SetActive(true); }
        public void Hide() { ClearSecrets(); gameObject.SetActive(false); }
        private void Show(string heading, string message)
        {
            gameObject.SetActive(true); ClearSecrets(); title.text = heading; status.text = message;
            methods.gameObject.SetActive(false); credentials.gameObject.SetActive(false); nicknamePage.gameObject.SetActive(false); state.gameObject.SetActive(true);
            cancel.transform.SetParent(state,false); Place((RectTransform)cancel.transform,84,1130,912,134);
            foreach (var control in new Component[] { loginId, password, nickname, login, guest, google, register, resetPassword, submitName, retry, cancel, chooseId, back }) control.gameObject.SetActive(false);
            retry.transform.SetParent(state, false); Place((RectTransform)retry.transform, 84, 960, 912, 134);
            ApplySafeArea(); scroll.StopMovement(); form.anchoredPosition = Vector2.zero;
        }
        private void SubmitLogin() { var id = loginId.text; var secret = password.text; ClearSecrets(); LoginSubmitted?.Invoke(id, secret); }
        private void ClearSecrets() { if (password != null) password.SetTextWithoutNotify(""); }
        private void OnDisable() { ClearSecrets(); }
        private void OnRectTransformDimensionsChange() { if(isActiveAndEnabled)ApplySafeArea(); }
        private void OnEnable() { ApplySafeArea(); }
        private void ApplySafeArea()
        {
            if (safe == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            form.sizeDelta = new Vector2(0, Mathf.Max(960, safe.rect.height));
            float width = safe.rect.width;
            if (width <= 0) return;
            AuthLandscapeLayout.EntryMethods(methods, width);
            AuthLandscapeLayout.EntryCredentials(credentials, width);
            AuthLandscapeLayout.EntryNickname(nicknamePage, width);
            AuthLandscapeLayout.EntryState(state, width);
        }
        private TMP_InputField Input(Transform parent, string name, string hint, TMP_FontAsset font, bool secret)
        {
            var input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources()).GetComponent<TMP_InputField>();
            input.name = name; input.transform.SetParent(parent, false);
            input.contentType = secret ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.characterLimit = secret ? 128 : 64; input.richText = false;
            input.transition = Selectable.Transition.None;
            var background = input.GetComponent<Image>(); background.sprite = null; background.color = new Color(.04f, .08f, .11f, .79f);
            InputEdge(input.transform, "TopBorder", new Vector2(0, 1), Vector2.one, new Vector2(0, 1));
            InputEdge(input.transform, "BottomBorder", Vector2.zero, new Vector2(1, 0), new Vector2(0, 1));
            InputEdge(input.transform, "RightBorder", new Vector2(1, 0), Vector2.one, new Vector2(1, 0));
            var accent = Rect("Accent", input.transform); accent.anchorMax = new Vector2(0, 1); accent.pivot = new Vector2(0, .5f); accent.sizeDelta = new Vector2(6, 0);
            var edge = accent.gameObject.AddComponent<Image>(); edge.color = new Color(.47f, .73f, .78f); edge.raycastTarget = false;
            input.textViewport.offsetMin = new Vector2(40, 12); input.textViewport.offsetMax = new Vector2(-30, -12);
            foreach (var text in input.GetComponentsInChildren<TMP_Text>(true))
            { text.font = font; text.fontSize = 34; text.fontStyle = FontStyles.Normal; text.color = new Color(.82f, .88f, .9f); text.alignment = TextAlignmentOptions.MidlineLeft; text.richText = false; }
            ((TMP_Text)input.placeholder).text = hint; input.customCaretColor = true; input.caretColor = new Color(.5f, .9f, 1); return input;
        }
        private static void InputEdge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size)
        {
            var rect = Rect(name, parent); rect.anchorMin = min; rect.anchorMax = max; rect.sizeDelta = size;
            var edge = rect.gameObject.AddComponent<Image>(); edge.color = new Color(.33f, .44f, .47f); edge.raycastTarget = false;
        }
        private Button MetalButton(Transform parent, string name, string label, TMP_FontAsset font, bool primary, Action click)
        { return AuthFormVisuals.MetalButton(parent, name, label, font, primary, click); }
        private Button Link(Transform parent, string name, string label, TMP_FontAsset font, Action click, float size = 24, bool bold = false)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            var text = Text(rect, "Label", label, font, size, bold, TextAlignmentOptions.Center); button.targetGraphic = text;
            button.onClick.AddListener(() => click()); return button;
        }
        private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = bold ? new Color(.96f, .97f, .95f) : new Color(.77f, .83f, .85f);
            text.text = value; text.alignment = align; text.richText = false; text.raycastTarget = false; return text;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; return rect;
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
