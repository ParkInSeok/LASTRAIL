#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode {
 public sealed class BNamedHttpIntegrationTests {
  const string Qa="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/NamedHttpIntegration-20261005/";
  static IEnumerator Wait(Task task){var deadline=DateTime.UtcNow.AddSeconds(30);while(!task.IsCompleted){Assert.Less(DateTime.UtcNow,deadline,"Actual HTTP task timed out");yield return null;}}
  static GameApiAuthenticatedHttpTransport Transport(UnityWebRequestRunApiHttpAdapter adapter)=>new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter));
  static RunApiHttpRequest Request(bool effective,string version,string ifMatch=null){var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader(effective?"X-B-Named-Effective-Version":"X-B-Named-Operator-Version",version)};if(ifMatch!=null)headers.Add(new RunApiHttpHeader("If-Match",ifMatch));return new RunApiHttpRequest("POST",effective?"/v1/combat/b-named-effective-source":"/v1/combat/b-named-operator-source",new JObject{["operatorIds"]=new JArray("op_kyle","op_sable"),["expectedFormationRevision"]=1,["expectedLoadoutRevision"]=1}.ToString(),headers);}
  [UnityTest] public IEnumerator OriginalAuthenticatedTcpSourceFeedsNativeAndRejectsLatePublication(){
   var path=Environment.GetEnvironmentVariable("LASTRAIL_U30_CONNECTION");
   if(string.IsNullOrWhiteSpace(path))Assert.Ignore("U30 isolated original server harness must be started explicitly");
   var config=JObject.Parse(File.ReadAllText(path));var token=new GameApiAccessToken((string)config["token"]);
   using(var publicAdapter=new UnityWebRequestRunApiHttpAdapter((string)config["publicOrigin"],10))
   using(var privateAdapter=new UnityWebRequestRunApiHttpAdapter((string)config["privateOrigin"],10))
   using(var delayedAdapter=new UnityWebRequestRunApiHttpAdapter((string)config["delayedOrigin"],10)) {
    var pub=Transport(publicAdapter);var priv=Transport(privateAdapter);var delayed=Transport(delayedAdapter);
    var sourceTask=new BNamedOperatorSourceHttpClient(pub).CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token);yield return Wait(sourceTask);var named=sourceTask.GetAwaiter().GetResult();
    Assert.AreEqual(JTokenType.Null,named.Capture()["refit"].Type);
    var publicTask=new BNamedEffectiveSourceHttpClient(pub).CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token,"a");yield return Wait(publicTask);var publicSource=publicTask.GetAwaiter().GetResult();Assert.AreEqual("OPERATING_PROVIDER_UNAVAILABLE",publicSource.ProviderStatus);
    var privateClient=new BNamedEffectiveSourceHttpClient(priv);var privateTask=privateClient.CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token,"a");yield return Wait(privateTask);var supplied=privateTask.GetAwaiter().GetResult();Assert.AreEqual("ORIGINAL_PERSISTED_PROVIDER_BOUND",supplied.ProviderStatus);
    var p=supplied.Capture();Assert.IsTrue(((JArray)p["actors"]).All(a=>a["effectiveStats"].Type==JTokenType.Null&&(int)a["modifierApplications"]==0));
    // Strict restoration is pinned to this actual HTTP response, not a rewritten sample.
    Assert.IsTrue(JToken.DeepEquals(p,BNamedEffectiveSource.Read(p,p,supplied.Hash,"a").Capture()));
    var tampered=(JObject)p.DeepClone();tampered["actors"][0]["resolvedPredepartureStats"]["maxHp"]=999999;Assert.Throws<ArgumentException>(()=>BNamedEffectiveSource.Read(tampered,p,supplied.Hash,"a"));
    var f=BNamedOperatorSourceTests.Native(supplied.RequirePredepartureBase());var g=BOrdinaryGeometryTests.Geometry(f.binding,BOrdinaryGeometryTests.ShortCarAsset());var n=f.adapter.CreateRuntime(f.binding,g,10000);
    Assert.AreEqual(1400,n.Hp("op_kyle"));Assert.AreEqual(1600,n.Hp("op_sable"));
    n.MoveOrdinaryOperator("u30-local-sable-probe","op_sable",0,new JArray(0,2400,0),g.Hash);var hits=new List<BDirectAttackBuffReceipt>();for(int i=0;i<15;i++)hits.AddRange(n.Advance("u30-native-"+i,10000000,500).Hits);var kyleHits=hits.Where(h=>h.Attack.SourceId=="op_kyle").ToArray();hits=hits.Where(h=>h.Attack.SourceId=="op_sable").ToList();Assert.That(kyleHits.Length,Is.GreaterThanOrEqualTo(2));Assert.That(kyleHits[0].Attack.Hp.HpLoss,Is.GreaterThan(0).And.LessThanOrEqualTo(36));Assert.AreEqual(900,kyleHits[1].Attack.Tick-kyleHits[0].Attack.Tick);Assert.That(hits.Count,Is.GreaterThanOrEqualTo(2));Assert.That(hits[0].Attack.Hp.HpLoss,Is.GreaterThan(0).And.LessThanOrEqualTo(32));Assert.AreEqual(1000,hits[1].Attack.Tick-hits[0].Attack.Tick);
    var state=n.Capture();var restored=BDirectAttackBasicLoop.RestoreOrdinaryGeometryRuntime(state,BDirectAttackInput.Digest(state),f.binding,BInterceptSourceTests.Read(BInterceptSourceTests.Sample()).OriginalSource,g,10000,false);Assert.IsTrue(JToken.DeepEquals(state,restored.Capture()));
    var observations=new JArray();
    foreach(bool effective in new[]{false,true})foreach(var mode in new[]{"same","stale","version","auth"}){
     var hash=effective?publicSource.Hash:named.Hash;var task=pub.SendAsync(Request(effective,mode=="version"?"old":effective?BNamedEffectiveSource.Version:BNamedOperatorSource.Version,mode=="same"?hash:mode=="stale"?new string('0',64):null),mode=="auth"?new GameApiAccessToken("lst1.invalid-u30"):token);yield return Wait(task);var result=task.GetAwaiter().GetResult();Assert.AreEqual(mode=="same"?200:mode=="auth"?401:409,result.Response.StatusCode);if(mode=="auth")Assert.AreEqual(GameApiAuthenticatedHttpOutcome.AuthRequired,result.Outcome);if(mode=="same")Assert.AreEqual(hash,result.Response.Header("ETag")?.Trim('"'));observations.Add(new JObject{["endpoint"]=effective?"S28":"S27",["case"]=mode,["status"]=result.Response.StatusCode,["errorCode"]=JObject.Parse(result.Response.BodyJson)["errorCode"]});
    }
    var revisionTask=privateClient.CaptureAsync(new[]{"op_kyle","op_sable"},2,1,token,"a");yield return Wait(revisionTask);Assert.IsTrue(revisionTask.IsFaulted);StringAssert.Contains("409",revisionTask.Exception.InnerException.Message);
    var loadoutTask=privateClient.CaptureAsync(new[]{"op_kyle","op_sable"},1,2,token,"a");yield return Wait(loadoutTask);Assert.IsTrue(loadoutTask.IsFaulted);StringAssert.Contains("409",loadoutTask.Exception.InnerException.Message);
    var effectTask=privateClient.CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token,"a",new string('0',64));yield return Wait(effectTask);Assert.IsTrue(effectTask.IsFaulted);StringAssert.Contains("409",effectTask.Exception.InnerException.Message);
    var accountTask=privateClient.CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token,"b");yield return Wait(accountTask);Assert.IsTrue(accountTask.IsFaulted);Assert.IsInstanceOf<ArgumentException>(accountTask.Exception.InnerException);
    using(var owner=new BNamedEffectiveSourceOwner("a")){
     int calls=0;owner.Bind(s=>calls++);var first=owner.RefreshAsync(()=>privateClient.CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token,"a"));yield return Wait(first);Assert.IsTrue(first.GetAwaiter().GetResult());var current=owner.Current;
     var late=owner.RefreshAsync(()=>new BNamedEffectiveSourceHttpClient(delayed).CaptureAsync(new[]{"op_kyle","op_sable"},1,1,token,"a"));Assert.AreEqual(1,delayedAdapter.InFlightCount);owner.Unbind();yield return Wait(late);Assert.IsFalse(late.GetAwaiter().GetResult());Assert.AreEqual(1,calls);Assert.AreSame(current,owner.Current);
    }
    Assert.AreEqual(0,publicAdapter.InFlightCount);Assert.AreEqual(0,privateAdapter.InFlightCount);Assert.AreEqual(0,delayedAdapter.InFlightCount);
    File.WriteAllText(Qa+"unity-response-native.json",new JObject{["evidence"]="actual UnityWebRequest -> original authenticated game-api loopback -> strict source -> existing LOCAL native",["publicNamedHash"]=named.Hash,["publicEffectiveHash"]=publicSource.Hash,["privateEffectiveHash"]=supplied.Hash,["effectSourceHash"]=supplied.EffectSourceHash,["nativeInputHash"]=f.adapter.Input.OriginalHash,["hpInitial"]=new JArray(1400,1600),["kyleBasicHits"]=kyleHits.Length,["kyleFirstHpDamage"]=kyleHits[0].Attack.Hp.HpLoss,["kylePeriodTicks"]=kyleHits[1].Attack.Tick-kyleHits[0].Attack.Tick,["sableBasicHits"]=hits.Count,["sableFirstHpDamage"]=hits[0].Attack.Hp.HpLoss,["sablePeriodTicks"]=hits[1].Attack.Tick-hits[0].Attack.Tick,["nativeSnapshotExactRestore"]=true,["responseExactRestore"]=true,["unboundLatePublication"]=false,["inFlightAtEnd"]=0,["httpObservations"]=observations,["operatingRuntimeSupported"]=false,["canStart"]=false}.ToString());
    File.WriteAllText(Qa+"actual-private-response.json",p.ToString());File.WriteAllText(Qa+"native-snapshot.json",state.ToString());
   }
  }
 }
}
#endif
