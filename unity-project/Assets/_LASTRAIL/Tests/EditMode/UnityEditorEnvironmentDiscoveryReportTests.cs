using NUnit.Framework;
using LastRail.Presentation;

namespace LastRail.Tests.EditMode
{
    public sealed class UnityEditorEnvironmentDiscoveryReportTests
    {
        [Test]
        public void FromDiscoveredHints_WhenEditorExecutableMissing_RecordsNoopDryRunAndExactBlockers()
        {
            var report = UnityEditorEnvironmentDiscoveryReport.FromDiscoveredHints(
                "6000.3.22f1",
                "6000.3.22f1 (1c726e1fb402)",
                editorExecutablePath: null,
                isUnity2DAnimationPackageDeclared: false,
                hasUnityHubPathHint: false);

            Assert.That(report.ProjectEditorVersion, Is.EqualTo("6000.3.22f1"));
            Assert.That(report.ProjectEditorVersionWithRevision, Is.EqualTo("6000.3.22f1 (1c726e1fb402)"));
            Assert.That(report.HasEditorExecutablePath, Is.EqualTo(false));
            Assert.That(report.CanRunEditorDryRun, Is.EqualTo(false));
            Assert.That(report.EditorDryRunStatus, Is.EqualTo("NOOP_EDITOR_EXECUTABLE_NOT_DISCOVERED"));
            Assert.That(report.IsUnity2DAnimationPackageDeclared, Is.EqualTo(false));
            Assert.That(report.OpenBlockers, Does.Contain("unity_editor_executable_not_discovered"));
            Assert.That(report.OpenBlockers, Does.Contain("com_unity_2d_animation_package_resolve_pending"));
            Assert.That(report.OpenBlockers, Does.Contain("editor_device_screenshot_qa_unavailable"));
            Assert.That(report.RequiresPackageImport, Is.EqualTo(false));
            Assert.That(report.RequiresGoogleSdkImport, Is.EqualTo(false));
            Assert.That(report.RequiresSecureStoragePluginImport, Is.EqualTo(false));
            Assert.That(report.RequiresAndroidDeviceOperation, Is.EqualTo(false));
        }

        [Test]
        public void FromDiscoveredHints_WhenEditorExecutableExists_AllowsSafestBatchmodeDryRunCandidate()
        {
            var report = UnityEditorEnvironmentDiscoveryReport.FromDiscoveredHints(
                "6000.3.22f1",
                "6000.3.22f1 (1c726e1fb402)",
                editorExecutablePath: "C:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe",
                isUnity2DAnimationPackageDeclared: true,
                hasUnityHubPathHint: true);

            Assert.That(report.HasEditorExecutablePath, Is.EqualTo(true));
            Assert.That(report.CanRunEditorDryRun, Is.EqualTo(true));
            Assert.That(report.EditorDryRunStatus, Is.EqualTo("READY_FOR_BATCHMODE_DRY_RUN"));
            Assert.That(report.RecommendedDryRunCommandKind, Is.EqualTo("batchmode_editmode_tests_or_compile_smoke"));
            Assert.That(report.OpenBlockers, Does.Not.Contain("unity_editor_executable_not_discovered"));
            Assert.That(report.OpenBlockers, Does.Not.Contain("com_unity_2d_animation_package_resolve_pending"));
        }

        [Test]
        public void ToJson_WritesStableHintsAndBlockersWithoutSensitiveOrRawPayloads()
        {
            var report = UnityEditorEnvironmentDiscoveryReport.FromDiscoveredHints(
                "6000.3.22f1",
                "6000.3.22f1 (1c726e1fb402)",
                editorExecutablePath: null,
                isUnity2DAnimationPackageDeclared: false,
                hasUnityHubPathHint: false);

            var json = UnityEditorEnvironmentDiscoveryReportJsonAdapter.ToJson(report);

            Assert.That(json.Contains("\"projectEditorVersion\":\"6000.3.22f1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEditorExecutablePath\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"canRunEditorDryRun\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"editorDryRunStatus\":\"NOOP_EDITOR_EXECUTABLE_NOT_DISCOVERED\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"openBlockers\":[\"unity_editor_executable_not_discovered\",\"com_unity_2d_animation_package_resolve_pending\",\"editor_device_screenshot_qa_unavailable\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("deviceId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawAssetPath"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("fullLocalizedCopy"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("GoogleSignIn"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }
    }
}
