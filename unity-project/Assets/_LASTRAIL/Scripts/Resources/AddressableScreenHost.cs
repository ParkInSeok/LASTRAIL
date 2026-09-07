using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace LastRail.Resources
{
    public sealed class AddressableScreenHost : MonoBehaviour
    {
        private sealed class InstanceLease
        {
            public AsyncOperationHandle<GameObject> Handle;
            private Task release;
            public Task Release() => release ?? (release = ReleaseAsync());
            private async Task ReleaseAsync()
            {
                if (!Handle.IsValid()) return;
                await Handle.Task;
                if (Handle.IsValid()) Addressables.ReleaseInstance(Handle);
            }
        }
        private InstanceLease lease;
        private readonly HashSet<InstanceLease> pending = new HashSet<InstanceLease>();
        private GameObject instance;
        private CancellationTokenSource lifetime;
        private int generation;
        public GameObject Current => instance;
        public void Close() { generation++; Clear(); }
        public Task CloseAsync()
        {
            Close();
            return Task.WhenAll(pending.Select(p => p.Release()).ToArray());
        }

        private void OnEnable() { lifetime = new CancellationTokenSource(); }
        private void OnDisable()
        {
            generation++;
            lifetime?.Cancel(); lifetime?.Dispose(); lifetime = null;
            Clear();
        }

        public Task<GameObject> OpenAsync(string key, CancellationToken cancel) => OpenKeyAsync(key, cancel);

        public Task<GameObject> OpenAsync(IResourceLocation location, CancellationToken cancel) =>
            OpenKeyAsync(location ?? throw new ArgumentNullException(nameof(location)), cancel);

        public Task<GameObject> OpenAsync(IResourceLocation location, Transform parent, CancellationToken cancel) =>
            OpenKeyAsync(location ?? throw new ArgumentNullException(nameof(location)), cancel, parent);

        private async Task<GameObject> OpenKeyAsync(object key, CancellationToken cancel, Transform parent = null)
        {
            if (!isActiveAndEnabled || lifetime == null) throw new InvalidOperationException("Screen host is disabled.");
            var request = ++generation;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, lifetime.Token))
            {
                var handle = key is IResourceLocation location
                    ? Addressables.InstantiateAsync(location, parent != null ? parent : transform)
                    : Addressables.InstantiateAsync(key, parent != null ? parent : transform);
                var nextLease = new InstanceLease { Handle = handle };
                pending.Add(nextLease);
                var retained = false;
                try
                {
                    await AddressableContentBackend.Complete(handle, linked.Token);
                    if (request != generation) throw new OperationCanceledException();
                    var next = handle.Result;
                    Clear();
                    instance = next; lease = nextLease; retained = true;
                    return next;
                }
                finally
                {
                    if (!retained) await nextLease.Release();
                    pending.Remove(nextLease);
                }
            }
        }

        private void Clear()
        {
            if (instance != null) instance.SetActive(false);
            instance = null;
            if (lease != null) _ = lease.Release();
            lease = null;
        }
    }
}
