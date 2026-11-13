#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LastRail.Editor
{
    // Separate PC QA entrypoint: does not select or publish the Android content release.
    public static class WindowsDevBundleQaBuild
    {
        public static void Build()
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            var configPath = Environment.GetEnvironmentVariable("LASTRAIL_WINDOWS_DEV_BUILD_CONFIG");
            if (string.IsNullOrEmpty(configPath)) throw new InvalidDataException("Explicit Windows DEV build configuration required.");
            var request = JObject.Parse(File.ReadAllText(configPath));
            if ((string)request["environment"] != "DEV" || (string)request["target"] != "StandaloneWindows64"
                || (string)request["releaseId"] != "pc-formation-20261112-v02"
                || (string)request["trustedCatalogRoot"] != "https://dev-content.lastrail.app/windowsqa/"
                || (string)request["releaseNamespace"] != "https://dev-content.lastrail.app/windowsqa/releases/pc-formation-20261112-v02/"
                || (string)request["appVersion"] != PlayerSettings.bundleVersion
                || EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64
                || Application.platform != RuntimePlatform.WindowsEditor)
                throw new InvalidDataException("Configuration must match the explicitly authorized PC DEV QA release and current Windows target.");
            var output = LocalContentBaselineArchive.Child(project, "QA/PCFormation03");
            if (Directory.Exists(output) || File.Exists(output) || File.Exists(output + ".lock"))
                throw new IOException("Immutable PC candidate already exists or is reserved.");
            Call("VerifySource", request, project);
            var source = AddressableAssetSettingsDefaultObject.Settings;
            Call("ValidateSettings", source);
            var roots = source.groups.Where(g => g != null && g.GetSchema<BundledAssetGroupSchema>()?.IncludeInBuild == true)
                .SelectMany(g => g.entries).Select(e => e.AssetPath).ToArray();
            DevContentBuild.ValidateDependencies(roots);
            DevContentBuild.ValidateDependencies(AssetDatabase.GetDependencies(roots, true));
            if (roots.Length != 1447 || roots.Distinct().Count() != 1447)
                throw new InvalidDataException("Expected exactly 1447 distinct shipping entries.");
            var sourceObjects = source.groups.Where(g => g != null).Cast<UnityEngine.Object>()
                .Concat(source.groups.Where(g => g != null).SelectMany(g => g.Schemas)).Append(source).Distinct()
                .ToDictionary(o => o, o => EditorJsonUtility.ToJson(o));
            var owned = new List<UnityEngine.Object>();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (new FileStream(output + ".lock", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                Directory.CreateDirectory(output);
                File.Copy((string)request["sourceManifest"], Path.Combine(output, "source-provenance.json"));
                try
                {
                    var settings = (AddressableAssetSettings)Call("CloneSettings", source, owned);
                    var profile = settings.profileSettings.GetProfileId("DEV");
                    if (string.IsNullOrEmpty(profile)) throw new InvalidDataException("DEV profile missing.");
                    settings.activeProfileId = profile;
                    settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath, (string)request["releaseNamespace"]);
                    settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, Path.Combine(output, "remote").Replace('\\', '/'));
                    settings.profileSettings.SetValue(profile, AddressableAssetSettings.kLocalBuildPath, "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]");
                    settings.profileSettings.SetValue(profile, AddressableAssetSettings.kLocalLoadPath, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]");
                    settings.ContentStateBuildPath = Path.Combine(output, "state");
                    Directory.CreateDirectory(settings.ContentStateBuildPath);
                    settings.buildSettings.bundleBuildPath = Path.Combine(output, "intermediate").Replace('\\', '/');
                    settings.BuildRemoteCatalog = true;
                    settings.DisableCatalogUpdateOnStartup = true;
                    settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                    PreserveWindowsRuntime(project, output, () =>
                    {
                        var result = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(
                            new AddressablesDataBuilderInput(settings, (string)request["appVersion"]));
                        if (result == null || !string.IsNullOrEmpty(result.Error) || result.IsUpdateContentBuild)
                            throw new InvalidOperationException(result?.Error ?? "Full packed build returned no result.");
                        AddressableResourceBuilder.PrepareInstalledCatalogSettings();
                        var remote = Path.Combine(output, "remote");
                        var catalog = Directory.GetFiles(remote, "catalog*.bin").Single();
                        var bundles = (JArray)Call("VerifyCatalog", catalog, remote, request);
                        var state = ContentUpdateScript.GetContentStateDataPath(false, settings);
                        if (!File.Exists(state) || Path.GetFullPath(state) != Path.GetFullPath(Path.Combine(output, "state/addressables_content_state.bin")))
                            throw new IOException("Content state escaped the candidate output.");
                        var packet = new JObject { ["status"] = "LOCAL_WINDOWS_QA_BUILT_NOT_DEPLOYED_NOT_LIFECYCLE_VERIFIED",
                            ["configuration"] = request.DeepClone(), ["catalogUrl"] = (string)request["releaseNamespace"] + Path.GetFileName(catalog),
                            ["catalogHash"] = "sha256:" + LocalContentBaselineArchive.Digest(catalog), ["bundles"] = bundles,
                            ["sourceProvenance"] = "source-provenance.json", ["sourceKind"] = "WORKING_TREE_SNAPSHOT_NOT_CLEAN_COMMIT",
                            ["runtimeSettings"] = Path.Combine(output, "runtime-output/settings.json"), ["entryCount"] = roots.Length };
                        File.WriteAllText(Path.Combine(output, "candidate.pending.json"), packet.ToString());
                    });
                }
                finally
                {
                    foreach (var item in owned.AsEnumerable().Reverse()) UnityEngine.Object.DestroyImmediate(item);
                    if (sourceObjects.Any(p => EditorJsonUtility.ToJson(p.Key) != p.Value))
                        throw new InvalidOperationException("Source settings changed during PC QA build.");
                    Call("VerifySource", request, project);
                }
            }
            var publicFiles = new JArray(Directory.GetFiles(Path.Combine(output, "remote")).OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => new JObject { ["file"] = Path.GetFileName(p), ["path"] = Path.GetFullPath(p).Replace('\\', '/'),
                    ["bytes"] = new FileInfo(p).Length, ["sha256"] = LocalContentBaselineArchive.Digest(p) }));
            File.WriteAllText(Path.Combine(output, "upload-manifest.json"), new JObject { ["environment"] = "DEV",
                ["target"] = "StandaloneWindows64", ["releaseId"] = (string)request["releaseId"], ["publicFiles"] = publicFiles }.ToString());
            File.Move(Path.Combine(output, "candidate.pending.json"), Path.Combine(output, "candidate.json"));
            Debug.Log("WINDOWS_DEV_QA_CANDIDATE " + output);
        }

        private static void PreserveWindowsRuntime(string project, string output, Action build)
        {
            var state = LocalContentBaselineArchive.Child(project, "Library/com.unity.addressables/Windows/addressables_content_state.bin");
            var previous = File.Exists(state) ? File.ReadAllBytes(state) : null;
            var fallback = LocalContentBaselineArchive.Child(project, "Assets/AddressableAssetsData/Windows/addressables_content_state.bin");
            // Protect the existing source state if the installed package attempts its fallback path.
            try
            {
                using (new FileStream(fallback, FileMode.Open, FileAccess.Read, FileShare.Read))
                    DevContentBuild.WithRuntimeOutput(project, Path.GetFullPath(Addressables.BuildPath), output, build);
            }
            finally
            {
                if (previous != null) File.WriteAllBytes(state, previous);
                else if (File.Exists(state)) File.Delete(state);
            }
        }

        // Reuse existing guarded clone/catalog/provenance checks without relaxing the Android public entrypoint.
        private static object Call(string name, params object[] args)
        {
            var method = typeof(DevContentBuild).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new MissingMethodException(typeof(DevContentBuild).FullName, name);
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
    }
}
#endif
