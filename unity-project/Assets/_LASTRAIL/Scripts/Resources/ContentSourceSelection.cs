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
        Task<IResourceLocator> InitializeAsync(CancellationToken cancel);
    }

    public static class EditorProjectContentBridge
    {
        public static Func<IEditorProjectContent> Factory { get; set; }
    }
#endif
}
