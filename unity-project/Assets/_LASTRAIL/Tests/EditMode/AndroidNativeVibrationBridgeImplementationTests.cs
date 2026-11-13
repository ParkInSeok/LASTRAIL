using System;
using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Presentation.Audio;

namespace LastRail.Tests.EditMode
{
    public sealed class AndroidNativeVibrationBridgeImplementationTests
    {
        [Test]
        public void NativeBridge_DispatchesLightMediumAndHeavyCuesThroughInjectedPort()
        {
            var port = new RecordingAndroidVibrationNativePort(isAndroidRuntime: true, isNativeBridgeAvailable: true);
            var bridge = new AndroidNativeVibrationBridge(port, vibrationEnabled: true);

            var light = bridge.TryVibrate(AndroidVibrationCue.FromCueId("vibration_light_hit"));
            var medium = bridge.TryVibrate(AndroidVibrationCue.FromCueId("vibration_skill_ready"));
            var heavy = bridge.TryVibrate(AndroidVibrationCue.FromCueId("vibration_boss_phase"));

            Assert.That(bridge.Status, Is.EqualTo(AndroidNativeVibrationBridgeStatus.Available));
            Assert.That(light.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.Dispatched));
            Assert.That(medium.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.Dispatched));
            Assert.That(heavy.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.Dispatched));
            Assert.That(port.Calls.Count, Is.EqualTo(3));
            Assert.That(port.Calls[0].DurationMs, Is.EqualTo(25));
            Assert.That(port.Calls[1].DurationMs, Is.EqualTo(45));
            Assert.That(port.Calls[2].DurationMs, Is.EqualTo(80));
            Assert.That(port.Calls[0].Amplitude, Is.LessThan(port.Calls[1].Amplitude));
            Assert.That(port.Calls[1].Amplitude, Is.LessThan(port.Calls[2].Amplitude));
            Assert.That(heavy.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(heavy.HasRawDeviceIdentifier, Is.EqualTo(false));
            Assert.That(heavy.HasRawLogPayload, Is.EqualTo(false));
        }

        [Test]
        public void NativeBridge_SkipsDisabledUnsupportedAndMissingBridgeWithoutCallingPort()
        {
            var disabledPort = new RecordingAndroidVibrationNativePort(isAndroidRuntime: true, isNativeBridgeAvailable: true);
            var unsupportedPort = new RecordingAndroidVibrationNativePort(isAndroidRuntime: false, isNativeBridgeAvailable: true);
            var missingPort = new RecordingAndroidVibrationNativePort(isAndroidRuntime: true, isNativeBridgeAvailable: false);
            var cue = AndroidVibrationCue.FromCueId("vibration_ultimate_ready");

            var disabled = new AndroidNativeVibrationBridge(disabledPort, vibrationEnabled: false).TryVibrate(cue);
            var unsupported = new AndroidNativeVibrationBridge(unsupportedPort, vibrationEnabled: true).TryVibrate(cue);
            var missing = new AndroidNativeVibrationBridge(missingPort, vibrationEnabled: true).TryVibrate(cue);

            Assert.That(disabled.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.SkippedDisabled));
            Assert.That(unsupported.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.SkippedUnsupportedPlatform));
            Assert.That(missing.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.FailedNativeBridgeMissing));
            Assert.That(disabledPort.Calls.Count, Is.EqualTo(0));
            Assert.That(unsupportedPort.Calls.Count, Is.EqualTo(0));
            Assert.That(missingPort.Calls.Count, Is.EqualTo(0));
            Assert.That(disabled.CanFallbackToNoVibration, Is.EqualTo(true));
            Assert.That(unsupported.CanFallbackToNoVibration, Is.EqualTo(true));
            Assert.That(missing.CanFallbackToNoVibration, Is.EqualTo(true));
            Assert.That(missing.IsBlockingGameplay, Is.EqualTo(false));
        }

        [Test]
        public void NativeBridge_FailedNativeDispatchFallsBackWithoutRawDevicePayloads()
        {
            var port = new RecordingAndroidVibrationNativePort(isAndroidRuntime: true, isNativeBridgeAvailable: true)
            {
                DispatchSucceeds = false
            };
            var bridge = new AndroidNativeVibrationBridge(port, vibrationEnabled: true);

            var result = bridge.TryVibrate(AndroidVibrationCue.FromCueId("vibration_gacha_heavy"));

            Assert.That(result.Outcome, Is.EqualTo(AndroidVibrationDispatchOutcome.FailedNativeDispatch));
            Assert.That(result.CanFallbackToNoVibration, Is.EqualTo(true));
            Assert.That(result.IsBlockingGameplay, Is.EqualTo(false));
            Assert.That(result.HasRawDeviceIdentifier, Is.EqualTo(false));
            Assert.That(result.HasRawLogPayload, Is.EqualTo(false));
            Assert.That(port.Calls.Count, Is.EqualTo(1));
        }

        private sealed class RecordingAndroidVibrationNativePort : IAndroidVibrationNativePort
        {
            public RecordingAndroidVibrationNativePort(bool isAndroidRuntime, bool isNativeBridgeAvailable)
            {
                IsAndroidRuntime = isAndroidRuntime;
                IsNativeBridgeAvailable = isNativeBridgeAvailable;
                Calls = new List<VibrationCall>();
                DispatchSucceeds = true;
            }

            public bool IsAndroidRuntime { get; }
            public bool IsNativeBridgeAvailable { get; }
            public bool DispatchSucceeds { get; set; }
            public List<VibrationCall> Calls { get; }

            public bool TryVibrate(int durationMs, int amplitude)
            {
                Calls.Add(new VibrationCall(durationMs, amplitude));
                return DispatchSucceeds;
            }
        }

        private readonly struct VibrationCall
        {
            public VibrationCall(int durationMs, int amplitude)
            {
                DurationMs = durationMs;
                Amplitude = amplitude;
            }

            public int DurationMs { get; }
            public int Amplitude { get; }
        }
    }
}
