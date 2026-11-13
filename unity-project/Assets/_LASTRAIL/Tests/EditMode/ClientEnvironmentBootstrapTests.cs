using System;
using System.IO;
using NUnit.Framework;
using LastRail.Client;

namespace LastRail.Tests.EditMode
{
    public sealed class ClientEnvironmentBootstrapTests
    {
        [Test]
        public void UrlFor_ReturnsClientConfigUrlForEachEnvironment()
        {
            Assert.That(ClientEnvironmentBootstrap.UrlFor(ClientEnvironment.Dev), Is.EqualTo("https://dev-api.lastrail.app/v1/client-config"));
            Assert.That(ClientEnvironmentBootstrap.UrlFor(ClientEnvironment.Prd), Is.EqualTo("https://api.lastrail.app/v1/client-config"));
        }

        [Test]
        public void UrlFor_RejectsUnconfiguredEnvironment()
        {
            Assert.Throws<InvalidOperationException>(() => ClientEnvironmentBootstrap.UrlFor(ClientEnvironment.Unconfigured));
        }

        [Test]
        public void EnvironmentSource_HasCompileTimeGuardAgainstMultipleDefines()
        {
            var source = File.ReadAllText("Assets/_LASTRAIL/Scripts/Client/ClientEnvironmentBootstrap.cs");

            Assert.That(source, Does.Contain("#if LASTRAIL_ENV_DEV && LASTRAIL_ENV_PRD"));
            Assert.That(source, Does.Contain("#error Exactly one LASTRAIL_ENV_* define must be active"));
        }

        [Test]
        public void EnvironmentSource_DoesNotUseStaleBackendEnvironmentAliases()
        {
            var source = File.ReadAllText("Assets/_LASTRAIL/Scripts/Client/ClientEnvironmentBootstrap.cs");
            var staleAliases = new[]
            {
                "LASTRAIL_ENV_STAGE",
                "LASTRAIL_ENV_QA",
                "LASTRAIL_ENV_LOCAL_SERVER",
                "LASTRAIL_ENV_MOCK_SERVER",
                "LASTRAIL_ENV_TEST_SERVER",
                "LASTRAIL_ENV_PROD"
            };

            foreach (var alias in staleAliases)
            {
                Assert.That(source, Does.Not.Contain(alias));
            }
        }
    }
}
