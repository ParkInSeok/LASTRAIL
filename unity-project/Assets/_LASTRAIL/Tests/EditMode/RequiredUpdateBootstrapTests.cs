using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Presentation.Startup;
using LastRail.Resources;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class RequiredUpdateBootstrapTests
    {
        private sealed class Destination : ITrustedUpdateDestination
        { public string PlayUrl => "https://play.google.com/store/apps/details?id=fixture.update.test"; }
        private sealed class Backend : IContentStartupBackend
        {
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken c) => Task.FromResult(new ClientContentGate { MinimumVersion = "99.0" });
            public Task InitializeAsync(CancellationToken c) => throw new Exception("Update gate bypass");
            public Task UpdateCatalogAsync(CancellationToken c) => throw new Exception("Update gate bypass");
            public Task<long> RequiredBytesAsync(CancellationToken c) => throw new Exception("Update gate bypass");
            public Task DownloadAsync(Action<long, long> progress, CancellationToken c) => throw new Exception("Update gate bypass");
            public Task VerifyRequiredAssetsAsync(CancellationToken c) => throw new Exception("Update gate bypass");
        }
        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public async Task PublicUpdateClickShowsNoticeAndCanRetryWithoutAdmission(bool configured, bool opened, bool throws)
        {
            var root = new GameObject("InactiveUpdateFixture"); root.SetActive(false);
            var controller = root.AddComponent<ResourceBootstrapController>();
            var startupObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            try
            {
                var startup = startupObject.GetComponent<StartupScreenRootBehaviour>();
                var coordinator = new DownloadCoordinator(new Backend(), new DownloadAccessPolicy());
                await coordinator.StartAsync("1.0", CancellationToken.None);
                Set(controller, "coordinator", coordinator); Set(controller, "startup", startup);
                var opens = 0;
                if (configured)
                    controller.ConfigureRequiredUpdateAction(new RequiredUpdateAction(new Destination(),
                        new[] { new Destination().PlayUrl }, _ => { opens++; if (throws) throw new Exception("synthetic-private-opener"); return opened; }));
                Assert.That(opens, Is.Zero);
                startup.Render(StartupScreenViewState.UpdateNotice("", "initial"));
                var primary = typeof(ResourceBootstrapController).GetMethod("Primary", BindingFlags.Instance | BindingFlags.NonPublic);
                startup.PrimaryActionRequested += () => primary.Invoke(controller, null);
                var button = (Button)typeof(StartupScreenRootBehaviour).GetField("primaryButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(startup);
                button.onClick.Invoke();
                if (!configured) Assert.That(startup.CurrentState.Details, Does.Contain("\uC124\uC815"));
                Assert.That(startup.CurrentState.Details, Does.Not.Contain("initial"));
                Assert.That(startup.CurrentState.Details, Does.Not.Contain("synthetic-private-opener"));
                Assert.That(button.interactable, Is.True, "A missing/failed market action must release the pending UI latch.");
                button.onClick.Invoke();
                Assert.That(opens, Is.EqualTo(configured ? 2 : 0));
                Assert.That(coordinator.ConfigAccepted, Is.False); Assert.That(coordinator.CanEnterGame, Is.False);
                Assert.That(coordinator.Stage, Is.EqualTo(ContentStage.UpdateRequired));
            }
            finally { UnityEngine.Object.DestroyImmediate(startupObject); UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void ExplicitActionCompositionExistsAndDoesNotOpenOnConfiguration()
        {
            var actionType = typeof(ResourceBootstrapController).Assembly.GetType("LastRail.Resources.RequiredUpdateAction");
            Assert.That(actionType, Is.Not.Null);
            var configure = typeof(ResourceBootstrapController).GetMethod("ConfigureRequiredUpdateAction", new[] { actionType });
            Assert.That(configure, Is.Not.Null);
            var opens = 0;
            var action = Activator.CreateInstance(actionType, new object[] { null, Array.Empty<string>(), (Func<string, bool>)(_ => { opens++; return true; }) });
            var root = new GameObject("InactiveUpdateConfiguration"); root.SetActive(false);
            try
            {
                var controller = root.AddComponent<ResourceBootstrapController>(); configure.Invoke(controller, new[] { action });
                Assert.That(opens, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
