#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Auth;
using LastRail.Client;
using LastRail.Diagnostics;
using LastRail.Resources;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class DevRemoteQaPreflightTests
    {
        [Test]
        public void ReportActualEnvironmentWithoutReadingSessionsOrCallingRemoteServices()
        {
            if (Environment.GetEnvironmentVariable("LASTRAIL_DEV_REMOTE_QA_PREFLIGHT") != "1")
                Assert.Ignore("Preflight report is opt-in. This is not a remote integration test.");
            var blockers = new List<string> { "REAL_SESSION_QA_RUNNER_SECURITY_APPROVAL_REQUIRED" };
            if (ClientEnvironmentBootstrap.CurrentEnvironment != ClientEnvironment.Dev) blockers.Add("DEV_ENVIRONMENT_REQUIRED");
            if (AppBuildSnapshot.CaptureInstalled().AppVersionCode == null) blockers.Add("REAL_INSTALLED_VERSION_CODE_UNAVAILABLE");
            var storage = new AndroidKeystoreNativeStoragePort();
            if (!storage.IsAndroidRuntime || !storage.IsKeystoreAvailable) blockers.Add("ANDROID_KEYSTORE_UNAVAILABLE");
            if (!typeof(IAuthenticatedContentBackend).IsAssignableFrom(typeof(AddressableContentBackend)))
                blockers.Add("AUTHORIZED_PRODUCTION_ADAPTER_NOT_CONNECTED");
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../QA/Reports"));
            Directory.CreateDirectory(directory);
            var report = new JObject { ["checkedAtUtc"] = DateTime.UtcNow.ToString("o"), ["status"] = "BLOCKED",
                ["scope"] = "PREFLIGHT_ONLY_NOT_REMOTE_FLOW", ["reasons"] = new JArray(blockers),
                ["sessionRead"] = false, ["authenticationAttempted"] = false, ["remoteDownloadAttempted"] = false,
                ["lobbyVerified"] = false, ["deviceVerified"] = false, ["fixtureUsed"] = false };
            File.WriteAllText(Path.Combine(directory, "dev-remote-startup-preflight.json"), report.ToString());
            Assert.Ignore("BLOCKED: real remote suite not executed; see QA/Reports/dev-remote-startup-preflight.json.");
        }
    }
}
#endif
