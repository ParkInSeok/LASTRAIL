using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class InstalledAdmissionTransportTests
    {
        private sealed class Wire : IRunApiHttpTransport
        {
            public RunApiHttpRequest Last;
            public int Count;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Last = request; Count++; return Task.FromResult(RunApiHttpResponse.Success(200, "{}")); }
        }
        private static IRunApiHttpTransport Create(Wire wire, long? code)
        {
            var type = typeof(PlayerAuthOwner).Assembly.GetType("LastRail.Auth.InstalledAdmissionTransport");
            Assert.That(type, Is.Not.Null, "Protected requests need installed admission headers");
            return (IRunApiHttpTransport)Activator.CreateInstance(type, wire, new AppBuildSnapshot("1.0.0", code), "android", "internal");
        }
        [Test]
        public async Task ContentPinFollowsCurrentOwnerAndCannotBeOverridden()
        {
            var wire=new Wire();string pin=null;
            var transport=(IRunApiHttpTransport)Activator.CreateInstance(typeof(InstalledAdmissionTransport),wire,new AppBuildSnapshot("0.1.0",42),"windows","qa",(Func<string>)(()=>pin));
            var request=new RunApiHttpRequest("GET","/v1/account/state",null,null);
            await transport.SendAsync(request);
            Assert.That(wire.Last.Headers.Any(h=>h.Name=="X-LastRail-Content-Release-Id"),Is.False);
            pin="release-a";await transport.SendAsync(request);
            Assert.That(wire.Last.Headers.Single(h=>h.Name=="X-LastRail-Content-Release-Id").Value,Is.EqualTo(pin));
            pin="release-b";await transport.SendAsync(request);
            Assert.That(wire.Last.Headers.Single(h=>h.Name=="X-LastRail-Content-Release-Id").Value,Is.EqualTo(pin));
            pin=null;await transport.SendAsync(request);
            Assert.That(wire.Last.Headers.Any(h=>h.Name=="X-LastRail-Content-Release-Id"),Is.False);
            var count=wire.Count;
            var rejected=await transport.SendAsync(new RunApiHttpRequest("GET","/v1/account/state",null,new[]{new RunApiHttpHeader("x-lastrail-content-release-id","forged")}));
            Assert.That(rejected.IsNetworkError,Is.True);Assert.That(wire.Count,Is.EqualTo(count));
        }
        [TestCase("/v1/auth/guest")]
        [TestCase("/v1/profile")]
        [TestCase("/v1/support/tickets")]
        [TestCase("/v1/runs/start")]
        public async Task ProtectedRequestPreservesBodyBearerAndReplayKey(string path)
        {
            var wire = new Wire(); var transport = Create(wire, 42);
            var original = new RunApiHttpRequest("POST", path, "{\"fixture\":true}", new[] {
                new RunApiHttpHeader("Authorization", "Bearer lst1.fixture"), new RunApiHttpHeader("Idempotency-Key", "fixture_key_00000001") });
            await transport.SendAsync(original);
            Assert.That(wire.Last.BodyJson, Is.EqualTo(original.BodyJson));
            Assert.That(wire.Last.Headers.Single(h => h.Name == "X-LastRail-App-Version").Value, Is.EqualTo("1.0.0"));
            Assert.That(wire.Last.Headers.Single(h => h.Name == "X-LastRail-App-Version-Code").Value, Is.EqualTo("42"));
            Assert.That(wire.Last.Headers.Single(h => h.Name == "X-LastRail-App-Platform").Value, Is.EqualTo("android"));
            Assert.That(wire.Last.Headers.Single(h => h.Name == "X-LastRail-App-Channel").Value, Is.EqualTo("internal"));
            Assert.That(wire.Last.Headers.Single(h => h.Name == "Authorization").Value, Is.EqualTo("Bearer lst1.fixture"));
            await transport.SendAsync(original);
            Assert.That(wire.Last.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo("fixture_key_00000001"));
            Assert.That(original.Headers.Count, Is.EqualTo(2));
        }
        [Test]
        public async Task CallerCannotOverrideInstalledCode()
        {
            var wire = new Wire(); var transport = Create(wire, 42);
            var response = await transport.SendAsync(new RunApiHttpRequest("GET", "/v1/profile", null,
                new[] { new RunApiHttpHeader("x-lastrail-app-version-code", "999") }));
            Assert.That(wire.Count, Is.Zero); Assert.That(response.IsNetworkError, Is.True);
        }
        [TestCase("X-LastRail-App-Version")]
        [TestCase("x-lastrail-app-version")]
        public async Task FrozenRequestCannotInjectOldDisplayVersion(string name)
        {
            var wire = new Wire(); var transport = Create(wire, 42);
            var original = new RunApiHttpRequest("POST", "/v1/runs/start", "{\"fixture\":true}", new[] {
                new RunApiHttpHeader(name, "old-build-B"),
                new RunApiHttpHeader("Idempotency-Key", "fixture_key_00000001") });
            var response = await transport.SendAsync(original);
            Assert.That(response.IsNetworkError, Is.True); Assert.That(wire.Count, Is.Zero);
            Assert.That(original.Headers.Count, Is.EqualTo(2));
            Assert.That(original.Headers[0].Value, Is.EqualTo("old-build-B"));
        }

        [Test]
        public async Task FrozenBodyAndKeyReplayUsesCurrentInstalledDisplayVersionWithoutMutatingOriginal()
        {
            var wire = new Wire();
            var buildB = new InstalledAdmissionTransport(wire, new AppBuildSnapshot("1.2.0", 42), "android", "internal");
            var buildC = new InstalledAdmissionTransport(wire, new AppBuildSnapshot("1.3.0", 43), "android", "internal");
            var original = new RunApiHttpRequest("POST", "/v1/runs/start", "{\"fixture\":true}", new[] {
                new RunApiHttpHeader("Authorization", "Bearer lst1.fixture"),
                new RunApiHttpHeader("Idempotency-Key", "fixture_key_00000001") });
            await buildB.SendAsync(original);
            var sentB = wire.Last;
            await buildC.SendAsync(original);
            var sentC = wire.Last;
            Assert.That(sentB.Headers.Single(h => h.Name == "X-LastRail-App-Version").Value, Is.EqualTo("1.2.0"));
            Assert.That(sentC.Headers.Single(h => h.Name == "X-LastRail-App-Version").Value, Is.EqualTo("1.3.0"));
            Assert.That(sentC.Headers.Single(h => h.Name == "X-LastRail-App-Version-Code").Value, Is.EqualTo("43"));
            Assert.That(sentC.BodyJson, Is.EqualTo(original.BodyJson));
            Assert.That(sentC.Method, Is.EqualTo(original.Method));
            Assert.That(sentC.EndpointPath, Is.EqualTo(original.EndpointPath));
            Assert.That(sentC.Headers.Single(h => h.Name == "Idempotency-Key").Value, Is.EqualTo("fixture_key_00000001"));
            Assert.That(sentC.Headers.Single(h => h.Name == "Authorization").Value, Is.EqualTo("Bearer lst1.fixture"));
            Assert.That(original.Headers.Count, Is.EqualTo(2));
            Assert.That(original.Headers.Any(h => h.Name.StartsWith("X-LastRail-App-", StringComparison.OrdinalIgnoreCase)), Is.False);
        }
        [Test]
        public async Task MissingEditorCodeCannotSendProtectedRequest()
        {
            var wire = new Wire(); var response = await Create(wire, null).SendAsync(new RunApiHttpRequest("GET", "/v1/profile", null, null));
            Assert.That(wire.Count, Is.Zero); Assert.That(response.IsNetworkError, Is.True);
        }

        [Test]
        public void ProtectedWireDoesNotForwardSessionToRedirectTargets()
        {
            var adapter = new UnityWebRequestRunApiHttpAdapter("https://api.lastrail.app", 30);
            var request = UnityWebRequestTransportRequest.FromRunApiRequest(new RunApiHttpRequest("GET", "/v1/profile", null,
                new[] { new RunApiHttpHeader("Authorization", "Bearer lst1.fixture") }));
            using (var native = (UnityEngine.Networking.UnityWebRequest)typeof(UnityWebRequestRunApiHttpAdapter)
                .GetMethod("BuildRequest", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(adapter, new object[] { request }))
                Assert.That(native.redirectLimit, Is.Zero);
        }
    }
}
