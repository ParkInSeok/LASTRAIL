#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BInterceptSourceHttpTests {
  [UnityTest] public IEnumerator SignedV3DefinitionAndAcceptedSourcesReachOriginalNativeOwner(){
   string raw=Environment.GetEnvironmentVariable("LASTRAIL_INTERCEPT_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_INTERCEPT_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned isolated supervisor required");var input=GameApiJson.ReadObject(raw);raw=null;string folder=(string)input["folder"];var auth=(JObject)input["session"];var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken((string)auth["token"]),DateTime.Parse((string)auth["expiresAt"]).ToUniversalTime(),new GameApiRefreshToken("unused-local"),DateTime.UtcNow.AddMinutes(10));using var adapter=new UnityWebRequestRunApiHttpAdapter((string)input["origin"],5);var wire=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter));using var client=new BInterceptDefinitionClient(wire,()=>session,()=>1);string hash=(string)input["expectedPacketHash"];
   var wait=client.RefreshAsync(hash);while(!wait.IsCompleted)yield return null;Assert.IsFalse(wait.IsFaulted);Assert.AreEqual("READY",client.Status);var packet=client.Snapshot.Capture();client.RestoreVerified(packet,hash);wait=client.RefreshAsync(hash,hash);while(!wait.IsCompleted)yield return null;Assert.AreEqual("READY",client.Status);wait=client.RefreshAsync(hash,new string('f',64));while(!wait.IsCompleted)yield return null;Assert.AreEqual("STALE",client.Status);
   foreach(var test in new[]{(BInterceptDefinitionSnapshot.Version,401,true,""),("old",409,false,""),(BInterceptDefinitionSnapshot.Version,400,false,"?haste=3000")}){var reply=wire.SendAsync(new RunApiHttpRequest("GET","/v3/combat/b-scout-gunner-definition"+test.Item4,null,new[]{new RunApiHttpHeader("X-Combat-B-Scout-Gunner-Version",test.Item1)}),test.Item3?new GameApiAccessToken("lst1.invalid"):session.AccessToken);while(!reply.IsCompleted)yield return null;if(test.Item3)Assert.AreEqual(GameApiAuthenticatedHttpOutcome.AuthRequired,reply.Result.Outcome);else Assert.AreEqual(test.Item2,reply.Result.Response.StatusCode);}
   using var v1=new BScoutGunnerDefinitionClient(wire,()=>session,()=>1);using var v2=new BBoardingEventsDefinitionClient(wire,()=>session,()=>1);wait=v1.RefreshAsync(BScoutGunnerDefinitionSnapshot.ApprovedPacketHash);while(!wait.IsCompleted)yield return null;Assert.AreEqual("READY",v1.Status);wait=v2.RefreshAsync(BBoardingEventsDefinitionSnapshot.ApprovedPacketHash);while(!wait.IsCompleted)yield return null;Assert.AreEqual("READY",v2.Status);
   input["samples"]=JArray.Parse(File.ReadAllText(folder+"/accepted-sources.json"));var sources=new JArray();foreach(var row in (JArray)input["samples"]){var p=(JObject)row;var receipt=BInterceptTimingSnapshot.Read(p,(string)p["sourceHash"],BInterceptSourceTests.Trust(p),BInterceptSourceTests.Defs());sources.Add(receipt.Capture());}
   var sample=(JObject)input["samples"][4];var accepted=BInterceptSourceTests.Read(sample);var b=BInterceptSourceTests.Binding(sample);var paths=BInterceptSourceTests.Paths(b.Composition.Attack);var native=BDirectAttackBasicLoop.CreateInterceptSourceRuntime(b,accepted.OriginalSource,paths,10000);native.Advance("launch",74000000,3700);string shot=(string)native.Capture()["gunnerIntercept"]["projectiles"][0]["id"];native.AcceptInterceptionSource("accepted",shot,accepted);var cache=native.Capture();var restored=BDirectAttackBasicLoop.RestoreInterceptSourceRuntime(cache,BDirectAttackInput.Digest(cache),b,accepted.OriginalSource,paths,10000);native.Advance("contact",2500000,125);restored.Advance("contact",2500000,125);Assert.IsTrue(JToken.DeepEquals(native.Capture(),restored.Capture()));Assert.AreEqual("INTERCEPTED",(string)native.Capture()["gunnerIntercept"]["projectiles"][0]["terminal"]);
   File.WriteAllText(folder+"/request1.json","{}");var deadline=DateTime.UtcNow.AddSeconds(30);while(!File.Exists(folder+"/published1.json")){Assert.Less(DateTime.UtcNow,deadline);yield return null;}wait=client.RefreshAsync(hash);while(!wait.IsCompleted)yield return null;Assert.AreEqual("AUTH_REQUIRED",client.Status);Assert.IsNull(client.Snapshot);Assert.AreEqual(0,adapter.InFlightCount);
   File.WriteAllText(folder+"/actual-unity.json",new JObject{["definitionPacket"]=packet,["sources"]=sources,["nativeCapture"]=native.Capture(),["restoredNativeCapture"]=restored.Capture(),["operatingStarts"]=0,["sourceInputStatus"]="ISOLATED_LOCAL_FIXTURE",["sableNativeProfileAvailable"]=false}.ToString());
  }
 }
}
#endif
