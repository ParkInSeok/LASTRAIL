using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryNodePreparationOwnerTests {
 sealed class Storage:IAndroidKeystoreNativeStoragePort {
  public AndroidKeystoreEncryptedTokenPayload Value;
  public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"TEST_ONLY";
  public AndroidKeystoreEncryptedTokenPayload Encrypt(string value)=>new AndroidKeystoreEncryptedTokenPayload(value,"test");
  public string Decrypt(AndroidKeystoreEncryptedTokenPayload value)=>value.CipherTextBase64;
  public bool Save(AndroidKeystoreEncryptedTokenPayload value){Value=value;return true;}
  public AndroidKeystoreEncryptedTokenPayload Load()=>Value;
  public bool Clear()=>throw new AssertionException("Do not erase restart recovery.");
 }
 sealed class Wire:IGameApiAuthenticatedHttpTransport {
  public readonly System.Collections.Generic.List<RunApiHttpRequest> Requests=new System.Collections.Generic.List<RunApiHttpRequest>();
  public Func<RunApiHttpRequest,Task<GameApiAuthenticatedHttpResult>> Reply;
  public Task<GameApiAuthenticatedHttpResult> SendAsync(RunApiHttpRequest request,GameApiAccessToken token){Requests.Add(request);return Reply(request);}
 }
 sealed class Fixture:IDisposable {
  public readonly Storage Disk;public readonly Wire Transport=new Wire();public readonly BRouteCommandResult Arrival;public readonly OrdinaryFiveStartInput Start;
  public object Owner;public bool Current=true;
  public Fixture(bool recovered=false,Storage disk=null){
   Disk=disk??new Storage();
   var method=typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic);
   var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))method.Invoke(null,null);Start=f.Start;Arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
   Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,Response(r))));
   var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.OrdinaryNodePreparationOwner");Assert.NotNull(type,"Missing public node preparation owner.");
   Func<OrdinaryNodeStart,Func<OrdinaryNodeRestartScope>,OrdinaryNodeRestartDelivery> delivery=(node,scope)=>new OrdinaryNodeRestartDelivery(node,Disk,Transport,scope);
   Owner=Activator.CreateInstance(type,Start,Arrival,170000L,recovered,(Func<bool>)(()=>Current),(Func<GameApiAccessToken>)(()=>new GameApiAccessToken("lst1.test.signature")),delivery);
  }
  public string Response(RunApiHttpRequest r){var q=JObject.Parse(r.BodyJson);var n=Arrival.NodeStart;return new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=r.Headers.Single(h=>h.Name=="Idempotency-Key").Value,["accountId"]=n.AccountId,["runId"]=n.RunId,["nodeId"]=n.NodeId,["status"]="RESTARTED",["revision"]=(long)q["expectedRevision"]+1,["generation"]=(long)q["expectedGeneration"]+1,["snapshot"]=n.Capture()["snapshot"],["snapshotHash"]=n.SnapshotHash}.ToString();}
  public Task<CombatSceneDriver> Prepare()=>(Task<CombatSceneDriver>)Owner.GetType().GetMethod("PrepareAsync").Invoke(Owner,null);
  public void Retire()=>Owner.GetType().GetMethod("Retire").Invoke(Owner,null);
  public void Dispose(){if(Owner is IDisposable d)d.Dispose();}
 }
 [Test] public async Task FreshArrivalReusesPreparationWithoutInitializationOrRestart(){using var f=new Fixture();var driver=await f.Prepare();Assert.AreSame(driver,await f.Prepare());var reuse=f.Owner.GetType().GetMethod("ReusePrepared");Assert.AreSame(driver,await (Task<CombatSceneDriver>)reuse.Invoke(f.Owner,new object[]{f.Start,null}));Assert.IsNull(reuse.Invoke(f.Owner,new object[]{f.Start,500000L}));Assert.Zero(f.Transport.Requests.Count);Assert.AreEqual(800,(int)driver.HudState["trainHp"]);Assert.IsFalse((bool)driver.HudState["fullCombatReady"]);var before=driver.StateJson;driver.Advance(100000,1);Assert.AreEqual(before,driver.StateJson);}
 [Test] public async Task RetiredAttemptRestartsFromOriginalNodeAndRejectsOldDriver(){using var f=new Fixture();var old=await f.Prepare();old.SetUltimateAuto(false);f.Retire();Assert.Throws<InvalidOperationException>(()=>old.SetSpeed(3));var next=await f.Prepare();Assert.AreNotSame(old,next);Assert.IsTrue((bool)next.HudState["ultimateAuto"]);Assert.AreEqual(1,f.Transport.Requests.Count);f.Retire();await f.Prepare();Assert.AreEqual(2,f.Transport.Requests.Count);Assert.AreEqual(2,(long)JObject.Parse(f.Transport.Requests[1].BodyJson)["expectedGeneration"]);}
 [Test] public async Task LostRestartReplyReusesOriginalKeyBeforePublishing(){using var f=new Fixture(true);f.Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.NetworkError("lost"));Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await f.Prepare());Assert.NotNull(f.Disk.Value);f.Transport.Reply=r=>Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,f.Response(r))));var driver=await f.Prepare();Assert.NotNull(driver);Assert.AreEqual(2,f.Transport.Requests.Count);Assert.AreEqual(f.Transport.Requests[0].BodyJson,f.Transport.Requests[1].BodyJson);}
 [Test] public async Task FailedUnstartedTransactionRecreatesDriverWithoutRestart(){using var f=new Fixture();var failed=await f.Prepare();failed.SetUltimateAuto(false);Assert.Throws<ArgumentOutOfRangeException>(()=>failed.SetSpeed(9));Assert.IsTrue(failed.RecoveryRequired);var next=await f.Prepare();Assert.AreNotSame(failed,next);Assert.IsTrue(failed.IsRetired);Assert.IsFalse(next.RecoveryRequired);Assert.IsTrue((bool)next.HudState["ultimateAuto"]);Assert.AreEqual(800,(int)next.HudState["trainHp"]);Assert.AreEqual(0,f.Transport.Requests.Count);}
 [Test] public async Task LeavingWhileRestartIsInFlightRetainsReceiptButNeverPublishesOldDriver(){using var f=new Fixture(true);var gate=new TaskCompletionSource<GameApiAuthenticatedHttpResult>();f.Transport.Reply=r=>gate.Task;var pending=f.Prepare();f.Retire();gate.SetResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,f.Response(f.Transport.Requests.Single()))));Assert.ThrowsAsync<InvalidOperationException>(async()=>await pending);Assert.NotNull(f.Disk.Value);await Task.CompletedTask;}
 [Test] public async Task FreshProcessCannotTreatCachedRestartReceiptAsCurrentAttempt(){var disk=new Storage();using(var f=new Fixture(true,disk)){await f.Prepare();}using var reopened=new Fixture(true,disk);Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await reopened.Prepare());Assert.Zero(reopened.Transport.Requests.Count);Assert.NotNull(disk.Value);}
 [Test] public void ConfirmedResultNeverCreatesAnotherBattle(){using var f=new Fixture(true);f.Transport.Reply=r=>{var n=f.Arrival.NodeStart;return Task.FromResult(GameApiAuthenticatedHttpResult.Succeeded(RunApiHttpResponse.Success(200,new JObject{["accepted"]=true,["version"]="ordinary.node-restart.v1.20261028",["requestId"]=r.Headers.Single(h=>h.Name=="Idempotency-Key").Value,["accountId"]=n.AccountId,["runId"]=n.RunId,["nodeId"]=n.NodeId,["status"]="RESULT_AVAILABLE",["revision"]=n.EntryMapRevision,["generation"]=JValue.CreateNull(),["runStatus"]="ACTIVE",["state"]=new JObject{["version"]="operation.map-progress.v1",["traversedEdgeIds"]=new JArray(),["pendingEdgeId"]=JValue.CreateNull(),["phase"]="READY",["holdIds"]=new JArray()}}.ToString())));};var error=Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await f.Prepare());Assert.AreEqual(OrdinaryPreparationFailure.NodeResultAvailable,error.Failure);Assert.IsFalse(error.RunResultAvailable,"An ACTIVE node result is not a finished run.");Assert.ThrowsAsync<OrdinaryNodeStartUnavailableException>(async()=>await f.Prepare());Assert.AreEqual(1,f.Transport.Requests.Count);}
}
