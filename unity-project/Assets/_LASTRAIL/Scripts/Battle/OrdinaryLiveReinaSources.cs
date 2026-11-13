using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 internal sealed partial class OrdinaryLiveCombatSources {
  internal JObject ObserveReina(string command,long tick,JObject prior,Func<string,bool> alive,bool actionEligible){
   BCombatFormation.Identity(command);
   var basic=ObserveBasic("op_reina",tick);var muzzle=Point(basic["source"]["muzzleMm"]);
   var blockers=(JArray)basic["blockers"];var available=(JArray)basic["targets"];
   bool Eligible(JToken t)=>t!=null&&(bool)t["eligible"]&&alive((string)t["id"])&&Math.Abs((long)t["slot"]-(long)basic["source"]["slot"])<=1&&
    Point(t["anchorMm"]).Select((v,i)=>(System.Numerics.BigInteger)(v-muzzle[i])*(v-muzzle[i])).Aggregate(System.Numerics.BigInteger.Zero,(a,b)=>a+b)<=(System.Numerics.BigInteger)24000*24000&&
    !blockers.Any(b=>MiraSweepKernel.Box(muzzle,Point(t["anchorMm"]),Point(b["centerMm"]),Point(b["centerMm"]),Point(b["sizeMm"]),0).HasValue);
   string primary=prior==null?factory.EnemyIds.FirstOrDefault(id=>Eligible(available.SingleOrDefault(t=>(string)t["id"]==id))):(string)prior["primaryTargetId"];
   var target=available.SingleOrDefault(t=>(string)t["id"]==primary);
   string frameId=prior==null?(target==null|| (long)target["slot"]==0?factory.Encounter.IssuedEnemyTrainId:factory.Original.Rail.PhysicalInstanceIds[(int)target["slot"]-1]):(string)prior["frame"]["id"];
   string frameAuthoring;
   if(frameId==factory.Encounter.IssuedEnemyTrainId)frameAuthoring="Enemy";
   else {
    int order=Array.IndexOf(factory.Original.Rail.PhysicalInstanceIds.ToArray(),frameId);
    var car=(definition["source"]["boardingGeometry"]?["cars"] as JArray)?.SingleOrDefault(c=>(int)c["carOrder"]==order);
    Need(order>=0&&car!=null,"Original physical car authoring required");frameAuthoring=(string)car["authoringId"];
   }
   Need(entities.ContainsKey(frameAuthoring)&&(string)entities[frameAuthoring]["role"]=="TRAIN","Original train frame required");
   var origin=Origin(frameAuthoring,FrontAt(tick));
   JArray Local(JToken point)=>new JArray(Point(point).Select((v,i)=>checked(v-origin[i])));
   var result=Context(tick);result["primaryTargetId"]=primary==null?JValue.CreateNull():new JValue(primary);
   result["source"]=new JObject{["id"]="op_reina",["generation"]=basic["source"]["generation"].DeepClone(),["executorId"]="op_reina",["abilityId"]="op_reina.ultimate",["muzzleMm"]=new JArray(muzzle),["actionEligible"]=actionEligible&&alive("op_reina")};
   result["frame"]=new JObject{["id"]=frameId,["generation"]=factory.Attempt.Generation,["alive"]=alive(factory.TrainId),["originMm"]=new JArray(origin),["basis"]=layout[frameAuthoring]["basis"].DeepClone()};
   var rows=new JArray();int orderIndex=0;
   foreach(string id in factory.EnemyIds){var t=available.SingleOrDefault(x=>(string)x["id"]==id);int order=orderIndex++;if(t==null)continue;
    rows.Add(new JObject{["id"]=id,["generation"]=t["generation"].DeepClone(),["battleOrder"]=order,["carDelta"]=(long)t["slot"]-(long)basic["source"]["slot"],["eligible"]=(bool)t["eligible"]&&alive(id),["castLineOfSight"]=Eligible(t),["protected"]=false,["frameId"]=frameId,["localAnchorMm"]=Local(t["anchorMm"]),["worldAnchorMm"]=t["anchorMm"].DeepClone()});
   }
   result["targets"]=rows;result["solidOccluders"]=new JArray(blockers.Select(b=>new JObject{["id"]=b["id"].DeepClone(),["centerMm"]=Local(b["centerMm"]),["sizeMm"]=b["sizeMm"].DeepClone()}));return result;
  }
 }
 public sealed partial class BasicCombatSimulation {
  OrdinaryReinaBarrageSources ordinaryLiveBarrageSources;
  void BindOrdinaryLiveBarrage(){
   if(ordinaryFactory.AugmentOwnership==null)return;
   if(ordinaryFactory.Original.ReinaBarrage==null)throw new NotSupportedException("Original Reina barrage definition required.");
   JObject Observe(string command,long tick,JObject prior){
    if(tick!=Tick||ordinaryNativeRetired)throw new InvalidOperationException("Current live barrage owner required.");
    return ordinaryLiveSources.ObserveReina(command,tick,prior,id=>ordinaryHealth.Hp(id)>0,ordinaryExecutionStarted&&(ordinaryObstacle==null||ordinaryObstacle.Phase=="COMBAT"));
   }
   ordinaryLiveBarrageSources=new OrdinaryReinaBarrageSources((c,t)=>Observe(c,t,null),ordinaryLiveNumbers.ReinaBarrageAttack,ordinaryLiveNumbers.Defense,new OrdinaryLiveSkillLifesteal(ordinaryFactory,ordinaryLiveNumbers.SkillHealingMultiplier),Observe);
   ordinaryBarrage=new OrdinaryReinaBarrageExecution(ordinaryFactory,ordinaryHealth,ordinaryLiveBarrageSources,(int)ordinaryFactory.Original.ReinaBarrage["cost"]);
  }
 }
}
