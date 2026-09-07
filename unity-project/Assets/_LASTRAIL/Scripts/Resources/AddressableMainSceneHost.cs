using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace LastRail.Resources
{
    public sealed class AddressableMainSceneHost
    {
        private sealed class Lease
        {
            public AsyncOperationHandle<SceneInstance> Handle;
            private Task release;
            private Scene loadedScene;
            public Task Release() => release == null || release.IsFaulted ? (release = ReleaseAsync()) : release;
            private async Task ReleaseAsync()
            {
                if (!Handle.IsValid())
                {
                    if (loadedScene.IsValid() && loadedScene.isLoaded)
                        throw new InvalidOperationException("Main scene cleanup lost its Addressables ownership.");
                    return;
                }
                await Handle.Task;
                if (!Handle.IsValid()) return;
                if (Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    loadedScene = Handle.Result.Scene;
                    var unload = Addressables.UnloadSceneAsync(Handle, false);
                    try { await AddressableContentBackend.Complete(unload, CancellationToken.None); }
                    finally { if (unload.IsValid()) Addressables.Release(unload); }
                }
                else Addressables.Release(Handle);
            }
        }

        private Lease current;
        private Lease retiring;
        private Task cleanup = Task.CompletedTask;
        private int generation;
        public MainSceneRoot Current { get; private set; }

        public async Task<MainSceneRoot> OpenAsync(IResourceLocation location, CancellationToken cancel)
        {
            if (location == null || location.ResourceType != typeof(SceneInstance))
                throw new InvalidOperationException("An approved Main scene location is required.");
            if (current != null) throw new InvalidOperationException("Main is already loading or loaded.");
            var request = ++generation;
            if (retiring != null) cleanup = ReleaseRetiringAsync(retiring);
            await cleanup;
            cancel.ThrowIfCancellationRequested();
            if (request != generation) throw new OperationCanceledException();
            var lease = new Lease { Handle = Addressables.LoadSceneAsync(location, LoadSceneMode.Additive, true) };
            current = lease;
            try
            {
                await AddressableContentBackend.Complete(lease.Handle, cancel);
                if (request != generation) throw new OperationCanceledException();
                var roots = lease.Handle.Result.Scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<MainSceneRoot>(true)).ToArray();
                if (roots.Length != 1) throw new InvalidOperationException("Main must contain exactly one content mount.");
                var root = roots[0];
                root.Hide();
                if (root.ContentRoot == null) throw new InvalidOperationException("Main content mount is missing.");
                Current = root;
                return root;
            }
            catch
            {
                if (ReferenceEquals(current, lease)) { current = null; Current = null; }
                retiring = lease;
                cleanup = ReleaseRetiringAsync(lease);
                await cleanup;
                throw;
            }
        }

        public async void Close()
        {
            try { await CloseAsync(); }
            catch { Debug.LogError("Main scene cleanup failed."); }
        }

        public Task CloseAsync()
        {
            ++generation;
            if (Current != null) Current.Hide();
            Current = null;
            var lease = current; current = null;
            if (lease != null) retiring = lease;
            if (retiring != null) cleanup = ReleaseRetiringAsync(retiring);
            return cleanup;
        }

        private async Task ReleaseRetiringAsync(Lease lease)
        {
            await lease.Release();
            if (ReferenceEquals(retiring, lease)) retiring = null;
        }
    }
}
