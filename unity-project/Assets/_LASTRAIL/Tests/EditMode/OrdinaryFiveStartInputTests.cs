using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Auth;
using LastRail.Run;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class OrdinaryFiveStartInputTests {
 sealed class NoNetwork:IRunApiHttpTransport {public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)=>throw new AssertionException("Input reuse must not send HTTP");}
 static AccountScreenRuntime Runtime(){var session=new GameApiAuthSessionSnapshot("account","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));return new AccountScreenRuntime(new NoNetwork(),null,()=>session,()=>1);}
 static DepartureStartResult Lookup(JObject start){start["operationPreviewId"]="preview";return new DepartureStartResult(200,new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]="run",["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null}.ToString(),"preview");}
 static RunLifecycleSnapshot Lifecycle(JObject start){var data=(JObject)typeof(LastRail.Tests.EditMode.RunLifecycleTests).GetMethod("Data",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"NOT_AVAILABLE"});data["runId"]="run";start["balanceVersion"]="balance";data["startResponse"]=start;return new RunLifecycleSnapshot(data.ToString(),"run");}
 static void Put(AccountScreenRuntime state,string property,object value)=>typeof(AccountScreenRuntime).GetProperty(property).SetValue(state,value);
 sealed class DelayedRunJournal:IRunOperationJournal {
  internal readonly TaskCompletionSource<System.Collections.Generic.IReadOnlyList<PendingRunOperation>> Read=new TaskCompletionSource<System.Collections.Generic.IReadOnlyList<PendingRunOperation>>();internal int Puts;
  public Task<System.Collections.Generic.IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>Read.Task;
  public Task<bool> PutAsync(PendingRunOperation op){Puts++;return Task.FromResult(false);}
  public Task<bool> ConfirmAsync(PendingRunOperation op,string json)=>throw new AssertionException("No confirmation expected");
  public Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key)=>throw new AssertionException("No deletion expected");
 }
 [Test] public async Task SameRunBecomingTerminalDuringRestoreDoesNotInitialize(){
  using var state=Runtime();state.SynchronizeSession();Put(state,"DepartureStartResult",Lookup(Fixture()));
  var journal=new DelayedRunJournal();var session=LastRail.Tests.EditMode.SupportInquiryTests.Session("account");using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(new NoNetwork()),journal,()=>session,()=>1);state.BindRunRecovery(client);
  var pending=state.InitializeOrdinaryCombatAsync("run");var start=Fixture();start["operationPreviewId"]="preview";
  Put(state,"DepartureStartResult",new DepartureStartResult(200,new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]="run",["runStatus"]="COMPLETED",["startResponse"]=start,["errorCode"]=null}.ToString(),"preview"));
  journal.Read.SetResult(Array.Empty<PendingRunOperation>());Assert.IsNull(await pending);Assert.AreEqual(LobbyRunSelectionKind.Terminal,state.RunSelection.Kind);Assert.AreEqual(0,journal.Puts);
 }
 [Test] public void ProductInputRejectsConflictingRunSelectionWithoutHttp(){using var state=Runtime();state.SynchronizeSession();Put(state,"DepartureStartResult",Lookup(Fixture()));state.OriginalRunId=()=>"different";Assert.Throws<InvalidOperationException>(()=>state.ReadOrdinaryCombatInput("run"));}
 [Test] public void ProductInputRejectsConflictingSameRunOriginsWithoutHttp(){using var state=Runtime();state.SynchronizeSession();Put(state,"DepartureStartResult",Lookup(Fixture()));var changed=Fixture();changed["ordinaryStatSource"]["origin"]["actors"][0]["permanentStats"]["maxHp"]=2000;var hash=Hash(changed["ordinaryStatSource"]["origin"]);changed["ordinaryStatSource"]["originHash"]=hash;changed["ordinaryKitSource"]["packet"]["originHash"]=hash;changed["ordinaryKitSource"]["kitHash"]=Hash(changed["ordinaryKitSource"]["packet"]);Put(state,"Lifecycle",Lifecycle(changed));Assert.Throws<InvalidOperationException>(()=>state.ReadOrdinaryCombatInput("run"));}
 [Test] public void ProductInputReusesSameOriginalAcrossLookupAndLifecycleWithoutHttp(){using var state=Runtime();state.SynchronizeSession();Put(state,"DepartureStartResult",Lookup(Fixture()));Put(state,"Lifecycle",Lifecycle(Fixture()));var first=state.ReadOrdinaryCombatInput("run");Assert.AreSame(first,state.ReadOrdinaryCombatInput("run"));}
 static string Hash(JToken x){JToken Sort(JToken t)=>t is JObject o?new JObject(o.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Sort(p.Value)))):t is JArray a?new JArray(a.Select(Sort)):t.DeepClone();using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Sort(x).ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();}
 // Synthetic identity/stats, exact published ability definitions; not an admitted product run.
 public static JObject Fixture(){
  var definition=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-definition-intake-20261001.json"))["definition"];
  var actors=new JArray(((JArray)definition["operators"]).Select((o,i)=>new JObject{["operatorId"]=o["operatorId"].DeepClone(),["slot"]=4-i,["permanentStats"]=new JObject{["maxHp"]=1000+i,["attack"]=new JObject{["numerator"]="100",["denominator"]="1"},["defense"]=50,["basicIntervalMs"]=1500},["skillMagnitude"]=new JObject{["numerator"]="1",["denominator"]="1"}}));
  var origin=new JObject{["version"]="combat.ordinary-five-core-stats.v1.20261023",["accountId"]="account",["runId"]="run",["policy"]=definition["policy"].DeepClone(),["ownedSourceHash"]="sha256:"+new string('a',64),["definitionVersion"]="combat.owned-base.2026-10-01.1",["definitionHash"]="sha256:"+new string('b',64),["formationRevision"]=3,["loadoutRevision"]=4,["applicability"]=new JObject(),["actors"]=actors};
  var authority=new JObject{["version"]="combat.authority-split.v1",["mode"]="ORDINARY_SOLO",["combatOwner"]="UNITY_LOCAL",["combatClock"]="LOCAL_LOGICAL",["transactionOwner"]="SERVER",["resultVerificationOwner"]="SERVER",["clientReportProvesCombat"]=false};
  var ops=new JArray(((JArray)definition["operators"]).Select((o,i)=>{var row=(JObject)o.DeepClone();row["slot"]=4-i;return row;}).Reverse());
  var packet=new JObject{["version"]="combat.ordinary-five-kit.v1.20261023",["accountId"]="account",["runId"]="run",["originHash"]=Hash(origin),["policyVersion"]=CombatDefinitionIntake.DefinitionVersion,["policyHash"]=Hash(definition["policy"]),["authority"]=authority.DeepClone(),["execution"]="REQUIRES_MATCHING_UNITY_HANDLERS",["operators"]=ops,["requiredEffectKinds"]=definition["requiredEffectKinds"].DeepClone()};
  return new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]="run",["ordinaryStatSource"]=new JObject{["origin"]=origin,["originHash"]=Hash(origin)},["ordinaryKitSource"]=new JObject{["packet"]=packet,["kitHash"]=Hash(packet)},["admission"]=new JObject{["version"]="departure-v4",["formationRevision"]=3,["loadoutRevision"]=4,["combatPolicyRequirements"]=definition["policy"].DeepClone(),["combatAuthority"]=authority,["formation"]=new JObject{["operators"]=new JArray(actors.Select(a=>new JObject{["operatorId"]=a["operatorId"].DeepClone(),["slot"]=a["slot"].DeepClone()}))}}};
 }
 static object Read(JObject start,string account="account",string run="run"){
  var type=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.OrdinaryFiveStartInput");Assert.NotNull(type,"Missing original START consumer");
  try{return type.GetMethod("Read").Invoke(null,new object[]{start,account,run});}catch(TargetInvocationException e){throw e.InnerException;}
 }
 public static JObject WithKyleTauntV2(JObject originalStart){
  var start=(JObject)originalStart.DeepClone();
  var packet=(JObject)start["ordinaryKitSource"]["packet"];
  var active=(JObject)packet["operators"].Single(o=>(string)o["operatorId"]=="op_kyle")["abilities"].Single(a=>(string)a["id"]=="op_kyle.active");
  active["maximumFrontTargets"]=3;active["tauntPolicyVersion"]="kyle-taunt.v2.20261109";active["tauntRangeMm"]=12000;
  active["tauntLineOfSightRequired"]=true;active["tauntScope"]="CURRENT_CARRIAGE_THREATS";active["tauntTargetOrder"]="DISTANCE_THEN_ORIGINAL_ORDER_THEN_ID";
  start["ordinaryKitSource"]["kitHash"]=Hash(packet);return start;
 }
 [Test] public void KyleTauntRevisionAcceptsOnlyExactApprovedDefinitionAndRetainsLegacy(){
  var original=Fixture();
  var legacy=(OrdinaryFiveStartInput)Read(original);Assert.AreEqual(2,(int)legacy.Actors.Single(a=>a.OperatorId=="op_kyle").Ability("op_kyle.active")["maximumFrontTargets"]);
  var start=WithKyleTauntV2(original);var before=start.DeepClone();
  var current=(OrdinaryFiveStartInput)Read(start);Assert.AreEqual(3,(int)current.Actors.Single(a=>a.OperatorId=="op_kyle").Ability("op_kyle.active")["maximumFrontTargets"]);
  Assert.IsTrue(JToken.DeepEquals(before,start));Assert.IsTrue(JToken.DeepEquals(original,Fixture()));
  var packet=(JObject)start["ordinaryKitSource"]["packet"];
  var active=(JObject)packet["operators"].Single(o=>(string)o["operatorId"]=="op_kyle")["abilities"].Single(a=>(string)a["id"]=="op_kyle.active");
  active["tauntRangeMm"]=12001;start["ordinaryKitSource"]["kitHash"]=Hash(packet);Assert.Throws<ArgumentException>(()=>Read(start));
 }
 [Test] public void ExistingLookupReusesTypedInputAndKeepsConfirmedReceiptOnInvalidKit(){
  var x=Fixture();x["operationPreviewId"]="preview";var envelope=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]="run",["runStatus"]="ACTIVE",["startResponse"]=x,["errorCode"]=null};
  var result=new DepartureStartResult(200,envelope.ToString(),"preview");var first=result.ReadOrdinaryInput("account");Assert.AreSame(first,result.ReadOrdinaryInput("account"));Assert.Throws<ArgumentException>(()=>result.ReadOrdinaryInput("other"));
  result.StartResponse["ordinaryKitSource"]["kitHash"]="changed";Assert.AreSame(first,result.ReadOrdinaryInput("account"));
  x["ordinaryKitSource"]["kitHash"]="changed";var invalid=new DepartureStartResult(200,envelope.ToString(),"preview");Assert.Throws<ArgumentException>(()=>invalid.ReadOrdinaryInput("account"));Assert.AreEqual("COMMITTED",invalid.Status);Assert.NotNull(invalid.StartResponse);
 }
 [Test] public void LifecycleReadsOriginalWithoutPromotingInitializationAndRejectsTerminal(){
  var method=typeof(LastRail.Tests.EditMode.RunLifecycleTests).GetMethod("Data",BindingFlags.NonPublic|BindingFlags.Static);
  var data=(JObject)method.Invoke(null,new object[]{"NOT_AVAILABLE"});data["runId"]="run";var start=Fixture();start["balanceVersion"]="balance";data["startResponse"]=start;
  var lifecycle=new RunLifecycleSnapshot(data.ToString(),"run");Assert.AreSame(lifecycle.ReadOrdinaryInput("account"),lifecycle.ReadOrdinaryInput("account"));Assert.IsFalse(lifecycle.NewAttemptAuthorized);Assert.IsNull(lifecycle.OrdinaryInitialization);
  data["runStatus"]="QUARANTINED";data["nextAction"]="REVIEW_REQUIRED";data["disposition"]="REVIEW_REQUIRED";Assert.Throws<InvalidOperationException>(()=>new RunLifecycleSnapshot(data.ToString(),"run").ReadOrdinaryInput("account"));
 }
 [Test] public void ExactFiveSlotsAreReadWithoutMutatingOriginal(){var x=Fixture();var before=x.DeepClone();var result=(OrdinaryFiveStartInput)Read(x);Assert.AreEqual("account",result.AccountId);CollectionAssert.AreEqual(new[]{"op_mira","op_sera","op_eve","op_reina","op_kyle"},result.Actors.Select(a=>a.OperatorId));Assert.IsTrue(JToken.DeepEquals(before,x));var ability=result.Actors[0].Ability("op_mira.active");ability["shots"]=99;Assert.AreEqual(6,(int)result.Actors[0].Ability("op_mira.active")["shots"]);x["ordinaryStatSource"]["origin"]["actors"][4]["permanentStats"]["maxHp"]=1;Assert.AreEqual(1004,result.Actors[0].PermanentMaxHp);}
 [TestCase("account")][TestCase("run")][TestCase("slot")][TestCase("ability")][TestCase("origin")][TestCase("authority")][TestCase("missing")]
 public void MismatchedOrRehashedUnsupportedInputRejects(string change){var x=Fixture();if(change=="account"){Assert.Throws<ArgumentException>(()=>Read(x,"other"));return;}if(change=="run"){Assert.Throws<ArgumentException>(()=>Read(x,"account","other"));return;}
  var packet=(JObject)x["ordinaryKitSource"]["packet"];
  if(change=="slot")packet["operators"][0]["slot"]=1;
  if(change=="ability")packet["operators"][0]["abilities"][0]["cooldownMs"]=1;
  if(change=="origin")x["ordinaryStatSource"]["origin"]["actors"][0]["permanentStats"]["maxHp"]=2;
  if(change=="authority")packet["authority"]["combatOwner"]="SERVER";
  if(change=="missing")((JObject)x).Remove("ordinaryKitSource");else x["ordinaryKitSource"]["kitHash"]=Hash(packet);
  Assert.Throws<ArgumentException>(()=>Read(x));
 }
}
