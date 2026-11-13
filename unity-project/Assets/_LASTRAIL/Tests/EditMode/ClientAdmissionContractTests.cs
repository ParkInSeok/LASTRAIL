using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine.Networking;

namespace LastRail.Tests.EditMode
{
    // Reflection keeps the RED suite compilable before the runtime contract exists.
    // The catalog root represents trusted build configuration, never response data.
    public sealed class ClientAdmissionContractTests
    {
        private const string ContractType = "LastRail.Resources.ClientAdmissionContract";
        private const string Source = "https://api.lastrail.app/v1/client-config";
        private const string CatalogRoot = "https://content.lastrail.test/releases/r42/";
        private const string Catalog = CatalogRoot + "catalog.json";
        private const string Hash = "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string AcceptedJson = @"{
            ""environment"":""PRD"",
            ""apiBaseUrl"":""https://api.lastrail.app"",
            ""balanceConfigEndpointTemplate"":""/v1/balance-config/:version"",
            ""minimumClientVersion"":""0.1.0"",
            ""maintenance"": {""enabled"":false},
            ""accepted"":true,
            ""serverVersion"": {""version"":""0.4.2"",""versionCode"":""42"",""minimumVersionCode"":""40""},
            ""clientAdmission"": {
                ""status"":""ACCEPTED"",""installedVersionCode"":""42"",
                ""minimumVersionCode"":""40"",""updateRequired"":false,
                ""platform"":""android"",""channel"":""internal""
            },
            ""contentRelease"": {
                ""status"":""APPROVED_COMPATIBLE"",""releaseId"":""r42"",""contentVersion"":""content-42"",
                ""catalogUrl"":""https://content.lastrail.test/releases/r42/catalog.json"",
                ""catalogHash"":""sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"",
                ""compatibility"": {""minimumVersionCode"":""40"",""maximumVersionCode"":""50""}
            },
            ""errorCode"":null,""message"":null
        }";

        [TestCase("1")]
        [TestCase("42")]
        [TestCase("2147483647")]
        public void RequestUsesExactInstalledDecimalCodeInServerHeader(string code)
        {
            using (var request = CreateRequest(Source, code))
            {
                Assert.That(request.method, Is.EqualTo("GET"));
                Assert.That(request.url, Is.EqualTo(Source));
                Assert.That(request.GetRequestHeader("X-LastRail-App-Version-Code"), Is.EqualTo(code));
                Assert.That(request.redirectLimit, Is.Zero, "Admission must not silently follow an untrusted redirect.");
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("+42")]
        [TestCase("042")]
        [TestCase("42.0")]
        [TestCase("0.1.0")]
        [TestCase("4e1")]
        [TestCase(" 42 ")]
        [TestCase("42\r\nX-Other: value")]
        [TestCase("2147483648")]
        [TestCase("999999999999999999999")]
        public void MissingOrInvalidInstalledCodeCannotBeReplacedWithAnEditorOrRuntimeDefault(string code)
        {
            var method = Method("CreateRequest", typeof(string), typeof(string));
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var request = (UnityWebRequest)Invoke(method, Source, code)) { }
            });
            AssertBlocked(AcceptedJson, code);
        }

        [Test]
        public void ExactServerResponseProducesApprovedPlanAndDisablesAllCatalogUpdates()
        {
            var gate = Parse(AcceptedJson);
            Assert.That(gate.Evaluate("0.1.0"), Is.EqualTo(ContentStage.Initializing));
            Assert.That(Member(gate, "ApprovedReleaseId"), Is.EqualTo("r42"));
            Assert.That(Member(gate, "ApprovedContentVersion"), Is.EqualTo("content-42"));
            Assert.That(Member(gate, "ApprovedCatalogUrl"), Is.EqualTo(Catalog));
            Assert.That(Member(gate, "ApprovedCatalogHash"), Is.EqualTo(Hash));
            Assert.That(Member(gate, "AllowAutomaticCatalogUpdates"), Is.EqualTo(false));
        }

        [TestCase("40")]
        [TestCase("50")]
        public void CompatibilityBoundsAreInclusive(string installed)
        {
            var json = AcceptedJson.Replace("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":\"" + installed + "\"");
            Assert.That(Parse(json, installed).Evaluate("0.1.0"), Is.EqualTo(ContentStage.Initializing));
        }

        [Test]
        public void NullMaximumCompatibilityCodeMeansNoUpperBound()
        {
            var json = AcceptedJson.Replace("\"maximumVersionCode\":\"50\"", "\"maximumVersionCode\":null")
                .Replace("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":\"2147483647\"");
            Assert.That(Parse(json, "2147483647").Evaluate("0.1.0"), Is.EqualTo(ContentStage.Initializing));
        }

        [TestCase("39")]
        [TestCase("51")]
        public void ClaimedAcceptanceCannotOverrideCompatibilityBounds(string installed)
        {
            AssertBlocked(AcceptedJson.Replace("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":\"" + installed + "\""), installed);
        }

        [Test]
        public void EchoedCodeMustMatchTheInstalledCodeUsedForTheRequest()
        {
            AssertBlocked(AcceptedJson, "43");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("{}")]
        [TestCase("{\"minimumClientVersion\":\"0.1.0\",\"maintenance\":{\"enabled\":false}}")]
        public void MalformedAndLegacyOnlyResponsesFailClosed(string json)
        {
            AssertBlocked(json);
        }

        [TestCase("\"accepted\":true,", "")]
        [TestCase("\"accepted\":true", "\"accepted\":false")]
        [TestCase("\"accepted\":true", "\"accepted\":\"true\"")]
        [TestCase("\"accepted\":true", "\"accepted\":1")]
        [TestCase("\"accepted\":true", "\"accepted\":null")]
        [TestCase("\"status\":\"ACCEPTED\"", "\"status\":\"UPDATE_REQUIRED\"")]
        [TestCase("\"status\":\"ACCEPTED\",", "")]
        [TestCase("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":42")]
        [TestCase("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":null")]
        [TestCase("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":\"042\"")]
        [TestCase("\"minimumVersionCode\":\"40\"", "\"minimumVersionCode\":\"43\"")]
        [TestCase("\"minimumVersionCode\":\"40\"", "\"minimumVersionCode\":\"0\"")]
        [TestCase("\"minimumVersionCode\":\"40\"", "\"minimumVersionCode\":40")]
        [TestCase("\"updateRequired\":false", "\"updateRequired\":true")]
        [TestCase("\"updateRequired\":false,", "")]
        [TestCase("\"updateRequired\":false", "\"updateRequired\":\"false\"")]
        [TestCase("\"clientAdmission\":", "\"ignoredAdmission\":")]
        [TestCase("\"contentRelease\":", "\"ignoredRelease\":")]
        [TestCase("APPROVED_COMPATIBLE", "APPROVED")]
        [TestCase("APPROVED_COMPATIBLE", "NOT_APPROVED")]
        [TestCase("APPROVED_COMPATIBLE", "INCOMPATIBLE")]
        [TestCase("APPROVED_COMPATIBLE", "UNCONFIGURED")]
        [TestCase("\"releaseId\":\"r42\"", "\"releaseId\":\"\"")]
        [TestCase("\"contentVersion\":\"content-42\"", "\"contentVersion\":null")]
        [TestCase("\"catalogHash\":", "\"ignoredHash\":")]
        [TestCase("sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "0123456789abcdef0123456789abcdef")]
        [TestCase("\"compatibility\":", "\"ignoredCompatibility\":")]
        [TestCase("\"maximumVersionCode\":\"50\"", "\"maximumVersionCode\":\"39\"")]
        [TestCase("\"maximumVersionCode\":\"50\"", "\"maximumVersionCode\":\"2147483648\"")]
        [TestCase("\"maximumVersionCode\":\"50\"", "\"maximumVersionCode\":50")]
        public void IncompleteOrContradictoryAdmissionCannotUseLegacySemverAsFallback(string before, string after)
        {
            Assert.That(AcceptedJson, Does.Contain(before), "Fixture mutation must change the input.");
            AssertBlocked(AcceptedJson.Replace(before, after));
        }

        [TestCase("https://evil.example/v1/client-config")]
        [TestCase("https://api.lastrail.app.evil.example/v1/client-config")]
        [TestCase("https://api.lastrail.app@evil.example/v1/client-config")]
        [TestCase("https://user:secret@api.lastrail.app/v1/client-config")]
        [TestCase("http://api.lastrail.app/v1/client-config")]
        [TestCase("https://api.lastrail.app:444/v1/client-config")]
        [TestCase("https://api.lastrail.app/other")]
        [TestCase("file:///tmp/client-config.json")]
        [TestCase("/v1/client-config")]
        public void UntrustedResponseSourceCannotAuthorizeContentEvenWithTrustedLookingPayload(string source)
        {
            AssertBlocked(AcceptedJson, source: source);
            var method = Method("CreateRequest", typeof(string), typeof(string));
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var request = (UnityWebRequest)Invoke(method, source, "42")) { }
            });
        }

        [TestCase("https://evil.example/catalog.json")]
        [TestCase("https://content.lastrail.test.evil.example/releases/r42/catalog.json")]
        [TestCase("https://content.lastrail.test@evil.example/releases/r42/catalog.json")]
        [TestCase("https://user:secret@content.lastrail.test/releases/r42/catalog.json")]
        [TestCase("https://content.lastrail.test:444/releases/r42/catalog.json")]
        [TestCase("http://content.lastrail.test/releases/r42/catalog.json")]
        [TestCase("https://content.lastrail.test/releases/r43/catalog.json")]
        [TestCase("https://content.lastrail.test/releases/r42-evil/catalog.json")]
        [TestCase("https://content.lastrail.test/releases/r42/../r43/catalog.json")]
        [TestCase("https://content.lastrail.test/releases/r42/%2e%2e/r43/catalog.json")]
        [TestCase("file:///tmp/catalog.json")]
        [TestCase("//evil.example/catalog.json")]
        [TestCase("catalog.json")]
        [TestCase("")]
        public void ApprovedLabelDoesNotAuthorizeCatalogOutsideTrustedReleaseRoot(string catalog)
        {
            AssertBlocked(AcceptedJson.Replace(Catalog, catalog));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("http://content.lastrail.test/releases/r42/")]
        public void MissingOrInsecureTrustedCatalogConfigurationFailsClosed(string root)
        {
            AssertBlocked(AcceptedJson, root: root);
        }

        [TestCase("APP_VERSION_REQUIRED", "UNCONFIGURED", "app_version_required", true)]
        [TestCase("VERSION_POLICY_UNCONFIGURED", "UNCONFIGURED", "version_policy_unconfigured", true)]
        [TestCase("UPDATE_REQUIRED", "INCOMPATIBLE", "client_update_required", true)]
        [TestCase("ACCEPTED", "UNCONFIGURED", "content_release_unconfigured", false)]
        [TestCase("ACCEPTED", "INCOMPATIBLE", "content_release_incompatible", false)]
        [TestCase("ACCEPTED", "NOT_APPROVED", "content_release_not_approved", false)]
        public async Task ServerRejectionsStopBeforeAnyAddressablesWork(string status, string releaseStatus, string error, bool update)
        {
            var installed = status == "APP_VERSION_REQUIRED" ? "null" : "\"42\"";
            var minimum = status == "VERSION_POLICY_UNCONFIGURED" ? "null" : "\"43\"";
            if (!update) minimum = "\"40\"";
            var json = "{\"accepted\":false,\"minimumClientVersion\":\"0.1.0\",\"maintenance\":{\"enabled\":false}," +
                "\"clientAdmission\":{\"status\":\"" + status + "\",\"installedVersionCode\":" + installed +
                ",\"minimumVersionCode\":" + minimum + ",\"updateRequired\":" + (update ? "true" : "false") + "}," +
                "\"contentRelease\":{\"status\":\"" + releaseStatus + "\",\"releaseId\":null,\"contentVersion\":null," +
                "\"catalogUrl\":null,\"catalogHash\":null,\"compatibility\":null},\"errorCode\":\"" + error + "\",\"message\":\"Unavailable.\"}";
            await AssertFlowBlocked(json);
        }

        [Test]
        public async Task RejectedEnvelopeWithOtherwiseValidReleaseNeverInitializesAddressables()
        {
            await AssertFlowBlocked(AcceptedJson.Replace("\"accepted\":true", "\"accepted\":false"));
        }

        [Test]
        public async Task AcceptedMaintenanceResponseNeverInitializesAddressables()
        {
            await AssertFlowBlocked(AcceptedJson.Replace("\"enabled\":false", "\"enabled\":true"));
        }

        private static MethodInfo Method(string name, params Type[] parameters)
        {
            var type = typeof(ClientContentGate).Assembly.GetType(ContractType);
            Assert.That(type, Is.Not.Null, "RED: implement " + ContractType + " in LastRail.Runtime.");
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            Assert.That(method, Is.Not.Null, "Missing proposed public static API: " + name);
            return method;
        }

        private static object Invoke(MethodInfo method, params object[] args)
        {
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException error) when (error.InnerException != null) { throw error.InnerException; }
        }

        private static UnityWebRequest CreateRequest(string source, string installed)
        {
            return (UnityWebRequest)Invoke(Method("CreateRequest", typeof(string), typeof(string)), source, installed);
        }

        private static ClientContentGate Parse(string json, string installed = "42", string source = Source, string root = CatalogRoot)
        {
            return (ClientContentGate)Invoke(Method("Parse", typeof(string), typeof(string), typeof(string), typeof(string)), json, installed, source, root);
        }

        private static object Member(object value, string name)
        {
            var type = value.GetType();
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.GetValue(value);
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "Missing approved-plan member: " + name);
            return field.GetValue(value);
        }

        private static void AssertBlocked(string json, string installed = "42", string source = Source, string root = CatalogRoot)
        {
            ContentStage stage;
            try { stage = Parse(json, installed, source, root).Evaluate("0.1.0"); }
            catch (InvalidOperationException) { return; }
            Assert.That(stage, Is.EqualTo(ContentStage.Failed).Or.EqualTo(ContentStage.UpdateRequired).Or.EqualTo(ContentStage.Maintenance));
        }

        private static async Task AssertFlowBlocked(string json)
        {
            // Resolve before entering the flow so missing-API assertions cannot become a false pass.
            var parser = Method("Parse", typeof(string), typeof(string), typeof(string), typeof(string));
            var backend = new AdmissionBackend(() => (ClientContentGate)Invoke(parser, json, "42", Source, CatalogRoot));
            var flow = new ContentStartupFlow();
            var consentCalls = 0;
            await flow.RunAsync(backend, "0.1.0", (bytes, cancel) =>
            {
                consentCalls++;
                return Task.FromResult(true);
            }, CancellationToken.None);
            Assert.That(flow.CanEnterGame, Is.False);
            Assert.That(flow.Stage, Is.EqualTo(ContentStage.Failed).Or.EqualTo(ContentStage.UpdateRequired).Or.EqualTo(ContentStage.Maintenance));
            Assert.That(backend.Calls, Is.EqualTo(new[] { "config" }));
            Assert.That(consentCalls, Is.Zero);
        }

        private sealed class AdmissionBackend : IContentStartupBackend
        {
            private readonly Func<ClientContentGate> fetch;
            public readonly List<string> Calls = new List<string>();
            public AdmissionBackend(Func<ClientContentGate> fetch) { this.fetch = fetch; }
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel)
            {
                Calls.Add("config");
                return Task.FromResult(fetch());
            }
            private Task Record(string name) { Calls.Add(name); return Task.CompletedTask; }
            public Task InitializeAsync(CancellationToken cancel) => Record("initialize");
            public Task UpdateCatalogAsync(CancellationToken cancel) => Record("catalog");
            public Task<long> RequiredBytesAsync(CancellationToken cancel) { Calls.Add("size"); return Task.FromResult(1L); }
            public Task DownloadAsync(Action<long, long> progress, CancellationToken cancel) => Record("download");
            public Task VerifyRequiredAssetsAsync(CancellationToken cancel) => Record("verify");
        }
    }
}
