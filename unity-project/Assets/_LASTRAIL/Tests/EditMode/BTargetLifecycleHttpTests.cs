#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BTargetLifecycleHttpTests {
  static JObject Restore(BDirectAttackBasicLoop owner,JObject p,string hash){var c=owner.Capture();var r=BDirectAttackBasicLoop.RestoreLifecycleValidation(c,p,hash,BMiraActiveExecutionTests.Definitions(),10000);Assert.IsTrue(JToken.DeepEquals(c,r.Capture()));return c;}
  static Task<JObject> Read(object transport,string hash,JObject original){return (Task<JObject>)transport.GetType().GetMethod("ReadAsync").Invoke(transport,new object[]{hash,original,default(System.Threading.CancellationToken)});}
  [UnityTest] public IEnumerator SignedSameRunProducerAndUnityGetExecuteOldMissManualAndAutoWithFullRestore(){
   var raw=Environment.GetEnvironmentVariable("LASTRAIL_TARGET_LIFECYCLE_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_TARGET_LIFECYCLE_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned private lifecycle supervisor required");var bundle=JObject.Parse(raw);raw=null;string folder=(string)bundle["folder"],trusted=(string)bundle["trustedHash"];var original=(JObject)bundle["original"].DeepClone();var type=typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.BTargetLifecycleHttpTransport");Assert.NotNull(type);
   using var transport=(IDisposable)Activator.CreateInstance(type,new object[]{bundle["intake"]});bundle=null;var read=Read(transport,trusted,original);while(!read.IsCompleted)yield return null;Assert.IsFalse(read.IsFaulted);var p=read.Result;var owner=BDirectAttackBasicLoop.CreateLifecycleValidation(p,trusted,BMiraActiveExecutionTests.Definitions(),10000);owner.SetActiveAuto("off",false);owner.RequestActive("old");owner.Advance("old-emit",3000000,300);var captures=new JArray();var wires=new JArray();
   for(int stage=1;stage<=2;stage++){
    var input=owner.PrepareTargetLifetime("actual-lifetime-"+stage);File.WriteAllText(folder+"/request"+stage+".json",input.ToString());var deadline=DateTime.UtcNow.AddSeconds(30);while(!File.Exists(folder+"/published"+stage+".json")){Assert.Less(DateTime.UtcNow,deadline,"Owned producer did not publish");yield return null;}
    var actual=JObject.Parse(File.ReadAllText(folder+"/published"+stage+".json"));trusted=(string)actual["trustedHash"];read=Read(transport,trusted,original);while(!read.IsCompleted)yield return null;Assert.IsFalse(read.IsFaulted);p=read.Result;Assert.IsTrue(JToken.DeepEquals(actual["packet"],p));long priorReady=(long)owner.Capture()["active"]["readyTick"];owner.RecordTargetLifetime(input,p,trusted);Assert.AreEqual(700,owner.Hp("enemy"));Assert.AreEqual(priorReady,(long)owner.Capture()["active"]["readyTick"]);captures.Add(Restore(owner,p,trusted));var duplicate=owner.Capture();owner.RecordTargetLifetime(input,p,trusted);Assert.IsTrue(JToken.DeepEquals(duplicate,owner.Capture()));
    if(stage==1){owner.Advance("old-impact",1200000,120);Assert.AreEqual("RESOLVED_MISS",(string)owner.Capture()["active"]["shots"][0]["status"]);wires.Add(owner.Capture()["active"].DeepClone());}
    long before=priorReady-owner.Tick-1;owner.Advance("ready-minus-one-"+stage,before*10000,(int)before);Assert.AreEqual("REJECTED_COOLDOWN",(string)owner.RequestActive("early-"+stage)["requestStatus"]);
    if(stage==2)owner.SetActiveAuto("on",true);owner.Advance("ready-"+stage,10000,1);if(stage==1)Assert.AreEqual("ACCEPTED",(string)owner.RequestActive("new-manual")["requestStatus"]);
    int hpBefore=owner.Hp("enemy");captures.Add(Restore(owner,p,trusted));owner.Advance("emit-"+stage,3000000,300);captures.Add(Restore(owner,p,trusted));owner.Advance("impact-"+stage,1200000,120);captures.Add(Restore(owner,p,trusted));owner.Advance("burst-"+stage,10000000,1000);Assert.AreEqual(hpBefore-78,owner.Hp("enemy"));var active=owner.Capture()["active"];Assert.AreEqual(6,((JArray)active["shots"]).Count);Assert.IsTrue(((JArray)active["shots"]).All(s=>(string)s["status"]=="RESOLVED_HIT"));wires.Add(active.DeepClone());captures.Add(Restore(owner,p,trusted));
   }
   File.WriteAllText(folder+"/actual-unity.json",new JObject{["actualSignedUnityGet"]=true,["sameRun"]=true,["projectionOnly"]=false,["captures"]=captures,["wires"]=wires,["final"]=owner.Capture()}.ToString());
  }
 }
}
#endif
