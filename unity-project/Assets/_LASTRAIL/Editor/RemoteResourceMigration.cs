#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LastRail.Resources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace LastRail.Editor
{
    // One-shot migration of the reviewed inventory; partial failure requires journal-led recovery.
    public static class RemoteResourceMigration
    {
        const string Plan = "../QA/ResourceIntake-20261110/connection/all-remote-migration-plan.json";
        const string PlanHash = "fd2e08e853224d6aae51b6a6421cb50b6b7622e4c8fdea83f5b79f62a768b9f6";
        const string Catalog = "Assets/_LASTRAIL/Scripts/Resources/RemoteContentCatalog.Generated.cs";
        const string Destination = "Assets/_LASTRAIL/Content/RemoteResources/";
        public static string Hash(string path)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        public static void Apply()
        {
            if (Hash(Plan) != PlanHash) throw new InvalidOperationException("Reviewed migration plan changed.");
            var plan = JObject.Parse(File.ReadAllText(Plan));
            var rows = (JArray)plan["rows"];
            if (rows.Count != 326 || rows.Count(r => (bool)r["local"]) != 27 || rows.Count(r => !(bool)r["local"]) != 299)
                throw new InvalidOperationException("Reviewed migration counts changed.");
            var settings = AddressableAssetSettingsDefaultObject.Settings ?? throw new InvalidOperationException("Addressables settings missing.");
            var offline = new System.Collections.Generic.HashSet<string>(OfflineBootstrapResources.ResourceKeys, StringComparer.Ordinal);
            foreach (var row in rows)
            {
                string source = (string)row["source"], target = (string)row["target"], key = (string)row["key"];
                bool local = (bool)row["local"];
                if (local != offline.Contains(key) || target != (local ? source : Destination + key + Path.GetExtension(source)))
                    throw new InvalidOperationException("Unexpected mapping: " + source);
                if (AssetDatabase.AssetPathToGUID(source) != (string)row["guid"] || Hash(source) != (string)row["sha256"] || Hash(source + ".meta") != (string)row["metaSha256"])
                    throw new InvalidOperationException("Source changed: " + source);
                if (!local && (File.Exists(target) || Directory.Exists(target) || File.Exists(target + ".meta")))
                    throw new InvalidOperationException("Destination occupied: " + target);
                var entry = settings.FindAssetEntry((string)row["guid"]);
                if (entry?.address != (string)row["existingAddress"] || entry?.parentGroup.Name != (string)row["existingGroup"])
                    throw new InvalidOperationException("Registration changed: " + source);
            }
            var closure = AssetDatabase.GetDependencies(OfflineBootstrapResources.DirectAssetPaths.Concat(rows.Where(r => (bool)r["local"]).Select(r => (string)r["source"])).ToArray(), true);
            if (rows.Any(r => !(bool)r["local"] && closure.Contains((string)r["source"])))
                throw new InvalidOperationException("Offline UI references a migration resource.");

            string run = "../QA/ResourceIntake-20261110/connection/migration-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(run);
            File.Copy(Plan, run + "/approved-plan.json");
            var backups = rows.SelectMany(r => new[] { (string)r["source"], (string)r["source"] + ".meta" })
                .Concat(Directory.GetFiles("Assets/AddressableAssetsData", "*", SearchOption.AllDirectories)).Concat(new[] { Catalog }).Distinct();
            foreach (string path in backups)
            {
                string target = run + "/backup/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(path, target, false);
            }
            var journal = new JObject { ["planSha256"] = PlanHash, ["state"] = "BACKED_UP", ["moves"] = new JArray() };
            Action save = () => File.WriteAllText(run + "/journal.json", journal.ToString(Formatting.Indented));
            save();
            try
            {
                foreach (var row in rows.Where(r => !(bool)r["local"]))
                {
                    string source = (string)row["source"], target = (string)row["target"];
                    EnsureFolder(Path.GetDirectoryName(target).Replace('\\', '/'));
                    var move = new JObject { ["source"] = source, ["target"] = target, ["state"] = "STARTED" };
                    ((JArray)journal["moves"]).Add(move); save();
                    string error = AssetDatabase.MoveAsset(source, target);
                    if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                    move["state"] = "MOVED"; save();
                    if (AssetDatabase.AssetPathToGUID(target) != (string)row["guid"] || Hash(target) != (string)row["sha256"] || Hash(target + ".meta") != (string)row["metaSha256"])
                        throw new InvalidOperationException("Moved identity or bytes changed: " + target);
                }
                const string groupName = "remote_resources";
                var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                schema.UseAssetBundleCache = true; schema.UseAssetBundleCrc = true; schema.UseUnityWebRequestForLocalBundles = true; schema.Timeout = 30; schema.RetryCount = 0;
                var provider = schema.AssetBundleProviderType; provider.Value = typeof(CacheOnlyAssetBundleProvider); schema.AssetBundleProviderType = provider;
                foreach (var row in rows.Where(r => !(bool)r["local"]))
                {
                    var entry = settings.CreateOrMoveEntry((string)row["guid"], group);
                    entry.address = (string)row["source"];
                    entry.SetLabel(groupName, true, true);
                    entry.SetLabel(AddressableContentBackend.RequiredLabel, true, true);
                }
                var code = new StringBuilder("// Reviewed migration: physical paths may change; original addresses remain stable.\nnamespace LastRail.Resources { public static partial class RemoteContentCatalog { public static readonly Entry[] Entries=new Entry[] {\n");
                foreach (var row in rows)
                    code.Append("new Entry(").Append(JsonConvert.ToString((string)row["key"])).Append(',').Append(JsonConvert.ToString((string)row["target"])).Append(',').Append(JsonConvert.ToString((string)row["source"])).Append(',').Append(JsonConvert.ToString((string)row["type"])).Append(',').Append((bool)row["local"] ? "true" : "false").Append("),\n");
                code.Append("}; } }\n");
                File.WriteAllText(Catalog, code.ToString());
                EditorUtility.SetDirty(schema); EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                journal["state"] = "REGISTERED"; save();
                Debug.Log("REMOTE_MIGRATION_REGISTERED moves=299 local=27 journal=" + run + "/journal.json");
                AssetDatabase.Refresh();
            }
            catch (Exception error)
            {
                journal["state"] = "FAILED_REQUIRES_JOURNAL_RECOVERY"; journal["error"] = error.ToString(); save();
                throw;
            }
        }

        public static void RegisterHudIcons()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup("remote_resources") ?? throw new InvalidOperationException("Apply reviewed migration first.");
            var keys = new[] { "CombatSecondFinal/DConsole/train-icon-v19", "CombatSecondFinal/DConsole/system-icon-v19" };
            foreach (string key in keys)
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(Destination + key + ".png") == null)
                    throw new InvalidOperationException("HUD texture missing: " + key);
            string code = File.ReadAllText(Catalog);
            if (!code.EndsWith("}; } }\n", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected generated catalog format.");
            foreach (string key in keys)
            {
                string path = Destination + key + ".png";
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.address = path; entry.SetLabel("remote_resources", true, true); entry.SetLabel(AddressableContentBackend.RequiredLabel, true, true);
                if (RemoteContentCatalog.Find(key) == null)
                {
                    string line = "new Entry(" + JsonConvert.ToString(key) + "," + JsonConvert.ToString(path) + "," + JsonConvert.ToString(path) + "," + JsonConvert.ToString(typeof(Texture2D).AssemblyQualifiedName) + ",false),\n";
                    if (!code.Contains(line)) code = code.Substring(0, code.Length - "}; } }\n".Length) + line + "}; } }\n";
                }
            }
            File.WriteAllText(Catalog, code);
            EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("REMOTE_HUD_REGISTERED count=2");
        }

        public static void ReconcileProductionGroups()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var offline = new System.Collections.Generic.HashSet<string>(AssetDatabase.GetDependencies(
                OfflineBootstrapResources.DirectAssetPaths.Concat(RemoteContentCatalog.Entries.Where(e => e.Local).Select(e => e.Path)).ToArray(), true), StringComparer.Ordinal);
            string run = "../QA/ResourceIntake-20261110/connection/group-reconcile-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(run);
            foreach (string path in Directory.GetFiles("Assets/AddressableAssetsData", "*", SearchOption.AllDirectories))
            {
                string backup = run + "/backup/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup, false);
            }
            var removed = new JArray(); var registered = new JArray();
            foreach (var group in settings.groups.Where(g => g != null).ToArray())
            {
                foreach (var entry in group.entries.ToArray())
                    if (offline.Contains(entry.AssetPath)) { removed.Add(entry.AssetPath); settings.RemoveAssetEntry(entry.guid); }
                if (!group.Name.StartsWith("remote_", StringComparison.Ordinal)) continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>() ?? throw new InvalidOperationException("Remote group schema missing: " + group.Name);
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                schema.UseAssetBundleCache = true; schema.UseAssetBundleCrc = true; schema.UseUnityWebRequestForLocalBundles = true; schema.Timeout = 30; schema.RetryCount = 0;
                var provider = schema.AssetBundleProviderType; provider.Value = typeof(CacheOnlyAssetBundleProvider); schema.AssetBundleProviderType = provider;
                foreach (var entry in group.entries)
                {
                    entry.SetLabel(AddressableContentBackend.RequiredLabel, true, true);
                    registered.Add(new JObject { ["path"] = entry.AssetPath, ["address"] = entry.address, ["group"] = group.Name });
                }
                EditorUtility.SetDirty(schema); EditorUtility.SetDirty(group);
            }
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            var extensions = new System.Collections.Generic.HashSet<string>(new[] { ".png", ".wav", ".prefab", ".anim", ".controller", ".spriteatlas", ".mat", ".asset", ".otf", ".unity" }, StringComparer.OrdinalIgnoreCase);
            var shippingRoots = registered.Select(r => (string)r["path"]).Concat(RemoteContentCatalog.Entries.Where(e => !e.Local).Select(e => e.Path)).Distinct().ToArray();
            var dependencyClosure = AssetDatabase.GetDependencies(shippingRoots, true);
            var unregisteredRoots = shippingRoots.Where(p => settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(p)) == null).ToArray();
            var implicitBundledDependencies = dependencyClosure.Where(p => !offline.Contains(p) && !AssetDatabase.IsValidFolder(p) && extensions.Contains(Path.GetExtension(p))
                && settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(p)) == null).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var sharedOfflineDependencies = dependencyClosure.Where(p => offline.Contains(p) && extensions.Contains(Path.GetExtension(p))).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var candidates = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/_LASTRAIL/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)
                && extensions.Contains(Path.GetExtension(p)) && !AddressableResourceBuilder.ExcludedProductionPath(p) && !offline.Contains(p)
                && !dependencyClosure.Contains(p) && settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(p)) == null).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            File.WriteAllText(run + "/audit.json", new JObject { ["registeredCount"] = registered.Count, ["registered"] = registered,
                ["removedOfflineEntries"] = removed, ["unregisteredCandidatesNotAssumedShipping"] = new JArray(candidates),
                ["unregisteredShippingRoots"] = new JArray(unregisteredRoots), ["implicitBundledDependencies"] = new JArray(implicitBundledDependencies),
                ["sharedOfflineDependenciesRequirePackedLayoutCheck"] = new JArray(sharedOfflineDependencies),
                ["scope"] = "Existing production remote groups reconciled. Candidate presence is not shipping approval. Packed build and cache-only playback require separate verification." }.ToString(Formatting.Indented));
            if (unregisteredRoots.Length != 0) throw new InvalidOperationException("Unregistered shipping roots: " + string.Join(",", unregisteredRoots));
            Debug.Log("REMOTE_GROUPS_RECONCILED entries=" + registered.Count + " offlineRemoved=" + removed.Count + " unclassifiedCandidates=" + candidates.Length + " report=" + run + "/audit.json");
        }

        public static void RegisterApprovedUnconnected()
        {
            const string planPath = "../QA/ResourceIntake-20261110/connection/approved-unconnected-registration-plan.json";
            if (Hash(planPath) != "e088dace7b353bb8038b9355b4787f4d30b3ea688fc96c123aa767f9278d9db0") throw new InvalidOperationException("Approved unconnected registration plan changed.");
            var plan = JObject.Parse(File.ReadAllText(planPath)); var rows = (JArray)plan["rows"];
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup("remote_resources") ?? throw new InvalidOperationException("Migration group missing.");
            foreach (var row in rows)
            {
                string path = (string)row["path"];
                if (AssetDatabase.AssetPathToGUID(path) != (string)row["guid"] || Hash(path) != (string)row["sha256"] || Hash(path + ".meta") != (string)row["metaSha256"])
                    throw new InvalidOperationException("Approved asset changed: " + path);
                var entry = settings.FindAssetEntry((string)row["guid"]);
                if (entry != null && (entry.address != path || entry.parentGroup != group)) throw new InvalidOperationException("Unexpected existing registration: " + path);
            }
            foreach (var excluded in (JArray)plan["excluded"])
                if (settings.FindAssetEntry(AssetDatabase.AssetPathToGUID((string)excluded["path"])) != null) throw new InvalidOperationException("Reference-only art registered unexpectedly.");
            string run = "../QA/ResourceIntake-20261110/connection/approved-unconnected-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(run);
            foreach (string path in Directory.GetFiles("Assets/AddressableAssetsData", "*", SearchOption.AllDirectories))
            {
                string backup = run + "/backup/" + path; Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup, false);
            }
            foreach (var row in rows)
            {
                var entry = settings.CreateOrMoveEntry((string)row["guid"], group);
                entry.address = (string)row["address"]; entry.SetLabel("remote_resources", true, true); entry.SetLabel(AddressableContentBackend.RequiredLabel, true, true);
            }
            EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            plan["result"] = "REGISTERED_53_PLAYBACK_AND_UI_CONNECTION_UNCHANGED";
            File.WriteAllText(run + "/result.json", plan.ToString(Formatting.Indented));
            Debug.Log("APPROVED_UNCONNECTED_REGISTERED ui=45 voices=8 excludedReference=1 result=" + run + "/result.json");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new IOException("Cannot create " + path);
        }
    }
}
#endif
