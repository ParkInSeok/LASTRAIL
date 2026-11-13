using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // A launched SINGLE hazard. The existing train owner must commit this receipt and resolved HP together.
 public sealed class RailArtilleryHazard {
  public const string Version="rail.artillery-hazard.v1.20261023";
  public const string ExactVersion="rail.artillery-hazard.v2.20261027";
  readonly JObject definition;
  JObject receipt;
  public long? NextImpactAtMs=>receipt==null?(long?)definition["timing"]["impactAtMs"]:null;
  public RailArtilleryHazard(JObject input){
   Fields(input,"version","runId","eventId","trainId","railSegmentId","carriageIds","startMm","endMm","previewAtMs","timing","damage");
   bool exact=(string)input["version"]==ExactVersion;Need(exact||(string)input["version"]==Version,"version");
   foreach(string key in new[]{"runId","eventId","trainId","railSegmentId"})Id(input[key]);
   var cars=Rows(input["carriageIds"],128);foreach(var car in cars)Id(car);
   Need(cars.Values<string>().Distinct(StringComparer.Ordinal).Count()==cars.Count,"carriages");
   long start=Natural(input["startMm"]),end=Natural(input["endMm"]),preview=Natural(input["previewAtMs"]);
   Need(start<end,"bounds");
   if(exact){var damage=CombatExactValue.From(input["damage"] as JObject);Need(damage.Numerator>0&&JToken.DeepEquals(input["damage"],damage.Snapshot()),"exact_damage");}
   else Need(Natural(input["damage"])>0,"bounds");
   var timing=input["timing"] as JObject;Fields(timing,"kind","impactAtMs");
   // shortcut: only the approved SINGLE slice is consumed; add RESIDUAL when its product caller is implemented.
   Need((string)timing["kind"]=="SINGLE"&&preview<=Natural(timing["impactAtMs"]),"single_timing");
   definition=(JObject)input.DeepClone();
  }
  public JObject Resolve(JObject input){
   Fields(input,"runId","trainId","atMs","spans");Id(input["runId"]);Id(input["trainId"]);long at=Natural(input["atMs"]);
   Need((string)input["runId"]==(string)definition["runId"]&&(string)input["trainId"]==(string)definition["trainId"],"scope");
   var spans=Rows(input["spans"],1024);
   foreach(var token in spans){var span=token as JObject;Fields(span,"carriageId","railSegmentId","startMm","endMm");Id(span["carriageId"]);Id(span["railSegmentId"]);Need(Natural(span["startMm"])<=Natural(span["endMm"]),"span_bounds");}
   Need(spans.Select(s=>(string)s["carriageId"]).ToHashSet(StringComparer.Ordinal).SetEquals(((JArray)definition["carriageIds"]).Values<string>()),"incomplete_consist");
   Need(spans.Select(Canonical).Distinct(StringComparer.Ordinal).Count()==spans.Count,"duplicate_span");
   var frame=(JObject)input.DeepClone();frame["spans"]=new JArray(spans.OrderBy(Canonical,StringComparer.Ordinal).Select(s=>s.DeepClone()));
   if(receipt!=null&&at==(long)receipt["input"]["atMs"]){Need(Canonical(receipt["input"])==Canonical(frame),"event_conflict");return (JObject)receipt["result"].DeepClone();}
   Need(at==NextImpactAtMs,"impact_time");
   var hits=spans.Where(s=>(string)s["railSegmentId"]==(string)definition["railSegmentId"]&&(long)s["startMm"]<=(long)definition["endMm"]&&(long)s["endMm"]>=(long)definition["startMm"])
    .Select(s=>(string)s["carriageId"]).Distinct(StringComparer.Ordinal).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
   JToken damage=hits.Length>0?definition["damage"].DeepClone():(string)definition["version"]==ExactVersion?(JToken)new JObject{["numerator"]="0",["denominator"]="1"}:new JValue(0);
   var result=new JObject{["version"]=definition["version"].DeepClone(),["identity"]=new JObject{["runId"]=definition["runId"].DeepClone(),["eventId"]=definition["eventId"].DeepClone(),["impactIndex"]=0},
    ["trainId"]=definition["trainId"].DeepClone(),["atMs"]=at,["outcome"]=hits.Length>0?"HIT":"MISSED",["rawDamageIntent"]=damage,["intersectingCarriageIds"]=new JArray(hits)};
   receipt=new JObject{["input"]=frame,["result"]=result};return (JObject)result.DeepClone();
  }
  public JObject Snapshot()=>new JObject{["version"]=definition["version"].DeepClone(),["definition"]=definition.DeepClone(),["receipts"]=receipt==null?new JArray():new JArray(receipt.DeepClone())};
  public static RailArtilleryHazard Restore(JObject saved,JObject originalDefinition){
   Fields(saved,"version","definition","receipts");Need(JToken.DeepEquals(saved["version"],originalDefinition?["version"])&&Canonical(saved["definition"])==Canonical(originalDefinition),"original_definition");
   var owner=new RailArtilleryHazard(originalDefinition);var rows=saved["receipts"] as JArray;Need(rows!=null&&rows.Count<=1,"receipt_count");
   foreach(var token in rows){var row=token as JObject;Fields(row,"input","result");Need(Canonical(owner.Resolve(row["input"] as JObject))==Canonical(row["result"]),"restore_result");}
   return owner;
  }
  static string Canonical(JToken value)=>BasicCombatSimulation.StableJson(value);
  static void Fields(JObject value,params string[] keys)=>BDirectAttackInput.Fields(value,keys);
  static void Id(JToken value)=>Need(value?.Type==JTokenType.String&&((string)value).Length>=1&&((string)value).Length<=128,"id");
  static long Natural(JToken value){Need(value?.Type==JTokenType.Integer,"number");long n=(long)value;Need(n>=0&&n<=9007199254740991,"number");return n;}
  static JArray Rows(JToken value,int maximum){var rows=value as JArray;Need(rows!=null&&rows.Count>=1&&rows.Count<=maximum,"rows");return rows;}
  static void Need(bool condition,string reason){if(!condition)throw new ArgumentException("rail_hazard_"+reason);}
 }
}
