using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class OperationProgressNavigationTests
    {
        sealed class Wire : IRunApiHttpTransport
        {
            public string Operation="opn_01_last_departure";
            public int Calls;
            public int StoryStatus=200;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Calls++;
                if(request.EndpointPath!="/v1/story-progress")return Task.FromResult(RunApiHttpResponse.Success(503,"{}"));
                return Task.FromResult(RunApiHttpResponse.Success(StoryStatus,StoryArchiveReadModelTests.Reply(Operation)));
            }
        }
        GameObject root;
        LobbyCommandDeck Create(AccountScreenRuntime owner)
        {
            root=new GameObject("ProgressNavigation",typeof(RectTransform),typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,1600);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);
            deck.BindAccount(owner);return deck;
        }
        Button Row(int index)=>root.GetComponentsInChildren<Button>().Single(b=>b.name=="SelectDeparture"+index);
        string Next()=>root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="NextOperation").text;
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}

        // Fails if the deck retains entry-time rows instead of observing its borrowed progress owner.
        [Test] public async Task OwnerNotificationUpdatesOpenListWithoutReopeningOrExtraReads()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.StoryArchive.EnsureAsync();var deck=Create(owner);deck.OpenOperations();
            Assert.IsTrue(Row(0).interactable);Assert.IsFalse(Row(1).interactable);
            var first=Row(0);string previous=Next();wire.Operation="opn_02_first_signal";
            await owner.StoryArchive.RefreshAsync();
            Assert.AreSame(first,Row(0),"An update must retain the open list and its input objects.");
            Assert.IsFalse(Row(0).interactable);Assert.IsTrue(Row(1).interactable);Assert.AreNotEqual(previous,Next());
            Assert.That(wire.Calls,Is.EqualTo(2),"Only the two explicit reads are allowed.");
            Capture("operation-list-current.png");deck.CloseDialog();Capture("operation-lobby-current.png");
        }
        // Fails if logout leaves account-specific operation labels or allows old buttons to navigate.
        [Test] public async Task LogoutClearsOpenListAndLobbyDisplayWithoutReading()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.StoryArchive.EnsureAsync();var deck=Create(owner);deck.OpenOperations();
            var previous=Next();session=null;owner.SynchronizeSession();
            Assert.AreNotEqual(previous,Next());
            Assert.That(root.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("SelectDeparture")).All(b=>!b.interactable));
            Assert.That(wire.Calls,Is.EqualTo(1));
        }
        // Fails if the disabled deck stays subscribed or fails to catch up on re-enable.
        [Test] public async Task DisableDetachesAndEnableUsesCurrentOwnerWithoutRead()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.StoryArchive.EnsureAsync();var deck=Create(owner);string previous=Next();
            deck.enabled=false;wire.Operation="opn_02_first_signal";await owner.StoryArchive.RefreshAsync();
            Assert.AreEqual(previous,Next());deck.enabled=true;Assert.AreNotEqual(previous,Next());
            Assert.That(wire.Calls,Is.EqualTo(2));
        }
        [Test] public async Task ProgressChangeRetainsPreparationAndDisablesStaleSelection()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.StoryArchive.EnsureAsync();var deck=Create(owner);deck.OpenOperations();Row(0).onClick.Invoke();
            var original=root.GetComponentInChildren<DeparturePreparationView>();Assert.IsNotNull(original);
            var label=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="OperationName").text;
            int calls=wire.Calls;wire.Operation="opn_02_first_signal";await owner.StoryArchive.RefreshAsync();
            Assert.AreSame(original,root.GetComponentInChildren<DeparturePreparationView>());
            Assert.AreEqual(label,root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="OperationName").text);
            Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="PreparationNotice").text,Does.Contain("잠김"));
            Assert.That(wire.Calls,Is.EqualTo(calls+1));
            Capture("operation-preparation-retained.png");
        }
        [Test] public async Task AuthenticationFailureHidesProgressButPreservesRecoveryOwner()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.StoryArchive.EnsureAsync();Create(owner);var previous=Next();
            wire.StoryStatus=401;await owner.StoryArchive.RefreshAsync();
            Assert.IsNotNull(owner.StoryArchive.State);Assert.AreNotEqual(previous,Next());Assert.That(wire.Calls,Is.EqualTo(2));
            wire.StoryStatus=503;await owner.StoryArchive.RefreshAsync();Assert.AreNotEqual(previous,Next(),"A failed authentication retry must keep recovery-only state hidden.");
            wire.StoryStatus=200;await owner.StoryArchive.RefreshAsync();Assert.AreEqual(previous,Next(),"Successful revalidation restores the display.");
        }
        [Test] public async Task UnbindingAccountCannotReuseItsOperationButtons()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.StoryArchive.EnsureAsync();var deck=Create(owner);
            wire.StoryStatus=503;await owner.StoryArchive.RefreshAsync();
            string previous=Next();deck.BindAccount(null);deck.OpenOperations();
            Assert.AreNotEqual(previous,Next());Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("SelectDeparture")&&b.interactable));
            Assert.That(wire.Calls,Is.EqualTo(2));
        }
        void Capture(string name)
        {
            if(System.Environment.GetEnvironmentVariable("LASTRAIL_PROGRESS_CAPTURE")!="1")return;
            typeof(AccountScreenIntegrationTests).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{root,name});
        }
    }
}
