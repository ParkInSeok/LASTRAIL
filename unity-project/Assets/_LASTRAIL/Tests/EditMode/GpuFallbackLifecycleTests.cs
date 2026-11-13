using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class GpuFallbackLifecycleTests
    {
        // Read-only observation of the package owner. Never release its resource
        // from the test: omitting the owner's shutdown cleanup must fail this.
        static ComputeBuffer CurrentBuffer()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "Unity.2D.Animation.Runtime");
            return (ComputeBuffer)assembly.GetType("UnityEngine.U2D.Animation.GpuDeformationSystem", true)
                .GetField("s_FallbackBuffer", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        }

        [UnityTest]
        public IEnumerator RepeatedPlayStopReleasesTheOriginalFallbackWithoutDomainReload()
        {
            if (!SystemInfo.supportsComputeShaders) Assert.Ignore("Requires a graphics device with compute support.");
            var enabled = EditorSettings.enterPlayModeOptionsEnabled;
            var options = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            try
            {
                for (var iteration = 0; iteration < 2; iteration++)
                {
                    yield return new EnterPlayMode();
                    var original = CurrentBuffer();
                    Assert.That(original, Is.Not.Null, "Fallback must remain available while playing.");
                    Assert.That(original.IsValid(), Is.True);
                    yield return new ExitPlayMode();
                    Assert.That(original.IsValid(), Is.False, "Play shutdown must release the original owned buffer.");
                }
            }
            finally
            {
                EditorSettings.enterPlayModeOptionsEnabled = enabled;
                EditorSettings.enterPlayModeOptions = options;
            }
        }
    }
}
