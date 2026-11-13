#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Battle;
namespace LastRail.Tests.EditMode {
 public sealed class BNamedNoBoostComparisonTests {
  static JObject Run(bool intercept){
   var f=BNamedOperatorSourceTests.Native();var g=BOrdinaryGeometryTests.Geometry(f.binding);var source=BInterceptSourceTests.Read(BInterceptSourceTests.Sample()).OriginalSource;
   var native=intercept?f.adapter.CreateRuntime(f.binding,g,10000):BDirectAttackBasicLoop.CreateOrdinaryGeometryRuntime(f.binding,source,g,10000,true);
   var hits=new JArray();var first=new System.Collections.Generic.Dictionary<string,long>();var kills=new System.Collections.Generic.Dictionary<string,long>();var downs=new System.Collections.Generic.Dictionary<string,long>();
   for(int second=0;second<120&&native.Outcome=="RUNNING";second++)foreach(var h in native.Advance("no-boost-"+second,10000000,500).Hits){var a=h.Attack;hits.Add(new JObject{["sourceId"]=a.SourceId,["targetId"]=a.Hp.TargetId,["tick"]=a.Tick,["hpLoss"]=a.Hp.HpLoss,["hpAfter"]=a.Hp.HpAfter});if(a.Hp.HpLoss>0&&!first.ContainsKey(a.Hp.TargetId))first[a.Hp.TargetId]=a.Tick;if(a.Hp.HpAfter==0){if(a.Hp.TargetId=="op_kyle"||a.Hp.TargetId=="op_sable"){if(!downs.ContainsKey(a.Hp.TargetId))downs[a.Hp.TargetId]=a.Tick;}else if(!kills.ContainsKey(a.Hp.TargetId))kills[a.Hp.TargetId]=a.Tick;}}
   var state=native.Capture();string folder="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/NamedOperatorAdapter";File.WriteAllText(folder+"/no-boost-"+(intercept?"intercept-on":"intercept-off")+"-native.json",state.ToString());
   var projectiles=(JArray)state["gunnerIntercept"]["projectiles"];
   return new JObject{["interception"]=intercept?"EXISTING_LOCAL_ACCEPTANCE_OWNER":"DISABLED_UNSUPPLIED_ACCEPTED_RECEIPT",["inputHash"]=f.adapter.Input.OriginalHash,["geometryHash"]=g.Hash,["sourceHash"]=source.SourceHash,["tick"]=native.Tick,["outcome"]=native.Outcome,["actors"]=new JArray(new[]{"op_kyle","op_sable"}.Select(id=>new JObject{["id"]=id,["initialHp"]=id=="op_kyle"?1400:1600,["hpAfter"]=native.Hp(id),["downTick"]=downs.TryGetValue(id,out long down)?new JValue(down):JValue.CreateNull(),["basicHitCount"]=hits.Count(h=>(string)h["sourceId"]==id),["receivedHitCount"]=hits.Count(h=>(string)h["targetId"]==id)})),["interceptedCount"]=projectiles.Count(p=>(string)p["terminal"]=="INTERCEPTED"),["projectileHitCount"]=projectiles.Count(p=>(string)p["terminal"]=="HIT"),["ttkBasis"]="FIRST_ACTUAL_HP_DAMAGE_TO_KILL_TICK;UNFINISHED_OR_UNHIT_NULL_CENSORED",["enemyTtk"]=new JArray(f.adapter.Input.Capture()["entities"].Where(e=>(string)e["kind"]=="LOCAL_ENEMY").Select(e=>{string id=(string)e["id"];return new JObject{["id"]=id,["hpAfter"]=native.Hp(id),["firstDamageTick"]=first.TryGetValue(id,out long start)?new JValue(start):JValue.CreateNull(),["killTick"]=kills.TryGetValue(id,out long end)?new JValue(end):JValue.CreateNull(),["ttkTicks"]=first.ContainsKey(id)&&kills.ContainsKey(id)?new JValue(kills[id]-first[id]):JValue.CreateNull()};})),["hits"]=hits};
  }
  [Test] public void SameSeedActualPermanentStatsComparisonRecordsCensoredOutcomes(){var original=BNamedOperatorSourceTests.Source().Capture();var on=Run(true);var off=Run(false);Assert.AreEqual(on["inputHash"],off["inputHash"]);Assert.AreEqual(on["geometryHash"],off["geometryHash"]);Assert.AreEqual(on["sourceHash"],off["sourceHash"]);Assert.IsTrue(JToken.DeepEquals(original,BNamedOperatorSourceTests.Source().Capture()));Assert.Greater((int)on["interceptedCount"],0);Assert.AreEqual(0,(int)off["interceptedCount"]);var result=new JObject{["version"]="unity.combat-b.named-no-boost-comparison.v1.20261004",["scope"]="BASE_PERMANENT_LOCAL_NOT_BALANCE_PASS",["namedPacketHash"]="0537f57c0ccdba49fe891ccc58b895d2ad8321aca1a96c3dead2cff83b7f198f",["maximumObservationSeconds"]=120,["geometry"]="U27_EXPLICIT_TEST_MODEL_AND_LOCAL_1800MM_BASIC_RANGE;NOT_OPERATING_GEOMETRY",["equipment"]=JValue.CreateNull(),["effectiveStats"]=JValue.CreateNull(),["acceptedServerTiming"]=JValue.CreateNull(),["operatingStart"]=false,["comparison"]=new JArray(on,off)};File.WriteAllText("D:/Project/LASTRAIL/QA/CombatBMigration-20261004/NamedOperatorAdapter/no-boost-comparison.json",result.ToString());}
 }
}
#endif
