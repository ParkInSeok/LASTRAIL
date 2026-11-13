using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Narrative;
using LastRail.Resources;
using LastRail.Run;
using LastRail.Presentation.Startup;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyHydrationTests
    {
        private const string Profile = "{\"accepted\":true,\"account\":{\"id\":\"account_test\"},\"profile\":{\"displayName\":\"fixture_name\",\"commanderXp\":0}}";
        private const string Story = "{\"accepted\":true,\"actId\":\"act_01_wasteland_railway\",\"storyProgress\":{\"highestStoryProgressId\":\"none\",\"highestClearedOperationId\":null,\"order\":0},\"clearedOperationIds\":[],\"unlockedOperations\":[\"opn_01_last_departure\"],\"narrativeVersion\":\"fixture_narrative\",\"narrative\":{\"contentStatus\":\"CANDIDATE\",\"localizationStatus\":\"PENDING\",\"fixtureStatus\":\"PARTIAL\",\"missingContentStatus\":\"UNKNOWN\"},\"updatedAt\":null}";
        private const string Bundle = "{\"tokenType\":\"Bearer\",\"account\":{\"id\":\"account_test\",\"status\":\"ACTIVE\"},\"accessToken\":\"lst1.fixture_rotated\",\"refreshToken\":\"fixture_rotated\",\"accessTokenExpiresAt\":\"2030-01-01T00:00:00.000Z\",\"refreshTokenExpiresAt\":\"2030-02-01T00:00:00.000Z\"}";
        private sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            { Requests.Add(request); return Reply?.Invoke(request) ?? Task.FromResult(RunApiHttpResponse.Success(200, request.EndpointPath == "/v1/profile" ? Profile : Story)); }
        }
        private sealed class Store : IGameApiAuthTokenReader, IGameApiAuthTokenStore
        {
            private GameApiAuthSessionSnapshot session;
            public Task<GameApiAuthTokenReadResult> ReadAsync() => Task.FromResult(new GameApiAuthTokenReadResult(session == null ? GameApiAuthTokenReadOutcome.Missing : GameApiAuthTokenReadOutcome.Loaded, session));
            public Task<GameApiAuthSessionSnapshot> LoadAsync() => throw new Exception("Legacy read forbidden");
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value)
            { session = value; return Task.FromResult(GameApiAuthTokenStoreResult.Saved); }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root, Startup;
            public readonly PlayerEntryRuntime Runtime;
            public Fixture(Wire wire)
            {
                Root = new GameObject("InactiveLobbyFixture"); Root.SetActive(false);
                var bootstrap = Root.AddComponent<ResourceBootstrapController>();
                Startup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
                Set(bootstrap, "startup", Startup.GetComponent<StartupScreenRootBehaviour>());
                Set(bootstrap, "access", new DownloadAccessPolicy());
                Set(bootstrap, "entry", new AuthenticatedScreenEntry(_ => Task.CompletedTask, () => { }));
                var store = new Store();
                Runtime = new PlayerEntryRuntime(bootstrap, AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"), wire, store, store);
            }
            public void Cancel() => Root.GetComponentsInChildren<Button>(true).Single(b => b.name == "Cancel").onClick.Invoke();
            public void Dispose() { UnityEngine.Object.DestroyImmediate(Root); Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Startup); }
        }
        private static void Set(object value, string name, object field) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, field);
        private static object Get(object value, string name)
        { var property = value.GetType().GetProperty(name); Assert.That(property, Is.Not.Null, name); return property.GetValue(value); }
        private static async Task<object> Read(GameApiPlayerEntryClient client, string method)
        {
            var member = client.GetType().GetMethod(method); Assert.That(member, Is.Not.Null, "Typed lobby read is missing");
            var task = (Task)member.Invoke(client, new object[] { SupportInquiryTests.Session("account_test") }); await task;
            return task.GetType().GetProperty("Result").GetValue(task);
        }

        [Test]
        public async Task ServerNoneBaselineIsValidWithoutPromotingDiagnosticStatus()
        {
            var wire = new Wire(); var result = await Read(new GameApiPlayerEntryClient(wire), "ReadStoryProgressAsync");
            var state = (StoryProgressReadState)Get(result, "Value"); Assert.That(state, Is.Not.Null);
            Assert.That(state.HighestStoryProgressId, Is.EqualTo("none")); Assert.That(state.Order, Is.Zero);
            Assert.That(state.UnlockedOperations, Is.EquivalentTo(new[] { "opn_01_last_departure" }));
            Assert.That(state.LocalizationStatus, Is.EqualTo("PENDING"));
            Assert.That(wire.Requests.Single().BodyJson, Is.Null); Assert.That(wire.Requests.Single().EndpointPath, Is.EqualTo("/v1/story-progress"));
            Assert.That(wire.Requests.Single().Headers.Count(h => h.Name == "Authorization"), Is.EqualTo(1));
        }
        [TestCase(503, "{}", false)]
        [TestCase(401, "{\"errorCode\":\"auth_required\"}", true)]
        [TestCase(200, "{\"accepted\":true}", false)]
        public async Task FailedStoryReadCannotBecomeDefaultProgress(int status, string body, bool auth)
        {
            var wire = new Wire { Reply = _ => Task.FromResult(RunApiHttpResponse.Success(status, body)) };
            var result = await Read(new GameApiPlayerEntryClient(wire), "ReadStoryProgressAsync");
            Assert.That(Get(result, "Value"), Is.Null); Assert.That(Get(result, "RequiresAuthentication"), Is.EqualTo(auth));
        }
        [Test]
        public async Task MissingProfileIsNotAnUnnamedProfileOrAuthenticationFailure()
        {
            var wire = new Wire { Reply = _ => Task.FromResult(RunApiHttpResponse.Success(404, "{\"errorCode\":\"profile_not_found\"}")) };
            var result = await Read(new GameApiPlayerEntryClient(wire), "ReadProfileAsync");
            Assert.That(Get(result, "Value"), Is.Null); Assert.That(Get(result, "RequiresAuthentication"), Is.EqualTo(false));
            Assert.That(Get(result, "ErrorCode"), Is.EqualTo("profile_not_found"));
        }
        [Test]
        public async Task CancelledLateStoryCannotPublishLobbySnapshot()
        {
            var pending = new TaskCompletionSource<RunApiHttpResponse>();
            var wire = new Wire { Reply = request => request.EndpointPath == "/v1/story-progress" ? pending.Task : Task.FromResult(RunApiHttpResponse.Success(200, Profile)) };
            using (var fixture = new Fixture(wire))
            {
                var load = fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                Assert.That(wire.Requests.Any(r => r.EndpointPath == "/v1/story-progress"), Is.True, "Actual runtime hydration must request story truth");
                Assert.That(Get(fixture.Runtime, "LobbySnapshot"), Is.Null);
                fixture.Cancel(); pending.SetResult(RunApiHttpResponse.Success(200, Story)); await load;
                Assert.That(Get(fixture.Runtime, "LobbySnapshot"), Is.Null); Assert.That(fixture.Runtime.Owner.Ready, Is.False);
            }
        }

        [Test]
        public async Task ExternalAuthenticationClearFencesPendingStory()
        {
            var pending = new TaskCompletionSource<RunApiHttpResponse>();
            var wire = new Wire { Reply = request => request.EndpointPath == "/v1/story-progress" ? pending.Task : Task.FromResult(RunApiHttpResponse.Success(200, Profile)) };
            using (var fixture = new Fixture(wire))
            {
                var bootstrap = fixture.Root.GetComponent<ResourceBootstrapController>();
                Set(bootstrap, "authentication", fixture.Runtime);
                var load = fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                bootstrap.ClearAuthenticatedSession();
                pending.SetResult(RunApiHttpResponse.Success(200, Story)); await load;
                Assert.That(fixture.Runtime.Owner.Session, Is.Null);
                Assert.That(fixture.Runtime.LobbySnapshot, Is.Null);
                Assert.That(fixture.Runtime.Owner.Ready, Is.False);
            }
        }

        [Test]
        public async Task DisablingBoundLobbyDetachesOpenSupportPanel()
        {
            using (var fixture = new Fixture(new Wire()))
            {
                await fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                var screen = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"));
                try
                {
                    Assert.That(fixture.Runtime.BindLobby(screen), Is.True);
                    screen.GetComponentsInChildren<Button>(true).Single(b => b.name == "SupportInquiry").onClick.Invoke();
                    var panel = fixture.Root.GetComponentsInChildren<LastRail.Support.SupportInquiryPanel>(true).Single();
                    Assert.That(panel.gameObject.activeSelf, Is.True);
                    screen.SetActive(false);
                    Assert.That(panel.gameObject.activeSelf, Is.False);
                    screen.SetActive(true);
                    screen.GetComponentsInChildren<Button>(true).Single(b => b.name == "SupportInquiry").onClick.Invoke();
                    Assert.That(panel.gameObject.activeSelf, Is.True, "Reactivating the same bound lobby must restore its public action");
                }
                finally { UnityEngine.Object.DestroyImmediate(screen); }
            }
        }

        [Test]
        public async Task BoundLobbyDisplaysConfirmedProfileNameWithoutRichText()
        {
            using (var fixture = new Fixture(new Wire()))
            {
                await fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                var screen = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"));
                try
                {
                    Assert.That(fixture.Runtime.BindLobby(screen), Is.True);
                    var label = screen.GetComponentsInChildren<TMP_Text>(true).SingleOrDefault(t => t.name == "PlayerProfileName");
                    Assert.That(label, Is.Not.Null, "Confirmed profile must be bound into the actual lobby");
                    Assert.That(label.text, Is.EqualTo("fixture_name")); Assert.That(label.richText, Is.False);
                }
                finally { UnityEngine.Object.DestroyImmediate(screen); }
            }
        }
        [Test]
        public async Task HydrationAuthenticationFailureRefreshesOnlyOnce()
        {
            var wire = new Wire { Reply = request => Task.FromResult(request.EndpointPath == "/v1/auth/refresh"
                ? RunApiHttpResponse.Success(200, Bundle) : RunApiHttpResponse.Success(401, "{\"errorCode\":\"auth_required\"}")) };
            using (var fixture = new Fixture(wire))
            {
                await fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                Assert.That(wire.Requests.Count(r => r.EndpointPath == "/v1/auth/refresh"), Is.EqualTo(1));
                Assert.That(wire.Requests.Count(r => r.EndpointPath == "/v1/profile"), Is.EqualTo(2));
                Assert.That(fixture.Runtime.Owner.Ready, Is.False);
            }
        }
        [Test]
        public async Task SuccessfulHydrationBindsRealMissionShellAndSupportPublicAction()
        {
            var wire = new Wire();
            using (var fixture = new Fixture(wire))
            {
                await fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                Assert.That(fixture.Runtime.Owner.Ready, Is.True);
                var screen = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"), fixture.Root.transform);
                var bind = fixture.Runtime.GetType().GetMethod("BindLobby"); Assert.That(bind, Is.Not.Null);
                Assert.That(bind.Invoke(fixture.Runtime, new object[] { screen }), Is.EqualTo(true));
                var state = screen.GetComponent<MissionShellScreenRootBehaviour>().CurrentState;
                Assert.That(state, Is.Not.Null);
                Assert.That(state.MissionRows.Where(r => r.IsServerListedUnlockedForDisplay).Select(r => r.MissionId), Is.EquivalentTo(new[] { "opn_01_last_departure" }));
                screen.GetComponentsInChildren<Button>(true).Single(b => b.name == "SupportInquiry").onClick.Invoke();
                Assert.That(fixture.Root.GetComponentsInChildren<LastRail.Support.SupportInquiryPanel>(true).Single().gameObject.activeSelf, Is.True);
            }
        }

        [TestCase(360, 640)]
        [TestCase(1080, 1920)]
        public async Task BoundLobbyRendersWithUsableSupportControl(int width, int height)
        {
            using (var fixture = new Fixture(new Wire()))
            {
                await fixture.Runtime.Owner.LoginAsync(() => Task.FromResult(SupportInquiryTests.Session("account_test")));
                var screen = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/MissionShellScreen.prefab"));
                var cameraObject = new GameObject("LobbyRenderCamera", typeof(Camera));
                var target = new RenderTexture(width, height, 24);
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    Assert.That(fixture.Runtime.BindLobby(screen), Is.True);
                    var camera = cameraObject.GetComponent<Camera>();
                    camera.orthographic = true; camera.orthographicSize = 960; camera.aspect = width / (float)height;
                    camera.transform.position = new Vector3(0, 0, -10);
                    var canvas = screen.GetComponent<Canvas>();
                    screen.GetComponent<CanvasScaler>().enabled = false;
                    canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
                    screen.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
                    screen.transform.position = Vector3.zero;
                    Canvas.ForceUpdateCanvases();
                    var button = screen.GetComponentsInChildren<Button>(true).Single(b => b.name == "SupportInquiry");
                    var label = button.GetComponentInChildren<TMP_Text>(); label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False);
                    Assert.That(button.interactable, Is.True);
                    camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                    var pixels = texture.GetPixels32();
                    Assert.That(pixels.Count(p => p.r != pixels[0].r || p.g != pixels[0].g), Is.GreaterThan(pixels.Length / 10));
                    Directory.CreateDirectory("QA/Act1Launch/Screenshots/PlayerEntry");
                    File.WriteAllBytes("QA/Act1Launch/Screenshots/PlayerEntry/lobby_" + width + "_" + height + ".png", texture.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null;
                    target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(screen); UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            }
        }
    }
}
