using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode {
 public sealed class OrdinaryNodeCompletionTests {
  sealed class Storage:IAndroidKeystoreNativeStoragePort {
   internal AndroidKeystoreEncryptedTokenPayload Saved;internal bool Writable=true;
   public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"TEST_BASE64_NOT_ENCRYPTION";
   public AndroidKeystoreEncryptedTokenPayload Encrypt(string text)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)),"test");
   public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>Encoding.UTF8.GetString(Convert.FromBase64String(p.CipherTextBase64));
   public bool Save(AndroidKeystoreEncryptedTokenPayload p){if(Writable)Saved=p;return Writable;}
   public AndroidKeystoreEncryptedTokenPayload Load()=>Saved;
   public bool Clear(){Saved=null;return true;}
  }
  sealed class Wire:IRunApiHttpTransport {
   internal readonly List<RunApiHttpRequest> Sent=new List<RunApiHttpRequest>();
   internal Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Sent.Add(r);return Reply(r);}
  }
  static OrdinaryNodeCompletion Request(string key="k",string evidence="original-evidence")=>new OrdinaryNodeCompletion("a","run",key,"node",evidence,3);
  static string Receipt(string key="k",string phase="READY")=>new JObject{["accepted"]=true,["receipt"]=new JObject{
   ["version"]="operation.map-receipt.v1",["eventId"]=key,["revision"]=4,["verificationId"]="server-verification",
   ["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray("entry:node"),["pendingEdgeId"]=null,["phase"]=phase,["holdIds"]=new JArray("original-hold")}
  }}.ToString(Newtonsoft.Json.Formatting.None);
  static DurableRunClient Client(Storage storage,Wire wire,Func<GameApiAuthSessionSnapshot> session=null,Func<long> generation=null)=>
   new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EncryptedRunOperationJournal(storage),
    session??(()=>SupportInquiryTests.Session("a")),generation??(()=>1),()=>new AppBuildSnapshot("completion-test",1));

  [TestCase("READY")][TestCase("FINAL_NODE_COMPLETE")]
  public async Task DurableResponseIsReusedAndNodeCompletionDoesNotEndRun(string phase){
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,Receipt(phase:phase)))};
   using(var client=Client(storage,wire)){
    var saved=await client.CompleteOrdinaryNodeAsync(Request());Assert.NotNull(saved);
    Assert.AreEqual(PendingRunKind.CompleteOrdinaryNode,saved.Kind);Assert.AreEqual(Request().BodyJson,wire.Sent.Single().BodyJson);
    Assert.AreEqual("/v1/runs/run/operation-map/completion",wire.Sent.Single().EndpointPath);
    Assert.IsTrue(wire.Sent.Single().Headers.Any(h=>h.Name=="Idempotency-Key"&&h.Value=="k"));
    Assert.IsTrue(wire.Sent.Single().Headers.Any(h=>h.Name=="Content-Type"&&h.Value=="application/json"));
    Assert.AreEqual(LobbyRunSelectionKind.Recovery,LobbyRunSelection.Resolve("a",client.Operations,null,null,null,null,null).Kind);
   }
   using(var reloaded=Client(storage,wire)){var saved=await reloaded.CompleteOrdinaryNodeAsync(Request());Assert.NotNull(saved);Assert.AreEqual(1,wire.Sent.Count);Assert.AreEqual(Receipt(phase:phase),saved.AcceptedResponseJson);}
  }
  [TestCase(409,"ordinary_objective_evidence_unavailable")][TestCase(503,"ordinary_completion_outcome_unknown")]
  public async Task RefusalAndUnknownOutcomeKeepExactRequestForRecovery(int status,string code){
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(status,new JObject{["accepted"]=false,["errorCode"]=code}.ToString()))};
   using(var client=Client(storage,wire)){Assert.IsNull(await client.CompleteOrdinaryNodeAsync(Request()));Assert.AreEqual(code,client.LastErrorCode);Assert.IsNull(client.Operations.Single().AcceptedResponseJson);}
   wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,Receipt()));
   using(var reopened=Client(storage,wire)){await reopened.RestoreAsync();var pending=reopened.Operations.Single();Assert.NotNull(await reopened.RetryAsync(pending));Assert.AreEqual(2,wire.Sent.Count);Assert.AreEqual(wire.Sent[0].BodyJson,wire.Sent[1].BodyJson);}
  }
  [Test] public async Task ConfirmedNodeReceiptStaysPendingUntilDurableConsumerAccepts(){
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,Receipt()))};using var client=Client(storage,wire);
   var saved=await client.CompleteOrdinaryNodeAsync(Request());Assert.NotNull(saved);
   Assert.IsFalse(await client.HandoffAsync(saved,_=>Task.FromResult(false)));Assert.AreEqual(1,client.Operations.Count);
   string consumed=null;Assert.IsTrue(await client.HandoffAsync(saved,row=>{consumed=row.AcceptedResponseJson;return Task.FromResult(true);}));
   Assert.AreEqual(Receipt(),consumed);Assert.IsEmpty(await new EncryptedRunOperationJournal(storage).ReadAsync("a"));Assert.AreEqual(1,wire.Sent.Count);
  }
  [Test] public async Task SaveBeforePostAndBufferedAckDoNotResend(){
   var storage=new Storage{Writable=false};var wire=new Wire{Reply=r=>{storage.Writable=false;return Task.FromResult(RunApiHttpResponse.Success(200,Receipt()));}};
   using var client=Client(storage,wire);Assert.IsNull(await client.CompleteOrdinaryNodeAsync(Request()));Assert.AreEqual(0,wire.Sent.Count);
   storage.Writable=true;Assert.IsNull(await client.CompleteOrdinaryNodeAsync(Request()));Assert.AreEqual(1,wire.Sent.Count);
   storage.Writable=true;Assert.NotNull(await client.RetryAsync(client.Operations.Single()));Assert.AreEqual(1,wire.Sent.Count);
  }
  [Test] public async Task ChangedBodyCannotReplaceOriginalAndAccountSwitchCannotPublish(){
   var storage=new Storage();var delayed=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>delayed.Task};
   var session=SupportInquiryTests.Session("a");long generation=1;
   using(var client=Client(storage,wire,()=>session,()=>generation)){
    var task=client.CompleteOrdinaryNodeAsync(Request());session=SupportInquiryTests.Session("other");generation++;
    delayed.SetResult(RunApiHttpResponse.Success(200,Receipt()));Assert.IsNull(await task);Assert.IsEmpty(client.Operations);
   }
   wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,Receipt()));
   using(var reopened=Client(storage,wire)){
    Assert.IsNull(await reopened.CompleteOrdinaryNodeAsync(Request(evidence:"changed")));Assert.AreEqual(1,wire.Sent.Count);
    Assert.NotNull(await reopened.CompleteOrdinaryNodeAsync(Request()));Assert.AreEqual(2,wire.Sent.Count);
   }
  }
  [TestCase("event")][TestCase("revision")][TestCase("phase")][TestCase("extra")]
  public async Task MalformedSuccessNeverConfirms(string change){
   var p=JObject.Parse(Receipt());if(change=="event")p["receipt"]["eventId"]="other";if(change=="revision")p["receipt"]["revision"]=5;if(change=="phase")p["receipt"]["state"]["phase"]="ACTIVE";if(change=="extra")p["completed"]=true;
   var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,p.ToString()))};using var client=Client(storage,wire);
   Assert.IsNull(await client.CompleteOrdinaryNodeAsync(Request()));Assert.IsNull(client.Operations.Single().AcceptedResponseJson);
  }
  [TestCase("")][TestCase("bad key")][TestCase("bad\nkey")][TestCase("한글")]
  public void InvalidKeysFailBeforePersistence(string key)=>Assert.Throws<ArgumentException>(()=>Request(key));

  static object Invoke(MethodBase method,object target,params object[] args){Assert.NotNull(method,"Missing current-attempt completion contract");try{return method is ConstructorInfo ctor?ctor.Invoke(args):((MethodInfo)method).Invoke(target,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static (OrdinaryNodeStart Node,object Entry,object Restarted) Attempts(){
   var f=global::OrdinaryNodeStartWireTests.Fixture();var node=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;
   var type=typeof(LastRail.Battle.OrdinaryFiveCombatFactory).Assembly.GetType("LastRail.Battle.OrdinaryNodeAttempt");Assert.NotNull(type,"Missing immutable current node attempt");
   var entry=Invoke(type.GetMethod("FromEntry"),null,node);var request=new OrdinaryNodeRestart(node,"restart",node.EntryMapRevision,1);
   var response=new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]="restart",["accountId"]=node.AccountId,["runId"]=node.RunId,["nodeId"]=node.NodeId,["status"]="RESTARTED",["revision"]=node.EntryMapRevision+1,["generation"]=2,["snapshot"]=node.Capture()["snapshot"],["snapshotHash"]=node.SnapshotHash};
   return(node,entry,Invoke(type.GetMethod("FromRestart"),null,request.ReadResponse(response.ToString())));
  }
  static OrdinaryNodeCompletion CurrentRequest(OrdinaryNodeStart node,object attempt,string account=null,string evidence="trusted-reference")=>
   (OrdinaryNodeCompletion)Invoke(typeof(OrdinaryNodeCompletion).GetConstructor(new[]{typeof(string),typeof(string),typeof(string),attempt.GetType(),typeof(string)}),null,account??node.AccountId,node.RunId,"current-complete",attempt,evidence);
  [Test] public void CurrentAttemptUsesRestartGenerationWithoutChangingHistoricalBody(){
   var f=Attempts();var request=CurrentRequest(f.Node,f.Restarted);var body=JObject.Parse(request.BodyJson);
   Assert.AreEqual(2,(long)body["attemptGeneration"]);Assert.AreEqual(f.Node.EntryMapRevision+1,(long)body["expectedRevision"]);Assert.AreEqual(f.Node.NodeId,(string)body["nodeId"]);Assert.AreEqual("trusted-reference",(string)body["evidenceRef"]);Assert.AreEqual(4,body.Count);
   Assert.AreEqual("{\"nodeId\":\"node\",\"evidenceRef\":\"original-evidence\",\"expectedRevision\":3}",Request().BodyJson);
   Invoke(typeof(OrdinaryNodeCompletion).GetMethod("RequireCurrentAttempt"),request,f.Restarted);
   Assert.Throws<ArgumentException>(()=>Invoke(typeof(OrdinaryNodeCompletion).GetMethod("RequireCurrentAttempt"),request,f.Entry));
   Assert.Throws<ArgumentException>(()=>CurrentRequest(f.Node,f.Restarted,account:"other"));Assert.Throws<ArgumentException>(()=>CurrentRequest(f.Node,f.Restarted,evidence:null));
  }
  [TestCase("0")][TestCase("-1")][TestCase("9007199254740992")][TestCase("null")][TestCase("1.5")][TestCase("\"2\"")]
  public void InvalidAttemptGenerationCannotEnterDurableBody(string generation){
   var body=JObject.Parse(Request().BodyJson);body["attemptGeneration"]=JToken.Parse(generation);
   Assert.Throws<ArgumentException>(()=>Invoke(typeof(OrdinaryNodeCompletion).GetMethod("ValidateBody",BindingFlags.Static|BindingFlags.NonPublic),null,body.ToString()));
  }
  [Test] public async Task RestartCompletionRetainsExactKeyAndBodyAcrossUnknownOutcome(){
   var f=Attempts();var request=CurrentRequest(f.Node,f.Restarted);var storage=new Storage();var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(503,"{\"accepted\":false,\"errorCode\":\"ordinary_completion_outcome_unknown\"}"))};
   using(var client=Client(storage,wire)){Assert.IsNull(await client.CompleteOrdinaryNodeAsync(request));Assert.AreEqual(request.BodyJson,client.Operations.Single().BodyJson);}
   var accepted=JObject.Parse(Receipt("current-complete"));accepted["receipt"]["revision"]=f.Node.EntryMapRevision+2;wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,accepted.ToString()));
   using(var client=Client(storage,wire)){await client.RestoreAsync();Assert.NotNull(await client.RetryAsync(client.Operations.Single()));Assert.AreEqual(2,wire.Sent.Count);Assert.IsTrue(wire.Sent.All(r=>r.BodyJson==request.BodyJson));Assert.IsTrue(wire.Sent.All(r=>r.Headers.Any(h=>h.Name=="Idempotency-Key"&&h.Value==request.Key)));}
  }
 }
}
