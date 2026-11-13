#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using LastRail.Resources;

namespace LastRail.Tests.EditMode
{
    public static class SyntheticContentDeltaQa
    {
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Synthetic runtime QA requires Win64, not Android bundles.");
            var id = "delta_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var gateQa = Environment.GetEnvironmentVariable("LASTRAIL_BUILD_GATE_QA") == "1";
            var root = Path.GetFullPath((gateQa ? "QA/ContentGateDelta/" : "QA/ContentDelta/") + id);
            var assets = "Assets/_LASTRAIL/Tests/GeneratedDelta/" + id;
            Directory.CreateDirectory(root); Directory.CreateDirectory(assets);
            var defaultSettings = AddressableAssetSettingsDefaultObject.Settings;
            var runtime = Path.GetFullPath(Addressables.BuildPath);
            var backup = Path.Combine(root, "previous-runtime");
            var transientState = Path.GetFullPath("Library/com.unity.addressables/Windows/addressables_content_state.bin");
            var previousState = File.Exists(transientState) ? File.ReadAllBytes(transientState) : null;
            var hadRuntime = Directory.Exists(runtime);
            if (hadRuntime) Move(runtime, backup);
            try
            {
                var settings = AddressableAssetSettings.Create(assets + "/Settings", "SyntheticSettings", true, true);
                var profile = settings.profileSettings.AddProfile("LOCAL_TEST", settings.activeProfileId);
                settings.activeProfileId = profile;
                settings.BuildRemoteCatalog = true;
                settings.DisableCatalogUpdateOnStartup = true;
                settings.ContentStateBuildPath = Path.Combine(root, "generated-state");
                settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, Path.Combine(root, "build-full").Replace('\\', '/'));
                settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath,
                    gateQa ? "http://127.0.0.1:18286/LOCAL_TEST/StandaloneWindows64" : "http://127.0.0.1:18285/bundles");
                settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                var group = settings.CreateGroup("synthetic_delta", false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                if (gateQa) { var provider = schema.AssetBundleProviderType; provider.Value = typeof(CacheOnlyAssetBundleProvider); schema.AssetBundleProviderType = provider; }
                group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
                var source = assets + "/payload.txt";
                File.WriteAllText(source, "synthetic-release-A"); AssetDatabase.ImportAsset(source);
                settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(source), group).address = "synthetic-delta-payload";
                var prefab = assets + "/SyntheticLobby.prefab";
                if (gateQa)
                {
                    SaveLobby(prefab, "synthetic-release-A");
                    var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefab), group);
                    entry.address = AddressableContentBackend.LobbyKey;
                    entry.SetLabel(AddressableContentBackend.RequiredLabel, true, true);
                }
                AssetDatabase.SaveAssets();
                var full = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(new AddressablesDataBuilderInput(settings));
                Check(full, false);
                var state = ContentUpdateScript.GetContentStateDataPath(false, settings);
                var identity = "LOCAL_TEST|StandaloneWindows64|" + Application.unityVersion + "|synthetic:" + id;
                var archiveRoot = Path.Combine(root, "archives");
                var archive = Type.GetType("LastRail.Editor.LocalContentBaselineArchive, LastRail.Editor", true);
                var baseline = (string)archive.GetMethod("Archive").Invoke(null, new object[] {
                    archiveRoot, "full_A", state, runtime, Path.Combine(root, "build-full"), identity, null });
                var baselineState = (string)archive.GetMethod("Validate").Invoke(null, new object[] { archiveRoot, "full_A", identity });
                var manifestHash = Hash(Path.Combine(baseline, "manifest.json"));
                var oldBundle = Bundle(full, group, root, "synthetic-release-A");
                if (gateQa)
                {
                    var installed = Path.Combine(root, "installed"); Directory.CreateDirectory(installed);
                    var data = JsonUtility.FromJson<ResourceManagerRuntimeData>(File.ReadAllText(Path.Combine(baseline, "runtime/settings.json")));
                    data.CatalogLocations.Clear();
                    data.CatalogLocations.Add(new ResourceLocationData(new[] { ResourceManagerRuntimeData.kCatalogAddress },
                        Path.Combine(baseline, "runtime/catalog.bin"), typeof(ContentCatalogProvider), typeof(ContentCatalogData)));
                    data.DisableCatalogUpdateOnStartup = true;
                    ApprovedCatalogSafety.ValidateInstalledRuntimeData(data, ApprovedCatalogSafety.NormalizeInstalledPath);
                    File.WriteAllText(Path.Combine(installed, "settings.json"), JsonUtility.ToJson(data));
                    SaveLobby(prefab, "synthetic-release-B-changed");
                }
                File.WriteAllText(source, "synthetic-release-B-changed"); AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceUpdate);
                settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, Path.Combine(root, "build-update").Replace('\\', '/'));
                AssetDatabase.SaveAssets();
                var update = ContentUpdateScript.BuildContentUpdate(settings, baselineState);
                Check(update, true);
                var newBundle = Bundle(update, group, root, "synthetic-release-B-changed");
                if ((string)oldBundle["hash"] == (string)newBundle["hash"]) throw new Exception("Changed synthetic asset did not change bundle hash.");
                archive.GetMethod("Archive").Invoke(null, new object[] {
                    archiveRoot, "update_B", baselineState, runtime, Path.Combine(root, "build-update"), identity, "full_A" });
                archive.GetMethod("Validate").Invoke(null, new object[] { archiveRoot, "full_A", identity });
                if (Hash(Path.Combine(baseline, "manifest.json")) != manifestHash) throw new Exception("Original baseline changed.");
                File.WriteAllText(Path.Combine(root, "fixture.json"), new JObject {
                    ["platform"] = "StandaloneWindows64", ["root"] = root, ["identity"] = identity,
                    ["baselineManifestSha256"] = manifestHash, ["old"] = oldBundle, ["update"] = newBundle
                }.ToString());
                Debug.Log("SYNTHETIC_DELTA_BUILD_OK " + root);
            }
            finally
            {
                if (Directory.Exists(runtime)) Move(runtime, Path.Combine(root, "generated-runtime"));
                if (hadRuntime) Move(backup, runtime);
                if (previousState != null) File.WriteAllBytes(transientState, previousState);
                else if (File.Exists(transientState)) File.Delete(transientState);
                if (!ReferenceEquals(defaultSettings, AddressableAssetSettingsDefaultObject.Settings))
                    throw new Exception("Synthetic build changed the default Addressables settings.");
            }
        }

        private static void SaveLobby(string path, string name)
        {
            var instance = new GameObject("SyntheticLobby");
            new GameObject(name).transform.SetParent(instance.transform, false);
            try { PrefabUtility.SaveAsPrefabAsset(instance, path); }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static JObject Bundle(AddressablesPlayerBuildResult result, AddressableAssetGroup group, string root, string payload)
        {
            var item = result.AssetBundleBuildResults.Single(b => b.SourceAssetGroup == group);
            var destination = Path.Combine(root, "bundles", Path.GetFileName(item.FilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(item.FilePath, destination, false);
            return new JObject { ["file"] = Path.GetFileName(destination), ["hash"] = item.Hash,
                ["crc"] = item.Crc, ["bundleName"] = item.InternalBundleName, ["payload"] = payload,
                ["sha256"] = Hash(destination), ["bytes"] = new FileInfo(destination).Length };
        }
        private static void Check(AddressablesPlayerBuildResult result, bool update)
        { if (result == null || !string.IsNullOrEmpty(result.Error) || result.IsUpdateContentBuild != update) throw new Exception(result?.Error ?? "Missing build result."); }
        public static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Move(string source, string destination)
        {
            var project = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)) + Path.DirectorySeparatorChar;
            foreach (var path in new[] { source, destination })
            {
                var full = Path.GetFullPath(path);
                if (!full.StartsWith(project, StringComparison.OrdinalIgnoreCase)) throw new IOException("QA move must stay in this project.");
                for (var current = full; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                    if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("QA move rejects reparse paths.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            Directory.Move(source, destination);
        }
    }
}
#endif
