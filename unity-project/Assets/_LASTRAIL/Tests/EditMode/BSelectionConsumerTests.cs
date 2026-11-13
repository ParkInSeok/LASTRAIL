#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.Networking;
namespace LastRail.Tests.EditMode {
 public class BSelectionConsumerTests {
  static JObject bundle;
  [OneTimeSetUp] public void PrivateChildIntake(){
   var raw=Environment.GetEnvironmentVariable("LASTRAIL_B_SELECTION_PRIVATE");
   Environment.SetEnvironmentVariable("LASTRAIL_B_SELECTION_PRIVATE",null);
   if(string.IsNullOrEmpty(raw))Assert.Ignore("Private selection supervisor required");
   bundle=JObject.Parse(raw);
  }
  [OneTimeTearDown] public void Release(){bundle=null;}
  static BSelectionHttpTransport Transport(string key="normal")=>new BSelectionHttpTransport(new BSelectionPrivateIntake((JObject)bundle[key].DeepClone()));
  static JObject Selection(int cargo=1){var rows=new JArray(new JObject{{"slot",0},{"carriageId","LOCAL_CAR"},{"occupant",new JObject{{"kind","OPERATOR"},{"operatorId","op_eve"}}}});for(int i=1;i<=cargo;i++)rows.Add(new JObject{{"slot",i},{"carriageId","LOCAL_CAR"},{"occupant",new JObject{{"kind","CARGO"}}}});return new JObject{{"version",BSelectionDefinition.Version},{"mountedTurrets",new JArray()},{"placements",rows}};}
  static IEnumerator Wait(Task task){while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted,"Safe selection exchange must complete");Assert.IsFalse(task.IsCanceled);}
  static void Same(JToken a,JToken b)=>Assert.IsTrue(JToken.DeepEquals(a,b),"Original value preserved");
  [UnityTest] public IEnumerator EmptySaveReadNoopReplayConflictAndNewKey(){
   using var t=Transport();using var c=new BSelectionController();using var connection=new BSelectionConnection(c,t);
   c.Read();yield return Wait(connection.LastExchange);Assert.AreEqual(0,c.State.Revision);Assert.IsNull(c.State.Selection);
   var r=c.Save(new BSelectionDefinition(Selection()),"save");Assert.Throws<InvalidOperationException>(()=>c.Read());yield return Wait(connection.LastExchange);
   Assert.AreEqual(1,c.State.Revision);Assert.IsFalse(c.State.StartReady);var ack=c.Receipt;
   c.ReplayOriginal();yield return Wait(connection.LastExchange);Same(ack,c.Receipt);
   c.Read();yield return Wait(connection.LastExchange);Assert.AreEqual(1,c.State.Revision);Same(Selection(),c.State.Selection.Capture());
   c.Save(new BSelectionDefinition(Selection()),"noop");yield return Wait(connection.LastExchange);Assert.AreEqual(1,c.State.Revision);Assert.IsFalse((bool)c.Receipt["changed"]);
   var wrong=BSelectionRequest.Save(1,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection(2)),"save");var x=t.SendAsync(wrong);yield return Wait(x);Assert.AreEqual(409,x.Result.Status);
   x=t.SendAsync(r);yield return Wait(x);Same(ack,x.Result.Body);
   x=t.SendAsync(BSelectionRequest.Save(0,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection(2)),"stale"));yield return Wait(x);Assert.AreEqual(409,x.Result.Status);
   c.Read();yield return Wait(connection.LastExchange);c.Save(new BSelectionDefinition(Selection(2)),"fresh");yield return Wait(connection.LastExchange);Assert.AreEqual(2,c.State.Revision);
   Assert.AreEqual(0,t.InFlightCount);
  }
  [UnityTest] public IEnumerator CatalogConflictRequiresReadAndOriginalRetryRemainsExact(){
   using var t=Transport("catalog2");using var c=new BSelectionController();using var conn=new BSelectionConnection(c,t);
   c.Read();yield return Wait(conn.LastExchange);Assert.AreEqual(2,c.State.CatalogRevision);
   var original=c.Save(new BSelectionDefinition(Selection()),"catalog-command");yield return Wait(conn.LastExchange);Assert.IsNull(c.LastError);
   var bad=t.SendAsync(BSelectionRequest.Save(c.State.Revision,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection()),"old-catalog"));yield return Wait(bad);Assert.AreEqual(409,bad.Result.Status);
   c.Read();yield return Wait(conn.LastExchange);c.Save(new BSelectionDefinition(Selection(2)),"new-catalog");yield return Wait(conn.LastExchange);Assert.IsNull(c.LastError);
   Assert.AreEqual(2,(long)c.Receipt["savedCatalog"]["revision"]);Assert.AreEqual(2,(long)original.Body["expectedCatalogRevision"]);
  }
  [UnityTest] public IEnumerator MemoryRestartPreservesSelectionReceiptAndRejectsOldAuth(){
   using var t=Transport("restored");var read=t.SendAsync(BSelectionRequest.Read());yield return Wait(read);var state=new BSelectionSnapshot(read.Result.Body);
   Assert.AreEqual(1,state.Revision);Same(Selection(),state.Selection.Capture());
   var replay=t.SendAsync(BSelectionRequest.Save(0,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection()),"checkpoint"));yield return Wait(replay);Same(bundle["checkpointReceipt"],replay.Result.Body);
   using var old=Transport("old");var denied=old.SendAsync(BSelectionRequest.Read());yield return Wait(denied);Assert.AreEqual(401,denied.Result.Status);
  }
  [UnityTest] public IEnumerator LostAcknowledgementAndStorageFailureKeepOriginalCommand(){
   foreach(string key in new[]{"lost","storage"}){
    using var t=Transport(key);using var c=new BSelectionController();using var conn=new BSelectionConnection(c,t);
    c.Read();yield return Wait(conn.LastExchange);var original=c.Save(new BSelectionDefinition(Selection()),"retry");yield return Wait(conn.LastExchange);
    Assert.AreEqual(503,c.LastError.Status);Same(original.Body,c.OriginalRequest.Body);Assert.AreEqual(0,c.State.Revision);
    c.ReplayOriginal();yield return Wait(conn.LastExchange);
    if(key=="lost"){Assert.IsNull(c.LastError);Assert.AreEqual(1,c.State.Revision);c.Read();yield return Wait(conn.LastExchange);Assert.AreEqual(1,c.State.Revision);}
    else {Assert.AreEqual(503,c.LastError.Status);c.Read();yield return Wait(conn.LastExchange);Assert.AreEqual(0,c.State.Revision);}
   }
  }
  [UnityTest] public IEnumerator HistoricalSelectionAndReceiptNeverReplaceCurrentEligibility(){
   foreach(string key in new[]{"ineligible","locked"}){
    using var t=Transport(key);using var c=new BSelectionController();using var conn=new BSelectionConnection(c,t);
    c.Read();yield return Wait(conn.LastExchange);Assert.IsNotNull(c.State.EligibilityError);var before=c.State.Capture();
    // Receipt replay succeeds independently of current ownership/catalog eligibility.
    var original=BSelectionRequest.Save(0,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection()),"checkpoint");
    var response=t.SendAsync(original);yield return Wait(response);Assert.AreEqual(200,response.Result.Status);Same(bundle["checkpointReceipt"],response.Result.Body);Same(before,c.State.Capture());
    c.Save(new BSelectionDefinition(Selection()),"not-eligible");yield return Wait(conn.LastExchange);Assert.AreEqual(key=="ineligible"?403:409,c.LastError.Status);Same(before,c.State.Capture());
   }
  }
  [UnityTest] public IEnumerator WriteConflictRequiresFreshReadAndFreshCommandKey(){
   using var t=Transport("catalog2");using var other=Transport("catalog2");using var c=new BSelectionController();using var conn=new BSelectionConnection(c,t);
   c.Read();yield return Wait(conn.LastExchange);var write=other.SendAsync(BSelectionRequest.Save(c.State.Revision,"LOCAL_B_CATALOG_1",2,new BSelectionDefinition(Selection(3)),"competing"));yield return Wait(write);Assert.AreEqual(200,write.Result.Status);
   c.Save(new BSelectionDefinition(Selection()),"conflict");yield return Wait(conn.LastExchange);Assert.AreEqual(409,c.LastError.Status);
   Assert.Throws<InvalidOperationException>(()=>c.Save(new BSelectionDefinition(Selection()),"retry-before-read"));Assert.Throws<InvalidOperationException>(()=>c.ReplayOriginal());
   c.Read();yield return Wait(conn.LastExchange);Assert.Throws<InvalidOperationException>(()=>c.Save(new BSelectionDefinition(Selection()),"conflict"));
   c.Save(new BSelectionDefinition(Selection()),"reconciled");yield return Wait(conn.LastExchange);Assert.IsNull(c.LastError);
  }
  [UnityTest] public IEnumerator AuthFailuresBlockAllCommandsAndUnavailableCatalogStaysAbsent(){
   foreach(var pair in new[]{("old",401),("unavailable",503)}){
    using var t=Transport(pair.Item1);using var c=new BSelectionController();using var conn=new BSelectionConnection(c,t);
    c.Read();yield return Wait(conn.LastExchange);Assert.AreEqual(pair.Item2,c.LastError.Status);Assert.IsNull(c.State);
    if(pair.Item2!=503)Assert.Throws<InvalidOperationException>(()=>c.Read());
   }
   using var forbidden=Transport("forbidden");using var owner=new BSelectionController();using var connection=new BSelectionConnection(owner,forbidden);
   owner.Read();yield return Wait(connection.LastExchange);var unowned=Selection();unowned["placements"][0]["occupant"]["operatorId"]="op_unowned";
   owner.Save(new BSelectionDefinition(unowned),"forbidden");yield return Wait(connection.LastExchange);Assert.AreEqual(403,owner.LastError.Status);Assert.Throws<InvalidOperationException>(()=>owner.Read());Assert.Throws<InvalidOperationException>(()=>owner.ReplayOriginal());
  }
  [UnityTest] public IEnumerator OwnedBindingReplacementCancellationAndDisposal(){
   using var c=new BSelectionController();var a=new BSelectionConnection(c,Transport());using var b=new BSelectionConnection(c,Transport());a.Dispose();
   c.Read();yield return Wait(b.LastExchange);Assert.IsNotNull(c.State);
   int changed=0;using var d=new BSelectionController();var t=Transport();var conn=new BSelectionConnection(d,t,()=>changed++);
   d.Read();var pending=conn.LastExchange;conn.Dispose();while(!pending.IsCompleted)yield return null;
   Assert.AreEqual(0,changed);Assert.AreEqual(0,t.InFlightCount);Assert.Throws<InvalidOperationException>(()=>d.Read());
   using var cancelled=Transport();using var stop=new CancellationTokenSource();var p=cancelled.SendAsync(BSelectionRequest.Read(),stop.Token);stop.Cancel();while(!p.IsCompleted)yield return null;Assert.IsTrue(p.IsCanceled);Assert.AreEqual(0,cancelled.InFlightCount);
  }
  [TestCase("origin")][TestCase("expiry")][TestCase("version")][TestCase("secret")][TestCase("extra")]
  public void PrivateBoundaryRejectsWithoutCredentialCapture(string kind){var x=(JObject)bundle["normal"].DeepClone();if(kind=="origin")x["origin"]="https://example.com";if(kind=="expiry")x["accessExpiresAt"]=0;if(kind=="version")x["headers"]["X-Combat-B-Selection-Version"]="old";if(kind=="secret")x["headers"]["Authorization"]="Bearer bad";if(kind=="extra")x["tokenFile"]="forbidden";bool rejected=false;try{using var t=new BSelectionPrivateIntake(x);}catch(ArgumentException){rejected=true;}Assert.IsTrue(rejected,"Private contract rejected");}
  [UnityTest] public IEnumerator ControllerReceiptReplayCannotEraseAReadEligibilityError(){
   using var t=Transport("restored");var read=t.SendAsync(BSelectionRequest.Read());yield return Wait(read);var current=read.Result.Body;current["revision"]=0;current["selection"]=null;current["savedCatalog"]=null;
   using var c=new BSelectionController();BSelectionRequest sent=null;c.Bind(r=>sent=r);c.Read();c.AcceptResponse(sent,new BSelectionHttpResponse(200,"no-store",BSelectionRequest.Version,current));
   c.Save(new BSelectionDefinition(Selection()),"checkpoint");c.AcceptResponse(sent,new BSelectionHttpResponse(200,"no-store",BSelectionRequest.Version,(JObject)bundle["checkpointReceipt"]));
   current=read.Result.Body;current["ownedSource"]["operators"]=new JArray();current["eligibilityError"]="combat_b_operator_not_owned";c.Read();c.AcceptResponse(sent,new BSelectionHttpResponse(200,"no-store",BSelectionRequest.Version,current));
   c.ReplayOriginal();c.AcceptResponse(sent,new BSelectionHttpResponse(200,"no-store",BSelectionRequest.Version,(JObject)bundle["checkpointReceipt"]));
   Assert.AreEqual("combat_b_operator_not_owned",c.State.EligibilityError);Same(current,c.State.Capture());
  }
  [UnityTest] public IEnumerator FiveCombinedCellsAndAccountKeyNamespacesAreReal(){
   using var a=Transport();using var b=Transport("other");var ar=a.SendAsync(BSelectionRequest.Read());yield return Wait(ar);var br=b.SendAsync(BSelectionRequest.Read());yield return Wait(br);Assert.AreEqual(0,(long)br.Result.Body["revision"]);
   var saved=b.SendAsync(BSelectionRequest.Save(0,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection(4)),"save"));yield return Wait(saved);Assert.AreEqual(200,saved.Result.Status);Assert.AreEqual(5,((JArray)saved.Result.Body["selection"]["placements"]).Count);
   var after=a.SendAsync(BSelectionRequest.Read());yield return Wait(after);Same(ar.Result.Body,after.Result.Body);
   using var controller=new BSelectionController();using var conn=new BSelectionConnection(controller,a);Assert.Throws<InvalidOperationException>(()=>new BSelectionConnection(controller,b));
  }
  [UnityTest] public IEnumerator StrictHttpVersionQueryShapeAndFiveCellBoundary(){
   foreach(string kind in new[]{"version","query","extra","six","v1"}){
    var intake=(JObject)bundle["normal"];using var web=new UnityWebRequest((string)intake["origin"]+BSelectionRequest.Path+(kind=="query"?"?accountId=a":""),kind=="version"||kind=="query"?"GET":"POST");web.redirectLimit=0;web.timeout=5;web.downloadHandler=new DownloadHandlerBuffer();
    foreach(var p in ((JObject)intake["headers"]).Properties())web.SetRequestHeader(p.Name,(string)p.Value);
    if(kind=="version")web.SetRequestHeader("X-Combat-B-Selection-Version","old");
    if(web.method=="POST"){
     var body=BSelectionRequest.Save(0,"LOCAL_B_CATALOG_1",1,new BSelectionDefinition(Selection()),"strict-"+kind).Body;
     if(kind=="extra")body["accountId"]="invented";if(kind=="six")body["selection"]=Selection(5);if(kind=="v1")body["selection"]["version"]=BCombatPlacementReplica.Version;
     web.SetRequestHeader("Content-Type","application/json");web.SetRequestHeader("Idempotency-Key","strict-"+kind);web.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None)));
    }
    yield return web.SendWebRequest();Assert.AreEqual(kind=="query"||kind=="extra"||kind=="six"?400:409,(int)web.responseCode,"Strict server contract");
   }
  }
  [UnityTest] public IEnumerator InvalidEnvelopeAndReadConflictPreserveOriginalRead(){
   using var t=Transport("restored");var read=t.SendAsync(BSelectionRequest.Read());yield return Wait(read);using var c=new BSelectionController();BSelectionRequest request=null;c.Bind(r=>request=r);c.Read();c.AcceptResponse(request,read.Result);var before=c.State.Capture();
   c.Read();var invalid=read.Result.Body;invalid["startReady"]=true;Assert.Throws<ArgumentException>(()=>c.AcceptResponse(request,new BSelectionHttpResponse(200,"no-store",BSelectionRequest.Version,invalid)));Same(before,c.State.Capture());
   c.AcceptResponse(request,new BSelectionHttpResponse(409,"no-store",BSelectionRequest.Version,new JObject{{"accepted",false},{"errorCode","combat_b_selection_read_conflict"}}));Assert.AreEqual(409,c.LastError.Status);Assert.Throws<InvalidOperationException>(()=>c.Save(new BSelectionDefinition(Selection()),"unreconciled"));Same(before,c.State.Capture());
   c.Read();c.AcceptResponse(request,read.Result);Assert.IsNull(c.LastError);Same(before,c.State.Capture());
  }
 }
 public class BSelectionModelTests {
  static JObject Selection()=>new JObject{{"version",BSelectionDefinition.Version},{"mountedTurrets",new JArray()},{"placements",new JArray(new JObject{{"slot",0},{"carriageId","type"},{"occupant",new JObject{{"kind","OPERATOR"},{"operatorId","op"}}}})}};
  [Test] public void DefinitionAndRequestAreImmutableAndVersionIsNotRewritten(){var x=Selection();var d=new BSelectionDefinition(x);x["placements"][0]["slot"]=4;Assert.AreEqual(0,(int)d.Capture()["placements"][0]["slot"]);var r=BSelectionRequest.Save(0,"catalog",1,d,"original");var copy=r.Body;copy["expectedRevision"]=8;Assert.AreEqual(0,(int)r.Body["expectedRevision"]);x["version"]=BCombatPlacementReplica.Version;Assert.Throws<ArgumentException>(()=>new BSelectionDefinition(x));}
  [TestCase("slot")][TestCase("duplicate")][TestCase("limit")][TestCase("operator")][TestCase("turret")][TestCase("extra")][TestCase("float")]
  public void InvalidDefinitionRejects(string kind){var x=Selection();var rows=(JArray)x["placements"];if(kind=="slot")rows[0]["slot"]=5;if(kind=="duplicate")rows.Add(rows[0].DeepClone());if(kind=="limit")for(int i=1;i<6;i++)rows.Add(new JObject{{"slot",i},{"carriageId","type"},{"occupant",new JObject{{"kind","CARGO"}}}});if(kind=="operator")rows[0]["occupant"]=new JObject{{"kind","CARGO"}};if(kind=="turret")((JArray)x["mountedTurrets"]).Add("gun");if(kind=="extra")x["instanceId"]="invented";if(kind=="float")rows[0]["slot"]=0.5;Assert.Throws<ArgumentException>(()=>new BSelectionDefinition(x));}
  [TestCase("")][TestCase("space key")][TestCase("한글")][TestCase("\n")]
  public void OriginalKeyIsNeverNormalized(string key){Assert.Throws<ArgumentException>(()=>BSelectionRequest.Save(0,"catalog",1,new BSelectionDefinition(Selection()),key));}
 }
}
#endif
