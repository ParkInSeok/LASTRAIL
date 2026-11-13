using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugmentAcquisitionCompletionTests {
 sealed class Journal:IAugmentSelectionJournal {
  public string Value;public bool LoseComplete;public int Saves;
  public string Load(string key)=>Value;
  public void SaveNew(string key,string value){Value=value;Saves++;}
  public void Complete(string key,string expected){Assert.AreEqual(Value,expected);if(LoseComplete)throw new IOException("Completion acknowledgement unavailable");Value=null;}
 }
 sealed class Wire:IGameApiAuthenticatedHttpTransport {
  public string Reply;public int Calls;
  public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){Calls++;return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply)));}
 }
 static PropertyInfo Completion=>typeof(AugmentSelectionRuntime).GetProperty("CompletionState");
 static EventInfo CompletedEvent=>typeof(AugmentSelectionRuntime).GetEvent("SelectionCompleted");
 [TestCase(false)][TestCase(true)]
 public async Task AcquiredCompletionWaitsForOriginalJournalAndNeverClaimsApplied(bool loseComplete){
  Assert.NotNull(Completion,"Acquisition completion must differ from native application");Assert.NotNull(CompletedEvent);
  var root=new GameObject("AcquisitionOnlyHost",typeof(RectTransform),typeof(Canvas));AugmentSelectionRuntime runtime=null;
  try{
   var view=AugmentSelectionView.Create(root.transform,UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
   var original=CombatO22OriginalTests.Packet();var confirmed=(JObject)original["definition"]["selectionSource"]["selection"];
   var initial=(JObject)confirmed.DeepClone();initial["state"]="SELECTING";initial["grantRevision"]=0;initial["selectedCandidateId"]=null;
   var wire=new Wire{Reply=new JObject{["version"]="combat.augment-select.v1.20261019",["committed"]=true,["result"]=new JObject{["outcome"]="SELECTED",["snapshot"]=confirmed,["approval"]=null,["applicationOriginal"]=original}}.ToString()};
   var auth=new GameApiAuthSessionSnapshot("account","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
   var scope=new AugmentSelectionScope(new AugmentSelectionOwner("account","TEST"),"run","grant");var journal=new Journal{LoseComplete=loseComplete};string stored=null;
   var store=new CombatO22ApplicationStore("D:/Project/LASTRAIL/LocalTemp/acquisition-completion-"+Guid.NewGuid().ToString("N"),()=>stored,p=>stored=p);
   runtime=new AugmentSelectionRuntime(view,scope,wire,()=>auth,()=>true,journal,store,new string('a',64));
   int completed=0,applied=0;CompletedEvent.AddEventHandler(runtime,new Action(()=>completed++));runtime.SelectionApplied+=()=>applied++;
   await runtime.OpenAsync(AugmentSelectionSnapshot.Parse(initial.ToString()));
   Button Find(string id)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==id);
   Find("Select0").onClick.Invoke();
   Assert.NotNull(stored);Assert.IsFalse(store.Load("account","run","grant","TEST",new string('a',64)).Applied);Assert.AreEqual(0,applied);
   if(loseComplete){
    Assert.IsFalse(runtime.Completed);Assert.AreEqual(0,completed);Assert.NotNull(journal.Value);Assert.AreEqual(1,journal.Saves);
    string originalRequest=journal.Value;journal.LoseComplete=false;
    var controller=(AugmentSelectionController)typeof(AugmentSelectionRuntime).GetField("controller",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(runtime);
    await controller.RetryAsync();Assert.AreEqual(1,journal.Saves);Assert.AreEqual(2,wire.Calls);Assert.NotNull(originalRequest);
   }
   Assert.IsTrue(runtime.Completed);Assert.AreEqual("ACQUIRED_PENDING_COMBAT",Completion.GetValue(runtime));Assert.AreEqual(1,completed);Assert.AreEqual(0,applied);Assert.IsNull(journal.Value);
   runtime.DetachView();runtime.AttachView(view);Assert.AreEqual(1,completed);Assert.AreEqual(0,applied);
  }finally{runtime?.Dispose();UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public async Task AcquisitionOnlyUnsupportedCandidateDoesNotSendOrJournal(){
  Assert.NotNull(Completion);var root=new GameObject("SupportedAcquisitionHost",typeof(RectTransform),typeof(Canvas));AugmentSelectionRuntime runtime=null;
  try{
   var view=AugmentSelectionView.Create(root.transform,UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
   var original=CombatO22OriginalTests.Packet();var initial=(JObject)original["definition"]["selectionSource"]["selection"].DeepClone();initial["state"]="SELECTING";initial["grantRevision"]=0;initial["selectedCandidateId"]=null;
   var wire=new Wire();var scope=new AugmentSelectionScope(new AugmentSelectionOwner("account","TEST"),"run","grant");var journal=new Journal();
   var store=new CombatO22ApplicationStore("D:/Project/LASTRAIL/LocalTemp/acquisition-filter-"+Guid.NewGuid().ToString("N"),()=>null,p=>Assert.Fail("Unsupported acquisition"));
   runtime=new AugmentSelectionRuntime(view,scope,wire,()=>null,()=>true,journal,store,new string('a',64));
   await runtime.OpenAsync(AugmentSelectionSnapshot.Parse(initial.ToString()));
   view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Select1").onClick.Invoke();
   Assert.AreEqual(0,journal.Saves);Assert.AreEqual(0,wire.Calls);Assert.IsFalse(runtime.Completed);
  }finally{runtime?.Dispose();UnityEngine.Object.DestroyImmediate(root);}
 }
 [TestCase(false)][TestCase(true)]
 public async Task ConfirmedStoredOriginalReopensWithoutNetworkAndAppliedEventRequiresApplication(bool apply){
  var root=new GameObject("RetainedSelectionHost",typeof(RectTransform),typeof(Canvas));AugmentSelectionRuntime runtime=null;
  try{
   var view=AugmentSelectionView.Create(root.transform,UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
   var original=CombatO22OriginalTests.Packet();string stored=original.ToString();var selection=AugmentSelectionSnapshot.Parse(original["definition"]["selectionSource"]["selection"].ToString());
   var scope=new AugmentSelectionScope(new AugmentSelectionOwner("account","TEST"),"run","grant");var wire=new Wire();int applications=0,completed=0,applied=0;
   var store=new CombatO22ApplicationStore("D:/Project/LASTRAIL/LocalTemp/acquisition-reopen-"+Guid.NewGuid().ToString("N"),()=>stored,p=>Assert.Fail("Existing original must be reused"));
   var crypto=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(CombatO22StoreTests).GetNestedType("TestCrypto",BindingFlags.NonPublic),true);
   string path="D:/Project/LASTRAIL/LocalTemp/acquisition-reopen-journal-"+Guid.NewGuid().ToString("N");
   Func<string,string,AugmentCardPresentation> definitions=(v,id)=>new AugmentCardPresentation(id,"증강","설명",null,"상세");
   runtime=apply?AugmentSelectionRuntime.CreatePersistentApplied(view,scope,wire,()=>null,()=>true,crypto,path,store,new string('a',64),definitions,o=>applications++)
    :AugmentSelectionRuntime.CreatePersistent(view,scope,wire,()=>null,()=>true,crypto,path,store,new string('a',64),definitions);
   runtime.SelectionCompleted+=()=>completed++;runtime.SelectionApplied+=()=>applied++;
   await runtime.OpenAsync(selection);
   Assert.IsTrue(runtime.Completed);Assert.AreEqual(apply?"APPLIED":"ACQUIRED_PENDING_COMBAT",runtime.CompletionState);
   Assert.AreEqual(0,wire.Calls);Assert.AreEqual(1,completed);Assert.AreEqual(apply?1:0,applied);Assert.AreEqual(apply?1:0,applications);
  }finally{runtime?.Dispose();UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public async Task NoncombatScreenFinishesAcquisitionAndNextCombatUsesSameOriginalBeforePublishing(){
  var helpers=typeof(LastRail.Tests.EditMode.BNoncombatAcquisitionTests);
  var boarding=LastRail.Tests.EditMode.BBoardingEventsTests.Source(0);
  var combatRoute=LastRail.Tests.EditMode.BNativeLifestealTests.Route(boarding.OriginalSource);
  var route=(BRouteBinding)helpers.GetMethod("Augment",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{combatRoute});
  var original=(CombatO22ApplicationOriginal)helpers.GetMethod("Original",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{combatRoute});
  var confirmed=JObject.Parse(original.SelectionJson);var initial=(JObject)confirmed.DeepClone();initial["state"]="SELECTING";initial["grantRevision"]=0;initial["selectedCandidateId"]=null;
  var root=new GameObject("NoncombatAcquisitionScreen",typeof(RectTransform),typeof(Canvas));var screen=root.AddComponent<BRouteAugmentScreen>();
  using(var controller=new BRouteSessionController(route.AccountId,route.RunId)){
   try{
    await controller.RefreshAsync(()=>Task.FromResult(route));
    var auth=new GameApiAuthSessionSnapshot(route.AccountId,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
    var scope=new AugmentSelectionScope(new AugmentSelectionOwner(route.AccountId,"TEST"),route.RunId,"grant");
    var wire=new Wire{Reply=new JObject{["version"]="combat.augment-select.v1.20261019",["committed"]=true,["result"]=new JObject{["outcome"]="SELECTED",["snapshot"]=confirmed,["approval"]=null,["applicationOriginal"]=JObject.Parse(original.Capture())}}.ToString()};
    string stored=null;var store=new CombatO22ApplicationStore("D:/Project/LASTRAIL/LocalTemp/screen-acquisition-"+Guid.NewGuid().ToString("N"),()=>stored,p=>stored=p);
    var crypto=(IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(typeof(CombatO22StoreTests).GetNestedType("TestCrypto",BindingFlags.NonPublic),true);
    string journal="D:/Project/LASTRAIL/LocalTemp/screen-acquisition-journal-"+Guid.NewGuid().ToString("N");
    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
    Func<string,string,AugmentCardPresentation> definitions=(v,id)=>new AugmentCardPresentation(id,"증강","설명",null,"상세");
    await screen.OpenAsync(controller,scope,AugmentSelectionSnapshot.Parse(initial.ToString()),wire,()=>auth,()=>true,crypto,journal,store,definitions,font);
    screen.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Select0").onClick.Invoke();
    Assert.AreEqual("ACQUIRED_PENDING_COMBAT",screen.ArrivalState);Assert.IsNull(screen.View);Assert.IsNull(controller.LocalCombatOutcome);Assert.NotNull(stored);
    Assert.IsNull(new AugmentSelectionFileJournal(scope,crypto,journal).Load(scope.StorageKey));Assert.AreEqual(1,wire.Calls);
    await screen.OpenAsync(controller,scope,AugmentSelectionSnapshot.Parse(confirmed.ToString()),wire,()=>auth,()=>true,crypto,journal,store,definitions,font);
    Assert.AreEqual("ACQUIRED_PENDING_COMBAT",screen.ArrivalState);Assert.AreEqual(1,wire.Calls);
    var next=combatRoute.Capture();next["mapRevision"]=route.MapRevision+1;next.Remove("packetHash");next["packetHash"]=BDirectAttackInput.Digest(next).Substring(7);
    combatRoute=BRouteBinding.Read(next.ToString(),(string)next["packetHash"],route.AccountId,route.RunId);
    await controller.RefreshAsync(()=>Task.FromResult(combatRoute));
    var native=LastRail.Tests.EditMode.BRouteNativeTests.Binding(boarding);var geometry=LastRail.Tests.EditMode.BOrdinaryGeometryTests.Geometry(native);
    var config=CombatRailJourneyTests.Config();config["offers"]=new JArray();config["throttleMaximumBps"]=13000;config["throttleCruiseBps"]=10000;
    var motion=BTravelRuntimeConfiguration.LocalValidation(config,BDirectAttackInput.Digest(config));
    var composition=new BRouteNativeComposition(combatRoute,boarding,native,geometry,motion,10000);JObject checkpoint=null;int publications=0;
    controller.Bind(p=>{if(p["nodeSession"]["runtime"].Type!=JTokenType.Null){publications++;Assert.AreEqual(original.ApplicationId,(string)p["nodeSession"]["runtime"]["state"]["direct"]["o22Application"]["applicationId"]);}});
    screen.AttachLocalNativeWithAcquisition(composition,controller,"screen-next-combat",()=>checkpoint,(p,e)=>checkpoint=(JObject)p.DeepClone(),store,"TEST");
    Assert.AreEqual(1,publications);Assert.AreEqual(1,wire.Calls);Assert.IsFalse(store.LoadForRun(route.AccountId,route.RunId,"TEST",(string)route.Capture()["startSnapshotHash"]).Applied);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
