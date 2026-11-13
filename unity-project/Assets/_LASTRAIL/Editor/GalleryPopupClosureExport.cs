#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class GalleryPopupClosureExport
{
    public static void Write(string output)
    {
        var rows = new JArray();
        foreach (var name in new[] { "popup-frame-v03", "popup-backplate", "control-frame-v03", "control-backplate" })
        {
            var key = "EntryPopup/" + name; var texture = Resources.Load<Texture2D>(key);
            if (texture == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out string guid, out long id)) throw new InvalidOperationException("Popup resource linkage missing.");
            var path = AssetDatabase.GetAssetPath(texture);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != texture) throw new InvalidOperationException("Popup resource identity mismatch.");
            rows.Add(new JObject { ["resourceKey"] = key, ["path"] = path, ["guid"] = guid, ["localFileId"] = id.ToString(),
                ["assetRawSha256"] = Hash(path), ["metaRawSha256"] = Hash(path + ".meta"), ["width"] = texture.width, ["height"] = texture.height,
                ["runtimeBytes"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture), ["resourcesLoadSameObject"] = true,
                ["combatRootReferenced"] = false, ["linkage"] = "IndependentMetalSurface Resources.Load; separate from combat selection" });
        }
        File.WriteAllText(Path.Combine(output, "gallery-popup-closure-extension.json"), new JObject {
            ["schema"] = "gallery-popup-resource-closure-v1", ["resources"] = rows, ["actualAdmissionReady"] = false,
            ["scope"] = "Physical asset/meta and runtime resource identity; no all-gallery visual or live admission assertion" }.ToString());
    }
    static string Hash(string path)
    { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
}
#endif
