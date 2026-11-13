using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class ContentSourceSelectionTests
    {
        [Test]
        public void NeitherSourceCanInitializeBeforeApprovedAdmission()
        {
            foreach (var source in new[] { ContentResourceSource.EditorLocal, ContentResourceSource.RemoteApproved })
            {
                using (var backend = new AddressableContentBackend(null, true, source: source))
                {
                    Assert.That(backend.Source, Is.EqualTo(source));
                    Assert.ThrowsAsync<InvalidOperationException>(async () => await backend.InitializeAsync(CancellationToken.None));
                }
            }
        }

        [UnityTest]
        public IEnumerator ActualLocalProjectLoadsMainAndLobbyDependencies()
        {
            yield return new EnterPlayMode();
            var run = LoadActualProject();
            while (!run.IsCompleted) yield return null;
            var error = run.Exception;
            yield return new ExitPlayMode();
            if (error != null) throw error;
        }

        private static async Task LoadActualProject()
        {
            var hadKey = PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath);
            var oldValue = PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath);
            var factory = EditorProjectContentBridge.Factory;
            Assert.That(factory, Is.Not.Null);
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            {
                var locator = await factory().InitializeAsync(timeout.Token);
                Assert.That(PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath), Is.EqualTo(hadKey));
                Assert.That(PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath), Is.EqualTo(oldValue));
                Assert.That(locator.Locate(AddressableContentBackend.MainSceneKey, typeof(SceneInstance), out var mainLocations), Is.True);
                Assert.That(locator.Locate(AddressableContentBackend.LobbyKey, typeof(GameObject), out var lobbyLocations), Is.True);
                var host = new AddressableMainSceneHost();
                var owner = new GameObject("LocalResourceTestHost");
                var screens = owner.AddComponent<AddressableScreenHost>();
                try
                {
                    var main = await host.OpenAsync(mainLocations[0], timeout.Token);
                    var lobby = await screens.OpenAsync(lobbyLocations[0], main.ContentRoot, timeout.Token);
                    Assert.That(lobby, Is.Not.Null);
                    Assert.That(main.ContentRoot.gameObject.activeInHierarchy, Is.False, "Resource inspection must not expose an authenticated lobby.");
                    var images = lobby.GetComponentsInChildren<Image>(true);
                    Assert.That(images.Length, Is.GreaterThan(0));
                    Assert.That(Array.Exists(images, item => item.sprite != null && item.sprite.texture != null), Is.True,
                        "Actual lobby sprite/texture dependencies must load.");
                }
                finally
                {
                    await screens.CloseAsync();
                    await host.CloseAsync();
                    UnityEngine.Object.Destroy(owner);
                }
            }
        }

        [TestCase(true, false, "EditorLocal")]
        [TestCase(true, true, "RemoteApproved")]
        [TestCase(false, false, "RemoteApproved")]
        [TestCase(false, true, "RemoteApproved")]
        public void TargetSelectionNeverGivesPlayerALocalFallback(bool editor, bool explicitRemote, string expected)
        {
            var type = Type.GetType("LastRail.Resources.ContentSourceSelection, LastRail.Runtime");
            Assert.That(type, Is.Not.Null, "Resource-source selection is missing.");
            Assert.That(type.GetMethod("Select").Invoke(null, new object[] { editor, explicitRemote }).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void LocalLoaderLivesOnlyInEditorAssembly()
        {
            var type = Type.GetType("LastRail.Editor.EditorProjectContent, LastRail.Editor");
            Assert.That(type, Is.Not.Null, "Actual project local loader is missing.");
            Assert.That(Type.GetType("LastRail.Editor.EditorProjectContent, LastRail.Runtime"), Is.Null);
        }
    }
}
