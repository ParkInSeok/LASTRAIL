using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Original BASIC execution fact. Immediate attacks have no invented charge, flight, or contact coordinates.
 public sealed class BDirectAttackBasicExecution {
  public string ActionId{get;} public string SourceId{get;} public string TargetId{get;}
  public long Tick{get;} public long Sequence{get;} public long NextDueTick{get;}
  internal BDirectAttackBasicExecution(string action,string source,string target,long tick,long sequence,long next){ActionId=action;SourceId=source;TargetId=target;Tick=tick;Sequence=sequence;NextDueTick=next;}
  public JObject Capture()=>new JObject{["version"]="unity.combat-b.basic-execution-observation.v1.20261017",["phase"]="EXECUTED_INSTANT",["actionId"]=ActionId,["sourceId"]=SourceId,["targetId"]=TargetId,["tick"]=Tick,["sequence"]=Sequence,["nextDueTick"]=NextDueTick,["projectileId"]=JValue.CreateNull(),["worldPoint"]=JValue.CreateNull(),["worldPointStatus"]="NOT_PROVIDED_BY_THIS_EXECUTION_EVENT"};
 }
}
