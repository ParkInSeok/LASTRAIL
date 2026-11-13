using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Editor
{
    public sealed class EditorProjectContent : IEditorProjectContent
    {
        private static readonly SemaphoreSlim Initialization = new SemaphoreSlim(1, 1);

        [InitializeOnLoadMethod]
        private static void Register() => EditorProjectContentBridge.Factory = () => new EditorProjectContent();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterRuntime() => Register();

        public async Task<IResourceLocator> InitializeAsync(CancellationToken cancel)
        {
            await Initialization.WaitAsync(cancel);
            try
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null || settings.InitializationObjects.Count != 0)
                    throw new InvalidOperationException("Local project settings are missing or contain initialization hooks.");
                foreach (var key in new[] { AddressableContentBackend.MainSceneKey, AddressableContentBackend.LobbyKey })
                {
                    var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(key));
                    if (entry == null || entry.address != key || !entry.labels.Contains(AddressableContentBackend.RequiredLabel))
                        throw new InvalidOperationException("Required local project address is not registered.");
                }
                var keyName = Addressables.kAddressablesRuntimeDataPath;
                var existed = PlayerPrefs.HasKey(keyName);
                var previous = PlayerPrefs.GetString(keyName);
                var builder = ScriptableObject.CreateInstance<BuildScriptFastMode>();
                try
                {
                    var result = builder.BuildData<AddressablesPlayModeBuildResult>(new AddressablesDataBuilderInput(settings));
                    if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
                    var handle = Addressables.InitializeAsync(false);
                    try
                    {
                        await AddressableContentBackend.Complete(handle, cancel);
                        var locator = handle.Result;
                        if (locator == null || locator.GetType().FullName != "UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsLocator"
                            || !locator.Locate(AddressableContentBackend.MainSceneKey, typeof(SceneInstance), out var main)
                            || main.Count != 1 || main[0].InternalId != AddressableContentBackend.MainSceneKey
                            || !locator.Locate(AddressableContentBackend.LobbyKey, typeof(GameObject), out var lobby)
                            || lobby.Count != 1 || lobby[0].InternalId != AddressableContentBackend.LobbyKey)
                            throw new InvalidOperationException("Addressables is not initialized with the actual local project.");
                        return locator;
                    }
                    finally { AddressableContentBackend.ReleaseAfterCompletion(handle); }
                }
                finally
                {
                    if (existed) PlayerPrefs.SetString(keyName, previous); else PlayerPrefs.DeleteKey(keyName);
                    UnityEngine.Object.DestroyImmediate(builder);
                }
            }
            finally { Initialization.Release(); }
        }
    }
}
