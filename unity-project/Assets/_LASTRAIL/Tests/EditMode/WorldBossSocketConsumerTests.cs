#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.Networking;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public class WorldBossSocketConsumerTests {
  static JObject bundle;
  [OneTimeSetUp] public void IntakeOnlyFromPrivateChildEnvironment() {
   var raw=Environment.GetEnvironmentVariable("LASTRAIL_WORLD_SOCKET_PRIVATE");
   Environment.SetEnvironmentVariable("LASTRAIL_WORLD_SOCKET_PRIVATE",null);
   if(string.IsNullOrEmpty(raw))throw new InvalidOperationException("Private supervisor required");
   using var reader=new Newtonsoft.Json.JsonTextReader(new StringReader(raw)){DateParseHandling=Newtonsoft.Json.DateParseHandling.None};
   bundle=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
  }
  [OneTimeTearDown] public void ReleaseIntakeReferences(){bundle=null;}
  static JObject Intake(string key)=>(JObject)bundle[key].DeepClone();
  static WorldBossSocketTransport Transport(string key)=>new WorldBossSocketTransport(new WorldBossSocketPrivateIntake(Intake(key)));
  static WorldBossAuthorityRequest ReadRequest()=>new WorldBossAuthorityRequest("trial","LOCAL_GEO");
  static JObject FrozenRead(int i=0)=>(JObject)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/WorldBossRail-20261002/AuthorityConsumer/hono-traces.json"))["traces"][i]["response"];
  static JObject Definition()=>(JObject)FrozenRead()["journal"]["rail"]["definition"];
  static JObject Profile()=>(JObject)FrozenRead()["journal"]["geometry"]["profile"];
  static WorldBossAuthorityLocalClient Client()=>new WorldBossAuthorityLocalClient(FrozenRead(),Definition(),Profile(),"trial","a","health-run","train");
  static IEnumerator Wait(Task task){while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted,"Credential-free HTTP exchange must complete");Assert.IsFalse(task.IsCanceled);}
  static void Same(JToken a,JToken b)=>Assert.IsTrue(JToken.DeepEquals(a,b),"Original state must remain exact");
  [TestCase("origin")][TestCase("account")][TestCase("expiry")][TestCase("version")][TestCase("secret")][TestCase("extra")][TestCase("numeric")]
  public void PrivateBoundaryRejectsUntrustedInputs(string field) {
   var x=Intake("train");
   switch(field){
    case "origin":x["origin"]="http://localhost:8080";break;
    case "account":x["accountId"]="";break;
    case "expiry":x["accessTokenExpiresAt"]="2000-01-01T00:00:00.000Z";break;
    case "version":x["headers"]["X-World-Rail-Version"]="old";break;
    case "secret":x["headers"]["Authorization"]="Bearer bad";break;
    case "extra":x["savedToken"]="forbidden";break;
    case "numeric":x["accountId"]=123;break;
   }
   bool rejected=false;try{using var ignored=new WorldBossSocketPrivateIntake(x);}catch(ArgumentException){rejected=true;}
   Assert.IsTrue(rejected,"Malformed private input must be rejected without printing credentials");
  }
  [UnityTest] public IEnumerator RealTcpRolesAndReplacedRevokedRestartedTokens() {
   foreach(var pair in new[]{("train",200),("operator",200),("outsider",403),("oldTrain",401),("revokedOperator",401)}) {
    using var t=Transport(pair.Item1);var task=t.SendAsync(ReadRequest());yield return Wait(task);
    Assert.AreEqual(pair.Item2,task.Result.Status,"Role/session boundary");
    Assert.AreEqual(0,t.InFlightCount);
   }
  }
  [UnityTest] public IEnumerator ExistingCallbackLive503PreservesOriginalSourceAndHp() {
   using var c=Client();using var t=Transport("train");int changes=0;
   using var connection=new WorldBossSocketConnection(c,t,()=>changes++);
   c.RequestRead();yield return Wait(connection.LastExchange);var before=c.State.Capture();
   c.PreviewRoute("p1r1","RIGHT");var original=c.ConfirmPreview("live-no-producer");
   yield return Wait(connection.LastExchange);
   Assert.AreEqual(503,c.LastError.HttpStatus);Assert.AreEqual("rail_journal_sample_clock_required",c.LastError.Code);
   Same(original.Capture(),c.OriginalForRetry().Capture());Same(before,c.State.Capture());
   c.RequestRead();yield return Wait(connection.LastExchange);Same(before,c.State.Capture());
   Assert.Greater(changes,0);Assert.AreEqual(0,t.InFlightCount);
  }
  [UnityTest] public IEnumerator MemoryRestoredOriginalReceiptReadReconcileAndReplayAreExact() {
   using var c=Client();using var t=Transport("restored");
   using(var connection=new WorldBossSocketConnection(c,t)){
    c.PreviewRoute("p1r1","RIGHT");var request=c.ConfirmPreview("route");yield return Wait(connection.LastExchange);
    var ack=c.OriginalAcknowledgement;Assert.IsNotNull(ack);Assert.AreEqual(1,c.State.JournalRevision);
    c.RequestRead();yield return Wait(connection.LastExchange);
    Assert.AreEqual(2,c.State.JournalRevision);Assert.IsNull(c.PendingRequest);Assert.AreEqual(1000,c.State.Hp);
    var replay=t.SendAsync(request);yield return Wait(replay);Same(ack,replay.Result.Body);
    c.ReplayOriginal("route");yield return Wait(connection.LastExchange);
    Same(request.Capture(),c.ReplayOriginal("route").Capture());yield return Wait(connection.LastExchange);
   }
   using var restored=WorldBossAuthorityLocalClient.RestoreLocal(c.CaptureLocal(),Definition(),Profile(),"trial","a","health-run","train");
   Assert.IsTrue(restored.AwaitingRead);
   using var transport=Transport("restored");using var reconnection=new WorldBossSocketConnection(restored,transport);
   restored.RequestRead();yield return Wait(reconnection.LastExchange);
   Same(c.State.Capture(),restored.State.Capture());Assert.AreEqual(1000,restored.State.Hp);
  }
  [UnityTest] public IEnumerator AppliedAndPendingHealthReadsNeverAdvanceTheOwners() {
   foreach(string key in new[]{"applied","pendingHealth"}) {
    using var t=Transport(key);var a=t.SendAsync(ReadRequest());yield return Wait(a);var b=t.SendAsync(ReadRequest());yield return Wait(b);
    Assert.AreEqual(200,a.Result.Status);Same(a.Result.Body,b.Result.Body);
    var s=new WorldBossAuthoritySnapshot(a.Result.Body,Definition(),Profile(),"trial","health-run","train");
    Assert.AreEqual(key=="applied"?740:1000,s.Hp);Assert.AreEqual(key=="applied"?"APPLIED":"PENDING_HEALTH",s.ProcessingStatus);
   }
  }
  [UnityTest] public IEnumerator ExactVersionMalformedBodyAndNoHealthApplyEndpoint() {
   foreach(string kind in new[]{"version","shape","healthApply"}) {
    var x=Intake("train");string path="/_local/world-boss/attempts/trial/authority";
    using var web=new UnityWebRequest((string)x["origin"]+path+(kind=="shape"?"/commands":kind=="healthApply"?"/health/apply":""),kind=="version"?"GET":"POST");
    web.redirectLimit=0;web.timeout=5;web.downloadHandler=new DownloadHandlerBuffer();
    foreach(var p in ((JObject)x["headers"]).Properties())web.SetRequestHeader(p.Name,(string)p.Value);
    if(kind=="version")web.SetRequestHeader("X-World-Rail-Version","unapproved");
    else {web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));web.SetRequestHeader("Idempotency-Key","invalid-shape");web.SetRequestHeader("X-World-Journal-Revision","1");}
    yield return web.SendWebRequest();
    Assert.AreEqual(kind=="version"?409:kind=="shape"?400:404,(int)web.responseCode,"Strict socket boundary");
   }
  }
  [UnityTest] public IEnumerator DisposingReplacedConnectionPreservesNewBinding() {
   using var c=Client();using var aTransport=Transport("train");using var bTransport=Transport("train");
   var a=new WorldBossSocketConnection(c,aTransport);
   using var b=new WorldBossSocketConnection(c,bTransport);
   a.Dispose();var previous=b.LastExchange;c.RequestRead();
   Assert.AreNotSame(previous,b.LastExchange,"Only the replaced connection's own binding may be removed");
   yield return Wait(b.LastExchange);Assert.AreEqual(1000,c.State.Hp);
  }
  [UnityTest] public IEnumerator CancelAndDisposeSuppressLatePublicationAndReleaseOwnRequests() {
   using(var t=Transport("train")) {
    using var stop=new CancellationTokenSource();var task=t.SendAsync(ReadRequest(),stop.Token);stop.Cancel();
    while(!task.IsCompleted)yield return null;
    Assert.IsTrue(task.IsCanceled);Assert.AreEqual(0,t.InFlightCount);
   }
   using var c=Client();using var transport=Transport("train");int updates=0;
   var conn=new WorldBossSocketConnection(c,transport,()=>updates++);
   c.RequestRead();var pending=conn.LastExchange;conn.Dispose();
   while(!pending.IsCompleted)yield return null;
   Assert.IsTrue(pending.IsCanceled||pending.IsFaulted);Assert.AreEqual(0,updates);Assert.AreEqual(0,transport.InFlightCount);
   var previous=conn.LastExchange;c.RequestRead();Assert.AreSame(previous,conn.LastExchange);
   Assert.ThrowsAsync<ObjectDisposedException>(async()=>await transport.SendAsync(ReadRequest()));
  }
 }
}
#endif



