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
    public sealed class StoryArchiveNavigationTests
    {
        [Test] public async Task LazyStoryReaderClearsOnLogoutBeforeOtherAccountReads()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var state=new AccountScreenRuntime(wire,null,()=>session,()=>0);
            await state.StoryArchive.RefreshAsync();Assert.IsNotNull(state.StoryArchive.State);
            session=null;state.SynchronizeSession();Assert.IsNull(state.StoryArchive.State);
        }
        sealed class Wire:IRunApiHttpTransport
        {
            public int Calls;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {Calls++;Assert.That(request.Method,Is.EqualTo("GET"));Assert.That(request.EndpointPath,Is.EqualTo("/v1/story-progress"));return Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")));}
        }
        [Test] public void ReplacingAccountRuntimeClosesPreviousArchiveBeforeNewReads()
        {
            var session=SupportInquiryTests.Session();var wireA=new Wire();var wireB=new Wire();
            using var a=new AccountScreenRuntime(wireA,null,()=>session,()=>0);using var b=new AccountScreenRuntime(wireB,null,()=>session,()=>1);
            var root=new GameObject("ArchiveRebind",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(a);deck.OpenStoryArchive();
                Assert.IsTrue(root.GetComponentInChildren<StoryArchiveView>().CurrentState.CanOpen);
                deck.BindAccount(b);Assert.IsNull(root.GetComponentInChildren<StoryArchiveView>(),"Replacing the account must close the old reader's screen.");
                deck.OpenStoryArchive();Assert.That(wireA.Calls,Is.EqualTo(1));Assert.That(wireB.Calls,Is.EqualTo(1));deck.CloseDialog();
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void LobbyMenuOpensSevenRowsTextAndReturnsWithOwnedSubscriptionReleased()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            var root=new GameObject("ArchiveNavigation",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1600);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);
                void Click(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name).onClick.Invoke();
                Click("LobbyMenu");Click("OpenStoryArchive");
                var view=root.GetComponentInChildren<StoryArchiveView>();Assert.IsNotNull(view);Assert.That(view.CurrentState.Rows.Count,Is.EqualTo(7));
                Click("StoryRow1");Assert.IsFalse(root.GetComponentsInChildren<Button>().Single(b=>b.name=="OpenStoryRecord").interactable);
                Click("StoryRow0");Click("OpenStoryRecord");Assert.IsTrue(view.CurrentState.IsReading);
                var current=view.CurrentState;
                view.Render(new StoryArchiveViewState(current.Rows,current.SelectedIndex,current.Status,current.NarrativeVersion,string.Join("\n",Enumerable.Repeat("긴 기록 표시 검수 문장입니다.",100))+"\n마지막 줄",true));
                Canvas.ForceUpdateCanvases();var scroll=view.GetComponentInChildren<ScrollRect>();Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
                UnityEngine.EventSystems.ExecuteEvents.Execute(scroll.gameObject,new UnityEngine.EventSystems.PointerEventData(null){scrollDelta=new Vector2(0,-100000)},UnityEngine.EventSystems.ExecuteEvents.scrollHandler);
                Assert.Less(scroll.verticalNormalizedPosition,.01f);float scrollPosition=scroll.content.anchoredPosition.y;deck.EvaluateLayout();Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(scrollPosition).Within(.1f));
                var reading=view.CurrentState;view.Render(new StoryArchiveViewState(reading.Rows,reading.SelectedIndex,LastRail.Narrative.StoryArchiveReadStatus.Loading,reading.NarrativeVersion,reading.DetailText,true));
                Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(scrollPosition).Within(.1f),"Refresh retains the current reading position");
                Click("BackStoryArchive");Assert.IsFalse(view.CurrentState.IsReading);Click("BackStoryArchive");Assert.IsNull(root.GetComponentInChildren<StoryArchiveView>());
                Click("LobbyMenu");Click("OpenStoryArchive");Assert.That(wire.Calls,Is.EqualTo(1));
                var before=state.StoryArchive;session=null;state.SynchronizeSession();Assert.IsFalse(root.GetComponentInChildren<StoryArchiveView>().CurrentState.CanOpen);
                root.SetActive(false);Assert.DoesNotThrow(()=>state.SynchronizeSession());Assert.AreSame(before,state.StoryArchive);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
