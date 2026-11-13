#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using LastRail.Resources;
using LastRail.Presentation.Startup;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace LastRail.Editor
{
    public static class AddressableResourceBuilder
    {
        private const string Root = "Assets/_LASTRAIL/";
        private const string Startup = Root + "Prefabs/Screens/StartupScreen.prefab";
        private const string ScenePath = Root + "Scenes/ResourceBootstrap.unity";
        private const string Qa = "QA/Act1Launch/Addressables";
        private static readonly HashSet<string> RuntimeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".png", ".wav", ".prefab", ".anim", ".controller", ".spriteatlas", ".mat", ".asset", ".otf", ".unity" };

        [MenuItem("LAST RAIL/Resources/Configure Addressables")]
        public static void Configure()
        {
            DevContentBuild.AssertLegacyConfigurationAllowed(AddressableAssetSettingsDefaultObject.Settings);
            Directory.CreateDirectory(Qa);
            PrepareBootstrapFont();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create("Assets/AddressableAssetsData", "AddressableAssetSettings", true, true);
                AddressableAssetSettingsDefaultObject.Settings = settings;
            }
            ConfigureProfiles(settings);
            settings.BuildRemoteCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.CatalogRequestsTimeout = 20;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.ActivePlayModeDataBuilderIndex = 2;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            ProjectConfigData.GenerateBuildLayout = true;
            ProjectConfigData.BuildLayoutReportFileFormat = ProjectConfigData.ReportFileFormat.JSON;

            var local = new HashSet<string>(AssetDatabase.GetDependencies(Startup, true));
            local.UnionWith(AssetDatabase.GetDependencies(OfflineBootstrapResources.DirectAssetPaths.Concat(RemoteContentCatalog.Entries.Where(e => e.Local).Select(e => e.Path)).ToArray(), true));
            foreach (var path in AssetDatabase.FindAssets("", new[] { "Assets/TextMesh Pro/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
                foreach (var dependency in AssetDatabase.GetDependencies(path, true)) local.Add(dependency);
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(Root + "UI/Branding/Android/") || p == Root + "UI/Branding/splash_logo_candidate.png"))
                local.Add(path);

            var inventory = new List<Entry>();
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(Root)).OrderBy(p => p, StringComparer.Ordinal))
            {
                if (AssetDatabase.IsValidFolder(path) || !RuntimeExtensions.Contains(Path.GetExtension(path))) continue;
                if (Excluded(path)) { inventory.Add(Record(path, "excluded", "source/editor/QA material")); continue; }
                if (local.Contains(path)) { inventory.Add(Record(path, "local_bootstrap", "startup/login/error/update or PlayerSettings/Resources dependency")); continue; }
                var groupName = GroupFor(path);
                var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.UseUnityWebRequestForLocalBundles = true;
                schema.Timeout = 30;
                var providerType = schema.AssetBundleProviderType;
                providerType.Value = typeof(CacheOnlyAssetBundleProvider);
                schema.AssetBundleProviderType = providerType;
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.address = RemoteContentCatalog.Entries.FirstOrDefault(e => e.Path == path)?.Address ?? path;
                entry.SetLabel(groupName, true, true);
                entry.SetLabel(AddressableContentBackend.RequiredLabel, true, true);
                inventory.Add(Record(path, groupName, "stable asset-path key; internal prefab dependencies remain bundled"));
            }
            var remote = inventory.Where(e => e.group.StartsWith("remote_")).Select(e => e.path).ToArray();
            var excludedDeps = AssetDatabase.GetDependencies(remote, true).Where(Excluded).ToArray();
            if (excludedDeps.Length > 0) throw new InvalidOperationException("Excluded production dependencies: " + string.Join(",", excludedDeps));
            CreateBootstrap();
            var sceneDeps = AssetDatabase.GetDependencies(ScenePath, true);
            var leaked = sceneDeps.Intersect(remote).ToArray();
            if (leaked.Length != 0) throw new InvalidOperationException("Remote assets pulled into bootstrap: " + string.Join(",", leaked));
            var shared = AssetDatabase.GetDependencies(remote, true).Intersect(local).Where(p => RuntimeExtensions.Contains(Path.GetExtension(p))).ToArray();
            File.WriteAllText(Qa + "/resource-inventory.json", JsonUtility.ToJson(new Inventory
            {
                unity = Application.unityVersion, entries = inventory.ToArray(), bootstrapDependencies = sceneDeps,
                sharedLocalRemoteDependencies = shared, remoteCount = remote.Length,
                remoteSourceBytes = inventory.Where(e => e.group.StartsWith("remote_")).Sum(e => e.bytes)
            }, true));
            AssetDatabase.SaveAssets();
            Debug.Log("ADDRESSABLE_CONFIGURED remote=" + remote.Length + " sharedLocalRemote=" + shared.Length);
        }

        private static bool Excluded(string path) => path.Contains("/Sources/") || path.Contains("/Showcase/") || path.Contains("Showcase")
            || path.Contains("/Tests/") || path.Contains("/Editor/") || path.Contains("/Docs/") || path.Contains("contact_sheet")
            || (path.EndsWith(".unity") && path != AddressableContentBackend.MainSceneKey);

        internal static bool ExcludedProductionPath(string path) => Excluded(path);

        private static string GroupFor(string path)
        {
            if (path == LastRail.Presentation.MissionShell.EquipmentIconResources.AtlasAddress) return "remote_equipment_icons";
            if (path.Contains("/Audio/")) return "remote_audio";
            if (path.Contains("Gacha")) return "remote_gacha";
            if (path.Contains("RaidBoss") || path.Contains("boss_")) return "remote_act1_raid_boss";
            if (path.Contains("Enemies") || path.Contains("enemy_")) return "remote_act1_combat_enemies";
            if (path.Contains("/Animation/") || path.Contains("/CombatAnimations/")) return "remote_act1_combat_operators";
            if (path.Contains("/Operators/")) return "remote_act1_operators";
            if (path.Contains("MissionStory") || path.Contains("/KeyVisuals/") || path.Contains("/Backgrounds/")) return "remote_act1_missions";
            return "remote_launch_required";
        }

        private static void PrepareBootstrapFont()
        {
            const string path = Root + "UI/StartupResources/BootstrapFont.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "UI/Fonts/Pretendard/Pretendard-Regular.otf");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = "BootstrapFont";
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                AssetDatabase.CreateAsset(font, path);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
                font.TryAddCharacters("LAST RAIL Google0123456789.% MiB\uC2DC\uC791\uAC8C\uC2A4\uD2B8\uB85C\uACC4\uC18D\uC900\uBE44\uC911\uB85C\uADF8\uC778\uB2E4\uC6B4\uB85C\uB4DC\uB9AC\uC18C\uC2A4\uC624\uB958\uB124\uD2B8\uC6CC\uD06C\uCDE8\uC18C\uD655\uC778\uC5C5\uB370\uC774\uD2B8\uC54C\uB9BC");
                EditorUtility.SetDirty(font);
            }
            var root = PrefabUtility.LoadPrefabContents(Startup);
            try
            {
                ApplyBootstrapFont(root);
                PrefabUtility.SaveAsPrefabAsset(root, Startup);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        public static void ApplyBootstrapFont(GameObject root)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "UI/StartupResources/BootstrapFont.asset");
            if (font == null) return;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
        }

        private static void ConfigureProfiles(AddressableAssetSettings settings)
        {
            DevContentBuild.AssertLegacyConfigurationAllowed(settings);
            foreach (var name in new[] { "LOCAL_TEST", "DEV", "PRD" })
            {
                var id = settings.profileSettings.GetProfileId(name);
                if (string.IsNullOrEmpty(id)) id = settings.profileSettings.AddProfile(name, settings.activeProfileId);
                settings.profileSettings.SetValue(id, AddressableAssetSettings.kRemoteBuildPath, "ServerData/" + name + "/[BuildTarget]");
                settings.profileSettings.SetValue(id, AddressableAssetSettings.kRemoteLoadPath,
                    name == "LOCAL_TEST" ? "http://127.0.0.1:18284/[BuildTarget]" : "UNCONFIGURED_" + name + "_CONTENT_HOST/[BuildTarget]");
            }
            settings.activeProfileId = settings.profileSettings.GetProfileId("LOCAL_TEST");
        }

        private static void CreateBootstrap()
        {
            DevContentBuild.AssertLegacyConfigurationAllowed(AddressableAssetSettingsDefaultObject.Settings);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var owner = new GameObject("ResourceBootstrap");
            owner.SetActive(false);
            var startup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Startup), scene);
            var host = new GameObject("AddressableScreenHost").AddComponent<AddressableScreenHost>();
            var controller = owner.AddComponent<ResourceBootstrapController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("startup").objectReferenceValue = startup.GetComponent<StartupScreenRootBehaviour>();
            serialized.FindProperty("screens").objectReferenceValue = host;
            serialized.FindProperty("remoteHostingConfigured").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var camera=new GameObject("StartupDisplayCamera").AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.025f,.03f);camera.cullingMask=0;camera.depth=-100;
            owner.SetActive(true);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("LAST RAIL/Resources/Build LOCAL_TEST Content")]
        public static void BuildLocalContent()
        {
            BuildLocalFullAndArchive("full_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"));
        }

        public static string BuildLocalFullAndArchive(string fullId)
        {
            ValidateBuildId(fullId);
            var settings = LocalSettings();
            RequireUnusedArchive(fullId);
            var identity = BaselineIdentity(settings);
            BuildFullContentCore();
            var state = ContentUpdateScript.GetContentStateDataPath(false, settings);
            RequireProjectPath(state);
            var archive = LocalContentBaselineArchive.Archive(BaselineRoot, fullId, state,
                Path.GetFullPath(Addressables.BuildPath), RemoteOutput(settings), identity, null);
            Debug.Log("CONTENT_BASELINE_FULL_OK " + archive);
            return archive;
        }

        public static string BuildLocalUpdate(string fullId, string updateId)
        {
            ValidateBuildId(fullId); ValidateBuildId(updateId);
            var settings = LocalSettings();
            RequireUnusedArchive(updateId);
            var identity = BaselineIdentity(settings);
            var state = LocalContentBaselineArchive.Validate(BaselineRoot, fullId, identity);
            var before = FileHash(state);
            var working = Path.GetFullPath(Path.Combine(Qa, "UpdateWorking", updateId));
            RequireProjectPath(working);
            if (Directory.Exists(working)) throw new IOException("Update working ID already exists.");
            Directory.CreateDirectory(working);
            var input = Path.Combine(working, "state.bin");
            File.Copy(state, input, false);
            if (FileHash(input) != before) throw new IOException("Baseline input copy changed.");
            var previousBuild = settings.profileSettings.GetValueByName(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath);
            try
            {
                settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath,
                    Path.Combine(working, "remote").Replace('\\', '/'));
                var result = ContentUpdateScript.BuildContentUpdate(settings, input);
                if (result == null || !string.IsNullOrEmpty(result.Error) || !result.IsUpdateContentBuild)
                    throw new InvalidOperationException(result?.Error ?? "Content update did not produce an update result.");
                PrepareInstalledCatalogSettings();
                if (FileHash(state) != before || FileHash(input) != before) throw new IOException("Content update changed its baseline.");
                LocalContentBaselineArchive.Validate(BaselineRoot, fullId, identity);
                var archive = LocalContentBaselineArchive.Archive(BaselineRoot, updateId, input,
                    Path.GetFullPath(Addressables.BuildPath), RemoteOutput(settings), identity, fullId);
                Debug.Log("CONTENT_BASELINE_UPDATE_OK " + archive + " baseline=" + fullId + " stateSha256=" + before);
                return archive;
            }
            finally
            {
                settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath, previousBuild);
                EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            }
        }

        public static void VerifyLocalBaselineBuild()
        {
            var prefix = "qa_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var first = BuildLocalFullAndArchive(prefix + "_A");
            var manifestHash = FileHash(Path.Combine(first, "manifest.json"));
            var second = BuildLocalFullAndArchive(prefix + "_B");
            var secondHash = FileHash(Path.Combine(second, "manifest.json"));
            BuildLocalUpdate(prefix + "_A", prefix + "_updateA");
            var identity = BaselineIdentity(LocalSettings());
            LocalContentBaselineArchive.Validate(BaselineRoot, prefix + "_A", identity);
            LocalContentBaselineArchive.Validate(BaselineRoot, prefix + "_B", identity);
            if (manifestHash != FileHash(Path.Combine(first, "manifest.json")) || secondHash != FileHash(Path.Combine(second, "manifest.json")))
                throw new IOException("Full archive manifest changed.");
            Debug.Log("CONTENT_BASELINE_QA_OK " + prefix + " fullA+fullB+explicitUpdateA unchanged-content-update");
        }

        private static string BaselineRoot => Path.GetFullPath(Path.Combine(Qa, "Baselines"));
        private static void ValidateBuildId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 120 || id.Any(c => !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')
                && !(c >= '0' && c <= '9') && c != '_' && c != '-')) throw new ArgumentException("An explicit safe build ID is required.");
        }
        private static void RequireUnusedArchive(string id)
        {
            RequireProjectPath(BaselineRoot);
            if (Directory.Exists(Path.Combine(BaselineRoot, id)) || File.Exists(Path.Combine(BaselineRoot, id)))
                throw new IOException("Content archive ID already exists.");
        }
        private static void RequireProjectPath(string path)
        {
            var project = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(path);
            if (!full.StartsWith(project, StringComparison.OrdinalIgnoreCase) || full.Contains("://"))
                throw new IOException("Content outputs must remain in the local project.");
            for (var cursor = full; !string.IsNullOrEmpty(cursor); cursor = Path.GetDirectoryName(cursor))
                if ((File.Exists(cursor) || Directory.Exists(cursor)) && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Reparse paths are not content outputs.");
        }
        private static AddressableAssetSettings LocalSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || settings.activeProfileId != settings.profileSettings.GetProfileId("LOCAL_TEST"))
                throw new InvalidOperationException("Only configured LOCAL_TEST content builds are authorized.");
            var remoteBuild = AddressableAssetSettings.kRemoteBuildPath;
            var remoteLoad = AddressableAssetSettings.kRemoteLoadPath;
            var localBuild = AddressableAssetSettings.kLocalBuildPath;
            var localLoad = AddressableAssetSettings.kLocalLoadPath;
            if (settings.RemoteCatalogBuildPath.GetName(settings) != remoteBuild || settings.RemoteCatalogLoadPath.GetName(settings) != remoteLoad)
                throw new InvalidOperationException("Catalog paths must reference the configured remote profile variables.");
            foreach (var group in settings.groups.Where(g => g != null && g.entries.Count > 0))
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null) continue;
                if (!(schema.BuildPath.GetName(settings) == remoteBuild && schema.LoadPath.GetName(settings) == remoteLoad)
                    && !(schema.BuildPath.GetName(settings) == localBuild && schema.LoadPath.GetName(settings) == localLoad))
                    throw new InvalidOperationException("Group path overrides are not authorized for baseline builds.");
                RequireProjectPath(schema.BuildPath.GetValue(settings));
            }
            var stateDirectory = settings.profileSettings.EvaluateString(settings.activeProfileId, settings.ContentStateBuildPath);
            if (string.IsNullOrEmpty(stateDirectory)) stateDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(settings));
            RequireProjectPath(stateDirectory);
            RemoteOutput(settings);
            BaselineIdentity(settings);
            return settings;
        }
        private static string RemoteOutput(AddressableAssetSettings settings)
        {
            var path = Path.GetFullPath(settings.RemoteCatalogBuildPath.GetValue(settings));
            RequireProjectPath(path); return path;
        }
        private static string BaselineIdentity(AddressableAssetSettings settings)
        {
            var load = settings.RemoteCatalogLoadPath.GetValue(settings);
            if (!Uri.TryCreate(load, UriKind.Absolute, out var uri) || uri.Scheme != "http" || uri.Host != "127.0.0.1"
                || !string.IsNullOrEmpty(uri.UserInfo)) throw new InvalidOperationException("LOCAL_TEST requires a loopback catalog URL.");
            return new JObject {
                ["profile"] = "LOCAL_TEST", ["profileId"] = settings.activeProfileId,
                ["target"] = EditorUserBuildSettings.activeBuildTarget.ToString(), ["unity"] = Application.unityVersion,
                ["addressables"] = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AddressableAssetSettings).Assembly).version,
                ["settingsGuid"] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings)), ["catalogLoadPath"] = load
            }.ToString(Newtonsoft.Json.Formatting.None);
        }
        private static string FileHash(string path)
        { using (var sha = SHA256.Create()) using (var input = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); }

        private static void BuildFullContentCore()
        {
            var settings = LocalSettings();
            settings.ActivePlayModeDataBuilderIndex = 2;
            EditorUtility.SetDirty(settings);
            settings.DisableCatalogUpdateOnStartup = true;
            AssetDatabase.SaveAssets();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
            PrepareInstalledCatalogSettings();
            Debug.Log("ADDRESSABLE_CONTENT_BUILD_OK " + result.OutputPath);
        }

        internal static void PrepareInstalledCatalogSettings()
        {
            // Keep the generated remote catalog artifact, but remove hash/cache selection from installed startup.
            var path = Path.Combine(Addressables.BuildPath, "settings.json");
            var data = JsonUtility.FromJson<ResourceManagerRuntimeData>(File.ReadAllText(path));
            var catalogs = data.CatalogLocations.Where(location => location.Keys.Contains(ResourceManagerRuntimeData.kCatalogAddress)).ToArray();
            if (catalogs.Length != 1 || !catalogs[0].InternalId.StartsWith("{UnityEngine.AddressableAssets.Addressables.RuntimePath}/", StringComparison.Ordinal))
                throw new InvalidOperationException("Content build did not generate exactly one installed local catalog.");
            var installed = new ResourceLocationData(new[] { ResourceManagerRuntimeData.kCatalogAddress },
                catalogs[0].InternalId, typeof(ContentCatalogProvider), typeof(ContentCatalogData));
            data.CatalogLocations.Clear();
            data.CatalogLocations.Add(installed);
            data.DisableCatalogUpdateOnStartup = true;
            ApprovedCatalogSafety.ValidateInstalledRuntimeData(data,
                id => ApprovedCatalogSafety.NormalizeInstalledPath(Addressables.ResolveInternalId(id)));
            File.WriteAllText(path, JsonUtility.ToJson(data));
        }

        private static Entry Record(string path, string group, string reason) => new Entry
            { path = path, key = path, group = group, reason = reason, bytes = File.Exists(path) ? new FileInfo(path).Length : 0 };
        [Serializable] private sealed class Entry { public string path, key, group, reason; public long bytes; }
        [Serializable] private sealed class Inventory
        {
            public string unity;
            public Entry[] entries;
            public string[] bootstrapDependencies, sharedLocalRemoteDependencies;
            public int remoteCount;
            public long remoteSourceBytes;
        }
    }
}
#endif
