using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Submissions;
using LastRail.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class SupportReceiptRecoveryPanelTests
    {
        private const string Account = "account_recovery_fixture";
        private static readonly DateTimeOffset Origin = DateTimeOffset.Parse("2026-09-06T00:00:00.000Z");
        private sealed class Journal : IPendingReceiptJournal
        {
            public readonly List<PendingReceipt> Rows = new List<PendingReceipt>();
            public readonly List<string> Removed = new List<string>();
            public int Reads;
            public bool FailRead, FailRemove;
            public TaskCompletionSource<IReadOnlyList<PendingReceipt>> PendingRead;
            public Task<bool> PutAsync(PendingReceipt record) { Rows.Add(record); return Task.FromResult(true); }
            public Task<IReadOnlyList<PendingReceipt>> ReadAsync(string account)
            {
                Reads++;
                if (PendingRead != null) return PendingRead.Task;
                if (FailRead) return Task.FromException<IReadOnlyList<PendingReceipt>>(new InvalidOperationException("synthetic unreadable metadata"));
                return Task.FromResult<IReadOnlyList<PendingReceipt>>(Rows.Where(r => r.AccountId == account).ToArray());
            }
            public Task<bool> RemoveAsync(string account, PendingReceiptKind kind, string key)
            {
                Removed.Add(account + ":" + kind + ":" + key);
                if (FailRemove) return Task.FromResult(false);
                Rows.RemoveAll(r => r.AccountId == account && r.Kind == kind && r.IdempotencyKey == key);
                return Task.FromResult(true);
            }
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public TaskCompletionSource<RunApiHttpResponse> Pending;
            public RunApiHttpResponse Reply = RunApiHttpResponse.NetworkError("synthetic unavailable receipt");
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                return Pending == null ? Task.FromResult(Reply) : Pending.Task;
            }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly Journal Journal = new Journal();
            public readonly Wire Wire = new Wire();
            public GameApiAuthSessionSnapshot Session = SupportInquiryTests.Session(Account);
            public long Generation = 1;
            public DateTimeOffset Now = Origin;
            public readonly SupportInquiryHttpClient Inner;
            public readonly SupportInquiryController Controller;
            public readonly object Recovery;
            public readonly GameObject Root;
            public readonly SupportInquiryPanel Panel;
            public Fixture()
            {
                var configure = typeof(SupportInquiryPanel).GetMethods().SingleOrDefault(m => m.Name == "ConfigureRecovery");
                Assert.That(configure, Is.Not.Null, "PHASE1 RED: missing public ConfigureRecovery(SupportReceiptRecovery).");
                var recoveryType = typeof(SupportInquiryPanel).Assembly.GetType("LastRail.Support.SupportReceiptRecovery");
                Assert.That(recoveryType, Is.Not.Null);
                Assert.That(configure.GetParameters().Select(p => p.ParameterType), Is.EqualTo(new[] { recoveryType }));
                Assert.That(configure.ReturnType, Is.EqualTo(typeof(void)));
                var inner = Inner = new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(Wire), () => new AppBuildSnapshot("1.0", 1));
                var tracking = new ReceiptTrackingSupportClient(inner, Journal, () => Generation, () => Session);
                Controller = new SupportInquiryController(Session, tracking, SupportInquiryHttpClient.Categories, 2000,
                    () => new AppBuildSnapshot("1.0", 1), () => Now, 10);
                var constructor = recoveryType.GetConstructor(new[] { typeof(SupportInquiryController), typeof(SupportInquiryHttpClient),
                    typeof(IPendingReceiptJournal), typeof(Func<GameApiAuthSessionSnapshot>), typeof(Func<long>) });
                Assert.That(constructor, Is.Not.Null, "Recovery constructor must expose the agreed controller, journal and owner dependencies.");
                Recovery = constructor.Invoke(new object[] { Controller, inner, Journal,
                    (Func<GameApiAuthSessionSnapshot>)(() => Session), (Func<long>)(() => Generation) });
                Root = new GameObject("ReceiptRecoveryPanelFixture", typeof(RectTransform));
                try
                {
                    Panel = Root.AddComponent<SupportInquiryPanel>();
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                    Assert.That(font, Is.Not.Null);
                    Panel.Initialize(Controller, font, SupportInquiryHttpClient.CategoryLabel);
                    configure.Invoke(Panel, new[] { Recovery }); Panel.Show();
                }
                catch { UnityEngine.Object.DestroyImmediate(Root); throw; }
            }
            public object Get(string name) => Recovery.GetType().GetProperty(name).GetValue(Recovery);
            public Task Refresh() => (Task)Recovery.GetType().GetMethod("RefreshAsync").Invoke(Recovery, null);
            public bool Select(PendingReceipt record) => (bool)Recovery.GetType().GetMethod("Select").Invoke(Recovery, new object[] { record });
            public TMP_Dropdown Records => Root.GetComponentsInChildren<TMP_Dropdown>(true).Single(x => x.name == "PendingReceipts");
            public TMP_InputField Content => Root.GetComponentsInChildren<TMP_InputField>(true).Single(x => x.name == "Content");
            public Button Button(string name) => Root.GetComponentsInChildren<Button>(true).Single(x => x.name == name);
            public TMP_Text Text(string name) => Root.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == name);
            public void Click(string name)
            {
                var button = Button(name); Assert.That(button.gameObject.activeInHierarchy, Is.True, name);
                Assert.That(button.IsInteractable(), Is.True, name); button.onClick.Invoke();
            }
            public void Pick(string label)
            {
                Assert.That(Records.IsInteractable(), Is.True);
                var index = Records.options.FindIndex(o => o.text.Contains(label));
                Assert.That(index, Is.GreaterThanOrEqualTo(0), label);
                Records.SetValueWithoutNotify(index); Records.onValueChanged.Invoke(index);
            }
            public async Task Idle()
            {
                for (var i = 0; i < 100 && (bool)Get("Busy"); i++) await Task.Yield();
                Assert.That(Get("Busy"), Is.EqualTo(false));
            }
            public void Dispose()
            { UnityEngine.Object.DestroyImmediate(Root); (Recovery as IDisposable)?.Dispose(); }
        }
        private static PendingReceipt Record(int index, bool error = false, string account = Account) => new PendingReceipt(
            error ? PendingReceiptKind.ClientError : PendingReceiptKind.Support, account, index.ToString("D32"),
            error ? "UNITY_EXCEPTION" : index == 1 ? "ACCOUNT_LOGIN" : "BUG_OTHER", new AppBuildSnapshot("1.0", 1),
            Origin.AddMinutes(index), Origin.AddMinutes(index));
        private static void SafeLabels(Fixture f)
        {
            var labels = f.Root.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text).Concat(f.Records.options.Select(o => o.text));
            foreach (var label in labels)
            {
                Assert.That(label, Does.Not.Contain(Account)); Assert.That(label, Does.Not.Contain("account_other"));
                foreach (var record in f.Journal.Rows) Assert.That(label, Does.Not.Contain(record.IdempotencyKey));
            }
        }

        [Test]
        public async Task PendingDropdownShowsDatedKoreanMetadataWithoutKeysOrAccounts()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); f.Journal.Rows.Add(Record(2, true)); await f.Refresh();
                Assert.That(f.Records.options.Any(o => o.text.Contains("2026") && o.text.Contains("\uACC4\uC815")), Is.True);
                Assert.That(f.Records.options.Any(o => o.text.Contains("2026") && o.text.Contains("\uC608\uC678")), Is.True);
                SafeLabels(f);
                Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [Test]
        public async Task PublicDropdownSelectionRestoresSupportAndExternalSelectionStaysSynchronized()
        {
            using (var f = new Fixture())
            {
                var first = Record(1); var second = Record(2); f.Journal.Rows.Add(first); f.Journal.Rows.Add(second);
                await f.Refresh(); f.Pick("\uACC4\uC815");
                Assert.That(GetSelectedKey(f), Is.EqualTo(first.IdempotencyKey));
                Assert.That(f.Controller.Pending.IdempotencyKey, Is.EqualTo(first.IdempotencyKey));
                Assert.That(f.Controller.Pending.ReceiptOnly, Is.True); Assert.That(f.Button("Send").interactable, Is.False);
                Assert.That(f.Content.interactable, Is.False); Assert.That(f.Button("Reconcile").interactable, Is.True);
                Assert.That(f.Select(second), Is.True);
                Assert.That(f.Records.options[f.Records.value].text, Does.Contain("\uAE30\uD0C0"));
                Assert.That(f.Controller.Pending.IdempotencyKey, Is.EqualTo(second.IdempotencyKey));
                Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [Test]
        public async Task ErrorSelectionOffersLocalDeletionWithoutSupportReceiptLookup()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); f.Journal.Rows.Add(Record(2, true)); await f.Refresh();
                f.Pick("\uACC4\uC815"); f.Pick("\uC608\uC678");
                Assert.That(((PendingReceipt)f.Get("Selected")).Kind, Is.EqualTo(PendingReceiptKind.ClientError));
                Assert.That(f.Button("Reconcile").gameObject.activeInHierarchy && f.Button("Reconcile").interactable, Is.False);
                Assert.That(f.Button("ForgetReceipt").interactable, Is.True);
                Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Removed, Is.Empty);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ForgetRequiresSeparateConfirmationAndDeletesOnlySelectedAccountRecord(bool confirm)
        {
            using (var f = new Fixture())
            {
                var selected = Record(1); var foreign = Record(1, account: "account_other");
                f.Journal.Rows.Add(selected); f.Journal.Rows.Add(foreign); f.Journal.Rows.Add(Record(2, true));
                var originalSession = f.Session; await f.Refresh(); f.Pick("\uACC4\uC815"); f.Click("ForgetReceipt");
                Assert.That(f.Journal.Removed, Is.Empty);
                var warning = f.Text("ForgetWarning"); Assert.That(warning.gameObject.activeInHierarchy, Is.True);
                Assert.That(warning.text, Does.Contain("\uC11C\uBC84")); Assert.That(warning.text, Does.Contain("\uCDE8\uC18C"));
                Assert.That(warning.text, Does.Contain("\uC11C\uBC84 \uC811\uC218 \uCDE8\uC18C\uAC00 \uC544\uB2D9\uB2C8\uB2E4.")); Assert.That(warning.text, Does.Contain("\uC774 \uAE30\uAE30"));
                Assert.That(warning.text, Does.Contain("\uC870\uD68C")); Assert.That(warning.text, Does.Contain("\uC5C6"));
                f.Click(confirm ? "ConfirmForgetReceipt" : "CancelForgetReceipt"); await f.Idle();
                Assert.That(f.Journal.Removed.Count, Is.EqualTo(confirm ? 1 : 0));
                if (confirm) Assert.That(f.Journal.Removed.Single(), Is.EqualTo(Account + ":Support:" + selected.IdempotencyKey));
                Assert.That(f.Journal.Rows.Contains(foreign), Is.True); Assert.That(f.Session, Is.SameAs(originalSession));
                Assert.That(f.Wire.Requests, Is.Empty);
                Assert.That(f.Button("ConfirmForgetReceipt").gameObject.activeInHierarchy, Is.False);
            }
        }

        [Test]
        public async Task ChangingSelectedRecordInvalidatesEarlierDeleteConfirmation()
        {
            using (var f = new Fixture())
            {
                var second = Record(2); f.Journal.Rows.Add(Record(1)); f.Journal.Rows.Add(second);
                await f.Refresh(); f.Pick("\uACC4\uC815"); f.Click("ForgetReceipt");
                Assert.That(f.Select(second), Is.True);
                Assert.That(f.Button("ConfirmForgetReceipt").gameObject.activeInHierarchy, Is.False);
                f.Button("ConfirmForgetReceipt").onClick.Invoke(); await f.Idle();
                Assert.That(f.Journal.Removed, Is.Empty, "A stale confirmation must never delete the newly selected record.");
            }
        }

        [Test]
        public async Task ClosingAndReopeningDoesNotPreserveDeleteConfirmation()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh(); f.Pick("\uACC4\uC815"); f.Click("ForgetReceipt");
                f.Click("Close"); f.Panel.Show();
                Assert.That(f.Button("ConfirmForgetReceipt").gameObject.activeInHierarchy, Is.False);
                Assert.That(f.Journal.Removed, Is.Empty);
            }
        }

        [Test]
        public async Task ReconfigureAfterDisposedModelReusesControlsAndClearsDeleteConfirmation()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh(); f.Pick("\uACC4\uC815"); f.Click("ForgetReceipt");
                (f.Recovery as IDisposable)?.Dispose();
                var replacement = Activator.CreateInstance(f.Recovery.GetType(), f.Controller, f.Inner, f.Journal,
                    (Func<GameApiAuthSessionSnapshot>)(() => f.Session), (Func<long>)(() => f.Generation));
                try
                {
                    typeof(SupportInquiryPanel).GetMethod("ConfigureRecovery").Invoke(f.Panel, new[] { replacement });
                    Assert.That(f.Button("ConfirmForgetReceipt").gameObject.activeInHierarchy, Is.False);
                    f.Journal.Rows.Add(Record(2));
                    await (Task)replacement.GetType().GetMethod("RefreshAsync").Invoke(replacement, null);
                    f.Pick("\uAE30\uD0C0");
                    Assert.That(((PendingReceipt)replacement.GetType().GetProperty("Selected").GetValue(replacement)).IdempotencyKey, Is.EqualTo(Record(2).IdempotencyKey));
                    Assert.That(f.Root.GetComponentsInChildren<TMP_Dropdown>(true).Count(d => d.name == "PendingReceipts"), Is.EqualTo(1));
                    foreach (var name in new[] { "RefreshReceipts", "StartIndependentInquiry", "ForgetReceipt", "ConfirmForgetReceipt", "CancelForgetReceipt" })
                        Assert.That(f.Root.GetComponentsInChildren<Button>(true).Count(b => b.name == name), Is.EqualTo(1), name);
                    Assert.That(f.Journal.Removed, Is.Empty);
                }
                finally { (replacement as IDisposable)?.Dispose(); }
            }
        }

        [Test]
        public async Task IndependentInquiryButtonCreatesEmptyDraftWithoutDroppingHeldMetadata()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh(); f.Pick("\uACC4\uC815"); f.Click("StartIndependentInquiry");
                Assert.That(f.Controller.Pending, Is.Null); Assert.That(f.Content.text, Is.Empty);
                Assert.That(f.Content.interactable, Is.True); Assert.That(f.Get("Selected"), Is.Null);
                Assert.That(f.Journal.Rows.Count, Is.EqualTo(1)); Assert.That(f.Journal.Removed, Is.Empty); Assert.That(f.Wire.Requests, Is.Empty);
                Assert.That(f.Button("Send").interactable, Is.False);
            }
        }

        [Test]
        public async Task RefreshButtonUpdatesEmptyAndCapacityNoticesWithoutLeakingIdentifiers()
        {
            using (var f = new Fixture())
            {
                await f.Refresh(); Assert.That(f.Records.interactable, Is.False);
                Assert.That(f.Text("RecoveryStatus").text, Does.Contain("\uC5C6"));
                Assert.That(f.Button("ForgetReceipt").interactable, Is.False);
                for (var i = 1; i <= 64; i++) f.Journal.Rows.Add(Record(i));
                var reads = f.Journal.Reads; f.Click("RefreshReceipts"); await f.Idle();
                Assert.That(f.Journal.Reads, Is.GreaterThan(reads));
                Assert.That(f.Text("RecoveryStatus").text, Does.Contain("64")); SafeLabels(f);
                Assert.That(f.Journal.Rows.Count, Is.EqualTo(64)); Assert.That(f.Journal.Removed, Is.Empty);
            }
        }

        [Test]
        public async Task ReadFailureLeavesRetryAndCloseWithoutDestructiveReset()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); f.Journal.FailRead = true; await f.Refresh();
                Assert.That(f.Get("StorageFailed"), Is.EqualTo(true));
                Assert.That(string.IsNullOrWhiteSpace(f.Text("RecoveryStatus").text), Is.False);
                Assert.That(f.Button("ForgetReceipt").interactable, Is.False);
                Assert.That(f.Button("Close").interactable, Is.True); Assert.That(f.Journal.Removed, Is.Empty);
                f.Journal.FailRead = false; f.Click("RefreshReceipts"); await f.Idle();
                Assert.That(f.Get("StorageFailed"), Is.EqualTo(false)); Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task FailedDeletionKeepsHeldRecordAndShowsFailureWithoutAffectingAuthentication()
        {
            using (var f = new Fixture())
            {
                var record = Record(1); f.Journal.Rows.Add(record); await f.Refresh(); f.Pick("\uACC4\uC815");
                var originalSession = f.Session; f.Journal.FailRemove = true;
                f.Click("ForgetReceipt"); f.Click("ConfirmForgetReceipt"); await f.Idle();
                Assert.That(f.Journal.Rows.Contains(record), Is.True); Assert.That(f.Session, Is.SameAs(originalSession));
                Assert.That(string.IsNullOrWhiteSpace(f.Text("RecoveryStatus").text), Is.False);
                Assert.That(f.Button("RefreshReceipts").interactable, Is.True);
                Assert.That(f.Button("Close").interactable, Is.True); Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [Test]
        public async Task RefreshBusyBlocksSelectionAndMutationButLeavesCloseAvailable()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh(); f.Pick("\uACC4\uC815");
                var pending = new TaskCompletionSource<IReadOnlyList<PendingReceipt>>(); f.Journal.PendingRead = pending;
                f.Click("RefreshReceipts");
                try
                {
                    Assert.That(f.Get("Busy"), Is.EqualTo(true)); Assert.That(f.Records.interactable, Is.False);
                    foreach (var name in new[] { "RefreshReceipts", "ForgetReceipt", "StartIndependentInquiry" })
                        Assert.That(f.Button(name).interactable, Is.False, name);
                    Assert.That(f.Button("Close").interactable, Is.True);
                }
                finally { pending.TrySetResult(f.Journal.Rows.ToArray()); }
                await f.Idle();
            }
        }

        [Test]
        public async Task ActiveSupportSendBlocksRecoverySelectionAndIndependentDraft()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh();
                if (f.Get("Selected") != null) f.Click("StartIndependentInquiry");
                f.Content.text = "synthetic independent inquiry content";
                var pending = new TaskCompletionSource<RunApiHttpResponse>(); f.Wire.Pending = pending;
                f.Click("Send");
                try
                {
                    Assert.That(f.Controller.State, Is.EqualTo(InquiryState.Sending));
                    Assert.That(f.Records.interactable, Is.False);
                    Assert.That(f.Button("StartIndependentInquiry").interactable, Is.False);
                    Assert.That(f.Button("ForgetReceipt").interactable, Is.False);
                }
                finally { pending.TrySetResult(RunApiHttpResponse.NetworkError("synthetic lost response")); }
                for (var i = 0; i < 100 && f.Controller.State == InquiryState.Sending; i++) await Task.Yield();
                Assert.That(f.Controller.State, Is.EqualTo(InquiryState.Uncertain));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CapacityCleanupThroughPublicButtonsKeepsIndependentDraft(bool error)
        {
            using (var f = new Fixture())
            {
                var selected = Record(1, error); f.Journal.Rows.Add(selected);
                for (var i = 2; i <= 64; i++) f.Journal.Rows.Add(Record(i));
                await f.Refresh(); f.Click("StartIndependentInquiry");
                f.Content.text = "synthetic independent draft to preserve";
                Assert.That(f.Button("Send").interactable, Is.False, "Full metadata capacity must block a new send before dispatch.");
                f.Pick(error ? "\uC608\uC678" : "\uACC4\uC815");
                Assert.That(GetSelectedKey(f), Is.EqualTo(selected.IdempotencyKey));
                Assert.That(f.Get("ManagingDraft"), Is.EqualTo(true)); Assert.That(f.Content.interactable, Is.True);
                Assert.That(f.Controller.Pending, Is.Null);
                Assert.That(f.Button("Reconcile").gameObject.activeInHierarchy && f.Button("Reconcile").interactable, Is.False);
                f.Content.text += " edited";
                f.Click("ForgetReceipt"); f.Click("ConfirmForgetReceipt"); await f.Idle();
                Assert.That(f.Journal.Rows.Count, Is.EqualTo(63)); Assert.That(f.Journal.Rows.Contains(selected), Is.False);
                Assert.That(f.Content.text, Is.EqualTo("synthetic independent draft to preserve edited"));
                Assert.That(f.Controller.Content, Is.EqualTo(f.Content.text)); Assert.That(f.Controller.Pending, Is.Null);
                Assert.That(f.Button("Send").interactable, Is.True); Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task PublicManagementDeselectPreservesDraftAndInvalidatesDeleteWarning(bool independentButton)
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh(); f.Click("StartIndependentInquiry");
                f.Content.text = "synthetic draft must survive deselection"; f.Pick("\uACC4\uC815");
                Assert.That(f.Get("ManagingDraft"), Is.EqualTo(true)); f.Click("ForgetReceipt");
                if (independentButton) f.Click("StartIndependentInquiry");
                else { f.Records.SetValueWithoutNotify(0); f.Records.onValueChanged.Invoke(0); }
                Assert.That(f.Get("Selected"), Is.Null); Assert.That(f.Records.value, Is.Zero);
                Assert.That(f.Content.text, Is.EqualTo("synthetic draft must survive deselection"));
                Assert.That(f.Button("ConfirmForgetReceipt").gameObject.activeInHierarchy, Is.False);
                Assert.That(f.Journal.Removed, Is.Empty); Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        [Test]
        public async Task PublicRecordSwitchPreservesCooldownAndBlocksIndependentSend()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); await f.Refresh(); f.Click("StartIndependentInquiry");
                f.Content.text = "synthetic rate limited inquiry";
                f.Wire.Reply = RunApiHttpResponse.Success(429,
                    "{\"accepted\":false,\"ticketStatus\":\"REJECTED\",\"errorCode\":\"support_ticket_rate_limited\",\"retryAfterSeconds\":120}");
                f.Click("Send"); await f.Idle(); Assert.That(f.Controller.RetryWaitSeconds, Is.EqualTo(120));
                f.Pick("\uACC4\uC815"); Assert.That(f.Controller.RetryWaitSeconds, Is.EqualTo(120));
                f.Click("StartIndependentInquiry"); f.Content.text = "another synthetic independent inquiry";
                Assert.That(f.Button("Send").interactable, Is.False);
                Assert.That(f.Text("Status").text, Does.Contain("120"));
                f.Button("Send").onClick.Invoke(); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task RecoveryAndDeleteWarningRenderAt360By640WithoutOverflow(bool confirmation)
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Record(1)); f.Journal.Rows.Add(Record(2, true)); await f.Refresh(); f.Pick("\uACC4\uC815");
                var cameraObject = new GameObject("ReceiptRecoveryCamera", typeof(Camera));
                var target = new RenderTexture(360, 640, 24);
                var texture = new Texture2D(360, 640, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true;
                    camera.orthographicSize = 960; camera.aspect = 360f / 640; camera.transform.position = new Vector3(0, 0, -10);
                    f.Root.GetComponent<CanvasScaler>().enabled = false;
                    var canvas = f.Root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                    var rootRect = (RectTransform)f.Root.transform; rootRect.sizeDelta = new Vector2(1080, 1920); rootRect.position = Vector3.zero;
                    Canvas.ForceUpdateCanvases(); f.Panel.Show();
                    if (confirmation) f.Click("ForgetReceipt");
                    Canvas.ForceUpdateCanvases();
                    var scroll = f.Root.GetComponentInChildren<ScrollRect>(); Assert.That(scroll, Is.Not.Null);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content); Canvas.ForceUpdateCanvases();
                    foreach (var text in f.Root.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name);
                        var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-541), text.name);
                        Assert.That(corners[2].x, Is.LessThanOrEqualTo(541), text.name);
                    }
                    scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                    var close = new Vector3[4]; f.Button("Close").GetComponent<RectTransform>().GetWorldCorners(close);
                    Assert.That(close[0].y, Is.GreaterThanOrEqualTo(-961)); Assert.That(close[2].y, Is.LessThanOrEqualTo(961));
                    camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                    texture.ReadPixels(new Rect(0, 0, 360, 640), 0, 0); texture.Apply();
                    var pixels = texture.GetPixels32();
                    Assert.That(pixels.Count(p => p.r != pixels[0].r || p.g != pixels[0].g), Is.GreaterThan(pixels.Length / 20));
                }
                finally
                {
                    RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                    target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            }
        }
        private static string GetSelectedKey(Fixture f) => ((PendingReceipt)f.Get("Selected"))?.IdempotencyKey;
    }
}
