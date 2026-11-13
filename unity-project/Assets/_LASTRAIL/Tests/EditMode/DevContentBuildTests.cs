#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets.ResourceLocators;
using LastRail.Resources;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Tests.EditMode
{
    public sealed class DevContentBuildTests
    {
        private string root;
        private JObject request;
        private AddressableAssetSettings fixtureSettings;
        private Type Builder => Type.GetType("LastRail.Editor.DevContentBuild, LastRail.Editor");
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var input = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }
        [SetUp] public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "lastrail-dev-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var files = new JArray();
            foreach (var dir in new[] { "Assets", "Packages", "ProjectSettings", "Tools" })
            {
                Directory.CreateDirectory(Path.Combine(root, dir));
                var relative = dir + "/fixture.txt";
                var path = Path.Combine(root, relative); File.WriteAllText(path, "source");
                files.Add(new JObject { ["path"] = relative, ["length"] = 6, ["sha256"] = Hash(path) });
            }
            var manifest = Path.Combine(root, "source.json");
            File.WriteAllText(manifest, new JObject { ["commitSha"] = new string('a', 40), ["files"] = files }.ToString());
            request = new JObject {
                ["environment"] = "DEV", ["target"] = "Android",
                ["trustedCatalogRoot"] = "https://content.example.com/android/releases/",
                ["releaseNamespace"] = "https://content.example.com/android/releases/release-a/",
                ["releaseId"] = "release-a", ["contentVersion"] = "content-a", ["appVersion"] = "0.1.0",
                ["versionCode"] = 1, ["minimumVersionCode"] = 1, ["maximumVersionCode"] = 1,
                ["sourceCommit"] = new string('a', 40), ["sourceManifest"] = manifest,
                ["sourceManifestSha256"] = Hash(manifest), ["outputId"] = "candidate-a"
            };
        }
        [TearDown] public void TearDown()
        {
            if (fixtureSettings != null) UnityEngine.Object.DestroyImmediate(fixtureSettings);
            if (root != null && Path.GetDirectoryName(root) == Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)
                && Path.GetFileName(root).StartsWith("lastrail-dev-test-", StringComparison.Ordinal)) Directory.Delete(root, true);
        }
        private object Validate()
        {
            Assert.That(Builder, Is.Not.Null, "DEV entrypoint is missing");
            return Builder.GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { request.ToString(), root });
        }
        [Test] public void ValidExplicitInputIsReadOnly()
        {
            Assert.That(Validate(), Is.Not.Null);
            Assert.That(Directory.Exists(Path.Combine(root, "QA")), Is.False);
        }
        [TestCase("environment", "PRD")]
        [TestCase("target", "StandaloneWindows64")]
        [TestCase("trustedCatalogRoot", "http://content.example.com/android/releases/")]
        [TestCase("trustedCatalogRoot", "https://UNCONFIGURED_HOST/releases/")]
        [TestCase("releaseNamespace", "https://other.example.com/android/releases/release-a/")]
        [TestCase("releaseNamespace", "https://content.example.com/android/releases/other/")]
        [TestCase("releaseNamespace", "https://content.example.com/android/releases/release-a/?token=x")]
        [TestCase("outputId", "../escape")]
        [TestCase("outputId", "CON")]
        [TestCase("sourceCommit", "")]
        [TestCase("sourceManifestSha256", "")]
        [TestCase("appVersion", "")]
        public void InvalidInputFailsBeforeAnyOutput(string key, string value)
        {
            request[key] = value;
            Assert.That(Builder, Is.Not.Null, "DEV entrypoint is missing");
            Assert.Throws<TargetInvocationException>(() => Validate());
            Assert.That(Directory.Exists(Path.Combine(root, "QA")), Is.False);
        }
        [TestCase("versionCode", 0)] [TestCase("minimumVersionCode", 2)] [TestCase("maximumVersionCode", 0)]
        public void InvalidCompatibilityIsRejected(string key, int value)
        { request[key] = value; Assert.That(Builder, Is.Not.Null); Assert.Throws<TargetInvocationException>(() => Validate()); }
        [Test] public void ExistingOutputIsNeverOverwritten()
        {
            var output = Path.Combine(root, "QA/DevContentCandidates/candidate-a");
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "keep"), "original");
            Assert.That(Builder, Is.Not.Null); Assert.Throws<TargetInvocationException>(() => Validate());
            Assert.That(File.ReadAllText(Path.Combine(output, "keep")), Is.EqualTo("original"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ChangedOrUnlistedSourceIsRejected(bool extra)
        {
            File.WriteAllText(Path.Combine(root, extra ? "Assets/extra.txt" : "Assets/fixture.txt"), "changed");
            Assert.That(Builder, Is.Not.Null); Assert.Throws<TargetInvocationException>(() => Validate());
        }
        [Test] public void MissingProvenanceIsRejected()
        { request.Remove("sourceManifest"); Assert.That(Builder, Is.Not.Null); Assert.Throws<TargetInvocationException>(() => Validate()); }
        [TestCase("placeholder")] [TestCase("TBD")] [TestCase("TODO")]
        public void PlaceholderHostingAndReleaseIdentityAreRejected(string placeholder)
        {
            request["releaseId"] = placeholder;
            request["trustedCatalogRoot"] = "https://" + placeholder + ".example.com/releases/";
            request["releaseNamespace"] = (string)request["trustedCatalogRoot"] + placeholder + "/";
            Assert.Throws<TargetInvocationException>(() => Validate());
            Assert.That(Directory.Exists(Path.Combine(root, "QA")), Is.False);
        }
        [TestCase(false)] [TestCase(true)]
        public void CloneSurvivesSceneCleanupAndExplicitUnloading(bool unload)
        {
            var source = AddressableAssetSettingsDefaultObject.Settings;
            var sourceBefore = EditorJsonUtility.ToJson(source);
            var owned = new System.Collections.Generic.List<UnityEngine.Object>();
            var scenes = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var clone = (AddressableAssetSettings)Builder.GetMethod("CloneSettings", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { source, owned });
                var groups = clone.groups.Select(g => g.Guid).ToArray();
                var entries = clone.groups.Sum(g => g.entries.Count);
                // SBP SceneStateCleanup replaces the editor scene after content processing.
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
                if (unload) EditorUtility.UnloadUnusedAssetsImmediate(true);
                Assert.That(clone != null, Is.True, "Build settings must survive scene cleanup.");
                Assert.That(owned.All(item => item != null), Is.True, "Every owned group, schema and builder must survive.");
                Assert.That(clone.groups.Select(g => g.Guid), Is.EqualTo(groups));
                Assert.That(clone.groups.Sum(g => g.entries.Count), Is.EqualTo(entries));
                foreach (var group in clone.groups)
                {
                    Assert.That(group.Settings, Is.SameAs(clone));
                    Assert.That(group.Schemas.All(schema => schema.Group == group), Is.True);
                }
                Assert.That(clone.DefaultGroup, Is.Not.Null);
                Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(sourceBefore));
            }
            finally
            {
                foreach (var item in owned.AsEnumerable().Reverse()) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                if (scenes.Length > 0 && scenes.All(scene => !string.IsNullOrEmpty(scene.path)))
                    UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(scenes);
            }
            Assert.That(owned.All(item => item == null), Is.True, "Scope teardown must still destroy retained build objects.");
        }
        [TestCase(false)] [TestCase(true)]
        public void ConfigurationIsIsolatedAndGuardsReconfigureEvenOnFailure(bool fail)
        {
            Assert.That(Builder, Is.Not.Null);
            var defaultSettings = AddressableAssetSettingsDefaultObject.Settings;
            fixtureSettings = UnityEngine.Object.Instantiate(defaultSettings);
            fixtureSettings.groups.RemoveAll(g => g != null && AssetDatabase.GetAssetPath(g).Contains("/Tests/"));
            var original = fixtureSettings;
            // The callback's FindAssetEntry lazily computes this cache; include it in the fixture baseline.
            _ = original.currentHash;
            var before = EditorJsonUtility.ToJson(original);
            var scenePath = "Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity";
            var sceneBefore = Hash(scenePath);
            var called = false;
            Action<AddressableAssetSettings, string> body = (settings, output) => {
                called = true;
                Assert.That(settings, Is.Not.SameAs(original));
                Assert.That(settings.IsPersisted, Is.False);
                Assert.That(settings.activeProfileId, Is.EqualTo(settings.profileSettings.GetProfileId("DEV")));
                Assert.That(settings.RemoteCatalogLoadPath.GetValue(settings), Is.EqualTo((string)request["releaseNamespace"]));
                Assert.That(settings.RemoteCatalogBuildPath.GetValue(settings).Replace('\\', '/'), Does.StartWith(output.Replace('\\', '/')));
                Assert.That(settings.DisableCatalogUpdateOnStartup, Is.True);
                Assert.That(settings.buildSettings.bundleBuildPath.Replace('\\', '/'), Does.StartWith(output.Replace('\\', '/') + "/"));
                foreach (var group in settings.groups.Where(g => g != null))
                {
                    Assert.That(group.Settings, Is.SameAs(settings));
                    Assert.That(original.groups.Contains(group), Is.False);
                    foreach (var entry in group.entries)
                    {
                        Assert.That(entry.parentGroup, Is.SameAs(group));
                        Assert.That(original.FindAssetEntry(entry.guid), Is.Not.SameAs(entry));
                    }
                    foreach (var schema in group.Schemas) Assert.That(schema.Group, Is.SameAs(group));
                    var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (bundleSchema?.BuildPath.GetName(settings) == AddressableAssetSettings.kRemoteBuildPath)
                        Assert.That(bundleSchema.BuildPath.GetValue(group.Settings).Replace('\\', '/'), Does.StartWith(output.Replace('\\', '/') + "/"));
                }
                Assert.That(Hash(Path.Combine(output, "source-provenance.json")), Is.EqualTo((string)request["sourceManifestSha256"]));
                var legacy = Type.GetType("LastRail.Editor.AddressableResourceBuilder, LastRail.Editor", true);
                Assert.Throws<TargetInvocationException>(() => legacy.GetMethod("Configure").Invoke(null, null));
                Assert.Throws<TargetInvocationException>(() => legacy.GetMethod("ConfigureProfiles", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { settings }));
                Assert.Throws<TargetInvocationException>(() => legacy.GetMethod("CreateBootstrap", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null));
                if (fail) throw new IOException("injected build failure");
            };
            var method = Builder.GetMethod("WithConfiguration", BindingFlags.Static | BindingFlags.NonPublic);
            try { method.Invoke(null, new object[] { request.ToString(), root, original, body }); }
            catch (TargetInvocationException error) { if (!fail || !(error.InnerException is IOException)) throw; }
            Assert.That(called, Is.True);
            Assert.That(EditorJsonUtility.ToJson(original), Is.EqualTo(before));
            Assert.That(Hash(scenePath), Is.EqualTo(sceneBefore));
            Assert.That(AddressableAssetSettingsDefaultObject.Settings, Is.SameAs(defaultSettings));
        }
        [TestCase("Assets/_LASTRAIL/Tests/GeneratedDelta/lobby.prefab")]
        [TestCase("Assets/_LASTRAIL/UI/Showcase/screen.prefab")]
        [TestCase("Assets/_LASTRAIL/Sources/raw.png")]
        [TestCase("Assets/_LASTRAIL/QA/fixture.prefab")]
        [TestCase("Assets/_LASTRAIL/tests/fixture.prefab")]
        [TestCase("Assets/_LASTRAIL/UI")]
        public void ExcludedDependencyIsRejected(string path)
        {
            Assert.That(Builder, Is.Not.Null);
            Assert.Throws<TargetInvocationException>(() => Builder.GetMethod("ValidateDependencies", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new[] { path } }));
        }
        [TestCase(false)] [TestCase(true)]
        public void RuntimeAndTransientStateAreRestoredAfterLocalBuildOperation(bool fail)
        {
            var runtime = Path.Combine(root, "Library/aa");
            var output = Path.Combine(root, "QA/isolated");
            var transient = Path.Combine(root, "Library/com.unity.addressables/Android/addressables_content_state.bin");
            Directory.CreateDirectory(runtime); Directory.CreateDirectory(output); Directory.CreateDirectory(Path.GetDirectoryName(transient));
            File.WriteAllText(Path.Combine(runtime, "old.bin"), "old-runtime"); File.WriteAllText(transient, "old-state");
            Action action = () => {
                Assert.That(File.Exists(Path.Combine(runtime, "old.bin")), Is.False);
                Directory.CreateDirectory(runtime); File.WriteAllText(Path.Combine(runtime, "new.bin"), "new-runtime");
                File.WriteAllText(transient, "new-state");
                if (fail) throw new IOException("injected");
            };
            var method = Builder.GetMethod("WithRuntimeOutput", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "runtime preservation seam missing");
            try { method.Invoke(null, new object[] { root, runtime, output, action }); }
            catch (TargetInvocationException e) { if (!fail || !(e.InnerException is IOException)) throw; }
            Assert.That(File.ReadAllText(Path.Combine(runtime, "old.bin")), Is.EqualTo("old-runtime"));
            Assert.That(File.Exists(Path.Combine(runtime, "new.bin")), Is.False);
            Assert.That(File.ReadAllText(transient), Is.EqualTo("old-state"));
            Assert.That(File.ReadAllText(Path.Combine(output, "runtime-output/new.bin")), Is.EqualTo("new-runtime"));
        }
        [TestCase("valid")] [TestCase("extra")] [TestCase("missing")] [TestCase("size")]
        [TestCase("foreign")] [TestCase("synthetic")]
        public void BuiltDependencyClosureMustMatchExactly(string fault)
        {
            var remote = Path.Combine(root, "remote"); Directory.CreateDirectory(remote);
            File.WriteAllText(Path.Combine(remote, "one.bundle"), "bundle");
            if (fault == "extra") File.WriteAllText(Path.Combine(remote, "extra.bundle"), "extra");
            if (fault == "missing") File.Delete(Path.Combine(remote, "one.bundle"));
            var url = (string)request["releaseNamespace"] + "one.bundle";
            if (fault == "foreign") url = "https://other.example.com/one.bundle";
            var bundle = new ResourceLocationBase("bundle", url, typeof(CacheOnlyAssetBundleProvider).FullName, typeof(UnityEngine.AssetBundle)) {
                Data = new AssetBundleRequestOptions { BundleName = "one", Hash = new string('a', 32), Crc = 123, BundleSize = fault == "size" ? 8 : 6 }
            };
            var asset = new ResourceLocationBase("lobby", fault == "synthetic" ? "Assets/_LASTRAIL/Tests/lobby.prefab" : AddressableContentBackend.LobbyKey,
                typeof(BundledAssetProvider).FullName, typeof(UnityEngine.GameObject), bundle);
            var method = Builder.GetMethod("VerifyBundleFiles", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "bundle closure seam missing");
            if (fault == "valid")
            {
                var result = (JArray)method.Invoke(null, new object[] { new IResourceLocation[] { asset }, remote, request });
                Assert.That(result.Count, Is.EqualTo(1)); Assert.That((string)result[0]["sha256"], Is.EqualTo(Hash(Path.Combine(remote, "one.bundle"))));
            }
            else Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { new IResourceLocation[] { asset }, remote, request }));
        }
        [Test] public void CleanupFailureStillRestoresTransientStateAndRetainsOriginalRuntime()
        {
            var runtime = Path.Combine(root, "Library/aa"); var output = Path.Combine(root, "QA/out");
            var transient = Path.Combine(root, "Library/com.unity.addressables/Android/addressables_content_state.bin");
            Directory.CreateDirectory(runtime); Directory.CreateDirectory(output); Directory.CreateDirectory(Path.GetDirectoryName(transient));
            File.WriteAllText(Path.Combine(runtime, "original"), "keep"); File.WriteAllText(transient, "original");
            Action action = () => {
                Directory.CreateDirectory(runtime); File.WriteAllText(Path.Combine(runtime, "new"), "generated");
                Directory.CreateDirectory(Path.Combine(output, "runtime-output")); File.WriteAllText(transient, "changed");
            };
            Assert.Throws<TargetInvocationException>(() => Builder.GetMethod("WithRuntimeOutput", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { root, runtime, output, action }));
            Assert.That(File.ReadAllText(transient), Is.EqualTo("original"));
            Assert.That(File.ReadAllText(Path.Combine(output, "previous-runtime/original")), Is.EqualTo("keep"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ShippingLobbyRequiresTheRuntimeRequiredLabel(bool present)
        {
            var bundle = new ResourceLocationBase("bundle", "https://content.example.com/android/releases/release-a/one.bundle",
                typeof(CacheOnlyAssetBundleProvider).FullName, typeof(UnityEngine.AssetBundle)) {
                Data = new AssetBundleRequestOptions { BundleName = "one", Hash = new string('a', 32), Crc = 1, BundleSize = 6 }
            };
            var lobby = new ResourceLocationBase(AddressableContentBackend.LobbyKey, AddressableContentBackend.LobbyKey,
                typeof(BundledAssetProvider).FullName, typeof(UnityEngine.GameObject), bundle);
            var locator = new ResourceLocationMap("fixture", 2);
            locator.Add(AddressableContentBackend.LobbyKey, lobby);
            var main = new ResourceLocationBase("Assets/_LASTRAIL/Scenes/Main.unity", "Assets/_LASTRAIL/Scenes/Main.unity",
                typeof(SceneProvider).FullName, typeof(SceneInstance), bundle);
            locator.Add("Assets/_LASTRAIL/Scenes/Main.unity", main);
            if (present) locator.Add(AddressableContentBackend.RequiredLabel, lobby);
            if (present) locator.Add(AddressableContentBackend.RequiredLabel, main);
            var method = Builder.GetMethod("VerifyRequiredLocations", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            if (present) Assert.DoesNotThrow(() => method.Invoke(null, new object[] { locator, request }));
            else Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { locator, request }));
        }

        [Test]
        public void CandidateWithoutMainSceneMustBeRejected()
        {
            var bundle = new ResourceLocationBase("bundle", "https://content.example.com/android/releases/release-a/one.bundle",
                typeof(CacheOnlyAssetBundleProvider).FullName, typeof(UnityEngine.AssetBundle)) {
                Data = new AssetBundleRequestOptions { BundleName = "one", Hash = new string('a', 32), Crc = 1, BundleSize = 6 }
            };
            var lobby = new ResourceLocationBase(AddressableContentBackend.LobbyKey, AddressableContentBackend.LobbyKey,
                typeof(BundledAssetProvider).FullName, typeof(UnityEngine.GameObject), bundle);
            var locator = new ResourceLocationMap("no-main-fixture", 2);
            locator.Add(AddressableContentBackend.LobbyKey, lobby); locator.Add(AddressableContentBackend.RequiredLabel, lobby);
            Assert.Throws<TargetInvocationException>(() => Builder.GetMethod("VerifyRequiredLocations", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { locator, request }));
        }
        [TestCase("jsonCatalog")] [TestCase("initialization")]
        public void UnsupportedInstalledCatalogModeFailsBeforeBuild(string mode)
        {
            fixtureSettings = UnityEngine.Object.Instantiate(AddressableAssetSettingsDefaultObject.Settings);
            fixtureSettings.groups.RemoveAll(g => g != null && AssetDatabase.GetAssetPath(g).Contains("/Tests/"));
            var method = Builder.GetMethod("ValidateSettings", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { fixtureSettings }));
            if (mode == "jsonCatalog") fixtureSettings.EnableJsonCatalog = true;
            if (mode == "initialization") fixtureSettings.InitializationObjects.Add(fixtureSettings);
            Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { fixtureSettings }));
        }
        [TestCase(false)] [TestCase(true)]
        public void PackageFallbackCannotOverwriteSourceState(bool missing)
        {
            var path = Path.Combine(root, "Assets/AddressableAssetsData/Android/addressables_content_state.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!missing) File.WriteAllText(path, "original");
            var called = false;
            Action callback = () => { called = true; Assert.Throws<IOException>(() => File.WriteAllText(path, "overwritten")); };
            var method = Builder.GetMethod("WithFallbackStateGuard", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            if (missing) Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { root, callback }));
            else method.Invoke(null, new object[] { root, callback });
            Assert.That(called, Is.EqualTo(!missing));
            if (!missing) Assert.That(File.ReadAllText(path), Is.EqualTo("original"));
        }
        [Test] public void CatalogFilenameUsesExplicitApprovedVersionNotSourceOverride()
        {
            fixtureSettings = UnityEngine.Object.Instantiate(AddressableAssetSettingsDefaultObject.Settings);
            fixtureSettings.OverridePlayerVersion = "x/../../escape";
            var method = Builder.GetMethod("CreateBuildInput", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var input = (AddressablesDataBuilderInput)method.Invoke(null, new object[] { fixtureSettings, request });
            Assert.That(input.PlayerVersion, Is.EqualTo((string)request["appVersion"]));
        }
    }
}
#endif
