#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class RunRiskBindingHttpTests {
  static GameApiAuthSessionSnapshot Session(JObject s)=>new GameApiAuthSessionSnapshot((string)s["accountId"],"ACTIVE",new GameApiAccessToken((string)s["token"]),DateTime.Parse((string)s["expiresAt"]).ToUniversalTime(),new GameApiRefreshToken("local-fixture-unused"),DateTime.UtcNow.AddMinutes(10));
  [UnityTest] public IEnumerator OriginalSqlSignedRoutePublishesVisitedMetadataIntoExistingModelWithoutBPhysics(){
   string raw=Environment.GetEnvironmentVariable("LASTRAIL_RISK_RUN_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_RISK_RUN_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned original SQL/auth supervisor required");var privateBundle=GameApiJson.ReadObject(raw);raw=null;string folder=(string)privateBundle["folder"],origin=(string)privateBundle["origin"];var owner=Session((JObject)privateBundle["session"]);var originalOwner=owner;var foreignOwner=Session((JObject)privateBundle["foreignSession"]);long generation=1;using var adapter=new UnityWebRequestRunApiHttpAdapter(origin,5);var wire=new UnityWebRequestRunApiHttpTransport(adapter);using var model=new AccountScreenRuntime(wire,null,()=>owner,()=>generation);model.SynchronizeSession();var client=model.RiskRunBinding;var initialTrust=(JObject)privateBundle["trust"].DeepClone();privateBundle=null;
   var task=client.RefreshAsync(initialTrust);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("READY",client.Status);var frozen=client.Snapshot.Frozen;var outputs=new JArray(client.Snapshot.Capture());string previousHash=(string)initialTrust["packetHash"];Assert.IsFalse(model.CanConfirmDeparture);Assert.Throws<InvalidOperationException>(()=>client.Snapshot.RequireScalingSource());
   for(int stage=1;stage<=18;stage++){
    File.WriteAllText(folder+"/request"+stage+".json",new JObject{["stage"]=stage}.ToString());var deadline=DateTime.UtcNow.AddSeconds(30);while(!File.Exists(folder+"/published"+stage+".json")){Assert.Less(DateTime.UtcNow,deadline,"Producer stage unavailable");yield return null;}var published=JObject.Parse(File.ReadAllText(folder+"/published"+stage+".json"));var trust=(JObject)published["trust"];
    if(stage<=15){task=client.RefreshAsync(trust,previousHash);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("STALE",client.Status);Assert.IsNull(client.Snapshot);task=client.RefreshAsync(trust,(string)trust["packetHash"]);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("READY",client.Status);var snapshot=client.Snapshot;Assert.IsTrue(JToken.DeepEquals(frozen,snapshot.Frozen));Assert.AreEqual((int)published["depth"],(int)snapshot.Progress["visitDepth"]);Assert.IsTrue(JToken.DeepEquals(published["packet"],snapshot.Capture()));Assert.Throws<InvalidOperationException>(()=>snapshot.RequireScalingSource());client.RestoreVerified(snapshot.Capture(),trust);Assert.IsTrue(JToken.DeepEquals(published["packet"],client.Snapshot.Capture()));if(stage==13){Assert.AreEqual("EARLY",(string)snapshot.Progress["band"]);Assert.IsNotNull((string)snapshot.Progress["pendingEdgeId"]);}if(stage==15)Assert.IsFalse(snapshot.ActiveRun);outputs.Add(snapshot.Capture());previousHash=(string)trust["packetHash"];}
    else {if(stage==17){owner=foreignOwner;generation++;model.SynchronizeSession();trust["accountId"]=owner.AccountId;}if(stage==18){owner=originalOwner;generation++;model.SynchronizeSession();}task=client.RefreshAsync(trust);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.IsNull(client.Snapshot);Assert.AreEqual((string)published["expectedError"],client.ErrorCode);outputs.Add(new JObject{["stage"]=stage,["status"]=client.Status,["errorCode"]=client.ErrorCode});}
   }
   Assert.IsFalse(model.CanConfirmDeparture);Assert.AreEqual(0,adapter.InFlightCount);
   using(var ownedAdapter=new UnityWebRequestRunApiHttpAdapter(origin,5)){var ownedWire=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(ownedAdapter));var pending=ownedWire.SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+(string)initialTrust["runId"]+"/operation-risk-binding",null,new[]{new RunApiHttpHeader("X-Combat-B-Risk-Binding-Version",RunRiskBindingSnapshot.Version)}),originalOwner.AccessToken);Assert.AreEqual(1,ownedAdapter.InFlightCount);ownedAdapter.Dispose();while(!pending.IsCompleted)yield return null;Assert.IsTrue(pending.IsFaulted);Assert.IsTrue(pending.Exception.InnerException is ObjectDisposedException);Assert.AreEqual(0,ownedAdapter.InFlightCount);}
   File.WriteAllText(folder+"/actual-unity.json",new JObject{["metadataOnly"]=true,["missionStart"]=false,["scaling"]=false,["legacyRuntimeReinterpreted"]=false,["nativeAbortAndRelease"]=true,["packetsAndErrors"]=outputs}.ToString());
  }
 }
}
#endif
