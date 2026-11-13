using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  NyxZoneDispatch ordinaryNyxDispatch;
  internal static string NyxCastBlocker(bool ultimate,long tick,long activeReadyAt,bool busy,bool alive,int charge,int cost){
   if(!alive)return "REJECTED_DOWN";if(busy)return "REJECTED_BUSY";if(!ultimate&&activeReadyAt>tick)return "REJECTED_COOLDOWN";if(ultimate&&charge<cost)return "REJECTED_CHARGE";return null;
  }
  string RequestOrdinaryNyx(string commandId,string abilityId,bool record){
   if(ordinaryNyxDispatch==null||!IsExecutionReady||ordinaryObstacle!=null&&ordinaryObstacle.Phase!="COMBAT")return "REJECTED_PREPARATION";
   if(ordinaryNativeRetired||clock.IsPaused||Outcome!=BasicCombatOutcome.Running)return "REJECTED_STOPPED";
   bool ultimate=abilityId=="op_nyx.ultimate";var ability=ordinaryFactory.Original.Actors.Single(a=>a.OperatorId=="op_nyx").Ability(abilityId);
   int cost=ultimate?(int)ability["cost"]:0;string blocked=NyxCastBlocker(ultimate,Tick,ordinaryReady["op_nyx"],OrdinarySkillBusy("op_nyx")||OrdinaryBasicBusy("op_nyx"),ordinaryNyx.CasterAlive,ordinaryCharge["op_nyx"],cost);if(blocked!=null)return blocked;
   if(ordinaryActions.Count>=10000)throw new InvalidOperationException("Ordinary action limit reached.");
   if(!ordinaryNyxDispatch.Prepare(commandId,ultimate,Tick))return "REJECTED_TARGET";
   long T(long ms)=>checked((ms+ordinaryFactory.Step/10000-1)/(ordinaryFactory.Step/10000));long due=checked(Tick+T((long)ability["windupMs"]));
   if(record)RecordOrdinary(new JObject{["kind"]="SKILL",["id"]=commandId,["actor"]="op_nyx",["ability"]=abilityId});
   ordinaryCharge["op_nyx"]-=cost;if(!ultimate)ordinaryReady["op_nyx"]=checked(Tick+T((long)ability["cooldownMs"]));
   ordinaryActions.Add(new JObject{["id"]=commandId,["actor"]="op_nyx",["ability"]=abilityId,["kind"]=ultimate?"ULTIMATE":"ACTIVE",["nyxZone"]=true,["status"]="CASTING",["target"]=null,["targets"]=new JArray(),["windupTicks"]=due-Tick,["due"]=due,["recoverAt"]=checked(due+T((long)ability["recoveryMs"])),["consumedCharge"]=cost});return "CASTING";
  }
  void StepOrdinaryNyx(){
   if(ordinaryNyxDispatch==null)return;bool running=Outcome==BasicCombatOutcome.Running&&(ordinaryObstacle==null||ordinaryObstacle.Phase=="COMBAT");
   foreach(var action in ordinaryActions.OfType<JObject>().Where(a=>(bool?)a["nyxZone"]==true&&(string)a["status"]=="CASTING").ToArray()){
    if(!running||!ordinaryNyx.CasterAlive){ordinaryNyxDispatch.Cancel();action["status"]="CANCELLED_DOWN_OR_NODE_END";continue;}
    if(Tick<(long)action["due"])continue;if(Tick!=(long)action["due"])throw new InvalidOperationException("Original Nyx commit observation missed.");
    action["status"]=ordinaryNyxDispatch.Commit((string)action["id"],Tick)?"APPLIED":"CANCELLED_CREATION";
   }
   foreach(var receipt in ordinaryNyxDispatch.Step(Tick,running)){
    var action=(JObject)ordinaryActions.Single(a=>(string)a["id"]==ordinaryNyxDispatch.ActiveCommandId);
    RecordOrdinaryEffect(action,receipt.TargetId,"DAMAGE",new JObject{["hpLoss"]=receipt.HpLoss,["absorbed"]=receipt.Absorbed},"NYX_ZONE",ordinaryNyx.PulseCount,OrdinaryHealthReceipt(receipt));
   }
  }
 }
}
