using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  internal bool CollectBarragePresentationEvents;
  internal readonly System.Collections.Generic.List<JObject> BarragePresentationEvents=new System.Collections.Generic.List<JObject>();
  bool BarrageCommitBoundary=>CollectBarragePresentationEvents&&BarragePresentationEvents.Any(e=>(string)e["kind"]=="COMMIT");
  string RequestOrdinaryBarrage(string commandId,bool record){
   if(ordinaryBarrage==null||ordinaryLiveBarrageSources!=null&&(ordinaryObstacle?.Phase!="COMBAT"||!ordinaryExecutionStarted))return "REJECTED_PREPARATION";
   if(ordinaryNativeRetired||clock.IsPaused||Outcome!=BasicCombatOutcome.Running)return "REJECTED_STOPPED";
   if(OrdinarySkillBusy("op_reina")||OrdinaryBasicBusy("op_reina")||(ordinaryBoarding?.PartyMoving("op_reina")??false))return "REJECTED_BUSY";
   if(ordinaryActions.Count>=10000)throw new InvalidOperationException("Ordinary action limit reached.");
   ordinaryFactory.SceneMotion?.Invoke(Tick,ordinaryFront);
   string result=ordinaryBarrage.Request(commandId,Tick,ordinaryCharge["op_reina"]);
   if(result!="ACCEPTED")return "REJECTED_"+result;
   var pending=ordinaryBarrage.Capture()["actions"].Single(a=>(string)a["commandId"]==commandId);
   if(record)RecordOrdinary(new JObject{["kind"]="SKILL",["id"]=commandId,["actor"]="op_reina",["ability"]="op_reina.ultimate"});
   ordinaryActions.Add(new JObject{["id"]=commandId,["actor"]="op_reina",["ability"]="op_reina.ultimate",["kind"]="ULTIMATE",["reinaBarrage"]=true,["status"]="CASTING",["target"]=pending["primaryId"].DeepClone(),["targets"]=new JArray(pending["primaryId"].DeepClone()),["due"]=pending["commitTick"].DeepClone(),["windupTicks"]=(long)pending["commitTick"]-Tick,["recoverAt"]=0});
   return "CASTING";
  }
  void StepOrdinaryBarrage(){
   if(ordinaryBarrage==null)return;
   long Sequence(string command){int index=ordinaryActions.ToList().FindIndex(a=>(string)a["id"]==command&&(bool?)a["reinaBarrage"]==true);if(index<0)throw new InvalidOperationException("Original barrage command missing.");return index+1;}
   foreach(var result in ordinaryBarrage.Step(Tick,ordinaryCharge["op_reina"],Sequence)){
    var action=(JObject)ordinaryActions.Single(a=>(string)a["id"]==(string)result["commandId"]);
    if((string)result["kind"]=="COMMIT"){
     int cost=(int)result["cost"];if(cost!=(int)ordinaryFactory.Original.ReinaBarrage["cost"]||ordinaryCharge["op_reina"]<cost||(string)action["status"]!="CASTING")throw new InvalidOperationException("Barrage charge commit conflict.");
     ordinaryCharge["op_reina"]-=cost;action["status"]="APPLIED";action["consumedCharge"]=cost;action["recoverAt"]=ordinaryBarrage.ReadActor("op_reina")["recoveryEnd"].DeepClone();
     var committed=ordinaryBarrage.Capture()["actions"].Single(a=>(string)a["commandId"]==(string)result["commandId"]);
     action["targets"]=new JArray(committed["targets"].Select(t=>t["id"].DeepClone()));
     action["barrageCommit"]=new JObject(new[]{"frame","centerMm","targets","attack","schedule"}.Select(k=>new JProperty(k,committed[k].DeepClone())));
     QueueBarragePresentation(result);
    }else if((string)result["kind"]=="IMPACT"||(string)result["kind"]=="CANCEL"){
     if((string)action["status"]!="APPLIED")throw new InvalidOperationException("Uncommitted barrage impact.");
     QueueBarragePresentation(result);
    }else if((string)result["kind"]=="DAMAGE"){
     if((string)action["status"]!="APPLIED")throw new InvalidOperationException("Uncommitted barrage damage.");
     var row=(JObject)result.DeepClone();row.Remove("receipt");row["reinaBarrage"]=true;row["journalSequence"]=ordinaryEffects.Count+1;row["configurationSha256"]=Digest(ConfigurationVersion);row["effectIndex"]=0;
     ValidateActualResult("DAMAGE",(JObject)row["actualResult"]);ordinaryEffects.Add(row);
    }else throw new InvalidOperationException("Unknown barrage event.");
   }
   foreach(var action in ordinaryActions.OfType<JObject>().Where(a=>(bool?)a["reinaBarrage"]==true&&(string)a["status"]=="CASTING"&&!ordinaryBarrage.IsPreparing((string)a["id"]))){action["status"]="CANCELLED";action["recoverAt"]=Tick;}
  }
  void QueueBarragePresentation(JObject result){
   if(!CollectBarragePresentationEvents)return;
   var row=(JObject)result.DeepClone();row["combatSeconds"]=(long)row["tick"]*(double)ordinaryFactory.Step/TimeSpan.TicksPerSecond;
   if((string)row["kind"]=="IMPACT")row["frontArcMm"]=ordinaryFront;
   BarragePresentationEvents.Add(row);
  }
 }
}
