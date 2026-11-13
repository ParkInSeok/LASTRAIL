using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 internal static class BRefitWire {
  internal static void Need(bool value)=>BSelectionContract.Check(value);
  internal static void Closed(JObject x){Need((string)x["version"]==BRefitRequest.Version&&x["startReady"]?.Type==JTokenType.Boolean&&!(bool)x["startReady"]);}
  internal static string Hash(JToken x){Need(x?.Type==JTokenType.String&&((string)x).Length==64&&Regex.IsMatch((string)x,"^[a-f0-9]{64}$"));return (string)x;}
  internal static string DefinitionHash(JObject x)=>BDirectAttackInput.Digest(new JObject{["version"]=x["buffVersion"].DeepClone(),["definitions"]=x["definitions"].DeepClone()}).Substring(7);
 }
 public sealed class BRefitSnapshot {
  readonly JObject value;public string AccountId {get;}public long Revision=>(long)value["revision"];public long SelectionRevision=>(long)value["selectionRevision"];public long RankRevision=>(long)value["rankSource"]["revision"];
  public string DefinitionHash=>(string)value["definitionHash"];public string SourceHash=>(string)value["sourceHash"];public string EligibilityError=>(string)value["eligibilityError"];public bool StartReady=>false;
  public JObject Refit=>value["refit"].Type==JTokenType.Null?null:(JObject)value["refit"].DeepClone();public JObject Capture()=>(JObject)value.DeepClone();
  public BRefitSnapshot(JObject x,string account){
   if(account==null)throw new ArgumentNullException(nameof(account));AccountId=account;
   BSelectionContract.Fields(x,"version","buffVersion","definitionHash","sourceHash","definitions","revision","refit","savedDefinitionHash","savedSourceHash","selectionRevision","rankSource","eligibilityError","startReady");BRefitWire.Closed(x);BRefitWire.Need((string)x["buffVersion"]==BCarriageBuffDefinitions.Version);
   BRefitWire.Hash(x["sourceHash"]);BRefitWire.Need(BRefitWire.Hash(x["definitionHash"])==BRefitWire.DefinitionHash(x));
   var original=new JObject{["version"]=x["buffVersion"].DeepClone(),["definitions"]=x["definitions"].DeepClone()};var json=BasicCombatSimulation.StableJson(original);new BCarriageBuffDefinitions(json,BDirectAttackInput.Digest(original).Substring(7));
   BSelectionContract.Number(x,"revision");BRefitWire.Need(BSelectionContract.Number(x,"selectionRevision")>0);var rank=x["rankSource"] as JObject;BSelectionContract.Fields(rank,"revision","status","ranks","instances");BRefitWire.Need(BSelectionContract.Number(rank,"revision")>0&&(string)rank["status"]=="ISOLATED_LOCAL_FIXTURE");
   var ranks=BuffWire.Array(rank["ranks"],7,7);var types=new HashSet<string>(StringComparer.Ordinal);foreach(var token in ranks){var r=BuffWire.Object(token);BuffWire.Fields(r,"type","rank");BuffWire.Need(types.Add(BuffWire.Type(r["type"])));BuffWire.Number(r["rank"],1,5);}
   var slots=new HashSet<long>();var instances=new HashSet<string>(StringComparer.Ordinal);foreach(var token in BuffWire.Array(rank["instances"],1,5)){var i=BuffWire.Object(token);BuffWire.Fields(i,"slot","instanceId","carriageId");BuffWire.Need(slots.Add(BuffWire.Number(i["slot"],0,4))&&instances.Add(BuffWire.Text(i["instanceId"])));BuffWire.Text(i["carriageId"]);}
   bool empty=x["refit"].Type==JTokenType.Null;BRefitWire.Need(empty==((long)x["revision"]==0)&&empty==(x["savedDefinitionHash"].Type==JTokenType.Null)&&empty==(x["savedSourceHash"].Type==JTokenType.Null));
   BRefitWire.Need(x["eligibilityError"].Type==JTokenType.Null||x["eligibilityError"].Type==JTokenType.String);if(x["eligibilityError"].Type==JTokenType.String)BSelectionContract.Text(x,"eligibilityError");
   if(empty)BRefitWire.Need(x["eligibilityError"].Type==JTokenType.Null);
   else{var d=BuffWire.Object(x["refit"]);BCarriageBuffModel.ValidateDeparture(d);BRefitWire.Need((string)d["accountId"]==account);BRefitWire.Hash(x["savedDefinitionHash"]);BRefitWire.Hash(x["savedSourceHash"]);
    if(x["eligibilityError"].Type==JTokenType.Null){BRefitWire.Need((string)x["savedDefinitionHash"]==(string)x["definitionHash"]&&(string)x["savedSourceHash"]==(string)x["sourceHash"]);ValidateCurrent(d,x);}}
   value=(JObject)x.DeepClone();
  }
  internal static void ValidateCurrent(JObject candidate,JObject snapshot){
   BRefitWire.Need((long)candidate["selectionRevision"]==(long)snapshot["selectionRevision"]&&(long)candidate["rankRevision"]==(long)snapshot["rankSource"]["revision"]);var instances=(JArray)snapshot["rankSource"]["instances"];var cars=(JArray)candidate["cars"];BRefitWire.Need(cars.Count==instances.Count);
   foreach(var c in cars){var i=instances.SingleOrDefault(t=>(long)t["slot"]==(long)c["slot"]);BRefitWire.Need(i!=null&&(string)i["instanceId"]==(string)c["instanceId"]&&(string)i["carriageId"]==(string)c["carriageId"]);var rank=((JArray)snapshot["rankSource"]["ranks"]).Single(r=>(string)r["type"]==(string)c["type"]);BRefitWire.Need((long)rank["rank"]==(long)c["rank"]);}
  }
 }
 public sealed class BRefitRequest {
  public const string Version="combat.b-refit-http.v1.20261002",Path="/_local/combat-b/refit";
  readonly JObject body,originalSource;public string Method {get;}public string Key {get;}public string AccountId {get;}public JObject Body=>body==null?null:(JObject)body.DeepClone();internal JObject OriginalSource=>originalSource==null?null:(JObject)originalSource.DeepClone();
  BRefitRequest(string method,string key,JObject data,BRefitSnapshot source){Method=method;Key=key;body=data;originalSource=source?.Capture();AccountId=source?.AccountId;}
  public static BRefitRequest Read()=>new BRefitRequest("GET",null,null,null);
  public static BRefitRequest Save(BRefitSnapshot source,JArray refits,string key){
   BRefitWire.Need(source!=null&&key!=null&&key.Length>=1&&key.Length<=128&&key.All(ch=>ch>=0x21&&ch<=0x7e));var original=source.Capture();var rows=BuffWire.Array(refits,1,5);var seen=new HashSet<long>();var instances=(JArray)original["rankSource"]["instances"];BRefitWire.Need(rows.Count==instances.Count);
   foreach(var token in rows){var r=BuffWire.Object(token);BuffWire.Fields(r,"slot","type","rank");long slot=BuffWire.Number(r["slot"],0,4);BuffWire.Need(seen.Add(slot)&&instances.Any(i=>(long)i["slot"]==slot));var type=BuffWire.Type(r["type"]);long rank=BuffWire.Number(r["rank"],1,5);BuffWire.Need(((JArray)original["rankSource"]["ranks"]).Any(t=>(string)t["type"]==type&&(long)t["rank"]==rank),"combat_b_rank_conflict");}
   BRefitWire.Need(source.Revision<BuffWire.Safe&&source.SelectionRevision<BuffWire.Safe&&source.RankRevision<BuffWire.Safe);
   return new BRefitRequest("POST",key,new JObject{["version"]=Version,["expectedRevision"]=source.Revision,["selectionRevision"]=source.SelectionRevision,["rankRevision"]=source.RankRevision,["definitionHash"]=source.DefinitionHash,["sourceHash"]=source.SourceHash,["refits"]=new JArray(rows.OrderBy(r=>(long)r["slot"]).Select(r=>r.DeepClone()))},source);
  }
  internal JObject ValidateReceipt(JObject x){
   BuffWire.Fields(x,"accepted","version","receiptId","revision","changed","definitionHash","sourceHash","refit","startReady");BRefitWire.Closed(x);BuffWire.Need(BuffWire.Flag(x["accepted"]));BuffWire.Text(x["receiptId"]);bool changed=BuffWire.Flag(x["changed"]);var d=BuffWire.Object(x["refit"]);BCarriageBuffModel.ValidateDeparture(d);
   BuffWire.Need((string)d["accountId"]==AccountId&&BRefitWire.Hash(x["definitionHash"])==(string)body["definitionHash"]&&BRefitWire.Hash(x["sourceHash"])==(string)body["sourceHash"]&&BuffWire.Number(x["revision"],1)==(long)body["expectedRevision"]+(changed?1:0));BRefitSnapshot.ValidateCurrent(d,originalSource);
   var expected=(JArray)body["refits"];var actual=(JArray)d["cars"];BuffWire.Need(expected.Count==actual.Count);foreach(var r in expected)BuffWire.Need(actual.Any(c=>(long)c["slot"]==(long)r["slot"]&&(string)c["type"]==(string)r["type"]&&(long)c["rank"]==(long)r["rank"]));return (JObject)x.DeepClone();
  }
 }
 public sealed class BRefitHttpResponse {
  readonly JObject body;public int Status {get;}public string Version {get;}public string CacheControl {get;}public JObject Body=>(JObject)body.DeepClone();
  public BRefitHttpResponse(int status,string cache,string version,JObject input){Status=status;CacheControl=cache;Version=version;body=(JObject)(input??throw new ArgumentNullException(nameof(input))).DeepClone();}
 }
}