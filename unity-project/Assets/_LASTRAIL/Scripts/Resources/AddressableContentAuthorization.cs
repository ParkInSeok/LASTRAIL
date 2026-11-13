using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;

namespace LastRail.Resources
{
    public sealed partial class AddressableContentBackend : IAuthenticatedContentBackend
    {
        SessionContentAuthorization authorization;
        bool sessionBoundGate;
        public event Action SessionExpired;
        public bool HasSessionAuthorization => authorization?.HasAuthorization == true;
        public bool SessionAuthorizationCurrent => authorization?.AuthorizationCurrent == true;
        public bool HasSessionWork => authorization?.HasSessionWork == true;
        public bool SessionWorkCurrent => authorization?.SessionWorkCurrent == true;

        public void ConfigureAuthorization(Func<GameApiAuthSessionSnapshot> session, Func<long> generation)
        {
            if (Source != ContentResourceSource.RemoteApproved || !contentConfigured || string.IsNullOrEmpty(configUrl)) return;
            if (authorization != null) throw new InvalidOperationException("Content authorization owner is already configured.");
            authorization = new SessionContentAuthorization(configUrl, installedVersionCode, trustedCatalogRoot, session, generation);
            authorization.SessionExpired += () => SessionExpired?.Invoke();
        }

        public async Task<ClientContentGate> FetchAuthorizedConfigAsync(CancellationToken cancel)
        {
            if (disposed) throw new ObjectDisposedException(nameof(AddressableContentBackend));
            if (authorization == null) throw new InvalidOperationException("Authenticated content lookup is not configured.");
            var generation = admissionGeneration;
            var gate = await authorization.FetchAsync(cancel);
            cancel.ThrowIfCancellationRequested();
            if (disposed || generation != admissionGeneration) throw new OperationCanceledException();
            if (gate.Evaluate(null) != ContentStage.Initializing) { approvedGate = null; return gate; }
            try { releasePin.VerifyOrPin(configUrl, installedVersionCode, trustedCatalogRoot, gate); }
            catch (ApprovedReleasePinException) { PinConfirmationRequired = true; throw; }
            approvedGate = gate;
            sessionBoundGate = true;
            return gate;
        }

        public void InvalidateSessionAuthorization()
        {
            authorization?.Invalidate(); admissionGeneration++; approvedGate = null; sessionBoundGate = false;
            ReleaseOwnedLocator(); bundles.Clear(); ContentRevision = null;
        }
    }
}
