using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using LastRail.Resources;

public sealed class RemoteResourceMigrationTests
{
    [Test]
    public void ReviewedResourcesRetainBytesIdentityAndLegacyAddresses()
    {
        var plan = JObject.Parse(File.ReadAllText("../QA/ResourceIntake-20261110/connection/all-remote-migration-plan.json"));
        var rows = (JArray)plan["rows"];
        Assert.AreEqual(326, rows.Count);
        Assert.AreEqual(27, rows.Count(r => (bool)r["local"]));
        var replacements=(JArray)JObject.Parse(File.ReadAllText("../QA/ResourceIntake-20261110/connection/landscape-entry-v01/runtime-replacements.json"))["rows"];
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var original in rows)
        {
            var row=replacements.SingleOrDefault(r=>(string)r["key"]==(string)original["key"])??original;
            string path = (string)row["target"], source = (string)row["source"];
            if((bool?)row["retired"]==true){Assert.IsFalse(File.Exists(path));Assert.IsFalse(File.Exists(path+".meta"));Assert.IsNull(RemoteContentCatalog.Find((string)(row["lookupKey"]??row["key"])));continue;}
            bool local = (bool)row["local"];
            Assert.AreEqual((string)row["guid"], AssetDatabase.AssetPathToGUID(path), path);
            Assert.AreEqual((string)row["sha256"], Hash(path), path);
            Assert.AreEqual((string)row["metaSha256"], Hash(path + ".meta"), path);
            var metadata = RemoteContentCatalog.Find((string)(row["lookupKey"]??row["key"]));
            Assert.NotNull(metadata, path); Assert.AreEqual(path, metadata.Path); Assert.AreEqual(source, metadata.Address); Assert.AreEqual(local, metadata.Local);
            if (local) continue;
            if(source!=path){Assert.IsFalse(File.Exists(source), source);Assert.IsFalse(File.Exists(source + ".meta"), source);}
            var entry = settings.FindAssetEntry((string)row["guid"]);
            Assert.NotNull(entry, path); Assert.AreEqual(source, entry.address, path);
            Assert.IsTrue(entry.labels.Contains(AddressableContentBackend.RequiredLabel), path);
            var schema = entry.parentGroup.GetSchema<BundledAssetGroupSchema>();
            Assert.AreEqual(typeof(CacheOnlyAssetBundleProvider), schema.AssetBundleProviderType.Value, path);
            Assert.IsTrue(schema.UseAssetBundleCache, path);
        }
        var bootstrap = AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity", true);
        Assert.IsEmpty(rows.Where(r => !(bool)r["local"] && bootstrap.Contains((string)r["target"])));
    }

    [Test]
    public void AllRegisteredRemoteGroupsAreDownloadRequiredAndOfflineAssetsAreNotEntries()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var group in settings.groups.Where(g => g != null && g.Name.StartsWith("remote_", StringComparison.Ordinal)))
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.NotNull(schema, group.Name);
            Assert.AreEqual(typeof(CacheOnlyAssetBundleProvider), schema.AssetBundleProviderType.Value, group.Name);
            Assert.IsTrue(schema.UseAssetBundleCache, group.Name);
            foreach (var entry in group.entries) Assert.IsTrue(entry.labels.Contains(AddressableContentBackend.RequiredLabel), entry.AssetPath);
        }
        var offline = AssetDatabase.GetDependencies(RemoteContentCatalog.Entries.Where(e => e.Local).Select(e => e.Path)
            .Concat(new[] { "Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity", "Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab" }).ToArray(), true);
        foreach (string path in offline) Assert.IsNull(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)), path);
    }

    [Test]
    public void ApprovedUnconnectedResourcesAreRegisteredWithoutPromotingReferenceComposition()
    {
        var plan = JObject.Parse(File.ReadAllText("../QA/ResourceIntake-20261110/connection/approved-unconnected-registration-plan.json"));
        var rows = (JArray)plan["rows"];
        Assert.AreEqual(53, rows.Count); Assert.AreEqual(8, rows.Count(r => (string)r["kind"] == "voice"));
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var row in rows)
        {
            string path = (string)row["path"];
            Assert.AreEqual((string)row["sha256"], Hash(path), path);
            Assert.AreEqual((string)row["metaSha256"], Hash(path + ".meta"), path);
            var entry = settings.FindAssetEntry((string)row["guid"]);
            Assert.NotNull(entry, path); Assert.AreEqual(path, entry.address); Assert.AreEqual("remote_resources", entry.parentGroup.Name);
            Assert.IsTrue(entry.labels.Contains(AddressableContentBackend.RequiredLabel), path);
        }
        foreach (var row in (JArray)plan["excluded"])
            Assert.IsNull(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID((string)row["path"])), (string)row["path"]);
    }

    static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
}
