using System;
using System.Reflection;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class DevAuthorizedContentClientTests
    {
        private static void Validate(string endpoint)
        {
            var type = Type.GetType("LastRail.Resources.DevAuthorizedContentClient, LastRail.Runtime");
            Assert.That(type, Is.Not.Null, "Scoped DEV authorized client is missing.");
            try { type.GetMethod("ValidateEndpoint").Invoke(null, new object[] { endpoint }); }
            catch (TargetInvocationException error) { throw error.InnerException; }
        }

        [Test]
        public void AcceptsOnlyTheApprovedExactDevEndpoint() =>
            Validate("https://dev-api.lastrail.app/v1/client-config/authorized");

        [TestCase("https://api.lastrail.app/v1/client-config/authorized")]
        [TestCase("https://dev-api.lastrail.app/v1/profile")]
        [TestCase("https://dev-api.lastrail.app/v1/client-config/authorized/")]
        [TestCase("https://dev-api.lastrail.app/v1/client-config/authorized?next=other")]
        [TestCase("https://dev-api.lastrail.app:444/v1/client-config/authorized")]
        [TestCase("https://dev-api.lastrail.app.attacker.test/v1/client-config/authorized")]
        [TestCase("http://dev-api.lastrail.app/v1/client-config/authorized")]
        [TestCase("https://dev-content.lastrail.app/android/releases/catalog.bin")]
        [TestCase(null)]
        public void RejectsEveryOtherCredentialDestination(string endpoint) =>
            Assert.Throws<InvalidOperationException>(() => Validate(endpoint));
    }
}
