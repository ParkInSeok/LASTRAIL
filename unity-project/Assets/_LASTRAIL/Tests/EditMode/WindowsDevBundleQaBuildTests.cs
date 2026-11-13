#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class WindowsDevBundleQaBuildTests
    {
        [TestCase(null)]
        [TestCase("Android")]
        [TestCase("StandaloneWindows64")]
        public void MissingOrUnauthorizedReleaseFailsBeforeBuild(string target)
        {
            var previous = Environment.GetEnvironmentVariable("LASTRAIL_WINDOWS_DEV_BUILD_CONFIG");
            var path = Path.Combine(Path.GetTempPath(), "windows-dev-build-rejection-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                if (target != null) File.WriteAllText(path, new JObject { ["environment"] = "DEV", ["target"] = target,
                    ["releaseId"] = "unapproved", ["releaseNamespace"] = "https://dev-content.lastrail.app/Android/" }.ToString());
                Environment.SetEnvironmentVariable("LASTRAIL_WINDOWS_DEV_BUILD_CONFIG", target == null ? null : path);
                var type = Type.GetType("LastRail.Editor.WindowsDevBundleQaBuild, LastRail.Editor", true);
                var error = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Build").Invoke(null, null));
                Assert.That(error.InnerException, Is.TypeOf<InvalidDataException>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("LASTRAIL_WINDOWS_DEV_BUILD_CONFIG", previous);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
#endif
