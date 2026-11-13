using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastRail.Tests.EditMode
{
    public sealed class Ce03TypedAssetExportTests
    {
        const string Source = "D:/Project/LASTRAIL/WorkspaceRepo/docs/ops/ce03-content-20260911/ce03-minimum-operation-manifest.json";
        const string SourceHash = "034115a76319ed4abd8c21593a6533440a4d0dbe9cd62da182ac6c711e94b5a2";
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        static readonly Dictionary<string, string> GlobalRoles = new Dictionary<string, string>
        {
            ["combat_train_exterior_advance_lane"] = "scene.exteriorLane",
            ["combat_train_interior_boarding_lane"] = "scene.interiorLane",
            ["combat_train_interior_initial_squad"] = "scene.initialSquadInterior",
            ["combat_victory"] = "scene.transitions.victory",
            ["combat_defeat"] = "scene.transitions.defeat",
            ["vfx_basic_hit_spark"] = "scene.vfx.basicHit",
            ["vfx_skill_activation"] = "scene.vfx.skillActivation",
            ["vfx_ultimate_activation"] = "scene.vfx.ultimateActivation",
            ["vfx_shield_status"] = "scene.vfx.shield",
            ["vfx_heal_status"] = "scene.vfx.heal"
        };
        static readonly string[] Actors = { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "enemy_raider_scout", "enemy_spike_cart_gunner", "enemy_railbike_marauder" };

        static string Role(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (GlobalRoles.TryGetValue(name, out var global)) return global;
            var actor = Actors.SingleOrDefault(x => name == x || name.StartsWith(x + "_", StringComparison.Ordinal));
            Assert.That(actor, Is.Not.Null, "Unclassified asset: " + path);
            if (path.EndsWith(".controller", StringComparison.Ordinal)) return actor + ".controller";
            if (path.EndsWith(".anim", StringComparison.Ordinal)) return actor + ".clip." + name.Substring(actor.Length + 1);
            if (path.Contains("/SkillIcons/")) return actor + ".skillIcon";
            if (path.Contains("/CombatResources/Enemies/")) return actor + ".sprite";
            if (path.Contains("/CombatAnimations/")) return actor + ".frame." + name.Substring(actor.Length + 1);
            Assert.Fail("Unclassified role: " + path);
            return null;
        }

        [Test, Explicit("Editor-only local typed binding evidence, not executable content readiness.")]
        public void ExportAndRoundtripAllDirectionBindings()
        {
            Assert.AreEqual(SourceHash, Hash(Source));
            var rows = (JArray)JObject.Parse(File.ReadAllText(Source))["assets"];
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/') + "/";
            var bindings = new List<JObject>();
            var objects = new Dictionary<string, Object>(StringComparer.Ordinal);
            var byPath = new Dictionary<string, JObject>(StringComparer.Ordinal);
            var sourceEvidence = new JArray();
            foreach (var row in rows)
            {
                var path = ((string)row["path"]).Replace('\\', '/');
                Assert.IsTrue(path.StartsWith(root, StringComparison.OrdinalIgnoreCase));
                Assert.AreEqual((string)row["sha256"], Hash(path), path);
                Assert.AreEqual((string)row["metaSha256"], Hash(path + ".meta"), path);
                var relative = path.Substring(root.Length);
                if (relative.StartsWith("Library/PackageCache/", StringComparison.Ordinal))
                {
                    sourceEvidence.Add(new JObject { ["path"] = path, ["rawSha256"] = Hash(path), ["metaSha256"] = Hash(path + ".meta"), ["classification"] = "SOURCE_ONLY_NOT_TYPED_BINDING" });
                    continue;
                }
                Object obj = relative.EndsWith(".png", StringComparison.Ordinal) ? AssetDatabase.LoadAssetAtPath<Sprite>(relative)
                    : relative.EndsWith(".anim", StringComparison.Ordinal) ? AssetDatabase.LoadAssetAtPath<AnimationClip>(relative)
                    : AssetDatabase.LoadAssetAtPath<AnimatorController>(relative);
                Assert.IsNotNull(obj, relative);
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId));
                Assert.AreEqual((string)row["guid"], guid);
                Assert.AreNotEqual(0L, localId);
                var importer = AssetImporter.GetAtPath(relative);
                Assert.IsNotNull(importer);
                var role = Role(relative);
                var id = "binding." + role;
                var binding = new JObject
                {
                    ["bindingId"] = id, ["actorOrRoleId"] = role, ["assetPath"] = relative,
                    ["locator"] = new JObject { ["kind"] = "UNITY_ASSET_GUID", ["guid"] = guid, ["localFileId"] = localId.ToString(CultureInfo.InvariantCulture) },
                    ["assetType"] = obj is Sprite ? "Sprite" : obj is AnimationClip ? "AnimationClip" : "AnimatorController",
                    ["assetRawSha256"] = Hash(path), ["metaRawSha256"] = Hash(path + ".meta"),
                    ["importValidation"] = new JObject
                    {
                        ["unityVersion"] = Application.unityVersion,
                        ["importerVersion"] = "Assembly " + importer.GetType().Assembly.GetName().Version + "; native importer version not exposed",
                        ["importerType"] = importer.GetType().FullName,
                        ["dependencyHash"] = AssetDatabase.GetAssetDependencyHash(relative).ToString(),
                        ["loadVerified"] = true
                    }
                };
                objects.Add(id, obj);
                byPath.Add(relative, binding);
                bindings.Add(binding);
            }

            var externalDependencies = new JArray();
            foreach (var binding in bindings)
            {
                var path = (string)binding["assetPath"];
                var deps = new SortedSet<string>(StringComparer.Ordinal);
                var codeDeps = new JArray();
                foreach (var dependency in AssetDatabase.GetDependencies(path, false).Where(x => x != path))
                {
                    if (byPath.TryGetValue(dependency, out var target)) deps.Add((string)target["bindingId"]);
                    else
                    {
                        var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(dependency);
                        var resolved = package == null ? null : Path.Combine(package.resolvedPath, dependency.Substring(package.assetPath.Length + 1)).Replace('\\', '/');
                        var evidence = sourceEvidence.OfType<JObject>().SingleOrDefault(x => string.Equals((string)x["path"], resolved, StringComparison.OrdinalIgnoreCase));
                        if (evidence != null && dependency.EndsWith(".cs", StringComparison.Ordinal))
                        {
                            Assert.AreEqual((string)evidence["rawSha256"], Hash(resolved));
                            Assert.AreEqual((string)evidence["metaSha256"], Hash(resolved + ".meta"));
                            codeDeps.Add(new JObject { ["assetPath"] = dependency, ["sourcePath"] = resolved, ["rawSha256"] = evidence["rawSha256"], ["metaSha256"] = evidence["metaSha256"], ["packageVersion"] = package.version });
                        }
                        else externalDependencies.Add(new JObject { ["bindingId"] = binding["bindingId"], ["path"] = dependency, ["status"] = "OUTSIDE_PINNED_TYPED_SET" });
                    }
                }
                binding["dependencies"] = new JArray(deps);
                binding["codeDependencyEvidence"] = codeDeps;
            }

            var required = GlobalRoles.Values.Concat(Actors.Select(x => x + ".controller"))
                .Concat(Actors.Where(x => x.StartsWith("op_", StringComparison.Ordinal)).Select(x => x + ".skillIcon"))
                .Concat(Actors.Where(x => x.StartsWith("enemy_", StringComparison.Ordinal)).Select(x => x + ".sprite")).ToArray();
            var roles = new HashSet<string>(bindings.Select(x => (string)x["actorOrRoleId"]), StringComparer.Ordinal);
            var missing = required.Where(x => !roles.Contains(x)).ToArray();
            var artifact = new JObject
            {
                ["schemaVersion"] = "ce03-editor-typed-binding-evidence-v1", ["sourceRawSha256"] = SourceHash,
                ["shippingReady"] = false, ["actualBattleReady"] = false,
                ["resolver"] = new JObject { ["mode"] = "UNITY_EDITOR_ASSET_DATABASE", ["playerBuildAllowed"] = false },
                ["bindings"] = new JArray(bindings.OrderBy(x => (string)x["bindingId"], StringComparer.Ordinal)),
                ["sourceEvidence"] = sourceEvidence, ["outsideTypedDependencies"] = externalDependencies,
                ["missingRequiredRoles"] = new JArray(missing),
                ["nullableRolesNotBound"] = new JArray(Actors.Where(x => x.StartsWith("op_", StringComparison.Ordinal)).Select(x => x + ".portrait").Concat(new[] { "train.sprite", "car.Combat.sprite", "car.Gun.sprite" })),
                ["limitations"] = new JArray("Typed asset evidence only; no profile/content/snapshot hashes or execution implementation claimed.", "Nullable separate art is unbound; final composition selection remains a producer responsibility.", "Importer assembly version and dependency hash are recorded; native importer implementation version is not exposed.")
            };

            // Parse serialized locators afresh, then resolve every exact subasset/type.
            var serialized = JObject.Parse(artifact.ToString());
            foreach (var binding in serialized["bindings"])
            {
                var guid = (string)binding["locator"]["guid"];
                var localId = long.Parse((string)binding["locator"]["localFileId"], CultureInfo.InvariantCulture);
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.AreEqual((string)binding["assetPath"], path);
                var matches = AssetDatabase.LoadAllAssetsAtPath(path).Where(obj =>
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string actualGuid, out long actualId) && actualGuid == guid && actualId == localId).ToArray();
                Assert.AreEqual(1, matches.Length, path);
                var original = objects[(string)binding["bindingId"]];
                Assert.AreEqual(original.GetInstanceID(), matches[0].GetInstanceID(), path);
                Assert.AreEqual(original.GetType(), matches[0].GetType(), path);
            }
            artifact["roundtripVerified"] = bindings.Count;
            artifact["sourceFileCount"] = rows.Count;
            var output = Path.Combine(root, "QA/ScreenIntegration/ce03-typed-bindings.json");
            File.WriteAllText(output, artifact.ToString());
            Assert.IsEmpty(missing, "Required roles missing; artifact includes exact list.");
            Assert.IsEmpty(externalDependencies, "Dependencies outside typed set; artifact includes exact list.");
        }
    }
}
