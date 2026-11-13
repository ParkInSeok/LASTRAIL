using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using LastRail.Resources;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RequiredUpdateActionTests
    {
        // Synthetic fixture only. Every opener is an in-memory delegate.
        private const string Approved = "https://play.google.com/store/apps/details?id=fixture.update.test";
        private sealed class Fixture
        {
            public readonly object Action;
            public readonly List<string> Opens = new List<string>();
            public bool Result = true;
            public bool Throw;
            public Fixture(string url = Approved, string[] allowed = null, bool missing = false)
            {
                var assembly = typeof(ClientContentGate).Assembly;
                var type = assembly.GetType("LastRail.Resources.RequiredUpdateAction");
                var port = assembly.GetType("LastRail.Resources.ITrustedUpdateDestination");
                Assert.That(type, Is.Not.Null, "Missing RequiredUpdateAction");
                Assert.That(port, Is.Not.Null, "Missing ITrustedUpdateDestination");
                Assert.That(port.IsInterface, Is.True);
                var ctor = type.GetConstructor(new[] { port, typeof(IReadOnlyCollection<string>), typeof(Func<string, bool>) });
                Assert.That(ctor, Is.Not.Null, "Expected approved destination/allowlist/opener constructor");
                Action = ctor.Invoke(new object[] { missing ? null : Destination(port, url), allowed ?? new[] { Approved },
                    (Func<string, bool>)(value => { Opens.Add(value); if (Throw) throw new InvalidOperationException("raw-secret-opener-error"); return Result; }) });
            }
            public bool CanOpen => (bool)Property("CanOpen");
            public string Notice => (string)Property("Notice");
            private object Property(string name)
            {
                var property = Action.GetType().GetProperty(name);
                Assert.That(property, Is.Not.Null, name);
                return property.GetValue(Action);
            }
            public bool Click()
            {
                var method = Action.GetType().GetMethod("OpenFromUserClick", Type.EmptyTypes);
                Assert.That(method, Is.Not.Null);
                Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
                return (bool)method.Invoke(Action, null);
            }
        }

        private static object Destination(Type port, string url)
        {
            // The port does not exist in phase-one RED; emit only this trivial test double.
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UpdateDestinationFixture" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            var builder = assembly.DefineDynamicModule("fixture").DefineType("Destination", TypeAttributes.Public | TypeAttributes.Sealed);
            builder.AddInterfaceImplementation(port);
            builder.DefineDefaultConstructor(MethodAttributes.Public);
            var getter = port.GetProperty("PlayUrl");
            Assert.That(getter, Is.Not.Null);
            Assert.That(getter.PropertyType, Is.EqualTo(typeof(string)));
            var method = builder.DefineMethod("get_PlayUrl", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string), Type.EmptyTypes);
            var il = method.GetILGenerator();
            if (url == null) il.Emit(OpCodes.Ldnull); else il.Emit(OpCodes.Ldstr, url);
            il.Emit(OpCodes.Ret);
            builder.DefineMethodOverride(method, getter.GetGetMethod());
            return Activator.CreateInstance(builder.CreateType());
        }

        private static void SafeNotice(string notice)
        {
            Assert.That(notice, Is.Not.Null.And.Not.Empty);
            Assert.That(notice, Does.Match("[\uAC00-\uD7A3]"));
            Assert.That(notice, Does.Not.Contain("raw-secret"));
            Assert.That(notice, Does.Not.Contain("https://"));
            Assert.That(notice, Does.Not.Contain("fixture.update"));
        }

        [Test] public void MissingDestinationRemainsSafeOnRepeatedClicks()
        {
            var f = new Fixture(allowed: new string[0], missing: true);
            Assert.That(f.CanOpen, Is.False);
            Assert.That(f.Click(), Is.False); SafeNotice(f.Notice);
            var notice = f.Notice;
            Assert.That(f.Click(), Is.False); Assert.That(f.Notice, Is.EqualTo(notice));
            Assert.That(f.Opens, Is.Empty);
        }

        [Test] public void EmptyDestinationNeverOpens()
        {
            var f = new Fixture("");
            Assert.That(f.CanOpen, Is.False); Assert.That(f.Click(), Is.False);
            SafeNotice(f.Notice); Assert.That(f.Opens, Is.Empty);
        }

        [Test] public void ValidPlayAddressStillRequiresExactApproval()
        {
            var f = new Fixture(allowed: new[] { Approved + "x" });
            Assert.That(f.CanOpen, Is.False); Assert.That(f.Click(), Is.False);
            SafeNotice(f.Notice); Assert.That(f.Opens, Is.Empty);
        }

        [TestCase("http://play.google.com/store/apps/details?id=fixture.update.test")]
        [TestCase("https://play.google.com.evil.invalid/store/apps/details?id=fixture.update.test")]
        [TestCase("https://user:password@play.google.com/store/apps/details?id=fixture.update.test")]
        [TestCase("https://play.google.com/store/apps/details?id=fixture.update.test&redirect=https://evil.invalid")]
        [TestCase("https://play.google.com/store/apps/details?id=fixture.update.test&id=other")]
        [TestCase("https://play.google.com/store/apps/details?id=%66ixture.update.test")]
        [TestCase("https://play.google.com:444/store/apps/details?id=fixture.update.test")]
        [TestCase("https://play.google.com/store/apps/details?id=fixture.update.test#redirect")]
        [TestCase("https://play.google.com/store/apps/details?id=")]
        [TestCase("https://play.google.com/url?q=https://evil.invalid")]
        public void UnsafeAddressRejectedEvenIfExplicitlyAllowlisted(string url)
        {
            var f = new Fixture(url, new[] { url });
            Assert.That(f.CanOpen, Is.False); Assert.That(f.Click(), Is.False);
            SafeNotice(f.Notice); Assert.That(f.Opens, Is.Empty);
        }

        [Test] public void ConstructionAndRepeatedPropertyReadsNeverOpen()
        {
            var f = new Fixture();
            for (var i = 0; i < 3; i++) { Assert.That(f.CanOpen, Is.True); var ignored = f.Notice; }
            Assert.That(f.Opens, Is.Empty);
            Assert.That(f.Click(), Is.True);
            Assert.That(f.Opens, Is.EqualTo(new[] { Approved }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OpenFailureHasFixedSafeNoticeAndExplicitRetry(bool throws)
        {
            var f = new Fixture { Result = false, Throw = throws };
            Assert.That(f.Click(), Is.False); SafeNotice(f.Notice);
            var falseFailure = new Fixture { Result = false };
            Assert.That(falseFailure.Click(), Is.False);
            Assert.That(f.Notice, Is.EqualTo(falseFailure.Notice));
            Assert.That(f.CanOpen, Is.True); Assert.That(f.Opens.Count, Is.EqualTo(1));
            f.Throw = false; f.Result = true;
            Assert.That(f.Click(), Is.True); Assert.That(f.Opens.Count, Is.EqualTo(2));
            Assert.That(f.Notice, Is.Not.EqualTo(falseFailure.Notice));
        }

        [Test] public void SuccessfulOpenDoesNotSatisfyUpdateAdmission()
        {
            var gate = (ClientContentGate)typeof(ClientContentGate).GetMethod("RejectedUpdate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            var f = new Fixture();
            Assert.That(gate.Evaluate("1"), Is.EqualTo(ContentStage.UpdateRequired));
            Assert.That(f.Click(), Is.True);
            Assert.That(gate.Evaluate("1"), Is.EqualTo(ContentStage.UpdateRequired));
            Assert.That(gate.ApprovedCatalogUrl, Is.Null);
            Assert.That(f.Opens.Count, Is.EqualTo(1));
        }
    }
}
