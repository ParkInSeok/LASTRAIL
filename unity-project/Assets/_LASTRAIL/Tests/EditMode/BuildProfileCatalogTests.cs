using NUnit.Framework;
using LastRail.Build;
using LastRail.Client;

namespace LastRail.Tests.EditMode
{
    public sealed class BuildProfileCatalogTests
    {
        [Test]
        public void All_ReturnsAndroidDevAndPrdProfiles()
        {
            var profiles = BuildProfileCatalog.All;

            Assert.That(profiles.Length, Is.EqualTo(2));
            Assert.That(profiles[0].Name, Is.EqualTo("Android_DEV"));
            Assert.That(profiles[0].Environment, Is.EqualTo(ClientEnvironment.Dev));
            Assert.That(profiles[0].EnvironmentDefine, Is.EqualTo("LASTRAIL_ENV_DEV"));
            Assert.That(profiles[0].DevelopmentBuild, Is.EqualTo(true));
            Assert.That(profiles[0].OutputExtension, Is.EqualTo("apk"));

            Assert.That(profiles[1].Name, Is.EqualTo("Android_PRD"));
            Assert.That(profiles[1].Environment, Is.EqualTo(ClientEnvironment.Prd));
            Assert.That(profiles[1].EnvironmentDefine, Is.EqualTo("LASTRAIL_ENV_PRD"));
            Assert.That(profiles[1].DevelopmentBuild, Is.EqualTo(false));
            Assert.That(profiles[1].OutputExtension, Is.EqualTo("aab"));
        }

        [Test]
        public void ForName_ReturnsProfileByExactName()
        {
            var profile = BuildProfileCatalog.ForName("Android_PRD");

            Assert.That(profile.Environment, Is.EqualTo(ClientEnvironment.Prd));
        }

        [Test]
        public void ForName_RejectsStageProfileInCurrentScope()
        {
            Assert.Throws<System.InvalidOperationException>(() => BuildProfileCatalog.ForName("Android_STAGE"));
        }

        [Test]
        public void All_UsesOnlyLaunchServerEnvironmentDefines()
        {
            var profiles = BuildProfileCatalog.All;
            var defines = new System.Collections.Generic.HashSet<string>();

            foreach (var profile in profiles)
            {
                defines.Add(profile.EnvironmentDefine);
                Assert.That(profile.EnvironmentDefine, Does.StartWith("LASTRAIL_ENV_"));
            }

            Assert.That(defines, Is.EquivalentTo(new[] { "LASTRAIL_ENV_DEV", "LASTRAIL_ENV_PRD" }));
        }
    }
}
