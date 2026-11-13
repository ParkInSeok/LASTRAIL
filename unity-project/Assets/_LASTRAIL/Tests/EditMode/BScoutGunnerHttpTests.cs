#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BScoutGunnerHttpTests {
  [UnityTest] public IEnumerator ActualAuthenticatedDefinitionAndOriginalSyntheticSourcesStayMetadataOnly(){
   string raw=Environment.GetEnvironmentVariable("LASTRAIL_SCOUT_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_SCOUT_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned isolated supervisor required");var b=GameApiJson.ReadObject(raw);raw=null;string folder=(string)b["folder"];var s=(JObject)b["session"];var owner=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken((string)s["token"]),DateTime.Parse((string)s["expiresAt"]).ToUniversalTime(),new GameApiRefreshToken("unused-local"),DateTime.UtcNow.AddMinutes(10));using var adapter=new UnityWebRequestRunApiHttpAdapter((string)b["origin"],5);var wire=new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter));using var client=new BScoutGunnerDefinitionClient(wire,()=>owner,()=>1);string hash=(string)b["expectedPacketHash"];var task=client.RefreshAsync(hash);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("READY",client.Status);var packet=client.Snapshot.Capture();client.RestoreVerified(packet,hash);Assert.IsFalse(client.Snapshot.CanStart);Assert.IsFalse(client.Snapshot.RuntimeSupported);
   var nodeOwner=new BScoutGunnerNodeOwner();var sources=new JArray();for(int i=0;i<3;i++){nodeOwner.RestoreVerified((JObject)b["samples"][i],(JObject)b["trusts"][i]);Assert.IsFalse(nodeOwner.Snapshot.CanStart);Assert.IsFalse(nodeOwner.Snapshot.RuntimeSupported);sources.Add(nodeOwner.Snapshot.Capture());}nodeOwner.Invalidate();Assert.IsNull(nodeOwner.Snapshot);
   var timings=new JArray();foreach(string risk in new[]{"I","II","III","IV","V"})foreach(int depth in new[]{1,5,9})foreach(int step in new[]{1000,3000,10000})foreach(int group in new[]{0,1,2,8}){var t=BScoutGunnerTiming.EntryAtClock(risk,depth,step,group);timings.Add(new JObject{["risk"]=risk,["depth"]=depth,["stepUs"]=step,["group"]=group,["intervalTicks"]=t.IntervalTicks.ToString(),["intervalUs"]=t.IntervalMicroseconds.ToString(),["entryNotBeforeTicks"]=t.EntryNotBeforeTicks.ToString(),["entryNotBeforeUs"]=t.EntryNotBeforeMicroseconds.ToString()});}
   task=client.RefreshAsync(hash,new string('f',64));while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("STALE",client.Status);Assert.IsTrue(JToken.DeepEquals(packet,client.Snapshot.Capture()));
   foreach(var path in new[]{"/v1/combat/b-scout-gunner-definition","/v1/combat/b-scout-gunner-definition?risk=V"}){var pending=wire.SendAsync(new RunApiHttpRequest("GET",path,null,new[]{new RunApiHttpHeader("X-Combat-B-Scout-Gunner-Version",path.Contains("?")?BScoutGunnerDefinitionSnapshot.Version:"old")}),owner.AccessToken);while(!pending.IsCompleted)yield return null;Assert.IsFalse(pending.IsFaulted);Assert.AreEqual(path.Contains("?")?400:409,pending.Result.Response.StatusCode);}
   File.WriteAllText(folder+"/request1.json","{\"stage\":1}");var deadline=DateTime.UtcNow.AddSeconds(30);while(!File.Exists(folder+"/published1.json")){Assert.Less(DateTime.UtcNow,deadline);yield return null;}task=client.RefreshAsync(hash);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("AUTH_REQUIRED",client.Status);Assert.IsNull(client.Snapshot);Assert.AreEqual(0,adapter.InFlightCount);b=null;
   File.WriteAllText(folder+"/actual-unity.json",new JObject{["definitionPacket"]=packet,["originalSources"]=sources,["clockTimings"]=timings,["revokedSessionStatus"]=client.Status,["operatingStarts"]=0,["generatedNewReceipts"]=0,["spawn"]=false,["geometry"]=false}.ToString());
  }
 }
}
#endif
