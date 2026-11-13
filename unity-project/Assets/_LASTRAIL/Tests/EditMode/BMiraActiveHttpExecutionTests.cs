#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BMiraActiveHttpExecutionTests {
  [UnityTest] public IEnumerator SignedActualMiraSourcesRanksOneThroughFiveExecuteAndRestoreNativeHp(){
   var raw=Environment.GetEnvironmentVariable("LASTRAIL_MIRA_ACTIVE_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_MIRA_ACTIVE_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned private supplier supervisor required");var bundle=JObject.Parse(raw);raw=null;var observed=new JArray();
   foreach(var item in (JArray)bundle["sources"]){using var transport=new BActiveProjectileSourceHttpTransport((JObject)item["intake"]);var read=transport.ReadAsync((string)item["trustedHash"],BMiraActiveExecutionTests.Definitions());while(!read.IsCompleted)yield return null;Assert.IsFalse(read.IsFaulted,"Authenticated Mira source exchange failed");Assert.IsFalse(read.IsCanceled);var p=read.Result;var owner=BMiraActiveExecutionTests.Fresh(p);var acceptance=owner.RequestActive("actual-http-cast");Assert.AreEqual("ACCEPTED",(string)acceptance["requestStatus"]);long ready=(long)acceptance["readyTick"];Assert.AreEqual((long)item["expectedReady"],ready);
    owner.Advance("before",4190000,419);Assert.AreEqual(700,owner.Hp("enemy"));var cache=owner.Capture();var restored=BDirectAttackBasicLoop.RestoreActiveValidation(cache,p,(string)item["trustedHash"],BMiraActiveExecutionTests.Definitions(),10000);owner.Advance("tail",10010000,1001);restored.Advance("tail",10010000,1001);Assert.IsTrue(JToken.DeepEquals(owner.Capture(),restored.Capture()));Assert.AreEqual(622,owner.Hp("enemy"));var shots=(JArray)owner.Capture()["active"]["shots"];Assert.AreEqual(6,shots.Count);Assert.IsTrue(shots.All(x=>(string)x["status"]=="RESOLVED_HIT"));Assert.IsFalse((bool)p["startReady"]);observed.Add(new JObject{["rank"]=item["rank"].DeepClone(),["packetHash"]=p["packetHash"].DeepClone(),["ready"]=ready,["hp"]=owner.Hp("enemy"),["active"]=owner.Capture()["active"].DeepClone(),["capture419"]=cache,["capture1420"]=owner.Capture()});
   }
   File.WriteAllText((string)bundle["observationPath"],new JObject{["version"]="combat.b-active-projectile-source.v1.20261003",["actualSignedUnityHttp"]=true,["sourcePacketsMutated"]=false,["ranks"]=observed}.ToString());bundle=null;
  }
 }
}
#endif
