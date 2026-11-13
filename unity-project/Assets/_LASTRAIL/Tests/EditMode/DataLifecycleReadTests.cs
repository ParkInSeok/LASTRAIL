using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Linq;
namespace LastRail.Tests.EditMode {
 public sealed class DataLifecycleReadTests {
  sealed class Wire:IRunApiHttpTransport {
   public int Calls;public Func<Task<RunApiHttpResponse>> Reply;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Calls++;return Reply();}
  }
  sealed class Journal:IAccountActionJournal {
   public PendingAccountAction Action;public int RemoveFailure;
   public PendingAccountAction Read(string id)=>Action;
   public bool Save(PendingAccountAction a){Action=a;return true;}
   public bool Remove(PendingAccountAction a){if(RemoveFailure==1)return false;if(RemoveFailure==2)throw new System.IO.IOException();Action=null;return true;}
  }
  [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)] public async Task FormationSaveV2PublishesOnlyValidPresetPatch(int scenario){
   var account=JObject.Parse(AccountStateReadTests.Json());var formation=(JObject)account["formation"];
   formation["activationSupported"]=true;formation["revision"]=0;formation["activePresetRevision"]=null;formation["updatedAt"]=null;formation["operators"]=new JArray();
   var operators=new JArray(Enumerable.Range(0,5).Select(i=>new JObject{["slot"]=i,["operatorId"]="op-"+i}));
   var payload=new JObject{["expectedRevision"]=0,["name"]="Saved squad",["operators"]=operators,["responseVersion"]="formation.command.v2.20261021"};
   var response=new JObject{["accepted"]=true,["receiptId"]="receipt_save",["kind"]="SAVE",["presetId"]="preset_new",["presetRevision"]=1,["refreshRequired"]=false,["contractVersion"]="formation.command.v2.20261021",
    ["owners"]=new JObject{["account"]=new JObject{["kind"]="PATCH",["beforeMutationSequence"]=0,["mutationSequence"]=1,["coverage"]=new JArray("FORMATION_PRESETS"),["preset"]=new JObject{["id"]="preset_new",["name"]="Saved squad",["revision"]=1,["operators"]=operators.DeepClone(),["definitionJson"]=new JObject{["schemaVersion"]=1,["operators"]=operators.DeepClone()}.ToString(),["updatedAt"]="2026-10-21T00:00:00.000Z"}}},["invalidates"]=new JArray()};
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,account.ToString()))};var session=SupportInquiryTests.Session();
   var journal=new Journal{RemoveFailure=scenario>=3?scenario-2:0};
   using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);await owner.EnsureAccountAsync();var before=owner.Account;
   wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,response.ToString()));
   if(scenario==1){
    var pending=new TaskCompletionSource<RunApiHttpResponse>();wire.Reply=()=>pending.Task;
    var command=owner.WriteFormationAsync("formation-save","preset_new",payload.ToString());
    wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(401,"{\"accepted\":false,\"errorCode\":\"unauthorized\"}"));await owner.RefreshAccountAsync();
    Assert.AreEqual(AccountStateReadStatus.AuthRequired,owner.AccountStatus);
    pending.SetResult(RunApiHttpResponse.Success(200,response.ToString()));await command;
    Assert.IsNotNull(owner.LastFormationReceipt,"Committed receipt survives display invalidation");Assert.AreNotEqual(AccountStateReadStatus.Ready,owner.AccountStatus,"Preset-only reply must not validate the whole account");return;
   }
   if(scenario==2){response["presetRevision"]=5;response["owners"]["account"]["preset"]["revision"]=5;}
   await owner.WriteFormationAsync("formation-save","preset_new",payload.ToString());
   if(scenario==2){Assert.IsNull(owner.LastFormationReceipt,"Revision must follow the original expectedRevision");Assert.IsNotNull(owner.Pending);Assert.AreSame(before,owner.Account);return;}
   if(scenario>=3){
    Assert.IsNotNull(owner.Pending);var confirmed=owner.LastFormationReceipt;
    await owner.WriteFormationAsync("formation-save","preset_new",payload.ToString());Assert.AreEqual(2,wire.Calls,"Direct command entry must also preserve known success");
    session=SupportInquiryTests.Session();owner.SynchronizeSession();
    await owner.RetryPendingAsync();Assert.AreEqual(2,wire.Calls,"Known success with local storage failure must not send HTTP again");
    Assert.AreSame(confirmed,owner.LastFormationReceipt);
    journal.RemoveFailure=0;await owner.RetryPendingAsync();Assert.AreEqual(2,wire.Calls);Assert.IsNull(journal.Action);
   }
   Assert.IsNotNull(owner.LastFormationReceipt,owner.ActionNotice);Assert.AreEqual(2,wire.Calls,"One account read and one command only");
   Assert.AreEqual(1,owner.Account.MutationSequence);Assert.AreEqual(2,owner.Account.Presets.Count);Assert.AreEqual("Saved squad",owner.Account.Presets.Single(p=>p.Text("id")=="preset_new").Text("name"));
   Assert.AreSame(before.Currencies,owner.Account.Currencies);Assert.AreSame(before.Operators,owner.Account.Operators);Assert.IsNull(owner.Account.ActivePresetId);Assert.IsNull(owner.Pending);
  }
  [Test] public async Task ConcurrentExplicitAccountRefreshSharesOneTransmission(){
   var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>pending.Task};var session=SupportInquiryTests.Session();
   using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var first=owner.RefreshAccountAsync();var second=owner.RefreshAccountAsync();int calls=wire.Calls;
   pending.SetResult(RunApiHttpResponse.Success(200,AccountStateReadTests.Json()));await Task.WhenAll(first,second);
   Assert.AreEqual(1,calls);Assert.AreEqual(AccountStateReadStatus.Ready,owner.AccountStatus);
   await owner.RefreshAccountAsync();Assert.AreEqual(2,wire.Calls,"A later explicit refresh must still read");
  }
  [TestCase(false,"")][TestCase(true,"")][TestCase(false,"operators")][TestCase(false,"invalidates")] public async Task FormationActivationV2UsesPatchWithoutConfirmationReads(bool unchanged,string fault){
   var account=JObject.Parse(FormationContractTests.S03Json());var ops=new JArray(Enumerable.Range(0,5).Select(i=>new JObject{["slot"]=i,["operatorId"]="op-"+i}));
   account["formation"]["presets"]=new JArray(new JObject{["id"]="preset_active",["name"]="Squad",["revision"]=1,["definitionJson"]=new JObject{["schemaVersion"]=1,["operators"]=ops.DeepClone()}.ToString(),["updatedAt"]="2026-11-07T00:00:00.000Z"});
   if(unchanged){account["formation"]["activePresetId"]="preset_active";account["formation"]["activePresetRevision"]=1;account["formation"]["revision"]=1;account["formation"]["operators"]=ops.DeepClone();account["formation"]["updatedAt"]="2026-11-07T00:00:00.000Z";}
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,account.ToString()))};var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await owner.EnsureAccountAsync();
   var response=new JObject{["accepted"]=true,["receiptId"]="activate_receipt",["kind"]="ACTIVATE",["presetId"]="preset_active",["presetRevision"]=1,["formationRevision"]=1,["formationChanged"]=!unchanged,["operators"]=ops.DeepClone(),["refreshRequired"]=false,["contractVersion"]=FormationWrite.CompleteVersion,
    ["owners"]=new JObject{["account"]=new JObject{["kind"]="PATCH",["beforeMutationSequence"]=0,["mutationSequence"]=1,["coverage"]=new JArray("ACTIVE_FORMATION"),["formation"]=new JObject{["activePresetId"]="preset_active",["activePresetRevision"]=1,["revision"]=1,["operators"]=ops.DeepClone()}}},["invalidates"]=unchanged?new JArray():new JArray("ULTIMATE_SELECTION","DEPARTURE_PREVIEW")};
   if(fault=="operators")response["owners"]["account"]["formation"]["operators"][0]["operatorId"]="wrong";
   if(fault=="invalidates")response["invalidates"]=new JArray();
   wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,response.ToString()));
   await owner.WriteFormationAsync("formation-activate","preset_active",new JObject{["expectedRevision"]=1,["expectedFormationRevision"]=unchanged?1:0,["responseVersion"]=FormationWrite.CompleteVersion}.ToString());
   if(fault!=""){Assert.IsNotNull(owner.Pending);Assert.IsNull(owner.LastFormationReceipt);Assert.IsNull(owner.Account.ActivePresetId);return;}
   Assert.AreEqual(2,wire.Calls,"One initial read and one activation; no confirmation GET");Assert.IsNull(owner.Pending);Assert.AreEqual(AccountStateReadStatus.Ready,owner.AccountStatus);Assert.AreEqual("preset_active",owner.Account.ActivePresetId);Assert.AreEqual(1,owner.Account.MutationSequence);Assert.AreEqual(1,owner.Account.FormationRevision);Assert.AreEqual(Enumerable.Range(0,5).Select(i=>"op-"+i),owner.Account.ActiveOperators.OrderBy(r=>r.Number("slot")).Select(r=>r.Text("operatorId")));
  }
  [Test] public async Task ValidAccountSurvivesSameGenerationActiveTokenReplacement(){
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,AccountStateReadTests.Json()))};var session=SupportInquiryTests.Session();long generation=1;
   using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>generation);await owner.EnsureAccountAsync();var original=owner.Account;
   session=SupportInquiryTests.Session();owner.SynchronizeSession();await owner.EnsureAccountAsync();
   Assert.AreSame(original,owner.Account);Assert.AreEqual(1,wire.Calls);
   generation++;owner.SynchronizeSession();Assert.IsNull(owner.Account);await owner.EnsureAccountAsync();Assert.AreEqual(2,wire.Calls);
  }
  [Test] public async Task UnsavedFormationSelectionSurvivesCredentialReplacementOnly(){
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,AccountStateReadTests.Json()))};var session=SupportInquiryTests.Session();
   using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);await owner.EnsureAccountAsync();
   owner.Formation.Rename("My pending squad");Assert.IsTrue(owner.Formation.Select(2,"op-a"));string id=owner.Formation.PresetId;
   session=SupportInquiryTests.Session();owner.SynchronizeSession();
   Assert.AreEqual(id,owner.Formation.PresetId);Assert.AreEqual("My pending squad",owner.Formation.Name);Assert.AreEqual("op-a",owner.Formation.Selection[2]);Assert.IsTrue(owner.Formation.Dirty);
   session=SupportInquiryTests.Session("other");owner.SynchronizeSession();Assert.IsNull(owner.Formation.Selection[2]);Assert.IsFalse(owner.Formation.Dirty);
  }
  [Test] public async Task StoryEntryFiveTimesUsesOneOwnedReadAndExplicitRefreshReadsAgain(){
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")))};var session=SupportInquiryTests.Session();
   using var owner=new StoryArchiveReadModel(wire,()=>session,()=>1);
   var ensure=typeof(StoryArchiveReadModel).GetMethod("EnsureAsync");Assert.IsNotNull(ensure,"Entry must use the existing story owner");
   for(int i=0;i<5;i++)await (Task)ensure.Invoke(owner,null);
   Assert.AreEqual(1,wire.Calls);await owner.RefreshAsync();Assert.AreEqual(2,wire.Calls);
  }
  [TestCase("TrainLoadout",typeof(TrainLoadoutTests),"Snapshot")]
  [TestCase("Ultimate",typeof(UltimateSelectionTests),"Read")]
  public async Task ConfirmedSelectionSurvivesTokenRotationButNotAccountChange(string name,Type fixture,string factory){
   var method=fixture.GetMethod(factory,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   string json=method.Invoke(null,name=="TrainLoadout"?new object[]{1L}:new object[]{null,0L}).ToString();
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,json))};var session=SupportInquiryTests.Session();
   using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var ensure=typeof(AccountScreenRuntime).GetMethod("Ensure"+name+"Async");
   var state=typeof(AccountScreenRuntime).GetProperty(name);
   await (Task)ensure.Invoke(owner,null);var confirmed=state.GetValue(owner);
   session=SupportInquiryTests.Session();owner.SynchronizeSession();
   Assert.AreSame(confirmed,state.GetValue(owner),"Credential replacement must retain confirmed selection");
   await (Task)ensure.Invoke(owner,null);Assert.AreEqual(1,wire.Calls);
   session=SupportInquiryTests.Session("other");owner.SynchronizeSession();Assert.IsNull(state.GetValue(owner));
  }
  [Test] public async Task StoryConcurrentRefreshSharesOneRequest(){
   var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>pending.Task};var session=SupportInquiryTests.Session();
   using var owner=new StoryArchiveReadModel(wire,()=>session,()=>1);var first=owner.RefreshAsync();var second=owner.RefreshAsync();int count=wire.Calls;
   pending.SetResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")));await Task.WhenAll(first,second);Assert.AreEqual(1,count);
  }
  [TestCase("TrainLoadout",typeof(TrainLoadoutTests),"Snapshot")]
  [TestCase("Ultimate",typeof(UltimateSelectionTests),"Read")]
  public async Task TokenRotationDetachesOldReadAndLateReplyCannotReplaceNewSelection(string name,Type fixture,string factory){
   var method=fixture.GetMethod(factory,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   string json=method.Invoke(null,name=="TrainLoadout"?new object[]{1L}:new object[]{null,0L}).ToString();
   var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>pending.Task};var session=SupportInquiryTests.Session();
   using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var ensure=typeof(AccountScreenRuntime).GetMethod("Ensure"+name+"Async");var state=typeof(AccountScreenRuntime).GetProperty(name);
   var old=(Task)ensure.Invoke(owner,null);session=SupportInquiryTests.Session();owner.SynchronizeSession();
   wire.Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,json));await (Task)ensure.Invoke(owner,null);var current=state.GetValue(owner);
   Assert.IsNotNull(current);Assert.AreEqual(2,wire.Calls);
   pending.SetResult(RunApiHttpResponse.NetworkError("expired credential"));await old;
   Assert.AreSame(current,state.GetValue(owner));Assert.IsTrue((bool)typeof(AccountScreenRuntime).GetProperty(name+"Ready").GetValue(owner));
  }
  [TestCase("TrainLoadout",typeof(TrainLoadoutTests),"Snapshot")]
  [TestCase("Ultimate",typeof(UltimateSelectionTests),"Read")]
  public async Task FiveDisplayEntriesReuseReadButExplicitRefreshStillReads(string name,Type fixture,string factory){
   var method=fixture.GetMethod(factory,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   string json=method.Invoke(null,name=="TrainLoadout"?new object[]{1L}:new object[]{null,0L}).ToString();
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,json))};var session=SupportInquiryTests.Session();
   using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);var ensure=typeof(AccountScreenRuntime).GetMethod("Ensure"+name+"Async");Assert.IsNotNull(ensure);
   for(int i=0;i<5;i++)await (Task)ensure.Invoke(owner,null);Assert.AreEqual(1,wire.Calls);
   await (Task)typeof(AccountScreenRuntime).GetMethod("Refresh"+name+"Async").Invoke(owner,null);Assert.AreEqual(2,wire.Calls);
  }
  [Test] public async Task SuppliedLoginStoryIsSameOwnerStateWithoutAnotherRequest(){
   var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")))};var session=SupportInquiryTests.Session();
   using var login=new StoryArchiveReadModel(wire,()=>session,()=>1);await login.RefreshAsync();
   using var owner=new StoryArchiveReadModel(wire,()=>session,()=>1);owner.SupplyValidated(session,1,login.State);await owner.EnsureAsync();
   Assert.AreSame(login.State,owner.State);Assert.AreEqual(1,wire.Calls);
  }
  [Test] public async Task StoryRefreshAndFailureKeepPriorDisplayState(){
   var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,StoryArchiveReadModelTests.Reply("opn_01_last_departure")))};var session=SupportInquiryTests.Session();
   using var owner=new StoryArchiveReadModel(wire,()=>session,()=>1);await owner.RefreshAsync();var before=owner.State;wire.Reply=()=>pending.Task;
   var refresh=owner.RefreshAsync();var during=owner.State;pending.SetResult(RunApiHttpResponse.NetworkError("fixture"));await refresh;
   Assert.AreSame(before,during,"Refresh must keep the displayed records");Assert.AreSame(before,owner.State);Assert.AreEqual(StoryArchiveReadStatus.Unavailable,owner.Status);
  }
 }
}
