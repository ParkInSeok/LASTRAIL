#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace LastRail.Tests.EditMode
{
    internal sealed class LoopbackBundleQa : IDisposable
    {
        internal const string Origin = "http://127.0.0.1:18284";
        private readonly object context;
        private readonly bool hadRuntimePathPreference;
        private readonly string runtimePathPreference;
        internal readonly string StateDirectory;
        internal AddressableContentBackend Backend { get; }
        internal ClientContentGate Gate { get; private set; }

        internal LoopbackBundleQa(DownloadAccessPolicy policy, string endpoint = "/client-config.json")
        {
            hadRuntimePathPreference = PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath);
            runtimePathPreference = PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath, "");
            var settings = Environment.GetEnvironmentVariable("LASTRAIL_FIXTURE_SETTINGS");
            Assert.That(string.IsNullOrEmpty(settings), Is.False, "Set LASTRAIL_FIXTURE_SETTINGS to the freshly built Windows settings.json.");
            Assert.That(File.Exists(settings), Is.True, settings);
            StateDirectory = Path.Combine(Path.GetTempPath(), "lastrail-loopback-" + Guid.NewGuid().ToString("N"));
            var type = typeof(AddressableContentBackend).Assembly.GetType("LastRail.Resources.EditorLoopbackContentFixture", true);
            context = Invoke(type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic), null,
                Origin + endpoint, Origin + "/LOCAL_TEST/StandaloneWindows64/", settings, StateDirectory, "42");
            Backend = (AddressableContentBackend)Invoke(typeof(AddressableContentBackend).GetMethod(
                "CreateForEditorLoopbackFixture", BindingFlags.Static | BindingFlags.NonPublic), null, context, policy);
        }

        internal async Task PrepareColdAsync(CancellationToken cancel)
        {
            Gate = await Backend.FetchConfigAsync(cancel);
            Assert.That(Gate.Evaluate(null), Is.EqualTo(ContentStage.Initializing));
            await Backend.InitializeAsync(cancel);
            Assert.That(PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath), Is.EqualTo(hadRuntimePathPreference));
            Assert.That(PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath, ""), Is.EqualTo(runtimePathPreference),
                "Explicit fixture initialization must leave existing runtime-path preferences unchanged.");
            await Backend.UpdateCatalogAsync(cancel);
            // Read only the backend-owned locator for cache eviction; never inject a locator or gate.
            var locator = (IResourceLocator)typeof(AddressableContentBackend).GetField("approvedLocator",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Backend);
            Assert.That(locator.Locate(AddressableContentBackend.RequiredLabel, null, out var roots), Is.True);
            var plan = (IResourceLocation[])Invoke(context.GetType().GetMethod("BuildRemoteBundlePlan",
                BindingFlags.Instance | BindingFlags.NonPublic), context,
                new System.Collections.Generic.List<IResourceLocation>(roots).ToArray(),
                (Func<IResourceLocation, string>)Addressables.ResourceManager.TransformInternalId);
            Assert.That(plan.Length, Is.GreaterThan(0));
            foreach (var location in plan)
            {
                var options = (AssetBundleRequestOptions)location.Data;
                var hash = Hash128.Parse(options.Hash);
                Caching.ClearCachedVersion(options.BundleName, hash);
                Assert.That(Caching.IsVersionCached(new CachedAssetBundle(options.BundleName, hash)), Is.False);
            }
        }

        // Leave the isolated pin/catalog evidence available for the parent's review.
        public void Dispose() => Backend.Dispose();

        private static object Invoke(MethodInfo method, object owner, params object[] args)
        {
            Assert.That(method, Is.Not.Null);
            try { return method.Invoke(owner, args); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
    }

    public static class LoopbackBundleQaBuild
    {
        // Parent-only executeMethod. Uses the existing real builder and restores profile values.
        public static void BuildWindows()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Launch Unity with -buildTarget Win64.");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var profile = settings.profileSettings.GetProfileId("LOCAL_TEST");
            if (string.IsNullOrEmpty(profile)) throw new InvalidOperationException("Configure LOCAL_TEST first.");
            var oldProfile = settings.activeProfileId;
            var oldLoad = settings.profileSettings.GetValueByName(profile, AddressableAssetSettings.kRemoteLoadPath);
            try
            {
                settings.activeProfileId = profile;
                settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath,
                    LoopbackBundleQa.Origin + "/LOCAL_TEST/[BuildTarget]");
                var builder = Type.GetType("LastRail.Editor.AddressableResourceBuilder, LastRail.Editor", true);
                try { builder.GetMethod("BuildLocalContent").Invoke(null, null); }
                catch (TargetInvocationException error) { throw error.InnerException ?? error; }
                Debug.Log("LOOPBACK_FIXTURE_SETTINGS " + Path.GetFullPath(Path.Combine(Addressables.BuildPath, "settings.json")));
            }
            finally
            {
                settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath, oldLoad);
                settings.activeProfileId = oldProfile;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
