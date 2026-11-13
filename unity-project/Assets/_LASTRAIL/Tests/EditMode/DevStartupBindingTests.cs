#if UNITY_EDITOR
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Diagnostics;
using LastRail.Resources;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LastRail.Tests.EditMode
{
    public sealed class DevStartupBindingTests
    {
        [Test]
        public void ActualAndroidMainBundleCanBeInspectedInWindowsEditor()
        {
            var packetPath = System.Environment.GetEnvironmentVariable("LASTRAIL_DEV_CONTENT_PACKET");
            if (string.IsNullOrEmpty(packetPath)) Assert.Ignore("Requires explicit actual content packet, not a generated fixture.");
            var packet = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(packetPath));
            var proof = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText((string)packet["catalogProofPath"]));
            var url = (string)proof["mainBundleUrls"].Single();
            var file = packet["publicFiles"].Single(f => (string)f["url"] == url);
            var bundle = UnityEngine.AssetBundle.LoadFromFile((string)file["localPath"], (uint)file["crc"]);
            try
            {
                Assert.That(bundle, Is.Not.Null);
                Assert.That(bundle.isStreamedSceneAssetBundle, Is.True);
                Assert.That(bundle.GetAllScenePaths().Single(),
                    Is.EqualTo(AddressableContentBackend.MainSceneKey).IgnoreCase);
            }
            finally { if (bundle != null) bundle.Unload(true); }
        }

        [Test]
        public void AndroidDevBootstrapHasApprovedHostWithoutPinnedRelease()
        {
            Assert.That(ClientEnvironmentBootstrap.CurrentEnvironment, Is.EqualTo(ClientEnvironment.Dev));
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity");
            try
            {
                var controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ResourceBootstrapController>(true)).Single();
                var data = new SerializedObject(controller);
                Assert.That(data.FindProperty("remoteHostingConfigured").boolValue, Is.True);
                Assert.That(data.FindProperty("trustedCatalogRoot").stringValue,
                    Is.EqualTo("https://dev-content.lastrail.app/android/releases/"));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public async Task ActualEditorBackendFailsClosedWithoutInstalledAppIdentity()
        {
            Assert.That(ClientEnvironmentBootstrap.CurrentEnvironment, Is.EqualTo(ClientEnvironment.Dev));
            Assert.That(AppBuildSnapshot.CaptureInstalled().AppVersionCode, Is.Null);
            var access = new DownloadAccessPolicy();
            using (var backend = new AddressableContentBackend(ClientEnvironmentBootstrap.CurrentClientConfigUrl,
                true, access, "https://dev-content.lastrail.app/android/releases/"))
            {
                var coordinator = new DownloadCoordinator(backend, access);
                await coordinator.StartAsync("0.1.0", CancellationToken.None);
                Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.Failed));
                Assert.That(coordinator.ConfigAccepted, Is.False);
                Assert.That(coordinator.CanEnterGame, Is.False);
                Assert.That(coordinator.Failure, Is.Not.Null);
                Assert.That(coordinator.Failure.Message,
                    Is.EqualTo("Installed app version code is required and must be canonical."));
            }
            Assert.That(new AndroidKeystoreNativeStoragePort().IsAndroidRuntime, Is.False);
            Assert.That(new AndroidKeystoreNativeStoragePort().IsKeystoreAvailable, Is.False);
        }
    }
}
#endif
