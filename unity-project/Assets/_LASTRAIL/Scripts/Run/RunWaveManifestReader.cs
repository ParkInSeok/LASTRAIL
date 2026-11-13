using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 internal static class RunWaveManifestReader {
  internal const int ManifestBytes=524288;
  internal static void Need(bool value){if(!value)throw new ArgumentException("combat_b_wave_pinned_conflict");}
  internal static JObject Obj(JToken p)=>p as JObject??throw new ArgumentException("combat_b_wave_packet_invalid");
  internal static void Fields(JObject p,params string[] names)=>Need(p.Count==names.Length&&names.All(n=>p[n]!=null));
  internal static JArray Rows(JToken p,int min,int max){var a=p as JArray;Need(a!=null&&a.Count>=min&&a.Count<=max);return a;}
  internal static string Id(JToken p,int max=200){Need(p?.Type==JTokenType.String&&((string)p).Length>0&&((string)p).Length<=max);return (string)p;}
  internal static string Hash(JToken p){Need(p?.Type==JTokenType.String&&Regex.IsMatch((string)p,"\\A[a-f0-9]{64}\\z"));return (string)p;}
  internal static void NullableHash(JToken p){Need(p!=null);if(p.Type!=JTokenType.Null)Hash(p);}
  internal static long Nat(JToken p,long min=0,long max=9007199254740991){Need(p?.Type==JTokenType.Integer);long n;try{n=(long)p;}catch(OverflowException){throw new ArgumentException("combat_b_wave_number_invalid");}Need(n>=min&&n<=max);return n;}
  internal static void One(JToken p,params string[] values)=>Need(p?.Type==JTokenType.String&&values.Contains((string)p));
  internal static void Null(JToken p)=>Need(p?.Type==JTokenType.Null);
  internal static string Digest(JToken p)=>RunWaveCanonicalJson.Digest(p);
  internal static void Bounded(JToken p)=>Need(Encoding.UTF8.GetByteCount(RunWaveCanonicalJson.Canonical(p))<=ManifestBytes);
  internal static void SelfHash(JObject p,string name){var h=Hash(p[name]);var unsigned=(JObject)p.DeepClone();unsigned.Remove(name);Need(Digest(unsigned)==h);}
  internal static JObject Parse(string json){
   // Transport budget is distinct from the supplier's canonical proposal/pin byte limit.
   Need(json!=null&&Encoding.UTF8.GetByteCount(json)<=2*1024*1024);
   using(var scan=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=64}){while(scan.Read())Need(scan.TokenType!=JsonToken.Comment&&scan.TokenType!=JsonToken.Undefined&&scan.TokenType!=JsonToken.StartConstructor);}
   using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=64}){var p=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});Need(!reader.Read()&&!p.Descendants().Any(t=>t.Type==JTokenType.Comment));RunWaveCanonicalJson.CorrectParsedNumbers(p,json);return p;}
  }
  internal static void DepartureOrigin(JObject d){Fields(d,"accountId","runId","operationPreviewId","operationId","selectedRisk","departureVersion","baseSnapshotHash","definitionVersion","planHash","publicationHash");foreach(var k in new[]{"accountId","runId","operationPreviewId","operationId","departureVersion","definitionVersion"})Id(d[k]);foreach(var k in new[]{"baseSnapshotHash","planHash","publicationHash"})Hash(d[k]);One(d["selectedRisk"],"I","II","III","IV","V");}
  internal static void Origin(JObject p){
   Fields(p,"departure","contextId","profileRef","parentDefinitionHash","parentRawProfilesHash","riskApplicationVersion");var d=Obj(p["departure"]);DepartureOrigin(d);Need(Id(p["contextId"])!=(string)d["runId"]);var f=Obj(p["profileRef"]);Fields(f,"id","version","executorVersion");foreach(var k in new[]{"id","version","executorVersion"})Id(f[k]);Hash(p["parentDefinitionHash"]);Hash(p["parentRawProfilesHash"]);Need((string)p["riskApplicationVersion"]=="combat.b-operation-risk.v1.20261003");
  }
  internal static JObject TrustedCopy(JObject input){
   Need(input!=null);var t=(JObject)input.DeepClone();Fields(t,"accountId","runId","startSnapshotHash","parentPinHash","manifestHash","packetHash","origin","plan");Id(t["accountId"]);Id(t["runId"]);Hash(t["startSnapshotHash"]);Hash(t["packetHash"]);NullableHash(t["parentPinHash"]);NullableHash(t["manifestHash"]);
   if(t["manifestHash"].Type==JTokenType.Null){Null(t["origin"]);Null(t["plan"]);}else{Need(t["parentPinHash"].Type!=JTokenType.Null);var o=Obj(t["origin"]);Origin(o);Need((string)o["departure"]["accountId"]==(string)t["accountId"]&&(string)o["departure"]["runId"]==(string)t["runId"]);var plan=Obj(t["plan"]);Need(Digest(plan)==(string)o["departure"]["planHash"]&&(string)plan["definitionVersion"]==(string)o["departure"]["definitionVersion"]);}
   return t;
  }
  static void Raw(JObject p,bool hp){Fields(p,hp?new[]{"sourceId","kind","value","riskAppliedVersion"}:new[]{"sourceId","kind","value","riskAppliedVersion","progressAppliedVersion"});Id(p["sourceId"],128);One(p["kind"],hp?new[]{"BASE_HP","EHP_BUDGET"}:new[]{"RAW_ACTION_DAMAGE"});Null(p["riskAppliedVersion"]);if(!hp)Null(p["progressAppliedVersion"]);var exact=CombatExactValue.From(Obj(p["value"]));if(hp)Need(exact.Numerator>0);}
  internal static JObject Pressure(){
   var bands=new[]{"EARLY","MIDDLE","LATE"};var from=new[]{1,5,9};var to=new[]{4,8,11};var minimum=new[]{6,9,13};var maximum=new[]{8,12,16};var alive=new[]{4,6,8};var first=new[]{4,4,6};var interval=new[]{1800,1500,1200};
   return new JObject{["version"]="combat.b-wave-pressure.v3.20261003",["battlefield"]="ROOF_EXTERIOR",["stages"]=new JArray(Enumerable.Range(0,3).Select(i=>new JObject{["fromDepth"]=from[i],["toDepth"]=to[i],["minimumCount"]=minimum[i],["maximumCount"]=maximum[i],["aliveCap"]=alive[i],["firstGroup"]=first[i],["nextGroupMaximum"]=2,["entryIntervalMs"]=interval[i],["attackSlots"]=BEnemyAttackAdmissionPolicy.ForBand(bands[i]).GlobalSlots})),["perTargetAttackSlots"]=2,["targetPreparationIntervalMs"]=600,["entranceGraceMs"]=600,["basicTrial"]=new JObject{["rawDamage"]=24,["periodMs"]=1600,["preparationMs"]=500,["recoveryMs"]=300},["namedEnemyMappingApproved"]=false,["executionRules"]=new JObject{["aliveIncludesEntranceAndClimbing"]=true,["skipFullEntryOpportunity"]=true,["noCatchupEntries"]=true,["combatClockOnly"]=true,["entryGraceIsNotClimbingTime"]=true,["slotCancellationDoesNotAdvanceReuse"]=true,["targetGenerationLocked"]=true,["completionRequiresPlannedAndLivingAndResidualZero"]=true,["rewardCountIndependent"]=true}};
  }
  static void Manifest(JObject m,JObject trust){
   Bounded(m);Fields(m,"version","contextId","templates","waves","origin","pressurePolicy","manifestHash");Need((string)m["version"]==RunWaveManifestSnapshot.Version&&Id(m["contextId"])==(string)trust["origin"]["contextId"]&&JToken.DeepEquals(m["origin"],trust["origin"])&&JToken.DeepEquals(m["pressurePolicy"],Pressure()));SelfHash(m,"manifestHash");Need((string)m["manifestHash"]==(string)trust["manifestHash"]);
   var proposal=new JObject{["version"]=m["version"].DeepClone(),["contextId"]=m["contextId"].DeepClone(),["templates"]=m["templates"].DeepClone(),["waves"]=m["waves"].DeepClone()};Bounded(proposal);
   var templates=new Dictionary<string,JObject>(StringComparer.Ordinal);
   foreach(var row in Rows(m["templates"],1,32)){var t=Obj(row);Fields(t,"templateId","version","definitionId","definition","hp","actions","templateHash");string id=Id(t["templateId"]);Need(!templates.ContainsKey(id));Id(t["version"]);Id(t["definitionId"]);Obj(t["definition"]);Raw(Obj(t["hp"]),true);var actions=new HashSet<string>();foreach(var action in Rows(t["actions"],1,32)){var a=Obj(action);Raw(a,false);Need(actions.Add((string)a["sourceId"]));}SelfHash(t,"templateHash");templates.Add(id,t);}
   // The original plan is independently verified by its owner; this reader validates only wave references/order.
   var nodes=Rows(trust["plan"]["nodes"],1,78).Select(Obj).ToArray();Need(nodes.Select(n=>Id(n["nodeId"])).Distinct().Count()==nodes.Length);var ids=new HashSet<string>();var covered=new Dictionary<string,HashSet<long>>();int lastNode=-1;long lastWave=0;
   foreach(var row in Rows(m["waves"],2,128)){var w=Obj(row);Fields(w,"nodeId","visitDepth","waveOrdinal","instances");string nodeId=Id(w["nodeId"]);long depth=Nat(w["visitDepth"],1,11),ordinal=Nat(w["waveOrdinal"],1,2);int index=Array.FindIndex(nodes,n=>(string)n["nodeId"]==nodeId);Need(index>=0&&(string)nodes[index]["eventKind"]=="COMBAT"&&Nat(nodes[index]["visitDepth"],1,12)==depth&&(index>lastNode||index==lastNode&&ordinal>lastWave));lastNode=index;lastWave=ordinal;if(!covered.TryGetValue(nodeId,out var waveOrdinals)){waveOrdinals=new HashSet<long>();covered.Add(nodeId,waveOrdinals);}Need(waveOrdinals.Add(ordinal)&&covered.Count<=64);
    int band=depth<=4?0:depth<=8?1:2,min=new[]{6,9,13}[band],max=new[]{8,12,16}[band],first=new[]{4,4,6}[band],interval=new[]{1800,1500,1200}[band];var instances=Rows(w["instances"],min,max);var groups=new Dictionary<long,int>();long prior=0;
    for(int i=0;i<instances.Count;i++){var x=Obj(instances[i]);Fields(x,"instanceId","ordinal","templateRef","entryGroup","entryNotBeforeMs");Need(ids.Add(Id(x["instanceId"]))&&Nat(x["ordinal"])==i);var r=Obj(x["templateRef"]);Fields(r,"templateId","version","templateHash");string templateId=Id(r["templateId"]);Id(r["version"]);Hash(r["templateHash"]);Need(templates.TryGetValue(templateId,out var template)&&JToken.DeepEquals(r["version"],template["version"])&&JToken.DeepEquals(r["templateHash"],template["templateHash"]));long group=Nat(x["entryGroup"]);Need((i!=0||group==0)&&group>=prior&&group<=prior+1&&Nat(x["entryNotBeforeMs"])==group*interval);prior=group;groups[group]=groups.TryGetValue(group,out var n)?n+1:1;}
    Need(groups.TryGetValue(0,out var initial)&&initial==first&&groups.All(g=>g.Key==0||g.Value<=2));
   }
   Need(covered.Values.All(v=>v.SetEquals(new long[]{1,2})));
  }
  internal static JObject Read(JObject input,JObject independentTrust){
   Need(input!=null);var p=(JObject)input.DeepClone();var trust=TrustedCopy(independentTrust);Fields(p,"version","accountId","runId","runStatus","startSnapshotHash","parentPinHash","status","manifest","canStart","newProfileAdmission","fullCombatSupported","manifestPinned","packetHash");Need((string)p["version"]==RunWaveManifestSnapshot.Version);foreach(var k in new[]{"accountId","runId"})Id(p[k]);foreach(var k in new[]{"startSnapshotHash","packetHash"})Hash(p[k]);NullableHash(p["parentPinHash"]);One(p["runStatus"],"ACTIVE","FINISH_PENDING","COMPLETED","FAILED","QUARANTINED","EXPIRED","ABANDONED");foreach(var k in new[]{"canStart","newProfileAdmission","fullCombatSupported"})Need(p[k].Type==JTokenType.Boolean&&!(bool)p[k]);Need(p["manifestPinned"].Type==JTokenType.Boolean);
   foreach(var k in new[]{"accountId","runId","startSnapshotHash","parentPinHash","packetHash"})Need(JToken.DeepEquals(p[k],trust[k]));SelfHash(p,"packetHash");
   if((bool)p["manifestPinned"]){Need((string)p["status"]=="PINNED_MANIFEST"&&trust["manifestHash"].Type!=JTokenType.Null);Manifest(Obj(p["manifest"]),trust);}else{Need((string)p["status"]=="HISTORICAL_NO_MANIFEST");Null(p["manifest"]);Null(trust["manifestHash"]);}return p;
  }
 }
}
