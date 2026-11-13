using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Diagnostics;
using UnityEngine.Networking;

namespace LastRail.Resources
{
    public sealed class DevAuthorizedContentClient
    {
        public const string Endpoint = "https://dev-api.lastrail.app/v1/client-config/authorized";
        private const string CatalogRoot = "https://dev-content.lastrail.app/android/releases/";

        public static void ValidateEndpoint(string endpoint)
        {
            if (!string.Equals(endpoint, Endpoint, StringComparison.Ordinal))
                throw new InvalidOperationException("Credential destination is not the approved exact DEV endpoint.");
        }

        public static GameApiAuthSessionSnapshot ValidateDedicatedSession(PlayerAuthOwner owner, string dedicatedAccountId, DateTime utcNow)
        {
            if (owner == null || !owner.Ready || string.IsNullOrWhiteSpace(dedicatedAccountId))
                throw new InvalidOperationException("A prepared dedicated DEV session is required.");
            var session = owner.Session;
            if (session == null || !string.Equals(session.AccountId, dedicatedAccountId, StringComparison.Ordinal)
                || session.AccountStatus != "ACTIVE" || session.AccessTokenExpiresAt <= utcNow)
                throw new InvalidOperationException("Dedicated DEV session ownership or lifetime is invalid.");
            foreach (var value in session.AccessToken.Value)
                if (char.IsWhiteSpace(value) || char.IsControl(value))
                    throw new InvalidOperationException("Dedicated DEV session token format is invalid.");
            return session;
        }

        public async Task<ClientContentGate> FetchAsync(PlayerAuthOwner owner, string dedicatedAccountId, CancellationToken cancel)
        {
            if (ClientEnvironmentBootstrap.CurrentEnvironment != ClientEnvironment.Dev)
                throw new InvalidOperationException("Authorized QA is restricted to DEV.");
            ValidateEndpoint(Endpoint);
            var session = ValidateDedicatedSession(owner, dedicatedAccountId, DateTime.UtcNow);
            var generation = owner.Generation;
            var installed = AppBuildSnapshot.CaptureInstalled().AppVersionCode;
            var code = installed?.ToString(CultureInfo.InvariantCulture);
            ClientAdmissionContract.VersionCode(code);
            cancel.ThrowIfCancellationRequested();
            using (var request = UnityWebRequest.Get(Endpoint))
            {
                request.timeout = 20;
                request.redirectLimit = 0;
                request.SetRequestHeader("X-LastRail-App-Version-Code", code);
                request.SetRequestHeader("Authorization", "Bearer " + session.AccessToken.Value);
                var invalidated = false;
                Action changed = () =>
                {
                    if (!owner.Ready || owner.Generation != generation || !ReferenceEquals(owner.Session, session))
                    { invalidated = true; request.Abort(); }
                };
                owner.Changed += changed;
                try
                {
                    var operation = request.SendWebRequest();
                    using (cancel.Register(request.Abort))
                        while (!operation.isDone)
                        {
                            cancel.ThrowIfCancellationRequested();
                            if (invalidated) throw new OperationCanceledException();
                            await Task.Yield();
                        }
                    cancel.ThrowIfCancellationRequested();
                    if (invalidated || !owner.Ready || owner.Generation != generation || !ReferenceEquals(owner.Session, session))
                        throw new OperationCanceledException();
                    ValidateDedicatedSession(owner, dedicatedAccountId, DateTime.UtcNow);
                    if (request.result != UnityWebRequest.Result.Success && request.responseCode != 426)
                        throw new InvalidOperationException("DEV content authorization was rejected.");
                    return ClientAdmissionContract.Parse(request.downloadHandler.text, code,
                        ClientEnvironmentBootstrap.DevClientConfigUrl, CatalogRoot);
                }
                catch (OperationCanceledException) { throw; }
                catch { throw new InvalidOperationException("DEV content authorization failed; response details are redacted."); }
                finally { owner.Changed -= changed; }
            }
        }
    }
}
