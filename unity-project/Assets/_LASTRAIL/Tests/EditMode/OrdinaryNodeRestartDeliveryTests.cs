using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodeRestartDeliveryTests {
 sealed class Storage:IAndroidKeystoreNativeStoragePort {
  public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"test";
  public AndroidKeystoreEncryptedTokenPayload Value;public bool Fail;public int Saves;
  public AndroidKeystoreEncryptedTokenPayload Encrypt(string s)=>new AndroidKeystoreEncryptedTokenPayload(s,"test");
  public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>p.CipherTextBase64;
  public AndroidKeystoreEncryptedTokenPayload Load()=>Value;
  public bool Save(AndroidKeystoreEncryptedTokenPayload p){Saves++;if(Fail)return false;Value=p;return true;}
  public bool Clear(){throw new AssertionException("No UI cancellation may delete the durable request.");}
 }
 sealed class Transport:IGameApiAuthenticatedHttpTransport {
  public int Calls;public Func<RunApiHttpRequest,Task<GameApiAuthenticatedHttpResult>> Send;
  public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest r,GameApiAccessToken token){Calls++;return Send(r);}
 }
 static OrdinaryNodeStart Start(){var f=OrdinaryNodeStartWireTests.Fixture();return BRouteCommandResult.Read(200,f.Response.ToString(),f.Command).NodeStart;}
 static readonly GameApiAccessToken Token=new GameApiAccessToken("lst1.test.signature");
 static string Reply(OrdinaryNodeStart s,OrdinaryNodeRestart r)=>new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=r.Key,["accountId"]=s.AccountId,["runId"]=s.RunId,["nodeId"]=s.NodeId,["status"]="RESTARTED",["revision"]=r.ExpectedRevision+1,["generation"]=r.ExpectedGeneration+1,["snapshot"]=s.Capture()["snapshot"],["snapshotHash"]=s.SnapshotHash}.ToString();
 static OrdinaryNodeRestartScope Scope(OrdinaryNodeStart s,long? revision=null,long? generation=null)=>new OrdinaryNodeRestartScope(s.AccountId,s.RunId,s.NodeId,revision??s.EntryMapRevision,generation??s.Generation);
 [Test] public async Task LostResponseReconstructionUsesPersistedKeyAndExactBody(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"original",s.EntryMapRevision,1);var disk=new Storage();var wire=new Transport();
  wire.Send=q=>{Assert.NotNull(disk.Value);Assert.AreEqual(r.BodyJson,q.BodyJson);Assert.AreEqual("application/json",q.Headers.Single(h=>h.Name=="Content-Type").Value);Assert.AreEqual(r.Key,q.Headers.Single(h=>h.Name=="Idempotency-Key").Value);return Task.FromResult(GameApiAuthenticatedHttpResult.NetworkError("lost"));};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));
  Assert.IsNull(await owner.DeliverAsync(r,Token));
  wire.Send=q=>{Assert.AreEqual(r.BodyJson,q.BodyJson);return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))));};
  var rebuilt=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));var result=await rebuilt.RecoverAsync(Token);
  Assert.AreEqual(2,result.Generation);Assert.IsFalse(result.Start.CanStart);Assert.AreEqual(2,wire.Calls);Assert.AreEqual(2,disk.Saves);
  Assert.AreEqual(2,(await rebuilt.RecoverAsync(Token)).Generation);Assert.AreEqual(2,wire.Calls);
 }
 [Test] public void FailedPreSendSaveNeverCallsHttp(){
  var s=Start();var disk=new Storage{Fail=true};var wire=new Transport();var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.DeliverAsync(new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1),Token));Assert.Zero(wire.Calls);
 }
 [Test] public async Task FailedConfirmationSaveDoesNotPublishAndRetainsOriginalRequest(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var wire=new Transport();
  wire.Send=q=>{disk.Fail=true;return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))));};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.DeliverAsync(r,Token));
  disk.Fail=false;wire.Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))));
  Assert.NotNull(await owner.RecoverAsync(Token));Assert.AreEqual(2,wire.Calls);
 }
 [Test] public async Task CancellationAfterSendStillPersistsReceiptForAssetRetry(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var wire=new Transport();using var cancel=new CancellationTokenSource();
  wire.Send=q=>{cancel.Cancel();return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))));};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));Assert.CatchAsync<OperationCanceledException>(async()=>await owner.DeliverAsync(r,Token,cancel.Token));
  Assert.NotNull(await new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s)).RecoverAsync(Token));Assert.AreEqual(1,wire.Calls);
 }
 [Test] public async Task KnownInvalidationSurvivesReconstructionAndNeverReturnsCachedPermission(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var wire=new Transport{Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))))};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));await owner.DeliverAsync(r,Token);await owner.InvalidateAsync();
  var rebuilt=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));Assert.ThrowsAsync<InvalidOperationException>(async()=>await rebuilt.RecoverAsync(Token));Assert.AreEqual(1,wire.Calls);
 }
 [Test] public async Task ChangedScopeAfterHttpRetainsReceiptButDoesNotPublish(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var scope=Scope(s);var wire=new Transport();
  wire.Send=q=>{scope=new OrdinaryNodeRestartScope("other",s.RunId,s.NodeId,s.EntryMapRevision,1);return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))));};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>scope);Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.DeliverAsync(r,Token));Assert.AreEqual(2,disk.Saves);
  scope=Scope(s);Assert.NotNull(await owner.RecoverAsync(Token));Assert.AreEqual(1,wire.Calls);
 }
 [Test] public async Task UnresolvedRequestCannotBeReplacedWithNewKey(){
  var s=Start();var disk=new Storage();var wire=new Transport{Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.NetworkError("lost"))};var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));
  await owner.DeliverAsync(new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1),Token);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.DeliverAsync(new OrdinaryNodeRestart(s,"different",s.EntryMapRevision,1),Token));Assert.AreEqual(1,wire.Calls);
 }
 [Test] public async Task RepeatedCallWhileSendingSharesDurableResultWithoutSecondHttp(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var pending=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();var wire=new Transport{Send=q=>pending.Task};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));var first=owner.DeliverAsync(r,Token);var second=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s)).RecoverAsync(Token);
  Assert.AreEqual(1,wire.Calls);pending.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))));
  Assert.NotNull(await first);Assert.NotNull(await second);Assert.AreEqual(1,wire.Calls);
 }
 [Test] public async Task ExplicitRetirementAllowsOnlyNewKeyForExactAcceptedSuccessor(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var scope=Scope(s);var wire=new Transport{Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Reply(s,r))))};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>scope);await owner.DeliverAsync(r,Token);await owner.InvalidateAsync();scope=Scope(s,s.EntryMapRevision+1,2);
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.DeliverAsync(new OrdinaryNodeRestart(s,"key",scope.Revision,scope.Generation),Token));
  r=new OrdinaryNodeRestart(s,"next-key",scope.Revision,scope.Generation);Assert.AreEqual(3,(await owner.DeliverAsync(r,Token)).Generation);Assert.AreEqual(2,wire.Calls);
 }
 [Test] public void InvalidResponseIsNotPersistedAsAccepted(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var reply=JObject.Parse(Reply(s,r));reply["generation"]=99;
  var wire=new Transport{Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,reply.ToString())))};var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));
  Assert.ThrowsAsync<ArgumentException>(async()=>await owner.DeliverAsync(r,Token));Assert.AreEqual(1,disk.Saves);Assert.AreEqual(JTokenType.Null,JObject.Parse(disk.Value.CipherTextBase64)["responseJson"].Type);
 }
 [Test] public async Task SupersededServerResponseDurablyDisablesFurtherRecovery(){
  var s=Start();var disk=new Storage();var wire=new Transport{Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(409,"{\"errorCode\":\"ordinary_node_restart_superseded\"}")))};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s));Assert.IsNull(await owner.DeliverAsync(new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1),Token));
  Assert.ThrowsAsync<InvalidOperationException>(async()=>await new OrdinaryNodeRestartDelivery(s,disk,wire,()=>Scope(s)).RecoverAsync(Token));Assert.AreEqual(1,wire.Calls);
 }
 [Test] public async Task RetainedTerminalResultAllowsItsAppliedRevisionButNeverAnUnrelatedLaterScope(){
  var s=Start();var r=new OrdinaryNodeRestart(s,"key",s.EntryMapRevision,1);var disk=new Storage();var scope=Scope(s);var reply=JObject.Parse(Reply(s,r));
  reply["status"]="RESULT_AVAILABLE";reply["generation"]=null;reply.Remove("snapshot");reply.Remove("snapshotHash");reply["runStatus"]="ACTIVE";
  reply["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray(),["pendingEdgeId"]=null,["phase"]="READY",["holdIds"]=new JArray()};
  var wire=new Transport{Send=q=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,reply.ToString())))};
  var owner=new OrdinaryNodeRestartDelivery(s,disk,wire,()=>scope);Assert.IsTrue((await owner.DeliverAsync(r,Token)).ResultAvailable);
  scope=Scope(s,(long)reply["revision"],0);var retained=await owner.RecoverAsync(Token);Assert.IsTrue(retained.ResultAvailable);Assert.IsNull(retained.Start);Assert.AreEqual(1,wire.Calls);
  scope=Scope(s,(long)reply["revision"]+1,0);Assert.ThrowsAsync<InvalidOperationException>(async()=>await owner.RecoverAsync(Token));Assert.AreEqual(1,wire.Calls);
 }
}
