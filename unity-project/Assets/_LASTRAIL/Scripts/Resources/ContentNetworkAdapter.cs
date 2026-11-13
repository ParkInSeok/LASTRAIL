using UnityEngine;

namespace LastRail.Resources
{
    public static class ContentNetworkAdapter
    {
        public static ContentNetwork Classify(bool hasNetwork, bool internet, bool validated, bool wifi, bool cellular)
        {
            if (!hasNetwork || !internet) return ContentNetwork.Offline;
            if (!validated) return ContentNetwork.UnknownOnline;
            if (wifi && !cellular) return ContentNetwork.Wifi;
            return ContentNetwork.OtherOnline;
        }

        public static ContentNetwork Read()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "connectivity"))
                using (var network = manager.Call<AndroidJavaObject>("getActiveNetwork"))
                {
                    if (network == null) return ContentNetwork.Offline;
                    using (var caps = manager.Call<AndroidJavaObject>("getNetworkCapabilities", network))
                    {
                        if (caps == null) return ContentNetwork.UnknownOnline;
                        return Classify(true, caps.Call<bool>("hasCapability", 12), caps.Call<bool>("hasCapability", 16),
                            caps.Call<bool>("hasTransport", 1), caps.Call<bool>("hasTransport", 0));
                    }
                }
            }
            catch (AndroidJavaException) { return ContentNetwork.UnknownOnline; }
#else
            return Application.internetReachability == NetworkReachability.NotReachable
                ? ContentNetwork.Offline : ContentNetwork.UnknownOnline;
#endif
        }
    }
}
