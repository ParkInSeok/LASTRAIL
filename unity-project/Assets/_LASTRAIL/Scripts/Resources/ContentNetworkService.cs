using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LastRail.Resources
{
    public sealed class ContentNetworkService : IDisposable
    {
        private SynchronizationContext main;
        private int generation;
        private bool started;
        private readonly Func<ContentNetwork> snapshotReader;
        private readonly HashSet<TaskCompletionSource<ContentNetwork>> refreshes = new HashSet<TaskCompletionSource<ContentNetwork>>();
        public ContentNetwork Current { get; private set; } = ContentNetwork.UnknownOnline;
        public event Action<ContentNetwork> NetworkStateChanged;

        public ContentNetworkService() { }
        internal ContentNetworkService(Func<ContentNetwork> snapshotReader)
        {
            this.snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject bridge;
        private Listener listener;
        private sealed class Listener : AndroidJavaProxy
        {
            private readonly Action<int> receive;
            public Listener(Action<int> receive) : base("app.lastrail.network.NetworkBridge$Listener") { this.receive = receive; }
            [UnityEngine.Scripting.Preserve]
            public void onState(int value) { receive(value); }
        }
#endif
        public void Start()
        {
            if (started) return;
            main = SynchronizationContext.Current ?? throw new InvalidOperationException("Start on Unity main thread.");
            started = true;
            var token = ++generation;
            if (snapshotReader != null) { Refresh(); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                listener = new Listener(value => Post(token));
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    bridge = new AndroidJavaObject("app.lastrail.network.NetworkBridge", activity, listener);
                bridge.Call("start");
            }
            catch (AndroidJavaException) { Post(token); }
#else
            Refresh();
#endif
        }

        private void Post(int token)
        {
            main.Post(_ =>
            {
                if (!started || token != generation) return;
                // A queued native event is only a trigger; its old transport may already be obsolete.
                var snapshot = ReadSnapshot();
                if (started && token == generation) Publish(snapshot);
            }, null);
        }

        private void Publish(ContentNetwork value)
        {
            if (Current == value) return;
            Current = value; NetworkStateChanged?.Invoke(value);
        }

        public void Refresh()
        {
            if (!started) return;
            Publish(ReadSnapshot());
        }

        public async Task<ContentNetwork> RefreshAsync(CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            if (!started) throw new InvalidOperationException("Network service is not started.");
            var token = generation;
            var completion = new TaskCompletionSource<ContentNetwork>();
            refreshes.Add(completion);
            try
            {
                using (cancel.Register(() => completion.TrySetCanceled()))
                {
                    main.Post(_ =>
                    {
                        if (completion.Task.IsCompleted) return;
                        if (!started || token != generation) { completion.TrySetCanceled(); return; }
                        try
                        {
                            var snapshot = ReadSnapshot();
                            if (!started || token != generation || cancel.IsCancellationRequested)
                            { completion.TrySetCanceled(); return; }
                            Publish(snapshot);
                            if (!started || token != generation || cancel.IsCancellationRequested)
                            { completion.TrySetCanceled(); return; }
                            completion.TrySetResult(snapshot);
                        }
                        catch (Exception error) { completion.TrySetException(error); }
                    }, null);
                    return await completion.Task;
                }
            }
            finally { refreshes.Remove(completion); }
        }

        private ContentNetwork ReadSnapshot()
        {
            if (snapshotReader != null) return Normalize((int)snapshotReader());
#if UNITY_ANDROID && !UNITY_EDITOR
            try { return bridge == null ? ContentNetwork.UnknownOnline : Normalize(bridge.Call<int>("snapshot")); }
            catch (AndroidJavaException) { return ContentNetwork.UnknownOnline; }
#else
            return ContentNetworkAdapter.Read();
#endif
        }

        private static ContentNetwork Normalize(int value) =>
            Enum.IsDefined(typeof(ContentNetwork), value) ? (ContentNetwork)value : ContentNetwork.UnknownOnline;

#if UNITY_EDITOR
        public void InjectForTests(ContentNetwork value) { if (started) Publish(value); }
#endif

        public void Dispose()
        {
            if (!started) return;
            started = false; ++generation;
            var pending = new List<TaskCompletionSource<ContentNetwork>>(refreshes);
            refreshes.Clear();
            NetworkStateChanged = null;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try { bridge?.Call("stop"); }
                finally { bridge?.Dispose(); bridge = null; listener = null; }
#endif
            }
            finally { foreach (var refresh in pending) refresh.TrySetCanceled(); }
        }
    }
}
