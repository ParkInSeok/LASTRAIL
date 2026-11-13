#if UNITY_EDITOR
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class ApiVerificationConfigurationTests
    {
        static Type Configuration => typeof(ApiVerificationConfigurationTests).Assembly.GetType("LastRail.Tests.EditMode.ApiVerificationConfiguration");
        static JObject Input(string origin = "http://127.0.0.1:12345") => new JObject {
            ["origin"] = origin, ["accountId"] = "isolated", ["authorization"] = "Bearer secret-value",
            ["expiresAt"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds(), ["disposable"] = true,
            ["profile"] = "ACCOUNT_READ_LOCAL", ["appVersionCode"] = 123,
            ["serverRevision"] = new string('a',40), ["environment"] = "LOCAL_SQLJS" };
        static object Parse(JObject value) {
            Assert.That(Configuration, Is.Not.Null, "API verification configuration boundary is missing.");
            return Configuration.GetMethod("Parse").Invoke(null,new object[]{value});
        }
        [TestCase("https://api.lastrail.app")]
        [TestCase("http://dev-api.lastrail.app")]
        [TestCase("http://localhost:1234")]
        [TestCase("http://127.0.0.1:1234/path")]
        [TestCase("http://user:pass@127.0.0.1:1234")]
        public void UnsafeOriginIsRejected(string origin) {
            Assert.That(Configuration, Is.Not.Null, "API verification configuration boundary is missing.");
            var failure = Assert.Throws<TargetInvocationException>(()=>Parse(Input(origin)));
            Assert.That(failure.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(failure.InnerException.Message, Does.Not.Contain(origin));
        }
        [Test]
        public void ExplicitDisposableLocalConfigurationProducesSafeMetadata() {
            var value=Parse(Input());
            var metadata=(JObject)Configuration.GetMethod("SafeMetadata").Invoke(value,null);
            Assert.That((string)metadata["environment"], Is.EqualTo("LOCAL_SQLJS"));
            var json=metadata.ToString();
            Assert.That(json, Does.Not.Contain("secret-value"));
            Assert.That(json, Does.Not.Contain("isolated"));
            Assert.That(json, Does.Not.Contain("authorization"));
        }
        [TestCase("disposable")][TestCase("expiresAt")][TestCase("serverRevision")][TestCase("appVersionCode")]
        public void MissingRequiredBoundaryIsRejected(string field) {
            Assert.That(Configuration, Is.Not.Null, "API verification configuration boundary is missing.");
            var input=Input(); input.Remove(field);
            Assert.Throws<TargetInvocationException>(()=>Parse(input));
        }
        [Test] public void SecondaryBoundaryMismatchesAndExpiryRejectIndividually(){var input=Input();input["profile"]="SUPPORT_LOCAL_DEV";var primary=Parse(input);foreach(var field in new[]{"accountId","authorization","serverRevision","profile","environment","expiresAt"}){var other=(JObject)input.DeepClone();other["accountId"]="other";other["authorization"]="Bearer other-secret";if(field=="accountId")other[field]=input[field].DeepClone();else if(field=="authorization")other[field]=input[field].DeepClone();else if(field=="serverRevision")other[field]=new string('b',40);else if(field=="profile")other[field]="ACCOUNT_READ_LOCAL";else if(field=="environment")other[field]="DEV_ISOLATED";else other[field]=0;Assert.Throws<TargetInvocationException>(()=>Other(other,primary),field);}}
        static object Other(JObject secondary,object primary){var method=Configuration.GetMethod("ParseOther");Assert.NotNull(method,"Secondary isolated account boundary missing");return method.Invoke(null,new[]{(object)secondary,primary});}
        [Test] public void SecondaryAccountRequiresSameSupplierAndDistinctOwner(){var input=Input();input["profile"]="SUPPORT_LOCAL_DEV";var primary=Parse(input);var other=(JObject)input.DeepClone();other["accountId"]="isolated-other";other["authorization"]="Bearer other-secret";Assert.NotNull(Other(other,primary));other["origin"]="http://127.0.0.1:54321";Assert.Throws<TargetInvocationException>(()=>Other(other,primary));other=(JObject)input.DeepClone();Assert.Throws<TargetInvocationException>(()=>Other(other,primary));other["accountId"]="other";other["authorization"]="Bearer other-secret";other["appVersionCode"]=124;Assert.Throws<TargetInvocationException>(()=>Other(other,primary));}
        [Test]
        public void ExpiredAndSharedFixtureAreRejected() {
            Assert.That(Configuration, Is.Not.Null, "API verification configuration boundary is missing.");
            var input=Input(); input["expiresAt"]=0;
            Assert.Throws<TargetInvocationException>(()=>Parse(input));
            input=Input(); input["disposable"]=false;
            Assert.Throws<TargetInvocationException>(()=>Parse(input));
        }
    }
}
#endif
