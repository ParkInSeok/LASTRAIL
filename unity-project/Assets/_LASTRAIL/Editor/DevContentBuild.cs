#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LastRail.Client;
using LastRail.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Editor
{
    // Explicit local build API only: no menu defaults, endpoint writes or APK generation.
    public static class DevContentBuild
    {
        private static bool configuring;
        private static readonly string[] SourceRoots = { "Assets", "Packages", "ProjectSettings", "Tools" };
        internal static void AssertLegacyConfigurationAllowed(AddressableAssetSettings settings)
        {
            var dev = settings?.profileSettings.GetProfileId("DEV");
            var load = string.IsNullOrEmpty(dev) ? null : settings.profileSettings.GetValueByName(dev, AddressableAssetSettings.kRemoteLoadPath);
            if (configuring || (load != null && load.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Generic configuration must not overwrite explicit DEV configuration.");
        }

        public static string Build(string approvedConfigurationJson)
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            var request = Validate(approvedConfigurationJson, project);
            if (Application.platform != RuntimePlatform.WindowsEditor || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                || PlayerSettings.bundleVersion != Text(request, "appVersion")
                || PlayerSettings.Android.bundleVersionCode != (int)request["versionCode"])
                throw new InvalidOperationException("Android target and stored app version must already match the approved input.");
            WithFallbackStateGuard(project, () => WithConfiguration(approvedConfigurationJson, project, AddressableAssetSettingsDefaultObject.Settings,
                (settings, output) => BuildContent(settings, output, request, project)));
            var destination = Output(request, project);
            File.Move(Path.Combine(destination, "candidate.pending.json"), Path.Combine(destination, "candidate.json"));
            return destination;
        }

        internal static JObject Validate(string json, string project)
        {
            var r = Parse(json);
            var keys = new[] { "environment", "target", "trustedCatalogRoot", "releaseNamespace", "releaseId", "contentVersion",
                "appVersion", "versionCode", "minimumVersionCode", "maximumVersionCode", "sourceCommit", "sourceManifest",
                "sourceManifestSha256", "outputId" };
            if (r.Properties().Any(p => !keys.Contains(p.Name)) || keys.Where(k => k != "maximumVersionCode").Any(k => r[k] == null))
                throw new InvalidDataException("Explicit DEV configuration fields are required.");
            if (Text(r, "environment") != "DEV" || Text(r, "target") != "Android")
                throw new InvalidDataException("This entrypoint only accepts DEV Android input.");
            var id = Text(r, "releaseId"); SafeId(id); SafeId(Text(r, "contentVersion")); SafeId(Text(r, "outputId"));
            var app = Text(r, "appVersion");
            if (!Regex.IsMatch(app, "\\A[A-Za-z0-9][A-Za-z0-9._+-]{0,63}\\z")) throw new InvalidDataException("App version is required.");
            var code = Positive(r, "versionCode"); var min = Positive(r, "minimumVersionCode");
            var max = r["maximumVersionCode"] == null || r["maximumVersionCode"].Type == JTokenType.Null
                ? int.MaxValue : Positive(r, "maximumVersionCode");
            if (min > code || max < code || min > max) throw new InvalidDataException("Inconsistent app compatibility input.");
            var root = Text(r, "trustedCatalogRoot"); var release = Text(r, "releaseNamespace");
            if (!release.EndsWith("/", StringComparison.Ordinal) || !release.EndsWith("/" + id + "/", StringComparison.Ordinal)
                || root == release || Placeholder(root) || Placeholder(release)
                || root.Contains("<") || root.Contains(">") || release.Contains("<") || release.Contains(">"))
                throw new InvalidDataException("An explicit release namespace below the trusted root is required.");
            // Reuse the wire validator without fetching or granting runtime authority.
            var approval = new JObject {
                ["accepted"] = true, ["clientAdmission"] = new JObject { ["status"] = "ACCEPTED", ["updateRequired"] = false,
                    ["installedVersionCode"] = code.ToString(System.Globalization.CultureInfo.InvariantCulture), ["minimumVersionCode"] = min.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                ["maintenance"] = new JObject { ["enabled"] = false },
                ["contentRelease"] = new JObject { ["status"] = "APPROVED_COMPATIBLE", ["releaseId"] = id,
                    ["contentVersion"] = Text(r, "contentVersion"), ["catalogUrl"] = release + "catalog.bin",
                    ["catalogHash"] = "sha256:" + new string('a', 64), ["compatibility"] = new JObject {
                        ["minimumVersionCode"] = min.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["maximumVersionCode"] = max.ToString(System.Globalization.CultureInfo.InvariantCulture) } }
            };
            ClientAdmissionContract.Parse(approval.ToString(), code.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClientEnvironmentBootstrap.DevClientConfigUrl, root);
            var output = Output(r, project);
            if (File.Exists(output) || Directory.Exists(output) || File.Exists(output + ".lock") || Directory.Exists(output + ".lock"))
                throw new IOException("Candidate output already exists or is reserved.");
            VerifySource(r, project);
            return r;
        }

        internal static void WithConfiguration(string json, string project, AddressableAssetSettings source,
            Action<AddressableAssetSettings, string> build)
        {
            if (configuring) throw new InvalidOperationException("A DEV configuration scope is already active.");
            var request = Validate(json, project);
            if (source == null || build == null) throw new ArgumentException("Explicit settings and build operation are required.");
            ValidateSettings(source);
            var roots = source.groups.Where(g => g != null && g.GetSchema<BundledAssetGroupSchema>()?.IncludeInBuild == true)
                .SelectMany(g => g.entries).Select(e => e.AssetPath).ToArray();
            ValidateDependencies(roots);
            ValidateDependencies(AssetDatabase.GetDependencies(roots, true));
            if (!roots.Contains(AddressableContentBackend.LobbyKey)) throw new InvalidDataException("Shipping lobby is missing.");
            if (!roots.Contains(AddressableContentBackend.MainSceneKey)) throw new InvalidDataException("Shipping Main scene is missing.");
            var output = Output(request, project);
            var before = EditorJsonUtility.ToJson(source);
            var sourceObjects = source.groups.Where(g => g != null).Cast<UnityEngine.Object>()
                .Concat(source.groups.Where(g => g != null).SelectMany(g => g.Schemas)).Distinct()
                .ToDictionary(o => o, o => EditorJsonUtility.ToJson(o));
            var owned = new List<UnityEngine.Object>();
            configuring = true;
            try
            {
                var clone = CloneSettings(source, owned);
                var profile = clone.profileSettings.GetProfileId("DEV");
                if (string.IsNullOrEmpty(profile)) throw new InvalidDataException("DEV profile must exist.");
                clone.activeProfileId = profile;
                clone.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath, Text(request, "releaseNamespace"));
                clone.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, Path.Combine(output, "remote").Replace('\\', '/'));
                clone.profileSettings.SetValue(profile, AddressableAssetSettings.kLocalBuildPath, "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]");
                clone.profileSettings.SetValue(profile, AddressableAssetSettings.kLocalLoadPath, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]");
                clone.ContentStateBuildPath = Path.Combine(output, "state");
                clone.buildSettings.bundleBuildPath = Path.Combine(output, "intermediate").Replace('\\', '/');
                clone.BuildRemoteCatalog = true;
                clone.DisableCatalogUpdateOnStartup = true;
                clone.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                // Reserve a new directory only after every input/dependency check has passed.
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var reservation = output + ".lock";
                using (new FileStream(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Candidate output appeared during validation.");
                    Directory.CreateDirectory(output);
                    var provenance = Path.Combine(output, "source-provenance.json");
                    File.Copy(Text(request, "sourceManifest"), provenance, false);
                    if (Digest(provenance) != Text(request, "sourceManifestSha256")) throw new IOException("Source provenance copy changed.");
                    build(clone, output);
                }
            }
            finally
            {
                foreach (var item in owned.AsEnumerable().Reverse()) UnityEngine.Object.DestroyImmediate(item);
                configuring = false;
                if (EditorJsonUtility.ToJson(source) != before || sourceObjects.Any(p => EditorJsonUtility.ToJson(p.Key) != p.Value))
                    throw new InvalidOperationException("Source settings changed during the DEV build scope: "
                        + ChangedFields(before, EditorJsonUtility.ToJson(source)) + "; "
                        + string.Join("; ", sourceObjects.Where(p => EditorJsonUtility.ToJson(p.Key) != p.Value)
                            .Select(p => p.Key.name + ":" + ChangedFields(p.Value, EditorJsonUtility.ToJson(p.Key)))));
                VerifySource(request, project);
            }
        }

        private static string ChangedFields(string before, string after)
        {
            var a = JObject.Parse(before); var b = JObject.Parse(after);
            a = a["MonoBehaviour"] as JObject ?? a;
            b = b["MonoBehaviour"] as JObject ?? b;
            return string.Join(",", a.Properties().Select(p => p.Name).Union(b.Properties().Select(p => p.Name))
                .Where(k => !JToken.DeepEquals(a[k], b[k])));
        }

        internal static void ValidateDependencies(string[] paths)
        {
            if (paths == null || paths.Any(p => string.IsNullOrWhiteSpace(p) || AssetDatabase.IsValidFolder(p) || AddressableResourceBuilder.ExcludedProductionPath(p)
                || p.Replace('\\', '/').Split('/').Any(s => new[] { "qa", "tests", "editor", "docs", "sources", "showcase", "generateddelta" }.Contains(s.ToLowerInvariant()))))
                throw new InvalidDataException("Shipping dependency closure contains excluded input.");
        }

        internal static AddressableAssetSettings CloneSettings(AddressableAssetSettings source, List<UnityEngine.Object> owned)
        {
            var clone = AddressableAssetSettings.Create("", "DevContentBuildScope", false, false); owned.Add(clone);
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), clone);
            clone.groups.Clear();
            foreach (var original in source.groups.Where(g => g != null))
            {
                // Unity object references are not deep-cloned by Instantiate(settings).
                // Rebind every group/schema; JSON serialization deep-copies its entry data.
                var group = ScriptableObject.CreateInstance<AddressableAssetGroup>(); owned.Add(group);
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(original), group);
                var serialized = new SerializedObject(group);
                serialized.FindProperty("m_Settings").objectReferenceValue = clone;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                group.Schemas.Clear();
                foreach (var originalSchema in original.Schemas)
                {
                    var schema = ScriptableObject.CreateInstance(originalSchema.GetType()) as AddressableAssetGroupSchema; owned.Add(schema);
                    EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(originalSchema), schema);
                    var schemaData = new SerializedObject(schema);
                    schemaData.FindProperty("m_Group").objectReferenceValue = group;
                    schemaData.ApplyModifiedPropertiesWithoutUndo();
                    group.Schemas.Add(schema);
                }
                group.OnAfterDeserialize();
                clone.groups.Add(group);
            }
            for (var i = 0; i < clone.DataBuilders.Count; i++)
            {
                if (clone.DataBuilders[i] == null) continue;
                clone.DataBuilders[i] = UnityEngine.Object.Instantiate(clone.DataBuilders[i]); owned.Add(clone.DataBuilders[i]);
            }
            // SBP scene cleanup/unloading must retain these unsaved objects until the scope destroys them.
            foreach (var item in owned) item.hideFlags |= HideFlags.HideAndDontSave;
            return clone;
        }
        private static void ValidateSettings(AddressableAssetSettings settings)
        {
            if (settings.EnableJsonCatalog || settings.InitializationObjects.Count != 0)
                throw new InvalidDataException("DEV builds require the installed binary catalog mode with no initialization objects.");
#if ENABLE_JSON_CATALOG
            throw new InvalidDataException("DEV binary catalog builds cannot run with ENABLE_JSON_CATALOG.");
#endif
            if (settings.RemoteCatalogBuildPath.GetName(settings) != AddressableAssetSettings.kRemoteBuildPath
                || settings.RemoteCatalogLoadPath.GetName(settings) != AddressableAssetSettings.kRemoteLoadPath)
                throw new InvalidDataException("Catalog profile overrides are not permitted.");
            foreach (var group in settings.groups.Where(g => g != null))
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null || !schema.IncludeInBuild) continue;
                var remote = schema.BuildPath.GetName(settings) == AddressableAssetSettings.kRemoteBuildPath
                    && schema.LoadPath.GetName(settings) == AddressableAssetSettings.kRemoteLoadPath;
                var local = schema.BuildPath.GetName(settings) == AddressableAssetSettings.kLocalBuildPath
                    && schema.LoadPath.GetName(settings) == AddressableAssetSettings.kLocalLoadPath;
                if (!remote && !local) throw new InvalidDataException("Group profile overrides are not permitted: " + group.Name
                    + " (" + schema.BuildPath.GetName(settings) + ", " + schema.LoadPath.GetName(settings) + ").");
                if (remote && schema.AssetBundleProviderType.Value != typeof(CacheOnlyAssetBundleProvider))
                    throw new InvalidDataException("Remote content must use the cache-only provider.");
            }
        }

        private static void BuildContent(AddressableAssetSettings settings, string output, JObject request, string project)
        {
            var runtime = Path.GetFullPath(Addressables.BuildPath);
            WithRuntimeOutput(project, runtime, output, () => {
                Directory.CreateDirectory(LocalContentBaselineArchive.Child(output, "state"));
                var result = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(CreateBuildInput(settings, request));
                if (result == null || !string.IsNullOrEmpty(result.Error) || result.IsUpdateContentBuild)
                    throw new InvalidOperationException(result?.Error ?? "DEV content build did not return a full result.");
                AddressableResourceBuilder.PrepareInstalledCatalogSettings();
                var remote = Path.Combine(output, "remote");
                var catalog = Directory.GetFiles(remote, "catalog*.bin").Single();
                var bundles = VerifyCatalog(catalog, remote, request);
                var state = ContentUpdateScript.GetContentStateDataPath(false, settings);
                if (!string.Equals(Path.GetFullPath(state), Path.GetFullPath(Path.Combine(output, "state", "addressables_content_state.bin")), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Content state fell back outside candidate output.");
                var identity = new JObject { ["request"] = request.DeepClone(), ["unity"] = Application.unityVersion,
                    ["addressables"] = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AddressableAssetSettings).Assembly).version };
                var archive = LocalContentBaselineArchive.Archive(Path.Combine(output, "archives"), "content", state, runtime, remote, identity.ToString(Formatting.None), null);
                var packet = new JObject { ["status"] = "LOCAL_BUILT_NOT_DEPLOYED_NOT_DEVICE_VERIFIED", ["configuration"] = request.DeepClone(),
                    ["archive"] = archive, ["catalogUrl"] = Text(request, "releaseNamespace") + Path.GetFileName(catalog),
                    ["catalogHash"] = "sha256:" + Digest(catalog), ["bundles"] = bundles,
                    ["sourceProvenance"] = "source-provenance.json",
                    ["bootstrapRequirements"] = new JObject { ["define"] = "LASTRAIL_ENV_DEV", ["remoteHostingConfigured"] = true,
                        ["trustedCatalogRoot"] = Text(request, "trustedCatalogRoot"), ["appliedToSource"] = false } };
                // Publish after restoration and provenance verification in Build, not inside the callback.
                WriteNew(Path.Combine(output, "candidate.pending.json"), packet.ToString());
            });
        }

        internal static void WithRuntimeOutput(string project, string runtime, string output, Action build)
        {
            var prefix = Path.GetFullPath(project).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            runtime = LocalContentBaselineArchive.LocalPath(runtime);
            output = LocalContentBaselineArchive.LocalPath(output);
            if (!runtime.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || runtime == output || runtime.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || output.StartsWith(runtime + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Runtime and output paths must be separate project-local directories.");
            var backup = LocalContentBaselineArchive.Child(output, "previous-runtime");
            var generated = LocalContentBaselineArchive.Child(output, "runtime-output");
            if (File.Exists(backup) || Directory.Exists(backup) || File.Exists(generated) || Directory.Exists(generated))
                throw new IOException("Runtime preservation destination already exists.");
            var transient = LocalContentBaselineArchive.Child(project, "Library/com.unity.addressables/Android/addressables_content_state.bin");
            var priorState = File.Exists(transient) ? File.ReadAllBytes(transient) : null;
            var hadRuntime = Directory.Exists(runtime);
            if (hadRuntime) { LocalContentBaselineArchive.Files(runtime); Directory.Move(runtime, backup); }
            try { build(); }
            finally
            {
                var errors = new List<Exception>();
                try
                {
                    LocalContentBaselineArchive.LocalPath(runtime);
                    LocalContentBaselineArchive.LocalPath(generated);
                    if (Directory.Exists(runtime)) { LocalContentBaselineArchive.Files(runtime); Directory.Move(runtime, generated); }
                }
                catch (Exception e) { errors.Add(e); }
                try
                {
                    LocalContentBaselineArchive.LocalPath(backup);
                    LocalContentBaselineArchive.LocalPath(runtime);
                    if (hadRuntime) Directory.Move(backup, runtime);
                }
                catch (Exception e) { errors.Add(e); }
                try
                {
                    LocalContentBaselineArchive.LocalPath(transient);
                    if (priorState != null) File.WriteAllBytes(transient, priorState);
                    else if (File.Exists(transient)) File.Delete(transient);
                }
                catch (Exception e) { errors.Add(e); }
                if (errors.Count != 0) throw new AggregateException("Runtime restoration incomplete; original backup retained at " + backup, errors);
            }
        }

        internal static AddressablesDataBuilderInput CreateBuildInput(AddressableAssetSettings settings, JObject request) =>
            new AddressablesDataBuilderInput(settings, Text(request, "appVersion"));

        internal static void WithFallbackStateGuard(string project, Action build)
        {
            // Addressables 2.8.1 may fall back here when its explicit state directory fails.
            // Hold an existing source file read-only so File.Copy cannot overwrite it.
            // No sentinel/source file is created; an absent fallback fails before any build.
            var fallback = LocalContentBaselineArchive.Child(project,
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder + "/Android/addressables_content_state.bin");
            if (!File.Exists(fallback)) throw new IOException("A preserved fallback state file is required for the Windows write guard.");
            using (var guard = new FileStream(fallback, FileMode.Open, FileAccess.Read, FileShare.Read)) build();
        }

        private static JArray VerifyCatalog(string path, string remote, JObject request)
        {
            // Use the same installed binary reader seam as existing local catalog verification.
            var type = typeof(ContentCatalogData);
            var load = type.GetMethod("LoadFromFile", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
            if (load == null) throw new InvalidOperationException("Installed binary catalog reader is unavailable.");
            var data = load.Invoke(null, new object[] { path, true });
            var locator = (IResourceLocator)type.GetMethod("CreateCustomLocator", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(data, new object[] { "dev-build-verification", null });
            var assets = locator.Keys.SelectMany(k => locator.Locate(k, null, out var locations) ? locations : Array.Empty<IResourceLocation>())
                .Where(l => l.ProviderId == typeof(BundledAssetProvider).FullName).Distinct().ToArray();
            ValidateDependencies(assets.Select(a => a.InternalId).ToArray());
            VerifyRequiredLocations(locator, request);
            return VerifyBundleFiles(assets, remote, request);
        }

        internal static void VerifyRequiredLocations(IResourceLocator locator, JObject request)
        {
            if (!locator.Locate(AddressableContentBackend.LobbyKey, typeof(GameObject), out var lobby) || lobby.Count != 1
                || !locator.Locate(AddressableContentBackend.MainSceneKey, typeof(SceneInstance), out var main) || main.Count != 1
                || !locator.Locate(AddressableContentBackend.RequiredLabel, null, out var required) || required.Count == 0)
                throw new InvalidDataException("Shipping Main, lobby and required label must exist.");
            var root = Text(request, "trustedCatalogRoot");
            var plan = ApprovedCatalogSafety.BuildRemoteBundlePlan(required.ToArray(), root, l => Addressables.ResolveInternalId(l.InternalId));
            var lobbyPlan = ApprovedCatalogSafety.BuildRemoteBundlePlan(lobby.ToArray(), root, l => Addressables.ResolveInternalId(l.InternalId));
            var mainPlan = ApprovedCatalogSafety.BuildRemoteBundlePlan(main.ToArray(), root, l => Addressables.ResolveInternalId(l.InternalId));
            var requiredIds = new HashSet<string>(plan.Select(l => l.InternalId), StringComparer.Ordinal);
            if (lobbyPlan.Any(l => !requiredIds.Contains(l.InternalId))) throw new InvalidDataException("Required label does not cover lobby bundles.");
            if (mainPlan.Any(l => !requiredIds.Contains(l.InternalId))) throw new InvalidDataException("Required label does not cover Main bundles.");
        }

        internal static JArray VerifyBundleFiles(IResourceLocation[] assets, string remote, JObject request)
        {
            ValidateDependencies(assets.Select(a => a.InternalId).ToArray());
            var bundles = ApprovedCatalogSafety.BuildRemoteBundlePlan(assets, Text(request, "trustedCatalogRoot"),
                l => Addressables.ResolveInternalId(l.InternalId));
            var result = new JArray();
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var bundle in bundles)
            {
                var url = Addressables.ResolveInternalId(bundle.InternalId);
                if (!url.StartsWith(Text(request, "releaseNamespace"), StringComparison.Ordinal)) throw new InvalidDataException("Bundle escapes release namespace.");
                var relative = url.Substring(Text(request, "releaseNamespace").Length);
                if (relative.Contains("/")) throw new InvalidDataException("Unexpected nested bundle output.");
                var file = LocalContentBaselineArchive.Child(remote, relative);
                var options = (AssetBundleRequestOptions)bundle.Data;
                if (!listed.Add(file) || new FileInfo(file).Length != options.BundleSize) throw new InvalidDataException("Bundle file/size mismatch.");
                result.Add(new JObject { ["file"] = relative, ["bytes"] = options.BundleSize, ["sha256"] = Digest(file),
                    ["hash"] = options.Hash, ["crc"] = options.Crc, ["url"] = url });
            }
            if (!listed.SetEquals(Directory.GetFiles(remote, "*.bundle"))) throw new InvalidDataException("Unreferenced or missing remote bundle.");
            return result;
        }

        private static void VerifySource(JObject r, string project)
        {
            if (!Regex.IsMatch(Text(r, "sourceCommit"), "\\A[a-f0-9]{40}\\z")) throw new InvalidDataException("Source commit is required.");
            var path = LocalContentBaselineArchive.LocalPath(Text(r, "sourceManifest"));
            var expected = Text(r, "sourceManifestSha256");
            if (!Regex.IsMatch(expected, "\\A[a-f0-9]{64}\\z") || Digest(path) != expected) throw new InvalidDataException("Source manifest hash mismatch.");
            var manifest = Parse(File.ReadAllText(path));
            if (manifest.Count != 2 || Text(manifest, "commitSha") != Text(r, "sourceCommit") || !(manifest["files"] is JArray files))
                throw new InvalidDataException("Source provenance schema mismatch.");
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JObject entry in files)
            {
                var relative = Text(entry, "path");
                if (entry.Count != 3 || relative.Contains("\\") || !SourceRoots.Any(s => relative.StartsWith(s + "/", StringComparison.Ordinal)))
                    throw new InvalidDataException("Unexpected source path.");
                var file = LocalContentBaselineArchive.Child(project, relative);
                if (!listed.Add(file) || entry["length"]?.Type != JTokenType.Integer || new FileInfo(file).Length != (long)entry["length"]
                    || Digest(file) != Text(entry, "sha256")) throw new InvalidDataException("Source changed or provenance is incomplete.");
            }
            var actual = SourceRoots.SelectMany(s => LocalContentBaselineArchive.Files(LocalContentBaselineArchive.Child(project, s)));
            if (!listed.SetEquals(actual)) throw new InvalidDataException("Unlisted source inputs.");
        }
        private static string Output(JObject r, string project) => LocalContentBaselineArchive.Child(project, "QA/DevContentCandidates/" + Text(r, "outputId"));
        private static string Digest(string path) => LocalContentBaselineArchive.Digest(path);
        private static JObject Parse(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new InvalidDataException("Trailing configuration data.");
                return value;
            }
        }
        private static string Text(JObject r, string key)
        { if (r[key]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)r[key])) throw new InvalidDataException("Missing " + key); return (string)r[key]; }
        private static int Positive(JObject r, string key)
        { if (r[key]?.Type != JTokenType.Integer || (long)r[key] < 1 || (long)r[key] > int.MaxValue) throw new InvalidDataException("Invalid " + key); return (int)r[key]; }
        private static void SafeId(string value)
        {
            if (Placeholder(value) || !Regex.IsMatch(value, "\\A[A-Za-z0-9][A-Za-z0-9_-]{0,119}\\z")) throw new InvalidDataException("Unsafe identity.");
            LocalContentBaselineArchive.Child(Path.GetTempPath(), value);
        }
        private static bool Placeholder(string value) => Regex.IsMatch(value,
            "UNCONFIGURED|PLACEHOLDER|(^|[^A-Za-z0-9])(TBD|TODO)([^A-Za-z0-9]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static void WriteNew(string path, string value)
        { using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) using (var writer = new StreamWriter(file)) writer.Write(value); }
    }
}
#endif
