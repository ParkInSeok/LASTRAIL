using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class PasswordCredentialPanelTests
    {
        private const string Login = "fixture_id";
        private const string Email = "synthetic@example.invalid";
        private const string Code = "00123456";
        private const string Secret = "fixture_password";
        private const string Challenge = "email_challenge_fixture";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private static readonly DateTimeOffset Origin = DateTimeOffset.Parse("2026-09-06T00:00:00Z");
        private static readonly string[] Actions = { "SendCode", "ConfirmCode", "CompleteCredential", "ReconcileCredential", "CancelCredential" };

        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public readonly List<string> SecretsAtDispatch = new List<string>();
            public Func<string> ReadSecrets;
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> ReplyOverride;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                SecretsAtDispatch.Add(ReadSecrets == null ? "" : ReadSecrets());
                return ReplyOverride == null ? Task.FromResult(Reply(request)) : ReplyOverride(request);
            }
        }

        // Only the proposed types are reflected; UI actions and transport payloads remain real.
        private sealed class Fixture : IDisposable
        {
            public readonly Wire Wire = new Wire();
            public readonly object Flow;
            public readonly GameObject Root;
            public readonly Component Panel;
            public GameApiAuthSessionSnapshot Session;
            public DateTimeOffset Now = Origin;
            public int Commits;
            public Fixture(string mode = "Register")
            {
                var panelType = RequiredType("PasswordCredentialPanel");
                var flowType = RequiredType("PasswordCredentialFlow");
                Session = mode == "Link" || mode == "Reset" ? SupportInquiryTests.Session("account_test") : null;
                var client = Activator.CreateInstance(RequiredType("PasswordEmailClient"), Wire);
                Flow = Activator.CreateInstance(flowType, client, mode,
                    (Func<GameApiAuthSessionSnapshot>)(() => Session), (Func<long>)(() => 0L),
                    (Func<GameApiAuthSessionSnapshot, Task<bool>>)(candidate =>
                    { Commits++; Session = candidate; return Task.FromResult(true); }),
                    (Func<DateTimeOffset>)(() => Now));
                Root = new GameObject("PasswordCredentialFixture", typeof(RectTransform));
                try
                {
                    Panel = Root.AddComponent(panelType);
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                    Assert.That(font, Is.Not.Null, "Existing entry font is required.");
                    var initialize = panelType.GetMethod("Initialize", new[] { flowType, typeof(TMP_FontAsset) });
                    Assert.That(initialize, Is.Not.Null, "Initialize(flow, font) is required.");
                    initialize.Invoke(Panel, new[] { Flow, (object)font });
                    PanelCall("Show");
                    Wire.ReadSecrets = () => Input("Password").text + Input("ConfirmPassword").text + Input("VerificationCode").text;
                }
                catch { UnityEngine.Object.DestroyImmediate(Root); throw; }
            }
            public TMP_InputField Input(string name) => Root.GetComponentsInChildren<TMP_InputField>(true).Single(x => x.name == name);
            public Button Button(string name) => Root.GetComponentsInChildren<Button>(true).Single(x => x.name == name);
            public string Status => Root.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "Status").text;
            public object Get(string name) => Flow.GetType().GetProperty(name).GetValue(Flow);
            public string Stage => Get("Stage").ToString();
            public void PanelCall(string name)
            {
                var method = Panel.GetType().GetMethod(name, Type.EmptyTypes);
                Assert.That(method, Is.Not.Null, name + "() must be public.");
                method.Invoke(Panel, null);
            }
            public void OnClosed(Action callback)
            {
                var closed = Panel.GetType().GetEvent("Closed");
                Assert.That(closed, Is.Not.Null);
                Assert.That(closed.EventHandlerType, Is.EqualTo(typeof(Action)));
                closed.AddEventHandler(Panel, callback);
            }
            public void Click(string name)
            {
                var button = Button(name);
                Assert.That(button.gameObject.activeInHierarchy, Is.True, name + " must be visible.");
                Assert.That(button.IsInteractable(), Is.True, name + " must be usable.");
                button.onClick.Invoke();
            }
            public void Identity()
            { Input("LoginId").text = Login; Input("RecoveryEmail").text = Email; }
            public void Secrets()
            { Input("VerificationCode").text = Code; Input("Password").text = Secret; Input("ConfirmPassword").text = Secret; foreach(var toggle in Root.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true; }
            // Resume presentation after advancing the injected clock, as foregrounding the app does.
            public void RegainFocus() => Panel.GetType().GetMethod("OnApplicationFocus",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(Panel, new object[] { true });
            public async Task Start()
            { Identity(); if((string)Get("Mode")=="Register")Secrets(); Click("SendCode"); await Idle(); }
            public async Task Verify()
            { await Start(); Input("VerificationCode").text = Code; Click("ConfirmCode"); await Idle(); }
            public async Task Idle()
            {
                for (var i = 0; i < 100 && (bool)Get("Busy"); i++) await Task.Yield();
                Assert.That(Get("Busy"), Is.False, "Fixture response should settle without wall-clock delays.");
            }
            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
        }

        [Test]
        public void RepeatedConsentChangesPreserveLandscapeFieldsAndSecrets()
        {
            using var f = new Fixture();
            Canvas.ForceUpdateCanvases(); f.RegainFocus();
            f.Input("Password").SetTextWithoutNotify(Secret);
            f.Input("ConfirmPassword").SetTextWithoutNotify(Secret);
            var count = f.Root.GetComponentsInChildren<LayoutElement>(true).Length;
            foreach (var toggle in f.Root.GetComponentsInChildren<Toggle>(true))
                for (int i = 0; i < 10; i++) toggle.isOn = !toggle.isOn;
            Canvas.ForceUpdateCanvases(); f.RegainFocus();
            Assert.That(f.Root.GetComponentsInChildren<LayoutElement>(true).Length, Is.EqualTo(count));
            var names = new[] { "LoginId", "Password", "ConfirmPassword", "RecoveryEmail" };
            var rows = names.Select(name => (RectTransform)f.Input(name).transform.parent).ToArray();
            foreach (var row in rows)
            {
                Assert.That(row.gameObject.activeInHierarchy, Is.True, row.name);
                var input = (RectTransform)row.GetComponentInChildren<TMP_InputField>().transform;
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(row, input);
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(row.rect.yMin - .1f), row.name);
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(row.rect.yMax + .1f), row.name);
            }
            for (int i = 0; i < rows.Length; i++)
                for (int j = i + 1; j < rows.Length; j++)
                {
                    var a = RectTransformUtility.CalculateRelativeRectTransformBounds(rows[i].parent, rows[i]);
                    var b = RectTransformUtility.CalculateRelativeRectTransformBounds(rows[j].parent, rows[j]);
                    Assert.That(a.Intersects(b), Is.False, rows[i].name + " overlaps " + rows[j].name);
                }
            Assert.That(f.Input("Password").text, Is.EqualTo(Secret));
            Assert.That(f.Input("ConfirmPassword").text, Is.EqualTo(Secret));
            Assert.That(f.Wire.Requests, Is.Empty, "Presentation changes must not dispatch requests.");
        }
        [Test]
        public async Task ApprovedRegistrationInputsSurviveOnlyTheLocalVerificationJourney()
        {
            using var f=new Fixture();
            foreach(string name in new[]{"LoginId","Password","ConfirmPassword","RecoveryEmail"})
                Assert.That(f.Input(name).gameObject.activeInHierarchy,Is.True,name+" initial approved field");
            f.Identity();f.Secrets();f.Click("SendCode");await f.Idle();
            Assert.That(f.Stage,Is.EqualTo("AwaitingCode"));Assert.That(f.Commits,Is.Zero);
            Assert.That(f.Wire.Requests.Count(r=>r.EndpointPath.EndsWith("/register")),Is.Zero);
            Assert.That(f.Input("Password").text,Is.Empty,"Fields clear before sending; temporary draft must not be displayed.");
            f.Input("VerificationCode").text=Code;f.Click("ConfirmCode");await f.Idle();
            Assert.That(f.Stage,Is.EqualTo("Verified"));Assert.That(f.Input("Password").text,Is.Empty);
            f.Click("CompleteCredential");await f.Idle();Assert.That(f.Commits,Is.EqualTo(1));
            Assert.That(f.Input("Password").text,Is.Empty);Assert.That(f.Wire.SecretsAtDispatch.All(string.IsNullOrEmpty),Is.True);
        }
        [Test]
        public async Task HidingRegistrationClearsDraftBeforeDelayedVerificationReturns()
        {
            using var f=new Fixture();await f.Start();
            var reply=new TaskCompletionSource<RunApiHttpResponse>();f.Wire.ReplyOverride=_=>reply.Task;
            f.Input("VerificationCode").text=Code;f.Click("ConfirmCode");f.PanelCall("Hide");
            reply.SetResult(Reply(f.Wire.Requests.Last()));await f.Idle();f.PanelCall("Show");
            int before=f.Wire.Requests.Count;f.Click("CompleteCredential");await f.Idle();
            Assert.That(f.Wire.Requests.Count,Is.EqualTo(before));Assert.That(f.Commits,Is.Zero);
        }
        [Test]
        public void IncompleteRegistrationDoesNotSendEmail()
        {
            using var f=new Fixture();f.Identity();f.Click("SendCode");
            Assert.That(f.Wire.Requests,Is.Empty);Assert.That(f.Stage,Is.EqualTo("Editing"));
        }
        private static Type RequiredType(string name)
        {
            var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth." + name);
            Assert.That(type, Is.Not.Null, "Missing proposed type LastRail.Auth." + name);
            return type;
        }
        private static RunApiHttpResponse Verification(string status, int? wait) => RunApiHttpResponse.Success(200,
            new JObject { ["accepted"] = true, ["verificationStatus"] = status, ["challengeId"] = Challenge,
                ["expiresAt"] = "2026-09-06T00:15:00.000Z", ["retryAfterSeconds"] = wait }.ToString());
        private static RunApiHttpResponse Bundle(string field, string status) => RunApiHttpResponse.Success(200,
            new JObject { ["accepted"] = true, [field] = status, ["tokenType"] = "Bearer",
                ["account"] = new JObject { ["id"] = "account_test", ["status"] = "ACTIVE" },
                ["accessToken"] = "lst1.fixture", ["refreshToken"] = "fixture_refresh",
                ["accessTokenExpiresAt"] = "2030-01-01T00:00:00.000Z", ["refreshTokenExpiresAt"] = "2030-02-01T00:00:00.000Z" }.ToString());
        private static RunApiHttpResponse Reply(RunApiHttpRequest request)
        {
            switch (request.EndpointPath)
            {
                case "/v1/auth/password/recovery-email/verify/start": return Verification("PENDING", 120);
                case "/v1/auth/password/recovery-email/verify/confirm": return Verification("VERIFIED", null);
                case "/v1/auth/password/register": return Bundle("registerStatus", "REGISTERED");
                case "/v1/auth/password/link": return Bundle("linkStatus", "LINKED");
                case "/v1/auth/password/login": return Bundle("signInStatus", "SIGNED_IN");
                case "/v1/auth/password/reset/start": return RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"EMAIL_SENT_IF_AVAILABLE\"}");
                case "/v1/auth/password/reset/confirm": return RunApiHttpResponse.Success(200, "{\"accepted\":true,\"resetStatus\":\"RESET_CONFIRMED\"}");
                default: throw new InvalidOperationException("Unexpected fixture route.");
            }
        }
        private static void Cleared(Fixture f)
        {
            Assert.That(f.Input("Password").text, Is.Empty);
            Assert.That(f.Input("ConfirmPassword").text, Is.Empty);
            Assert.That(f.Input("VerificationCode").text, Is.Empty);
        }
        private static void SafeKorean(string text)
        {
            Assert.That(string.IsNullOrWhiteSpace(text), Is.False);
            Assert.That(text.Any(c => c >= '\uAC00' && c <= '\uD7A3'), Is.True, "Status requires Korean copy.");
            foreach (var secret in new[] { Login, Email, Code, Secret, Challenge, "<b>", "raw_fixture_error" })
                Assert.That(text, Does.Not.Contain(secret));
        }

        [TestCase("Register")]
        [TestCase("Link")]
        [TestCase("Reset")]
        public void PublicPanelContractProvidesNamedControlsAndMasksPassword(string mode)
        {
            using (var f = new Fixture(mode))
            {
                foreach (var name in new[] { "LoginId", "RecoveryEmail", "VerificationCode", "Password" })
                    Assert.That(f.Input(name).richText, Is.False, name);
                foreach (var name in Actions) Assert.That(f.Button(name), Is.Not.Null);
                Assert.That(f.Input("Password").contentType, Is.EqualTo(TMP_InputField.ContentType.Password));
                Assert.That(f.Input("Password").inputType, Is.EqualTo(TMP_InputField.InputType.Password));
                var code = f.Input("VerificationCode");
                // TMP changes Standard to Custom when Digit validation is assigned.
                Assert.That(code.contentType == TMP_InputField.ContentType.Standard || code.contentType == TMP_InputField.ContentType.Custom, Is.True);
                Assert.That(code.inputType, Is.EqualTo(TMP_InputField.InputType.Standard));
                Assert.That(code.characterValidation, Is.EqualTo(TMP_InputField.CharacterValidation.Digit));
                Assert.That(code.keyboardType, Is.EqualTo(TouchScreenKeyboardType.NumberPad));
                Assert.That(code.characterLimit, Is.EqualTo(8));
                code.text = Code; Assert.That(code.text, Is.EqualTo(Code));
                Assert.That(f.Input("LoginId").interactable, Is.EqualTo((bool)f.Get("CanEdit")));
                Assert.That(f.Input("RecoveryEmail").interactable, Is.EqualTo((bool)f.Get("CanEdit")));
            }
        }

        [TestCase("Register", "PASSWORD_REGISTER")]
        [TestCase("Link", "PASSWORD_LINK")]
        public async Task SendUsesTypedIdentityAndClearsBothSecretsBeforeDispatch(string mode, string purpose)
        {
            using (var f = new Fixture(mode))
            {
                f.Identity(); f.Secrets();
                var secretsAtChanged = new List<string>();
                f.Flow.GetType().GetEvent("Changed").AddEventHandler(f.Flow,
                    (Action)(() => secretsAtChanged.Add(f.Input("Password").text + f.Input("VerificationCode").text)));
                f.Click("SendCode"); await f.Idle();
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                var body = JObject.Parse(f.Wire.Requests[0].BodyJson);
                Assert.That((string)body["recoveryEmail"], Is.EqualTo(Email));
                Assert.That((string)body["purpose"], Is.EqualTo(purpose));
                Assert.That(f.Get("LoginId"), Is.EqualTo(Login));
                Assert.That(secretsAtChanged, Is.Not.Empty);
                Assert.That(secretsAtChanged.All(string.IsNullOrEmpty), Is.True, "Clear before entering the flow, including Changed callbacks.");
                Assert.That(f.Wire.SecretsAtDispatch.Single(), Is.Empty); Cleared(f);
                Assert.That(f.Input("LoginId").interactable, Is.False);
                Assert.That(f.Input("RecoveryEmail").interactable, Is.False);
            }
        }

        [Test]
        public async Task ConfirmPreservesLeadingZerosAndClearsSecretsBeforeDispatch()
        {
            using (var f = new Fixture())
            {
                await f.Start(); f.Secrets(); f.Click("ConfirmCode"); await f.Idle();
                var request = f.Wire.Requests.Last();
                Assert.That(request.EndpointPath, Is.EqualTo("/v1/auth/password/recovery-email/verify/confirm"));
                var code = JObject.Parse(request.BodyJson)["verificationCode"];
                Assert.That(code.Type, Is.EqualTo(JTokenType.String)); Assert.That((string)code, Is.EqualTo(Code));
                Assert.That(f.Wire.SecretsAtDispatch.Last(), Is.Empty); Cleared(f);
                Assert.That(f.Stage, Is.EqualTo("Verified"));
            }
        }

        [TestCase("Register", "/v1/auth/password/register")]
        [TestCase("Link", "/v1/auth/password/link")]
        public async Task CompleteDispatchesCapturedPasswordOnlyAfterClearingFields(string mode, string endpoint)
        {
            using (var f = new Fixture(mode))
            {
                await f.Verify(); f.Secrets(); f.Click("CompleteCredential"); await f.Idle();
                var request = f.Wire.Requests.Last(); Assert.That(request.EndpointPath, Is.EqualTo(endpoint));
                var body = JObject.Parse(request.BodyJson);
                Assert.That((string)body["password"], Is.EqualTo(Secret));
                Assert.That((string)body["loginId"], Is.EqualTo(Login));
                Assert.That((string)body["verificationChallengeId"], Is.EqualTo(Challenge));
                Assert.That(f.Wire.SecretsAtDispatch.Last(), Is.Empty); Cleared(f);
                Assert.That(f.Commits, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ResetUsesStringCodeAndNewPasswordWithoutAdoptingSession()
        {
            using (var f = new Fixture("Reset"))
            {
                var guest = f.Session; await f.Start();
                SafeKorean(f.Status);
                Assert.That(f.Status, Does.Not.Contain("EMAIL_SENT_IF_AVAILABLE"));
                Assert.That(f.Status, Does.Not.Contain("\uBC1C\uC1A1\uD588"));
                Assert.That(f.Status, Does.Not.Contain("\uBC1C\uC1A1\uB418\uC5C8"));
                Assert.That(f.Status, Does.Not.Contain("\uC804\uC1A1\uD588"));
                Assert.That(f.Button("ConfirmCode").gameObject.activeInHierarchy && f.Button("ConfirmCode").interactable, Is.False);
                f.Secrets(); f.Click("CompleteCredential"); await f.Idle();
                Assert.That(f.Wire.Requests.Select(r => r.EndpointPath), Is.EqualTo(new[] { "/v1/auth/password/reset/start", "/v1/auth/password/reset/confirm" }));
                var body = JObject.Parse(f.Wire.Requests.Last().BodyJson);
                Assert.That(body["verificationCode"].Type, Is.EqualTo(JTokenType.String));
                Assert.That((string)body["verificationCode"], Is.EqualTo(Code));
                Assert.That((string)body["newPassword"], Is.EqualTo(Secret));
                Assert.That(f.Wire.SecretsAtDispatch.Last(), Is.Empty); Cleared(f);
                Assert.That(f.Session, Is.SameAs(guest)); Assert.That(f.Commits, Is.Zero);
            }
        }

        [Test]
        public async Task ReconcileUsesFreshPasswordWithoutRepeatingRegistration()
        {
            using (var f = new Fixture())
            {
                await f.Verify();
                f.Wire.ReplyOverride = r => Task.FromResult(r.EndpointPath.EndsWith("/register") ? RunApiHttpResponse.NetworkError("raw_fixture_error") : Reply(r));
                f.Input("Password").text = Secret; f.Input("ConfirmPassword").text = Secret; foreach(var toggle in f.Root.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true; f.Click("CompleteCredential"); await f.Idle();
                Assert.That(f.Stage, Is.EqualTo("Uncertain"));
                Assert.That(f.Button("CompleteCredential").gameObject.activeInHierarchy && f.Button("CompleteCredential").interactable, Is.False);
                f.Secrets(); f.Click("ReconcileCredential"); await f.Idle();
                Assert.That(f.Wire.Requests.Last().EndpointPath, Is.EqualTo("/v1/auth/password/login"));
                Assert.That((string)JObject.Parse(f.Wire.Requests.Last().BodyJson)["password"], Is.EqualTo(Secret));
                Assert.That(f.Wire.Requests.Count(r => r.EndpointPath.EndsWith("/register")), Is.EqualTo(1));
                Assert.That(f.Wire.SecretsAtDispatch.Last(), Is.Empty); Cleared(f);
            }
        }

        [TestCase("Register", "/v1/auth/password/register")]
        [TestCase("Link", "/v1/auth/password/link")]
        public async Task UncertainVerificationOffersServerCheckedCompletionWithoutReplayingCode(string mode, string endpoint)
        {
            using (var f = new Fixture(mode))
            {
                await f.Start();
                f.Wire.ReplyOverride = r => Task.FromResult(r.EndpointPath.EndsWith("/verify/confirm")
                    ? RunApiHttpResponse.NetworkError("raw_fixture_error") : Reply(r));
                f.Input("VerificationCode").text = Code; f.Click("ConfirmCode"); await f.Idle();
                Assert.That(f.Stage, Is.EqualTo("VerificationUncertain"));
                Assert.That(f.Get("CanConfirm"), Is.False);
                Assert.That(f.Button("ConfirmCode").gameObject.activeInHierarchy, Is.False);
                Assert.That(f.Input("VerificationCode").interactable, Is.False);
                Assert.That(f.Input("Password").interactable, Is.True);
                SafeKorean(f.Status);
                Assert.That(f.Status, Does.Contain("\uC11C\uBC84"));
                Assert.That(f.Status, Does.Contain("\uC644\uB8CC"));
                Assert.That(f.Status, Does.Not.Contain("\uCF54\uB4DC\uB97C \uB2E4\uC2DC \uC785\uB825"));
                Assert.That(f.Commits, Is.Zero);
                f.Secrets(); f.Click("CompleteCredential"); await f.Idle();
                Assert.That(f.Wire.Requests.Last().EndpointPath, Is.EqualTo(endpoint));
                Assert.That(f.Wire.Requests.Count(r => r.EndpointPath.EndsWith("/verify/confirm")), Is.EqualTo(1));
                Assert.That(f.Wire.SecretsAtDispatch.Last(), Is.Empty); Cleared(f);
                Assert.That(f.Stage, Is.EqualTo("Completed")); Assert.That(f.Commits, Is.EqualTo(1));
            }
        }

        [TestCase("Hide")]
        [TestCase("Disable")]
        public void HidingOrDisablingClearsSecretsAndReopenDoesNotRestoreThem(string action)
        {
            using (var f = new Fixture())
            {
                f.Secrets();
                if (action == "Hide") f.PanelCall("Hide"); else f.Root.SetActive(false);
                Cleared(f); Assert.That(f.Root.activeSelf, Is.False);
                f.PanelCall("Show"); Cleared(f); Assert.That(f.Root.activeSelf, Is.True);
                Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [TestCase("SendCode")]
        [TestCase("ConfirmCode")]
        [TestCase("CompleteCredential")]
        [TestCase("ReconcileCredential")]
        public async Task BusyDisablesEveryActionExceptCancelAndCancelPrecedesClosed(string action)
        {
            using (var f = new Fixture("Link"))
            {
                var guest = f.Session;
                if (action == "ConfirmCode") await f.Start();
                if (action == "CompleteCredential" || action == "ReconcileCredential") await f.Verify();
                if (action == "ReconcileCredential")
                {
                    f.Wire.ReplyOverride = _ => Task.FromResult(RunApiHttpResponse.NetworkError("raw_fixture_error"));
                    f.Input("Password").text = Secret; f.Input("ConfirmPassword").text = Secret; foreach(var toggle in f.Root.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true; f.Click("CompleteCredential"); await f.Idle();
                    Assert.That(f.Stage, Is.EqualTo("Uncertain"));
                }
                var pending = new TaskCompletionSource<RunApiHttpResponse>();
                f.Wire.ReplyOverride = _ => pending.Task;
                f.Identity(); f.Secrets(); var before = f.Wire.Requests.Count; f.Click(action);
                try
                {
                    Assert.That(f.Get("Busy"), Is.True);
                    foreach (var name in Actions.Where(x => x != "CancelCredential"))
                        Assert.That(f.Button(name).interactable, Is.False, name);
                    foreach (var input in f.Root.GetComponentsInChildren<TMP_InputField>(true))
                        Assert.That(input.interactable, Is.False, input.name);
                    var closedCount = 0; string stageAtClosed = null; string secretsAtClosed = null;
                    f.OnClosed(() => { closedCount++; stageAtClosed = f.Stage; secretsAtClosed = f.Input("Password").text + f.Input("VerificationCode").text; });
                    f.Secrets(); f.Click("CancelCredential");
                    Assert.That(closedCount, Is.EqualTo(1)); Assert.That(stageAtClosed, Is.EqualTo("Cancelled"));
                    Assert.That(secretsAtClosed, Is.Empty); Assert.That(f.Root.activeSelf, Is.False);
                    Assert.That(f.Session, Is.SameAs(guest)); Assert.That(f.Commits, Is.Zero);
                }
                finally { pending.TrySetResult(Verification("PENDING", 120)); }
                await f.Idle(); Assert.That(f.Stage, Is.EqualTo("Cancelled"));
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(before + 1)); Assert.That(f.Commits, Is.Zero);
            }
        }

        [Test]
        public void CountdownRendersDoNotOverwriteUnsubmittedIdentityOrSecrets()
        {
            using (var f = new Fixture())
            {
                f.Identity(); f.Secrets(); f.Now = f.Now.AddSeconds(1); f.RegainFocus();
                Assert.That(f.Input("LoginId").text, Is.EqualTo(Login));
                Assert.That(f.Input("RecoveryEmail").text, Is.EqualTo(Email));
                Assert.That(f.Input("VerificationCode").text, Is.EqualTo(Code));
                Assert.That(f.Input("Password").text, Is.EqualTo(Secret));
            }
        }

        [Test]
        public async Task ServerCountdownUpdatesWithoutErasingTypedCodeAndPassword()
        {
            using (var f = new Fixture())
            {
                await f.Start(); Assert.That(f.Status, Does.Contain("120"));
                f.Secrets(); f.Now = f.Now.AddSeconds(119); f.RegainFocus();
                Assert.That(f.Get("RetryWaitSeconds"), Is.EqualTo(1));
                Assert.That(f.Status, Does.Contain("1")); Assert.That(f.Status, Does.Not.Contain("120"));
                Assert.That(f.Input("VerificationCode").text, Is.EqualTo(Code));
                Assert.That(f.Input("Password").text, Is.EqualTo(Secret));
                Assert.That(f.Input("LoginId").text, Is.EqualTo(Login));
                Assert.That(f.Input("RecoveryEmail").text, Is.EqualTo(Email));
                Assert.That(f.Button("SendCode").interactable, Is.False);
                f.Now = f.Now.AddSeconds(1); f.RegainFocus();
                Assert.That(f.Button("SendCode").interactable, Is.True);
                f.Click("SendCode"); await f.Idle(); Assert.That(f.Wire.Requests.Count, Is.EqualTo(2));
            }
        }

        [TestCase("raw_fixture_error<b>fixture_password</b>")]
        [TestCase("recovery_email_resend_limited")]
        public async Task ServerErrorsUseSafeKoreanCopyWithoutRawPayloads(string error)
        {
            using (var f = new Fixture())
            {
                f.Wire.ReplyOverride = _ => Task.FromResult(RunApiHttpResponse.Success(429,
                    new JObject { ["accepted"] = false, ["errorCode"] = error,
                        ["message"] = Secret + Email + Code, ["retryAfterSeconds"] = 120 }.ToString()));
                await f.Start(); SafeKorean(f.Status); Assert.That(f.Status, Does.Not.Contain(error));
                Assert.That(f.Status, Does.Contain("120"));
                f.Now = f.Now.AddSeconds(120); f.RegainFocus();
                SafeKorean(f.Status); Assert.That(f.Status, Does.Not.Contain(error));
                Assert.That(f.Root.GetComponentsInChildren<TMP_Text>(true).All(t => !t.richText), Is.True);
            }
        }

        [TestCase("SendCode")]
        [TestCase("ConfirmCode")]
        [TestCase("CompleteCredential")]
        [TestCase("ReconcileCredential")]
        public async Task AsyncButtonContainsThrownFlowCallbackWithoutLoggingRawError(string action)
        {
            using (var f = new Fixture())
            {
                if (action == "ConfirmCode") await f.Start();
                if (action == "CompleteCredential" || action == "ReconcileCredential") await f.Verify();
                if (action == "ReconcileCredential")
                {
                    f.Wire.ReplyOverride = _ => Task.FromResult(RunApiHttpResponse.NetworkError("raw_fixture_error"));
                    f.Input("Password").text = Secret; f.Input("ConfirmPassword").text = Secret; foreach(var toggle in f.Root.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true; f.Click("CompleteCredential"); await f.Idle();
                    Assert.That(f.Stage, Is.EqualTo("Uncertain"));
                }
                var logs = new List<string>();
                Application.LogCallback capture = (message, stack, kind) => logs.Add(message + stack);
                Application.logMessageReceived += capture;
                try
                {
                    f.Flow.GetType().GetEvent("Changed").AddEventHandler(f.Flow,
                        (Action)(() => { throw new InvalidOperationException("raw_fixture_error " + Secret + Email + Code); }));
                    f.Identity(); f.Secrets(); f.Click(action);
                    for (var i = 0; i < 3; i++) await Task.Yield();
                    Cleared(f);
                    Assert.That(logs.Any(x => x.Contains("raw_fixture_error") || x.Contains(Secret) || x.Contains(Email) || x.Contains(Code)), Is.False);
                }
                finally { Application.logMessageReceived -= capture; }
            }
        }

        [Test]
        public async Task MismatchedPasswordConfirmationCannotDispatchCredentialWrite()
        {
            using (var f = new Fixture())
            {
                await f.Verify();
                f.Input("Password").text = Secret;
                f.Input("ConfirmPassword").text = "different_fixture";
                foreach(var toggle in f.Root.GetComponentsInChildren<Toggle>(true)) toggle.isOn=true;
                var before=f.Wire.Requests.Count;
                f.Button("CompleteCredential").onClick.Invoke(); await f.Idle();
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(before));
                Assert.That(f.Status, Does.Contain("일치"));
                f.PanelCall("Hide");
                Assert.That(f.Input("ConfirmPassword").text, Is.Empty);
            }
        }

        [Test]
        public async Task MissingConsentCannotDispatchRegistration()
        {
            using (var f = new Fixture())
            {
                await f.Verify();
                f.Input("Password").text=Secret; f.Input("ConfirmPassword").text=Secret;
                var toggles=f.Root.GetComponentsInChildren<Toggle>(true);foreach(var toggle in toggles)toggle.isOn=false;
                Assert.That(toggles.Length, Is.EqualTo(2));
                var before=f.Wire.Requests.Count;
                f.Button("CompleteCredential").onClick.Invoke(); await f.Idle();
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(before));
            }
        }

        [Test]
        public async Task ExpiredVerifiedProofOffersResendWithoutRestartingForm()
        {
            using (var f = new Fixture())
            {
                await f.Verify(); f.Now=f.Now.AddMinutes(16); f.RegainFocus();
                Assert.That(f.Get("CanComplete"), Is.False);
                f.Click("SendCode"); await f.Idle();
                Assert.That(f.Wire.Requests.Count(r=>r.EndpointPath.EndsWith("/verify/start")), Is.EqualTo(2));
            }
        }
        [TestCase("Register", 1920, 1080)]
        [TestCase("Register", 2520, 1080)]
        [TestCase("Register", 1440, 1080)]
        [TestCase("Link", 1920, 1080)]
        [TestCase("Link", 2520, 1080)]
        [TestCase("Link", 1440, 1080)]
        [TestCase("Reset", 1920, 1080)]
        [TestCase("Reset", 2520, 1080)]
        [TestCase("Reset", 1440, 1080)]
        public void LandscapePanelRendersAndCancelRemainsReachableWithinViewport(string mode, int width, int height)
        {
            using (var f = new Fixture(mode))
            {
                var canvas = f.Root.GetComponent<Canvas>();
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.sortingOrder, Is.EqualTo(75));
                Assert.That(f.Root.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                var scroll = f.Root.GetComponentInChildren<ScrollRect>();
                Assert.That(scroll, Is.Not.Null); Assert.That(scroll.vertical, Is.True); Assert.That(scroll.horizontal, Is.False);
                Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
                f.RegainFocus();
                if (Screen.width > 0 && Screen.height > 0)
                {
                    var safe = (RectTransform)scroll.transform;
                    Assert.That(safe.anchorMin.x, Is.EqualTo(Screen.safeArea.xMin / Screen.width).Within(0.001f));
                    Assert.That(safe.anchorMax.x, Is.EqualTo(Screen.safeArea.xMax / Screen.width).Within(0.001f));
                    Assert.That(safe.anchorMin.y, Is.EqualTo(Screen.safeArea.yMin / Screen.height).Within(0.001f));
                    Assert.That(safe.anchorMax.y, Is.EqualTo(Screen.safeArea.yMax / Screen.height).Within(0.001f));
                }
                var cameraObject = new GameObject("CredentialCamera", typeof(Camera));
                var target = new RenderTexture(width, height, 24);
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    var camera = cameraObject.GetComponent<Camera>();
                    var canvasWidth = 1920f * Mathf.Sqrt((width / (float)height) / (16f / 9));
                    var canvasHeight = canvasWidth * height / width;
                    camera.orthographic = true; camera.orthographicSize = canvasHeight / 2; camera.aspect = width / (float)height;
                    camera.transform.position = new Vector3(0, 0, -10);
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.magenta;
                    f.Root.GetComponent<CanvasScaler>().enabled = false;
                    canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                    var rootRect = f.Root.GetComponent<RectTransform>();
                    rootRect.sizeDelta = new Vector2(canvasWidth, canvasHeight); rootRect.position = Vector3.zero;
                    Canvas.ForceUpdateCanvases(); f.PanelCall("Show"); Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                    foreach (var text in f.Root.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name);
                        var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-canvasWidth / 2 - 1), text.name);
                        Assert.That(corners[2].x, Is.LessThanOrEqualTo(canvasWidth / 2 + 1), text.name);
                    }
                    scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                    var cancelCorners = new Vector3[4];
                    f.Button("CancelCredential").GetComponent<RectTransform>().GetWorldCorners(cancelCorners);
                    var viewportCorners = new Vector3[4]; scroll.viewport.GetWorldCorners(viewportCorners);
                    Assert.That(cancelCorners[0].y, Is.GreaterThanOrEqualTo(viewportCorners[0].y - 1), "Cancel must remain reachable above the viewport bottom.");
                    Assert.That(cancelCorners[2].y, Is.LessThanOrEqualTo(canvasHeight / 2 + 1));
                    camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                    var pixels = texture.GetPixels32();
                    Assert.That(pixels.Count(p => p.r != pixels[0].r || p.g != pixels[0].g || p.b != pixels[0].b), Is.GreaterThan(pixels.Length / 20), "Panel must render visible UI.");
                }
                finally
                {
                    RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                    target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            }
        }
    }
}
