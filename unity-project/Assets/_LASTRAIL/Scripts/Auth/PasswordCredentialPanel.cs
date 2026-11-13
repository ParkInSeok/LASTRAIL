using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LastRail.Auth.AuthFormVisuals;

namespace LastRail.Auth
{
    [ExecuteAlways]
    public sealed class PasswordCredentialPanel : MonoBehaviour
    {
        private PasswordCredentialFlow model;
        private TMP_InputField loginId, recoveryEmail, verificationCode, password, confirmPassword;
        private TMP_Text status;
        private RectTransform recoveryHeaderSpacing;
        private RectTransform identityRow, emailRow, codeRow, passwordRow, confirmationRow, consents;
        private Toggle terms, privacy;
        private ScrollRect scroll;
        private string validationMessage, renderedPage;
        private Button send, confirm, complete, reconcile, cancel;
        private RectTransform safe, viewport, form;
        private int shownWaitSeconds = -1;
        private CancellationTokenSource presentationDelay;
        private bool applyingLayout;
        private bool shownExpired, actionFailed, closing;
        // Volatile UI draft only. Never persisted or passed to email-start/confirm requests.
        private char[] registrationPassword;
        public event Action Closed;

        public void Initialize(PasswordCredentialFlow flow, TMP_FontAsset font)
        {
            if (model != null) throw new InvalidOperationException("Panel already initialized.");
            if (flow == null) throw new ArgumentNullException(nameof(flow));
            if (font == null) throw new ArgumentNullException(nameof(font));
            model = flow;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 75;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var background = Rect("Background", transform).gameObject.AddComponent<Image>();
            background.color = new Color(.03f, .04f, .05f); background.raycastTarget = false;
            LastRail.Presentation.Startup.StartupMotionBackdrop.Attach(transform, true).transform.SetSiblingIndex(1);
            Rect("ReadabilityShade", transform).gameObject.AddComponent<EntryBackdropShade>().raycastTarget = false;
            safe = Rect("SafeArea", transform);
            scroll = safe.gameObject.AddComponent<ScrollRect>();
            viewport = Rect("Viewport", safe);
            viewport.offsetMin = new Vector2(0, 24); viewport.offsetMax = new Vector2(0, -120);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            form = Rect("Form", viewport);
            form.anchorMin = new Vector2(0, 1); form.anchorMax = Vector2.one; form.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport; scroll.content = form; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var header = Link(safe, "BackCredential", model.Mode == "Reset" ? "‹ 계정 복구" : model.Mode == "Link" ? "‹ 계정 연결" : "‹ 회원가입", font, CancelCredential, 38, true);
            Place((RectTransform)header.transform, 48, 20, 600, 80); header.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
            if(model.Mode == "Reset")
            {
                // Preserve the approved form positions without the removed lock/trim artwork.
                recoveryHeaderSpacing = Rect("RecoveryHeaderSpacing", form);
                Size(recoveryHeaderSpacing.gameObject, 180);
            }
            var eyebrow = Text(form, "Eyebrow", model.Mode == "Reset" ? "ACCOUNT RECOVERY" : "YOUR ACCOUNT", font, 24, true);
            eyebrow.color=new Color(.53f,.84f,.87f); Size(eyebrow.gameObject, 46);
            var title = Text(form, "Title", model.Mode == "Reset" ? "다시 연결할 수 있습니다" : model.Mode == "Link" ? "내 계정으로 이어가기" : "계정 만들기", font, 62, true); Size(title.gameObject, 100);
            status = Text(form, "Status", "", font, 30); Size(status.gameObject, 140);
            loginId = Field("LoginId", "아이디", "아이디 입력", font, false, 128, out identityRow);
            recoveryEmail = Field("RecoveryEmail", "복구 이메일", "이메일 주소 입력", font, false, 254, out emailRow);
            recoveryEmail.keyboardType = TouchScreenKeyboardType.EmailAddress;
            verificationCode = Field("VerificationCode", "이메일 인증코드", "인증코드 8자리 입력", font, false, 8, out codeRow);
            verificationCode.characterValidation = TMP_InputField.CharacterValidation.Digit;
            verificationCode.keyboardType = TouchScreenKeyboardType.NumberPad;
            password = Field("Password", "비밀번호", "비밀번호 입력 (8~128자)", font, true, 128, out passwordRow);
            confirmPassword = Field("ConfirmPassword", "비밀번호 확인", "비밀번호 다시 입력", font, true, 128, out confirmationRow);
            consents = Rect("Consents", form); Size(consents.gameObject, 156);
            terms = Consent("TermsConsent", "이용약관 동의", 0, font);
            privacy = Consent("PrivacyConsent", "개인정보 처리방침 확인", 74, font);
            send = MakeButton("SendCode", model.Mode == "Reset" ? "복구 안내 받기" : "인증코드 받기", font, SendCode);
            confirm = MakeButton("ConfirmCode", "인증코드 확인", font, ConfirmCode);
            complete = MakeButton("CompleteCredential", model.Mode == "Reset" ? "비밀번호 변경" : model.Mode == "Link" ? "계정 연결" : "계정 만들기", font, CompleteCredential);
            reconcile = MakeButton("ReconcileCredential", "완료 여부 확인", font, ReconcileCredential);
            cancel = MakeButton("CancelCredential", "로그인으로 돌아가기", font, CancelCredential, false);
            var cancelLabel = cancel.GetComponentInChildren<TMP_Text>();
            cancelLabel.alignment = TextAlignmentOptions.Center;
            cancelLabel.rectTransform.anchorMin = Vector2.zero;
            cancelLabel.rectTransform.anchorMax = Vector2.one;
            gameObject.AddComponent<AuthKeyboardAvoidance>().Initialize(scroll);
            loginId.SetTextWithoutNotify(model.LoginId ?? "");
            recoveryEmail.SetTextWithoutNotify(model.RecoveryEmail ?? "");
            if (isActiveAndEnabled) model.Changed += Render;
            Render();
        }

        public void Show() { gameObject.SetActive(true); Render(); }
        public void Hide() { ClearSecrets(); gameObject.SetActive(false); }

        private async void SendCode()
        {
            if (model == null || model.Busy || !model.CanStart) return;
            if(model.Mode=="Register"&&model.Stage==PasswordCredentialStage.Editing){
                if(password.text.Length<8||password.text.Length>128||password.text!=confirmPassword.text){validationMessage="비밀번호 8~128자와 비밀번호 확인을 확인해 주세요.";Render();return;}
                if(!terms.isOn||!privacy.isOn){validationMessage="이용약관과 개인정보 처리방침을 확인해 주세요.";Render();return;}
                ClearRegistrationDraft();registrationPassword=password.text.ToCharArray();
            }
            var id = loginId.text; var email = recoveryEmail.text;
            ClearSecretFields(); actionFailed = false; validationMessage = null;
            try { await model.StartAsync(id, email); }
            catch { actionFailed = true; }
            if(actionFailed||!string.IsNullOrEmpty(model.ErrorCode))ClearRegistrationDraft();
            Render();
        }
        private async void ConfirmCode()
        {
            if (model == null || model.Busy || !model.CanConfirm) return;
            var code = verificationCode.text;
            ClearSecretFields(); actionFailed = false; validationMessage = null;
            try { await model.ConfirmAsync(code); }
            catch { actionFailed = true; }
            finally { code = null; }
            if(actionFailed||model.Stage!=PasswordCredentialStage.Verified)ClearRegistrationDraft();
            Render();
        }
        private async void CompleteCredential()
        {
            if (model == null || model.Busy || !model.CanComplete) return;
            if (password.text != confirmPassword.text)
            { validationMessage = "비밀번호와 비밀번호 확인이 일치하지 않습니다."; Render(); return; }
            if (model.Mode != "Reset" && (!terms.isOn || !privacy.isOn))
            { validationMessage = "이용약관과 개인정보 처리방침을 확인해 주세요."; Render(); return; }
            validationMessage = null;
            var secret = password.text.Length==0&&registrationPassword!=null?new string(registrationPassword):password.text; var code = verificationCode.text;
            ClearSecrets(); actionFailed = false; validationMessage = null;
            try { await model.CompleteAsync(secret, model.Mode == "Reset" ? code : null); }
            catch { actionFailed = true; }
            finally { secret = null; code = null; }
            Render();
        }
        private async void ReconcileCredential()
        {
            if (model == null || model.Busy || !model.CanReconcile) return;
            var secret = password.text;
            ClearSecrets(); actionFailed = false; validationMessage = null;
            try { await model.ReconcileAsync(secret); }
            catch { actionFailed = true; }
            finally { secret = null; }
            Render();
        }
        private void CancelCredential()
        {
            if (closing) return;
            closing = true; ClearSecrets();
            try { model?.Cancel(); }
            catch { actionFailed = true; }
            finally { Hide(); }
            try { Closed?.Invoke(); }
            finally { closing = false; }
        }
        private void ClearSecrets()
        { ClearSecretFields();ClearRegistrationDraft(); }
        private void ClearRegistrationDraft(){if(registrationPassword!=null)Array.Clear(registrationPassword,0,registrationPassword.Length);registrationPassword=null;}
        private void ClearSecretFields()
        {
            if (verificationCode != null) verificationCode.SetTextWithoutNotify("");
            if (password != null) password.SetTextWithoutNotify("");
            if (confirmPassword != null) confirmPassword.SetTextWithoutNotify("");
        }
        private void OnEnable()
        {
            if (model != null) { model.Changed -= Render; model.Changed += Render; }
            Render();
        }
        private void OnDisable()
        {
            CancelPresentationDelay(); ClearSecrets();
            if (model != null) model.Changed -= Render;
        }
        private void OnDestroy() { CancelPresentationDelay(); ClearSecrets(); if (model != null) model.Changed -= Render; }
        private void OnApplicationFocus(bool focused) { if (focused) Render(); }
        private void OnApplicationPause(bool paused) { if (!paused) Render(); }
        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && cancel != null) ApplySafeArea();
        }
        private void CancelPresentationDelay()
        {
            var scheduled = presentationDelay; presentationDelay = null;
            scheduled?.Cancel();
        }
        private async void SchedulePresentationChange()
        {
            CancelPresentationDelay();
            var delay = model.NextPresentationChangeDelay;
            if (!isActiveAndEnabled || !delay.HasValue) return;
            var scheduled = new CancellationTokenSource(); presentationDelay = scheduled;
            try
            {
                await Task.Delay((int)Math.Min(int.MaxValue - 1, Math.Max(1, Math.Ceiling(delay.Value.TotalMilliseconds))), scheduled.Token);
                if (this != null && isActiveAndEnabled && ReferenceEquals(presentationDelay, scheduled))
                {
                    presentationDelay = null;
                    Render();
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (ReferenceEquals(presentationDelay, scheduled)) presentationDelay = null;
                scheduled.Dispose();
            }
        }

        private void Render()
        {
            if (this == null || !isActiveAndEnabled || model == null || cancel == null) return;
            var stage = model.Stage.ToString();
            var terminal = stage == "Completed" || stage == "Cancelled";
            var busy = model.Busy;
            var reset = model.Mode == "Reset";
            var registrationEntry=model.Mode=="Register"&&stage=="Editing";
            shownWaitSeconds = model.RetryWaitSeconds; shownExpired = model.ProofExpired;if(shownExpired)ClearRegistrationDraft();
            // Rendering only changes presentation, never the user's in-progress field values.
            loginId.interactable = recoveryEmail.interactable = !busy && model.CanEdit;
            verificationCode.interactable = !busy && (model.CanConfirm || (reset && model.CanComplete));
            password.interactable = !busy && (registrationEntry || model.CanComplete || model.CanReconcile);
            confirmPassword.interactable = !busy && (registrationEntry || model.CanComplete);
            terms.interactable = privacy.interactable = !busy;
            send.interactable = !busy && model.CanStart;
            confirm.interactable = !busy && model.CanConfirm;
            complete.interactable = !busy && model.CanComplete;
            reconcile.interactable = !busy && model.CanReconcile;
            cancel.interactable = true;
            var passwordStep = stage == "Verified" || stage == "VerificationUncertain" || stage == "Completing" || stage == "Uncertain";
            var useDraft=stage=="Verified"&&registrationPassword!=null;
            ((TMP_Text)password.placeholder).text=useDraft?"입력한 비밀번호 사용 (변경 가능)":"비밀번호 입력 (8~128자)";
            ((TMP_Text)confirmPassword.placeholder).text=useDraft?"변경할 때 비밀번호 다시 입력":"비밀번호 다시 입력";
            var codeStep = stage == "AwaitingCode" || stage == "Confirming";
            if (recoveryHeaderSpacing != null) recoveryHeaderSpacing.gameObject.SetActive(false);
            identityRow.gameObject.SetActive(!terminal);
            emailRow.gameObject.SetActive(!terminal && !passwordStep && !codeStep);
            codeRow.gameObject.SetActive(!terminal && codeStep);
            passwordRow.gameObject.SetActive(!terminal && (registrationEntry || passwordStep || reset && codeStep));
            confirmationRow.gameObject.SetActive(!terminal && stage != "Uncertain" && (registrationEntry || passwordStep || reset && codeStep));
            consents.gameObject.SetActive(!terminal && !reset && (registrationEntry || passwordStep) && stage != "Uncertain");
            if(registrationEntry){passwordRow.SetSiblingIndex(identityRow.GetSiblingIndex()+1);confirmationRow.SetSiblingIndex(passwordRow.GetSiblingIndex()+1);emailRow.SetSiblingIndex(confirmationRow.GetSiblingIndex()+1);}
            send.GetComponentInChildren<TMP_Text>().text=registrationEntry?"이메일 인증하기":reset?"복구 안내 받기":"인증코드 받기";
            send.gameObject.SetActive(!terminal && (!passwordStep || model.ProofExpired));
            confirm.gameObject.SetActive(!reset && !terminal && (stage == "AwaitingCode" || stage == "Confirming"));
            complete.gameObject.SetActive(!terminal && stage != "Uncertain" && (passwordStep || reset && codeStep));
            reconcile.gameObject.SetActive(!terminal && (stage == "Uncertain" || model.CanReconcile));
            if (terminal) ClearSecrets();
            status.text = validationMessage ?? StatusCopy(stage);
            if (!terminal && send.gameObject.activeSelf && shownWaitSeconds > 0)
                status.text += "\n\uB2E4\uC2DC \uC2DC\uB3C4\uAE4C\uC9C0 " + shownWaitSeconds + "\uCD08";
            ApplySafeArea();
            var page = terminal ? "terminal" : passwordStep ? "password" : codeStep ? "code" : "identity";
            if(renderedPage != page) { renderedPage=page; scroll.StopMovement(); form.anchoredPosition=Vector2.zero; }
            SchedulePresentationChange();
        }

        private string StatusCopy(string stage)
        {
            if (stage == "Completed") return model.Mode == "Reset" ? "\uBE44\uBC00\uBC88\uD638\uAC00 \uBCC0\uACBD\uB418\uC5C8\uC2B5\uB2C8\uB2E4." : "\uACC4\uC815 \uC124\uC815\uC774 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
            if (stage == "Cancelled") return "\uC694\uCCAD\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
            if (actionFailed) return "\uC694\uCCAD\uC744 \uCC98\uB9AC\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uC7A0\uC2DC \uD6C4 \uB2E4\uC2DC \uD655\uC778\uD574 \uC8FC\uC138\uC694.";
            if (stage == "Sending") return "\uC694\uCCAD\uC744 \uD655\uC778\uD558\uB294 \uC911\uC785\uB2C8\uB2E4.";
            if (stage == "Confirming") return "\uC778\uC99D\uCF54\uB4DC\uB97C \uD655\uC778\uD558\uB294 \uC911\uC785\uB2C8\uB2E4.";
            if (stage == "Completing") return "\uC644\uB8CC \uC5EC\uBD80\uB97C \uD655\uC778\uD558\uB294 \uC911\uC785\uB2C8\uB2E4.";
            if (stage == "Uncertain") return "\uC644\uB8CC \uC5EC\uBD80\uB97C \uD655\uC778\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uBE44\uBC00\uBC88\uD638\uB97C \uB2E4\uC2DC \uC785\uB825\uD574 \uD655\uC778\uD574 \uC8FC\uC138\uC694.";
            if (stage == "VerificationUncertain") return "\uC778\uC99D \uC5EC\uBD80\uAC00 \uBD88\uD655\uC2E4\uD569\uB2C8\uB2E4. \uBE44\uBC00\uBC88\uD638\uB97C \uC785\uB825\uD574 \uC644\uB8CC\uB97C \uC694\uCCAD\uD558\uBA74 \uC11C\uBC84\uC5D0\uC11C \uC778\uC99D \uC5EC\uBD80\uB97C \uD655\uC778\uD569\uB2C8\uB2E4.";
            if (shownExpired) return "\uC778\uC99D \uC720\uD6A8 \uC2DC\uAC04\uC774 \uC9C0\uB0AC\uC2B5\uB2C8\uB2E4. \uC778\uC99D\uCF54\uB4DC\uB97C \uB2E4\uC2DC \uC694\uCCAD\uD574 \uC8FC\uC138\uC694.";
            if (!string.IsNullOrEmpty(model.ErrorCode)) return ErrorCopy(model.ErrorCode);
            if (stage == "Verified" && registrationPassword!=null) return "이메일 인증이 완료되었습니다. 입력한 비밀번호로 계정을 만듭니다.";
            if (stage == "Verified") return "\uC774\uBA54\uC77C \uC778\uC99D\uC774 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4. \uC0C8 \uBE44\uBC00\uBC88\uD638\uB97C \uC785\uB825\uD574 \uC8FC\uC138\uC694.";
            if (stage == "AwaitingCode") return model.Mode == "Reset"
                ? "\uC694\uCCAD\uC744 \uC811\uC218\uD588\uC2B5\uB2C8\uB2E4. \uC785\uB825\uD55C \uC815\uBCF4\uAC00 \uC77C\uCE58\uD558\uBA74 \uC774\uBA54\uC77C\uB85C \uC548\uB0B4\uB429\uB2C8\uB2E4."
                : "\uC774\uBA54\uC77C\uC758 \uC778\uC99D\uCF54\uB4DC\uB97C \uC785\uB825\uD574 \uC8FC\uC138\uC694.";
            return model.Mode == "Reset" ? "\uC544\uC774\uB514\uC640 \uBCF5\uAD6C \uC774\uBA54\uC77C\uC744 \uC785\uB825\uD574 \uC8FC\uC138\uC694." : model.Mode=="Register"&&stage=="Editing" ? "아이디, 비밀번호와 복구 이메일을 입력해 주세요." : "\uC0AC\uC6A9\uD560 \uC544\uC774\uB514\uC640 \uBCF5\uAD6C \uC774\uBA54\uC77C\uC744 \uC785\uB825\uD574 \uC8FC\uC138\uC694.";
        }
        private static string ErrorCopy(string errorCode)
        {
            switch (errorCode)
            {
                case "client_validation_failed": return "\uC785\uB825\uD55C \uC815\uBCF4\uB97C \uD655\uC778\uD574 \uC8FC\uC138\uC694.";
                case "recovery_email_resend_limited": return "\uC7A0\uC2DC \uD6C4 \uC778\uC99D\uCF54\uB4DC\uB97C \uB2E4\uC2DC \uC694\uCCAD\uD574 \uC8FC\uC138\uC694.";
                case "recovery_email_verification_expired":
                case "verification_expired": return "\uC778\uC99D \uC720\uD6A8 \uC2DC\uAC04\uC774 \uC9C0\uB0AC\uC2B5\uB2C8\uB2E4. \uC778\uC99D\uCF54\uB4DC\uB97C \uB2E4\uC2DC \uC694\uCCAD\uD574 \uC8FC\uC138\uC694.";
                case "recovery_email_verification_invalid":
                case "verification_code_invalid": return "\uC778\uC99D\uCF54\uB4DC\uB97C \uD655\uC778\uD574 \uC8FC\uC138\uC694.";
                default: return "\uC694\uCCAD\uC744 \uCC98\uB9AC\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uC785\uB825\uD55C \uC815\uBCF4\uB97C \uD655\uC778\uD574 \uC8FC\uC138\uC694.";
            }
        }

        private void ApplySafeArea()
        {
            if (applyingLayout) return;
            applyingLayout = true;
            try { LayoutSafeArea(); }
            finally { applyingLayout = false; }
        }
        private void LayoutSafeArea()
        {
            if (safe == null || form == null) return;
            if (Screen.width > 0 && Screen.height > 0)
            {
                var area = Screen.safeArea;
                safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }
            // The description stays in the left column. Active fields flow across the right
            // column in pairs; stage changes alter visibility without moving data ownership.
            float width = safe.rect.width;
            if (width <= 0) return;
            float right = width * .38f, available = width - right - 48;
            float cell = (available - 24) * .5f;
            AuthLandscapeLayout.Item(form, "Eyebrow", 48, 36, right - 96, 50);
            AuthLandscapeLayout.Item(form, "Title", 48, 108, right - 96, 200);
            AuthLandscapeLayout.Item(form, "Status", 48, 340, right - 96, 450);
            int count = 0;
            foreach (var row in new[] { identityRow, passwordRow, confirmationRow, emailRow, codeRow })
            {
                if (!row.gameObject.activeSelf) continue;
                // The input ends at 170; include its centered 1-unit border within the row.
                Place(row, right + (count % 2) * (cell + 24), 32 + (count / 2) * 190, cell, 176);
                var input = (RectTransform)row.GetComponentInChildren<TMP_InputField>(true).transform;
                input.anchorMin = new Vector2(0, 1); input.anchorMax = Vector2.one;
                input.pivot = new Vector2(.5f, 1); input.anchoredPosition = new Vector2(0, -56);
                input.sizeDelta = new Vector2(0, 114);
                count++;
            }
            float y = 32 + ((count + 1) / 2) * 190;
            if (consents.gameObject.activeSelf)
            {
                Place(consents, right, y, available, 156);
                AuthLandscapeLayout.Item(consents, "TermsConsent", 0, 0, available - 140, 64);
                AuthLandscapeLayout.Item(consents, "PrivacyConsent", 0, 74, available - 140, 64);
                AuthLandscapeLayout.Item(consents, "TermsConsentView", available - 132, 0, 132, 64);
                AuthLandscapeLayout.Item(consents, "PrivacyConsentView", available - 132, 74, 132, 64);
                y += 180;
            }
            int action = 0;
            foreach (var button in new[] { send, confirm, complete, reconcile, cancel })
            {
                if (!button.gameObject.activeSelf) continue;
                Place((RectTransform)button.transform, right + (action % 2) * (cell + 24), y + (action / 2) * 140, cell, 120);
                button.GetComponentInChildren<TMP_Text>().fontSize = 28;
                action++;
            }
            float height = Mathf.Max(840, y + ((action + 1) / 2) * 140 + 32);
            form.sizeDelta = new Vector2(0, Mathf.Max(height, viewport.rect.height));
        }
        private TMP_InputField Field(string name, string label, string hint, TMP_FontAsset font, bool secret, int limit, out RectTransform row)
        {
            row=Rect(name+"Field",form); Size(row.gameObject,176);
            var caption=Text(row,name+"Label",label,font,28); caption.rectTransform.anchorMin=new Vector2(0,1); caption.rectTransform.anchorMax=Vector2.one; caption.rectTransform.pivot=new Vector2(.5f,1); caption.rectTransform.sizeDelta=new Vector2(0,48);
            var input=AuthFormVisuals.Input(row,name,hint,font,secret,limit);
            var rect=(RectTransform)input.transform; rect.anchorMin=new Vector2(0,1); rect.anchorMax=Vector2.one; rect.pivot=new Vector2(.5f,1); rect.anchoredPosition=new Vector2(0,-62); rect.sizeDelta=new Vector2(0,114);
            return input;
        }
        private Toggle Consent(string name,string label,float y,TMP_FontAsset font)
        {
            var root=Rect(name,consents); Place(root,0,y,760,64);
            var hit=root.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            var toggle=root.gameObject.AddComponent<Toggle>();
            var box=Rect("Box",root); Place(box,0,12,36,36); var image=box.gameObject.AddComponent<Image>(); image.color=new Color(.45f,.72f,.76f);
            var fill=Rect("Fill",box); fill.offsetMin=new Vector2(4,4); fill.offsetMax=new Vector2(-4,-4); fill.gameObject.AddComponent<Image>().color=new Color(.03f,.06f,.07f);
            var tick=Rect("Check",fill); tick.offsetMin=new Vector2(4,4); tick.offsetMax=new Vector2(-4,-4); var check=tick.gameObject.AddComponent<Image>(); check.color=new Color(.35f,.9f,1);
            toggle.targetGraphic=image; toggle.graphic=check; toggle.isOn=false;
            var text=Text(root,"Label",label,font,28); text.rectTransform.offsetMin=new Vector2(52,0);
            var view=Link(consents,name+"View","보기",font,()=>{validationMessage="문서 연결을 준비 중입니다."; Render();},26); Place((RectTransform)view.transform,780,y,132,64);
            toggle.onValueChanged.AddListener(_=>{validationMessage=null; Render();}); return toggle;
        }
        private Button MakeButton(string name,string label,TMP_FontAsset font,Action click,bool primary=true)
        { var button=MetalButton(form,name,label,font,primary,click); Size(button.gameObject,134); return button; }
        private static void Size(GameObject target,float height)
        { var size=target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>(); size.minHeight=size.preferredHeight=height; }
    }
}
