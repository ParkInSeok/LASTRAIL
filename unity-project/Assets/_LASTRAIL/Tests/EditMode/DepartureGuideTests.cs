using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.MissionShell;
using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Tests.EditMode;

public sealed class DepartureGuideTests
{
    sealed class Wire:IRunApiHttpTransport
    {
        public int Writes;
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){if(r.Method!="GET")Writes++;if(r.EndpointPath=="/v1/story-progress")return Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")));return Task.FromResult(RunApiHttpResponse.Success(r.EndpointPath=="/v1/account/state"?200:404,r.EndpointPath=="/v1/account/state"?AccountStateReadTests.Json():"{}"));}
    }
    sealed class Journal:IAccountActionJournal{public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>throw new InvalidOperationException("Guide journal write");public bool Remove(PendingAccountAction a)=>throw new InvalidOperationException("Guide journal remove");}
    [Test] public async Task ActualDepartureEntryAndNativeNavigationLeaveWireAndJournalUnchanged()
    {
        var wire=new Wire();var session=SupportInquiryTests.Session();
        using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await state.RefreshAccountAsync();await state.StoryArchive.EnsureAsync();
        var root=new GameObject("GuideEntryTest",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        try{
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new[]{new MissionShellMissionRowState("opn_01_last_departure","QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(state);deck.OpenOperations();
            root.GetComponentsInChildren<Button>().Single(b=>b.name=="SelectDeparture0").onClick.Invoke();
            Assert.IsNotNull(root.GetComponentsInChildren<MonoBehaviour>().SingleOrDefault(x=>x.GetType().Name=="DeparturePreparationView"),"Actual operation selection opens the full preparation view.");
            root.GetComponentsInChildren<Button>().Single(b=>b.name=="DepartureFormation").onClick.Invoke();
            Assert.IsTrue(deck.Pages.IsOpen);Assert.AreEqual(LobbyPage.Formation,deck.Pages.CurrentPage);
            deck.Pages.Close();
            Assert.IsNotNull(root.GetComponentsInChildren<MonoBehaviour>().SingleOrDefault(x=>x.GetType().Name=="DeparturePreparationView"),"Closing formation returns to the same preparation route.");
            Assert.IsFalse(root.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmDeparture").interactable,"Missing train/quote does not authorize start.");
            root.GetComponentsInChildren<Button>().Single(b=>b.name=="DepartureGuideEntry").onClick.Invoke();
            var view=root.GetComponentInChildren<DepartureGuideView>();Assert.IsNotNull(view);
            Assert.IsNull(view.transform.Find("GuideOverview"),"Actual guide must retain the preparation screen rather than duplicate a summary screen.");
            Assert.IsNotNull(view.transform.Find("GuideCallout"),"Actual guide needs a bounded callout.");
            var focusProperty=typeof(DepartureGuideView).GetProperty("FocusTarget");Assert.IsNotNull(focusProperty);
            Assert.AreSame(root.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="FormationPanel"),focusProperty.GetValue(view));
            Assert.AreEqual(0,root.GetComponentsInChildren<RawImage>().Count(x=>
                (x.name.StartsWith("PreparationOperator")||x.name.StartsWith("OperatorPortrait"))&&x.enabled&&x.texture!=null),
                "Unassigned account must not invent portraits; guide materials and junction geometry are not portraits.");
            for(int i=0;i<3;i++){
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="GuideNext").onClick.Invoke();
                if(i<2)Assert.That(view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="GuideBody").text,Does.Contain("확인이 필요"),"Missing reads cannot assert a saved zero count.");
            }
            Assert.AreSame(root.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="FuelPanel"),focusProperty.GetValue(view));
            view.GetComponentsInChildren<Button>().Single(b=>b.name=="GuideClose").onClick.Invoke();Assert.IsNull(root.GetComponentInChildren<DepartureGuideView>());
            root.GetComponentsInChildren<Button>().Single(b=>b.name=="DepartureGuideEntry").onClick.Invoke();view=root.GetComponentInChildren<DepartureGuideView>();session=null;state.SynchronizeSession();view.ValidateOwner();Assert.IsTrue(view==null);Assert.AreEqual(0,wire.Writes);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void NativeFourStepsUseActualCountAndCloseWithoutMutationCallbacks()
    {
        var root=new GameObject("GuideTest",typeof(RectTransform),typeof(Canvas));
        ((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
        try {
            bool valid=true;
            var data=new DepartureGuideData("작전 QA",new[]{"opr_test_a","opr_test_b"},new[]{"1 · car_a","2 · car_b"},new[]{"지원 사격","방어막"},"출발 비용 확인 필요");
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=DepartureGuideView.Open(root.transform,font,null,data,()=>valid);
            Button Find(string name)=>view.GetComponentsInChildren<Button>().Single(x=>x.name==name);
            Assert.AreEqual(2,view.transform.GetComponentsInChildren<RawImage>().Count(x=>x.name.StartsWith("OperatorPortrait")));
            Assert.IsFalse(Find("GuidePrevious").interactable);
            for(int i=1;i<4;i++){Find("GuideNext").onClick.Invoke();Assert.AreEqual(i,view.Step);}
            Find("GuidePrevious").onClick.Invoke();Assert.AreEqual(2,view.Step);
            Find("GuideNext").onClick.Invoke();Find("GuideNext").onClick.Invoke();Assert.IsTrue(view==null||!view.gameObject.activeSelf);
            view=DepartureGuideView.Open(root.transform,font,null,data,()=>valid);
            valid=false;view.ValidateOwner();Assert.IsTrue(view==null||!view.gameObject.activeSelf);
        } finally {UnityEngine.Object.DestroyImmediate(root);}
    }
}
