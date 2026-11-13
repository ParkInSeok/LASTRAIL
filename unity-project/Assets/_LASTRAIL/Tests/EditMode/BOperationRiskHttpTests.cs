#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public sealed class BOperationRiskHttpTests {
  [UnityTest] public IEnumerator SignedFifteenProjectionsUseExactOriginalPipelineAndNativeReplay(){
   string raw=Environment.GetEnvironmentVariable("LASTRAIL_OPERATION_RISK_PRIVATE");Environment.SetEnvironmentVariable("LASTRAIL_OPERATION_RISK_PRIVATE",null);if(string.IsNullOrEmpty(raw))Assert.Ignore("Owned standalone signed risk supervisor required");var bundle=JObject.Parse(raw);raw=null;var outputs=new JArray();
   int[] hp={700,875,1085,1330,1610};int[,] damage={{30,33,36},{34,37,41},{39,42,46},{45,49,54},{52,57,63}};int index=0;
   foreach(var row in (JArray)bundle["rows"]){
    using var transport=new BOperationRiskHttpTransport((JObject)row["intake"]);var task=transport.ReadAsync((JObject)row["binding"],(string)row["hash"]);while(!task.IsCompleted)yield return null;Assert.IsFalse(task.IsFaulted,task.Exception?.ToString());var packet=task.Result;var source=BOperationRiskSource.Read(packet,(JObject)row["binding"],(string)row["hash"]);Assert.AreEqual(hp[index/3],source.BaseHpAllocationMagnitude());var action=BOperationRiskTests.Action(packet,BOperationRiskTests.Pipeline());var budget=source.ResolveOriginalAction(action);Assert.AreEqual(damage[index/3,index%3],budget.ToVitalsMagnitude());
    var owner=BOperationRiskLocalConsumer.CreateLocalValidation(source,BOperationRiskTests.Formation(),BOperationRiskTests.HpBindings(),10000);var receipt=owner.ApplyOriginalAction("local-risk-hit","car0",1,action);Assert.AreEqual(1000-budget.ToVitalsMagnitude(),owner.Hp("car0"));Assert.AreEqual(5000-budget.ToVitalsMagnitude()*30/100,owner.Hp("train"));var capture=owner.Capture();Assert.AreSame(receipt,owner.ApplyOriginalAction("local-risk-hit","car0",1,action));var restored=BOperationRiskLocalConsumer.RestoreLocalValidation(capture,source,BOperationRiskTests.Formation(),BOperationRiskTests.HpBindings(),10000);Assert.IsTrue(JToken.DeepEquals(capture,restored.Capture()));Assert.AreEqual(0,transport.InFlightCount);
    outputs.Add(new JObject{["packet"]=packet,["budget"]=budget.Snapshot(),["native"]=capture});index++;
   }
   Assert.AreEqual(15,index);
   var first=(JObject)bundle["rows"][0];using(var foreign=new BOperationRiskHttpTransport((JObject)bundle["foreignIntake"])){var task=foreign.ReadAsync((JObject)first["binding"],(string)first["hash"]);while(!task.IsCompleted)yield return null;Assert.IsTrue(task.IsFaulted);Assert.AreEqual(0,foreign.InFlightCount);}
   using(var cancelled=new BOperationRiskHttpTransport((JObject)first["intake"])){var stop=new System.Threading.CancellationToken(true);var task=cancelled.ReadAsync((JObject)first["binding"],(string)first["hash"],stop);while(!task.IsCompleted)yield return null;Assert.IsTrue(task.IsCanceled);Assert.AreEqual(0,cancelled.InFlightCount);}
   File.WriteAllText((string)bundle["folder"]+"/actual-unity.json",new JObject{["localOnlyNotMission"]=true,["hpAllocationNotSpawn"]=true,["scoutMapping"]=false,["actualSignedGet"]=true,["rows"]=outputs}.ToString());
  }
 }
}
#endif
