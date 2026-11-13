using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class OfflineBootstrapResourcesTests
    {
        static string[] Entries(string field)
        {
            var type = Type.GetType("LastRail.Editor.OfflineBootstrapResources, LastRail.Editor", true);
            return (string[])type.GetField(field).GetValue(null);
        }

        [Test]
        public void ExplicitOfflineEntriesExistAndAreUnique()
        {
            var keys = Entries("ResourceKeys");
            var paths = Entries("DirectAssetPaths");
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
            Assert.That(paths.Distinct().Count(), Is.EqualTo(paths.Length));
            foreach (var key in keys)
                Assert.That(UnityEngine.Resources.Load(key), Is.Not.Null, key);
            foreach (var path in paths)
                Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Not.Null, path);
        }

        [Test]
        public void DisabledLegacyVisualDoesNotPackageCombatBackground()
        {
            var path = "Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.transform.Find("StartupKeyVisual").GetComponent<Image>().sprite, Is.Null);
            Assert.That(AssetDatabase.GetDependencies(path, true), Does.Not.Contain(
                "Assets/_LASTRAIL/UI/Backgrounds/bg_combat_wasteland_rail.png"));
        }
    }
}
