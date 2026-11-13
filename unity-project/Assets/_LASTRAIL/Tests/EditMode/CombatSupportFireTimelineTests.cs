using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatSupportFireTimelineTests {
 object Create(JObject state){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Battle.CombatSupportFireTimeline")).FirstOrDefault(t=>t!=null);Assert.IsNotNull(type,"Approved support-fire timeline must be connected");return Activator.CreateInstance(type,new object[]{state});}
 static JObject Hud(float seconds,string status="CASTING",bool hit=false){var s=new JObject{["motionSeconds"]=seconds,["outcome"]="Running",["casts"]=new JArray(new JObject{["id"]="fire1",["ability"]="support_fire",["status"]=status,["due"]=10,["windupTicks"]=10,["targets"]=new JArray("target")}),["effectResults"]=new JArray()};if(hit)((JArray)s["effectResults"]).Add(new JObject{["commandId"]="fire1",["abilityId"]="support_fire",["kind"]="DAMAGE",["targetId"]="target",["tick"]=10,["actualResult"]=new JObject{["hpLoss"]=123,["absorbed"]=0}});return s;}
 static JObject Read(object timeline,JObject hud)=>(JObject)timeline.GetType().GetMethod("Read").Invoke(timeline,new object[]{hud,false});
 [Test] public void PreparationFiresThreeVisualRocketsButImpactRequiresActualReceipt(){
  var s=Hud(0);var timeline=Create(s);var at=Read(timeline,Hud(.4f));Assert.Greater((float)at["raise"],.99f);Assert.AreEqual(3,((JArray)at["rockets"]).Count(x=>(float)x>=0&&(float)x<1));Assert.IsFalse((bool)at["impact"]);
  at=Read(timeline,Hud(.5f,"APPLIED",false));Assert.IsFalse((bool)at["impact"]);at=Read(timeline,Hud(.5f,"APPLIED",true));Assert.IsTrue((bool)at["impact"]);Assert.AreEqual("target",(string)at["target"]);
  at=Read(timeline,Hud(1.6f,"APPLIED",true));Assert.IsFalse((bool)at["active"]);
 }
 [Test] public void PauseSameModelTimeIsExactAndCancellationCannotShowAHit(){
  var timeline=Create(Hud(0));var at=Read(timeline,Hud(.36f));Assert.AreEqual(at.ToString(),Read(timeline,Hud(.36f)).ToString());Assert.IsFalse((bool)Read(timeline,Hud(.37f,"CANCELLED_TARGET"))["active"]);
 }
 [Test] public void RestoringAnAppliedHistoryNeverReplaysButLiveCastingReconstructs(){
  var old=Hud(.65f,"APPLIED",true);Assert.IsFalse((bool)Read(Create(old),old)["active"]);var casting=Hud(.36f);Assert.Greater((float)Read(Create(casting),casting)["raise"],.99f);
 }
}
