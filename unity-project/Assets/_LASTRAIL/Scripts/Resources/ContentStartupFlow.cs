using System;
using System.Threading;
using System.Threading.Tasks;

namespace LastRail.Resources
{
    public enum ContentStage { Idle, Config, Initializing, Catalog, Size, Consent, Downloading, Loading, Ready, Failed, Cancelled, Maintenance, UpdateRequired, AwaitingAuthentication }

    public sealed class ClientContentGate
    {
        public bool Maintenance;
        public string MinimumVersion;
        public string ApprovedReleaseId { get; private set; }
        public string ApprovedContentVersion { get; private set; }
        public string ApprovedCatalogUrl { get; private set; }
        public string ApprovedCatalogHash { get; private set; }
        public bool AllowAutomaticCatalogUpdates => false;
        private bool strictAdmission;
        private bool updateRequired;
        private bool authenticationRequired;
        public bool RegistryManaged { get; internal set; }
        public string RegistryRevision { get; internal set; }

        internal static ClientContentGate AwaitingLogin(bool maintenance, string revision) =>
            new ClientContentGate { strictAdmission = true, authenticationRequired = true, Maintenance = maintenance,
                RegistryManaged = true, RegistryRevision = revision };

        internal static ClientContentGate Approved(string releaseId, string contentVersion, string catalogUrl, string hash, bool maintenance)
        {
            return new ClientContentGate { strictAdmission = true, ApprovedReleaseId = releaseId,
                ApprovedContentVersion = contentVersion, ApprovedCatalogUrl = catalogUrl,
                ApprovedCatalogHash = hash, Maintenance = maintenance };
        }

        internal static ClientContentGate RejectedUpdate()
        {
            return new ClientContentGate { strictAdmission = true, updateRequired = true };
        }

        public ContentStage Evaluate(string version)
        {
            if (strictAdmission)
            {
                if (updateRequired) return ContentStage.UpdateRequired;
                if (authenticationRequired) return Maintenance ? ContentStage.Maintenance : ContentStage.AwaitingAuthentication;
                if (string.IsNullOrEmpty(ApprovedCatalogUrl)) return ContentStage.Failed;
                return Maintenance ? ContentStage.Maintenance : ContentStage.Initializing;
            }
            if (!Version.TryParse(MinimumVersion, out var minimum) || !Version.TryParse(version, out var current))
                throw new InvalidOperationException("Invalid client version configuration.");
            if (Maintenance) return ContentStage.Maintenance;
            return current < minimum ? ContentStage.UpdateRequired : ContentStage.Initializing;
        }
    }

    public interface IContentStartupBackend
    {
        Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel);
        Task InitializeAsync(CancellationToken cancel);
        Task UpdateCatalogAsync(CancellationToken cancel);
        Task<long> RequiredBytesAsync(CancellationToken cancel);
        Task DownloadAsync(Action<long, long> progress, CancellationToken cancel);
        Task VerifyRequiredAssetsAsync(CancellationToken cancel);
    }

    public interface IAuthenticatedContentBackend
    {
        Task<ClientContentGate> FetchAuthorizedConfigAsync(CancellationToken cancel);
    }

    public sealed class ContentStartupFlow
    {
        public ContentStage Stage { get; private set; }
        public Exception Failure { get; private set; }
        public bool CanEnterGame => Stage == ContentStage.Ready;
        public event Action<ContentStage> Changed;
        public event Action<long, long> Progress;
        private bool running;

        public async Task RunAsync(IContentStartupBackend backend, string version,
            Func<long, CancellationToken, Task<bool>> confirm, CancellationToken cancel)
        {
            if (running) throw new InvalidOperationException("Content preparation is already running.");
            running = true;
            Failure = null;
            try
            {
                Set(ContentStage.Config);
                var gate = await backend.FetchConfigAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(gate.Evaluate(version));
                if (Stage != ContentStage.Initializing) return;
                await backend.InitializeAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Catalog);
                await backend.UpdateCatalogAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Size);
                var bytes = await backend.RequiredBytesAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                if (bytes < 0) throw new InvalidOperationException("Invalid download size.");
                if (bytes > 0)
                {
                    Set(ContentStage.Consent);
                    if (!await confirm(bytes, cancel)) throw new OperationCanceledException();
                    cancel.ThrowIfCancellationRequested();
                    Set(ContentStage.Downloading);
                    await backend.DownloadAsync((received, total) => { if (!cancel.IsCancellationRequested) Progress?.Invoke(received, total); }, cancel);
                }
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Loading);
                await backend.VerifyRequiredAssetsAsync(cancel);
                cancel.ThrowIfCancellationRequested();
                Set(ContentStage.Ready);
            }
            catch (OperationCanceledException) { Set(ContentStage.Cancelled); }
            catch (Exception error) { Failure = error; Set(ContentStage.Failed); }
            finally { running = false; }
        }

        private void Set(ContentStage stage) { Stage = stage; Changed?.Invoke(stage); }
    }
}
