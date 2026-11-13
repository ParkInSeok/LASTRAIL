#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using System.Linq;

namespace LastRail.Tests.EditMode
{
    public sealed class AddressableResourceTests
    {
        [Test]
        public void BootstrapIsOnlyEnabledBuildScene()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            Assert.That(scenes.Length, Is.EqualTo(1));
            Assert.That(scenes[0].path, Is.EqualTo("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity"));
        }

        [Test]
        public void AddressableSettingsAndInventoryExist()
        {
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/AddressableAssetsData/AddressableAssetSettings.asset"), Is.Not.Null);
            Assert.That(System.IO.File.Exists("QA/Act1Launch/Addressables/resource-inventory.json"), Is.True);
        }

        [Test]
        public void BootstrapDoesNotPullLargeGameFontOrGameScreens()
        {
            var dependencies = AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity", true);
            Assert.That(dependencies, Does.Contain("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
            Assert.That(dependencies, Does.Not.Contain("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"));
            Assert.That(dependencies, Does.Not.Contain("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab"));
            Assert.That(dependencies, Does.Not.Contain("Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab"));
            Assert.That(dependencies.Any(p => p.Contains("/Sources/") || p.Contains("Showcase")), Is.False);
        }

        [Test]
        public void RemoteProfilesAreExplicitlyUnconfigured()
        {
            var settings = System.IO.File.ReadAllText("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
            Assert.That(settings, Does.Contain("UNCONFIGURED_DEV_CONTENT_HOST"));
            Assert.That(settings, Does.Contain("UNCONFIGURED_PRD_CONTENT_HOST"));
        }
    }
}
#endif
