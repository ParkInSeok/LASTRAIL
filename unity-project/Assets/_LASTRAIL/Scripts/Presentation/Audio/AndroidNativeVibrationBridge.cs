using System;
using UnityEngine;

namespace LastRail.Presentation.Audio
{
    public interface IAndroidVibrationNativePort
    {
        bool IsAndroidRuntime { get; }
        bool IsNativeBridgeAvailable { get; }
        bool TryVibrate(int durationMs, int amplitude);
    }

    public sealed class AndroidNativeVibrationBridge : IAndroidNativeVibrationBridge
    {
        private readonly IAndroidVibrationNativePort nativePort;
        private readonly AndroidNativeVibrationDispatchPolicy dispatchPolicy;

        public AndroidNativeVibrationBridge(IAndroidVibrationNativePort nativePort, bool vibrationEnabled)
        {
            this.nativePort = nativePort ?? throw new ArgumentNullException(nameof(nativePort));
            VibrationEnabled = vibrationEnabled;
            Status = ResolveStatus(nativePort, vibrationEnabled);
            dispatchPolicy = AndroidNativeVibrationDispatchPolicy.ForStatus(Status, vibrationEnabled);
        }

        public AndroidNativeVibrationBridgeStatus Status { get; }
        public bool VibrationEnabled { get; }
        public bool IsBlockingGameplay => false;

        public static AndroidNativeVibrationBridge CreateDefault(bool vibrationEnabled)
        {
            return new AndroidNativeVibrationBridge(new AndroidUnityNativeVibrationPort(), vibrationEnabled);
        }

        public AndroidVibrationDispatchResult TryVibrate(AndroidVibrationCue cue)
        {
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            var readiness = dispatchPolicy.TryDispatch(cue);
            if (readiness.Outcome != AndroidVibrationDispatchOutcome.WouldDispatch)
            {
                return readiness;
            }

            if (!nativePort.TryVibrate(cue.DurationMs, ToAmplitude(cue.Intensity)))
            {
                return new AndroidVibrationDispatchResult(
                    AndroidVibrationDispatchOutcome.FailedNativeDispatch,
                    cue.CueId,
                    canFallbackToNoVibration: true);
            }

            return new AndroidVibrationDispatchResult(
                AndroidVibrationDispatchOutcome.Dispatched,
                cue.CueId,
                canFallbackToNoVibration: false);
        }

        private static AndroidNativeVibrationBridgeStatus ResolveStatus(
            IAndroidVibrationNativePort nativePort,
            bool vibrationEnabled)
        {
            if (!vibrationEnabled)
            {
                return AndroidNativeVibrationBridgeStatus.DisabledByUser;
            }

            if (!nativePort.IsAndroidRuntime)
            {
                return AndroidNativeVibrationBridgeStatus.UnsupportedPlatform;
            }

            if (!nativePort.IsNativeBridgeAvailable)
            {
                return AndroidNativeVibrationBridgeStatus.NativeBridgeMissing;
            }

            return AndroidNativeVibrationBridgeStatus.Available;
        }

        private static int ToAmplitude(AndroidVibrationIntensity intensity)
        {
            switch (intensity)
            {
                case AndroidVibrationIntensity.Light:
                    return 64;
                case AndroidVibrationIntensity.Medium:
                    return 128;
                case AndroidVibrationIntensity.Heavy:
                    return 220;
                default:
                    return 128;
            }
        }
    }

    public sealed class AndroidUnityNativeVibrationPort : IAndroidVibrationNativePort
    {
        public bool IsAndroidRuntime
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsNativeBridgeAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var vibrator = GetVibrator())
                    {
                        return vibrator != null && vibrator.Call<bool>("hasVibrator");
                    }
                }
                catch
                {
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public bool TryVibrate(int durationMs, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (durationMs <= 0)
            {
                return false;
            }

            try
            {
                using (var vibrator = GetVibrator())
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    {
                        return false;
                    }

                    var clampedAmplitude = Mathf.Clamp(amplitude, 1, 255);
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        var sdkInt = version.GetStatic<int>("SDK_INT");
                        if (sdkInt >= 26)
                        {
                            using (var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                            using (var effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                                "createOneShot",
                                (long)durationMs,
                                clampedAmplitude))
                            {
                                vibrator.Call("vibrate", effect);
                            }
                        }
                        else
                        {
                            vibrator.Call("vibrate", (long)durationMs);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject GetVibrator()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (activity == null)
                {
                    return null;
                }

                return activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
        }
#endif
    }
}
