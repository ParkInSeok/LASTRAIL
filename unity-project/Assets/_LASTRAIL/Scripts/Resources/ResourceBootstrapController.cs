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
#if UNITY_EDITOR
        [SerializeField] private bool editorRemoteContentVerification;
        // Explicit, one-use local recording inputs. Compiled out of player builds.
        public static Func<IContentStartupBackend> EditorCaptureBackend;
        public static Func<DownloadAccessPolicy, AddressableContentBackend> EditorVerificationBackend;
        public static Func<ResourceBootstrapController, TMP_FontAsset, string, PlayerEntryRuntime> EditorCaptureAuthentication;
#endif
        private AddressableContentBackend backend;
        private IDisposable preparedContent;
        public LastRail.Presentation.MissionShell.EquipmentIconResources EquipmentIcons { get; private set; }
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
        private StartupIntroOverlay opening;
        private LobbyArrivalTransition arrival;
        private bool resourcePreparationPending;
        private bool invalidatingContent;
        public StartupIntroOverlay Opening => opening;
        private readonly SemaphoreSlim mainEntryLease = new SemaphoreSlim(1, 1);
        public PlayerEntryRuntime Authentication => authentication;
        public string ApprovedContentReleaseId => isActiveAndEnabled && !invalidatingContent ? backend?.ApprovedContentReleaseId : null;
        public event Action LoginRequested;
        public event Action GuestLoginRequested;
        public event Action SessionRestoreRequested;
        public DownloadCoordinator Coordinator => coordinator;
        public bool NeedsPreparedAuthentication => isActiveAndEnabled && !invalidatingContent &&
            coordinator?.CanAuthenticate == true && !coordinator.Access.Authenticated;

        private void OnEnable()
        {
            resourcePreparationPending = false;
            invalidatingContent = false;
            lifetime = new CancellationTokenSource();
            access = new DownloadAccessPolicy();
            network = new ContentNetworkService();
            network.NetworkStateChanged += NetworkChanged;
            network.Start();
            access.SetNetwork(network.Current);
            authView = StartupScreenViewState.FromAuth(AuthGateUxState.FirstLaunch());
            var url = ClientEnvironmentBootstrap.CurrentEnvironment == ClientEnvironment.Unconfigured
                ? null : ClientEnvironmentBootstrap.CurrentClientConfigUrl;
            var source = ContentSourceSelection.Current();
#if UNITY_EDITOR
            source = ContentSourceSelection.Current(editorRemoteContentVerification);
#endif
#if UNITY_EDITOR
            var verificationBackend = EditorVerificationBackend; EditorVerificationBackend = null;
            backend = verificationBackend != null ? verificationBackend(access) :
                new AddressableContentBackend(url, remoteHostingConfigured, access, trustedCatalogRoot, source: source);
#else
            backend = new AddressableContentBackend(url, remoteHostingConfigured, access, trustedCatalogRoot, source: source);
#endif
            preparedContent = PreparedContent.Install(backend);
            EquipmentIcons = new LastRail.Presentation.MissionShell.EquipmentIconResources(backend);
            IContentStartupBackend startupBackend = backend;
#if UNITY_EDITOR
            var captureBackend = EditorCaptureBackend; EditorCaptureBackend = null;
            if (captureBackend != null) startupBackend = captureBackend();
#endif
            coordinator = new DownloadCoordinator(startupBackend, access);
            mainScene = new AddressableMainSceneHost();
            var sceneOwner = mainScene;
            var screenOwner = screens;
            var contentOwner = backend;
            entry = new AuthenticatedScreenEntry(async token =>
            {
                await mainEntryLease.WaitAsync(token);
                var authOwner = authentication;
                PreparedContent.Scope shellResources = null;
                try
                {
                    shellResources = await PreparedContent.PrepareAsync(new[]{"LobbyDeck/","OperatorIdentity/","NumberedFormation/","ApprovedUi/","CommanderSupportIcons/","DestinationPages/operators-atlas","DestinationPages/TabIcon","DestinationPages/tab-states-v01","NoncombatCodex/codex-dark-steel-surface-v01","CombatPresentation/Audio/LobbyDepartureV01","StartupMotion/JunctionLightFill","StartupMotion/login-map-detail-v02","StartupMotion/login-route-emissive-mask-v01","StartupMotion/SharedRailwayPath","StartupMotion/RailwayNode"}, token);
                    var main = await sceneOwner.OpenAsync(contentOwner.ResolveApprovedMainScene(), token);
                    var screen = await screenOwner.OpenAsync(contentOwner.ResolveApprovedScreen(AddressableContentBackend.LobbyKey), main.ContentRoot, token);
                    token.ThrowIfCancellationRequested();
                    PreparedContent.Attach(screen, shellResources); shellResources = null;
                    if (authOwner == null || !ReferenceEquals(authOwner, authentication) || !authOwner.BindLobby(screen))
                        throw new InvalidOperationException("Lobby account binding is not ready.");
                    var previous = GetComponentInChildren<ResourcePreparationView>(true)?.GetComponentInChildren<StartupMotionBackdrop>(true)
                        ?? GetComponentInChildren<PlayerEntryPanel>(true)?.GetComponentInChildren<StartupMotionBackdrop>(true);
                    var transition = LobbyArrivalTransition.Create(transform, previous);
                    arrival = transition;
                    try
                    {
                            transition.PrepareLobby(screen);
                        await transition.IlluminateAsync(token);
                        token.ThrowIfCancellationRequested();
                        if (!ReferenceEquals(authOwner, authentication) || !authOwner.Owner.Ready)
                            throw new OperationCanceledException(token);
                        contentOwner.CompletePendingRelease();
                        main.Reveal();
                        startup.gameObject.SetActive(false); overlay.Show(false, 0, false);
                        await transition.RevealAsync(token);
                    }
                    finally
                    {
                        transition.Abort();
                        if (arrival == transition) arrival = null;
                        Destroy(transition.gameObject);
                    }
                }
                catch (Exception error)
                {
#if UNITY_EDITOR
                    Debug.LogException(error);
#endif
                    await screenOwner.CloseAsync();
                    await sceneOwner.CloseAsync();
                    throw;
                }
                finally { shellResources?.Dispose(); mainEntryLease.Release(); }
            }, () => CloseMain(sceneOwner, screenOwner));
            entry.Changed += Render;
            coordinator.ConfigPassed += ConfigPassed;
            coordinator.AuthenticationAllowed += ResumePreparedAuthentication;
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
            overlay.PauseRequested = () => access.SetPaused(!access.Paused);
            var font = startup.GetComponentInChildren<TMP_Text>(true).font;
#if UNITY_EDITOR
            var captureAuth = EditorCaptureAuthentication; EditorCaptureAuthentication = null;
            authentication = captureAuth == null ? new PlayerEntryRuntime(this, font, url) : captureAuth(this, font, url);
#else
            authentication = new PlayerEntryRuntime(this, font, url);
#endif
            backend.ConfigureAuthorization(() => authentication?.Owner.Ready == true ? authentication.Owner.Session : null,
                () => authentication?.Owner.Generation ?? -1);
            authentication.Owner.Changed += ContentOwnerChanged;
            backend.SessionExpired += InvalidateContentOwner;
            startup.SetSupportAction(()=>authentication?.OpenStartupSupport(),()=>authentication?.CanOpenStartupSupport==true);
            opening = StartupIntroOverlay.Create(transform, font, () => coordinator.CanAuthenticate,
                () => coordinator.Stage, RevealLogin);
            Begin();
        }

        private void NetworkChanged(ContentNetwork value) { access.SetNetwork(value); }
        private void OnApplicationFocus(bool focused)
        {
            if (focused) ContentOwnerChanged();
        }
        private void ContentOwnerChanged()
        {
            if (!invalidatingContent && backend?.HasSessionWork == true && !backend.SessionWorkCurrent)
                InvalidateContentOwner();
        }

        private async void InvalidateContentOwner()
        {
            if (invalidatingContent || lifetime == null) return;
            invalidatingContent = true;
            var owner = coordinator;
            var ownerLifetime = lifetime;
            backend.InvalidateSessionAuthorization();
            ResetAuthenticatedView();
            try
            {
                await owner.ResetAdmissionAsync();
                if (lifetime != ownerLifetime || coordinator != owner) return;
                invalidatingContent = false;
                if (isActiveAndEnabled && !ownerLifetime.IsCancellationRequested) Begin();
            }
            catch (OperationCanceledException) { }
            finally { if (lifetime == ownerLifetime && coordinator == owner) invalidatingContent = false; }
        }
        private async void OnApplicationPause(bool paused)
        {
            if (access == null || lifetime == null) return;
            var generation = ++pauseGeneration;
            var owner = access;
            owner.SetSuspended(true);
            if (paused) return;
            ContentOwnerChanged();
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
            resourcePreparationPending = false;
            ++pauseGeneration;
            if (arrival != null) arrival.Abort();
            if (opening != null) { opening.gameObject.SetActive(false); Destroy(opening.gameObject); opening = null; }
            if (backend != null) backend.SessionExpired -= InvalidateContentOwner;
            if (authentication != null) authentication.Owner.Changed -= ContentOwnerChanged;
            authentication?.Dispose(); authentication = null;
            startup.PrimaryActionRequested -= Primary; startup.SecondaryActionRequested -= Secondary;
            startup.SetSupportAction(null,null);
            coordinator.ConfigPassed -= ConfigPassed; coordinator.Changed -= Render; access.Changed -= Render;
            coordinator.AuthenticationAllowed -= ResumePreparedAuthentication;
            network.NetworkStateChanged -= NetworkChanged; network.Dispose();
            lifetime.Cancel(); lifetime.Dispose(); lifetime = null;
            if (overlay != null) Destroy(overlay.gameObject);
            session = null;
            if (entry != null) { entry.Changed -= Render; entry.Reset(); }
            preparedContent?.Dispose(); preparedContent = null;
            backend?.Dispose(); backend = null; EquipmentIcons = null;
        }

        private void ConfigPassed() { Render(); }
        private async void ResumePreparedAuthentication()
        {
            if (invalidatingContent || !isActiveAndEnabled || opening?.Timeline.Revealed != true || authentication?.Owner.Ready != true) return;
            await authentication.ResumePreparedContentAsync();
        }

        private void RevealLogin()
        {
            if (!isActiveAndEnabled || !coordinator.CanAuthenticate) return;
            startup.GetComponentInChildren<StartupMotionBackdrop>(true)?.Show(true, false);
            Render();
            SessionRestoreRequested?.Invoke();
            ResumePreparedAuthentication();
        }

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
            authentication?.CloseStartupSupport();
            if (coordinator.Failure is TesterContentForbiddenException || coordinator.Failure is ContentSessionExpiredException) { ReturnToLoginAfterDenial(); return; }
            if (coordinator.Stage == ContentStage.UpdateRequired)
            {
                try { UpdateAction.OpenFromUserClick(); RenderUpdateNotice(); }
                finally { startup.CompletePendingAction(); }
                return;
            }
            if (coordinator.Stage == ContentStage.Failed || coordinator.Stage == ContentStage.Maintenance) { Retry(); return; }
            if (!coordinator.CanAuthenticate) return;
            if (opening != null && !opening.Timeline.Revealed) return;
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
            authentication?.CloseStartupSupport();
            if (coordinator.Stage == ContentStage.UpdateRequired || coordinator.Failure is TesterContentForbiddenException || coordinator.Failure is ContentSessionExpiredException) { Application.Quit(); return; }
            if (coordinator.Stage == ContentStage.Failed || entry?.Failed == true) { ReturnToLoginAfterDenial(); return; }
            if (opening != null && !opening.Timeline.Revealed) return;
            if (coordinator.CanAuthenticate && session == null && GuestLoginRequested != null)
            {
                authView = StartupScreenViewState.Waiting(); Render(); GuestLoginRequested.Invoke();
            }
        }

        public async Task AcceptAuthenticatedSessionAsync(GameApiAuthSessionSnapshot accepted)
        {
            if (accepted == null) throw new ArgumentNullException(nameof(accepted));
            if (authentication != null && (!authentication.Owner.Ready || !ReferenceEquals(authentication.Owner.Session, accepted)))
                throw new InvalidOperationException("Authenticated account preparation has not completed.");
            if (lifetime == null || !coordinator.CanAuthenticate || invalidatingContent) throw new InvalidOperationException("Config gate has not accepted authentication.");
            if (opening != null && !opening.Timeline.Revealed) throw new InvalidOperationException("Opening transition has not completed.");
            if (!string.Equals(session?.AccountId, accepted.AccountId, StringComparison.Ordinal))
            {
                entry.Reset(); startup.gameObject.SetActive(true);
            }
            if (!resourcePreparationPending) overlay?.BeginAuthenticatedPlan(coordinator, access);
            resourcePreparationPending = false;
            session = accepted;
            startup.GetComponentInChildren<StartupMotionBackdrop>(true)?.Show(true, true);
            network.Refresh();
            access.SetAuthenticated(true);
            Render();
            await EnterAsync(lifetime.Token);
        }

        private async void ReturnToLoginAfterDenial()
        {
            if (invalidatingContent) return;
            invalidatingContent = true;
            var owner = coordinator;
            var ownerLifetime = lifetime;
            try
            {
                backend.InvalidateSessionAuthorization();
                ClearAuthenticatedSession();
                await owner.ResetAdmissionAsync();
                if (isActiveAndEnabled && coordinator == owner && lifetime == ownerLifetime) Begin();
            }
            catch (OperationCanceledException) { }
            finally { if (coordinator == owner && lifetime == ownerLifetime) invalidatingContent = false; }
        }

        public void RenderAuthentication(AuthGateUxState state) { authView = StartupScreenViewState.FromAuth(state); Render(); }

        public void ClearAuthenticatedSession()
        {
            authentication?.InvalidateAuthentication();
            ResetAuthenticatedView();
        }

        internal void ResetAuthenticatedView()
        {
            resourcePreparationPending = false;
            if (arrival != null) arrival.Abort();
            authentication?.CloseLobby();
            session = null; access.SetAuthenticated(false); entry.Reset();
            startup.GetComponentInChildren<StartupMotionBackdrop>(true)?.Show(opening != null && opening.Timeline.Revealed, false);
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

        public void SetResourcePreparationPending(bool value)
        {
            if (value && !resourcePreparationPending && coordinator != null) overlay?.BeginAuthenticatedPlan(coordinator,access);
            resourcePreparationPending=value;
            Render();
        }

        private void Render()
        {
            if (!isActiveAndEnabled || overlay == null || (entry != null && entry.Entered)) return;
            if ((entry?.Failed == true || coordinator.Stage == ContentStage.Failed) && coordinator.Failure is not TesterContentForbiddenException && coordinator.Failure is not ContentSessionExpiredException)
            {
                startup.Render(StartupScreenViewState.NetworkInterrupted(backend?.PinConfirmationRequired == true
                    ? "콘텐츠 버전이 변경되었습니다. 다시 확인해 주세요." : "네트워크 상태를 확인한 뒤 다시 시도해 주세요."));
                overlay.Show(false, 0, false); overlay.RenderPreparation(false, coordinator, access); return;
            }
            if (entry != null && entry.Failed)
            {
                startup.Render(StartupScreenViewState.DownloadError(backend?.PinConfirmationRequired == true
                    ? "\uCF58\uD150\uCE20 \uBC84\uC804\uC774 \uBCC0\uACBD\uB418\uC5C8\uC2B5\uB2C8\uB2E4." : "\uB9AC\uC18C\uC2A4 \uB85C\uB4DC \uC2E4\uD328"));
                overlay.Show(false, 0, true); overlay.RenderPreparation(session != null, coordinator, access, true); return;
            }
            if (!coordinator.ConfigAccepted)
            {
                if (coordinator.Failure is ContentSessionExpiredException) startup.Render(StartupScreenViewState.SessionExpired());
                else if (coordinator.Failure is TesterContentForbiddenException) startup.Render(StartupScreenViewState.InternalAccessDenied());
                else if (coordinator.Stage == ContentStage.AwaitingAuthentication && coordinator.CanAuthenticate) startup.Render(authView);
                else if (coordinator.Stage == ContentStage.UpdateRequired) RenderUpdateNotice();
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
            overlay.Show(access.NeedsConsent, remaining, stage == ContentStage.Failed || (remaining > 0 && !access.Allowed && !access.NeedsConsent && !access.Paused));
            overlay.RenderPreparation((session != null || resourcePreparationPending) && arrival == null, coordinator, access);
        }
    }
}
