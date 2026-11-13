using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Trusted server snapshot replica. Never advances combat, applies HP, or awards score locally.
 public sealed class WorldBossRailSnapshot {
  public const string Version="world-boss.rail-action.v3.20261002";
  readonly JObject original,state;
  public JObject Capture()=>(JObject)state.DeepClone();
  public JObject Definition=>(JObject)original.DeepClone();
  public long Revision=>(long)state["revision"];
  public long Now=>(long)state["now"];
  public string AttemptId=>(string)state["attemptId"];
  internal static void Need(bool b){if(!b)throw new ArgumentException("world_boss_rail_snapshot_invalid");}
  internal static void Fields(JObject o,params string[] keys){Need(o!=null&&o.Properties().Select(p=>p.Name).OrderBy(k=>k,StringComparer.Ordinal).SequenceEqual(keys.OrderBy(k=>k,StringComparer.Ordinal)));}
  internal static string Text(JToken o,string key,int max=200){Need(o?[key]?.Type==JTokenType.String);var s=(string)o[key];Need(s.Length>0&&s.Length<=max);return s;}
  internal static long Nat(JToken o,string key){Need(o?[key]?.Type==JTokenType.Integer);long v=(long)o[key];Need(v>=0&&v<=9007199254740991L);return v;}
  static bool Bool(JToken o,string key){Need(o?[key]?.Type==JTokenType.Boolean);return (bool)o[key];}
  static string Route(JToken o,string key){var v=Text(o,key);Need(v=="LEFT"||v=="RIGHT"||v=="ENTRY");return v;}
  static void NullableId(JToken v){Need(v!=null&&(v.Type==JTokenType.Null||v.Type==JTokenType.String&&((string)v).Length>0&&((string)v).Length<=200));}
  internal static string Canonical(JToken t)=>BasicCombatSimulation.StableJson(t);
  public WorldBossRailSnapshot(JObject snapshot,JObject trustedOriginalDefinition,string expectedAttempt){
   Need(trustedOriginalDefinition!=null&&snapshot!=null&&Canonical(snapshot).Length<=1048576);
   original=(JObject)trustedOriginalDefinition.DeepClone();state=(JObject)snapshot.DeepClone();
   Need(Text(original,"version")==Version&&Text(original,"status")=="LOCAL_CANDIDATE"&&original["rounds"] is JArray defs&&defs.Count==6);
   Need(Text(original["geometry"],"currentPathStatus")=="LOCAL_LOGICAL_ONLY_UNVERIFIED_Y");
   var mode=Text(original,"mode");Need((mode=="SOLO"||mode=="COOP")&&((mode=="SOLO")== (Text(original,"trainAccountId")==Text(original,"operatorAccountId"))));
   Fields(state,"version","attemptId","definition","revision","now","rounds","results","buff","receipts");
   Need(Text(state,"version")==Version&&Text(state,"attemptId")==expectedAttempt&&Canonical(state["definition"])==Canonical(original));
   Nat(state,"revision");Need(Nat(state,"now")>=Nat(original,"startedAt"));
   Need(state["rounds"] is JArray rounds&&rounds.Count==6&&state["results"] is JArray results&&results.Count<=6&&state["receipts"] is JArray receipts&&receipts.Count<=12);
   var ids=((JArray)original["rounds"]).Select(d=>Text(d,"roundId")).ToArray();Need(ids.Distinct().Count()==6);
   var resultIds=((JArray)state["results"]).Select(r=>Text(r,"roundId")).ToArray();Need(resultIds.Distinct().Count()==resultIds.Length);
   var boss=Text(original,"bossId");string[] patterns=boss=="world_boss_raider_king"?new[]{"rail-bombardment","traction-chain","main-gun-charge"}:boss=="world_boss_veil"?new[]{"boarding-leap","carriage-breach","retreat-reentry"}:boss=="world_boss_ort"?new[]{"rail-resonance","polarity-switch","resonance-backflow"}:throw new ArgumentException("Unsupported boss");
   for(int i=0;i<6;i++){
    var r=(JObject)state["rounds"][i];var d=original["rounds"][i];Fields(r,"roundId","status","route","confirmed","internalAction");Need(Text(r,"roundId")==ids[i]);var status=Text(r,"status");Need(new[]{"PENDING","OPEN","FINISHED"}.Contains(status));string route=Route(r,"route");Need(Bool(r,"confirmed")||route=="ENTRY");NullableId(r["internalAction"]);
    var result=((JArray)state["results"]).SingleOrDefault(x=>(string)x["roundId"]==ids[i]);Need((status=="FINISHED")== (result!=null));
    if(result==null)continue;
    Fields(result as JObject,"roundId","patternId","outcome","route","railSuccess","internalSuccess","resolvedAt","failureAttacks","directScore");
    Need(Text(result,"patternId")==patterns[i/2]&&Route(result,"route")==route&&Nat(result,"resolvedAt")<=Now&&Nat(result,"directScore")==0);
    var outcome=Text(result,"outcome");Need(new[]{"FULL_SUCCESS","PARTIAL_SUCCESS","FAILURE","CANCELLED","SKIPPED"}.Contains(outcome));
    bool normal=outcome!="CANCELLED"&&outcome!="SKIPPED",rail=normal&&(route=="ENTRY"?Bool(d,"entrySafe"):((JArray)d["safeSides"]).Any(s=>(string)s==route)),inside=normal&&(i<2||JToken.DeepEquals(r["internalAction"],d["internalActions"][route]));
    Need(Bool(result,"railSuccess")==rail&&Bool(result,"internalSuccess")==inside);
    var attacks=result["failureAttacks"] as JArray;Need(attacks!=null&&attacks.Count==(normal&&!(rail&&inside)?1:0));
    if(normal)Need(outcome==(rail&&inside?"FULL_SUCCESS":rail||i>=2&&inside?"PARTIAL_SUCCESS":"FAILURE"));
    foreach(var a in attacks){Fields(a as JObject,"eventId","reason","attackRef","targetRef","defensePipeline","carriageTransfer","penalty");Need(Text(a,"eventId",3000)==Canonical(new JArray(expectedAttempt,ids[i],"failure"))&&Text(a,"reason")==(!rail?"RAIL_FAILURE":"INTERNAL_FAILURE")&&Text(a,"attackRef")==Text(d,!rail?"railAttackRef":"internalAttackRef")&&Text(a,"targetRef")==Text(d,"targetRef")&&Text(a,"defensePipeline")=="SUPPORTED_DEF_REDUCTION_SHIELD"&&!Bool(a,"carriageTransfer")&&!Bool(a,"penalty"));}
   }
   Need(resultIds.All(id=>ids.Contains(id)));var keys=new System.Collections.Generic.HashSet<string>();
   foreach(var rec in (JArray)state["receipts"]){
    Fields(rec as JObject,"accountId","commandId","digest","receipt");string account=Text(rec,"accountId"),key=Text(rec,"commandId");Need(keys.Add(Canonical(new JArray(account,key))));
    var body=JObject.Parse(Text(rec,"digest",20000));ValidateCommand(body,expectedAttempt,Text(original,"contentVersion"));Need(Canonical(body)==(string)rec["digest"]&&Text(body,"commandId")==key);
    var d=((JArray)original["rounds"]).SingleOrDefault(t=>(string)t["roundId"]==(string)body["roundId"]);Need(d!=null);var receipt=rec["receipt"];ValidateReceipt(receipt as JObject,body);Need(Nat(receipt,"acceptedRevision")<=Revision&&Nat(receipt,"acceptedAt")<=Now);
    var routeRequest=(string)body["action"]["kind"]=="CONFIRM_ROUTE";long at=Nat(receipt,"acceptedAt")-Nat(original,"startedAt");Need(account==Text(original,routeRequest?"trainAccountId":"operatorAccountId")&&at>=Nat(d,routeRequest?"previewAt":"internalOpenAt")&&at<Nat(d,routeRequest?"lockAt":"internalCloseAt"));
    var current=((JArray)state["rounds"]).Single(r=>(string)r["roundId"]==(string)body["roundId"]);Need(receipt["route"].ToString()==current["route"].ToString());if(!routeRequest)Need(JToken.DeepEquals(receipt["internalAction"],current["internalAction"]));
   }
   if(state["buff"].Type!=JTokenType.Null){var b=state["buff"];Fields(b as JObject,"roundId","multiplierBps","startsAt","endsAt");var res=((JArray)state["results"]).SingleOrDefault(r=>(string)r["roundId"]==(string)b["roundId"]);var d=((JArray)original["rounds"]).SingleOrDefault(r=>(string)r["roundId"]==(string)b["roundId"]);Need(res!=null&&d!=null&&(string)res["outcome"]=="FULL_SUCCESS"&&Nat(b,"startsAt")==Nat(res,"resolvedAt")&&Nat(b,"multiplierBps")==Nat(d,"buffBps")&&Nat(b,"endsAt")==Math.Min(Nat(b,"startsAt")+Nat(d,"buffDurationMs"),Nat(original,"startedAt")+180000));}
  }
  internal static void ValidateCommand(JObject b,string attempt,string content){
   Fields(b,"version","attemptId","contentVersion","roundId","commandId","expectedRevision","action");Need(Text(b,"version")==Version&&Text(b,"attemptId")==attempt&&Text(b,"contentVersion")==content);Text(b,"roundId");Text(b,"commandId");Nat(b,"expectedRevision");var action=b["action"] as JObject;var kind=Text(action,"kind");if(kind=="CONFIRM_ROUTE"){Fields(action,"kind","side");Need(Route(action,"side")!="ENTRY");}else{Need(kind=="INTERNAL");Fields(action,"kind","actionId");Text(action,"actionId");}
  }
  internal static void ValidateReceipt(JObject r,JObject body){Fields(r,"commandId","roundId","acceptedRevision","acceptedAt","route","internalAction");Need(Text(r,"commandId")==Text(body,"commandId")&&Text(r,"roundId")==Text(body,"roundId")&&Nat(r,"acceptedRevision")==Nat(body,"expectedRevision")+1);Nat(r,"acceptedAt");Route(r,"route");NullableId(r["internalAction"]);if((string)body["action"]["kind"]=="CONFIRM_ROUTE")Need(JToken.DeepEquals(r["route"],body["action"]["side"])&&r["internalAction"].Type==JTokenType.Null);else Need(JToken.DeepEquals(r["internalAction"],body["action"]["actionId"]));}
 }
}