#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace LastRail.Tests.EditMode
{
    public sealed class LocalContentBaselineBuilderTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CatalogAndGroupPathOverridesAreRejectedBeforeBuild(bool groupOverride)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var reference = groupOverride
                ? settings.groups.First(g => g != null && g.entries.Count > 0 && g.GetSchema<BundledAssetGroupSchema>() != null).GetSchema<BundledAssetGroupSchema>().BuildPath
                : settings.RemoteCatalogLoadPath;
            var property = typeof(ProfileValueReference).GetProperty("Id");
            var original = reference.Id;
            try
            {
                property.SetValue(reference, groupOverride ? "QA/override-output" : "http://127.0.0.1:18284/override");
                var method = Type.GetType("LastRail.Editor.AddressableResourceBuilder, LastRail.Editor", true)
                    .GetMethod("LocalSettings", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.Throws<TargetInvocationException>(() => method.Invoke(null, null));
            }
            finally { property.SetValue(reference, original); }
        }
        [TestCase(null)]
        [TestCase("")]
        [TestCase("../outside")]
        public void UpdateRequiresExplicitSafeBaselineBeforeStartingBuild(string id)
        {
            var type = Type.GetType("LastRail.Editor.AddressableResourceBuilder, LastRail.Editor", true);
            var method = type.GetMethod("BuildLocalUpdate", new[] { typeof(string), typeof(string) });
            Assert.That(method, Is.Not.Null);
            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { id, "fixture_update" }));
            Assert.That(error.InnerException, Is.InstanceOf<ArgumentException>());
        }

        [Test]
        public void MissingBaselineDoesNotStartBuild()
        {
            var type = Type.GetType("LastRail.Editor.AddressableResourceBuilder, LastRail.Editor", true);
            var method = type.GetMethod("BuildLocalUpdate", new[] { typeof(string), typeof(string) });
            Assert.That(method, Is.Not.Null);
            Assert.Throws<TargetInvocationException>(() => method.Invoke(null,
                new object[] { "missing_" + Guid.NewGuid().ToString("N"), "fixture_update" }));
        }
    }
}
#endif
