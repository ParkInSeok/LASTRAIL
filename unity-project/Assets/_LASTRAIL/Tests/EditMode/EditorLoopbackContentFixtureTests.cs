#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Tests.EditMode
{
    // Phase-one RED contract: reflection permits compilation before the Editor API exists.
    // No request is sent, Addressables initialized, or server started by this suite.
    public sealed class EditorLoopbackContentFixtureTests
    {
        private const string Source = "http://127.0.0.1:18284/client-config.json";
        private const string Root = "http://127.0.0.1:18284/LOCAL_TEST/Windows/";
        private const string Digest = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ProductionAdmissionAndCatalogPathsRemainClosedToLoopback()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (ClientAdmissionContract.CreateRequest(Source, "42")) { }
            });
            Assert.Throws<InvalidOperationException>(() => ClientAdmissionContract.Parse(Json(), "42", Source, Root));
            Assert.Throws<InvalidOperationException>(() => ApprovedCatalogSafety.VerifiedCatalogPath(
                Path.GetTempPath(), Root + "catalog.bin", "sha256:" + Digest, false));
            Assert.Throws<InvalidOperationException>(() => ApprovedCatalogSafety.BuildRemoteBundlePlan(
                new IResourceLocation[] { Bundle(Root + "screen.bundle") }, Root, location => location.InternalId));
        }

        [Test]
        public void ProductionRequestStillRequiresExplicitInstalledCode()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (ClientAdmissionContract.CreateRequest("https://api.lastrail.app/v1/client-config", null)) { }
            });
        }

        [Test]
        public void DefaultBackendCannotInitializeBeforeAdmissionOrAfterDisposal()
        {
            using (var backend = new AddressableContentBackend(Source, true))
            {
                Assert.ThrowsAsync<InvalidOperationException>(() => backend.InitializeAsync(CancellationToken.None));
                Assert.ThrowsAsync<InvalidOperationException>(() => backend.UpdateCatalogAsync(CancellationToken.None));
                backend.Dispose();
                Assert.ThrowsAsync<ObjectDisposedException>(() => backend.FetchConfigAsync(CancellationToken.None));
            }
        }

        [Test]
        public void DefaultRuntimePathSelectionStillRejectsAnExistingOverride()
        {
            using (var backend = new AddressableContentBackend(Source, true))
                Assert.Throws<InvalidOperationException>(() => Call(backend, "SelectRuntimeSettingsPath",
                    new[] { typeof(bool) }, true));
        }

        [Test]
        public void FixtureRuntimePathCannotBeRedirectedByAnExistingOverride()
        {
            var key = UnityEngine.AddressableAssets.Addressables.kAddressablesRuntimeDataPath;
            var hadPreference = UnityEngine.PlayerPrefs.HasKey(key);
            var preference = UnityEngine.PlayerPrefs.GetString(key, "");
            var context = Context();
            var factory = Method(typeof(AddressableContentBackend), "CreateForEditorLoopbackFixture", Static,
                FixtureType(), typeof(DownloadAccessPolicy));
            using (var backend = (AddressableContentBackend)Invoke(factory, null, context, new DownloadAccessPolicy()))
            {
                var types = new[] { typeof(bool) };
                var expected = Path.GetFullPath("Library/com.unity.addressables/aa/Windows/settings.json");
                Assert.That(Call(backend, "SelectRuntimeSettingsPath", types, true), Is.EqualTo(expected));
                Assert.That(Call(backend, "SelectRuntimeSettingsPath", types, false), Is.EqualTo(expected));
            }
            Assert.That(UnityEngine.PlayerPrefs.HasKey(key), Is.EqualTo(hadPreference));
            Assert.That(UnityEngine.PlayerPrefs.GetString(key, ""), Is.EqualTo(preference));
        }

        [Test]
        public void ContextIsInternalSealedImmutableAndAbsentFromDefaultConstructor()
        {
            var type = FixtureType();
            Assert.That(type.IsNotPublic, Is.True);
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetConstructors(), Is.Empty);
            foreach (var field in type.GetFields(Instance | BindingFlags.Public))
                Assert.That(field.IsInitOnly, Is.True, field.Name);
            foreach (var property in type.GetProperties(Instance | BindingFlags.Public))
                Assert.That(property.GetSetMethod(true), Is.Null, property.Name);
            foreach (var field in type.GetFields(Static | BindingFlags.Public))
                Assert.That(field.IsLiteral, Is.True, "No shared fixture state: " + field.Name);
            foreach (var constructor in typeof(AddressableContentBackend).GetConstructors())
                Assert.That(constructor.GetParameters().Any(p => p.ParameterType == type), Is.False);
            Assert.That(Method(typeof(AddressableContentBackend), "CreateForEditorLoopbackFixture", Static,
                type, typeof(DownloadAccessPolicy)).ReturnType, Is.EqualTo(typeof(AddressableContentBackend)));
        }

        [TestCase("http://localhost:18284/client-config.json")]
        [TestCase("http://127.0.0.2:18284/client-config.json")]
        [TestCase("http://[::1]:18284/client-config.json")]
        [TestCase("https://api.lastrail.app/v1/client-config")]
        [TestCase("http://127.0.0.1:18285/client-config.json")]
        [TestCase("http://127.0.0.1:18284/client-config.json?override=1")]
        public void FixtureCannotExpandItsBoundAuthority(string source)
        {
            Assert.Throws<InvalidOperationException>(() => Context(source: source));
        }

        [TestCase(null)]
        [TestCase("0")]
        [TestCase("042")]
        [TestCase("0.1.0")]
        public void FixtureRequiresCanonicalVersionCode(string code)
        {
            Assert.Throws<InvalidOperationException>(() => Context(code: code));
        }

        [Test]
        public void FixtureRequestAndParsedApprovalRetainServerIdentityAndHash()
        {
            var context = Context();
            using (var request = (UnityWebRequest)Call(context, "CreateAdmissionRequest", Type.EmptyTypes))
            {
                Assert.That(request.url, Is.EqualTo(Source));
                Assert.That(request.GetRequestHeader("X-LastRail-App-Version-Code"), Is.EqualTo("42"));
                Assert.That(request.redirectLimit, Is.Zero);
            }
            var gate = Parse(context, Json());
            Assert.That(gate.Evaluate(null), Is.EqualTo(ContentStage.Initializing));
            Assert.That(gate.ApprovedReleaseId, Is.EqualTo("LOCAL_TEST"));
            Assert.That(gate.ApprovedContentVersion, Is.EqualTo("local-42"));
            Assert.That(gate.ApprovedCatalogUrl, Is.EqualTo(Root + "catalog.bin"));
            Assert.That(gate.ApprovedCatalogHash, Is.EqualTo("sha256:" + Digest));
            Assert.That(gate.AllowAutomaticCatalogUpdates, Is.False);
            Assert.Throws<InvalidOperationException>(() => ClientAdmissionContract.Parse(Json(), "42", Source, Root));
        }

        [TestCase("\"accepted\":true", "\"accepted\":false")]
        [TestCase("APPROVED_COMPATIBLE", "UNAPPROVED")]
        [TestCase("\"installedVersionCode\":\"42\"", "\"installedVersionCode\":\"41\"")]
        [TestCase("\"minimumVersionCode\":\"40\"", "\"minimumVersionCode\":\"43\"")]
        [TestCase("sha256:", "md5:")]
        [TestCase("127.0.0.1:18284", "127.0.0.1:18285")]
        [TestCase("LOCAL_TEST/Windows/", "LOCAL_TEST/Android/")]
        public void FixtureDoesNotManufactureApproval(string before, string after)
        {
            var context = Context();
            Assert.Throws<InvalidOperationException>(() => Parse(context, Json().Replace(before, after)));
        }

        [Test]
        public void FixtureCatalogPathPreservesProviderFormatAndDigest()
        {
            var context = Context();
            var types = new[] { typeof(string), typeof(string), typeof(bool) };
            var path = (string)Call(context, "VerifiedCatalogPath", types, Root + "catalog.bin", "sha256:" + Digest, false);
            Assert.That(Path.GetFileName(path), Is.EqualTo(Digest + ".bin"));
            Assert.Throws<InvalidOperationException>(() => Call(context, "VerifiedCatalogPath", types,
                Root + "catalog.json", "sha256:" + Digest, false));
            Assert.Throws<InvalidOperationException>(() => Call(context, "VerifiedCatalogPath", types,
                Root + "catalog.bin", "sha256:bad", false));
        }

        [Test]
        public void FixtureGraphChecksTransformedUrlsAndNestedProviders()
        {
            var context = Context();
            var bundle = Bundle(Root + "screen.bundle");
            var roots = new IResourceLocation[] { bundle };
            Func<IResourceLocation, string> identity = location => location.InternalId;
            var types = new[] { typeof(IResourceLocation[]), typeof(Func<IResourceLocation, string>) };
            Assert.That((IResourceLocation[])Call(context, "BuildRemoteBundlePlan", types, roots, identity),
                Is.EqualTo(roots));
            Func<IResourceLocation, string> escaped = location => "http://127.0.0.1:18285/screen.bundle";
            Assert.Throws<InvalidOperationException>(() => Call(context, "BuildRemoteBundlePlan", types, roots, escaped));
            var text = new ResourceLocationBase("text", Root + "config.txt", typeof(TextDataProvider).FullName, typeof(string));
            var screen = new ResourceLocationBase("screen", AddressableContentBackend.LobbyKey,
                typeof(BundledAssetProvider).FullName, typeof(UnityEngine.GameObject), text);
            Assert.Throws<InvalidOperationException>(() => Call(context, "BuildRemoteBundlePlan", types,
                new IResourceLocation[] { screen }, identity));
        }

        [Test]
        public void ExplicitBackendStillNeedsFetchAndHonorsDisposal()
        {
            var context = Context();
            var factory = Method(typeof(AddressableContentBackend), "CreateForEditorLoopbackFixture", Static,
                FixtureType(), typeof(DownloadAccessPolicy));
            using (var backend = (AddressableContentBackend)Invoke(factory, null, context, new DownloadAccessPolicy()))
            {
                Assert.ThrowsAsync<InvalidOperationException>(() => backend.InitializeAsync(CancellationToken.None));
                Assert.That(backend.ContentRevision, Is.Null);
                backend.Dispose();
                Assert.ThrowsAsync<ObjectDisposedException>(() => backend.FetchConfigAsync(CancellationToken.None));
                Assert.Throws<ObjectDisposedException>(() => backend.ResolveApprovedScreen(AddressableContentBackend.LobbyKey));
            }
        }

        private static object Context(string source = Source, string code = "42") => Invoke(
            Method(FixtureType(), "Create", Static, typeof(string), typeof(string), typeof(string), typeof(string), typeof(string)),
            null, source, Root, Path.GetFullPath("Library/com.unity.addressables/aa/Windows/settings.json"),
            Path.Combine(Path.GetTempPath(), "lastrail-loopback-" + Guid.NewGuid().ToString("N")), code);

        private static ClientContentGate Parse(object context, string json) =>
            (ClientContentGate)Call(context, "ParseAdmission", new[] { typeof(string) }, json);

        private static object Call(object target, string name, Type[] types, params object[] args) =>
            Invoke(Method(target.GetType(), name, Instance, types), target, args);

        private static Type FixtureType()
        {
            var type = typeof(AddressableContentBackend).Assembly.GetType("LastRail.Resources.EditorLoopbackContentFixture");
            Assert.That(type, Is.Not.Null, "Phase 1 RED: implement the frozen Editor-only fixture API, never relax production defaults.");
            return type;
        }

        private static MethodInfo Method(Type type, string name, BindingFlags flags, params Type[] types)
        {
            var method = type.GetMethod(name, flags, null, types, null);
            Assert.That(method, Is.Not.Null, type.FullName + "." + name);
            return method;
        }

        private static object Invoke(MethodInfo method, object target, params object[] args)
        {
            try { return method.Invoke(target, args); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        private static IResourceLocation Bundle(string url) => new ResourceLocationBase("bundle", url,
            typeof(AssetBundleProvider).FullName, typeof(UnityEngine.AssetBundle))
        {
            Data = new AssetBundleRequestOptions { BundleName = "local-test", Hash = "0123456789abcdef0123456789abcdef", BundleSize = 4096 }
        };

        private static string Json() => "{\"accepted\":true,\"maintenance\":{\"enabled\":false}," +
            "\"clientAdmission\":{\"status\":\"ACCEPTED\",\"installedVersionCode\":\"42\",\"minimumVersionCode\":\"40\",\"updateRequired\":false}," +
            "\"contentRelease\":{\"status\":\"APPROVED_COMPATIBLE\",\"releaseId\":\"LOCAL_TEST\",\"contentVersion\":\"local-42\"," +
            "\"catalogUrl\":\"" + Root + "catalog.bin\",\"catalogHash\":\"sha256:" + Digest + "\"," +
            "\"compatibility\":{\"minimumVersionCode\":\"40\",\"maximumVersionCode\":\"50\"}}}";
    }
}
#endif
