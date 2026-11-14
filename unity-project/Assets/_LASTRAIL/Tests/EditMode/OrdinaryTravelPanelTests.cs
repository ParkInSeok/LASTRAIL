using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;

namespace LastRail.Tests.EditMode {
 public sealed class OrdinaryTravelPanelTests {
  [Test] public void RepeatedChoicesReuseRowsAndDispatchCurrentEdgeOnly(){
   var root=new GameObject("ChoiceReuse");try{var view=OrdinaryTravelPanel.Create(root.transform,null);string selected=null;view.Bind(id=>selected=id,null,null,null,null,null,null);
    view.ShowChoices(new[]{("old","Old")},true,"");var original=view.GetComponentsInChildren<Button>().Single(b=>b.name=="Destination0");
    view.ShowChoices(new[]{("new","New")},true,"");Assert.AreSame(original,view.GetComponentsInChildren<Button>().Single(b=>b.name=="Destination0"));original.onClick.Invoke();Assert.AreEqual("new",selected);
    selected=null;view.ShowUnavailable("Waiting");original.onClick.Invoke();Assert.IsNull(selected);view.ShowChoices(new[]{("third","Third")},true,"");Assert.AreSame(original,view.GetComponentsInChildren<Button>().Single(b=>b.name=="Destination0"));view.Unbind();original.onClick.Invoke();Assert.IsNull(selected);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  sealed class TimerWire:IRunApiHttpTransport {
   public readonly List<RunApiHttpRequest> Arrivals=new List<RunApiHttpRequest>();
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){
    if((string)JObject.Parse(request.BodyJson)["kind"]=="ARRIVE"){
     Arrivals.Add(request);return Task.FromResult(RunApiHttpResponse.Success(503,"{\"errorCode\":\"outcome_unknown\"}"));
    }
    var reply=(RunApiHttpResponse)typeof(OrdinaryTravelPreparationTests).GetMethod("ScopedResponse",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{request});
    if(request.EndpointPath.EndsWith("/operation-map/destination")){
     var body=JObject.Parse(reply.BodyJson);body["travelInitial"]["nodeId"]="node-b";reply=RunApiHttpResponse.Success(200,body.ToString());
    }
    return Task.FromResult(reply);
   }
  }
  sealed class TimerHarness:IDisposable {
   public readonly TimerWire Wire=new TimerWire();
   public DurableRunClient Client;public AccountScreenRuntime Account;public OrdinaryTravelOwner Owner;
   public OrdinaryTravelScreenController Screen;public OrdinaryTravelPanel Panel;public GameObject Root;
   public int CombatEntries;
   public static async Task<TimerHarness> Create(){
    var h=new TimerHarness();var session=SupportInquiryTests.Session("test-account");
    var storage=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(OrdinaryTravelPreparationTests).GetNestedType("Storage",BindingFlags.NonPublic),true);
    h.Client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(h.Wire),new EncryptedRunOperationJournal(storage),()=>session,()=>1,()=>new AppBuildSnapshot("timer-test",1));
    h.Account=new AccountScreenRuntime(h.Wire,null,()=>session,()=>1){OriginalRunId=()=>"test-run"};
    h.Account.SynchronizeSession();h.Account.BindRunRecovery(h.Client);h.Owner=h.Account.ReadOrdinaryTravel("test-run");
    await h.Owner.ConfirmDestinationAsync(new BOperationDestinationIntent("test-account","test-run","destination-1",2,"edge-ab"),"node-b");
    h.Root=new GameObject("RealTravelDeadline");
    h.Screen=new OrdinaryTravelScreenController(h.Account,"test-account","test-run",h.Root.transform,null,()=>true,()=>{},()=>{h.CombatEntries++;return Task.CompletedTask;});
    h.Panel=h.Root.GetComponentInChildren<OrdinaryTravelPanel>();
    var fixture=typeof(BArrivalBoundaryTravelTests);
    var source=(JObject)fixture.GetMethod("Source",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    var binding=(JObject)fixture.GetMethod("Binding",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    // Supply the existing validated display fixture; exercise the real controller/owner/wire below.
    typeof(OrdinaryTravelScreenController).GetField("plan",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(h.Screen,OrdinaryTravelDisplayPlan.Read(source,binding));
    return h;
   }
   public void Show(){
    typeof(OrdinaryTravelPanel).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Panel,new object[]{false});
    typeof(OrdinaryTravelPanel).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Panel,new object[]{true});
    if(Owner.Latest.State.Paused)Click("Resume");
   }
   public void Click(string name)=>Panel.GetComponentsInChildren<Button>(true).Single(b=>b.name==name).onClick.Invoke();
   public void Dispose(){Screen?.Dispose();Account?.Dispose();Client?.Dispose();if(Root!=null)UnityEngine.Object.Destroy(Root);}
  }
  [UnityTest] public IEnumerator VisibleDeadlineSendsOneArrivalAndUnknownResultKeepsOriginalKeyWithoutRetry(){
   yield return new EnterPlayMode();var creation=TimerHarness.Create();while(!creation.IsCompleted)yield return null;
   using(var h=creation.GetAwaiter().GetResult()){
    yield return null;h.Show();yield return new WaitForSecondsRealtime(.8f);
    Assert.That(h.Wire.Arrivals.Count,Is.EqualTo(1));Assert.That(h.Owner.NeedsRecovery,Is.True);
    var pending=h.Client.Operations.Single(o=>o.Kind==PendingRunKind.TravelCommand);
    Assert.That(pending.AcceptedResponseJson,Is.Null);
    Assert.That(pending.IdempotencyKey,Is.EqualTo((string)JObject.Parse(h.Wire.Arrivals[0].BodyJson)["requestId"]));
    string body=pending.BodyJson;yield return new WaitForSecondsRealtime(.65f);
    Assert.That(h.Wire.Arrivals.Count,Is.EqualTo(1));Assert.That(h.Client.Operations.Single(o=>o.Kind==PendingRunKind.TravelCommand).BodyJson,Is.EqualTo(body));
    Assert.That(h.CombatEntries,Is.Zero);
   }
   yield return null;
  }
  [UnityTest] public IEnumerator HiddenDeadlineDoesNotArriveAndReenableSchedulesOnce(){
   yield return new EnterPlayMode();var creation=TimerHarness.Create();while(!creation.IsCompleted)yield return null;
   using(var h=creation.GetAwaiter().GetResult()){
    yield return null;h.Show();h.Panel.gameObject.SetActive(false);yield return new WaitForSecondsRealtime(.65f);
    Assert.That(h.Wire.Arrivals.Count,Is.Zero);
    h.Panel.gameObject.SetActive(true);yield return new WaitForSecondsRealtime(.2f);
    Assert.That(h.Wire.Arrivals.Count,Is.EqualTo(1));
   }
   yield return null;
  }
  [UnityTest] public IEnumerator DisposedDeadlineNeverSendsArrival(){
   yield return new EnterPlayMode();var creation=TimerHarness.Create();while(!creation.IsCompleted)yield return null;
   using(var h=creation.GetAwaiter().GetResult()){
    yield return null;h.Show();h.Screen.Dispose();yield return new WaitForSecondsRealtime(.65f);
    Assert.That(h.Wire.Arrivals.Count,Is.Zero);
   }
   yield return null;
  }
  [UnityTest] public IEnumerator PausedDeadlineStaysSilentAndResumeThrottleReceiptSchedulesOneArrival(){
   yield return new EnterPlayMode();var creation=TimerHarness.Create();while(!creation.IsCompleted)yield return null;
   using(var h=creation.GetAwaiter().GetResult()){
    yield return null;h.Show();h.Click("Pause");yield return new WaitForSecondsRealtime(.65f);
    Assert.That(h.Wire.Arrivals.Count,Is.Zero);h.Click("Resume");
    var nozzle=h.Panel.GetComponentInChildren<OrdinaryTravelNozzle>();nozzle.value=10000;nozzle.OnPointerUp(new PointerEventData(null));
    Assert.That(h.Owner.Latest.State.ThrottleBps,Is.EqualTo(10000));yield return new WaitForSecondsRealtime(.65f);
    Assert.That(h.Wire.Arrivals.Count,Is.EqualTo(1));
   }
   yield return null;
  }
  [Test] public void TravelPanelHasNoAlwaysRunningFrameCallback(){
   Assert.That(typeof(OrdinaryTravelPanel).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic),Is.Null,"Travel deadlines belong to the controller, including cancellation when hidden.");
  }
  [UnityTearDown] public IEnumerator LeaveRuntime(){if(Application.isPlaying)yield return new ExitPlayMode();}
  [UnityTest] public IEnumerator HiddenTravelCancelsItsVisibleWorkAndUnbindRemovesOnlyOwnedCallbacks(){
   yield return new EnterPlayMode();
   var root=new GameObject("travel-visibility");
   try{
    var panel=OrdinaryTravelPanel.Create(root.transform,null);int shown=0,hidden=0;
    var focus=typeof(OrdinaryTravelPanel).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic);
    var pause=typeof(OrdinaryTravelPanel).GetMethod("OnApplicationPause",BindingFlags.Instance|BindingFlags.NonPublic);
    focus.Invoke(panel,new object[]{true});
    panel.BindVisibility(visible=>{if(visible)shown++;else hidden++;});
    Assert.That(shown,Is.EqualTo(1),"Binding supplies the current visibility immediately.");
    panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);
    Assert.That(hidden,Is.EqualTo(1));Assert.That(shown,Is.EqualTo(2));
    pause.Invoke(panel,new object[]{true});focus.Invoke(panel,new object[]{true});
    panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);
    Assert.That(shown,Is.EqualTo(2),"Focus or reenable while suspended must not restart visible work.");
    pause.Invoke(panel,new object[]{false});Assert.That(shown,Is.EqualTo(3));
    int beforeHidden=hidden;
    panel.Unbind();panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);
    Assert.That(hidden,Is.EqualTo(beforeHidden));Assert.That(shown,Is.EqualTo(3));
   }finally{UnityEngine.Object.Destroy(root);}
   yield return null;
  }
  [Test] public void EveryTravelButtonHasItsVisibleLabel(){
   var root=new GameObject("label-test");
   try{
    var panel=OrdinaryTravelPanel.Create(root.transform,null);
    panel.ShowChoices(new[]{("a","목적지 A")},true,"선택");
    foreach(var button in panel.GetComponentsInChildren<Button>(true))Assert.IsNotEmpty(button.GetComponentInChildren<TMP_Text>(true).text,button.name);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public void ArrivalDisplayUsesPinnedStepBoundaryAndNeverAdvancesPausedState(){
   var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.OrdinaryTravelDisplayPlan");Assert.NotNull(type);
   var fixture=typeof(BArrivalBoundaryTravelTests);
   var source=fixture.GetMethod("Source",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   var binding=fixture.GetMethod("Binding",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   var plan=type.GetMethod("Read").Invoke(null,new[]{source,binding});
   Assert.AreEqual(.3m,type.GetMethod("SecondsUntilBoundary").Invoke(plan,new object[]{0L,0L,10000}));
   Assert.AreEqual(.5m,type.GetMethod("SecondsUntilBoundary").Invoke(plan,new object[]{0L,0L,5000}));
   Assert.Throws<TargetInvocationException>(()=>type.GetMethod("SecondsUntilBoundary").Invoke(plan,new object[]{0L,0L,1}));
  }
  [Test] public void DisplayAndDragDoNotSubmitButReleaseSubmitsOnceAndUnbindStopsInput(){
   var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.OrdinaryTravelPanel");
   Assert.NotNull(type,"The product travel view must exist.");
   var root=new GameObject("test-root");
   try{
    var panel=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,null});int calls=0,value=0;
    type.GetMethod("Bind").Invoke(panel,new object[]{(Action<string>)(_=>{}),(Action<int>)(v=>{calls++;value=v;}),(Action)(()=>{}),(Action)(()=>{}),(Action)(()=>{}),(Action)(()=>{}),(Action)(()=>{})});
    type.GetMethod("ShowTravel").Invoke(panel,new object[]{"목적지","이동 중",5000,1,13000,false,false});
    var slider=panel.GetComponentInChildren<Slider>();Assert.NotNull(slider);Assert.AreEqual(0,calls);
    slider.value=7000;Assert.AreEqual(0,calls);
    slider.GetType().GetMethod("OnPointerUp").Invoke(slider,new object[]{new PointerEventData(null)});
    Assert.AreEqual(1,calls);Assert.AreEqual(7000,value);
    type.GetMethod("Unbind").Invoke(panel,null);slider.value=9000;
    slider.GetType().GetMethod("OnPointerUp").Invoke(slider,new object[]{new PointerEventData(null)});Assert.AreEqual(1,calls);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
