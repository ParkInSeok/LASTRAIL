#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Tests.EditMode
{
    public sealed class SyntheticContentDeltaHttpTests
    {
        [Test]
        public async Task ChangedWindowsBundleLoadsFromHttpAndRetainsBothCachedVersions()
        {
            var root = Environment.GetEnvironmentVariable("LASTRAIL_DELTA_QA_ROOT");
            if (string.IsNullOrEmpty(root)) Assert.Ignore("Requires isolated Win64 synthetic full/update fixtures and localhost:18285 server.");
            var fixture = JObject.Parse(File.ReadAllText(Path.Combine(root, "fixture.json")));
            Assert.That((string)fixture["platform"], Is.EqualTo("StandaloneWindows64"));
            Assert.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor));
            var name = "synthetic-delta-" + Path.GetFileName(root);
            await ResolveCatalog(root, "old", "full_A", fixture["old"]);
            await ResolveCatalog(root, "update", "update_B", fixture["update"]);
            foreach (var entry in new[] { fixture["old"], fixture["update"] })
            {
                Assert.That(SyntheticContentDeltaQa.Hash(Path.Combine(root, "bundles", (string)entry["file"])), Is.EqualTo((string)entry["sha256"]));
                Caching.ClearCachedVersion(name, Hash128.Parse((string)entry["hash"]));
            }
            var before = await Stats();
            Assert.That(await Load(fixture["old"], name), Is.EqualTo("synthetic-release-A"));
            var first = await Stats();
            Assert.That(first.requests - before.requests, Is.EqualTo(1));
            Assert.That(first.bytes - before.bytes, Is.EqualTo((long)fixture["old"]["bytes"]));
            Assert.That(await Load(fixture["update"], name), Is.EqualTo("synthetic-release-B-changed"));
            var second = await Stats();
            Assert.That(second.requests - first.requests, Is.EqualTo(1));
            Assert.That(second.bytes - first.bytes, Is.EqualTo((long)fixture["update"]["bytes"]));
            Assert.That(await Load(fixture["update"], name), Is.EqualTo("synthetic-release-B-changed"));
            Assert.That(await Load(fixture["old"], name), Is.EqualTo("synthetic-release-A"));
            var final = await Stats();
            Assert.That(final.requests, Is.EqualTo(second.requests), "Both warm versions must load without HTTP.");
            Assert.That(final.bytes, Is.EqualTo(second.bytes));
            var archive = Type.GetType("LastRail.Editor.LocalContentBaselineArchive, LastRail.Editor", true);
            archive.GetMethod("Validate").Invoke(null, new object[] { Path.Combine(root, "archives"), "full_A", (string)fixture["identity"] });
            Assert.That(SyntheticContentDeltaQa.Hash(Path.Combine(root, "archives/full_A/manifest.json")), Is.EqualTo((string)fixture["baselineManifestSha256"]));
            TestContext.WriteLine("SYNTHETIC_DELTA_HTTP_OK platform=StandaloneWindows64 old=A update=B-changed coldRequests=2 coldBytes="
                + (second.bytes - before.bytes) + " warmRequests=0 warmBytes=0 baseline=unchanged root=" + root);
        }
        private static async Task<string> Load(JToken entry, string name)
        {
            var key = new CachedAssetBundle(name, Hash128.Parse((string)entry["hash"]));
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle((string)entry["resolvedUrl"], key, (uint)entry["crc"]))
            {
                request.timeout = 15; request.redirectLimit = 0;
                await Send(request);
                var bundle = DownloadHandlerAssetBundle.GetContent(request);
                Assert.That(bundle, Is.Not.Null);
                try
                {
                    var names = bundle.GetAllAssetNames();
                    Assert.That(names, Has.Length.EqualTo(1));
                    var asset = bundle.LoadAsset<TextAsset>(names[0]);
                    Assert.That(asset, Is.Not.Null);
                    Assert.That(Caching.IsVersionCached(key), Is.True);
                    return asset.text;
                }
                finally { bundle.Unload(true); }
            }
        }
        private static async Task ResolveCatalog(string root, string label, string archive, JToken entry)
        {
            var folder = Path.Combine(root, "archives", archive);
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")));
            var item = manifest["files"].Single(e => ((string)e["path"]).StartsWith("remote/catalog", StringComparison.Ordinal)
                && ((string)e["path"]).EndsWith(".bin", StringComparison.Ordinal));
            var downloaded = Path.Combine(root, "downloaded-" + label + ".bin");
            using (var request = UnityWebRequest.Get("http://127.0.0.1:18285/catalogs/" + label))
            {
                request.timeout = 15; request.redirectLimit = 0; await Send(request);
                File.WriteAllBytes(downloaded, request.downloadHandler.data);
            }
            Assert.That(SyntheticContentDeltaQa.Hash(downloaded), Is.EqualTo((string)item["sha256"]));
            var type = typeof(ContentCatalogData);
            var load = type.GetMethod("LoadFromFile", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
            Assert.That(load, Is.Not.Null, "This fixture targets the installed binary catalog format.");
            var data = load.Invoke(null, new object[] { downloaded, true });
            var locator = (IResourceLocator)type.GetMethod("CreateCustomLocator", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(data, new object[] { "synthetic-" + label, null });
            Assert.That(locator.Locate("synthetic-delta-payload", typeof(TextAsset), out var assets), Is.True);
            Assert.That(assets.Count, Is.EqualTo(1));
            var bundle = assets[0].Dependencies.Single(d => d.Data is AssetBundleRequestOptions);
            var options = (AssetBundleRequestOptions)bundle.Data;
            Assert.That(options.Hash, Is.EqualTo((string)entry["hash"]));
            Assert.That(options.Crc, Is.EqualTo((uint)entry["crc"]));
            Assert.That(bundle.InternalId, Is.EqualTo("http://127.0.0.1:18285/bundles/" + (string)entry["file"]));
            entry["resolvedUrl"] = bundle.InternalId;
        }
        private static async Task<(long requests, long bytes)> Stats()
        {
            using (var request = UnityWebRequest.Get("http://127.0.0.1:18285/stats"))
            {
                request.timeout = 15; request.redirectLimit = 0; await Send(request);
                var value = JObject.Parse(request.downloadHandler.text);
                return ((long)value["requests"], (long)value["bytes"]);
            }
        }
        private static async Task Send(UnityWebRequest request)
        {
            var operation = request.SendWebRequest(); var completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.TrySetResult(true); await completion.Task;
            Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
        }
    }
}
#endif
