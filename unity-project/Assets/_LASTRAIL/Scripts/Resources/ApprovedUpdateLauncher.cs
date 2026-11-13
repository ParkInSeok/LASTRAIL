using UnityEngine;

namespace LastRail.Resources
{
    internal static class ApprovedUpdateLauncher
    {
        internal static bool TryOpen(string approvedUrl)
        {
            if (!RequiredUpdateAction.IsCanonicalPlayUrl(approvedUrl)) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", approvedUrl))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", uri))
                using (var manager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (var resolved = intent.Call<AndroidJavaObject>("resolveActivity", manager))
                {
                    if (resolved == null) return false;
                    activity.Call("startActivity", intent);
                    return true;
                }
            }
            catch { return false; }
#else
            return false;
#endif
        }
    }
}
