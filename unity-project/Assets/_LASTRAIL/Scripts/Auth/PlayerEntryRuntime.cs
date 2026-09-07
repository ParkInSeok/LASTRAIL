using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Narrative;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using LastRail.Resources;
using LastRail.Run;
using LastRail.Support;
using LastRail.Submissions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Auth
{
    public sealed class PlayerEntryRuntime : IDisposable
    {
        private readonly ResourceBootstrapController bootstrap;
        private readonly GameApiPlayerEntryClient client;
        private readonly IGameApiAuthTokenReader reader;
        private readonly KeystoreAuthReplayJournal replayJournal;
        private readonly GameApiRefreshAuthHttpClient refreshClient;
        private readonly PlayerEntryPanel panel;
        private readonly PasswordCredentialRuntime credentials;
        private readonly TMP_FontAsset font;
        private readonly SupportInquiryHttpClient supportClient;
        private readonly ISupportInquiryClient trackedSupport;
        private readonly IPendingReceiptJournal receiptJournal;
        private readonly Func<AppBuildSnapshot> captureBuild;
        private string supportAccount;
        private long supportGeneration;
        private GameApiAuthSessionSnapshot reconciledErrorSession;
        private GameApiAuthSessionSnapshot restoredRunSession;
        private SupportInquiryController support;
        private SupportInquiryPanel supportPanel;
        private GameObject boundLobby;
        private GameObject retainedLobby;
        private bool readRequiresRefresh, hydrationRefreshAttempted;
        private TaskCompletionSource<bool> nicknameGate;
        private bool disposed, submittingName, entering, replayBlocked;
        private bool recheckAfterCancellation;
        private AuthReplayKind? replayIntent;
        private bool HasReplayRecovery => replayJournal != null && (replayJournal.Pending != null || replayIntent.HasValue);
        private bool CanRecoverReplay => HasReplayRecovery && !replayBlocked;
        private long uiGeneration;
        private string preparationError;
        public PlayerAuthOwner Owner { get; }
        public CurrentSessionErrorRuntime Diagnostics { get; }
        public DurableRunClient Runs { get; }
        public SupportReceiptRecovery ReceiptRecovery { get; private set; }
        public PlayerEntryProfile Profile { get; private set; }
        public PlayerLobbySnapshot LobbySnapshot { get; private set; }
        // A confirmed account-data binder must be installed; absence is a visible, closed runtime gate.
        public Func<GameApiAuthSessionSnapshot, Task<bool>> LobbyPreparation { get; set; }

        public PlayerEntryRuntime(ResourceBootstrapController bootstrap, TMP_FontAsset font, string configUrl)
            : this(bootstrap, font, CreateWire(configUrl),
                new KeystoreAuthReplayJournal(new AndroidKeystoreNativeStoragePort())) { }

        private PlayerEntryRuntime(ResourceBootstrapController bootstrap, TMP_FontAsset font, IRunApiHttpTransport wire,
            KeystoreAuthReplayJournal store) : this(bootstrap, font, wire, store, store, AppBuildSnapshot.CaptureInstalled,
                new EncryptedPendingReceiptJournal(new AndroidEncryptedReceiptFilePort(new AndroidKeystoreNativeStoragePort(),
                    Path.Combine(Application.persistentDataPath, "receipts", "pending-v1.enc"))),
                new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(new AndroidKeystoreNativeStoragePort(),
                    Path.Combine(Application.persistentDataPath, "runs", "operations-v1.enc")))) { }

        public PlayerEntryRuntime(ResourceBootstrapController bootstrap, TMP_FontAsset font, IRunApiHttpTransport wire,
            IGameApiAuthTokenStore store, IGameApiAuthTokenReader reader)
            : this(bootstrap, font, wire, store, reader, AppBuildSnapshot.CaptureInstalled) { }

        public PlayerEntryRuntime(ResourceBootstrapController bootstrap, TMP_FontAsset font, IRunApiHttpTransport wire,
            IGameApiAuthTokenStore store, IGameApiAuthTokenReader reader, Func<AppBuildSnapshot> capture)
            : this(bootstrap, font, wire, store, reader, capture, null) { }

        public PlayerEntryRuntime(ResourceBootstrapController bootstrap, TMP_FontAsset font, IRunApiHttpTransport wire,
            IGameApiAuthTokenStore store, IGameApiAuthTokenReader reader, Func<AppBuildSnapshot> capture, IPendingReceiptJournal receiptJournal)
            : this(bootstrap, font, wire, store, reader, capture, receiptJournal, null) { }

        public PlayerEntryRuntime(ResourceBootstrapController bootstrap, TMP_FontAsset font, IRunApiHttpTransport wire,
            IGameApiAuthTokenStore store, IGameApiAuthTokenReader reader, Func<AppBuildSnapshot> capture,
            IPendingReceiptJournal receiptJournal, IRunOperationJournal runJournal)
        {
            this.bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.font = font;
            this.receiptJournal = receiptJournal;
            captureBuild = capture ?? AppBuildSnapshot.CaptureInstalled;
            replayJournal = store as KeystoreAuthReplayJournal;
            client = new GameApiPlayerEntryClient(wire);
            supportClient = new SupportInquiryHttpClient(new GameApiAuthenticatedHttpTransport(wire), capture);
            refreshClient = new GameApiRefreshAuthHttpClient(wire);
            LobbyPreparation = PrepareLobbyDataAsync;
            Owner = new PlayerAuthOwner(reader, store, RefreshSessionAsync, PrepareAsync);
            if (runJournal != null)
                Runs = new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire), runJournal,
                    () => Owner.Session, () => Owner.Generation, captureBuild);
            trackedSupport = receiptJournal == null ? (ISupportInquiryClient)supportClient
                : new ReceiptTrackingSupportClient(supportClient, receiptJournal, () => Owner.Generation, () => Owner.Session);
            Diagnostics = new CurrentSessionErrorRuntime(new CurrentSessionErrorClient(new GameApiAuthenticatedHttpTransport(wire),
                () => Owner.Session, () => Owner.Generation, capture, null, receiptJournal));
            var root = new GameObject("PlayerEntry", typeof(RectTransform)); root.transform.SetParent(bootstrap.transform, false);
            panel = root.AddComponent<PlayerEntryPanel>(); panel.Initialize(font); panel.Hide();
            panel.LoginSubmitted += Login; panel.GuestRequested += Guest;
            panel.CancelRequested += Cancel; panel.RetryRequested += Retry;
            panel.NicknameSubmitted += SubmitNickname;
            credentials = new PasswordCredentialRuntime(new PasswordEmailClient(wire), Owner, reader, replayJournal,
                bootstrap.transform, font, panel.Hide, ShowLogin);
            panel.RegisterRequested += () => credentials.Open("Register");
            panel.ResetPasswordRequested += () => credentials.Open("Reset");
            Owner.Changed += OwnerChanged;
            bootstrap.LoginRequested += ShowLogin; bootstrap.GuestLoginRequested += Guest;
            bootstrap.SessionRestoreRequested += Restore;
        }

        private static IRunApiHttpTransport CreateWire(string configUrl)
        {
            if (!Uri.TryCreate(configUrl, UriKind.Absolute, out var uri)) return new UnconfiguredTransport();
            return new InstalledAdmissionTransport(
                new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(uri.GetLeftPart(UriPartial.Authority), 30)),
                AppBuildSnapshot.CaptureInstalled(), Application.platform == RuntimePlatform.Android ? "android" : null, null);
        }
        private sealed class UnconfiguredTransport : IRunApiHttpTransport
        {
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) =>
                Task.FromResult(RunApiHttpResponse.NetworkError("Client environment is unconfigured."));
        }

        private async void Restore()
        {
            if (disposed || Owner.Busy) return;
            hydrationRefreshAttempted = false;
            var generation = uiGeneration;
            if (replayJournal != null)
            {
                await replayJournal.ReadAsync();
                if (disposed || generation != uiGeneration) return;
                if (replayJournal.Pending != null) { await ResumeReplayAsync(); return; }
            }
            await Owner.RestoreAsync();
        }

        private async Task<GameApiAuthSessionSnapshot> RefreshSessionAsync(GameApiAuthSessionSnapshot session)
        {
            var generation = uiGeneration;
            var request = GameApiRefreshAuthRequest.Create(session.RefreshToken);
            if (replayJournal != null)
            {
                replayIntent = AuthReplayKind.Refresh;
                var pending = await replayJournal.BeginRefreshAsync(session);
                if (pending == null || disposed || generation != uiGeneration) return null;
                request = GameApiRefreshAuthRequest.CreateReplay(session.RefreshToken, pending.IdempotencyKey);
            }
            var result = await refreshClient.RefreshAsync(request);
            if (disposed || generation != uiGeneration) return null;
            replayBlocked = TerminalReplay(result.ErrorCode);
            return result.Outcome == GameApiRefreshAuthClientOutcome.Refreshed
                ? GameApiAuthSessionSnapshot.FromGuestResponse(result.Response) : null;
        }

        private async Task ResumeReplayAsync()
        {
            if (disposed || Owner.Busy || replayJournal == null || replayBlocked) return;
            var generation = ++uiGeneration;
            await Owner.LoginAsync(async () =>
            {
                var stored = await replayJournal.ReadAsync();
                if (disposed || generation != uiGeneration) return null;
                var pending = replayJournal.Pending;
                var kind = pending?.Kind ?? replayIntent;
                replayIntent = kind;
                if (pending == null && stored.Session != null && kind != AuthReplayKind.Refresh) return stored.Session;
                if (kind == AuthReplayKind.Refresh)
                    return stored.Session == null ? null : await RefreshSessionAsync(stored.Session);
                if (kind != AuthReplayKind.Guest || stored.Outcome != GameApiAuthTokenReadOutcome.Missing) return null;
                var durable = await replayJournal.BeginGuestAsync();
                if (durable == null || disposed || generation != uiGeneration) return null;
                var candidate = await client.GuestReplayAsync(durable.IdempotencyKey);
                if (disposed || generation != uiGeneration) return null;
                replayBlocked = TerminalReplay(client.ReplayFailureCode);
                return candidate;
            });
        }
        private static bool TerminalReplay(string code) => code == "auth_replay_expired" || code == "account_not_active";
        private void ShowLogin()
        {
            if (disposed) return;
            if (Owner.Busy || credentials?.Busy == true) { ShowPendingCancellation(); return; }
            if (HasReplayRecovery) panel.ShowRecoveryLogin(CanRecoverReplay);
            else if (Owner.CanResumeStoredSession) panel.ShowStoredSessionLogin();
            else panel.ShowLogin(Owner.CanCreateGuest, false);
        }
        private void ShowPendingCancellation() => panel.ShowBusy("\uC774\uC804 \uC694\uCCAD \uC885\uB8CC \uD655\uC778 \uC911");
        private async void Login(string id, string password)
        {
            if (disposed) return;
            if (Owner.Busy) { ShowPendingCancellation(); return; }
            hydrationRefreshAttempted = false;
            var generation = ++uiGeneration;
            await Owner.LoginAsync(async () =>
            {
                var stored = await reader.ReadAsync();
                if (disposed || generation != uiGeneration) return null;
                if (stored == null || (stored.Outcome != GameApiAuthTokenReadOutcome.Loaded && stored.Outcome != GameApiAuthTokenReadOutcome.Missing)) return null;
                if (replayJournal?.Pending != null && !(replayBlocked && stored.Session != null)) return null;
                var candidate = await client.PasswordAsync(id, password);
                // Different-account switching requires its own explicit proof/confirmation flow.
                return stored.Session != null && candidate?.AccountId != stored.Session.AccountId ? null : candidate;
            });
        }
        private async void Guest()
        {
            if (disposed) return;
            if (Owner.Busy) { ShowPendingCancellation(); return; }
            hydrationRefreshAttempted = false;
            if (!Owner.CanCreateGuest) { panel.ShowError("\uAE30\uC874 \uB85C\uADF8\uC778 \uC815\uBCF4 \uD655\uC778\uC774 \uD544\uC694\uD569\uB2C8\uB2E4.", false); return; }
            var generation = ++uiGeneration;
            if (replayJournal != null) replayIntent = AuthReplayKind.Guest;
            await Owner.GuestAsync(async () =>
            {
                var stored = await reader.ReadAsync();
                if (disposed || generation != uiGeneration) return null;
                if (stored?.Outcome != GameApiAuthTokenReadOutcome.Missing) return null;
                if (replayJournal == null) return await client.GuestAsync();
                var pending = await replayJournal.BeginGuestAsync();
                if (pending == null || disposed || generation != uiGeneration) return null;
                var candidate = await client.GuestReplayAsync(pending.IdempotencyKey);
                if (disposed || generation != uiGeneration) return null;
                replayBlocked = TerminalReplay(client.ReplayFailureCode);
                return candidate;
            });
        }

        private async Task<bool> PrepareAsync(GameApiAuthSessionSnapshot session)
        {
            var generation = uiGeneration;
            var errorGeneration = Owner.Generation;
            preparationError = null;
            LobbySnapshot = null; readRequiresRefresh = false;
            var read = await client.ReadProfileAsync(session);
            if (!Current(generation, session)) return false;
            readRequiresRefresh = read.RequiresAuthentication;
            var profile = read.Value;
            if (profile == null)
            {
                if (!read.RequiresAuthentication) ReportPreparationFailure(errorGeneration);
                return false;
            }
            Profile = profile;
            if (profile.DisplayName == null)
            {
                var gate = new TaskCompletionSource<bool>(); nicknameGate = gate;
                panel.ShowNickname();
                if (!await gate.Task || !Current(generation, session)) return false;
            }
            if (LobbyPreparation == null)
            {
                preparationError = "\uB85C\uBE44 \uB370\uC774\uD130 \uC5F0\uACB0 \uC124\uC815\uC774 \uD544\uC694\uD569\uB2C8\uB2E4.";
                return false;
            }
            panel.ShowBusy("\uAC8C\uC784 \uB370\uC774\uD130 \uD655\uC778 \uC911");
            return await LobbyPreparation(session) && Current(generation, session);
        }

        private async Task<bool> PrepareLobbyDataAsync(GameApiAuthSessionSnapshot session)
        {
            var generation = uiGeneration;
            var errorGeneration = Owner.Generation;
            var result = await client.ReadStoryProgressAsync(session);
            if (!Current(generation, session)) return false;
            readRequiresRefresh = result.RequiresAuthentication;
            if (result.Value == null || Profile?.AccountId != session.AccountId || Profile.DisplayName == null)
            {
                if (!result.RequiresAuthentication) ReportPreparationFailure(errorGeneration);
                return false;
            }
            // Bundled narrative is presentation fallback only; unlocks remain server-owned.
            var catalog = MissionNarrativeCatalog.CreateActOne();
            if (catalog.Count == 0 || result.Value.UnlockedOperations.Any(id => catalog.FindByMissionId(id) == null))
            { ReportPreparationFailure(errorGeneration); return false; }
            LobbySnapshot = new PlayerLobbySnapshot(Profile, result.Value, catalog, Owner.Generation);
            return true;
        }

        private async void ReportPreparationFailure(long originatingGeneration)
        {
            if (disposed) return;
            try
            {
                // Explicit typed producer: no raw server message, log text, stack or old queue is imported.
                if (Diagnostics.Record(SafeClientErrorKind.UnityError, originatingGeneration)) await Diagnostics.FlushAsync();
            }
            catch { }
        }

        public bool BindLobby(GameObject screen)
        {
            if (disposed || !Owner.Ready || LobbySnapshot == null || LobbySnapshot.Generation != Owner.Generation
                || LobbySnapshot.Profile.AccountId != Owner.Session?.AccountId || screen == null) return false;
            var view = screen.GetComponent<MissionShellScreenRootBehaviour>();
            var settings = screen.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SettingsTogglePanelImage");
            var profileFrame = screen.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ProfileNameplateFrameImage");
            if (view == null || settings == null || profileFrame == null) return false;
            new MissionShellScreenPresenter(view).RenderMissionSelect(LobbySnapshot.Story, LobbySnapshot.Catalog);
            var profileLabel = profileFrame.Find("PlayerProfileName")?.GetComponent<TMP_Text>();
            if (profileLabel == null)
            {
                var label = new GameObject("PlayerProfileName", typeof(RectTransform), typeof(TextMeshProUGUI));
                profileLabel = label.GetComponent<TMP_Text>(); profileLabel.rectTransform.SetParent(profileFrame, false);
                profileLabel.rectTransform.anchorMin = Vector2.zero; profileLabel.rectTransform.anchorMax = Vector2.one;
                profileLabel.rectTransform.offsetMin = new Vector2(24, 8); profileLabel.rectTransform.offsetMax = new Vector2(-24, -8);
                profileLabel.font = font; profileLabel.fontSize = 32; profileLabel.color = Color.white;
                profileLabel.richText = false; profileLabel.raycastTarget = false; profileLabel.alignment = TextAlignmentOptions.MidlineLeft;
                profileLabel.enableAutoSizing = true; profileLabel.fontSizeMin = 20; profileLabel.fontSizeMax = 32;
            }
            profileLabel.text = LobbySnapshot.Profile.DisplayName;
            var accountButton = profileFrame.GetComponent<Button>() ?? profileFrame.gameObject.AddComponent<Button>();
            accountButton.targetGraphic = profileFrame.GetComponent<Image>();
            accountButton.onClick.RemoveListener(OpenAccount); accountButton.onClick.AddListener(OpenAccount);
            var existing = settings.Find("SupportInquiry");
            var button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                var root = new GameObject("SupportInquiry", typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = (RectTransform)root.transform; rect.SetParent(settings, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
                root.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
                button = root.GetComponent<Button>(); button.targetGraphic = root.GetComponent<Image>();
                var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                var text = label.GetComponent<TextMeshProUGUI>(); text.rectTransform.SetParent(rect, false);
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.sizeDelta = Vector2.zero; text.font = font; text.fontSize = 28;
                text.text = "\uACE0\uAC1D\uBB38\uC758"; text.richText = false; text.raycastTarget = false;
                text.alignment = TextAlignmentOptions.Center;
            }
            button.onClick.RemoveListener(OpenSupport); button.onClick.AddListener(OpenSupport);
            if (boundLobby != null && boundLobby != screen) HideLobbyScreen(boundLobby);
            retainedLobby = screen; boundLobby = screen;
            var lifetime = screen.GetComponent<PlayerLobbyLifetime>() ?? screen.AddComponent<PlayerLobbyLifetime>();
            lifetime.Deactivated = () => HideLobbyScreen(screen);
            lifetime.Activated = () =>
            {
                if (!disposed && ReferenceEquals(retainedLobby, screen) && Owner.Ready && LobbySnapshot?.Generation == Owner.Generation)
                    boundLobby = screen;
            };
            return true;
        }

        private void HideLobbyScreen(GameObject screen)
        {
            if (!ReferenceEquals(boundLobby, screen)) return;
            boundLobby = null;
            ReceiptRecovery?.Dispose(); ReceiptRecovery = null;
            credentials.Cancel();
            support?.SetSession(null); if (supportPanel != null) supportPanel.Hide();
        }

        private async void OpenSupport()
        {
            if (disposed || boundLobby == null || !Owner.Ready || LobbySnapshot?.Generation != Owner.Generation) return;
            if (support != null && (supportAccount != Owner.Session.AccountId || supportGeneration != Owner.Generation))
            {
                ReceiptRecovery?.Dispose(); ReceiptRecovery = null;
                if (supportPanel != null)
                {
                    supportPanel.Hide();
                    if (Application.isPlaying) UnityEngine.Object.Destroy(supportPanel.gameObject);
                    else UnityEngine.Object.DestroyImmediate(supportPanel.gameObject);
                    supportPanel = null;
                }
                support = null;
            }
            if (support == null)
            {
                supportAccount = Owner.Session.AccountId;
                supportGeneration = Owner.Generation;
                support = new SupportInquiryController(Owner.Session, trackedSupport, SupportInquiryHttpClient.Categories,
                    2000, captureBuild, minContentLength: 10);
            }
            else support.SetSession(Owner.Session);
            if (supportPanel == null)
            {
                var root = new GameObject("SupportInquiryPanel", typeof(RectTransform)); root.transform.SetParent(bootstrap.transform, false);
                supportPanel = root.AddComponent<SupportInquiryPanel>();
                supportPanel.Initialize(support, font, SupportInquiryHttpClient.CategoryLabel);
            }
            if (receiptJournal != null && ReceiptRecovery == null)
            {
                ReceiptRecovery = new SupportReceiptRecovery(support, supportClient, receiptJournal, () => Owner.Session, () => Owner.Generation);
                supportPanel.ConfigureRecovery(ReceiptRecovery);
            }
            supportPanel.Show();
            if (ReceiptRecovery != null) await ReceiptRecovery.RefreshAsync();
        }

        private void OpenAccount()
        {
            if (disposed || boundLobby == null || !Owner.Ready) return;
            supportPanel?.Hide(); credentials.Open("Link");
        }

        public void CloseLobby()
        {
            ReceiptRecovery?.Dispose(); ReceiptRecovery = null;
            credentials?.Cancel();
            boundLobby = null; retainedLobby = null; LobbySnapshot = null;
            support?.SetSession(null); if (supportPanel != null) supportPanel.Hide();
        }

        public void InvalidateAuthentication()
        {
            if (disposed) return;
            recheckAfterCancellation = false;
            credentials.Cancel(true);
            uiGeneration++; var gate = nicknameGate; nicknameGate = null;
            readRequiresRefresh = false;
            Owner.SignOutLocally(); Profile = null; CloseLobby();
            gate?.TrySetResult(false);
        }

        private async void SubmitNickname(string name)
        {
            var gate = nicknameGate; var session = Owner.Session; var generation = uiGeneration;
            if (disposed || submittingName || gate == null || gate.Task.IsCompleted || session == null) return;
            submittingName = true; panel.ShowBusy("\uB2C9\uB124\uC784 \uD655\uC778 \uC911");
            try
            {
                await client.SetDisplayNameAsync(session, name);
                if (!Current(generation, session)) return;
                // A lost write response is reconciled by the authoritative profile, never local input.
                var read = await client.ReadProfileAsync(session);
                if (!Current(generation, session)) return;
                var profile = read.Value;
                if (read.RequiresAuthentication) { readRequiresRefresh = true; gate.TrySetResult(false); return; }
                if (profile?.DisplayName != null) { Profile = profile; gate.TrySetResult(true); }
                else panel.ShowNickname();
            }
            finally { submittingName = false; }
        }

        private void Cancel()
        {
            if (disposed) return;
            InvalidateAuthentication(); bootstrap.ResetAuthenticatedView();
            recheckAfterCancellation = true;
            if (Owner.Busy) ShowPendingCancellation(); else OwnerChanged();
        }
        private async void Retry()
        {
            if (disposed || Owner.Busy) return;
            hydrationRefreshAttempted = false;
            if (CanRecoverReplay) await ResumeReplayAsync();
            else if (Owner.Session != null) await Owner.PrepareAgainAsync();
            else if (!HasReplayRecovery && Owner.CanResumeStoredSession)
            {
                uiGeneration++;
                await Owner.ResumeStoredSessionAsync();
            }
            else ShowLogin();
        }
        private async void OwnerChanged()
        {
            if (disposed) return;
            if (recheckAfterCancellation && !Owner.Busy)
            {
                recheckAfterCancellation = false;
                await Owner.RecheckStorageAsync();
                return;
            }
            switch (Owner.Stage)
            {
                case PlayerAuthStage.Login: if (!Owner.Busy) ShowLogin(); break;
                case PlayerAuthStage.Reading: panel.ShowBusy("\uB85C\uADF8\uC778 \uC815\uBCF4 \uD655\uC778 \uC911"); break;
                case PlayerAuthStage.Authenticating: preparationError = null; CloseLobby(); bootstrap.ResetAuthenticatedView(); panel.ShowBusy("\uB85C\uADF8\uC778 \uD655\uC778 \uC911"); break;
                case PlayerAuthStage.Saving: panel.ShowBusy("\uB85C\uADF8\uC778 \uC815\uBCF4 \uBCF4\uAD00 \uC911"); break;
                case PlayerAuthStage.Preparing:
                    if (replayJournal?.Pending == null) { replayIntent = null; replayBlocked = false; }
                    if (nicknameGate == null || nicknameGate.Task.IsCompleted) panel.ShowBusy("\uD504\uB85C\uD544 \uD655\uC778 \uC911"); break;
                case PlayerAuthStage.Recovery:
                    if (!Owner.Busy && readRequiresRefresh && !hydrationRefreshAttempted && !replayBlocked && Owner.Session != null)
                    {
                        hydrationRefreshAttempted = true; readRequiresRefresh = false;
                        var original = Owner.Session;
                        await Owner.LoginAsync(async () =>
                        {
                            var candidate = await RefreshSessionAsync(original);
                            return candidate?.AccountId == original.AccountId ? candidate : null;
                        });
                        return;
                    }
                    panel.ShowError(preparationError ?? "\uB85C\uADF8\uC778 \uB610\uB294 \uACC4\uC815 \uC815\uBCF4\uB97C \uD655\uC778\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2E4.", CanRecoverReplay || (!replayBlocked && Owner.Session != null)); break;
                case PlayerAuthStage.Ready:
                    if (entering) return;
                    entering = true; panel.Hide();
                    ReconcileErrorReceipts();
                    RestoreRunOperations();
                    try { await bootstrap.AcceptAuthenticatedSessionAsync(Owner.Session); }
                    catch { if (!disposed) panel.ShowError("\uB85C\uBE44 \uC5F0\uACB0\uC744 \uD655\uC778\uD574 \uC8FC\uC138\uC694.", false); }
                    finally { entering = false; }
                    break;
            }
        }
        private bool Current(long generation, GameApiAuthSessionSnapshot session) =>
            !disposed && generation == uiGeneration && ReferenceEquals(Owner.Session, session);
        private async void ReconcileErrorReceipts()
        {
            var owner = Owner.Session;
            if (owner == null || ReferenceEquals(owner, reconciledErrorSession)) return;
            reconciledErrorSession = owner;
            try { await Diagnostics.ReconcilePendingAsync(); }
            catch { /* The support receipt view exposes storage failures without blocking lobby entry. */ }
        }
        private async void RestoreRunOperations()
        {
            var owner = Owner.Session;
            if (Runs == null || owner == null || ReferenceEquals(owner, restoredRunSession)) return;
            restoredRunSession = owner;
            try { await Runs.RestoreAsync(); }
            catch { /* Runs.Notice retains the storage failure; restoration never dispatches a POST. */ }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; uiGeneration++; nicknameGate?.TrySetResult(false);
            Runs?.Dispose();
            Diagnostics.Dispose();
            credentials.Dispose();
            CloseLobby();
            if (supportPanel != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(supportPanel.gameObject);
                else UnityEngine.Object.DestroyImmediate(supportPanel.gameObject);
            }
            bootstrap.LoginRequested -= ShowLogin; bootstrap.GuestLoginRequested -= Guest; bootstrap.SessionRestoreRequested -= Restore;
            Owner.Changed -= OwnerChanged; Owner.Dispose();
            if (panel != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(panel.gameObject);
                else UnityEngine.Object.DestroyImmediate(panel.gameObject);
            }
        }
    }
}
