using System;
using System.IO;
using System.Reflection;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Tests.EditMode
{
    // Pure validation boundaries called before invoking Addressables or any provider.
    public sealed class ApprovedCatalogSafetyTests
    {
        private const string Root = "https://content.lastrail.test/releases/r42/";
        private const string Digest = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string InstalledCatalog = "file:///installed/StreamingAssets/aa/catalog.bin";

        [Test]
        public void EditorBuiltRelativePathsAreNormalizedWithoutTurningRemoteUrisIntoLocalFiles()
        {
            var method = Method("NormalizeInstalledPath", typeof(string));
            var relative = "Library/com.unity.addressables/aa/Windows/catalog.bin";
            Assert.That((string)Invoke(method, relative), Is.EqualTo(Path.GetFullPath(relative)));
            Assert.Throws<InvalidOperationException>(() => Invoke(method, "https://evil.example/catalog.bin"));
            Assert.Throws<InvalidOperationException>(() => Invoke(method, "\\\\evil.example\\share\\catalog.bin"));
            Assert.That((string)Invoke(method, InstalledCatalog), Is.EqualTo(InstalledCatalog));
        }

        [Test]
        public void VerifiedBinaryCatalogKeepsBinaryExtensionForTheInstalledProvider()
        {
            var directory = Path.Combine(Path.GetTempPath(), "lastrail-catalog-test");
            var method = Method("VerifiedCatalogPath", typeof(string), typeof(string), typeof(string), typeof(bool));
            var path = (string)Invoke(method, directory, Root + "catalog.bin", "sha256:" + Digest, false);
            Assert.That(Path.GetExtension(path), Is.EqualTo(".bin"));
            Assert.That(Path.GetFileNameWithoutExtension(path), Is.EqualTo(Digest));
            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(directory));
        }

        [Test]
        public void CatalogFormatMustMatchTheProviderInsteadOfRenamingIncompatibleBytes()
        {
            var method = Method("VerifiedCatalogPath", typeof(string), typeof(string), typeof(string), typeof(bool));
            var directory = Path.Combine(Path.GetTempPath(), "lastrail-catalog-test");
            Assert.Throws<InvalidOperationException>(() => Invoke(method, directory, Root + "catalog.json", "sha256:" + Digest, false));
            Assert.Throws<InvalidOperationException>(() => Invoke(method, directory, Root + "catalog.bin", "sha256:" + Digest, true));
            Assert.Throws<InvalidOperationException>(() => Invoke(method, directory, Root + "catalog.zip", "sha256:" + Digest, false));
            Assert.That(Path.GetExtension((string)Invoke(method, directory, Root + "catalog.json", "sha256:" + Digest, true)), Is.EqualTo(".json"));
        }

        [Test]
        public void NestedRemoteTextProviderCannotBypassTheBundleConsentPlan()
        {
            var method = GraphMethod();
            foreach (var url in new[] { "https://evil.example/config.txt", Root + "config.txt" })
            {
                var text = new ResourceLocationBase("text", url, typeof(TextDataProvider).FullName, typeof(string));
                var screen = Screen(text);
                Assert.Throws<InvalidOperationException>(() => Invoke(method, new IResourceLocation[] { screen }, Root, Identity));
            }
        }

        [Test]
        public void BundleTrustUsesTransformedIdsThroughoutTheDependencyGraph()
        {
            var method = GraphMethod();
            var bundle = Bundle(Root + "screen.bundle");
            Func<IResourceLocation, string> transform = location => location == bundle ? "https://evil.example/screen.bundle" : location.InternalId;
            Assert.Throws<InvalidOperationException>(() => Invoke(method, new IResourceLocation[] { Screen(bundle) }, Root, transform));
        }

        [Test]
        public void InstalledBundlesDoNotRequireRemoteCacheEntriesButRemoteBundlesDo()
        {
            var local = Bundle("file:///installed/StreamingAssets/aa/local.bundle");
            var remote = Bundle(Root + "remote.bundle");
            var roots = new IResourceLocation[] { Screen(local, remote) };
            var plan = (IResourceLocation[])Invoke(GraphMethod(), roots, Root, Identity);
            Assert.That(plan, Is.EqualTo(new IResourceLocation[] { remote }));

            var verify = Method("RequireCachedRemoteBundles", typeof(IResourceLocation[]), typeof(string),
                typeof(Func<IResourceLocation, string>), typeof(Func<IResourceLocation, bool>));
            var cacheQueries = 0;
            Func<IResourceLocation, bool> cache = location =>
            {
                Assert.That(location, Is.SameAs(remote), "Installed StreamingAssets must not require Caching.IsVersionCached.");
                cacheQueries++;
                return true;
            };
            Invoke(verify, roots, Root, Identity, cache);
            Assert.That(cacheQueries, Is.EqualTo(1));
            Func<IResourceLocation, bool> uncached = location => false;
            Assert.Throws<InvalidOperationException>(() => Invoke(verify, roots, Root, Identity, uncached));
        }

        [Test]
        public void InstalledSettingsMustExplicitlyDisableStartupCatalogUpdates()
        {
            var method = RuntimeMethod();
            Assert.Throws<InvalidOperationException>(() => Invoke(method, null, ResolveIdentity));
            var settings = Settings(false, InstalledCatalog);
            Assert.Throws<InvalidOperationException>(() => Invoke(method, settings, ResolveIdentity));
            settings.DisableCatalogUpdateOnStartup = true;
            Assert.DoesNotThrow(() => Invoke(method, settings, ResolveIdentity));
        }

        [Test]
        public void DisabledUpdateFlagDoesNotAuthorizeDirectOrFallbackRemoteCatalogs()
        {
            var method = RuntimeMethod();
            Assert.Throws<InvalidOperationException>(() => Invoke(method, Settings(true, Root + "catalog.bin"), ResolveIdentity));
            Assert.Throws<InvalidOperationException>(() => Invoke(method,
                Settings(true, InstalledCatalog, "https://evil.example/fallback.bin"), ResolveIdentity));
        }

        [Test]
        public void InstalledRuntimeGraphRejectsRemoteDependenciesAndTransformedRemoteCatalogs()
        {
            var method = RuntimeMethod();
            var settings = new ResourceManagerRuntimeData { DisableCatalogUpdateOnStartup = true };
            settings.CatalogLocations.Add(new ResourceLocationData(new[] { ResourceManagerRuntimeData.kCatalogAddress },
                InstalledCatalog, typeof(ContentCatalogProvider), typeof(object), new[] { "remote-hash" }));
            settings.CatalogLocations.Add(new ResourceLocationData(new[] { "remote-hash" },
                "https://evil.example/catalog.hash", typeof(TextDataProvider), typeof(string)));
            Assert.Throws<InvalidOperationException>(() => Invoke(method, settings, ResolveIdentity));

            Func<string, string> remoteTransform = value => "https://evil.example/catalog.bin";
            Assert.Throws<InvalidOperationException>(() => Invoke(method, Settings(true, InstalledCatalog), remoteTransform));
        }

        private static readonly Func<IResourceLocation, string> Identity = location => location.InternalId;
        private static readonly Func<string, string> ResolveIdentity = value => value;

        private static ResourceLocationBase Bundle(string url)
        {
            return new ResourceLocationBase(url, url, typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource))
            {
                Data = new AssetBundleRequestOptions
                {
                    BundleName = "screen", Hash = "0123456789abcdef0123456789abcdef", BundleSize = 1024
                }
            };
        }

        private static ResourceLocationBase Screen(params IResourceLocation[] dependencies)
        {
            return new ResourceLocationBase("screen", "Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab",
                typeof(BundledAssetProvider).FullName, typeof(GameObject), dependencies);
        }

        private static ResourceManagerRuntimeData Settings(bool disableUpdates, params string[] catalogs)
        {
            var settings = new ResourceManagerRuntimeData { DisableCatalogUpdateOnStartup = disableUpdates };
            foreach (var catalog in catalogs)
                settings.CatalogLocations.Add(new ResourceLocationData(new[] { ResourceManagerRuntimeData.kCatalogAddress },
                    catalog, typeof(ContentCatalogProvider), typeof(object)));
            return settings;
        }

        private static MethodInfo GraphMethod() => Method("BuildRemoteBundlePlan", typeof(IResourceLocation[]),
            typeof(string), typeof(Func<IResourceLocation, string>));

        private static MethodInfo RuntimeMethod() => Method("ValidateInstalledRuntimeData", typeof(ResourceManagerRuntimeData), typeof(Func<string, string>));

        private static MethodInfo Method(string name, params Type[] parameters)
        {
            var type = typeof(ClientContentGate).Assembly.GetType("LastRail.Resources.ApprovedCatalogSafety");
            Assert.That(type, Is.Not.Null, "RED: implement LastRail.Resources.ApprovedCatalogSafety before loading catalogs.");
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            Assert.That(method, Is.Not.Null, "Missing proposed API: " + name);
            return method;
        }

        private static object Invoke(MethodInfo method, params object[] args)
        {
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException error) when (error.InnerException != null) { throw error.InnerException; }
        }
    }
}
