#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public class BRankFlowExecutionTests {
  static JObject bundle;BSelectionController selection;BRefitController refit;BSelectionHttpTransport st;BRefitHttpTransport rt;BSelectionConnection sc;BRefitConnection rc;
  [OneTimeSetUp]public void Intake(){var raw=Environment.GetEnvironmentVariable("LASTRAIL_B_RANK_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_B_RANK_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Private Server rank fixture supervisor required");bundle=JObject.Parse(raw);}
  [OneTimeTearDown]public void Release()=>bundle=null;
  [TearDown]public void Close(){sc?.Dispose();rc?.Dispose();selection?.Dispose();refit?.Dispose();st?.Dispose();rt?.Dispose();}
  static IEnumerator Wait(Task t){while(!t.IsCompleted)yield return null;Assert.IsFalse(t.IsFaulted,"Safe rank exchange");Assert.IsFalse(t.IsCanceled);}
  static void Same(JToken a,JToken b)=>Assert.IsTrue(JToken.DeepEquals(a,b),"Original rank/receipt/run preserved");
  static JObject Intake(string name,string kind=null){var full=(JObject)bundle[name].DeepClone();if(kind!=null){var h=(JObject)full["headers"];var key=kind=="selection"?"X-Combat-B-Selection-Version":"X-Combat-B-Refit-Version";full["headers"]=new JObject{["Authorization"]=h["Authorization"].DeepClone(),[key]=h[key].DeepClone()};}return full;}
  static BLocalFlowSourceHttpTransport Source(string name)=>new BLocalFlowSourceHttpTransport(new BLocalFlowSourcePrivateIntake(Intake(name)));
  static string Trusted(string name,int stage=0)=>(string)bundle["trusted"][name][stage];
  void Open(string name){st=new BSelectionHttpTransport(new BSelectionPrivateIntake(Intake(name,"selection")));rt=new BRefitHttpTransport(new BRefitPrivateIntake(Intake(name,"refit")));selection=new BSelectionController();refit=new BRefitController();sc=new BSelectionConnection(selection,st);rc=new BRefitConnection(refit,rt);}
  BLocalFlowCandidate Candidate()=>new BLocalFlowCandidate(st.AccountId,selection.State,refit.State,refit.OriginalRequest,refit.Receipt);
  static JArray Cells(int rank)=>new JArray(Enumerable.Range(0,2).Select(slot=>new JObject{["slot"]=slot,["type"]="BCB01",["rank"]=rank}));
  IEnumerator Read(){selection.Read();yield return Wait(sc.LastExchange);refit.Read();yield return Wait(rc.LastExchange);}
  [UnityTest]public IEnumerator ActualPrivateOwnerRanksOneThroughFiveAndResetDriveNativeHpWhileOldRunAndReceiptsStayFrozen(){
   Open("workflow");var rows=new JArray();BLocalFlowSourcePacket first=null;BDirectAttackBasicLoop old=null;JObject oldRun=null,oldWire=null,oldReceipt=null;BRefitRequest oldRequest=null;BRefitSnapshot initialRead=null;var ranks=new[]{1,2,3,4,5,1};var damage=new[]{28,29,31,32,33,28};string priorHash=null;
   for(int stage=0;stage<ranks.Length;stage++){
    yield return Read();var read=refit.State;Assert.AreEqual(stage+1,(long)read.Capture()["rankSource"]["revision"]);var rs=(JArray)read.Capture()["rankSource"]["ranks"];Assert.AreEqual(ranks[stage],(int)rs.Single(r=>(string)r["type"]=="BCB01")["rank"]);if(stage>0){Assert.IsNotNull(read.EligibilityError);Assert.AreNotEqual(priorHash,(string)read.Capture()["sourceHash"]);}
    refit.Save(Cells(ranks[stage]),"rank-refit-"+stage);yield return Wait(rc.LastExchange);Assert.IsNotNull(refit.Receipt);refit.Read();yield return Wait(rc.LastExchange);Assert.IsNull(refit.State.EligibilityError);var beforeTransition=refit.State;var candidate=Candidate();priorHash=(string)refit.State.Capture()["sourceHash"];
    using var source=Source("workflow");using var connection=new BLocalFlowSourceConnection(candidate,source,Trusted("workflow",stage));yield return Wait(connection.ReadAsync());var packet=connection.Packet;Assert.IsNotNull(packet);var wire=packet.Capture();Assert.AreEqual(stage+1,(long)wire["rankRevision"]);Assert.AreEqual(1,(long)wire["selectionRevision"]);var cars=(JArray)wire["departure"]["cars"];CollectionAssert.AreEqual(new[]{"a_physical1","a_physical2"},cars.Select(c=>(string)c["instanceId"]));Assert.IsTrue(cars.All(c=>(string)c["type"]=="BCB01"&&(int)c["rank"]==ranks[stage]),"Both placements use actual shared type rank");
    var loop=packet.CreateLoop(1000);var hits=loop.Advance("three",40010000,4001);CollectionAssert.AreEqual(new long[]{1,2001,4001},hits.Hits.Select(h=>h.Attack.Tick));CollectionAssert.AreEqual(Enumerable.Repeat(damage[stage],3),hits.Hits.Select(h=>h.Attack.BeforeShield));Assert.AreEqual(700-3*damage[stage],loop.Hp("enemy"));Assert.AreEqual(1368,loop.Hp("op_eve"));Assert.IsFalse(packet.StartReady);
    rows.Add(new JObject{["rank"]=ranks[stage],["rankRevision"]=wire["rankRevision"].DeepClone(),["refitRevision"]=wire["refitRevision"].DeepClone(),["sourceHash"]=wire["sourceHash"].DeepClone(),["packetHash"]=packet.OriginalHash,["physical"]=cars.DeepClone(),["damage"]=damage[stage],["hpAfterThree"]=loop.Hp("enemy"),["ticks"]=new JArray(hits.Hits.Select(h=>h.Attack.Tick))});
    if(stage==0){first=packet;old=loop;oldRun=loop.Capture();oldWire=wire;oldReceipt=refit.Receipt;oldRequest=refit.OriginalRequest;initialRead=read;}
    else {Same(oldRun,old.Capture());Same(oldWire,first.Capture());}
    var replay=rt.SendAsync(oldRequest);yield return Wait(replay);Assert.AreEqual(200,replay.Result.Status);Same(oldReceipt,replay.Result.Body);
    if(stage<5){var stale=rt.SendAsync(BRefitRequest.Save(beforeTransition,Cells(ranks[stage]),"stale-"+stage));yield return Wait(stale);Assert.AreEqual(409,stale.Result.Status);}
   }
   var restore=BDirectAttackBasicLoop.Restore(oldRun,first.Binding,1000);old.Advance("fourth",20000000,2000);restore.Advance("fourth",20000000,2000);Same(old.Capture(),restore.Capture());Assert.AreEqual(588,restore.Hp("enemy"));
   File.WriteAllText((string)bundle["observationPath"],new JObject{["taskId"]="U-B-RANK-FLOW-12",["version"]="combat.b-local-rank-fixture.v1.20261003",["stages"]=rows,["oldR1RunFrozenThroughR5AndReset"]=true,["oldR1ReceiptReplayedAtEveryStage"]=true,["oldR1RestoreFourthHp"]=restore.Hp("enemy"),["sharedSameTypeBothPlacements"]=true,["rankWriteCapabilityInClient"]=false,["startReady"]=false}.ToString());
  }
  [UnityTest]public IEnumerator RestoredActualRankFiveSourceAndOriginalRankOneReceiptRemainDistinctWithFreshAuth(){
   Open("restored");yield return Read();Assert.AreEqual(5,(long)refit.State.Capture()["rankSource"]["revision"]);refit.Save(Cells(5),"restore-noop");yield return Wait(rc.LastExchange);refit.Read();yield return Wait(rc.LastExchange);using var connection=new BLocalFlowSourceConnection(Candidate(),Source("restored"),Trusted("restored"));yield return Wait(connection.ReadAsync());Same(bundle["restoredPacket"],connection.Packet.Capture());var loop=connection.Packet.CreateLoop(1000);Assert.AreEqual(33,loop.Advance("one",10000,1).Hits[0].Attack.BeforeShield);Assert.AreEqual(667,loop.Hp("enemy"));var before=new BRefitSnapshot((JObject)bundle["firstRefitRead"],"a");var replay=rt.SendAsync(BRefitRequest.Save(before,Cells(1),"rank-refit-0"));yield return Wait(replay);Same(bundle["firstRefitReceipt"],replay.Result.Body);Assert.AreEqual(1,(long)replay.Result.Body["refit"]["rankRevision"]);using var old=Source("old");var expired=old.ReadAsync();yield return Wait(expired);Assert.AreEqual(401,expired.Result.Status);
  }
  [UnityTest]public IEnumerator AccountBStillHasRankOneAfterAccountARankFiveAndReset(){Open("accountB");yield return Read();Assert.AreEqual(1,(long)refit.State.Capture()["rankSource"]["revision"]);refit.Save(Cells(1),"b-noop");yield return Wait(rc.LastExchange);refit.Read();yield return Wait(rc.LastExchange);using var connection=new BLocalFlowSourceConnection(Candidate(),Source("accountB"),Trusted("accountB"));yield return Wait(connection.ReadAsync());var wire=connection.Packet.Capture();Assert.AreEqual("b",(string)wire["accountId"]);Assert.AreEqual("b_physical1",(string)wire["departure"]["cars"][0]["instanceId"]);Assert.AreEqual(28,connection.Packet.CreateLoop(1000).Advance("one",10000,1).Hits[0].Attack.BeforeShield);}
  [UnityTest]public IEnumerator DisposeRankFiveSourceRequestDoesNotPublishOrLeakOwnedExchange(){Open("slow");yield return Read();refit.Save(Cells(5),"slow-noop");yield return Wait(rc.LastExchange);refit.Read();yield return Wait(rc.LastExchange);var source=Source("slow");var connection=new BLocalFlowSourceConnection(Candidate(),source,Trusted("slow"));var pending=connection.ReadAsync();yield return new UnityEngine.WaitForSecondsRealtime(.15f);connection.Dispose();while(!pending.IsCompleted)yield return null;Assert.IsTrue(pending.IsCanceled||pending.IsFaulted);Assert.IsNull(connection.Packet);Assert.IsFalse(connection.InFlight);Assert.AreEqual(0,source.InFlightCount);using var fresh=new BLocalFlowSourceConnection(Candidate(),Source("slow"),Trusted("slow"));yield return Wait(fresh.ReadAsync());Assert.AreEqual(33,fresh.Packet.CreateLoop(1000).Advance("reopen",10000,1).Hits[0].Attack.BeforeShield);}
 }
}
#endif