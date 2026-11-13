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
 public sealed class RunWaveManifestHttpTests {
  [UnityTest] public IEnumerator RealBearerReadPreservesOriginalPinAcrossWithdrawalTerminalAndRevocation(){
   var raw=Environment.GetEnvironmentVariable("LASTRAIL_WAVE_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_WAVE_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned isolated supervisor required");var b=GameApiJson.ReadObject(raw);raw=null;var s=(JObject)b["session"];var owner=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken((string)s["token"]),DateTime.Parse((string)s["expiresAt"]).ToUniversalTime(),new GameApiRefreshToken("unused-local"),DateTime.UtcNow.AddMinutes(10));var folder=(string)b["folder"];using var adapter=new UnityWebRequestRunApiHttpAdapter((string)b["origin"],5);using var client=new RunWaveManifestClient(new GameApiAuthenticatedHttpTransport(new UnityWebRequestRunApiHttpTransport(adapter)),()=>owner,()=>1);var trust=(JObject)b["trust"];b=null;var task=client.RefreshAsync(trust);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("READY",client.Status);var original=client.Snapshot.Capture();var outputs=new JArray(original);var oldHash=(string)trust["packetHash"];
   for(int stage=1;stage<=3;stage++){
    File.WriteAllText(folder+"/request"+stage+".json",new JObject{["stage"]=stage}.ToString());var deadline=DateTime.UtcNow.AddSeconds(30);while(!File.Exists(folder+"/published"+stage+".json")){Assert.Less(DateTime.UtcNow,deadline);yield return null;}var published=JObject.Parse(File.ReadAllText(folder+"/published"+stage+".json"));trust=(JObject)published["trust"];
    if(stage==2){task=client.RefreshAsync(trust,oldHash);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);Assert.AreEqual("STALE",client.Status);Assert.IsTrue(JToken.DeepEquals(original,client.Snapshot.Capture()));}
    task=client.RefreshAsync(trust,(string)trust["packetHash"]);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted);
    if(stage<3){Assert.AreEqual("READY",client.Status);Assert.IsTrue(JToken.DeepEquals(published["packet"],client.Snapshot.Capture()));Assert.IsTrue(JToken.DeepEquals(original["manifest"],client.Snapshot.Capture()["manifest"]));client.RestoreVerified(client.Snapshot.Capture(),trust);Assert.IsFalse(client.Snapshot.CanStart);Assert.IsFalse(client.Snapshot.FullCombatSupported);outputs.Add(client.Snapshot.Capture());}else{Assert.AreEqual("AUTH_REQUIRED",client.Status);Assert.IsNull(client.Snapshot);outputs.Add(new JObject{["status"]=client.Status,["errorCode"]=client.ErrorCode});}
   }
   Assert.AreEqual(0,adapter.InFlightCount);File.WriteAllText(folder+"/actual-unity.json",new JObject{["packetsAndErrors"]=outputs,["operatingStarts"]=0,["spawn"]=false,["geometry"]=false}.ToString());
  }
 }
}
#endif
