using System;
using System.IO;
using UnityEngine;

namespace LastRail.Diagnostics
{
    public sealed class LocalClientErrorCapture : MonoBehaviour
    {
        private static LocalClientErrorCapture active;
        private SafeClientErrorQueue queue;
        private AppBuildSnapshot build;
        private float nextSave;
        [ThreadStatic] private static bool capturing;
        public bool CollectionAvailable => queue != null;
        public bool TransportAvailable => false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if !UNITY_EDITOR
            if (active != null) return;
            var host = new GameObject("LocalClientErrorCapture");
            DontDestroyOnLoad(host);
            active = host.AddComponent<LocalClientErrorCapture>();
            active.build = AppBuildSnapshot.CaptureInstalled();
            string explicitQaDirectory = null;
#if UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD
            explicitQaDirectory = Environment.GetEnvironmentVariable("LASTRAIL_LOCAL_QA_DIAGNOSTICS_DIRECTORY");
#endif
            active.queue = new SafeClientErrorQueue(LocalDevelopmentDiagnosticsPath.Resolve(Application.persistentDataPath, explicitQaDirectory));
            active.queue.Load(Now());
            Application.logMessageReceivedThreaded += active.Capture;
#endif
        }

        private void Capture(string ignoredCondition, string ignoredStack, LogType type)
        {
            if (capturing || queue == null) return;
            SafeClientErrorKind kind;
            switch (type)
            {
                case LogType.Error: kind = SafeClientErrorKind.UnityError; break;
                case LogType.Exception: kind = SafeClientErrorKind.UnityException; break;
                case LogType.Assert: kind = SafeClientErrorKind.UnityAssert; break;
                default: return;
            }
            capturing = true;
            try { queue.Record(kind, build, Now()); }
            catch (Exception) { }
            finally { capturing = false; }
        }

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private void Update()
        {
            if (Time.unscaledTime < nextSave) return;
            nextSave = Time.unscaledTime + 5;
            queue?.Save(Now());
        }
        private void OnApplicationPause(bool paused) { if (paused) queue?.Save(Now()); }
        private void OnApplicationQuit() { Application.logMessageReceivedThreaded -= Capture; queue?.Save(Now()); }
        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= Capture;
            queue?.Save(Now());
            if (active == this) active = null;
        }
    }
}
