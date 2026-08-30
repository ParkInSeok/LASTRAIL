using System;
using NUnit.Framework;
using LastRail.Presentation;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchBlockerClosurePlanTests
    {
        [Test]
        public void CurrentPlan_ListsEditorPackageDeviceHandoffStepsInLaunchOrder()
        {
            var plan = LaunchBlockerClosurePlan.ForCurrentCodeReadiness();

            Assert.That(plan.StepCount, Is.EqualTo(7));
            Assert.That(plan.Steps[0].StepKey, Is.EqualTo("unity_2d_animation_package_resolve"));
            Assert.That(plan.Steps[1].StepKey, Is.EqualTo("scene_prefab_binding_pass"));
            Assert.That(plan.Steps[2].StepKey, Is.EqualTo("player_facing_screenshot_qa_7_screens"));
            Assert.That(plan.Steps[3].StepKey, Is.EqualTo("android_vibration_native_device_pass"));
            Assert.That(plan.Steps[4].StepKey, Is.EqualTo("android_keystore_secure_storage_plugin_pass"));
            Assert.That(plan.Steps[5].StepKey, Is.EqualTo("google_sign_in_sdk_pass_after_app_registration"));
            Assert.That(plan.Steps[6].StepKey, Is.EqualTo("android_build_smoke"));
            Assert.That(plan.Steps[0].RequiresEditorApproval, Is.EqualTo(true));
            Assert.That(plan.Steps[2].RequiresDeviceValidation, Is.EqualTo(true));
            Assert.That(plan.Steps[3].RequiresDeviceValidation, Is.EqualTo(true));
            Assert.That(plan.Steps[5].RequiresUserApproval, Is.EqualTo(true));
            Assert.That(plan.Steps[5].RequiresOauthAppRegistration, Is.EqualTo(true));
            Assert.That(plan.IsImplementationReadyForHandoff, Is.EqualTo(true));
            Assert.That(plan.IsLaunchQaComplete, Is.EqualTo(false));
        }

        [Test]
        public void CurrentPlan_ReportsOnlyEditorPackageDeviceAndApprovalBlockersWithoutNewScope()
        {
            var plan = LaunchBlockerClosurePlan.ForCurrentCodeReadiness();

            Assert.That(plan.OpenBlockerCount, Is.EqualTo(7));
            Assert.That(plan.OpenBlockers, Does.Contain("com_unity_2d_animation_package_resolve_pending"));
            Assert.That(plan.OpenBlockers, Does.Contain("scene_prefab_binding_pending"));
            Assert.That(plan.OpenBlockers, Does.Contain("editor_device_screenshot_qa_unavailable"));
            Assert.That(plan.OpenBlockers, Does.Contain("native_vibration_bridge_pending"));
            Assert.That(plan.OpenBlockers, Does.Contain("android_keystore_secure_storage_plugin_pending"));
            Assert.That(plan.OpenBlockers, Does.Contain("google_sdk_oauth_app_registration_pending"));
            Assert.That(plan.OpenBlockers, Does.Contain("android_build_smoke_pending"));
            Assert.That(plan.RequiresNewServerScope, Is.EqualTo(false));
            Assert.That(plan.RequiresNewAdminScope, Is.EqualTo(false));
            Assert.That(plan.RequiresNewNarrativeScope, Is.EqualTo(false));
            Assert.That(plan.RequiresRailChoiceEvents, Is.EqualTo(false));
            Assert.That(plan.RequiresWorldRaidUi, Is.EqualTo(false));
            Assert.That(plan.HasServerTruthCalculation, Is.EqualTo(false));
            Assert.That(plan.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(plan.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(plan.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesStableIdsCountsAndBlockersWithoutSensitivePayloads()
        {
            var plan = LaunchBlockerClosurePlan.ForCurrentCodeReadiness();

            var json = LaunchBlockerClosurePlanJsonAdapter.ToJson(plan);

            Assert.That(json.Contains("\"stepCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"openBlockerCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"unity_2d_animation_package_resolve\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"player_facing_screenshot_qa_7_screens\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"google_sign_in_sdk_pass_after_app_registration\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresNewServerScope\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresNewAdminScope\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiresNewNarrativeScope\":false"), Is.EqualTo(true));
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
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }
    }
}
