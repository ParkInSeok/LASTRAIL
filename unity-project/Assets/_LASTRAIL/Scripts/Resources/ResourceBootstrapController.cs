using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Presentation.AuthGate;
using LastRail.Presentation.Startup;
using TMPro;
using UnityEngine;

namespace LastRail.Resources
{
    public sealed class ResourceBootstrapController : MonoBehaviour
    {
        [SerializeField] private StartupScreenRootBehaviour startup;
        [SerializeField] private AddressableScreenHost screens;
        [SerializeField] private bool remoteHostingConfigured;
        [SerializeField] private string trustedCatalogRoot;
        private AddressableContentBackend backend;
        private CancellationTokenSource lifetime;
        private DownloadAccessPolicy access;
        private DownloadCoordinator coordinator;
        private ContentNetworkService network;
        private StartupDownloadOverlay overlay;
        private StartupScreenViewState authView;
        private GameApiAuthSessionSnapshot session;
        private AuthenticatedScreenEntry entry;
        private PlayerEntryRuntime authentication;
        private RequiredUpdateAction requiredUpdate;
        private AddressableMainSceneHost mainScene;
        private int pauseGeneration;
        private readonly SemaphoreSlim mainEntryLease = new SemaphoreSlim(1, 1);
        public PlayerEntryRuntime Authentication => authentication;
        public event Action LoginRequested;
        public event Action GuestLoginRequested;
        public event Action SessionRestoreRequested;
        public DownloadCoordinator Coordinator => coordinator;

        private void OnEnable()
        {
            lifetime = new CancellationTokenSource();
            access = new DownloadAccessPolicy();
            network = new ContentNetworkService();
            network.NetworkStateChanged += NetworkChanged;
            network.Start();
            access.SetNetwork(network.Current);
            authView = StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch());
            var url = ClientEnvironmentBootstrap.CurrentEnvironment == ClientEnvironment.Unconfigured
                ? null : ClientEnvironmentBootstrap.CurrentClientConfigUrl;
            backend = new AddressableContentBackend(url, remoteHostingConfigured, access, trustedCatalogRoot);
            coordinator = new DownloadCoordinator(backend, access);
            mainScene = new AddressableMainSceneHost();
            var sceneOwner = mainScene;
            var screenOwner = screens;
            var contentOwner = backend;
            entry = new AuthenticatedScreenEntry(async token =>
            {
                await mainEntryLease.WaitAsync(token);
                var authOwner = authentication;
                try
                {
                    var main = await sceneOwner.OpenAsync(contentOwner.ResolveApprovedMainScene(), token);
                    var screen = await screenOwner.OpenAsync(contentOwner.ResolveApprovedScreen(AddressableContentBackend.LobbyKey), main.ContentRoot, token);
                    token.ThrowIfCancellationRequested();
                    if (authOwner == null || !ReferenceEquals(authOwner, authentication) || !authOwner.BindLobby(screen))
                        throw new InvalidOperationException("Lobby account binding is not ready.");
                    contentOwner.CompletePendingRelease();
                    main.Reveal();
                }
                catch
                {
                    await screenOwner.CloseAsync();
                    await sceneOwner.CloseAsync();
                    throw;
                }
                finally { mainEntryLease.Release(); }
            }, () => CloseMain(sceneOwner, screenOwner));
            entry.Changed += Render;
            coordinator.ConfigPassed += ConfigPassed;
            coordinator.Changed += Render;
            access.Changed += Render;
            startup.gameObject.SetActive(true);
            startup.PrimaryActionRequested += Primary;
            startup.SecondaryActionRequested += Secondary;
            var ui = new GameObject("StartupDownloadOverlay", typeof(RectTransform));
            ui.transform.SetParent(transform, false);
            overlay = ui.AddComponent<StartupDownloadOverlay>();
            overlay.Initialize(startup.GetComponentInChildren<TMP_Text>(true).font,
                () => access.Approve(), () => access.Decline(), Retry);
            authentication = new PlayerEntryRuntime(this, startup.GetComponentInChildren<TMP_Text>(true).font, url);
            Begin();
        }

        private void NetworkChanged(ContentNetwork value) { access.SetNetwork(value); }
        private async void OnApplicationPause(bool paused)
        {
            if (access == null || lifetime == null) return;
            var generation = ++pauseGeneration;
            var owner = access;
            owner.SetSuspended(true);
            if (paused) return;
            try
            {
                var snapshot = await network.RefreshAsync(lifetime.Token);
                if (generation != pauseGeneration || access != owner || lifetime == null) return;
                owner.SetNetwork(snapshot);
                owner.SetSuspended(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { Render(); }
        }

        private async void CloseMain(AddressableMainSceneHost sceneOwner, AddressableScreenHost screenOwner)
        {
            sceneOwner.Current?.Hide();
            screenOwner.Close();
            await mainEntryLease.WaitAsync();
            try
            {
                await screenOwner.CloseAsync();
                await sceneOwner.CloseAsync();
            }
            catch (Exception) { Debug.LogError("Main scene cleanup failed."); }
            finally { mainEntryLease.Release(); }
        }

        private void OnDisable()
        {
            ++pauseGeneration;
            authentication?.Dispose(); authentication = null;
            startup.PrimaryActionRequested -= Primary; startup.SecondaryActionRequested -= Secondary;
            coordinator.ConfigPassed -= ConfigPassed; coordinator.Changed -= Render; access.Changed -= Render;
            network.NetworkStateChanged -= NetworkChanged; network.Dispose();
            lifetime.Cancel(); lifetime.Dispose(); lifetime = null;
            if (overlay != null) Destroy(overlay.gameObject);
            session = null;
            if (entry != null) { entry.Changed -= Render; entry.Reset(); }
            backend?.Dispose(); backend = null;
        }

        private void ConfigPassed() { Render(); SessionRestoreRequested?.Invoke(); }

        private async void Begin()
        {
            var owner = coordinator;
            var cancel = lifetime.Token;
            if (!owner.CanEnterGame || entry.Failed) await owner.StartAsync(Application.version, cancel);
            if (!cancel.IsCancellationRequested && owner == coordinator) await EnterAsync(cancel);
        }

        private void Retry()
        {
            if (backend?.PinConfirmationRequired == true && (coordinator.Operation == null || coordinator.Operation.IsCompleted))
            {
                try
                {
                    backend.ResetPendingRelease();
                    entry.Reset();
                    coordinator.InvalidateAdmission();
                }
                catch { Render(); return; }
            }
            network.Refresh(); access.Retry();
            if (coordinator.Operation == null || coordinator.Operation.IsCompleted) Begin();
        }

        private void Primary()
        {
            if (coordinator.Stage == ContentStage.UpdateRequired)
            {
                try { UpdateAction.OpenFromUserClick(); RenderUpdateNotice(); }
                finally { startup.CompletePendingAction(); }
                return;
            }
            if (!coordinator.ConfigAccepted) { if (coordinator.Stage == ContentStage.Failed || coordinator.Stage == ContentStage.Maintenance) Retry(); return; }
            if (entry != null && entry.Failed) { Retry(); return; }
            if (session == null && LoginRequested != null)
            {
                authView = StartupScreenViewState.Waiting(); Render(); LoginRequested.Invoke();
            }
        }

        private RequiredUpdateAction UpdateAction => requiredUpdate ?? (requiredUpdate = new RequiredUpdateAction(
            null, Array.Empty<string>(), ApprovedUpdateLauncher.TryOpen));

        public void ConfigureRequiredUpdateAction(RequiredUpdateAction action)
        {
            requiredUpdate = action ?? throw new ArgumentNullException(nameof(action));
            if (coordinator?.Stage == ContentStage.UpdateRequired) RenderUpdateNotice();
        }

        private void RenderUpdateNotice() => startup.Render(StartupScreenViewState.UpdateNotice("", UpdateAction.Notice));

        private void Secondary()
        {
            if (coordinator.ConfigAccepted && session == null && GuestLoginRequested != null)
            {
                authView = StartupScreenViewState.Waiting(); Render(); GuestLoginRequested.Invoke();
            }
        }

        public async Task AcceptAuthenticatedSessionAsync(GameApiAuthSessionSnapshot accepted)
        {
            if (accepted == null) throw new ArgumentNullException(nameof(accepted));
            if (authentication != null && (!authentication.Owner.Ready || !ReferenceEquals(authentication.Owner.Session, accepted)))
                throw new InvalidOperationException("Authenticated account preparation has not completed.");
            if (lifetime == null || !coordinator.ConfigAccepted) throw new InvalidOperationException("Config gate has not accepted authentication.");
            if (!string.Equals(session?.AccountId, accepted.AccountId, StringComparison.Ordinal))
            {
                entry.Reset(); startup.gameObject.SetActive(true);
            }
            session = accepted;
            network.Refresh();
            access.SetAuthenticated(true);
            Render();
            await EnterAsync(lifetime.Token);
        }

        public void RenderAuthentication(AuthGateUxState state) { authView = StartupScreenViewState.FromAuth(state); Render(); }

        public void ClearAuthenticatedSession()
        {
            authentication?.InvalidateAuthentication();
            ResetAuthenticatedView();
        }

        internal void ResetAuthenticatedView()
        {
            authentication?.CloseLobby();
            session = null; access.SetAuthenticated(false); entry.Reset();
            startup.gameObject.SetActive(true); authView = StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch()); Render();
        }

        private async Task EnterAsync(CancellationToken cancel)
        {
            if (!coordinator.CanEnterGame || session == null || entry == null) return;
            var owner = entry;
            if (await owner.EnterAsync(cancel) && owner == entry && owner.Entered && session != null && !cancel.IsCancellationRequested)
            {
                startup.gameObject.SetActive(false); overlay.Show(false, 0, false);
            }
        }

        private void Render()
        {
            if (!isActiveAndEnabled || overlay == null || (entry != null && entry.Entered)) return;
            if (entry != null && entry.Failed)
            {
                startup.Render(StartupScreenViewState.DownloadError(backend?.PinConfirmationRequired == true
                    ? "\uCF58\uD150\uCE20 \uBC84\uC804\uC774 \uBCC0\uACBD\uB418\uC5C8\uC2B5\uB2C8\uB2E4." : "\uB9AC\uC18C\uC2A4 \uB85C\uB4DC \uC2E4\uD328"));
                overlay.Show(false, 0, true); return;
            }
            if (!coordinator.ConfigAccepted)
            {
                if (coordinator.Stage == ContentStage.UpdateRequired) RenderUpdateNotice();
                else if (coordinator.Stage == ContentStage.Maintenance || coordinator.Stage == ContentStage.Failed)
                    startup.Render(StartupScreenViewState.DownloadError(backend?.PinConfirmationRequired == true
                        ? "\uCF58\uD150\uCE20 \uBC84\uC804\uC774 \uBCC0\uACBD\uB418\uC5C8\uC2B5\uB2C8\uB2E4."
                        : "\uC11C\uBC84 \uC0C1\uD0DC\uC640 \uC5F0\uACB0\uC744 \uD655\uC778\uD574 \uC8FC\uC138\uC694."));
                else startup.Render(StartupScreenViewState.Waiting());
                overlay.Show(false, 0, false); return;
            }
            var stage = coordinator.Stage;
            var remaining = access.RemainingBytes;
            var label = stage == ContentStage.Failed ? "\uB2E4\uC6B4\uB85C\uB4DC \uC624\uB958"
                : stage == ContentStage.Loading ? "\uB9AC\uC18C\uC2A4 \uBD88\uB7EC\uC624\uB294 \uC911"
                : access.Network == ContentNetwork.Offline ? "\uC5F0\uACB0 \uD6C4 \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694."
                : remaining > 0 && !access.Allowed ? "\uB2E4\uC6B4\uB85C\uB4DC \uB300\uAE30"
                : stage == ContentStage.AwaitingAuthentication || stage == ContentStage.Ready ? "\uB2E4\uC6B4\uB85C\uB4DC \uC644\uB8CC"
                : "\uCF58\uD150\uCE20 \uD655\uC778 \uC911";
            float? fraction = null;
            if (stage == ContentStage.Downloading && access.Allowed && coordinator.TotalBytes > 0)
            {
                var received = Math.Max(0, Math.Min(coordinator.ReceivedBytes, coordinator.TotalBytes));
                fraction = (float)received / coordinator.TotalBytes;
                label = (received / 1048576d).ToString("0.0") + " / " + (coordinator.TotalBytes / 1048576d).ToString("0.0") + " MiB";
            }
            startup.Render((session == null ? authView : StartupScreenViewState.Waiting()).WithContent(label, fraction));
            overlay.Show(access.NeedsConsent, remaining, stage == ContentStage.Failed || (remaining > 0 && !access.Allowed && !access.NeedsConsent));
        }
    }
}
