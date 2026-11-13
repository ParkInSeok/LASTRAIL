using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Tests.EditMode
{
    public sealed class MainSceneIntegrationTests
    {
        private const string MainPath = "Assets/_LASTRAIL/Scenes/Main.unity";
        private sealed class Backend : IContentStartupBackend
        {
            public int ConfigReads, SizeReads;
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken c) { ConfigReads++; return Task.FromResult(new ClientContentGate { MinimumVersion = "0.1.0" }); }
            public Task InitializeAsync(CancellationToken c) => Task.CompletedTask;
            public Task UpdateCatalogAsync(CancellationToken c) => Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken c) { SizeReads++; return Task.FromResult(0L); }
            public Task DownloadAsync(Action<long, long> p, CancellationToken c) => Task.CompletedTask;
            public Task VerifyRequiredAssetsAsync(CancellationToken c) => Task.CompletedTask;
        }

        [Test]
        public async Task FailedLobbyRetryRecalculatesContentWithoutReplacingAcceptedAdmission()
        {
            var root = new GameObject("InactiveRetryFixture"); root.SetActive(false);
            var cancel = new CancellationTokenSource();
            try
            {
                var controller = root.AddComponent<ResourceBootstrapController>();
                var access = new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi); access.SetAuthenticated(true);
                var backend = new Backend(); var coordinator = new DownloadCoordinator(backend, access);
                await coordinator.StartAsync("0.1.0", cancel.Token);
                var entry = new AuthenticatedScreenEntry(_ => throw new InvalidOperationException("cache evicted"), () => { });
                await entry.EnterAsync(cancel.Token);
                Set(controller, "coordinator", coordinator); Set(controller, "access", access);
                Set(controller, "network", new ContentNetworkService()); Set(controller, "entry", entry); Set(controller, "lifetime", cancel);
                typeof(ResourceBootstrapController).GetMethod("Retry", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                if (coordinator.Operation != null) await coordinator.Operation;
                Assert.That(backend.SizeReads, Is.EqualTo(2), "Entry failure must recalculate missing cache before retry.");
                Assert.That(backend.ConfigReads, Is.EqualTo(1), "Keep accepted same-release admission.");
            }
            finally { cancel.Cancel(); cancel.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void MainIsRemoteRequiredAndNotAnExtraLocalBootScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainPath);
            Assert.That(scene, Is.Not.Null, "Shipping integration scene is missing.");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(MainPath));
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.address, Is.EqualTo(MainPath));
            Assert.That(entry.labels, Does.Contain(AddressableContentBackend.RequiredLabel));
            Assert.That(entry.parentGroup.Name, Is.EqualTo("remote_launch_required"));
            Assert.That(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path),
                Is.EqualTo(new[] { "Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity" }));
        }

        [Test]
        public void MainContainsOneMountAndNoDuplicateServiceOrLocalArt()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(MainPath), Is.Not.Null);
            var scene = EditorSceneManager.OpenPreviewScene(MainPath);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
                Assert.That(components.Count(c => c != null && c.GetType().FullName == "LastRail.Resources.MainSceneRoot"), Is.EqualTo(1));
                Assert.That(components.OfType<ResourceBootstrapController>(), Is.Empty);
                Assert.That(components.OfType<EventSystem>(), Is.Empty);
                Assert.That(components.OfType<AudioListener>(), Is.Empty);
                Assert.That(AssetDatabase.GetDependencies(MainPath, true).Where(p => p.EndsWith(".prefab") || p.EndsWith(".png") || p.EndsWith(".wav")), Is.Empty);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void BackendHasSceneTypedApprovedLocationResolution()
        {
            Assert.That(typeof(AddressableContentBackend).GetMethod("ResolveApprovedMainScene"), Is.Not.Null,
                "A GameObject-only resolver must not be used for a scene.");
        }

        [Test]
        public void RequiredDependencyPolicyAcceptsOnlyShippingMainSceneProvider()
        {
            var bundle = new ResourceLocationBase("bundle", "https://content.example.test/releases/a/main.bundle",
                typeof(CacheOnlyAssetBundleProvider).FullName, typeof(AssetBundle)) {
                Data = new AssetBundleRequestOptions { BundleName = "main", Hash = new string('a', 32), BundleSize = 100, Crc = 1 }
            };
            var main = new ResourceLocationBase(MainPath, MainPath, typeof(SceneProvider).FullName, typeof(SceneInstance), bundle);
            Assert.That(ApprovedCatalogSafety.BuildRemoteBundlePlan(new[] { main }, "https://content.example.test/releases/", l => l.InternalId), Has.Length.EqualTo(1));
            var other = new ResourceLocationBase("other", "Assets/_LASTRAIL/Scenes/LaunchUiKitShowcase.unity", typeof(SceneProvider).FullName, typeof(SceneInstance), bundle);
            Assert.Throws<InvalidOperationException>(() => ApprovedCatalogSafety.BuildRemoteBundlePlan(new[] { other }, "https://content.example.test/releases/", l => l.InternalId));
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
