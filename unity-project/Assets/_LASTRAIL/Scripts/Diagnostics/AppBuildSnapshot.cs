using System;
using UnityEngine;

namespace LastRail.Diagnostics
{
    public sealed class AppBuildSnapshot
    {
        public string AppVersion { get; }
        public long? AppVersionCode { get; }
        public AppBuildSnapshot(string appVersion, long? appVersionCode)
        {
            AppVersion = SafeVersion(appVersion);
            AppVersionCode = appVersionCode.HasValue && appVersionCode.Value > 0 ? appVersionCode : null;
        }

        private static string SafeVersion(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64) return null;
            foreach (var c in value)
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')
                    && c != '.' && c != '-' && c != '_' && c != '+') return null;
            return value;
        }

        // Call once on Unity's main thread before installing the threaded log hook.
        public static AppBuildSnapshot CaptureInstalled()
        {
#if UNITY_EDITOR
            return new AppBuildSnapshot(null, null);
#else
            long? code = null;
#if UNITY_ANDROID
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var manager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (var info = manager.Call<AndroidJavaObject>("getPackageInfo", activity.Call<string>("getPackageName"), 0))
                using (var build = new AndroidJavaClass("android.os.Build$VERSION"))
                    code = build.GetStatic<int>("SDK_INT") >= 28 ? info.Call<long>("getLongVersionCode") : info.Get<int>("versionCode");
            }
            catch (Exception) { }
#endif
            return new AppBuildSnapshot(Application.version, code);
#endif
        }
    }
}
