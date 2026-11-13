#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace LastRail.Editor.Signing
{
    public static class DevPlayPublisher
    {
        private static string Project => Path.GetDirectoryName(Application.dataPath);

        public static void BuildPublicationCandidate(BuildRequest request, string password, string candidatePath)
        {
            if (request.Server != "DEV" || request.Format != "aab") throw new InvalidOperationException("Play publication supports DEV AAB only.");
            if (!Path.IsPathRooted(candidatePath) || !File.Exists(candidatePath)) throw new InvalidOperationException("An explicit completed content candidate is required.");
            var candidateHash = Hash(candidatePath);
            var directory = AndroidSignedBuild.Build(request, password, candidatePath);
            if (Hash(candidatePath) != candidateHash) throw new InvalidOperationException("Content candidate changed during app build; publication is blocked.");
            var report = JObject.Parse(File.ReadAllText(Path.Combine(directory, "build-report.json")));
            var code = (int)report["versionCode"];
            var candidate = ValidateCandidate(request, code, candidatePath);
            var artifact = Path.Combine(directory, request.ArtifactName(code));
            var manifest = new JObject {
                ["schemaVersion"] = 1, ["environment"] = "DEV", ["package"] = request.Package,
                ["version"] = request.Version, ["versionCode"] = code, ["format"] = "aab", ["track"] = "internal",
                ["appPath"] = artifact, ["appSha256"] = Hash(artifact), ["appBytes"] = new FileInfo(artifact).Length,
                ["signerSha256"] = ((string)report["certificateSha256"]).ToLowerInvariant(),
                ["candidatePath"] = Path.GetFullPath(candidatePath), ["candidateSha256"] = candidateHash,
                ["contentRelease"] = ContentRelease(candidate)
            };
            var path = Path.Combine(directory, "publication-manifest.json");
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file)) { writer.Write(manifest.ToString()); }
            UnityEngine.Debug.Log("LASTRAIL publication candidate prepared locally: " + path + "; no remote publication performed.");
        }

        public static JObject ValidateCandidate(BuildRequest request, int code, string path)
        {
            var candidate = JObject.Parse(File.ReadAllText(path));
            var c = candidate["configuration"];
            if ((string)candidate["status"] != "LOCAL_BUILT_NOT_DEPLOYED_NOT_DEVICE_VERIFIED"
                || (string)c?["environment"] != request.Server || (string)c?["target"] != "Android"
                || (string)c?["appVersion"] != request.Version || (int?)c?["versionCode"] != code
                || (int?)c?["minimumVersionCode"] > code || (int?)c?["maximumVersionCode"] < code
                || c?["minimumVersionCode"] == null || c?["maximumVersionCode"] == null)
                throw new InvalidOperationException("Content candidate must match the exact signed app environment/version/code.");
            AndroidBuildPolicy.ValidateContentRoot(request.Server, (string)c["releaseNamespace"]);
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var profile = settings.profileSettings.GetProfileId(request.Server);
            if (settings.profileSettings.GetValueByName(profile, UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.kRemoteLoadPath) != (string)c["releaseNamespace"])
                throw new InvalidOperationException("App content profile differs from the candidate release namespace.");
            return candidate;
        }

        private static JObject ContentRelease(JObject candidate)
        {
            var c = candidate["configuration"];
            return new JObject { ["status"] = "APPROVED", ["releaseId"] = c["releaseId"], ["contentVersion"] = c["contentVersion"],
                ["catalogUrl"] = candidate["catalogUrl"], ["catalogHash"] = candidate["catalogHash"],
                ["minimumVersionCode"] = c["minimumVersionCode"].ToString(), ["maximumVersionCode"] = c["maximumVersionCode"].ToString() };
        }

        public static void PublishExisting(string manifestPath, bool legacyAcknowledged)
        {
            throw new InvalidOperationException("REMOTE_EXECUTION_APPROVAL_REQUIRED: Unity publisher execution is disabled pending direct scoped approval.");
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
