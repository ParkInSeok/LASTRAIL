using System;
using System.Threading;
using System.Threading.Tasks;

namespace LastRail.Resources
{
    public interface IContentRevision { string ContentRevision { get; } }

    public sealed class DownloadCoordinator
    {
        private readonly IContentStartupBackend backend;
        public DownloadAccessPolicy Access { get; }
        public ContentStage Stage { get; private set; }
        public bool ConfigAccepted { get; private set; }
        private bool loginEligible, registryManaged;
        private CancellationTokenSource operationCancellation;
        public bool CanAuthenticate => loginEligible || ConfigAccepted;
        public bool CanEnterGame => ConfigAccepted && Access.Authenticated && Stage == ContentStage.Ready;
        public long ReceivedBytes { get; private set; }
        public long TotalBytes { get; private set; }
        public Exception Failure { get; private set; }
        public Task Operation { get; private set; }
        public event Action Changed;
        public event Action ConfigPassed;
        public event Action AuthenticationAllowed;

        public DownloadCoordinator(IContentStartupBackend backend, DownloadAccessPolicy access)
        {
            this.backend = backend; Access = access;
        }

        public Task StartAsync(string version, CancellationToken cancel)
        {
            if (Operation != null && !Operation.IsCompleted) return Operation;
            operationCancellation?.Dispose();
            operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            Operation = RunOwnedAsync(version, operationCancellation);
            return Operation;
        }

        private async Task RunOwnedAsync(string version, CancellationTokenSource owner)
        {
            try { await RunAsync(version, owner.Token); }
            finally
            {
                if (ReferenceEquals(operationCancellation, owner)) operationCancellation = null;
                owner.Dispose();
            }
        }

        public async Task ResetAdmissionAsync()
        {
            operationCancellation?.Cancel();
            if (Operation != null) await Operation;
            InvalidateAdmission();
        }

        public void InvalidateAdmission()
        {
            if (Operation != null && !Operation.IsCompleted) throw new InvalidOperationException("Content preparation is still running.");
            ConfigAccepted = false; loginEligible = false; registryManaged = false; Failure = null;
            Set(ContentStage.Idle);
        }

        private async Task RunAsync(string version, CancellationToken cancel)
        {
            Failure = null;
            try
            {
                if (!ConfigAccepted)
                {
                    Set(ContentStage.Config);
                    var gate = await backend.FetchConfigAsync(cancel);
                    cancel.ThrowIfCancellationRequested();
                    var result = gate.Evaluate(version);
                    Set(result);
                    if (result == ContentStage.AwaitingAuthentication)
                    {
                        if (!(backend is IAuthenticatedContentBackend))
                            throw new InvalidOperationException("Authenticated content lookup is unavailable.");
                        loginEligible = true;
                        AuthenticationAllowed?.Invoke();
                        await Access.WaitForAuthenticationAsync(cancel);
                        gate = await AuthorizeAsync(cancel);
                        result = gate.Evaluate(version);
                        Set(result);
                    }
                    if (result != ContentStage.Initializing) return;
                    registryManaged = gate.RegistryManaged;
                    ConfigAccepted = true;
                    if (!loginEligible)
                    {
                        loginEligible = true;
                        AuthenticationAllowed?.Invoke();
                    }
                    ConfigPassed?.Invoke();
                }
                Set(ContentStage.Initializing);
                await backend.InitializeAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Catalog);
                await backend.UpdateCatalogAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Size);
                var bytes = await backend.RequiredBytesAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Access.SetPlan((backend as IContentRevision)?.ContentRevision ?? version, bytes);
                ReceivedBytes = 0; TotalBytes = bytes;
                if (bytes > 0)
                {
                    Set(ContentStage.Downloading);
                    await Access.WaitForAccessAsync(cancel);
                    await backend.DownloadAsync((received, total) =>
                    {
                        if (cancel.IsCancellationRequested) return;
                        ReceivedBytes = received; TotalBytes = total;
                        Changed?.Invoke();
                    }, cancel);
                }
                cancel.ThrowIfCancellationRequested();
                Access.SetRemaining(0);
                Set(ContentStage.AwaitingAuthentication);
                await Access.WaitForAuthenticationAsync(cancel);
                if (registryManaged)
                {
                    var authorized = await AuthorizeAsync(cancel);
                    var decision = authorized.Evaluate(version);
                    if (decision != ContentStage.Initializing) { ConfigAccepted = false; Set(decision); return; }
                }
                Set(ContentStage.Loading);
                await backend.VerifyRequiredAssetsAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Ready);
            }
            catch (OperationCanceledException) { Set(ContentStage.Cancelled); }
            catch (Exception error) { Failure = error; Set(ContentStage.Failed); }
        }

        private async Task<ClientContentGate> AuthorizeAsync(CancellationToken cancel)
        {
            try
            {
                if (!(backend is IAuthenticatedContentBackend authenticated))
                    throw new InvalidOperationException("Authenticated content lookup is unavailable.");
                var gate = await authenticated.FetchAuthorizedConfigAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                if (gate.Evaluate(null) == ContentStage.AwaitingAuthentication)
                    throw new InvalidOperationException("The session did not authorize content.");
                return gate;
            }
            catch { ConfigAccepted = false; throw; }
        }

        private void Set(ContentStage stage) { Stage = stage; Changed?.Invoke(); }
    }
}
