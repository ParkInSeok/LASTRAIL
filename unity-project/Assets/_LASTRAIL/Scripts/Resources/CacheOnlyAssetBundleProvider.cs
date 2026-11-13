using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace LastRail.Resources
{
    public sealed class CacheOnlyAssetBundleProvider : AssetBundleProvider
    {
        public override void Provide(ProvideHandle handle)
        {
            var url = handle.ResourceManager.TransformInternalId(handle.Location);
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var options = handle.Location.Data as AssetBundleRequestOptions;
                if (options == null || string.IsNullOrEmpty(options.Hash) ||
                    !Caching.IsVersionCached(new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash))))
                {
                    handle.Complete<IAssetBundleResource>(null, false, new InvalidOperationException("Bundle cache missing. Return to the download coordinator."));
                    return;
                }
            }
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                LoadCached(handle, (AssetBundleRequestOptions)handle.Location.Data);
            else base.Provide(handle);
        }

        private sealed class CachedResource : IAssetBundleResource
        {
            public AssetBundle Bundle;
            public AssetBundle GetAssetBundle() => Bundle;
        }

        private static async void LoadCached(ProvideHandle handle, AssetBundleRequestOptions options)
        {
            var identity = new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash));
            try
            {
                // The explicit cache name/hash resolves the existing cache. A local-only
                // fallback prevents Unity's internal CRC fallback from contacting a CDN.
                var localOnly = new Uri(Path.Combine(Application.temporaryCachePath, "lastrail-cache-only", Guid.NewGuid().ToString("N") + ".bundle")).AbsoluteUri;
                using var request = UnityWebRequestAssetBundle.GetAssetBundle(localOnly, identity, options.UseCrcForCachedBundle ? options.Crc : 0);
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();
                if (request.result != UnityWebRequest.Result.Success) throw new InvalidOperationException("Cached bundle validation failed.");
                var bundle = DownloadHandlerAssetBundle.GetContent(request);
                if (bundle == null) throw new InvalidOperationException("Cached bundle is missing.");
                handle.Complete<IAssetBundleResource>(new CachedResource { Bundle = bundle }, true, null);
            }
            catch (Exception error)
            {
                Caching.ClearCachedVersion(identity.name, identity.hash);
                handle.Complete<IAssetBundleResource>(null, false, error);
            }
        }

        public override void Release(IResourceLocation location, object asset)
        {
            if (asset is CachedResource resource)
            {
                if (resource.Bundle != null) resource.Bundle.Unload(true);
            }
            else base.Release(location, asset);
        }

        public override bool ShouldRetryDownloadError(UnityWebRequestResult result) => false;
    }
}
