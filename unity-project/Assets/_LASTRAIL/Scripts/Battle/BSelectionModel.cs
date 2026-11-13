using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 internal static class BSelectionContract {
  internal static void Check(bool ok){if(!ok)throw new ArgumentException("combat_b_selection_invalid");}
  internal static void Fields(JObject o,params string[] keys)=>WorldBossRailSnapshot.Fields(o,keys);
  internal static string Text(JToken o,string key)=>WorldBossRailSnapshot.Text(o,key,128);
  internal static long Number(JToken o,string key){long n=WorldBossRailSnapshot.Nat(o,key);Check(n<9007199254740991L);return n;}
  internal static void Closed(JObject o){Check(Text(o,"version")==BSelectionRequest.Version&&o["startReady"]?.Type==JTokenType.Boolean&&!(bool)o["startReady"]);}
  internal static void Saved(JObject o){Fields(o,"version","revision");Text(o,"version");Check(Number(o,"revision")>0);}
  internal static void Strings(JToken token){Check(token is JArray);var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var id in (JArray)token)Check(id.Type==JTokenType.String&&((string)id).Length>0&&((string)id).Length<=128&&ids.Add((string)id));}
 }
 // This v2 next-selection definition neither renames historical v1 nor creates inventory instances.
 public sealed class BSelectionDefinition {
  public const string Version="combat.b-placement.v2.20261002";
  readonly JObject value;
  public JObject Capture()=>(JObject)value.DeepClone();
  public BSelectionDefinition(JObject input){
   BSelectionContract.Fields(input,"version","mountedTurrets","placements");BSelectionContract.Check(BSelectionContract.Text(input,"version")==Version&&input["mountedTurrets"] is JArray turrets&&turrets.Count==0&&input["placements"] is JArray);
   var rows=(JArray)input["placements"];BSelectionContract.Check(rows.Count>=1&&rows.Count<=5);var slots=new HashSet<long>();var operators=new HashSet<string>(StringComparer.Ordinal);
   foreach(var row in rows){BSelectionContract.Fields(row as JObject,"slot","carriageId","occupant");long slot=BSelectionContract.Number(row,"slot");BSelectionContract.Check(slot<=4&&slots.Add(slot));BSelectionContract.Text(row,"carriageId");var occupant=row["occupant"] as JObject;string kind=BSelectionContract.Text(occupant,"kind");if(kind=="OPERATOR"){BSelectionContract.Fields(occupant,"kind","operatorId");BSelectionContract.Check(operators.Add(BSelectionContract.Text(occupant,"operatorId")));}else{BSelectionContract.Fields(occupant,"kind");BSelectionContract.Check(kind=="CARGO");}}
   BSelectionContract.Check(operators.Count>=1);value=(JObject)input.DeepClone();value["placements"]=new JArray(rows.OrderBy(r=>(long)r["slot"]).Select(r=>r.DeepClone()));
  }
 }
 public sealed class BSelectionRequest {
  public const string Version="combat.b-selection-http.v1.20261002";
  public const string Path="/_local/combat-b/selection";
  readonly JObject body;
  public string Method {get;}public string Key {get;}
  public JObject Body=>body==null?null:(JObject)body.DeepClone();
  BSelectionRequest(string method,JObject input,string key){Method=method;body=input;Key=key;}
  public static BSelectionRequest Read()=>new BSelectionRequest("GET",null,null);
  public static BSelectionRequest Save(long revision,string catalog,long catalogRevision,BSelectionDefinition definition,string originalKey){
   BSelectionContract.Check(revision>=0&&revision<9007199254740991L&&catalogRevision>0&&catalogRevision<9007199254740991L&&catalog!=null&&catalog.Length>0&&catalog.Length<=128&&definition!=null&&originalKey!=null&&originalKey.Length>=1&&originalKey.Length<=128&&originalKey.All(c=>c>=0x21&&c<=0x7e));
   return new BSelectionRequest("POST",new JObject{{"version",Version},{"expectedRevision",revision},{"catalogVersion",catalog},{"expectedCatalogRevision",catalogRevision},{"selection",definition.Capture()}},originalKey);
  }
 }
 // Read owns current catalog/source independently from the original saved selection/catalog.
 public sealed class BSelectionSnapshot {
  readonly JObject value;
  public long Revision=>(long)value["revision"];
  public long CatalogRevision=>(long)value["catalog"]["revision"];
  public string CatalogVersion=>(string)value["catalog"]["version"];
  public string EligibilityError=>(string)value["eligibilityError"];
  public bool StartReady=>false;
  public BSelectionDefinition Selection=>value["selection"].Type==JTokenType.Null?null:new BSelectionDefinition((JObject)value["selection"]);
  public JObject Capture()=>(JObject)value.DeepClone();
  public BSelectionSnapshot(JObject input){
   BSelectionContract.Fields(input,"version","revision","selection","savedCatalog","catalog","ownedSource","eligibilityError","startReady");BSelectionContract.Closed(input);long revision=BSelectionContract.Number(input,"revision");
   bool empty=input["selection"].Type==JTokenType.Null;BSelectionContract.Check(empty==(revision==0)&&empty==(input["savedCatalog"].Type==JTokenType.Null));
   if(!empty){new BSelectionDefinition((JObject)input["selection"]);BSelectionContract.Saved(input["savedCatalog"] as JObject);}
   var catalog=input["catalog"] as JObject;BSelectionContract.Fields(catalog,"version","status","unlockedSlotCount","carriages","revision");BSelectionContract.Text(catalog,"version");BSelectionContract.Check(BSelectionContract.Text(catalog,"status")=="ISOLATED_LOCAL_FIXTURE"&&BSelectionContract.Number(catalog,"unlockedSlotCount")<=5&&BSelectionContract.Number(catalog,"revision")>0&&catalog["carriages"] is JArray);
   var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var row in (JArray)catalog["carriages"]){BSelectionContract.Fields(row as JObject,"carriageId","occupants");BSelectionContract.Check(ids.Add(BSelectionContract.Text(row,"carriageId")));BSelectionContract.Strings(row["occupants"]);BSelectionContract.Check(((JArray)row["occupants"]).Count>0&&row["occupants"].All(k=>(string)k=="OPERATOR"||(string)k=="CARGO"));}
   var owned=input["ownedSource"] as JObject;BSelectionContract.Fields(owned,"carriages","operators");BSelectionContract.Strings(owned["carriages"]);BSelectionContract.Strings(owned["operators"]);
   BSelectionContract.Check(input["eligibilityError"].Type==JTokenType.Null||input["eligibilityError"].Type==JTokenType.String);if(input["eligibilityError"].Type==JTokenType.String)BSelectionContract.Text(input,"eligibilityError");
   BSelectionContract.Check(!empty||input["eligibilityError"].Type==JTokenType.Null);value=(JObject)input.DeepClone();
  }
  internal BSelectionSnapshot WithReceipt(JObject receipt){var next=Capture();next["revision"]=receipt["revision"].DeepClone();next["selection"]=receipt["selection"].DeepClone();next["savedCatalog"]=receipt["savedCatalog"].DeepClone();next["eligibilityError"]=null;return new BSelectionSnapshot(next);}
 }
 public sealed class BSelectionFailure {
  public int Status {get;}public string Code {get;}
  internal BSelectionFailure(int status,JObject input){BSelectionContract.Check(status>=400&&status<=599);BSelectionContract.Fields(input,"accepted","errorCode");BSelectionContract.Check(input["accepted"]?.Type==JTokenType.Boolean&&!(bool)input["accepted"]);Code=BSelectionContract.Text(input,"errorCode");Status=status;}
 }
}
