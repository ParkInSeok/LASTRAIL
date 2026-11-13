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
 public sealed class BBoardingEventsHttpTests {
  [UnityTest] public IEnumerator ActualSignedV2DefinitionAndOriginalSourcesBindNativeWithoutOperatingStart(){
   string raw=Environment.GetEnvironmentVariable("LASTRAIL_BOARDING_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_BOARDING_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned isolated supervisor required");var input=GameApiJson.ReadObject(raw);raw=null;string folder=(string)input["folder"];var auth=(JObject)input["session"];var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken((string)auth["token"]),DateTime.Parse((string)auth["expiresAt"]).ToUniversalTime(),new GameApiRefreshToken("unused-local"),DateTime.UtcNow.AddMinutes(10));using var adapter=new UnityWebRequestRunApiHttpAdapter((string)input["origin"],5);var wire=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter));using var client=new BBoardingEventsDefinitionClient(wire,()=>session,()=>1);string hash=(string)input["expectedPacketHash"];
   var wait=client.RefreshAsync(hash);while(!wait.IsCompleted)yield return null;Assert.IsFalse(wait.IsFaulted);Assert.AreEqual("READY",client.Status);var packet=client.Snapshot.Capture();client.RestoreVerified(packet,hash);wait=client.RefreshAsync(hash,hash);while(!wait.IsCompleted)yield return null;Assert.IsFalse(wait.IsFaulted);Assert.AreEqual("READY",client.Status);wait=client.RefreshAsync(hash,new string('f',64));while(!wait.IsCompleted)yield return null;Assert.IsFalse(wait.IsFaulted);Assert.AreEqual("STALE",client.Status);Assert.IsTrue(JToken.DeepEquals(packet,client.Snapshot.Capture()));
   foreach(var test in new[]{("/v2/combat/b-scout-gunner-definition",BBoardingEventsDefinitionSnapshot.Version,401,true),("/v2/combat/b-scout-gunner-definition","old",409,false),("/v2/combat/b-scout-gunner-definition?risk=V",BBoardingEventsDefinitionSnapshot.Version,400,false)}){var reply=wire.SendAsync(new RunApiHttpRequest("GET",test.Item1,null,new[]{new RunApiHttpHeader("X-Combat-B-Scout-Gunner-Version",test.Item2)}),test.Item4?new GameApiAccessToken("lst1.invalid"):session.AccessToken);while(!reply.IsCompleted)yield return null;Assert.IsFalse(reply.IsFaulted);if(test.Item4)Assert.AreEqual(GameApiAuthenticatedHttpOutcome.AuthRequired,reply.Result.Outcome);else Assert.AreEqual(test.Item3,reply.Result.Response.StatusCode);}
   using var legacy=new BScoutGunnerDefinitionClient(wire,()=>session,()=>1);wait=legacy.RefreshAsync(BScoutGunnerDefinitionSnapshot.ApprovedPacketHash);while(!wait.IsCompleted)yield return null;Assert.IsFalse(wait.IsFaulted);Assert.AreEqual("READY",legacy.Status);Assert.IsTrue(JToken.DeepEquals(packet["definition"]["originalDefinition"],legacy.Snapshot.Capture()["definition"]));
   var sources=new JArray();var nativeCaptures=new JArray();for(int i=0;i<3;i++){var p=(JObject)input["samples"][i];var source=BBoardingEventsNodeSnapshot.Read(p,(string)p["sourceHash"],(JObject)input["trusts"][i]);sources.Add(source.Capture());var binding=BWaveBoardingRuntimeTests.WaveBinding(source.OriginalSource);var native=BDirectAttackBasicLoop.CreateBoardingEventsRuntime(binding,source,null,10000);native.Advance("actual-clock",6000000,600);Assert.IsFalse(native.OrdinaryRuntimeGeometrySupported);Assert.AreEqual(source.SourceHash,(string)native.Capture()["boardingEventsSourceHash"]);Assert.IsTrue(JToken.DeepEquals(packet["definition"],native.Capture()["boardingEventsSource"]["definition"]));nativeCaptures.Add(native.Capture());}
   File.WriteAllText(folder+"/request1.json","{\"stage\":1}");var deadline=DateTime.UtcNow.AddSeconds(30);while(!File.Exists(folder+"/published1.json")){Assert.Less(DateTime.UtcNow,deadline);yield return null;}wait=client.RefreshAsync(hash);while(!wait.IsCompleted)yield return null;Assert.IsFalse(wait.IsFaulted);Assert.AreEqual("AUTH_REQUIRED",client.Status);Assert.IsNull(client.Snapshot);Assert.AreEqual(0,adapter.InFlightCount);input=null;
   File.WriteAllText(folder+"/actual-unity.json",new JObject{["definitionPacket"]=packet,["originalV1Packet"]=legacy.Snapshot.Capture(),["sources"]=sources,["nativeCaptures"]=nativeCaptures,["revokedSessionStatus"]=client.Status,["generatedNewReceipts"]=0,["operatingStarts"]=0,["actualGeometry"]=false}.ToString());
  }
 }
}
#endif
