using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Render-only timeline. It reads fixed simulation time and actual result receipts;
// it never accepts a skill, applies damage, advances time, or changes a target.
public sealed class CombatSupportFireTimeline {
 readonly HashSet<string> historical=new HashSet<string>(StringComparer.Ordinal);
 public CombatSupportFireTimeline(JObject initial){foreach(var c in (JArray)initial["casts"])if((string)c["status"]!="CASTING")historical.Add((string)c["id"]);}
 static float Clamp(float p)=>Math.Max(0,Math.Min(1,p));
 static float Smooth(float p){p=Clamp(p);return p*p*(3-2*p);}
 static JObject Idle()=>new JObject{["active"]=false,["raise"]=0f,["hatch"]=0f,["recoil"]=0f,["impact"]=false,["impactAge"]=0f,["target"]=null,["command"]=null,["rockets"]=new JArray(-1f,-1f,-1f)};
 public JObject Read(JObject hud,bool simplified){
  if((string)hud["outcome"]!="Running")return Idle();
  var cast=((JArray)hud["casts"]).LastOrDefault(c=>(string)c["ability"]=="support_fire"&&!historical.Contains((string)c["id"]));
  if(cast==null||((string)cast["status"]!="CASTING"&&(string)cast["status"]!="APPLIED"))return Idle();
  float now=(float)hud["motionSeconds"],due=(float)((long)cast["due"]/20d),windup=(float)((long)cast["windupTicks"]/20d),age=now-(due-windup);
  if(windup<=0||age<0||now>=due+1)return Idle();
  var hit=((JArray)hud["effectResults"]).FirstOrDefault(r=>(string)r["commandId"]==(string)cast["id"]&&(string)r["abilityId"]=="support_fire"&&(string)r["kind"]=="DAMAGE"&&CombatEffectFeedback.Positive((JObject)r));
  float hitAge=hit==null?-1:now-(float)((long)hit["tick"]/20d);
  float retract=1-Smooth((now-due-.55f)/.4f);
  float raise=Smooth((age/windup-.2f)/.32f)*retract,hatch=Smooth(age/windup/.24f)*(1-Smooth((now-due-.83f)/.17f));
  var rockets=new JArray();float recoil=0;
  for(int i=0;i<3;i++){float launch=windup*(.54f+.12f*i),p=(age-launch)/(windup-launch);rockets.Add(!simplified&&p>=0&&p<1?p:-1f);float kick=(age-launch)/.07f;if(kick>=0&&kick<1)recoil=Math.Max(recoil,1-kick);}
  return new JObject{["active"]=true,["raise"]=raise,["hatch"]=hatch,["recoil"]=recoil,["impact"]=hitAge>=0&&hitAge<(simplified?.2f:.65f),["impactAge"]=Math.Max(0,hitAge),["target"]=hit?["targetId"]??cast["targets"]?.FirstOrDefault(),["command"]=cast["id"],["rockets"]=rockets};
 }
}
}
