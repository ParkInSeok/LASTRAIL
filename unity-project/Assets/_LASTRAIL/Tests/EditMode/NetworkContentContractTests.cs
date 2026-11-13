using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace LastRail.Tests.EditMode
{
    public sealed class NetworkContentContractTests
    {
        [Test]
        public void RuntimeHasIndependentNetworkDownloadOwner()
        {
            Assert.That(typeof(LastRail.Resources.ContentStartupFlow).Assembly.GetType("LastRail.Resources.DownloadCoordinator"), Is.Not.Null);
        }

        [Test]
        public void RemoteBundlesCannotBypassConsentViaDefaultProvider()
        {
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
                if (group != null && group.Name.StartsWith("remote_", StringComparison.Ordinal))
                    Assert.That(group.GetSchema<BundledAssetGroupSchema>().AssetBundleProviderType.Value.Name,
                        Is.EqualTo("CacheOnlyAssetBundleProvider"), group.Name);
        }
    }
}
