using System;
using System.Globalization;
using System.Text.RegularExpressions;
using LastRail.Auth;
using LastRail.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace LastRail.Resources
{
    public static class ClientAdmissionContract
    {
        public static UnityWebRequest CreateRequest(string sourceUrl, string installedVersionCode)
        {
            ValidateSource(sourceUrl);
            return CreateValidatedRequest(sourceUrl, installedVersionCode);
        }

        private static UnityWebRequest CreateValidatedRequest(string sourceUrl, string installedVersionCode)
        {
            VersionCode(installedVersionCode);
            var request = UnityWebRequest.Get(sourceUrl);
            request.timeout = 20;
            request.redirectLimit = 0;
            request.SetRequestHeader("X-LastRail-App-Version-Code", installedVersionCode);
            return request;
        }

        public static ClientContentGate Parse(string json, string installedVersionCode, string sourceUrl, string trustedCatalogRoot)
        {
            ValidateSource(sourceUrl);
            ValidateCatalogRoot(trustedCatalogRoot);
            return ParseValidated(json, installedVersionCode, url => ValidateCatalogUrl(url, trustedCatalogRoot));
        }

#if UNITY_EDITOR
        internal static UnityWebRequest CreateFixtureRequest(EditorLoopbackContentFixture fixture) =>
            CreateValidatedRequest(fixture.ConfigUrl, fixture.InstalledVersionCode);

        internal static ClientContentGate ParseFixture(string json, EditorLoopbackContentFixture fixture) =>
            ParseValidated(json, fixture.InstalledVersionCode, fixture.ValidateContentUrl);
#endif

        private static ClientContentGate ParseValidated(string json, string installedVersionCode, Action<string> validateCatalogUrl)
        {
            var installed = VersionCode(installedVersionCode);
            try
            {
                var root = GameApiJson.ReadObject(json);
                var admission = GameApiJson.Object(root, "clientAdmission");
                var accepted = GameApiJson.Boolean(root, "accepted");
                var status = GameApiJson.String(admission, "status");
                var update = GameApiJson.Boolean(admission, "updateRequired");
                var distribution = GameApiJson.String(admission, "distribution", true);
                var revision = GameApiJson.String(root, "registryRevision", true);
                if (!accepted)
                {
                    if (status == "AUTH_REQUIRED" && distribution == "INTERNAL" && !update &&
                        GameApiJson.String(admission, "installedVersionCode") == installedVersionCode &&
                        VersionCode(GameApiJson.String(admission, "minimumVersionCode")) <= installed &&
                        GameApiJson.String(root, "errorCode") == "authentication_required")
                    {
                        ValidateRegistryRevision(revision);
                        var bootstrap = GameApiJson.Object(root, "bootstrap");
                        if (!GameApiJson.Boolean(bootstrap, "canAuthenticate") || GameApiJson.Boolean(bootstrap, "contentAuthorized")
                            || !GameApiJson.Boolean(bootstrap, "authenticationRequired"))
                            throw new InvalidOperationException("Internal bootstrap approval is inconsistent.");
                        var withheld = GameApiJson.Object(root, "contentRelease");
                        foreach (var field in new[] { "releaseId", "contentVersion", "catalogUrl", "catalogHash" })
                            if (withheld[field] != null && withheld[field].Type != JTokenType.Null)
                                throw new InvalidOperationException("Pre-auth internal response contains content metadata.");
                        return ClientContentGate.AwaitingLogin(GameApiJson.Boolean(GameApiJson.Object(root, "maintenance"), "enabled"), revision);
                    }
                    // A rejected response never carries a usable content plan.
                    if (status == "UPDATE_REQUIRED" && update &&
                        GameApiJson.String(admission, "installedVersionCode") == installedVersionCode &&
                        VersionCode(GameApiJson.String(admission, "minimumVersionCode")) > installed)
                        return ClientContentGate.RejectedUpdate();
                    throw new InvalidOperationException("Client admission was rejected.");
                }
                if (status != "ACCEPTED" || update ||
                    GameApiJson.String(admission, "installedVersionCode") != installedVersionCode ||
                    VersionCode(GameApiJson.String(admission, "minimumVersionCode")) > installed)
                    throw new InvalidOperationException("Client admission is inconsistent.");
                if (root["errorCode"] != null && root["errorCode"].Type != JTokenType.Null)
                    throw new InvalidOperationException("Accepted admission contains an error.");
                if (revision != null || distribution != null)
                {
                    ValidateRegistryRevision(revision);
                    if (distribution != "INTERNAL" && distribution != "STABLE") throw new InvalidOperationException("Invalid release distribution.");
                    var bootstrap = GameApiJson.Object(root, "bootstrap");
                    if (!GameApiJson.Boolean(bootstrap, "canAuthenticate") || !GameApiJson.Boolean(bootstrap, "contentAuthorized")
                        || GameApiJson.Boolean(bootstrap, "authenticationRequired"))
                        throw new InvalidOperationException("Content authorization is inconsistent.");
                }

                var release = GameApiJson.Object(root, "contentRelease");
                if (GameApiJson.String(release, "status") != "APPROVED_COMPATIBLE")
                    throw new InvalidOperationException("Content release is not approved and compatible.");
                var compatibility = GameApiJson.Object(release, "compatibility");
                var minimum = VersionCode(GameApiJson.String(compatibility, "minimumVersionCode"));
                var maximumText = GameApiJson.String(compatibility, "maximumVersionCode", true);
                var maximum = maximumText == null ? int.MaxValue : VersionCode(maximumText);
                if (minimum > maximum || installed < minimum || installed > maximum)
                    throw new InvalidOperationException("Content release is incompatible.");
                var releaseId = SafeId(GameApiJson.String(release, "releaseId"));
                var contentVersion = SafeId(GameApiJson.String(release, "contentVersion"));
                var catalogUrl = GameApiJson.String(release, "catalogUrl");
                validateCatalogUrl(catalogUrl);
                var hash = GameApiJson.String(release, "catalogHash");
                if (!Regex.IsMatch(hash, "\\Asha256:[a-f0-9]{64}\\z", RegexOptions.CultureInvariant))
                    throw new InvalidOperationException("Unsupported content catalog hash.");
                var maintenance = GameApiJson.Boolean(GameApiJson.Object(root, "maintenance"), "enabled");
                var gate = ClientContentGate.Approved(releaseId, contentVersion, catalogUrl, hash, maintenance);
                gate.RegistryManaged = revision != null;
                gate.RegistryRevision = revision;
                return gate;
            }
            catch (JsonException error) { throw new InvalidOperationException("Invalid client admission JSON.", error); }
            catch (FormatException error) { throw new InvalidOperationException("Invalid client admission fields.", error); }
        }

        internal static void ValidateRegistryRevision(string revision)
        {
            if (revision == null || revision.Length < 3 || revision.Length > 256 || revision[0] != '"'
                || revision[revision.Length - 1] != '"') throw new InvalidOperationException("Registry revision is missing.");
            for (var i = 1; i < revision.Length - 1; i++)
                if (revision[i] < 33 || revision[i] > 126 || revision[i] == '"')
                    throw new InvalidOperationException("Registry revision is invalid.");
        }

        internal static int VersionCode(string value)
        {
            if (value == null || !Regex.IsMatch(value, "\\A[1-9][0-9]{0,9}\\z", RegexOptions.CultureInvariant) ||
                !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var code))
                throw new InvalidOperationException("Installed app version code is required and must be canonical.");
            return code;
        }

        private static string SafeId(string value)
        {
            if (!Regex.IsMatch(value, "\\A[A-Za-z0-9][A-Za-z0-9._:-]{0,127}\\z", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Invalid content release identity.");
            return value;
        }

        private static void ValidateSource(string source)
        {
            if (source != ClientEnvironmentBootstrap.DevClientConfigUrl && source != ClientEnvironmentBootstrap.PrdClientConfigUrl)
                throw new InvalidOperationException("Untrusted client admission endpoint.");
        }

        internal static void ValidateCatalogRoot(string root)
        {
            var uri = HttpsUri(root);
            if (!uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
                throw new InvalidOperationException("Trusted catalog root must be an explicit directory URL.");
        }

        internal static void ValidateCatalogUrl(string value, string root)
        {
            ValidateCatalogRoot(root);
            var catalog = HttpsUri(value);
            var approvedRoot = HttpsUri(root);
            if (catalog.GetLeftPart(UriPartial.Authority) != approvedRoot.GetLeftPart(UriPartial.Authority) ||
                !catalog.AbsolutePath.StartsWith(approvedRoot.AbsolutePath, StringComparison.Ordinal) ||
                catalog.AbsolutePath == approvedRoot.AbsolutePath)
                throw new InvalidOperationException("Content URL is outside the trusted release root.");
        }

        private static Uri HttpsUri(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.IndexOf('\\') >= 0 ||
                value.IndexOf('%') >= 0 || !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException("Content URL must be an unambiguous HTTPS URL.");
            return uri;
        }
    }
}
