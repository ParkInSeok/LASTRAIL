using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Resources
{
    public static class ApprovedCatalogSafety
    {
        public static string NormalizeInstalledPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("//", StringComparison.Ordinal)
                || value.StartsWith("\\\\", StringComparison.Ordinal) || value.Contains("{") || value.Contains("}"))
                throw new InvalidOperationException("Installed path is not local.");
            if (value.StartsWith("jar:file:/", StringComparison.Ordinal)) return value;
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                if (!uri.IsFile || uri.IsUnc) throw new InvalidOperationException("Installed path is not local.");
                return value;
            }
            return Path.GetFullPath(value);
        }

        public static string VerifiedCatalogPath(string directory, string catalogUrl, string catalogHash, bool jsonCatalogEnabled)
        {
            if (!Uri.TryCreate(catalogUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Approved catalog format must match the installed catalog provider.");
            return CatalogPath(directory, uri, catalogHash, jsonCatalogEnabled);
        }

        private static string CatalogPath(string directory, Uri uri, string catalogHash, bool jsonCatalogEnabled)
        {
            var extension = jsonCatalogEnabled ? ".json" : ".bin";
            if (string.IsNullOrWhiteSpace(directory) || Path.GetExtension(uri.AbsolutePath) != extension ||
                catalogHash == null || !Regex.IsMatch(catalogHash, "\\Asha256:[a-f0-9]{64}\\z"))
                throw new InvalidOperationException("Approved catalog format must match the installed catalog provider.");
            return Path.Combine(directory, catalogHash.Substring(7) + extension);
        }

        public static IResourceLocation[] BuildRemoteBundlePlan(IResourceLocation[] roots, string trustedRoot,
            Func<IResourceLocation, string> transform)
        {
            ClientAdmissionContract.ValidateCatalogRoot(trustedRoot);
            return BuildPlan(roots, transform, url => ClientAdmissionContract.ValidateCatalogUrl(url, trustedRoot));
        }

#if UNITY_EDITOR
        internal static string FixtureCatalogPath(EditorLoopbackContentFixture fixture, string url, string hash, bool json)
        {
            fixture.ValidateContentUrl(url);
            return CatalogPath(Path.Combine(fixture.StateDirectory, "approved-catalogs"), new Uri(url), hash, json);
        }

        internal static IResourceLocation[] BuildFixturePlan(EditorLoopbackContentFixture fixture,
            IResourceLocation[] roots, Func<IResourceLocation, string> transform) =>
            BuildPlan(roots, transform, fixture.ValidateContentUrl);
#endif

        private static IResourceLocation[] BuildPlan(IResourceLocation[] roots,
            Func<IResourceLocation, string> transform, Action<string> validateUrl)
        {
            if (roots == null || roots.Length == 0 || transform == null)
                throw new InvalidOperationException("Required resource graph is missing.");
            var visited = new HashSet<IResourceLocation>();
            var active = new HashSet<IResourceLocation>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<IResourceLocation>();
            foreach (var root in roots) Visit(root, transform, validateUrl, visited, active, identities, result);
            return result.ToArray();
        }

        private static void Visit(IResourceLocation location, Func<IResourceLocation, string> transform, Action<string> validateUrl,
            HashSet<IResourceLocation> visited, HashSet<IResourceLocation> active, HashSet<string> identities,
            List<IResourceLocation> result)
        {
            if (location == null || active.Contains(location)) throw new InvalidOperationException("Invalid resource dependency graph.");
            if (visited.Contains(location)) return;
            active.Add(location);
            var id = transform(location);
            var bundleProvider = location.ProviderId == typeof(AssetBundleProvider).FullName ||
                location.ProviderId == typeof(CacheOnlyAssetBundleProvider).FullName;
            if (bundleProvider)
            {
                if (!(location.Data is AssetBundleRequestOptions options))
                    throw new InvalidOperationException("Bundle metadata is missing.");
                if (IsRemote(id))
                {
                    validateUrl(id);
                    if (string.IsNullOrWhiteSpace(options.BundleName) || options.Hash == null ||
                        !Regex.IsMatch(options.Hash, "\\A[a-fA-F0-9]{32}\\z") || !Hash128.Parse(options.Hash).isValid || options.BundleSize <= 0)
                        throw new InvalidOperationException("Remote bundle cache identity and size are required.");
                    if (identities.Add(options.BundleName + ":" + options.Hash)) result.Add(location);
                }
                else if (!IsLocal(NormalizeInstalledPath(id))) throw new InvalidOperationException("Unsupported bundle location.");
            }
            else if ((location.ProviderId != typeof(BundledAssetProvider).FullName &&
                !(location.ProviderId == typeof(SceneProvider).FullName && location.ResourceType == typeof(SceneInstance)
                    && id == AddressableContentBackend.MainSceneKey)) || string.IsNullOrWhiteSpace(id) ||
                !id.StartsWith("Assets/", StringComparison.Ordinal) || !location.HasDependencies)
                throw new InvalidOperationException("Unsupported resource provider or asset location.");
            if (location.HasDependencies)
                foreach (var dependency in location.Dependencies) Visit(dependency, transform, validateUrl, visited, active, identities, result);
            active.Remove(location);
            visited.Add(location);
        }

        public static void RequireCachedRemoteBundles(IResourceLocation[] roots, string trustedRoot,
            Func<IResourceLocation, string> transform, Func<IResourceLocation, bool> isCached)
        {
            if (isCached == null) throw new InvalidOperationException("Bundle cache verifier is required.");
            foreach (var bundle in BuildRemoteBundlePlan(roots, trustedRoot, transform))
                if (!isCached(bundle)) throw new InvalidOperationException("Approved remote dependencies must be cached before loading.");
        }

        public static void ValidateInstalledRuntimeData(ResourceManagerRuntimeData data, Func<string, string> resolveInternalId)
        {
            if (data == null || !data.DisableCatalogUpdateOnStartup || resolveInternalId == null ||
                data.CatalogLocations == null || data.CatalogLocations.Count == 0 || data.InitializationObjects.Count != 0)
                throw new InvalidOperationException("Approved content requires local-only installed Addressables settings.");
            var catalogs = 0;
            foreach (var location in data.CatalogLocations)
            {
                if (location == null || !IsLocal(resolveInternalId(location.InternalId)) ||
                    location.Dependencies == null || location.Dependencies.Length != 0 ||
                    location.Provider != typeof(ContentCatalogProvider).FullName || location.Keys == null ||
                    !location.Keys.Contains(ResourceManagerRuntimeData.kCatalogAddress))
                    throw new InvalidOperationException("Installed startup catalog graph must not select remote or cached fallback catalogs.");
                catalogs++;
            }
            if (catalogs != 1) throw new InvalidOperationException("Exactly one installed startup catalog is required.");
        }

        private static bool IsRemote(string value) => value != null &&
            (value.StartsWith("https:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("http:", StringComparison.OrdinalIgnoreCase));

        private static bool IsLocal(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Contains("{") || value.Contains("}") ||
                value.StartsWith("//", StringComparison.Ordinal) || value.StartsWith("\\\\", StringComparison.Ordinal)) return false;
            if (value.StartsWith("jar:file:/", StringComparison.Ordinal)) return true;
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)) return uri.IsFile && !uri.IsUnc;
            return Path.IsPathRooted(value);
        }
    }
}
