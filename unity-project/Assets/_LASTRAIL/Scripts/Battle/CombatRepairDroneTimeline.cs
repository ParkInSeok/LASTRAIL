using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
public sealed class CombatRepairDroneTimeline {
 readonly HashSet<string> historical=new HashSet<string>();
 public CombatRepairDroneTimeline(JObject initial){foreach(var c in (JArray)initial["casts"])if((string)c["status"]!="CASTING")historical.Add((string)c["id"]);}
 public JObject Read(JObject hud){
  var off=new JObject{["active"]=false,["travel"]=0f,["weld"]=false,["return"]=0f};if((string)hud["outcome"]!="Running")return off;
  var c=((JArray)hud["casts"]).LastOrDefault(c=>(string)c["ability"]=="emergency_repair"&&!historical.Contains((string)c["id"]));if(c==null||((string)c["status"]!="CASTING"&&(string)c["status"]!="APPLIED"))return off;
  double now=(double)hud["motionSeconds"],due=(long)c["due"]/20d,windup=(long)c["windupTicks"]/20d,age=now-(due-windup);if(age<0||windup<=0||now>=due+1.25)return off;
  var result=((JArray)hud["effectResults"]).FirstOrDefault(r=>(string)r["commandId"]==(string)c["id"]&&(string)r["kind"]=="REPAIR"&&CombatEffectFeedback.Positive((JObject)r));
  return new JObject{["active"]=true,["command"]=c["id"],["travel"]=Math.Max(0,Math.Min(1,age/(windup*.85))),["return"]=Math.Max(0,Math.Min(1,(now-due-.65)/.6)),["weld"]=result!=null&&now>=due&&now<due+.6,["time"]=now,["resolved"]=result!=null};
 }
}
}
