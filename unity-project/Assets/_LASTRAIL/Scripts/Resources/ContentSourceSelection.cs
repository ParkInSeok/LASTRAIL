using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace LastRail.Resources
{
    public enum ContentResourceSource { RemoteApproved, EditorLocal }

    public static class ContentSourceSelection
    {
        public static ContentResourceSource Select(bool editor, bool explicitRemote) =>
            editor && !explicitRemote ? ContentResourceSource.EditorLocal : ContentResourceSource.RemoteApproved;

        public static ContentResourceSource Current(bool explicitRemote = false)
        {
#if UNITY_EDITOR
            return Select(true, explicitRemote);
#else
            return ContentResourceSource.RemoteApproved;
#endif
        }
    }

#if UNITY_EDITOR
    public interface IEditorProjectContent
    {
        string SettingsPath { get; }
        Task<IResourceLocator> InitializeAsync(CancellationToken cancel);
    }

    public static class EditorProjectContentBridge
    {
        public static Func<IEditorProjectContent> Factory { get; set; }
        public const string LocalRoot = "http://127.0.0.1:18284/LOCAL_TEST/Windows/";
        internal static EditorLoopbackContentFixture Fixture(string path) => EditorLoopbackContentFixture.Create(
            "http://127.0.0.1:18284/client-config.json", LocalRoot, path,
            System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,"../Library/LocalPackedState")), "45");
        public static async Task<IResourceLocator> InitializeBuiltAsync(string path,CancellationToken cancel) {
            if(!System.IO.File.Exists(path))throw new InvalidOperationException("Built fixture settings are missing.");
            var data=UnityEngine.JsonUtility.FromJson<UnityEngine.AddressableAssets.Initialization.ResourceManagerRuntimeData>(System.IO.File.ReadAllText(path));
            ApprovedCatalogSafety.ValidateInstalledRuntimeData(data,id=>ApprovedCatalogSafety.NormalizeInstalledPath(UnityEngine.AddressableAssets.Addressables.ResolveInternalId(id)));
            var handle=Fixture(path).InitializeRuntime(out var instance);
            try { await AddressableContentBackend.Complete(handle,cancel); return handle.Result; }
            finally { AddressableContentBackend.ReleaseAfterCompletion(handle); }
        }
    }
#endif
}
