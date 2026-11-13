using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace LastRail.Resources
{
    public sealed class TesterContentForbiddenException : InvalidOperationException
    {
        public TesterContentForbiddenException() : base("This account is not authorized for this internal release.") { }
    }
    public sealed class ContentSessionExpiredException : InvalidOperationException
    {
        public ContentSessionExpiredException() : base("The authenticated content session has expired.") { }
    }

    // Borrows the auth owner's session. A response is publishable only to the same live request/session.
    public sealed class SessionContentAuthorization : IDisposable
    {
        readonly string source, code, catalogRoot;
        readonly Func<GameApiAuthSessionSnapshot> session;
        readonly Func<long> generation;
        readonly Func<DateTime> now;
        readonly Func<string, string, string, CancellationToken, Task<RunApiHttpResponse>> send;
        CancellationTokenSource active, expiry;
        public event Action SessionExpired;
        long sequence, approvedGeneration;
        GameApiAuthSessionSnapshot approvedSession;
        GameApiAuthSessionSnapshot requestSession;
        long requestGeneration;
        bool disposed;

        public SessionContentAuthorization(string source, string code, string catalogRoot,
            Func<GameApiAuthSessionSnapshot> session, Func<long> generation,
            Func<string, string, string, CancellationToken, Task<RunApiHttpResponse>> send = null, Func<DateTime> now = null)
        {
            using (ClientAdmissionContract.CreateRequest(source, code)) { }
            ClientAdmissionContract.ValidateCatalogRoot(catalogRoot);
            this.source = source; this.code = code; this.catalogRoot = catalogRoot;
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.generation = generation ?? throw new ArgumentNullException(nameof(generation));
            this.send = send ?? Send; this.now = now ?? (() => DateTime.UtcNow);
        }

        public bool HasAuthorization => approvedSession != null;
        public bool HasSessionWork => active != null || HasAuthorization;
        public bool SessionWorkCurrent => active == null ? AuthorizationCurrent : !disposed &&
            ReferenceEquals(requestSession, session()) && requestGeneration == generation() && Valid(requestSession);
        public bool AuthorizationCurrent => !disposed && approvedSession != null &&
            ReferenceEquals(approvedSession, session()) && approvedGeneration == generation() && Valid(approvedSession);

        bool Valid(GameApiAuthSessionSnapshot value) => value != null && value.AccountStatus == "ACTIVE" && value.AccessTokenExpiresAt > now();

        public async Task<ClientContentGate> FetchAsync(CancellationToken cancel)
        {
            if (disposed) throw new ObjectDisposedException(nameof(SessionContentAuthorization));
            Invalidate();
            var requestId = sequence; var owner = session(); var ownerGeneration = generation();
            if (owner != null && owner.AccessTokenExpiresAt <= now()) throw new ContentSessionExpiredException();
            if (!Valid(owner)) throw new InvalidOperationException("A current authenticated session is required.");
            foreach (var character in owner.AccessToken.Value)
                if (char.IsWhiteSpace(character) || char.IsControl(character)) throw new InvalidOperationException("Invalid session token format.");
            var request = CancellationTokenSource.CreateLinkedTokenSource(cancel); active = request;
            requestSession = owner; requestGeneration = ownerGeneration;
            ScheduleExpiry(owner, requestId);
            try
            {
                request.Token.ThrowIfCancellationRequested();
                var response = await send(source + "/authorized", owner.AccessToken.Value, code, request.Token);
                request.Token.ThrowIfCancellationRequested();
                if (disposed || sequence != requestId || !ReferenceEquals(owner, session()) || ownerGeneration != generation())
                    throw new OperationCanceledException();
                if (owner.AccessTokenExpiresAt <= now()) throw new ContentSessionExpiredException();
                if (!Valid(owner)) throw new InvalidOperationException("Content session is no longer active.");
                if (response == null || response.IsNetworkError) throw new InvalidOperationException("Content authorization request failed.");
                var gate = ParseResponse(response.StatusCode, response.BodyJson, code, source, catalogRoot);
                if (gate.Evaluate(null) == ContentStage.Initializing)
                { approvedSession = owner; approvedGeneration = ownerGeneration; }
                return gate;
            }
            finally
            {
                if (ReferenceEquals(active, request))
                {
                    active = null; requestSession = null;
                    if (!HasAuthorization) CancelExpiry();
                }
                request.Dispose();
            }
        }

        private async void ScheduleExpiry(GameApiAuthSessionSnapshot owner, long requestId)
        {
            var scheduled = new CancellationTokenSource();
            expiry = scheduled;
            try
            {
                // UTC remains authoritative after suspension or a wall-clock correction.
                while (!scheduled.IsCancellationRequested && sequence == requestId && HasSessionWork)
                {
                    var remaining = owner.AccessTokenExpiresAt - now();
                    if (remaining <= TimeSpan.Zero)
                    {
                        Invalidate();
                        SessionExpired?.Invoke();
                        return;
                    }
                    await Task.Delay((int)Math.Min(int.MaxValue - 1, Math.Max(1, Math.Ceiling(remaining.TotalMilliseconds))), scheduled.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (ReferenceEquals(expiry, scheduled)) expiry = null;
                scheduled.Dispose();
            }
        }
        private void CancelExpiry()
        {
            var scheduled = expiry; expiry = null;
            scheduled?.Cancel();
        }
        public void Invalidate() { sequence++; CancelExpiry(); active?.Cancel(); approvedSession = null; }
        public void Dispose() { disposed = true; Invalidate(); }

        public static ClientContentGate ParseResponse(int httpStatus, string json, string code, string source, string catalogRoot)
        {
            using (ClientAdmissionContract.CreateRequest(source, code)) { }
            ClientAdmissionContract.ValidateCatalogRoot(catalogRoot);
            if (httpStatus == 403)
            {
                bool denied;
                try
                {
                    var root = GameApiJson.ReadObject(json); var admission = GameApiJson.Object(root, "clientAdmission");
                    var bootstrap = GameApiJson.Object(root, "bootstrap"); var content = GameApiJson.Object(root, "contentRelease");
                    ClientAdmissionContract.ValidateRegistryRevision(GameApiJson.String(root, "registryRevision"));
                    denied = !GameApiJson.Boolean(root, "accepted") && GameApiJson.String(root, "errorCode") == "tester_forbidden"
                        && GameApiJson.String(admission, "status") == "TESTER_FORBIDDEN" && GameApiJson.String(admission, "distribution") == "INTERNAL"
                        && GameApiJson.String(admission, "installedVersionCode") == code && !GameApiJson.Boolean(admission, "updateRequired")
                        && ClientAdmissionContract.VersionCode(GameApiJson.String(admission, "minimumVersionCode")) <= ClientAdmissionContract.VersionCode(code)
                        && GameApiJson.Boolean(bootstrap, "canAuthenticate") && !GameApiJson.Boolean(bootstrap, "contentAuthorized")
                        && GameApiJson.Boolean(bootstrap, "authenticationRequired");
                    foreach (var field in new[] { "releaseId", "contentVersion", "catalogUrl", "catalogHash" })
                        denied &= content[field] == null || content[field].Type == JTokenType.Null;
                }
                catch { throw new InvalidOperationException("Invalid content authorization rejection."); }
                if (denied) throw new TesterContentForbiddenException();
                throw new InvalidOperationException("Content authorization was rejected.");
            }
            if (httpStatus != 200 && httpStatus != 426) throw new InvalidOperationException("Content authorization request failed.");
            var gate = ClientAdmissionContract.Parse(json, code, source, catalogRoot);
            if (gate.Evaluate(null) == ContentStage.AwaitingAuthentication || (httpStatus == 426 && gate.Evaluate(null) != ContentStage.UpdateRequired))
                throw new InvalidOperationException("Authenticated response did not authorize content.");
            return gate;
        }

        static async Task<RunApiHttpResponse> Send(string endpoint, string token, string code, CancellationToken cancel)
        {
            using (var request = UnityWebRequest.Get(endpoint))
            {
                request.timeout = 20; request.redirectLimit = 0;
                request.SetRequestHeader("Authorization", "Bearer " + token);
                request.SetRequestHeader("X-LastRail-App-Version-Code", code);
                var operation = request.SendWebRequest();
                using (cancel.Register(request.Abort))
                    while (!operation.isDone) { cancel.ThrowIfCancellationRequested(); await Task.Yield(); }
                cancel.ThrowIfCancellationRequested();
                if (request.responseCode <= 0 || string.IsNullOrWhiteSpace(request.downloadHandler.text))
                    throw new InvalidOperationException("Content authorization request failed.");
                return RunApiHttpResponse.Success((int)request.responseCode, request.downloadHandler.text);
            }
        }
    }
}
